// SPDX-License-Identifier: LGPL-3.0-or-later
// LocomotionFSM — pure-C# locomotion state machine.
//
// Legacy: implicit flag-based logic spread across VolumeInputController
//   (lines ~200-400): _locomotionState enum, _currentGripPositions[],
//   _startGripSeparation, _startGripCenter, _startDataSetScales[],
//   StateTransitionIdleToMoving, StateTransitionMovingToScaling,
//   StateTransitionScalingToMoving, StateTransitionMovingToIdle,
//   UpdateMoving, UpdateScaling.
//
// Refactor delta:
//   - State pattern (GoF). Each state (IdleState, MovingState,
//     ScalingRotatingState) is a sealed class implementing ILocomotionState.
//     VolumeInputController.Update calls GetTranslationDelta / GetScalingData
//     each frame; the results are Unity-typed only inside the MonoBehaviour ACL.
//   - Pure C# — no UnityEngine dependency (MOD-02, REU-02).
//     VolumeInputController converts Unity Vector3/Quaternion → CartesianCoord
//     before calling into this class, and converts the delta tuple back to
//     Vector3/Quaternion when applying the Unity Transform.
//   - SOLID impact:
//       SRP  — one class per state, one concern per class.
//       OCP  — adding HoverState = one new file, zero existing files change.
//       LSP  — all states substitutable behind ILocomotionState.
//       DIP  — LocomotionFSM depends on ILocomotionState abstraction.
//       GRASP Polymorphism — dispatch is polymorphic; no conditional branching
//             on state in the host.

using System;
using iDaVIE.Kernel.Contracts.Types;  // CartesianCoord (ST1, M-21)

namespace iDaVIE.Interaction
{
    // ── State interface ───────────────────────────────────────────────────────

    /// <summary>
    /// One method per locomotion input event. Each state returns the successor
    /// state; the FSM calls Transition() only when the returned reference differs.
    /// Pure C# — no UnityEngine types. ≤7 members (ISP, MOD-05).
    /// </summary>
    internal interface ILocomotionState
    {
        LocomotionState Kind { get; }

        ILocomotionState OnGripPressed(
            int controllerIndex, CartesianCoord pos,
            float currentScale, (float X, float Y, float Z, float W) currentRot,
            bool volumeLoaded);

        ILocomotionState OnGripReleased(
            int controllerIndex,
            CartesianCoord primaryPos, CartesianCoord secondaryPos,
            float currentScale, (float X, float Y, float Z, float W) currentRot);

        ILocomotionState OnTeleport(CartesianCoord target, bool volumeLoaded);

        CartesianCoord GetTranslationDelta(CartesianCoord primaryPos, LocomotionConfig cfg);
    }

    // ── Concrete states ───────────────────────────────────────────────────────

    internal sealed class IdleState : ILocomotionState
    {
        public LocomotionState Kind => LocomotionState.Idle;

        public ILocomotionState OnGripPressed(
            int idx, CartesianCoord pos,
            float scale, (float X, float Y, float Z, float W) rot,
            bool loaded)
        {
            if (!loaded) return this;
            return new MovingState(idx, pos);
        }

        public ILocomotionState OnGripReleased(
            int idx, CartesianCoord primary, CartesianCoord secondary,
            float scale, (float X, float Y, float Z, float W) rot) => this;

        public ILocomotionState OnTeleport(CartesianCoord target, bool loaded)
            => loaded ? new IdleState() : this;

        public CartesianCoord GetTranslationDelta(CartesianCoord _, LocomotionConfig __)
            => default;
    }

    internal sealed class MovingState : ILocomotionState
    {
        private readonly int            _primaryIndex;
        private readonly CartesianCoord _anchor;

        public MovingState(int primaryIndex, CartesianCoord anchor)
        {
            _primaryIndex = primaryIndex;
            _anchor       = anchor;
        }

        public LocomotionState Kind => LocomotionState.Moving;

        public ILocomotionState OnGripPressed(
            int idx, CartesianCoord pos,
            float scale, (float X, float Y, float Z, float W) rot,
            bool loaded)
        {
            if (idx == _primaryIndex) return this; // same hand, no-op
            return new ScalingRotatingState(_primaryIndex, _anchor, idx, pos, scale, rot);
        }

        public ILocomotionState OnGripReleased(
            int idx, CartesianCoord primary, CartesianCoord secondary,
            float scale, (float X, float Y, float Z, float W) rot)
            => idx == _primaryIndex ? (ILocomotionState)new IdleState() : this;

        public ILocomotionState OnTeleport(CartesianCoord target, bool loaded)
            => loaded ? new IdleState() : this;

        public CartesianCoord GetTranslationDelta(CartesianCoord currentPos, LocomotionConfig cfg)
            => new CartesianCoord(
                (int)((currentPos.X - _anchor.X) * cfg.TranslationSensitivity),
                (int)((currentPos.Y - _anchor.Y) * cfg.TranslationSensitivity),
                (int)((currentPos.Z - _anchor.Z) * cfg.TranslationSensitivity));
    }

    internal sealed class ScalingRotatingState : ILocomotionState
    {
        private readonly int                              _primaryIndex;
        private readonly CartesianCoord                   _primaryAnchor;
        private readonly int                              _secondaryIndex;
        private readonly CartesianCoord                   _secondaryAnchor;
        private readonly float                            _refScale;
        private readonly (float X, float Y, float Z, float W) _refRotation;

        public ScalingRotatingState(
            int pi, CartesianCoord pa, int si, CartesianCoord sa,
            float scale, (float X, float Y, float Z, float W) rot)
        {
            _primaryIndex   = pi; _primaryAnchor   = pa;
            _secondaryIndex = si; _secondaryAnchor = sa;
            _refScale       = scale; _refRotation   = rot;
        }

        public LocomotionState Kind => LocomotionState.ScalingRotating;

        public ILocomotionState OnGripPressed(
            int idx, CartesianCoord pos,
            float scale, (float X, float Y, float Z, float W) rot,
            bool loaded) => this; // already two-handed

        public ILocomotionState OnGripReleased(
            int idx, CartesianCoord primary, CartesianCoord secondary,
            float scale, (float X, float Y, float Z, float W) rot)
        {
            if (idx == _secondaryIndex)
                return new MovingState(_primaryIndex, primary); // one grip remains
            if (idx == _primaryIndex)
                return new IdleState();
            return this;
        }

        public ILocomotionState OnTeleport(CartesianCoord target, bool loaded)
            => loaded ? new IdleState() : this;

        // Scale/rotation is computed by the Unity ACL using ref scale/rotation
        // captured on entry. Translation is zero during two-hand manipulation.
        public CartesianCoord GetTranslationDelta(CartesianCoord _, LocomotionConfig __)
            => default;

        /// <summary>Scale factor relative to state entry for the Unity ACL.</summary>
        public float GetScaleFactor(float currentScale)
            => _refScale > 0f ? currentScale / _refScale : 1f;

        public (float X, float Y, float Z, float W) RefRotation => _refRotation;
    }

    // ── LocomotionConfig ─────────────────────────────────────────────────────

    /// <summary>Tuning parameters loaded from Config at startup; not mutated at runtime.</summary>
    public readonly record struct LocomotionConfig
    {
        public float TranslationSensitivity { get; init; }
        public float RotationSensitivity    { get; init; }
        public float ScaleSensitivity       { get; init; }
        public float MinScale               { get; init; }
        public float MaxScale               { get; init; }
    }

    // ── FSM orchestrator ─────────────────────────────────────────────────────

    /// <summary>
    /// Pure-C# locomotion state machine. VolumeInputController is the sole driver.
    /// Replaces the flag-based locomotion logic in the legacy VolumeInputController
    /// (~200 lines). No UnityEngine dependency (MOD-02, REU-02).
    ///
    /// TransformCommitted carries (translation delta, rotation tuple, scale multiplier)
    /// as plain C# types; the caller (VolumeInputController) applies these to the
    /// Unity Transform — the only site where Vector3/Quaternion appear.
    /// </summary>
    public sealed class LocomotionFSM
    {
        private ILocomotionState _state = new IdleState();

        public LocomotionState State => _state.Kind;

        public event Action<LocomotionState>? StateChanged;

        /// <summary>
        /// Fired when a locomotion state exits with a committed position or transform change.
        /// Tuple: (translationDelta, rotation(xyzw), scaleFactor).
        /// </summary>
        public event Action<CartesianCoord, (float X, float Y, float Z, float W), float>? TransformCommitted;

        public void OnGripPressed(
            int idx, CartesianCoord pos,
            float currentScale, (float X, float Y, float Z, float W) currentRot,
            bool volumeLoaded)
        {
            var next = _state.OnGripPressed(idx, pos, currentScale, currentRot, volumeLoaded);
            Transition(next);
        }

        public void OnGripReleased(
            int idx,
            CartesianCoord primaryPos, CartesianCoord secondaryPos,
            float currentScale, (float X, float Y, float Z, float W) currentRot)
        {
            var next = _state.OnGripReleased(idx, primaryPos, secondaryPos, currentScale, currentRot);

            // Commit the transform on any transition to Idle.
            if (next.Kind == LocomotionState.Idle && _state.Kind != LocomotionState.Idle)
            {
                var delta = _state.GetTranslationDelta(primaryPos, default);
                TransformCommitted?.Invoke(delta, currentRot, currentScale);
            }

            Transition(next);
        }

        public void OnTeleport(CartesianCoord target, bool volumeLoaded)
        {
            var next = _state.OnTeleport(target, volumeLoaded);
            if (next != _state)
                TransformCommitted?.Invoke(target, (0, 0, 0, 1), 1f);
            Transition(next);
        }

        /// <summary>Called each Update tick while in Moving state to get the live delta.</summary>
        public CartesianCoord GetCurrentTranslationDelta(CartesianCoord primaryPos, LocomotionConfig cfg)
            => _state.GetTranslationDelta(primaryPos, cfg);

        private void Transition(ILocomotionState next)
        {
            if (next == _state || next.Kind == _state.Kind) return;
            _state = next;
            StateChanged?.Invoke(next.Kind);
        }
    }
}
