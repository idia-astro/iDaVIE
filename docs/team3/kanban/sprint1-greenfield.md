# Kanban Board — Sprint 1 (Greenfield Draft)
## Sub-team 3: Rendering Engine ("Cache Me If You Can")
**Sprint Dates:** Mon 18 May – Fri 22 May 2026
**Sprint Goal:** Understand the rendering codebase from scratch, get all metric tools running, establish the Day 2 CK baseline, and deliver the requirements document.
**Scrum Master:** Cathal | **Tech Lead:** Damien | **PO Liaison:** Ciallian | **Quality Champion:** Chris

> **Note:** This is a greenfield draft — all tasks start in Backlog as if the brief just landed.
> Move cards across columns as work progresses each day.

---

## Columns

| 📋 Backlog | 🔄 In Progress | 👀 In Review | ✅ Done |
|-----------|---------------|-------------|--------|
| All tasks listed below | — | — | — |

---

## 📋 Backlog

---

### 🟦 Kickoff & Setup

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-K01 | Read and annotate the full project brief — note every constraint, deliverable, and deadline that affects our sub-team | All | 🔴 High | 2 h |
| S1-K02 | Agree the Sprint 1 goal and Definition of Done as a sub-team | Cathal | 🔴 High | 1 h |
| S1-K03 | Formally assign Sprint 1 roles (Scrum Master, Tech Lead, PO Liaison, Quality Champion) and confirm responsibilities | Cathal | 🔴 High | 0.5 h |
| S1-K04 | Create sub-team GitHub repository (or folder) for all deliverables | Cathal | 🔴 High | 1 h |
| S1-K05 | Set up project folder structure (`docs/`, `refactoring-examples/`, `diagrams/`, `kanban/`, `standup/`, `tests/`) | Cathal | 🔴 High | 0.5 h |
| S1-K06 | Write project charter / `CLAUDE.md` — scope, constraints, sprint plan, roles, key decisions log | Cathal | 🔴 High | 1.5 h |
| S1-K07 | Write `CONTEXT.md` — running technical context file, updated each session | Cathal | 🟡 Medium | 1 h |
| S1-K08 | Write `PROGRESS.md` — running status log, to be updated after every working session | Cathal | 🟡 Medium | 0.5 h |

---

### 🔧 Tooling Setup

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-T01 | Install and configure **Understand** (Scitools) — verify it can analyse the iDaVIE C# solution and produce CK metrics | Chris | 🔴 High | 2 h |
| S1-T02 | Set up **SonarQube Cloud** — link iDaVIE repository, run first scan, verify code smell and complexity reports are populated | Chris | 🔴 High | 2 h |
| S1-T03 | Set up **NDepend** — import iDaVIE `.sln`, run architecture rules, verify dependency graph renders correctly | Chris | 🔴 High | 2 h |
| S1-T04 | Install **CodeScene** — connect iDaVIE Git history, verify hotspot and churn analysis are available | Chris | 🟡 Medium | 1.5 h |
| S1-T05 | Install **DV8** — import iDaVIE dependency data, verify Dependency Structure Matrix (DSM) can be generated | Chris | 🟡 Medium | 1.5 h |
| S1-T06 | Smoke-test all tools against `VolumeDataSetRenderer.cs` — confirm each one produces usable output before relying on it for metrics | Chris | 🔴 High | 1 h |
| S1-T07 | Document tool versions, access URLs/licences, and any setup gotchas in `CONTEXT.md` so all team members can reproduce results | Chris | 🟡 Medium | 0.5 h |

---

### 🔍 Codebase Exploration

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-C01 | Clone the iDaVIE repository locally — verify build and confirm Unity version | Damien | 🔴 High | 1 h |
| S1-C02 | Read `VolumeDataSetRenderer.cs` in full — annotate every distinct responsibility (material, texture, camera, foveation) | Damien | 🔴 High | 3 h |
| S1-C03 | Read `VolumeDataSetRendererMaskMode.cs` — document how mask modes are currently implemented (expected: switch/if-else anti-pattern) | Damien | 🔴 High | 1 h |
| S1-C04 | Read `Shaders/VolumeRender.shader` — understand the ray-march loop, sampling rate uniform, and nearest-neighbour filtering | Damien | 🔴 High | 2 h |
| S1-C05 | Read all `ColourMap*.shader` files — understand how the colour transfer function is applied per ray step | Damien | 🟡 Medium | 1.5 h |
| S1-C06 | Map all class-level dependencies of `VolumeDataSetRenderer` — list every external type it imports or calls | Damien | 🔴 High | 2 h |
| S1-C07 | List every hardcoded constant in the rendering layer — value, location (file + line), and what it governs | Damien | 🔴 High | 1 h |
| S1-C08 | Catalogue all Unity 5 / Built-in render pipeline APIs currently used — each one will need a SRP replacement in the refactored design | Damien | 🟡 Medium | 1.5 h |
| S1-C09 | Write up the full render-frame call sequence end-to-end — this becomes the input for the Sprint 2 sequence diagram | Damien | 🟡 Medium | 1 h |
| S1-C10 | Identify any other rendering-adjacent classes to include in the metrics baseline (e.g. ColourMapController, camera helpers) | Damien | 🟡 Medium | 1 h |

---

### 📊 CK Metrics Baseline

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-M01 | Run **Understand** on `VolumeDataSetRenderer.cs` — record WMC, DIT, NOC, CBO, RFC, LCOM | Chris | 🔴 High | 1 h |
| S1-M02 | Run **Understand** on any other rendering-adjacent classes identified in S1-C10 | Chris | 🟡 Medium | 1 h |
| S1-M03 | Run **SonarQube** on rendering files — record cyclomatic complexity and total code smell count | Chris | 🔴 High | 0.5 h |
| S1-M04 | Run **CodeScene** — identify top hotspots and churn rate for the rendering layer | Chris | 🟡 Medium | 0.5 h |
| S1-M05 | Run **NDepend** — check for circular dependencies and flag any existing architecture violations | Chris | 🟡 Medium | 1 h |
| S1-M06 | Run **DV8** — generate Dependency Structure Matrix for the rendering layer and note any dependency tangles | Chris | 🟡 Medium | 1 h |
| S1-M07 | Collate all raw numbers into `docs/metrics-worksheet.md` — Day 2 baseline column only | Chris | 🔴 High | 1.5 h |
| S1-M08 | Write a one-paragraph summary of what the baseline metrics reveal — is the refactoring case quantitatively strong? | Chris | 🟡 Medium | 0.5 h |

---

### 📝 Requirements Document

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-R01 | Draft Section 1 of `docs/requirements.md` — current system invariants (90 fps, 4 GB texture limit, 368 MB budget, blocky filtering, foveated rendering) | Ciallian | 🔴 High | 1 h |
| S1-R02 | Draft Section 2 — future requirements the design must not preclude (iso-contours, multi-cube, time-series) | Ciallian | 🔴 High | 1 h |
| S1-R03 | Draft Section 3 — interface contracts needed from other sub-teams (`IGazeProvider` from Sub-team 4, `RawVolumeData` from Sub-team 2) | Ciallian | 🔴 High | 1 h |
| S1-R04 | Draft Section 4 — architectural constraints from brief §4.2 (no SOLID violations, no circular deps, no Unity types in domain layer, etc.) | Ciallian | 🟡 Medium | 1 h |
| S1-R05 | Team review of full `docs/requirements.md` draft — check against project charter and brief §9.2 | All | 🔴 High | 1 h |
| S1-R06 | Incorporate review feedback and finalise `docs/requirements.md` | Ciallian | 🔴 High | 0.5 h |

---

### 🤝 Cross-team Coordination

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-X01 | Contact Sub-team 2 (Data I/O) — request the `RawVolumeData` struct definition (width, height, depth, voxel array, texture format) | Ciallian | 🔴 High | 0.5 h |
| S1-X02 | Contact Sub-team 4 (Interaction) — request the `IGazeProvider` interface definition (gaze direction, focus point, availability flag) | Ciallian | 🔴 High | 0.5 h |
| S1-X03 | Attend any whole-team kickoff or cross-team sync — note any decisions that affect our scope or interfaces | Ciallian | 🟡 Medium | 1 h |
| S1-X04 | If `IGazeProvider` not received by Wed 20 May — define a working stub in `CONTEXT.md` so design is not blocked | Ciallian | 🟡 Medium | 0.5 h |
| S1-X05 | If `RawVolumeData` not received by Wed 20 May — define a working stub in `CONTEXT.md` so design is not blocked | Ciallian | 🟡 Medium | 0.5 h |
| S1-X06 | Log all cross-team interface agreements and open questions in the `PROGRESS.md` blockers table | Cathal | 🟡 Medium | 0.5 h |

---

### 🏗️ Early Design Groundwork

| ID | Task | Owner | Priority | Est. |
|----|------|-------|----------|------|
| S1-D01 | Sketch the four-class split: `VolumeRenderCoordinator`, `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` — rough notes only | Damien | 🟡 Medium | 1.5 h |
| S1-D02 | Draft `IRenderPipeline` interface stub (4 methods: `CreateVolumeDescriptor`, `SetShaderKeyword`, `ScheduleRenderPass`, `GetActiveCameraData`) | Damien | 🟡 Medium | 1 h |
| S1-D03 | Draft `IMaskMode` interface stub and three empty implementations (`ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`) | Damien | 🟡 Medium | 1 h |
| S1-D04 | Identify and agree which two refactoring examples to use in Sprint 2 — document reasoning in `PROGRESS.md` | All | 🔴 High | 1 h |
| S1-D05 | List every SOLID and GRASP principle violated in the current code — with a specific line of evidence from codebase exploration for each | Damien | 🟡 Medium | 1 h |

---

## Sprint 1 Task Summary

| Category | Tasks | Est. Hours |
|----------|-------|-----------|
| 🟦 Kickoff & Setup | 8 | ~8 h |
| 🔧 Tooling Setup | 7 | ~11 h |
| 🔍 Codebase Exploration | 10 | ~15 h |
| 📊 CK Metrics Baseline | 8 | ~7 h |
| 📝 Requirements Document | 6 | ~5.5 h |
| 🤝 Cross-team Coordination | 6 | ~3.5 h |
| 🏗️ Early Design Groundwork | 5 | ~5.5 h |
| **Total** | **50** | **~55 h** |

*~55 hours across 4 people over 5 days ≈ 2.75 h/person/day focused work. Achievable.*

---

## Task Dependencies

```
S1-K04/K05 (repo + folder structure)
  └─► All other tasks (nothing can be committed until this exists)

S1-C01 (clone repo)
  └─► S1-C02 – S1-C10 (all codebase exploration)
        └─► S1-D01 – S1-D05 (design groundwork informed by exploration)
        └─► S1-M01, S1-M02 (Understand needs the codebase)

S1-T01 (Understand installed)   └─► S1-M01, S1-M02
S1-T02 (SonarQube set up)       └─► S1-M03
S1-T03 (NDepend set up)         └─► S1-M05
S1-T04 (CodeScene set up)       └─► S1-M04
S1-T05 (DV8 set up)             └─► S1-M06

S1-M01 – S1-M06 (all raw metrics)
  └─► S1-M07 (metrics worksheet — needs all numbers first)
        └─► S1-M08 (summary paragraph)

S1-R01 – S1-R04 (requirements sections)
  └─► S1-R05 (team review)
        └─► S1-R06 (finalise)

S1-X01, S1-X02 (contact sub-teams Mon 18 May)
  └─► S1-X04, S1-X05 (stubs if no reply by Wed 20 May)
```

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Sub-team 2 doesn't deliver `RawVolumeData` this sprint | Medium | Medium | S1-X05: stub by Wed 20 May |
| Sub-team 4 doesn't deliver `IGazeProvider` this sprint | Medium | Medium | S1-X04: stub by Wed 20 May |
| Tool licensing issue with Understand or NDepend | Low | High | Flag to Ciallian (PO Liaison) on Day 1 — blocks all metrics work |
| `VolumeDataSetRenderer.cs` too large to fully read in one session | Medium | Medium | Split: Damien owns responsibilities, Chris does a parallel dependency/coupling pass |
| Unity version mismatch prevents local build | Low | Medium | Damien flags immediately on S1-C01; read-only codebase exploration still possible |

---

## Definition of Done — Sprint 1

A task is **Done** when:
- The output exists as a committed file, recorded number, or documented decision
- At least one other sub-team member has seen or reviewed the output
- It would survive being read cold by someone who wasn't in the room

Sprint 1 is **Done** when all of the following are true:
- [ ] `docs/requirements.md` is finalised and reviewed
- [ ] `docs/metrics-worksheet.md` has a complete Day 2 baseline column (all 6 CK metrics for `VolumeDataSetRenderer`)
- [ ] All metric tools (Understand, SonarQube, NDepend, CodeScene, DV8) have been run at least once and produce usable output
- [ ] The two Sprint 2 refactoring examples are agreed and documented in `PROGRESS.md`
- [ ] `CONTEXT.md` contains a full render-frame call sequence and dependency map
- [ ] Sub-team 2 and Sub-team 4 have been contacted; stubs are in place if no response received

---

## Sprint 1 Retrospective

*(To be completed at sprint review — Fri 22 May EOD)*

**What went well:**

**What was harder than expected:**

**Carry-over tasks:**

**Action items for Sprint 2:**
