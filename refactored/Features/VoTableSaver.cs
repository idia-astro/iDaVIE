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

using iDaVIE.Data;                       // ISourceStatsProvider, ICoordinateTransformer

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
            // 1. Open an XmlWriter (UTF-8, indented) on filePath.
            // 2. Emit VOTable header from featureSet.RawDataKeys + a fixed schema
            //    for Name/Flag/Center/Size/BoxMin/BoxMax/Statistics where applicable.
            // 3. For each feature in featureSet.Features:
            //      a. Sky coords via _coords.Transform(feature.Center)
            //         (replaces the legacy AstTool reach).
            //      b. If feature.Statistics != null, append the eight stats columns.
            //      c. Append raw-column values.
            // 4. Close the document.
            // TODO: full implementation; behaviour matches legacy VoTableSaver.SaveFeatureSetAsVoTable.
        }
    }
}
