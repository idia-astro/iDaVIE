// SPDX-License-Identifier: LGPL-3.0-or-later
// SourceStats / ISourceStatsProvider / IDataAnalysisPlugin
//
// Cross-team types declared by ST5 per shared_interfaces.md §5.5
// (resolution lines 8, 16, 17). The resolution chose ST5's narrow design over
// ST2's, so they live in iDaVIE.Features even though ST2 realises them at
// runtime (production responsibility stays with ST2's DataAnalysisPlugin).
//
// Consumed in this refactor by:
//   • Features/FeatureFactory.cs        (PopulateFromSourceStats)
//   • Features/FeatureSetService.cs     (SourceStatsUpdated subscription)
//   • Features/VoTableSaver.cs          (flux-weighted centroids on export)

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord

namespace iDaVIE.Features
{
    /// <summary>Stats payload POCO. The "// ← x" comments map this field to
    /// the legacy DataAnalysis.SourceStats field that populates it (guidance
    /// for ST2's adapter implementation).</summary>
    public sealed class SourceStats
    {
        public long           VoxelCount           { get; init; }   // ← numVoxels
        public CartesianCoord BoundsMin            { get; init; }   // ← minX/Y/Z
        public CartesianCoord BoundsMax            { get; init; }   // ← maxX/Y/Z
        public double         TotalFlux            { get; init; }   // ← sum
        public double         PeakFlux             { get; init; }   // ← peak
        public CartesianCoord FluxWeightedCentroid { get; init; }   // ← cX/Y/Z
        public double         ChannelW20           { get; init; }
        public double         VeloW20              { get; init; }
        public double         ChannelVsys          { get; init; }
        public double         VeloVsys             { get; init; }
        public IReadOnlyList<double> SpectralProfile { get; init; } = Array.Empty<double>();
        public int            ZStartChannel        { get; init; }
    }

    public interface ISourceStatsProvider
    {
        SourceStats? GetStatsForSource(int originId);
        IReadOnlyDictionary<int, SourceStats> GetAllStats();

        /// <summary>Fired with the source id whose stats were updated (or -1 for bulk reload).</summary>
        event Action<int> SourceStatsUpdated;
    }

    /// <summary>Narrow ST5 design (resolution line 8). ST1's VolumeDataSet uses
    /// GetStats() / GetHistogram() for cube-wide aggregates; this port covers
    /// region-bounded stats used by ST5's spectral profile service and feature
    /// annotation pipeline.</summary>
    public interface IDataAnalysisPlugin
    {
        SourceStats ComputeRegionStats(CartesianCoord boundsMin, CartesianCoord boundsMax);
    }
}
