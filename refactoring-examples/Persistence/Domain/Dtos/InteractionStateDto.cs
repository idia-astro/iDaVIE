namespace iDaVIE.Persistence.Domain.Dtos;

/// <summary>
/// Persistent state for the VR interaction sub-system.
/// Source: VolumeInputController public fields + GetLocomotionStateString().
/// NOT persisted: SteamVR controller refs, grip positions, hovered/editing feature refs.
///
/// Two scopes:
///   User preferences (survive crashes, restored even on blank session):
///     PrimaryHand, InPlaceScaling, ScalingEnabled, VignetteFadeSpeed, RotationAxisCutoff.
///   Session state (restored within same session):
///     ActiveInteractionMode, LocomotionState, BrushSize, SourceId, AdditiveBrush.
/// </summary>
public class InteractionStateDto
{
    /// <summary>
    /// VolumeInputController.InteractionState enum as string (e.g. "IdleSelecting", "IdlePainting").
    /// WorkspaceValidator resets "VideoCamPosRecording" → "IdleSelecting" on load —
    /// recording mode is transient and must never survive across sessions.
    /// </summary>
    [PersistField] public string? ActiveInteractionMode { get; set; } = "IdleSelecting";

    /// <summary>
    /// VolumeInputController.LocomotionState (private enum) as string.
    /// Captured via GetLocomotionStateString(); default on restore is "Idle".
    /// </summary>
    [PersistField] public string? LocomotionState { get; set; } = "Idle";

    // ── Session state ────────────────────────────────────────────────────────
    [PersistField] public bool AdditiveBrush { get; set; } = true;
    [PersistField] public int BrushSize { get; set; } = 1;
    [PersistField] public short SourceId { get; set; } = -1;

    // ── User preferences ─────────────────────────────────────────────────────
    /// <summary>"LeftHand" or "RightHand" — replaces SteamVR_Input_Sources enum.</summary>
    [PersistField] public string? PrimaryHand { get; set; } = "RightHand";
    [PersistField] public bool InPlaceScaling { get; set; } = true;
    [PersistField] public bool ScalingEnabled { get; set; } = true;

    /// <summary>Range [0.1, 5.0] — clamped by WorkspaceValidator on load.</summary>
    [PersistField] public float VignetteFadeSpeed { get; set; } = 2.0f;

    /// <summary>Must be &gt; 0 to function as angle threshold (VolumeInputController line 918).</summary>
    [PersistField] public float RotationAxisCutoff { get; set; } = 5.0f;
}
