# Design Justification — VolumeDataSetRenderer Refactoring

**Author:** Sub-team 1 — Architecture and Micro-kernel Core  
**Date:** 2026-06-02  
**Artefact cross-reference:** `after/` code skeletons, `metrics.md`, `docs/Architecture overview.pdf`

---

## How to read this document

Each section names one design decision, states the specific rule from the Architecture Overview that mandates or motivates it, and explains how the decision satisfies that rule. References use the following shorthands:

| Shorthand | Refers to |
|-----------|-----------|
| ASR 2.1   | Architecture Overview §2.1 Modularity ASRs |
| ASR 2.2   | Architecture Overview §2.2 Analysability ASRs |
| ASR 2.3   | Architecture Overview §2.3 Modifiability ASRs |
| ASR 2.4   | Architecture Overview §2.4 Testability ASRs |
| NFR-x     | Architecture Overview §3 Kernel NFRs (load time, plug-in isolation, hot-reload, …) |
| Layer     | Architecture Overview "Layered architecture inside the kernel" diagram |
| ABI §n    | Architecture Overview C ABI spec sections |
| CLAUDE §n | CLAUDE.md section |

---

## Decision 1 — Split the god-class into six plain-C# services

**What:** `VolumeRenderingService`, `MaskControllerService`, `VolumeCoordinateMapper`, `RegionControllerService`, `RestFrequencyService`, and `VolumeDataExportService` are all plain C# classes — none inherit `MonoBehaviour`.

**Rules satisfied:**

**ASR 2.4 Testability — "Every domain class must have zero direct UnityEngine imports"** and **"Every public interface must be mockable with no Unity runtime present."**  
The original `VolumeDataSetRenderer` was a `MonoBehaviour`, which means any class that held a reference to it pulled in the full Unity runtime. A plain-C# service can be instantiated in an NUnit Edit-mode test without loading a Unity scene. The Architecture Overview specifies that this mockability is verified by the *Edit-mode unit test suite in CI*.

**Layer — Domain and Application layers must not import UnityEngine.**  
The layered diagram explicitly labels the Domain Layer "pure C# · no UnityEngine · no SteamVR · fully unit testable". Services that orchestrate mask painting (`MaskControllerService`), coordinate mapping (`VolumeCoordinateMapper`), and frequency management (`RestFrequencyService`) contain no Unity types and map directly onto the Application layer. The only Unity-coupled code is in `VolumeRenderingService` (which owns `Material` instances) and in the thin `VolumeDataSetRenderer` MonoBehaviour — both of which live in the Infrastructure layer.

**ASR 2.3 Modifiability — "Replacing Unity version requires changes only in the Infrastructure layer — zero Domain or Application changes."**  
Because `Material`, `Shader`, `FilterMode`, `Transform`, and `MeshRenderer` are confined to `VolumeRenderingService` and the orchestrator, a Unity 5 → Unity 6 migration touches exactly those two classes. The five domain-facing services are unaffected.

---

## Decision 2 — Thin MonoBehaviour orchestrator (~200 lines, WMC 18)

**What:** `VolumeDataSetRenderer` retains the `MonoBehaviour` base class and Inspector-serialised fields but contains no business logic. Its `Update()` builds four value objects from Inspector state and calls the four `ApplyXxx` methods on `IVolumeRenderer`. Its `_startFunc()` creates and wires services. All passthrough methods are one-liners.

**Rules satisfied:**

**ASR 2.2 Analysability — "No class shall exceed 200 lines in the domain layer"** (verified by SonarQube LOC rule — fail = merge blocked).  
The original was 1 403 lines. The refactored orchestrator is approximately 200 lines. This keeps it within the SonarQube-enforced threshold.

**ASR 2.2 Analysability — "Cyclomatic complexity per method must not exceed 10"** (SonarQube complexity gate on every PR).  
In the original, `_startFunc()` had cyclomatic complexity ≈ 12 and `Update()` ≈ 8. In the refactored orchestrator, `Update()` has complexity 2 (one `if (!started)` guard), and `_startFunc()` has complexity ≈ 5. No orchestrator method approaches the threshold of 10.

**ASR 2.1 Modularity — "CBO must be ≤14 for domain classes and ≤25 for orchestrators"** (Understand CK metrics snapshot at Day 2 and Day 13).  
The original CBO was ≈ 17, violating even the orchestrator threshold. The refactored orchestrator's CBO is ≈ 9 (the six service interfaces plus `VolumeDataSet`, `MomentMapRenderer`, and `Config`).

**ASR 2.3 Modifiability — "Propagation cost (DV8) must decrease by ≥30% from Day 2 baseline to Day 13 projection."**  
The god-class was a hub in the dependency graph — 17 outgoing couplings meant that almost any change propagated through it. Splitting to six services removes the god-class from the propagation path for five of those concerns, directly reducing DV8 propagation cost.

---

## Decision 3 — Six narrow interfaces (ISP compliance, ≤7 members each)

**What:** `IVolumeRenderer` (6), `IMaskController` (3), `ICoordinateMapper` (5), `IRegionController` (7), `IRestFrequencyController` (4 + 1 event), `IVolumeDataExporter` (3).

**Rules satisfied:**

**ASR 2.4 Testability — "Interface size must not exceed 7 public members (ISP compliance)"** (NDepend interface size rule).  
Every interface in the refactored design sits at or below the mandated threshold. This is a hard CI gate: the NDepend rule will block merge if violated.

**ASR 2.4 Testability — "Every public interface must be mockable with no Unity runtime present."**  
Because each interface is narrow and references only value objects or plain-C# types (`VolumeDataSet`, `Feature`, `Vector3`, `Vector3Int`), a Moq or NSubstitute stub can implement any interface in an NUnit Edit-mode test without loading Unity. The Architecture Overview states this is enforced by the *Edit-mode unit test suite in CI*.

**CLAUDE.md §4 — "Every public API boundary between layers is an interface + has at least one test double."**  
Each of the six interfaces is the boundary between the Infrastructure-layer MonoBehaviour and the Application-layer services. Narrowness is what makes test-double creation tractable: a developer need only stub the 3–7 relevant methods rather than a 45-method god class.

**ASR 2.3 Modifiability — "A new C/C++ plug-in can be added without modifying existing kernel code (Open-Closed Principle)."**  
Sub-team 2 (Data I/O) delivers plug-ins that feed data into the rendering pipeline. `IVolumeRenderer.RegenerateCubes()` is the seam through which a new data source signals the renderer to reload — adding a new data format does not require touching `VolumeDataSetRenderer` or any service, only adding a new `IFitsReaderPlugin` implementation behind the Plug-in Host.

---

## Decision 4 — Four immutable value objects replace scattered Inspector fields

**What:** `RenderingParameters`, `MaskParameters`, `FoveationParameters`, and `VignetteParameters` are `readonly struct` types. `VolumeDataSetRenderer.Update()` constructs them from Inspector fields each frame and passes them to `IVolumeRenderer`.

**Rules satisfied:**

**ASR 2.2 Analysability — "Effectiveness of assessing change impact and identifying parts to modify."**  
In the original, threshold parameters, mask parameters, foveation parameters, and vignette parameters were 20+ flat fields interleaved in a single class and referenced from three different methods (`Update`, `_startFunc`, `OnRenderObject`). A change to the threshold model required searching 1 403 lines for every reference. After refactoring, a threshold change touches `RenderingParameters.WithThresholds()` and `VolumeRenderingService.ApplyRenderingParameters()` — two locations in two files.

**ASR 2.3 Modifiability — "Propagation cost (DV8) must decrease by ≥30% from Day 2 baseline."**  
Value objects are the primary tool for reducing propagation cost: because `RenderingParameters` owns its fields, a field addition propagates to exactly one struct and one service method rather than to every method that previously referenced the flat field.

**ASR 2.2 Analysability — "Code duplication must not exceed 5% across domain classes"** (SonarQube duplication report).  
The original `Update()` contained inline shader property assignments that duplicated parts of `_startFunc()`. The value-object pattern centralises parameter grouping; the `ApplyXxx` methods are the single site for each set of assignments.

---

## Decision 5 — `ICoordinateMapper` injected into `RegionControllerService`

**What:** `RegionControllerService` does not hold a `Transform` reference directly. It receives an `ICoordinateMapper` at construction time. `VolumeCoordinateMapper` implements the interface and holds the `Transform`.

**Rules satisfied:**

**ASR 2.1 Modularity — "No circular dependencies shall exist between any two top-level components"** (NDepend CQLinq rule on every PR — fail = merge blocked) and **"Dependency cycles at namespace and assembly level must be 0."**  
If `RegionControllerService` held a `Transform` it would couple the region service directly to `UnityEngine`, creating an upward dependency from the Application layer to the Infrastructure layer (which wraps Unity). The Architecture Overview's layered diagram marks any upward dependency a **FORBIDDEN** CI fail condition. Injecting `ICoordinateMapper` keeps the region service in the Application layer, which may only depend downward.

**Layer — "Infrastructure is where Unity lives. It wraps Unity and SteamVR APIs in adapters so the layers above never directly touch Unity code."**  
`VolumeCoordinateMapper` wraps the `Transform` and lives in the Infrastructure layer. `RegionControllerService` sees only `ICoordinateMapper` — a Domain/Application-layer contract. This is the exact pattern described in the layered diagram for the Infrastructure layer.

**ASR 2.4 Testability — "Every public interface must be mockable with no Unity runtime present."**  
Because `ICoordinateMapper` takes and returns `Vector3` and `Vector3Int` (Unity math structs, but available in the Unity Mathematics package or stubbed), crop-boundary calculations in `RegionControllerService` can be unit-tested by supplying a stub `ICoordinateMapper` that returns pre-set voxel coordinates — no Transform, no scene required.

---

## Decision 6 — All `FitsReader` P/Invoke calls contained in `VolumeDataExportService`

**What:** `VolumeDataExportService` is the only class in the refactored design that calls `FitsReader.FitsOpenFileReadOnly`, `FitsReader.FitsOpenFileReadWrite`, and `FitsReader.FitsCloseFile`. The original `VolumeDataSetRenderer.SaveMask()` called these directly.

**Rules satisfied:**

**Layer — Plug-in Host Layer: "loads C/C++ DLLs · enforces versioned ABI · isolates native failures."**  
The Architecture Overview's Level 3 Component Diagram places `FitsReader` as an external C/C++ plug-in loaded by the Plug-in Host. The C ABI spec (§4 Threading Model) explicitly notes: *"All native delegates are invoked from a single dispatcher thread in the Infrastructure layer. The dispatcher also resolves the UpdateMaskVoxel violation (FitsReader.cs:601) where Vector3Int leaks UnityEngine into the binding."* By confining `FitsReader` calls to one service, the dispatcher boundary is a single wiring point — not scattered across rendering code.

**NFR Plug-in isolation — "A crash in one plug-in must not bring down the kernel or other plug-ins"** (contract test suite — fault injection).  
When all FitsReader calls are in one service, fault injection tests can target that service in isolation. A native crash inside `FitsReader` is caught at the boundary of `VolumeDataExportService` without propagating through the rendering pipeline.

**ASR 2.3 Modifiability — "A new C/C++ plug-in can be added without modifying existing kernel code (Open-Closed Principle)."**  
If CFITSIO is replaced with a different FITS backend, the change is localised to `VolumeDataExportService` (and the corresponding Plug-in Host adapter). The rendering services, coordinate mapper, and region controller are entirely unaffected.

**ABI §4 Threading Model — "C# dispatcher: All native delegates are invoked from a single dispatcher thread in the Infrastructure layer."**  
Containing all P/Invoke sites in one service is a prerequisite for the dispatcher pattern: you cannot enforce single-threaded dispatch if P/Invoke calls are scattered across an untraceable god-class.

---

## Decision 7 — `RestFrequencyService` exposes a `FrequencyChanged` event rather than direct coupling

**What:** `IRestFrequencyController.FrequencyChanged : Action<double>` fires whenever the active rest frequency changes. The orchestrator and downstream UI panels subscribe to this event rather than polling the renderer.

**Rules satisfied:**

**ASR 2.3 Modifiability — "Replacing Unity version requires changes only in the Infrastructure layer."**  
In the original, `RestFrequencyGHzChanged` was an `event Action` on `VolumeDataSetRenderer` — a MonoBehaviour. Sub-team 6 (Desktop GUI) and sub-team 3 (Rendering) subscribed to it. If the MonoBehaviour is refactored or moved, all subscribers break. With `IRestFrequencyController.FrequencyChanged`, subscribers depend only on the interface — not on the MonoBehaviour — so the Unity version upgrade does not require changing subscriber code.

**CLAUDE.md §6 — Sub-team contract for Sub-team 6 (Desktop GUI): "Service Gateway contract; transport mechanism."**  
The `FrequencyChanged` event is the seam through which the Desktop GUI panel (Sub-team 6) receives spectral axis updates. Publishing through an interface event rather than a direct field reference ensures that the Desktop GUI's only compile-time dependency is on `IRestFrequencyController` — not on the renderer assembly.

**Layer — Application Layer: "coordinates domain objects · no Unity types."**  
`RestFrequencyService` imports only `System`, `System.Collections.Generic`, and `System.Linq` — no Unity types. The `Action<double>` event type is a standard .NET delegate. This places the service cleanly in the Application layer as described by the Architecture Overview.

---

## Decision 8 — `VolumeRenderingService` owns the `MaterialID` property-ID cache

**What:** The `private static class Ids` with all 33 `Shader.PropertyToID` calls is defined inside `VolumeRenderingService` and is inaccessible to any other class. In the original, the equivalent `MaterialID` struct was nested in `VolumeDataSetRenderer` but called from multiple method clusters.

**Rules satisfied:**

**ASR 2.2 Analysability — "Code duplication must not exceed 5% across domain classes"** (SonarQube duplication report).  
Centralising all property IDs in one location ensures that a shader property rename requires editing exactly one `static readonly int` — not hunting for string literals scattered across a large file.

**ASR 2.3 Modifiability — "Replacing Unity version requires changes only in the Infrastructure layer."**  
`Shader.PropertyToID` is a Unity API. Keeping all calls in `VolumeRenderingService` (Infrastructure layer) means a Unity shader API change requires changes only in that one service.

**NFR Platform portability — "No direct kernel32 calls — NativePluginLoader must be cross-platform."**  
By extension, Unity-specific APIs (`Shader`, `Material`, `FilterMode`) should not leak beyond the Infrastructure layer. Confining `Shader.PropertyToID` to `VolumeRenderingService` enforces this boundary at a class level, not just a layer level.

---

## Decision 9 — `VolumeCoordinateMapper` wraps `Transform` behind `ICoordinateMapper`

**What:** The six coordinate-conversion methods previously scattered across `VolumeDataSetRenderer` (`ConvertWorldPositionToDataCubePosition`, `ConvertWorldRotationToDatacubeRotation`, `GetVoxelPositionWorldSpace`, `VolumePositionToLocalPosition`, `LocalPositionToVolumePosition`, `GetVoxelPositionDataSpace`) are consolidated into one service implementing `ICoordinateMapper`.

**Rules satisfied:**

**ASR 2.1 Modularity — "CBO must be ≤14 for domain classes."**  
The original renderer's CBO of 17 included coupling to `Transform` from within coordinate-mapping methods, rendering methods, and region methods all at once. Extracting the mapper reduces the renderer's CBO directly (it no longer needs to call `transform.InverseTransformPoint` from seven different methods) and groups all Transform usage into a class whose CBO is 3.

**ASR 2.2 Analysability — "Cyclomatic complexity per method must not exceed 10."**  
`GetVoxelPositionWorldSpace` in the original had complexity ≈ 4. Consolidating the six methods into `VolumeCoordinateMapper` with uniform implementations keeps each method's complexity at ≤ 3.

**Layer — "Infrastructure is where Unity lives. It wraps Unity and SteamVR APIs in adapters."**  
The Architecture Overview's Infrastructure layer description names `UnityVolumeRendererAdapter` and `UnityInputAdapter` as examples of the adapter pattern. `VolumeCoordinateMapper` is the coordinate-space adapter — it wraps `transform.InverseTransformPoint` and `transform.rotation` behind an interface so that callers in the Application layer are dependency-inverted away from `UnityEngine.Transform`.

---

## Decision 10 — CBO reduced from 17 to ≤9 (orchestrator) and ≤8 (any service)

**What:** The combined effect of all the above decisions is that no single class has CBO exceeding 9.

**Rules satisfied:**

**ASR 2.1 Modularity — "CBO must be ≤14 for domain classes and ≤25 for orchestrators"** (Understand CK metrics snapshot at Day 2 and Day 13).  
This is a hard tracked metric. The Architecture Overview states it is measured at Day 2 and Day 13; the Day 13 projection must be within threshold. The refactored design brings every class within both the domain and orchestrator limits:

| Class | Before CBO | After CBO | Limit |
|-------|-----------|-----------|-------|
| `VolumeDataSetRenderer` | 17 | 9 | 25 (orchestrator) |
| `VolumeRenderingService` | — | 8 | 14 (service) |
| All other services | — | ≤6 | 14 |

**ASR 2.1 Modularity — "A change to one plug-in shall require zero recompilation of any other plug-in."**  
High CBO is the mechanism by which recompilation cascades. When `VolumeDataSetRenderer` coupled to `FitsReader`, `Material`, `FeatureSetManager`, `VolumeInputController`, and `MomentMapRenderer` simultaneously, a change to any of those cascaded through the renderer to every class that referenced it. The six-service split breaks those cascade paths: a change to `FitsReader` propagates only to `VolumeDataExportService`.

---

## Decision 11 — `MaskControllerService` isolates voxel painting state

**What:** `PaintCursor`, `PaintMask`, `FinishBrushStroke`, `_previousPaintLocation`, `_previousPaintValue`, and `_brushRadius` are moved from `VolumeDataSetRenderer` into `MaskControllerService`.

**Rules satisfied:**

**ASR 2.4 Testability — "Branch coverage ≥70% on all domain and application layer classes"** (SonarQube coverage gate).  
In the original, `PaintMask` contained a guard clause checking `_maskDataSet`, a region-size check, an offset calculation, and a deduplication check — all interleaved with rendering state. Unit tests for paint logic required constructing a full `VolumeDataSetRenderer` with materials and outlines. As a standalone service, `MaskControllerService.PaintMask` can be tested with only a `VolumeDataSet` stub, making 70% branch coverage achievable in the Edit-mode CI suite.

**Layer — Application Layer: use-case orchestration.**  
The Architecture Overview's Level 3 Component Diagram places `MaskAggregate` in the Domain layer and `ExportMaskUseCase` in the Application layer. Voxel painting is the Application-layer use case that writes to `MaskAggregate`. `MaskControllerService` maps directly onto this slot in the layered diagram.

**NFR Plug-in isolation — "A crash in one plug-in must not bring down the kernel."**  
Mask painting calls `_maskDataSet.PaintMaskVoxel`, which eventually crosses the native boundary via `FitsReader`. If this call throws a native exception, it is caught within `MaskControllerService` — it does not propagate through the rendering service's `Update` loop and crash the entire frame.

---

## Summary traceability matrix

| Decision | Primary ASR/NFR/Layer | Secondary |
|----------|----------------------|-----------|
| 1. Six plain-C# services | ASR 2.4 Testability (zero UnityEngine imports); Layer (Application/Domain) | ASR 2.3 Modifiability (Unity version isolation) |
| 2. Thin MonoBehaviour orchestrator | ASR 2.2 Analysability (≤200 LOC, CC≤10); ASR 2.1 (CBO≤25) | ASR 2.3 Propagation cost |
| 3. Six narrow interfaces ≤7 members | ASR 2.4 (ISP, mockability); CLAUDE.md §4 | ASR 2.3 (Open-Closed) |
| 4. Immutable value objects | ASR 2.2 Analysability (change impact); ASR 2.3 Propagation cost | ASR 2.2 Duplication |
| 5. ICoordinateMapper injection | ASR 2.1 (no circular deps, CQLinq gate); Layer (no upward deps) | ASR 2.4 Testability |
| 6. FitsReader contained in VolumeDataExportService | Layer (Plug-in Host); ABI §4 Threading; NFR Plug-in isolation | ASR 2.3 OCP |
| 7. FrequencyChanged event | ASR 2.3 (Unity version isolation); CLAUDE.md §6 Sub-team 6 contract | Layer (Application) |
| 8. MaterialID cache in VolumeRenderingService | ASR 2.2 Duplication; ASR 2.3 (Unity version isolation) | NFR Platform portability |
| 9. VolumeCoordinateMapper wraps Transform | ASR 2.1 CBO; Layer (Infrastructure adapter) | ASR 2.2 CC |
| 10. CBO ≤9/≤8 system-wide | ASR 2.1 CBO threshold (Day 13 measurement) | ASR 2.1 No recompilation cascade |
| 11. MaskControllerService | ASR 2.4 Branch coverage ≥70%; Layer (Application use case) | NFR Plug-in isolation |
