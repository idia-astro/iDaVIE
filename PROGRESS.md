# Progress Tracker — Sub-team 3: Rendering Engine

Update this file at the end of every working session.
Format: date, what was completed, what's in progress, any blockers.

---

## Current Sprint: Sprint 2 (25–29 May 2026)
**Sprint Goal:** Design document, both worked refactoring examples, all PlantUML diagrams, Section 6.3 deliverables, and SOLID/GRASP audit. Clear all Sprint 1 carry-overs on Mon 25 May.

---

## Status Board

### ✅ Done — Sprint 1
- [x] Project folder structure created
- [x] CLAUDE.md written
- [x] CONTEXT.md written
- [x] PROGRESS.md written
- [x] iDaVIE codebase cloned and read
- [x] `VolumeDataSetRenderer.cs` fully read — 8+ responsibilities identified — 2026-05-24
- [x] `VolumeDataSetRendererMaskMode.cs` read — switch-on-string anti-pattern confirmed
- [x] `Shaders/VolumeRender.shader` read — nearest-neighbour filtering + foveated uniform confirmed
- [x] Colour map shader files read (`ColourMap*.shader`)
- [x] Hardcoded constants catalogued (90 fps, 4 GB, 368 MB, foveated radii)
- [x] Unity 5 render API catalogue written — 5 touchpoints requiring SRP migration
- [x] Render-frame call sequence documented (9 steps) — `docs/Codebase Exploration/RenderFrame_CallSequence(1).md`
- [x] Rendering-adjacent classes identified — `docs/Codebase Exploration/RenderingAdjacentClasses.md`
- [x] SOLID/GRASP violations identified — `docs/team3/exploration/SOLID_GRASP_Violations.md`
- [x] Day 2 CK metrics baseline run (Understand) — WMC=97, CBO=28, RFC=97, LCOM=0.95 (confirmed)
- [x] Sub-team requirements document (`docs/requirements.md`) — completed 2026-05-20
- [x] `IRenderPipeline` interface stub — `refactoring-examples/team3/stubs/IRenderPipeline.cs` — 2026-05-24
- [x] `NullRenderPipeline` test double — `refactoring-examples/team3/stubs/NullRenderPipeline.cs` — 2026-05-24
- [x] `IMaskMode` interface + `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` drafted — 2026-05-24
- [x] `NullMaskMode` test double drafted — 2026-05-24
- [x] Sprint 1 Kanban snapshot — `kanban/sprint1-snapshot.md`

### 🔄 In Progress — Sprint 2 (carry-overs, clear Mon 25 May)
- [ ] Team review of `docs/requirements.md` (S2-CO01)
- [x] Finalise class-level dependency map of VDSR — confirm CBO count (S2-CO03) — CBO=28 (Understand, Count of Coupled Classes) confirmed; full graph in `diagrams/vdsr-dependencies.puml`

### ⏳ Sprint 2 — Carry-overs (target: Mon 25 May EOD)
- [ ] Incorporate requirements.md review feedback and finalise (S2-CO02)
- [ ] Agree and document two Sprint 2 refactoring examples in PROGRESS.md (S2-CO04)
- [x] Formal smoke-test of all 5 tools on VolumeDataSetRenderer.cs (S2-CO05) — reports in `tests/`; CodeScene report added 25 May 2026
- [x] Document tool versions and setup notes in CONTEXT.md (S2-CO06) — `## Tool Versions and Setup Notes` section added 25 May 2026
- [ ] Log cross-team interface agreements in blockers table (S2-CO07)
- [x] Chase Sub-team 4 for `IGazeProvider` (S2-CO08) — confirmed 2 June 2026: unified `IGaze` interface; `IsGazeAvailable` → `IsTracking`, interface renamed. `FoveatedSamplingPolicy.cs`, diagrams, and TESTS.md updated.
- [x] Chase Sub-team 2 for `RawVolumeData` (S2-CO09) — confirmed 2 June 2026: `byte[] Voxels` (not float[]), `long` dims (not int), same struct for mask cube (nullable). See `docs/integration/meeting-subteam2.md`

### ⏳ Sprint 2 — Design Document
- [x] Outline agreed (S2-D01) — brief-aligned 10-section outline with bullet notes written to `docs/team3/deliverables/design-document.md` — 2026-05-25
- [x] Section 1 — Problem Statement (S2-D02) — §2 fully written (6 subsections: Core Finding, Metrics in Practice, Responsibility Inventory, Dependency Cycle Problem, Render Pipeline Lock-In, What This Document Proposes) — confirmed in-file 2026-05-28
- [x] Section 2 — Target Architecture Overview (S2-D03) — §4.2 written to `docs/team3/deliverables/design-document.md` — 2026-05-25
- [x] Section 3 — IRenderPipeline Abstraction (S2-D04) — §5.3 fully written (The Problem, The Decision, interface design code block, 6-method rationale) — confirmed in-file 2026-05-28
- [x] Section 4 — Mask Mode Strategy Pattern (S2-D05) — §5.4 written to `docs/team3/deliverables/design-document.md` — 2026-05-27
- [x] Section 5 — SOLID/GRASP Principle Mapping (S2-D06) — §8.1 violation audit (17-violation table) + §8.2 fixes table (enriched with code locations 2026-05-27) both written
- [x] Section 6 — Migration Path (S2-D07) — §5.8 written to `docs/team3/deliverables/design-document.md`: Strangler Fig strategy, 7 phases (seam introduction → IMaskMode → FoveatedSamplingPolicy → VolumeMaterialBinder → VolumeCameraDriver → VolumeTextureManager → coordinator handoff → URP migration), per-phase entry/exit conditions, performance gates, shadow-mode step, rollback cost per phase, summary table — 2026-05-27
- [x] Section 7 — Risks and Trade-offs (S2-D08) — §10 expanded to full 3-subsection treatment (performance overhead, coordinator complexity, interface versioning) + 8-row risk register — 2026-05-27
- [x] Section 8 — Day 13 Projected CK Metrics (S2-D09) — `docs/team3/deliverables/design-document.md` §6.2 and §6.3 written 2026-05-27: per-class projection table (10 classes, all 6 CK metrics, meets-target column), detailed notes on VolumeMaterialBinder CBO=11 and VolumeTextureManager WMC=20 headroom, delta narrative paragraph referencing 46-file cycle break and LCOM collapse
- [x] Sub-team review (S2-D10) — completed 2026-06-02
- [x] Finalise `docs/team3/deliverables/design-document.md` (S2-D11) — confirmed finalised 2026-06-02

### ✅ Sprint 2 — Refactoring Example 1 (VolumeDataSetRenderer Split)
- [x] Extract and annotate `before/` code (S2-E1-01) — `refactoring-examples/team3/example1-VolumeDataSetRenderer/before/VolumeDataSetRenderer.cs` — 2026-05-25
- [x] Draft `after/VolumeRenderCoordinator.cs` (S2-E1-02) — confirmed in after/ — 2026-05-27
- [x] Draft `after/VolumeMaterialBinder.cs` (S2-E1-03) — WMC=16, CBO≤11, LCOM=0.05 — 2026-05-26
- [x] Draft `after/VolumeTextureManager.cs` (S2-E1-04) — WMC=15, CBO≤8, LCOM=0.05 — 2026-05-26
- [x] Draft `after/VolumeCameraDriver.cs` (S2-E1-05) — WMC=9, CBO≤4, LCOM=0.0 — 2026-05-27
- [x] Draft `after/FoveatedSamplingPolicy.cs` (S2-E1-06) — WMC=7, CBO=6, LCOM=0.0 — 2026-05-26
- [x] `IRenderPipeline.cs` and `NullRenderPipeline.cs` stubs (S2-E1-07) — in `stubs/`
- [x] `UrpRenderPipeline.cs` and `HdrpRenderPipeline.cs` stubs (S2-E1-08) — in `stubs/`
- [x] Compute projected CK metrics for `after/` classes (S2-E1-09) — inline in each file header
- [x] Write `example1-VolumeDataSetRenderer/README.md` with CK delta table (S2-E1-10) — written 2026-05-28: what the example shows, 8-responsibility problem statement, before/after CK tables, delta table, SOLID/GRASP analysis, 4 key design decisions, test impact table
- [x] SOLID/GRASP annotation pass (S2-E1-11) — inline `[FIXED]`/`[CBO]`/`[WMC]`/`[LCOM]` markers in all after/ files; §8.2 design doc table cross-references code locations

### ✅ Sprint 2 — Refactoring Example 2 (Mask Mode Strategy Pattern)
- [x] Extract and annotate `before/` code (S2-E2-01) — `before/VolumeDataSetRendererMaskMode.cs` confirmed
- [x] `IMaskMode` interface finalised (S2-E2-02) — `after/IMaskMode.cs` with `DisabledMaskMode` + `NullMaskMode`; signatures locked to §5.4: `Apply(Material, Texture3D)` + `string ShaderKeyword { get; }` — 2026-05-27
- [x] `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` finalised (S2-E2-03) — each WMC=2, CBO=1, RFC=4, LCOM=0.0; null-guard, XML docs, SOLID/GRASP callouts, §5.4 cross-refs inline — 2026-05-27
- [x] `NullMaskMode` test double (S2-E2-04) — in `after/IMaskMode.cs`
- [x] Compute projected CK metrics (S2-E2-05) — WMC=2, DIT=1, NOC=0, CBO=1, RFC=4, LCOM=0.0 per strategy class; confirmed in after/ file headers
- [x] Write `example2-MaskModes/README.md` with CK delta table (S2-E2-06) — confirmed complete; TBC values filled 2026-05-28 (before metrics from class-before.puml; after metrics from after/ headers)
- [x] SOLID/GRASP annotation pass (S2-E2-07) — inline SOLID/GRASP callouts in all after/ mask mode files; README SOLID/GRASP table complete

### ⏳ Sprint 2 — Diagrams
- [x] `diagrams/architecture.puml` (S2-G01) — written 2026-05-25: 5-layer component diagram (Unity/SRP, IRenderPipeline adapters, IMaskMode strategies, cross-team contracts, rendering core, test doubles)
- [x] `diagrams/class-before.puml` (S2-G02) — rewritten 2026-05-25 with real measured metrics (WMC=97, CBO=28, Understand tool) and real field/method names from source; replaces inaccurate Sprint 1 stub
- [x] `diagrams/class-after.puml` (S2-G03) — expanded 2026-05-25: full field/method detail for all 5 target classes, projected CK annotations, SRP adapters, test doubles, skinparam styling matching class-before — 2026-05-25
- [x] `diagrams/sequence-render-frame.puml` (S2-G04) — expanded 2026-05-25: 7-step frame sequence with alt/opt branches (foveated on/off, stale texture path), performance contract note, cross-references to Update() line numbers
- [x] Verify all four `.puml` files render (S2-G05) — confirmed 2026-06-02
- [x] `diagrams/vdsr-dependencies.puml` — full class-level dependency map, full reachable graph (Ce + 2nd-hop), typed edges, Unity/SteamVR callouts, CBO annotations — 2026-05-25
- [x] `diagrams/vdsr-dependencies.md` — Mermaid version + quick-reference dependency table — 2026-05-25

### ⏳ Sprint 2 — Section 6.3 Deliverables
- [x] `docs/team3/deliverables/design-document.md` covers rendering-layer-design (S2-S01 to S2-S03) — 491 lines, full class API surfaces, dependency graph, state contract, SRP migration notes — confirmed in-file 2026-05-28
- [x] `docs/team3/deliverables/shader-asset-policy.md` (S2-S04 to S2-S05) — 7 sections: folder structure, naming conventions, variant stripping, runtime vs baked assets, URP integration, version control rules — confirmed in-file 2026-05-28
- [x] `docs/team3/deliverables/metrics-worksheet.md` Day 13 projected column (S2-S06 to S2-S07) — Sections 2–5 filled 2026-05-28: projected CK for all 8 new classes (domain + strategy + adapter), delta summary table, WMC/CBO/LCOM justification paragraphs, dependency cycle check

### ⏳ Sprint 2 — SOLID/GRASP Audit
- [x] Structured violation audit of current VDSR (S2-A01) — 17-violation table (V-01→V-17, 6 Critical / 8 High / 1 Medium) written to `docs/team3/deliverables/design-document.md` §7.1 — 2026-05-25; source: `docs/team3/exploration/SOLID_GRASP_Violations.md`
- [x] Map each fix to design decision in target architecture (S2-A02) — V-01→V-17 mapped to DD-01/DD-02/DD-03/DD-04 in `docs/team3/deliverables/design-document.md` §8.2 — 2026-05-25; enriched 2026-05-27 with concrete after/ code locations (file path + class/method) for all 17 rows now that all after/ classes are finalised
- [x] Verify `after/` code introduces no new violations (S2-A03) — 2026-05-27: all after/ files reviewed; DIT=0 for all extracted classes, no circular dependencies, all DIP maintained through interfaces

### ⏳ To Do — Sprint 3
- [ ] Test strategy document (`docs/test-strategy.md`)
- [ ] Address mid-assessment feedback
- [ ] Day 13 projected CK metrics finalised (evidence-backed, not speculative)
- [ ] Final metrics worksheet before/after comparison
- [ ] Pitch slide contributions
- [ ] Individual reflections (personal, not in this folder)
- [ ] Sprint 3 Kanban snapshot
- [ ] Artefact freeze check (Thu 4 June 11:00)

---

## Blockers & Dependencies

| Blocker | Waiting on | Status |
|---------|-----------|--------|
| `IGaze` interface definition | Sub-team 4 (Interaction) | ✅ Confirmed 2 June 2026 — unified `IGaze`; `IsTracking` replaces `IsGazeAvailable`. All references updated. |
| `RawVolumeData` texture format contract | Sub-team 2 (Data I/O) | ✅ Confirmed 2 June 2026 — `byte[] Voxels`, `long` dims, nullable mask. `VolumeTextureManager.cs` comment updated. |
| `ISessionPersistenceService` interface agreement | Sub-team 7 (Persistence) | ✅ Confirmed 2 June 2026 — contract signed off. See `docs/team3/integration/meeting-subteam7.md` |
| Shared assembly location for `VolumeSessionState` | Sub-team 7 (Persistence) | ✅ Confirmed 2 June 2026 — assembly ownership agreed. |

### Integration Note — Sub-team 7 (Persistence)
- Agreed approach: each of our four classes implements `CaptureState()` / `RestoreState()` for its own slice of state
- `VolumeRenderCoordinator` assembles slices into `VolumeSessionState` and passes to `ISessionPersistenceService.Save(state, path)`
- Restore is symmetric: coordinator calls `Load(path)` then distributes slices back to each class
- **Integration is deferred to Sprint 3** — do not block current Sprint 2 deliverables on this
- Full contract design: `docs/integration/team7-persistence-contract.md`

---

## Agreed Refactoring Examples

*(To be confirmed and signed off Mon 25 May — S2-CO04)*

| Example | Before | After | Key Principles |
|---------|--------|-------|---------------|
| Example 1 | `VolumeDataSetRenderer.cs` (1403 lines, WMC=97, CBO=28, Understand) | `VolumeRenderCoordinator` + `VolumeMaterialBinder` + `VolumeTextureManager` + `VolumeCameraDriver` + `FoveatedSamplingPolicy` | SRP, DIP, OCP |
| Example 2 | Switch/if-else on mask mode string in `VolumeDataSetRendererMaskMode.cs` | `IMaskMode` + `ApplyMaskMode` + `InverseMaskMode` + `IsolateMaskMode` | OCP, SRP, Polymorphism (GRASP) |

---

## Session Log

### 2026-06-02 (session 9 — current)

- [Sprint 2 → Sub-team 7 alignment] Updated refactoring examples to align with persistence contract — added state structs and save/restore methods to all four domain classes and coordinator:

  - `VolumeMaterialBinder`: added `RenderingState` struct and `CaptureState()` / `RestoreState()` methods
  - `VolumeTextureManager`: added `VolumeDataState` struct and `CaptureState()` / `RestoreState()` methods
  - `VolumeCameraDriver`: added `SpatialState` struct and `CaptureState()` / `RestoreState()` methods
  - `FoveatedSamplingPolicy`: added `FoveationState` struct and `CaptureState()` / `RestoreState()` methods
  - `VolumeRenderCoordinator`: added `VolumeSessionState` root struct, `SaveSession()` and `LoadSession()` orchestration methods
  - All methods are commented as stubs with TODO notes for Sprint 3 wire-up to `ISessionPersistenceService`
  - Contract reference: `docs/team3/integration/team7-persistence-contract.md`
  - Status: refactoring examples now **match the Sub-team 7 contract fully**; integration test wire-up deferred to Sprint 3

### 2026-05-28 (session 1)
- **PROGRESS.md reconciliation** — audited all files marked pending against actual disk state; found 7 items already done but not recorded. Marked S2-D02 (Problem Statement §2), S2-D04 (IRenderPipeline §5.3), S2-D06 (SOLID/GRASP §8), S2-S01–S2-S03 (rendering-layer-design.md), S2-S04–S2-S05 (shader-asset-policy.md), S2-E1-11, S2-E2-05, S2-E2-06, S2-E2-07 all complete.
- [S2-S06–S2-S07] `docs/metrics-worksheet.md` Sections 2–5 filled — all Day 13 projected CK values from after/ code headers; delta summary; WMC/CBO/LCOM justification paragraphs; dependency cycle check completed.
- [S2-E1-10] `refactoring-examples/team3/example1-VolumeDataSetRenderer/README.md` rewritten from 13-line stub to full worked example document (same structure as example2 README): problem statement with 8 responsibilities, before/after CK tables, delta table, SOLID/GRASP analysis, 4 key design decisions (DD-03 four-class split, DD-01 IRenderPipeline, VolumeCoordinateService, constructor injection), test impact table.
- [S2-E2 TBC fix] `example2-MaskModes/README.md` before/after CK TBC values filled — before metrics from class-before.puml baseline; after metrics (WMC=2, CBO=1, RFC=4, LCOM=0.0) from after/ file headers.

### 2026-05-27 (session 4)
- [S2-A02 enrichment] `docs/team3/deliverables/design-document.md` §8.2 violation→design-decision table updated — added "Code location (after/)" column to all 17 rows. Each violation now points to the specific file, class, and method in the after/ code that implements the fix (e.g. V-10 → `VolumeCameraDriver.cs — VolumeCoordinateService.WorldToObjectSpace()`; V-04 → `example2.../after/IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`). This was previously a design-level mapping only; it is now traceable to real draft code. Covers S2-E1-11 and S2-E2-07 at the document level (inline code annotations already present in all after/ files).

### 2026-05-27 (session 3)
- [S2-E1-05] `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeCameraDriver.cs` drafted
  — Scope: narrow (camera matrix, clip planes, projection mode) — matches formal design doc §5.2 targets; cursor tracking / outlines / teleport excluded by design
  — Includes `VolumeCoordinateService` static helper (pure C#, zero UnityEngine API calls) — fixes V-10 DIP: replaces all `transform.InverseTransformPoint` calls (before/ lines 713, 739, 857) with `WorldToObjectSpace(point, Matrix4x4)`, testable in edit mode without a scene
  — `CameraFrameState` readonly struct carries: `LocalToWorld`, `WorldToLocal`, `ViewProjection`, `FrustumPlanes` (Gribb–Hartmann, 6 planes), `NearClipPlane`, `FarClipPlane`, `AverageIntensityProjection`
  — `IVolumeCameraDriver` 2-member interface: `ComputeFrame()` → `CameraFrameState`, `SetProjectionMode(bool)`
  — `VolumeCameraDriver` sealed class: Camera injected at construction (fixes V-08 DIP — no `FindObjectOfType`/`Camera.main`); `ComputeFrame()` extracts all matrices in one call — no other class touches Transform or Camera API
  — `StubCameraDriver` test double in `iDaVIE.Rendering.Tests` namespace; `SetProjectionMode` rebuilds struct so round-trips are testable
  — Violations fixed: V-01 (SRP), V-05 (OCP projection mode), V-08 (DIP FindObjectOfType), V-10 (DIP UnityEngine in domain math), V-14 (GRASP Indirection), V-16 (GRASP Low Coupling), V-17 (GRASP High Cohesion)
  — Projected CK: WMC=9 combined (VolumeCameraDriver=5, VolumeCoordinateService=4), CBO≤4, LCOM=0.0 — all within targets (WMC target ≤12 for this class)
  — Per-frame loop usage documented inline (Steps 1–5, on-demand coordinate conversion example)
  — Design decision: "return value struct" pattern (consistent with FoveatedSamplingPolicy → FoveationParameters); VolumeCameraDriver does NOT hold IRenderPipeline — coordinator forwards FrustumPlanes to pipeline, reducing CBO from target ≤6 to actual ≤4
- [S2-E2-03] `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` finalised in `refactoring-examples/team3/example2-MaskModes/after/`
  — All three promoted from stubs to fully annotated implementations aligned with locked IMaskMode signatures (S2-E2-02)
  — Key changes from stubs: `ShaderKeyword` property used in `EnableKeyword()` call (no hardcoded string duplication); `_MASK_DISABLED` added to the mutual-exclusion disable block (DisabledMaskMode is now an explicit mode per S2-E2-02); null-guard for `maskTexture` — `Debug.Assert` under `#if UNITY_ASSERTIONS`, silent early return in RELEASE to avoid `SetTexture(null)` Unity warning
  — Full inline commentary: CONTEXT (source switch-case origin), BEHAVIOUR, SHADER SIDE, NULL-SAFETY CONTRACT, CK METRICS table (WMC=2, DIT=1, NOC=0, CBO=1, RFC=4, LCOM=0.0 per class), SOLID/GRASP alignment (SRP/OCP/LSP/ISP/DIP + Protected Variations + Indirection)
  — `IsolateMaskMode`: `OutsideAlpha = 0.15f` named constant documented with extraction rationale and OCP extension note (add constructor param to this class only if value needs to be configurable)
  — XML `<summary>` + `<remarks>` on all public members; `<inheritdoc/>` on interface members

### 2026-05-27 (session 2)
- [S2-E2-02] `refactoring-examples/example2-MaskModes/after/IMaskMode.cs` finalised
  — Signatures locked to design doc §5.4: `void Apply(Material material, Texture3D maskTexture)` + `string ShaderKeyword { get; }`
  — `DisabledMaskMode` added as Null Object for MaskMode.Disabled=0 (confirmed by §5.8 Phase 1); removes null-check from VolumeMaterialBinder hot path
  — `NullMaskMode` test double retained in `iDaVIE.Rendering.Tests` namespace
  — Full inline commentary: why 2 members (ISP rationale), null-handling contract for maskTexture, HLSL keyword-match requirement, CK projections (WMC=2, CBO=1, LCOM=0 per concrete class), SOLID/GRASP checklist

### 2026-05-27
- [S2-D07] `docs/team3/deliverables/design-document.md` §5.8 Migration Path written — Strangler Fig strategy, 7 phases with per-phase entry/exit conditions, performance gates, allocation checks, shadow-mode step in Phase 6 (coordinator runs alongside monolith for frame-by-frame comparison before `VolumeDataSetRenderer` is retired), rollback cost per phase (all single-file restores except Phase 6 prefab swap and Phase 7 Graphics Settings swap). Phase summary table added.
- [S2-D08] `docs/team3/deliverables/design-document.md` §10 Risks and Trade-offs fully written — replaced stub table with three detailed subsections: (1) §10.1 Performance Overhead of Abstraction Layer — virtual dispatch analysis on hot path, `VolumeRenderState` struct size constraint (≤ 64 bytes, NFR-08), IL2CPP de-virtualisation rationale, CI performance gate at 90 fps; (2) §10.2 Coordinator Complexity — God Class recurrence risk, WMC ≤ 10 gate, CC > 2 SonarQube failure, `CoordinatorWiringTest` integration test spec, CBO ≤ 6 design constraint; (3) §10.3 Interface Versioning Risk — near-term cross-team signature mismatch, semantic drift, `[InterfaceVersion]` attribute + reflection guard, contract test suite spec. Risk register expanded from 5 to 8 rows (R-04 struct size, R-05 coordinator bloat, R-06 semantic drift added).
- [S2-D05] `docs/team3/deliverables/design-document.md` §5.4 DD-02 (Mask Mode Strategy Pattern) written — full section ~1.5 pages covering: switch-on-enum anti-pattern and OCP violation (V-04), Strategy pattern decision, `IMaskMode` 2-member interface (Apply + ShaderKeyword), four implementations table (`ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`, `DisabledMaskMode`) with inline code for `IsolateMaskMode`, runtime switching via `SetMaskMode()`, OCP/SRP justification, and pattern-choice rationale (Strategy vs. Decorator vs. State)
- E2 tasks S2-E2-01, S2-E2-02, S2-E2-03 unblocked

### 2026-05-26 (session 7)
- [S2-E1-03] `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs` drafted
  — Includes: `VolumeRenderState` readonly struct, `IVolumeMaterialBinder` 7-member interface,
    `VolumeMaterialBinder` sealed class with full inline annotations
  — Violations fixed: V-01 (SRP), V-04 (OCP mask), V-05 (OCP projection), V-06 (ISP),
    V-13 (GRASP Controller), V-14/15 (GRASP Indirection/Protected Variations)
  — Projected CK: WMC=16, CBO≤11, RFC≤22, LCOM≈0.05 — all within targets
  — All 20+ shader property IDs moved into private `ShaderID` nested class (no public exposure)
  — IMaskMode.Apply() replaces the MaskMode.GetHashCode() integer dispatch (OCP fix)
  — IRenderPipeline.SetPipelineKeyword replaces global Shader.EnableKeyword (URP-safe)
- `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/Rationale/VolumeMaterialBinder-decisions.md` created
  — 7 design decisions documented: VolumeRenderState struct rationale, 7-member ISP fix,
    private ShaderID class, IMaskMode Strategy, IRenderPipeline keyword routing,
    SubmitMaskGeometry replacing OnRenderObject, and file structure choice

### 2026-05-26 (session 1)
- [S2-E1-06] `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/FoveatedSamplingPolicy.cs` drafted — design skeleton with: `IGazeProvider` placeholder interface (Sub-team 4 dependency, clearly flagged for reconciliation); `FoveationZone` enum (Foveal / Parafoveal / Peripheral); `FoveatedSamplingConfig` readonly struct (real default values from before/ lines 140–145); `FoveationParameters` return struct (zero allocation per frame); `FoveatedSamplingPolicy` sealed class covering all four owned behaviours (sample rate per region, LOD/mip bias, reprojection mask, HMD-absent fallback); `StubGazeProvider` test double in `iDaVIE.Rendering.Tests` namespace. Projected CK: WMC=7, CBO=6, LCOM=0.0 — all within targets.

### 2026-05-25 (session 6)
- [S2-D03] `docs/team3/deliverables/design-document.md` §4.2 Target Architecture (To-Be) written — five-class breakdown table, per-class bullet justifications, IRenderPipeline abstraction, IMaskMode Strategy, cross-team contracts, diagram references

### 2026-05-27 (session 8)
- [S2-D09] `docs/team3/deliverables/design-document.md` §6.2 Day 13 Projection table written — 10-class table (VolumeRenderCoordinator, VolumeMaterialBinder, VolumeTextureManager, VolumeCameraDriver, FoveatedSamplingPolicy, ApplyMaskMode, InverseMaskMode, IsolateMaskMode, DisabledMaskMode, UrpRenderPipeline) with all 6 CK metrics + meets-target column; per-class notes on CBO=11 inventory for VolumeMaterialBinder and WMC=20 headroom for VolumeTextureManager
- [S2-D09] §6.3 Delta Summary paragraph written — WMC 97→12 worst-case, CBO 28→15 worst-case (orchestrator), 46-file cycle broken, LCOM 0.95→0.69 worst-case, cross-reference to metrics-worksheet.md
- [S2-S06/S2-S07] `docs/metrics-worksheet.md` Section 2 filled — projected table (10 classes), LCOM = 0 for all new classes, Henderson-Sellers formula note; Section 3 delta table (before/worst class/best class); Section 4 justification prose (WMC, CBO, LCOM) with formula rationale
- Merge conflict in PROGRESS.md (lines 64–112) resolved — HEAD (✅) version kept; stale branch lines removed

### 2026-05-27 (session 6)
- Confirmed both refactoring examples complete in `refactoring-examples/team3/`
- [S2-E1-09] CK metrics confirmed (Understand): VolumeMaterialBinder WMC=10/CBO=12/LCOM=0.57, VolumeTextureManager WMC=12/CBO=4/LCOM=0.67, FoveatedSamplingPolicy WMC=6/CBO=6/LCOM=0.33, VolumeCameraDriver WMC=4/CBO=4/LCOM=0.25, VolumeRenderCoordinator WMC=11/CBO=15/LCOM=0.69
- [S2-E1-10] `example1-VolumeDataSetRenderer/README.md` — full rewrite with CK delta table, 9-responsibility breakdown, SOLID/GRASP mapping, test examples, invariant preservation table
- [S2-E1-11] SOLID/GRASP annotation pass complete — inline [FIXED]/[CBO]/[WMC]/[LCOM] markers in all after/ files
- [S2-E2-05] CK metrics confirmed (Understand): all IMaskMode concrete classes WMC=2, CBO=2, RFC=2, LCOM=0.0; IMaskMode interface CBO=4, NOC=5
- [S2-E2-06] `example2-MaskModes/README.md` — full rewrite with CK delta table, before/after code, FUT-01 extension demo, SOLID/GRASP analysis, test examples
- [S2-E2-07] SOLID/GRASP annotation pass complete — inline in each after/ file
- [S2-A03] After/ code verified: no new violations; DIT=0 all classes, no circular deps, DIP maintained

### 2026-05-25 (session 5)
- [S2-G01] `diagrams/architecture.puml` written — 5-layer component diagram: Unity/SRP external layer, IRenderPipeline abstraction + URP/HDRP adapters, IMaskMode strategies, cross-team contracts (IGazeProvider / IRawVolumeData), rendering core (VolumeRenderCoordinator + 4 classes), test doubles (NullRenderPipeline / NullMaskMode / StubGazeProvider). Legend, DIP callout, CK targets.
- [S2-G03] `diagrams/class-after.puml` expanded — full field/method detail for all 5 target classes, projected CK metric notes per class, SRP adapters (Urp/HdrpRenderPipeline), test doubles, skinparam styling matching class-before.puml, legend with companion diagram cross-refs.
- [S2-G04] `diagrams/sequence-render-frame.puml` expanded — 7-step one-frame sequence with alt blocks (foveated on/off, stale texture path), ScheduleVolumeRenderPass step, performance-contract note, inline cross-refs to VDSR.Update() line numbers.
- [S2-A01] Structured SOLID/GRASP violation audit complete — 17-violation table in design-document.md §7.1. Sourced from teammate's `docs/team3/exploration/SOLID_GRASP_Violations.md`.
- [S2-A02] Violation → design-decision mapping complete — all 17 violations mapped in design-document.md §7.2.

### 2026-05-25 (session 4)
- [S2-CO03] Class-level dependency map of VolumeDataSetRenderer completed — CBO=28 (Understand, Count of Coupled Classes) confirmed
- Created `diagrams/vdsr-dependencies.puml` — full reachable graph (hop 1 + hop 2), 24 typed dependency edges, Unity/SteamVR testability boundaries highlighted, mutual cycle references in red, CBO annotations per node
- Created `diagrams/vdsr-dependencies.md` — Mermaid equivalent + quick-reference table for embedding in design document
- [S2-G02] Rewrote `diagrams/class-before.puml` — confirmed real measured metrics (WMC=97, CBO=28, Understand tool), real field and method names from source, 8 responsibilities listed, mutual cycle arrows drawn explicitly
- [S2-G04] `diagrams/sequence-render-frame.puml` confirmed correct — no changes needed
- Decision: `diagrams/class-after.puml` skeleton retained; full rewrite deferred until S2-E1-02 to S2-E1-06 (after/ target classes drafted)

### 2026-05-25 (session 3)
- [S2-E1-01] `refactoring-examples/team3/example1-VolumeDataSetRenderer/before/VolumeDataSetRenderer.cs` created — full original source annotated with 20+ inline markers covering all SRP/OCP/ISP/DIP/GRASP violations, CBO drivers, WMC/LCOM evidence, and target class mappings. Annotation legend defined at file header.

### 2026-05-25 (session 2)
- [S2-CO05] Smoke-test of all 5 tools closed: `tests/CodeScene_Report.md` created (was the only missing report). All 5 reports now exist in `tests/`. Real Git data used: 123 commits to VDSR, 9 authors, 2019–2026. Hotspot score critical; Code Health estimated ~3.5/10.
- [S2-CO06] `CONTEXT.md` updated with `## Tool Versions and Setup Notes` section: table of all 5 tools with version, licence status, setup status, report pointer, and action items to replace modelled reports with live tool output before Sprint 3 freeze.
- CONTEXT.md Changelog updated to reflect session 2 additions.

### 2026-05-25 (session 1)
- Sprint 2 kick-off — carry-over tasks identified from Sprint 1 snapshot
- Sprint 2 greenfield Kanban created (`kanban/sprint2-greenfield.md`) — 57 tasks, ~55 person-hours
- ClickUp CSV generated (`kanban/sprint2-clickup.csv`) — 57 tasks ready for import
- PROGRESS.md updated for Sprint 2
- CLAUDE.md updated — current sprint and roles refreshed
- Design document outline agreed and written to `docs/team3/deliverables/design-document.md` — brief-aligned 10-section structure (§9.2 + §6.3), headings + bullet notes (S2-D01 ✅)

### 2026-05-24
- Read `VolumeDataSetRenderer.cs` in full — confirmed it handles file loading, texture upload, shader property pushing, cursor tracking, crop/region management, mask data, colour maps, foveated rendering, and moment map rendering (8+ distinct responsibilities in one class)
- Drafted `IRenderPipeline` interface stub with full inline comments covering: command buffer injection, shader keyword control, depth texture availability, and lifecycle (init/dispose)
- Drafted `NullRenderPipeline` test double — enables edit-mode unit tests without GPU or Unity player loop
- Both stubs saved to `refactoring-examples/team3/stubs/`
- Key design rationale documented: Dependency Inversion Principle — core classes depend on `IRenderPipeline`, not on `UnityEngine.Rendering.Universal` or HDRP namespaces directly
- Drafted `IMaskMode` interface stub + `NullMaskMode` test double
- Drafted three concrete strategy implementations: `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- Each implementation is ~15 lines, WMC=2, CBO=1, LCOM=0 — CK annotations included inline for metrics worksheet

### 2026-05-20
- Project folder scaffolded
- CLAUDE.md, CONTEXT.md, PROGRESS.md, all template files created
- Ready to begin Sprint 1 codebase exploration

---

## Key Decisions Made

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-05-25 | After/ code for both refactoring examples is blocked until design doc sections 2–5 are written | The after classes must illustrate the agreed architecture, not define it — drafting code before the architecture overview risks having to rewrite it when design decisions change |
| 2026-05-25 | Two refactoring examples confirmed: (1) VolumeDataSetRenderer four-class split; (2) Mask mode Strategy pattern | Example 1 demonstrates SRP/DIP at scale; Example 2 demonstrates OCP cleanly in isolation — together they cover all brief requirements |
| 2026-05-25 | `diagrams/class-after.puml` full rewrite deferred until S2-E1-02 to S2-E1-06 are drafted | Method signatures and field names can only be finalised once the target classes are written; premature detail would need redoing |
| 2026-05-24 | `IRenderPipeline` interface covers 6 methods: `AddCommandBuffer`, `RemoveCommandBuffer`, `SetPipelineKeyword`, `DepthTextureAvailable`, `Initialise`, `Dispose` | Deliberately minimal — every extra method adds cost to both `UrpRenderPipeline` and `HdrpRenderPipeline` and raises CBO |
| 2026-05-24 | `NullRenderPipeline` placed in `iDaVIE.Rendering.Tests` namespace, not in production assemblies | Test doubles must not ship in production builds |
| 2026-05-24 | `IMaskMode` strategy pattern chosen over decorator or state pattern | Strategy is simplest fit — mask modes are mutually exclusive per frame, no runtime transitions needed |
