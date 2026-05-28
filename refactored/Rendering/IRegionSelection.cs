// SPDX-License-Identifier: LGPL-3.0-or-later
// IRegionSelection — read-only consumer view of RegionSelection state.
//
// Mirrors the IMaskGpuBuffers / IMaskMode pattern: ST3-internal port that lets
// VolumeCameraDriver, MaskEditingService, and VolumeDataSetRenderer depend on
// region state through an interface rather than the concrete RegionSelection
// class. The composition root constructs RegionSelection and hands the same
// instance to each consumer via this port; the writes (SetCursorPosition,
// SetRegionPosition, SetRegionBounds, ClearRegion, SetHighlightedSource) stay
// on the concrete class, used only by the input controller that drives editing.

using System;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord

namespace iDaVIE.Rendering
{
    /// <summary>
    /// ST3-internal port: read-only view of the live region / cursor state
    /// that downstream binders subscribe to. ≤7 read members (ISP, MOD-05).
    /// </summary>
    public interface IRegionSelection
    {
        CartesianCoord CursorVoxel       { get; }
        CartesianCoord RegionStartVoxel  { get; }
        CartesianCoord RegionEndVoxel    { get; }
        bool           IsActive          { get; }
        short          HighlightedSource { get; }
        int            BrushSize         { get; }

        event Action CursorMoved;
        event Action RegionChanged;
        event Action RegionCleared;
    }
}
