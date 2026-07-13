/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * VoTableExportService.cs (after state, new file, design-level example)
 *
 * The concrete implementation of IVoTableExporter, in
 * iDaVIE.Infrastructure.Persistence (ADR-008). XML serialisation is an
 * infrastructure concern, so it can't sit in iDaVIE.Domain.Feature without
 * pointing the dependency the wrong way. From here it depends inward on the
 * domain interfaces (IVoTableExporter, ICoordinateTransformer, FeatureSet,
 * Feature), which is the right direction per ADR-001. It imports no UnityEngine
 * (ADR-002) and works only on the iDaVIE.Domain.Feature model and
 * System.Xml.Linq, so it can be tested without a running Unity instance.
 *
 * Compared with the old VoTableSaver:
 *   - was a static class; now implements IVoTableExporter
 *   - took a FeatureSetRenderer (Unity); now takes a plain FeatureSet
 *   - called AstTool.* directly; now calls an injected ICoordinateTransformer
 *   - passed an IntPtr into a DLL wrapper; now passes IAstFrame, no unsafe types
 *   - read VolumeRenderer.SourceStats; now reads FeatureSet.Features
 *   - wrote to file (doc.Save); now returns an XML string for the caller to write
 *   - lived in namespace VoTableReader; now in iDaVIE.Infrastructure.Persistence
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using iDaVIE.Domain.Feature;

// No "using UnityEngine" here on purpose: the class stays Unity-free (ADR-002).

namespace iDaVIE.Infrastructure.Persistence
{
    /// <summary>
    /// VOTable 1.3 XML serialiser.
    /// <para>
    /// Implements <see cref="IVoTableExporter"/> and depends only on
    /// <see cref="ICoordinateTransformer"/> for WCS conversion and the
    /// <see cref="IAstFrame"/> domain handle for the coordinate frame.
    /// It references no Unity types, so it can be unit tested directly.
    /// </para>
    /// <para>
    /// Injected into <see cref="IFeaturePersistenceService"/> at the
    /// composition root. The persistence service calls
    /// <see cref="Export"/> and writes the returned string to disk.
    /// </para>
    /// </summary>
    public sealed class VoTableExportService : IVoTableExporter
    {
        /// <inheritdoc/>
        public string Export(FeatureSet featureSet, ICoordinateTransformer transformer)
        {
            if (featureSet  == null) throw new ArgumentNullException(nameof(featureSet));
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));

            // 1. Build column headers.
            var headers = BuildHeaders(featureSet);

            // 2. Build the VOTABLE skeleton.
            XDocument doc = BuildDocument(headers, featureSet);

            // 3. Populate the data rows.
            XElement tableData = doc.Root
                                    .Element("RESOURCE")
                                    .Element("TABLE")
                                    .Element("DATA")
                                    .Element("TABLEDATA");

            foreach (Feature feature in featureSet.Features)
            {
                // featureSet.AstFrame is an IAstFrame, so there's no IntPtr in this
                // layer. The infrastructure AstFrameHandle wraps the real IntPtr;
                // VoTableExportService never sees it.
                transformer.Transform(
                    featureSet.AstFrame,
                    feature.Center.X, feature.Center.Y, feature.Center.Z,
                    out double ra, out double dec, out double zPhys);

                transformer.Normalise(
                    featureSet.AstFrame,
                    ra, dec, zPhys,
                    out double normRa, out double normDec, out double normZ);

                tableData.Add(BuildRow(feature, normRa, normDec, normZ));
            }

            // 4. Return the XML string; the caller writes it to disk.
            return doc.ToString();
        }

        // Private helpers

        /// <summary>
        /// Returns the ordered list of column names for the FIELD elements.
        /// Fixed positional columns are followed by the set's RawData keys.
        /// </summary>
        private static List<string> BuildHeaders(FeatureSet featureSet)
        {
            var headers = new List<string>
            {
                "id", "x", "y", "z",
                "x_min", "x_max", "y_min", "y_max", "z_min", "z_max",
                "ra", "dec", featureSet.ZAxisLabel,
                "Flag (" + DateTime.Now.ToString("dd/MM/yy HH:mm") + ")"
            };

            if (featureSet.RawDataKeys != null)
                headers.AddRange(featureSet.RawDataKeys);

            return headers;
        }

        /// <summary>
        /// Builds the VOTABLE / RESOURCE / TABLE structure with FIELD declarations
        /// but an empty TABLEDATA element ready for row insertion.
        /// </summary>
        private static XDocument BuildDocument(List<string> headers, FeatureSet featureSet)
        {
            int fixedColumnCount = headers.Count
                                   - (featureSet.RawDataKeys?.Length ?? 0);

            var fieldElements = new XElement[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                fieldElements[i] = i < fixedColumnCount
                    ? new XElement("FIELD",
                        new XAttribute("datatype", "float"),
                        new XAttribute("name", headers[i]))
                    : new XElement("FIELD",
                        new XAttribute("arraysize", "30"),
                        new XAttribute("datatype", "char"),
                        new XAttribute("name", headers[i]));
            }

            var tableElement = new XElement("TABLE",
                new XAttribute("ID",   "idavie_cat"),
                new XAttribute("name", "idavie_cat"),
                new XElement("DATA", new XElement("TABLEDATA")));

            tableElement.AddFirst(fieldElements);

            return new XDocument(
                new XElement("VOTABLE",
                    new XElement("RESOURCE", new XAttribute("name", "iDaVIE catalogue"),
                        new XElement("DESCRIPTION", "Source data exported from iDaVIE"),
                        new XElement("COOSYS", new XAttribute("ID", "J2000")),
                        tableElement)));
        }

        /// <summary>
        /// Builds a single TR element from the domain <see cref="Feature"/>
        /// and pre-computed normalised coordinates.
        /// </summary>
        private static XElement BuildRow(
            Feature feature,
            double normRa, double normDec, double normZ)
        {
            double raDeg = 180.0 * normRa  / Math.PI;
            double decDeg = 180.0 * normDec / Math.PI;
            double zKms   = 1000.0 * normZ;

            var row = new XElement("TR",
                new XElement("TD", (feature.Id + 1).ToString()),
                new XElement("TD", feature.Center.X.ToString()),
                new XElement("TD", feature.Center.Y.ToString()),
                new XElement("TD", feature.Center.Z.ToString()),
                new XElement("TD", feature.CornerMin.X.ToString()),
                new XElement("TD", feature.CornerMax.X.ToString()),
                new XElement("TD", feature.CornerMin.Y.ToString()),
                new XElement("TD", feature.CornerMax.Y.ToString()),
                new XElement("TD", feature.CornerMin.Z.ToString()),
                new XElement("TD", feature.CornerMax.Z.ToString()),
                new XElement("TD", raDeg.ToString()),
                new XElement("TD", decDeg.ToString()),
                new XElement("TD", zKms.ToString()),
                new XElement("TD", feature.Flag));

            if (feature.RawData != null)
            {
                foreach (string value in feature.RawData)
                    row.Add(new XElement("TD", value));
            }

            return row;
        }
    }
}
