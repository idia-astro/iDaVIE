# Sub-team 6 (Die Boks / Team Alpha) — Desktop GUI & Client Shell — Requirements (D1)

**Owner:** PO Liaison (Sprint 1).
**Last revised:** 2026-05-21 (Day 4).
**Feeds:** brief §6.6 (sub-team scope), §9.2.1 (sub-team requirements deliverable), LO2 (NFR → testable architectural drivers); contributes to **T4** consolidated proposal and **T7** integration & metrics report.
**Length target:** 1–2 pages of body. Supporting depth lives in `deliverables/D1-requirements/` (CanvassDesktop method reference, long-term roadmap brief).

## 1. Scope

The **Desktop GUI and Client Shell**: `Assets/Scripts/UI/CanvassDesktop.cs` (~1900 LOC, single `MonoBehaviour`), file/mask loaders, parameter panels, debug consoles, and the client-side composition root that will wire the GUI to the future server. The behavioural element owned is the one named in brief §6.6. After-state must satisfy the four architectural non-negotiables in §4.2 (no SOLID/GRASP violations without ADR trade-off; zero circular deps; domain code free of `UnityEngine`/`SteamVR`; every public boundary an interface with ≥ 1 test double).

## 2. Current behaviour & coupling catalogue (REQ-1)

Each row captures observable behaviour, whether the tab performs **direct file I/O that the brief expects to move server-side** (§6.6), and the Unity/native coupling that motivates the after-state NFRs.

| Tab | Behaviour today | Direct file I/O → server-side? | Unity / SteamVR coupling visible | Native-plugin call sites |
|---|---|---|---|---|
| **File** | Browse + load FITS / HDF5 volumes; HDU selection; subset definition; image + mask overlay apply | **Yes — moves to server.** Interim path: service gateway (ADR-0002) returns parsed DTO; local-mode pipe targets in-process shim | `transform.Find("…/…")` chains into scene hierarchy; `FindObjectOfType<VolumeDataSetRenderer>` | `VolumeCommandController.LoadFitsFile`, native FITS reader |
| **Render** | Colour map, transfer-function thresholds, rest-frequency dropdown, histogram-based brightness | No | Slider bindings to renderer GameObject; coroutine lifecycles | Indirect via `VolumeDataSetRenderer` |
| **Stats** | Min / max / mean / σ, sigma clipping, percentiles — read-only summary | No | Reads renderer state | None |
| **Sources** | Load + display catalogue overlays, sky coords, visibility toggles | **Yes — moves to server.** Catalogue parsing belongs server-side | `FindObjectOfType<FeatureSetManager>` | Native catalogue reader |
| **Debug** | Tool-state read-out, OnGUI popup toggles, internal-state log, performance counters | No | Legacy `OnGUI` IMGUI; static logger access | None |

**Direct-file-I/O verdict.** File and Sources are the two tabs whose parsing the client should not own once the client–server split is in place. They drive the worked refactoring example (File tab) and confirm the transport ADR's pipe contract has a real consumer.

## 3. ISO/IEC 25010 maintainability NFRs (REQ-2)

Every NFR below is **testable** — each acceptance criterion is a number a Quality-Guild tool already produces. Thresholds: §7.1 (CK), §7.2 (sub-characteristic families), §4.2 (non-negotiables). Priority is MoSCoW (M = must, S = should). Coverage of the five ISO/IEC 25010 maintainability sub-characteristics (Appendix A) is complete.

| NFR ID | Sub-char | Statement | Acceptance metric (tool) | Priority | Spec § |
|---|---|---|---|---|---|
| **NFR-MOD-1** | Modularity | View, ViewModel and Service Gateway live in separate namespaces / assemblies with no circular dependencies. | Cycle count = 0 (NDepend or DV8 DSM) | M | §4.2.2 |
| **NFR-MOD-2** | Modularity | Each domain class in our slice has CBO ≤ 14; orchestrators ≤ 25. | CBO from Understand | M | §7.1 |
| **NFR-MOD-3** | Modularity | Instability I = Ce/(Ca+Ce) decreases monotonically View → ViewModel → Domain (Stable Dependencies Principle). | Per-package I (NDepend) | S | §7.2 |
| **NFR-REU-1** | Reusability | Every public API boundary is expressed as an interface with ≥ 1 test double. | Coverage report + NDepend CQLinq "public type without interface" rule | M | §4.2.4 |
| **NFR-REU-2** | Reusability | Interfaces obey ISP — ≤ 7 public members. | Audit table (BNCH-7) | M | §7.2 |
| **NFR-REU-3** | Reusability | ViewModel layer has zero transitive dependency on `UnityEngine` or `SteamVR`. | NDepend "no Unity refs from `ViewModel.*`" rule, every PR | M | §4.2.3 |
| **NFR-ANA-1** | Analysability | No class in our slice exceeds WMC 20 (domain) or 40 (adapter). | WMC from Understand | M | §7.1 |
| **NFR-ANA-2** | Analysability | Cognitive complexity per method ≤ 15 (SonarQube default). | SonarQube, every PR | M | §7.2 |
| **NFR-ANA-3** | Analysability | Duplicated lines ≤ 3 % of LOC in our slice; 30-line block threshold. | SonarQube duplication | S | §7.2 |
| **NFR-MOF-1** | Modifiability | Adding a new desktop tab touches ≤ 3 production files outside `Views/<TabName>/`. | Walkthrough on representative change scenario; CodeScene change-coupling | S | §7.2 |
| **NFR-MOF-2** | Modifiability | Propagation cost on our slice drops ≥ 30 % between Day 2 baseline and Day 13 projection. | DV8 propagation cost | M | §7 |
| **NFR-MOF-3** | Modifiability | After-state classes satisfy RFC ≤ 50 and LCOM ≤ 0.5. | Understand | M | §7.1 |
| **NFR-TST-1** | Testability | ViewModel branch + line coverage ≥ 70 %; overall ≥ 50 %; Unity-bound tracked-not-strict. | SonarQube coverage | M | §7.2 |
| **NFR-TST-2** | Testability | Mocking difficulty: static / Unity API call count per ViewModel class = 0. | Custom NDepend rule | M | §7.2 |
| **NFR-TST-3** | Testability | DIT ≤ 4 and NOC ≤ 5 (keep mocking surface small). | Understand | S | §7.1 |

## 4. Roadmap drivers as architectural requirements (REQ-3)

The two roadmap features named in §6.6 are translated into **testable architectural drivers** — not features — because the assignment is a design proposal, not a code change.

- **ARQ-1 — Python console (long term).** The ViewModel layer must be invokable from a non-Unity process. *Acceptance:* every ViewModel command surface is a pure-C# method on an interface; no constructor parameter is a Unity type. *Evidence:* the NFR-REU-3 rule is necessary-and-sufficient.
- **ARQ-2 — Workspace / state saving (long term).** Desktop-shell state must be expressible as a serialisable DTO containing no Unity types. *Acceptance:* a `DesktopShellState` record round-trips through JSON without reflection on Unity types. *Deliverable:* the state contract is handed to Sub-team 7 (Persistence) by Day 9 — see D13.

## 5. Architectural non-negotiables (§4.2 mapped to our slice)

- No SOLID / GRASP violation without a documented ADR trade-off — covered across all NFR families.
- Zero circular dependencies — **NFR-MOD-1**.
- Domain code does not transitively depend on `UnityEngine` / `SteamVR` — **NFR-REU-3**.
- Every public API boundary is an interface with ≥ 1 test double — **NFR-REU-1**.
- Plug-in C ABI semver — Sub-team 1 owns; our service gateway must consume the major version it declares.

## 6. Out of scope

Server-side service implementation (Sub-team 1) · VR-side menus (Sub-team 4) · native plug-in internals (Sub-team 2) · volume rendering (Sub-team 3) · feature / source domain model (Sub-team 5) · persistence schema and lifecycle (Sub-team 7 — we only hand them ARQ-2's state contract).

## 7. Traceability matrix (LO2 evidence)

| Requirements | ISO 25010 sub-char | Tool(s) | CI / sprint enforcement point | Where measured / reported |
|---|---|---|---|---|
| NFR-MOD-1 … MOD-3 | Modularity | NDepend, Understand, DV8 | Architecture rules on PR; sprint-boundary snapshot | D9 metric report; T7 |
| NFR-REU-1 … REU-3 | Reusability | NDepend, ISP audit | Architecture rules on PR | D4 worked examples; D5 test strategy |
| NFR-ANA-1 … ANA-3 | Analysability | Understand, SonarQube | Quality gates on PR | D9; T2 baseline + projection |
| NFR-MOF-1 … MOF-3 | Modifiability | CodeScene, DV8, Understand | Sprint review | D9; T4 |
| NFR-TST-1 … TST-3 | Testability | SonarQube, NDepend, Understand | Coverage gate on PR | D5 test strategy; D9 |
| ARQ-1, ARQ-2 | (architectural driver) | NDepend rule; JSON round-trip test | Rule on PR; contract review with Sub-team 7 (Day 9) | D4 worked examples; D13 state contract |

---

**Superseded inputs:** `deliverables/D7-deliverables/archive-superseded/REQ2_NFR_Maintainability_Table.md` (generic NFR-M1…M10 set retired 2026-05-21 — not traceable to our slice or tool chain).
