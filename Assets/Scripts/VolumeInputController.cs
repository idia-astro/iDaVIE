using System;
using System.Collections.Generic;
using VolumeData;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Vectrosity;
using System.Diagnostics;
using DataFeatures;
using Debug = UnityEngine.Debug;
using VRHand = Valve.VR.InteractionSystem.Hand;

[RequireComponent(typeof(Player))]
public class VolumeInputController : MonoBehaviour
{
    private enum VRFamily
    {
        Unknown,
        Oculus,
        Vive,
        WindowsMixedReality
    }

    private enum LocomotionState
    {
        Idle,
        Moving,
        Scaling
    }
    
    public enum InteractionState
    {
        SelectionMode,
        PaintMode
    }

    [Flags]
    private enum RotationAxes
    {
        None = 0,
        Roll = 1,
        Yaw = 2
    }
    //reference to quick menu canvass
    public GameObject CanvassQuickMenu;


    // Scaling/Rotation options
    public bool InPlaceScaling = true;
    public bool ScalingEnabled = true;
    public float RotationAxisCutoff = 5.0f;

    [Range(0.1f, 5.0f)] public float VignetteFadeSpeed = 2.0f;

    // Painting
    public bool AdditiveBrush = true;
    public int BrushSize = 1;
    public short BrushValue = 1;
    
    private Player _player;
    private VRHand[] _hands;
    private Transform[] _handTransforms;
    private SteamVR_Action_Boolean _grabGripAction;
    private SteamVR_Action_Boolean _grabPinchAction;
    private SteamVR_Action_Boolean _quickMenuAction;
    private VolumeDataSetRenderer[] _volumeDataSets;
    private float[] _startDataSetScales;
    private Vector3[] _currentGripPositions;
    private Vector3 _startGripSeparation;
    private Vector3 _startGripCenter;
    private Vector3 _starGripForwardAxis;
    private LocomotionState _locomotionState;
    
    // Interactions
    private InteractionState _interactionState;
    private bool _isPainting;
    private bool _isSelecting;
    private bool _isQuickMenu;

    private VectorLine _lineAxisSeparation;
    private VectorLine _lineRotationAxes;

    private Vector3Int _coordDecimcalPlaces;

    private TextMeshPro _scalingTextComponent;

    private float _rotationYawCumulative = 0;
    private float _rotationRollCumulative = 0;
    private RotationAxes _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

    // Vignetting
    private float _currentVignetteIntensity = 0;
    private float _targetVignetteIntensity = 0;

    // Selecting
    private VRHand _selectingHand;
    private Stopwatch selectionStopwatch = new Stopwatch();

    // VR-family dependent values
    private VRFamily _vrFamily;

    // Used for moving the pointer transform to an acceptable position for each controller type
    private static readonly Dictionary<VRFamily, Vector3> PointerOffsetsLeft = new Dictionary<VRFamily, Vector3>
    {
        {VRFamily.Unknown, Vector3.zero},
        {VRFamily.Oculus, new Vector3(0.005f, -0.025f, -0.025f)},
        {VRFamily.Vive, new Vector3(0, -0.09f, 0.06f)},
        {VRFamily.WindowsMixedReality, new Vector3(0.05f, -0.029f, 0.03f)}
    };

    private static readonly Dictionary<VRFamily, Vector3> PointerOffsetsRight = new Dictionary<VRFamily, Vector3>
    {
        {VRFamily.Unknown, Vector3.zero},
        {VRFamily.Oculus, new Vector3(-0.005f, -0.025f, -0.025f)},
        {VRFamily.Vive, new Vector3(0, -0.09f, 0.06f)},
        {VRFamily.WindowsMixedReality, new Vector3(-0.05f, -0.029f, 0.03f)}
    };

    private void OnEnable()
    {
        _vrFamily = DetermineVRFamily();
        Vector3 pointerOffset = PointerOffsetsLeft[_vrFamily];
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
                _handTransforms[i].localPosition = pointerOffset;
            }

            _handTransforms[0].localPosition = PointerOffsetsLeft[_vrFamily];
            _handTransforms[1].localPosition = PointerOffsetsRight[_vrFamily];


            _grabGripAction = _player.leftHand.grabGripAction;
            _grabPinchAction = _player.leftHand.grabPinchAction;
            _quickMenuAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("QuickMenu");
        }

        _grabGripAction.AddOnChangeListener(OnGripChanged, SteamVR_Input_Sources.LeftHand);
        _grabGripAction.AddOnChangeListener(OnGripChanged, SteamVR_Input_Sources.RightHand);
        _grabPinchAction.AddOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.LeftHand);
        _grabPinchAction.AddOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.RightHand);
        _quickMenuAction.AddOnChangeListener(OnQuickMenuChanged, SteamVR_Input_Sources.LeftHand);
        _quickMenuAction.AddOnChangeListener(OnQuickMenuChanged, SteamVR_Input_Sources.RightHand);

        var volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
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
        _startDataSetScales = new float[_volumeDataSets.Length];
        _currentGripPositions = new Vector3[2];
        _startGripSeparation = Vector3.zero;
        _startGripCenter = Vector3.zero;

        _locomotionState = LocomotionState.Idle;
        _interactionState = InteractionState.SelectionMode;
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _grabGripAction.RemoveOnChangeListener(OnGripChanged, SteamVR_Input_Sources.LeftHand);
            _grabGripAction.RemoveOnChangeListener(OnGripChanged, SteamVR_Input_Sources.RightHand);
            _grabPinchAction.RemoveOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.LeftHand);
            _grabPinchAction.RemoveOnChangeListener(OnPinchChanged, SteamVR_Input_Sources.RightHand);
            _quickMenuAction.RemoveOnChangeListener(OnQuickMenuChanged, SteamVR_Input_Sources.LeftHand);
            _quickMenuAction.RemoveOnChangeListener(OnQuickMenuChanged, SteamVR_Input_Sources.RightHand);
        }
    }

    private void OnQuickMenuChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (newState )
        {
            //  StartSelection(hand);
            StartRequestQuickMenu(fromSource == SteamVR_Input_Sources.LeftHand ? 0 : 1);
        }
        else
        {
            //  EndSelection();
            EndRequestQuickMenu();
        }
    }

    private void OnGripChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
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
                if (_locomotionState == LocomotionState.Idle)
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


    private void OnPinchChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (_interactionState == InteractionState.PaintMode)
        {
            // Handle painting brush stroke ending
            if (_isPainting && !newState)
            {
                foreach (var dataSet in _volumeDataSets)
                {
                    dataSet.FinishBrushStroke();
                }
            }
            _isPainting = newState;
        }
        else
        {
            var hand = fromSource == SteamVR_Input_Sources.LeftHand ? _hands[0] : _hands[1];
            if (newState && _selectingHand == null)
            {
                StartSelection(hand);

            }
            else if (!newState && _selectingHand == hand)
            {
                EndSelection();
            }
        }
    }

    private void StateTransitionMovingToIdle()
    {
        _locomotionState = LocomotionState.Idle;
        _targetVignetteIntensity = 0;
    }

    private void StateTransitionIdleToMoving()
    {
        _locomotionState = LocomotionState.Moving;
        _targetVignetteIntensity = 1;
    }

    private void StartRequestQuickMenu(int handIndex)
    {
        Debug.Log("Request Quick menu!");
        CanvassQuickMenu.transform.SetParent(_handTransforms[handIndex], false);
        CanvassQuickMenu.transform.localPosition= new Vector3(-0.1f,(handIndex == 0 ? 1: -1) * 0.175f, 0.10f);
        CanvassQuickMenu.transform.localRotation= Quaternion.Euler((handIndex == 0 ? 1: -1) * -3.25f,15f, 90f);
        CanvassQuickMenu.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
        CanvassQuickMenu.SetActive(true);
        _isQuickMenu = true;
    }

    private void EndRequestQuickMenu()
    {
        Debug.Log("END Request Quick menu!");
        CanvassQuickMenu.SetActive(false);
        _isQuickMenu = false;
    }

    private void StartSelection(VRHand selectingHand)
    {
        _selectingHand = selectingHand;
        _isSelecting = true;
        int handIndex = _selectingHand.handType == SteamVR_Input_Sources.LeftHand ? 0 : 1;
        var startPosition = _handTransforms[handIndex].position;
        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.SetRegionPosition(startPosition, true);
        }

        selectionStopwatch.Reset();
        selectionStopwatch.Start();

        Debug.Log($"Entering selecting state with hand {selectingHand.handType}!");
    }

    private void EndSelection()
    {
        int handIndex = _selectingHand.handType == SteamVR_Input_Sources.LeftHand ? 0 : 1;
        var endPosition = _handTransforms[handIndex].position;

        _selectingHand = null;
        _isSelecting = false;

        selectionStopwatch.Stop();
        var activeDataSet = getFirstActiveDataSet();

        if (activeDataSet)
        {
            activeDataSet.ClearRegion();
            var featureSetManager = activeDataSet.GetComponentInChildren<FeatureSetManager>();
            // Clear region selection by clicking selection. Attempt to select feature
            if (selectionStopwatch.ElapsedMilliseconds < 200)
            {
                activeDataSet.SelectFeature(endPosition);
            }
            else
            {
                if (featureSetManager)
                {
                    featureSetManager.CreateNewFeature(activeDataSet.RegionStartVoxel, activeDataSet.RegionEndVoxel, "selection", true);
                }
            }
        }
    }

    private void StateTransitionMovingToScaling()
    {
        _locomotionState = LocomotionState.Scaling;
        _startGripSeparation = _handTransforms[0].position - _handTransforms[1].position;
        _startGripCenter = (_handTransforms[0].position + _handTransforms[1].position) / 2.0f;
        _starGripForwardAxis = Vector3.Cross(Vector3.up, _startGripSeparation.normalized).normalized;
        _rotationYawCumulative = 0;
        _rotationRollCumulative = 0;
        _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

        for (var i = 0; i < _volumeDataSets.Length; i++)
        {
            if (_volumeDataSets[i].isActiveAndEnabled)
            {
                _startDataSetScales[i] = _volumeDataSets[i].transform.localScale.magnitude;
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
        _locomotionState = LocomotionState.Moving;
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

        switch (_locomotionState)
        {
            case LocomotionState.Moving:
                UpdateMoving();
                break;
            case LocomotionState.Scaling:
                UpdateScaling();
                break;
            case LocomotionState.Idle:
                UpdateIdle();
                break;
        }

        if (_isSelecting)
        {
            UpdateSelecting();
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

            foreach (var dataSet in _volumeDataSets)
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
        for (var i = 0; i < _volumeDataSets.Length; i++)
        {
            var dataSet = _volumeDataSets[i];
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
                foreach (var dataSet in _volumeDataSets)
                {
                    if (dataSet.isActiveAndEnabled)
                    {
                        dataSet.transform.position += delta;
                    }
                }
            }
        }
    }

    private void UpdateIdle()
    {
        if (_volumeDataSets == null)
        {
            return;
        }

        if (!_isSelecting)
        {
            string cursorString = "";

            foreach (var dataSet in _volumeDataSets)
            {
                
                if (_interactionState == InteractionState.PaintMode)
                {
                    if (_isPainting)
                    {
                        dataSet.PaintCursor(AdditiveBrush ? BrushValue : (short) 0, BrushSize);
                    }
                    dataSet.SetCursorPosition(_handTransforms[0].position, BrushSize);
                }
                else
                {
                    dataSet.SetCursorPosition(_handTransforms[0].position, 1);
                }
                if (dataSet.isActiveAndEnabled)
                {
                    string sourceIndex = "";
                    if (dataSet.CursorSource != 0)
                        sourceIndex = $"Source # {dataSet.CursorSource}";
                    var voxelCoordinate = dataSet.CursorVoxel;
                    if (voxelCoordinate.x >= 0 && _scalingTextComponent != null)
                    {
                        Vector3Int coordDecimcalPlaces = dataSet.GetDimDecimals();
                        var voxelValue = dataSet.CursorValue;
                        string raDecVel = dataSet.GetFitsCoordsString(voxelCoordinate.x, voxelCoordinate.y, voxelCoordinate.z);
                        cursorString = "(" + voxelCoordinate.x.ToString().PadLeft(coordDecimcalPlaces.x)
                                           + "," + voxelCoordinate.y.ToString().PadLeft(coordDecimcalPlaces.y) + ","
                                           + voxelCoordinate.z.ToString().PadLeft(coordDecimcalPlaces.z) + "): "
                                           + voxelValue.ToString("0.###E+000").PadLeft(11) + System.Environment.NewLine
                                           + raDecVel + System.Environment.NewLine + sourceIndex;
                    }
                }
            }

            _scalingTextComponent.enabled = true;
            _scalingTextComponent.text = cursorString;
        }
    }

    private void UpdateSelecting()
    {
        if (_selectingHand == null)
        {
            return;
        }

        string cursorString = "";
        int handIndex = _selectingHand.handType == SteamVR_Input_Sources.LeftHand ? 0 : 1;
        var endPosition = _handTransforms[handIndex].position;

        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.SetRegionPosition(endPosition, false);
            if (dataSet.isActiveAndEnabled)
            {
                var regionSize = Vector3.Max(dataSet.RegionStartVoxel, dataSet.RegionEndVoxel) - Vector3.Min(dataSet.RegionStartVoxel, dataSet.RegionEndVoxel) + Vector3.one;
                Vector3 wcsLengths = dataSet.GetFitsLengths(regionSize.x, regionSize.y, regionSize.z);
                cursorString = $"Region: {regionSize.x} x {regionSize.y} x {regionSize.z}" + System.Environment.NewLine
                                                                                           + $"Physical: {Math.Truncate(wcsLengths.x * 100) / 100}° x {Math.Truncate(wcsLengths.y * 100) / 100}° x {Math.Truncate(wcsLengths.z * 100) / 100 / 1000} km/s";
            }
        }

        _scalingTextComponent.enabled = true;
        _scalingTextComponent.text = cursorString;
    }


    private void UpdateScalingText(VolumeDataSetRenderer dataSet)
    {
        // TODO: update scaling text
    }

    private VRFamily DetermineVRFamily()
    {
        string vrModel = XRDevice.model.ToLower();
        if (vrModel.Contains("oculus"))
        {
            return VRFamily.Oculus;
        }

        if (vrModel.Contains("vive"))
        {
            return VRFamily.Vive;
        }

        if (vrModel.Contains("mixed reality") || vrModel.Contains("acer"))
        {
            return VRFamily.WindowsMixedReality;
        }

        Debug.Log($"Unknown VR model {XRDevice.model}!");
        return VRFamily.Unknown;
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in _volumeDataSets)
        {
            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }

        return null;
    }

    public void Teleport(Vector3 boundsMin, Vector3 boundsMax)
    {
        float targetSize = 0.3f;
        float targetDistance = 0.5f;

        var activeDataSet = getFirstActiveDataSet();
        if (activeDataSet != null && Camera.main != null)
        {
            var dataSetTransform = activeDataSet.transform;
            var cameraTransform = Camera.main.transform;
            Vector3 boundsMinObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMin);
            Vector3 boundsMaxObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMax);
            Vector3 deltaObjectSpace = boundsMaxObjectSpace - boundsMinObjectSpace;
            Vector3 deltaWorldSpace = dataSetTransform.TransformVector(deltaObjectSpace);
            float lengthWorldSpace = deltaWorldSpace.magnitude;
            float scalingRequired = targetSize / lengthWorldSpace;
            dataSetTransform.localScale *= scalingRequired;

            Vector3 cameraPosWorldSpace = cameraTransform.position;
            Vector3 cameraDirWorldSpace = cameraTransform.forward.normalized;
            Vector3 targetPosition = cameraPosWorldSpace + cameraDirWorldSpace * targetDistance;
            Vector3 centerWorldSpace = dataSetTransform.TransformPoint((boundsMaxObjectSpace + boundsMinObjectSpace) / 2.0f);
            Vector3 deltaPosition = targetPosition - centerWorldSpace;
            dataSetTransform.position += deltaPosition;
        }
    }

    public void VibrateController(SteamVR_Input_Sources hand, float duration, float frequency, float amplitude)
    {
        _player.leftHand.hapticAction.Execute(0, duration, frequency, amplitude, hand);
    }

    public void SetInteractionState(InteractionState interactionState)
    {
        if (interactionState != _interactionState)
        {
            // TODO: handle state transitions properly
            _interactionState = interactionState;
        }
    }
}