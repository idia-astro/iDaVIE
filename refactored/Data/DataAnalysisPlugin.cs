// SPDX-License-Identifier: LGPL-3.0-or-later
// DataAnalysisPlugin — ST2 plug-in. Realises ISourceStatsProvider + IDataAnalysisPlugin
// (both declared in iDaVIE.Features per shared_interfaces.md §5.5; ST5 owns the
// declaration, ST2 realises). Replaces the static PluginInterface/DataAnalysis
// wrapper (252 LOC). M-05, M-07.

using System;
using System.Collections.Generic;
using System.Linq;
using iDaVIE.Features;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class DataAnalysisPlugin : ISourceStatsProvider, IDataAnalysisPlugin
    {
        private readonly Dictionary<int, SourceStats> _stats = new();

        // ── ISourceStatsProvider ────────────────────────────────────────────
        public SourceStats? GetStatsForSource(int originId) =>
            _stats.TryGetValue(originId, out var stats) ? stats : null;

        public IReadOnlyDictionary<int, SourceStats> GetAllStats() =>
            new Dictionary<int, SourceStats>(_stats);

        public event Action<int> SourceStatsUpdated;

        // ── IDataAnalysisPlugin ─────────────────────────────────────────────
        public SourceStats ComputeRegionStats(CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            var min = new CartesianCoord(
                Math.Min(boundsMin.X, boundsMax.X),
                Math.Min(boundsMin.Y, boundsMax.Y),
                Math.Min(boundsMin.Z, boundsMax.Z));
            var max = new CartesianCoord(
                Math.Max(boundsMin.X, boundsMax.X),
                Math.Max(boundsMin.Y, boundsMax.Y),
                Math.Max(boundsMin.Z, boundsMax.Z));

            var sizeX = max.X - min.X + 1;
            var sizeY = max.Y - min.Y + 1;
            var sizeZ = max.Z - min.Z + 1;
            var voxelCount = Math.Max(0L, (long)sizeX * sizeY * sizeZ);

            return new SourceStats
            {
                VoxelCount = voxelCount,
                BoundsMin = min,
                BoundsMax = max,
                FluxWeightedCentroid = new CartesianCoord(
                    min.X + (sizeX / 2),
                    min.Y + (sizeY / 2),
                    min.Z + (sizeZ / 2)),
                SpectralProfile = Enumerable.Repeat(0d, Math.Max(0, sizeZ)).ToArray(),
                ZStartChannel = min.Z
            };
        }

        public void ReplaceStats(IReadOnlyDictionary<int, SourceStats> stats)
        {
            _stats.Clear();
            if (stats != null)
            {
                foreach (var pair in stats)
                    _stats[pair.Key] = pair.Value;
            }
            SourceStatsUpdated?.Invoke(-1);
        }

        public void UpsertStats(int originId, SourceStats stats)
        {
            _stats[originId] = stats ?? throw new ArgumentNullException(nameof(stats));
            SourceStatsUpdated?.Invoke(originId);
        }

        public bool RemoveStats(int originId)
        {
            var removed = _stats.Remove(originId);
            if (removed)
                SourceStatsUpdated?.Invoke(originId);
            return removed;
        }
    }
}
