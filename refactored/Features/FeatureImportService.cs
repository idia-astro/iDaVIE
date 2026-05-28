// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// FeatureImportService — orchestrates file-based import of Imported feature sets.
// Realises IFeatureImportService (cross-team, provided to ST6).
// Internal sealed per DD-5.
//
// Also owns column-mapping persistence (LoadMappingFromFile / SaveMappingToFile).
// ISP trade-off accepted — DD-10: the caller always pairs mapping persistence and
// import in the same workflow; splitting would increase constructor surface area
// with no reduction in coupling.
//
// Runs the §6.3 five-step construction sequence (ST5_domain_design.md §6.3):
//   1. CreateListener  — FeatureVisualiser with no set yet; dirty events buffered.
//   2. CreateSet       — empty FeatureSet registered; FeatureSetChanged fires (event 1).
//   3. AttachToSet     — visualiser receives set reference; drains buffer.
//   4. PopulateFromTable — factory parses rows, transforms coords, attaches features.
//   5. NotifySetPopulated — FeatureSetChanged fires again (event 2); consumers re-query.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using iDaVIE.Kernel.Contracts.Types;   // FeatureColour

namespace iDaVIE.Features
{
    internal sealed class FeatureImportService : IFeatureImportService, IImportRestorer
    {
        private readonly IFeatureCatalogueReader _reader;
        private readonly IFeatureSetCatalog      _catalog;
        private readonly IVisualiserBinder       _binder;
        private readonly IFeatureFactory         _factory;
        private readonly IFeatureSetQuery        _query;

        public FeatureImportService(IFeatureCatalogueReader reader,
                                    IFeatureSetCatalog catalog,
                                    IVisualiserBinder binder,
                                    IFeatureFactory factory,
                                    IFeatureSetQuery query)
        {
            _reader  = reader;
            _catalog = catalog;
            _binder  = binder;
            _factory = factory;
            _query   = query;
        }

        // ── IFeatureImportService ───────────────────────────────────────────

        public IReadOnlyList<FeatureColumnInfo> GetColumns(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath must not be empty.", nameof(filePath));
            // Delegates to _reader — no set created, no event fired (precondition row).
            return _reader.Read(filePath).Columns;
        }

        public void ImportFromFile(string filePath, FeatureImportMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath must not be empty.", nameof(filePath));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

            var table = _reader.Read(filePath);

            if (mapping.ColumnMask != null && mapping.ColumnMask.Count != table.Columns.Count)
                throw new ArgumentException(
                    $"mapping.ColumnMask.Count ({mapping.ColumnMask.Count}) must equal " +
                    $"the column count ({table.Columns.Count}).", nameof(mapping));

            // ── §6.3 construction sequence ──────────────────────────────────
            // Step 1: instantiate FeatureVisualiser with no set yet; dirty events buffered.
            var listener = _binder.CreateListener();

            // Step 2: register empty FeatureSet; FeatureSetChanged fires (event 1 of 2).
            var rawDataKeys = ResolveDisplayedKeys(table, mapping);
            var set = _catalog.CreateSet(
                fileName:    string.IsNullOrEmpty(mapping.SetName)
                                 ? Path.GetFileName(filePath)
                                 : mapping.SetName,
                type:        FeatureSetType.Imported,
                rawDataKeys: rawDataKeys,
                colour:      mapping.DisplayColour,
                listener:    listener);

            // Step 3: hand set reference to visualiser; drains buffered dirty queue.
            _binder.AttachToSet(listener, set);

            // Step 4: parse rows, transform WCS coords, attach Feature objects via
            //         FeatureSet.AddFeatures (bulk — one GPU dirty signal per batch).
            _factory.PopulateFromTable(set, table, mapping);

            // Record provenance so IFeatureStateCapture.Capture can persist this set
            // as (file path + mapping) rather than inlining features.
            _catalog.RecordImportProvenance(set, filePath, mapping);

            // Step 5: FeatureSetChanged fires (event 2 of 2); consumers re-query the
            //         now-populated set (bulk-population rule, ST5_interface.md §6).
            _catalog.NotifySetPopulated();
        }

        public FeatureImportMapping LoadMappingFromFile(string mappingFilePath)
        {
            if (string.IsNullOrWhiteSpace(mappingFilePath))
                throw new ArgumentException("mappingFilePath must not be empty.", nameof(mappingFilePath));
            if (!File.Exists(mappingFilePath))
                throw new FileNotFoundException(mappingFilePath);

            using var stream = File.OpenRead(mappingFilePath);
            var dto = JsonSerializer.Deserialize<FeatureImportMappingDto>(stream, JsonOpts)
                      ?? throw new InvalidOperationException("Mapping file deserialised to null.");
            return dto.ToMapping();
        }

        public void SaveMappingToFile(FeatureImportMapping mapping, string mappingFilePath)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (string.IsNullOrWhiteSpace(mappingFilePath))
                throw new ArgumentException("mappingFilePath must not be empty.", nameof(mappingFilePath));

            using var stream = File.Create(mappingFilePath);
            JsonSerializer.Serialize(stream, FeatureImportMappingDto.From(mapping), JsonOpts);
        }

        // ── IImportRestorer ─────────────────────────────────────────────────

        public void RestoreImported(string filePath, FeatureImportMapping mapping,
                                    FeatureColour displayColour, bool isVisible)
        {
            ImportFromFile(filePath, mapping);

            // The set was just created via ImportFromFile — locate it and re-apply
            // the captured display state (which may have drifted from mapping.DisplayColour
            // if the user changed it after import).
            var imported = _query.GetFeatureSetsByType(FeatureSetType.Imported);
            var restored = imported[imported.Count - 1];
            _query.SetDisplayColour(restored, displayColour);
            _query.SetVisible(restored, isVisible);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Serialisation envelope. FeatureImportMapping holds IReadOnlyDictionary /
        /// IReadOnlyList which System.Text.Json cannot deserialise directly; the DTO uses
        /// concrete Dictionary / List and converts on the boundary.</summary>
        private sealed class FeatureImportMappingDto
        {
            public Dictionary<SourceMappingOptions, string> ColumnAssignments { get; set; } = new();
            public List<bool>    ColumnMask      { get; set; } = new();
            public bool          ExcludeExternal { get; set; }
            public string        SetName         { get; set; } = string.Empty;
            public FeatureColour DisplayColour   { get; set; }

            public static FeatureImportMappingDto From(FeatureImportMapping m) => new()
            {
                ColumnAssignments = new Dictionary<SourceMappingOptions, string>(m.ColumnAssignments),
                ColumnMask        = new List<bool>(m.ColumnMask),
                ExcludeExternal   = m.ExcludeExternal,
                SetName           = m.SetName,
                DisplayColour     = m.DisplayColour,
            };

            public FeatureImportMapping ToMapping() => new()
            {
                ColumnAssignments = ColumnAssignments,
                ColumnMask        = ColumnMask,
                ExcludeExternal   = ExcludeExternal,
                SetName           = SetName,
                DisplayColour     = DisplayColour,
            };
        }

        /// <summary>Derives IFeatureSet.RawDataKeys from column headers filtered by
        /// mapping.ColumnMask. If ColumnMask is null or empty, all column names are used.</summary>
        private static IReadOnlyList<string> ResolveDisplayedKeys(
            FeatureTable table, FeatureImportMapping mapping)
        {
            var mask = mapping.ColumnMask;
            var keys = new List<string>();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (mask == null || mask.Count == 0 || mask[i])
                    keys.Add(table.Columns[i].Name);
            }
            return keys;
        }
    }
}
