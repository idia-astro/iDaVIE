using System.Collections.Generic;
using CatalogData;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Player), typeof(LineRenderer))]
public class InputController : MonoBehaviour
{
    private enum InputState
    {
        Idle,
        Moving,
        Scaling
    }

    public bool InPlaceScaling = true;
    public CatalogDataSetManager CatalogDataSetManager;
    private Player _player;
    private Hand[] _hands;
    private Transform[] _handTransforms;
    private SteamVR_Action_Boolean _grabGripAction;
    private SteamVR_Action_Boolean _grabPinchAction;
    private CatalogDataSetRenderer[] _catalogDataSets;
    private VolumeDataSet[] _volumeDataSets;
    private List<MonoBehaviour> _allDataSets;
    private float[] _startDataSetScales;
    private Vector3[] _currentGripPositions;
    private Vector3 _startGripSeparation;
    private Vector3 _startGripCenter;
    private InputState _inputState;
    private LineRenderer _lineRenderer;
    private TextMeshPro _scalingTextComponent;

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

        var volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSet>();
            _allDataSets.AddRange(_volumeDataSets);
        }

        _lineRenderer = GetComponent<LineRenderer>();
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
    }

    private void StateTransitionIdleToMoving()
    {
        _inputState = InputState.Moving;
    }

    private void StateTransitionMovingToScaling()
    {
        _inputState = InputState.Scaling;
        _startGripSeparation = _handTransforms[0].position - _handTransforms[1].position;
        _startGripCenter = (_handTransforms[0].position + _handTransforms[1].position) / 2.0f;
        for (var i = 0; i < _allDataSets.Count; i++)
        {
            _startDataSetScales[i] = _allDataSets[i].transform.localScale.magnitude;
        }

        // World space: 2 mm lines
        _lineRenderer.positionCount = 3;
        _lineRenderer.startWidth = 2e-3f;
        _lineRenderer.endWidth = 2e-3f;
        _lineRenderer.SetPositions(new[] {_currentGripPositions[0], _startGripCenter, _currentGripPositions[1]});
        _lineRenderer.enabled = true;

        if (_scalingTextComponent)
        {
            _scalingTextComponent.enabled = true;
        }
    }

    private void StateTransitionScalingToMoving()
    {
        _inputState = InputState.Moving;
        _lineRenderer.enabled = false;

        if (_scalingTextComponent)
        {
            _scalingTextComponent.enabled = false;
        }
    }

    private void Update()
    {
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

        // Each dataSet needs to be updated separately, as they can have different initial scales.        
        for (var i = 0; i < _allDataSets.Count; i++)
        {
            var dataSet = _allDataSets[i];
            float initialScale = _startDataSetScales[i];
            float currentScale = dataSet.transform.localScale.magnitude;
            float newScale = Mathf.Max(1e-6f, initialScale * scalingFactor);

            // Calculate the change in rotation of the grip vector about the up (Y+) axis
            Vector3 previousGripDirectionXz = new Vector3(previousGripSeparation.x, 0, previousGripSeparation.z).normalized;
            Vector3 currentGripDirectionXz = new Vector3(currentGripSeparation.x, 0, currentGripSeparation.z).normalized;
            // A x B = |A| |B| sin(theta)
            float sine = Vector3.Cross(previousGripDirectionXz, currentGripDirectionXz).y;
            float angle = Mathf.Asin(sine);

            if (InPlaceScaling)
            {
                // Adjust dataSet position while scaling to keep the pivot point fixed 
                Vector3 dataSetPositionWorldSpace = dataSet.transform.position;
                Vector3 preScaleOffset = dataSetPositionWorldSpace - _startGripCenter;
                float scaleRatio = newScale / currentScale;
                dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                dataSet.transform.position = _startGripCenter + preScaleOffset * scaleRatio;

                // Adjust dataSet position while rotating to keep the pivot point fixed
                Vector3 startGripPositionDataSpace = dataSet.transform.InverseTransformPoint(_startGripCenter);
                dataSet.transform.RotateAround(_startGripCenter, Vector3.up, angle * Mathf.Rad2Deg);
                Vector3 updatedPositionWorldSpace = dataSet.transform.TransformPoint(startGripPositionDataSpace);
                dataSet.transform.position -= updatedPositionWorldSpace - _startGripCenter;
            }
            else
            {
                // Rotate and scale with the pivot at the origin
                dataSet.transform.localScale = dataSet.transform.localScale.normalized * newScale;
                dataSet.transform.Rotate(Vector3.up, angle * Mathf.Rad2Deg);
            }

            UpdateScalingText(dataSet);
        }

        _lineRenderer.SetPositions(new[] {_currentGripPositions[0], InPlaceScaling ? _startGripCenter : currentGripCenter, _currentGripPositions[1]});
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
                    dataSet.transform.position += delta;
                }
            }
        }
    }

    // (Placeholder) Update function for FSM Idle state
    private void UpdateIdle()
    {
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