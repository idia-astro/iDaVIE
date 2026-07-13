/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// =============================================================================
// AFTER — Service: MaskControllerService (design-level worked example)
//
// Single responsibility: voxel mask painting within the active crop region.
//
// CK AFTER (estimated):
//   WMC  ≈ 12  (3 public + 1 private + constructor, avg complexity 2.5)
//   CBO  ≈ 4   (VolumeDataSet×2, MomentMapRenderer, UnityEngine.Vector3Int)
//   RFC  ≈ 14
//   LCOM ≈ 0.10
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Paints mask voxels within the region currently loaded into GPU memory.
    /// <para>
    /// Holding only mask-painting state means the class is testable without
    /// a rendering context — pass a headless <see cref="VolumeDataSet"/> stub
    /// and verify that <see cref="PaintCursor"/> produces the expected voxel values.
    /// </para>
    /// </summary>
    public sealed class MaskControllerService : IMaskController
    {
        private VolumeDataSet _maskDataSet;
        private MomentMapRenderer _momentMapRenderer;

        private Vector3 _sliceMin;
        private Vector3 _sliceMax;
        private Vector3Int _cursorVoxel;
        private int _brushRadius;

        private Vector3Int _previousPaintLocation;
        private short _previousPaintValue;

        public MaskControllerService() { }

        // ── IMaskController ────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void Initialise(VolumeDataSet dataSet)
        {
            _maskDataSet = dataSet.GenerateEmptyMask();
        }

        /// <inheritdoc/>
        public bool PaintCursor(short value)
        {
            int limit = (_brushRadius - 1) / 2;
            bool anyPainted = false;
            for (int i = -limit; i <= limit; i++)
                for (int j = -limit; j <= limit; j++)
                    for (int k = -limit; k <= limit; k++)
                        anyPainted |= PaintMask(
                            new Vector3Int(_cursorVoxel.x + i, _cursorVoxel.y + j, _cursorVoxel.z + k),
                            value);
            return anyPainted;
        }

        /// <inheritdoc/>
        public void FinishBrushStroke()
        {
            _maskDataSet?.FlushBrushStroke();
            _momentMapRenderer?.CalculateMomentMaps();
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private bool PaintMask(Vector3Int position, short value)
        {
            if (_maskDataSet == null || _maskDataSet.RegionCube == null)
                return false;

            var regionSizeObjectSpace = _sliceMax - _sliceMin;
            var regionSizeDataSpace = new Vector3(
                _maskDataSet.XDim * regionSizeObjectSpace.x,
                _maskDataSet.YDim * regionSizeObjectSpace.y,
                _maskDataSet.ZDim * regionSizeObjectSpace.z);

            if (Mathf.Floor(regionSizeDataSpace.x) > _maskDataSet.RegionCube.width  ||
                Mathf.Floor(regionSizeDataSpace.y) > _maskDataSet.RegionCube.height ||
                Mathf.Floor(regionSizeDataSpace.z) > _maskDataSet.RegionCube.depth)
                return false;

            var offsetRegionSpace = Vector3Int.FloorToInt(new Vector3(
                (0.5f + _sliceMin.x) * _maskDataSet.XDim,
                (0.5f + _sliceMin.y) * _maskDataSet.YDim,
                (0.5f + _sliceMin.z) * _maskDataSet.ZDim));

            var coordsRegionSpace = position - Vector3Int.one - offsetRegionSpace;
            if (coordsRegionSpace == _previousPaintLocation && value == _previousPaintValue)
                return true;

            _previousPaintLocation = coordsRegionSpace;
            _previousPaintValue    = value;
            return _maskDataSet.PaintMaskVoxel(coordsRegionSpace, value);
        }

        /// <summary>
        /// Called by the orchestrator each frame to update the active cursor and brush.
        /// </summary>
        public void UpdateCursor(Vector3Int cursorVoxel, int brushRadius, Vector3 sliceMin, Vector3 sliceMax)
        {
            _cursorVoxel  = cursorVoxel;
            _brushRadius  = brushRadius;
            _sliceMin     = sliceMin;
            _sliceMax     = sliceMax;
        }

        /// <summary>Wires the moment-map renderer after scene setup.</summary>
        public void BindMomentMapRenderer(MomentMapRenderer renderer)
        {
            _momentMapRenderer = renderer;
        }
    }
}
