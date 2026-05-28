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

## What is intentionally not here

- Concrete `FeatureMenuController`, `FeatureMenuCell`, `MomentMapMenuController`, `SpectralProfileHelper` — feature-domain menu controllers per `ST5_domain_design.md` §7, but not direct decompositions of `FeatureSetRenderer`.
- Full `SelectionService`, `SelectionAnchorRenderer`, `SpectralProfileService`, `MomentMapServiceAdapter` — covered by the ST5 proposal; not on the `FeatureSetRenderer` decomposition path.
- ST3 controllers (`VolumeInputController`, `VolumeCommandController`) — own decomposition (ST4 / ST3-orchestration); separately worked.

## Build status

These skeletons reference types from `iDaVIE.Kernel.Contracts.Types` (`CartesianCoord`, `FeatureColour`, `VolumeExtents`, `SubcubeBounds`) and `iDaVIE.Data` (`SourceStats`, `ISourceStatsProvider`, `ICoordinateTransformer`, `IMaskMutationService`) that are declared in `shared_interfaces.md` but do not yet exist as source. The skeletons are **not buildable in the current repository** — they evidence target shape, not a parallel build.
