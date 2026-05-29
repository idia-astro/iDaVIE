// SPDX-License-Identifier: LGPL-3.0-or-later
// Delegates — central declaration site for cross-cutting event delegates (M-15).
// Replaces Assets/Scripts/Tools/Delegates.cs (28 LOC). New delegate types require
// ADR-002 sign-off per global_model.md §1 ST1.

using iDaVIE.Features;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord, SubcubeBounds
using iDaVIE.Rendering.Contracts;

namespace iDaVIE.Kernel.Contracts
{
    /// <summary>Central declaration site for cross-team event delegates.</summary>
    public static class Delegates
    {
        public delegate void DatasetLoaded(IVolumeDataSet dataset);
        public delegate void DatasetUnloaded(IVolumeDataSet dataset);
        public delegate void SubcubeChanged(IVolumeDataSet dataset, SubcubeBounds newBounds);
        public delegate void RestFrequencyChanged(IVolumeDataSet dataset, double newFrequencyHz);

        public delegate void ConfigChanged(Config newConfig);

        public delegate void RenderSettingsChanged();
        public delegate void MomentMapReady(MomentMapResult result);

        public delegate void MaskBufferChanged(IVolumeDataSet dataset);
        public delegate void MaskModeChanged(IVolumeDataSet dataset, MaskMode newMode);
        public delegate void BrushHistoryChanged(bool canUndo, bool canRedo);

        public delegate void FeatureSetChanged();
        public delegate void SelectionChanged(IFeature? feature);
    }
}

namespace iDaVIE.Kernel
{
    // Compatibility aliases retained for earlier refactored skeletons that
    // raised parameterless events before shared_interfaces.md settled.
    public delegate void DatasetLoadedHandler();
    public delegate void DatasetUnloadedHandler();
    public delegate void SubcubeChangedHandler(SubcubeBounds bounds);
    public delegate void RestFrequencyChangedHandler(double restFrequencyGHz);
    public delegate void ConfigChangedHandler();
    public delegate void CursorMovedHandler(CartesianCoord voxel);
}
