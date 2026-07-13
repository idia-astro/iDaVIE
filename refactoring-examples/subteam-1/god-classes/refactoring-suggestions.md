# Refactoring suggestions per god class

Companion to the [god-class inventory](README.md). For every god class identified there, this doc says **what to extract, which pattern to use, which layer it lands in, and which sub-team contract it shapes**. Recommendations are keyed to the target architecture in `CLAUDE.md §3` (Domain → Application → Infrastructure → Plug-in Host, ACL around Unity) and the ADR backlog in `CLAUDE.md §5`.

> Design-only. No upstream `.cs` files are modified.

## Conventions

- **Layer tags:** `[Domain]`, `[Application]`, `[Infrastructure]`, `[Plug-in Host]`, `[ACL]`.
- **Pattern names** are emphasised in *italics* — Repository, Command, Strategy, State, Value Object, Service Gateway, Mediator, Memento, Specification, Adapter, MVP, MVVM, Observer, Service Locator.
- "Extract" lists *what splits off*; "Keep" lists *what stays in the slimmed-down original class*.
- "Contract implications" calls out which downstream sub-team boundary (`CLAUDE.md §6`) changes shape as a result.

---

## 1. `VolumeDataSet`

`Assets/Scripts/VolumeData/VolumeDataSet.cs` — model + I/O + math + edit-history.

### Extract

- FITS read/write → `FitsCubeRepository` *(Repository, [Infrastructure])* — wraps `FitsReader` P/Invoke.
- Mask read/write → `FitsMaskRepository` *(Repository, [Infrastructure])*.
- AST / WCS coordinate ops → `WcsCoordinateService` *(Adapter, [Infrastructure])* — wraps `AstTool`; Domain consumes it via an interface.
- Histogram + statistics → `HistogramCalculator` *(pure function, [Application])* — wraps `DataAnalysis`.
- Subcube cropping → `SubcubeRequest` value object + `SubcubeService` *(Value Object + Service, [Domain] / [Application])*.
- Brush-stroke transactions / undo / redo → `BrushStrokeHistory` *(Command + Memento, [Domain])*. Each stroke is a `Command`; history is a Memento ring buffer.

### Keep

The bare `VolumeCube` aggregate (voxel buffer dimensions, owned `IntPtr`s, identity). Becomes a Domain aggregate root, no Unity types.

### Contract implications

- Sub-team 2 (Data I/O) consumes `FitsCubeRepository` and `FitsMaskRepository`.
- Sub-team 7 (Persistence) consumes the `BrushStrokeHistory` snapshot format.

### ADRs touched

ADR-2 (ABI versioning — repositories sit on the plug-in boundary), ADR-3 (error model — repository wraps native error codes), ADR-5 (memory ownership — `IntPtr` lifetime tokens leave the Domain).

---

## 2. `CanvassDesktop`

`Assets/Scripts/UI/CanvassDesktop.cs` — desktop launcher MonoBehaviour with 62 public methods.

### Extract

- File dialogs → `IFileDialogService` *(ACL, [Infrastructure])* — wraps `StandaloneFileBrowser` so Application stays SFB-free.
- File-load wizard (browse → HDU pick → subset bounds → mask validate → load) → `FitsImportWizardController` *(State, [Application])* — Stateless state machine, one state per step.
- Mask compatibility checks → `MaskCompatibilityValidator` *(Specification, [Domain])*.
- Memory pre-flight (`CheckMemSpaceForCubes`) → `CubeMemoryChecker` *(Service, [Infrastructure])*.
- Source / feature mapping → `SourceMappingViewModel` + `SourceMappingMapper` *(MVVM, [Application])*.
- Color-map dropdown, sigma, restore-defaults → bind to `RayMarchSettings` value object via `IRenderSettingsGateway` *(Service Gateway, [Application])*.
- `Exit()` → `IApplicationLifecycle` *(ACL, [Infrastructure])*.

### Keep

MonoBehaviour view glue only — Unity-side event wiring (`onClick`, `onValueChanged`).

### Contract implications

Sub-team 6 (Desktop GUI) **owns the slim `CanvassDesktop`** going forward — our refactoring removes its domain reach. `IRenderSettingsGateway` and the import-wizard are the two contracts they consume from us.

### ADRs touched

ADR-7 (ACL pattern for Unity / SFB), ADR-8 (registry / Service Locator — wizard pulls services from the locator), and a new ADR slot for "Service Gateway contract format" (`CLAUDE.md §6` owes Sub-team 6 a transport).

---

## 3. `VolumeInputController`

`Assets/Scripts/VolumeData/VolumeInputController.cs` — SteamVR input + state machines + hardware detection.

### Extract

- VR hardware family detection → `VRPlatformDetector` *(Strategy selector, [Infrastructure])*.
- Per-VR-family button mapping → `IControllerProfile` with one impl per family (`OculusProfile`, `ViveProfile`, `WMRProfile`) *(Strategy, [Application])*.
- SteamVR action plumbing → `ISteamVrInputProvider` *(ACL, [Infrastructure])* — the `IInputProvider` boundary type owed to Sub-team 4 (`CLAUDE.md §6`).
- Locomotion state machine → `LocomotionStateMachine` *(State, [Application])* in its own file.
- Interaction state machine → `InteractionStateMachine` *(State, [Application])* in its own file.
- Brush / paint inputs → `BrushInputHandler` *(Command, [Application])*.
- Feature anchor editing → `FeatureAnchorEditor` *([Application])*.
- Quick-menu toggling → `QuickMenuPresenter` *(MVP, [Application])*.

### Keep

A thin `VolumeInputController` MonoBehaviour that wires SteamVR events into the input provider and dispatches to the relevant handler — pure transport.

### Contract implications

Delivers `IInputProvider` to Sub-team 4 exactly as `CLAUDE.md §6` requires. Removes Sub-team 4's blocker on knowing "where does input land in the layered model."

### ADRs touched

ADR-4 (plug-in threading model — input dispatch is the canonical async boundary), ADR-7 (ACL — SteamVR is wrapped).

---

## 4. `DesktopPaintController`

`Assets/Scripts/UI/DesktopPaintController.cs` — paint UI + slice math + cameras + pointer handling.

### Extract

- Slice extraction `GetFloatSlice(Texture3D, axis, slice)` → `VolumeSliceExtractor` *(pure function, [Domain])* — consumes the `ITexture3DView` ACL wrapper.
- Region cube + texture management → `RegionCubeService` *(Domain service)* — operates on the `VolumeCube` aggregate from refactor #1.
- Slice / orthogonal camera rig → `SliceCameraRig` *([Application])*.
- Pointer / drag handlers → thin `IPointerDownHandler` adapter that delegates to `PaintCommandDispatcher` *(Command, [Application])*.
- Source list UI → `SourceListViewModel` *(MVVM, [Application])*.

### Keep

The MonoBehaviour + pointer interfaces. Everything math-heavy moves to Domain.

### Contract implications

Indirectly unblocks Sub-team 5 (Feature System) — features become first-class Domain objects rather than UI state of the paint controller.

### ADRs touched

ADR-6 (layer-dep enforcement — slice math is the cleanest "Domain must not see `Texture3D`" smoke test).

---

## 5. `VolumeDataSetRenderer` — canonical D5 target

`Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` — 171 public members, ray-march driver + everything else.

### Extract

- Ray-march parameters (steps, jitter, scaling bias / contrast / alpha / gamma, threshold) → `RayMarchSettings` *(Value Object, [Domain])*.
- Projection mode → `IProjectionStrategy` with `MaxProjection` + `AverageProjection` *(Strategy, [Application])*.
- Foveated rendering → `FoveatedRenderingController` *([Application])*.
- Vignette → `VignetteController` *([Application])* — shared with refactor #6.
- Mask + projection state → `MaskDisplayController` *([Application])* — consumes `IFitsMaskRepository` from refactor #1.
- Cursor voxel readback → `CursorVoxelReader` *([Application])*.
- Moment-map orchestration → `IMomentMapOrchestrator` interface; existing `MomentMapRenderer` implements it *([Application])*.
- Feature-set ownership → inverted to DI: renderer no longer owns `FeatureSetManager`; resolved through `IFeatureSetRegistry` *(Repository, [Application])*.
- Benchmark coupling (`BenchmarkManager` directly reads renderer fields) → `IRenderingTelemetry` event stream *(Observer, [Application])*.
- Shader / Material binding → `IVolumeMaterialBinder` *(ACL, [Infrastructure])* — the only class that touches `Material` / `Graphics`.

### Keep

MonoBehaviour shell — `Awake` / `Start` / `Update` lifecycle, the actual `Graphics.DrawProcedural` call, and the held `RayMarchSettings`.

### Contract implications

Delivers the **render-pipeline abstraction seam** owed to Sub-team 3 (`CLAUDE.md §6`) — `IVolumeMaterialBinder` *is* the texture-data handoff contract.

### ADRs touched

ADR-6 (layer-dep enforcement — central case), ADR-7 (ACL for `UnityEngine.Material` / `Graphics`), ADR-8 (registry — `IFeatureSetRegistry`).

---

## 6. `CatalogDataSetRenderer`

`Assets/Scripts/CatalogData/CatalogDataSetRenderer.cs` — catalog rendering god, same shape as #5.

### Extract

- VOTable / catalog table loading → `ICatalogTableLoader` *(Repository, [Infrastructure])* — sits next to `VoTable.cs`.
- Column mapping (`UpdateMappingColumns`) → `CatalogColumnMapper` *(Mapper, [Application])*.
- Value cutoffs → `ValueCutoffFilter` *(Specification, [Domain])*.
- Visuals (color-map texture, sprite sheet, opacity, visibility) → `CatalogVisualPresenter` *([Application])*.
- Vignette → reuse `VignetteController` from refactor #5 (same code shape — extract once, share).

### Keep

The MonoBehaviour shell + the `Material` / `ComputeBuffer` hookup.

### Contract implications

Mirrors refactor #5's render-pipeline seam — both renderers should consume the same `IVolumeMaterialBinder`-style ACL.

### ADRs touched

ADR-6, ADR-7 (same as #5).

---

## 7. `VolumeCommandController`

`Assets/Scripts/VolumeData/VolumeCommandController.cs` — voice keyword recogniser + 30+ verb handlers.

### Extract

- `KeywordRecognizer` wrapper → `IVoiceRecogniser` *(ACL, [Infrastructure])* — the contract owed to Sub-team 4 (`CLAUDE.md §6`).
- 30+ verb handlers → one class per command implementing `IVoiceCommand` *(Command, [Application])*. `VolumeCommandController` becomes a thin dispatcher.
- Push-to-talk → `PushToTalkController` *([Application])*.
- Dataset registry (`AddDataSet` / `RemoveDataSet`) → shared `IDatasetRegistry` *(Service Locator, [Application])* — same registry used by refactors #3 and #5.

### Keep

MonoBehaviour lifecycle that subscribes to `IVoiceRecogniser` and routes recognised keywords to the command bus.

### Contract implications

Delivers `IVoiceRecogniser` to Sub-team 4 (`CLAUDE.md §6`). The Command-pattern shape **matches** the paint refactor (#4) — same dispatcher, two recognisers (voice + pointer).

### ADRs touched

ADR-7 (ACL for `UnityEngine.Windows.Speech`), ADR-8 (registry).

---

## Cross-cutting refactorings

Themes that appear in three or more of the seven recommendations above — worth extracting once, then reused.

1. **`IDatasetRegistry`** — central seam shared by `VolumeInputController`, `VolumeCommandController`, `VolumeDataSetRenderer`, and `CanvassDesktop`. Today each class re-discovers active datasets independently (array fields, `FirstOrDefault(isActiveAndEnabled)`). Centralise.
2. **ACL bundle** — `IFileDialogService`, `ISteamVrInputProvider`, `IVoiceRecogniser`, `IVolumeMaterialBinder`, `IApplicationLifecycle`, plus thin wrappers for `Texture3D` / `Material` / `Graphics`. Together these *are* the anti-corruption layer mandated by `CLAUDE.md §3`.
3. **Repository pattern for plug-in I/O** — `FitsCubeRepository`, `FitsMaskRepository`, `ICatalogTableLoader`. Standardises how Domain consumes the native plug-in and pins the kernel boundary that ADR-2 / ADR-3 / ADR-5 are about.
4. **Command pattern shared between paint and voice** — `IVoiceCommand` (refactor #7) and `IPaintCommand` (refactor #4) are the same shape. One dispatcher class, two recognisers (voice + pointer). Also makes record-and-replay (for video / benchmark) trivial.

## ADR backlog mapping

Each ADR in `CLAUDE.md §5` is now backed by at least one motivating refactor:

| ADR (`CLAUDE.md §5`) | Justified by refactor # |
|---|---|
| 1. Client–server transport | (out of scope for god-class work) |
| 2. Plug-in C ABI versioning | 1, 6 |
| 3. Plug-in error model | 1, 6 |
| 4. Plug-in threading | 3 |
| 5. Memory ownership at ABI | 1 |
| 6. Layer-dep enforcement | 4, 5, 6 (canonical) |
| 7. ACL pattern for Unity | 2, 3, 4, 5, 6, 7 |
| 8. Plug-in registry / Service Locator | 2, 5, 7 |

This is the precondition for ADR drafting — first drafts are due by `CLAUDE.md §8 day 3` (2026-05-20). Every ADR now has a concrete refactor it can cite for context, decision, and consequences.

## D5 worked-example selection

Default recommendation reaffirmed: **`VolumeDataSetRenderer`** (mandated canonical, `CLAUDE.md §4`) + **`VolumeDataSet`**. They share the bulk of the cross-cutting refactorings (ACL, Repository, Service Locator), so the two before/after walk-throughs reinforce each other and cover six of the eight ADRs between them.
