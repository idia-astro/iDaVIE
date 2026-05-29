// SPDX-License-Identifier: LGPL-3.0-or-later
// DesktopPaintRasteriser — pure C# polygon rasterisation. No UnityEngine.
// Owned by ST6 per global_model.md §1 ST6 "Direct Texture3D reads of
// RegionCube / MaskCube — Removed; replaced by IRawVoxelAccess slice fetches
// + ST6-side Texture2D construction" (M-14).
//
// Output crosses ST6 → ST2 as IReadOnlyList<VoxelCoord2D> via
// IMaskMutationService.ApplyBrush(BrushStroke).

using System.Collections.Generic;
using System.Numerics;
using iDaVIE.Data;                       // VoxelCoord2D

namespace iDaVIE.UI
{
    public sealed class DesktopPaintRasteriser
    {
        /// <summary>Rasterises a closed polygon (in slice-local coords) into the
        /// voxel indices it covers. Circle / disc brushes are passed as a
        /// many-sided polygon by the caller.</summary>
        public IReadOnlyList<VoxelCoord2D> RasterisePolygon(
            IReadOnlyList<Vector2> polygon, int sliceWidth, int sliceHeight)
            => throw new System.NotImplementedException();

        /// <summary>Bresenham midpoint circle rasterisation around the given centre.</summary>
        public IReadOnlyList<VoxelCoord2D> RasteriseDisc(
            int centreU, int centreV, int radius, int sliceWidth, int sliceHeight)
            => throw new System.NotImplementedException();
    }
}
