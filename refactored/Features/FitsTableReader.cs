// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// FitsTableReader — Infrastructure realisation of IFeatureCatalogueReader for
// FITS binary-table / ASCII-table files (.fits, .fit). Replaces the .fits
// branch of the legacy FeatureTable.GetFeatureTableFromFile dispatch
// (FeatureTable.cs:103 — GetFeatureTableFromFits).
//
// Refactor delta:
//   - The legacy reader uses the static FitsReader P/Invoke wrapper directly.
//     This skeleton retains that boundary as IFitsBinaryTableSource so ST5's
//     reader is not coupled to the ST2-owned native DLL surface — see
//     ST5_domain_design.md §8.1 "ST5 never touches FitsReader … directly".
//   - Returns a boundary FeatureTable (the readonly value type from
//     IFeatureImportExport.cs). All cell values are stringified — matching the
//     legacy "Not a good way of doing this, but makes it compatible with current
//     VOTable functionality" decision (FeatureTable.cs:166). Reader output
//     parity with VoTableReader is intentional.
//   - No UnityEngine reference.
//
// Held by:
//   - FeatureImportService when an Imported set is loaded from a .fits / .fit
//     file. Composition root dispatches on file extension to pick the right
//     IFeatureCatalogueReader implementation.

using System;
using System.Collections.Generic;
using System.IO;

namespace iDaVIE.Features
{
    /// <summary>Narrow port over the CFITSIO binary-table surface so this reader
    /// can be unit-tested with a fake source. Realised in the ST2 plug-in adapter
    /// (wraps the legacy static FitsReader); the realisation is not part of the
    /// ST5 contract — see ST5_domain_design.md §8.1.</summary>
    internal interface IFitsBinaryTableSource
    {
        IReadOnlyList<FeatureColumnInfo> ReadColumns(string filePath);
        IReadOnlyList<IReadOnlyList<string>> ReadRows(string filePath, int columnCount);
    }

    internal sealed class FitsTableReader : IFeatureCatalogueReader
    {
        private readonly IFitsBinaryTableSource _source;

        public FitsTableReader(IFitsBinaryTableSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public FeatureTable Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath must not be empty.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            // Mirrors the legacy GetFeatureTableFromFits sequence: scan for the
            // first BinaryTbl/AsciiTbl HDU, enumerate columns, then read rows.
            // The "no table HDU" failure mode surfaces here rather than as the
            // legacy null return (FeatureTable.cs:124).
            var columns = _source.ReadColumns(filePath);
            if (columns == null || columns.Count == 0)
                throw new InvalidOperationException(
                    $"FITS file '{filePath}' contains no binary or ASCII table HDU.");

            var rows = _source.ReadRows(filePath, columns.Count);
            return new FeatureTable { Columns = columns, Rows = rows };
        }
    }
}
