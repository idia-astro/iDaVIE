// SPDX-License-Identifier: LGPL-3.0-or-later
// RegionSelection — extracts cursor / video-cursor / region state from
// VolumeDataSetRenderer (lines 639-872, ~230 lines, eight methods).
//
// Refactor delta:
//   - Unity-free domain aggregate. The outline GameObjects (CuboidLine) that
//     the legacy class managed in-place live on a separate small MonoBehaviour
//     adapter — RegionOutlineRenderer — driven by the events declared below.
//   - Information Expert — region state lives on one class; the renderer
//     reads RegionStartVoxel / RegionEndVoxel via a read-only handle on the
//     post-refactor VolumeDataSetRenderer.
//
// Events replace the legacy direct mutation of CuboidLine.Center / Bounds /
// Activate / Deactivate calls scattered through SetCursorPosition, SetRegionPosition,
// UpdateRegionBounds, ClearRegion, ClearMeasure, etc.

using System;
using iDaVIE.Kernel.Contracts;            // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;      // CartesianCoord
using iDaVIE.Data;                        // IMaskEditState (mask sampling at cursor)

namespace iDaVIE.Rendering
{
    public sealed class RegionSelection
    {
        private readonly IVolumeDataSet _volume;
        private readonly VolumeCoordinateService _coords;

        public RegionSelection(IVolumeDataSet volume, VolumeCoordinateService coords)
        {
            _volume = volume;
            _coords = coords;
        }

        public CartesianCoord CursorVoxel       { get; private set; }
        public CartesianCoord RegionStartVoxel  { get; private set; }
        public CartesianCoord RegionEndVoxel    { get; private set; }
        public float          CursorValue       { get; private set; }
        public short          CursorSource      { get; private set; }
        public int            BrushSize         { get; private set; } = 1;

        /// <summary>True while RegionStart/EndVoxel describe a live selection;
        /// false after ClearRegion or before the first SetRegionPosition call.
        /// Replaces the legacy CuboidLine.Active flag the renderer used to gate
        /// the HighlightMin/Max shader uniforms (legacy line 1056).</summary>
        public bool           IsActive          { get; private set; }

        /// <summary>Mask source-id the user is currently highlighting (paint
        /// brush / hover). Forwarded to the mask shader so a single source can
        /// be rendered isolated. Default 0 ⇒ no highlight.</summary>
        public short          HighlightedSource { get; private set; }

        public event Action CursorMoved;
        public event Action RegionChanged;
        public event Action RegionCleared;

        public void SetCursorPosition(LocalPosition cubePosition, int brushSize)
        {
            // Legacy implementation: SetCursorPosition at FeatureSetRenderer.cs:639.
            // Refactored to:
            //   1. CursorVoxel = _coords.ClampedVoxelFromCubePosition(cubePosition).
            //   2. CursorValue = _volume.RawVoxelAccess.GetValue(CursorVoxel)        // ST2 port
            //   3. CursorSource = _volume.MaskEditState.GetMaskValue(CursorVoxel)    // ST2 port
            //   4. BrushSize = brushSize.
            //   5. Raise CursorMoved (RegionOutlineRenderer listens and updates
            //      its Unity-side CuboidLine).
            // TODO: full implementation; the per-step body is unchanged.
            CursorMoved?.Invoke();
        }

        public void SetRegionPosition(LocalPosition cubePosition, bool isStartCorner)
        {
            var voxel = _coords.ClampedVoxelFromCubePosition(cubePosition);
            if (isStartCorner) RegionStartVoxel = voxel;
            else               RegionEndVoxel   = voxel;
            IsActive = true;
            RegionChanged?.Invoke();
        }

        public void SetRegionBounds(CartesianCoord min, CartesianCoord max)
        {
            RegionStartVoxel = min;
            RegionEndVoxel   = max;
            IsActive = true;
            RegionChanged?.Invoke();
        }

        public void SetHighlightedSource(short sourceId)
        {
            HighlightedSource = sourceId;
            RegionChanged?.Invoke();
        }

        public void ClearRegion()
        {
            RegionStartVoxel = default;
            RegionEndVoxel   = default;
            IsActive = false;
            RegionCleared?.Invoke();
        }
    }
}
