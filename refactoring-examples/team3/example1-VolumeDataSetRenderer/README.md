# Refactoring Example 1: VolumeDataSetRenderer Four-Class Split
**Team Alpha | Cache Me If You Can — Sub-team 3**  
*Brief reference: §9.2 Sub-team deliverable 3 — worked refactoring example with CK metrics; Section 6.3 Software Construction — SRP, DIP, ISP, GRASP*

---

## What This Example Shows

`VolumeDataSetRenderer` is a 1,402-line monolith with eight distinct responsibility clusters crammed into one class. This example demonstrates how to break it into four focused classes using the Single Responsibility Principle (SRP), backed by interfaces that satisfy the Dependency Inversion Principle (DIP) and Interface Segregation Principle (ISP).

The four extracted classes:

| New Class | Single Responsibility |
|-----------|----------------------|
| `VolumeMaterialBinder` | Shader keyword management, material property binding, colour-map application |
| `VolumeTextureManager` | 3D texture creation, GPU upload, memory-budget enforcement, cropped-region management |
| `VolumeCameraDriver` | Camera matrix extraction, clip-plane computation, coordinate conversion, projection mode |
| `FoveatedSamplingPolicy` | Per-frame foveation decisions: sample rates, LOD bias, reprojection mask, HMD-absent fallback |

A thin `VolumeRenderCoordinator` (pure C# orchestrator) + `VolumeRendererBehaviour` (MonoBehaviour shell ≤ 30 lines) replace the monolith's Unity lifecycle. The coordinator has no domain logic of its own — it drives the per-frame loop by delegating to the four injected interfaces.

---

## Before: The Monolith

See `before/VolumeDataSetRenderer.cs` — the full original source with 20+ inline violation markers.

### The Problem

The class has eight responsibility clusters in one 1,402-line file:

1. **FITS texture upload** — allocates `Texture3D`, enforces 368 MB budget
2. **Shader / material binding** — 20+ shader property IDs pushed to GPU each frame
3. **Mask modes** — if/else chain on `MaskMode` enum, 3 branches calling `EnableKeyword` directly
4. **Mask I/O** — native plugin calls, file-path logic, UI toasts — all in `SaveMask()`
5. **Coordinate conversion** — `transform.InverseTransformPoint` calls in domain math (DIP violation)
6. **Cursor / outline management** — data lookup, outline update, paint trigger mixed in one method
7. **Foveated rendering** — sample-rate decisions interleaved with material binding
8. **Unity lifecycle** — `Start`, `Update`, `OnDestroy` mixed with domain logic

### Before CK Metrics (Day 2 Baseline — Understand tool)

| Metric | Value | Brief Target | Status |
|--------|-------|-------------|--------|
| WMC | 97 | ≤ 20 (domain) | ❌ |
| DIT | 2 | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | 28 | ≤ 14 (domain) | ❌ |
| RFC | 97 | ≤ 50 | ❌ |
| LCOM | 0.95 | ≤ 0.5 | ❌ |
| NIM | 97 | — | — |
| NIV | 84 | — | — |
| LOC | 1,403 | — | — |

Additional problems:
- Inside a 46-file dependency cycle; propagation cost 39.8%
- `_startFunc()` init coroutine: 185 lines, CC = 28
- `SaveMask()`: 83 lines, CC = 19
- 152-member public API — violates Interface Segregation Principle
- 4× `FindObjectOfType` calls — violates Dependency Inversion Principle
- `Graphics.DrawProceduralNow` in `OnRenderObject()` — URP-incompatible built-in API

---

## After: Four Focused Classes

See `after/` for fully annotated implementations.

### The Solution

Each responsibility cluster is assigned to exactly one class. The coordinator re-composes the four domain classes each frame without containing any logic of its own:

```
VolumeRendererBehaviour  (MonoBehaviour shell — adapter layer only, ≤ 30 lines)
    │
    └── VolumeRenderCoordinator  (pure C# orchestrator — no domain logic)
            │
            ├── IVolumeMaterialBinder      →  VolumeMaterialBinder
            ├── IVolumeTextureManager      →  VolumeTextureManager
            ├── IVolumeCameraDriver        →  VolumeCameraDriver
            └── IFoveatedSamplingPolicy    →  FoveatedSamplingPolicy
```

`VolumeRenderCoordinator` never calls `new` on a concrete domain type — every collaborator is injected at construction time by `VolumeRendererBehaviour.Start()` (the composition root).

### After CK Metrics (Understand tool — measured from worked examples)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | 11 | 1 | 0 | 15 | 11 | 0.69 | ❌ CBO, LCOM |
| `VolumeRendererBehaviour` (MB shell) | 3 | 2 | 0 | 8 | 3 | 0.00 | ✅ all |
| `VolumeMaterialBinder` | 10 | 1 | 0 | 12 | 10 | 0.57 | ❌ LCOM |
| `VolumeTextureManager` | 12 | 1 | 0 | 4 | 12 | 0.67 | ❌ LCOM |
| `VolumeCameraDriver` | 4 | 1 | 0 | 4 | 4 | 0.25 | ✅ all |
| `VolumeCoordinateService` | 3 | 1 | 0 | 3 | 3 | 0.00 | ✅ all |
| `FoveatedSamplingPolicy` | 6 | 1 | 0 | 6 | 6 | 0.33 | ✅ all |

> `VolumeRenderCoordinator` CBO = 15: marginally exceeds domain target (≤ 14); within orchestrator threshold (≤ 25). LCOM values above 0.5 reflect multi-phase lifecycle structure (Initialise / Tick / Dispose) — see `docs/metrics-worksheet.md §4` for justification.

### CK Delta Summary

| Metric | Before (VDSR) | After (worst class) | Improvement |
|--------|--------------|---------------------|-------------|
| WMC | 97 | 12 (`VolumeTextureManager`) | ✅ −85 worst-case; all ≤ 12 |
| CBO | 28 | 15 (`VolumeRenderCoordinator`) | ✅ domain classes ≤ 12; cycle broken |
| RFC | 97 | 12 (`VolumeTextureManager`) | ✅ −85 worst-case; all ≤ 12 |
| LCOM | 0.95 | 0.69 (`VolumeRenderCoordinator`) | ✅ −0.26 worst-case; 0.00 for strategy classes |
| LOC | 1,403 | ~150–250 per class | ✅ ~6–9× smaller per class |

---

## SOLID/GRASP Analysis

| Principle | Violation in Before | Fix in After |
|-----------|--------------------|-----------------------------------------|
| **SRP** | 9 responsibilities in one class | One class per concern; each ~150–200 LOC |
| **OCP** | Projection mode if/else (lines 1218–1221) | `IRenderPipeline.SetPipelineKeyword` — variant hidden behind interface |
| **ISP** | 152-member public API | `IVolumeMaterialBinder` (7 members), `IVolumeTextureManager` (6 members) |
| **DIP** | `FindObjectOfType` (lines 381, 522); `Camera.main`; `Graphics.DrawProceduralNow` | All collaborators injected via constructor; `IRenderPipeline` abstracts SRP API |
| **GRASP Low Coupling** | CBO = 28; in 46-file cycle | Per-class CBO ≤ 12 (domain), 15 (orchestrator); cycle broken by interface boundaries |
| **GRASP High Cohesion** | LCOM = 0.95 — mask, camera, foveation, texture fields mixed | LCOM 0.00–0.25 for focused classes; 0.57–0.69 for lifecycle-structured classes |
| **GRASP Protected Variations** | Global `Shader.EnableKeyword` calls scattered across Update() | All pipeline-keyword calls proxied through `IRenderPipeline` |
| **GRASP Information Expert** | Coordinator holds foveation, texture, and camera knowledge | Each class owns only its relevant data |

---

## Dependency Inversion — The Key Design Decision

The brief §4.2 mandates: *"Domain rendering math must NOT transitively depend on `UnityEngine` or `SteamVR` types."*

`VolumeDataSetRenderer` violated this by calling `transform.InverseTransformPoint` (lines 713, 739, 857) inside methods that also performed mathematical coordinate transforms — making them impossible to unit-test outside a running Unity scene.

The after design creates a **dependency firewall**:

```
VolumeRenderCoordinator  ←── owns ──► VolumeMaterialBinder
                                           uses ──► IRenderPipeline
                                                         ▲
                                            UrpRenderPipeline  (only class that imports
                                            HdrpRenderPipeline   UnityEngine.Rendering.Universal)
```

`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, and `FoveatedSamplingPolicy` never import `UnityEngine.Rendering.Universal` or `UnityEngine.Rendering.HighDefinition`. The SRP adapters are the only classes that cross that boundary.

---

## Test Impact

| Test type | Before | After |
|-----------|--------|-------|
| Unit test domain coordinate math | ❌ Requires live `Transform` in a running scene | ✅ `VolumeCoordinateService` takes `Matrix4x4`; pure NUnit, no Unity context needed |
| Unit test material binding | ❌ Requires `Material` asset + `Shader.EnableKeyword` (GPU) | ✅ Inject `NullRenderPipeline`; verify keyword calls without GPU |
| Unit test texture budget enforcement | ❌ Requires `Texture3D` allocation (GPU) | ✅ Inject `StubVolumeDataSource`; budget logic is pure integer arithmetic |
| Unit test foveated zone selection | ❌ Requires HMD hardware (`SteamVR.Init`) | ✅ Inject `StubGazeProvider`; zone selection is pure float arithmetic |
| Integration test full frame | ❌ Full Unity player required | ⚠️ `VolumeRendererBehaviour` still requires player; coordinator is testable in edit mode with all stubs injected |

Test doubles: `NullRenderPipeline` (`stubs/NullRenderPipeline.cs`), `StubGazeProvider` (`after/FoveatedSamplingPolicy.cs`), `StubCameraDriver` (`after/VolumeCameraDriver.cs`).

---

## Integration with Sub-Team 5 (Feature Rendering)

Sub-Team 5 has published the `IFeatureRenderer` interface contract (v1.0, 2026-05-28) defining how their GPU rendering layer integrates with the domain. Our refactoring follows the same architectural pattern:

| Aspect | Sub-Team 5 (Features) | Sub-Team 3 (Volumes) | Pattern |
|--------|----------------------|----------------------|---------|
| **Domain layer** | `Feature`, `FeatureSet`, `FeatureCatalog` | `VolumeDataSet` (future) | Pure C#, no Unity |
| **Adapter** | `FeatureVisualiser` (MonoBehaviour) | `VolumeRendererBehaviour` | Scene integration + wiring |
| **Renderer boundary** | `IFeatureRenderer` | `IVolumeRenderer` (see below) | Events → GPU updates |
| **Dirty notification** | `FeatureSet.FeatureDirty` event | Texture/Camera change events | Event-driven updates |
| **Implementation** | `FeatureSetRenderer` | `VolumeRenderCoordinator` + four classes | Interface-based delegation |

Both systems use **event-driven architecture**: domain state changes fire events that the adapter wires to renderer methods, eliminating direct coupling between domain and GPU layers. No renderer mutates domain state.

### IVolumeRenderer Interface

Our refactored renderer surfaces the same shape as `IFeatureRenderer` — a minimal interface for domain-to-renderer communication:

```csharp
public interface IVolumeRenderer
{
    // Analogous to IFeatureRenderer.AddFeature
    void LoadDataSet(VolumeDataSet dataSet);
    
    // Analogous to IFeatureRenderer.SetFeatureAsDirty
    void SetTextureAsDirty(int cubeIndex);
    void SetCameraAsDirty();
    void SetFoveationAsDirty();
    
    // Analogous to IFeatureRenderer.FeatureColor
    void ApplyColorMap(ColorMapData colorMap);
}
```

Both interfaces define the rendering boundary: the GPU layer is a **consumer** of domain state, never a **mutator**. See `refactoring-examples/team3/stubs/IVolumeRenderer.cs` for the full definition.

---

## Open Items (blocked on cross-team interface confirmation)

| Item | Blocker |
|------|---------|
| `VolumeTextureManager` uses `VolumeDataSet` directly | Sub-team 2 `RawVolumeData` struct (task S2-CO09) |
| `IGazeProvider` GazeFocusPoint coord space not confirmed | Sub-team 4 interface delivery (task S2-CO08) |

See `PROGRESS.md` blockers table for current status.
