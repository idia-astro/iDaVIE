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
// AFTER — Service: RegionControllerService (design-level worked example)
//
// Single responsibility: interactive bounding-box selection, crop, and teleport.
// Depends on ICoordinateMapper (injected) rather than Transform directly —
// this is the dependency-inversion that breaks the circular coupling.
//
// CK AFTER (estimated):
//   WMC  ≈ 18  (12 public + 2 private, avg complexity 1.9)
//   CBO  ≈ 6   (ICoordinateMapper, PolyLine, CuboidLine, VolumeDataSet,
//               VolumeInputController, Feature)
//   RFC  ≈ 22
//   LCOM ≈ 0.18
// =============================================================================

using System.Collections.Generic;
using DataFeatures;
using LineRenderer;
using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Manages the interactive region selection outline, crop operations,
    /// video-cursor overlay, and VR teleport-to-region.
    /// <para>
    /// Depends on <see cref="ICoordinateMapper"/> so coordinate math can be
    /// tested without a live Unity Transform.
    /// </para>
    /// </summary>
    public sealed class RegionControllerService : IRegionController
    {
        private readonly ICoordinateMapper _coordinateMapper;
        private readonly VolumeDataSet _dataSet;

        private CuboidLine _regionOutline;
        private CuboidLine _voxelOutline;
        private CuboidLine _videoCursorOutline;
        private PolyLine _measuringLine;
        private VolumeInputController _inputController;

        private long _maxCubeSizeMb;
        private bool _showMeasuringLine;

        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel   { get; private set; }
        public bool IsCropped              { get; private set; }
        public Vector3Int CurrentCropMin   { get; private set; }
        public Vector3Int CurrentCropMax   { get; private set; }

        public RegionControllerService(ICoordinateMapper coordinateMapper, VolumeDataSet dataSet,
                                       Transform parent, long maxCubeSizeMb)
        {
            _coordinateMapper = coordinateMapper;
            _dataSet          = dataSet;
            _maxCubeSizeMb    = maxCubeSizeMb;

            _regionOutline     = new CuboidLine { Parent = parent, Center = Vector3.zero, Color = Color.green, Bounds = Vector3.one };
            _voxelOutline      = new CuboidLine { Parent = parent, Center = Vector3.zero, Color = Color.green, Bounds = Vector3.one };
            _videoCursorOutline = new CuboidLine { Parent = parent, Center = Vector3.zero, Color = Color.cyan,  Bounds = Vector3.one };
            _measuringLine     = new PolyLine   { Parent = parent, Color = Color.white };
        }

        // ── IRegionController ──────────────────────────────────────────────────

        public void SetCursor(Vector3 worldPos, int brushSize)
        {
            var voxel = _coordinateMapper.GetVoxelAtWorldPos(worldPos);
            var localCenter = _coordinateMapper.VolumeToLocal(voxel + 0.5f * Vector3.one);
            _voxelOutline.Center = localCenter;
            _voxelOutline.Bounds = brushSize * new Vector3(
                1f / _dataSet.XDim, 1f / _dataSet.YDim, 1f / _dataSet.ZDim);
            _voxelOutline.Activate();
        }

        public void SetVideoCursor(Vector3 worldPos)
        {
            var voxel      = _coordinateMapper.GetVoxelAtWorldPos(worldPos);
            var localCenter = _coordinateMapper.VolumeToLocal(voxel + 0.5f * Vector3.one);
            _videoCursorOutline.Center = localCenter;
            _videoCursorOutline.Activate();
        }

        public void ClearVideoCursor()
        {
            _videoCursorOutline?.Deactivate();
        }

        public void SetRegionStart(Vector3 worldPos)
        {
            RegionStartVoxel = _coordinateMapper.GetVoxelAtWorldPos(worldPos);
            RefreshRegionOutline();
            _regionOutline.Activate();
        }

        public void SetRegionEnd(Vector3 worldPos)
        {
            RegionEndVoxel = _coordinateMapper.GetVoxelAtWorldPos(worldPos);
            RefreshRegionOutline();
            _regionOutline.Activate();
        }

        public void SetRegionBounds(Vector3Int min, Vector3Int max)
        {
            RegionStartVoxel = min;
            RegionEndVoxel   = max;
            RefreshRegionOutline();
        }

        public void ClearRegion()
        {
            _regionOutline?.Deactivate();
            _measuringLine?.Deactivate();
        }

        public bool CropToFeature(Feature feature)
        {
            if (feature == null) return false;
            CropToRegion(feature.CornerMin, feature.CornerMax);
            return true;
        }

        public void CropToRegion(Vector3 min, Vector3 max)
        {
            CurrentCropMin = Vector3Int.FloorToInt(min);
            CurrentCropMax = Vector3Int.FloorToInt(max);
            IsCropped      = true;
        }

        public void ResetCrop()
        {
            CurrentCropMin = Vector3Int.zero;
            CurrentCropMax = new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
            IsCropped      = false;
        }

        public void TeleportToRegion()
        {
            if (_inputController == null) return;
            var boundsMin = (Vector3)CurrentCropMin;
            var boundsMax = (Vector3)CurrentCropMax;
            _inputController.Teleport(
                _coordinateMapper.VolumeToLocal(boundsMin) - 0.5f * Vector3.one,
                _coordinateMapper.VolumeToLocal(boundsMax) + 0.5f * Vector3.one);
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void RefreshRegionOutline()
        {
            var regionMin  = Vector3.Min(RegionStartVoxel, RegionEndVoxel);
            var regionMax  = Vector3.Max(RegionStartVoxel, RegionEndVoxel);
            var regionSize = regionMax - regionMin + Vector3.one;
            var regionCenter = (regionMax + regionMin) / 2f - 0.5f * Vector3.one;

            _regionOutline.Center = _coordinateMapper.VolumeToLocal(regionCenter);
            _regionOutline.Bounds = new Vector3(
                regionSize.x / _dataSet.XDim,
                regionSize.y / _dataSet.YDim,
                regionSize.z / _dataSet.ZDim);

            long sizeBytes = (long)(regionSize.x * regionSize.y * regionSize.z * sizeof(float));
            bool isFullRes = sizeBytes <= _maxCubeSizeMb * 1_000_000L;
            Feature.SetCubeColors(_regionOutline, isFullRes ? Color.white : Color.yellow, isFullRes);

            if (_showMeasuringLine)
            {
                var startPt = _coordinateMapper.VolumeToLocal(RegionStartVoxel);
                var endPt   = _coordinateMapper.VolumeToLocal(RegionEndVoxel);
                _measuringLine.Vertices = new List<Vector3> { startPt, endPt };
                _measuringLine.Activate();
            }
        }

        /// <summary>Wires the VR input controller for teleport support after scene setup.</summary>
        public void BindInputController(VolumeInputController controller)
        {
            _inputController = controller;
        }
    }
}
