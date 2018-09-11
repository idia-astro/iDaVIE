using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngineInternal;

public class InputController : MonoBehaviour
{
    private enum InputState
    {
        Idle,
        Moving,
        Scaling
    }

    public SteamVR_TrackedController[] TrackedControllers;
    public bool InPlaceScaling = true;
    private PointDataSet[] _pointDataSets;
    private VolumeDataSet[] _volumeDataSets;
    private List<MonoBehaviour> _allDataSets;
    private float[] _startDataSetScales;
    private Vector3[] _previousGripPositions;
    private Vector3[] _currentGripPositions;
    private Vector3 _startGripSeparation;
    private Vector3 _startGripCenter;
    private InputState _inputState;
    private LineRenderer _lineRenderer;

    protected void Start()
    {
        _allDataSets = new List<MonoBehaviour>();
        // Connect this behaviour component to others
        var pointDataSetManager = GameObject.Find("PointDataSetManager");
        if (pointDataSetManager)
        {
            _pointDataSets = pointDataSetManager.GetComponentsInChildren<PointDataSet>();
            _allDataSets.AddRange(_pointDataSets);
        }
        var volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSet>();
            _allDataSets.AddRange(_volumeDataSets);
        }
        _lineRenderer = GetComponent<LineRenderer>();

        _startDataSetScales = new float[_allDataSets.Count];
        _previousGripPositions = new Vector3[TrackedControllers.Length];
        _currentGripPositions = new Vector3[TrackedControllers.Length];
        _startGripSeparation = Vector3.zero;
        _startGripCenter = Vector3.zero;

        foreach (var controller in TrackedControllers)
        {
            controller.Gripped += OnControllerGripped;
            controller.Ungripped += OnControllerUngripped;
            controller.TriggerClicked += OnTriggerClicked;
        }

        _inputState = InputState.Idle;
    }

    private void OnTriggerClicked(object sender, ClickedEventArgs e)
    {
        // Shift color map forward for Controller #1, backward for #2
        int delta = ((SteamVR_TrackedController)sender == TrackedControllers[0]) ? 1 : -1;
        foreach (var dataSet in _pointDataSets)
        {
            dataSet.ShiftColorMap(delta);
        }
    }

    private void OnControllerGripped(object sender, ClickedEventArgs e)
    {
        int gripCount = 0;

        for (var i = 0; i < TrackedControllers.Length; i++)
        {
            var controller = TrackedControllers[i];
            if ((SteamVR_TrackedController)sender == controller)
            {
                _previousGripPositions[i] = controller.transform.position;
            }

            if (controller.gripped)
            {
                gripCount++;
            }
        }

        // State transitions are handled by specific transition functions. A more explicitly enforced FSM should be used in future to manage state transition rules
        // At this point, they may appear a bit verbose, but they are useful for managing states and side-effects of transitions
        switch (gripCount)
        {
            case 2:
                StateTransitionMovingToScaling();
                break;
            case 1:
                StateTransitionIdleToMoving();
                break;
            default:
                Debug.LogError("This state transition should not be possible");
                break;
        }
    }

    private void OnControllerUngripped(object sender, ClickedEventArgs e)
    {
        int gripCount = 0;
        foreach (var controller in TrackedControllers)
        {
            gripCount += controller.gripped ? 1 : 0;
        }

        switch (gripCount)
        {
            case 1:
                StateTransitionScalingToMoving();
                break;
            default:
                StateTransitionMovingToIdle();
                break;
        }
    }

    private void StateTransitionMovingToIdle()
    {
        _inputState = InputState.Idle;
    }

    private void StateTransitionMovingToScaling()
    {
        _inputState = InputState.Scaling;
        _startGripSeparation = TrackedControllers[0].transform.position - TrackedControllers[1].transform.position;
        _startGripCenter = (TrackedControllers[0].transform.position + TrackedControllers[1].transform.position) / 2.0f;
        for (var i = 0; i < _allDataSets.Count; i++)
        {
            _startDataSetScales[i] = _allDataSets[i].transform.localScale.magnitude;
        }       

        if (_lineRenderer)
        {
            // World space: 2 mm lines
            _lineRenderer.positionCount = 3;
            _lineRenderer.startWidth = 2e-3f;
            _lineRenderer.endWidth = 2e-3f;
            _lineRenderer.SetPositions(new[] {TrackedControllers[0].transform.position, _startGripCenter, TrackedControllers[1].transform.position});
        }
    }

    private void StateTransitionIdleToMoving()
    {
        _inputState = InputState.Moving;
    }


    private void StateTransitionScalingToMoving()
    {
        _inputState = InputState.Moving;
        if (_lineRenderer)
        {
            _lineRenderer.positionCount = 0;
        }
    }

    // Update is called once per frame
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
        for (var i = 0; i < TrackedControllers.Length; i++)
        {
            var controller = TrackedControllers[i];
            _currentGripPositions[i] = controller.transform.position;
            if (controller.gripped)
            {
                _previousGripPositions[i] = _currentGripPositions[i];
            }
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
        }

        if (_lineRenderer)
        {
            _lineRenderer.SetPositions(new[] {TrackedControllers[0].transform.position, InPlaceScaling ? _startGripCenter : currentGripCenter, TrackedControllers[1].transform.position});
        }
    }

    // Update function for FSM Moving state
    private void UpdateMoving()
    {
        for (var i = 0; i < TrackedControllers.Length; i++)
        {
            var controller = TrackedControllers[i];
            _currentGripPositions[i] = controller.transform.position;
            if (controller.gripped)
            {
                var delta = _currentGripPositions[i] - _previousGripPositions[i];
                foreach (var dataSet in _allDataSets)
                {
                    dataSet.transform.position += delta;
                }

                _previousGripPositions[i] = _currentGripPositions[i];
            }
        }
    }

    // (Placeholder) Update function for FSM Idle state
    private void UpdateIdle()
    {
    }
}