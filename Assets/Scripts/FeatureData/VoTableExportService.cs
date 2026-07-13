using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DataFeatures;
using iDaVIE.Domain.Feature;

namespace iDaVIE.Infrastructure.Persistence
{
    /// <summary>
    /// VOTable 1.3 XML serialiser with no Unity dependencies. Depends only on the
    /// domain types (FeatureSet, Feature) and ICoordinateTransformer.
    /// </summary>
    public sealed class VoTableExportService : IVoTableExporter
    {
        public string Export(FeatureSet featureSet, ICoordinateTransformer transformer)
        {
            if (featureSet  == null) throw new ArgumentNullException(nameof(featureSet));
            if (transformer == null) throw new ArgumentNullException(nameof(transformer));

            var headers  = BuildHeaders(featureSet);
            var doc      = BuildDocument(headers, featureSet);
            var tableData = doc.Root!
                               .Element("RESOURCE")!
                               .Element("TABLE")!
                               .Element("DATA")!
                               .Element("TABLEDATA")!;

            foreach (Feature feature in featureSet.Features)
            {
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

            return doc.ToString();
        }

        private static List<string> BuildHeaders(FeatureSet featureSet)
        {
            var headers = new List<string>
            {
                "id", "x", "y", "z",
                "x_min", "x_max", "y_min", "y_max", "z_min", "z_max",
                "ra", "dec", featureSet.ZAxisLabel,
                "Flag"
            };

            if (featureSet.RawDataKeys != null)
                headers.AddRange(featureSet.RawDataKeys);

            return headers;
        }

        private static XDocument BuildDocument(List<string> headers, FeatureSet featureSet)
        {
            int fixedCount = headers.Count - (featureSet.RawDataKeys?.Length ?? 0);

            var fields = new XElement[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                fields[i] = i < fixedCount
                    ? new XElement("FIELD",
                        new XAttribute("datatype", "float"),
                        new XAttribute("name", headers[i]))
                    : new XElement("FIELD",
                        new XAttribute("arraysize", "30"),
                        new XAttribute("datatype", "char"),
                        new XAttribute("name", headers[i]));
            }

            var table = new XElement("TABLE",
                new XAttribute("ID",   "idavie_cat"),
                new XAttribute("name", "idavie_cat"),
                new XElement("DATA", new XElement("TABLEDATA")));

            table.AddFirst(fields);

            return new XDocument(
                new XElement("VOTABLE",
                    new XElement("RESOURCE", new XAttribute("name", "iDaVIE catalogue"),
                        new XElement("DESCRIPTION", "Source data exported from iDaVIE"),
                        new XElement("COOSYS", new XAttribute("ID", "J2000")),
                        table)));
        }

        private static XElement BuildRow(Feature feature, double normRa, double normDec, double normZ)
        {
            double raDeg  = 180.0 * normRa  / Math.PI;
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
                foreach (string value in feature.RawData)
                    row.Add(new XElement("TD", value));

            return row;
        }
    }
}
