﻿using System;
using System.Collections.Generic;
using CatalogData;
using VolumeData;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Vectrosity;

[RequireComponent(typeof(Player))]
public class InputController : MonoBehaviour
{
    private enum InputState
    {
        Idle,
        Moving,
        Scaling
    }

    [Flags]
    private enum RotationAxes
    {
        None = 0,
        Roll = 1,
        Yaw = 2        
    }

    // Scaling/Rotation options
    public bool InPlaceScaling = true;
    public bool ScalingEnabled = true;
    public float RotationAxisCutoff = 5.0f;

    [Range(0.1f, 5.0f)] public float VignetteFadeSpeed = 2.0f;

    public CatalogDataSetManager CatalogDataSetManager;
    private Player _player;
    private Hand[] _hands;
    private Transform[] _handTransforms;
    private SteamVR_Action_Boolean _grabGripAction;
    private SteamVR_Action_Boolean _grabPinchAction;
    private CatalogDataSetRenderer[] _catalogDataSets;
    private VolumeDataSetRenderer[] _volumeDataSets;
    private List<MonoBehaviour> _allDataSets;
    private float[] _startDataSetScales;
    private Vector3[] _currentGripPositions;
    private Vector3 _startGripSeparation;
    private Vector3 _startGripCenter;
    private Vector3 _starGripForwardAxis;
    private InputState _inputState;
    private VectorLine _lineAxisSeparation;
    private VectorLine _lineRotationAxes;
    
    private TextMeshPro _scalingTextComponent;

    private float _rotationYawCumulative = 0;
    private float _rotationRollCumulative = 0;
    private RotationAxes _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;
    
    // Vignetting
    private float _currentVignetteIntensity = 0;
    private float _targetVignetteIntensity = 0;
    
    private void OnEnable()
    {
        if (_player == null)
        {
            _player = GetComponent<Player>();
            _hands = new[] {_player.leftHand, _player.rightHand};

            // Set hand transforms to laser pointer position if it exists
            _handTransforms = new Transform[2];
            for (var i = 0; i < 2; i++)
            {
                var laserPointer = _hands[i].GetComponentInChildren<LaserPointer>();
                _handTransforms[i] = (laserPointer != null) ? laserPointer.transform : _hands[i].transform;
            }

            _grabGripAction = _player.leftHand.grabGripAction;
            _grabPinchAction = _player.leftHand.grabPinchAction;
        }

        _grabGripAction.AddOnChangeListener(OnGripChanged, SteamVR_Input_Sources.LeftHand);
        _grabGripAction.AddOnChangeListener(OnGripChanged, SteamVR_Input_Sources.RightHand);
        _grabPinchAction.AddOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.Any);

        _allDataSets = new List<MonoBehaviour>();
        // Connect this behaviour component to others        
        if (CatalogDataSetManager)
        {
            _catalogDataSets = CatalogDataSetManager.GetComponentsInChildren<CatalogDataSetRenderer>();
            _allDataSets.AddRange(_catalogDataSets);
        }
        else
        {
            _catalogDataSets = new CatalogDataSetRenderer[0];
        }

        var volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
            _allDataSets.AddRange(_volumeDataSets);
        }
        else
        {
            _volumeDataSets = new VolumeDataSetRenderer[0];
        }

        // Line renderer for showing separation between controllers while scaling/rotating
        _lineRotationAxes = new VectorLine("RotationAxes", new List<Vector3>(new Vector3[3]), 2.0f, LineType.Continuous);
        _lineRotationAxes.color = Color.white;
        _lineRotationAxes.Draw3DAuto();
        
        _lineAxisSeparation = new VectorLine("AxisSeparation", new List<Vector3>(new Vector3[3]), 2.0f, LineType.Continuous);
        _lineAxisSeparation.color = Color.red;
        _lineAxisSeparation.Draw3DAuto();

        _scalingTextComponent = _hands[0].GetComponentInChildren<TextMeshPro>();
        _startDataSetScales = new float[_allDataSets.Count];
        _currentGripPositions = new Vector3[2];
        _startGripSeparation = Vector3.zero;
        _startGripCenter = Vector3.zero;

        _inputState = InputState.Idle;

    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _grabGripAction.RemoveOnChangeListener(OnGripChanged, SteamVR_Input_Sources.LeftHand);
            _grabGripAction.RemoveOnChangeListener(OnGripChanged, SteamVR_Input_Sources.RightHand);
            _grabPinchAction.RemoveOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.Any);
        }
    }

    private void OnGripChanged(SteamVR_Action_In actionIn)
    {
        int gripCount = (_grabGripAction.GetState(SteamVR_Input_Sources.LeftHand) ? 1 : 0) + (_grabGripAction.GetState(SteamVR_Input_Sources.RightHand) ? 1 : 0);

        for (var i = 0; i < 2; i++)
        {
            _currentGripPositions[i] = _handTransforms[i].position;
        }

        switch (gripCount)
        {
            case 0:
                StateTransitionMovingToIdle();
                break;
            case 1:
                // Can do a transition either from Idle or Scaling to Moving
                if (_inputState == InputState.Idle)
                {
                    StateTransitionIdleToMoving();
                }
                else
                {
                    StateTransitionScalingToMoving();
                }

                break;
            case 2:
                StateTransitionMovingToScaling();
                break;
        }
    }


    private void OnPinchChanged(SteamVR_Action_In actionIn)
    {
    }

    private void StateTransitionMovingToIdle()
    {
        _inputState = InputState.Idle;
        _targetVignetteIntensity = 0;
    }

    private void StateTransitionIdleToMoving()
    {
        _inputState = InputState.Moving;
        _targetVignetteIntensity = 1;
    }

    private void StateTransitionMovingToScaling()
    {
        _inputState = InputState.Scaling;
        _startGripSeparation = _handTransforms[0].position - _handTransforms[1].position;
        _startGripCenter = (_handTransforms[0].position + _handTransforms[1].position) / 2.0f;
        _starGripForwardAxis = Vector3.Cross(Vector3.up, _startGripSeparation.normalized).normalized;
        _rotationYawCumulative = 0;
        _rotationRollCumulative = 0;
        _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

        for (var i = 0; i < _allDataSets.Count; i++)
        {
            if (_allDataSets[i].isActiveAndEnabled)
            {
                _startDataSetScales[i] = _allDataSets[i].transform.localScale.magnitude;
            }
        }

        _lineAxisSeparation.points3[0] = _currentGripPositions[0];
        _lineAxisSeparation.points3[1] = _startGripCenter;
        _lineAxisSeparation.points3[2] = _currentGripPositions[1];
        _lineAxisSeparation.active = true;

        // Axis lines: 10 cm length
        _lineRotationAxes.points3[0] = _startGripCenter + _starGripForwardAxis * 0.1f;
        _lineRotationAxes.points3[1] = _startGripCenter;
        _lineRotationAxes.points3[2] = _startGripCenter + Vector3.up * 0.1f;
        _lineRotationAxes.active = true;

        if (_scalingTextComponent)
        {
            _scalingTextComponent.enabled = true;
        }
    }

    private void StateTransitionScalingToMoving()
    {
        _inputState = InputState.Moving;
        _lineRotationAxes.active = false;
        _lineAxisSeparation.active = false;

        if (_scalingTextComponent)
        {
            _scalingTextComponent.enabled = false;
        }
    }

    private void Update()
    {
        // Common update functions
        UpdateVignette();
        if (Camera.current)
        {
            Camera.current.depthTextureMode = DepthTextureMode.Depth;
        }

        switch (_inputState)
        {
            case InputState.Moving:
                UpdateMoving();
                break;
            case InputState.Scaling:
                UpdateScaling();
                break;
            case InputState.Idle:
                UpdateIdle();
                break;
        }
    }

    private void UpdateVignette()
    {
        float requiredChange = _targetVignetteIntensity - _currentVignetteIntensity;
        // Skip updates if the target is sufficiently close
        if (Mathf.Abs(requiredChange) > 1e-6f)
        {
            float maxChange = Mathf.Sign(requiredChange) * Time.deltaTime * VignetteFadeSpeed;
            if (Mathf.Abs(maxChange) > Mathf.Abs(requiredChange))
            {
                maxChange = requiredChange;
            }

            _currentVignetteIntensity += maxChange;
            foreach (var dataSet in _volumeDataSets)
            {
                if (dataSet.isActiveAndEnabled)
                {
                    dataSet.VignetteIntensity = _currentVignetteIntensity;                    
                }
            }
            
            foreach (var dataSet in _catalogDataSets)
            {
                dataSet.VignetteIntensity = _currentVignetteIntensity;
            }
        }
    }

    // Update function for FSM Scaling state
    private void UpdateScaling()
    {
        Vector3 previousGripSeparation = _currentGripPositions[0] - _currentGripPositions[1];
        for (var i = 0; i < 2; i++)
        {
            _currentGripPositions[i] = _handTransforms[i].position;
        }

        // Adjusting the scaling based on the ratio between the initial grip separation and the current grip separation is more accurate
        // than updating the scaling based on the previous positions, due to rounding errors
        Vector3 currentGripSeparation = _currentGripPositions[0] - _currentGripPositions[1];
        Vector3 currentGripCenter = (_currentGripPositions[0] + _currentGripPositions[1]) / 2.0f;
        float startGripDistance = _startGripSeparation.magnitude;
        float currentGripDistance = currentGripSeparation.magnitude;
        float scalingFactor = currentGripDistance / Mathf.Max(startGripDistance, 1.0e-6f);                               
        
        // Calculate the change in rotation of the grip vector about the up (Y+) axis
        Vector3 previousGripDirectionXz = new Vector3(previousGripSeparation.x, 0, previousGripSeparation.z).normalized;
        Vector3 currentGripDirectionXz = new Vector3(currentGripSeparation.x, 0, currentGripSeparation.z).normalized;
        float angleYaw = Mathf.Asin(Vector3.Cross(previousGripDirectionXz, currentGripDirectionXz).y);

        // Calculate the change in rotation of the grip vector about the custom rotation axis
        Vector3 perpendicularAxis = Vector3.Cross(_starGripForwardAxis, Vector3.up);
        Vector3 previousGripDirectionRotationAxis = new Vector3(Vector3.Dot(perpendicularAxis, previousGripSeparation), Vector3.Dot(Vector3.up, previousGripSeparation), 0).normalized;
        Vector3 currentGripDirectionRotationAxis = new Vector3(Vector3.Dot(perpendicularAxis, currentGripSeparation), Vector3.Dot(Vector3.up, currentGripSeparation), 0).normalized;
        float angleRoll = Mathf.Asin(-Vector3.Cross(previousGripDirectionRotationAxis, currentGripDirectionRotationAxis).z);
                        
        if ((_rotationAxes & RotationAxes.Yaw) == RotationAxes.Yaw)
        {
            _rotationYawCumulative += angleYaw * Mathf.Rad2Deg;
            if (Mathf.Abs(_rotationYawCumulative) >= RotationAxisCutoff)
            {
                _rotationAxes = RotationAxes.Yaw;
            }
        }       
        
        // Only apply yaw if roll rotation is below the cutoff threshold
        if ((_rotationAxes & RotationAxes.Roll) == RotationAxes.Roll)
        {
            _rotationRollCumulative += angleRoll * Mathf.Rad2Deg;
            if (Mathf.Abs(_rotationRollCumulative) >= RotationAxisCutoff)
            {
                _rotationAxes = RotationAxes.Roll;
            }
        }

        var yawCurrentlyActive = (_rotationAxes & RotationAxes.Yaw) == RotationAxes.Yaw;
        var rollCurrentlyActive = (_rotationAxes & RotationAxes.Roll) == RotationAxes.Roll;
        
        // Each dataSet needs to be updated separately, as they can have different initial scales.        
        for (var i = 0; i < _allDataSets.Count; i++)
        {
            var dataSet = _allDataSets[i];
            if (!dataSet.isActiveAndEnabled)
            {
                continue;
            }
            
            float initialScale = _startDataSetScales[i];
            float currentScale = dataSet.transform.localScale.magnitude;
            float newScale = Mathf.Max(1e-6f, initialScale * scalingFactor);                     
            
            if (InPlaceScaling)
            {
                // Adjust dataSet position while scaling to keep the pivot point fixed
                if (ScalingEnabled)
                {
                    Vector3 dataSetPositionWorldSpace = dataSet.transform.position;
                    Vector3 preScaleOffset = dataSetPositionWorldSpace - _startGripCenter;
                    float scaleRatio = newScale / currentScale;
                    dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                    dataSet.transform.position = _startGripCenter + preScaleOffset * scaleRatio;
                }

                // Adjust dataSet position while rotating to keep the pivot point fixed
                Vector3 startGripPositionDataSpace = dataSet.transform.InverseTransformPoint(_startGripCenter);
                
                if (yawCurrentlyActive)
                {
                    dataSet.transform.RotateAround(_startGripCenter, Vector3.up, angleYaw * Mathf.Rad2Deg);
                }

                if (rollCurrentlyActive)
                {
                    dataSet.transform.RotateAround(_startGripCenter, _starGripForwardAxis, angleRoll * Mathf.Rad2Deg);
                }

                Vector3 updatedPositionWorldSpace = dataSet.transform.TransformPoint(startGripPositionDataSpace);
                dataSet.transform.position -= updatedPositionWorldSpace - _startGripCenter;
            }
            else
            {
                // Rotate and scale with the pivot at the origin
                if (ScalingEnabled)
                {
                    dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                }

                if (yawCurrentlyActive)
                {
                    var angleDegrees = angleYaw * Mathf.Rad2Deg;
                    _rotationYawCumulative += angleDegrees;
                    dataSet.transform.Rotate(Vector3.up, angleDegrees);
                }

                if (rollCurrentlyActive)
                {
                    var angleDegrees = angleRoll * Mathf.Rad2Deg;
                    _rotationRollCumulative += angleDegrees;
                    dataSet.transform.Rotate(_starGripForwardAxis, angleDegrees);
                }
            }

            UpdateScalingText(dataSet);
        }

        var rotationPoint = InPlaceScaling ? _startGripCenter : currentGripCenter;
        _lineAxisSeparation.points3[0] = _currentGripPositions[0];
        _lineAxisSeparation.points3[1] = rotationPoint;
        _lineAxisSeparation.points3[2] = _currentGripPositions[1];
        
        _lineRotationAxes.points3[0] = _startGripCenter + _starGripForwardAxis * (rollCurrentlyActive ? 0.1f : 0.0f);
        _lineRotationAxes.points3[1] = rotationPoint;
        _lineRotationAxes.points3[2] = _startGripCenter + Vector3.up * (yawCurrentlyActive ? 0.1f : 0.0f);
    }

    // Update function for FSM Moving state
    private void UpdateMoving()
    {
        for (var i = 0; i < 2; i++)
        {
            var previousPosition = _currentGripPositions[i];
            _currentGripPositions[i] = _handTransforms[i].position;
            if (_grabGripAction.GetState(_hands[i].handType))
            {
                var delta = _currentGripPositions[i] - previousPosition;
                foreach (var dataSet in _allDataSets)
                {
                    if (dataSet.isActiveAndEnabled)
                    {
                        dataSet.transform.position += delta;
                    }
                }
            }
        }
    }

    // (Placeholder) Update function for FSM Idle state
    private void UpdateIdle()
    {
        if (_volumeDataSets == null)
        {
            return;            
        }

        string cursorString = "";
        
        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.SetCursorPosition(_handTransforms[0].position);
            var voxelCoordinate = dataSet.CursorVoxel;
            if (voxelCoordinate.x >= 0 &&  _scalingTextComponent != null)
            {
                var voxelValue = dataSet.CursorValue;                
                cursorString = $"({voxelCoordinate.x}, {voxelCoordinate.y}, {voxelCoordinate.z}): {voxelValue}";                
            }
        }

        _scalingTextComponent.enabled = true;
        _scalingTextComponent.text = cursorString;
    }

    private void UpdateScalingText(MonoBehaviour dataSet)
    {
        CatalogDataSetRenderer catalogDataSetRenderer = dataSet as CatalogDataSetRenderer;
        if (catalogDataSetRenderer != null)
        {
            string scalingString = catalogDataSetRenderer.ScalingString;
            if (_scalingTextComponent != null)
            {
                _scalingTextComponent.enabled = true;
                _scalingTextComponent.text = scalingString;
            }
        }
    }
}