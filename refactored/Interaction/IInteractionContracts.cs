// SPDX-License-Identifier: LGPL-3.0-or-later
// IInteractionContracts.cs — cross-team interfaces owned by ST4.
//
// Canonical signatures match shared_interfaces.md §3.4 and global_model.md §3.4.
// Consumed by:
//   • IControllerEventStream / IVoiceCommandStream — ST4 internal (ACL only)
//   • IInteractionStateProvider — ST6 (CanvassDesktop subscribes to read FSM state)
//   • IInteractionStateCapture  — ST7 (WorkspaceSerializer calls Capture/Restore)
//
// M-12: IDesktopMediationBoundary dropped; render-side events covered by
//        IRenderSettings.SettingsChanged (ST3); interaction-side by IInteractionStateProvider.
// M-24: IControllerEventStream and IVoiceCommandStream owned here.

using System;
using System.Collections.Generic;
using iDaVIE.Data;                    // BrushPaintMode (ST2-owned; single source of truth)
using iDaVIE.Kernel.Contracts.Types;  // CartesianCoord (ST1 shared types, M-21)

namespace iDaVIE.Interaction
{
    // ── Enums ────────────────────────────────────────────────────────────────

    public enum LocomotionState  { Idle, Moving, ScalingRotating }
    public enum InteractionState { Idle, ParameterEditing, CreatingSelection, EditingRegion, SourceEditing, PaintingStroke }
    public enum QuickMenuPanel   { None, QuickMenu, PaintMenu }
    public enum ShapeType        { Cube, Sphere, Cylinder, Cuboid }

    // BrushPaintMode is iDaVIE.Data.BrushPaintMode (above using). It was previously
    // duplicated here; consolidated so VolumeCommandController.CommitShapeGesture can
    // build a StrokePaintConfig directly from BrushConfig without a per-call mapper.
    // ST4 already depends on iDaVIE.Data for IMaskMutationService, so no new edge.

    // NOTE: shared_interfaces.md §4.3 defines a minimal 8-value VoiceCommand for the
    // cross-team boundary (NextSource, PreviousSource, Confirm, Cancel, Undo,
    // StartPainting, StopPainting, EmergencyStop). ST4 intentionally extends this to
    // cover all domain-specific commands dispatched by VolumeCommandController.
    // IVoiceCommandStream (and the adapter) fire from this full set; the 8-value enum
    // in shared_interfaces.md describes only the minimum external consumers must handle.
    public enum VoiceCommand
    {
        EditThresholdMin, EditThresholdMax, EditZAxis, SaveZAxis, ResetZAxis,
        SaveThreshold, ResetThreshold, ResetTransform,
        ColormapPlasma, ColormapRainbow, ColormapMagma, ColormapInferno,
        ColormapViridis, ColormapCubeHelix, ColormapTurbo,
        CropSelection, Teleport, ResetCropSelection,
        MaskDisabled, MaskEnabled, MaskInverted, MaskIsolated,
        ProjectionMaximum, ProjectionAverage, SamplingModeMaximum, SamplingModeAverage,
        PaintMode, ExitPaintMode, BrushAdd, BrushErase,
        ShowMaskOutline, HideMaskOutline, TakePicture, CursorInfo,
        LinearScale, LogScale, SqrtScale,
        AddNewSource, SetSourceId, AddToList, Undo, Redo, SaveSubCube,
        NextSourceList, PreviousSourceList,
        ExportVideoScript, EnterVideoMode, RecordVideoPosition
    }

    public enum VoiceActivationMode { Continuous, PushToTalk }

    // ── Value types ──────────────────────────────────────────────────────────

    /// <summary>
    /// Plain-C# brush configuration. Renamed from BrushState to distinguish from
    /// ST2-owned brush-stroke history (M-04). Persisted in InteractionStateDto.
    /// Field names/types match shared_interfaces.md §4.1 (resolution line 26).
    /// </summary>
    public readonly record struct BrushConfig
    {
        public float  Radius    { get; init; }
        public bool   Additive  { get; init; }
        public int    SourceId  { get; init; }
        public BrushPaintMode PaintMode { get; init; }
    }

    /// <summary>
    /// In-progress drag-gesture AABB. Renamed from SelectionState per M-10.
    /// On completion, min/max passed directly to IFeatureSetQuery.SetSelectionBoxBounds.
    /// </summary>
    public sealed class DragGestureState
    {
        public CartesianCoord Origin     { get; set; }
        public CartesianCoord CurrentMin { get; set; }
        public CartesianCoord CurrentMax { get; set; }
        public bool           IsActive   { get; set; }
    }

    /// <summary>Shape gesture state (Assets/Scripts/Shapes/). Assigned to ST4 per M-23.</summary>
    public sealed class ShapeGestureState
    {
        public ShapeType      ActiveShape { get; set; }
        public CartesianCoord Anchor      { get; set; }
        public CartesianCoord Extent      { get; set; }
        public bool           IsActive    { get; set; }
    }

    /// <summary>
    /// Plain-C# config for voice adapter. Replaces UnityEngine.Windows.Speech.ConfidenceLevel
    /// to avoid Unity transitive dependency in domain layer (MOD-02).
    /// </summary>
    public readonly record struct SpeechRecognitionConfig
    {
        public string?              Locale              { get; init; }  // BCP-47, e.g. "en-ZA"
        public VoiceActivationMode  ActivationMode      { get; init; }
        public float                ConfidenceThreshold { get; init; }  // [0,1]
    }

    /// <summary>
    /// M-16 persistence DTO. All fields plain C#; no UnityEngine types.
    /// Field names match shared_interfaces.md §4.4 (BrushRadius: float, BrushSourceId: int).
    /// </summary>
    public sealed class InteractionStateDto
    {
        public string  CurrentLocomotionState   { get; set; } = nameof(LocomotionState.Idle);
        public string  CurrentInteractionState  { get; set; } = nameof(InteractionState.Idle);
        public float   BrushRadius              { get; set; }
        public bool    BrushAdditive            { get; set; }
        public int     BrushSourceId            { get; set; }
        public string  BrushPaintMode           { get; set; } = nameof(iDaVIE.Data.BrushPaintMode.Add);
        public string? ActiveVoiceLocale        { get; set; }
        public bool    PushToTalkEnabled        { get; set; }
        public float   VoiceConfidenceThreshold { get; set; }
        public string  ActiveMenuPanel          { get; set; } = nameof(QuickMenuPanel.None);
        public int     SchemaVersion            { get; set; } = 1;
        // Enum-string fields above are parsed on Restore via the shared
        // iDaVIE.Kernel.Contracts.EnumString.TryParseOrDefault (shared_interfaces.md §1.9).
    }

    // ── ISP sub-interfaces for IControllerEventStream ────────────────────────

    /// <summary>Grip button events. ≤7 members (MOD-05).</summary>
    public interface IGripInput
    {
        event Action<int> GripPressed;
        event Action<int> GripReleased;
        bool IsAnyGripHeld   { get; }
        bool IsBothGripsHeld { get; }
    }

    /// <summary>Trigger and pointer events. ≤7 members (MOD-05).</summary>
    public interface IPointerInput
    {
        event Action<int>                 TriggerPressed;
        event Action<int>                 TriggerReleased;
        event Action<int, float>          TriggerHeld;
        event Action<int, CartesianCoord> PointerEntered;
        event Action<int>                 PointerExited;
    }

    /// <summary>Haptic output. ≤7 members (MOD-05).</summary>
    public interface IHapticsOutput
    {
        void Pulse(int controllerIndex, float durationSeconds, float amplitude);
    }

    /// <summary>Thumbstick axis and click. ≤7 members (MOD-05).</summary>
    public interface IThumbstickInput
    {
        event Action<int, float, float> ThumbstickMoved;
        event Action<int>               ThumbstickClicked;
    }

    /// <summary>Teleport gesture completion. ≤7 members (MOD-05).</summary>
    public interface ITeleportInput
    {
        event Action<CartesianCoord> TeleportRequested;
    }

    // ── Cross-team interfaces owned by ST4 ───────────────────────────────────

    /// <summary>
    /// Full platform-neutral controller event stream (M-24).
    /// Composed of 5 ISP sub-interfaces. Only SteamVRInputAdapter (ST4 ACL)
    /// implements this. No SteamVR types cross this boundary (MOD-01).
    /// </summary>
    public interface IControllerEventStream
        : IGripInput, IPointerInput, IHapticsOutput, IThumbstickInput, ITeleportInput { }

    /// <summary>
    /// Platform-neutral voice command stream (M-24). Only VoiceRecogniserAdapter (ST4 ACL)
    /// implements this. No Windows.Speech types cross this boundary (MOD-04). ≤7 members.
    /// </summary>
    public interface IVoiceCommandStream
    {
        event Action<VoiceCommand> CommandRecognised;
        bool                       IsListening { get; }
        SpeechRecognitionConfig    Config      { get; }
        void StartListening();
        void StopListening();
    }

    /// <summary>
    /// Read-only FSM view published to ST6 (M-12, M-13).
    /// Replaces the dropped IDesktopMediationBoundary.
    /// Render-side notifications now come from IRenderSettings.SettingsChanged (ST3).
    /// ≤7 members (MOD-05).
    /// </summary>
    public interface IInteractionStateProvider
    {
        LocomotionState  LocomotionState  { get; }
        InteractionState InteractionState { get; }
        bool             IsPaintModeActive { get; }
        bool             IsQuickMenuOpen   { get; }
        QuickMenuPanel   ActiveMenuPanel   { get; }
        event Action?    InteractionStateChanged;
    }

    /// <summary>
    /// ST4 persistence port — M-16 uniform I&lt;Team&gt;Capture pattern.
    /// Consumed by ST7 only.
    /// </summary>
    public interface IInteractionStateCapture
    {
        InteractionStateDto Capture();
        void Restore(InteractionStateDto state);
    }
}
