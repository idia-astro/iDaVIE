# Sub-team 3: Rendering Engine — Test Strategy
**Team Alpha | Cache Me If You Can**  
*Brief reference: Section 6.3 Software Testing + Section 9.2 Deliverable 4*

---

## 1. Overview

The central goal of the refactoring is to make rendering logic **independently testable** without a running Unity scene, GPU, or VR hardware. The two worked examples produce 60+ concrete NUnit tests that demonstrate this is achievable today.

Test files:
- `refactoring-examples/team3/tests/Example1_RendererSplitTests.cs` — renderer split
- `refactoring-examples/team3/tests/Example2_MaskModeTests.cs` — mask mode strategy

---

## 2. Test Tiers

### Tier 1 — Pure NUnit (no Unity runtime)

All logic tests that do not touch `Material`, `Renderer`, or player-loop APIs. Safe in headless CI (no Unity Editor licence required).

Covers:
- `FoveatedSamplingPolicy` — `ComputeParameters`, `ComputeMipBias`, `IsGazeAvailable`
- `FoveationParameters.Uniform()` — fallback struct factory
- `FoveatedSamplingConfig.Default` — Inspector-constant regression guard
- `VolumeCoordinateService` — `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes`
- `StubCameraDriver` — `ComputeFrame`, `SetProjectionMode` round-trip
- `ShaderKeyword` properties on all `IMaskMode` implementations

### Tier 2 — Unity Edit Mode (Material required, no player loop)

Tests that call `Material.EnableKeyword` / `Material.GetFloat`. These run in the Unity Test Runner's Edit Mode; they do **not** need a running game or GPU.

Covers:
- `Apply()` mutual-exclusion invariant for `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- `_MaskAlpha` uniform values (1.0 for Apply/Inverse, 0.15 for Isolate)
- `DisabledMaskMode` — Null Object safety (`Apply(mat, null)` never throws, all keywords off, idempotent)
- `StrategySubstitutabilityTests` — LSP round-robin over six mode switches

---

## 3. What the Tests Prove

### Example 1 — VolumeDataSetRenderer Split

The monolithic `VolumeDataSetRenderer.Update()` block (before/ lines 1139–1165) could only be executed by booting Unity with a scene and a connected HMD. After the split:

| Class | Can now be tested because |
|---|---|
| `FoveatedSamplingPolicy` | Depends on `IGazeProvider` interface; `StubGazeProvider` injects any gaze state |
| `VolumeCoordinateService` | Pure `static` math; no `Transform` dependency, no `MonoBehaviour` |
| `StubCameraDriver` | Implements `IVolumeCameraDriver`; test double returns controllable `CameraFrameState` |

Critical test: `ComputeParameters_GazeUnavailable_BothStepCountsEqualMaxSteps` — this directly mirrors the `else`-branch of the before/ code (lines 1163–1165) that previously had no test coverage.

### Example 2 — Mask Mode Strategy

The before/ switch statement was a single block that could only be exercised through a live render. After the Strategy pattern:

- Each `IMaskMode` class is tested in isolation.
- The mutual-exclusion invariant (exactly one keyword active) is enforced per-class and verified by an exhaustive six-step transition sequence.
- `IsolateMaskMode._MaskAlpha = 0.15` has a dedicated regression test — the only active mode that differs from 1.0.
- `DisabledMaskMode` (Null Object) makes the "no mask loaded" state explicit and testable; previously there was an implicit null check inside the God Class.

---

## 4. Stub and Mock Strategy

| Dependency | Before (untestable) | After (stub used in tests) |
|---|---|---|
| Eye-tracking SDK | Direct `SteamVR_Action_Vector2` call | `StubGazeProvider` implements `IGazeProvider` |
| Unity `Camera` | Direct `Camera.main` access | `StubCameraDriver` implements `IVolumeCameraDriver` |
| URP/HDRP adapter | Direct `UniversalRenderPipeline` import | `NullRenderPipeline` / `StubRenderPipeline` implements `IRenderPipeline` |
| Sub-team 2 data | Not injectable | Synthetic `RawVolumeData` passed directly to `VolumeTextureManager` |

Naming convention: `Stub*` for test doubles with configurable state; `Null*` for no-op implementations.

---

## 5. What Is Not Tested Here (and Why)

| Area | Reason |
|---|---|
| GPU shader compilation | Hardware-bound; covered by integration test on reference machine |
| `VolumeMaterialBinder.Tick()` full pipeline | Requires live `Material` bound to a real shader; play-mode scope |
| Frame rate (90 fps invariant) | Play-mode `[UnityTest]`, requires player loop |
| Sub-team 4 gaze SDK | Out of scope; `StubGazeProvider` substitutes until interface is finalised |
| FITS loading | Sub-team 2's responsibility |

---

## 6. Short-Term Impact on the Codebase (Sprint 2–3)

1. **Every new class in the split must have a corresponding test fixture** before its design is considered final. The test file is part of the design deliverable.
2. `FoveatedSamplingConfig.Default` tests serve as a **regression lock** on the Inspector defaults extracted from the before/ code. Any accidental value change will fail immediately.
3. The `ShaderKeyword` tests are a **contract test** between the C# strategy classes and the HLSL `#pragma multi_compile` declarations. A typo in either place fails a test before a render is ever attempted.

---

## 7. Long-Term Impact on the Codebase

1. **New mask modes cost one class and one test fixture.** The before/ switch required modifying `VolumeDataSetRenderer`. The after/ design means adding `SilhouetteMaskMode` or `HeatmapMaskMode` is a zero-change-to-existing-code operation — the test structure mirrors this directly.
2. **IRenderPipeline boundary enables headless CI for all domain logic.** No Unity Editor licence is needed for Tier 1 tests, which covers the entire mathematical core of the renderer. This scales as the team grows.
3. **`VolumeCoordinateService` tests are a safety net for future shader-side coordinate changes.** If `ObjectToNormalisedVolume` or `ExtractFrustumPlanes` is modified during a URP migration, the pure-math tests catch regressions before any render is needed.
4. **`StubCameraDriver` enables future `VolumeCameraDriver` changes to be verified without a live camera.** Camera matrix logic (clip planes, VP matrix, AIP toggle) is tested independently of the Unity Camera component.

---

## 8. CI Integration

- Tier 1 tests: run on every PR via GitHub Actions (headless, no Unity licence required)
- Tier 2 (Edit Mode) tests: run on every PR where a Unity Editor is available on the runner
- Play-mode integration tests: nightly on reference machine
- Coverage gate: ≥ 70% branch/line coverage on domain classes blocks merge from Day 10 (brief §7.2)

---

## 9. Coverage Summary

| Class | Tier | Methods covered |
|---|---|---|
| `FoveatedSamplingPolicy` | 1 | `ComputeParameters`, `ComputeMipBias`, `IsGazeAvailable` |
| `FoveationParameters` | 1 | `Uniform()` |
| `FoveatedSamplingConfig` | 1 | Constructor, all 6 default constants |
| `VolumeCoordinateService` | 1 | `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes` |
| `StubCameraDriver` | 1 | `ComputeFrame`, `SetProjectionMode` |
| `ApplyMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `InverseMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `IsolateMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `DisabledMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` (idempotence, null-safety) |
| `NullMaskMode` | 1 | `ShaderKeyword` (sentinel check) |

---

## 10. Unity 6 / URP Migration Testing

*Full migration sequence and before/after code examples: `docs/team3/migration_plan.md`*
*Architectural rationale and 7-phase Strangler Fig plan: design document §4.9 DD-07 and §2.3*

The design document (§5) delegates the migration entry/exit conditions and per-phase test strategy to this document. This section fulfils that commitment.

### 10.1 Key Architectural Guarantee

Because `IRenderPipeline` (DD-01) is in place before migration begins, **all Category 1 render-loop changes are confined to two new files** — `UrpRenderPipeline.cs` and `VolumeRenderFeature.cs`. The five domain classes (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`, `VolumeRenderCoordinator`) are untouched during the Unity 6 migration. This is a testable invariant: the domain assembly must not import `UnityEngine.Rendering.Universal` at any point during or after migration.

### 10.2 Migration Categories and Test Coverage

The migration plan (`docs/team3/migration_plan.md`) groups the 15 migration steps into four categories. Each maps to a specific test approach:

| Category | Changes | Test approach | Tier |
|---|---|---|---|
| 1 — Render Loop (Critical) | `OnRenderObject` → `ScriptableRenderPass`; `DrawProceduralNow` → `cmd.DrawProcedural`; `AddCommandBuffer` → `renderPassEvent` | Visual verification in Unity 6 player; `NullRenderPipeline` confirms domain classes unchanged | Play-mode |
| 2 — Shader Keywords (High) | `Shader.EnableKeyword` → `material.SetKeyword(LocalKeyword)` | Existing `IMaskMode` Tier 2 edit-mode tests; new `LocalKeyword` round-trip test in `VolumeMaterialBinder` | Tier 1 + 2 |
| 3 — C# API Deprecations (Medium) | `FindObjectOfType` → `FindAnyObjectByType`; `AddComponent` → DI; `Config.Instance` → `IAppConfig` | Tier 1 unit tests on injected `IAppConfig`; compile-time verification for deprecated API removal | Tier 1 |
| 4 — Shader Language (CGPROGRAM → HLSL) | Include files, matrix macros, sampler declarations, precision types, depth texture | Shader compilation gate in CI; visual nearest-neighbour filter check (INV-04); VolumeCoordinateService math tests unchanged | Shader compiler + visual |

### 10.3 Steps Testable Without a GPU

From the 15-step migration sequence in `migration_plan.md`, steps 1–6 are verifiable without launching the Unity 6 player:

| Step | Change | Verification |
|---|---|---|
| 2 | `FindObjectOfType` → `FindAnyObjectByType` (lines 381, 522) | Compile succeeds; no deprecated API warnings |
| 3 | `GetComponentInChildren` + `includeInactive: true` (line 382) | Compile succeeds |
| 4 | `Config.Instance` → injected `IAppConfig` (lines 361, 553, 644) | Tier 1 unit test: `VolumeTextureManager` receives `IAppConfig` stub and reads correct values |
| 5 | `Shader.EnableKeyword` → `material.SetKeyword(LocalKeyword)` (lines 1099, 1103) | Tier 2 edit-mode test: `VolumeMaterialBinder` sets keyword via `LocalKeyword`; `material.IsKeywordEnabled` assertion |
| 6 | Mask-mode `EnableKeyword` → `LocalKeyword` in each `IMaskMode.Apply()` | Existing Tier 2 `IMaskMode` tests already cover mutual-exclusion and `_MaskAlpha` — no changes required if test doubles updated to `LocalKeyword` |

Steps 7–15 require a Unity 6 player, a GPU, and (for step 15) the reference headset configuration.

### 10.4 Entry and Exit Conditions per Migration Phase

The design document §4.9 defines a 7-phase Strangler Fig plan. Each phase has a test gate before it can be considered complete:

| Phase | What is introduced | Entry condition | Exit condition (test gate) |
|---|---|---|---|
| 1 | `IRenderPipeline` seam | Domain assembly compiles with `NullRenderPipeline` | All Tier 1 domain tests pass; no `UnityEngine.Rendering.Universal` import in domain assembly |
| 2 | `IMaskMode` strategies | Phase 1 exit passed | All `IMaskMode` Tier 1 + Tier 2 tests pass; `LocalKeyword` variant verified |
| 3 | `FoveatedSamplingPolicy` | Phase 2 exit passed | `FoveatedSamplingPolicy` Tier 1 tests pass; fallback path (`IsGazeAvailable = false`) verified |
| 4 | `VolumeMaterialBinder` | Phase 3 exit passed | `VolumeMaterialBinder` Tier 2 tests pass; `DepthTextureAvailable` check verified |
| 5 | `VolumeCameraDriver` | Phase 4 exit passed | `VolumeCoordinateService` Tier 1 tests pass; `StubCameraDriver` round-trip verified |
| 6 | `VolumeTextureManager` shadow mode | Phase 5 exit passed | Shadow mode runs for one sprint; frame-comparison output matches between old and new texture path; no 90 fps regression (CI gate: per-frame CPU ≤ 2 ms) |
| 7 | `VolumeRenderCoordinator` takes over; monolith retired | Phase 6 shadow comparison approved | `CoordinatorWiringTest` passes; 90 fps invariant (INV-01) passes on reference machine; no `VolumeDataSetRenderer` references remain in build |

### 10.5 INV-01 and INV-04 Preservation

Two invariants are at specific risk during the Unity 6 migration:

**INV-01 (90 fps minimum):** Phase 6 shadow mode is the highest-risk point. A CI performance gate (play-mode, reference headset) must confirm per-frame CPU time on the rendering layer remains ≤ 2 ms throughout. If the gate fails at any phase, that phase is rolled back — not the gate.

**INV-04 (nearest-neighbour / blocky filtering):** The URP sampler macro change (Category 4, step 14) must preserve `FilterMode.Point`. The migration plan §4.7 specifies `sampler_point_clamp` in the HLSL sampler declaration. A visual verification test (side-by-side screenshot comparison against a reference render) is run at step 14 to confirm blocky filtering is intact. `VolumeTextureManager` must also continue to set `texture.filterMode = FilterMode.Point` on the C# side — this line is marked with `// INV-04 — do not remove` in the after/ code.

### 10.6 What Is Not Tested Here

| Area | Reason |
|---|---|
| HDRP migration | Out of scope for this sprint; `HdrpRenderPipeline` adapter design is complete but testing is deferred |
| Sub-team 2 / Sub-team 4 interface changes during migration | Covered by contract tests in §4 (Stub and Mock Strategy) |
| Unity 6 LTS point-release shader API changes | R-03 in design document risk register; `IRenderPipeline` confines blast radius to `UrpRenderPipeline.cs` |
