/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * Before state.
 *
 * This file is an annotated copy of the production VoTableSaver class found in
 * Assets/Scripts/FeatureData/VoTable.cs (lines 428 to 488). No production code has
 * been modified. The annotations use the prefix SMELL to mark each design problem
 * and name the CK metric or SOLID principle it breaks.
 *
 * Problems catalogued here:
 *   [1] Static class: no polymorphism, not injectable, not mockable
 *   [2] Unity MonoBehaviour parameter: couples the exporter to the UI layer
 *   [3] Law of Demeter: navigates 3+ levels into Unity objects
 *   [4] Direct DLL coupling: calls AstTool.Transform3D / AstTool.Norm inline
 *   [5] No export seam: can't produce FITS or JSON-LD without edits
 *   [6] Wrong namespace: export logic lives inside VoTableReader (the reading layer)
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DataFeatures;
using UnityEngine; // SMELL [2][3]: Unity import; export logic depends on the runtime

// SMELL [6]: SaveFeatureSetAsVoTable lives in VoTableReader, a reading namespace.
//            Export and import responsibilities are collapsed into the same file.
namespace VoTableReader
{
    // SMELL [1]: Static class, so it can't be injected, subclassed, or replaced.
    //            There is no IVoTableExporter interface; callers depend directly
    //            on this concrete type, breaking the Dependency Inversion Principle.
    public static class VoTableSaver
    {
        // SMELL [2]: The first parameter is a FeatureSetRenderer, a MonoBehaviour,
        //            so the method only runs inside a live Unity scene and can't be
        //            tested in isolation.
        //
        // SMELL [5]: The signature (FeatureSetRenderer, string) gives no seam. To
        //            support FITS output a caller would have to modify this method
        //            or copy it, which breaks Open/Closed.
        public static void SaveFeatureSetAsVoTable(FeatureSetRenderer featureSet, string filePath)
        {
            // SMELL [3]: featureSet.VolumeRenderer.Data.GetAstAttribute(...) is three
            //            levels of navigation into Unity components. FeatureCatalog
            //            should receive a plain FeatureSet domain object, not one that
            //            owns a VolumeDataSetRenderer.
            string zType = featureSet.VolumeRenderer.Data.GetAstAttribute("System(3)");

            List<string> sourceDataHeaders = new List<string>
            {
                "id", "x", "y", "z",
                "x_min", "x_max", "y_min", "y_max", "z_min", "z_max",
                "ra", "dec", zType
            };
            int initialHeaderCount = sourceDataHeaders.Count;
            sourceDataHeaders.Add("Flag (" + DateTime.Now.ToString("dd/MM/yy HH:mm") + ")");

            // SMELL [3]: featureSet.RawDataKeys read directly from MonoBehaviour.
            if (featureSet.RawDataKeys != null)
                sourceDataHeaders.AddRange(featureSet.RawDataKeys);

            XDocument doc = new XDocument(
                new XElement("VOTABLE",
                    new XElement("RESOURCE", new XAttribute("name", "iDaVIE catalogue"),
                        new XElement("DESCRIPTION", "Source data exported from iDaVIE"),
                        new XElement("COOSYS", new XAttribute("ID", "J2000")),
                        new XElement("TABLE",
                            new XAttribute("ID", "idavie_cat"),
                            new XAttribute("name", "idavie_cat"),
                            new XElement("DATA",
                                new XElement("TABLEDATA"))))));

            XElement[] xmlFields = new XElement[sourceDataHeaders.Count];
            for (int i = 0; i < sourceDataHeaders.Count; i++)
            {
                if (i < initialHeaderCount)
                    xmlFields[i] = new XElement("FIELD",
                        new XAttribute("datatype", "float"),
                        new XAttribute("name", sourceDataHeaders[i]));
                else
                    xmlFields[i] = new XElement("FIELD",
                        new XAttribute("arraysize", "30"),
                        new XAttribute("datatype", "char"),
                        new XAttribute("name", sourceDataHeaders[i]));
            }
            doc.Root.Element("RESOURCE").Element("TABLE").AddFirst(xmlFields);

            for (int i = 0; i < featureSet.FeatureList.Count; i++)
            {
                double centerX, centerY, centerZ, ra, dec, zPhys, normR, normD, normZ;
                Feature currentFeature = featureSet.FeatureList[i];

                // SMELL [3]: featureSet.VolumeRenderer.SourceStatsDict reads through
                //            two levels of Unity component to reach a dictionary that
                //            should have been passed in as a plain value.
                if (featureSet.VolumeRenderer.SourceStatsDict == null)
                {
                    centerX = currentFeature.Center.x;
                    centerY = currentFeature.Center.y;
                    centerZ = currentFeature.Center.z;
                }
                else
                {
                    centerX = featureSet.VolumeRenderer.SourceStatsDict
                                        .ElementAt(currentFeature.Index).Value.cX;
                    centerY = featureSet.VolumeRenderer.SourceStatsDict
                                        .ElementAt(currentFeature.Index).Value.cY;
                    centerZ = featureSet.VolumeRenderer.SourceStatsDict
                                        .ElementAt(currentFeature.Index).Value.cZ;
                }

                // SMELL [4]: Direct static call to AstTool.Transform3D. AstTool is a
                //            P/Invoke wrapper around DataAnalysis.dll, so VoTableSaver
                //            can't run without the native DLL and can't be stubbed for
                //            deterministic RA/Dec/Z values. CBO goes up by one for
                //            every DLL entry point used.
                AstTool.Transform3D(
                    featureSet.VolumeRenderer.AstFrame,
                    centerX, centerY, centerZ, 1,
                    out ra, out dec, out zPhys);

                // SMELL [4]: Same DLL coupling problem; AstTool.Norm is called inline.
                AstTool.Norm(
                    featureSet.VolumeRenderer.AstFrame,
                    ra, dec, zPhys,
                    out normR, out normD, out normZ);

                XElement voRow = new XElement("TR",
                    new XElement("TD", (currentFeature.Id + 1).ToString()),
                    new XElement("TD", currentFeature.Center.x.ToString()),
                    new XElement("TD", currentFeature.Center.y.ToString()),
                    new XElement("TD", currentFeature.Center.z.ToString()),
                    new XElement("TD", currentFeature.CornerMin.x.ToString()),
                    new XElement("TD", currentFeature.CornerMax.x.ToString()),
                    new XElement("TD", currentFeature.CornerMin.y.ToString()),
                    new XElement("TD", currentFeature.CornerMax.y.ToString()),
                    new XElement("TD", currentFeature.CornerMin.z.ToString()),
                    new XElement("TD", currentFeature.CornerMax.z.ToString()),
                    new XElement("TD", (180f * normR / Math.PI).ToString()),
                    new XElement("TD", (180f * normD / Math.PI).ToString()),
                    new XElement("TD", (1000 * normZ).ToString()),
                    new XElement("TD", currentFeature.Flag));

                for (int j = 0; j < currentFeature.RawData.Length; j++)
                    voRow.Add(new XElement("TD", currentFeature.RawData[j]));

                doc.Root
                   .Element("RESOURCE")
                   .Element("TABLE")
                   .Element("DATA")
                   .Element("TABLEDATA")
                   .Add(voRow);
            }

            // SMELL [1][5]: doc.Save writes straight to disk with no abstraction over
            //               the output stream, so output can't be redirected to memory,
            //               the network, or another format without rewriting this method.
            doc.Save(filePath);
        }
    }
}
