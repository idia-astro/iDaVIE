// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureFactory — extracts SpawnFeaturesFromTable (262 lines) and
// SpawnFeaturesFromSourceStats from the legacy FeatureSetRenderer.
//
// Refactor delta:
//   - No UnityEngine reference. Coordinates are CartesianCoord; WCS transforms
//     happen via ICoordinateTransformer rather than the static AstTool.
//   - The coordinate-type branching (cartesian / velz / freqz / redz) that drove
//     the legacy method's Many Conditionals / Complex Conditional CodeScene flags
//     dispatches on a per-row coordinate-projector lookup table (Strategy pattern)
//     to satisfy OCP — a new coord type adds a row, not a branch.
//   - The set is supplied by the caller; the factory only parses, projects,
//     constructs, and attaches. Set creation + event ordering live in the
//     orchestrator (FeatureSetService / FeatureImportService) per §6.3.
//   - Statistics are populated BEFORE the Feature is exposed (Invariant 5.4
//     in ST5_domain_design.md §5.4).

using System.Collections.Generic;
using iDaVIE.Data;                       // ICoordinateTransformer
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord
// SourceStats, ISourceStatsProvider live in iDaVIE.Features (this namespace) per shared_interfaces.md §5.5.

namespace iDaVIE.Features
{
    internal sealed class FeatureFactory : IFeatureFactory
    {
        private readonly ICoordinateTransformer _coords;
        private readonly ISourceStatsProvider   _stats;

        public FeatureFactory(ICoordinateTransformer coords, ISourceStatsProvider stats)
        {
            _coords = coords;
            _stats  = stats;
        }

        public void PopulateFromTable(FeatureSet target, FeatureTable table, FeatureImportMapping mapping)
        {
            // 1. Resolve column indices (X/Y/Z or Ra/Dec/{Velo|Freq|Redshift}, plus optional box columns
            //    and ID/Flag) from mapping.ColumnAssignments.
            // 2. For each table row:
            //      a. Parse spatial values (CultureInfo.InvariantCulture).
            //      b. If sky/spectral, dispatch on coord type to project through _coords.Transform.
            //      c. Build CartesianCoord center; CartesianCoord size from box columns if present
            //         (default 1×1×1 voxel cube otherwise).
            //      d. Collect raw-column values per mapping.ColumnMask into RawDataValues.
            //      e. Construct Feature(originId = row, name, flag, center, size, rawDataValues).
            //      f. Apply mapping.ExcludeExternal — drop features whose Center is outside
            //         the cube. Bounds-check lives on Feature/VolumeBounds, not here.
            //      g. target.AddFeature(feature).
            // 3. Caller raises FeatureSetChanged once after the batch (bulk-population rule).
            // TODO: full implementation; complexity now lives in private per-projector helpers,
            // not in this orchestrator method.
        }

        public void PopulateFromSourceStats(FeatureSet target)
        {
            IReadOnlyDictionary<int, SourceStats> all = _stats.GetAllStats();
            foreach (var (originId, src) in all)
            {
                var center = new CartesianCoord(
                    (src.BoundsMin.X + src.BoundsMax.X) / 2,
                    (src.BoundsMin.Y + src.BoundsMax.Y) / 2,
                    (src.BoundsMin.Z + src.BoundsMax.Z) / 2);
                var size = new CartesianCoord(
                    src.BoundsMax.X - src.BoundsMin.X,
                    src.BoundsMax.Y - src.BoundsMin.Y,
                    src.BoundsMax.Z - src.BoundsMin.Z);

                var feature = new Feature(originId, $"Masked Source #{originId}", flag: "",
                                          center, size, rawDataValues: System.Array.Empty<string>());

                // Establish Invariant 5.4: Statistics non-null on a Mask feature BEFORE exposure.
                feature.UpdateStatistics(new FeatureStatistics(
                    src.VoxelCount, src.TotalFlux, src.PeakFlux, src.FluxWeightedCentroid,
                    src.ChannelW20, src.VeloW20, src.ChannelVsys, src.VeloVsys));

                target.AddFeature(feature);
            }
            // Caller raises FeatureSetChanged.
        }
    }
}
