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

## Path to a buildable project

Every legacy file under `Assets/Scripts/` is now accounted for — each has a refactored skeleton in `refactored/`, a "replaced by" closeout, or an owned-outright re-home note (the per-method legacy → new-home mapping is in "Mapping back to the legacy files" above). The skeletons evidence target *shape*, not a parallel build: every concrete carries `=> throw new NotImplementedException();` (or `// ASSUMPTION:` blocks in `Rendering/VolumeDataSet.cs`), there is no `.asmdef`, and the `External/` files are reference declarations standing in for assemblies their owning teams ship. What remains is the post-deliverable work of turning that shape into compiling, tested code.

### Assembly map

One `.asmdef` per namespace; these are the canonical declaration sites on disk today:

| Namespace | Canonical source |
|---|---|
| `iDaVIE.Kernel` | `Kernel/Config.cs`, `Kernel/KernelCompositionRoot.cs`, `Kernel/PluginRegistry.cs`, `Kernel/Delegates.cs`, `Kernel/DebugLogSink.cs` |
| `iDaVIE.Kernel.Contracts` | `Kernel/Contracts/I{PluginRegistry,VolumeLoader,VolumeRegistry,LogSink,DesktopShell,VolumeStateCapture}.cs`, `Kernel/Contracts/EnumString.cs` + `External/IVolumeDataSet.cs` (`IVolumeDataSet`, `IMaskEditState`, `LoadStatus`) |
| `iDaVIE.Kernel.Contracts.Types` | `Kernel/BoundaryValueTypes.cs` (`CartesianCoord`, `FeatureColour`, `VolumeExtents`, `SubcubeBounds`, `DataStats`, `HistogramData`, `AxisUnits`) |
| `iDaVIE.Kernel.Contracts.Plugins` | `Kernel/Contracts/Plugins/{IFitsPlugin, IWcsPlugin}.cs` + `External/IVolumeDataSet.cs` (`IRawVoxelAccess`, `VoxelBufferDescriptor`) |
| `iDaVIE.Data` | `Data/{FitsReaderPlugin, WcsTransformPlugin, DataAnalysisPlugin, MaskEditService, NativePluginLoader}.cs` + `External/ICoordinateTransformer.cs` (`ICoordinateTransformer`, `WorldCoord`, `IMaskMutationService`, `BrushStroke`, `VoxelCoord2D`, `BrushPaintMode`, `PaintConfig`, `SourceEntry`) |
| `iDaVIE.Data.Contracts` | `Data/Contracts/{IBrushStrokeHistory, IMaskStateCapture}.cs` |
| `iDaVIE.Rendering` | `Rendering/{VolumeDataSet, MomentMapRenderer, CatalogDataSetRenderer, HistogramService}.cs` + the §6.3 split files |
| `iDaVIE.Rendering.Contracts` | `Rendering/Contracts/RenderingContracts.cs` (`IRenderSettings`, `IRenderSettingsMutator`, `MaskMode`, `ScalingType`, `ProjectionMode`, `ColorMapEnum`, `IMomentMapRenderer`, `MomentMapRequest`, `MomentMapResult`, `MomentOrder`, `IRenderStateCapture`) |
| `iDaVIE.Interaction` | `Interaction/*.cs` (FSMs, contracts, value types, voice / controller / keypad adapters, shape gesture FSM, shape menu controller) |
| `iDaVIE.Features` | `Features/*.cs` (full ST5 catalogue) |
| `iDaVIE.UI` | `UI/*.cs` (5 ViewModels + 2 thin shells + rasteriser + ST5 menu shells incl. `SpectralProfileMenuController` + ST3 histogram menu) |
| `iDaVIE.UI.Contracts` | `UI/Contracts/IDesktopStateCapture.cs` |
| `iDaVIE.Persistence` | `Persistence/PersistenceContracts.cs` (`IWorkspaceSaveCommand`, `IWorkspaceLoadCommand`, `IStateIndexQuery`, `IPersistenceEvents`, `SavedStateInfo`) |
| `iDaVIE.Persistence.Domain` | `Persistence/Domain/*.cs` (7 files) |
| `iDaVIE.Persistence.Application` | `Persistence/Application/ValidationAndRecoveryService.cs` (1 file) |

ST7 Infrastructure (`iDaVIE.Persistence.Infrastructure`) and Presentation (`iDaVIE.Persistence.Presentation`) are specified but not yet on disk (see the ST5/ST7 notes above); they gain `.asmdef`s when their concretes land in step 4.

### Ordered steps

1. **Assembly definitions (`.asmdef` per namespace).** Add one assembly definition per row of the map above, with `references` wired strictly bottom-up along the acyclic ownership graph in `global_model.md §2` (ST1 → ST2 → ST3 → ST4 → ST5 → ST6 → ST7; the `*.Contracts` assemblies carry no outbound team references). This step is the enforcement point for brief §4.2 constraint 2 — a back-edge fails to compile instead of slipping past review. Replace the two `External/` reference declarations (`IVolumeDataSet`, `ICoordinateTransformer`) with real references to the owning assemblies (`iDaVIE.Kernel.Contracts`, `iDaVIE.Data`); they exist only so ST3/ST5 compile in isolation.

2. **Settle the open design questions.** Resolve the `// ASSUMPTION:` blocks in `Rendering/VolumeDataSet.cs` (file-I/O ownership ST1 vs ST2; histogram lazy evaluation; WCS-frame ownership) with ST1/ST2 before any body moves — they decide which class owns which field. This needs a per-method hotspot table for `VolumeDataSet`, which `refactor_plan.md` does not yet provide (flagged), built the same way as its existing `VolumeDataSetRenderer` / `FeatureSetRenderer` tables.

3. **Move the legacy bodies in, verbatim.** For each skeleton-ported class, port the legacy method bodies into the `NotImplementedException` stubs, following the per-method mapping in "Mapping back to the legacy files" and the hotspot tables in `refactor_plan.md`. Bodies move unchanged except for the seams the refactor introduced: `Config.Instance` → injected `IConfig`; static plug-in calls (`FitsReader.` / `AstTool.` / `DataAnalysis.`) → the injected plug-in interfaces; direct MonoBehaviour references → the cross-team interfaces (`IRenderSettings`, `IFeatureSetQuery`, …).

4. **Implement the remainder.** (a) Greenfield concretes with no legacy body — the ST7 Infrastructure (`FileSystemStorageBackend`, `EnvelopeSerializer`, `StateIndexPersistor`, `PersistenceConfigLoader`) and the Presentation dialogs, plus the consolidated `Data/MaskEditService` — get real implementations. (b) Owned-outright files (`CatalogData/*`, `VRKeyboard/*`, `VideoMaker/*`, the ST6 UI widgets, `WorldSpaceLineRenderer`, `ColorMapEnum` / `ColorMapUtils`, `BenchmarkManager`, `FitsReaderDebug`, the `Tools/` utilities) drop in with only a namespace change into the assembly named by their re-home note. (c) `DebugLogSink` realises `ILogSink`.

5. **Wire the composition roots.** `KernelCompositionRoot` (the sole permitted `new()` site) and `PersistenceCompositionRoot` instantiate the concretes and hand out interfaces — the single construction point that replaces the removed `Config.Instance` singleton and the previously-static plug-in registry / native loader.

6. **Rewrite the Editor inspectors.** The five Unity custom inspectors are rebuilt against the refactored MonoBehaviours once those exist (step 3); they have no contract surface and stay in each team's `Editor/`.

7. **Compile, integrate, test.** Resolve against the real per-team assemblies (replacing `External/`), then add the unit tests the refactor was *for*: the now-Unity-free domain (`Feature`, `FeatureSet`, the FSMs, the application services) is testable without a Unity context — the headline payoff of the SRP / DIP splits, and the gap the baseline report cited as "no automated tests".

> **Intentionally absent:** the ST3 `VolumeInputController` / `VolumeCommandController` MonoBehaviour shells — the FSM split lives in `Interaction/`; the residual boilerplate is a one-line ST4 re-home, not a skeleton.

This is post-deliverable work: the design-only assignment stops at evidencing the target shape, and the steps above are the hand-off for whoever takes the skeletons forward.
