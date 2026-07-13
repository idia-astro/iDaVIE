using System;
using System.Collections.Generic;
using Interaction.Interfaces;
using LineRenderer;
using TMPro;
using UnityEngine;
using VolumeData;

namespace Interaction
{
    public sealed class LocomotionController : ILocomotionController
    {
        [Flags]
        private enum RotationAxes
        {
            None = 0,
            Roll = 1,
            Yaw = 2
        }

        private readonly Func<Transform[]> _getHandTransforms;
        private readonly Func<int> _getPrimaryHandIndex;
        private readonly Func<VolumeDataSetRenderer[]> _getDataSets;
        private readonly Func<TextMeshPro[]> _getHandInfo;
        private readonly Action _onIdleUpdate;
        private readonly Action<float> _setVignetteTarget;

        private readonly bool _inPlaceScaling;
        private readonly bool _scalingEnabled;
        private readonly float _rotationAxisCutoff;
        private readonly float _maxVignetteIntensity;

        private readonly float[] _startDataSetScales;
        private readonly Vector3[] _currentGripPositions;
        private Vector3 _startGripSeparation;
        private Vector3 _startGripCenter;
        private Vector3 _starGripForwardAxis;
        private float _previousControllerHeight;
        private float _rotationYawCumulative;
        private float _rotationRollCumulative;
        private RotationAxes _rotationAxes;
        private readonly PolyLine _lineAxisSeparation;
        private readonly PolyLine _lineRotationAxes;

        public LocomotionState CurrentState { get; private set; }

        public LocomotionController(
            Func<Transform[]> getHandTransforms,
            Func<int> getPrimaryHandIndex,
            Func<VolumeDataSetRenderer[]> getDataSets,
            Func<TextMeshPro[]> getHandInfo,
            Action onIdleUpdate,
            Action<float> setVignetteTarget,
            bool inPlaceScaling,
            bool scalingEnabled,
            float rotationAxisCutoff,
            float maxVignetteIntensity)
        {
            _getHandTransforms = getHandTransforms;
            _getPrimaryHandIndex = getPrimaryHandIndex;
            _getDataSets = getDataSets;
            _getHandInfo = getHandInfo;
            _onIdleUpdate = onIdleUpdate;
            _setVignetteTarget = setVignetteTarget;
            _inPlaceScaling = inPlaceScaling;
            _scalingEnabled = scalingEnabled;
            _rotationAxisCutoff = rotationAxisCutoff;
            _maxVignetteIntensity = maxVignetteIntensity;

            var dataSets = _getDataSets();
            _startDataSetScales = new float[dataSets?.Length ?? 0];
            _currentGripPositions = new Vector3[2];
            _startGripSeparation = Vector3.zero;
            _startGripCenter = Vector3.zero;
            CurrentState = LocomotionState.Idle;

            _lineRotationAxes = new PolyLine { Color = Color.white, Vertices = new List<Vector3>(new Vector3[3]) };
            _lineAxisSeparation = new PolyLine { Color = Color.red, Vertices = new List<Vector3>(new Vector3[3]) };
            _lineRotationAxes.Activate();
            _lineAxisSeparation.Activate();
        }

        public void TransitionToMoving()
        {
            CurrentState = LocomotionState.Moving;
            _setVignetteTarget?.Invoke(_maxVignetteIntensity);
            var handInfo = _getHandInfo?.Invoke();
            if (handInfo != null)
            {
                handInfo[_getPrimaryHandIndex()].enabled = false;
                handInfo[1 - _getPrimaryHandIndex()].enabled = false;
            }
        }

        public void TransitionToIdle()
        {
            CurrentState = LocomotionState.Idle;
            _setVignetteTarget?.Invoke(0f);
            _lineRotationAxes.Deactivate();
            _lineAxisSeparation.Deactivate();
        }

        public void TransitionToScaling()
        {
            CurrentState = LocomotionState.Scaling;
            var hands = _getHandTransforms();
            _startGripSeparation = hands[0].position - hands[1].position;
            _startGripCenter = (hands[0].position + hands[1].position) / 2.0f;
            _starGripForwardAxis = Vector3.Cross(Vector3.up, _startGripSeparation.normalized).normalized;
            _rotationYawCumulative = 0;
            _rotationRollCumulative = 0;
            _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

            var dataSets = _getDataSets();
            for (var i = 0; i < dataSets.Length; i++)
            {
                if (dataSets[i].isActiveAndEnabled)
                {
                    _startDataSetScales[i] = dataSets[i].transform.localScale.magnitude;
                }
            }

            _lineAxisSeparation.Vertices[0] = _currentGripPositions[0];
            _lineAxisSeparation.Vertices[1] = _startGripCenter;
            _lineAxisSeparation.Vertices[2] = _currentGripPositions[1];
            _lineAxisSeparation.Activate();

            _lineRotationAxes.Vertices[0] = _startGripCenter + _starGripForwardAxis * 0.1f;
            _lineRotationAxes.Vertices[1] = _startGripCenter;
            _lineRotationAxes.Vertices[2] = _startGripCenter + Vector3.up * 0.1f;
            _lineRotationAxes.Activate();

            var handInfo = _getHandInfo?.Invoke();
            if (handInfo != null)
            {
                handInfo[_getPrimaryHandIndex()].enabled = true;
                handInfo[1 - _getPrimaryHandIndex()].enabled = false;
            }
        }

        public void Update(float deltaTime)
        {
            switch (CurrentState)
            {
                case LocomotionState.Moving:
                    UpdateMoving();
                    break;
                case LocomotionState.Scaling:
                    UpdateScaling();
                    break;
                case LocomotionState.Idle:
                    _onIdleUpdate?.Invoke();
                    break;
                case LocomotionState.EditingThresholdMax:
                    UpdateEditingThreshold(true);
                    break;
                case LocomotionState.EditingThresholdMin:
                    UpdateEditingThreshold(false);
                    break;
                case LocomotionState.EditingZAxis:
                    UpdateEditingZAxis();
                    break;
            }
        }

        public void StartThresholdEditing(bool editingMax)
        {
            CurrentState = editingMax ? LocomotionState.EditingThresholdMax : LocomotionState.EditingThresholdMin;
            _setVignetteTarget?.Invoke(0f);
            _previousControllerHeight = _getHandTransforms()[_getPrimaryHandIndex()].position.y;
        }

        public void StartZAxisEditing()
        {
            CurrentState = LocomotionState.EditingZAxis;
            _setVignetteTarget?.Invoke(0f);
            _previousControllerHeight = _getHandTransforms()[_getPrimaryHandIndex()].position.y;
        }

        public void EndEditing()
        {
            CurrentState = LocomotionState.Idle;
            _setVignetteTarget?.Invoke(0f);
        }

        private void UpdateMoving()
        {
            var hands = _getHandTransforms();
            var dataSets = _getDataSets();
            for (var i = 0; i < 2; i++)
            {
                var previousPosition = _currentGripPositions[i];
                _currentGripPositions[i] = hands[i].position;
                var delta = _currentGripPositions[i] - previousPosition;
                for (var j = 0; j < dataSets.Length; j++)
                {
                    if (dataSets[j].isActiveAndEnabled)
                    {
                        dataSets[j].transform.position += delta;
                    }
                }
            }
        }

        private void UpdateScaling()
        {
            var hands = _getHandTransforms();
            var previousGripSeparation = _currentGripPositions[0] - _currentGripPositions[1];
            for (var i = 0; i < 2; i++)
            {
                _currentGripPositions[i] = hands[i].position;
            }

            var currentGripSeparation = _currentGripPositions[0] - _currentGripPositions[1];
            var currentGripCenter = (_currentGripPositions[0] + _currentGripPositions[1]) / 2.0f;
            float startGripDistance = _startGripSeparation.magnitude;
            float currentGripDistance = currentGripSeparation.magnitude;
            float scalingFactor = currentGripDistance / Mathf.Max(startGripDistance, 1.0e-6f);

            Vector3 previousGripDirectionXz = new Vector3(previousGripSeparation.x, 0, previousGripSeparation.z).normalized;
            Vector3 currentGripDirectionXz = new Vector3(currentGripSeparation.x, 0, currentGripSeparation.z).normalized;
            float angleYaw = Mathf.Asin(Vector3.Cross(previousGripDirectionXz, currentGripDirectionXz).y);

            Vector3 perpendicularAxis = Vector3.Cross(_starGripForwardAxis, Vector3.up);
            Vector3 previousGripDirectionRotationAxis = new Vector3(Vector3.Dot(perpendicularAxis, previousGripSeparation), Vector3.Dot(Vector3.up, previousGripSeparation), 0).normalized;
            Vector3 currentGripDirectionRotationAxis = new Vector3(Vector3.Dot(perpendicularAxis, currentGripSeparation), Vector3.Dot(Vector3.up, currentGripSeparation), 0).normalized;
            float angleRoll = Mathf.Asin(-Vector3.Cross(previousGripDirectionRotationAxis, currentGripDirectionRotationAxis).z);

            if ((_rotationAxes & RotationAxes.Yaw) == RotationAxes.Yaw)
            {
                _rotationYawCumulative += angleYaw * Mathf.Rad2Deg;
                if (Mathf.Abs(_rotationYawCumulative) >= _rotationAxisCutoff)
                {
                    _rotationAxes = RotationAxes.Yaw;
                }
            }

            if ((_rotationAxes & RotationAxes.Roll) == RotationAxes.Roll)
            {
                _rotationRollCumulative += angleRoll * Mathf.Rad2Deg;
                if (Mathf.Abs(_rotationRollCumulative) >= _rotationAxisCutoff)
                {
                    _rotationAxes = RotationAxes.Roll;
                }
            }

            var yawActive = (_rotationAxes & RotationAxes.Yaw) == RotationAxes.Yaw;
            var rollActive = (_rotationAxes & RotationAxes.Roll) == RotationAxes.Roll;
            var dataSets = _getDataSets();

            for (var i = 0; i < dataSets.Length; i++)
            {
                var dataSet = dataSets[i];
                if (!dataSet.isActiveAndEnabled)
                {
                    continue;
                }

                float initialScale = _startDataSetScales[i];
                float currentScale = dataSet.transform.localScale.magnitude;
                float newScale = Mathf.Max(1e-6f, initialScale * scalingFactor);

                if (_inPlaceScaling)
                {
                    if (_scalingEnabled)
                    {
                        Vector3 dataSetPositionWorldSpace = dataSet.transform.position;
                        Vector3 preScaleOffset = dataSetPositionWorldSpace - _startGripCenter;
                        float scaleRatio = newScale / currentScale;
                        dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                        dataSet.transform.position = _startGripCenter + preScaleOffset * scaleRatio;
                    }

                    Vector3 startGripPositionDataSpace = dataSet.transform.InverseTransformPoint(_startGripCenter);
                    if (yawActive) dataSet.transform.RotateAround(_startGripCenter, Vector3.up, angleYaw * Mathf.Rad2Deg);
                    if (rollActive) dataSet.transform.RotateAround(_startGripCenter, _starGripForwardAxis, angleRoll * Mathf.Rad2Deg);
                    Vector3 updatedPositionWorldSpace = dataSet.transform.TransformPoint(startGripPositionDataSpace);
                    dataSet.transform.position -= updatedPositionWorldSpace - _startGripCenter;
                }
                else
                {
                    if (_scalingEnabled)
                    {
                        dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                    }

                    if (yawActive)
                    {
                        var angleDegrees = angleYaw * Mathf.Rad2Deg;
                        _rotationYawCumulative += angleDegrees;
                        dataSet.transform.Rotate(Vector3.up, angleDegrees);
                    }

                    if (rollActive)
                    {
                        var angleDegrees = angleRoll * Mathf.Rad2Deg;
                        _rotationRollCumulative += angleDegrees;
                        dataSet.transform.Rotate(_starGripForwardAxis, angleDegrees);
                    }
                }
            }

            var rotationPoint = _inPlaceScaling ? _startGripCenter : currentGripCenter;
            _lineAxisSeparation.Vertices[0] = _currentGripPositions[0];
            _lineAxisSeparation.Vertices[1] = rotationPoint;
            _lineAxisSeparation.Vertices[2] = _currentGripPositions[1];
            _lineRotationAxes.Vertices[0] = _startGripCenter + _starGripForwardAxis * (rollActive ? 0.1f : 0.0f);
            _lineRotationAxes.Vertices[1] = rotationPoint;
            _lineRotationAxes.Vertices[2] = _startGripCenter + Vector3.up * (yawActive ? 0.1f : 0.0f);
        }

        private void UpdateEditingThreshold(bool editingMax)
        {
            var dataSets = _getDataSets();
            var handInfo = _getHandInfo?.Invoke();
            var controllerHeight = _getHandTransforms()[_getPrimaryHandIndex()].position.y;
            var delta = controllerHeight - _previousControllerHeight;
            _previousControllerHeight = controllerHeight;
            string cursorString = string.Empty;

            for (var i = 0; i < dataSets.Length; i++)
            {
                var dataSet = dataSets[i];
                if (editingMax)
                {
                    var newValue = dataSet.ThresholdMax + delta;
                    dataSet.ThresholdMax = Mathf.Clamp(newValue, dataSet.ThresholdMin, 1);
                }
                else
                {
                    var newValue = dataSet.ThresholdMin + delta;
                    dataSet.ThresholdMin = Mathf.Clamp(newValue, 0, dataSet.ThresholdMax);
                }

                var range = dataSet.ScaleMax - dataSet.ScaleMin;
                var effectiveMin = dataSet.ScaleMin + dataSet.ThresholdMin * range;
                var effectiveMax = dataSet.ScaleMin + dataSet.ThresholdMax * range;
                cursorString = $"Min: {effectiveMin.ToString("0.###E+000").PadLeft(11)} ({(dataSet.ThresholdMin * 100):0.0}%)\n";
                cursorString += $"Max: {effectiveMax.ToString("0.###E+000").PadLeft(11)} ({(dataSet.ThresholdMax * 100):0.0}%)";
            }

            if (handInfo != null)
            {
                handInfo[_getPrimaryHandIndex()].enabled = true;
                handInfo[1 - _getPrimaryHandIndex()].enabled = false;
                handInfo[_getPrimaryHandIndex()].text = cursorString;
            }
        }

        private void UpdateEditingZAxis()
        {
            var controllerHeight = _getHandTransforms()[_getPrimaryHandIndex()].position.y;
            var delta = controllerHeight - _previousControllerHeight;
            _previousControllerHeight = controllerHeight;
            var dataSets = _getDataSets();
            for (var i = 0; i < dataSets.Length; i++)
            {
                var dataSet = dataSets[i];
                float zxRatio = dataSet.InitialScale.z / dataSet.InitialScale.x;
                var newValue = dataSet.transform.localScale.z + delta;
                dataSet.transform.localScale = new Vector3(
                    dataSet.transform.localScale.x,
                    dataSet.transform.localScale.y,
                    Mathf.Clamp(newValue, dataSet.transform.localScale.x * zxRatio * dataSet.ZAxisMinFactor, dataSet.transform.localScale.x * zxRatio * dataSet.ZAxisMaxFactor));
            }
        }
    }
}
