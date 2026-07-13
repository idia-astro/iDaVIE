# Sub-team 3: Rendering Engine — Design Document
**Team Alpha | Cache Me If You Can**
*Sprint 2 | 25 May 2026*
*Brief reference: Section 9.2 Deliverable 2 + Section 6.3 (Rendering Layer Design, Shader/Asset Policy, Metrics Worksheet)*

---

## 1. Executive Summary

- This document analyses the iDaVIE rendering layer using CK metrics to evaluate code health, performance and maintainability
- In addition, this document proposes modifications to the codebase's architecture and design to improve performance and stability, in particular, `VolumeDataSetRenderer`.
- `VolumeDataSetRenderer` (~1 400 lines, WMC 97, CBO 28) is a monolith that violates SRP, OCP, and DIP; the proposal splits it into four focused classes behind clean interfaces.
- The document outlines the two concrete refactoring examples demonstrated in `refactoring-examples/`.

---

## 2. Problem Statement

### 2.1 The Core Finding

`VolumeDataSetRenderer` spans 1,403 lines of C# and fails every measurable quality threshold:

| Metric | Baseline | Target | Excess |
|--------|---------------|--------|--------|
| WMC (Weighted Methods per Class) | **44** | ≤ 20 | 2.2× over |
| CBO (Coupling Between Objects) | **45** | ≤ 14 | 3.2× over |
| RFC (Response For a Class) | **89** | ≤ 50 | 1.8× over |
| LCOM (Lack of Cohesion in Methods) | **0.81** | ≤ 0.5 | 1.6× over |
| DIT (Depth of Inheritance Tree) | 1 | ≤ 4 | ✅ |
| NOC (Number of Children) | 0 | ≤ 5 | ✅ |

The four failing metrics indicate a **God Class**. The worst single method, `_startFunc`, carries CC = 28 across 185 lines. CBO = 45 means 45 other files must be considered on any edit.

### 2.2 Responsibility Inventory

| # | Responsibility | Proposed Owner |
|---|---------------|----------------|
| 1 | Shader keyword and material property management | `VolumeMaterialBinder` |
| 2 | 3D texture upload, caching, memory budget | `VolumeTextureManager` |
| 3 | Camera matrix calculation and clip planes | `VolumeCameraDriver` |
| 4 | Foveated sampling rate decisions | `FoveatedSamplingPolicy` |
| 5 | Mask mode branching | `IMaskMode` strategy implementations |
| 6–8 | Region selection, cursor tracking, FITS I/O | Out of scope for this refactor |

### 2.3 Hard-coded Render Pipeline

Three calls in `VolumeDataSetRenderer` are incompatible with Unity 6 URP: `Graphics.DrawProceduralNow` (lines 1148, 1154), `OnRenderObject` (line 1142), and `Shader.EnableKeyword` (lines 1099, 1103). The refactored architecture isolates all pipeline-specific code behind `IRenderPipeline`, making the domain testable without a Unity player context.

---

## 3. Scope

### 3.1 What Our Sub-Team Owns

`VolumeDataSetRenderer.cs` is the subject of this refactor. 

We own the GPU-side volume rendering layer: all classes, interfaces, and shader files that convert a loaded FITS cube into a visible 3D image in the VR headset. This covers the five target classes, the `IRenderPipeline` and `IMaskMode` interfaces and their implementations, and the test doubles in `iDaVIE.Rendering.Tests`. Sub-team 3 consumes the `IGaze` interface (defined jointly with Sub-team 4; confirmed 2 June 2026) and owns the `StubGazeProvider` test double, but holds no dependency on any SteamVR SDK type in its domain assemblies.

### 3.2 Out of Scope

- **FITS data ingest (Sub-team 2)** — Sub-team 3 consumes volume data only through `IRawVolumeDataSource` and `RawVolumeData`; it never reads a FITS file directly.
- **VR interaction and gaze SDK (Sub-team 4)** — The concrete gaze implementation is Sub-team 4's responsibility. `FoveatedSamplingPolicy` has no dependency on VR-specific input beyond the `IGaze` interface.
- **Application shell (Sub-team 7)** — Scene lifecycle, menus, and session save/restore are out of scope. `VolumeRenderCoordinator` is Sub-team 3's sole contribution to the scene graph. `RegionSelectionController` and `CursorPaintController` remain untouched in the monolith this sprint.

## 4. Design Decisions

### 4.1 Current Architecture (As-Is)

The current structure is a single node coupled to 45 files across 8 packages. The SOLID/GRASP audit (§8) catalogues 17 confirmed violations — 6 Critical, 8 High, 1 Medium.

### 4.2 Target Architecture (To-Be)

`VolumeDataSetRenderer` is replaced by five collaborating classes, each behind its own interface:

| Class | Single Responsibility | Proj. WMC | Proj. CBO |
|---|---|---|---|
| `VolumeRenderCoordinator` | Orchestrates per-frame loop; wires the four domain classes | ≤ 10 | ≤ 6 |
| `VolumeMaterialBinder` | Shader keyword management, material binding, colour-map application | ~16 | ≤ 11 |
| `VolumeTextureManager` | 3D texture upload, LRU caching, 368 MB budget enforcement | ≤ 20 | ≤ 8 |
| `VolumeCameraDriver` | Camera matrix calculation, clip planes, projection mode | ≤ 12 | ≤ 6 |
| `FoveatedSamplingPolicy` | Per-frame sample-rate decision from gaze direction | ≤ 8 | ≤ 6 |

`VolumeRenderCoordinator` is the only class that inherits from `MonoBehaviour` and the only class that knows all four domain classes exist. It contains zero domain logic — all computation is delegated.

### 4.3 DD-01 — Render Pipeline Abstraction (`IRenderPipeline`)

`IRenderPipeline` is a six-member interface in `iDaVIE.Rendering`. The two concrete adapters — `UrpRenderPipeline` and `HdrpRenderPipeline` — are the only files permitted to import `UnityEngine.Rendering.Universal` or HDRP namespaces. A `NullRenderPipeline` test double enables edit-mode unit tests without a GPU.

**Problem — three Built-In RP calls that break in URP:**

| Call | Location | URP incompatibility |
|---|---|---|
| `Graphics.DrawProceduralNow` | lines 1148, 1154 | Executes outside a command buffer — silently dropped by URP |
| `OnRenderObject` callback | line 1142 | Not invoked by the URP render loop — draw call never fires |
| `Shader.EnableKeyword` | lines 1099, 1103 | URP deprecates global keywords in favour of per-material `LocalKeyword` |

```csharp
public interface IRenderPipeline {
    void AddCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
    void RemoveCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
    void SetPipelineKeyword(string keyword, bool enabled);
    bool DepthTextureAvailable { get; }
    void Initialise();
    void Dispose();
}
```

**Method rationale:**

| Method | Replaces | Domain role |
|---|---|---|
| `AddCommandBuffer` / `RemoveCommandBuffer` | `Graphics.DrawProceduralNow` + `OnRenderObject` | Domain schedules ray-march work via `CommandBuffer`; adapter chooses the camera event |
| `SetPipelineKeyword` | `Shader.EnableKeyword` / `DisableKeyword` | Adapter translates to `LocalKeyword` (URP) or HDRP equivalent — domain never knows which |
| `DepthTextureAvailable` | Inline `_CameraDepthTexture` branching | URP requires opt-in; HDRP always provides it — `VolumeMaterialBinder` reads this once at startup |
| `Initialise` / `Dispose` | Inline setup/teardown in `_startFunc` | Adapter registers `ScriptableRenderFeature`, allocates command buffers, releases GPU resources |

**DIP justification:** High-level policy depends on `IRenderPipeline`, not on `UnityEngine.Rendering.Universal`. Future pipeline migrations touch only the adapter — zero domain changes. This resolves V-10 and V-15 (§8.1).

**Concrete adapters:**

| Adapter | Assembly | Key distinction |
|---|---|---|
| `UrpRenderPipeline` | `iDaVIE.Rendering.URP` | Only class in the codebase importing `UnityEngine.Rendering.Universal`; URP major-version upgrades touch this file only |
| `HdrpRenderPipeline` | `iDaVIE.Rendering.HDRP` | Injects via `RenderPipelineManager.beginCameraRendering`; `DepthTextureAvailable` returns `true` unconditionally |
| `NullRenderPipeline` | `iDaVIE.Rendering.Tests` (Editor-only) | All no-ops; `DepthTextureAvailable = true`; `.asmdef` Editor flag prevents shipping in production builds |

### 4.4 DD-02 — Mask Mode Strategy Pattern (`IMaskMode`)

The current if/else chain at lines 1072–1094 (`VolumeDataSetRendererMaskMode.cs`) is an OCP violation (V-04): adding a new mask mode requires editing four files — `MaskMode` enum, `VolumeDataSetRenderer`, `BasicVolume.cginc`, and `PaintMenuController`. It also cannot be unit-tested without a real `Material` and a running Unity context.

**Before (abbreviated):**
```csharp
if (MaskMode == MaskMode.Enabled)      { _materialInstance.EnableKeyword("_MASK_APPLY");   _materialInstance.SetFloat("_MaskAlpha", 1.0f); }
else if (MaskMode == MaskMode.Inverted) { _materialInstance.EnableKeyword("_MASK_INVERSE"); _materialInstance.SetFloat("_MaskAlpha", 1.0f); }
else if (MaskMode == MaskMode.Isolated) { _materialInstance.EnableKeyword("_MASK_ISOLATE"); _materialInstance.SetFloat("_MaskAlpha", 0.15f); }
```

The Strategy pattern replaces it with a two-member interface:

```csharp
public interface IMaskMode {
    void Apply(Material material, Texture3D maskTexture);
    string ShaderKeyword { get; }
}
```

| Class | Shader Keyword | `_MaskAlpha` | Behaviour |
|---|---|---|---|
| `ApplyMaskMode` | `_MASK_APPLY` | 1.0 | Mask region rendered; outside hidden |
| `InverseMaskMode` | `_MASK_INVERSE` | 1.0 | Outside rendered; mask hidden |
| `IsolateMaskMode` | `_MASK_ISOLATE` | 0.15 | Mask full opacity; outside at 15% |
| `DisabledMaskMode` | *(none)* | 0.0 | Mask texture unbound |

Adding a new mode = one new class, zero existing files changed. Worked example in `refactoring-examples/example2-MaskModes/`.

**OCP/SRP justification:** Each implementation changes only when its own rendering behaviour changes — no class has a reason to change that belongs to another. `VolumeMaterialBinder` holds a single `IMaskMode _activeMaskMode` field; mode switching is a constructor call with no branching in the coordinator.

**Pattern choice:** Strategy over Decorator (mask modes are mutually exclusive per frame — composition adds complexity without benefit) and over State (mode is set by external user input, not autonomous transitions — Strategy is the simpler fit).

### 4.5 DD-03 — `VolumeDataSetRenderer` Split (SRP)

Colour-coding analysis of the class body identified four dense, well-separated clusters. Each maps to an extracted class:

| Cluster | Lines (approx.) | Extracted To |
|---|---|---|
| Shader keyword and material property writes | ~180 | `VolumeMaterialBinder` |
| 3D texture allocation, upload, eviction | ~210 | `VolumeTextureManager` |
| Camera matrix, clip planes, projection mode | ~95 | `VolumeCameraDriver` |
| Foveated sample-rate calculation | ~40 | `FoveatedSamplingPolicy` |
| Orchestration shell | Thin | `VolumeRenderCoordinator` |

Each extracted class operates on a single field cluster — the structural guarantee that LCOM approaches zero.

**One reason to change per class:**
- `VolumeMaterialBinder` — shader property protocol changes (new keyword, new float, colour map pipeline change)
- `VolumeTextureManager` — texture lifecycle policy changes (budget, eviction strategy, filter mode)
- `VolumeCameraDriver` — camera math changes (projection mode, coordinate frame, clip plane policy)
- `FoveatedSamplingPolicy` — foveation algorithm changes (zone thresholds, fallback policy)

`VolumeRenderCoordinator` contains zero domain logic — every computation is delegated. A SonarQube gate (CC > 2 = build failure) enforces this; any method that computes rather than delegates fails the build.

### 4.6 DD-04 — Foveated Rendering Extraction (`FoveatedSamplingPolicy`)

The current code calls the SteamVR gaze API directly inside `Update()` — a DIP breach (V-07). `FoveatedSamplingPolicy` takes an `IGaze` interface at construction. When `IsTracking == false`, it falls back to a uniform sample rate, preserving INV-01. Sub-team 4 implements the concrete gaze class; `StubGazeProvider` covers all unit tests.

**Confirmed interface — agreed with Sub-team 4, 2 June 2026:**

```csharp
// Defined jointly with Sub-team 4. Sub-team 3 consumes; Sub-team 4 implements.
// We use GazeFocusPoint, GazeDirection, and IsTracking only.
public interface IGaze
{
    Vector3 GazeDirection  { get; }  // world-space; valid when IsTracking is true
    Vector2 GazeFocusPoint { get; }  // normalised screen coords [0,1]; (0.5, 0.5) = centre
    bool    IsTracking     { get; }  // false = no HMD / not worn / not calibrated
}
```

Sub-team 4 owns a unified `IGaze` interface used by both teams. We consume only the three members above; any additional members on their full interface are irrelevant to `FoveatedSamplingPolicy`.

**Fallback:** `IsTracking == false` → `FoveatedSamplingPolicy.ComputeParameters()` returns `FoveationParameters.Uniform(maxSteps)` — identical to today's HMD-absent path; INV-01 preserved.

**Unity 6 migration:** `VolumeRenderCoordinator` wires the gaze provider as a serialised field:
```csharp
[SerializeField] private IGaze _gazeProvider;
```
Swapping the eye-tracking SDK = swap the component in the Inspector. No code changes in the rendering layer.

**DIP dependency direction:**
- `FoveatedSamplingPolicy` (high-level) → `IGaze` (abstraction)
- Concrete gaze class (low-level, Sub-team 4) → implements `IGaze`

The domain assembly never names the concrete type. Resolves V-07 and V-15.

### 4.7 DD-05 — Constructor Injection / Composition Root

All four domain classes (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`) receive every collaborator through their constructor. No class calls `FindObjectOfType`, `Camera.main`, `Config.Instance`, or any other ambient locator at runtime. This resolves V-07 and V-08 (§6.1).

`VolumeRenderCoordinator` is the single **composition root**: the only place in the rendering layer where concrete types are named. It instantiates each domain class, injects interfaces, and holds the resulting references. All other classes see only interfaces.

```csharp
// VolumeRenderCoordinator.Awake() — sole point where concrete types are named
_pipeline   = new UrpRenderPipeline();
_binder     = new VolumeMaterialBinder(_pipeline, _material);
_texManager = new VolumeTextureManager(_memoryBudget);
_camera     = new VolumeCameraDriver(Camera.main);
_foveation  = new FoveatedSamplingPolicy(_gazeProvider, _foveationConfig);
```

**Why this matters for testability:** every class can be exercised in edit-mode by substituting a stub at construction — no Unity player loop needed. `NullRenderPipeline`, `StubGazeProvider`, and `NullMaskMode` are injected in tests; nothing else changes.

**Trade-off:** the coordinator's `Awake()` method names all concrete types, making it the one file that changes when a concrete implementation is swapped. This is intentional and contained — it is the definition of a composition root.

---

### 4.8 DD-06 — Zero-Allocation Per-Frame Value Types

Domain methods that are called every frame return **readonly structs** rather than classes, eliminating per-frame heap allocation and GC pressure on the 90 fps hot path.

| Struct | Produced by | Consumed by | Size target |
|---|---|---|---|
| `FoveationParameters` | `FoveatedSamplingPolicy.Evaluate()` | `VolumeMaterialBinder` | ≤ 32 bytes |
| `CameraFrameState` | `VolumeCameraDriver.ComputeFrame()` | `VolumeMaterialBinder`, `VolumeRenderCoordinator` | ≤ 64 bytes |
| `VolumeRenderState` | `VolumeRenderCoordinator` (assembled) | `VolumeMaterialBinder.Apply()` | ≤ 64 bytes |

The 64-byte cap is enforced by NFR-08 and a SonarQube custom rule: any struct in the `iDaVIE.Rendering` namespace that exceeds 64 bytes fails the build. This prevents the structs from growing over time into hidden allocation sources.

**IL2CPP consideration:** Unity's IL2CPP compiler de-virtualises interface calls on sealed classes in release builds, eliminating virtual dispatch cost on the hot path. All four domain classes are `sealed` explicitly to enable this optimisation and to communicate that they are not designed for subclassing.

---

### 4.9 DD-07 — Strangler Fig Migration Strategy

The refactor uses the **Strangler Fig pattern**: new classes are introduced alongside the monolith and activated incrementally, so `VolumeDataSetRenderer` continues to function throughout the migration. A Big Bang rewrite — deleting the monolith and replacing it in one step — was rejected because the codebase has no integration test harness, making it impossible to verify correctness after a wholesale replacement.

The migration proceeds in seven phases (full phase detail, entry/exit conditions, and performance gates in `docs/test-strategy.md`):

| Phase | What is introduced | Monolith role |
|---|---|---|
| 1 | Seam + `IRenderPipeline` | Still active |
| 2 | `IMaskMode` strategies | Still active |
| 3 | `FoveatedSamplingPolicy` | Still active |
| 4 | `VolumeMaterialBinder` | Still active |
| 5 | `VolumeCameraDriver` | Still active |
| 6 | `VolumeTextureManager` + shadow mode | Runs in parallel for frame comparison |
| 7 | `VolumeRenderCoordinator` takes over; `VolumeDataSetRenderer` retired | Removed |

Each phase has a single-file rollback cost (restore the previous seam). Phase 6 shadow mode runs both the old and new texture path for one sprint, comparing output frame-by-frame before the monolith is retired. This is the highest-risk phase due to the 90fps invariant (INV-01).

---

### 4.10 Sub-Team 5 Integration (Feature Rendering Interface Alignment)

**Alignment with IFeatureRenderer Contract v1.0 (2026-05-28)**

Sub-Team 5 (Feature System & Domain Model) published the `IFeatureRenderer` interface contract defining their domain/rendering boundary. Our refactoring follows the same architectural pattern, validating the approach across two independent systems:

| Aspect | IFeatureRenderer (Sub-Team 5) | IVolumeRenderer (Sub-Team 3) | Pattern |
|--------|---------|---------|---------|
| **Domain layer** | `Feature`, `FeatureSet`, `FeatureCatalog` (pure C#) | `VolumeDataSet` (future; pure C#) | Unity-free domain model |
| **Adapter** | `FeatureVisualiser` (MonoBehaviour shell) | `VolumeRendererBehaviour` (≤ 30 lines) | Event wiring + composition root |
| **Renderer boundary** | 5 methods + 1 property | 4 methods (see below) | Minimal interface |
| **Dirty notification** | `FeatureSet.FeatureDirty` event | Texture/Camera/Foveation change events | Event-driven updates |
| **Implementation** | `FeatureSetRenderer` (GPU layer) | `VolumeRenderCoordinator` + four classes | Interface-based delegation |
| **Mutation rule** | Renderer is consumer, never mutator | Renderer is consumer, never mutator | Clear responsibility boundary |

Both systems use **event-driven architecture**: domain state changes fire events that the adapter wires to renderer methods. Neither renderer mutates domain state; rendering is purely a response to domain events.

**The IVolumeRenderer interface** (defined in `refactoring-examples/stubs/IVolumeRenderer.cs`):

```csharp
public interface IVolumeRenderer
{
    void LoadDataSet(VolumeDataSet dataSet);        // Analogous to AddFeature
    void SetTextureAsDirty(int cubeIndex);          // Analogous to SetFeatureAsDirty
    void SetCameraAsDirty();                        // Coordinate frame refresh
    void SetFoveationAsDirty();                      // Sampling rate refresh
    void ApplyColorMap(ColorMapData colorMap);      // Colour mapping (like FeatureColor property)
}
```

This minimal interface ensures that:
1. Domain and rendering concerns are separated by a wire protocol, not a monolithic class
2. Test doubles (null/stub implementations) enable unit testing without GPU context
3. The rendering layer is a passive observer of domain changes, not an active mutator
4. Future rendering pipelines or GPU frameworks can be swapped by implementing a new interface

**Reference documentation:** See `docs/team3/integration/WP3_IFeatureRenderer_Interface_Contract.pdf` (Sub-Team 5 handoff document) for the complete contract, dirty-notification chain, and acceptance criteria.

---

### 4.11 Shader/Asset Organisation Policy

See `docs/shader-asset-policy.md` for the complete shader and asset organisation policy for Unity 6.

---

## 5. Migration & Testing Strategy

See `docs/team3/test-strategy.md` for the detailed phased migration plan, entry/exit conditions, and testing strategy per phase.

---

## 6. Class and Sequence Diagrams

All diagrams are PlantUML source in `diagrams/`:

- **`class-before.puml`** — current `VolumeDataSetRenderer` with 45 couplings annotated.
- **`class-after.puml`** — five-class target architecture with interface boundaries.
- **`architecture.puml`** — component diagram annotating the `IRenderPipeline` boundary and cross-team contracts.
- **`sequence-render-frame.puml`** — 8-step per-frame sequence: Update → camera → foveation → texture → material → mask → pipeline → GPU.

---

## 7. SOLID/GRASP Audit

*Full evidence with code line references: `docs/Codebase Exploration/SOLID_GRASP_Violations.md`*

### 7.1 Violations in Current Code

| ID | Principle | Violation | Severity |
|----|-----------|-----------|----------|
| V-01 | SRP | 9 distinct responsibilities in one class | 🔴 Critical |
| V-04 | OCP | New mask mode requires 4 files, 8 code blocks | 🔴 Critical |
| V-06 | ISP | 152 public members; consumers use only 3–5 | 🔴 Critical |
| V-07 | DIP | `Config.Instance` singleton accessed directly | 🟠 High |
| V-08 | DIP | `FindObjectOfType<VolumeInputController>()` — concrete scene-search | 🟠 High |
| V-10 | DIP | `transform.InverseTransformPoint()` inside domain method | 🔴 Critical |
| V-13 | GRASP Controller | `CropToRegion()` spans validation, data load, material update, outline update | 🔴 Critical |
| V-14 | GRASP Indirection | No abstraction layer between Unity lifecycle hooks and domain logic | 🔴 Critical |
| V-15 | GRASP Prot. Variations | Render pipeline, input provider, data format have zero interface protection | 🔴 Critical |
| V-16 | GRASP Low Coupling | CBO ~45; 12 concrete dependencies, 0 interface dependencies | 🔴 Critical |
| V-17 | GRASP High Cohesion | LCOM ~0.81; unrelated field clusters share one class | 🔴 Critical |

*6 Critical, 8 High, 1 Medium total — see `SOLID_GRASP_Violations.md` for full 17-row table including V-02, V-03, V-05, V-09, V-11, V-12.*

### 7.2 Fixes in Proposed Design

| Violation | Resolved By | How |
|-----------|------------|-----|
| V-01 (SRP) | DD-03 | Four-class split; each class has one reason to change |
| V-04 (OCP) | DD-02 | `IMaskMode` Strategy — new mode = new class only |
| V-06 (ISP) | DD-03 | Four narrow interfaces (≤ 7 members each) |
| V-07/V-08/V-09 (DIP) | DD-03 | All collaborators injected at composition root |
| V-10 (DIP) | DD-01 + DD-03 | `VolumeCameraDriver` takes `Matrix4x4`; `IRenderPipeline` removes `UnityEngine.Rendering.*` from domain |
| V-15 (Prot. Variations) | DD-01 | `IRenderPipeline` is the stable seam around the render pipeline variation point |
| V-16/V-17 | DD-01 + DD-03 | Per-class CBO ≤ 11; per-class LCOM ≤ 0.05 |

### 7.3 Remaining Trade-offs

**T-01 — Coordinator coupling (CBO ~6):** `VolumeRenderCoordinator` couples to four interfaces — acceptable (CBO ≤ 25 for orchestrators per brief). Coupling is to interfaces, not concrete classes.

**T-02 — `MonoBehaviour` lifecycle:** A thin Unity entry-point shell must remain. Mitigation: ≤ 30 lines, zero domain logic, enforced by SonarQube (CC > 2 = build failure).

---

## 8. Sub-team Dependencies

| Dependency | Interface / Struct | Status |
|---|---|---|
| Sub-team 2 (Data I/O) | `RawVolumeData` | ✅ Confirmed 2 June 2026 |
| Sub-team 4 (Interaction / Gaze) | `IGaze` | ✅ Confirmed 2 June 2026 |
| Sub-team 7 (Persistence) | `ISessionPersistenceService` / `VolumeSessionState` | ⏳ Pending sign-off — integration deferred to Sprint 3 |

Sub-team 3 exposes camera state via `CameraFrameState` (available to Sub-team 4 if needed). No upstream dependencies on Sub-teams 1, 5, 6, or 7.

### 8.1 Sub-team 2 — `RawVolumeData` (Data I/O boundary)

`VolumeTextureManager` receives voxel data through a shared `RawVolumeData` struct produced by Sub-team 2's data layer. The confirmed struct:

```csharp
public readonly struct RawVolumeData
{
    public byte[]      Voxels  { get; init; }  // raw bytes; interpret via Format
    public long        XDim    { get; init; }  // long: astronomical cubes exceed int range
    public long        YDim    { get; init; }
    public long        ZDim    { get; init; }
    public DataFormat  Format  { get; init; }  // Float32 (data cube) | Int16 (mask cube)
}
```

Key decisions: `byte[]` rather than `float[]` because the data cube uses `FitsReadSubImageFloat` (Float32) while the mask cube uses `FitsReadSubImageInt16` (Int16) — a single typed array cannot carry both without silent upcast or memory waste. `DataFormat` tells `VolumeTextureManager` whether to call `Texture3D.SetPixelData<float>` or `SetPixelData<short>`. Dimensions are `long` to match `VolumeDataSet`'s existing field types and avoid narrowing conversion.

Constructor: `VolumeTextureManager(VolumeTextureConfig config, RawVolumeData dataCube, RawVolumeData? maskCube)` — nullable mask because not every volume has one. Downsample factors are computed by `VolumeTextureManager` (not pre-computed by Sub-team 2).

### 8.2 Sub-team 4 — `IGaze` (Gaze / Interaction boundary)

`FoveatedSamplingPolicy` consumes gaze data through `IGaze`, a unified interface owned jointly with Sub-team 4. See §4.6 for the full interface definition and DIP rationale. Sub-team 4 implements the concrete class; we implement `StubGazeProvider` for unit tests.

The three members we use: `GazeFocusPoint` (screen-space Vector2, normalised [0,1]), `GazeDirection` (world-space Vector3), `IsTracking` (bool). `IsTracking == false` triggers the uniform-sampling fallback — no null checks, no throws.

### 8.3 Sub-team 7 — `ISessionPersistenceService` (Persistence boundary)

`VolumeRenderCoordinator` exposes `CaptureState()` / `RestoreState()` for session save/load. Each of the four domain classes captures its own slice; the coordinator assembles them into `VolumeSessionState` and passes it to `ISessionPersistenceService.Save()`.

```csharp
// Sub-team 7 owns this interface
public interface ISessionPersistenceService
{
    void              Save(VolumeSessionState state, string path);
    VolumeSessionState Load(string path);
}

// Shared struct — assembly ownership TBD (see docs/integration/meeting-subteam7.md)
public readonly struct VolumeSessionState
{
    public RenderingState  Rendering  { get; init; }  // VolumeMaterialBinder
    public VolumeDataState VolumeData { get; init; }  // VolumeTextureManager
    public SpatialState    Spatial    { get; init; }  // VolumeCameraDriver
    public FoveationState  Foveation  { get; init; }  // FoveatedSamplingPolicy
    public MaskState       Mask       { get; init; }  // VolumeMaterialBinder
}
```

No `UnityEngine` types anywhere in the struct tree — plain floats only, fully serialisable without a Unity context. `VolumeSessionState` must live in a shared contracts assembly (not Sub-team 3's or Sub-team 7's); assembly location is TBD pending Sub-team 7 sign-off. Integration deferred to Sprint 3.

---

## 9. Risks

### 9.1 Performance Overhead of the Abstraction Layer

The primary performance concern is virtual dispatch on the per-frame hot path. `VolumeRenderCoordinator.Update()` calls `IVolumeMaterialBinder.Apply()`, `IVolumeCameraDriver.ComputeFrame()`, and `IFoveatedSamplingPolicy.Evaluate()` every frame at 90 fps. Three virtual calls per frame at 90 fps is negligible in isolation, but the concern is the *chain* of calls each triggers.

Mitigations:
- All four domain classes are `sealed`, enabling IL2CPP to de-virtualise interface dispatch in release builds (no vtable lookup at runtime).
- Per-frame return values use readonly structs (DD-06) — zero heap allocation, zero GC pressure.
- A CI performance gate fails the build if per-frame CPU time on the rendering layer exceeds 2 ms. This gate runs in Unity Play Mode on a reference headset configuration.

The 90 fps invariant (INV-01) is non-negotiable. If the gate fails at any phase during migration, the phase is rolled back — not the gate.

### 9.2 Coordinator Complexity (God Class Recurrence)

`VolumeRenderCoordinator` is at structural risk of becoming a new God Class if domain logic migrates into it over time. This is a known failure mode of Strangler Fig migrations: the coordinator starts thin and accumulates logic as the team takes shortcuts.

Mitigations:
- SonarQube gate: any method in `VolumeRenderCoordinator` with cyclomatic complexity > 2 fails the build. A method that only delegates has CC = 1; any branching is a signal that domain logic is leaking in.
- WMC target ≤ 10 for the coordinator. If WMC approaches this cap, the team reviews whether a new domain class is warranted before adding methods.
- A `CoordinatorWiringTest` integration test verifies that the coordinator constructs, runs one frame, and tears down without errors — it never tests domain correctness, which belongs to the domain class unit tests.

### 9.3 Interface Versioning Risk

Two of the three cross-team contracts are now formally confirmed (`IGaze` and `RawVolumeData`, both 2 June 2026). `ISessionPersistenceService` / `VolumeSessionState` is designed but awaiting Sub-team 7 sign-off. A signature change after either side has coded to it causes rework.

Mitigations:
- `[InterfaceVersion("1.0")]` attribute on all cross-team interfaces. A reflection-based guard in the test suite asserts the attribute is present and that its value matches the expected version string. Any unannounced change to a method signature triggers a build failure on both sides.
- Contract tests (one per cross-team interface) verify that our stub implementations remain consistent with the agreed signatures. These run in edit mode with no Unity context required.
- Interface freeze target: end of Sprint 2 (29 May). Changes after freeze require sign-off from both sub-team leads.

### 9.4 Risk Register

| ID | Risk | Likelihood | Impact | Mitigation |
|----|------|-----------|--------|------------|
| R-01 | ~~`IGaze` not delivered before freeze~~ | ~~Medium~~ | ~~Low~~ | ✅ Resolved 2 June 2026 — `IGaze` confirmed with Sub-team 4 |
| R-02 | ~~`RawVolumeData` struct changes post-implementation~~ | ~~Medium~~ | ~~Medium~~ | ✅ Resolved 2 June 2026 — `RawVolumeData` confirmed with Sub-team 2 |
| R-03 | URP command buffer API changes in Unity 6 LTS | Low | High | `IRenderPipeline` isolates changes to `UrpRenderPipeline` only |
| R-04 | `VolumeRenderState` struct grows beyond 64 bytes | Low | Medium | NFR-08 cap; SonarQube custom rule flags additions |
| R-05 | `VolumeRenderCoordinator` accumulates domain logic | Medium | High | WMC ≤ 10 + CC > 2 = build failure (§8.2) |
| R-06 | Interface semantic drift across sub-teams | Medium | Medium | `[InterfaceVersion]` attribute + contract test suite (§8.3) |
| R-07 | CK targets not achievable within 90 fps | Low | High | CI gate: fail build if per-frame CPU > 2 ms (§8.1) |
| R-08 | Proposal rejected by maintainer panel | Low | Medium | Data-driven case; all trade-offs documented |

---

## 10. Appendices

**Appendix A — Diagram Source Files:** `diagrams/class-before.puml`, `class-after.puml`, `architecture.puml`, `sequence-render-frame.puml`, `vdsr-dependencies.puml`.

**Appendix B — Interface Stubs:** `refactoring-examples/stubs/IRenderPipeline.cs`, `NullRenderPipeline.cs`, `IMaskMode.cs`, `NullMaskMode.cs`, `IGazeProvider.cs`, `IVolumeRenderer.cs`, `IRawVolumeDataSource.cs`.

**Appendix C — Integration References:** Sub-Team 5 IFeatureRenderer contract (v1.0, 2026-05-28): `docs/team3/integration/WP3_IFeatureRenderer_Interface_Contract.pdf`. iDaVIE GitHub: https://github.com/idia-astro/iDaVIE. Brief §4.2 (Architectural Constraints), §6.3 (Deliverables), §9.2 (Deliverable 2). CK thresholds: `docs/requirements.md`.
