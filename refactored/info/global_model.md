# iDaVIE — Joint Aligned Conceptual Model

A single joint conceptual model for the seven iDaVIE sub-team work packages, produced by aligning the per-team conceptual models against each other following the methodology in `coordination.md`. Companion document: `misalignments.md` records every conflict and its resolution. Each cross-team interface in §3 is referenced back to the misalignment ID that introduced it where applicable.

## 1. Aligned per-team domains

Each table lists the **post-alignment** owned domain — data and functionality only. Cross-team interfaces and value types that cross sub-team boundaries are listed once in §3 (the single authoritative interface registry). Differences from the original per-team model are linked by misalignment ID (M-NN).

### ST1 — Architecture & Micro-kernel Core

| Component | Kind | Notes |
| --- | --- | --- |
| `VolumeDataSet` | Domain aggregate (plain C#, `internal sealed`) | Owns identity, lifecycle, current subcube, header dictionary, derived stats / histogram view, alt-spectral frame parameters. Holds *references* (not data) to raw voxel / mask / WCS / brush-history services realised by ST2. Concrete is internal; consumers see `IVolumeDataSet`. **(M-01, M-02)** |
| `Config` | Value object | Loaded once at startup from JSON. Read by every team. Source-verified: `Assets/Scripts/VolumeData/Config.cs` exists today. |
| `KernelCompositionRoot` | Application | Sole place permitted to call `new` on cross-layer concretes; wires the registry and the aggregates. |
| `PluginRegistry` | Infrastructure (service locator at kernel boundary) | Loads, holds, exposes plug-ins via `GetPlugin<T>()`. |
| `BenchmarkHarness` | Infrastructure | Anti-corruption layer over Unity Profiler. |
| `Delegates` (cross-cutting) | Cross-cutting | Central declaration site for event delegates (`DatasetLoaded`, `DatasetUnloaded`, `SubcubeChanged`, `RestFrequencyChanged`, `ConfigChanged`, plus rendering / mask / selection delegates). New types require ADR-002 sign-off. **(M-15)** |
| Boundary value-types module *(new)* | Cross-cutting | `CartesianCoord`, `FeatureColour`, `MomentMapResult`, `DataStats`, `HistogramData`, `VolumeExtents`, `SubcubeBounds`. Two-team-only types (`WorldCoord`, `SourceStats`) live with their producer (ST2). **(M-21)** |
| Cross-team interfaces owned by ST1 | — | See §3.1. Includes `IVolumeDataSet`, `IVolumeLoader`, `IVolumeRegistry`, `IPluginRegistry`, all plug-in contracts, `ILogSink`, `IDesktopShell`, `IVolumeStateCapture`. |

### ST2 — Data I/O and FITS/WCS Plug-ins

| Component | Kind | Notes |
| --- | --- | --- |
| Voxel buffer | Data | Raw float array; ST2-managed unmanaged-memory lifetime. |
| Mask buffer | Data | Int16 array per loaded mask FITS file. **(M-04)** |
| FITS header store | Data | Key-value dictionary + raw header string. |
| Cube geometry | Data | `NAXIS1/2/3`, HDU count, subcube bounds, region offset. |
| WCS frame | Data | Starlink AST frame set; alt spectral frame + rest frequency. |
| Voxel statistics | Data | min / max / mean / RMS / histogram / ZScale percentiles. |
| Source-statistics catalogue | Data | Per-source voxel count, total / peak flux, flux-weighted centroid, channel/velocity W20, channel/velocity Vsys. Realises ST5's `ISourceStatsProvider`. **(M-07)** |
| IPAC catalogue (point-cloud) | Data | Column metadata + typed arrays; pure C#. |
| `FitsReaderPlugin` | Plug-in (realises `IFitsPlugin`) | open / close / read full or subcube / HDU management / mask voxel update / chunked reads. |
| `WcsTransformPlugin` | Plug-in | Realises `IWcsPlugin` (ST1), `IWcsMapping` (ST1), `ICoordinateTransformer` (ST5). Three windows on the same engine. **(M-06)** |
| `DataAnalysisPlugin` | Plug-in | Realises `IDataAnalysisPlugin` (ST1), `IRawVoxelAccess` (ST1), `ISourceStatsProvider` (ST5). Statistics / profile / crop+downsample / source detection. **(M-05, M-07)** |
| **`MaskEditService`** *(new)* | Application | Realises `IMaskEditState` + `IBrushStrokeHistory` + the new **`IMaskMutationService`**. Apply paint, undo / redo, save / load mask FITS, mask-mode toggles. ST6's desktop paint is rasterised on the ST6 side and committed via `ApplyBrush` — no polygon or Unity 2D coordinate type crosses the boundary. **(M-04, M-14)** |
| IPAC catalogue parser | Application | Pipe-delimited reader; no native dependency. |
| Native-plugin loader | Infrastructure | Reflection-based P/Invoke delegate binding. |
| Two-team boundary value types | Value types | `WorldCoord`, `SourceStats` — held with the ST2 producer rather than the ST1 shared module since each crosses only ST2↔ST5. **(M-21)** |
| Cross-team interfaces owned by ST2 | — | See §3.2. Includes `IMaskMutationService`, `ICoordinateTransformer`, `ISourceStatsProvider`, `IMaskStateCapture`. |

### ST3 — Rendering Engine

| Component | Kind | Notes |
| --- | --- | --- |
| `VolumeDataSetRenderer` | MonoBehaviour (concrete, internal) | Slated for decomposition into `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` (brief §6.3). Concrete name does not cross boundaries (consumers see `IRenderSettings` etc.). **(M-03)** |
| `MomentMapRenderer` | MonoBehaviour | Realises **`IMomentMapRenderer`** *(new)*. ST5's `IMomentMapService` wraps this. **(M-08)** |
| `WorldSpaceLineRenderer` | MonoBehaviour | Owned outright. |
| `ColorMapEnum` + `ColorMapUtils` | Value type + helpers | Owned outright; consumed by ST1 Delegates, ST4, ST5, ST6. |
| Shaders | GPU programs | `BasicVolume.shader`, `VolumeMask.shader`, `MomentMapGenerator.compute`. |
| `CatalogDataSetRenderer` | MonoBehaviour | Point-cloud catalogue rendering. **(M-18, IR-02)** |
| Cross-team interfaces owned by ST3 | — | See §3.3. Includes `IRenderSettings`, `IRenderSettingsMutator`, `IMomentMapRenderer`, `IRenderStateCapture`. |

### ST4 — Interaction System (VR, voice, controllers)

| Component | Kind | Notes |
| --- | --- | --- |
| `VolumeInputController`, `VolumeCommandController`, `QuickMenuController`, `PaintMenuController` | MonoBehaviours | Brief §6.4 ownership. |
| `LocomotionFSM`, `InteractionFSM` | State machines (plain C#) | Re-platformed onto Unity 6 Input System per brief §6.4. |
| `ControllerIdentity`, `BrushConfig` (brush size / additive / sourceId / paint-mode), `DragGestureState` *(renamed from `SelectionState`)*, `QuickMenuState`, `ScrollState`, `LocomotionConfig` | Owned state | `BrushState` renamed to `BrushConfig` to distinguish from ST2-owned brush-stroke history. `DragGestureState` rename per **M-10**. |
| `VoiceCommandRegistry` | Owned state | Keyword vocabulary. |
| Shape gesture state | Owned state | Source `Assets/Scripts/Shapes/` ; converts to mask edits via `IMaskMutationService`. **(M-23)** |
| Cross-team interfaces owned by ST4 | — | See §3.4. Includes `IControllerEventStream`, `IVoiceCommandStream`, `IInteractionStateProvider`, `IInteractionStateCapture`. |
| ~~`DataSetRegistry`~~ | *Removed* | Replaced by ST1's `IVolumeRegistry`. **(M-03, M-19)** |
| ~~`IDesktopMediationBoundary`~~ | *Removed* | Subsumed by `IRenderSettings` + `IInteractionStateProvider` events. **(M-12)** |
| ~~`IFeatureConsumer`~~ | *Removed* | Replaced by direct calls into `IFeatureSetQuery`. **(M-10)** |

### ST5 — Feature System and Domain Model

| Component | Kind | Notes |
| --- | --- | --- |
| `Feature`, `FeatureSet`, `FeatureStatistics` | Domain aggregates (`internal sealed`) | Per `ST5_interface.md`. |
| `FeatureSetService`, `FeatureSetCatalog`, `SelectionService`, `FeatureFactory`, `FeatureImportService`, `SpectralProfileService` | Application | Per `ST5_refactoring_proposal.md`. |
| `VoTableReader`, `FitsTableReader`, `VoTableSaver` | Infrastructure | Realise `IFeatureCatalogueReader` / `IFeatureCatalogueWriter`. |
| `FeatureVisualiser`, `SelectionAnchorRenderer`, `MomentMapMenuController`, `SpectralProfileMenuController`, `FeatureMenuController` | Unity ACL / menus | Brief §6.5; menus owned here. |
| `MomentMapServiceAdapter` | Application | Wraps ST3's new `IMomentMapRenderer`. **(M-08)** |
| Cross-team interfaces & value types owned by ST5 | — | See §3.5. Includes the `IFeature` / `IFeatureSet` / `IFeatureStatistics` read view, `IFeatureSetQuery`, `IFeatureSelectionService`, `IFeatureListNavigation`, `IFeatureImportService`, `IMomentMapService`, `ISpectralProfileService`, `IFeatureCatalogueReader/Writer`, `IFeatureStateCapture`, plus boundary value types (`FeatureTable`, `FeatureImportMapping`, `FeatureColumnInfo`, `SpectralProfileResult`, `SourceMappingOptions`, `FeatureSetType`). |

### ST6 — Desktop GUI and Client Shell

| Component | Kind | Notes |
| --- | --- | --- |
| `CanvassDesktop`, `RenderingController` (boundary panel), `DesktopPaintController`, `MenuBarBehaviour`, `TabsManager` | Presentation controllers | Brief §6.6 ownership. |
| Panel State, File / Mask Path Inputs, Paint Selection (polygon) State, Menu / Dialog State | Owned UI state | Local UI state only. |
| Debug Console content | Owned UI state | Renders `ILogSink` output (M-20). |
| Cross-team interfaces owned by ST6 | — | See §3.6. Includes `IDesktopStateCapture`. ST6's `CanvassDesktop` realises `IDesktopShell`, declared by ST1 (M-26). |
| ~~GUI Render Settings cache~~ | *Removed* | UI now binds against `IRenderSettings` and re-reads on `SettingsChanged`. **(M-09)** |
| ~~Direct `Texture3D` reads of RegionCube / MaskCube~~ | *Removed* | Replaced by `IRawVoxelAccess` slice fetches + ST6-side `Texture2D` construction. **(M-14)** |

### ST7 — Persistence and Workspace State

| Component | Kind | Notes |
| --- | --- | --- |
| `StoredState` (envelope), `StateIndex`, `StorageLocation`, `PersistenceConfig`, `IntegrityRecord`, `PersistenceLog`, `MigrationRule` | Domain | Per `ST7_conceptual_model.md`. |
| Save use case, Load use case, State-management use cases, Validation & recovery | Application | Composes per-team capture ports → `StoredState`; reverse on restore. |
| Persistence UI panels | Presentation (uses `IDesktopShell`) | Save / load / state-list dialogs. |
| Cross-team interfaces owned by ST7 | — | See §3.7. Includes `IWorkspaceSaveCommand`, `IWorkspaceLoadCommand`, `IStateIndexQuery`, `IPersistenceEvents`. |
| Per-team capture ports (consumed) | — | Realised by ST1–ST6 per §3.1–§3.6. **(M-16)** |
| ~~User / Session information subsystem~~ | *Dropped* | OS-user + wall-clock timestamp only. **(M-22)** |

## 2. Aligned dependency graph (no cycles)

Arrows are `consumer → provider`. Every interface mentioned is declared in §3. Verified acyclic (M-25).

```
ST6 ──────────────────► ST3 (IRenderSettings, IRenderSettingsMutator)
ST6 ──────────────────► ST4 (IInteractionStateProvider)
ST6 ──────────────────► ST5 (IFeatureSetQuery, IFeatureSelectionService,
                              IFeatureImportService, IMomentMapService,
                              ISpectralProfileService)
ST6 ──────────────────► ST1 (IVolumeRegistry, IVolumeDataSet, Config, ILogSink,
                              IVolumeLoader for cube-load commands)
ST6 ──────────────────► ST2 (IMaskMutationService for paint commits;
                              IRawVoxelAccess + IMaskEditState for paint
                              preview slices, via the IVolumeDataSet aggregate)
ST6 ──────────────────► ST7 (IWorkspaceSaveCommand, IWorkspaceLoadCommand,
                              IStateIndexQuery)

ST4 ──────────────────► ST1 (IVolumeRegistry, Config)
ST4 ──────────────────► ST2 (IMaskMutationService)
ST4 ──────────────────► ST3 (IRenderSettings, IRenderSettingsMutator)
ST4 ──────────────────► ST5 (IFeatureSetQuery, IFeatureSelectionService,
                              IFeatureListNavigation,
                              IFeatureCatalogueReader/Writer [optional])
ST4 ──────────────────► ST7 (IWorkspaceSaveCommand, IWorkspaceLoadCommand)

ST5 ──────────────────► ST1 (IVolumeDataSet read view)
ST5 ──────────────────► ST2 (ICoordinateTransformer, ISourceStatsProvider,
                              IDataAnalysisPlugin)
ST5 ──────────────────► ST3 (IMomentMapRenderer)

ST3 ──────────────────► ST1 (IVolumeDataSet read view, Config, Delegates)
ST3 ──────────────────► ST2 (IRawVoxelAccess, IMaskEditState [render-side
                              read of mask data], IDataAnalysisPlugin for
                              stats; via the IVolumeDataSet aggregate)

ST2 ──────────────────► ST1 (Plug-in host lifecycle)

ST7 ──────────────────► ST1 (IVolumeStateCapture, Config, ILogSink,
                              IDesktopShell for mounting persistence UI panels)
ST7 ──────────────────► ST2 (IMaskStateCapture)
ST7 ──────────────────► ST3 (IRenderStateCapture)
ST7 ──────────────────► ST4 (IInteractionStateCapture)
ST7 ──────────────────► ST5 (IFeatureStateCapture)
ST7 ──────────────────► ST6 (IDesktopStateCapture)

ST1 ──────────────────► (no outbound cross-team dependencies; kernel is the floor)
```

No team has a back-edge into ST7; no team has a back-edge into the kernel (ST1). The ST6 → ST7 → ST6 route that an earlier draft tried to defend as "not a cycle at the package level" was a genuine package-level cycle; it is dissolved by relocating `IDesktopShell` from ST6 to ST1 as a cross-cutting UI-mount port (see M-26), the same pattern as `ILogSink` (M-20).

## 3. Cross-team interface registry — "new exports"

The single authoritative list of interfaces and value types that cross sub-team boundaries post-alignment. Every entry names exactly one owner; every consumer holds the interface, not a concrete.

### 3.1 ST1-owned (kernel + cross-cutting)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IVolumeDataSet` | Interface | ST3, ST4, ST5, ST6, ST7 | Read-only view of the loaded volume aggregate (dims, subcube, header dict, stats, axis-coord formatter, status) plus the `RawVoxelAccess : IRawVoxelAccess` and `MaskEditState : IMaskEditState` sub-port handles ST3 / ST6 reach through this aggregate. | **M-02, M-27** |
| `IVolumeLoader` | Interface | ST6, ST4 | `Load(path, hdu, subcubeBounds)`, `Unload(volume)`, `SetSubcube(volume, bounds)`. | **M-02-derived** |
| `IVolumeRegistry` | Interface | ST3, ST4, ST6 | `LoadedVolumes`, `ActiveVolume`, `SetActive`, `Action Changed`. | **M-19** |
| `IPluginRegistry` | Interface | ST2 (registers), all teams (lookup at composition root) | Plug-in service locator at kernel boundary. | from ST1 |
| `IFitsPlugin`, `IWcsPlugin`, `IWcsMapping`, `IDataAnalysisPlugin`, `IRawVoxelAccess`, `IMaskEditState`, `IBrushStrokeHistory` | Plug-in interfaces | Realised by ST2 plug-ins; held inside ST1's `VolumeDataSet` | Versioned per brief §4.2 constraint 5. | from ST1 |
| `Config` | Value object | All teams (read) | Loaded once at startup. | from ST1 / source-verified |
| `Delegates` (centralised module) | Cross-cutting | All teams | Declarations only — `DatasetLoaded`, `DatasetUnloaded`, `SubcubeChanged`, `ConfigChanged`, plus rendering / mask / selection delegates. ADR-002 governs additions. | **M-15** |
| `ILogSink` | Cross-cutting port | All teams (emit), ST6 (subscribe) | Domain-safe logging seam. | **M-20** |
| `IDesktopShell` | Cross-cutting port | ST7 (mounts persistence UI panel); future non-ST6 UI contributors | Menu / tab / file-dialog mount points. Realised by ST6's `CanvassDesktop`; declared in ST1 so cross-team consumers do not back-edge into ST6. Same pattern as `ILogSink`. | **M-26** |
| Shared value types | — | All teams | `CartesianCoord`, `FeatureColour`, `MomentMapResult`, `DataStats`, `HistogramData`, `VolumeExtents`, `SubcubeBounds`. Two-team-only types (`WorldCoord`, `SourceStats`) are owned by ST2 — see §3.2. | **M-21** |
| `IVolumeStateCapture` | Persistence port | ST7 | Capture / restore ST1-owned state for the workspace snapshot. | **M-16** |

### 3.2 ST2-owned (data plumbing)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IMaskMutationService` | Interface | ST4, ST6 | `ApplyBrush(stroke)`, `Undo()`, `Redo()`, `FinishStroke()`, `InitialiseMask()`, `SaveMask(overwrite)`, `MaskMode` get/set, `DisplayMask` get/set, `NewSourceId` get/set, `CursorSource` get/set. Consolidates ST4's draft `IMaskMutationInterface`. Polygon paint is rasterised on ST6's side (`DesktopPaintController`) and committed via `ApplyBrush`; no polygon and no `UnityEngine.Vector2` cross the boundary. | **M-04, M-14** |
| Plug-in realisations | — | (via `IPluginRegistry`) | `FitsReaderPlugin`, `WcsTransformPlugin`, `DataAnalysisPlugin`. | from ST2 |
| `ICoordinateTransformer` | Interface | ST5 | `Transform(CartesianCoord) → WorldCoord`. Single-method facade over `WcsTransformPlugin`. | **M-06** |
| `ISourceStatsProvider` | Interface | ST5 | `GetStatsForSource`, `GetAllStats`, `SourceStatsUpdated`. ISP split deferred per ST5 DD-11. | **M-07** |
| `WorldCoord`, `SourceStats` | Value types | ST5 | Two-team-only payloads (ST2 → ST5); held with the producer rather than the ST1 shared module per the >2-teams rule. | **M-21** |
| `IMaskStateCapture` | Persistence port | ST7 | Capture / restore mask buffer. | **M-16** |

### 3.3 ST3-owned (rendering)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IRenderSettings` | Interface | ST4, ST6 | Read view: thresholds, Z-axis factor, full-resolution flag, colour map, projection mode, scaling type, vignette + `SettingsChanged` event. | **M-09** |
| `IRenderSettingsMutator` | Interface | ST4, ST6 | Setters for the same fields; resets for transform / threshold / Z-axis. | **M-09** |
| `IMomentMapRenderer` | Interface | ST5 | `Task<MomentMapResult> RenderMomentMap(momentOrder, threshold, useMask)` — the GPU seam. Replaces the ST5-side ACL wrapper. | **M-08** |
| `IRenderStateCapture` | Persistence port | ST7 | Capture / restore live render settings. | **M-16** |

### 3.4 ST4-owned (interaction)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IControllerEventStream` | Port | (ST4 internal — adapters in ST4 ACL) | Platform-neutral controller event abstraction. | **M-24** |
| `IVoiceCommandStream` | Port | (ST4 internal — adapters in ST4 ACL) | Platform-neutral voice abstraction. | **M-24** |
| `IInteractionStateProvider` | Interface | ST6 | Read view + `InteractionStateChanged` event. Replaces ST4's draft `IDesktopMediationBoundary`. | **M-12, M-13** |
| `IInteractionStateCapture` | Persistence port | ST7 | Capture / restore interaction-side state. | **M-16** |

### 3.5 ST5-owned (feature system)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IFeature`, `IFeatureSet`, `IFeatureStatistics` | Interfaces | ST3, ST6, ST7 | Read-only domain view. | `ST5_interface.md` §1 |
| `IFeatureSetQuery` | Interface | ST4, ST6 | Query + display mutation + per-feature mutation + `FeatureSetChanged` event. Includes `SetSelectionBoxBounds` (M-10). | `ST5_interface.md` §1 |
| `IFeatureSelectionService` | Interface | ST4, ST6 | Cursor pick, direct select / deselect, `SelectionChanged` event. | `ST5_interface.md` §1 |
| `IFeatureListNavigation` | Interface | ST4 | `DisplayNextSet()`, `DisplayPreviousSet()`. | **M-11** |
| `IFeatureImportService` | Interface | ST6 | Column discovery, file import, mapping load/save. | `ST5_interface.md` §1 |
| `IMomentMapService` | Interface | ST6 | Use-case orchestration (wraps `IMomentMapRenderer` from ST3). | `ST5_interface.md` + **M-08** |
| `ISpectralProfileService` | Interface | ST6 | Region → spectral profile (wraps ST2's `IDataAnalysisPlugin`). | `ST5_interface.md` §1 |
| `IFeatureCatalogueReader`, `IFeatureCatalogueWriter` | Port interfaces | ST4 (optional); held internally | File ↔ in-memory table / `IFeatureSet`. | `ST5_interface.md` §1 |
| `FeatureTable`, `FeatureImportMapping`, `FeatureColumnInfo`, `SpectralProfileResult`, `SourceMappingOptions`, `FeatureSetType` | Value types | Cross-team | Per `ST5_interface.md` §3. | `ST5_interface.md` |
| `IFeatureStateCapture` | Persistence port | ST7 | Capture / restore feature catalogue. | **M-16** |

### 3.6 ST6-owned (desktop shell)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IDesktopStateCapture` | Persistence port | ST7 | Capture / restore UI state (active tab, panel positions). | **M-16** |

### 3.7 ST7-owned (persistence)

| Name | Kind | Consumers | Purpose | Origin |
| --- | --- | --- | --- | --- |
| `IWorkspaceSaveCommand` | Interface | ST4 (voice / quick-menu), ST6 (UI) | Trigger save. Payload-free. | `ST7_conceptual_model.md` §4 |
| `IWorkspaceLoadCommand` | Interface | ST4, ST6 | Trigger load by `StateId`. | `ST7_conceptual_model.md` §4 |
| `IStateIndexQuery` | Interface | ST6 | Enumerate / search saved states. | `ST7_conceptual_model.md` §4 |
| `IPersistenceEvents` | Interface | ST6 (UI feedback) | `SaveStarted/Completed/Failed`, `LoadStarted/Completed/Failed` notifications. | `ST7_conceptual_model.md` §4 (optional in original) |

## 4. Architectural fitness check against the brief

| Constraint (brief §4.2) | Status | Evidence |
| --- | --- | --- |
| **1.** No SOLID / GRASP violation without documented trade-off | OK — three documented trade-offs remain (ST5 DD-9, DD-10, DD-11); all justified per their proposal. | `ST5_refactoring_proposal.md` |
| **2.** No circular dependencies | OK — graph in §2 verified acyclic; see also M-25. | This document §2 |
| **3.** Domain code must not transitively depend on `UnityEngine` / `SteamVR` | OK — all interface payloads in §3 are plain C#; M-14 removed the only remaining `Texture3D` boundary. | §3 |
| **4.** Every public API boundary is an interface | OK — §3 enumerates interfaces; concrete classes are `internal sealed` (M-02 mirrors ST5 DD-5). | §3 |
| **5.** Plug-in ABI versioned, ABI-stable within major version | OK — ST1 owns the plug-in contracts and the semver policy; ST2 plug-ins carry `AbiVersion : string = "1.0.0"`. | `ST1_conceptual_model.md` |

## 5. Open items

Tracked in `misalignments.md` — see "Integration risk register (open items)".

## 6. References

- `coordination.md` — alignment procedure followed by this document.
- `misalignments.md` — per-conflict resolution log; ADR-implied list in its appendix.
- `iDaVIE_Refactoring_Assignment_FINAL_1.pdf` §4 (mandatory constraints), §6.1–§6.7 (work packages), §7 (CK metrics), §8.2 (Day 9 sign-off).
- Per-team sources: `ST1_conceptual_model.md`, `ST2_conceptual_model.puml`, `ST3_conceptual_model.puml`, `ST4_conceptual_model.puml`, `ST5_conceptual_model.md` + `.puml`, `ST5_refactoring_proposal.md`, `ST5_interface.md`, `ST6_conceptual_model.puml`, `ST7_conceptual_model.md`.
- Source spot-checks: `Assets/Scripts/VolumeData/Config.cs`, `Assets/Scripts/Tools/Delegates.cs`, `Assets/Scripts/CatalogData/`, `Assets/Scripts/Shapes/`, `Assets/Scripts/UI/DesktopPaintController.cs`.
