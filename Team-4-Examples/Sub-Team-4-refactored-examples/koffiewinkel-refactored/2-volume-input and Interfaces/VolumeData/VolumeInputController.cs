/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 */
using System;
using DataFeatures;
using Interaction;
using Interaction.Interfaces;
using Stateless;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VolumeData;
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

    public enum InteractionState
    {
        IdleSelecting,
        IdlePainting,
        EditingSourceId,
        Creating,
        Editing,
        Painting,
        VideoCamPosRecording
    }

    public enum InteractionEvents
    {
        InteractionStarted,
        InteractionEnded,
        PaintModeEnabled,
        PaintModeDisabled,
        StartEditSource,
        EndEditSource,
        CancelEditSource,
        StartVideoRecording,
        EndVideoRecording
    }

    public GameObject CanvassQuickMenu;
    public SteamVR_Input_Sources PrimaryHand = SteamVR_Input_Sources.RightHand;
    public int PrimaryHandIndex => PrimaryHand == SteamVR_Input_Sources.LeftHand ? 0 : 1;
    public VolumeDataSetRenderer ActiveDataSet => _volumeDataSets != null ? Array.Find(_volumeDataSets, ds => ds != null && ds.isActiveAndEnabled) : null;
    public bool AdditiveBrush => _brushController?.AdditiveBrush ?? true;
    public int BrushSize => _brushController?.BrushSize ?? 1;
    public short SourceId => _brushController?.SourceId ?? (short)-1;

    [SerializeField] public bool InPlaceScaling = true;
    [SerializeField] public bool ScalingEnabled = true;
    [SerializeField] public float RotationAxisCutoff = 5.0f;
    [SerializeField] [Range(0.1f, 5.0f)] public float VignetteFadeSpeed = 2.0f;

    public GameObject toastNotificationPrefab = null;
    public GameObject followHead = null;

    public VolumeDataSetRenderer[] _volumeDataSets;
    public GameObject volumeDatasetManager;
    public VideoPosRecorder _videoPosRecorder { get; set; } = null;
    public bool ShowCursorInfo { get; private set; } = true;
    public bool scrollSelected = false;
    public GameObject ScrollObject;
    public bool scrollUp = false;
    public bool scrollDown = false;

    public event Action PushToTalkButtonPressed;
    public event Action PushToTalkButtonReleased;

    [SerializeReference] private ILocomotionController _locomotionController;
    [SerializeReference] private IInteractionController _interactionController;
    [SerializeReference] private IBrushController _brushController;
    [SerializeReference] private IVignetteController _vignetteController;
    [SerializeReference] private ICursorInfoFormatter _cursorInfoFormatter;
    [SerializeReference] private IQuickMenuPositioner _quickMenuPositioner;
    [SerializeReference] private IGaze _gazeProvider;

    

    private Player _player;
    private VRHand[] _hands;
    private Transform[] _handTransforms;
    private SteamVR_Action_Boolean _grabGripAction;
    private SteamVR_Action_Boolean _grabPinchAction;
    private SteamVR_Action_Boolean _quickMenuAction;
    private TextMeshPro[] _handInfoComponents;
    private Config _config;
    private VRFamily _vrFamily;
    private bool _isQuickMenu;
    private float _maxVignetteIntensity = 1.0f;
    private float _nextVideoRecordAllowedAt = 0.0f;
    private readonly float _videoRecordCooldown = 0.5f;

    public InteractionStateMachineProxy InteractionStateMachine { get; private set; }

    private static readonly System.Collections.Generic.Dictionary<VRFamily, Vector3> PointerOffsetsLeft =
        new System.Collections.Generic.Dictionary<VRFamily, Vector3>
        {
            { VRFamily.Unknown, Vector3.zero },
            { VRFamily.Oculus, new Vector3(0.005f, -0.035f, 0.0f) },
            { VRFamily.Vive, new Vector3(0, -0.09f, 0.06f) },
            { VRFamily.WindowsMixedReality, new Vector3(0.05f, -0.029f, 0.03f) }
        };

    private static readonly System.Collections.Generic.Dictionary<VRFamily, Vector3> PointerOffsetsRight =
        new System.Collections.Generic.Dictionary<VRFamily, Vector3>
        {
            { VRFamily.Unknown, Vector3.zero },
            { VRFamily.Oculus, new Vector3(-0.005f, -0.035f, 0.0f) },
            { VRFamily.Vive, new Vector3(0, -0.09f, 0.06f) },
            { VRFamily.WindowsMixedReality, new Vector3(-0.05f, -0.029f, 0.03f) }
        };

    private void OnEnable()
    {
        _config = Config.Instance;
        _vrFamily = DetermineVRFamily();
        _maxVignetteIntensity = _config.tunnellingVignetteIntensity;

        if (_player == null)
        {
            _player = GetComponent<Player>();
            _hands = new[] { _player.leftHand, _player.rightHand };
            _handTransforms = new Transform[2];
            for (var i = 0; i < 2; i++)
            {
                var laserPointer = _hands[i].GetComponentInChildren<LaserPointer>();
                _handTransforms[i] = laserPointer != null ? laserPointer.transform : _hands[i].transform;
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

        UpdateDataSets();
        _handInfoComponents = new[] { _hands[0].GetComponentInChildren<TextMeshPro>(), _hands[1].GetComponentInChildren<TextMeshPro>() };
        BuildCollaborators();
    }

    private void OnDisable()
    {
        if (_player == null)
        {
            return;
        }

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
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.RemoveOnStateUpListener(OnMenuUpReleased, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuUp")?.RemoveOnStateUpListener(OnMenuUpReleased, SteamVR_Input_Sources.RightHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.RemoveOnStateUpListener(OnMenuDownReleased, SteamVR_Input_Sources.LeftHand);
        SteamVR_Input.GetAction<SteamVR_Action_Boolean>("MenuDown")?.RemoveOnStateUpListener(OnMenuDownReleased, SteamVR_Input_Sources.RightHand);
    }

    private void Update()
    {
        _locomotionController?.Update(Time.deltaTime);
        _vignetteController?.Update(Time.deltaTime);
    }

    public void UpdateDataSets()
    {
        var manager = GameObject.Find("VolumeDataSetManager");
        _volumeDataSets = manager ? manager.GetComponentsInChildren<VolumeDataSetRenderer>(true) : Array.Empty<VolumeDataSetRenderer>();
    }

    private void BuildCollaborators()
    {
        _cursorInfoFormatter ??= new CursorInfoFormatter();
        _quickMenuPositioner ??= new QuickMenuPositioner(CanvassQuickMenu);
        _gazeProvider ??= new CameraGazeProvider();

        var brushController = _brushController as BrushController;
        if (brushController == null)
        {
            brushController = new BrushController(
                () => ActiveDataSet,
                () => _handTransforms[PrimaryHandIndex].position,
                () => _interactionController?.Fire(Interaction.Interfaces.InteractionEvent.StartEditSource),
                () => _interactionController != null ? _interactionController.CurrentState : Interaction.Interfaces.InteractionState.IdleSelecting,
                () => _interactionController?.Fire(Interaction.Interfaces.InteractionEvent.CancelEditSource),
                hand => VibrateController(hand == BrushHand.Primary ? PrimaryHand : (PrimaryHand == SteamVR_Input_Sources.LeftHand ? SteamVR_Input_Sources.RightHand : SteamVR_Input_Sources.LeftHand), 0.1f));
            _brushController = brushController;
        }

        _interactionController ??= new InteractionController(
            () => ActiveDataSet,
            () => _handTransforms[PrimaryHandIndex].position,
            () => PrimaryHandIndex,
            () => ShowCursorInfo,
            () => _locomotionController == null || _locomotionController.CurrentState == LocomotionState.Idle,
            EnterPaintModeCore,
            ExitPaintModeCore,
            () => ActiveDataSet?.SetRegionPosition(_handTransforms[PrimaryHandIndex].position, true),
            () => { },
            StartVideoCamPosRecording,
            EndVideoCamPosRecording,
            () => _brushController.BrushSize,
            () => _brushController.AdditiveBrush,
            () => _brushController.SourceId,
            _cursorInfoFormatter,
            _handInfoComponents,
            () => Config.Instance.displayCursorInfoOutsideCube,
            brushController);

        _locomotionController ??= new LocomotionController(
            () => _handTransforms,
            () => PrimaryHandIndex,
            () => _volumeDataSets,
            () => _handInfoComponents,
            () => _interactionController?.Update(),
            intensity => _vignetteController?.SetTarget(intensity),
            InPlaceScaling,
            ScalingEnabled,
            RotationAxisCutoff,
            _maxVignetteIntensity);

        _vignetteController ??= new VignetteController(() => _volumeDataSets, _config.tunnellingVignetteOn, _maxVignetteIntensity, VignetteFadeSpeed);
        InteractionStateMachine = new InteractionStateMachineProxy(_interactionController);
    }

    private void OnGripChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        int gripCount = (_grabGripAction.GetState(SteamVR_Input_Sources.LeftHand) ? 1 : 0) + (_grabGripAction.GetState(SteamVR_Input_Sources.RightHand) ? 1 : 0);
        switch (gripCount)
        {
            case 0:
                _locomotionController.TransitionToIdle();
                break;
            case 1:
                if (_locomotionController.CurrentState == LocomotionState.Idle) _locomotionController.TransitionToMoving();
                else _locomotionController.TransitionToMoving();
                break;
            case 2:
                _locomotionController.TransitionToScaling();
                break;
        }
    }

    private void OnPinchChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (fromSource != PrimaryHand)
        {
            return;
        }

        if (newState && InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            return;
        }

        if (InteractionStateMachine.State == InteractionState.VideoCamPosRecording)
        {
            if (newState && Time.time >= _nextVideoRecordAllowedAt)
            {
                _nextVideoRecordAllowedAt = Time.time + _videoRecordCooldown;
                AddNewLocation(fromSource);
            }

            return;
        }

        _interactionController.Fire(newState ? Interaction.Interfaces.InteractionEvent.InteractionStarted : Interaction.Interfaces.InteractionEvent.InteractionEnded);
    }

    private void OnQuickMenuChanged(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
    {
        if (fromSource == PrimaryHand)
        {
            if (_config.usePushToTalk)
            {
                if (newState) PushToTalkButtonPressed?.Invoke();
                else PushToTalkButtonReleased?.Invoke();
            }
            return;
        }

        if (newState && InteractionStateMachine.State == InteractionState.IdleSelecting)
        {
            int handIndex = fromSource == SteamVR_Input_Sources.LeftHand ? 0 : 1;
            _quickMenuPositioner.Show(handIndex, _handTransforms[handIndex]);
            _isQuickMenu = true;
        }
        else
        {
            _quickMenuPositioner.Hide();
            _isQuickMenu = false;
        }
    }

    private void OnUiInteractDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (_locomotionController.CurrentState == LocomotionState.EditingThresholdMax ||
            _locomotionController.CurrentState == LocomotionState.EditingThresholdMin ||
            _locomotionController.CurrentState == LocomotionState.EditingZAxis)
        {
            _locomotionController.EndEditing();
        }

        if (InteractionStateMachine.State == InteractionState.EditingSourceId)
        {
            _interactionController.Fire(Interaction.Interfaces.InteractionEvent.EndEditSource);
        }
    }

    private void OnMenuUpPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            _brushController.IncreaseBrushSize();
        }
        else if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollDown = false;
            scrollUp = true;
        }
    }

    private void OnMenuDownPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            _brushController.DecreaseBrushSize();
        }
        else if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollUp = false;
            scrollDown = true;
        }
    }

    private void OnMenuLeftPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource != PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            _brushController.UndoBrushStroke(BrushHand.Secondary);
        }
    }

    private void OnMenuRightPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource != PrimaryHand && InteractionStateMachine.State == InteractionState.IdlePainting)
        {
            _brushController.RedoBrushStroke(BrushHand.Secondary);
        }
    }

    private void OnMenuUpReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollUp = false;
        }
    }

    private void OnMenuDownReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == PrimaryHand && scrollSelected)
        {
            scrollDown = false;
        }
    }

    public void StartThresholdEditing(bool editingMax) => _locomotionController.StartThresholdEditing(editingMax);
    public void EndEditing() => _locomotionController.EndEditing();
    public void StartZAxisEditing() => _locomotionController.StartZAxisEditing();
    public void ToggleCursorInfoVisibility() => ShowCursorInfo = !ShowCursorInfo;
    public void SetBrushAdditive() => _brushController.SetBrushAdditive();
    public void SetBrushSubtractive() => _brushController.SetBrushSubtractive();
    public void AddNewSource() => _brushController.AddNewSource();
    public void StartEditSourceID() => _brushController.StartEditSourceID();
    public void IncreaseBrushSize() => _brushController.IncreaseBrushSize();
    public void DecreaseBrushSize() => _brushController.DecreaseBrushSize();
    public void ResetBrushSize() => _brushController.ResetBrushSize();
    public void UndoBrushStroke(SteamVR_Input_Sources fromSource) => _brushController.UndoBrushStroke(MapHand(fromSource));
    public void RedoBrushStroke(SteamVR_Input_Sources fromSource) => _brushController.RedoBrushStroke(MapHand(fromSource));
    public void SetHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor) => _interactionController.SetHoveredFeature(featureSetManager, featureAnchor);
    public void ClearHoveredFeature(FeatureSetManager featureSetManager, FeatureAnchor featureAnchor) => _interactionController.ClearHoveredFeature(featureSetManager, featureAnchor);

    public void Teleport(Vector3 boundsMin, Vector3 boundsMax)
    {
        float targetSize = 0.3f;
        float targetDistance = 0.5f;
        var activeDataSet = ActiveDataSet;
        if (activeDataSet == null)
        {
            return;
        }

        var dataSetTransform = activeDataSet.transform;
        Vector3 boundsMinObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMin);
        Vector3 boundsMaxObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMax);
        Vector3 deltaObjectSpace = boundsMaxObjectSpace - boundsMinObjectSpace;
        Vector3 deltaWorldSpace = dataSetTransform.TransformVector(deltaObjectSpace);
        float lengthWorldSpace = deltaWorldSpace.magnitude;
        float scalingRequired = targetSize / lengthWorldSpace;
        dataSetTransform.localScale *= scalingRequired;

        Vector3 targetPosition = _gazeProvider.GazeOrigin + _gazeProvider.GazeDirection * targetDistance;
        Vector3 centerWorldSpace = dataSetTransform.TransformPoint((boundsMaxObjectSpace + boundsMinObjectSpace) / 2.0f);
        dataSetTransform.position += targetPosition - centerWorldSpace;
    }

    public void TeleportToVidRecLoc(Vector3 pos, Vector3 rotEulerAngles)
    {
        var activeDataSet = ActiveDataSet;
        if (activeDataSet == null)
        {
            return;
        }

        var dataSetTransform = activeDataSet.transform;
        dataSetTransform.rotation = _gazeProvider.GazeRotation * Quaternion.Inverse(Quaternion.Euler(rotEulerAngles));
        dataSetTransform.position = _gazeProvider.GazeOrigin - dataSetTransform.TransformVector(pos);
    }

    public void AddNewLocation(SteamVR_Input_Sources fromSource)
    {
        if (_videoPosRecorder == null || ActiveDataSet == null)
        {
            return;
        }

        var mode = _videoPosRecorder.GetRecordingMode();
        switch (mode)
        {
            case VideoPosRecorder.videoLocRecMode.CURSOR:
                Vector3 cursorPos = ActiveDataSet.ConvertWorldPositionToDataCubePosition(_handTransforms[PrimaryHandIndex].position);
                _videoPosRecorder.addLocation(cursorPos, Vector3.zero, mode);
                VibrateController(fromSource, 0.1f);
                break;
            case VideoPosRecorder.videoLocRecMode.HEAD:
                Vector3 headPos = ActiveDataSet.ConvertWorldPositionToDataCubePosition(_gazeProvider.GazeOrigin);
                Vector3 headRot = ActiveDataSet.ConvertWorldRotationToDatacubeRotation(_gazeProvider.GazeRotation).eulerAngles;
                _videoPosRecorder.addLocation(headPos, headRot, mode);
                VibrateController(fromSource, 0.1f);
                break;
            default:
                Debug.LogError("Invalid video recording mode.");
                break;
        }
    }

    public void TakePicture()
    {
        CameraControllerTool cameraController = GameObject.Find("CameraController").GetComponentInChildren<CameraControllerTool>(true);
        cameraController.OnUse();
    }

    public void SaveSubCube()
    {
        ActiveDataSet?.SaveSubCube();
    }

    public void StartVideoCamPosRecording()
    {
        _nextVideoRecordAllowedAt = Time.time;
        Debug.Log("Entering video position recording mode.");
    }

    public void EndVideoCamPosRecording()
    {
        Debug.Log("Exiting video position recording mode.");
    }
    public void ChangeShapeSelection() { }

    public void VibrateController(SteamVR_Input_Sources hand, float duration = 0.25f, float frequency = 100.0f, float amplitude = 1.0f)
    {
        _player.leftHand.hapticAction.Execute(0, duration, frequency, amplitude, hand);
    }

    private void EnterPaintModeCore()
    {
        foreach (var dataSet in _volumeDataSets)
        {
            if (!dataSet.IsFullResolution)
            {
                return;
            }
        }

        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.InitialiseMask();
            dataSet.DisplayMask = true;
        }

        if (ActiveDataSet != null)
        {
            ActiveDataSet.FileChanged = true;
        }

        (_brushController as BrushController)?.ResetSourceIdForPaintEntry();
        _interactionController?.Fire(Interaction.Interfaces.InteractionEvent.StartEditSource);
    }

    private void ExitPaintModeCore()
    {
        foreach (var dataSet in _volumeDataSets)
        {
            dataSet.DisplayMask = false;
        }
    }

    private BrushHand MapHand(SteamVR_Input_Sources fromSource)
    {
        return fromSource == PrimaryHand ? BrushHand.Primary : BrushHand.Secondary;
    }

    private static VRFamily DetermineVRFamily()
    {
        try
        {
            var instance = SteamVR.instance;
            if (instance == null || string.IsNullOrEmpty(instance.hmd_ModelNumber))
            {
                return VRFamily.Unknown;
            }

            string vrModel = instance.hmd_ModelNumber.ToLower();
            if (vrModel.Contains("oculus")) return VRFamily.Oculus;
            if (vrModel.Contains("vive") || vrModel.Contains("index")) return VRFamily.Vive;
            if (vrModel.Contains("mixed reality") || vrModel.Contains("acer")) return VRFamily.WindowsMixedReality;
            return VRFamily.Unknown;
        }
        catch
        {
            return VRFamily.Unknown;
        }
    }

    public sealed class InteractionStateMachineProxy
    {
        private readonly IInteractionController _controller;

        public InteractionStateMachineProxy(IInteractionController controller)
        {
            _controller = controller;
        }

        public InteractionState State => ToOuter(_controller.CurrentState);

        public void Fire(InteractionEvents interactionEvent)
        {
            _controller.Fire(ToInner(interactionEvent));
        }

        private static InteractionState ToOuter(Interaction.Interfaces.InteractionState state)
        {
            return state switch
            {
                Interaction.Interfaces.InteractionState.IdleSelecting => InteractionState.IdleSelecting,
                Interaction.Interfaces.InteractionState.IdlePainting => InteractionState.IdlePainting,
                Interaction.Interfaces.InteractionState.EditingSourceId => InteractionState.EditingSourceId,
                Interaction.Interfaces.InteractionState.Creating => InteractionState.Creating,
                Interaction.Interfaces.InteractionState.Editing => InteractionState.Editing,
                Interaction.Interfaces.InteractionState.Painting => InteractionState.Painting,
                Interaction.Interfaces.InteractionState.VideoCamPosRecording => InteractionState.VideoCamPosRecording,
                _ => InteractionState.IdleSelecting
            };
        }

        private static Interaction.Interfaces.InteractionEvent ToInner(InteractionEvents interactionEvent)
        {
            return interactionEvent switch
            {
                InteractionEvents.InteractionStarted => Interaction.Interfaces.InteractionEvent.InteractionStarted,
                InteractionEvents.InteractionEnded => Interaction.Interfaces.InteractionEvent.InteractionEnded,
                InteractionEvents.PaintModeEnabled => Interaction.Interfaces.InteractionEvent.PaintModeEnabled,
                InteractionEvents.PaintModeDisabled => Interaction.Interfaces.InteractionEvent.PaintModeDisabled,
                InteractionEvents.StartEditSource => Interaction.Interfaces.InteractionEvent.StartEditSource,
                InteractionEvents.EndEditSource => Interaction.Interfaces.InteractionEvent.EndEditSource,
                InteractionEvents.CancelEditSource => Interaction.Interfaces.InteractionEvent.CancelEditSource,
                InteractionEvents.StartVideoRecording => Interaction.Interfaces.InteractionEvent.StartVideoRecording,
                InteractionEvents.EndVideoRecording => Interaction.Interfaces.InteractionEvent.EndVideoRecording,
                _ => Interaction.Interfaces.InteractionEvent.InteractionEnded
            };
        }
    }
}
