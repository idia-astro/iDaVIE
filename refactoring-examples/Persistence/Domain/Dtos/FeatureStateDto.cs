using iDaVIE.Persistence.Domain.Serialization;

namespace iDaVIE.Persistence.Domain.Dtos;

/// <summary>
/// Persistent state for all feature sets managed by FeatureSetManager.
/// Source: FeatureSetManager.MaskFeatureSetList, NewFeatureSetList, ImportedFeatureSetList.
/// NOT persisted: Selected, mask bounding boxes, mask statistics, GPU refs.
/// Temporary features are now persisted with IsTemporary = true for crash recovery.
/// Recomputed on restore: mask bboxes/stats (DataAnalysis), imported RawData (catalog reader).
/// </summary>
public class FeatureStateDto
{
    [PersistField] public List<FeatureSetDto> FeatureSets { get; set; } = new();
}

/// <summary>Persistent state for a single feature set (FeatureSetRenderer).</summary>
public class FeatureSetDto
{
    /// <summary>FeatureSetType enum serialised as string (e.g. "Mask", "New", "Imported").</summary>
    [PersistField] public string? FeatureSetType { get; set; }

    /// <summary>Name from featureSetRenderer.gameObject.name.</summary>
    [PersistField] public string? SetName { get; set; }

    /// <summary>FeatureSetRenderer.FeatureColor — Unity Color → SerializableColor.</summary>
    [PersistField] public SerializableColor? FeatureColor { get; set; }

    /// <summary>FeatureSetRenderer.featureSetVisible.</summary>
    [PersistField] public bool FeatureVisibility { get; set; } = true;

    // ── Imported sets only ──────────────────────────────────────────────────

    /// <summary>Source catalog file path (Imported sets only).</summary>
    [PersistField(Optional = true)] public string? FileName { get; set; }

    /// <summary>
    /// Column mapping: SourceMappingOptions.ToString() → catalog column name.
    /// Serialized as Dictionary&lt;string, string&gt; to avoid Unity/SDK enum dependencies.
    /// </summary>
    [PersistField(Optional = true)] public Dictionary<string, string>? ColumnMapping { get; set; }

    /// <summary>Boolean mask over table columns (Imported sets only).</summary>
    [PersistField(Optional = true)] public bool[]? ColumnsMask { get; set; }

    /// <summary>Whether features outside the current volume bounds were excluded at import.</summary>
    [PersistField(Optional = true)] public bool ExcludeExternal { get; set; }

    [PersistField] public List<FeatureDto> Features { get; set; } = new();
}

/// <summary>Persistent state for a single Feature.</summary>
public class FeatureDto
{
    /// <summary>Feature.Id (int).</summary>
    [PersistField] public int Id { get; set; }

    /// <summary>Feature.Name (string).</summary>
    [PersistField] public string? Name { get; set; }

    /// <summary>Feature.Flag (string).</summary>
    [PersistField] public string? Flag { get; set; }

    /// <summary>Feature.Comment (optional).</summary>
    [PersistField(Optional = true)] public string? Comment { get; set; }

    /// <summary>Feature.Visible — default true when missing.</summary>
    [PersistField] public bool Visible { get; set; } = true;

    /// <summary>Feature.CubeColor — Unity Color → SerializableColor.</summary>
    [PersistField] public SerializableColor? CubeColor { get; set; }

    /// <summary>Feature.CornerMin — Unity Vector3 → SerializableVector3 (Imported/New only).</summary>
    [PersistField(Optional = true)] public SerializableVector3? CornerMin { get; set; }

    /// <summary>Feature.CornerMax — Unity Vector3 → SerializableVector3 (Imported/New only).</summary>
    [PersistField(Optional = true)] public SerializableVector3? CornerMax { get; set; }

    /// <summary>Feature.RawData string[] — New features only; absent for Mask/Imported.</summary>
    [PersistField(Optional = true)] public string[]? RawData { get; set; }

    /// <summary>Feature.Temporary — true when the feature is mid-paint at save time (crash recovery).</summary>
    [PersistField(Optional = true)] public bool IsTemporary { get; set; } = false;
}
