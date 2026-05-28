// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// SpectralProfileService — replaces the direct DataAnalysis.GetSourceStats P/Invoke
// in the legacy SpectralProfileHelper.OnCroppedRegionChanged.
//
// Realises ISpectralProfileService (cross-team, provided to ST6 and to ST5's own
// SpectralProfileHelper — brief §6.5 "spectral profiles").
// Internal sealed per DD-5.
//
// The native call may be slow on large regions, so the work is run off the main
// thread via Task.Run. The caller must consume the result on the main thread before
// touching Unity APIs (ST5_interface.md §6 threading constraints).

using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord
// IDataAnalysisPlugin and SpectralProfileResult live in iDaVIE.Features (this namespace).

namespace iDaVIE.Features
{
    internal sealed class SpectralProfileService : ISpectralProfileService
    {
        private readonly IDataAnalysisPlugin _plugin;

        public SpectralProfileService(IDataAnalysisPlugin plugin)
        {
            _plugin = plugin;
        }

        public Task<SpectralProfileResult> ComputeForRegionAsync(
            CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            if (boundsMin.X > boundsMax.X || boundsMin.Y > boundsMax.Y || boundsMin.Z > boundsMax.Z)
                throw new System.ArgumentException(
                    "boundsMin must be ≤ boundsMax on each axis.", nameof(boundsMin));

            // Capture parameters for the lambda — CartesianCoord is a value type so no closure risk.
            return Task.Run(() =>
            {
                var stats = _plugin.ComputeRegionStats(boundsMin, boundsMax);
                // SpectralProfileResult.ZStartChannel lets the consumer recover per-channel
                // Z-axis world values via ICoordinateTransformer — replacing the inline
                // AstTool.Transform3D(0, 0, i + minZ, …) walk in the legacy SpectralProfileHelper.
                return new SpectralProfileResult(
                    stats.SpectralProfile,
                    stats.ZStartChannel,
                    stats.TotalFlux,
                    stats.PeakFlux);
            });
        }
    }
}
