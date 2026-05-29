# Sub-Team 5: Feature Domain Design Document

ISE Refactoring Assessment, 18 May – 5 June 2026.
Companion documents: `ST5_refactoring_proposal.md` (full refactoring proposal, audit, design decisions, worked examples), `ST5_interface.md` (cross-team contract), `Feature_BDD.puml` (SysML BDD of every interface crossing an ST5 boundary).

---

## 1. Purpose

This document defines the **Feature domain** as it will exist after the proposed refactor: what it is, what it owns, what it depends on, and what invariants it enforces. It is the *domain-layer* counterpart to the proposal — the proposal answers "how do we refactor?", and this document answers "what are we modelling?".

It is one of the three deliverables named in the brief §6.5:

1. **Feature domain design document** — this file
2. Feature aggregate UML class diagram + invariants list — §4, §5 below (UML source in `Feature_BDD.puml`)
3. Worked statistics test specification — `ST5_refactoring_proposal.md`, "Testing Strategy" section

---

## 2. Bounded Context

The **Feature System** is one of seven canonical work packages in the iDaVIE refactor (Assignment §6). Its bounded context is the in-memory model of detected astronomical sources and user-created regions of interest, together with the use-case services that act on them.

```
                          ┌─────────────────────────────┐
                          │  ST4 — Interaction System   │
                          │  (locomotion, voice,        │
                          │   quick-menu, paint-menu)   │
                          └──────────────┬──────────────┘
                                         │ commands / queries
                                         │ (cursor selection,
                                         │  selection-box bounds,
                                         │  optional direct import/export)
                                         ▼
┌──────────────┐   stats    ┌─────────────────────────────┐    read    ┌──────────────┐
│ ST2 — Data   │───────────▶│   ST5 — Feature System      │───────────▶│ ST3 —        │
│ I/O plug-ins │  WCS, etc. │   (THIS BOUNDED CONTEXT)    │  IFeature  │ Rendering    │
└──────────────┘            └──────┬──────────────────────┘            └──────────────┘
                                   │                ▲
                                   │ query / mutate │ snapshots
                                   ▼                │
                          ┌─────────────────────────────┐
                          │  ST6 Desktop GUI / ST7      │
                          │  Persistence                │
                          └─────────────────────────────┘
```

**Owned by ST5:** the `Feature` aggregate; the `FeatureSet` collection; the application services that create, query, mutate, select, import, export, and analyse features and feature sets.

**Not owned by ST5** (but neighbours):
- C++ FITS / WCS / source-statistics plug-ins (ST2)
- GPU volume rendering and shaders (ST3)
- Input-side / state-machine controllers brief §6.4 enumerates: locomotion, voice commands, quick-menu, paint-menu (ST4) — not every `*Controller` MonoBehaviour
- Desktop GUI shell and parameter panels (ST6)
- Workspace save/restore (ST7)

ST5 *does* own the feature-domain menu controllers brief §6.5 enumerates: the source-list panel (`FeatureMenuController` + `FeatureMenuCell`), the moment-map menu (`MomentMapMenuController`), and the spectral-profile menu (`SpectralProfileHelper`). These are feature-domain UIs, not generic input controllers.

The boundary is enforced two ways: the cross-team contract (`ST5_interface.md`) is the only legal interface surface, and the domain layer holds zero `UnityEngine` / `SteamVR` references (an architectural constraint inherited from Assignment §4.2).

---

## 3. Ubiquitous Language

Terms used consistently across the codebase, the brief, and this document.

| Term | Meaning |
|---|---|
| **Feature** | A single detected astronomical source or user-defined region. Has identity, geometry (axis-aligned bounding box), display state, and — for Mask features — realtime statistics. |
| **Feature set** | A homogeneous collection of features with a single `FeatureSetType`. The collection is the type-discrimination point, never the element. |
| **Feature-set type** | One of `Mask` / `Imported` / `UserDefined` / `SelectionBox`. Stamped at construction; never changes. |
| **Mask feature** | A feature whose geometry was derived from a loaded mask FITS file by the C++ `DataAnalysis` plug-in. The only flavour that carries statistics. |
| **Imported feature** | A feature loaded from a VOTable or FITS binary-table catalogue. Carries raw catalogue columns, no native statistics. |
| **UserDefined feature** | A feature created in-session by the user (drawn shape, copied source). No native statistics. |
| **SelectionBox feature** | A transient feature representing the user's current bounding-box region of interest. At most one exists at any time — the SelectionBox set is lazily created on the first `SetSelectionBoxBounds` call and holds exactly one feature whose bounds are updated in-place on subsequent calls. Distinct from the per-feature `IsSelected` flag (which marks the *currently focused* feature in the source list — see DD-15). |
| **Origin ID** | A feature's identity in its source of origin. For Mask, the `maskVal` integer from the C++ layer; for Imported, the row index in the catalogue; for UserDefined, inherited from the source feature; for the SelectionBox feature, `-1`. |
| **Statistics** | The realtime quantities produced by the `DataAnalysis` plug-in for a Mask source: voxel count, total flux, peak flux, flux-weighted centroid, W20 line width (channel + velocity variants), and systemic central position (channel + velocity variants). Recomputed when the mask is edited. |
| **Active type** | The feature-set type the user is currently working with, set by tab click on the source-list panel or by the `DisplayNextSet` / `DisplayPreviousSet` voice command. Nullable — `null` means no source-list panel is open. Written through `IActiveFeatureSetTypeProvider` by ST5's source-list controller and ST6's desktop tab strip; read by ST5's `SelectionService`. |
| **W20** | Line width measured at 20% of peak flux. Reported in two units on `IFeatureStatistics`: `ChannelW20` (cube channels) and `VeloW20` (velocity). Wider than W50 by construction (W50 would be `≤ W20`). |
| **Vsys** | Systemic central position of the source's spectral line. Reported in two units on `IFeatureStatistics`: `ChannelVsys` (cube channels) and `VeloVsys` (velocity). |
| **Bounding box** | An axis-aligned cube in data-cube voxel space. Expressed as `(Center, Size)` on `IFeature`; equivalent to `CornerMin = Center - Size/2` and `CornerMax = Center + Size/2`. |
| **Data cube space** | The 3D voxel coordinate system of the loaded FITS cube. Z may be channel, velocity, frequency, or redshift depending on the cube's WCS; ST5 treats it uniformly as a `CartesianCoord`. |
| **World sky coordinates** | The astronomical RA / Dec / spectral-axis representation, produced from data-cube positions by ST2's `ICoordinateTransformer`. ST5 never computes these itself. |

---

## 4. Domain Model

### 4.1 Aggregates and entities

The Feature domain has **two aggregates**: `FeatureSet` and `Feature`.

`FeatureSet` is the aggregate root for a collection of features. Membership and the `FeatureSetType` invariant are enforced at this boundary. The collection owns its features; a feature's lifecycle is bounded by its set.

`Feature` is itself an aggregate root for the realtime statistics it carries (the `IFeatureStatistics` value object). Treating statistics as an internal value object — rather than a separately-rooted entity — keeps the GRASP Information Expert assignment crisp: the Feature has the data, the Feature provides the answers.

```
┌────────────────────────────────────────────────────────────────┐
│ FeatureSet  «aggregate root»                                   │
│   Index, FileName, Type, DisplayColour, IsVisible,             │
│   Features, RawDataKeys                                        │
│   ▼ Features contains 0..* (homogeneous in Type)               │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ Feature  «aggregate root»                                  │ │
│ │   OriginId, Name, Flag, Center, Size, IsSelected,          │ │
│ │   RawDataValues                                            │ │
│ │   ▼ optional (Mask only)                                   │ │
│ │ ┌────────────────────────────────────────────────────────┐ │ │
│ │ │ FeatureStatistics  «value object»                      │ │ │
│ │ │   VoxelCount, TotalFlux, PeakFlux,                     │ │ │
│ │ │   FluxWeightedCentroid,                                │ │ │
│ │ │   ChannelW20, VeloW20, ChannelVsys, VeloVsys           │ │ │
│ │ └────────────────────────────────────────────────────────┘ │ │
│ └────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

The full BDD (with realisations, dependencies, and cross-team interfaces) is in `Feature_BDD.puml`.

### 4.2 Value objects

Full field schemas live in `ST5_interface.md` §3. Domain-relevant notes:

- `CartesianCoord` is `(int X, int Y, int Z)` per `shared_interfaces.md` §1.1 (resolved canonical shape). All voxel-space positions ST5 exposes — `Center`, `Size`, `FluxWeightedCentroid`, `BoundsMin`/`BoundsMax` — therefore round to integer voxel indices at the boundary. The native `DataAnalysis.SourceStats` centroid (`double cX, cY, cZ`) is rounded to nearest voxel inside ST2's adapter; sub-voxel precision is not preserved across the cross-team seam. Arithmetic is an ST5-internal `CartesianCoordOps` extension class — not on the contract.
- `FeatureColour` replaces `UnityEngine.Color`; conversion happens only inside the ACL.
- `WorldCoord` is **ST2-owned** and used only internally by ST5 (never re-exposed): returned by `ICoordinateTransformer.Transform` (consumed from ST2), held by `VoTableSaver` and the spectral-profile display path. It does not appear on any ST5-provided interface.
- `FeatureStatistics` is the value object realising `IFeatureStatistics`; `VoxelCount` is `long` because a large cube can exceed `int.MaxValue` voxels.
- `SpectralProfileResult.ZStartChannel` lets the consumer recover per-channel Z-axis world values via `ICoordinateTransformer` — replacing the existing inline `AstTool.Transform3D(0, 0, i + minZ, …)` walk in `SpectralProfileHelper`.
- `SourceMappingOptions` drops the legacy `none` sentinel (`SourceRow.cs:28`) — unassigned roles are absent from `FeatureImportMapping.ColumnAssignments`.

All boundary value types are `readonly record struct` (or sealed immutable class for `FeatureImportMapping` / `FeatureTable`). No setters, no Unity types.

---

## 5. Invariants

### 5.1 Feature-set homogeneity

For every `IFeatureSet` instance: every feature in `Features` has the set's `Type`. The type is fixed at construction and cannot change. The current production code enforces this implicitly by keeping three named lists plus a Selection singleton; the refactored design enforces it by construction in `FeatureSetService.CreateSet`.

### 5.2 Mask-feature statistics — centroid inside bounding box

For every Mask `IFeature` with non-null `Statistics`:

```
feature.Center - feature.Size / 2
    ≤ feature.Statistics.FluxWeightedCentroid
    ≤ feature.Center + feature.Size / 2
```

on each of the three axes. Both `Center`/`Size` and `Statistics.FluxWeightedCentroid` derive from the same `SourceStats` snapshot at feature construction, so the invariant holds by construction. On `ISourceStatsProvider.SourceStatsUpdated`, `FeatureSetService` refreshes both bounds (via `Feature.UpdateGeometry`) and statistics (via `Feature.UpdateStatistics`) from the new snapshot in a single update, so the invariant is preserved across edits.

### 5.3 Mask-feature statistics — flux non-negativity

For every Mask `IFeature` with non-null `Statistics`:

```
feature.Statistics.TotalFlux ≥ 0
feature.Statistics.PeakFlux  ≥ 0
```

Flux can be physically zero for an all-masked-out region, but cannot be negative. Enforced at the same boundary as 5.2.

### 5.4 Statistics presence

| Feature type | `Statistics` | Rationale |
|---|---|---|
| Mask | non-null `IFeatureStatistics` | computed by the C++ `DataAnalysis` plug-in from the mask voxels |
| Imported | `null` | source catalogue does not supply native statistics |
| UserDefined | `null` | drawn / copied in-session; no mask backing |
| SelectionBox | `null` | transient region; no analysis required |

The invariant applies to any feature reachable via `IFeatureSet.Features` — i.e. to every feature observable from outside ST5. Internally, `FeatureFactory.PopulateFromSourceStats` constructs a Mask `Feature`, calls `UpdateStatistics` with the populated stats, and only then calls `FeatureSet.AddFeature`; the feature is never exposed without `Statistics`.

Callers must null-check `IFeature.Statistics` before use.

### 5.5 Focused-feature cardinality

At most one `Feature` instance has `IsSelected == true` at any time. Enforced by `SelectionService.SelectFeature` (which always calls `DeselectFeature` first). The previously focused `Feature.IsSelected` is set back to `false` before the new selection takes effect.

This invariant concerns *per-feature focus only* — the `IsSelected` flag that highlights one feature in the source list. It is independent of the SelectionBox feature defined in §3 and DD-15, which is a transient region-of-interest box that exists at most once (zero before the first `SetSelectionBoxBounds` call) inside the single SelectionBox-typed set.

### 5.6 Origin-ID stability

A feature's `OriginId` is immutable. For Mask features it equals the `maskVal` of the originating mask voxel; for Imported features the catalogue row index; for UserDefined features the OriginId of the source feature it was copied from; for the SelectionBox feature `-1`. Code that joins an `IFeature` with external data (statistics provider, catalogue writer) does so via `OriginId`.

### 5.7 W50 — deliberately not modelled

W50 is excluded from `IFeatureStatistics`, which exposes `ChannelW20` and `VeloW20` only (each in its native units; see §3 ubiquitous language). Rationale and the brief-error flag are in DD-13 of `ST5_refactoring_proposal.md`.

---

## 6. Domain Events and Lifecycle

### 6.1 Events produced

| Event | Owner | Payload | When fired |
|---|---|---|---|
| `FeatureSetChanged` | `IFeatureSetQuery` (impl: `FeatureSetService`) | none | Any feature set is created, updated, or deleted, or any feature-set display property (`IsVisible`, `DisplayColour`) is mutated. Bulk-population flows fire twice (empty set → populated). See `ST5_interface.md` §6 for the canonical event-count rules, including the full enumeration of bulk-population flows. |
| `SelectionChanged` | `IFeatureSelectionService` (impl: `SelectionService`) | `IFeature?` | The selection changes; payload is the newly-selected feature, or `null` on deselect. Consumers that only care about Mask selection filter on `SelectedFeatureSet?.Type == FeatureSetType.Mask`. |

Threading and snapshot-invalidation rules are canonical in `ST5_interface.md` §6.

### 6.2 Events consumed

| Event | Source | Payload | Effect |
|---|---|---|---|
| `SourceStatsUpdated` | `ISourceStatsProvider` (ST2) | `int originId` | `FeatureSetService` resolves the originId against its Mask set and applies one of four branches. The three per-edit branches (existing+non-empty, absent+non-empty, existing+empty) mirror the legacy logic in `VolumeDataSet.UpdateStats`, lines 528–587; the bootstrap branch is new behaviour the refactor adds to lazily create the Mask set on the first event. **Mask set not yet created** (bootstrap) → run the §6.3 sequence and populate from `ISourceStatsProvider.GetAllStats()` — raises `FeatureSetChanged` twice per §6.3 (steps 2 and 5); **existing + non-empty stats** → refresh bounds via `Feature.UpdateGeometry` and statistics via `Feature.UpdateStatistics`; **absent + non-empty stats** → append a new Mask feature using the set's listener; **existing + empty stats** (voxel count went to 0) → remove the emptied feature. The three per-edit branches each raise `FeatureSetChanged` once after the branch completes. |

### 6.3 Construction order — listener, set, visualiser binding, then features

`FeatureSet` is constructed with an `IFeatureDirtyListener` (the per-set GPU notifier), which is shared by every `Feature` attached to that set. The visualiser realising that listener is a MonoBehaviour in the ACL, so the Application-layer caller obtains it first and binds it back to the set once the set exists. The caller (`FeatureImportService` for table-driven Imported sets; `FeatureSetService` itself for the Mask bootstrap) holds three injected ports — `IFeatureSetCatalog`, `IVisualiserBinder`, `IFeatureFactory` — and runs the following sequence synchronously:

1. **Listener creation.** `listener = binder.CreateListener()` instantiates a `FeatureVisualiser` MonoBehaviour with no set reference yet. Any `OnFeatureDirty` calls received before step 3 are buffered. `IVisualiserBinder` is the ST5-internal port implemented by an ACL-layer orchestrator; it exists precisely because the Application layer cannot reference `MonoBehaviour`. `FeatureVisualiser` is the only object that implements `IFeatureDirtyListener`.
2. **Set creation.** `set = catalog.CreateSet(fileName, type, rawDataKeys, colour, listener)` builds an empty `FeatureSet` (with the listener stored on it), registers it in the store, and raises `FeatureSetChanged`. Consumers observing the event see an empty set.
3. **Visualiser binding.** `binder.AttachToSet(listener, set)` hands the set reference to the visualiser; the visualiser drains its buffered dirty queue and begins uploading vertex data.
4. **Feature population.** `factory.PopulateFromTable(set, table, mapping)` or `factory.PopulateFromSourceStats(set)` parses inputs, constructs `Feature` instances, and attaches them via `FeatureSet.AddFeature` — which wires each Feature to the set's shared listener. The factory does not raise `FeatureSetChanged` per feature — `FeatureSet.AddFeature` is intentionally silent (see proposal §`FeatureSet`).
5. **Population complete.** The caller raises a second `FeatureSetChanged` so consumers can re-query and pick up the populated set.

The factory does not create the visualiser, does not call `IFeatureSetCatalog`, and never sees a `MonoBehaviour`. The three port references live on the caller (or, for the Mask flow, directly on `FeatureSetService`).

**UserDefined and SelectionBox sets** are created by `FeatureSetService` itself (in `CopyFeatureToUserDefined` and the first `SetSelectionBoxBounds` call respectively). These flows reuse steps 1–3 (listener creation, set creation, visualiser binding) and step 5 (final `FeatureSetChanged`), but skip step 4: instead of invoking `IFeatureFactory`, the service attaches a single pre-built `Feature` directly via `FeatureSet.AddFeature` — a copy of the source feature for UserDefined, or a freshly-constructed bounds feature for SelectionBox.

### 6.4 Feature lifecycle

```
Construction:
    FeatureFactory.PopulateFromTable(set, table, mapping)
        ↳ (Imported) — geometry from catalogue columns; Statistics = null
    FeatureFactory.PopulateFromSourceStats(set)
        ↳ (Mask) — geometry and Statistics from SourceStats; invariants 5.2 / 5.3 hold
    FeatureSetService.CopyFeatureToUserDefined(source)
        ↳ (UserDefined) — geometry copied from source; Statistics = null
    FeatureSetService.SetSelectionBoxBounds(min, max)
        ↳ (SelectionBox) — bounds from arguments; Statistics = null;
           subsequent calls update the existing feature in-place

Mutation:
    Feature.IsSelected                              → notifies IFeatureDirtyListener (GPU refresh)
    Feature.SetFlag(string)                         → notifies IFeatureDirtyListener (GPU refresh)
    Feature.UpdateGeometry(center, size)            → notifies IFeatureDirtyListener (GPU refresh)
        Driven by FeatureSetService for two flows:
          • SourceStatsUpdated refresh of an existing Mask feature (§6.2)
          • IFeatureSetQuery.SetFeatureBounds on a non-Mask feature (ST4 anchor-drag)
    Feature.UpdateStatistics(IFeatureStatistics?)   → no GPU notification
                                                       (statistics do not affect vertex data)

Disposal:
    FeatureSet.RemoveFeature(feature) — feature is no longer reachable from the set;
        also used by the SourceStatsUpdated handler to evict emptied Mask features
    FeatureSetService removal of the owning set → composition-root orchestrator
        destroys the bound FeatureVisualiser MonoBehaviour
```

`Feature` has no explicit `Dispose`; it carries no unmanaged resources. The GPU buffer is owned by `FeatureVisualiser` and managed by set-level lifecycle.

---

## 7. Domain Services

The domain layer is intentionally thin — most behaviour lives in the application layer because it coordinates between domain objects and external collaborators.

Every concrete class listed below is `internal sealed` inside the ST5 assembly per DD-5; cross-team consumers reach them only through the listed interfaces. The split:

**Domain layer (no Unity, no orchestration):**
- `Feature` — aggregate root
- `FeatureSet` — aggregate root
- `IFeatureStatistics` / `FeatureStatistics` — value object
- `IFeatureDirtyListener` — internal port (so `Feature` can notify the GPU layer without depending on it)

**Application layer (no Unity, orchestration):**
- `FeatureSetService` — implements `IFeatureSetQuery` (cross-team — query, display mutation, set-membership mutation, event), `IFeatureSetCatalog` (ST5-internal — `CreateSet` factory method held by `FeatureImportService`), and `IFeatureStateCapture` (cross-team — persistence capture/restore per M-16; DTO field schema in draft per IR-01, canonical shape in `shared_interfaces.md` §5.6 and reproduced in `ST5_interface.md` §3). Owns the set store and the Mask creation flow (subscribes to `ISourceStatsProvider.SourceStatsUpdated`; on the first event lazily runs the §6.3 sequence using its own `IVisualiserBinder` and `IFeatureFactory` references). Assigns each set a monotonic `Index` (see `IFeatureSet.Index` in `ST5_interface.md` §3). For `IFeatureStateCapture`, only UserDefined / Imported / SelectionBox sets are snapshotted — Mask sets re-derive from ST2 on restore via `IMaskStateCapture` + the `SourceStatsUpdated` bootstrap. On `Restore`, `FeatureSetEntryDto.Type` is parsed via `EnumString.TryParseOrDefault` (ST1 Kernel) with a `FeatureSetType.UserDefined` fallback so that unknown future enum members degrade gracefully (see `shared_interfaces.md` §1.9). Holds `IVolumeDataSet` (M-02) to gate the Mask-set bootstrap on a loaded volume, and exposes `ResetCatalogue()` — wired by the composition root to ST1's `DatasetUnloaded` — to clear all sets when the active volume is unloaded.
- `MomentMapServiceAdapter` — implements `IMomentMapService` (M-08) by delegating to ST3's `IMomentMapRenderer`. Plain C# — no `UnityEngine` reference.
- `SelectionService` — implements `IFeatureSelectionService`; coordinates selection state across sets. Depends on `ISelectionVisualiser` so the layer stays UnityEngine-free.
- `FeatureFactory` — implements the ST5-internal `IFeatureFactory`; populates a caller-supplied `FeatureSet` with `Feature` instances built from catalogue tables or source-stats dictionaries; holds `ICoordinateTransformer`, `ISourceStatsProvider`, and `IVolumeDataSet` (the last for cube `Extents` in the `ExcludeExternal` import filter) — set creation and visualiser binding live on the caller (see §6.3). Reads the listener from `FeatureSet.Listener` at attach time.
- `FeatureImportService` — implements `IFeatureImportService`; holds `IFeatureCatalogueReader`, `IFeatureSetCatalog`, `IVisualiserBinder`, and `IFeatureFactory`, and runs the §6.3 sequence for Imported / UserDefined sets.
- `SpectralProfileService` — implements `ISpectralProfileService`; wraps `IDataAnalysisPlugin` for the region → profile use case.
- `IFeatureCatalogueReader` / `IFeatureCatalogueWriter` — ports for catalogue I/O.
- `IFeatureSetCatalog`, `IFeatureFactory`, `ISelectionVisualiser`, `IVisualiserBinder` — ST5-internal ports (not part of the cross-team contract). `IVisualiserBinder` is realised in the Unity ACL because the visualiser it constructs is a `MonoBehaviour`.
- `IActiveFeatureSetTypeProvider` — a **cross-team** ST5 export (provided to ST6; see `ST5_interface.md` §1, §3). Written by ST5's `FeatureMenuController` and ST6's `SourcesTabViewModel`, read by `SelectionService`; the single shared writer keeps the VR and desktop active-tab state in sync.

**Infrastructure (no Unity):**
- `VoTableReader`, `FitsTableReader` — realise `IFeatureCatalogueReader`
- `VoTableSaver` — realises `IFeatureCatalogueWriter`; holds `ISourceStatsProvider` and `ICoordinateTransformer` to resolve flux-weighted centroids and convert to sky coordinates

**Unity ACL (the only layer permitted to touch UnityEngine):** concretes are `internal sealed MonoBehaviour`s.
- `FeatureVisualiser` — realises `IFeatureDirtyListener`; owns the `ComputeBuffer`; converts `CartesianCoord ↔ Vector3` at exactly this boundary
- `SelectionAnchorRenderer` — realises `ISelectionVisualiser`; manages the 8 bounding-box corner-handle GameObjects in response to selection events. The legacy implementation lives inside `FeatureSetManager` as the `_anchorColliders` array driven by the `FeatureAnchor` MonoBehaviour; this class extracts that responsibility into a dedicated scene-renderer. The name uses *Renderer* rather than *Controller* because it only renders selection state into the scene — it has no input or state-machine responsibilities.

Per M-08, `MomentMapServiceAdapter` no longer sits in the Unity ACL: it is plain ST5 Application code that wraps ST3's `IMomentMapRenderer`. The MonoBehaviour that drives `MomentMapRenderer` and converts the render texture to `float[]` lives in ST3.

The feature-domain menu controllers (`FeatureMenuController` for the source list, `MomentMapMenuController`, `SpectralProfileHelper`, `FeatureMenuCell`) live in ST5's UI layer — these are the menus brief §6.5 ascribes to ST5 ("source-list statistics; moment maps; spectral profiles; VOTable export"). They are MonoBehaviours but they consume ST5 service interfaces directly with no separate ACL wrapper. ST4 owns the input-side controllers brief §6.4 lists (locomotion, voice commands, quick-menu, paint-menu), which call into ST5's services for selection, selection-bounds, source-list navigation, and any direct import/export action.

`FeatureMenuController` additionally realises `IFeatureListNavigation` (M-11) — a two-method surface (`DisplayNextSet` / `DisplayPreviousSet`) consumed by ST4's voice-command handlers for `next source list` / `previous source list`. The `Count` ST4 originally drafted on its `ISourceCatalogue` is derivable from `IFeatureSetQuery.GetAllFeatureSets().Count` and is not re-exported.

---

## 8. Anti-Corruption Boundaries

The Feature domain meets four external contexts. Each meeting point is explicit and narrow.

### 8.1 ST2 — Native data plug-ins

ST5 consumes `ISourceStatsProvider`, `ICoordinateTransformer`, and `IDataAnalysisPlugin` from ST2 (full signatures in `ST5_interface.md` §2). Internal consumers: `FeatureFactory`, `FeatureSetService`, and `VoTableSaver` hold the stats / coordinate ports; `SpectralProfileService` holds the data-analysis plug-in. ST5 never touches `FitsReader`, `AstTool`, the raw mask buffer, or `DataAnalysis.SourceStats` directly — the `SourceStats` POCO is the only ST2 type that crosses the boundary.

DD-11 of the proposal records that `ISourceStatsProvider` could be split (ISP); the split is deferred because the interface is ST2's to define.

### 8.2 ST3 — Rendering engine

ST3 reads `IFeatureSet` / `IFeature` to render bounding boxes and selection highlights. Selection state is exposed as `IFeature.IsSelected`; ST3 polls or subscribes via `FeatureSetChanged`. ST3 does **not** call any ST5 service — it is a read-only consumer of the read-only contract.

### 8.3 ST4 — Interaction system

ST4 owns the input-side controllers brief §6.4 enumerates — locomotion, voice commands, quick-menu, paint-menu. Those controllers consume ST5 services for selection, selection-bounds, and any interaction-driven import/export action:

| ST5 contract used by ST4 | ST4-owned consumer |
|---|---|
| `IFeatureSelectionService` | cursor-selection gesture (quick-menu / pointer) |
| `IFeatureSetQuery` (merged: query + display mutation + set-membership mutation + per-feature mutation + `FeatureSetChanged`) | paint-menu (`SetSelectionBoxBounds` on selection-box drag, `CopyFeatureToUserDefined` from edit gestures, `SetFeatureBounds` on anchor-drag editing) |
| `IFeatureCatalogueReader` and `IFeatureCatalogueWriter` | optional — in case an interaction-driven import or export action wires up a port directly without going through the ST5-owned source-list controller |

`IMomentMapService` and `ISpectralProfileService` are consumed by ST5's *own* menu controllers (which are ST5-owned per brief §6.5 "moment maps" / "spectral profiles"); they are also part of the cross-team contract because ST6's desktop panels for the same use cases consume them too.

No ST4 → ST5 interface is consumed. `IActiveFeatureSetTypeProvider` is a cross-team ST5 export (provided to ST6 — see `ST5_interface.md` §1, §3): it is written by ST5's source-list controller (`FeatureMenuController`, ST5-owned per brief §6.5 "source-list statistics") and by ST6's `SourcesTabViewModel` (desktop tab strip), and read by ST5's `SelectionService` — one authoritative writer so the VR and desktop GUIs cannot drift apart. `SelectionService.SelectAtCursor` uses it to prioritise the active type during spatial search; when `ActiveType` is `null` (no source-list panel open), `SelectionService` falls back to scanning every loaded set in `FeatureSetType`-declaration order.

### 8.4 ST6 — Desktop GUI / ST7 — Persistence

ST6 binds its desktop panels to `IFeatureSetQuery`, `IFeatureImportService`, `IFeatureSelectionService` (desktop-side direct selection), `IMomentMapService`, and `ISpectralProfileService`. ST7 holds `IFeatureStateCapture` (M-16) to capture/restore UserDefined / Imported / SelectionBox state into the workspace snapshot; the read-only view (`IFeatureSet` / `IFeature` / `IFeatureStatistics`) remains the inspection surface for both teams. Mask sets are re-derived from ST2 on restore (via `IMaskStateCapture` + the `SourceStatsUpdated` bootstrap path in §6.2/§6.3), so they are not part of the ST5 capture payload. The capture DTO's field schema is in **draft (IR-01)** — the minimum viable shape (`FeatureStateDto` + `FeatureSetEntryDto` + `FeatureEntryDto`) is canonical in `shared_interfaces.md` §5.6 and reproduced in `ST5_interface.md` §3; further fields may be added with defaults post Day-9 sign-off without breaking existing callers.

Neither team can mutate `FeatureSet` or `Feature` directly — the read-only interface boundary is the enforcement mechanism (see DD-5).

---

## 9. Future Extension Points

The brief's "Future requirements" for the Feature domain are iso-contours/surfaces, particle datasets, and Virtual Observatory integration. The current design accommodates them as follows.

| Future requirement | Extension seam |
|---|---|
| **Iso-contours / surfaces** | `IFeature` would gain a `Geometry` value object (replacing the `(Center, Size)` AABB pair) or expose a separate `IFeatureSurface` accessor. AABB-derived methods like `SelectAtCursor`'s containment check would dispatch on geometry type. |
| **Particle datasets** | A fifth value for `FeatureSetType` and a new `Feature` flavour. `FeatureFactory.CreateFromParticleCloud(...)` extends the construction surface; the homogeneity invariant (5.1) keeps particle sets cleanly segregated. |
| **VO integration** | A new `IFeatureCatalogueReader` implementation (`VoCatalogueReader`) realising the same port; the import service is unaware. A `VoCatalogueWriter` similarly realises `IFeatureCatalogueWriter`. No domain change required. |

The ports / value-object split makes each of these an additive change. The proposal's "Current Architecture Problems Mapped to SOLID / GRASP" table and the "Cross-team contract — accepted SOLID / GRASP trade-offs" subsection record the SOLID/GRASP rationale for each seam.

---

## 10. References

- **Assignment brief** — `iDaVIE_Refactoring_Assignment_FINAL_1.pdf` §6.5 (Sub-team 5 work package), §4.2 (mandatory architectural constraints), §7.1 (CK metric targets).
- **`ST5_refactoring_proposal.md`** — current behaviour file-by-file, target architecture audit (SOLID/GRASP), 15 numbered design decisions, two worked refactoring examples (moment-map, VOTable export).
- **`ST5_interface.md`** — cross-team interface contract: provided / consumed tables, method signatures, preconditions/postconditions, error model, threading constraints, versioning policy.
- **`Feature_BDD.puml`** — SysML Block Definition Diagram covering every interface that crosses an ST5 boundary; structured by Domain / Application / Infrastructure / Unity ACL layers with cross-team consumed / provided stereotypes.
- **Source code reference points** — `Assets/Scripts/FeatureData/{Feature,FeatureSet*,FeatureMapper,FeatureMenuController,FeatureTable,VoTable}.cs`; `Assets/Scripts/PluginInterface/{DataAnalysis,AstTool,FitsReader}.cs`; `Assets/Scripts/Menu/SpectralProfileHelper.cs`; `Assets/Scripts/VolumeData/MomentMapRenderer.cs`.
