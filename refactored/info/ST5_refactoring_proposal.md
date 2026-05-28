# Sub-Team 5: Feature System & Domain Model — ISE Refactoring Proposal

ISE Refactoring Assessment, 18 May – 5 June 2026. Sub-team 5 owns the `FeatureData/` package. The assessment is **design-only** — no production code is changed. Outputs are UML diagrams, CK metric tables, and a written refactoring proposal with two worked examples.

**Assessment-wide constraints:**
- Target style: Client–server at system level; micro-kernel server with a versioned C/C++ plug-in ABI; layered architecture (Domain → Application → Infrastructure → Plug-in Host); anti-corruption layer around Unity APIs.
- Quality standard: ISO/IEC 25010:2023 maintainability sub-characteristics (modularity, analysability, modifiability, testability).
- Metrics: Chidamber & Kemerer (CK) suite — WMC ≤ 20 (domain) / ≤ 40 (adapters), DIT ≤ 4, NOC ≤ 5, CBO ≤ 14 (domain) / ≤ 25 (orchestrators), RFC ≤ 50, LCOM ≤ 0.5.
- Mandatory constraints: No circular dependencies; domain code must not transitively depend on `UnityEngine` or `SteamVR` types; every public API boundary must be an interface; plug-in ABI must be semantically versioned and ABI-stable within a major version.

Key classes flagged as refactoring targets: `VolumeDataSetRenderer` (god-class MonoBehaviour), `VolumeDataSet` (god-class plain C#), `Config` (singleton), `FitsReader`/`AstTool`/`DataAnalysis` (thin unversioned P/Invoke adapters).

---

## Requirements Engineering

### Current behaviour (three Feature flavours)

| Flavour | Origin | Statistics available |
|---|---|---|
| Mask | Derived from a loaded mask FITS file by the DataAnalysis native library | Yes — voxel count, total flux, peak flux, flux-weighted centroid, W20 (channel + velocity), Vsys (channel + velocity) |
| Imported | Loaded from a VOTable or FITS binary table file | No native statistics; raw catalogue columns only |
| UserDefined | Created by the user in-session (drawn shapes, copied sources) | No native statistics |

Realtime statistics for Mask features are computed by the C++ `DataAnalysis` plugin and keyed by `maskVal` (the source ID in the mask FITS file). They must remain consistent with the mask after any paint-brush edit.

Statistics invariants that must hold for all Mask features:
- **Centroid inside bounding box:** `CornerMin ≤ FluxWeightedCentroid ≤ CornerMax` on all three axes
- **Flux non-negative:** `TotalFlux ≥ 0`, `PeakFlux ≥ 0`

The brief's third invariant constrains W50; excluded — see DD-13.

### Future requirements (out of scope for this assessment, but design must accommodate)

- **Iso-contours / surfaces:** feature boundaries as 3D surface meshes, not just axis-aligned bounding boxes
- **Particle datasets:** sparse multi-parameter point clouds — would add a fourth Feature flavour
- **Virtual Observatory integration:** features retrieved from or pushed to remote VO catalogues via IVOA protocols

The target architecture must not foreclose these. The `IFeature` interface and the `FeatureSet` collection (the brief's "FeatureCatalog" — see DD-6) must be extensible to new flavours without modification (OCP).

---

## Current Feature System: File-by-File

All files are in `Assets/Scripts/FeatureData/` under namespace `DataFeatures`.

### `Feature.cs` (~201 lines) — domain aggregate, plain C#

Represents a single detected astronomical source (a bounding-box region in the data cube).

Key fields:
- `CornerMin`, `CornerMax` (Vector3) — axis-aligned bounding box in pixel coordinates
- `CubeColor` (Color) — display colour
- `Name`, `Flag`, `Id`, `Index` (int) — identity
- `RawData` (string[]) — raw column values from source catalog (parallel to `FeatureSetRenderer.RawDataKeys`/`RawDataTypes`)
- `Visible`, `Selected` (bool) — display state
- `Temporary` (bool) — marks the transient selection-box feature
- `FeatureSetParent` (FeatureSetRenderer) — back-reference to owning renderer

Problems:
- `FeatureSetParent` is a `MonoBehaviour` reference — domain object depends on Unity
- Every property setter calls `FeatureSetParent.SetFeatureAsDirty(Index)` — observer coupling without an interface
- `static void SetCubeColors(CuboidLine cube, Color baseColor, bool colorAxes)` directly references `CuboidLine` (a Unity scene object)
- No interface; nothing can substitute a `Feature` in tests without pulling in Unity

### `FeatureSetRenderer.cs` (~620 lines) — MonoBehaviour, GPU rendering

Renders bounding boxes for one `FeatureSetType` via GPU vertex pulling.

Key types:
- `FeatureVertex` struct: `Vector3 Position`, `Vector4 Color`, `FeatureVisibility Visibility` (Hidden=0, Visible=1, Selected=2); 32 bytes each
- `CoordTypes` enum: `cartesian`, `freqz`, `velz`, `redz` — Z-axis coordinate space of loaded features
- Constants: `VerticesPerFeature = 24`, `BytesPerVertex = 32`, `DefaultFeatureCapacity = 16384`

Key methods:
- `Update()` — drains dirty list, reallocates `ComputeBuffer` if capacity exceeded, uploads changed vertices
- `OnRenderObject()` — `Graphics.DrawProceduralNow(MeshTopology.Lines, count * 24)` each frame
- `SpawnFeaturesFromTable(FeatureTable, VolumeDataSetRenderer)` (~250 lines) — coordinate parsing, WCS transforms (`AstTool.GetAltSpecSet`, `AstTool.Transform3D`), and `Feature` construction all inline
- `SpawnFeaturesFromSourceStats(Dictionary<int, SourceStats>, VolumeDataSetRenderer)` — populates from mask analysis results
- `RawDataKeys` / `RawDataTypes` — column metadata stored on renderer (TODO comment: "Need to find a better host")

Problems:
- `SpawnFeaturesFromTable` violates SRP: it parses coordinates, transforms WCS, and constructs domain objects all in one method
- Renderer owns column schema (`RawDataKeys`/`RawDataTypes`) that belongs on the `FeatureSet`, not the renderer
- Direct references to `VolumeDataSetRenderer` and `FeatureSetManager` — high CBO
- `ComputeBuffer` allocation and GPU vertex layout are interleaved with feature-list management

### `FeatureSetManager.cs` (~466 lines) — MonoBehaviour, orchestrator

Manages `FeatureSetRenderer` instances grouped by `FeatureSetType` — three lists (one per non-Selection type) plus a single Selection renderer. **Critically, each renderer is stamped with a fixed `FeatureSetType` at construction and never changes.** The manager enforces strict type homogeneity: `MaskFeatureSetList` contains only Mask renderers, `ImportedFeatureSetList` only Imported, and so on. No single renderer ever holds features of mixed types.

Key fields:
- `MaskFeatureSetList`, `ImportedFeatureSetList`, `NewFeatureSetList` — `List<FeatureSetRenderer>`, each strictly one type
- `SelectionFeatureSet` — single `FeatureSetRenderer` for the transient selection box
- `_anchorColliders` — 8 GameObjects for bounding-box corner handles (scene objects managed here)
- `SelectedFeature` property — fires `MaskFeatureSelected` event

Problems:
- `SelectFeature(Vector3)` calls `GameObject.Find("SourcesMenu")` — hardcoded scene coupling
- `ExportFeatureSet(FeatureSetRenderer, string)` (line 462) is an **empty stub** — likely an abandoned early plan. The working export path actually runs via the menu: `FeatureMenuController.SaveListAsVoTable()` (line 399) → `FeatureSetRenderer.SaveAsVoTable(filePath)` (line 612) → static `VoTableSaver.SaveFeatureSetAsVoTable(featureSet, filePath)` in `VoTable.cs:430` (namespace `VoTableReader`). That static saver reaches `featureSet.VolumeRenderer.SourceStatsDict` and calls `AstTool.Transform3D` / `AstTool.Norm` directly — the coupling chain DD-4 dissolves.
- `AppendFeatureToFile()` writes ASCII (legacy debug method, not a real export path)
- Uses `Config.Instance` directly (singleton coupling)
- Manages Unity scene objects (`_anchorColliders`) alongside domain state — mixed concerns

### `FeatureTable.cs` / `VoTable.cs` / `FeatureMapper.cs` — file I/O

`FeatureTable` (~235 lines) dispatches on extension (`.xml` → `VoTable` parser; `.fits` → `FitsReader` static P/Invoke). `VoTable.cs` is namespace `VoTableReader`, adapted from WorldWideTelescope, string-only data model. `FeatureMapper.cs` contains four types: `Mapping` and `MapEntry` (JSON-serialisable column-assignment records), `FeatureMapping` (instance class holding the `GetMappingFromFile` / `SaveMappingToFile` load/save logic), and the static `FeatureMapper` class — which is **completely empty**. The load/save logic has a home in `FeatureMapping`; what has no home is the apply-mapping logic that translates a `Mapping` plus a `FeatureTable` into `Feature` instances — that work lives inline inside `FeatureSetRenderer.SpawnFeaturesFromTable`. None of these has an interface; callers cannot substitute mocks.

### `FeatureMenuController.cs` (~426 lines) — MonoBehaviour, source list

`UpdateInfo()` builds formatted display strings and calls `AstTool.Transform3D` / `AstTool.Norm` directly — UI layer holds a native plugin dependency. Accesses `_featureSetManager.VolumeRenderer.SourceStatsDict` via `ElementAt(SelectedFeature.Index)` (see SOLID-violations table for the ordinal-coupling bug). `ToggleListVisibility()` uses `GameObject.Find("RenderMenu")`.

### `FeatureMenuCell.cs` (~297 lines) — source-list row

`Start()` uses `GameObject.Find("VolumeDataSetManager")`; `ToggleFlagIndex()` reads `Config.Instance.flags` directly.

---

## Current Architecture Problems Mapped to SOLID / GRASP

| Problem | Location | Violation |
|---|---|---|
| `Feature` holds `MonoBehaviour` back-reference | `Feature.FeatureSetParent` | DIP — domain depends on infrastructure |
| Property setters call renderer directly | `Feature` setters | OCP / DIP — no observer interface |
| WCS transform logic in renderer | `FeatureSetRenderer.SpawnFeaturesFromTable` | SRP — parsing, transform, and construction in one method |
| Column schema owned by renderer | `FeatureSetRenderer.RawDataKeys/RawDataTypes` | Information Expert (GRASP) — schema belongs with the set |
| Export stub never implemented | `FeatureSetManager.ExportFeatureSet` | OCP — open for extension but no extension point exists |
| `GameObject.Find` calls throughout | `FeatureSetManager`, `FeatureMenuController`, `FeatureMenuCell` | DIP — concrete scene coupling, untestable |
| `AstTool` called from UI layer | `FeatureMenuController.UpdateInfo` | Layering violation — infrastructure dependency in presentation |
| `SourceStatsDict.ElementAt(SelectedFeature.Index).Value` | `FeatureMenuController.UpdateInfo` (l. 327–329), `SpectralProfileHelper.OnMaskedSourceSelected` (l. 107), `VoTableSaver.SaveFeatureSetAsVoTable` (l. 469–471) | Information Expert / correctness — keyed by *ordinal position in the dictionary*, not by `maskVal`. Iteration order is not guaranteed; a mask edit that re-inserts an entry can silently bind the wrong stats to the wrong feature. The refactor's `ISourceStatsProvider.GetStatsForSource(originId == maskVal)` lookup eliminates the ordinal coupling. |
| `FeatureMapper` static class is empty | `FeatureMapper.cs` | SRP gap — mapping logic has no home |
| `Config.Instance` accessed everywhere | Multiple files | DIP — singleton, no injection point |

---

## Target Architecture (Assignment §6.5)

The target design is presented in two parts: the **cross-team contract** (the agreed public API surface, defined canonically in `ST5_interface.md`) and the **internal design** (ST5 implementation choices, invisible to other teams).

### Cross-team contract — accepted SOLID / GRASP trade-offs

The target preserves several deliberate trade-offs against strict SOLID / GRASP, all justified below:

- **ISP — `IFeatureSetQuery`** merges query + display mutation + set-membership mutation + per-feature mutation + event. DD-9.
- **ISP — `IFeatureImportService`** combines mapping persistence with import. DD-10.
- **ISP — `ISourceStatsProvider`** exposes query + observable on one interface; split deferred to ST2. DD-11.
- **OCP — `SourceMappingOptions` enum.** Closed vocabulary (FITS standard) — adding a role requires editing the enum, so OCP closure is intentionally violated; alternatives (a string tag, an open registry) would lose compile-time exhaustiveness for no benefit at this vocabulary's expected churn rate.

`SelectionService.SelectAtCursor`'s inline AABB containment (no spatial index) is also a deliberate SRP-adjacent trade-off, but `SelectionService` is ST5-internal — see the Internal Design section.

The cross-team boundary names (`FeatureSetType` with `UserDefined` replacing the legacy `New` and `SelectionBox` replacing the legacy `Selection`; no `Unassigned` sentinel) are listed canonically in the interface document. The `SelectionBox` rename is motivated in DD-15; the per-feature `IsSelected` flag is unchanged.

### Boundary value types

All boundary types are plain C# — no `UnityEngine` types cross any interface. Full schemas in `ST5_interface.md` §3; domain-relevant notes (including the `CartesianCoord` precision rationale) in `ST5_domain_design.md` §4.2. One design choice not covered there: `FeatureColumnInfo.Ucd` carries the VOTable Unified Content Descriptor (e.g. `pos.eq.ra`, `phys.veloc`) — ST6's column-mapping UI should use it to auto-suggest `SourceMappingOptions` assignments.

### Consumed contract — prerequisites ST2 must guarantee

The ST5 internal services depend on guarantees from ST2's implementation that aren't expressible in the interface signatures alone:

- `ISourceStatsProvider.GetAllStats()` must return a fully populated dictionary before `FeatureFactory.PopulateFromSourceStats` is called.
- `ISourceStatsProvider.SourceStatsUpdated` must fire after any mask edit that changes source statistics; `FeatureSetService` refreshes the affected `Feature.Statistics` in response.
- `ICoordinateTransformer.Transform` must be available before any feature-table import or VOTable export is attempted.
- `IDataAnalysisPlugin.ComputeRegionStats` must be available before the spectral-profile-from-region use case is triggered.
- All coordinates ST2 emits must be in data-cube voxel space consistent with the loaded FITS axes — ST5 performs no coordinate transformation internally.

ST5 deliberately does **not** require access to the raw mask array, the FITS data buffer, or `AstFrameSet` directly — `SourceStats`, `WorldCoord`, and the three port methods listed in `ST5_interface.md` §2 are the entire consumed surface.

### Cross-team events

The three events (two produced by ST5 — `FeatureSetChanged`, `SelectionChanged`; one consumed from ST2 — `SourceStatsUpdated`), their owners, payloads, and trigger conditions are listed in `ST5_domain_design.md` §6.1–§6.2; threading guarantees and snapshot-invalidation rules in `ST5_interface.md` §6.

---

## Internal Design (ST5-only)

Everything below is invisible to other sub-teams. Other sub-teams must not take a dependency on any class or interface in this section.

### Layer map

The full layered architecture (Domain / Application / Infrastructure / Unity ACL with concrete class names) is documented in `ST5_domain_design.md` §7. Two hard constraints apply across all layers:

- `Feature`, `FeatureSet`, `FeatureSetService`, `FeatureFactory`, and all Infrastructure classes must have zero transitive dependency on `UnityEngine` or `SteamVR`.
- `FeatureVisualiser` is the only class permitted to hold a `ComputeBuffer`.

Ownership of `*Controller` MonoBehaviours follows brief §6.4 / §6.5, not a blanket rule. ST4 owns the input / state-machine controllers brief §6.4 enumerates (locomotion, voice, quick-menu, paint-menu). ST5 owns the feature-domain menus brief §6.5 enumerates (source-list, moment maps, spectral profiles, VOTable export). Each refactored controller holds the relevant ST5 service interfaces directly — no separate ACL wrapper.

### Catalogue I/O implementation notes

`FeatureTable`, `IFeatureCatalogueReader`, and `IFeatureCatalogueWriter` are listed in `ST5_interface.md` §1; full signatures in §3. Concrete realisations and dependency injection:

- `VoTableReader : IFeatureCatalogueReader` and `FitsTableReader : IFeatureCatalogueReader` live in `ST5 — Infrastructure`. Neither holds a reference to any ST2 port.
- `VoTableSaver : IFeatureCatalogueWriter` lives in `ST5 — Infrastructure` and is constructor-injected with `ISourceStatsProvider` and `ICoordinateTransformer` from ST2. Per DD-4, export logic is encapsulated in the writer rather than a separate `FeatureExporter` orchestrator — the writer is already the single class responsible for export, so a wrapper would add a delegation layer with no logic of its own.
- The reader is held by `FeatureImportService` (ST5 Application) for the import flow; the writer is held by `FeatureMenuController` (ST5 feature-domain menu — see Domain Design §7) for the export action. ST4 holds either port directly only when an interaction-driven action skips the menu (optional).
- The legacy `FeatureSetManager.ExportFeatureSet` empty stub is dropped — the working path was always `FeatureMenuController.SaveListAsVoTable → FeatureSetRenderer.SaveAsVoTable → static VoTableSaver.SaveFeatureSetAsVoTable`; the refactor collapses the chain to one writer call.

### `Feature` — concrete domain aggregate

`Feature` implements `IFeature`. The listener is owned by the parent `FeatureSet` (so `FeatureSetService` can construct new Mask features in `OnSourceStatsUpdated` without a side-channel listener lookup); `FeatureSet.AddFeature` wires it into the Feature via the internal `SetListener` method. `IFeatureDirtyListener` is not part of the cross-team contract:

```csharp
// Internal to ST5 — not visible to other sub-teams
internal interface IFeatureDirtyListener
{
    void OnFeatureDirty(int originId);
}

internal sealed class Feature : IFeature
{
    // Set by FeatureSet.AddFeature — the listener is owned by the set so every
    // Feature in the same set shares one IFeatureDirtyListener (the FeatureVisualiser).
    private IFeatureDirtyListener _listener;
    internal void SetListener(IFeatureDirtyListener listener) => _listener = listener;

    public Feature(int originId, string name, string flag, CartesianCoord center,
                   CartesianCoord size, IReadOnlyList<string> rawDataValues)
    {
        OriginId = originId; Name = name; Flag = flag; Center = center;
        Size = size; RawDataValues = rawDataValues;
    }

    public int OriginId { get; }
    public string Name { get; }
    public string Flag { get; private set; }
    public CartesianCoord Center { get; private set; }
    public CartesianCoord Size { get; private set; }
    public IReadOnlyList<string> RawDataValues { get; }
    public IFeatureStatistics? Statistics { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; _listener.OnFeatureDirty(OriginId); }
    }
    private bool _isSelected;

    public void SetFlag(string flag)
    {
        Flag = flag;
        _listener.OnFeatureDirty(OriginId);
    }

    // Called by FeatureSetService for two flows:
    //   1. SourceStatsUpdated — a mask edit changed this source's voxel extent.
    //   2. SetFeatureBounds — ST4 anchor-drag editing of a non-Mask feature.
    // Affects GPU vertex data, so OnFeatureDirty is called.
    public void UpdateGeometry(CartesianCoord center, CartesianCoord size)
    {
        Center = center; Size = size;
        _listener.OnFeatureDirty(OriginId);
    }

    // Called by FeatureSetService when ISourceStatsProvider.SourceStatsUpdated fires.
    // Statistics changes do not affect GPU vertex data, so OnFeatureDirty is not called.
    public void UpdateStatistics(IFeatureStatistics? stats) => Statistics = stats;
}
```

### `FeatureSet` — concrete mutable collection

`FeatureSet` implements `IFeatureSet` and adds mutation methods. It is `internal sealed` inside the ST5 assembly (DD-5); ST5 code holds `FeatureSet` references and calls `AddFeature`/`RemoveFeature` directly. Code outside the assembly cannot name the class and receives only `IFeatureSet`, which omits the mutators:

```csharp
internal sealed class FeatureSet : IFeatureSet
{
    private readonly List<IFeature> _features = new();
    // Callback injected by FeatureSetService at construction time.
    // Fires FeatureSetChanged whenever display state is mutated — ensuring the
    // event is raised regardless of call site (Protected Variations, GRASP).
    private readonly Action _onChanged;

    public FeatureSet(int index, string fileName, FeatureSetType type,
                      IReadOnlyList<string> rawDataKeys, FeatureColour displayColour,
                      IFeatureDirtyListener listener, Action onChanged)
    {
        Index = index; FileName = fileName; Type = type;
        RawDataKeys = rawDataKeys; _displayColour = displayColour;
        Listener = listener; _onChanged = onChanged;
        // Bypass the IsVisible setter — calling it during construction would
        // fire _onChanged (FeatureSetChanged) before any consumer can hold a
        // reference to this set. The empty-set event is raised once by
        // FeatureSetService.CreateSet after the set is registered.
        _isVisible = true;
    }

    public int Index { get; }
    public string FileName { get; }
    public FeatureSetType Type { get; }
    public IReadOnlyList<IFeature> Features => _features.AsReadOnly();
    public IReadOnlyList<string> RawDataKeys { get; }

    // Shared listener for every Feature attached to this set. Held here (rather
    // than on each Feature) so FeatureSetService can construct new Mask features
    // in OnSourceStatsUpdated without a side-channel listener lookup.
    internal IFeatureDirtyListener Listener { get; }

    private FeatureColour _displayColour;
    public FeatureColour DisplayColour
    {
        get => _displayColour;
        set { _displayColour = value; _onChanged(); }
    }

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; _onChanged(); }
    }

    // Membership mutators are intentionally silent. The calling service
    // (FeatureSetService / FeatureFactory) is responsible for raising
    // FeatureSetChanged once a batch of additions or removals is complete,
    // so bulk population does not emit one event per feature.
    public void AddFeature(IFeature feature)
    {
        ((Feature)feature).SetListener(Listener);
        _features.Add(feature);
    }
    public void RemoveFeature(IFeature feature) => _features.Remove(feature);
}
```

### `FeatureSetService` — application orchestrator

Replaces `FeatureSetManager`'s catalog-management role and absorbs the legacy stats-refresh logic in `VolumeDataSet.UpdateStats` (lines 528–587). Implements `IFeatureSetQuery`. Internally holds `FeatureSet` (concrete, mutable); externally returns `IFeatureSet` (read-only). Uses a `Dictionary<FeatureSetType, List<FeatureSet>>` rather than three separate named lists plus a Selection singleton, eliminating repeated type-dispatch switch statements:

`FeatureSetService` realises **two** interfaces: the cross-team `IFeatureSetQuery` (boundary-facing — query, display mutation, set-membership mutation, event) and the ST5-internal `IFeatureSetCatalog` (factory-style `CreateSet`, held by `FeatureImportService` to create empty sets before populating them). Boundary consumers (ST4, ST6, ST7) hold only `IFeatureSetQuery`; ST5-internal callers hold `IFeatureSetCatalog`. `FeatureFactory` does **not** hold `IFeatureSetCatalog` — it receives an already-created `FeatureSet` from its caller and only populates it. The class is `internal sealed` inside the ST5 assembly (DD-5) — cross-team code cannot reach the concrete at all.

`FeatureSetService` also owns the **Mask creation flow** — there is no separate orchestrator. On the first `SourceStatsUpdated` after a mask load (i.e. while the Mask set does not yet exist), the service lazily runs the five-step population sequence canonical in `ST5_domain_design.md` §6.3 (listener creation → set creation → visualiser binding → feature population → second `FeatureSetChanged`). Subsequent events refresh, append, or remove individual Mask features using the already-bound `set.Listener`. To do this the service holds `IVisualiserBinder`, `IFeatureFactory`, and `ISourceStatsProvider`. `IVisualiserBinder` is also used by the service's other lazy-creation paths (`CopyFeatureToUserDefined`, `SetSelectionBoxBounds`), which create a new set and attach one feature directly without going through `IFeatureFactory`.

```csharp
// ST5-internal — not part of the cross-team contract. Held by FeatureFactory
// and FeatureImportService; never injected into cross-team consumers.
internal interface IFeatureSetCatalog
{
    // Index is assigned by the service, not by the caller — see IFeatureSet.Index
    // in ST5_interface.md §3.
    FeatureSet CreateSet(string fileName, FeatureSetType type,
                         IReadOnlyList<string> rawDataKeys, FeatureColour colour,
                         IFeatureDirtyListener listener);
}

internal sealed class FeatureSetService : IFeatureSetQuery, IFeatureSetCatalog
{
    // Mask / Imported / UserDefined hold 0..* sets each. The SelectionBox slot
    // is a singleton (matches the legacy FeatureSetManager.SelectionFeatureSet
    // field): SetSelectionBoxBounds is its sole writer — it lazily creates the
    // set on first call and updates the single feature's bounds in-place after.
    private readonly Dictionary<FeatureSetType, List<FeatureSet>> _sets = new()
    {
        [FeatureSetType.Mask]         = new(),
        [FeatureSetType.Imported]     = new(),
        [FeatureSetType.UserDefined]  = new(),
        [FeatureSetType.SelectionBox] = new(), // length 0 or 1
    };

    public IReadOnlyList<IFeatureSet> GetAllFeatureSets()
        => _sets.Values.SelectMany(l => l).ToList<IFeatureSet>();

    public IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type)
        => _sets[type].ToList<IFeatureSet>();

    // SetVisible and SetDisplayColour delegate to FeatureSet's property setters, which
    // fire _onChanged (bound to FeatureSetChanged at construction). The downcast is safe:
    // FeatureSetService is the sole producer of IFeatureSet instances (see DD-5).
    public void SetVisible(IFeatureSet featureSet, bool visible)
        => ((FeatureSet)featureSet).IsVisible = visible;

    public void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour)
        => ((FeatureSet)featureSet).DisplayColour = colour;

    // ST4 anchor-drag editing of a non-Mask feature's bounding box.
    // Mask-feature bounds are owned by ST2's source statistics and must not be
    // mutated directly — they refresh via the SourceStatsUpdated path below.
    public void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax)
    {
        var owningSet = _sets.Values.SelectMany(l => l).First(s => s.Features.Contains(feature));
        if (owningSet.Type == FeatureSetType.Mask)
            throw new InvalidOperationException("Mask feature bounds are owned by ISourceStatsProvider.");
        var center = boundsMin.Add(boundsMax).Scale(0.5);
        var size   = boundsMax.Sub(boundsMin);
        ((Feature)feature).UpdateGeometry(center, size);
        FeatureSetChanged?.Invoke();
    }

    public void SetFeatureFlag(IFeature feature, string flag)
    {
        ((Feature)feature).SetFlag(flag);
        FeatureSetChanged?.Invoke();
    }

    // Appends the copy to a UserDefined set, creating one lazily if none exists.
    // Event count follows the bulk-population rule (ST5_interface.md §6).
    // Replaces FeatureSetManager.AddSelectedFeatureToNewSet.
    public void CopyFeatureToUserDefined(IFeature source) { /* ... */ }

    // Replaces the singleton SelectionBox set with one feature at the given bounds.
    // Event count follows the bulk-population rule (ST5_interface.md §6).
    // Replaces FeatureSetManager.CreateSelectionFeature.
    public void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax) { /* ... */ }

    // SourceStatsUpdated handler — subscribed during construction.
    // Four branches (bootstrap + three per-edit) and their event counts are
    // canonical in ST5_domain_design.md §6.2; the three per-edit branches
    // mirror VolumeDataSet.UpdateStats (lines 528–587), the bootstrap branch
    // is new behaviour (legacy code early-returned if _maskFeatureSet was null).
    private void OnSourceStatsUpdated(int originId) { /* ... */ }

    public event Action FeatureSetChanged;

    private int _nextIndex;

    public FeatureSet CreateSet(string fileName, FeatureSetType type,
                                IReadOnlyList<string> rawDataKeys, FeatureColour colour,
                                IFeatureDirtyListener listener)
    {
        // FeatureSetChanged is bound as the onChanged callback — any display-state
        // mutation on the FeatureSet will automatically raise it (Protected Variations).
        var set = new FeatureSet(_nextIndex++, fileName, type, rawDataKeys, colour,
                                 listener, onChanged: () => FeatureSetChanged?.Invoke());
        _sets[type].Add(set);
        FeatureSetChanged?.Invoke();
        return set;
    }
}
```

The three named lists in `FeatureSetManager` (`MaskFeatureSetList`, `ImportedFeatureSetList`, `NewFeatureSetList`) and the `SelectionFeatureSet` singleton are replaced by this single keyed store.

### `FeatureFactory` — feature construction

Extracts the ~250-line `SpawnFeaturesFromTable` method from `FeatureSetRenderer`. Owns the pipeline of: parse column values → transform WCS coordinates → construct `Feature` objects, then attach them to a set the caller already created. Realises the ST5-internal `IFeatureFactory` interface (held by `FeatureImportService` for catalogue-driven flows and by `FeatureSetService` for the Mask flow); the concrete is `internal sealed` and has no Unity dependency:

```csharp
// ST5-internal — not part of the cross-team contract.
internal interface IFeatureFactory
{
    // The caller owns set creation (see Domain Design §6.3). The set carries
    // its IFeatureDirtyListener (FeatureSet.Listener), so the factory only
    // parses inputs, builds Feature objects, and attaches them.
    void PopulateFromTable(FeatureSet target, FeatureTable table,
                           FeatureImportMapping mapping);
    void PopulateFromSourceStats(FeatureSet target);
}

internal sealed class FeatureFactory : IFeatureFactory
{
    private readonly ICoordinateTransformer _coords;
    private readonly ISourceStatsProvider _stats;

    public FeatureFactory(ICoordinateTransformer coords, ISourceStatsProvider stats)
    {
        _coords = coords; _stats = stats;
    }

    public void PopulateFromTable(
        FeatureSet target,
        FeatureTable table,
        FeatureImportMapping mapping)
    {
        // 1. Parse column values using mapping
        // 2. Transform sky/spectral coords via _coords.Transform(...)
        // 3. Construct Feature objects with CartesianCoord bounds; pass to
        //    target.AddFeature, which wires each Feature to target.Listener.
        // The caller raises FeatureSetChanged after the batch completes.
    }

    public void PopulateFromSourceStats(FeatureSet target)
    {
        // Constructs Mask-type features from native-layer source statistics.
        // Reads the dictionary from _stats.GetAllStats(). For each entry:
        //   1. Construct a Feature with bounds from SourceStats.
        //   2. Call feature.UpdateStatistics(stats) — establishes invariant 5.4
        //      BEFORE the feature is exposed.
        //   3. Call target.AddFeature(feature) — wires target.Listener and
        //      makes the feature observable via IFeatureSet.Features.
    }
}
```

`FeatureFactory` is independently testable: inject fakes for `ICoordinateTransformer` and `ISourceStatsProvider`; pass a synthetic `FeatureTable` and a stub `FeatureSet` (constructed with a no-op `IFeatureDirtyListener`); assert on the attached `Feature` list — no Unity context needed.

### `IVisualiserBinder` — composition-root bridge for listener wiring

`FeatureSet` is constructed with its `IFeatureDirtyListener`, but the listener (`FeatureVisualiser`) is a `MonoBehaviour` and must be instantiated by the Unity ACL. The Application layer therefore creates the listener via a binder *before* calling `IFeatureSetCatalog.CreateSet`, and supplies the set reference back to the visualiser once the set exists. Two methods, no circular initialisation:

```csharp
// ST5-internal — implemented by an ACL-layer orchestrator that knows how to
// instantiate FeatureVisualiser MonoBehaviours.
internal interface IVisualiserBinder
{
    /// <summary>Instantiates a homeless FeatureVisualiser. Dirty events received
    /// before AttachToSet are buffered.</summary>
    IFeatureDirtyListener CreateListener();

    /// <summary>Hands the set reference to a previously-created listener.
    /// After this call the visualiser drains its buffered dirty queue and
    /// uploads vertex data.</summary>
    void AttachToSet(IFeatureDirtyListener listener, IFeatureSet set);
}
```

`FeatureImportService` (for table-driven Imported sets) holds `IFeatureSetCatalog`, `IVisualiserBinder`, and `IFeatureFactory`. `FeatureSetService` (for the Mask flow — see above) implements `IFeatureSetCatalog` itself, and additionally holds `IVisualiserBinder` and `IFeatureFactory`. The canonical five-step population sequence (listener → set → bind → populate → second `FeatureSetChanged`) is documented in `ST5_domain_design.md` §6.3 and applies to both the import flow and the Mask bootstrap; in either case two events fire per bulk population, and consumers re-query on the second. UserDefined and SelectionBox sets are created by `FeatureSetService` directly (without `IFeatureFactory`) and follow the same event-count rules listed in `ST5_interface.md` §6.

### `SpectralProfileService` — region → spectral profile

Replaces the direct `DataAnalysis.GetSourceStats` P/Invoke in `SpectralProfileHelper.OnCroppedRegionChanged`. Holds the injected `IDataAnalysisPlugin` and repackages the resulting `SourceStats` into a slim `SpectralProfileResult` for ST6's spectral-profile menu:

```csharp
internal sealed class SpectralProfileService : ISpectralProfileService // (DD-5)
{
    private readonly IDataAnalysisPlugin _plugin;

    public SpectralProfileService(IDataAnalysisPlugin plugin) { _plugin = plugin; }

    public Task<SpectralProfileResult> ComputeForRegionAsync(
        CartesianCoord boundsMin, CartesianCoord boundsMax)
    {
        // Native call may be slow on large regions — run off the main thread.
        return Task.Run(() =>
        {
            var stats = _plugin.ComputeRegionStats(boundsMin, boundsMax);
            return new SpectralProfileResult(
                stats.SpectralProfile, stats.ZStartChannel, stats.TotalFlux, stats.PeakFlux);
        });
    }
}
```

`SpectralProfileHelper` is refactored in place to hold `ISpectralProfileService` instead of the static `DataAnalysis` reference. The spectral-profile menu is ST5-owned per brief §6.5; the controller stays in ST5's Unity ACL / menus layer. No Unity types cross the interface boundary, and the service is unit-testable with a fake `IDataAnalysisPlugin`.

### `SelectionService` — selection state

Owns the selection-tracking state: `SelectedFeature`, the `SelectionChanged` event, and an `ISelectionVisualiser` port for highlighting the selected region in the scene. `SelectionService` lives in the Application layer — the brief forbids any transitive `UnityEngine` dependency from non-ACL layers, so the visualiser is reached through an interface. The concrete is `internal sealed` inside the ST5 assembly (DD-5); cross-team consumers see only `IFeatureSelectionService`.

```csharp
// ST5-internal port — implemented by SelectionAnchorRenderer in the ACL.
// Application-layer SelectionService depends on this, not on the MonoBehaviour.
internal interface ISelectionVisualiser
{
    void ShowAt(IFeature feature, IFeatureSet owningSet);
    void Hide();
}

internal sealed class SelectionService : IFeatureSelectionService
{
    private readonly ISelectionVisualiser _anchors;      // DIP: depend on abstraction
    private readonly IFeatureSetQuery _setService;
    private readonly IActiveFeatureSetTypeProvider _activeType;
    public event Action<IFeature?> SelectionChanged;

    public SelectionService(ISelectionVisualiser anchors, IFeatureSetQuery setService,
                            IActiveFeatureSetTypeProvider activeType)
    {
        _anchors = anchors; _setService = setService; _activeType = activeType;
    }

    public IFeature? SelectedFeature { get; private set; }
    public IFeatureSet? SelectedFeatureSet { get; private set; }

    public bool SelectAtCursor(CartesianCoord cursorVoxelSpace)
    {
        // Linear AABB containment scan. iDaVIE catalogue sizes (≤ low thousands
        // of features per set) make a spatial index unnecessary — four
        // comparisons per box at this scale runs in well under a frame.
        // Search the active type's sets first (if a source-list panel is open),
        // then remaining types — replaces GameObject.Find("SourcesMenu").
        // ActiveType is nullable: null = no source-list open, so scan everything
        // in FeatureSetType-declaration order.
        var active = _activeType.ActiveType;
        IEnumerable<IFeatureSet> prioritised = active is { } t
            ? _setService.GetFeatureSetsByType(t)
                .Concat(_setService.GetAllFeatureSets().Where(s => s.Type != t))
            : _setService.GetAllFeatureSets();
        foreach (var set in prioritised)
        {
            foreach (var feature in set.Features)
            {
                var min = feature.Center.Sub(feature.Size.Scale(0.5f));
                var max = feature.Center.Add(feature.Size.Scale(0.5f));
                if (cursorVoxelSpace.X >= min.X && cursorVoxelSpace.X <= max.X &&
                    cursorVoxelSpace.Y >= min.Y && cursorVoxelSpace.Y <= max.Y &&
                    cursorVoxelSpace.Z >= min.Z && cursorVoxelSpace.Z <= max.Z)
                {
                    SelectFeature(feature, set);
                    return true;
                }
            }
        }
        return false;
    }

    public void SelectFeature(IFeature feature, IFeatureSet owningSet)
    {
        DeselectFeature();
        SelectedFeature = feature;
        SelectedFeatureSet = owningSet;
        ((Feature)feature).IsSelected = true;
        _anchors.ShowAt(feature, owningSet);
        SelectionChanged?.Invoke(feature);
    }

    public void DeselectFeature()
    {
        if (SelectedFeature != null)
        {
            ((Feature)SelectedFeature).IsSelected = false;
            _anchors.Hide();
            SelectedFeature = null;
            SelectedFeatureSet = null;
            SelectionChanged?.Invoke(null);
        }
    }
}
```

### `SelectionAnchorRenderer` — bounding-box corner handles

Extracts the 8 `_anchorColliders` GameObjects from `FeatureSetManager`. This is a pure Unity rendering concern:

```csharp
internal sealed class SelectionAnchorRenderer : MonoBehaviour, ISelectionVisualiser
{
    [SerializeField] private GameObject _anchorPrefab;
    private readonly GameObject[] _anchors = new GameObject[8];

    private void Awake() { /* instantiate 8 anchor GameObjects */ }

    public void ShowAt(IFeature feature, IFeatureSet owningSet) { /* position anchors */ }
    public void Hide() { /* scale anchors to zero */ }
}
```

`SelectionAnchorRenderer` is wired to the scene via Unity Inspector serialization — no `GameObject.Find` calls. The composition root passes the `MonoBehaviour` as `ISelectionVisualiser` when constructing `SelectionService`, so the Application layer holds no `UnityEngine` type.

### `IActiveFeatureSetTypeProvider` — replaces `GameObject.Find` in selection (ST5-internal)

`SelectionService.SelectAtCursor(CartesianCoord cursorVoxelSpace)` needs to know which feature-set type the user is currently working with so it can prioritise that type during spatial search. Currently `FeatureSetManager` calls `GameObject.Find("SourcesMenu")` at runtime. Replace with an internal interface implemented by ST5's own source-list menu controller (the refactored `FeatureMenuController`, owned by ST5 per brief §6.5 "source-list statistics"):

```csharp
// ST5-internal — both producer (FeatureMenuController) and consumer
// (SelectionService) are ST5-owned.
internal interface IActiveFeatureSetTypeProvider
{
    /// <summary>The user's current working type, or null if no source-list
    /// panel is currently open. SelectionService falls back to scanning all
    /// sets in FeatureSetType-declaration order on null.</summary>
    FeatureSetType? ActiveType { get; }
}
```

The "active type" is interaction state set by the source-list menu (tab click) and by the `DisplayNextSet`/`DisplayPreviousSet` voice commands. The source-list menu is ST5-owned per brief §6.5; voice commands route through ST4's voice subsystem but flip a property that the ST5-owned source-list menu controller exposes — so the producer of this signal is ST5's `FeatureMenuController`, not a cross-team interface. `SelectionService` holds a constructor-injected reference. The scene object lookup happens once at composition-root wiring time via Unity Inspector, never at query time. `ActiveType` returns null when no source-list panel is currently open — `SelectionService` then falls back to scanning all loaded sets in `FeatureSetType`-declaration order.

### `FeatureVisualiser` — GPU rendering (replaces `FeatureSetRenderer`)

Owns the `ComputeBuffer` and implements `IFeatureDirtyListener`. One `FeatureVisualiser` per `FeatureSet`:

```csharp
internal sealed class FeatureVisualiser : MonoBehaviour, IFeatureDirtyListener
{
    // Constants unchanged from FeatureSetRenderer
    private const int VerticesPerFeature = 24;
    private const int BytesPerVertex = 32;

    private ComputeBuffer _buffer;
    private readonly Queue<int> _dirtyQueue = new();

    public void OnFeatureDirty(int originId) => _dirtyQueue.Enqueue(originId);

    private void Update()
    {
        // Drain dirty queue, reallocate buffer if needed, upload changed vertices
        // Convert CartesianCoord → Vector3 here (only Unity type conversion site)
    }

    private void OnRenderObject()
    {
        // Graphics.DrawProceduralNow(MeshTopology.Lines, featureCount * VerticesPerFeature)
    }
}
```

---

## Design Decisions

Each decision below records the choice made and its rationale — including alternatives evaluated, where relevant — so the same ground is not re-covered in future sprints.

---

### DD-1: `FeatureSetType` is a property of `IFeatureSet`, not `IFeature`

**Decision:** `IFeature` has no type discriminator. `IFeatureSet.Type` carries the type for the whole collection.

**Alternative considered:** Add `FeatureSetType FeatureType { get; }` to `IFeature` so each feature instance carries its own type tag.

**Rationale:** `FeatureSetManager.CreateEmptyFeatureSet` (line 185 of the current codebase) stamps every renderer with a single `FeatureSetType` at construction; the type is never changed and the three type-specific lists (`MaskFeatureSetList`, `ImportedFeatureSetList`, `NewFeatureSetList`) and the `SelectionFeatureSet` singleton are strictly segregated by type. No renderer ever holds features of mixed types. The type is therefore a property of the collection, not the element. Putting it on `IFeature` would create redundant data with no corresponding invariant to enforce, and would mislead readers into thinking per-feature type discrimination is meaningful.

---

### DD-2: Statistics are on `IFeature` via a separate `IFeatureStatistics` interface, not inline

**Decision:** `IFeature` exposes `IFeatureStatistics? Statistics { get; }`. The statistics interface is separate from the geometry/identity properties, and returns null for Imported and UserDefined features that have no native statistics.

**Alternative 1 considered:** Inline `VoxelCount`, `TotalFlux`, `PeakFlux`, `FluxWeightedCentroid`, `ChannelW20`, `VeloW20`, `ChannelVsys`, `VeloVsys` directly on `IFeature`.

Rejected: would expand `IFeature` to 15 members and conflate two distinct concerns — feature identity and feature statistics — in one interface.

**Alternative 2 considered:** Exclude statistics from `IFeature` entirely; consumers query `ISourceStatsProvider.GetStatsForSource(feature.OriginId)` from ST2 instead.

Rejected: contradicts the brief's explicit requirement for "GRASP Information Expert for feature-derived statistics on the Feature aggregate." Information Expert assigns responsibility to the class that has the information. Once statistics are populated from the DataAnalysis plug-in at feature creation time, the Feature aggregate is the appropriate holder.

**Rationale for chosen approach:** `IFeatureStatistics` is a separate interface, keeping `IFeature` within a manageable member count (OriginId, Name, Flag, Center, Size, IsSelected, RawDataValues, Statistics = 8). Statistics are populated by `FeatureFactory` when creating Mask features, using data from `ISourceStatsProvider`. When mask edits change statistics, `ISourceStatsProvider.SourceStatsUpdated` fires and `FeatureSetService` refreshes the affected `Feature.Statistics`. Feature remains the Information Expert — it stores and provides the statistics; it does not compute them.

---

### DD-3: `DisplayColour` and `IsVisible` are getter-only on `IFeatureSet`; mutation goes through `IFeatureSetQuery`

**Decision:** `IFeatureSet` exposes `DisplayColour` and `IsVisible` as read-only getters. External consumers that need to mutate these call `IFeatureSetQuery.SetVisible(IFeatureSet, bool)` or `SetDisplayColour(IFeatureSet, FeatureColour)`.

**Alternative considered:** Expose `{ get; set; }` directly on `IFeatureSet`.

**Rationale:** Any mutation that should cause `FeatureSetChanged` to fire must pass through `FeatureSetService`. If `IFeatureSet` exposed setters, any consumer — including sub-team 6 — could change visibility or colour without ST5 being notified, breaking the event contract. Routing through `IFeatureSetQuery` service methods gives ST5 a single, enforceable control point for state changes that have external observers.

---

### DD-4: `IFeatureCatalogueReader` and `IFeatureCatalogueWriter` are separate interfaces

**Decision:** Two distinct port interfaces rather than a single combined `IFeaturePersistence`.

**Alternative considered:** One `IFeaturePersistence` interface with both `Load(string) : FeatureTable` and `Save(IFeatureSet, string)` methods.

**Rationale:** Reading and writing have different dependency profiles. `IFeatureCatalogueWriter` implementations require `ISourceStatsProvider` (to resolve flux-weighted centroids) and `ICoordinateTransformer` (to convert pixel positions to sky coordinates for the output file). `IFeatureCatalogueReader` implementations require neither. A combined interface would force every reader to declare dependencies it never uses, violating ISP. Separating them also allows a future read-only adapter (e.g. a FITS binary table reader) to be added without touching the write-path interfaces.

---

### DD-5: Concrete classes are `internal` to an ST5 assembly; boundary is interface-only

**Decision:** Cross-team consumers see only interfaces (`IFeature`, `IFeatureSet`, `IFeatureSetQuery`, …) and boundary value types — these are `public`. Every concrete realisation (`Feature`, `FeatureSet`, `FeatureStatistics`, `FeatureSetService`, `SelectionService`, `FeatureFactory`, `FeatureImportService`, `SpectralProfileService`, `VoTableReader`, `FitsTableReader`, `VoTableSaver`) is `internal sealed` inside a dedicated ST5 assembly definition. Two ST5 services intentionally downcast within the assembly — `FeatureSetService.SetVisible` / `SetDisplayColour` cast `IFeatureSet → FeatureSet`; `SelectionService.SelectFeature` casts `IFeature → Feature` to set `IsSelected`.

**Why an `.asmdef` is in scope:** without an assembly boundary `internal` is toothless in iDaVIE's single `Assembly-CSharp` layout. ST5 ships its own `.asmdef` so the access modifier carries weight: cross-team code cannot name `FeatureSet`, cannot cast to it, and cannot reach the mutators. The assembly split is a deliberate sub-team deliverable, not a deferred concern.

**Why the downcasts are safe:** the assembly boundary makes ST5 the sole producer of `IFeature` / `IFeatureSet` instances — no foreign implementor can be passed in. `Feature` and `FeatureSet` are the only concretes. `SelectionAnchorRenderer.ShowAt` takes `IFeatureSet` rather than the concrete, so the ACL holds no downcast.

**Alternatives considered and rejected:**
- *`{ get; set; }` on `IFeatureSet`* — bypasses `FeatureSetChanged` (see DD-3).
- *`public sealed` concretes with no assembly split* — interface-shaped hiding only; relied on consumers' good behaviour rather than the compiler.
- *Parallel `Dictionary<IFeatureSet, FeatureSet>`* — unnecessary indirection.
- *Generic `FeatureSet<TFeature>`* — covariance complexity for no expressiveness gain; flavours are structurally identical.

**Constraint for future maintainers:** Do not introduce a second `IFeature` or `IFeatureSet` implementor inside the assembly without revisiting this reasoning.

---

### DD-6: `FeatureSet` vs `FeatureCatalog` naming

The brief (§6.5) names "FeatureCatalog (persistence + identity)". This design splits that responsibility: `FeatureSet` owns in-memory identity; `IFeatureCatalogueReader` / `IFeatureCatalogueWriter` own persistence. The combined name describes the responsibility from the outside; separating it internally is the intent of the refactoring exercise.

---

### DD-7: Moment maps and spectral profiles use separate seams (GPU vs. native plug-in)

**Decision:** ST5 exposes two distinct application-layer services for the two analysis use cases ST6 needs:

- `IMomentMapService` — realised by `MomentMapServiceAdapter` in ST5's Application layer (plain C#), which delegates to ST3's `IMomentMapRenderer` (M-08). The MonoBehaviour that bridges `IMomentMapRenderer` to the GPU `MomentMapRenderer` lives in ST3, not ST5; `MomentMapServiceAdapter` does not touch `IDataAnalysisPlugin` or any native DLL.
- `ISpectralProfileService` — backed by `IDataAnalysisPlugin.ComputeRegionStats` (which wraps the native `DataAnalysis.GetSourceStats`), realised by `SpectralProfileService` in the Application layer. Replaces the existing direct P/Invoke in `SpectralProfileHelper.OnCroppedRegionChanged`.

**Rationale:** The two use cases have different computation back-ends. Moment maps are GPU-resident operations (render textures) with no native-plugin equivalent. Spectral profiles require voxel-level access from the C++ plugin. Conflating them into a single interface would mix GPU and native-DLL concerns and force every consumer to depend on both back-ends; separating them lets each be abstracted at the seam appropriate to its computation path.

---

### DD-8: `Feature.SetCubeColors()` extracted from the domain class

The static method `Feature.SetCubeColors(CuboidLine, Color, bool)` (`Feature.cs:187`) is removed — it takes `UnityEngine.Color` and `CuboidLine`, making the domain class transitively Unity-dependent. The only callers in the audit are `VolumeDataSetRenderer.cs:438` (`_cubeOutline`) and `:860` (`_regionOutline`) — both ST3 volume-rendering concerns; no caller actually colours a feature bounding box. ST3 re-homes the helper. ST5's `FeatureVisualiser` colours feature outlines inline at its existing `CartesianCoord → Vector3` boundary.

---

### DD-9: `IFeatureSetQuery` combines query, display mutation, set-membership mutation, per-feature mutation, and event — ISP trade-off accepted

**Decision:** `IFeatureSetQuery` exposes the query getters, `SetVisible`/`SetDisplayColour` display mutators, `CopyFeatureToUserDefined` / `SetSelectionBoxBounds` set-membership mutators, `SetFeatureBounds` / `SetFeatureFlag` per-feature mutators, and the `FeatureSetChanged` event — all on one interface.

**Alternative considered:** Four-way split — `IFeatureSetReader` (query + event), `IFeatureSetDisplayMutator` (SetVisible, SetDisplayColour), `IFeatureSetMembershipMutator` (CopyFeatureToUserDefined, SetSelectionBoxBounds), `IFeatureMutator` (SetFeatureBounds, SetFeatureFlag).

**Rationale:** Every identified consumer holds at least two slices. ST4's input flows mutate and then re-query after `FeatureSetChanged` (`SetFeatureBounds` from anchor-drag editing, `SetSelectionBoxBounds` from selection-box drag, `CopyFeatureToUserDefined` from edit gestures); ST6's import flow needs `CopyFeatureToUserDefined` plus the query surface to render results, and the ST5-owned source-list controller drives `SetFeatureFlag`. Splitting buys no decoupling — every consumer ends up holding multiple slices. The split should be revisited when a consumer surfaces that genuinely uses only one slice (e.g. an analytics or telemetry component that reads but never mutates).

**Why per-feature mutators live on a set-level interface:** `IFeature` is read-only (DD-14 keeps the domain interface narrow and prevents callers from bypassing the change-notification path). The mutation methods therefore have to live on a service. Adding a second service interface for two methods would force every consumer that already holds `IFeatureSetQuery` to also inject it; co-locating them is the smaller surface.

---

### DD-10: `IFeatureImportService` retains mapping persistence — ISP trade-off accepted

**Decision:** `IFeatureImportService` includes `LoadMappingFromFile`/`SaveMappingToFile` alongside `GetColumns`/`ImportFromFile`, rather than extracting a separate `IFeatureMappingRepository`.

**Alternative considered:** `IFeatureMappingRepository { FeatureImportMapping Load(string); void Save(FeatureImportMapping, string); }` consumed separately by the caller.

**Rationale for accepted trade-off:** The caller (`CanvassDesktop`) uses mapping persistence and import together in a single workflow: load mapping → show column UI → confirm → import. Splitting into two injected interfaces increases constructor surface area for the caller with no corresponding reduction in coupling (the caller always needs both). The mapping file format is also internal to the import workflow — exposing it as a separate interface risks callers treating a mapping file as a first-class resource independent of import, which it is not.

---

### DD-11: `ISourceStatsProvider` — ISP violation noted, deferred to ST2

**Finding:** `ISourceStatsProvider` violates ISP: `VoTableSaver` needs only `GetStatsForSource`; `FeatureFactory` needs only `GetAllStats`; `FeatureSetService` needs only `SourceStatsUpdated`. No consumer needs all three simultaneously.

**Decision:** The split (`ISourceStatsQuery` + `ISourceStatsObservable`) is correct in principle but deferred because `ISourceStatsProvider` is ST2's contract to deliver, not ST5's to define unilaterally. ST5 will raise this as a contract-change request to ST2 tagged `contract-change` per the versioning policy. Until that is done, all three consumers hold the full `ISourceStatsProvider` reference.

---

### DD-12: Pure-data carriers cross the boundary as records and sealed classes, not interfaces

**Decision:** Supporting types that carry only immutable data — `FeatureTable`, `FeatureImportMapping`, `FeatureColumnInfo`, `SpectralProfileResult`, `SourceStats`, `CartesianCoord`, `FeatureColour`, `WorldCoord` (and the ST3-owned `MomentMapResult` per `interface_resolutions.md` line 13) — are declared as `readonly record struct` or `sealed class` with `init`-only properties. The brief's "every public API boundary must be an interface" constraint applies to behavioural boundaries (services, ports, query and mutation surfaces), not to pure data carriers.

**Rationale:** An interface adds value when an implementation needs to be hidden behind a stable contract — for polymorphism or test substitution. Pure data carriers have no implementation to hide: there is only one correct way to be `MomentMapResult(MomentOrder Order, int Width, int Height, float[] Values, float MinValue, float MaxValue)` (ST3-owned per `interface_resolutions.md` line 13; schema in `shared_interfaces.md` §3.3), the fields *are* the contract, and equality and hashing follow structurally. An `IMomentMapResult` indirection would force virtual property calls to read fields that already have one canonical layout, with no test or polymorphism benefit. Mutability is constrained by the type system itself — `readonly record struct` and `init`-only setters make these values impossible to mutate after construction, so the boundary needs no behavioural guard.

**Where the rule still applies:** Every interface-shaped contract on the boundary — `IFeature`, `IFeatureSet`, `IFeatureStatistics`, `IFeatureSetQuery`, `IMomentMapService`, `ISpectralProfileService`, `IFeatureSelectionService`, `IFeatureImportService`, `IFeatureCatalogueReader`, `IFeatureCatalogueWriter`, plus the ST2-provided `ISourceStatsProvider`, `ICoordinateTransformer`, `IDataAnalysisPlugin` — is an interface per the brief.

---

### DD-13: W50 statistic excluded — suspected brief error

**Finding:** The brief lists three Mask-feature statistics invariants, one constraining W50 (line width at 50% of peak). W50 is not among the statistics ST2 is required to produce — `DataAnalysis.SourceStats` computes only `channelW20`/`veloW20`, and no ST2 deliverable adds W50. An invariant cannot be enforced against a value the architecture never computes.

**Secondary concern:** As written the invariant reads `W20 ≤ W50`, which is physically inverted. W20 is measured at 20% of peak flux — lower on the line profile — so it is the wider width: `W50 ≤ W20`.

**Decision:** W50 is excluded from `IFeatureStatistics`, which exposes the line width as `ChannelW20` (cube channels) and `VeloW20` (velocity), alongside the systemic position `ChannelVsys` / `VeloVsys` — matching the four fields the legacy `VolumeDataSet.UpdateStats` (lines 562 and 579) packs into `Feature.RawData` from `DataAnalysis.SourceStats`. Two invariants are retained: centroid inside bounding box, and flux non-negative.

**Action:** ST5 will raise the W50 invariant with the assessment owners as a suspected brief error. If W50 is genuinely required it must first become an ST2 deliverable, and the invariant direction corrected to `W50 ≤ W20`.

---

### DD-14: Per-feature `Color` and `Visible` removed; display state lives only on `IFeatureSet`

**Decision:** `IFeature` exposes no display colour and no per-feature visibility flag. Colour and visibility live on `IFeatureSet` only, as `DisplayColour` and `IsVisible`. The transient per-feature `IsSelected` flag remains because selection is inherently per-feature, not per-set.

**What the legacy code had:** `Feature.CubeColor (Color)` and `Feature.Visible (bool)`, set via property setters that called back into the renderer. `CubeColor` was unused at the call sites enumerated in DD-8 (its only consumers passed `Color` to `CuboidLine`, a ST3 concern); `Visible` was wired but had no UI surfacing it independently of set-level toggles.

**Rationale:** Removing both fields shrinks `IFeature` from 10 members to 8 and eliminates two Unity-coloured (`UnityEngine.Color`) properties from a domain interface. Set-level display is sufficient for every currently shipping use case — bounding-box outlines for an `Imported` set are drawn in one colour; visibility is toggled per set in the source-list menu. Future per-feature emphasis (e.g. highlighting outliers) is an additive interface change, not a removal.

**Migration note:** Sub-team 7 (workspace persistence) snapshots `IFeatureSet.DisplayColour` and `IsVisible`; nothing snapshots per-feature colour or visibility today, so no save-format change is induced.

**Why `Flag` is also read-only on `IFeature`:** the same reasoning applies — keeping `IFeature` interface narrow and forcing every mutation through a single change-notification path (`FeatureSetChanged`). Flag mutation is exposed as `IFeatureSetQuery.SetFeatureFlag(IFeature, string)`; consumers (ST5's own source-list controller, plus any cross-team caller that needs to flag features) call the service rather than the concrete.

---

### DD-15: `FeatureSetType.SelectionBox` distinguishes the transient region from per-feature selection

**Decision:** The legacy `FeatureSetType.Selection` enum value is renamed to `SelectionBox`. The set-level mutator that replaces `FeatureSetManager.CreateSelectionFeature` is named `SetSelectionBoxBounds` to carry the same `Box` qualifier. The per-feature `IsSelected` flag, `IFeatureSelectionService`, `SelectionChanged`, and `SelectedFeature`/`SelectedFeatureSet` are all unchanged.

**Why two names are needed:** The legacy code uses "Selection" for two unrelated concepts. (1) `FeatureSetType.Selection` is the set type that holds the *single transient bounding-box feature* the user draws to mark a region of interest — created via `SetSelectionBounds`, OriginId always `-1`. (2) `Feature.IsSelected` and `SelectionService.SelectedFeature` are the *currently focused feature in the source list* — typically a Mask feature the user has clicked. A reader of the legacy code cannot tell which "selection" any given comment refers to.

**Rationale:** Renaming the set-type cleanly removes the ambiguity. After the rename, "Selection" unqualified refers only to per-feature focus; "SelectionBox" refers only to the transient region-of-interest feature/set. No clarifying sentence is needed at every call site.

---

## Worked Refactoring Example 1: Moment-map generation as an application-layer use case

**Before (current) — Unity menu script holds a direct VolumeDataSetRenderer reference to reach the GPU renderer:**
```csharp
// MomentMapMenuController.cs (under Menu/) — MonoBehaviour
// Holds VolumeDataSetRenderer[] and navigates to MomentMapRenderer through it
private VolumeDataSetRenderer[] dataSets;

public void SetThresholdType()
{
    // Navigates three hops: menu → VolumeDataSetRenderer → MomentMapRenderer
    getFirstActiveDataSet().GetMomentMapRenderer().UseMask = false;
    // ...
    getFirstActiveDataSet().GetMomentMapRenderer().CalculateMomentMaps();
    ThresholdTypeText.text = (ThresholdType)thresholdType + "";
}

public void SetMomentMapThreshold()
{
    // Same three-hop coupling for every threshold change
    getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold = _threshold;
}
```

Problems: The menu depends on `VolumeDataSetRenderer` (ST3's god-class) just to reach `MomentMapRenderer`; `MomentMapMenuController` cannot be tested without a full `VolumeDataSetRenderer` scene hierarchy; there is no seam to swap the computation implementation.

**After (target) — use case exposed as an application-layer interface; Unity GPU coupling contained in the ACL:**
```csharp
// IMomentMapService.cs — Application layer port (ST5-owned, cross-team contract)
public interface IMomentMapService
{
    /// <summary>Triggers GPU moment-map computation and returns the result as plain C# data.
    /// momentOrder: 0 = integrated intensity, 1 = velocity field.
    /// threshold: intensity cut-off passed to MomentMapRenderer.MomentMapThreshold.
    /// useMask: passed to MomentMapRenderer.UseMask.
    /// MomentMapResult and MomentOrder are owned by ST3 per
    /// interface_resolutions.md line 13 (iDaVIE.Rendering.Contracts).</summary>
    Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask);
}

// MomentMapResult / MomentMapRequest / MomentOrder are owned by ST3 per
// interface_resolutions.md line 13 — declared in iDaVIE.Rendering.Contracts.
// Schema in shared_interfaces.md §3.3:
//   record struct MomentMapResult(MomentOrder Order, int Width, int Height,
//                                 float[] Values, float MinValue, float MaxValue);
//   record struct MomentMapRequest(MomentOrder Order, float Threshold,
//                                  bool UseMask, bool UseZScale, bool Inverted);

// MomentMapServiceAdapter.cs — ST5 Application layer (plain C#); wraps ST3's
// IMomentMapRenderer (M-08). The MonoBehaviour that converts RenderTexture →
// float[] now lives in ST3, behind IMomentMapRenderer.
internal sealed class MomentMapServiceAdapter : IMomentMapService
{
    private readonly IMomentMapRenderer _renderer; // ST3-owned (M-08)

    public MomentMapServiceAdapter(IMomentMapRenderer renderer)
        => _renderer = renderer;

    public Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask)
        => _renderer.RenderMomentMap(
            new MomentMapRequest(
                Order:     (MomentOrder)momentOrder,
                Threshold: threshold,
                UseMask:   useMask,
                UseZScale: false,
                Inverted:  false),
            CancellationToken.None);
}

// MomentMapMenuController.cs — ST5-owned (brief §6.5 "moment maps"); refactored in place
internal sealed class MomentMapMenuController : MonoBehaviour
{
    [SerializeField] private RawImage _displayPanel;
    private IMomentMapService _service;   // ST5 contract; injected via composition root

    private async void OnThresholdChanged(float threshold, bool useMask)
    {
        var result = await _service.GenerateAsync(_momentOrder, threshold, useMask);
        // Unity Texture2D conversion happens here only.
        // result.Values is the row-major float[] payload per shared_interfaces.md §3.3.
        var tex = new Texture2D(result.Width, result.Height, TextureFormat.RFloat, false);
        tex.SetPixelData(result.Values, 0);
        tex.Apply();
        _displayPanel.texture = tex;
    }
}
```

The coupling from `MomentMapMenuController` to `VolumeDataSetRenderer.GetMomentMapRenderer()` is severed. The refactored controller (ST5-owned per brief §6.5 "moment maps") only holds an `IMomentMapService` reference — injected by the composition root, not discovered via `GetComponentsInChildren`. `MomentMapServiceAdapter` is now plain ST5 Application code that delegates to ST3's `IMomentMapRenderer` (M-08); the MonoBehaviour that drives `MomentMapRenderer.CalculateMomentMaps()` and reads the render texture lives in ST3. The service can be faked in tests by substituting a spy `IMomentMapService` that returns a known `MomentMapResult` without any GPU context.

---

## Worked Refactoring Example 2: VOTable Export via `IFeatureCatalogueWriter`

**Before (current):**
```csharp
// FeatureSetManager.cs:462 — empty stub, never called
public void ExportFeatureSet(FeatureSetRenderer setToExport, string FileName) { }

// FeatureMenuController.cs:399 — the actual export entry point
public void SaveListAsVoTable()
{
    // ... resolve Outputs/Catalogs directory + timestamped filename ...
    _featureSetRendererList[CurrentFeatureSetIndex].SaveAsVoTable(path);
}

// FeatureSetRenderer.cs:612 — one-line delegation to the static saver
public void SaveAsVoTable(string filePath)
    => VoTableSaver.SaveFeatureSetAsVoTable(this, filePath);

// VoTable.cs:430 — static class in namespace VoTableReader; reaches the renderer to fetch centroids and the AST frame
public static class VoTableSaver
{
    public static void SaveFeatureSetAsVoTable(FeatureSetRenderer featureSet, string filePath)
    {
        // ...
        centerX = featureSet.VolumeRenderer.SourceStatsDict.ElementAt(i).Value.cX;  // SourceStatsDict on the renderer
        AstTool.Transform3D(featureSet.VolumeRenderer.AstFrame, centerX, centerY, centerZ, 1, out ra, out dec, out zPhys);
        AstTool.Norm(featureSet.VolumeRenderer.AstFrame, ra, dec, zPhys, out normR, out normD, out normZ);
        // ... write VOTable XML ...
    }
}
```

**After (target):**
```csharp
// VoTableSaver.cs — Infrastructure; implements the write port; no UnityEngine import
internal sealed class VoTableSaver : IFeatureCatalogueWriter
{
    private readonly ISourceStatsProvider _stats;
    private readonly ICoordinateTransformer _coords;

    public VoTableSaver(ISourceStatsProvider stats, ICoordinateTransformer coords)
    { _stats = stats; _coords = coords; }

    public void Write(IFeatureSet featureSet, string filePath)
    {
        foreach (var feature in featureSet.Features)
        {
            var centroid = _stats.GetStatsForSource(feature.OriginId)?.FluxWeightedCentroid
                           ?? feature.Center;
            var world = _coords.Transform(centroid);
            // write world.RightAscension, world.Declination, world.SpectralValue,
            // world.SpectralUnit to VOTable XML ... (WorldCoord schema: shared_interfaces.md §2)
        }
    }
}

// FeatureMenuController.cs — ST5-owned (brief §6.5 "source-list statistics" + "VOTable export");
// refactored in place; holds the writer directly
private IFeatureCatalogueWriter _writer;   // ST5 contract; injected via composition root
private IFeatureSetQuery _query;           // ST5 contract; injected via composition root

// The set the user is currently viewing in the source-list panel. Tracks the
// tab the user has selected — same role as the legacy CurrentFeatureSetIndex
// in the un-refactored FeatureMenuController.SaveListAsVoTable. Updated on
// tab-click and on FeatureSetChanged (re-resolves the tab to the current
// snapshot from _query). IActiveFeatureSetTypeProvider.ActiveType is derived
// from this field as _activeSet?.Type.
private IFeatureSet _activeSet;

private void OnExportClicked()
    => _writer.Write(_activeSet, _fileBrowser.SelectedPath);
```

---

## Testing Strategy

All domain and application layer tests run as plain NUnit tests — no Unity Play Mode required.

**Feature statistics invariant tests (worked statistics test specification — required deliverable):**

These property-based tests verify the statistics invariants for any Mask feature:
- **Centroid inside bbox:** for all axes, `feature.Center - feature.Size/2 ≤ feature.Statistics.FluxWeightedCentroid ≤ feature.Center + feature.Size/2`
- **Flux non-negative:** `feature.Statistics.TotalFlux ≥ 0` and `feature.Statistics.PeakFlux ≥ 0`

The brief's third invariant constrains W50, which is excluded as a suspected brief error — see DD-13.

Imported and UserDefined features must return `Statistics == null` — assert this for all non-Mask features.

**`FeatureFactory` unit tests:**
- Given an empty `FeatureSet`, a `FeatureTable` with N rows, and a fake `ICoordinateTransformer`, `PopulateFromTable` leaves the set with `Features.Count == N` and each `Feature.Center` matching the expected transformed coordinate
- Given an empty Mask `FeatureSet` and a fake `ISourceStatsProvider` returning known statistics, `PopulateFromSourceStats` leaves each `Feature.Statistics` populated and satisfying invariants 5.2 and 5.3
- Given an empty Imported `FeatureSet`, `PopulateFromTable` leaves every `Feature.Statistics == null`
- `Feature.UpdateGeometry` preserves invariant 5.2 when both bounds and statistics are refreshed from the same `SourceStats` snapshot (the `SourceStatsUpdated` path)

**`VoTableSaver` unit tests:**
- Given an `IFeatureSet` and a spy `ISourceStatsProvider` returning known centroids, assert the written VOTable XML contains the expected RA/Dec/Z values
- Assert `VoTableSaver` can be constructed and called with no Unity assemblies on the class path

**`MomentMapServiceAdapter` integration tests:**
- `MomentMapServiceAdapter` wraps `MomentMapRenderer` (Unity ACL) and cannot be unit-tested without a GPU context
- Instead, test the `IMomentMapService` seam: given a fake `IMomentMapService` returning a known `MomentMapResult`, assert that the refactored ST5-owned `MomentMapMenuController` calls `GenerateAsync` with the correct `momentOrder`, `threshold`, and `useMask` values, and that the resulting `Values` array (per ST3's `MomentMapResult` schema) is applied to the display texture
- Assert that the refactored `MomentMapMenuController` never accesses `VolumeDataSetRenderer` directly — only through `IMomentMapService`

**Scenario test: mask → masked features → edited feature → exported VOTable**
1. Load a mask FITS file → assert Mask `FeatureSet` is created with features matching the mask's source count
2. Verify each Mask feature's `Statistics` satisfies both invariants
3. Select a feature and edit its flag → assert the change propagates through the `FeatureSet`
4. Export to VOTable → reload via `VoTableReader` → assert round-trip equality of bounds, flags, and statistics centroid coordinates

---

## Sub-Team 5 Deliverables Checklist

The three deliverables named in the brief (§6.5) are in **bold**.

- [ ] **Feature domain design document** — covers requirements engineering (current + future), target architecture, SOLID/GRASP audit, design decisions log, risk register
- [ ] **Feature aggregate UML class diagram + invariants list** — to-be class diagram split into public contract layer and internal layer; invariants list covering the two statistics invariants and the FeatureSet homogeneity invariant
- [ ] **Worked statistics test specification** — NUnit test skeletons for both statistics invariants; scenario test for mask → masked features → edited feature → exported VOTable

Supporting evidence expected in the design document:
- [ ] As-is class diagram: `Feature`, `FeatureSetRenderer`, `FeatureSetManager`, `FeatureTable`, `VoTable`, `FeatureMapper`, `FeatureMenuController`, `FeatureMenuCell`
- [ ] CK metrics (before): WMC, DIT, NOC, CBO, RFC, LCOM for each class; measured with Understand / NDepend
- [ ] Smell catalogue: table of identified smells, SOLID/GRASP principle violated, line-number evidence
- [ ] CK metrics (after): measured on refactored stubs
- [ ] Worked example 1: moment-map generation — sequence diagram showing the refactored ST5-owned `MomentMapMenuController` → `IMomentMapService` → `MomentMapServiceAdapter` (ST5 GPU ACL); contrast with current three-hop `getFirstActiveDataSet().GetMomentMapRenderer().CalculateMomentMaps()` coupling
- [ ] Worked example 2: VOTable export — component diagram showing the refactored ST5-owned `FeatureMenuController` → `IFeatureCatalogueWriter` → `VoTableSaver`; highlight that `VoTableSaver` holds `ISourceStatsProvider` and `ICoordinateTransformer` directly

## Sub-Team Dependencies

Per-interface "Exposed to" / "From" cells live in `ST5_interface.md` §1 and §2. Summary of consumer roles:

- **Depends on ST2 (Data I/O):** `ISourceStatsProvider`, `ICoordinateTransformer`, `IDataAnalysisPlugin`, `SourceStats` — required before `FeatureFactory`, `VoTableSaver`, and `MomentMapServiceAdapter` can be designed.
- **ST3 (Rendering Engine):** read-only consumer of `IFeatureSet` / `IFeature` / `IFeatureStatistics` / `FeatureColour` for cross-volume rendering and selection highlights; ST3 calls no ST5 service.
- **ST4 (Interaction):** holds `IFeatureSetQuery` and `IFeatureSelectionService` for cursor selection, selection-box drawing (`SetSelectionBoxBounds`), copy-to-UserDefined, and interactive bounding-box editing (`SetFeatureBounds`); holds `IFeatureListNavigation` (M-11) for the `next/previous source list` voice commands; optionally holds `IFeatureCatalogueReader` / `Writer` for direct interaction-driven import/export.
- **ST6 (Desktop GUI):** binds desktop panels to `IFeatureSetQuery`, `IFeatureImportService`, `IFeatureSelectionService`, `IMomentMapService`, `ISpectralProfileService`.
- **ST7 (Persistence):** invokes `IFeatureStateCapture` (M-16; DTO schema in draft per IR-01 — minimum viable shape `FeatureStateDto` + `FeatureSetEntryDto` + `FeatureEntryDto` is canonical in `shared_interfaces.md` §5.6 and reproduced in `ST5_interface.md` §3) to snapshot UserDefined / Imported / SelectionBox state — Mask sets re-derive from ST2 on restore via `IMaskStateCapture` and the `SourceStatsUpdated` bootstrap path; also reads the domain view (`IFeatureSet`, `IFeature`, `IFeatureStatistics`, `FeatureColour`, `FeatureSetType`) for inspection.

