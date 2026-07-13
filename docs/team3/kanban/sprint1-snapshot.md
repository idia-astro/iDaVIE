# Kanban Snapshot — Sprint 1
## Sub-team 3: Rendering Engine ("Cache Me If You Can")
**Sprint Dates:** Mon 18 May – Fri 22 May 2026
**Sprint Goal:** Fully understand the rendering codebase, establish Day 2 CK metric baseline, complete the requirements document, and lay groundwork for Sprint 2 design work.
**Scrum Master:** Cathal | **Tech Lead:** Damien | **PO Liaison:** Ciallian | **Quality Champion:** Chris

---

## Board State (as of Fri 22 May EOD)

| Column | Count |
|--------|-------|
| ✅ Done | 38 |
| 🔄 In Progress | 2 |
| ⏳ To Do | 5 |
| 🚫 Blocked | 2 |

---

## ✅ Done

### 🟦 Kickoff & Setup

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-K01 | Read and annotate the full project brief | All | Constraints, deliverables, and deadlines catalogued |
| S1-K02 | Agree Sprint 1 goal and Definition of Done as a sub-team | Cathal | Agreed Mon 18 May |
| S1-K03 | Formally assign Sprint 1 roles and confirm responsibilities | Cathal | Scrum Master: Cathal, Tech Lead: Damien, PO Liaison: Ciallian, QC: Chris |
| S1-K04 | Create sub-team GitHub repository/folder for all deliverables | Cathal | Repo set up Mon 18 May |
| S1-K05 | Set up project folder structure | Cathal | `docs/`, `refactoring-examples/`, `diagrams/`, `kanban/`, `standup/`, `tests/` all created |
| S1-K06 | Write `CLAUDE.md` — project charter and session instructions | Cathal | Includes scope, roles, constraints, sprint plan, key decisions log |
| S1-K07 | Write `CONTEXT.md` — running technical context file | Cathal | Populated with architecture understanding; updated each session |
| S1-K08 | Write `PROGRESS.md` — running session log | Cathal | Updated after every working session |

### 🔧 Tooling Setup

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-T01 | Install and configure **Understand** (SciTools) | Chris | Verified against iDaVIE C# solution; CK metrics confirmed working |
| S1-T02 | Set up **SonarQube Cloud** | Chris | iDaVIE repo linked; first scan run; code smell + complexity reports populated |
| S1-T03 | Set up **NDepend** | Chris | iDaVIE `.sln` imported; dependency graph renders correctly; architecture rules running |
| S1-T04 | Install **CodeScene** | Chris | iDaVIE Git history connected; hotspot and churn analysis available |
| S1-T05 | Install **DV8** | Chris | iDaVIE dependency data imported; Dependency Structure Matrix generating correctly |

### 🔍 Codebase Exploration

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-C01 | Clone iDaVIE repository and read top-level structure | Damien | https://github.com/idia-astro/iDaVIE; Unity version confirmed |
| S1-C02 | Read `VolumeDataSetRenderer.cs` in full — annotate all responsibilities | Damien | ~1400 lines; God Class confirmed; 8+ distinct responsibilities identified |
| S1-C03 | Read `VolumeDataSetRendererMaskMode.cs` | Damien | Switch-on-string anti-pattern confirmed; documented in `docs/Codebase Exploration/MaskMode_Annotation.md` |
| S1-C04 | Read `Shaders/VolumeRender.shader` | Damien | Blocky (nearest-neighbour) filtering confirmed; foveated sampling rate uniform found; see `VolumeRender.md` |
| S1-C05 | Read all `ColourMap*.shader` files | Chris | Colour transfer function applied per-ray-step in shader; see `ColourMap_Annotation.md` |
| S1-C07 | List every hardcoded constant in the rendering layer | Chris | See table in `docs/requirements.md` §3 |
| S1-C08 | Catalogue all Unity 5 / Built-in RP APIs currently used | Damien | 5 API touchpoints identified; see `Unity5_BuiltInRP_API_Catalogue.md` |
| S1-C09 | Write up the full render-frame call sequence end-to-end | All | 9-step sequence written up; see `RenderFrame_CallSequence.md` |
| S1-C10 | Identify other rendering-adjacent classes for metrics baseline | Damien | ColourMapController and related classes identified; see `RenderingAdjacentClasses.md` |

### 📊 CK Metrics Baseline

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-M01 | Run **Understand** on `VolumeDataSetRenderer.cs` — record WMC, DIT, NOC, CBO, RFC, LCOM | Chris | Day 2 baseline captured; all 6 CK metrics recorded |
| S1-M02 | Run **Understand** on rendering-adjacent classes | Chris | ColourMapController and camera helpers included in baseline |
| S1-M03 | Run **SonarQube** on rendering files — record cyclomatic complexity and code smell count | Chris | 14 code smells; cyclomatic complexity 312 on VolumeDataSetRenderer |
| S1-M04 | Run **CodeScene** — identify top hotspots and churn rate | Chris | VolumeDataSetRenderer flagged as top hotspot; 47 commits in last 6 months |
| S1-M05 | Run **NDepend** — check for circular dependencies and flag architecture violations | Chris | No circular deps found; 3 architecture violations flagged |
| S1-M06 | Run **DV8** — generate Dependency Structure Matrix for rendering layer | Chris | DSM generated; dependency tangles noted in metrics worksheet |
| S1-M07 | Collate all raw numbers into `docs/metrics-worksheet.md` — Day 2 baseline column | Chris | All 6 CK metrics populated; structure in place for Day 13 projected column |
| S1-M08 | Write one-paragraph summary of what baseline metrics reveal | Chris | Summary written; quantitative case for refactoring confirmed as strong |

### 📝 Requirements Document

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-R01 | Draft Section 1 — current system invariants | Ciallian | 90 fps, 4 GB texture limit, 368 MB budget, blocky filtering, foveated rendering documented |
| S1-R02 | Draft Section 2 — future requirements the design must not preclude | Ciallian | Iso-contours, multi-cube, time-series all captured |
| S1-R03 | Draft Section 3 — interface contracts needed from other sub-teams | Ciallian | `IGazeProvider` and `RawVolumeData` stubs defined; flagged as pending |
| S1-R04 | Draft Section 4 — architectural constraints from brief §4.2 | Ciallian | All 5 constraints from §4.2 documented with rationale |

### 🤝 Cross-team Coordination

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-X01 | Contact Sub-team 2 (Data I/O) — request `RawVolumeData` struct definition | Ciallian | Chased 18 May and 20 May; no response by sprint end |
| S1-X02 | Contact Sub-team 4 (Interaction) — request `IGazeProvider` interface definition | Ciallian | Chased 18 May and 20 May; no response by sprint end |
| S1-X03 | Attend whole-team kickoff and cross-team sync | Ciallian | Attended; no scope changes for sub-team 3 |
| S1-X04 | Define working `IGazeProvider` stub (fallback if no reply by Wed 20 May) | Ciallian | Stub defined in `CONTEXT.md`; design is unblocked |
| S1-X05 | Define working `RawVolumeData` stub (fallback if no reply by Wed 20 May) | Ciallian | Stub defined in `CONTEXT.md`; design is unblocked |

### 🏗️ Early Design Groundwork

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-D01 | Sketch the four-class split rough notes | Damien | `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` all named and scoped |
| S1-D02 | Draft `IRenderPipeline` interface stub | Damien | Stub created in `refactoring-examples/stubs/IRenderPipeline.cs`; 6 methods; DIP rationale documented |
| S1-D03 | Draft `IMaskMode` interface stub and three empty implementations | Damien | `IMaskMode` + `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode` drafted; `NullMaskMode` test double included |
| S1-D05 | List every SOLID/GRASP violation in current code | Damien | See `docs/Codebase Exploration/SOLID_GRASP_Violations.md`; 6 violations identified with line evidence |

### ⚙️ Process

| ID | Task | Owner | Notes |
|----|------|-------|-------|
| S1-P01 | Maintain daily standup log (`standup/standup-log.md`) | Cathal | All 5 standups recorded |
| S1-P02 | Mid-sprint status review (Wed 20 May) | All | Carried out; three carry-over tasks identified |

---

## 🔄 In Progress

| ID | Task | Owner | Due | Notes |
|----|------|-------|-----|-------|
| S1-R05 | Team review of full `docs/requirements.md` draft — check against CLAUDE.md invariants and brief §9.2 | Chris, All | 22 May EOD | Review started; one gap found (iso-contour future requirement); waiting on final sign-off |
| S1-C06 | Map all class-level dependencies of `VolumeDataSetRenderer` — list every external type it imports or calls | Damien | 22 May EOD | 31 external couplings identified so far; finalising CBO count for metrics worksheet |

---

## ⏳ To Do (Carry-over to Sprint 2)

| ID | Task | Owner | Priority | Notes |
|----|------|-------|----------|-------|
| S1-D04 | Agree and document the two Sprint 2 refactoring examples | Damien, Ciallian, Chris | 🔴 Urgent | Leading candidates confirmed in design groundwork; final team decision still needed |
| S1-X06 | Log all cross-team interface agreements and open questions in `PROGRESS.md` blockers table | Cathal | 🟡 Normal | Blocked items logged; full interface agreement log not yet written up |
| S1-R06 | Incorporate review feedback and finalise `docs/requirements.md` | Chris | 🔴 Urgent | Depends on S1-R05 completing; one known gap (iso-contour) already flagged |
| S1-T07 | Document tool versions, access URLs/licences, and setup gotchas in `CONTEXT.md` | Ciallian | 🟡 Normal | Tools all running; documentation not yet written up |
| S1-T06 | Smoke-test all tools against `VolumeDataSetRenderer.cs` — confirm each produces usable output | Ciallian | 🔴 Urgent | Individual tools verified; formal combined smoke-test pass not completed |

---

## 🚫 Blocked

| ID | Task | Owner | Blocked By | Notes |
|----|------|-------|-----------|-------|
| S1-16 | Obtain `IGazeProvider` interface definition from Sub-team 4 | Ciallian | Sub-team 4 — not yet delivered | Needed for foveated rendering design in Sprint 2; chased 20 May; stub in place |
| S1-17 | Obtain `RawVolumeData` texture format contract from Sub-team 2 | Ciallian | Sub-team 2 — not yet delivered | Needed for `VolumeTextureManager` design in Sprint 2; chased 20 May; stub in place |

---

## CK Metric Baseline (Day 2)

Full numbers in `docs/metrics-worksheet.md`.

| Metric | `VolumeDataSetRenderer` (measured) | Target (refactored classes) | Gap |
|--------|-----------------------------------|-----------------------------|-----|
| WMC | ~74 | ≤ 20 per class | ❌ 3.7× over |
| DIT | 2 | ≤ 4 | ✅ Within target |
| NOC | 0 | ≤ 5 | ✅ Within target |
| CBO | ~31 | ≤ 14 (domain), ≤ 25 (orchestrators) | ❌ 2.2× over |
| RFC | ~89 | ≤ 50 | ❌ 1.8× over |
| LCOM | ~0.81 | ≤ 0.5 | ❌ 62% over |

*WMC, CBO, RFC, and LCOM all significantly exceed targets — the quantitative case for refactoring is strong.*

---

## Hardcoded Constants Identified

| Constant | Value | Location |
|----------|-------|----------|
| Target FPS | 90 | `VolumeDataSetRenderer.cs` |
| Max texture size | 4 GB | Unity engine limit — enforced in texture upload logic |
| Default cube memory budget | 368 MB | `VolumeDataSetRenderer.cs` |
| Foveated inner radius | ~0.15 (normalised screen coords) | `VolumeRender.shader` (`_FovRadius` uniform) |
| Foveated sample rate ratio (centre:edge) | ~4:1 | `VolumeRender.shader` |

---

## Unity 5 APIs Requiring SRP Migration

| Current API (Unity 5 / Built-in) | Unity 6 URP Replacement | Risk |
|----------------------------------|-------------------------|------|
| `Camera.onPostRender` | `ScriptableRenderPass.Execute()` | High — core render loop |
| `Graphics.Blit()` | `Blitter.BlitCameraTexture()` | Medium |
| `RenderTexture` creation (direct) | `RenderTextureDescriptor` via `IRenderPipeline` | Medium |
| Shader `UNITY_MATRIX_VP` macros | URP `GetViewProjectionMatrix()` | Low |
| `CommandBuffer` (legacy path) | `CommandBuffer` via URP `RenderingData` | Medium |

---

## Sprint 1 Retrospective Notes

*(Completed at sprint review — Fri 22 May)*

**What went well:**
- Codebase exploration moved faster than expected — Damien had the full VDSR responsibility map done by Wed 20 May
- All five metric tools were installed and producing output before mid-sprint review
- Requirements document first draft completed by Ciallian on day 3, leaving time for review
- Design groundwork (IRenderPipeline, IMaskMode stubs) is further along than the sprint goal required

**What could be improved:**
- Cross-team coordination should have started on day 1 with a harder deadline communicated — both sub-teams 2 and 4 are still non-responsive at sprint end
- Tool smoke-test (S1-T06) and tool documentation (S1-T07) were deprioritised in favour of codebase work and should have been parallelised earlier in the week
- Agreeing the Sprint 2 refactoring examples (S1-D04) should have been a dedicated session, not left as the last item

**Action items for Sprint 2:**
- Chase Sub-team 2 and Sub-team 4 at first standup Mon 25 May — stubs are sufficient for design but real interfaces needed by Wed 27 May
- S1-R05/R06 (requirements review + finalise) to be completed Mon 25 May morning before Sprint 2 design work begins
- S1-D04 (agree refactoring examples) to be the first team decision of Sprint 2 — blocks all worked example work
- Ciallian to complete S1-T06/T07 (smoke-test + tool docs) Mon 25 May

---

## Carry-Over to Sprint 2

| ID | Task | Priority | Sprint 2 Owner |
|----|------|----------|----------------|
| S1-R05 | Complete team review of `docs/requirements.md` | HIGH | Chris, All |
| S1-R06 | Incorporate feedback and finalise `docs/requirements.md` | HIGH | Chris |
| S1-C06 | Finalise class-level dependency map of VDSR | HIGH — feeds CBO baseline | Damien |
| S1-D04 | Agree and document two Sprint 2 refactoring examples | HIGH — blocks all example work | All |
| S1-T06 | Formal smoke-test of all tools on VDSR | MEDIUM | Ciallian |
| S1-T07 | Document tool versions and setup notes in CONTEXT.md | MEDIUM | Ciallian |
| S1-X06 | Log interface agreements and open questions in PROGRESS.md | MEDIUM | Cathal |
| S1-16 | `IGazeProvider` from Sub-team 4 | MEDIUM — stub in place | Ciallian |
| S1-17 | `RawVolumeData` from Sub-team 2 | MEDIUM — stub in place | Ciallian |
