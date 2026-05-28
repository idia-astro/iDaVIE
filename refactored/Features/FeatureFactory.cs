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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using iDaVIE.Data;                       // ICoordinateTransformer
using iDaVIE.Kernel.Contracts;           // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, VolumeExtents
// SourceStats, ISourceStatsProvider live in iDaVIE.Features (this namespace) per shared_interfaces.md §5.5.

namespace iDaVIE.Features
{
    internal sealed class FeatureFactory : IFeatureFactory
    {
        private readonly ICoordinateTransformer _coords;
        private readonly ISourceStatsProvider   _stats;
        private readonly IVolumeDataSet         _volume;

        public FeatureFactory(ICoordinateTransformer coords, ISourceStatsProvider stats,
                              IVolumeDataSet volume)
        {
            _coords = coords;
            _stats  = stats;
            _volume = volume;
        }

        public void PopulateFromTable(FeatureSet target, FeatureTable table, FeatureImportMapping mapping)
        {
            if (target  == null) throw new ArgumentNullException(nameof(target));
            if (table   == null) throw new ArgumentNullException(nameof(table));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            var assignments = mapping.ColumnAssignments;
            var columnNames = table.Columns.Select(c => c.Name).ToArray();
            int Find(SourceMappingOptions key)
                => assignments.TryGetValue(key, out var name) ? Array.IndexOf(columnNames, name) : -1;

            // Centroid projector — cartesian or sky-coord, dispatched via Strategy. Sky paths
            // call ICoordinateTransformer.PixelOf(WorldCoord) (the inverse of Transform), which
            // replaces the legacy AstTool.Invert + Transform3D walk in SpawnFeaturesFromTable.
            ICentroidProjector projector;
            if (assignments.ContainsKey(SourceMappingOptions.X))
            {
                projector = new CartesianProjector(Find(SourceMappingOptions.X),
                                                   Find(SourceMappingOptions.Y),
                                                   Find(SourceMappingOptions.Z));
            }
            else if (assignments.ContainsKey(SourceMappingOptions.Ra))
            {
                var raIdx  = Find(SourceMappingOptions.Ra);
                var decIdx = Find(SourceMappingOptions.Dec);
                // Pick whichever spectral column was mapped; SpectralUnit is read off the
                // table column header so the WCS engine can route Velo/Freq/Redshift through
                // the right alternate-spectral frame.
                int specIdx; string specUnit;
                if (assignments.ContainsKey(SourceMappingOptions.Velo))
                {
                    specIdx = Find(SourceMappingOptions.Velo);
                    specUnit = table.Columns[specIdx].Unit;
                }
                else if (assignments.ContainsKey(SourceMappingOptions.Freq))
                {
                    specIdx = Find(SourceMappingOptions.Freq);
                    specUnit = table.Columns[specIdx].Unit;
                }
                else if (assignments.ContainsKey(SourceMappingOptions.Redshift))
                {
                    specIdx = Find(SourceMappingOptions.Redshift);
                    specUnit = "REDSHIFT";
                }
                else
                {
                    throw new InvalidOperationException(
                        "Ra/Dec mapping requires one of Velo, Freq, or Redshift for the spectral axis.");
                }
                projector = new SkyProjector(raIdx, decIdx, specIdx, specUnit, _coords);
            }
            else
            {
                projector = null;
            }

            // Box-bounds projector — overrides centroid-derived 1×1×1 size when all six columns present.
            BoxProjector boxes = null;
            if (assignments.ContainsKey(SourceMappingOptions.Xmin))
            {
                boxes = new BoxProjector(
                    Find(SourceMappingOptions.Xmin), Find(SourceMappingOptions.Xmax),
                    Find(SourceMappingOptions.Ymin), Find(SourceMappingOptions.Ymax),
                    Find(SourceMappingOptions.Zmin), Find(SourceMappingOptions.Zmax));
            }

            if (projector == null && boxes == null)
                throw new InvalidOperationException(
                    "Mapping must assign either spatial coordinates (X/Y/Z) or bounding-box columns (Xmin..Zmax).");

            var nameIndex = Find(SourceMappingOptions.ID);
            var flagIndex = Find(SourceMappingOptions.Flag);
            var extents   = _volume.Extents;
            var keepMask  = mapping.ColumnMask;

            var buffer = new List<IFeature>(table.Rows.Count);
            for (var row = 0; row < table.Rows.Count; row++)
            {
                var values = table.Rows[row];

                CartesianCoord center, size;
                if (boxes != null)
                {
                    var (min, max) = boxes.Read(values);
                    center = new CartesianCoord((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
                    size   = new CartesianCoord(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
                }
                else
                {
                    center = projector.Project(values);
                    size   = new CartesianCoord(1, 1, 1);
                }

                if (mapping.ExcludeExternal && !InsideExtents(center, extents))
                    continue;

                var rawValues = CollectRawValues(values, keepMask);
                var name = nameIndex >= 0 ? values[nameIndex] : $"Source #{row + 1}";
                var flag = flagIndex >= 0 ? values[flagIndex] : string.Empty;

                buffer.Add(new Feature(originId: row, name, flag, center, size, rawValues));
            }

            target.AddFeatures(buffer);
        }

        private static bool InsideExtents(CartesianCoord c, VolumeExtents e)
            => c.X >= 0 && c.X <= e.NAxis1
            && c.Y >= 0 && c.Y <= e.NAxis2
            && c.Z >= 0 && c.Z <= e.NAxis3;

        private static IReadOnlyList<string> CollectRawValues(
            IReadOnlyList<string> row, IReadOnlyList<bool> keepMask)
        {
            if (keepMask == null || keepMask.Count == 0)
                return row.ToArray();
            var kept = new List<string>(row.Count);
            for (var i = 0; i < row.Count; i++)
                if (i < keepMask.Count && keepMask[i]) kept.Add(row[i]);
            return kept;
        }

        // ── Centroid / box projectors (Strategy) ───────────────────────────

        private interface ICentroidProjector
        {
            CartesianCoord Project(IReadOnlyList<string> row);
        }

        private sealed class CartesianProjector : ICentroidProjector
        {
            private readonly int _ix, _iy, _iz;
            public CartesianProjector(int ix, int iy, int iz) { _ix = ix; _iy = iy; _iz = iz; }
            public CartesianCoord Project(IReadOnlyList<string> row)
                => new(ParseInt(row[_ix]), ParseInt(row[_iy]), ParseInt(row[_iz]));
        }

        /// <summary>Ra/Dec + spectral → voxel via ICoordinateTransformer.PixelOf.
        /// Replaces the legacy AstTool.Invert + Transform3D walk. Ra/Dec values are
        /// parsed as degrees and converted to radians to match the WorldCoord
        /// convention shared with ST2 (see WorldCoord.RightAscension docstring).</summary>
        private sealed class SkyProjector : ICentroidProjector
        {
            private readonly int _raIdx, _decIdx, _specIdx;
            private readonly string _specUnit;
            private readonly ICoordinateTransformer _coords;
            public SkyProjector(int raIdx, int decIdx, int specIdx, string specUnit,
                                ICoordinateTransformer coords)
            {
                _raIdx = raIdx; _decIdx = decIdx; _specIdx = specIdx;
                _specUnit = specUnit; _coords = coords;
            }
            public CartesianCoord Project(IReadOnlyList<string> row)
            {
                var raDeg  = ParseDouble(row[_raIdx]);
                var decDeg = ParseDouble(row[_decIdx]);
                var spec   = ParseDouble(row[_specIdx]);
                var world  = new WorldCoord(
                    RightAscension: Math.PI * raDeg  / 180.0,
                    Declination:    Math.PI * decDeg / 180.0,
                    SpectralValue:  spec,
                    SpectralUnit:   _specUnit ?? string.Empty);
                return _coords.PixelOf(world);
            }
        }

        private sealed class BoxProjector
        {
            private readonly int _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;
            public BoxProjector(int xMin, int xMax, int yMin, int yMax, int zMin, int zMax)
            { _xMin = xMin; _xMax = xMax; _yMin = yMin; _yMax = yMax; _zMin = zMin; _zMax = zMax; }
            public (CartesianCoord Min, CartesianCoord Max) Read(IReadOnlyList<string> row)
                => (new CartesianCoord(ParseInt(row[_xMin]), ParseInt(row[_yMin]), ParseInt(row[_zMin])),
                    new CartesianCoord(ParseInt(row[_xMax]), ParseInt(row[_yMax]), ParseInt(row[_zMax])));
        }

        private static int ParseInt(string s)
            => (int)Math.Round(ParseDouble(s));

        private static double ParseDouble(string s)
            => double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);

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
