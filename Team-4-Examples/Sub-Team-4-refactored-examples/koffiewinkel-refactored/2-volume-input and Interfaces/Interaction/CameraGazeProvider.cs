using Interaction.Interfaces;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// Implements IGaze using Camera.main (HMD head direction).
    /// Works with Unity 5, Unity 6, SteamVR, OpenXR, or any VR SDK.
    ///
    /// GazeConfidence is fixed at 0.5 to indicate head-direction approximation —
    /// the user is assumed to be looking at the centre of wherever the headset
    /// is pointed, which is not true eye tracking.
    ///
    /// GazeFocusPoint always returns (0.5, 0.5) — screen centre — because with
    /// head direction the gaze is always at the centre of the camera frustum.
    /// UnityXREyeGazeProvider (Unity 6) returns the actual projected eye position.
    ///
    /// Replace this component with UnityXREyeGazeProvider when migrating to
    /// Unity 6 with hardware eye tracking. No other code changes required.
    /// </summary>
    public sealed class CameraGazeProvider : MonoBehaviour, IGaze
    {
        [SerializeField]
        [Tooltip("Distance in metres to project the world-space fixation point.")]
        private float _fixationDistance = 3.0f;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        /// <inheritdoc/>
        public Vector3 GazeOrigin
            => _camera != null ? _camera.transform.position : Vector3.zero;

        /// <inheritdoc/>
        public Vector3 GazeDirection
            => _camera != null ? _camera.transform.forward.normalized : Vector3.forward;

        /// <inheritdoc/>
        public Quaternion GazeRotation
            => _camera != null ? _camera.transform.rotation : Quaternion.identity;

        /// <inheritdoc/>
        /// <remarks>
        /// Head direction always maps to screen centre (0.5, 0.5).
        /// UnityXREyeGazeProvider computes the real screen-space position
        /// from hardware eye tracking data.
        /// </remarks>
        public Vector2 GazeFocusPoint
            => new Vector2(0.5f, 0.5f);

        /// <inheritdoc/>
        /// <remarks>0.5 indicates head-direction approximation, not true eye tracking.</remarks>
        public float GazeConfidence
            => _camera != null ? 0.5f : 0.0f;

        /// <inheritdoc/>
        public bool IsTracking
            => _camera != null;

        /// <inheritdoc/>
        public Vector3 GazeFixationPoint
            => GazeOrigin + GazeDirection * _fixationDistance;
    }
}