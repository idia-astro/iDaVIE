// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureImportService — orchestrates the Imported-set creation flow that was
// previously interleaved into FeatureSetRenderer.SpawnFeaturesFromTable.
// Realises IFeatureImportService (cross-team).
//
// ISP trade-off (DD-10): mapping persistence sits on the same interface as
// import because callers always pair the two actions.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;     // FeatureColour

namespace iDaVIE.Features
{
    internal sealed class FeatureImportService : IFeatureImportService
    {
        private readonly IFeatureCatalogueReader _reader;
        private readonly IFeatureSetCatalog      _catalog;
        private readonly IVisualiserBinder       _binder;
        private readonly IFeatureFactory         _factory;

        public FeatureImportService(IFeatureCatalogueReader reader,
                                    IFeatureSetCatalog catalog,
                                    IVisualiserBinder binder,
                                    IFeatureFactory factory)
        {
            _reader  = reader;
            _catalog = catalog;
            _binder  = binder;
            _factory = factory;
        }

        public IReadOnlyList<FeatureColumnInfo> GetColumns(string filePath)
            => _reader.Read(filePath).Columns;

        public void ImportFromFile(string filePath, FeatureImportMapping mapping)
        {
            var table = _reader.Read(filePath);

            // §6.3 construction sequence:
            //   1. Reserve a listener slot (binder needs the set; we create the set first
            //      with a placeholder listener and rebind, OR — cleaner — the binder is
            //      called with the partly-built set in a two-phase init. The proposal's
            //      worked example uses the two-phase path.)
            //   2. CreateSet (raises FeatureSetChanged — empty-set event).
            //   3. Bind the visualiser.
            //   4. PopulateFromTable.
            //   5. Raise FeatureSetChanged a second time (bulk-population rule, §6).
            // TODO: implement two-phase binder init as per ST5 proposal §6.3.

            FeatureSet set = null;   // <- built by CreateSet once listener resolved
            _factory.PopulateFromTable(set, table, mapping);
        }

        public FeatureImportMapping LoadMappingFromFile(string mappingFilePath)
        {
            // TODO: JSON deserialise from the same shape SaveMappingToFile emits.
            return null;
        }

        public void SaveMappingToFile(FeatureImportMapping mapping, string mappingFilePath)
        {
            // TODO: JSON serialise the mapping. No Unity dependency.
        }
    }
}
