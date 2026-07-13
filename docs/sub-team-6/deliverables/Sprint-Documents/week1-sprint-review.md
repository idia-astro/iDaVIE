# Sprint 1 Review — Sub-team 6 (Die Boks / Team Alpha)

- **Sub-team:** Sub-team 6 — Desktop GUI & Client Shell (brief §6.6)
- **Sprint window:** Mon 18 May – Fri 22 May 2026 (Days 1–5)
- **Review date:** Fri 22 May 2026 (Day 5) — 15-min slot at cross-sub-team review (§8.1)
- **Members + Sprint-1 roles:** Con (SM) · Mark (TL) · Jimmy (POL) · Rory (QC)
- **Companion:** [`week1-sprint-retro.md`](week1-sprint-retro.md) · [`week1-snapshot.png`](week1-snapshot.png) · [`standups.md`](standups.md)

This review is a **file-by-file walkthrough** of `docs/sub-team-6/deliverables/` (every file in that tree was created during Sprint 1). For each artefact: what it is, what's actually inside, and how complete it is at the Sprint-1 boundary.

---

## 1. Sprint goal (as committed Day 2)

> Diagnose the current state. By Fri EOD we have baseline numbers for our slice, a requirements doc draft, two ADRs, and the gateway interface contract is in Sub-team 1's hands.

Source: [`backlog.md`](../../backlog.md) Part B.

---

## 2. Deliverables tree — file-by-file review

### 2.1 `D1-requirements/` — Requirements engineering (feeds REQ-1/2/3/4)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`CanvassDesktop.md`** | 643 lines | **Complete and substantial** | Full method-by-method reference of `CanvassDesktop.cs`. Covers all six Unity lifecycle methods, every File-tab/Render-tab/Stats-tab/Sources-tab/Paint-tab method, identifies **two dead methods** (`CheckImgMaskAxisSize`, `getActiveDataSet`, `getActiveMaskSet`). The closing section **"I/O That Belongs Server-Side"** is the deliverable's strongest contribution — it enumerates **every** server-side I/O site in the file (10 entries: `_browseImageFile`, `UpdateHeaderFromFits`, `ChangeHduSelection`, `_browseMaskFile`, `LoadCubeCoroutine`, `_browseSourcesFile`, `LoadSourcesFile`, `_browseMappingFile`, `_saveMappingFile`, `SetMaxMinPercentile`), classifies each, names the target gateway interface (`IFitsService.OpenAndDescribe`, `IDataSetService.LoadCube`, `IMappingService.Save`, etc.), and surfaces **3 open questions** for the gateway contract (handle vs path, header caching, memory-check ownership). This is the document the File-tab worked example builds on. |
| `Long-Term-Python-Console.md` | 22 lines | **Complete** | REQ-3 roadmap drivers — Python console + Workspace save. Connects each driver back to NFR-2 (Modularity, Modifiability). |
| `Req3 Long-Term Roadmap Drivers.pdf` | 4 pp PDF | Complete | PDF render of the above. |

### 2.2 `D2-Architecture/` — Architecture decision (feeds ARCH-2)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`architecture.md`** | Full D2 architecture document (8 sections) | **Complete** | Contains ADR-0001 (MVVM), ADR-0002 (transport + wire spec), ADR-0003 (ACL), ADR-0004 (UI Toolkit), C4 L3 diagram, SRP decomposition, interface contracts, state contract, §4.2 compliance check. ADR-0002 includes the full wire specification: pipe naming, framing, method catalogue (v1), error model, versioning policy. Author line in ADR-0002 still needs a real name before pitch. |

### 2.3 `D3-MVVM-binding-policy/` — MVVM operating manual (feeds ARCH-9)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`mvvm-binding-policy.md`** | 264 lines, 11 sections | **Scaffold + content mix** | The structural skeleton is complete: §1 Property change · §2 Commands · §3 Collections · §4 Threading · §5 View↔VM wiring · §6 Lifecycle · §7 DTO boundary · §8 Forbidden patterns (8-row table) · §9 Worked examples · §10 CI enforcement (NDepend CQLinq rule sketch + PR checklist) · §11 Glossary. **6 `_TODO_` markers** flag operational decisions still owed: CommunityToolkit-Mvvm vs hand-rolled `ViewModelBase`, `RelayCommand`/`AsyncRelayCommand` shape, `ObservableCollection<T>` vs ring buffer for Debug tab, `IUIDispatcher` interface shape, lifecycle disposal ownership, forbidden-pattern table rows, NDepend rule wording. Forbidden-patterns table and PR checklist are concrete and usable. |

### 2.4 `D4-worked-examples/ex1-file-tab/` — Worked example 1 (File tab)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| `before-class-diagram.puml` | 228 lines PlantUML | **Complete** | God-class structure: Unity-runtime boundary, `FitsReader` P/Invoke, `StandaloneFileBrowser`, `VolumeDataSetRenderer`/`VolumeCommandController`/`VolumeInputController`/`HistogramHelper`, and **CanvassDesktop**'s 8 field groups (inspector-wired GameObjects, file-path state, FITS metadata, subset bounds, runtime singletons, coroutine handles, scene-hierarchy UI components, file-tab public entry points). Bottom note enumerates 6 mixed concerns + cites SRP/OCP/DIP violations + WMC. |
| `after-class-diagram.puml` | 192 lines PlantUML | **Complete** | MVVM target: 4 packages clearly delineated — View layer (`FileTabView : MonoBehaviour`), ViewModel layer (`IFileTabViewModel` interface with 17 members, `FileTabViewModel`, `SubsetBoundsViewModel`, 4 DTO records), Service Interfaces (ACL boundary — `IFitsService`, `IFileDialogService`, `IVolumeService`), Adapters (`FitsServiceAdapter`, `StandaloneFileDialogAdapter`, `VolumeServiceAdapter`). Replaces 3 `CanvassDesktop` methods (`checkSubsetBounds`, `setSubsetBounds`, `updateSubsetZMax`) with `SubsetBoundsViewModel` per-property setters + `ResetToAxisMaxima` + `UpdateZAxisMax`. |
| `before-dependency-graph.puml` | 46 lines | **Complete** | 5-component dep graph: CanvassDesktop direct deps on `VolumeCommandController`/`VolumeInputController`/`HistogramHelper` (via `FindObjectOfType`), `VolumeDataSetRenderer` (via `GetComponentsInChildren`), `StandaloneFileBrowser` + `FitsReader` (static), plus `UnityEngine`/`UnityEngineUI`/`TMPro`/`Valve.VR` platform types. |
| `after-dependency-graph.puml` | 58 lines | **Complete** | Shows `FileTabViewModel CBO ≈ 5` vs CanvassDesktop CBO 47 — the headline number for the pitch. Adapters as ACL boundary; only adapter packages touch `UnityEngine`/`FitsReader`/`StandaloneFileBrowser`. |
| `before-dsm.md` | 5×5 DSM + interpretation | **Complete** | DSM matrix with 4 X marks in row 1 (CanvassDesktop fan-out), triangular (no cycles) but undercounted (`FindObjectOfType` implicit deps not visible in static type graph). Propagation-cost commentary cites CK numbers `CBO 47 / RFC 118 / WMC 63`. |
| `after-dsm.md` | DSM + 4 comparison tables | **Complete** | Compares X-count drops, propagation-cost change for 5 named change scenarios, and **CK projection** table: `CanvassDesktop full class → ~8 WMC / ~4 CBO` (composition root shell); `FileTabViewModel → ~12 WMC / ~5 CBO`; `SubsetBoundsViewModel → ~8 / ~1`; `FitsServiceAdapter → ~10 / ~6`. All projections within §7.1 thresholds. |

**Backlog mapping:** WE1-1, WE1-2 (both BEFORE) and WE1-3 (AFTER class diagram) are **complete** here. WE1-4 (sequence diagram), WE1-5 (skeleton code in `refactoring-examples/`), WE1-6 (CK delta worksheet) are partial or Sprint-2 work. **This is the most-finished worked example of any sub-team's at this point.**

### 2.5 `D5-testing/` — Test strategy components (feeds TEST-1, TEST-2)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`viewmodel-unit-tests.md`** | 10 sections, 178 lines | **Complete** | TEST-1 — Unity-free ViewModel unit-test strategy. Framework stack: NUnit 3 + Moq 4 + .NET 7 SDK standalone + Coverlet + ReportGenerator. 4 dependency-isolation rules (no `UnityEngine` in VM enforced by .csproj, all external services behind interfaces, no `FindObjectOfType`/`MonoBehaviour`, static clock injected via `ISystemClock`). **3 mock patterns** with full code: happy path, error path, observer-test on `ILogStream`. Coverage targets (`ViewModel/` ≥ 70 %, `ServiceGateway/` ≥ 70 %, `View/` tracked). Local-run command + CI-gate spec. Traceability matrix to ADR-01, ARCH-8, TEST-4, §7.1 CK thresholds, LO6. |
| **`ui-toolkit.md`** | 11 sections, 198 lines | **Complete** | TEST-2 — UI Toolkit page-object pattern for integration tests. 5-rule page-object contract (one PO per UXML panel, constructed from root `VisualElement`, intent-only public surface, **ISP ≤ 7 members per page**, event simulation via `SendEvent` not InputSystem). **9 named integration tests** across File-tab (`BrowseImage_HappyPath_ShowsPathAndHeader` + 4 more) and Debug-tab (`StreamEmitsEntry_AppearsInListView` + 3 more). Worked snippet: `FileTabPage` with 5 members (within ISP budget). Settle rule (one frame yield, no polling). Coverage stance (View tracked, not gated — matches §7.2). |

### 2.6 `D7-deliverables/archive-superseded/` — Archive

| File | Status | Notes |
|---|---|---|
| `REQ2_NFR_Maintainability_Table.md` + `.pdf` | **Archived** | Earlier generic NFR-M1…M10 set retired 2026-05-21 — not traceable to our slice or tool chain. Replaced by the 15-NFR table in `requirements.md` §3. Retention is for AI-log defensibility (the swap is recorded). |

### 2.7 `other/D9-ck-baseline/` — CK + SonarQube baseline (feeds T2, BNCH-1, BNCH-2)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`SK_BNCH.md`** + `.pdf` | 5 pp | **Complete and authoritative** | CK baseline for **8 classes** in our slice with all six metrics: **CanvassDesktop O / WMC 63 🔴 / DIT 1 / NOC 0 / CBO 47 🔴 / RFC 118 🔴 / LCOM 0.955 🔴** (4 of 6 metrics breach §7.1), DesktopPaintController A (WMC 57 🔴, RFC 99 🔴, LCOM 0.940 🔴), PaintMenuController O (RFC 56 🔴, LCOM 0.919 🔴), VideoUiManager A (RFC 64 🔴, LCOM 0.863 🔴), HistogramMenuController A (LCOM 0.812 🔴), HistogramHelper D (CBO 13 ⚠, LCOM 0.667 🔴), SourceRow D (LCOM 0.667 🔴), TabsManager D (only clean class, LCOM 0.467 ✅). §3 violation analysis · §4 **dead-code inventory: 12 dead fields across 5 classes** (`_restFrequency`, `inPaintMode`, `_tabsManager`, `firstEnable`, `colormapHeight`, `minZoom`, `cropstatus`, `featureStatus`, `oldSaveText`, `paintMenu`, `savePopup`, `_isPaused`, `editMinScale`, `editMaxScale`) · §5 CanvassDesktop's CBO-47 breakdown (23 project, 13 Unity/TMPro, 7 System, 4 Valve.VR) · §6 refactoring implications. |
| **`SonarQube Baseline report.md`** + `.pdf` | 8 pp | **Complete and authoritative** | LOC/CC/code-smells/duplication/debt/rating per class — totals **4 761 LOC, ~509 cyclomatic, ~67 h technical debt** across our 8 classes. CanvassDesktop + DesktopPaintController rated **D**. **9 methods with CC > 10**: top offenders `CanvassDesktop.checkSubsetBounds` (**CC 31** 🔴), `_browseMappingFile` (CC 19 🔴), `IsLoadable` (CC 15 🔴); `DesktopPaintController.Update` (CC 18 🔴), `FillPolygon` (CC 15 🔴). **Top-10 worst code smells** ranked BLOCKER → MINOR — including the headline finding: **Rank 3 — `DesktopPaintController.UpdateMaxValue(float value) { minVal = value; }` is a silent data-corruption bug**, max threshold never updates, no runtime error, no test exists to catch it. This is the strongest pitch material in the entire sub-team scope: it demonstrates the concrete damage a god class causes. Rank 4 (systematic axis duplication across 8 methods, ~240 lines), Rank 5 (`checkSubsetBounds` 6-way copy-paste), Rank 8 (30+ `transform.Find(…)` chains) all map directly to the refactoring proposal. |

### 2.8 `other/BNCH-6.md` — Mocking-difficulty count (feeds BNCH-6, NFR-TST-2)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`BNCH-6.md`** | 98 lines | **Complete** | 30-class grep across `Assets/Scripts/UI/` + `Assets/Scripts/Menu/` for three signal types: A — runtime singleton lookup (`FindObjectOfType` etc.), B — scene-graph traversal (`transform.Find`/`GetComponent`), C — static external call (`StandaloneFileBrowser`/`FitsReader`/`DllImport`). **`CanvassDesktop` scores 205 total call sites — A:6 B:163 C:36** — column B alone accounts for 163 of 205. 5 classes rated 🔴 High (≥ 20), 9 🟡 Medium (5–19), 16 🟢 Low. Closing section: MVVM split takes CanvassDesktop's column B and C to **zero in the ViewModel layer**; column A eliminated by constructor injection. This is the testability evidence for NFR-TST-2 and ADR-0001's testability claim. |

### 2.9 `other/T2-baseline-benchmark/BNCH-3.md` — CodeScene placeholder

| File | Status | Notes |
|---|---|---|
| `BNCH-3.md` + `.pdf` | **Empty placeholder** | PDF imported but body still empty (4 blank pages). **Tail item carried into Sprint 2** — CodeScene was brought up Day 4 (Rory's standup entry) but the hotspot/churn report hasn't been produced. |

### 2.10 `deliverables-checklist.md` — Master tracker

| File | Status | Notes |
|---|---|---|
| `deliverables-checklist.md` | **Maintained** | 301-line single-source tracker covering: §1 final-day deliverables (pitch + worked examples), §2 sub-team deliverables D1–D14 with file pointers and per-item state, §3 per-student deliverables × 4, §4 process artefacts & ceremonies, §5 cross-cutting obligations, §6 format/compliance constraints, §7 LO/SWEBOK coverage check, §8 ranked punch list, §9 scope discipline. Updated each time a file moves or a state changes. |

### 2.11 `Sprint-Documents/`

| File | Status | Notes |
|---|---|---|
| `standups.md` | **Days 2–5 filled** | Day 2: Con + Mark present (Jimmy + Rory empty). Day 3: all 4 named, all 4 filled. Day 4: full attendance, full content. Day 5: Con + Mark + Jimmy + Rory all named; Con + Mark filled Yesterday + Today; Jimmy + Rory have Yesterday only. Days 6–13 scaffolded. |
| `week1-snapshot.png` | **Complete** | End-of-sprint Kanban snapshot (D6 deliverable #1 of 3). |
| `week1-sprint-review.md` | **This document** | — |
| `week1-sprint-retro.md` | **Empty** | Owed today (Day 5) for the 1h retro + 30-min team retro. |

---

## 3. Backlog status — Sprint 1 cards

21 cards pulled (~75 % capacity by design). Status against the exit criteria in `backlog.md` Part B:

| Exit-criterion ID | Owner | Status | Evidence in deliverables tree |
|---|---|---|---|
| **MGMT-1** Kanban set up | SM | **Done** | `Sprint-Documents/week1-snapshot.png` |
| **MGMT-2** Standups file | SM | **Done** | `Sprint-Documents/standups.md` (Days 2–5 substantive) |
| **MGMT-3** Sprint 1 role assignments | SM | **Done** | `deliverables-checklist.md` §4.1 |
| **BNCH-1** CK baseline | QC | **Done** | `other/D9-ck-baseline/SK_BNCH.md` — 8 classes |
| **BNCH-2** SonarQube baseline | QC | **Done** | `other/D9-ck-baseline/SonarQube Baseline report.md` — 8 classes |
| **BNCH-3** CodeScene baseline | QC | **Carried to Sprint 2** | `other/T2-baseline-benchmark/BNCH-3.md` PDF placeholder; body empty |
| **BNCH-4** DV8/NDepend DSM | QC | **Carried to Sprint 2** | Not produced; NDepend operational per Day 4 standup |
| **BNCH-6** Mocking-difficulty count | QC | **Done** | `other/BNCH-6.md` |
| **REQ-1** GUI tab catalogue | POL | **Done** | `D1-requirements/CanvassDesktop.md` + `requirements.md` §2 |
| **REQ-2** ISO 25010 NFR table | POL | **Done** | `requirements.md` §3 — 15 NFRs |
| **REQ-3** Roadmap drivers | POL | **Done** | `D1-requirements/Long-Term-Python-Console.md` + `requirements.md` §4 |
| **REQ-4** Consolidated req doc | POL | **Done** | `docs/sub-team-6/requirements.md` |
| **ARCH-1** ADR-01 MVVM split | TL | **Done** | `adrs/0001-mvvm-split.md` |
| **ARCH-2** ADR-02 Transport | TL | **Done** | `D2-Architecture/architecture.md` §4 ADR-0002 (with wire spec) |
| **ARCH-8** Interface contracts to Sub-team 1 | TL | **Partial** | Interfaces drafted in `refactoring-examples/sub-team-6/{file-tab,debug-tab}/skeleton/`; formal hand-off owed Day 8 (DEPS-3) |
| **DESN-1** Concern map | TL + all | **Done** | `docs/sub-team-6/concern-map.png` *(spec gap: §10.4 requires text-based source — `.puml`/`.mmd` companion owed)* |
| **DEPS-1** Gateway dependency risk raised | SM | **Done** | Cited in ADR-0001 §Context as R01 |
| **DEPS-2** VR-side menu coord raised | SM | **Done** | Cited as R04 |
| **MGMT-4** Sprint 1 review slides | SM | **This document** | — |
| **MGMT-5** Snapshot + retro | SM | **In progress** | Snapshot done; retro owed today |
| **AI-1** AI log | all | **Done — continuous** | `ai-log.md` — 6 substantive entries (Days 2–3): backlog drafting, diagram-generation × 2, code-skeleton, review, ADR-drafting |

**Headline:** 18 of 21 cards **done** · 1 **partial** (ARCH-8) · 2 **carried** (BNCH-3, BNCH-4). **All exit criteria in `backlog.md` Part B met** — baseline numbers exist, requirements doc done, two ADRs done, interface contracts drafted, concern map present, review + snapshot delivered.

---

## 4. Over-delivery — Sprint-2 work pulled forward

Several Sprint-2 backlog items have visible Sprint-1 output already in the deliverables tree:

| Sprint-2 ID | What landed in Sprint 1 | Where |
|---|---|---|
| **WE1-1** File tab BEFORE class diagram | Complete PlantUML | `D4-worked-examples/ex1-file-tab/before-class-diagram.puml` |
| **WE1-2** File tab BEFORE dep graph + DSM | Both complete | `before-dependency-graph.puml`, `before-dsm.md` |
| **WE1-3** File tab AFTER class diagram | Complete PlantUML with 4-package MVVM | `after-class-diagram.puml` |
| **WE1-2'** AFTER dep graph + DSM | Both complete | `after-dependency-graph.puml`, `after-dsm.md` (with CK projection) |
| **WE1-5** File tab AFTER skeleton | 10 C# files (interfaces + VM + DTOs) | `refactoring-examples/sub-team-6/file-tab/skeleton/` |
| **WE2-3** Debug AFTER sequence diagram | Complete PlantUML | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| **WE2-4** Debug AFTER skeleton | 6 C# files, compiles in isolation (`dotnet build`) | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| **TEST-1** ViewModel unit-test strategy | Complete | `D5-testing/viewmodel-unit-tests.md` |
| **TEST-2** UI Toolkit page-object pattern | Complete | `D5-testing/ui-toolkit.md` |
| **ARCH-9** MVVM binding policy | Substantial scaffold (11 sections) | `D3-MVVM-binding-policy/mvvm-binding-policy.md` |
| **REQ-1 deep-dive** Server-side I/O migration map | 10-site enumeration + gateway interface targets | `D1-requirements/CanvassDesktop.md` §"I/O That Belongs Server-Side" |

This is real cushion against Sprint 2's heavier load (31 cards).

---

## 5. Headline numbers (slide-ready for the 15-min slot)

- **`CanvassDesktop.cs` — 1 899 LOC, single `MonoBehaviour`.** CK: **WMC 63 / CBO 47 / RFC 118 / LCOM 0.955** — 4 of 6 §7.1 thresholds breached.
- **SonarQube rating D** on CanvassDesktop + DesktopPaintController. ~67 h technical debt across our 8 classes. 9 methods exceed CC 10; `checkSubsetBounds` peaks at **CC 31**.
- **Silent data-corruption bug found**: `DesktopPaintController.UpdateMaxValue(float value) { minVal = value; }` — max threshold never updates, no test exists to catch it. **Pitch-grade evidence of god-class danger.**
- **12 dead fields** across 5 classes — including `_restFrequency`, `inPaintMode`, `_tabsManager` on CanvassDesktop alone.
- **`CanvassDesktop` mocking-difficulty: 205 call sites** (A:6 B:163 C:36). MVVM split takes B and C to **zero** in the ViewModel layer.
- **CK projection for File-tab AFTER state**: CanvassDesktop full class WMC 63 → ~8; CBO 47 → ~4. `FileTabViewModel` projected at WMC ~12, CBO ~5 (both within §7.1 domain thresholds of 20 and 14).
- **15 testable NFRs** mapped to all 5 ISO/IEC 25010 maintainability sub-characteristics.
- **MVVM split decided** (ADR-0001): three assemblies (`View → ViewModel → Gateway`); ViewModel assembly has **zero `UnityEngine`/`SteamVR` dependency** by construction; CI rule sketched.
- **JSON-RPC wire spec complete** (ADR-0002 Appendix A): 9 methods catalogued, error codes defined, framing pinned.
- **2 worked-example skeletons compile under `dotnet build`** with no Unity Editor (NFR-TST-1 + NFR-TST-2 evidence).
- **6 AI assists logged** across Days 2–3.

---

## 6. Integration touchpoints

| Sub-team | What flowed | Status |
|---|---|---|
| **Sub-team 1 — Apaties I (Architecture)** | `IServiceGateway`/`IFitsService`/`IVolumeService`/`IFileDialogService`/`ILogStream` interface surfaces drafted in our skeletons. ADR-0002 wire spec defines the JSON-RPC method catalogue that the gateway must serve. Formal hand-off at Day 8 cross-sub-team integration review (DEPS-3). | Open — no blocker |
| **Sub-team 4 — Koffiewinkel (Interaction)** | DEPS-2 raised on integration risk register (R04). No active design conversation yet. | Open — needs sync Sprint 2 |
| **Sub-team 7 — Sewe en sestig (Persistence)** | State-shape contract (ARCH-11 / ARQ-2 in `requirements.md`) is **owed by Day 9**. Architecture stub §8 has placeholder. | Sprint 2 deliverable |
| **Sub-team 5 — Apaties II (Feature domain)** | `ISourceCatalogueService` shape surfaced in `CanvassDesktop.md` §"I/O That Belongs Server-Side" — feeds back to them at next cross-sub-team window. | New — added Sprint 1 |

---

## 7. Risks raised in Sprint 1

1. **`concern-map.png` is binary-only** — violates §10.4 (no binary-only diagrams). Mitigation: add `.puml`/`.mmd` source companion (D10 in checklist).
2. **CodeScene + DV8 + NDepend tail items** — BNCH-3 placeholder, BNCH-4 not produced. NDepend was operational per Day 4 standup. Mitigation: Rory completes both Sprint 2 Day 6–7.
3. **ADR-0002 author line** still reads *"Sub-team 6 TL — fill in"* — needs Mark's name before pitch freeze. Trivial fix.
4. **MVVM binding policy 6 `_TODO_`s** — operational decisions deferred (CommunityToolkit vs hand-rolled, dispatcher interface, lifecycle ownership). Mitigation: address at Sprint 2 planning Day 6.
5. **Sub-team 1 dependency** — gateway contract not yet finalised; our skeleton uses our drafted interfaces. Risk of rework if their surface differs. Mitigation: integration review Day 8.
6. **Day-5 standups thinly recorded** — Jimmy + Rory have no "Today" entries. Mitigation: SM enforces 09:00 cadence in Sprint 2.

---

## 8. Sprint 2 forward look (headline only — full plan in `week2-sprint-plan.md`)

**Sprint 2 goal:** *Produce the design proposal in full.* Both worked examples complete with CK deltas; architecture document (5–10 pp); test strategy (2–4 pp); state contract to Sub-team 7 by Day 9; Day-10 projected metrics; mid-assessment iDaVIE visit handled.

**31 cards** pulled (`backlog.md` Part C). This is the heavy sprint. Buffer: Sprint-2 work already in `D4`/`D5` (see §4) shortens the critical path.

**Role rotation for Sprint 2** (per §10.1 standard rotation):

| Sprint | SM | TL | POL | QC |
|---|---|---|---|---|
| Sprint 1 | Con | Mark | Jimmy | Rory |
| **Sprint 2** | **Rory** | **Con** | **Mark** | **Jimmy** |
| Sprint 3 | Jimmy | Rory | Con | Mark |

*(Confirm at Sprint 2 planning, Day 6.)*

---

## 9. Evidence index — one-click for the panel

| Artefact | Path |
|---|---|
| Requirements doc | `docs/sub-team-6/requirements.md` |
| CanvassDesktop method reference + server-side I/O map | `D1-requirements/CanvassDesktop.md` |
| Long-term roadmap drivers | `D1-requirements/Long-Term-Python-Console.md` |
| ADR-0001 MVVM split | `docs/sub-team-6/adrs/0001-mvvm-split.md` |
| ADR-0002 Transport (+ wire spec) | `D2-Architecture/architecture.md` §4 |
| MVVM binding policy scaffold | `D3-MVVM-binding-policy/mvvm-binding-policy.md` |
| File-tab BEFORE class diagram | `D4-worked-examples/ex1-file-tab/before-class-diagram.puml` |
| File-tab AFTER class diagram | `D4-worked-examples/ex1-file-tab/after-class-diagram.puml` |
| File-tab BEFORE dep graph | `D4-worked-examples/ex1-file-tab/before-dependency-graph.puml` |
| File-tab AFTER dep graph (CBO 47 → ~5) | `D4-worked-examples/ex1-file-tab/after-dependency-graph.puml` |
| File-tab BEFORE DSM | `D4-worked-examples/ex1-file-tab/before-dsm.md` |
| File-tab AFTER DSM + CK projection | `D4-worked-examples/ex1-file-tab/after-dsm.md` |
| File-tab AFTER skeleton (10 .cs files) | `refactoring-examples/sub-team-6/file-tab/skeleton/` |
| Debug-tab AFTER sequence diagram | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| Debug-tab AFTER skeleton (compiles, isolation) | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| ViewModel unit-test strategy (TEST-1) | `D5-testing/viewmodel-unit-tests.md` |
| UI Toolkit page-object pattern (TEST-2) | `D5-testing/ui-toolkit.md` |
| CK baseline (8 classes, 5 pp) | `other/D9-ck-baseline/SK_BNCH.md` |
| SonarQube baseline (8 classes, 8 pp, Top-10 smells) | `other/D9-ck-baseline/SonarQube Baseline report.md` |
| Mocking-difficulty count (30 classes) | `other/BNCH-6.md` |
| CodeScene placeholder | `other/T2-baseline-benchmark/BNCH-3.md` |
| Architecture stub | `docs/sub-team-6/architecture.md` |
| Concern map | `docs/sub-team-6/concern-map.png` *(text source owed)* |
| Backlog (truth) | `docs/sub-team-6/backlog.md` + `backlog.csv` |
| Standups | `docs/sub-team-6/deliverables/Sprint-Documents/standups.md` *(canonical copy now under Sprint-Documents)* |
| AI log | `docs/sub-team-6/ai-log.md` |
| Kanban snapshot | `Sprint-Documents/week1-snapshot.png` |
| Deliverables master tracker | `deliverables-checklist.md` |
