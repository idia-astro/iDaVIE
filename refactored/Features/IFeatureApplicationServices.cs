// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// Application-layer service interfaces provided by ST5:
//   IFeatureListNavigation — ST4 voice-command navigation (M-11)
//   IMomentMapService      — ST6 + ST5 moment-map menus (M-08)
//   ISpectralProfileService — ST6 + ST5 spectral-profile menus
//
// Canonical signatures: ST5_interface.md §3; shared_interfaces.md §5.3.

using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord
using iDaVIE.Rendering.Contracts;      // MomentMapResult (ST3, resolution line 13)

namespace iDaVIE.Features
{
    /// <summary>Source-list navigation surface for ST4's voice commands
    /// (`next source list` / `previous source list`). Realised by
    /// FeatureMenuController. M-11.
    ///
    /// The Count property ST4 originally drafted on its ISourceCatalogue is
    /// derivable from IFeatureSetQuery.GetAllFeatureSets().Count — not re-exported
    /// here (ST5_domain_design.md §7, FeatureMenuController paragraph).</summary>
    public interface IFeatureListNavigation
    {
        void DisplayNextSet();
        void DisplayPreviousSet();
    }

    /// <summary>Moment-map generation use case. Realised by MomentMapServiceAdapter,
    /// which wraps ST3's IMomentMapRenderer (M-08). Consumed by ST5's own
    /// MomentMapMenuController (brief §6.5 "moment maps") and by ST6's desktop panel.
    ///
    /// MomentMapResult is owned by ST3 (iDaVIE.Rendering.Contracts, resolution line 13).</summary>
    public interface IMomentMapService
    {
        /// <summary>
        /// momentOrder: 0 = integrated intensity (Moment0), 1 = velocity field (Moment1).
        /// Returns a task; the result must be consumed on the Unity main thread before
        /// touching Unity APIs (ST5_interface.md §6 threading constraints).
        /// </summary>
        Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask);
    }

    /// <summary>Region → spectral profile use case. Realised by SpectralProfileService,
    /// which wraps IDataAnalysisPlugin.ComputeRegionStats. Consumed by ST5's own
    /// SpectralProfileHelper (brief §6.5 "spectral profiles") and by ST6's desktop panel.
    ///
    /// Replaces the direct DataAnalysis.GetSourceStats P/Invoke in the legacy
    /// SpectralProfileHelper.OnCroppedRegionChanged.</summary>
    public interface ISpectralProfileService
    {
        /// <summary>Bounds are data-cube voxel coordinates.
        /// Profile[i] is flux at channel ZStartChannel + i.</summary>
        Task<SpectralProfileResult> ComputeForRegionAsync(
            CartesianCoord boundsMin, CartesianCoord boundsMax);
    }
}
