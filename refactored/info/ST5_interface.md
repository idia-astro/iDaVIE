# Sub-Team 5 — Feature System Interface Contract

ISE Refactoring Assessment, 18 May – 5 June 2026. Companion documents: `ST5_refactoring_proposal.md` (design rationale, DDs, worked examples), `ST5_domain_design.md` (domain model, invariants, layer map), `Feature_BDD.puml` (UML).

The public contract surface is defined entirely by interfaces and plain C# value types — no `UnityEngine` types cross any boundary. The concrete `FeatureSet` class is held internally; external consumers receive `IFeatureSet` and cannot mutate the collection.

## 1. Provided

| Name | Kind | Exposed to | Purpose |
| --- | --- | --- | --- |
| `IFeatureSet` | Interface | ST3, ST6, ST7 | Read-only homogeneous feature collection |
| `IFeature` | Interface | ST3, ST6, ST7 | Read-only single feature |
| `IFeatureStatistics` | Interface | ST3, ST6, ST7 | Realtime Mask-feature statistics (null otherwise) |
| `IFeatureSetQuery` | Interface | ST4, ST6 | Query + display mutation + set-membership mutation + per-feature mutation + `FeatureSetChanged` event |
| `IFeatureSelectionService` | Interface | ST4, ST6 | Cursor selection, direct selection, selection-changed event |
| `IFeatureListNavigation` | Interface | ST4 | `DisplayNextSet()` / `DisplayPreviousSet()` for the source-list voice commands (M-11) |
| `IFeatureImportService` | Interface | ST6 | File-based import of Imported sets; column-mapping persistence |
| `IMomentMapService` | Interface | ST6 | Moment-map generation (GPU-backed via the ACL) |
| `ISpectralProfileService` | Interface | ST6 | Region → spectral profile (via `IDataAnalysisPlugin`) |
| `IFeatureCatalogueReader` | Port interface | ST4 (optional); also held internally by ST5's `FeatureImportService` | File → `FeatureTable` parser |
| `IFeatureCatalogueWriter` | Port interface | ST4 (optional); also held internally by ST5's `FeatureMenuController` for VOTable export | `IFeatureSet` → file writer |
| `IFeatureStateCapture` | Persistence port | ST7 | `Capture()` / `Restore(dto)` of UserDefined / Imported / SelectionBox sets per the M-16 uniform pattern. DTO field schema is in draft (IR-01 — pending Architecture Guild Day 9 sign-off; see §3 and `shared_interfaces.md` §5.6) |
| `FeatureTable` | Sealed class | ST4, ST6 | Parsed in-memory catalogue |
| `FeatureImportMapping` | Sealed class | ST6 | Column-to-field mapping for import |
| `FeatureColumnInfo` | Value type | ST4, ST6 | Column schema row (carried inside `FeatureTable`) |
| `SpectralProfileResult` | Value type | ST6 | Spectral-profile result |
| `SourceMappingOptions` | Enum | ST6 | Catalogue-column semantic role |
| `FeatureSetType` | Enum | All | `Mask` / `Imported` / `UserDefined` / `SelectionBox` |

Boundary value types declared elsewhere and used at the ST5 interface (ownership recorded for cross-reference, not re-declared here):

| Name | Owner | Notes |
| --- | --- | --- |
| `CartesianCoord` | ST1 shared-types module (M-21) | `(int X, int Y, int Z)` voxel-space record struct; replaces `UnityEngine.Vector3` at the boundary. See `shared_interfaces.md` §1.1. |
| `FeatureColour` | ST1 shared-types module (M-21) | `(float R, G, B, A)` record struct; replaces `UnityEngine.Color` at the boundary |
| `MomentMapResult` | ST3 (`iDaVIE.Rendering.Contracts`) | Owned by ST3 per `interface_resolutions.md` line 13; jointly used by ST3, ST5, ST6 (M-08). Schema in `shared_interfaces.md` §3.3. |

## 2. Consumed

| Name | Kind | From | Purpose |
| --- | --- | --- | --- |
| `IVolumeDataSet` | Interface | ST1 | Read-only view of the loaded volume aggregate (dims, subcube, status). Held by `FeatureSetService` for dataset-lifecycle resets and by `FeatureFactory` for cube-dimension preconditions (M-02) |
| `ISourceStatsProvider` | Interface | ST2 | Per-source stats; `SourceStatsUpdated` event |
| `ICoordinateTransformer` | Interface | ST2 | Cube → sky transform |
| `IDataAnalysisPlugin` | Interface | ST2 | Per-region stats |
| `IMomentMapRenderer` | Interface | ST3 | `Task<MomentMapResult> RenderMomentMap(MomentMapRequest request, CancellationToken ct)` — the GPU seam wrapped by ST5's `MomentMapServiceAdapter` (M-08). `MomentMapRequest`, `MomentMapResult`, and `MomentOrder` are owned by ST3 per `interface_resolutions.md` line 13 — see `shared_interfaces.md` §3.3 |
| `SourceStats` | Sealed class | ST2 | Stats payload POCO |
| `WorldCoord` | Value type | ST2 | Sky-coordinate payload (inbound only; ST5 does not re-expose) |

## 3. Method signatures and data schemas

```csharp
// ── Shared value types ────────────────────────────────────────────────────────
// CartesianCoord and FeatureColour are declared in ST1's shared-types module
// (global_model.md §3.1, M-21) and referenced here. MomentMapResult is owned by
// ST3 per interface_resolutions.md line 13 — declared in iDaVIE.Rendering.Contracts;
// shared_interfaces.md §3.3 carries the schema. WorldCoord is declared in ST2 (M-21)
// since it crosses only ST2↔ST5.

public enum FeatureSetType { Mask, Imported, UserDefined, SelectionBox }


// ── Domain — provided to ST3, ST6, ST7 ───────────────────────────────────────

/// <summary>Realtime statistics for a Mask feature. Invariants guaranteed by ST5:
/// centroid inside bounding box; TotalFlux and PeakFlux non-negative.
/// Eight public members — one over the brief's ISP target of 7 — because the
/// line-width and systemic-velocity quantities are each reported in both
/// channel and velocity units (see SourceStats below).</summary>
public interface IFeatureStatistics
{
    long VoxelCount { get; }
    double TotalFlux { get; }
    double PeakFlux { get; }
    CartesianCoord FluxWeightedCentroid { get; }
    /// <summary>Line width at 20% of peak, in channel units.
    /// Populated from SourceStats.ChannelW20.</summary>
    double ChannelW20 { get; }
    /// <summary>Line width at 20% of peak, in velocity units.
    /// Populated from SourceStats.VeloW20.</summary>
    double VeloW20 { get; }
    /// <summary>Systemic central position in channel units.
    /// Populated from SourceStats.ChannelVsys.</summary>
    double ChannelVsys { get; }
    /// <summary>Systemic central position in velocity units.
    /// Populated from SourceStats.VeloVsys.</summary>
    double VeloVsys { get; }
}

/// <summary>Read-only view of a single feature.
/// Statistics is null for Imported, UserDefined, and SelectionBox.</summary>
public interface IFeature
{
    /// <summary>For Mask: maskVal. For Imported: row index in the source
    /// catalogue. For UserDefined: copied verbatim from the source feature
    /// the user duplicated (so multiple UserDefined copies of the same source
    /// share an OriginId). For SelectionBox: -1.</summary>
    int OriginId { get; }
    string Name { get; }
    string Flag { get; }
    /// <summary>Bounding-box centre in data-cube voxel coordinates.</summary>
    CartesianCoord Center { get; }
    /// <summary>Bounding-box extent on each axis in data-cube voxel units.
    /// Equivalent corner form: CornerMin = Center - Size/2; CornerMax = Center + Size/2.</summary>
    CartesianCoord Size { get; }
    bool IsSelected { get; }
    /// <summary>Raw catalogue column values for this feature, in the same
    /// order as the owning IFeatureSet.RawDataKeys. All values are strings —
    /// callers parse per RawDataKeys semantics. Empty for Mask features
    /// constructed from source statistics.</summary>
    IReadOnlyList<string> RawDataValues { get; }
    IFeatureStatistics? Statistics { get; }
}

/// <summary>Read-only view of a homogeneous feature collection.
/// All members share Type. Mutation is via IFeatureSetQuery.</summary>
public interface IFeatureSet
{
    /// <summary>Monotonic identifier assigned by FeatureSetService at creation;
    /// unique across all sets for the lifetime of the catalogue. Stable across
    /// display-state mutations; not reused when a set is removed.</summary>
    int Index { get; }
    string FileName { get; }
    FeatureSetType Type { get; }
    FeatureColour DisplayColour { get; }
    bool IsVisible { get; }
    IReadOnlyList<IFeature> Features { get; }
    /// <summary>Column header names from the source catalogue, parallel to
    /// each Feature.RawDataValues. Empty for Mask and SelectionBox sets.</summary>
    IReadOnlyList<string> RawDataKeys { get; }
}


// ── Application — provided to ST4, ST6 ───────────────────────────────────────

public interface IFeatureSetQuery
{
    IReadOnlyList<IFeatureSet> GetAllFeatureSets();
    IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type);
    void SetVisible(IFeatureSet featureSet, bool visible);
    void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour);
    /// <summary>Updates a non-Mask feature's bounding box (e.g. ST4 anchor-drag).
    /// Mask-feature bounds are owned by ST2's source statistics and refreshed via
    /// SourceStatsUpdated; this method rejects Mask features.</summary>
    void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax);
    /// <summary>Updates a feature's flag string (e.g. source-list flag toggle).</summary>
    void SetFeatureFlag(IFeature feature, string flag);
    /// <summary>Appends source to a UserDefined set, creating one lazily if none exists.</summary>
    void CopyFeatureToUserDefined(IFeature source);
    /// <summary>Sets the bounds of the singleton SelectionBox feature. The
    /// SelectionBox set is lazily created on the first call; subsequent calls
    /// update the existing feature's bounds in-place (the set is never replaced,
    /// and never holds more than one feature).</summary>
    void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax);
    event Action FeatureSetChanged;
}

public interface IFeatureSelectionService
{
    IFeature? SelectedFeature { get; }
    IFeatureSet? SelectedFeatureSet { get; }
    /// <summary>Selects the first feature whose AABB contains cursorVoxelSpace.</summary>
    bool SelectAtCursor(CartesianCoord cursorVoxelSpace);
    void SelectFeature(IFeature feature, IFeatureSet owningSet);
    void DeselectFeature();
    event Action<IFeature?> SelectionChanged;
}

/// <summary>Source-list navigation surface for ST4's voice commands
/// (`next source list` / `previous source list`). Realised by
/// FeatureMenuController. M-11.</summary>
public interface IFeatureListNavigation
{
    void DisplayNextSet();
    void DisplayPreviousSet();
}

public interface IMomentMapService
{
    /// <summary>momentOrder: 0 = integrated intensity, 1 = velocity field.
    /// MomentMapResult is owned by ST3 per interface_resolutions.md line 13
    /// (declared in iDaVIE.Rendering.Contracts; schema in shared_interfaces.md §3.3).</summary>
    Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask);
}

public interface ISpectralProfileService
{
    /// <summary>Bounds are pixel coordinates.</summary>
    Task<SpectralProfileResult> ComputeForRegionAsync(
        CartesianCoord boundsMin, CartesianCoord boundsMax);
}

/// <summary>Profile[i] is flux at channel ZStartChannel + i.</summary>
public readonly record struct SpectralProfileResult(
    IReadOnlyList<double> Profile, int ZStartChannel,
    double TotalFlux, double PeakFlux);


// ── Provided to ST6 (import flow) ────────────────────────────────────────────

public enum SourceMappingOptions
{
    ID,
    X, Y, Z,
    Xmin, Xmax, Ymin, Ymax, Zmin, Zmax,
    Ra, Dec, Velo, Freq, Redshift,
    Flag
}

/// <summary>Ucd carries the VOTable Unified Content Descriptor; empty string if absent.</summary>
public readonly record struct FeatureColumnInfo(
    string Name, string Unit, string DataType, string Ucd);

public sealed class FeatureImportMapping
{
    public IReadOnlyDictionary<SourceMappingOptions, string> ColumnAssignments { get; init; }
    public IReadOnlyList<bool> ColumnMask { get; init; }
    public bool ExcludeExternal { get; init; }
    public string SetName { get; init; }
    public FeatureColour DisplayColour { get; init; }
}

public interface IFeatureImportService
{
    IReadOnlyList<FeatureColumnInfo> GetColumns(string filePath);
    void ImportFromFile(string filePath, FeatureImportMapping mapping);
    FeatureImportMapping LoadMappingFromFile(string mappingFilePath);
    void SaveMappingToFile(FeatureImportMapping mapping, string mappingFilePath);
}


// ── Catalogue I/O ports — provided to ST4 ────────────────────────────────────

public sealed class FeatureTable
{
    public IReadOnlyList<FeatureColumnInfo> Columns { get; init; }
    /// <summary>Outer = rows; inner = column values in Columns order. All strings.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}

public interface IFeatureCatalogueReader
{
    FeatureTable Read(string filePath);
}

public interface IFeatureCatalogueWriter
{
    void Write(IFeatureSet featureSet, string filePath);
}


// ── Persistence port — provided to ST7 ───────────────────────────────────────
// Follows the M-16 uniform Capture/Restore pattern. The DTO field schema is
// in draft (IR-01 — pending Architecture Guild Day 9 sign-off); the shape
// below is canonical in `shared_interfaces.md` §5.6 and reproduced here for
// reference. Persistable state: UserDefined sets (features + bounds + flags),
// Imported sets (file path + mapping; features re-derived on Restore),
// SelectionBox bounds if present, plus per-set DisplayColour and IsVisible.
// Mask sets are not snapshotted by ST5 — they re-derive from the loaded mask
// via ST2 (IMaskStateCapture + the SourceStatsUpdated bootstrap path).
//
// FeatureSetEntryDto.Type is serialised as an enum-name string for forward
// compatibility; on Restore, ST5 MUST use
// InteractionStateDto.TryParseOrDefault(dto.Type, FeatureSetType.UserDefined)
// (declared in iDaVIE.Interaction; see `shared_interfaces.md` §4.4) so that
// workspaces saved with a future FeatureSetType member degrade gracefully on
// older builds rather than throwing.

public sealed class FeatureStateDto
{
    public int SchemaVersion { get; set; } = 1;
    public List<FeatureSetEntryDto> FeatureSets { get; set; } = new();
    public SubcubeBoundsDto? SelectionBoxBounds { get; set; }   // ST1's persistence DTO
}

public sealed class FeatureSetEntryDto
{
    public string        SetName       { get; set; } = string.Empty;
    public string        Type          { get; set; } = nameof(FeatureSetType.UserDefined);
    public FeatureColour DisplayColour { get; set; }
    public bool          IsVisible     { get; set; } = true;

    // Imported sets: file path + mapping — features re-derived on Restore.
    // UserDefined sets: inline feature list in Features.
    // Mask sets: empty — re-derived via IMaskStateCapture + SourceStatsUpdated.
    public string?               ImportFilePath { get; set; }
    public FeatureImportMapping? ImportMapping  { get; set; }
    public List<FeatureEntryDto> Features       { get; set; } = new();
}

public sealed class FeatureEntryDto
{
    public int    OriginId   { get; set; }
    public string Name       { get; set; } = string.Empty;
    public string Flag       { get; set; } = string.Empty;
    public int    CenterX    { get; set; }
    public int    CenterY    { get; set; }
    public int    CenterZ    { get; set; }
    public int    BoundsMinX { get; set; }
    public int    BoundsMinY { get; set; }
    public int    BoundsMinZ { get; set; }
    public int    BoundsMaxX { get; set; }
    public int    BoundsMaxY { get; set; }
    public int    BoundsMaxZ { get; set; }
}

public interface IFeatureStateCapture
{
    FeatureStateDto Capture();
    void Restore(FeatureStateDto dto);
}


// ── Consumed from ST2 ────────────────────────────────────────────────────────

// "// ← x" comments map this field to the legacy DataAnalysis.SourceStats
// field that populates it — guidance for ST2's adapter implementation.
public sealed class SourceStats
{
    public long VoxelCount { get; init; }                       // ← numVoxels
    public CartesianCoord BoundsMin { get; init; }              // ← minX/Y/Z
    public CartesianCoord BoundsMax { get; init; }              // ← maxX/Y/Z
    public double TotalFlux { get; init; }                      // ← sum
    public double PeakFlux { get; init; }                       // ← peak
    public CartesianCoord FluxWeightedCentroid { get; init; }   // ← cX/Y/Z
    public double ChannelW20 { get; init; }
    public double VeloW20 { get; init; }
    public double ChannelVsys { get; init; }
    public double VeloVsys { get; init; }
    public IReadOnlyList<double> SpectralProfile { get; init; }
    public int ZStartChannel { get; init; }
}

public interface ISourceStatsProvider
{
    SourceStats? GetStatsForSource(int originId);
    IReadOnlyDictionary<int, SourceStats> GetAllStats();
    event Action<int> SourceStatsUpdated;
}

public interface ICoordinateTransformer
{
    /// <summary>Voxel → world coordinate. Returns <c>WorldCoord.Invalid</c>
    /// (all-NaN values, empty unit string) if the transform is undefined at
    /// the given position. See `shared_interfaces.md` §2.</summary>
    WorldCoord Transform(CartesianCoord pixelCoord);
}

public interface IDataAnalysisPlugin
{
    SourceStats ComputeRegionStats(CartesianCoord boundsMin, CartesianCoord boundsMax);
}
```

## 4. Preconditions and postconditions

| Method | Pre | Post |
| --- | --- | --- |
| `GetAllFeatureSets`, `GetFeatureSetsByType` | Called after `FeatureSetService` is constructed by the composition root | Non-null snapshot; individual `IFeatureSet` references remain valid until that set is removed (display-only mutations preserve them) |
| `SetVisible`, `SetDisplayColour` | `featureSet` from a prior query; no `FeatureSetChanged` since | Display state updated; `FeatureSetChanged` raised |
| `SetFeatureBounds` | `feature` from a non-Mask set; `boundsMin ≤ boundsMax` on each axis | Bounds updated; `FeatureSetChanged` raised |
| `SetFeatureFlag` | `feature` from a loaded set | Flag updated; `FeatureSetChanged` raised |
| `CopyFeatureToUserDefined` | `source` from a loaded set | Copy added to UserDefined set (created lazily). `FeatureSetChanged` raised once if a UserDefined set already existed; twice if lazy creation triggered (once at empty-set creation, once after the copy is attached) — see §6 |
| `SetSelectionBoxBounds` | `boundsMin ≤ boundsMax` on each axis | The (singleton) SelectionBox set exists and holds exactly one feature. `FeatureSetChanged` raised once if the SelectionBox set already existed; twice on the first call (once at empty-set creation, once after the bounds feature is attached) — see §6 |
| `SelectAtCursor` | Coordinate in cube voxel space | True and `SelectedFeature` updated on hit; false and prior selection unchanged on miss |
| `SelectFeature` | Both args non-null; `feature ∈ owningSet.Features` | `SelectedFeature == feature`; `SelectionChanged` raised |
| `GetColumns` | `filePath` non-whitespace | Returns schema; no FeatureSet created |
| `ImportFromFile` | At least one spatial coordinate group assigned; `ColumnMask.Count == GetColumns(filePath).Count` | New Imported set visible; `FeatureSetChanged` raised |
| `GenerateAsync` | Cube loaded; `momentOrder ∈ {0, 1}` | `Values.Length == Width * Height` (per ST3's `MomentMapResult` schema, `shared_interfaces.md` §3.3) |
| `ComputeForRegionAsync` | Cube loaded; bounds well-ordered | `Profile.Count` equals number of Z-channels spanned |
| `GetStatsForSource` | `originId` from a Mask `IFeature.OriginId` | Populated `SourceStats` or null |
| `Transform` | Cube loaded with AST frame | Populated `WorldCoord`; returns `WorldCoord.Invalid` if the transform is undefined at the given position |

## 5. Error model

| Scenario | Response |
| --- | --- |
| `GetStatsForSource` — unknown id | null |
| `IFeature.Statistics` on non-Mask feature | null |
| `SelectAtCursor` — cursor outside all features | false; prior selection unchanged |
| `Transform` — undefined at position | Returns `WorldCoord.Invalid` (all-NaN, empty unit) per `shared_interfaces.md` §2 |
| `GenerateAsync` / `ComputeForRegionAsync` — no cube | `InvalidOperationException` |
| `ComputeForRegionAsync` — bounds inverted | `ArgumentException` |
| `CopyFeatureToUserDefined`, `SelectFeature`, `ImportFromFile` — null arg | `ArgumentNullException` |
| `SelectFeature` — feature not in owningSet | `ArgumentException` |
| `SetFeatureBounds` — feature belongs to a Mask set | `InvalidOperationException` |
| `SetFeatureBounds` — bounds inverted | `ArgumentException` |
| `GetColumns` / `ImportFromFile` — file not found | `FileNotFoundException` |
| `GetColumns` / `ImportFromFile` — unsupported format / missing spatial mapping | `InvalidOperationException` |
| `ImportFromFile` — column mask length mismatch | `ArgumentException` |

## 6. Threading and lifecycle

| Constraint | Detail |
| --- | --- |
| Main thread | All methods on `IFeatureSetQuery`, `IFeatureSet`, `IFeature`, `IFeatureSelectionService`, and `ICoordinateTransformer.Transform` |
| Main-thread events | `FeatureSetChanged`, `SelectionChanged`, `SourceStatsUpdated` raised on the Unity main thread |
| `*Async` methods | May run off-thread; result must be consumed on the main thread before touching Unity APIs |
| Background-safe ports | `IFeatureCatalogueReader.Read`, `IFeatureCatalogueWriter.Write`, `IFeatureImportService.GetColumns` — implementations must not call Unity APIs |
| Initialisation order | `IFeatureSetQuery` and `IFeatureSelectionService` valid only after `FeatureSetService` and `SelectionService` are constructed by the composition root |
| Snapshot validity | An `IFeatureSet` reference is invalidated when *that set* is removed from the catalogue. `FeatureSetChanged` fires for both set-membership changes (create/remove) and in-place display-state mutations (`SetVisible`, `SetDisplayColour`); the latter do **not** invalidate references. The list returned by `GetAllFeatureSets` / `GetFeatureSetsByType` is a snapshot — re-query after `FeatureSetChanged` to pick up new or removed sets. |
| SelectionBox cardinality | `GetFeatureSetsByType(SelectionBox)` returns 0 or 1 entries. The SelectionBox set is lazily created on the first `SetSelectionBoxBounds` call and is never recreated thereafter; it always holds exactly one feature, whose bounds are updated in-place on subsequent calls. |
| Bulk-population events | Any flow that creates a new set and then attaches features to it raises `FeatureSetChanged` **twice**: once when the empty set is created, once after the features are attached. The flows are: `IFeatureImportService.ImportFromFile` (every call); the first `ISourceStatsProvider.SourceStatsUpdated` after a mask load (the lazy Mask-set bootstrap inside `FeatureSetService`); the first `CopyFeatureToUserDefined` when no UserDefined set exists yet; and the first `SetSelectionBoxBounds` call. Subsequent calls that mutate an existing set (later `CopyFeatureToUserDefined`, later `SetSelectionBoxBounds`, and `SourceStatsUpdated` events that refresh, append, or remove individual Mask features) raise `FeatureSetChanged` once. Consumers should re-query on the second event of a bulk population. |

## 7. Versioning policy

1. All contract changes require a Kanban card tagged `contract-change`, approved by the ST5 TL and the TL of every affected sub-team before merging.
2. **Additive changes** — new optional member, new enum value — may merge once all affected teams have acknowledged.
3. **Breaking changes** — rename, removal, type change, new required member — must not merge until every affected consumer has confirmed they are ready to update.
4. A diff of this document must accompany every PR that touches a contract definition.
