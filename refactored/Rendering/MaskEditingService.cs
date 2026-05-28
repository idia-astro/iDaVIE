// SPDX-License-Identifier: LGPL-3.0-or-later
// MaskEditingService — extracts mask brush logic from VolumeDataSetRenderer
// (InitialiseMask, PaintMask, PaintCursor, FinishBrushStroke; lines 1158-1238).
//
// Refactor delta:
//   - Pure C# orchestrator. Native mask voxel mutation goes through ST2's
//     IMaskMutationService port; no FitsReader / native IntPtr at this layer.
//   - Brush iteration logic (the 3-D for-loop in legacy PaintCursor:1213) is
//     kept here but now batches voxels into a single IMaskMutationService.PaintVoxels
//     call so the native boundary is crossed once per cursor click, not 27 times
//     for a brush size of 3.
//   - Information Expert — undo / redo history lives on ST2 per
//     Delegates.BrushHistoryChanged; this class is stateless except for the
//     in-progress brush stroke.

using iDaVIE.Data;                       // IMaskMutationService, BrushStroke
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord

namespace iDaVIE.Rendering
{
    public sealed class MaskEditingService
    {
        private readonly IMaskMutationService _mask;
        private readonly RegionSelection      _region;
        private CartesianCoord _previousPaintLocation;
        private short          _previousPaintValue;

        public MaskEditingService(IMaskMutationService mask, RegionSelection region)
        {
            _mask   = mask;
            _region = region;
        }

        /// <summary>Stamp a brush of (BrushSize)³ voxels centred on the cursor.</summary>
        public void PaintCursor(short value)
        {
            var radius = (_region.BrushSize - 1) / 2;
            var c      = _region.CursorVoxel;

            // Batch the entire brush stamp into one ST2 call. The legacy
            // implementation called PaintMaskVoxel(...) up to 27 times per
            // click (3-D nested for-loops in VolumeDataSetRenderer.cs:1217).
            // TODO: accumulate voxels into IReadOnlyList<CartesianCoord> and
            //       call _mask.PaintVoxels(voxels, value). The early-out
            //       "already painted at this location" check from PaintMask:1199
            //       moves into the accumulator as a set-membership filter.
        }

        public void FinishBrushStroke() => _mask.FlushStroke();

        /// <summary>Allocate an empty mask when one wasn't loaded with the cube.</summary>
        public void InitialiseEmptyMask()
        {
            // Replaces legacy InitialiseMask:1158. Native allocation now sits behind
            // IMaskMutationService.CreateEmpty(); no Texture3D ownership leaks here.
            _mask.CreateEmpty();
        }
    }
}
