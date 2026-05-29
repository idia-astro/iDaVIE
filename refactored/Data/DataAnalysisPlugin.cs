// SPDX-License-Identifier: LGPL-3.0-or-later
// DataAnalysisPlugin — ST2 plug-in. Realises ISourceStatsProvider + IDataAnalysisPlugin
// (both declared in iDaVIE.Features per shared_interfaces.md §5.5; ST5 owns the
// declaration, ST2 realises). Replaces the static PluginInterface/DataAnalysis
// wrapper (252 LOC). M-05, M-07.

using System;
using System.Collections.Generic;
using iDaVIE.Features;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class DataAnalysisPlugin : ISourceStatsProvider, IDataAnalysisPlugin
    {
        // ── ISourceStatsProvider ────────────────────────────────────────────
        public SourceStats GetStatsForSource(int originId) => throw new NotImplementedException();
        public IReadOnlyDictionary<int, SourceStats> GetAllStats() => throw new NotImplementedException();
        public event Action<int> SourceStatsUpdated;

        // ── IDataAnalysisPlugin ─────────────────────────────────────────────
        public SourceStats ComputeRegionStats(CartesianCoord boundsMin, CartesianCoord boundsMax)
            => throw new NotImplementedException();
    }
}
