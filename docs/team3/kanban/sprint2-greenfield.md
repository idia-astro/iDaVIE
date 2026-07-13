# Kanban Board — Sprint 2 (Greenfield Draft)
## Sub-team 3: Rendering Engine ("Cache Me If You Can")
**Sprint Dates:** Mon 25 May – Fri 29 May 2026
**Sprint Goal:** Produce the full design document, both worked refactoring examples with CK deltas, all PlantUML diagrams, and the three Section 6.3 deliverables — while clearing all Sprint 1 carry-overs on day 1.
**Scrum Master:** Damien | **Tech Lead:** Cathal | **PO Liaison:** Chris | **Quality Champion:** Ciallian

> **Note:** This is a greenfield draft — all tasks start in Backlog as if the sprint just started.
> Move cards across columns as work progresses each day.

---

## Columns

| 📋 Backlog | 🔄 In Progress | 👀 In Review | ✅ Done |
|-----------|---------------|-------------|--------|
| All tasks listed below | — | — | — |

---

## ⚠️ Sprint 1 Carry-Overs (Clear These First — Mon 25 May)

These must be resolved before new Sprint 2 design work begins. Target: all closed by Mon 25 May EOD.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-CO01 | Complete team review of `docs/requirements.md` against CLAUDE.md invariants and brief §9.2 | Chris + All | 🔴 High | 1 h |
| S2-CO02 | Incorporate review feedback and finalise `docs/requirements.md` | Chris | 🔴 High | 0.5 h |
| S2-CO03 | Finalise class-level dependency map of `VolumeDataSetRenderer` — confirm CBO count for metrics worksheet | Damien | 🔴 High | 1 h |
| S2-CO04 | Formally agree and document the two Sprint 2 refactoring examples in `PROGRESS.md` | All | 🔴 High | 0.5 h |
| S2-CO05 | Formal smoke-test of all five tools (Understand, SonarQube, NDepend, CodeScene, DV8) against `VolumeDataSetRenderer.cs` | Ciallian | 🟡 Medium | 1 h |
| S2-CO06 | Document tool versions, access URLs/licences, and setup gotchas in `CONTEXT.md` | Ciallian | 🟡 Medium | 0.5 h |
| S2-CO07 | Log all cross-team interface agreements and open questions in `PROGRESS.md` blockers table | Damien | 🟡 Medium | 0.5 h |
| S2-CO08 | Chase Sub-team 4 (Interaction) for `IGazeProvider` interface — escalate to full-team coordinator if no response | Damien | 🔴 High | 0.5 h |
| S2-CO09 | Chase Sub-team 2 (Data I/O) for `RawVolumeData` struct definition — escalate if no response | Damien | 🔴 High | 0.5 h |

---

## 📐 Design Document

Primary deliverable: `docs/design-document.md` (5–10 pages, brief §9.2). Owned by Tech Lead (Cathal) with section contributions from the full sub-team.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-D01 | Draft `docs/design-document.md` outline — agree section headings and page budget with sub-team before writing begins | Cathal | 🔴 High | 0.5 h |
| S2-D02 | Write Section 1 — Problem Statement: quantitative refactoring case using Day 2 CK baseline (WMC 74, CBO 31, LCOM 0.81, RFC 89) | Cathal | 🔴 High | 1 h |
| S2-D03 | Write Section 2 — Target Architecture Overview: four-class split, `VolumeRenderCoordinator` role, responsibility table, and rationale | Cathal | 🔴 High | 2 h |
| S2-D04 | Write Section 3 — `IRenderPipeline` Abstraction: interface design, method rationale, URP/HDRP concrete implementations, DIP justification | Cathal | 🔴 High | 1.5 h |
| S2-D05 | Write Section 4 — Mask Mode Strategy Pattern: `IMaskMode` interface, three implementations, OCP/SRP justification, CK projection | Cathal | 🔴 High | 1 h |
| S2-D06 | Write Section 5 — SOLID/GRASP Principle Mapping: one paragraph per principle showing how the refactored design satisfies it | Ciallian | 🔴 High | 1.5 h |
| S2-D07 | Write Section 6 — Migration Path: how a maintainer would move from the monolith to the split in stages without breaking production | Chris | 🟡 Medium | 1.5 h |
| S2-D08 | Write Section 7 — Risks and Trade-offs: performance overhead of abstraction layer, coordinator complexity, interface versioning risk | Chris | 🟡 Medium | 1 h |
| S2-D09 | Write Section 8 — Day 13 Projected CK Metrics: target numbers per new class with evidence-backed reasoning (not speculative) | Ciallian | 🔴 High | 1 h |
| S2-D10 | Sub-team review of full `docs/design-document.md` draft — check for gaps, contradictions with CLAUDE.md, and SOLID violations | All | 🔴 High | 1.5 h |
| S2-D11 | Incorporate review feedback and finalise `docs/design-document.md` | Cathal | 🔴 High | 1 h |

---

## 🔬 Refactoring Example 1: VolumeDataSetRenderer → Four-Class Split

Deliverable: `refactoring-examples/example1-VolumeDataSetRenderer/` with `before/`, `after/`, and a `README.md` containing CK deltas.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-E1-01 | Extract and annotate the `before/` code — copy the relevant regions of `VolumeDataSetRenderer.cs` that will be split, with inline comments identifying which new class each block belongs to | Damien | 🔴 High | 2 h |
| S2-E1-02 | Draft `after/VolumeRenderCoordinator.cs` — thin orchestrator; wires up the four focused classes; no domain logic | Cathal | 🔴 High | 1 h |
| S2-E1-03 | Draft `after/VolumeMaterialBinder.cs` — owns all shader property pushes and material state; WMC target ≤ 20 | Damien | 🔴 High | 1.5 h |
| S2-E1-04 | Draft `after/VolumeTextureManager.cs` — owns 3D texture upload, caching, and eviction; WMC target ≤ 20 | Damien | 🔴 High | 1.5 h |
| S2-E1-05 | Draft `after/VolumeCameraDriver.cs` — owns camera matrix calculations and clip plane updates | Cathal | 🟡 Medium | 1 h |
| S2-E1-06 | Draft `after/FoveatedSamplingPolicy.cs` — depends on `IGazeProvider` stub; owns sampling rate decisions | Cathal | 🟡 Medium | 1 h |
| S2-E1-07 | Refine `IRenderPipeline.cs` and `NullRenderPipeline.cs` stubs (already in `refactoring-examples/stubs/`) — add any methods revealed as needed during E1-02 to E1-05 | Cathal | 🟡 Medium | 0.5 h |
| S2-E1-08 | Draft `after/UrpRenderPipeline.cs` and `after/HdrpRenderPipeline.cs` concrete stubs — method signatures only, with `// TODO: implement` bodies | Damien | 🟡 Medium | 1 h |
| S2-E1-09 | Compute projected CK metrics for each `after/` class — WMC, DIT, NOC, CBO, RFC, LCOM — compare to Day 2 baseline | Ciallian | 🔴 High | 1.5 h |
| S2-E1-10 | Write `refactoring-examples/example1-VolumeDataSetRenderer/README.md` — before/after summary, CK delta table, SOLID principles demonstrated | Ciallian | 🔴 High | 1 h |
| S2-E1-11 | SOLID/GRASP annotation pass — add inline comments to `after/` code citing the specific principle each design choice satisfies | Ciallian | 🟡 Medium | 1 h |

---

## 🎭 Refactoring Example 2: Mask Mode Switch → Strategy Pattern

Deliverable: `refactoring-examples/example2-MaskModes/` with `before/`, `after/`, and a `README.md` containing CK deltas.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-E2-01 | Extract and annotate the `before/` code — copy the switch/if-else block from `VolumeDataSetRendererMaskMode.cs` with inline comments marking the OCP violation | Damien | 🔴 High | 1 h |
| S2-E2-02 | Finalise `after/IMaskMode.cs` interface (stub exists in `refactoring-examples/stubs/`) — confirm method signatures are complete | Damien | 🔴 High | 0.5 h |
| S2-E2-03 | Finalise `after/ApplyMaskMode.cs`, `after/InverseMaskMode.cs`, `after/IsolateMaskMode.cs` (stubs exist) — flesh out logic from the original switch cases | Damien | 🔴 High | 1.5 h |
| S2-E2-04 | Draft `after/NullMaskMode.cs` test double — implements `IMaskMode` as a no-op for edit-mode unit testing | Ciallian | 🟡 Medium | 0.5 h |
| S2-E2-05 | Compute projected CK metrics for each `after/` class — confirm WMC ≤ 5, CBO ≤ 2, LCOM = 0 per implementation | Ciallian | 🔴 High | 0.5 h |
| S2-E2-06 | Write `refactoring-examples/example2-MaskModes/README.md` — before/after summary, CK delta table, OCP/SRP demonstration | Ciallian | 🔴 High | 0.5 h |
| S2-E2-07 | SOLID/GRASP annotation pass on `after/` code — inline citations of OCP, SRP, and Polymorphism (GRASP) | Ciallian | 🟡 Medium | 0.5 h |

---

## 🗂️ PlantUML / Mermaid Diagrams

All diagrams must be in PlantUML or Mermaid source format. No PNG/SVG-only files committed.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-G01 | Write `diagrams/architecture.puml` — high-level component view of the refactored rendering layer: four focused classes, `IRenderPipeline`, `IMaskMode`, and their relationships to `VolumeRenderCoordinator` | Cathal | 🔴 High | 1.5 h |
| S2-G02 | Write `diagrams/class-before.puml` — current `VolumeDataSetRenderer` class with all 31 external couplings shown; highlights God Class smell | Damien | 🔴 High | 1 h |
| S2-G03 | Write `diagrams/class-after.puml` — post-refactoring class diagram: four focused classes, interface dependencies, test doubles; contrasts with class-before | Cathal | 🔴 High | 1.5 h |
| S2-G04 | Write `diagrams/sequence-render-frame.puml` — sequence diagram of one full rendered frame through the new architecture, from `VolumeRenderCoordinator` down to `IRenderPipeline.Execute()` | Damien | 🟡 Medium | 1.5 h |
| S2-G05 | Verify all four `.puml` files render correctly (PlantUML CLI or online renderer) — fix any syntax errors | Ciallian | 🟡 Medium | 0.5 h |

---

## 📁 Section 6.3 Deliverables

Three documents required by brief §6.3, beyond the main design document.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-S01 | Draft `docs/rendering-layer-design.md` — detailed rendering layer design: class responsibilities, API surface, lifecycle (init/update/dispose), and thread safety notes | Chris | 🔴 High | 2 h |
| S2-S02 | Team review of `docs/rendering-layer-design.md` — check consistency with `docs/design-document.md` and worked examples | All | 🔴 High | 0.5 h |
| S2-S03 | Finalise `docs/rendering-layer-design.md` incorporating review feedback | Chris | 🔴 High | 0.5 h |
| S2-S04 | Draft `docs/shader-asset-policy.md` — shader/asset organisation policy for Unity 6: naming conventions, folder layout, SRP compatibility rules, shader keyword management | Chris | 🟡 Medium | 1.5 h |
| S2-S05 | Review `docs/shader-asset-policy.md` against Unity 6 URP shader guidelines (check Cathal / Damien's codebase notes) | Damien | 🟡 Medium | 0.5 h |
| S2-S06 | Populate `docs/metrics-worksheet.md` Day 13 projected column — one projected value per CK metric per new class, with methodology note explaining how each was derived | Ciallian | 🔴 High | 1.5 h |
| S2-S07 | Write the before/after comparison summary paragraph in `docs/metrics-worksheet.md` — is the projected improvement quantitatively convincing? | Ciallian | 🟡 Medium | 0.5 h |

---

## 🔍 SOLID/GRASP Audit

A standalone audit to be cross-referenced in the design document and worked examples.

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-A01 | Produce a structured SOLID violation audit of the current `VolumeDataSetRenderer` — one row per violation with file+line evidence, principle violated, and proposed fix | Ciallian | 🔴 High | 1.5 h |
| S2-A02 | Map each proposed fix from S2-A01 to a specific design decision in the target architecture — closes the loop between "smell identified" and "smell resolved" | Cathal | 🟡 Medium | 1 h |
| S2-A03 | Verify the `after/` code in both examples introduces no new SOLID violations | Ciallian | 🔴 High | 0.5 h |

---

## ⚙️ Process

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S2-P01 | Daily standup each morning — 15 min; log in `standup/standup-log.md` | Damien | 🔴 High | 1.25 h |
| S2-P02 | Update `PROGRESS.md` at end of each working session — what changed, what's in flight, any new blockers | Damien | 🔴 High | 1 h |
| S2-P03 | Mid-sprint status review (Wed 27 May) — check all deliverables on track, reprioritise if needed | All | 🔴 High | 1 h |
| S2-P04 | Sprint 2 Kanban snapshot (Fri 29 May EOD) — this board captured as `kanban/sprint2-snapshot.md` | Damien | 🔴 High | 0.5 h |

---

## Sprint 2 Task Summary

| Category | Tasks | Est. Hours |
|----------|-------|-----------|
| ⚠️ Sprint 1 Carry-Overs | 9 | ~5.5 h |
| 📐 Design Document | 11 | ~13 h |
| 🔬 Refactoring Example 1 | 11 | ~12 h |
| 🎭 Refactoring Example 2 | 7 | ~5 h |
| 🗂️ Diagrams | 5 | ~6 h |
| 📁 Section 6.3 Docs | 7 | ~7 h |
| 🔍 SOLID/GRASP Audit | 3 | ~3 h |
| ⚙️ Process | 4 | ~3.75 h |
| **Total** | **57** | **~55.25 h** |

*~55 hours across 4 people over 5 days ≈ 2.75 h/person/day. Matches Sprint 1 capacity.*

---

## Task Dependencies

```
S2-CO03 (finalise VDSR dependency map)
  └─► S2-E1-01 (before/ extraction needs complete coupling picture)
  └─► docs/metrics-worksheet.md (CBO baseline must be confirmed)

S2-CO04 (agree two examples)
  └─► All S2-E1-* and S2-E2-* tasks

S2-E1-01 (before/ code extracted)
  └─► S2-E1-02 – S2-E1-08 (after/ classes designed)
        └─► S2-E1-09 (projected CK metrics need after/ code to exist)
              └─► S2-E1-10 (README needs CK delta table)
              └─► S2-S06 (Day 13 projected column uses same numbers)

S2-E2-01 (before/ code extracted)
  └─► S2-E2-02 – S2-E2-03 (after/ code finalised)
        └─► S2-E2-05 (projected metrics need after/ code)
              └─► S2-E2-06 (README needs CK delta table)

S2-D01 (outline agreed)
  └─► S2-D02 – S2-D09 (all sections)
        └─► S2-D10 (team review needs full draft)
              └─► S2-D11 (finalise after review)

S2-G02 (class-before diagram)     ← depends on S2-CO03 (31 couplings confirmed)
S2-G03 (class-after diagram)      ← depends on S2-E1-02 to S2-E1-06 (after/ classes defined)
S2-G04 (sequence diagram)         ← depends on S2-E1-02 (coordinator structure known)

S2-A01 (SOLID violation audit)
  └─► S2-D06 (SOLID/GRASP section in design doc)
  └─► S2-A02 (fix mapping)
        └─► S2-A03 (verify no new violations in after/ code)

S2-S01 (rendering-layer-design.md)   ← depends on S2-D03 (architecture overview settled)
S2-S06 (metrics worksheet Day 13)    ← depends on S2-E1-09 and S2-E2-05 (projected numbers)
```

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Sub-teams 2 and 4 still non-responsive; real interfaces never arrive | High | Medium | Stubs are already in place — design proceeds with `IGazeProvider` stub and `RawVolumeData` stub; document clearly as a known assumption |
| Design document grows beyond 10 pages | Medium | Low | Enforce page budget at S2-D01 outline stage; §6.3 docs absorb overflow content |
| After/ code for Example 1 reveals unforeseen coupling | Medium | Medium | Damien flags immediately; Cathal adjusts interface design before the metrics worksheet is populated |
| CK projections not evidence-backed (just wishful thinking) | Medium | High | Ciallian must derive each projected number from the after/ code structure, not from targets in CLAUDE.md; method documented in worksheet |
| Mid-sprint review (Wed 27 May) reveals design document significantly behind | Low | High | Diagrams and §6.3 docs are lower priority than design doc + examples; drop S2-G04 and S2-S04/S2-S05 to Sprint 3 if needed |
| Sprint 2 capacity overrun due to carry-over volume | Low | Medium | Carry-overs targeted for Mon 25 May EOD — if S2-CO05/CO06/CO07 are not done by Mon EOD, Damien reprioritises or reassigns Tue morning |

---

## Definition of Done — Sprint 2

A task is **Done** when:
- The output exists as a committed file, recorded number, or documented decision
- At least one other sub-team member has seen or reviewed the output
- It would survive being read cold by someone who wasn't in the room

Sprint 2 is **Done** when all of the following are true:
- [ ] All Sprint 1 carry-overs (S2-CO01 to S2-CO09) are closed or formally parked
- [ ] `docs/design-document.md` is finalised and reviewed (5–10 pages)
- [ ] Both `refactoring-examples/example1-*/README.md` and `example2-*/README.md` are complete with CK delta tables
- [ ] All four `.puml` diagram files exist and render without errors
- [ ] `docs/rendering-layer-design.md` and `docs/shader-asset-policy.md` are written
- [ ] `docs/metrics-worksheet.md` has a populated Day 13 projected column
- [ ] `docs/design-document.md` includes a SOLID/GRASP section backed by S2-A01 audit
- [ ] `standup/standup-log.md` has all 5 Sprint 2 standups recorded
- [ ] `PROGRESS.md` is up to date

---

## Sprint 2 Retrospective

*(To be completed at sprint review — Fri 29 May EOD)*

**What went well:**

**What was harder than expected:**

**Carry-over tasks:**

**Action items for Sprint 3:**
