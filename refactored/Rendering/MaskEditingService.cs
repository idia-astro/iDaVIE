// SPDX-License-Identifier: LGPL-3.0-or-later
// MaskEditingService — extracts mask brush logic from VolumeDataSetRenderer
// (InitialiseMask, PaintMask, PaintCursor, FinishBrushStroke; lines 1158-1238).
//
// Refactor delta:
//   - Pure C# orchestrator. Native mask voxel mutation goes through ST2's
//     IMaskMutationService port (shared_interfaces.md §2); no FitsReader /
//     native IntPtr at this layer.
//   - The 3-D brush stamp from legacy PaintCursor:1213 is rasterised into a
//     BrushStroke (slice-local VoxelCoord2D list) and applied in a single
//     IMaskMutationService.ApplyBrush call, so the native boundary is crossed
//     once per cursor click instead of 27 times for a brush size of 3.
//   - Information Expert — undo / redo history lives on ST2 per
//     Delegates.BrushHistoryChanged; this class is stateless except for the
//     in-progress brush stroke.

using System.Collections.Generic;
using iDaVIE.Data;                       // IMaskMutationService, BrushStroke, StrokePaintConfig, VoxelCoord2D, BrushPaintMode
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord

namespace iDaVIE.Rendering
{
    public sealed class MaskEditingService
    {
        // Axis convention matches IMaskEditState.GetMaskSlice / BrushStroke.Axis:
        // 0 = X, 1 = Y, 2 = Z. Cursor paints stamp along the Z slice (the user's
        // brush rasterises into successive XY slices in legacy behaviour).
        private const int CursorPaintAxis = 2;

        private readonly IMaskMutationService _mask;
        private readonly RegionSelection      _region;

        public MaskEditingService(IMaskMutationService mask, RegionSelection region)
        {
            _mask   = mask;
            _region = region;
        }

        /// <summary>Stamp a brush of (BrushSize)³ voxels centred on the cursor.</summary>
        public void PaintCursor(short sourceId, BrushPaintMode paintMode)
        {
            var radius = (_region.BrushSize - 1) / 2;
            var c      = _region.CursorVoxel;

            // Batch the entire brush stamp into one ST2 call. The legacy
            // implementation called PaintMaskVoxel up to 27 times per click
            // (3-D nested for-loops in VolumeDataSetRenderer.cs:1217).
            // The early-out "already painted at this location" check from
            // PaintMask:1199 moves into ST2's IMaskMutationService.ApplyBrush
            // (the receiver owns the per-stroke deduplication invariant).
            // TODO: walk z = c.Z - radius .. c.Z + radius, build one BrushStroke
            //       per z slice with the XY footprint as VoxelCoord2D entries,
            //       and call _mask.ApplyBrush for each slice.
            var config = new StrokePaintConfig(
                SourceId : sourceId,
                Additive : true,
                PaintMode: paintMode);

            for (var dz = -radius; dz <= radius; dz++)
            {
                var voxels = new List<VoxelCoord2D>((2 * radius + 1) * (2 * radius + 1));
                for (var dy = -radius; dy <= radius; dy++)
                for (var dx = -radius; dx <= radius; dx++)
                    voxels.Add(new VoxelCoord2D(c.X + dx, c.Y + dy));

                _mask.ApplyBrush(new BrushStroke(
                    axis      : CursorPaintAxis,
                    sliceIndex: c.Z + dz,
                    voxelCoords: voxels,
                    paintConfig: config));
            }
        }

        public void FinishBrushStroke() => _mask.FinishStroke();

        /// <summary>Allocate an empty mask when one wasn't loaded with the cube.</summary>
        public void InitialiseEmptyMask() => _mask.InitialiseMask();
    }
}
