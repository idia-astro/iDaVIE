// SPDX-License-Identifier: LGPL-3.0-or-later
// PaintTabViewModel — ST6 ViewModel for the Paint tab. Replaces the presentation
// state in Assets/Scripts/UI/DesktopPaintController.cs (1558 LOC). Commits via
// IMaskMutationService.ApplyBrush (the rasterised path) or PaintPolygon (the
// polygon path); no Texture3D read crosses the boundary (M-14).
//
// The polygon → voxel-coord rasterisation lives in DesktopPaintRasteriser.

using System;
using System.Collections.Generic;
using System.Numerics;
using iDaVIE.Data;                       // IMaskMutationService, BrushStroke, BrushPaintMode
using iDaVIE.Interaction;                // BrushConfig, IInteractionStateProvider
using iDaVIE.Kernel.Contracts.Plugins;   // IRawVoxelAccess

namespace iDaVIE.UI
{
    public sealed class PaintTabViewModel : IDisposable
    {
        public PaintTabViewModel(IMaskMutationService mask,
                                 IRawVoxelAccess voxels,
                                 IInteractionStateProvider interactionState,
                                 DesktopPaintRasteriser rasteriser)
            => throw new NotImplementedException();

        public BrushConfig BrushConfig       { get; private set; }
        public bool        StrokeInProgress  { get; private set; }
        public int         ActiveAxis        { get; private set; }
        public int         ActiveSlice       { get; private set; }
        public IReadOnlyList<Vector2> PolygonPreview { get; private set; }

        public event Action BrushConfigChanged;
        public event Action PolygonPreviewUpdated;
        public event Action StrokeCommitted;

        public void SetBrushRadius(float radius)              => throw new NotImplementedException();
        public void SetPaintMode(BrushPaintMode mode)         => throw new NotImplementedException();
        public void SetSourceId(int sourceId)                 => throw new NotImplementedException();
        public void SetAxisAndSlice(int axis, int slice)      => throw new NotImplementedException();

        public void StartStroke()                             => throw new NotImplementedException();
        public void AddPolygonPoint(float canvasX, float canvasY) => throw new NotImplementedException();
        public void FinishAndCommit()                         => throw new NotImplementedException();
        public void Clear()                                   => throw new NotImplementedException();

        public void Undo() => throw new NotImplementedException();
        public void Redo() => throw new NotImplementedException();

        public void Dispose() => throw new NotImplementedException();
    }
}
