// SPDX-License-Identifier: LGPL-3.0-or-later
// ShapeGestureFSM — plain-C# state machine extracted from the legacy
// Assets/Scripts/Shapes/ShapesManager.cs (481 LOC). Per global_model.md §1 ST4
// "Shape gesture state ... converts to mask edits via IMaskMutationService"
// (M-23). The drawing of polygons / surfaces stays in ST3 / Unity ACL via
// IMaskMutationService.PaintPolygon; this FSM owns the gesture lifecycle only.

using iDaVIE.Data;                         // BrushPaintMode
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Interaction
{
    public enum ShapeGesturePhase { Idle, Anchored, Resizing, Committed }

    internal sealed class ShapeGestureFSM
    {
        public ShapeGesturePhase Phase  { get; private set; }
        public ShapeGestureState State  { get; } = new();

        public void BeginShape(ShapeType shape, CartesianCoord anchor) => throw new System.NotImplementedException();
        public void UpdateExtent(CartesianCoord extent)                => throw new System.NotImplementedException();
        public void Cancel()                                           => throw new System.NotImplementedException();

        /// <summary>Commits the shape by rasterising it inside the shape's bounding
        /// box and invoking the supplied mask-mutation callback.</summary>
        public void Commit(System.Action<ShapeGestureState, BrushPaintMode> commitCallback,
                           BrushPaintMode paintMode)
            => throw new System.NotImplementedException();
    }
}
