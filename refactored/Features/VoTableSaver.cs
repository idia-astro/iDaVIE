// SPDX-License-Identifier: LGPL-3.0-or-later
// VoTableSaver — replaces the legacy static VoTableSaver + the
// FeatureSetRenderer.SaveAsVoTable wrapper. Realises IFeatureCatalogueWriter.
//
// Refactor delta:
//   - Constructor-injected dependencies replace static native-DLL reach.
//   - Holds ISourceStatsProvider (flux-weighted centroids) and
//     ICoordinateTransformer (pixel → sky). DD-4 justifies the separate
//     writer interface — only writers need these dependencies.
//   - No UnityEngine reference; pure I/O.

using System;
using System.Globalization;
using System.Xml.Linq;
using iDaVIE.Data;                       // ICoordinateTransformer, WorldCoord
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord
// ISourceStatsProvider lives in iDaVIE.Features (this namespace) per shared_interfaces.md §5.5.

namespace iDaVIE.Features
{
    internal sealed class VoTableSaver : IFeatureCatalogueWriter
    {
        private readonly ISourceStatsProvider   _stats;
        private readonly ICoordinateTransformer _coords;

        public VoTableSaver(ISourceStatsProvider stats, ICoordinateTransformer coords)
        {
            _stats  = stats;
            _coords = coords;
        }

        public void Write(IFeatureSet featureSet, string filePath)
        {
            if (featureSet == null) throw new ArgumentNullException(nameof(featureSet));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath must not be empty.", nameof(filePath));

            // Header columns mirror the legacy VoTableSaver layout. ZUnit comes from
            // whatever the first feature transforms into — the WCS engine reports the
            // active spectral unit per WorldCoord.SpectralUnit; empty if not yet known.
            var zUnit = SampleSpectralUnit(featureSet) ?? string.Empty;
            var fixedHeaders = new[]
            {
                "id", "x", "y", "z",
                "x_min", "x_max", "y_min", "y_max", "z_min", "z_max",
                "ra", "dec", string.IsNullOrEmpty(zUnit) ? "spectral" : zUnit,
                $"Flag ({DateTime.UtcNow:dd/MM/yy HH:mm})",
            };

            var table = new XElement("TABLE",
                new XAttribute("ID", "idavie_cat"),
                new XAttribute("name", "idavie_cat"));

            foreach (var name in fixedHeaders)
                table.Add(new XElement("FIELD",
                    new XAttribute("datatype", "char"),
                    new XAttribute("arraysize", "*"),
                    new XAttribute("name", name)));
            foreach (var raw in featureSet.RawDataKeys)
                table.Add(new XElement("FIELD",
                    new XAttribute("datatype", "char"),
                    new XAttribute("arraysize", "*"),
                    new XAttribute("name", raw)));

            var tableData = new XElement("TABLEDATA");
            foreach (var feature in featureSet.Features)
            {
                // Prefer flux-weighted centroid from ST2's stats for Mask features;
                // fall back to feature.Center otherwise (matches legacy behaviour).
                var statsCentroid = featureSet.Type == FeatureSetType.Mask
                    ? _stats.GetStatsForSource(feature.OriginId)?.FluxWeightedCentroid
                    : null;
                var centroid = statsCentroid ?? feature.Center;
                var world    = _coords.Transform(centroid);

                var hx = feature.Size.X / 2;
                var hy = feature.Size.Y / 2;
                var hz = feature.Size.Z / 2;

                var row = new XElement("TR",
                    Td(feature.OriginId + 1),
                    Td(feature.Center.X), Td(feature.Center.Y), Td(feature.Center.Z),
                    Td(feature.Center.X - hx), Td(feature.Center.X + hx),
                    Td(feature.Center.Y - hy), Td(feature.Center.Y + hy),
                    Td(feature.Center.Z - hz), Td(feature.Center.Z + hz),
                    Td(world.IsValid ? world.RightAscension * 180.0 / Math.PI : double.NaN),
                    Td(world.IsValid ? world.Declination    * 180.0 / Math.PI : double.NaN),
                    Td(world.IsValid ? world.SpectralValue                    : double.NaN),
                    Td(feature.Flag ?? string.Empty));

                foreach (var rawValue in feature.RawDataValues)
                    row.Add(new XElement("TD", rawValue ?? string.Empty));

                tableData.Add(row);
            }

            table.Add(new XElement("DATA", tableData));

            var doc = new XDocument(new XElement("VOTABLE",
                new XElement("RESOURCE",
                    new XAttribute("name", "iDaVIE catalogue"),
                    new XElement("DESCRIPTION", "Source data exported from iDaVIE"),
                    new XElement("COOSYS", new XAttribute("ID", "J2000")),
                    table)));

            doc.Save(filePath);
        }

        private string SampleSpectralUnit(IFeatureSet set)
        {
            if (set.Features.Count == 0) return null;
            var world = _coords.Transform(set.Features[0].Center);
            return world.IsValid ? world.SpectralUnit : null;
        }

        private static XElement Td(object value)
            => new("TD", Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}
