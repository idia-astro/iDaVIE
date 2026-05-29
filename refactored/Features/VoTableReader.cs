// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// VoTableReader — Infrastructure realisation of IFeatureCatalogueReader for
// VOTable (.xml) files. Replaces the static
//   FeatureTable.GetFeatureTableFromFile → VoTable.GetVOTableFromFile → GetFeatureTableFromVoTable
// chain that lives in the legacy DataFeatures namespace.
//
// Refactor delta:
//   - Reads only — no ISourceStatsProvider / ICoordinateTransformer dependencies
//     (DD-4: writers need those, readers do not).
//   - Returns a boundary FeatureTable (the readonly value type from
//     IFeatureImportExport.cs); the legacy mutable FeatureTable / FeatureRow /
//     FeatureColumn class triple is dropped.
//   - No UnityEngine reference; pure XML I/O. Unit-testable with a sample
//     VOTable string fed via a temp file (or refactored to TextReader if the
//     test harness needs to skip file I/O).
//
// Held by:
//   - FeatureImportService (composition-root injection; the import flow uses
//     the .xml branch of the legacy FeatureTable dispatch).
//   - Optionally by ST4 if an interaction-driven import path is wired up
//     directly (ST5_interface.md §1 — IFeatureCatalogueReader is exposed to
//     ST4 as an "optional" port).

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace iDaVIE.Features
{
    internal sealed class VoTableReader : IFeatureCatalogueReader
    {
        public FeatureTable Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath must not be empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var doc = new XmlDocument();
            doc.Load(filePath);

            var voTable = doc["VOTABLE"]
                ?? throw new InvalidOperationException(
                    $"File '{filePath}' is not a VOTable (missing <VOTABLE> root).");

            var tableNode = voTable["RESOURCE"]?["TABLE"]
                ?? throw new InvalidOperationException(
                    $"VOTable '{filePath}' has no <RESOURCE><TABLE> element.");

            // ── FIELDs → columns ───────────────────────────────────────────
            // Verbatim port of legacy VoTable.LoadFromXML field-iteration loop.
            // FeatureColumnInfo carries Ucd alongside Name/Unit/Datatype so ST6's
            // column-mapping UI can auto-suggest SourceMappingOptions assignments
            // (ST5_refactoring_proposal.md "Boundary value types").
            var columns = new List<FeatureColumnInfo>();
            foreach (XmlNode child in tableNode.ChildNodes)
            {
                if (child.Name != "FIELD") continue;
                columns.Add(new FeatureColumnInfo(
                    Name:     child.Attributes?["name"    ]?.Value ?? string.Empty,
                    Unit:     child.Attributes?["unit"    ]?.Value ?? string.Empty,
                    DataType: child.Attributes?["datatype"]?.Value ?? string.Empty,
                    Ucd:      child.Attributes?["ucd"     ]?.Value ?? string.Empty));
            }

            // ── DATA/TABLEDATA/TR → rows ───────────────────────────────────
            // Verbatim port of legacy VoTable row-parsing — all values are strings,
            // matching FeatureTable.Rows : IReadOnlyList<IReadOnlyList<string>>.
            var rows = new List<IReadOnlyList<string>>();
            var dataNode = tableNode["DATA"]?["TABLEDATA"];
            if (dataNode != null)
            {
                foreach (XmlNode trNode in dataNode.ChildNodes)
                {
                    if (trNode.Name != "TR") continue;
                    var values = new List<string>(columns.Count);
                    foreach (XmlNode td in trNode.ChildNodes)
                        if (td.Name == "TD") values.Add(td.InnerText);
                    rows.Add(values);
                }
            }

            return new FeatureTable { Columns = columns, Rows = rows };
        }
    }
}
