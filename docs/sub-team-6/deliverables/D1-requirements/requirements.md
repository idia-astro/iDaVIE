# Sub-team 6 (Die Boks / Team Alpha) — Desktop GUI & Client Shell — Requirements (D1)

**Owner:** PO Liaison (Sprint 1) · **Revised:** 2026-05-26 (Day 7) · **Length target:** 2 pages.
**Feeds:** brief §6.6 (sub-team scope), §9.2.1 (1–2 page requirements deliverable), LO2 (NFR → testable architectural drivers).


## 1. Scope

The **Desktop GUI and Client Shell** as defined in brief §6.6: `Assets/Scripts/UI/CanvassDesktop.cs` (1899 LOC, single `MonoBehaviour`), file / mask loaders, parameter panels, debug consoles, and the client-side composition root that wires the GUI to the future server. The after-state must satisfy the four architectural non-negotiables in §4.2 (no SOLID/GRASP violation without an ADR trade-off; zero circular deps between top-level components; domain code free of transitive `UnityEngine` / `SteamVR` deps; every public boundary an interface with ≥ 1 test double). **Out of scope:** server-side service implementation (Sub-team 1), VR menus (Sub-team 4), native plug-in internals (Sub-team 2), volume rendering (Sub-team 3), feature/source domain model (Sub-team 5), persistence schema and lifecycle (Sub-team 7 — we only hand them ARQ-2's state contract).

## 2. REQ-1 — Current behaviour and coupling catalogue

Per-tab summary below; full per-control behaviour, state flows, VR-side touchpoints and the 13-row defect register live in `CurrentGUIStateDoc.md`.

| Tab | Behaviour today | Direct I/O → server-side? | Unity / native coupling | Highest-severity defect |
|---|---|---|---|---|
| **File** | FITS browse + load via CFITSIO P/Invoke, header parse, optional mask, conditional 4D-axis dropdown | **Yes** | `transform.Find` chains into scene; `data_analysis_tool.dll` P/Invoke; `FindObjectOfType<VolumeDataSetRenderer>` | **B-08** UI blocks for seconds during load (no cancel, no background thread); **B-06** mask-dimension errors surface only in Unity log |
| **Render** | Cube-size, colour map, min/max thresholds, rest-frequency dropdown — shared state with STATS and VR Settings | No | Material property-block writes; shared float fields on `CanvassDesktop` | **B-03** sliders don't refresh when VR Settings changes thresholds (stale until tab re-opened) |
| **Stats** | Histogram, percentile quick-select, σ overlays, min/max scale | No | Reads in-memory float buffer; shared state with RENDER | **B-04** exact-percentile freeze on large cubes (no progress indicator) |
| **Sources** | VOTable / FITS catalogue load, per-column mapping UI, save/restore mapping JSON | **Yes** | `FindObjectOfType<FeatureSetManager>`; native catalogue reader | **B-05** WCS vs. (x,y,z) one-voxel offset (open issue #464) |
| **Debug** | Scrollable Unity log readout, save log to `.txt` | No | Subscribes to `Application.logMessageReceived` on the main thread with no thread guard | **B-02 (CRITICAL)** tab switch during cube load → process crash, all session state lost |

**Direct-file-I/O verdict.** File and Sources are the two tabs whose parsing leaves the client under the §6.6 split. The two **mandated worked refactoring examples** (§6.6 → D4) are **File** (direct native-plugin call → ViewModel command via the service gateway) and **Debug** (passive log readout → Observer of a structured logging stream). Together they exercise the transport ADR (`D2-Architecture/architecture.md` §4.2, ADR-0002) on both paths — File for request/response RPC, Debug for the server-pushed stream — confirming the contract has a real consumer.

**Defect → NFR linkage.** The bug list proves the NFRs aren't a wish-list of nice-to-haves — they're the specific requirements without which the documented bugs can't be fixed. **B-02** (DEBUG-tab crash) is caused by binding the IMGUI log readout to `Application.logMessageReceived` while the main thread is blocked inside CFITSIO — directly motivating **NFR-MOD-1** (no cycles), **NFR-REU-3** (no Unity-thread coupling in ViewModel) and **NFR-TST-2** (ViewModel mockable without Unity). **B-03** (slider sync) motivates the MVVM binding policy (D3). **B-08** (UI freeze during load) motivates moving long I/O behind the service gateway (D2).

## 3. REQ-2 — ISO/IEC 25010 maintainability NFRs

Every NFR is testable: MoSCoW priority (M = must, S = should). All five ISO 25010 maintainability sub-characteristics are covered.

| NFR | Sub-char | Statement | Acceptance metric (tool) | Pri | Spec |
|---|---|---|---|---|---|
| **NFR-MOD-1** | Modularity | View, ViewModel and Service Gateway live in separate assemblies; no circular deps | Cycle count = 0 (NDepend / DV8 DSM) | M | §4.2.2; ADR-009 |
| **NFR-MOD-2** | Modularity | CBO ≤ 14 (domain), ≤ 25 (orchestrators) | Understand | M | §7.1 |
| **NFR-MOD-3** | Modularity | Instability I = Ce / (Ca + Ce) decreases View → ViewModel → Domain (Stable Dependencies Principle) | Per-package I (NDepend) | S | §7.2 |
| **NFR-REU-1** | Reusability | Every public boundary is an interface with ≥ 1 test double | Coverage + NDepend CQLinq | M | §4.2.4 |
| **NFR-REU-2** | Reusability | ISP (Interface Segregation Principle) — interfaces ≤ 7 public members | Audit table (BNCH-7) | M | §7.2 |
| **NFR-REU-3** | Reusability | ViewModel layer has zero transitive dependency on `UnityEngine` / `SteamVR` | NDepend "no Unity refs from `ViewModel.*`" on every PR | M | §4.2.3; ADR-009 |
| **NFR-ANA-1** | Analysability | WMC ≤ 20 (domain), ≤ 40 (adapter) | Understand | M | §7.1 |
| **NFR-ANA-2** | Analysability | Cognitive complexity per method ≤ 15 (SonarQube default) | SonarQube, every PR | M | §7.2 |
| **NFR-ANA-3** | Analysability | Duplicated lines ≤ 3 % of LOC (30-line block) | SonarQube duplication | S | §7.2 |
| **NFR-MOF-1** | Modifiability | A new desktop tab touches ≤ 3 production files outside `Views/<TabName>/` | Walkthrough + CodeScene change-coupling | S | §7.2 |
| **NFR-MOF-2** | Modifiability | DV8 propagation cost drops ≥ 30 % between Day 2 baseline and Day 13 projection | DV8 | M | §7 |
| **NFR-MOF-3** | Modifiability | After-state RFC ≤ 50 and LCOM ≤ 0.5 | Understand | M | §7.1 |
| **NFR-TST-1** | Testability | ViewModel branch + line coverage ≥ 70 %; overall ≥ 50 %; Unity-bound tracked-not-strict | SonarQube coverage | M | §7.2; ADR-009 |
| **NFR-TST-2** | Testability | Mocking difficulty — static / Unity-API call count per ViewModel class = 0 | Custom NDepend rule | M | §7.2; ADR-009 |
| **NFR-TST-3** | Testability | DIT ≤ 4 and NOC ≤ 5 (keep mocking surface small) | Understand | S | §7.1 |

The four **§4.2 architectural non-negotiables** are enforced by this table end-to-end: no SOLID/GRASP violation without ADR (every row), no cycles (MOD-1), no Unity in domain code (REU-3), interface + test double on every public boundary (REU-1). Plug-in C ABI semver is owned by Sub-team 1; our service gateway consumes the major version it declares.

## 4. REQ-3 — Long-term roadmap drivers as architectural requirements

The two roadmap features named in §6.6 (Python console; workspace save) are translated into **architectural drivers**, not features — the assignment is a design proposal, not a code change. Detail in `Long-Term-Python-Console.md`.

- **ARQ-1 — Python console (long term).** The ViewModel layer must be invokable from a non-Unity process. *Acceptance:* every ViewModel command surface is a pure-C# method on an interface; no constructor parameter is a Unity type. *Evidence:* the **NFR-REU-3** rule is necessary and sufficient — no extra check needed.
- **ARQ-2 — Workspace / state saving (long term).** Desktop-shell state must be expressible as a serialisable DTO containing no Unity types. *Acceptance:* a `DesktopShellState` record round-trips through JSON without reflection on Unity types. *Deliverable:* state contract handed to Sub-team 7 before the architecture freeze (Thu 4 June 11:00).

## 5. Traceability (LO2 evidence)

| Requirements | ISO 25010 sub-char | Tool(s) | CI / sprint enforcement | Reported in |
|---|---|---|---|---|
| NFR-MOD-1…3 | Modularity | NDepend, Understand, DV8 | Architecture rules on PR; sprint-boundary snapshot | T7; T4 |
| NFR-REU-1…3 | Reusability | NDepend, ISP audit | Architecture rules on PR | D4 worked examples; D5 test strategy |
| NFR-ANA-1…3 | Analysability | Understand, SonarQube | Quality gates on PR | T7; T2 baseline + projection |
| NFR-MOF-1…3 | Modifiability | CodeScene, DV8, Understand | Sprint review | T7; T4 |
| NFR-TST-1…3 | Testability | SonarQube, NDepend, Understand | Coverage gate on PR | D5 test strategy; T7 |
| ARQ-1, ARQ-2 | (architectural driver) | NDepend rule; JSON round-trip test | Rule on PR; contract review with Sub-team 7 | D4 worked examples; D2 architecture |
