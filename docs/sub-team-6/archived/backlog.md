# Sub-team 6 — Backlog (v2)

**Cohort:** Team Alpha, Sub-team 5 "Die Boks" (work package = §6.6, Desktop GUI & Client Shell).

This backlog is the single source of truth for our sub-team's work across the three sprints of the iDaVIE refactoring assignment. Every card here maps to a graded artefact: a **pitch slot** (App C), a **team-level deliverable T1–T8** (§9.1), a **sub-team deliverable** (§9.2), or an **interview defence item** (§8.4).

A machine-readable version is in [`backlog.csv`](backlog.csv) — import that into the Kanban board.

## Conventions

- **ID prefix** = category. `BNCH`=benchmarking, `REQ`=requirements, `ARCH`=architecture, `DESN`=design, `WE1/WE2`=worked examples 1/2, `TEST`=testability, `DEPS`=dependencies, `MGMT`=management, `AI`=AI tool log, `PITCH`=pitch artefacts, `IND`=individual reflections.
- **Size:** S = ½ day · M = 1 day · L = 2 days · XL = 3+ days.
- **Owner role:** SM (Scrum Master) / TL (Tech Lead) / POL (PO Liaison) / QC (Quality Champion). Sprint 1 rotation per §10.1 — adjust to names in `README.md`.

## Definition of Done (every item)

1. Artefact exists at its stated repo path.
2. Peer-reviewed by at least one other sub-team member.
3. Linked from `docs/sub-team-6/README.md`.
4. Any cross-sub-team dependency it raises is on the integration risk register.

---

## Part A — Product Backlog (all sprints)

### Benchmark / metrics — feeds **T2**, **T7**, **pitch pain-points slot**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| BNCH-1 | CK suite baseline for `Assets/Scripts/UI/` + `Assets/Scripts/Menu/` | M | 1 | T2, pitch 1 |
| BNCH-2 | SonarQube baseline (cyclomatic + cognitive complexity, smells, duplication, MI, tech debt) | M | 1 | T2, pitch 1 |
| BNCH-3 | CodeScene hotspot + churn for our slice | S | 1 | T2, pitch 1 |
| BNCH-4 | DV8 / NDepend DSM + propagation cost for our slice | M | 1 | T2 modularity |
| BNCH-5 | Day-10 projected post-refactor CK + ISO 25010 metrics | M | 2 | T7, pitch 3 |
| BNCH-6 | Mocking-difficulty count (static + Unity API calls per class) | S | 1 | T2 testability |
| BNCH-7 | Interface-size audit (ISP ≤ 7) on proposed interfaces | S | 2 | T2 testability |

### Requirements — feeds **sub-team req doc**, **T4**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| REQ-1 | Catalogue current GUI tab behaviours (File/Render/Stats/Sources/Debug); flag direct file I/O | M | 1 | Req doc, T4 |
| REQ-2 | NFRs traced to ISO 25010 maintainability sub-chars — testable architectural drivers | M | 1 | Req doc, T4, LO2 |
| REQ-3 | Long-term roadmap drivers: Python console, workspace/state saving | S | 1 | Req doc, T4 |
| REQ-4 | Sub-team requirements document consolidated (1–2 pp) | M | 1 | §9.2.1 deliverable |

### Architecture — feeds **T3**, **T4**, **pitch C4 slot**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| ARCH-1 | ADR-01: MVVM split (Nygard format) | M | 1 | T3, T4 |
| ARCH-2 | ADR-02: Transport — JSON-RPC + gRPC | M | 1 | T3, T4 |
| ARCH-3 | C4 Level 1 (Context) — our portion | S | 2 | T3, pitch 2 |
| ARCH-4 | C4 Level 2 (Container) — desktop client | M | 2 | T3, pitch 2 |
| ARCH-5 | C4 Level 3 (Component) — inside desktop client | M | 2 | T3, pitch 2 |
| ARCH-6 | ADR-03: Anti-corruption layer around Unity 6 | S | 2 | T3, T4 |
| ARCH-7 | ADR-04: UI Toolkit View + Unity 6 migration plan | M | 2 | T3, T4, pitch 4 |
| ARCH-8 | Interface contracts proposed to Sub-team 1 (`IServiceGateway`, `IFileTabViewModel`, `ILogStream`, `IPanel`) | M | 1 | Sub-team 1 dep, T4 |
| ARCH-9 | MVVM binding policy (1–2 pp) | M | 2 | §6.6 deliverable |
| ARCH-10 | **Desktop client architecture document (5–10 pp)** | XL | 2 | §6.6 deliverable, T3, T4 |
| ARCH-11 | **State contract to Sub-team 7 (Persistence)** | M | 2 | §8.2 Day 9 exit criterion, T7 |

### Design — feeds **T4**, **interview defence**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| DESN-1 | Concern-map of CanvassDesktop (whiteboard, photographed) | S | 1 | DESN-2, WE1 |
| DESN-2 | SRP audit of CanvassDesktop showing four concerns separated | M | 2 | T4, LO4 |

### Worked example 1 — File tab — feeds **pitch worked-examples slot**, **T4**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| WE1-1 | File tab BEFORE: UML class diagram | M | 2 | T4, pitch 3 |
| WE1-2 | File tab BEFORE: dependency graph + DSM excerpt | S | 2 | T4 modularity |
| WE1-3 | File tab AFTER: UML class diagram (MVVM + Gateway) | M | 2 | T4, pitch 3 |
| WE1-4 | File tab AFTER: sequence diagram "load FITS cube" | M | 2 | T4, pitch 3 |
| WE1-5 | File tab AFTER: C# skeleton (compiles, no Unity in VM) | L | 2 | T4, LO4, LO5 |
| WE1-6 | File tab CK + ISO 25010 delta worksheet | M | 2 | T4, T7, pitch 3 |

### Worked example 2 — Debug tab — feeds **pitch worked-examples slot**, **T4**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| WE2-1 | Debug tab BEFORE: UML class diagram | S | 2 | T4, pitch 3 |
| WE2-2 | Debug tab AFTER: UML showing Observer of `IStructuredLogStream` | M | 2 | T4, pitch 3 |
| WE2-3 | Debug tab AFTER: sequence diagram "log line → desktop tab" | S | 2 | T4 |
| WE2-4 | Debug tab AFTER: C# skeleton (compiles in isolation) | M | 2 | T4, LO5 |
| WE2-5 | Debug tab CK + ISO 25010 delta worksheet | S | 2 | T4, T7, pitch 3 |

### Testability — feeds **sub-team test strategy**, **pitch testability slot**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| TEST-1 | ViewModel unit-test strategy (NUnit + Moq, no Unity, ≥ 70 %) | M | 2 | §9.2.4, pitch 4, LO6 |
| TEST-2 | UI Toolkit page-object pattern spec | M | 2 | §9.2.4, LO6 |
| TEST-3 | Worked test spec for `OpenCubeCommand` | M | 2 | T4, pitch 4 |
| TEST-4 | **Sub-team test strategy document (2–4 pp)** | M | 2 | §9.2.4 deliverable |

### Cross-sub-team coordination — feeds **T7**, **integration risk register**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| DEPS-1 | Raise gateway dependency on integration risk register (Sub-team 1) | S | 1 | T7 |
| DEPS-2 | Raise VR-side menu coord (Sub-team 4) | S | 1 | T7 |
| DEPS-3 | Day-8 cross-sub-team integration review attendance + reconciliation | S | 2 | §8.2 Day 8 |
| DEPS-4 | Day-9 hand state contract to Sub-team 7 | S | 2 | §8.2 Day 9 exit |

### Process / management — feeds **§9.2 deliverables**, **LO7**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| MGMT-1 | Sub-team Kanban board set up | S | 1 | §9.2.5 |
| MGMT-2 | Daily stand-up notes file | S | 1 | §9.2.6 |
| MGMT-3 | Sprint 1 role assignments documented | S | 1 | §10.1 |
| MGMT-4 | Sprint 1 review slides (Day 5, 15 min) | S | 1 | §8.1, LO7 |
| MGMT-5 | Sprint 1 Kanban snapshot + retro notes | S | 1 | §9.2.5 |
| MGMT-6 | Sprint 2 review slides + mid-assessment visit material (Day 10) | M | 2 | §8.2 Day 10, T5 dry-run |
| MGMT-7 | Sprint 2 Kanban snapshot + retro notes | S | 2 | §9.2.5 |
| MGMT-8 | Sprint 3 Kanban snapshot + sub-team retro | S | 3 | §10.2 |

### AI usage — feeds **T8**, **LO8**

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| AI-1 | Rolling AI prompt log (ongoing) | S × ongoing | 1+2+3 | T8 |

### Sprint 3 — Pitch and reflections

| ID | Item | Size | Sprint | Feeds |
|---|---|---|---|---|
| PITCH-1 | Trade-off analysis: MVVM vs MVP vs MVU; JSON-RPC vs gRPC; UI Toolkit vs IMGUI | M | 3 | §8.3 Day 11, pitch 5 |
| PITCH-2 | Final pitch slides for our sub-team's content | M | 3 | T5 |
| PITCH-3 | Address mid-assessment maintainer feedback | M | 3 | T4 |
| IND-1 | Per-student 2-page reflection — student A | S | 3 | §9.3 |
| IND-2 | Per-student 2-page reflection — student B | S | 3 | §9.3 |
| IND-3 | Per-student 2-page reflection — student C | S | 3 | §9.3 |
| IND-4 | Per-student 2-page reflection — student D | S | 3 | §9.3 |
| IND-5 | Peer-rated contribution table — student A | S | 3 | §9.3 |
| IND-6 | Peer-rated contribution table — student B | S | 3 | §9.3 |
| IND-7 | Peer-rated contribution table — student C | S | 3 | §9.3 |
| IND-8 | Peer-rated contribution table — student D | S | 3 | §9.3 |

---

## Part B — Sprint 1 Backlog (Days 2–5, Tue 19 – Fri 22 May 2026)

**Sprint goal:** Diagnose the current state. By Fri EOD we have baseline numbers for our slice, a requirements doc draft, two ADRs, and the gateway interface contract is in Sub-team 1's hands.

**Sprint 1 exit criteria:**

- BNCH-1..4, BNCH-6 — baseline metrics committed.
- REQ-4 — requirements doc draft committed.
- ARCH-1, ARCH-2, ARCH-8 — two ADRs + interface contracts proposed.
- DESN-1 — concern-map photographed + committed.
- MGMT-4, MGMT-5 — Sprint 1 review presented; Kanban snapshot + retro saved.

| ID | Owner | Days | Size |
|---|---|---|---|
| MGMT-1 | SM | 2 AM | S |
| MGMT-2 | SM | 2 AM | S |
| MGMT-3 | SM | 2 AM | S |
| BNCH-1 | QC | 2–3 | M |
| BNCH-2 | QC | 2–3 | M |
| BNCH-3 | QC | 3 | S |
| BNCH-4 | QC | 4 | M |
| BNCH-6 | QC | 4 | S |
| DESN-1 | TL + all | 2 PM | S |
| REQ-1 | POL | 3 | M |
| REQ-2 | POL | 3–4 | M |
| REQ-3 | POL | 4 | S |
| REQ-4 | POL | 4–5 | M |
| ARCH-1 | TL | 3–4 | M |
| ARCH-2 | TL | 4 | M |
| ARCH-8 | TL | 4–5 | M |
| DEPS-1 | SM | 2 | S |
| DEPS-2 | SM | 2 | S |
| MGMT-4 | SM | 5 AM | S |
| MGMT-5 | SM | 5 PM | S |
| AI-1 | all | 2–5 | S |

**21 cards.** ~75 % of capacity — slack is deliberate for discovery sprint.

---

## Part C — Sprint 2 Backlog (Days 6–10, Mon 25 – Fri 29 May 2026)

**Sprint goal:** Produce the design proposal in full. Both worked examples complete with CK deltas. Architecture document (5–10 pp). Test strategy (2–4 pp). State contract to Sub-team 7 by Day 9. Day-10 projected metrics. This sprint **is** the content of the pitch.

**Sprint 2 exit criteria:**

- WE1-1..6 and WE2-1..5 — both worked examples complete.
- ARCH-3..7, ARCH-9, ARCH-10 — C4 diagrams, ADRs 03/04, binding policy, architecture document.
- ARCH-11 + DEPS-4 — state contract handed to Sub-team 7 by Day 9.
- TEST-1..4 — test strategy document.
- BNCH-5, BNCH-7 — projected metrics + ISP audit.
- DEPS-3 — integration review attended.
- MGMT-6, MGMT-7 — Sprint 2 review + Kanban snapshot + retro.
- Mid-assessment maintainer visit (30 min) handled.

| ID | Owner | Days | Size |
|---|---|---|---|
| ARCH-3 | TL | 6 | S |
| ARCH-4 | TL | 6 | M |
| ARCH-5 | TL | 6–7 | M |
| ARCH-6 | TL | 6 | S |
| ARCH-7 | TL | 7 | M |
| ARCH-9 | TL | 7 | M |
| DESN-2 | TL | 7 | M |
| WE1-1 | TL | 7 | M |
| WE1-2 | QC | 7 | S |
| WE1-3 | TL | 7–8 | M |
| WE1-4 | TL | 8 | M |
| WE1-5 | TL + 1 | 7–8 | L |
| WE1-6 | QC | 8 | M |
| DEPS-3 | TL + all | 8 | S |
| WE2-1 | POL | 8 | S |
| WE2-2 | POL | 8–9 | M |
| WE2-3 | POL | 9 | S |
| WE2-4 | POL + 1 | 9 | M |
| WE2-5 | QC | 9 | S |
| ARCH-11 | TL | 8–9 | M |
| DEPS-4 | TL | 9 | S |
| TEST-1 | QC | 8 | M |
| TEST-2 | QC | 9 | M |
| TEST-3 | QC | 9 | M |
| TEST-4 | QC | 10 | M |
| BNCH-5 | QC | 10 | M |
| BNCH-7 | QC | 9–10 | S |
| ARCH-10 | TL | 9–10 | XL |
| MGMT-6 | SM | 10 AM | M |
| MGMT-7 | SM | 10 PM | S |
| AI-1 | all | 6–10 | S |

**31 cards.** This is the heavy sprint — protect it.

---

## Part D — Sprint 3 (Days 11–13, Mon 1 – Wed 3 Jun 2026)

**Sprint goal:** Polish. Trade-off analysis. Pitch rehearsal. Individual reflections.

| ID | Owner | Days |
|---|---|---|
| PITCH-3 | TL + POL | 11 |
| PITCH-1 | TL | 11 |
| PITCH-2 | SM + TL | 12 |
| MGMT-8 | SM | 12 PM |
| IND-1..4 | each student | 11–13 |
| IND-5..8 | each student | 11–13 |
| AI-1 | all | ongoing |

**Artefact freeze: Thu 4 June 11:00.**

---

## Pitch slot → backlog item map (work backwards from this)

| Pitch slot (40 min) | Slide content | Backlog IDs |
|---|---|---|
| 4 min pain points + metrics | CanvassDesktop CK row; SonarQube cognitive complexity; CodeScene hotspot | BNCH-1, BNCH-2, BNCH-3 |
| 10 min target architecture (C4) | C1 Context, C2 Container, C3 Component | ARCH-3, ARCH-4, ARCH-5 |
| 12 min worked examples + before/after CK | File tab + Debug tab before→after; CK delta tables | WE1-1..6, WE2-1..5 |
| 6 min testability + Unity 6 migration | ViewModel test pattern + UI Toolkit migration | TEST-1, TEST-2, TEST-3, ARCH-7 |
| 5 min trade-offs + risk | MVVM vs MVP; JSON-RPC vs gRPC; UI Toolkit vs IMGUI | PITCH-1 |
| 3 min summary | Summary slide only | PITCH-2 |

## LO + SWEBOK coverage

| LO | Covered by |
|---|---|
| LO1 — CK + static analysis benchmark | BNCH-1..4 |
| LO2 — NFRs traced to ISO 25010 | REQ-1, REQ-2, REQ-3 |
| LO3 — Client–server + micro-kernel architecture | ARCH-1..6 |
| LO4 — SOLID + GRASP at class/component | DESN-1, DESN-2, WE1-3, WE2-3 |
| LO5 — Unity 5 → Unity 6 refactor with UML/dep graphs/CK | WE1-1..6, WE2-1..5, ARCH-7 |
| LO6 — Testability strategy | TEST-1..4 |
| LO7 — Scrum-of-Scrums operation | MGMT-1..8 |
| LO8 — AI tool use, defensible authorship | AI-1 + authorship metadata on every doc |
| LO9 — Defend in pitch + interview | PITCH-1, PITCH-2 |

| SWEBOK competency (§6 intro) | Covered by |
|---|---|
| Requirements engineering | REQ-1..4 |
| Software architecture | ARCH-1..11 |
| Software design | DESN-1, DESN-2 |
| Software construction (vicarious) | WE1-1..6, WE2-1..5 |
| Software testing | TEST-1..4 |
| Software engineering management | MGMT-1..8 |
