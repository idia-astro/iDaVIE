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

End goal: every legacy file under `iDaVIE/Assets/Scripts/` has either a refactored skeleton in `refactored/`, an explicit "replaced by" line that closes it out, or an explicit "owned outright — no refactor required" tag. Items below are grouped by sub-team owner per `global_model.md §1`. LOC counts come from the legacy source. **Tier 1** is on the critical SOLID/GRASP path; **Tier 2** is owned-outright code that the refactor preserves verbatim; **Tier 3** is editor / debug glue with no production surface. A team should typically deliver Tier 1 first, ship Tier 2 as a one-line re-home note, and only touch Tier 3 if it crosses a contract boundary.

### ST1 — Kernel & shared types

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Tier 1 — pending** | `VolumeData/Config.cs` | 237 | Refactor target: `internal sealed Config` value object loaded once at startup; consumers receive injected `IConfig` rather than `Config.Instance`. Required by every sub-team. |
| Tier 1 — pending | new — `KernelCompositionRoot` | — | Sole `new()` site for cross-layer concretes (per global_model.md §1 ST1). Wires every `Inject(...)` call in `refactored/`. |
| Tier 1 — pending | new — `PluginRegistry` | — | `GetPlugin<T>()` service locator at the kernel boundary; loads ST2 plug-ins. |
| Tier 1 — pending | new — boundary value-types module | — | `CartesianCoord`, `FeatureColour`, `MomentMapResult`, `DataStats`, `HistogramData`, `VolumeExtents`, `SubcubeBounds` (M-21). Currently only stub-declared inside `External/`. |
| Tier 1 — pending | `Tools/Delegates.cs` | 28 | Central delegate declaration site (M-15). |
| Tier 2 — pending | `Tools/BenchmarkManager.cs` | 152 | Owned outright; ACL over Unity Profiler. Re-home note only. |
| Tier 3 — pending | `Tools/CameraControllerTool.cs`, `Tools/EventTriggerExample.cs`, `Tools/FPSDisplay.cs` | 282 | Utility / debug helpers; no contract surface. |

### ST2 — Data I/O plug-ins

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Tier 1 — pending** | `PluginInterface/FitsReader.cs` | 730 | Refactor target: `FitsReaderPlugin` realising `IFitsPlugin` + `IRawVoxelAccess` + the `IFitsBinaryTableSource` port declared by `Features/FitsTableReader.cs`. P/Invoke isolated to a versioned ABI per brief §4.2. |
| Tier 1 — pending | `PluginInterface/AstTool.cs` | 93 | Refactor target: `WcsTransformPlugin` realising `IWcsPlugin` + `ICoordinateTransformer` (M-06). |
| Tier 1 — pending | `PluginInterface/DataAnalysis.cs` | 252 | Refactor target: `DataAnalysisPlugin` realising `IDataAnalysisPlugin` + `ISourceStatsProvider` (M-05, M-07). |
| Tier 1 — pending | new — `MaskEditService` | — | Realises `IMaskMutationService` + `IBrushStrokeHistory` + `IMaskStateCapture` (M-04, M-14). Absorbs `VolumeDataSet`'s paint-brush undo / redo and mask-mode toggles. |
| Tier 2 — pending | `PluginInterface/NativePluginLoader.cs` | 271 | Infrastructure; reflection-based delegate binding. Re-home note only. |
| Tier 2 — pending | `CatalogData/CatalogDataSet.cs`, `CatalogData/CatalogDataSetManager.cs`, `CatalogData/ColumnInfo.cs`, `CatalogData/DataMapping.cs`, `CatalogData/CatalogInputController.cs` | 1357 | IPAC point-cloud parsing + bindings. Owned outright by ST2; preserve as-is. (`CatalogDataSetRenderer` is ST3 — see below.) |

### ST3 — Rendering Engine

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `VolumeData/VolumeDataSetRenderer.cs` → `Rendering/VolumeDataSetRenderer.cs` + 12 helpers | 1402 → 12 files | §6.3 mandated split + the unnamed services (`VolumeCoordinateService`, `RegionSelection`, `MaskEditingService`, `VolumePersistenceService`, `IRestFrequencyCatalogue`). |
| **Tier 1 — pending** | `VolumeData/VolumeDataSet.cs` | 1920 | The other god class. Refactor target: WCS / histogram / mask voxel editing / undo-redo / source statistics split per `refactor_plan.md`. Currently has no skeleton at all. |
| Tier 1 — pending | `VolumeData/MomentMapRenderer.cs` | 386 | Concrete behind `IMomentMapRenderer` (M-08) — `MomentMapServiceAdapter` references the contract but the realisation is not skeletonised. |
| Tier 1 — pending | `CatalogData/CatalogDataSetRenderer.cs` | 694 | Per global_model.md §1 ST3 (M-18, IR-02). Refactor target similar to `FeatureVisualiser`: separate the compute-buffer rendering from the data-set lifecycle. |
| Tier 2 — pending | `LineRenderer/WorldSpaceLineRenderer.cs` | 320 | Owned outright; re-home note only. |
| Tier 2 — pending | `Tools/ColorMapEnum.cs` | 57 | Owned outright; the `ColorMapEnum` referenced by `Rendering/MaskModes.cs` lives here. |
| Tier 1 — pending | `Menu/HistogramHelper.cs`, `Menu/HistogramMenuController.cs` | 323 | Histogram is a volume-data view (analogous to spectral profile but ST3-owned). Refactor target: `IHistogramService` on ST3 backed by `IRawVoxelAccess`; menu controller in ST6 holds the service. |

### ST4 — Interaction System

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| **Done** | `VolumeData/VolumeInputController.cs`, `VolumeData/VolumeCommandController.cs` → `Interaction/*.cs` | 2319 → 7 files | FSM split + interaction-contract surface. |
| **Done** | `Menu/QuickMenuController.cs`, `Menu/PaintMenuController.cs` → `Interaction/QuickAndPaintMenuControllers.cs` | — | Brief §6.4. |
| **Tier 1 — pending** | `Shapes/Shape.cs`, `Shapes/ShapeAction.cs`, `Shapes/ShapesManager.cs`, `Shapes/StretchMesh.cs` | 741 | Shape gesture state (M-23). Converts to mask edits via the new `IMaskMutationService` — refactor target: extract gesture FSM from the `ShapesManager` MonoBehaviour. |
| Tier 1 — pending | `Menu/ShapeMenuController.cs` | 170 | VR menu for shape tools; holds the new gesture FSM. |
| Tier 1 — pending | `VoiceCommands/VoiceCommandListCreator.cs`, `VoiceCommandIndicator.cs`, `VoiceCommandListItem.cs`, `ColourMapListCreator.cs` | 384 | Voice subsystem; refactor target: `IVoiceCommandStream` + `VoiceCommandRegistry`. The keyword vocabulary becomes injected rather than hard-coded per controller. |
| Tier 1 — pending | `UI/LaserPointer.cs`, `UI/PointerController.cs` | 326 | VR pointer; refactor target: `IControllerEventStream` realisation. |
| Tier 1 — pending | `UI/KeypadController.cs` | 92 | VR numeric input; consumed by `MomentMapMenuController` for threshold entry. |
| Tier 1 — pending | new — `LocomotionConfig`, `BrushConfig`, `DragGestureState`, `QuickMenuState`, `ScrollState`, `ControllerIdentity` value-types | — | Per global_model.md §1 ST4 (M-10 rename). |
| Tier 2 — pending | `VRKeyboard/*` (10 files) | 798 | Owned outright; re-home note only. |
| Tier 2 — pending | `VideoMaker/*` (11 files) | 3013 | Owned by ST4 per global_model.md (fly-through input). Re-home note only — the `IDVSParser` script-file format is internal and stable. |
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
| **Done (partial)** | `UI/Menus/RenderingController.cs` → `UI/RenderTabViewModel.cs` | 315 → 1 | ViewModel skeleton; View (binding code, prefab wiring) still pending. |
| **Done (partial)** | `UI/Menus/OptionController.cs` → `UI/SourcesTabViewModel.cs` | 127 → 1 | As above. |
| **Tier 1 — pending** | `UI/CanvassDesktop.cs` | 1899 | God class — refactor target: decompose into per-panel ViewModels (one per CanvassDesktop tab) consuming the ST5 / ST3 / ST7 contracts. The biggest single-file refactor still outstanding. |
| **Tier 1 — pending** | `UI/DesktopPaintController.cs` | 1558 | Refactor target: polygon-rasterise on the ST6 side; commit via `IMaskMutationService.ApplyBrush` (M-14). Currently holds direct Texture3D reads — removed per global_model.md §1 ST6 ("Direct Texture3D reads of RegionCube / MaskCube — Removed"). |
| Tier 1 — pending | new — `IDesktopShell` realisation | — | ST6 realises this for ST1; no skeleton yet. |
| Tier 1 — pending | new — `IDesktopStateCapture` | — | Persistence port (M-16). |
| Tier 2 — pending | `UI/MenuBarBehaviour.cs`, `UI/Colorbar.cs`, `Menu/TabsManager.cs`, `Menu/ExitController.cs` | 394 | Owned outright; UI shell widgets. Re-home note only. |
| Tier 2 — pending | `UI/ToastNotification.cs`, `UI/UserConfirmationPopupController.cs`, `UI/PopUpButtonController.cs`, `UI/UserDraggableMenu.cs`, `UI/BrushSizeTooltip.cs`, `UI/PngExporter.cs` | 596 | Owned outright; UI widget library. Re-home note only. |
| Tier 3 — pending | `UI/ButtonHoverBehaviour.cs`, `UI/CustomDragHandler.cs`, `UI/UserSelectableItem.cs`, `UI/UserScrollableItem.cs` | 208 | Pure UI widgets; no contract surface. |

### ST7 — Persistence

ST7 has no legacy files at all — the entire sub-team is greenfield.

| Status | Component | Notes |
|---|---|---|
| **Tier 1 — pending** | `StoredState` envelope, `StateIndex`, `StorageLocation`, `PersistenceConfig`, `IntegrityRecord`, `PersistenceLog`, `MigrationRule` | Domain (`ST7_conceptual_model.md`). |
| Tier 1 — pending | Save / Load / state-management use cases, validation & recovery | Application — composes the per-team capture ports into `StoredState`. |
| Tier 1 — pending | `IWorkspaceSaveCommand`, `IWorkspaceLoadCommand`, `IStateIndexQuery`, `IPersistenceEvents` | Cross-team contracts owned by ST7. |
| Tier 1 — pending | Persistence UI panels (uses `IDesktopShell`) | Save / load / state-list dialogs. |

ST5 already publishes `IFeatureStateCapture` (M-16) for ST7 to consume — that port is the ST5 side of the persistence contract.

### Editor / Debug — Tier 3

| Status | Legacy | LOC | Notes |
|---|---|---|---|
| Tier 3 — pending | `Editor/CatalogDataSetManagerEditor.cs`, `FeatureSetManagerEditor.cs`, `FeatureSetRendererEditor.cs`, `VolumeCommandControllerEditor.cs`, `VolumeInputControllerEditor.cs` | 403 | Unity custom inspectors — will need new inspectors for the refactored MonoBehaviours, but they have no contract surface and can be ported lazily as each renderer is rewritten. |
| Tier 3 — pending | `Debuggers/DebugLogging.cs`, `Debuggers/FitsReaderDebug.cs` | 314 | Editor-only helpers; refactor target: realise `ILogSink` (ST1) for `DebugLogging`. |

### What is intentionally not here

- ST3 controllers `VolumeInputController` / `VolumeCommandController` MonoBehaviour shells — the FSM split is in `Interaction/`; the residual MonoBehaviour boilerplate is owned by ST4 and is a one-line re-home note.

## Build status

These skeletons reference types declared in `shared_interfaces.md` but not yet realised as source:

| Namespace | Types referenced |
|---|---|
| `iDaVIE.Kernel.Contracts` | `IVolumeDataSet`, `IMaskEditState`, `LoadStatus` |
| `iDaVIE.Kernel.Contracts.Types` | `CartesianCoord`, `FeatureColour`, `VolumeExtents`, `SubcubeBounds`, `DataStats`, `HistogramData`, `AxisUnits` |
| `iDaVIE.Kernel.Contracts.Plugins` | `IRawVoxelAccess`, `VoxelBufferDescriptor` |
| `iDaVIE.Data` | `ICoordinateTransformer`, `WorldCoord`, `IMaskMutationService`, `BrushStroke`, `StrokePaintConfig`, `VoxelCoord2D`, `BrushPaintMode`, `PaintConfig`, `SourceEntry` |
| `iDaVIE.Features` | `SourceStats`, `ISourceStatsProvider`, `IDataAnalysisPlugin` (per shared_interfaces.md §5.5 — ST5 owns the declaration, ST2 realises) |
| `iDaVIE.Rendering.Contracts` | `IRenderSettings`, `IRenderSettingsMutator`, `MaskMode`, `ScalingType`, `ProjectionMode`, `ColorMapEnum` |

The skeletons are **not buildable in the current repository** — they evidence target shape, not a parallel build. Stub reference declarations for the most-consumed cross-team types live in `External/IVolumeDataSet.cs` and `External/ICoordinateTransformer.cs` so the ST3/ST5 splits compile-as-illustrated.
