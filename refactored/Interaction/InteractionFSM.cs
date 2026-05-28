// SPDX-License-Identifier: LGPL-3.0-or-later
// InteractionFSM — pure-C# interaction state machine.
//
// Legacy: StateMachine<InteractionState, InteractionEvents> (Stateless library)
//   built in VolumeInputController.CreateInteractionStateMachine (~80 lines),
//   plus flag-based guard conditions and side-effect entry/exit callbacks
//   (StartSelection, EndSelection, StartRegionEditing, EndRegionEditing,
//   EnterPaintMode, ExitPaintMode) scattered across ~150 more lines.
//
// Refactor delta:
//   - Replaces the Stateless library with plain C#. The Stateless DSL is
//     expressive but requires a runtime dependency and makes the transitions
//     opaque to static analysis. Each transition is now a named method on the
//     FSM, directly testable without Unity (TST-01).
//   - DragGestureState replaces SelectionState (M-10). On gesture completion,
//     SetSelectionBoxBounds is called directly on IFeatureSetQuery — the
//     IFeatureConsumer event interface is dropped (M-10).
//   - ShapeGestureState handles Assets/Scripts/Shapes/ geometry (M-23).
//   - PaintingStroke replaces the legacy IdlePainting + Painting two-state
//     design. Paint mode is armed by OnPaintModeToggled; trigger-held accumulates
//     samples; trigger-released commits to VolumeCommandController.
//   - No UnityEngine dependency (MOD-02, REU-02).

using System;
using iDaVIE.Features;               // IFeatureSetQuery (ST5, M-10 direct call)
using iDaVIE.Kernel.Contracts.Types;  // CartesianCoord

namespace iDaVIE.Interaction
{
    /// <summary>
    /// Pure-C# interaction state machine.
    /// Driven entirely by VolumeInputController; no Unity lifecycle inside.
    /// All mask mutations are forwarded to VolumeCommandController, which calls
    /// IMaskMutationService.ApplyBrush — no PaintPolygon from ST4 (M-14).
    /// </summary>
    public sealed class InteractionFSM
    {
        // ── State ─────────────────────────────────────────────────────────────

        public InteractionState State { get; private set; } = InteractionState.Idle;

        // Guard flags kept in sync by VolumeInputController each Update.
        public bool VolumeLoaded    { get; set; }
        public bool MaskLayerActive { get; set; }
        public bool SourceCatLoaded { get; set; }

        // Sub-state value objects.
        public readonly DragGestureState  DragGesture  = new();
        public readonly ShapeGestureState ShapeGesture = new();

        public event Action<InteractionState>? StateChanged;

        // ── Trigger input ─────────────────────────────────────────────────────

        public void OnTriggerPressed(int controllerIndex, CartesianCoord pointerPos)
        {
            if (State == InteractionState.Idle && VolumeLoaded)
                Transition(InteractionState.ParameterEditing);
        }

        /// <summary>
        /// Called on trigger release. Delegates are wired from VolumeCommandController so the
        /// FSM stays free of command-processor references (constructor injection via caller).
        /// M-10: SetSelectionBoxBounds called directly on IFeatureSetQuery.
        /// </summary>
        public void OnTriggerReleased(
            IFeatureSetQuery featureSetQuery,
            Action commitStroke,
            Action commitParameterEdit)
        {
            switch (State)
            {
                case InteractionState.ParameterEditing:
                    commitParameterEdit();
                    Transition(InteractionState.Idle);
                    break;

                case InteractionState.CreatingSelection when DragGesture.IsActive:
                    DragGesture.IsActive = false;
                    // M-10: direct call, not an event — IFeatureConsumer dropped.
                    featureSetQuery.SetSelectionBoxBounds(DragGesture.CurrentMin, DragGesture.CurrentMax);
                    Transition(InteractionState.Idle);
                    break;

                case InteractionState.PaintingStroke:
                    // Stroke accumulation happens in PaintMenuController; it calls
                    // VolumeCommandController.CommitStroke directly on trigger release.
                    // This FSM just stays in PaintingStroke (paint mode remains armed).
                    commitStroke();
                    break;
            }
        }

        // ── Mode toggles ──────────────────────────────────────────────────────

        public void OnPaintModeToggled()
        {
            if (State == InteractionState.Idle && MaskLayerActive)
                Transition(InteractionState.PaintingStroke);
            else if (State == InteractionState.PaintingStroke)
                Transition(InteractionState.Idle);
        }

        public void OnSelectionModeToggled()
        {
            if (State == InteractionState.Idle && VolumeLoaded)
            {
                DragGesture.IsActive = true;
                Transition(InteractionState.CreatingSelection);
            }
            else if (State == InteractionState.CreatingSelection)
            {
                DragGesture.IsActive = false;
                Transition(InteractionState.Idle);
            }
        }

        // ── Feature / source selection ────────────────────────────────────────

        public void OnFeatureSelected()
        {
            if (State == InteractionState.Idle)
                Transition(InteractionState.EditingRegion);
        }

        public void OnSourceSelected()
        {
            if (State == InteractionState.Idle && SourceCatLoaded)
                Transition(InteractionState.SourceEditing);
        }

        // ── Edit commit / cancel ──────────────────────────────────────────────

        public void OnEditCommitted()
        {
            if (State == InteractionState.EditingRegion || State == InteractionState.SourceEditing)
                Transition(InteractionState.Idle);
        }

        public void OnEditCancelled() => OnEditCommitted();

        // ── Voice: emergency stop ─────────────────────────────────────────────

        public void OnEmergencyStop()
        {
            if (State != InteractionState.Idle)
                Transition(InteractionState.Idle);
        }

        // ── Per-frame drag update ─────────────────────────────────────────────

        /// <summary>
        /// Called each Update tick while in CreatingSelection.
        /// Updates the in-progress AABB from the current pointer position in voxel space.
        /// </summary>
        public void UpdateDragGesture(CartesianCoord currentPointerPos)
        {
            if (State != InteractionState.CreatingSelection || !DragGesture.IsActive) return;

            DragGesture.CurrentMin = new CartesianCoord(
                Math.Min(DragGesture.Origin.X, currentPointerPos.X),
                Math.Min(DragGesture.Origin.Y, currentPointerPos.Y),
                Math.Min(DragGesture.Origin.Z, currentPointerPos.Z));

            DragGesture.CurrentMax = new CartesianCoord(
                Math.Max(DragGesture.Origin.X, currentPointerPos.X),
                Math.Max(DragGesture.Origin.Y, currentPointerPos.Y),
                Math.Max(DragGesture.Origin.Z, currentPointerPos.Z));
        }

        // ── Source-ID editing (legacy EditingSourceId state) ─────────────────

        /// <summary>
        /// Begins source-ID selection within paint mode (replaces legacy
        /// InteractionEvents.StartEditSource / EndEditSource / CancelEditSource).
        /// In the refactored design this is a sub-mode of PaintingStroke rather than
        /// a separate top-level state, keeping the public state count at 6.
        /// </summary>
        public bool IsSelectingSourceId { get; private set; }

        public void StartSourceIdSelection()
        {
            if (State == InteractionState.PaintingStroke)
                IsSelectingSourceId = true;
        }

        public void EndSourceIdSelection()   => IsSelectingSourceId = false;
        public void CancelSourceIdSelection() => IsSelectingSourceId = false;

        // ── Private ───────────────────────────────────────────────────────────

        private void Transition(InteractionState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
