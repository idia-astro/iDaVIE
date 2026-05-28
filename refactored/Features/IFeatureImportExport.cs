// SPDX-License-Identifier: LGPL-3.0-or-later
// Import / export ports — separated per DD-4 (read and write have different
// dependency profiles: writers need ISourceStatsProvider + ICoordinateTransformer,
// readers need neither). Replaces the legacy direct call chain
// FeatureMenuController → FeatureSetRenderer.SaveAsVoTable → static VoTableSaver.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;   // FeatureColour

namespace iDaVIE.Features
{
    /// <summary>Catalogue-column semantic role. OCP closure intentionally violated
    /// (DD-9 trade-off in ST5 proposal) — the vocabulary is closed (FITS standard).</summary>
    public enum SourceMappingOptions
    {
        ID,
        X, Y, Z,
        Xmin, Xmax, Ymin, Ymax, Zmin, Zmax,
        Ra, Dec, Velo, Freq, Redshift,
        Flag
    }

    public readonly record struct FeatureColumnInfo(string Name, string Unit, string DataType, string Ucd);

    public sealed class FeatureImportMapping
    {
        public IReadOnlyDictionary<SourceMappingOptions, string> ColumnAssignments { get; init; }
        public IReadOnlyList<bool> ColumnMask     { get; init; }
        public bool                ExcludeExternal { get; init; }
        public string              SetName        { get; init; }
        public FeatureColour       DisplayColour  { get; init; }
    }

    public sealed class FeatureTable
    {
        public IReadOnlyList<FeatureColumnInfo>            Columns { get; init; }
        public IReadOnlyList<IReadOnlyList<string>>        Rows    { get; init; }
    }

    public interface IFeatureImportService
    {
        IReadOnlyList<FeatureColumnInfo> GetColumns(string filePath);
        void ImportFromFile(string filePath, FeatureImportMapping mapping);
        FeatureImportMapping LoadMappingFromFile(string mappingFilePath);
        void SaveMappingToFile(FeatureImportMapping mapping, string mappingFilePath);
    }

    public interface IFeatureCatalogueReader
    {
        FeatureTable Read(string filePath);
    }

    public interface IFeatureCatalogueWriter
    {
        void Write(IFeatureSet featureSet, string filePath);
    }
}
