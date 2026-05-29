// SPDX-License-Identifier: LGPL-3.0-or-later
// MaskEditService — ST2 application service. Realises IMaskMutationService (ST2,
// External/ICoordinateTransformer.cs), IBrushStrokeHistory (ST2), IMaskStateCapture
// (ST2 — persistence port), and IMaskEditState (ST1 sub-port held by IVolumeDataSet).
// M-04, M-14.
//
// Absorbs the mask voxel editing, brush-stroke undo/redo, and mask file I/O
// responsibilities from legacy VolumeData/VolumeDataSet.cs (~550-700 LOC).

using System.Collections.Generic;
using System.Numerics;
using iDaVIE.Data.Contracts;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Rendering.Contracts;          // MaskMode

namespace iDaVIE.Data
{
    internal sealed class MaskEditService
        : IMaskMutationService, IBrushStrokeHistory, IMaskStateCapture, IMaskEditState
    {
        // ── IMaskMutationService ────────────────────────────────────────────
        public void ApplyBrush(BrushStroke stroke) => throw new System.NotImplementedException();
        public void FinishStroke()                  => throw new System.NotImplementedException();
        public void PaintPolygon(int axis, int sliceIndex,
            IReadOnlyList<Vector2> polygon, PaintConfig config)
            => throw new System.NotImplementedException();
        public void Undo() => throw new System.NotImplementedException();
        public void Redo() => throw new System.NotImplementedException();
        public void InitialiseMask() => throw new System.NotImplementedException();
        public int  SaveMask(bool overwrite) => throw new System.NotImplementedException();
        public MaskMode MaskMode    { get; set; }
        public bool     DisplayMask { get; set; }
        public short    NewSourceId  { get; set; }
        public short    CursorSource { get; set; }
        public IReadOnlyList<SourceEntry> GetMaskedSources() => throw new System.NotImplementedException();

        // ── IBrushStrokeHistory ─────────────────────────────────────────────
        public bool CanUndo => throw new System.NotImplementedException();
        public bool CanRedo => throw new System.NotImplementedException();
        public void Clear() => throw new System.NotImplementedException();

        // ── IMaskStateCapture (M-16) ────────────────────────────────────────
        public MaskStateDto Capture() => throw new System.NotImplementedException();
        public void Restore(MaskStateDto dto) => throw new System.NotImplementedException();

        // ── IMaskEditState (sub-port held by IVolumeDataSet, M-27) ──────────
        public short  GetMaskValue(int x, int y, int z)         => throw new System.NotImplementedException();
        public short[] GetMaskSlice(int axis, int sliceIndex)   => throw new System.NotImplementedException();
    }
}
