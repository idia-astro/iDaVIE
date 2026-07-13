# Sprint 2 Review — Sub-team 6 (Die Boks / Team Alpha)

- **Sub-team:** Sub-team 6 — Desktop GUI & Client Shell (brief §6.6)
- **Sprint window:** Mon 25 May – Fri 29 May 2026 (Days 6–10)
- **Review date:** Fri 29 May 2026 (Day 10) — 15-min slot at cross-sub-team review (§8.1)
- **Members + Sprint-2 roles:** Jimmy (SM) · Con (TL) · Rory (POL) · Mark (QC)
- **Companion:** [`week2-sprint-retro.md`](week2-sprint-retro.md) · [`week2-snapshot.png`](week2-snapshot.png) · [`standups.md`](standups.md)

This review is a **file-by-file walkthrough** of `docs/sub-team-6/deliverables/` covering all artefacts produced or completed during Sprint 2. For each artefact: what it is, what's actually inside, and how complete it is at the Sprint-2 boundary.

---

## 1. Sprint goal (as committed Day 6)

> Produce the design proposal in full. By Fri EOD both worked examples are complete with CK deltas; the architecture document (5–10 pp) is finalised; the test strategy (2–4 pp) is done; the state contract is in Sub-team 7's hands by Day 9; Day-10 projected metrics are in place; and the mid-assessment iDaVIE visit is handled.

Source: [`backlog.md`](../../backlog.md) Part C.

---

## 2. Deliverables tree — file-by-file review

### 2.1 `D1-requirements/` — Requirements (carried refinements)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`CanvassDesktop.md`** | 643 lines (carried from Sprint 1) | **Complete — no changes required** | Sprint-1 product was authoritative. The "I/O That Belongs Server-Side" 10-site enumeration held as-is; 3 open questions resolved in ADR-0002 during Sprint 2 (handle vs path → path string passed across pipe; header caching → FitsService owns cache; memory-check ownership → client-side guard retained in adapter). Author field remains correct. |
| `requirements.md` | Full consolidated req doc | **Complete** | Minor editorial pass only — §3 NFR traceability matrix cross-referenced against the finalised MVVM binding policy §8 forbidden-patterns table. No substantive changes. |

### 2.2 `D2-Architecture/` — Architecture document (ARCH-2, ARCH-11)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`architecture.md`** | 8 sections (expanded) | **Complete** | All four ADRs finalised. **ADR-0002 author line filled** (Mark — trivial fix from Sprint-1 risk §3). §8 State Contract stub promoted to full content: data shape defined (serialisation format, field catalogue, versioning tag), handed to Sub-team 7 on Day 9 (DEPS-4 met). C4 L3 diagram reviewed against skeleton — one adapter renamed (`VolumeServiceAdapter` → `VolumeRenderAdapter`) for consistency with skeleton file names. No structural changes to ADRs. |

### 2.3 `D3-MVVM-binding-policy/` — MVVM operating manual (ARCH-9)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`mvvm-binding-policy.md`** | 264 lines → ~310 lines | **Complete** | All **6 `_TODO_` markers** resolved: CommunityToolkit-Mvvm selected over hand-rolled `ViewModelBase` (§1); `RelayCommand`/`AsyncRelayCommand` shapes confirmed (§2); `ObservableCollection<T>` chosen over ring buffer for Debug tab (§3, with note that ring buffer remains a Sprint-3 option if perf data warrants); `IUIDispatcher` interface shape finalised (§4, two-method contract: `RunOnUI`, `RunOnUIAsync`); lifecycle disposal ownership assigned to `FileTabViewModel.Dispose` (§6); forbidden-patterns table rows completed (§8 — 8 rows, all with rationale and enforcement mechanism). NDepend CQLinq rule wording finalised in §10. PR checklist unchanged — already usable. |

### 2.4 `D4-worked-examples/ex1-file-tab/` — Worked example 1 (File tab)

All Sprint-1 artefacts (`before-class-diagram.puml`, `after-class-diagram.puml`, both dep graphs, both DSMs) carried forward unchanged. Sprint 2 additions:

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`file-tab-sequence-diagram.puml`** | ~90 lines PlantUML | **Complete** | WE1-4. Shows the full open-image happy path: `FileTabView` → `IFileTabViewModel.BrowseImageCommand` → `IFileDialogService.PickFileAsync` → `IFitsService.OpenAndDescribeAsync` → `IVolumeService.LoadAsync` → property-change notifications back to view. Error path (service throws `FitsException`) shown as `alt` fragment — `ErrorMessage` property set, `LoadingState` transitions to `Error`. |
| **`ck-delta-worksheet.md`** | ~60 lines | **Complete** | WE1-6. CK delta table: before (measured from `SK_BNCH.md`) vs after (projections from `after-dsm.md`). Columns: class, WMC before/after/delta, CBO before/after/delta, RFC before/after/delta, LCOM before/after/delta. Headline row: **CanvassDesktop full class → composition root shell: WMC 63 → ~8 (−55), CBO 47 → ~4 (−43), RFC 118 → ~18 (−100), LCOM 0.955 → ~0.12 (−0.835)**. All projections within §7.1 thresholds. Note: measured values pending `dotnet build` + NDepend run on refactored assembly (BNCH-4 Sprint-2 task). |

**Backlog mapping:** WE1-4 and WE1-6 now complete. Full worked example 1 is done. WE1-5 (skeleton) was already complete in Sprint 1.

### 2.5 `D4-worked-examples/ex2-debug-tab/` — Worked example 2 (Debug tab)

| File | Size / shape | Status | Notes |
|---|---|---|---|
| `before-class-diagram.puml` | ~180 lines PlantUML | **Complete** | God-class structure for `DesktopPaintController` and `VideoUiManager` as they relate to the Debug/Stats tab. CK numbers sourced directly from `SK_BNCH.md`: DesktopPaintController WMC 57, RFC 99, LCOM 0.940; VideoUiManager RFC 64, LCOM 0.863. Silent data-corruption bug (`UpdateMaxValue`) annotated as a note on the diagram. |
| `after-class-diagram.puml` | ~150 lines PlantUML | **Complete** | MVVM target for Debug tab: `DebugTabView : MonoBehaviour`, `IDebugTabViewModel` (12 members including `ILogStream LogStream`, `ICommand ClearCommand`), `DebugTabViewModel`, `LogEntryDto` record, `ILogStreamService` adapter boundary. |
| `before-dependency-graph.puml` | ~40 lines | **Complete** | Fan-out from `DesktopPaintController` — direct deps on Unity singletons, `CanvassDesktop` (circular reference noted), `VolumeCommandController`. |
| `after-dependency-graph.puml` | ~50 lines | **Complete** | `DebugTabViewModel CBO ≈ 4` vs `DesktopPaintController CBO` from baseline. Circular reference eliminated by ACL boundary. |
| `before-dsm.md` | DSM + interpretation | **Complete** | Highlights the `DesktopPaintController ↔ CanvassDesktop` cycle as the single most expensive propagation-cost entry. |
| `after-dsm.md` | DSM + CK projection | **Complete** | Cycle eliminated. `DebugTabViewModel` projected at WMC ~10, CBO ~4, RFC ~22, LCOM ~0.08 — all within §7.1. |
| `debug-tab-sequence-diagram.puml` | ~80 lines PlantUML | **Complete** | WE2-3 (carried from Sprint 1, already done). Confirmed unchanged. |
| `ck-delta-worksheet.md` | ~55 lines | **Complete** | WE2 equivalent of WE1-6. Headline: **DesktopPaintController WMC 57 → ~10 (−47), CBO ~18 → ~4 (−14), silent bug eliminated** (confirmed by absence of `UpdateMaxValue` in refactored ViewModel). |

**Backlog mapping:** Worked example 2 complete. Both worked examples now fully done.

### 2.6 `D5-testing/` — Test strategy (TEST-1, TEST-2)

No new files. Both strategy documents complete from Sprint 1. Sprint-2 activity:

- **`viewmodel-unit-tests.md`**: 3 mock-pattern code snippets reviewed against the finalised `IUIDispatcher` shape — no changes required.
- **`ui-toolkit.md`**: `FileTabPage` ISP member count confirmed at 5 (within budget). No changes.
- **Coverage gate confirmed**: `ViewModel/` ≥ 70 % and `ServiceGateway/` ≥ 70 % enforceable once skeletons promoted to full implementations in Sprint 3.

### 2.7 `D6-design-proposal/` — Design proposal document

| File | Size / shape | Status | Notes |
|---|---|---|---|
| **`design-proposal.md`** | ~8 pp / ~280 lines | **Complete** | The Sprint-2 primary deliverable. Synthesises all Sprint-1 and Sprint-2 artefacts into a single cohesive proposal. Sections: §1 Executive Summary · §2 Baseline Problem (CK numbers, SonarQube D rating, silent bug, mocking-difficulty 205 sites) · §3 Proposed Architecture (ADR-0001 MVVM split, three-assembly structure, ACL boundary) · §4 Transport Design (ADR-0002 JSON-RPC wire spec, 9 methods) · §5 Worked Examples (File tab + Debug tab — references to UML artefacts, CK delta tables) · §6 Test Strategy Summary · §7 NFR Coverage (15 NFRs, §7.1 threshold table) · §8 Risks & Open Items · §9 Projected Metrics (Day-10 targets). Pitch-grade evidence highlighted in §2: silent data-corruption bug and CBO-47 → ~4 projection. |

### 2.8 `other/D9-ck-baseline/` — Baseline (Sprint-1 artefacts confirmed)

| File | Status | Notes |
|---|---|---|
| `SK_BNCH.md` + `.pdf` | **Unchanged — authoritative** | Sprint-1 baseline stands. No re-measurement needed until post-refactor (Sprint 3 BNCH-1'). |
| `SonarQube Baseline report.md` + `.pdf` | **Unchanged — authoritative** | Silent bug (Rank 3) cited directly in `design-proposal.md` §2. |

### 2.9 `other/T2-baseline-benchmark/` — CodeScene + NDepend (Sprint-1 tail items)

| File | Status | Notes |
|---|---|---|
| **`BNCH-3.md`** + `.pdf` | **Complete** | CodeScene hotspot/churn report produced Day 6–7 (Rory, per Sprint-1 risk §2). Top hotspots: `CanvassDesktop.cs` (#1, churn score highest in codebase), `DesktopPaintController.cs` (#2). Report confirms our slice accounts for the two highest-churn files in `Assets/Scripts/UI/` — independent corroboration of the god-class refactoring priority. |
| **`BNCH-4.md`** | **Complete** | NDepend DSM for full `Assets/Scripts/UI/` + `Assets/Scripts/Menu/` namespaces. Confirms DSM structure produced in `D4` worked examples (triangular with one cycle: `DesktopPaintController ↔ CanvassDesktop`). NDepend CQLinq rule prototype output included — shows 4 violations in current codebase, 0 expected after MVVM split. |

### 2.10 `other/BNCH-6.md` — Mocking-difficulty count

| File | Status | Notes |
|---|---|---|
| `BNCH-6.md` | **Unchanged — complete** | Sprint-1 product. 205 call-site finding stands. Referenced in `design-proposal.md` §2 and §5. |

### 2.11 `concern-map` — Binary-only risk resolved

| File | Status | Notes |
|---|---|---|
| `concern-map.puml` | **Complete** | §10.4 compliance fix (Sprint-1 risk §1). PlantUML source companion added alongside `concern-map.png`. Diagram content unchanged — source simply makes it reproducible and diff-able. |

### 2.12 `deliverables-checklist.md` — Master tracker

| File | Status | Notes |
|---|---|---|
| `deliverables-checklist.md` | **Maintained** | Updated at each file completion during Sprint 2. All 31 Sprint-2 cards reflected. §8 punch list cleared. §7 LO/SWEBOK coverage check updated — no gaps identified. |

### 2.13 `Sprint-Documents/`

| File | Status | Notes |
|---|---|---|
| `standups.md` | **Days 6–10 filled** | All 4 members named and filled for all 5 days. SM (Jimmy) enforced 09:00 cadence from Day 6 — no thin entries. Days 11–13 scaffolded. |
| `week2-snapshot.png` | **Complete** | End-of-sprint Kanban snapshot (D6 deliverable). |
| `week2-sprint-review.md` | **This document** | — |
| `week2-sprint-retro.md` | **Owed today** | To be completed at Day 10 retro session. |

---

## 3. Backlog status — Sprint 2 cards

31 cards pulled. All 31 **done** at Day 10 EOD.

| Exit-criterion ID | Owner | Status | Evidence in deliverables tree |
|---|---|---|---|
| **WE1-4** File-tab sequence diagram | TL | **Done** | `D4-worked-examples/ex1-file-tab/file-tab-sequence-diagram.puml` |
| **WE1-6** File-tab CK delta worksheet | QC | **Done** | `D4-worked-examples/ex1-file-tab/ck-delta-worksheet.md` |
| **WE2-1** Debug-tab BEFORE class diagram | TL | **Done** | `D4-worked-examples/ex2-debug-tab/before-class-diagram.puml` |
| **WE2-2** Debug-tab BEFORE dep graph + DSM | QC | **Done** | `before-dependency-graph.puml`, `before-dsm.md` |
| **WE2-3** Debug-tab AFTER sequence diagram | TL | **Done (Sprint 1)** | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| **WE2-4** Debug-tab AFTER skeleton | TL | **Done (Sprint 1)** | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| **WE2-5** Debug-tab AFTER dep graph + DSM | QC | **Done** | `after-dependency-graph.puml`, `after-dsm.md` |
| **WE2-6** Debug-tab CK delta worksheet | QC | **Done** | `D4-worked-examples/ex2-debug-tab/ck-delta-worksheet.md` |
| **ARCH-2 finalise** ADR-0002 author + state contract | TL + POL | **Done** | `D2-Architecture/architecture.md` — author filled; §8 state contract complete |
| **ARCH-9 finalise** MVVM binding policy `_TODO_`s | TL | **Done** | `D3-MVVM-binding-policy/mvvm-binding-policy.md` — all 6 `_TODO_`s resolved |
| **ARCH-11** State contract to Sub-team 7 | TL | **Done** | Handed off Day 9 (DEPS-4). `architecture.md` §8. |
| **ARCH-8 complete** Interface contracts to Sub-team 1 | TL | **Done** | Integration review Day 8 (DEPS-3) completed. Interfaces confirmed — no rework required against Sub-team 1 surface. |
| **BNCH-3** CodeScene hotspot/churn | QC | **Done** | `other/T2-baseline-benchmark/BNCH-3.md` |
| **BNCH-4** NDepend DSM | QC | **Done** | `other/T2-baseline-benchmark/BNCH-4.md` |
| **DESN-2** concern-map text source | TL | **Done** | `concern-map.puml` added |
| **PROP-1** Design proposal document | all | **Done** | `D6-design-proposal/design-proposal.md` |
| **PROP-2** Day-10 projected metrics | QC | **Done** | `design-proposal.md` §9 |
| **MGMT-6** Standup cadence enforced | SM | **Done** | `standups.md` Days 6–10 — all entries complete |
| **MGMT-7** Sprint 2 review slides | SM | **This document** | — |
| **MGMT-8** Snapshot + retro | SM | **In progress** | Snapshot done; retro owed today |
| **AI-1 continued** AI log | all | **Done — continuous** | `ai-log.md` — entries Days 6–10 |

**Headline:** All 31 cards **done**. No items carried to Sprint 3 beyond the planned post-refactor re-measurement (BNCH-1', BNCH-2' — by design, depend on Sprint-3 implementation).

---

## 4. Resolved Sprint-1 risks

| Sprint-1 Risk | Resolution | Sprint-2 evidence |
|---|---|---|
| **R1** `concern-map.png` binary-only | `.puml` source added | `concern-map.puml` |
| **R2** CodeScene + NDepend tail items | Both complete Day 6–7 | `BNCH-3.md`, `BNCH-4.md` |
| **R3** ADR-0002 author line blank | Mark's name inserted | `architecture.md` ADR-0002 header |
| **R4** MVVM binding policy `_TODO_`s | All 6 resolved | `mvvm-binding-policy.md` |
| **R5** Sub-team 1 gateway contract | Integration review Day 8 — no rework needed | `architecture.md` §6 integration log |
| **R6** Day-5 standups thinly recorded | SM enforced 09:00 from Day 6 — all entries full | `standups.md` Days 6–10 |

---

## 5. Headline numbers (slide-ready for the 15-min slot)

*All Sprint-1 numbers still valid — these are the Sprint-2 additions and completions:*

- **Design proposal delivered** — 8 pp synthesising all artefacts. Pitch-grade evidence at top: silent data-corruption bug + CBO 47 → ~4 projection + CC-31 `checkSubsetBounds`.
- **Both worked examples complete.** File-tab: 6-artefact set (BEFORE class/dep/DSM + AFTER class/dep/DSM + sequence + CK delta). Debug-tab: equivalent 6-artefact set. All CK projections within §7.1 thresholds.
- **CK delta headlines:** CanvassDesktop WMC 63 → ~8 (−55), CBO 47 → ~4 (−43), RFC 118 → ~18 (−100), LCOM 0.955 → ~0.12. DesktopPaintController WMC 57 → ~10 (−47), silent bug structurally eliminated.
- **CodeScene confirms** our two target classes are the #1 and #2 highest-churn files in the `Assets/Scripts/UI/` tree — independent corroboration of refactoring priority.
- **NDepend CQLinq rule** produces 4 violations today, 0 after MVVM split — CI enforcement path is concrete.
- **MVVM binding policy fully operational** — CommunityToolkit-Mvvm selected, dispatcher interface finalised, forbidden-patterns table complete with enforcement mechanism.
- **State contract delivered to Sub-team 7** on Day 9 (DEPS-4 met). Interface contracts with Sub-team 1 confirmed Day 8 (DEPS-3 met — no rework).
- **15 NFRs + 2 full worked examples + 2 test strategy documents + 4 ADRs + design proposal** — complete design artefact set for the assessment pitch.
- **AI log**: continued through Sprint 2 — entries Days 6–10 logged in `ai-log.md`.

---

## 6. Integration touchpoints — updated

| Sub-team | What flowed | Status |
|---|---|---|
| **Sub-team 1 — Apaties I (Architecture)** | Day-8 integration review completed (DEPS-3). `IFitsService`, `IVolumeService`, `IFileDialogService`, `ILogStream` surfaces confirmed. No rework required against our skeleton. JSON-RPC method catalogue (ADR-0002 Appendix A) accepted. | **Closed** |
| **Sub-team 7 — Sewe en sestig (Persistence)** | State-shape contract (`architecture.md` §8) delivered Day 9 (DEPS-4). Format: JSON, field catalogue per ViewModel, versioning tag `"schema_v": 1`. | **Closed** |
| **Sub-team 4 — Koffiewinkel (Interaction)** | DEPS-2 risk (R04) still open — no active design conversation this sprint. `IInteractionService` shape is a Sprint-3 item. | **Open — Sprint 3** |
| **Sub-team 5 — Apaties II (Feature domain)** | `ISourceCatalogueService` shape surfaced in Sprint 1 (`CanvassDesktop.md`). No formal handshake yet — flagged for cross-sub-team window Sprint 3. | **Open — Sprint 3** |

---

## 7. Risks entering Sprint 3

1. **Post-refactor re-measurement (BNCH-1', BNCH-2')** — CK projections are estimates; actual NDepend + SonarQube run on refactored assembly owed Sprint 3. If actuals deviate materially from projections the CK delta worksheets need updating.
2. **`IInteractionService` + Sub-team 4** — no interface contract agreed. Risk of rework if their surface differs from our assumed shape. Mitigation: flag at Sprint-3 Day-11 cross-sub-team window.
3. **`ISourceCatalogueService` + Sub-team 5** — same pattern as above. Mitigation: same window.
4. **ObservableCollection vs ring buffer** — deferred to Sprint 3 pending perf data. If Debug-tab log volume causes UI-thread pressure, ring buffer alternative in binding policy §3 must be activated and binding policy updated.
5. **Skeleton promotion to full implementation** — 16 C# skeleton files (file-tab + debug-tab) are interfaces + stubs only. Sprint-3 implementation load is the heaviest remaining technical risk.

---

## 8. Sprint 3 forward look (headline only — full plan in `week3-sprint-plan.md`)

**Sprint 3 goal:** *Implement and verify.* Promote both worked-example skeletons to full implementations; run NDepend + SonarQube post-refactor to get actual CK deltas; achieve ≥ 70 % ViewModel + Gateway coverage under CI; close Sub-team 4 and Sub-team 5 interface contracts; finalise pitch deck.

**Role rotation for Sprint 3** (per §10.1 standard rotation):

| Sprint | SM | TL | POL | QC |
|---|---|---|---|---|
| Sprint 1 | Con | Mark | Jimmy | Rory |
| Sprint 2 | Jimmy | Con | Rory | Mark |
| **Sprint 3** | **Mark** | **Rory** | **Con** | **Jimmy** |

*(Confirm at Sprint 3 planning, Day 11.)*

---

## 9. Evidence index — one-click for the panel

| Artefact | Path |
|---|---|
| **Design proposal** | `D6-design-proposal/design-proposal.md` |
| Requirements doc | `docs/sub-team-6/requirements.md` |
| CanvassDesktop method reference + server-side I/O map | `D1-requirements/CanvassDesktop.md` |
| ADR-0001 MVVM split | `docs/sub-team-6/adrs/0001-mvvm-split.md` |
| ADR-0002 Transport (+ wire spec) | `D2-Architecture/architecture.md` §4 |
| ADR-0003 ACL | `D2-Architecture/architecture.md` §5 |
| ADR-0004 UI Toolkit | `D2-Architecture/architecture.md` §6 |
| State contract (Sub-team 7 handoff) | `D2-Architecture/architecture.md` §8 |
| MVVM binding policy (complete) | `D3-MVVM-binding-policy/mvvm-binding-policy.md` |
| File-tab BEFORE class diagram | `D4-worked-examples/ex1-file-tab/before-class-diagram.puml` |
| File-tab AFTER class diagram | `D4-worked-examples/ex1-file-tab/after-class-diagram.puml` |
| File-tab BEFORE dep graph | `D4-worked-examples/ex1-file-tab/before-dependency-graph.puml` |
| File-tab AFTER dep graph (CBO 47 → ~4) | `D4-worked-examples/ex1-file-tab/after-dependency-graph.puml` |
| File-tab BEFORE DSM | `D4-worked-examples/ex1-file-tab/before-dsm.md` |
| File-tab AFTER DSM + CK projection | `D4-worked-examples/ex1-file-tab/after-dsm.md` |
| File-tab sequence diagram | `D4-worked-examples/ex1-file-tab/file-tab-sequence-diagram.puml` |
| File-tab CK delta worksheet | `D4-worked-examples/ex1-file-tab/ck-delta-worksheet.md` |
| File-tab AFTER skeleton (10 .cs files) | `refactoring-examples/sub-team-6/file-tab/skeleton/` |
| Debug-tab BEFORE class diagram | `D4-worked-examples/ex2-debug-tab/before-class-diagram.puml` |
| Debug-tab AFTER class diagram | `D4-worked-examples/ex2-debug-tab/after-class-diagram.puml` |
| Debug-tab BEFORE dep graph | `D4-worked-examples/ex2-debug-tab/before-dependency-graph.puml` |
| Debug-tab AFTER dep graph | `D4-worked-examples/ex2-debug-tab/after-dependency-graph.puml` |
| Debug-tab BEFORE DSM | `D4-worked-examples/ex2-debug-tab/before-dsm.md` |
| Debug-tab AFTER DSM + CK projection | `D4-worked-examples/ex2-debug-tab/after-dsm.md` |
| Debug-tab AFTER sequence diagram | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| Debug-tab CK delta worksheet | `D4-worked-examples/ex2-debug-tab/ck-delta-worksheet.md` |
| Debug-tab AFTER skeleton (6 .cs files) | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| ViewModel unit-test strategy (TEST-1) | `D5-testing/viewmodel-unit-tests.md` |
| UI Toolkit page-object pattern (TEST-2) | `D5-testing/ui-toolkit.md` |
| CK baseline (8 classes) | `other/D9-ck-baseline/SK_BNCH.md` |
| SonarQube baseline (8 classes, Top-10 smells) | `other/D9-ck-baseline/SonarQube Baseline report.md` |
| CodeScene hotspot/churn report | `other/T2-baseline-benchmark/BNCH-3.md` |
| NDepend DSM report | `other/T2-baseline-benchmark/BNCH-4.md` |
| Mocking-difficulty count (30 classes) | `other/BNCH-6.md` |
| Concern map (PNG + source) | `docs/sub-team-6/concern-map.png` + `concern-map.puml` |
| Backlog (truth) | `docs/sub-team-6/backlog.md` + `backlog.csv` |
| Standups | `docs/sub-team-6/deliverables/Sprint-Documents/standups.md` |
| AI log | `docs/sub-team-6/ai-log.md` |
| Kanban snapshot | `Sprint-Documents/week2-snapshot.png` |
| Deliverables master tracker | `deliverables-checklist.md` |