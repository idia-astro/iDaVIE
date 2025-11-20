/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataFeatures;
using LineRenderer;
using Stateless;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VolumeData;
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
        Scaling,
        EditingThresholdMin,
        EditingThresholdMax,
        EditingZAxis
    }
    
    public enum InteractionState
    {
        IdleSelecting,
        IdlePainting,
        EditingSourceId,
        Creating,
        Editing,
        Painting
    }

    public enum InteractionEvents
    {
        InteractionStarted,
        InteractionEnded,
        PaintModeEnabled,
        PaintModeDisabled,
        StartEditSource,
        EndEditSource,
        CancelEditSource
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
    public GameObject CanvassPaintMenu;

    // Choice of left/right primary hand
    public SteamVR_Input_Sources PrimaryHand = SteamVR_Input_Sources.RightHand;

    public int PrimaryHandIndex => PrimaryHand == SteamVR_Input_Sources.LeftHand ? 0 : 1;
    public bool HasHoverAnchor => (_hoveredAnchor && _hoveredFeature != null);
    public bool HasEditingAnchor => (_editingAnchor && _editingFeature != null);
    public VolumeDataSetRenderer ActiveDataSet => _volumeDataSets.FirstOrDefault(dataSet => dataSet.isActiveAndEnabled);
    
    // Scaling/Rotation options
    public bool InPlaceScaling = true;
    public bool ScalingEnabled = true;
    public float RotationAxisCutoff = 5.0f;

    [Range(0.1f, 5.0f)] public float VignetteFadeSpeed = 2.0f;

    // Painting
    public bool AdditiveBrush { get; private set; } = true;
    public int BrushSize = 1;
    public short SourceId = -1;
    
    private Player _player;
    private VRHand[] _hands;
    private Transform[] _handTransforms;
    private SteamVR_Action_Boolean _grabGripAction;
    private SteamVR_Action_Boolean _grabPinchAction;
    private SteamVR_Action_Boolean _quickMenuAction;
    public VolumeDataSetRenderer[] _volumeDataSets;
    public GameObject volumeDatasetManager;
    public ShapesManager shapesManager;
    private float[] _startDataSetScales;
    private Vector3[] _currentGripPositions;
    private Vector3 _startGripSeparation;
    private Vector3 _startGripCenter;
    private Vector3 _starGripForwardAxis;
    private float _previousControllerHeight;
    private LocomotionState _locomotionState;
    private Config _config;

    public event Action PushToTalkButtonPressed;
    public event Action PushToTalkButtonReleased;
    
    
    // Interactions
    public StateMachine<InteractionState, InteractionEvents> InteractionStateMachine { get; private set; }
    private bool _isQuickMenu;
    private Feature _hoveredFeature, _editingFeature;
    private FeatureAnchor _hoveredAnchor, _editingAnchor;
    public bool ShowCursorInfo { get; private set; } = true;

    private PolyLine _lineAxisSeparation, _lineRotationAxes;

    private Vector3Int _coordDecimcalPlaces;

    private TextMeshPro[] _handInfoComponents;

    private float _rotationYawCumulative = 0;
    private float _rotationRollCumulative = 0;
    private RotationAxes _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

    //scrolling
    public bool scrollSelected = false;
    public GameObject ScrollObject;
    public bool scrollUp = false;
    public bool scrollDown = false;

    // Vignetting
    private bool _tunnellingVignetteOn = true;
    private float _currentVignetteIntensity = 0;
    private float _targetVignetteIntensity = 0;
    private float _maxVignetteIntensity = 1.0f;

    // Selecting
    private readonly Stopwatch _selectionStopwatch = new Stopwatch();

    // VR-family dependent values
    private VRFamily _vrFamily;

    //Shape Selection Scaling
    private bool scalingUp = false;
    private bool scalingDown = false;
    private float scalingTimer = 0f;

    private bool _paintMenuOn = false;
    private bool _shapeMenuOn = false;
    private bool _savePopupOn = false;
    private bool _exportPopupOn = false;
    private bool _shapeSelection = false;

    [SerializeField]
    public GameObject toastNotificationPrefab = null;
    public GameObject followHead = null;

    // Used for moving the pointer transform to an acceptable position for each controller type
    private static readonly Dictionary<VRFamily, Vector3> PointerOffsetsLeft = new Dictionary<VRFamily, Vector3>
    {
        {VRFamily.Unknown, Vector3.zero},
        {VRFamily.Oculus, new Vector3(0.005f, -0.035f, 0.0f)},
        {VRFamily.Vive, new Vector3(0, -0.09f, 0.06f)},
        {VRFamily.WindowsMixedReality, new Vector3(0.05f, -0.029f, 0.03f)}
    };

    private static readonly Dictionary<VRFamily, Vector3> PointerOffsetsRight = new Dictionary<VRFamily, Vector3>
    {
        {VRFamily.Unknown, Vector3.zero},
        {VRFamily.Oculus, new Vector3(-0.005f, -0.035f, 0.0f)},
        {VRFamily.Vive, new Vector3(0, -0.09f, 0.06f)},
        {VRFamily.WindowsMixedReality, new Vector3(-0.05f, -0.029f, 0.03f)}
    };

    private void OnEnable()
    {
        _config = Config.Instance;
        _tunnellingVignetteOn = _config.tunnellingVignetteOn;
        _maxVignetteIntensity = _config.tunnellingVignetteIntensity;
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
        _hands[0].uiInteractAction.AddOnStateDownListener(OnUiInteractDown, SteamVR_Input_Sources.Any);
        _hands[1].uiInteractAction.AddOnStateDownListener(OnUiInteractDown, SteamVR_Input_Sources.Any);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.AddOnStateDownListener(OnMenuUpPressed, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.AddOnStateDownListener(OnMenuUpPressed, SteamVR_Input_Sources.RightHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.AddOnStateDownListener(OnMenuDownPressed, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.AddOnStateDownListener(OnMenuDownPressed, SteamVR_Input_Sources.RightHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuLeft")?.AddOnStateDownListener(OnMenuLeftPressed, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuLeft")?.AddOnStateDownListener(OnMenuLeftPressed, SteamVR_Input_Sources.RightHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuRight")?.AddOnStateDownListener(OnMenuRightPressed, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuRight")?.AddOnStateDownListener(OnMenuRightPressed, SteamVR_Input_Sources.RightHand);

        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.AddOnStateUpListener(OnMenuUpReleased, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.AddOnStateUpListener(OnMenuUpReleased, SteamVR_Input_Sources.RightHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.AddOnStateUpListener(OnMenuDownReleased, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.AddOnStateUpListener(OnMenuDownReleased, SteamVR_Input_Sources.RightHand);

        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI")?.AddOnChangeListener(OnTriggerChanged, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI")?.AddOnChangeListener(OnTriggerChanged, SteamVR_Input_Sources.RightHand);


        UpdateDataSets();

        // Line renderer for showing separation between controllers while scaling/rotating
        _lineRotationAxes = new PolyLine
        {
            Color = Color.white,
            Vertices = new List<Vector3>(new Vector3[3])
        };
        _lineRotationAxes.Activate();

        _lineAxisSeparation = new PolyLine
        {
            Color = Color.red,
            Vertices = new List<Vector3>(new Vector3[3])
        };
        _lineAxisSeparation.Activate();
        
        _handInfoComponents = new[] {_hands[0].GetComponentInChildren<TextMeshPro>(), _hands[1].GetComponentInChildren<TextMeshPro>()};
        _startDataSetScales = new float[_volumeDataSets.Length];
        _currentGripPositions = new Vector3[2];
        _startGripSeparation = Vector3.zero;
        _startGripCenter = Vector3.zero;

        _locomotionState = LocomotionState.Idle;
        
        CreateInteractionStateMachine();
    }

    public void UpdateDataSets()
    {
        var volumeDataSetManager = GameObject.Find("VolumeDataSetManager");
        if (volumeDataSetManager)
        {
            _volumeDataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        }
        else
        {
            _volumeDataSets = Array.Empty<VolumeDataSetRenderer>();
        }
    }

    private void CreateInteractionStateMachine()
    {
        InteractionStateMachine = new StateMachine<InteractionState, InteractionEvents>(InteractionState.IdleSelecting);

        InteractionStateMachine.Configure(InteractionState.IdleSelecting)
            .OnEntryFrom(InteractionEvents.PaintModeDisabled, ExitPaintMode)
            .Permit(InteractionEvents.PaintModeEnabled, InteractionState.IdlePainting)
            .PermitIf(InteractionEvents.InteractionStarted, InteractionState.Creating, () => !HasHoverAnchor)
            .PermitIf(InteractionEvents.InteractionStarted, InteractionState.Editing, () => HasHoverAnchor);

        InteractionStateMachine.Configure(InteractionState.IdlePainting)
            .OnEntryFrom(InteractionEvents.PaintModeEnabled, EnterPaintMode)
            .OnEntryFrom(InteractionEvents.EndEditSource, UpdateSourceId)
            .Permit(InteractionEvents.StartEditSource, InteractionState.EditingSourceId)
            .Permit(InteractionEvents.PaintModeDisabled, InteractionState.IdleSelecting)
            .PermitIf(InteractionEvents.InteractionStarted, InteractionState.Painting,
                () => _locomotionState == LocomotionState.Idle && (ActiveDataSet?.IsFullResolution ?? false));

        InteractionStateMachine.Configure(InteractionState.EditingSourceId)
            .IgnoreIf(InteractionEvents.EndEditSource, () => SourceId <= 0 && ActiveDataSet?.CursorSource == 0)
            .PermitIf(InteractionEvents.EndEditSource, InteractionState.IdlePainting, () => ActiveDataSet?.CursorSource != 0 || SourceId > 0)
            .Permit(InteractionEvents.CancelEditSource, InteractionState.IdlePainting);

        InteractionStateMachine.Configure(InteractionState.Painting)
            .OnExit(() => ActiveDataSet?.FinishBrushStroke())
            .Permit(InteractionEvents.InteractionEnded, InteractionState.IdlePainting);

        InteractionStateMachine.Configure(InteractionState.Creating)
            .OnEntry(StartSelection)
            .OnExit(EndSelection)
            .Permit(InteractionEvents.InteractionEnded, InteractionState.IdleSelecting);

        InteractionStateMachine.Configure(InteractionState.Editing)
            .OnEntry(StartRegionEditing)
            .OnExit(EndRegionEditing)
            .Permit(InteractionEvents.InteractionEnded, InteractionState.IdleSelecting);
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
            _hands[0].uiInteractAction.RemoveOnStateDownListener(OnUiInteractDown, SteamVR_Input_Sources.Any);
            _hands[1].uiInteractAction.RemoveOnStateDownListener(OnUiInteractDown, SteamVR_Input_Sources.Any);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.RemoveOnStateDownListener(OnMenuUpPressed, SteamVR_Input_Sources.LeftHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.RemoveOnStateDownListener(OnMenuUpPressed, SteamVR_Input_Sources.RightHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.RemoveOnStateDownListener(OnMenuDownPressed, SteamVR_Input_Sources.LeftHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.RemoveOnStateDownListener(OnMenuDownPressed, SteamVR_Input_Sources.RightHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuLeft")?.RemoveOnStateDownListener(OnMenuLeftPressed, SteamVR_Input_Sources.LeftHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuLeft")?.RemoveOnStateDownListener(OnMenuLeftPressed, SteamVR_Input_Sources.RightHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuRight")?.RemoveOnStateDownListener(OnMenuRightPressed, SteamVR_Input_Sources.LeftHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuRight")?.RemoveOnStateDownListener(OnMenuRightPressed, SteamVR_Input_Sources.RightHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI")?.RemoveOnChangeListener(OnTriggerChanged, SteamVR_Input_Sources.LeftHand);
            SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI")?.RemoveOnChangeListener(OnTriggerChanged, SteamVR_Input_Sources.RightHand);
        }
    }

    private void OnUiInteractDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (_locomotionState == LocomotionState.EditingThresholdMax || 
            _locomotionState == LocomotionState.EditingThresholdMin ||
            _locomotionState == LocomotionState.EditingZAxis)
        {
            EndEditing();
        }

        if (InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            InteractionStateMachine.Fire(InteractionEvents.EndEditSource);
        }
    }

    private void OnMenuUpPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(_shapeSelection && fromSource == PrimaryHand) {
            ShowSelectableShape(shapesManager.GetNextShape());
            scalingDown = false;
            scalingUp = true;
        }
        else if (fromSource == PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            IncreaseBrushSize();
        }

        else if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollDown = false;
            scrollUp = true;
        }
    }

    private void OnMenuUpReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollUp = false;
        }
        else if(fromSource == PrimaryHand && _shapeSelection) {
            scalingUp = false;
            scalingTimer = 0f;
        }
    }

    private void OnMenuDownPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(_shapeSelection && fromSource == PrimaryHand) {
            ShowSelectableShape(shapesManager.GetPreviousShape());
            scalingUp = false;
            scalingDown = true;
        }
        else if (fromSource == PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            DecreaseBrushSize();
        }
        else if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollUp = false;
            scrollDown = true;
        }
    }

    private void OnMenuDownReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollDown = false;
        }
        else if(fromSource == PrimaryHand && _shapeSelection) {
            scalingDown = false;
            scalingTimer = 0f;
        }
    }

    private void OnMenuLeftPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if(fromSource == PrimaryHand && _shapeSelection) {
            shapesManager.ChangeShapeMode();
            return;
        }

        if (fromSource != PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            UndoBrushStroke(fromSource);
        }
    }
    
    private void OnMenuRightPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
		if(fromSource == PrimaryHand && _shapeSelection) {
            shapesManager.ChangeShapeMode();
            return;
        }
        if (fromSource != PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            RedoBrushStroke(fromSource);  
        }
    }

    public void RedoBrushStroke(SteamVR_Input_Sources fromSource)
    {
        if (ActiveDataSet?.Mask?.RedoBrushStroke() ?? false)
        {
            ActiveDataSet?.GetMomentMapRenderer()?.CalculateMomentMaps();
            VibrateController(fromSource, 0.1f);
        }
    }

    public void UndoBrushStroke(SteamVR_Input_Sources fromSource)
    {
        if (ActiveDataSet?.Mask?.UndoBrushStroke() ?? false)
        {
            ActiveDataSet?.GetMomentMapRenderer()?.CalculateMomentMaps();
            VibrateController(fromSource, 0.1f);
        }
    }

    public void IncreaseBrushSize()
    {
        BrushSize += 2;
        UpdatePaintCursor();
    }


    public void DecreaseBrushSize()
    {
        BrushSize = Math.Max(1, BrushSize - 2);
        UpdatePaintCursor();
    }

    public void ResetBrushSize()
    {
        BrushSize = 1;
        UpdatePaintCursor();
    }

    private void UpdatePaintCursor()
    {
        ActiveDataSet?.SetCursorPosition(_handTransforms[PrimaryHandIndex].position, BrushSize);
    }

    private void OnQuickMenuChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        // Use primary hand for voice command activation (push-to-talk)
        if (fromSource == PrimaryHand)
        {
            if(_shapeSelection) {
                if(newState)
                {
                    if(shapesManager.isShapeSelected()) shapesManager.DeselectShape();
                    else {
                        shapesManager.DestroyCurrentShape();
                        shapesManager.MakeIdle();
                    }
                }
                return;
            }
            if (_config.usePushToTalk)
            {
                if (newState)
                {
                    PushToTalkButtonPressed?.Invoke();
                }
                else
                {
                    PushToTalkButtonReleased?.Invoke();
                }
            }
            return;
        }

        _paintMenuOn = CanvassQuickMenu.GetComponent<QuickMenuController>().paintMenu.activeSelf;
        _shapeMenuOn = CanvassPaintMenu.GetComponent<PaintMenuController>().shapeMenu.activeSelf;
        _savePopupOn = CanvassQuickMenu.GetComponent<QuickMenuController>().savePopup.activeSelf;
        _exportPopupOn = CanvassQuickMenu.GetComponent<QuickMenuController>().exportPopup.activeSelf;

        if (newState && !_paintMenuOn && !_shapeMenuOn && !_savePopupOn && !_exportPopupOn)
        {
            StartRequestQuickMenu(fromSource == SteamVR_Input_Sources.LeftHand ? 0 : 1);
        }
        else
        {
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
        // Skip input from secondary hand
        if (fromSource != PrimaryHand)
        {
            return;
        }

        // Skip input if in source ID editing mode (only trigger is accepted then)
        if (newState && InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            Debug.Log("Pinch button does nothing in this state.");
            return;
        }
        
        if (_shapeSelection)
        {
            if (newState)
            {
                if (shapesManager.isShapeSelected()) PlaceShape();
                shapesManager.SelectShape();
            }
            return;
        }

        InteractionStateMachine.Fire(newState ? InteractionEvents.InteractionStarted : InteractionEvents.InteractionEnded);
    }

    private void StateTransitionMovingToIdle()
    {
        _locomotionState = LocomotionState.Idle;
        _targetVignetteIntensity = 0;
    }

    private void StateTransitionIdleToMoving()
    {
        _locomotionState = LocomotionState.Moving;
        _targetVignetteIntensity = _maxVignetteIntensity;
    }
    
    public void StartThresholdEditing(bool editingMax)
    {
        _locomotionState = editingMax?LocomotionState.EditingThresholdMax: LocomotionState.EditingThresholdMin;
        _targetVignetteIntensity = 0;
        _previousControllerHeight =  _hands[PrimaryHandIndex].transform.position.y;
    }

    public void EndEditing()
    {
        _locomotionState = LocomotionState.Idle;
        _targetVignetteIntensity = 0;
    }

    public void UpdateSourceId()
    {
        if (ActiveDataSet?.CursorSource != 0)
        {
            SourceId = ActiveDataSet.CursorSource;
            ActiveDataSet.HighlightedSource = SourceId;
            AdditiveBrush = true;
        }
    }

    public void StartZAxisEditing()
    {
        _locomotionState = LocomotionState.EditingZAxis;
        _targetVignetteIntensity = 0;
        _previousControllerHeight = _hands[PrimaryHandIndex].transform.position.y;
    }

    private void StartRequestQuickMenu(int handIndex)
    {
        CanvassQuickMenu.transform.SetParent(_handTransforms[handIndex], false);
        CanvassQuickMenu.transform.localPosition= new Vector3(-0.1f,(handIndex == 0 ? 1: -1) * 0.175f, 0.10f);
        CanvassQuickMenu.transform.localRotation= Quaternion.Euler((handIndex == 0 ? 1: -1) * -3.25f,15f, 90f);
        CanvassQuickMenu.transform.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
        CanvassQuickMenu.SetActive(true);
        _isQuickMenu = true;
    }

    private void EndRequestQuickMenu()
    {
        CanvassQuickMenu.SetActive(false);
        _isQuickMenu = false;
    }

    private void StartSelection()
    {
        var startPosition = _handTransforms[PrimaryHandIndex].position;
        ActiveDataSet?.SetRegionPosition(startPosition, true);
        _selectionStopwatch.Reset();
        _selectionStopwatch.Start();

        Debug.Log($"Entering selecting state");
    }

    private void EndSelection()
    {
        var endPosition = _handTransforms[PrimaryHandIndex].position;

        _selectionStopwatch.Stop();
        var activeDataSet = ActiveDataSet;

        if (!activeDataSet)
        {
            return;
        }
        
        activeDataSet.ClearRegion();
        activeDataSet.ClearMeasure();
        var featureSetManager = activeDataSet.GetComponentInChildren<FeatureSetManager>();
        // Clear region selection by clicking selection. Attempt to select feature
        if (_selectionStopwatch.ElapsedMilliseconds < 200)
        {
            activeDataSet.SelectFeature(endPosition);
        }
        else
        {
            if (featureSetManager)
            {
                featureSetManager.CreateSelectionFeature(activeDataSet.RegionStartVoxel, activeDataSet.RegionEndVoxel);
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

        _lineAxisSeparation.Vertices[0] = _currentGripPositions[0];
        _lineAxisSeparation.Vertices[1] = _startGripCenter;
        _lineAxisSeparation.Vertices[2] = _currentGripPositions[1];
        _lineAxisSeparation.Activate();

        // Axis lines: 10 cm length
        _lineRotationAxes.Vertices[0] = _startGripCenter + _starGripForwardAxis * 0.1f;
        _lineRotationAxes.Vertices[1] = _startGripCenter;
        _lineRotationAxes.Vertices[2] = _startGripCenter + Vector3.up * 0.1f;
        _lineRotationAxes.Activate();

        if (_handInfoComponents != null)
        {
            _handInfoComponents[PrimaryHandIndex].enabled = true;
            _handInfoComponents[1 - PrimaryHandIndex].enabled = false;
        }
    }

    private void StateTransitionScalingToMoving()
    {
        _locomotionState = LocomotionState.Moving;
        _lineRotationAxes.Deactivate();
        _lineAxisSeparation.Deactivate();

        if (_handInfoComponents != null)
        {
            _handInfoComponents[PrimaryHandIndex].enabled = false;
            _handInfoComponents[1 - PrimaryHandIndex].enabled = false;
        }
    }

    private void Start()
    {
        var cameras = Camera.allCameras;
        foreach (var camera in cameras)
        { 
            camera.depthTextureMode = DepthTextureMode.Depth;
        }
    }
    
    private void Update()
    {
        // Common update functions
        if (_tunnellingVignetteOn)
            UpdateVignette();

        switch (_locomotionState)
        {
            case LocomotionState.Moving:
                UpdateMoving();
                break;
            case LocomotionState.Scaling:
                UpdateScaling();
                break;
            case LocomotionState.Idle:
                UpdateInteractions();
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

        if (scrollDown)
        {
            ScrollObject.GetComponent<CustomDragHandler>().MoveDown();
        }
        if (scrollUp)
        { 
            ScrollObject.GetComponent<CustomDragHandler>().MoveUp();
        }

        scalingTimer += Time.deltaTime;
        if(scalingUp)
        {
            if(scalingTimer > 0.03f) {
                shapesManager.IncreaseScale();
                scalingTimer = 0f;
            }
        }
        if(scalingDown)
        {
            if(scalingTimer > 0.03f) {
                shapesManager.DecreaseScale();
                scalingTimer = 0f;
            }
        }

        ToastNotification.Update();
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
        _lineAxisSeparation.Vertices[0] = _currentGripPositions[0];
        _lineAxisSeparation.Vertices[1] = rotationPoint;
        _lineAxisSeparation.Vertices[2] = _currentGripPositions[1];

        _lineRotationAxes.Vertices[0] = _startGripCenter + _starGripForwardAxis * (rollCurrentlyActive ? 0.1f : 0.0f);
        _lineRotationAxes.Vertices[1] = rotationPoint;
        _lineRotationAxes.Vertices[2] = _startGripCenter + Vector3.up * (yawCurrentlyActive ? 0.1f : 0.0f);
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

    private void UpdateEditingThreshold(bool editingMax)
    {
        var controllerHeight = _hands[PrimaryHandIndex].transform.position.y;
        var delta = controllerHeight - _previousControllerHeight;
        _previousControllerHeight = controllerHeight;

        string cursorString = "";
        
        foreach (var dataSet in _volumeDataSets)
        {
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
        
        if (_handInfoComponents != null)
        {
            _handInfoComponents[PrimaryHandIndex].enabled = true;
            _handInfoComponents[1 - PrimaryHandIndex].enabled = false;
            // Threshold info should always be displayed
            _handInfoComponents[PrimaryHandIndex].text = cursorString;
        }
    }

    private void UpdateEditingZAxis()
    {
        var controllerHeight = _hands[PrimaryHandIndex].transform.position.y;
        var delta = controllerHeight - _previousControllerHeight;
        _previousControllerHeight = controllerHeight;
        foreach (var dataSet in _volumeDataSets)
        {
            float zxRatio = dataSet.InitialScale.z/dataSet.InitialScale.x;
            var newValue = dataSet.transform.localScale.z + delta;
            dataSet.transform.localScale = new Vector3(dataSet.transform.localScale.x, dataSet.transform.localScale.y, 
                                                        Mathf.Clamp(newValue,
                                                                    dataSet.transform.localScale.x * zxRatio * dataSet.ZAxisMinFactor,
                                                                    dataSet.transform.localScale.x * zxRatio * dataSet.ZAxisMaxFactor));
        }
    }
    
    private void UpdateInteractions()
    {
        var dataSet = ActiveDataSet;
        if (!dataSet)
        {
            return;
        }

        var currentState = InteractionStateMachine.State;
        var cursorPosWorldSpace = _handTransforms[PrimaryHandIndex].position;
        var activeBrushSize = (currentState == InteractionState.Painting || currentState == InteractionState.IdlePainting) ? BrushSize : 1;
        dataSet.SetCursorPosition(cursorPosWorldSpace, activeBrushSize);

        if (currentState == InteractionState.Painting)
        {
            dataSet.PaintCursor(AdditiveBrush ? SourceId : (short) 0);
        }
        else if (currentState == InteractionState.Creating)
        {
            dataSet.SetRegionPosition(cursorPosWorldSpace, false);
        }
        // Edit the region bounds in Editing state, but not for mask feature sets
        else if (currentState == InteractionState.Editing && HasEditingAnchor && _editingFeature.FeatureSetParent.FeatureSetType != FeatureSetType.Mask)
        {
            var voxelPosition = dataSet.GetVoxelPositionWorldSpace(cursorPosWorldSpace);
            var newCornerMin = _editingFeature.CornerMin;
            var newCornerMax = _editingFeature.CornerMax;

            if (_editingAnchor.name.Contains("front"))
            {
                newCornerMax.z = voxelPosition.z;
            }
            else if (_editingAnchor.name.Contains("back"))
            {
                newCornerMin.z = voxelPosition.z;
            }

            if (_editingAnchor.name.Contains("right"))
            {
                newCornerMax.x = voxelPosition.x;
            }
            else if (_editingAnchor.name.Contains("left"))
            {
                newCornerMin.x = voxelPosition.x;
            }

            if (_editingAnchor.name.Contains("top"))
            {
                newCornerMax.y = voxelPosition.y;
            }
            else if (_editingAnchor.name.Contains("bottom"))
            {
                newCornerMin.y = voxelPosition.y;
            }
            
            _editingFeature.SetBounds(newCornerMin, newCornerMax);
            dataSet.SetRegionBounds(Vector3Int.RoundToInt(newCornerMin), Vector3Int.RoundToInt(newCornerMax), true);
        }
        
        string cursorString = "";

        if (currentState == InteractionState.Creating || currentState == InteractionState.Editing)
        {
            cursorString = GetSelectionString(dataSet);
        }
        else if (currentState == InteractionState.EditingSourceId)
        {
            if (dataSet.CursorSource != 0)
            {
                cursorString = $"Press trigger to update{Environment.NewLine}source ID to {dataSet.CursorSource}";
            }
            else
            {
                cursorString = "Place hand in desired source";
                if (SourceId >= 0)
                {
                    cursorString += $"{Environment.NewLine}Press trigger to cancel";
                }
            }
        }
        else
        {
            cursorString = GetFormattedCursorString(dataSet, Config.Instance.displayCursorInfoOutsideCube);
        }
        
        if (_handInfoComponents != null)
        {
            _handInfoComponents[PrimaryHandIndex].enabled = true;
            _handInfoComponents[1 - PrimaryHandIndex].enabled = false;
            _handInfoComponents[PrimaryHandIndex].text = (ShowCursorInfo || currentState == InteractionState.EditingSourceId) ? cursorString : "";
            if (IsCursorOutsideCube(dataSet))
            {
                _handInfoComponents[PrimaryHandIndex].color = new Color(0.86f, 0.078f, 0.235f); //This is crimson rgb(220,20,60)
            }
            else
            {
                _handInfoComponents[PrimaryHandIndex].color = Color.white;
            }
        }
    }

    private static string GetSelectionString(VolumeDataSetRenderer dataSetRenderer)
    {
        VolumeDataSet dataSet = dataSetRenderer.Data;

        var regionMax = Vector3.Max(dataSetRenderer.RegionStartVoxel, dataSetRenderer.RegionEndVoxel);
        var regionMin = Vector3.Min(dataSetRenderer.RegionStartVoxel, dataSetRenderer.RegionEndVoxel);
        var regionSize = regionMax - regionMin + Vector3.one;
        double xLength, yLength, zLength, angle;
        
        string stringToReturn = "";

        stringToReturn = $"Region: {regionSize.x} x {regionSize.y} x {regionSize.z}{Environment.NewLine}";     

        if (dataSetRenderer.HasWCS)
        {
            dataSet.GetFitsLengthsAst(regionMin, regionMax + Vector3.one, out xLength, out yLength, out zLength, out angle);
            string depthUnit = dataSet.GetAxisUnit(3);
            stringToReturn += $"Angle: {FormatAngle(angle)}{Environment.NewLine}"
                            + $"Depth: {dataSet.GetFormattedCoord(Math.Abs(zLength), 3),15} {dataSet.GetAstAttribute("Unit(3)")}";
        }

        return stringToReturn;
    }

    /// <summary>
    /// Checks if the cursor is outside the volume cube.
    /// </summary>
    /// <param name="dataSetRenderer">The renderer that determines the volume cube bounds.</param>
    /// <returns>True if the cursor is outside the cube, false otherwise.</returns>
    public static bool IsCursorOutsideCube(VolumeDataSetRenderer dataSetRenderer)
    {
        return (dataSetRenderer.CursorVoxel.x < 1 || dataSetRenderer.CursorVoxel.y < 1 || dataSetRenderer.CursorVoxel.z < 1 || dataSetRenderer.CursorVoxel.x > dataSetRenderer.Data.XDim || dataSetRenderer.CursorVoxel.y > dataSetRenderer.Data.YDim || dataSetRenderer.CursorVoxel.z > dataSetRenderer.Data.ZDim);
    }
    
    /// <summary>
    /// Get the string of information to display on the VR controller
    /// </summary>
    /// <param name="dataSetRenderer">The volume renderer object which the cursor information will reference</param>
    /// <param name="displayOutsideCube">Enable information to display when the cursor is outside the cube. Blank otherwise.</param>
    /// <returns>String in the correct format to display on the controller</returns>
    private static string GetFormattedCursorString(VolumeDataSetRenderer dataSetRenderer, bool displayOutsideCube = false)
    {
        VolumeDataSet dataSet = dataSetRenderer.Data;

        if (dataSet == null)
        {
            return "";
        }

        var voxelCoordinate = dataSetRenderer.CursorVoxel;

        if (!displayOutsideCube && IsCursorOutsideCube(dataSetRenderer))
        {
            return "";
        }
        string stringToReturn = "";
        
        if (dataSetRenderer.HasWCS)
        {
            double physX, physY, physZ, normX, normY;
            double normZ = 0;
            var dataCoordinate = dataSetRenderer.GetVoxelPositionDataSpace();
            dataSet.GetFitsCoordsAst(dataCoordinate.x, dataCoordinate.y, dataCoordinate.z, out physX, out physY, out physZ);
            dataSet.GetNormCoords(physX, physY, physZ, out normX, out normY, out normZ);
            stringToReturn += $"WCS: ({dataSet.GetFormattedCoord(normX, 1)}, {dataSet.GetFormattedCoord(normY, 2)}){Environment.NewLine}"
                            + $"{dataSet.GetAstAttribute("System(3)")}: {dataSet.GetFormattedCoord(normZ, 3),10} {dataSet.GetAstAttribute("Unit(3)")}{Environment.NewLine}";
        }

        stringToReturn += $"World: ({voxelCoordinate.x,5}, {voxelCoordinate.y,5}, {voxelCoordinate.z,5}){Environment.NewLine}";
        
        if (dataSet.isSubset())
        {
            Vector3Int dataVoxel = dataSetRenderer.GetVoxelPositionDataSpace();
            stringToReturn += $"Data: ({dataVoxel.x,5}, {dataVoxel.y,5}, {dataVoxel.z,5}){Environment.NewLine}";
        }

        stringToReturn += $"Value: {dataSetRenderer.CursorValue,16} {dataSet.GetPixelUnit()}";

        if (dataSet.HasRestFrequency)
            stringToReturn += $"{Environment.NewLine}{dataSet.GetConvertedDepth(voxelCoordinate.z)}";

        if (dataSetRenderer.CursorSource != 0)
            stringToReturn += $"{Environment.NewLine}Source: {dataSetRenderer.CursorSource}";

        return stringToReturn;
    }

    private static string FormatAngle(double angleInRad)
    {
        double deg = angleInRad / Math.PI * 180.0;
        if (deg >= 1)
            return deg.ToString("N3") + "°";
        else
        {
            double angleMin = (deg - Math.Truncate(deg)) * 60;
            double angleSec = Math.Truncate((angleMin - Math.Truncate(angleMin)) * 60 * 100) / 100;
            return Math.Truncate(angleMin).ToString("00") + "'" + angleSec.ToString("00.00") + "\"";             
        }
    }

    private void UpdateScalingText(VolumeDataSetRenderer dataSet)
    {
        // TODO: update scaling text
    }

    private static VRFamily DetermineVRFamily()
    {
        string vrModel = SteamVR.instance.hmd_ModelNumber.ToLower();
        if (vrModel.Contains("oculus"))
        {
            return VRFamily.Oculus;
        }

        if (vrModel.Contains("vive") || vrModel.Contains("index"))
        {
            return VRFamily.Vive;
        }

        if (vrModel.Contains("mixed reality") || vrModel.Contains("acer"))
        {
            return VRFamily.WindowsMixedReality;
        }

        Debug.Log($"Unknown VR model {vrModel}!");
        return VRFamily.Unknown;
    }

    /// <summary>
    /// Teleports the cube to place a given bounds directly in the user's view.
    /// </summary>
    /// <param name="boundsMin">Lower front left corner of the bounds.</param>
    /// <param name="boundsMax">Top rear right corner of the bounds.</param>
    public void Teleport(Vector3 boundsMin, Vector3 boundsMax)
    {
        float targetSize = 0.3f;
        float targetDistance = 0.5f;

        var activeDataSet = ActiveDataSet;
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

    public void VibrateController(SteamVR_Input_Sources hand, float duration = 0.25f, float frequency = 100.0f, float amplitude = 1.0f)
    {
        _player.leftHand.hapticAction.Execute(0, duration, frequency, amplitude, hand);
    }

    public void SetHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor)
    {
        _hoveredFeature = featureSetManager?.SelectedFeature;
        _hoveredAnchor = featureAnchor;
        //ActiveDataSet?.SetRegionPosition(_hoveredFeature.GetMinBounds(), true);
    }

    public void ClearHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor)
    {
        var hoveredFeature = featureSetManager?.SelectedFeature;
        if (_hoveredFeature == hoveredFeature && _hoveredAnchor == featureAnchor)
        {
            _hoveredFeature = null;
            _hoveredAnchor = null;
        }
    }

    private void StartRegionEditing()
    {
        _editingFeature = _hoveredFeature;
        _editingAnchor = _hoveredAnchor;
    }

    private void EndRegionEditing()
    {
        _editingFeature = null;
        _editingAnchor = null;
    }

    private void EnterPaintMode()
    {
        // Prevent transition if volumes aren't full resolution
        foreach (var dataSet in _volumeDataSets)
        {
            if (!dataSet.IsFullResolution)
            {
                return;
            }
        }
        foreach (var dataSet in _volumeDataSets)
        {
            // Ensure a mask is present for each dataset
            dataSet.InitialiseMask();
            dataSet.DisplayMask = true;
        }
        
        ActiveDataSet.FileChanged = true;
        // Automatically start source ID editing when entering paint mode
        SourceId = -1;
        InteractionStateMachine.Fire(InteractionEvents.StartEditSource);
    }

    private void ExitPaintMode()
    {
        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.DisplayMask = false;
        }
    }

    public void ToggleCursorInfoVisibility()
    {
        ShowCursorInfo = !ShowCursorInfo;
    }

    public void AddNewSource()
    {
        AdditiveBrush = true;
        if (ActiveDataSet)
        {
            SourceId = ActiveDataSet.Mask.NewSourceId;
            ActiveDataSet.HighlightedSource = SourceId;
            ActiveDataSet.Mask.NewSourceId++;
            // End editing mode without updating the source ID to the cursor voxel
            InteractionStateMachine.Fire(InteractionEvents.CancelEditSource);
        }
    }

    /// <summary>
    /// Function called when trying to change source ID, used to control state machine when interacting with paint mode.
    /// </summary>
    public void StartEditSourceID()
    {
        if (InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            Debug.Log("Starting source ID editing mode");
            InteractionStateMachine.Fire(InteractionEvents.StartEditSource);
        }
        else if (InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            Debug.Log("Already in source ID editing mode.");
        }
        else
        {
            Debug.Log($"Attempted to enter source ID editing mode from incorrect state!");
        }
    }
    
    public void SetBrushAdditive()
    {
        AdditiveBrush = true;
        if (SourceId <= 0)
        {
            InteractionStateMachine.Fire(InteractionEvents.StartEditSource);
        }
    }

    public void SetBrushSubtractive()
    {
        AdditiveBrush = false;
        if (InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            InteractionStateMachine.Fire(InteractionEvents.CancelEditSource);
        }
    }

    public void TakePicture()
    {
        CameraControllerTool cameraController = GameObject.Find("CameraController").GetComponentInChildren<CameraControllerTool>(true);
        cameraController.OnUse();
    }

    public void SaveSubCube()
    {
        ActiveDataSet.SaveSubCube();
    }

    private void OnDestroy()
    {
        _lineAxisSeparation?.Destroy();
        _lineRotationAxes?.Destroy();
    }

    private void OnTriggerChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState) {
        if(_shapeSelection) {
            GameObject moveableShape = shapesManager.GetMoveableShape();
            if(moveableShape != null) {
                if(newState) {
                    moveableShape.transform.SetParent(_handTransforms[PrimaryHandIndex]);
                }
                else {
                    moveableShape.transform.SetParent(volumeDatasetManager.transform.GetChild(0));
                }
                return;
            }
        }
    }


    public void ChangeShapeSelection() {
        _shapeSelection = !_shapeSelection;
    }

    //Used to display the selectable shapes for the user to scroll through in the scene
    public void ShowSelectableShape(GameObject currentShape) {
        if(currentShape == null) return;
        Vector3 position = _handTransforms[PrimaryHandIndex].position;
        Quaternion rotation = _handTransforms[PrimaryHandIndex].rotation;
        GameObject shape = Instantiate(currentShape, position, rotation);
        shape.transform.localScale = Vector3.Scale((ActiveDataSet.transform.localScale/20.0f), shape.transform.localScale);
        shape.transform.SetParent(_handTransforms[PrimaryHandIndex]);
        position = shape.transform.localPosition;
        position.z+=shape.transform.localScale.x/2.0f;
        shape.transform.localPosition = position;
        shapesManager.SetSelectableShape(shape);
    }

    //Places the selected shape into the scene
    public void PlaceShape() {
        GameObject shape = shapesManager.GetCurrentShape();
        GameObject selectedShape = shapesManager.GetSelectedShape();
        if(selectedShape == null) return;
        GameObject shapeCopy = Instantiate(shape,selectedShape.transform.position,selectedShape.transform.rotation);
        shapeCopy.name = shapesManager.GetShapeName(shapeCopy);
        if(shapeCopy.name.Contains("Cylinder")) {
            var collider = shapeCopy.GetComponent<CapsuleCollider>();
            collider.enabled = true;
        }
        else if (shapeCopy.name.Contains("Sphere")) {
            var collider = shapeCopy.GetComponent<SphereCollider>();
            collider.enabled = true;
        }
        else {
            var collider = shapeCopy.GetComponent<BoxCollider>();
            collider.enabled = true;
        }
        shapeCopy.GetComponent<Shape>().SetAdditive(selectedShape.GetComponent<Shape>().isAdditive());
        shapeCopy.transform.localScale = selectedShape.transform.localScale;
        shapeCopy.transform.SetParent(volumeDatasetManager.transform.GetChild(0));
        shapesManager.AddShape(shapeCopy);
        shapesManager.AddSelectedShape(shapeCopy);
        shapesManager.DeselectShape();
        shapesManager.DestroyCurrentShape();
        shapesManager.MakeIdle();
    }

}