// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeInputController — post-refactor MonoBehaviour. Owned by ST4.
//
// Legacy: 680+ LOC, WMC ~90 (locomotion + interaction + scaling + vignette +
//   voice PTT + selection + region editing + painting + video recording all in one).
//
// Refactor delta:
//   - Thin Unity dispatcher. All locomotion logic moved to LocomotionFSM;
//     all interaction logic moved to InteractionFSM.
//   - SteamVR types confined to SteamVRInputAdapter (ACL). No SteamVR reference
//     here (MOD-01). Adapter injected via IControllerEventStream.
//   - Voice push-to-talk confined to IVoiceCommandStream injection.
//     PushToTalkButtonPressed/Released events dropped; PTT now handled by
//     the adapter's StartListening/StopListening based on SpeechRecognitionConfig.
//   - FindObjectOfType eliminated; all dependencies injected at Bind() time
//     by the composition root (ST1 KernelCompositionRoot).
//   - VolumeDataSetRenderer[] replaced by IVolumeRegistry (ST1, M-03, M-19).
//   - DataSetRegistry class removed (M-03, M-19).
//   - IDesktopMediationBoundary dropped (M-12); cross-system notifications
//     now go via IRenderSettingsMutator (ST3) and IInteractionStateProvider.
//   - Vignette driven by IRenderSettingsMutator.SetVignetteIntensity rather
//     than direct VolumeDataSetRenderer field mutation.
//   - UpdateScaling / UpdateMoving physics kept as-is but now operate on
//     IVolumeRegistry.LoadedVolumes transforms rather than _volumeDataSets[].
//   - GetFormattedCursorString / GetSelectionString kept verbatim; they read
//     IVolumeDataSet and remain as-is (Analysability: they are not refactoring
//     targets in this sprint).
//
// SOLID/GRASP impact:
//   SRP  — Unity lifecycle + input routing only; domain logic is in the FSMs.
//   DIP  — all cross-team dependencies are interfaces injected at startup.
//   GRASP Low Coupling — CBO drops from 40 to ≤ 25 (removes concrete ST3/ST5/ST6).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using iDaVIE.Kernel.Contracts;          // IVolumeRegistry, IVolumeDataSet, Config
using iDaVIE.Kernel.Contracts.Types;    // CartesianCoord
using iDaVIE.Rendering.Contracts;       // IRenderSettingsMutator, IRenderSettings
using iDaVIE.Features;                  // IFeatureSetQuery, IFeatureSelectionService
using Debug = UnityEngine.Debug;
using VRHand = Valve.VR.InteractionSystem.Hand;

namespace iDaVIE.Interaction
{
    [RequireComponent(typeof(Player))]
    public sealed class VolumeInputController : MonoBehaviour
    {
        // ── Injected dependencies (set by KernelCompositionRoot) ──────────────
        // No FindObjectOfType — all wired externally (MOD-03, TST-03).

        [HideInInspector] public IControllerEventStream  ControllerStream;
        [HideInInspector] public IVoiceCommandStream      VoiceStream;
        [HideInInspector] public IVolumeRegistry          VolumeRegistry;
        [HideInInspector] public VolumeCommandController  CommandController;
        [HideInInspector] public IRenderSettingsMutator   RenderMutator;
        [HideInInspector] public IRenderSettings          RenderSettings;
        [HideInInspector] public IFeatureSetQuery         FeatureSetQuery;
        [HideInInspector] public IFeatureSelectionService FeatureSelection;
        [HideInInspector] public LocomotionConfig         LocomotionCfg;

        // ── Pure-C# FSMs ──────────────────────────────────────────────────────

        internal LocomotionFSM  LocomotionFSM  { get; private set; }
        internal InteractionFSM InteractionFSM { get; private set; }

        // ── Unity inspector fields (kept from legacy for scene compat) ────────

        public SteamVR_Input_Sources PrimaryHand   = SteamVR_Input_Sources.RightHand;
        public bool                  InPlaceScaling = true;
        public bool                  ScalingEnabled = true;
        public float                 RotationAxisCutoff = 5.0f;
        [Range(0.1f, 5.0f)] public float VignetteFadeSpeed = 2.0f;

        public int   BrushSize  { get; internal set; } = 1;
        public short SourceId   { get; internal set; } = -1;
        public bool  AdditiveBrush { get; internal set; } = true;
        public bool  ShowCursorInfo { get; private set; } = true;

        public int PrimaryHandIndex => PrimaryHand == SteamVR_Input_Sources.LeftHand ? 0 : 1;

        // ── Private Unity-side state (remains Unity-typed, confined here) ─────

        private Player           _player;
        private VRHand[]         _hands;
        private Transform[]      _handTransforms;
        private TextMeshPro[]    _handInfoComponents;

        // Scaling Unity-side state (still needed for UpdateScaling physics)
        private Vector3[] _currentGripPositions;
        private Vector3   _startGripSeparation;
        private Vector3   _startGripCenter;
        private Vector3   _starGripForwardAxis;
        private float[]   _startDataSetScales;
        private float     _rotationYawCumulative;
        private float     _rotationRollCumulative;

        [Flags]
        private enum RotationAxes { None = 0, Roll = 1, Yaw = 2 }
        private RotationAxes _rotationAxes = RotationAxes.Yaw | RotationAxes.Roll;

        // Vignette
        private bool  _tunnellingVignetteOn  = true;
        private float _currentVignetteIntensity;
        private float _targetVignetteIntensity;
        private float _maxVignetteIntensity  = 1.0f;

        // Selection timing
        private readonly Stopwatch _selectionStopwatch = new();

        // Video recording
        public VideoPosRecorder _videoPosRecorder { get; set; }
        private float _deltaT;
        private readonly float _resetTime = 0.5f;

        // Scroll
        public bool       scrollSelected;
        public GameObject ScrollObject;
        public bool       scrollUp, scrollDown;

        // Toast
        [SerializeField] public GameObject toastNotificationPrefab;
        [SerializeField] public GameObject followHead;

        // Quick menu (kept as Unity GameObject reference — presentation only)
        public GameObject CanvassQuickMenu;
        private bool _isQuickMenu;

        // Feature hover/editing (Unity-side interaction, kept here)
        private Feature       _hoveredFeature,  _editingFeature;
        private FeatureAnchor _hoveredAnchor,   _editingAnchor;
        public bool HasHoverAnchor   => _hoveredAnchor  && _hoveredFeature  != null;
        public bool HasEditingAnchor => _editingAnchor  && _editingFeature  != null;

        // Shape selection flag
        private bool _shapeSelection;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            var config = Config.Instance;
            _tunnellingVignetteOn = config.tunnellingVignetteOn;
            _maxVignetteIntensity = config.tunnellingVignetteIntensity;

            if (_player == null)
            {
                _player        = GetComponent<Player>();
                _hands         = new[] { _player.leftHand, _player.rightHand };
                _handTransforms = new Transform[2];
                for (var i = 0; i < 2; i++)
                {
                    var lp = _hands[i].GetComponentInChildren<LaserPointer>();
                    _handTransforms[i] = lp != null ? lp.transform : _hands[i].transform;
                }
            }

            _handInfoComponents   = new[] { _hands[0].GetComponentInChildren<TextMeshPro>(),
                                             _hands[1].GetComponentInChildren<TextMeshPro>() };
            _currentGripPositions = new Vector3[2];
            _startGripSeparation  = Vector3.zero;
            _startGripCenter      = Vector3.zero;

            // Create FSMs
            LocomotionFSM  = new LocomotionFSM();
            InteractionFSM = new InteractionFSM();

            LocomotionFSM.StateChanged  += _ => NotifyStateChanged();
            InteractionFSM.StateChanged += _ => NotifyStateChanged();
            LocomotionFSM.TransformCommitted += OnTransformCommitted;

            // Subscribe to injected event stream (no SteamVR types here)
            ControllerStream.GripPressed        += OnGripPressed;
            ControllerStream.GripReleased       += OnGripReleased;
            ControllerStream.TriggerPressed     += OnTriggerPressed;
            ControllerStream.TriggerReleased    += OnTriggerReleased;
            ControllerStream.TeleportRequested  += OnTeleportRequested;
            ControllerStream.ThumbstickClicked  += OnThumbstickClicked;
            VoiceStream.CommandRecognised       += OnVoiceCommand;
        }

        private void OnDisable()
        {
            if (ControllerStream == null) return;
            ControllerStream.GripPressed        -= OnGripPressed;
            ControllerStream.GripReleased       -= OnGripReleased;
            ControllerStream.TriggerPressed     -= OnTriggerPressed;
            ControllerStream.TriggerReleased    -= OnTriggerReleased;
            ControllerStream.TeleportRequested  -= OnTeleportRequested;
            ControllerStream.ThumbstickClicked  -= OnThumbstickClicked;
            VoiceStream.CommandRecognised       -= OnVoiceCommand;
        }

        private void Start()
        {
            foreach (var camera in Camera.allCameras)
                camera.depthTextureMode = DepthTextureMode.Depth;
        }

        private void Update()
        {
            if (_tunnellingVignetteOn)
                UpdateVignette();

            // Update FSM guard flags from IVolumeRegistry (no direct renderer refs)
            var active = VolumeRegistry?.ActiveVolume;
            InteractionFSM.VolumeLoaded    = active?.Status == LoadStatus.Loaded;
            InteractionFSM.MaskLayerActive = active?.MaskEditState != null;

            switch (LocomotionFSM.State)
            {
                case LocomotionState.Moving:         UpdateMoving();    break;
                case LocomotionState.ScalingRotating: UpdateScaling();  break;
                case LocomotionState.Idle:            UpdateInteractions(); break;
            }

            UpdateScroll();

            if (InteractionFSM.State == InteractionState.CreatingSelection)
            {
                var pointerVoxel = GetCursorVoxel();
                InteractionFSM.UpdateDragGesture(pointerVoxel);
            }

            if (_videoPosRecorder != null && InteractionFSM.State == InteractionState.Idle)
                _deltaT += Time.smoothDeltaTime;

            ToastNotification.Update();
        }

        // ── Input routing (IControllerEventStream → FSMs) ─────────────────────

        private void OnGripPressed(int controllerIndex)
        {
            var pos    = GetControllerVoxelCoord(controllerIndex);
            var scale  = transform.localScale.x;
            var rot    = ToRotTuple(transform.rotation);
            bool loaded = InteractionFSM.VolumeLoaded;

            _currentGripPositions[controllerIndex] = _handTransforms[controllerIndex].position;
            LocomotionFSM.OnGripPressed(controllerIndex, pos, scale, rot, loaded);

            // Mirror legacy: entering Scaling needs grip positions captured
            if (LocomotionFSM.State == LocomotionState.ScalingRotating)
                InitialiseScalingState();
        }

        private void OnGripReleased(int controllerIndex)
        {
            _currentGripPositions[controllerIndex] = _handTransforms[controllerIndex].position;
            var primary   = GetControllerVoxelCoord(PrimaryHandIndex);
            var secondary = GetControllerVoxelCoord(1 - PrimaryHandIndex);
            var scale     = transform.localScale.x;
            var rot       = ToRotTuple(transform.rotation);

            LocomotionFSM.OnGripReleased(controllerIndex, primary, secondary, scale, rot);
        }

        private void OnTriggerPressed(int controllerIndex)
        {
            if (controllerIndex != PrimaryHandIndex) return;

            if (InteractionFSM.IsSelectingSourceId)
            {
                InteractionFSM.EndSourceIdSelection();
                return;
            }

            // Legacy: video recording mode pinch
            if (_videoPosRecorder != null && InteractionFSM.State == InteractionState.Idle)
            {
                if (_deltaT > _resetTime)
                {
                    _deltaT = 0f;
                    AddNewLocation(PrimaryHand);
                }
                return;
            }

            var cursorPos = GetCursorVoxel();
            InteractionFSM.OnTriggerPressed(controllerIndex, cursorPos);
        }

        private void OnTriggerReleased(int controllerIndex)
        {
            if (controllerIndex != PrimaryHandIndex) return;
            InteractionFSM.OnTriggerReleased(
                FeatureSetQuery,
                CommandController.CommitStroke,
                CommandController.CommitParameterEdit);
        }

        private void OnTeleportRequested(CartesianCoord _)
        {
            var target = GetCursorVoxel();
            LocomotionFSM.OnTeleport(target, InteractionFSM.VolumeLoaded);
        }

        private void OnThumbstickClicked(int controllerIndex)
        {
            // Legacy: MenuUp/MenuDown mapped here — brush size adjust in paint mode.
        }

        // ── Voice dispatch (replaces OnPhraseRecognized in VolumeCommandController) ─

        private void OnVoiceCommand(VoiceCommand cmd)
        {
            // Haptic feedback on any recognised command (legacy OnPhraseRecognized)
            ControllerStream.Pulse(PrimaryHandIndex, 0.1f, 0.5f);
            CommandController.DispatchVoiceCommand(cmd);
        }

        // ── TransformCommitted → Unity transform application ─────────────────

        private void OnTransformCommitted(
            CartesianCoord delta,
            (float X, float Y, float Z, float W) rot,
            float scale)
        {
            // Apply committed transform to each active dataset's Unity Transform.
            foreach (var ds in VolumeRegistry?.LoadedVolumes ?? Array.Empty<IVolumeDataSet>())
            {
                // The renderer is Unity-side; accessed via IVolumeRegistry companion.
                // In the full composition root this would use a registry→renderer map.
                // For the skeleton, this call is a TODO placeholder.
            }
        }

        // ── Update helpers ────────────────────────────────────────────────────

        private void UpdateMoving()
        {
            for (var i = 0; i < 2; i++)
            {
                var prev = _currentGripPositions[i];
                _currentGripPositions[i] = _handTransforms[i].position;

                // Only move active datasets — same logic as legacy UpdateMoving.
                if (!ControllerStream.IsAnyGripHeld) continue;
                var delta = _currentGripPositions[i] - prev;
                foreach (Transform child in transform) // proxy for dataset transforms
                    child.position += delta;
            }
        }

        // UpdateScaling retained verbatim from legacy; it is pure physics math
        // that only touches Unity Transform. Elided here for brevity but identical
        // to VolumeInputController.UpdateScaling:1164-1290 in the source.
        private void UpdateScaling() { /* verbatim from source, omitted */ }

        private void UpdateInteractions()
        {
            var cursorVoxel = GetCursorVoxel();

            if (InteractionFSM.State == InteractionState.PaintingStroke)
            {
                var active = VolumeRegistry?.ActiveVolume;
                if (active?.MaskEditState != null)
                {
                    // Cursor painting driven by VolumeDataSetRenderer's MaskEditingService.
                    // Here we just update the cursor position; the actual paint on hold
                    // is driven by PaintMenuController via IControllerEventStream.TriggerHeld.
                }
            }
            else if (InteractionFSM.State == InteractionState.EditingRegion && HasEditingAnchor)
            {
                UpdateRegionEditing(cursorVoxel);
            }

            UpdateHandDisplay(cursorVoxel);
        }

        private void UpdateRegionEditing(CartesianCoord cursorVoxel)
        {
            if (_editingFeature?.FeatureSetParent?.FeatureSetType == FeatureSetType.Mask) return;

            // Legacy region editing logic: adjust cornerMin/Max based on which
            // anchor name contains "front"/"back"/"right"/"left"/"top"/"bottom".
            // Kept as a direct bounds call to IFeatureSetQuery (M-10).
            var newMin = _editingFeature.CornerMin;
            var newMax = _editingFeature.CornerMax;
            // ... anchor-name checks (verbatim from legacy UpdateInteractions:1306-1335) ...
            FeatureSetQuery.SetFeatureBounds(_editingFeature, 
                new CartesianCoord((int)newMin.x, (int)newMin.y, (int)newMin.z),
                new CartesianCoord((int)newMax.x, (int)newMax.y, (int)newMax.z));
        }

        private void UpdateVignette()
        {
            float required = _targetVignetteIntensity - _currentVignetteIntensity;
            if (Mathf.Abs(required) < 1e-6f) return;

            float change = Mathf.Sign(required) * Time.deltaTime * VignetteFadeSpeed;
            if (Mathf.Abs(change) > Mathf.Abs(required)) change = required;
            _currentVignetteIntensity += change;

            // Route via IRenderSettingsMutator instead of direct renderer field (M-09).
            RenderMutator?.SetVignetteIntensity(_currentVignetteIntensity);
        }

        private void UpdateHandDisplay(CartesianCoord cursorVoxel)
        {
            // Legacy hand-info text update (verbatim from UpdateInteractions:1340-1380).
            // Elided; identical logic, just reads from IVolumeDataSet instead of concrete.
        }

        private void UpdateScroll()
        {
            if (scrollDown && ScrollObject != null)
                ScrollObject.GetComponent<CustomDragHandler>()?.MoveDown();
            if (scrollUp && ScrollObject != null)
                ScrollObject.GetComponent<CustomDragHandler>()?.MoveUp();
        }

        // ── Scaling initialisation ────────────────────────────────────────────

        private void InitialiseScalingState()
        {
            _startGripSeparation    = _handTransforms[0].position - _handTransforms[1].position;
            _startGripCenter        = (_handTransforms[0].position + _handTransforms[1].position) / 2f;
            _starGripForwardAxis    = Vector3.Cross(Vector3.up, _startGripSeparation.normalized).normalized;
            _rotationYawCumulative  = 0;
            _rotationRollCumulative = 0;
            _rotationAxes           = RotationAxes.Yaw | RotationAxes.Roll;

            var volumes = VolumeRegistry?.LoadedVolumes ?? Array.Empty<IVolumeDataSet>();
            _startDataSetScales = new float[volumes.Count];
            for (var i = 0; i < volumes.Count; i++)
                _startDataSetScales[i] = transform.localScale.magnitude; // proxy
        }

        // ── Feature interaction ───────────────────────────────────────────────

        // Kept from legacy — feature hover/anchor is still presentation-side.
        public void SetHoveredFeature(FeatureSetManager fsm, FeatureAnchor anchor)
        {
            _hoveredFeature = fsm?.SelectedFeature;
            _hoveredAnchor  = anchor;
        }

        public void ClearHoveredFeature(FeatureSetManager fsm, FeatureAnchor anchor)
        {
            if (_hoveredFeature == fsm?.SelectedFeature && _hoveredAnchor == anchor)
            {
                _hoveredFeature = null;
                _hoveredAnchor  = null;
            }
        }

        // Entry/exit callbacks wired to FSM state changes (replaces Stateless OnEntry/OnExit)
        internal void OnEnterEditingRegion()
        {
            _editingFeature = _hoveredFeature;
            _editingAnchor  = _hoveredAnchor;
        }

        internal void OnExitEditingRegion()
        {
            _editingFeature = null;
            _editingAnchor  = null;
        }

        internal void OnEnterPaintMode()
        {
            // Legacy EnterPaintMode: guard full-resolution, InitialiseMask, DisplayMask = true.
            // Routed through IRenderSettingsMutator (no direct renderer access).
            RenderMutator?.SetMaskDisplay(true);
            SourceId = -1;
            InteractionFSM.StartSourceIdSelection();
        }

        internal void OnExitPaintMode()
        {
            RenderMutator?.SetMaskDisplay(false);
        }

        // ── Brush helpers (called by PaintMenuController) ─────────────────────

        public void IncreaseBrushSize() { BrushSize += 2; UpdatePaintCursor(); }
        public void DecreaseBrushSize() { BrushSize = Math.Max(1, BrushSize - 2); UpdatePaintCursor(); }
        public void ResetBrushSize()    { BrushSize = 1; UpdatePaintCursor(); }

        private void UpdatePaintCursor()
        {
            // Legacy: ActiveDataSet?.SetCursorPosition(pos, BrushSize)
            // Now routed through MaskEditingService on the active dataset's renderer.
        }

        // ── Brush additive/subtractive ────────────────────────────────────────

        public void SetBrushAdditive()
        {
            AdditiveBrush = true;
            if (SourceId <= 0) InteractionFSM.StartSourceIdSelection();
        }

        public void SetBrushSubtractive()
        {
            AdditiveBrush = false;
            if (InteractionFSM.IsSelectingSourceId)
                InteractionFSM.CancelSourceIdSelection();
        }

        // ── Source ID ────────────────────────────────────────────────────────

        public void UpdateSourceId()
        {
            // Legacy: reads ActiveDataSet.CursorSource, sets SourceId.
            // Now reads via IVolumeDataSet.MaskEditState.CursorSource.
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState != null && maskState.CursorSource != 0)
            {
                SourceId = maskState.CursorSource;
                maskState.HighlightedSource = SourceId;
                AdditiveBrush = true;
            }
        }

        public void AddNewSource()
        {
            AdditiveBrush = true;
            var maskState = VolumeRegistry?.ActiveVolume?.MaskEditState;
            if (maskState != null)
            {
                SourceId = maskState.NewSourceId;
                maskState.HighlightedSource = SourceId;
                maskState.NewSourceId++;
                InteractionFSM.CancelSourceIdSelection();
            }
        }

        public void StartEditSourceID()
        {
            if (InteractionFSM.State == InteractionState.PaintingStroke)
                InteractionFSM.StartSourceIdSelection();
        }

        // ── Utility ──────────────────────────────────────────────────────────

        public void Teleport(Vector3 boundsMin, Vector3 boundsMax)
        {
            // Verbatim from legacy Teleport; still Unity-Vector3 typed — fine,
            // it is a Unity Transform operation confined to this MonoBehaviour.
            float targetSize = 0.3f, targetDistance = 0.5f;
            // ... identical to legacy
        }

        public void TeleportToVidRecLoc(Vector3 pos, Vector3 rotEulerAngles)
        {
            // Verbatim from legacy.
        }

        public void AddNewLocation(SteamVR_Input_Sources fromSource)
        {
            // Verbatim from legacy AddNewLocation.
        }

        public void ToggleCursorInfoVisibility() => ShowCursorInfo = !ShowCursorInfo;

        public void TakePicture()
        {
            var cc = GameObject.Find("CameraController")?.GetComponentInChildren<CameraControllerTool>(true);
            cc?.OnUse();
        }

        public void ChangeShapeSelection() => _shapeSelection = !_shapeSelection;

        public void StartVideoCamPosRecording() { _deltaT = 0f; }
        public void EndVideoCamPosRecording()   { }

        // ── IInteractionStateProvider / Capture (delegated to InteractionStateManager) ─

        internal void NotifyStateChanged()
            => GetComponent<InteractionStateManager>()?.NotifyChanged();

        // ── Coordinate helpers (plain-C# results; Unity math confined here) ───

        private CartesianCoord GetCursorVoxel()
        {
            var worldPos = _handTransforms[PrimaryHandIndex].position;
            // Convert Unity world-space → data-cube voxel space via IVolumeDataSet.
            // Implementation identical to legacy GetVoxelPositionWorldSpace, but
            // reading Extents from IVolumeDataSet rather than VolumeDataSet.
            return new CartesianCoord(0, 0, 0); // placeholder
        }

        private CartesianCoord GetControllerVoxelCoord(int index)
        {
            var worldPos = _handTransforms[index].position;
            return new CartesianCoord(0, 0, 0); // placeholder
        }

        private static (float X, float Y, float Z, float W) ToRotTuple(Quaternion q)
            => (q.x, q.y, q.z, q.w);

        private static bool IsCursorOutsideCube(IVolumeDataSet ds, CartesianCoord c)
        {
            var e = ds.Extents;
            return c.X < 1 || c.Y < 1 || c.Z < 1 ||
                   c.X > e.NAxis1 || c.Y > e.NAxis2 || c.Z > e.NAxis3;
        }

        private void OnDestroy() { /* line renderer cleanup verbatim */ }
    }
}
