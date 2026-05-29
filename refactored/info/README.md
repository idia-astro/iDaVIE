# `refactored/` — worked-example skeletons

Design-only refactor of `VolumeDataSetRenderer.cs` (ST3) and `FeatureSetRenderer.cs` (ST5), per assignment **§1 Mode of work** ("design-only refactoring proposal; no upstream code is changed; each team demonstrates with worked examples how the code would be changed").

These files are **skeletons** — signatures, fields, short bodies, `// TODO` markers where legacy logic moves verbatim. They exist to evidence the SRP / OCP / DIP / GRASP splits proposed in `refactor_plan.md` §5, the ST5 contract in `ST5_interface.md`, and the cross-team namespace plan in `shared_interfaces.md` §0.

## Mapping back to the legacy files

### `Assets/Scripts/FeatureData/FeatureSetRenderer.cs` (616 LOC) → ST5

| Legacy method / concern | Refactored home |
|---|---|
| `Awake`, `Update` (GPU buffer), `OnRenderObject` | `Features/FeatureVisualiser.cs` (Unity ACL, only class that holds `ComputeBuffer`) |
| `FeatureList`, `AddFeature`, `RemoveFeature`, `ClearFeatures` | `Features/FeatureSet.cs` (internal sealed) |
| `SetVisibilityOn/Off`, `ToggleVisibility`, `UpdateColor`, `featureSetVisible`, `FeatureColor` | `Features/FeatureSet.cs` + mutation via `Features/FeatureSetService.cs` (per DD-3) |
| `SetFeatureAsDirty`, dirty-set diff in `Update` | `Features/FeatureVisualiser.cs` via the `IFeatureDirtyListener` port |
| `SpawnFeaturesFromSourceStats`, `SpawnFeaturesFromTable` | `Features/FeatureFactory.cs` (Unity-free) |
| `SelectFeature` | `Features/SelectionService.cs` via `IFeatureSelectionService` (skeleton stubbed inside `FeatureSetService.cs` for brevity here; full split in ST5 proposal) |
| `SaveAsVoTable` | `Features/VoTableSaver.cs` (`IFeatureCatalogueWriter`) |
| `FeatureIsWithinVolume` | Removed — replaced by `Feature.Center` containment against `IVolumeDataSet.Extents` (Information Expert; bug noted in `refactor_plan.md` §6 fixed at the same time) |
| `FeatureSetType FeatureSetType` field, `Index`, `FileName` | `Features/FeatureSet.cs` (boundary contract `IFeatureSet`) |
| `RawDataKeys`, `RawDataTypes`, `Flags`, `FeatureNames`, `FeaturePositions`, `BoxMinPositions`, `BoxMaxPositions` parallel arrays | Folded onto `IFeature.RawDataValues` + `IFeatureSet.RawDataKeys` (per `ST5_interface.md` §3) |

### `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` (1402 LOC) → ST3

Splits follow brief §6.3 verbatim for the four named classes (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`) and the `IMaskMode` Strategy; concerns the §6.3 list does not name retain their own services (coord maths, region/cursor, mask brush, persistence, rest-frequency catalogue).

| Legacy method / concern | Refactored home |
|---|---|
| `Update`, `OnRenderObject`, `Start`/`_startFunc` (trimmed) — Unity lifecycle and per-frame dirty-flag pull | `Rendering/VolumeDataSetRenderer.cs` (post-refactor — thin MonoBehaviour, ~120 LOC, composes the four §6.3 splits) |
| Threshold / scaling / colour-map / projection / vignette shader uniforms (legacy `Update`:1022 main body) | `Rendering/VolumeMaterialBinder.cs` (§6.3 split) |
| Material instantiation, `Texture3D` upload from `IRawVoxelAccess`, `IMaskGpuBuffers` ownership (legacy `RegenerateCubes`:580, `InitialiseMask`:1158, OnDestroy GPU teardown) | `Rendering/VolumeTextureManager.cs` (§6.3 split) |
| Model matrix, region-highlight bounds, mask voxel offsets (legacy `BindRegionHighlight`/`BindMaskParameters` inside `Update`:1056–1095) | `Rendering/VolumeCameraDriver.cs` (§6.3 split) |
| Foveation step-budget binding (legacy `Update`:1042–1054) | `Rendering/FoveatedSamplingPolicy.cs` (§6.3 split) |
| Mask-mode dispatch (legacy switch on `MaskMode` enum inside `Update`:1097–1104 + `OnRenderObject`:1142–1156) | `Rendering/IMaskMode.cs` + `Rendering/MaskModes.cs` (Strategy + `MaskModeRegistry`; §6.3 OCP requirement) |
| `ConvertWorldPositionToDataCubePosition`, `ConvertWorldRotationToDatacubeRotation`, `GetVoxelPositionDataSpace`, `GetVoxelPositionWorldSpace`, `VolumePositionToLocalPosition`, `LocalPositionToVolumePosition`, `GetCubeDimensions` | `Rendering/VolumeCoordinateService.cs` (Unity-free; §4.2.3) |
| `SetCursorPosition`, `SetVideoCursorLocPosition`, `DeactivateVideoCursorLocPosition`, `SetRegionPosition`, `SetRegionBounds`, `UpdateRegionBounds`, `ClearRegion`, `ClearMeasure` | `Rendering/RegionSelection.cs` |
| `InitialiseMask`, `PaintMask`, `PaintCursor`, `FinishBrushStroke` | `Rendering/MaskEditingService.cs` |
| `SaveSubCube`, `SaveMask`, `GetMaskSavedFilePath` | `Rendering/VolumePersistenceService.cs` |
| `RestFrequencyGHzList`, `PopulateRestFrequenyList`, `RestFrequencyGHzListIndex` setter | `Rendering/IRestFrequencyCatalogue.cs` (port; concrete realised in ST1 Infrastructure from `Config`) |
| Shader property-ID cache (legacy private `MaterialID` static class inside the god class) | `Rendering/ShaderIds.cs` (shared by every binder) |

## Cross-team boundaries used (`External/`)

The deliverable was asked to include "at least one dependency owned by another sub-team." Two are stubbed for context:

- `External/IVolumeDataSet.cs` — **ST1** owned (`iDaVIE.Kernel.Contracts`). Consumed by both refactored renderers and by ST5's `FeatureSetService` for dataset-lifecycle resets and cube-dimension preconditions.
- `External/ICoordinateTransformer.cs` — **ST2** owned (`iDaVIE.Data`). Consumed by `FeatureFactory` and `VoTableSaver`.

These two files are **reference declarations** — they reproduce the contract shape from `shared_interfaces.md` so the ST3/ST5 skeletons compile-as-illustrated; ownership stays with the originating sub-team.

## ST5 menus, ACL, and infrastructure readers (`Features/`, `UI/`)

Every concrete class in `ST5_domain_design.md` §7 has a skeleton in `refactored/`:

| Legacy class | Refactored home |
|---|---|
| `Assets/Scripts/FeatureData/FeatureMenuController.cs` (425 LOC) | `UI/FeatureMenuController.cs` (realises `IFeatureListNavigation`; holds `IFeatureCatalogueWriter`) |
| `Assets/Scripts/FeatureData/FeatureMenuCell.cs` (297 LOC) | `UI/FeatureMenuCell.cs` (consumes `IFeatureSetQuery` + `IFeatureSelectionService` + `IFlagVocabulary`) |
| `Assets/Scripts/Menu/MomentMapMenuController.cs` (334 LOC) | `UI/MomentMapMenuController.cs` (holds `IMomentMapService` only — Worked Example 1) |
| `Assets/Scripts/Menu/SpectralProfileHelper.cs` (153 LOC) | `UI/SpectralProfileHelper.cs` (holds `ISpectralProfileService`; ordinal-coupling bug fixed) |
| `Assets/Scripts/FeatureData/FeatureSetManager.cs` anchor-cluster (l. 60, 117-174) | `Features/SelectionAnchorRenderer.cs` (Unity ACL `MonoBehaviour` realising `ISelectionVisualiser`) |
| `Assets/Scripts/FeatureData/VoTable.cs` (`VoTable` parser) | `Features/VoTableReader.cs` (Infrastructure realisation of `IFeatureCatalogueReader`) |
| `Assets/Scripts/FeatureData/FeatureTable.cs` (`GetFeatureTableFromFits`) | `Features/FitsTableReader.cs` (Infrastructure realisation of `IFeatureCatalogueReader` via an internal `IFitsBinaryTableSource` port over the ST2-owned CFITSIO wrapper) |

## Yet to be skeleton-ported

End goal: every legacy file under `iDaVIE/Assets/Scripts/` has either a refactored skeleton in `refactored/`, an explicit "replaced by" line that closes it out, or an explicit "owned outright — no refactor required" tag. Items below are grouped by sub-team owner per `global_model.md §1`. LOC counts come from the legacy source.

**All Tier 1 and Tier 2 across all sub-teams is now resolved** — Tier 1 skeleton-ported (thin contract-only depth — method signatures + `NotImplementedException` bodies); Tier 2 owned-outright files carry re-home notes (destination namespace + verbatim, no contract surface). The remaining work is Tier 3 (editor / debug glue).

### ST1 — Kernel & shared types

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `VolumeData/Config.cs` → `Kernel/Config.cs` | 237 | `IConfig` interface + `internal sealed Config`; the singleton is gone. |
| **Done** | new — `KernelCompositionRoot` → `Kernel/KernelCompositionRoot.cs` | — | Sole `new()` site; Bootstrap entry point. |
| **Done** | new — `PluginRegistry` → `Kernel/PluginRegistry.cs` + `Kernel/Contracts/IPluginRegistry.cs` | — | Service-locator at the kernel boundary. |
| **Done** | new — boundary value-types → `Kernel/BoundaryValueTypes.cs` | — | `CartesianCoord`, `FeatureColour`, `VolumeExtents`, `SubcubeBounds`, `DataStats`, `HistogramData`, `AxisUnits` (M-21). |
| **Done** | `Tools/Delegates.cs` → `Kernel/Delegates.cs` | 28 | M-15 declaration site. |
| **Done** | new — cross-team contracts → `Kernel/Contracts/{IVolumeLoader, IVolumeRegistry, ILogSink, IDesktopShell, IVolumeStateCapture, Plugins/IFitsPlugin, Plugins/IWcsPlugin}` | — | Plus `IRawVoxelAccess` / `IVolumeDataSet` already in `External/`. |
| Done — re-home | `Tools/BenchmarkManager.cs` | 152 | → `iDaVIE.Kernel`; Infrastructure ACL over Unity Profiler (the `BenchmarkHarness` of global_model.md §1 ST1), preserved verbatim, no contract surface. |
| Tier 3 — pending | `Tools/CameraControllerTool.cs`, `Tools/EventTriggerExample.cs`, `Tools/FPSDisplay.cs` | 282 | Utility / debug helpers; no contract surface. |

### ST2 — Data I/O plug-ins

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `PluginInterface/FitsReader.cs` → `Data/FitsReaderPlugin.cs` | 730 | Realises `IFitsPlugin` + `IRawVoxelAccess` + `IFitsBinaryTableSource`. |
| **Done** | `PluginInterface/AstTool.cs` → `Data/WcsTransformPlugin.cs` | 93 | Realises `IWcsPlugin` + `IWcsMapping` + `ICoordinateTransformer` (M-06). |
| **Done** | `PluginInterface/DataAnalysis.cs` → `Data/DataAnalysisPlugin.cs` | 252 | Realises `IDataAnalysisPlugin` + `ISourceStatsProvider` (M-05, M-07). |
| **Done** | new — `MaskEditService` → `Data/MaskEditService.cs` | — | Realises `IMaskMutationService` + `IBrushStrokeHistory` + `IMaskStateCapture` + `IMaskEditState` (M-04, M-14). |
| **Done** | `PluginInterface/NativePluginLoader.cs` → `Data/NativePluginLoader.cs` | 271 | Infrastructure; reflection-based delegate binding. |
| **Done** | new — `Data/Contracts/{IBrushStrokeHistory, IMaskStateCapture}` | — | ST2 cross-team / persistence ports. |
| Done — re-home | `CatalogData/CatalogDataSet.cs`, `CatalogData/CatalogDataSetManager.cs`, `CatalogData/ColumnInfo.cs`, `CatalogData/DataMapping.cs`, `CatalogData/CatalogInputController.cs` | 1357 | → `iDaVIE.Data`; IPAC point-cloud parser + bindings (global_model.md §1 ST2), preserved verbatim, no contract surface. (`CatalogDataSetRenderer` is ST3 — see below.) |

### ST3 — Rendering Engine

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `VolumeData/VolumeDataSetRenderer.cs` → `Rendering/VolumeDataSetRenderer.cs` + 12 helpers | 1402 → 12 files | §6.3 mandated split + the unnamed services (`VolumeCoordinateService`, `RegionSelection`, `MaskEditingService`, `VolumePersistenceService`, `IRestFrequencyCatalogue`). |
| **Done (with assumptions)** | `VolumeData/VolumeDataSet.cs` → `Rendering/VolumeDataSet.cs` | 1920 → 1 | Skeleton carries `// ASSUMPTION:` blocks at every open design question (file-I/O on ST1 vs ST2; histogram lazy evaluation; WCS frame ownership). `refactor_plan.md` does not provide a per-method hotspot table; flagged for resolution. |
| **Done** | `VolumeData/MomentMapRenderer.cs` → `Rendering/MomentMapRenderer.cs` | 386 | Realises `IMomentMapRenderer` (M-08) declared in new `Rendering/Contracts/RenderingContracts.cs`. |
| **Done** | `CatalogData/CatalogDataSetRenderer.cs` → `Rendering/CatalogDataSetRenderer.cs` | 694 | Compute-buffer-only MonoBehaviour mirroring `FeatureVisualiser`. |
| **Done** | `Menu/HistogramHelper.cs`, `Menu/HistogramMenuController.cs` → `Rendering/HistogramService.cs` + `UI/HistogramMenuController.cs` | 323 → 2 | `IHistogramService` backed by `IRawVoxelAccess`; menu shell holds the service + `IRenderSettingsMutator`. |
| **Done** | new — `Rendering/Contracts/RenderingContracts.cs` | — | Canonical declaration site for `IRenderSettings`, `IRenderSettingsMutator`, `MaskMode`, `ScalingType`, `ProjectionMode`, `ColorMapEnum`, `IMomentMapRenderer`, `MomentMapRequest`, `MomentMapResult`, `IRenderStateCapture`. |
| Done — re-home | `LineRenderer/WorldSpaceLineRenderer.cs` | 320 | → `iDaVIE.Rendering`; MonoBehaviour owned outright (global_model.md §1 ST3), preserved verbatim, no contract surface. |
| Done — re-home | `Tools/ColorMapEnum.cs` (+ `ColorMapUtils`) | 57 | → `iDaVIE.Rendering.Contracts`; the `ColorMapEnum` enum is already declared in `RenderingContracts.cs`, the `ColorMapUtils` helpers relocate alongside, preserved verbatim. |

### ST4 — Interaction System

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `VolumeData/VolumeInputController.cs`, `VolumeData/VolumeCommandController.cs` → `Interaction/*.cs` | 2319 → 7 files | FSM split + interaction-contract surface. |
| **Done** | `Menu/QuickMenuController.cs`, `Menu/PaintMenuController.cs` → `Interaction/QuickAndPaintMenuControllers.cs` | — | Brief §6.4. |
| **Done** | `Shapes/Shape.cs`, `Shapes/ShapeAction.cs`, `Shapes/ShapesManager.cs`, `Shapes/StretchMesh.cs` → `Interaction/ShapeGestureFSM.cs` | 741 → 1 | FSM extracted from `ShapesManager` (M-23). The mesh / draw classes (Shape, StretchMesh) are owned outright as Tier 2. |
| **Done** | `Menu/ShapeMenuController.cs` → `Interaction/ShapeMenuController.cs` | 170 | VR menu shell holding `ShapeGestureFSM`. |
| **Done** | `VoiceCommands/*` (4 files) → `Interaction/VoiceCommandService.cs` + `Interaction/VoiceCommandRegistry.cs` | 384 → 2 | `IVoiceCommandStream` + injected vocabulary; no `UnityEngine.Windows.Speech` reach into domain (MOD-04). |
| **Done** | `UI/LaserPointer.cs`, `UI/PointerController.cs` → `Interaction/ControllerInputAdapter.cs` | 326 → 1 | Realises `IControllerEventStream` (MOD-01). |
| **Done** | `UI/KeypadController.cs` → `Interaction/KeypadInputAdapter.cs` | 92 → 1 | VR numeric input. |
| **Done** | new — `LocomotionConfig` (in `LocomotionFSM.cs`), `BrushConfig` / `DragGestureState` / `ShapeGestureState` (in `IInteractionContracts.cs`), `QuickMenuState` / `ScrollState` / `ControllerIdentity` (in `Interaction/InteractionValueTypes.cs`) | — | Per global_model.md §1 ST4 (M-10). |
| Done — re-home | `Shapes/Shape.cs`, `Shapes/StretchMesh.cs` (mesh-side, kept verbatim) | 217 | → `iDaVIE.Interaction`; mesh & draw classes owned outright (the `ShapeGestureFSM` split is the testable part, already Done), preserved verbatim. |
| Done — re-home | `VRKeyboard/*` (10 files) | 798 | → `iDaVIE.Interaction`; owned outright, preserved verbatim, no contract surface. |
| Done — re-home | `VideoMaker/*` (11 files) | 3013 | → `iDaVIE.Interaction`; owned by ST4 per global_model.md (fly-through input), preserved verbatim — the `IDVSParser` script-file format is internal and stable. |
| Tier 3 — pending | `Menu/VideoRecordMenuController.cs`, `Menu/VideoRecPointListController.cs` | 229 | Video-maker menu shells — re-home with `VideoMaker/`. |

### ST5 — Feature System

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | All 10 `FeatureData/*` files + `Menu/MomentMapMenuController.cs` + `Menu/SpectralProfileHelper.cs` | 3133 → 23 files | See "ST5 menus, ACL, and infrastructure readers" table above. |
| Tier 3 — pending | `Menu/SpectralProfileMenuController.cs` | 107 | Sprite-display wrapper called by `SpectralProfileHelper.UpdateUI`. Forward-declared as a stub inside `UI/SpectralProfileHelper.cs`; a real port would split out the sprite/plot rendering. |
| Tier 3 — replaced by | `FeatureData/FeatureMapper.cs` | 81 | Closed out — the empty static `FeatureMapper` is gone; `FeatureMapping` load/save logic is on `FeatureImportService.LoadMappingFromFile / SaveMappingToFile`. |
| Tier 3 — replaced by | `FeatureData/FeatureMenuDataSource.cs` | 89 | Closed out — `RecyclableScrollRect` adapter; the boundary types are sufficient and the adapter is UI-framework glue. |
| Tier 3 — replaced by | `FeatureData/FeatureAnchor.cs` | 61 | Closed out — corner-handle MonoBehaviour absorbed into `SelectionAnchorRenderer.ShowAt`. |

### ST6 — Desktop GUI

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `UI/Menus/RenderingController.cs` → `UI/RenderTabViewModel.cs` | 315 → 1 | ViewModel skeleton from prior session. |
| **Done** | `UI/Menus/OptionController.cs` → `UI/SourcesTabViewModel.cs` | 127 → 1 | ViewModel skeleton from prior session. |
| **Done** | `UI/CanvassDesktop.cs` (stats panel) + `Menu/HistogramHelper.cs` → `UI/StatsTabViewModel.cs` | partial | Histogram bins + threshold; depends on `IHistogramService`. |
| **Done** | `UI/DesktopPaintController.cs` presentation state → `UI/PaintTabViewModel.cs` + `UI/DesktopPaintRasteriser.cs` | 1558 → 2 | Polygon→voxel rasterisation in pure C#; commits via `IMaskMutationService.ApplyBrush` (M-14). |
| **Done** | `UI/CanvassDesktop.cs` (information panel) → `UI/InformationTabViewModel.cs` | partial | Holds `IVolumeDataSet` + `IVolumeLoader`. |
| **Done** | `UI/MenuBarBehaviour.cs` (menu commands) → `UI/MenuBarViewModel.cs` | partial | Holds `IVolumeLoader`, `IFeatureImportService`, `IWorkspaceSaveCommand`. |
| **Done** | `UI/CanvassDesktop.cs` (debug console) → `UI/DebugTabViewModel.cs` | partial | Subscribes to `ILogSink`. |
| **Done** | `UI/CanvassDesktop.cs` → `UI/CanvassDesktop.cs` (thin shell) | 1899 → 1 | Realises `IDesktopShell` (M-26) and `IDesktopStateCapture` (M-16). |
| **Done** | `UI/DesktopPaintController.cs` → `UI/DesktopPaintController.cs` (thin shell) | 1558 → 1 | Holds `PaintTabViewModel` + `DesktopPaintRasteriser`; wires Unity pointer events. |
| **Done** | new — `UI/Contracts/IDesktopStateCapture.cs` | — | ST6 persistence port (M-16). |
| Done — re-home | `UI/MenuBarBehaviour.cs` (Unity prefab wiring), `UI/Colorbar.cs`, `Menu/TabsManager.cs`, `Menu/ExitController.cs` | 394 | → `iDaVIE.UI`; UI shell widgets owned outright (MenuBar command logic already on `MenuBarViewModel`; the residual prefab wiring is kept verbatim), no contract surface. |
| Done — re-home | `UI/ToastNotification.cs`, `UI/UserConfirmationPopupController.cs`, `UI/PopUpButtonController.cs`, `UI/UserDraggableMenu.cs`, `UI/BrushSizeTooltip.cs`, `UI/PngExporter.cs` | 596 | → `iDaVIE.UI`; UI widget library owned outright, preserved verbatim, no contract surface. |
| Tier 3 — pending | `UI/ButtonHoverBehaviour.cs`, `UI/CustomDragHandler.cs`, `UI/UserSelectableItem.cs`, `UI/UserScrollableItem.cs` | 208 | Pure UI widgets; no contract surface. |

### ST7 — Persistence

ST7 has no legacy files at all — the entire sub-team is greenfield. Tier-1 skeleton-port covers the cross-team Contracts, the save/restore orchestration (`WorkspaceService` + `WorkspaceRepository` + `WorkspaceEnvelope`, wired by `PersistenceCompositionRoot` and surfaced by the `PersistenceMenuController` MonoBehaviour), and a `Domain/` model. The Tier 2 surface — Infrastructure (`FileSystemStorageBackend`, `EnvelopeSerializer`, `StateIndexPersistor`, `PersistenceConfigLoader`) and Presentation (`SaveWorkspaceDialog`, `LoadWorkspaceDialog`, `StateListPanel`, `AutosaveIndicator`) — is **specified with destination sub-namespaces but intentionally not skeleton-ported**: every member is an internal realisation of contracts already declared in `Persistence/PersistenceContracts.cs`, so porting them adds no new cross-team API surface.

| Status | Component | Notes |
|---|---|---|
| **Done** | Cross-team contracts → `Persistence/PersistenceContracts.cs` | Sole declaration site for the four `IWorkspace*`/`IStateIndexQuery`/`IPersistenceEvents` interfaces + the `SavedStateInfo` sealed class. Signatures verbatim from `shared_interfaces.md` §7. |
| **Done** | Domain → `Persistence/Domain/{StoredState, StateIndex, StorageLocation, PersistenceConfig, IntegrityRecord, PersistenceLog, MigrationRule}.cs` | 7 files. A richer persistence model (schema migration, integrity records, on-disk index) kept for a future wiring pass; not yet connected to the `WorkspaceService` → `WorkspaceRepository` path, which currently uses a flat `SavedStateInfo` list. |
| **Done** | Orchestration → `Persistence/WorkspaceService.cs` (+ `WorkspaceRepository`, `WorkspaceEnvelope`) | Single application-layer realiser of all four ST7 contracts; the six per-team capture ports are constructor-injected. Documented ISP trade-off (one class, four narrow interfaces sharing the repository + event logic). |
| **Done** | Application helper → `Persistence/Application/ValidationAndRecoveryService.cs` | Skeleton present (Tier-1 depth). Integrity-check / migration / rollback on load; not yet wired into `WorkspaceService`. |
| Done — specified | Infrastructure → `iDaVIE.Persistence.Infrastructure` (`FileSystemStorageBackend`, `EnvelopeSerializer`, `StateIndexPersistor`, `PersistenceConfigLoader`) | Storage backend + envelope serializer. Internal realisations of the declared `IWorkspace*` contracts; documented, not skeleton-ported (no new contract surface). |
| Done — specified | Presentation → `iDaVIE.Persistence.Presentation` (`SaveWorkspaceDialog`, `LoadWorkspaceDialog`, `StateListPanel`, `AutosaveIndicator`) | UI dialogs that mount via `IDesktopShell`; documented, not skeleton-ported (no new contract surface). |

ST5 already publishes `IFeatureStateCapture` (M-16) for ST7 to consume — that port is the ST5 side of the persistence contract.

### Editor / Debug — Tier 3

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| Tier 3 — pending | `Editor/CatalogDataSetManagerEditor.cs`, `FeatureSetManagerEditor.cs`, `FeatureSetRendererEditor.cs`, `VolumeCommandControllerEditor.cs`, `VolumeInputControllerEditor.cs` | 403 | Unity custom inspectors — will need new inspectors for the refactored MonoBehaviours, but they have no contract surface and can be ported lazily as each renderer is rewritten. |
| Tier 3 — pending | `Debuggers/DebugLogging.cs`, `Debuggers/FitsReaderDebug.cs` | 314 | Editor-only helpers; refactor target: realise `ILogSink` (ST1) for `DebugLogging`. |

### What is intentionally not here

- ST3 controllers `VolumeInputController` / `VolumeCommandController` MonoBehaviour shells — the FSM split is in `Interaction/`; the residual MonoBehaviour boilerplate is owned by ST4 and is a one-line re-home note.

## Build status

After the Tier-1 push the canonical declaration sites for every namespace referenced by `refactored/` source now exist on disk:

| Namespace | Canonical source |
|---|---|
| `iDaVIE.Kernel` | `Kernel/Config.cs`, `Kernel/KernelCompositionRoot.cs`, `Kernel/PluginRegistry.cs`, `Kernel/Delegates.cs` |
| `iDaVIE.Kernel.Contracts` | `Kernel/Contracts/I{PluginRegistry,VolumeLoader,VolumeRegistry,LogSink,DesktopShell,VolumeStateCapture}.cs` + `External/IVolumeDataSet.cs` (`IVolumeDataSet`, `IMaskEditState`, `LoadStatus`) |
| `iDaVIE.Kernel.Contracts.Types` | `Kernel/BoundaryValueTypes.cs` (`CartesianCoord`, `FeatureColour`, `VolumeExtents`, `SubcubeBounds`, `DataStats`, `HistogramData`, `AxisUnits`) |
| `iDaVIE.Kernel.Contracts.Plugins` | `Kernel/Contracts/Plugins/{IFitsPlugin, IWcsPlugin}.cs` + `External/IVolumeDataSet.cs` (`IRawVoxelAccess`, `VoxelBufferDescriptor`) |
| `iDaVIE.Data` | `Data/{FitsReaderPlugin, WcsTransformPlugin, DataAnalysisPlugin, MaskEditService, NativePluginLoader}.cs` + `External/ICoordinateTransformer.cs` (`ICoordinateTransformer`, `WorldCoord`, `IMaskMutationService`, `BrushStroke`, `VoxelCoord2D`, `BrushPaintMode`, `PaintConfig`, `SourceEntry`) |
| `iDaVIE.Data.Contracts` | `Data/Contracts/{IBrushStrokeHistory, IMaskStateCapture}.cs` |
| `iDaVIE.Rendering` | `Rendering/{VolumeDataSet, MomentMapRenderer, CatalogDataSetRenderer, HistogramService}.cs` + the §6.3 split files |
| `iDaVIE.Rendering.Contracts` | `Rendering/Contracts/RenderingContracts.cs` (`IRenderSettings`, `IRenderSettingsMutator`, `MaskMode`, `ScalingType`, `ProjectionMode`, `ColorMapEnum`, `IMomentMapRenderer`, `MomentMapRequest`, `MomentMapResult`, `MomentOrder`, `IRenderStateCapture`) |
| `iDaVIE.Interaction` | `Interaction/*.cs` (FSMs, contracts, value types, voice / controller / keypad adapters, shape gesture FSM, shape menu controller) |
| `iDaVIE.Features` | `Features/*.cs` (full ST5 catalogue) |
| `iDaVIE.UI` | `UI/*.cs` (5 ViewModels + 2 thin shells + rasteriser + ST5 menu shells + ST3 histogram menu) |
| `iDaVIE.UI.Contracts` | `UI/Contracts/IDesktopStateCapture.cs` |
| `iDaVIE.Persistence` | `Persistence/PersistenceContracts.cs` (`IWorkspaceSaveCommand`, `IWorkspaceLoadCommand`, `IStateIndexQuery`, `IPersistenceEvents`, `SavedStateInfo`) |
| `iDaVIE.Persistence.Domain` | `Persistence/Domain/*.cs` (7 files) |
| `iDaVIE.Persistence.Application` | `Persistence/Application/ValidationAndRecoveryService.cs` (1 file) |

The skeletons are still **not buildable** — every concrete carries `=> throw new NotImplementedException();` (or `// ASSUMPTION:` blocks in `Rendering/VolumeDataSet.cs`). They evidence target shape, not a parallel build. To turn this into a buildable project an `.asmdef` per namespace + the legacy method bodies need to be moved in; that is post-deliverable work.
