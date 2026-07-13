using UnityEngine;

namespace Interaction.Interfaces
{
    /// <summary>
    /// Unified gaze contract for the iDaVIE interaction system.
    /// Serves two consumers:
    ///   - VolumeInputController uses GazeOrigin, GazeDirection, GazeRotation
    ///     for teleport positioning and head-position recording.
    ///   - VolumeDataSetRenderer (Sub-team 13) uses GazeFocusPoint, IsTracking,
    ///     GazeConfidence and GazeDirection for foveated volume rendering.
    /// Implementations may use hardware eye tracking or head-direction approximation.
    /// Consumers depend on this interface only — never on a concrete implementation.
    /// </summary>
    public interface IGaze
    {
        /// <summary>
        /// World-space origin of the gaze ray (eye or camera position).
        /// Used by VolumeInputController for teleport and head-position recording.
        /// </summary>
        Vector3 GazeOrigin { get; }

        /// <summary>
        /// Normalised world-space direction the user is looking.
        /// Used by both VolumeInputController and the renderer.
        /// </summary>
        Vector3 GazeDirection { get; }

        /// <summary>
        /// World-space rotation of the gaze source.
        /// Used by VolumeInputController to orient the data cube on teleport
        /// and to record camera rotation for video export.
        /// </summary>
        Quaternion GazeRotation { get; }

        /// <summary>
        /// Gaze focus in normalised screen coordinates [0, 1].
        /// (0.5, 0.5) is screen centre.
        /// Used by the renderer as the foveation centre for the ray-marching shader.
        /// Head-direction implementations always return (0.5, 0.5).
        /// Eye-tracking implementations return the actual projected screen position.
        /// </summary>
        Vector2 GazeFocusPoint { get; }

        /// <summary>
        /// Confidence 0.0 to 1.0.
        /// 1.0 = hardware eye tracking active.
        /// 0.5 = head-direction approximation.
        /// 0.0 = no gaze data available.
        /// Used by the renderer to scale foveation aggressiveness.
        /// </summary>
        float GazeConfidence { get; }

        /// <summary>
        /// True when gaze data is valid and being updated this frame.
        /// When false the renderer falls back to uniform sampling (screen centre).
        /// When false VolumeInputController teleport uses Camera.main as fallback.
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        /// World-space fixation point — gaze ray projected to a reference distance.
        /// Convenience property for consumers that need a world-space target point.
        /// </summary>
        Vector3 GazeFixationPoint { get; }
    }
}