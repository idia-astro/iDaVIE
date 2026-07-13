# T5 — Pitch Spine (Sub-team 6 / Team Alpha — Desktop GUI & Client Shell)

> **Status:** working spine — drafted 2026-05-22 (Day 5). Not slides yet. This document defines, for every minute of the 40-min pitch, the **claim** we make, the **theory** that backs it, the **evidence** we point at, and the **risk** the panel could press on. Where evidence is missing we flag it `[EVIDENCE-GAP]` — those are the holes we backfill next.
>
> **Audience:** iDaVIE maintainer panel (Section 8.5). They will probe design decisions, not implementations. Section 10.5 #4 says **inability to defend a slide is a fail signal**; every speaker note below is written so any of the four of us can defend it cold.
>
> **Scope of our slot:** the assignment pitch is **team-wide** (40 min Thu 4 June 11:00–12:00). This spine is **only our slice**. The 4 / 10 / 12 / 6 / 5 / 3-min budget is the team-wide structure; our slice contributes to each section as flagged in the deliverables checklist §1.1. The 12 minutes of worked examples is the moment we are unambiguously on stage. We co-author the architecture, testability, trade-offs and summary sections with the other sub-teams.
>
> **Conventions in the spine:** every slide block lists the design driver (the *why*), not the operation (the *what*). When two patterns disagree we name both and say why we picked one. When the audience can interrupt and ask "but isn't this just X?" — the **Risk if challenged** field is the answer rehearsed.

---

## 0. Narrative arc (memorise this before reading slides)

The story is one sentence:

> *The desktop client today violates all four §4.2 non-negotiables because one class owns every concern; the MVVM split with a service gateway is the smallest change that makes each non-negotiable satisfiable, and the CK numbers prove it.*

Everything in the deck is in service of that sentence. Trim ruthlessly: if a slide does not advance "the four non-negotiables now hold," it does not belong.

The four non-negotiables (§4.2) are the spine of the spine:

1. No SOLID/GRASP violation without a documented trade-off.
2. Zero circular dependencies.
3. Domain code must not transitively depend on `UnityEngine` / `SteamVR`.
4. Every public API boundary is an interface with at least one test double.

We map every architectural choice back to one of these. The panel cannot disagree with the constraints — they wrote them.

---

## Section 1 — Pain points with metrics (4 min, ~3–4 slides)

**Section goal:** earn the right to propose anything by showing the *structural* unliveability of the current code. Not "ugly", not "long" — **structurally hostile to every quality attribute in ISO/IEC 25010 maintainability**.

**Our contribution to the team-wide pain section:** the **desktop client slice**. Other sub-teams own their own pain. We bring the worst single class in the repo.

---

### Slide 1.2 — `CanvassDesktop.cs` is a textbook God Class (~75 sec)

**Claim:** A single `MonoBehaviour` of 1 899 lines owns FITS I/O, HDU parsing, histogram maths, colour maps, subset bounds, source mapping, paint-mode wiring, statistics, threshold controls, and configuration. That is not a class.

**Why this matters:** this single fact is what makes every quality attribute red. Until the class is split, no other change matters. The God Class is *not* a smell; it is the cause of every other smell on this slice.

**Theory anchor:**
- **SRP (Single Responsibility Principle).** "A class should have one reason to change." `CanvassDesktop` has at least nine. Robert Martin's framing — a class belongs to a single *actor*. Ours belongs to the file-loading actor, the rendering actor, the QA actor, the persistence actor and several others.
- **GRASP — High Cohesion / Low Coupling.** Henderson-Sellers LCOM of **0.955** is the operational proof that cohesion has collapsed: 63 methods touch 67 fields with only 189 total field-method accesses; methods operate on disjoint slices of the field set.
- **Fowler — Long Method, Large Class.** Refactoring catalogue.

**Evidence (real numbers, all from `SK_BNCH.md` and `SonarQube Baseline report.md`):**

| Metric | Measured | §7.1 threshold | Status |
|---|---|---|---|
| LOC | 1 899 | — | n/a |
| WMC | **63** | ≤ 40 (orchestrator) | violation +23 |
| CBO | **47** | ≤ 25 (orchestrator) | violation +22 |
| RFC | **118** | ≤ 50 | violation +68 |
| LCOM_HS | **0.955** | ≤ 0.50 | violation |
| Cyclomatic complexity (max method `checkSubsetBounds`) | **31** | ≤ 15 (SonarQube default) | violation |
| Dead fields | `_restFrequency`, `inPaintMode`, `_tabsManager` | 0 | violation |
| SonarQube maintainability rating | **D** | A | violation |

**Speaker note:** read the row that hurts most: "this one class is coupled to forty-seven other types — twenty-three project classes, thirteen Unity / TMPro types, seven System types, four Valve.VR types — and three of its own fields are declared but never accessed." Pause. The audience needs that pause.

**Risk if challenged — "isn't CBO=47 unfair on a Unity MonoBehaviour?":** the threshold for orchestrators is 25, not 14; we are already grading on a curve. Even the orchestrator curve is broken almost 2×.

---

### Slide 1.3 — The four non-negotiables already fail today (~60 sec)

**Claim:** Every architectural non-negotiable in §4.2 is *already* violated by the current desktop client. We are not improving good code; we are removing fail-state code.

**Why this matters:** §4.2 is the panel's contract with us. If we cannot point at four boxes and tick them in the after-state, we have failed the assignment regardless of how clever the design is.

**Theory anchor:** §4.2 is the assignment's encoding of:
- **DIP / SDP (Robert Martin — Stable Dependencies Principle).** Forbidding domain dependence on Unity is the Dependency-Inversion / Stable-Dependencies idea at the assembly level.
- **GRASP — Pure Fabrication + Indirection.** The reason interfaces with test doubles are mandatory is that they are the *seams* that make every other property testable.
- **Lakos — Levelisation.** Zero circular dependencies is a precondition for incremental build, parallel work, and reasoning.

**Evidence (current state vs §4.2):**

| §4.2 non-negotiable | Current state | Verdict |
|---|---|---|
| 1. No SOLID/GRASP violations | SRP, OCP, DIP all broken; LCOM 0.955 = cohesion collapsed | **FAIL** |
| 2. Zero circular dependencies | DV8 / NDepend not yet run end-to-end at slice level [EVIDENCE-GAP-1.3a]; `HistogramHelper → CanvassDesktop → HistogramMenuController → HistogramHelper` strongly suspected from SonarQube Rank 10 finding | **suspected fail** |
| 3. Domain code free of `UnityEngine`/`SteamVR` | `CanvassDesktop` directly imports `UnityEngine`, `TMPro`, `Valve.VR`; no domain isolation exists | **FAIL** |
| 4. Every public API has interface + test double | Zero interfaces on `CanvassDesktop` public surface | **FAIL** |

**Speaker note:** "three boxes are unambiguous fails. Box two is a fail pending DV8 — we will have the cycle report by Day 10. The point is not the audit; the point is that *no incremental fix* satisfies box three or box four. The only path is a split."

**Risk if challenged — "you don't have the cycle report":** acknowledge openly; commit to Day 10. The other three non-negotiables already justify the change.

**[EVIDENCE-GAP-1.3a]** DV8 / NDepend cycle report for our 8-class slice → [`../other/cycles-report.md`](../other/cycles-report.md). **Partially closed 2026-05-28:** after-state *assembly-level* acyclicity is now tool-backed (clean `dotnet build` of all 10 pure-C# projects — MSBuild rejects cyclic `<ProjectReference>` graphs) plus the documented reference graph. Still owed: class-level DV8/NDepend confirmation on the after-state and tool confirmation of the 2 manually-documented before-state cycles (BNCH-4). Owner: Quality Champion. Due: Day 10 sprint review.

---

### Slide 1.4 — The cost is real, not theoretical (~60 sec)

**Claim:** Untestable god classes ship silent bugs. We found one. (`UpdateMaxValue` writes to `minVal`.)

**Why this matters:** the panel believes design proposals only when they connect to incident risk. We can point at a real, in-tree, currently-shipping data-corruption bug whose only protection would have been a unit test that the architecture forbids us from writing.

**Theory anchor:**
- **Beck — Test-Driven Development.** "If it's hard to test, the design is wrong." Inverting: an undesignable test surface produces this exact class of silent fault.
- **DIP.** A `MonoBehaviour` is not constructible outside the Unity engine; therefore no unit test can be written against it; therefore copy-paste bugs in its methods cannot be caught.

**Evidence:**
- `docs/sub-team-6/archived/SonarQube Baseline report.md` Rank 3 — `DesktopPaintController.UpdateMaxValue(float value) { minVal = value; }` at line 306.
- `SK_BNCH.md` page 7 — the bug is in a class with **WMC 57, CBO 21, LCOM 0.940**, and zero unit tests exist for it (NFR-TST-1 coverage = 0% on this class).

**Speaker note:** "the cost of being unable to write a unit test is in the tree, today. Line 306 of `DesktopPaintController`: a method called `UpdateMaxValue` assigns to `minVal`. The class is 1 558 lines; the bug is undetectable by reading. It would be detectable by any of the three unit tests we will write for the equivalent `FileTabViewModel`."

**Risk if challenged — "have you raised this with upstream?":** this is a design proposal, not a code change. We surface the bug as evidence of cost. Upstream issue tracking is out of our scope (§6.6).

---

## Section 2 — Target architecture, C4 levels 1–3 (10 min, ~7–8 slides)

**Section goal:** show that the four non-negotiables *force* a specific shape — we did not invent the shape, we read it off the constraints. The architecture is then a series of named pattern choices defended individually.

**Our contribution to the team-wide architecture section:** the **desktop client slice**. The team-level C4 is owned by Sub-team 1 (Architecture). Our slice plugs into it via the `IServiceGateway` contract.

---

### Slide 2.1 — C4 Level 1 (Context): the desktop client is one client of many (~60 sec)

**Claim:** The desktop client is **not** "the app". It is one front-end against a server kernel that the VR client, the (future) Python console, and (future) workspace persistence also consume.

**Why this matters:** this is the framing that justifies the **service gateway** boundary inside the client. If the desktop were the only client, the gateway would be over-engineering; because it is not, the gateway is the cheapest design.

**Theory anchor:**
- **Conway's law applied in reverse.** The team is split into seven sub-teams along the kernel/client/persistence axis (§5.5); the architecture must align with the team split or integration is impossible.
- **Hexagonal / Ports & Adapters (Cockburn).** The kernel is the inside; the desktop client, VR client, Python console are outside adapters. Our `IServiceGateway` is one such port.
- **Long-term roadmap drivers (§6.6 — Python console, workspace persistence).** Translated into requirements as ARQ-1 and ARQ-2 in `requirements.md` §4.

**Evidence:**
- `docs/sub-team-6/deliverables/D2-Architecture/architecture.md` §3 — C4 Level 1 placeholder (PlantUML at `diagrams/c4-context.puml` [EVIDENCE-GAP-2.1a]).
- `docs/sub-team-6/deliverables/D1-requirements/requirements.md` §4 — ARQ-1, ARQ-2.
- `Assignment-Docs/iDaVIE_Refactoring_Assignment_FINAL_1.md` §4.1 — client–server style.

**Speaker note:** "the desktop tab is one client. The VR scene is another. The Python console will be a third. The persistence layer reads state from all three. Any design that hardcodes 'desktop' into the kernel breaks the assignment's roadmap (§6.6). The boundary between client and kernel is therefore a contract, not a method call."

**Risk if challenged — "isn't this just a sub-system, not a 'server'?":** in local mode, yes; in remote mode, no. ADR-0002 keeps the same `IServiceGateway` surface across both; the gateway *is* the abstraction over that distinction.

**[EVIDENCE-GAP-2.1a]** PlantUML C4 Level 1 diagram for our slice. Owner: TL. Backlog: ARCH-3.

---

### Slide 2.2 — C4 Level 2 (Container): three assemblies because dependency budgets differ (~90 sec)

**Claim:** The client decomposes into **three C# assemblies** — View, ViewModel, Gateway — because each must be allowed a different set of dependencies. This is not aesthetic separation; it is **mechanically enforced** by `.asmdef` references and **CI-checked** by NDepend CQLinq.

**Why this matters:** "MVVM" said in the abstract is a slogan. MVVM at the assembly boundary is a build error if you violate it. The panel will ask: how do you stop developers re-introducing `UnityEngine` in a ViewModel? Answer: they cannot — the assembly does not reference `UnityEngine`. The compiler refuses.

**Theory anchor:**
- **DIP (Dependency Inversion).** High-level (ViewModel) does not depend on low-level (View / Unity); both depend on abstractions (the interfaces in Gateway).
- **SDP (Stable Dependencies Principle, Martin).** Instability `I = Ce/(Ca+Ce)` must decrease as you move *toward* the domain. View is the most unstable (it changes the most often); ViewModel is more stable; the contracts are most stable.
- **CCP (Common Closure Principle).** Things that change together live together. UI Toolkit churn is in View; binding policy churn is in ViewModel; wire-format churn is in Gateway.

**Evidence:**

| Assembly | Allowed references | Forbidden references | CI rule |
|---|---|---|---|
| `iDaVIE.Client.View` | `UnityEngine`, `UnityEngine.UIElements`, `iDaVIE.Client.ViewModel` | server-side types, native P/Invoke | NDepend rule: no `DllImport` |
| `iDaVIE.Client.ViewModel` | `System.*` only | `UnityEngine`, `Valve.VR`, `System.Runtime.InteropServices` | NDepend CQLinq rule already drafted (`mvvm-binding-policy.md` §10.1) |
| `iDaVIE.Client.Gateway` | `System.*`, transport library | `UnityEngine` (Unity types belong to View only) | NDepend rule |

- ADR-0001 `Decision` section — the table above is the ADR.
- `mvvm-binding-policy.md` §10 — the CQLinq rule is drafted.
- `architecture.md` §10 — compliance check table (one row per §4.2 non-negotiable).

**Speaker note:** "this slide is the answer to *how do you prevent the team from re-creating the god class?* The compiler does it. The CI does it. There is no honour system."

**Risk if challenged — "isn't three assemblies overkill for a desktop tab refactor?":** the cost is three `.asmdef` files and one NDepend rule; the value is mechanical enforcement of §4.2.3. The dependency graph (after-state) shows zero `UnityEngine` reaching the ViewModel — this is the slide that proves it.

---

### Slide 2.3 — C4 Level 3 (Component): composition root + ACL + binder (~90 sec)

**Claim:** Inside each assembly, three named patterns do the load-bearing work: the **Composition Root** wires the graph at startup, the **Anti-Corruption Layer** lives in Gateway, the **Binder** marshals between View and ViewModel.

**Why this matters:** the panel will ask "where does `new` happen?" — the answer is "exactly once, in the composition root". This kills `FindObjectOfType<>`, kills static singletons, and makes every dependency explicit and mockable.

**Theory anchor:**
- **Composition Root (Mark Seemann, *Dependency Injection in .NET*).** The single place in the application where the object graph is composed. Everywhere else takes its dependencies via constructor injection.
- **Anti-Corruption Layer (Evans, *Domain-Driven Design*).** When two models meet at a boundary, an explicit translation layer prevents one model's vocabulary from polluting the other. Here: Unity vocabulary (`Vector3`, `GameObject`, `Coroutine`) does not enter ViewModel.
- **MVVM Binder (Gossman, originally Microsoft Avalon).** A glue layer the developer does not write — UI Toolkit's binding system in Unity 6, or a thin `UnityBinder<T>` shim in Unity 2021.3.

**Evidence:**
- `architecture.md` §6 — interface contracts list (`IServiceGateway`, `IFileTabViewModel`, `IDebugTabViewModel`, `ILogStream`, `ILogObserver`, `IPanel`).
- `D2-Architecture/architecture.md §4 (ADR-0001)` `Decision` section — "composition root replaces all `FindObjectOfType<>` singleton lookups; dependencies are explicit and mockable".
- `mvvm-binding-policy.md` §5 — "Composition root owns instantiation".

**Speaker note:** "the composition root is a single `MonoBehaviour` in the View assembly. It calls `new FitsServiceAdapter()`, `new FileTabViewModel(fits, dialog, ...)`, attaches the ViewModel to the `UIDocument`'s `userData`, and is done. There are no static singletons. There are no scene-graph lookups. There is one `new` site per type, and the test suite can replace any of them with a fake."

**Risk if challenged — "what's the difference between an ACL and just a service interface?":** the ACL is the *translation* responsibility; the service interface is the *contract*. The ACL is what implements the translation (Unity `Vector3` → DTO `(float, float, float)`). Both exist; they sit at the same boundary; they have different jobs.

---

### Slide 2.4 — Transport: JSON-RPC over named pipes, gRPC later (~75 sec)

**Claim:** The transport is **JSON-RPC 2.0 over named pipes** for local mode (Day 1), with a path to gRPC over HTTP/2 for future remote streaming. The `IServiceGateway` interface is **transport-agnostic** — switching transports is a composition-root decision.

**Why this matters:** Section 6.6 explicitly names this transport choice. Defending it as ours-by-coincidence is weak; defending it as ours-by-reason is required.

**Theory anchor:**
- **OCP (Open–Closed).** The gateway is open for extension (new transport) and closed for modification (interface unchanged). Achieved by separating *contract* (`IServiceGateway`) from *adapter* (`JsonRpcPipeGateway` vs future `GrpcGateway`).
- **Protected Variations (GRASP).** The variation we anticipate is *the wire*. The variation point is the gateway interface.
- **Versioning via wireVersion (ADR-0002 §A.6).** Semver discipline at the protocol level; consumers refuse incompatible majors.

**Evidence:**
- `docs/sub-team-6/deliverables/D2-Architecture/architecture.md` — ADR-0002 (§4).
- Appendix A — wire spec: pipe naming `\\.\pipe\idavie.<session-id>`, length-prefixed framing, method catalogue (`session.hello`, `file.open`, `file.close`, `dataset.getAxes`, `log.subscribe`, `log.emit`, `progress.update`), error model (`-32010`..`-32030`).
- Cross-link to ADR-0001 — the gateway's *placement* is in ADR-0001; its *wire* is in ADR-0002.

**Speaker note:** "the panel will likely ask: why not gRPC on Day 1? Answer: debuggability. A first-year cohort can `tail` a JSON pipe and see what is happening; they cannot easily `tail` a Protobuf stream. The interface is stable across the change; the cost of switching later is a new adapter, not a new design."

**Risk if challenged — "why not REST?":** in-process HTTP server adds a port-binding + auth concern and is strictly heavier than a pipe. JSON-RPC is the smallest envelope around our request/response and notification needs. Rejected and recorded in ADR-0002 Alternatives.

---

### Slide 2.5 — Mapping each §4.2 non-negotiable to its enforcement mechanism (~75 sec)

**Claim:** Every architectural non-negotiable has **one and exactly one** enforcement mechanism we can point at. Not policy; not "we'll review it"; a thing the build or a tool refuses to accept.

**Why this matters:** the panel hates aspirational diagrams. The slide that wins them is the one that converts §4.2 from "we promise" to "the CI refuses".

**Theory anchor:**
- **Fitness functions (Ford, *Building Evolutionary Architectures*).** Each architectural property is encoded as an automated test that fails the build if the property is violated.
- **Shift-left enforcement.** A rule that fires at PR time is worth ten rules in a review document.

**Evidence:**

| §4.2 non-negotiable | Enforcement mechanism | Tool | Owner | Status |
|---|---|---|---|---|
| 1. No SOLID/GRASP violation | CK thresholds + SOLID/GRASP audit in worked examples | Understand + manual audit | Quality Champion | baseline done, projection owed |
| 2. Zero circular dependencies | NDepend CQLinq `WarnIf cycle exists` | NDepend | Quality Guild | rule drafted [EVIDENCE-GAP-2.5a] |
| 3. Domain code free of Unity/SteamVR | NDepend CQLinq rule (drafted in `mvvm-binding-policy.md` §10.1) | NDepend | Quality Guild | rule drafted, not yet wired to CI [EVIDENCE-GAP-2.5b] |
| 4. Public API has interface + test double | Coverage gate + NDepend "public type without interface" rule | NDepend + SonarQube coverage | Quality Champion | rule pending [EVIDENCE-GAP-2.5c] |

**Speaker note:** "every row has a tool and an owner. The rules exist; the wiring to CI is owed by Day 10 (Sprint 2). This is the slide where we say: *we don't trust ourselves either*."

**Risk if challenged — "what if NDepend disagrees with DV8 on cycles?":** they measure differently (NDepend = static reference graph, DV8 = DSM with runtime annotations). Both must be green. Disagreements are surfaced in the Day 12 DV8 report and reconciled before pitch freeze.

**[EVIDENCE-GAP-2.5a / b / c]** NDepend rules wired into CI. Owner: Quality Guild. Due: Day 10.

---

### Slide 2.6 — What we *removed*: God-class concerns, redistributed (~60 sec)

**Claim:** The 1 899 lines of `CanvassDesktop` are not deleted — they are **redistributed** across five named units, each with one responsibility. This is the SRP audit (`architecture.md` §7).

**Why this matters:** the panel will ask "where did the code go?" A diagram that shows the old concerns mapped onto new units, with each arrow labelled by the *reason* for the move, is the answer.

**Theory anchor:**
- **SRP (Martin).** Each unit has one *actor*: the menu actor, the panel-state actor, the file-dialog actor, the configuration actor, the threshold-maths actor.
- **GRASP — Information Expert.** The class that *owns the data* owns the operation. Subset bounds maths moves to `SubsetBoundsViewModel` because the bounds *are* its state.
- **Concern-map convention** (Section 10.4 — diagrams must be text-based; satisfied by `D2-Architecture/concern-map.puml`, which supersedes the now-archived `concern-map.png` [EVIDENCE-GAP-2.6a CLOSED]).

**Evidence:**
- `architecture.md` §7 — SRP audit placeholder.
- `D2-Architecture/concern-map.puml` — text-based concern map (8 concerns → SRP homes), satisfies Section 10.4; supersedes the binary `archived/concern-map.png`.
- After-class diagrams: `D4-worked-examples/ex1-file-tab/after-class-diagram.puml`.

**Speaker note:** "the slide shows the old class as a centre node with eight concerns radiating out, and an arrow from each concern to its new home. Every arrow has a label: SRP, Information Expert, Indirection. No code is lost — what is lost is the *coincidence* of all eight concerns being in the same class."

**Risk if challenged — "did you measure this redistribution didn't just move the god class somewhere else?":** yes — `after-dsm.md` and the CK projection in §3 are the numerical answer. Show the table in Slide 3.4 / 4.4.

**EVIDENCE-GAP-2.6a — CLOSED 2026-05-28.** `concern-map.png` converted to text-based PlantUML at `D2-Architecture/concern-map.puml` (Section 10.4 compliance). The binary `.png` is retired to `archived/` (history kept, no longer the live artefact). Owner: TL.

---

### Slide 2.7 — ADR stack on this slice (~60 sec)

**Claim:** Three named decisions, each defensible in isolation, compose to the architecture above. The stack is **ADR-0001 (MVVM split)** → **ADR-0002 (transport)** → **ADR-0003 (anti-corruption layer / Unity 6 UI Toolkit migration)** [EVIDENCE-GAP-2.7a].

**Why this matters:** the panel reads ADRs as the audit trail of decisions. Three is the recommended minimum per sub-team. Each ADR must list Alternatives Considered with rejection reasons — that is the signal of a defensible decision, not a chosen one.

**Theory anchor:**
- **Nygard's ADR convention.** Context → Decision → Consequences → Alternatives.
- **Reversibility (Fowler — irreversibility as the gate for ADR-grade decisions).** A decision that costs significantly to reverse is ADR-grade. MVVM split, transport, ACL — all three pass.

**Evidence:**
- `D2-Architecture/architecture.md §4 (ADR-0001)` — Status accepted, with Alternatives section covering MVP, MVU, MVC, status quo, Reactive MVVM.
- `D2-Architecture/architecture.md` — ADR-0002 (§4), Alternatives covering gRPC-on-Day-1, REST, in-process.
- ADR-0003 owed [EVIDENCE-GAP-2.7a].

**Speaker note:** "we did not write three ADRs because the assignment says three. We wrote three because three reversible decisions exist on our slice. We rejected MVP, MVU, MVC and Reactive MVVM by name with stated reasons in ADR-0001 Alternatives — that section is where the panel will direct most questions."

**Risk if challenged — "why is MVU rejected? Elm-style is cleaner.":** named in ADR-0001 Alternatives: mismatch with UI Toolkit's two-way binding primitives; unfamiliarity risk for a first-year team that must defend the pattern at interview. Defensibility under questioning was an explicit decision criterion.

**[EVIDENCE-GAP-2.7a]** ADR-0003 on Unity 6 UI Toolkit migration / ACL rationale. Owner: TL. Due: Day 8.

---

## Section 3 — Worked example 1: File tab (6 of 12 min, ~5 slides)

**Section goal:** convert everything in Section 2 from claims to a *diff*. The audience must be able to see, in two pictures, that the after-state is materially different from the before-state.

The pattern of each worked-example block: **before** (1 slide showing pain) → **after class diagram** (1 slide showing structure) → **after sequence diagram** (1 slide showing flow) → **CK delta table** (1 slide showing numbers) → **SOLID / GRASP audit** (1 slide showing theory).

---

### Slide 3.1 — File tab today: every `transform.Find` is a scene-renaming bomb (~60 sec)

**Claim:** "Open Cube" today is direct native-plugin call + `FindObjectOfType<>` + four-level `transform.Find("…/…/…/…")` chain — 30+ such chains in the class, each a silent `NullReferenceException` waiting for a scene rename.

**Why this matters:** the audience must feel the *brittleness* before the *solution* lands. A scene rename produces a runtime exception that no compiler caught. This is the cost of the god class made concrete.

**Theory anchor:**
- **Coupling type (Yourdon & Constantine).** Scene-path coupling is *content coupling* (the worst kind): the class depends on the internal name of another component's tree.
- **Connascence of Name (Page-Jones).** Stringly-typed scene paths are connascence-of-name at runtime — the strongest form of connascence, weakly observable.

**Evidence:**
- `SonarQube Baseline report.md` Rank 8: 30+ locations use chains like `renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport")…GetComponent<TMP_Dropdown>()`.
- `before-dsm.md` — CanvassDesktop depends directly on `VolumeCommandController`, `StandaloneFileBrowser`, `FitsReader`, `UnityEngine`.
- `before-class-diagram.puml`, `before-dependency-graph.puml` (already in tree).

**Speaker note:** "if any of those GameObjects is renamed in the editor, the compiler is silent and the runtime throws. The class is structurally vulnerable to a one-line UI edit — that is the cost of stringly-typed scene-graph coupling."

**Risk if challenged — "isn't this just a Unity convention?":** it is a Unity *anti-pattern*. The `[SerializeField]` mechanism exists precisely so editor references are compile-checked. The class chose not to use it.

---

### Slide 3.2 — After: `FileTabView` ↔ `FileTabViewModel` ↔ `IFitsService` (~75 sec)

**Claim:** Three units replace the file-loading slice of the god class. The View binds to ViewModel properties and commands; the ViewModel calls a `IFitsService` interface; the adapter behind the interface is the only place native code is invoked.

**Why this matters:** this is the moment the panel must be able to *see* the SRP split. A diagram with three boxes and labelled arrows is enough.

**Theory anchor:**
- **SRP.** `FileTabView` owns binding; `FileTabViewModel` owns command logic + validation; `FitsServiceAdapter` owns the native call.
- **DIP.** ViewModel depends on `IFitsService` (interface), not `FitsServiceAdapter` (concrete).
- **GRASP Indirection.** The interface is the indirection that breaks the direct coupling.
- **MVVM (Gossman, Microsoft).** The View binds to ViewModel via property change + command dispatch, not method calls.

**Evidence:**
- `D4-worked-examples/ex1-file-tab/after-class-diagram.puml`.
- `D4-worked-examples/ex1-file-tab/after-dependency-graph.puml`.
- `mvvm-binding-policy.md` §3.1 — File tab walkthrough [EVIDENCE-GAP-3.2a, the `_TODO` in §3.1].

**Speaker note:** "the View knows about a ViewModel property called `ImagePath`. The ViewModel knows about an `IFitsService` method called `OpenImageAsync(path)`. Neither knows that under the interface lies a P/Invoke call into a C plug-in. Each layer can be tested in isolation."

**Risk if challenged — "this is just plain dependency injection — what does MVVM add?":** MVVM is DI plus *binding semantics* (`INotifyPropertyChanged` + `ICommand`). DI alone would still require the View to imperatively pull from the ViewModel. The binding contract is what makes the View declarative.

**[EVIDENCE-GAP-3.2a]** `mvvm-binding-policy.md` §3.1 walkthrough fully written, citing skeleton file paths. Owner: TL. Due: Day 7.

---

### Slide 3.3 — After: command-driven sequence (~60 sec)

**Claim:** Sequence diagram of "user clicks Open → file loaded → cube visible" in the new architecture. Three actors (View, ViewModel, Gateway); each step labelled with the rule it satisfies.

**Why this matters:** static class diagrams do not show *time*. The panel must see that the new architecture has the same observable behaviour as the old, just through different bones.

**Theory anchor:**
- **Behaviour preservation (Fowler — refactoring definition).** External behaviour unchanged; internal structure changed.
- **Command-Query Separation (Meyer).** `Open` is a command; it returns nothing; its observable effect is a property change.
- **Asynchrony (Async/await).** Gateway calls are `Task`-returning; ViewModel exposes `IsBusy` so the View disables controls during the round-trip (`mvvm-binding-policy.md` §2.2).

**Evidence:**
- After-state sequence diagram at [`refactoring-examples/sub-team-6/file-tab/after-sequence.md`](../../../../refactoring-examples/sub-team-6/file-tab/after-sequence.md) — rewritten 2026-05-27 to show gateway routing (`file.open` → `dataset.getAxes`) for Phase A; Phase B (volume load) stays client-side via `VolumeServiceAdapter`.
- Before-state sequence at [`refactoring-examples/sub-team-6/file-tab/before-sequence.md`](../../../../refactoring-examples/sub-team-6/file-tab/before-sequence.md).
- Debug-tab equivalent at [`refactoring-examples/sub-team-6/debug-tab/after-sequence.md`](../../../../refactoring-examples/sub-team-6/debug-tab/after-sequence.md).
- `mvvm-binding-policy.md` §2 — command semantics.

**Speaker note:** "every arrow in the sequence is labelled with the rule it satisfies — DIP at the ViewModel → IFitsService boundary, ACL at the gateway-proxy → IServiceGateway boundary, the wire spec (ADR-0002) at the IServiceGateway → server boundary, INPC at the ViewModel → View notification. The diagram is the architecture, viewed in time."

**Risk if challenged — "why async if this is local?":** because the underlying call is server-side FITS parse + filesystem read, which can take seconds for a large cube. Even in local-mode (named pipe in-process) the gateway round-trip is async by contract. Blocking the UI thread breaks NFR-TST-2 (no `Thread.Sleep` / synchronous wait in ViewModel) and also breaks user experience.

**EVIDENCE-GAP-3.3a — CLOSED 2026-05-27.** Before- and after-sequence diagrams for `file-tab/` are committed and reflect the gateway rewire.

---

### Slide 3.4 — CK delta — the numbers (~75 sec)

**Claim:** Replace `CanvassDesktop` *for the file-tab slice* with three classes; the measured CK numbers fall within §7.1 thresholds, with `FileTabViewModel` WMC (27) the one borderline case over the ≤ 20 domain ceiling — remediation documented.

**Why this matters:** Section 7 of the assignment is unambiguous — "speculative numbers without evidence are not accepted". The projection must be supported by the skeleton code, not by hope.

**Theory anchor:**
- **CK suite (Chidamber & Kemerer 1994).** WMC / DIT / NOC / CBO / RFC / LCOM. The metrics are designed to detect god classes; they detect ours.
- **Threshold-vs-trend reading.** Thresholds for absolute pass/fail; trend (Day 2 → Day 13) for the proposal's value claim.
- **Henderson-Sellers LCOM.** Normalises across class size; the right LCOM variant for comparing classes of different LOC.

**Evidence (from `after-dsm.md` CK projection table):**

| Class | WMC before | WMC after | CBO before | CBO after | RFC before | RFC after | LCOM before | LCOM after | Threshold (domain) |
|---|---|---|---|---|---|---|---|---|---|
| CanvassDesktop (post-split, composition-root shell) | 63 | ~8 | 47 | ~4 | 118 | ~12 | 0.955 | ~0.30 | ≤ 40 (orchestrator) |
| FileTabViewModel | — | 27 ⚠ | — | 9 | — | ~50 | — | ≈0.20 | ≤ 20 |
| SubsetBoundsViewModel | — | ~8 | — | ~1 | — | ~15 | — | ~0.20 | ≤ 20 |
| FitsServiceAdapter | — | ~10 | — | ~6 | — | ~20 | — | ~0.30 | ≤ 40 (adapter) |

**Note on `FileTabViewModel` WMC = 27 (⚠):** hand-counted Day 6 (`metrics.md §2.2`), this is borderline **over** the ≤ 20 domain threshold and we say so on the slide. Documented remediation: extract a `FileTabCommands` helper for the four command bodies, dropping the ViewModel to WMC ~22. Every other class in the slice is within threshold. Do not quote the old "~12 / re-derive to 15" figure — it was a stale projection, superseded by the measured 27.

**Speaker note:** "the projection is evidenced by the skeleton code in `refactoring-examples/sub-team-6/file-tab/`. The numbers are not aspirational — they are countable from the skeleton. The full Understand re-run on the skeleton is owed by Day 13."

**Risk if challenged — "projected numbers, not measured":** acknowledge. Per §7, projection is acceptable if supported by skeleton code that can be re-measured. We commit to a re-measurement by Day 13 (D9 projection deliverable).

---

### Slide 3.5 — SOLID + GRASP audit on the file-tab slice (~60 sec)

**Claim:** Every SOLID principle and the relevant GRASP patterns are *named* in the after-state. We do not gesture at SOLID; we point at the class that exemplifies each letter.

**Why this matters:** LO4 ("apply SOLID + GRASP at class/component level") is a learning outcome. The panel will check that we can connect the theory to the artefact.

**Theory anchor:**

| Letter | Principle | Embodied by |
|---|---|---|
| **S** | Single Responsibility | `FileTabViewModel` (no maths, no I/O); `SubsetBoundsViewModel` (no UI, only validation); `FitsServiceAdapter` (no UI, only translation). |
| **O** | Open–Closed | `IFitsService` is closed; new adapters (e.g. HDF5) implement the interface without changing the ViewModel. |
| **L** | Liskov Substitution | Any `IFitsService` implementation can replace another without breaking `FileTabViewModel`. Test doubles depend on this. |
| **I** | Interface Segregation | `IFitsService` and `IFileDialogService` are split; `FileTabViewModel` does not depend on dialog methods. Each interface ≤ 7 members (NFR-REU-2). |
| **D** | Dependency Inversion | `FileTabViewModel` depends on `IFitsService`, not on `FitsServiceAdapter`. |

| GRASP | Embodied by |
|---|---|
| **Information Expert** | `SubsetBoundsViewModel` owns the bounds *data*; therefore owns the *validation*. |
| **Indirection** | `IFitsService` is the indirection between ViewModel and native code. |
| **Protected Variations** | The variation we anticipate (transport, file format, dialog) is each behind a stable interface. |
| **Low Coupling / High Cohesion** | CBO drops 47 → ~4 on the composition-root shell; LCOM 0.955 → ~0.30. |
| **Polymorphism** | `IFitsService` adapters dispatch on file type — no `if (extension == ...)` switch in the ViewModel. |
| **Pure Fabrication** | `FileTabViewModel` is a *fabricated* class with no real-world counterpart; it exists only to make the ViewModel layer testable. |

**Evidence:**
- `after-class-diagram.puml`.
- ADR-0001 references each letter in the Decision section.

**Speaker note:** "every SOLID letter and every GRASP pattern in scope is named with a class that embodies it. If the panel asks 'where is LSP exercised?', we point at the test double swap. The audit is not decorative — it is the bridge from theory to artefact."

**Risk if challenged — "isn't naming a principle for every class a bit forced?":** the assignment requires (LO4) that we can identify SOLID and GRASP at the class level. We do not claim every class embodies every principle; we claim each principle is embodied by *some* class in the after-state. The table is the proof.

---

## Section 4 — Worked example 2: Debug tab (6 of 12 min, ~5 slides)

**Section goal:** show a *different* pattern (Observer) under the same MVVM frame, to prove the architecture is general — not bespoke to "open a file".

The reason the assignment names *two* worked examples (§6.6) is so we cannot pass with only command-style flow. Observer is push, not pull; it stresses the threading model, the collection-binding model, and the lifecycle model in ways the File tab does not.

---

### Slide 4.1 — Debug tab today: a static, unstructured, untestable log hook (~60 sec)

**Claim:** The Debug tab (`DebugLogging.cs`, a 255-line `MonoBehaviour`) *already* observes a Unity event — it subscribes to `Application.logMessageReceived` in `OnEnable` (push, event-driven). But it observes through a **static, Unity-coupled hook**, stores entries as **unstructured strings in a non-generic `Queue`**, does **per-message disk I/O**, and **rebuilds the entire output `StringBuilder` from the full queue on every message** (O(N)). There is no `LogEntry` type, no level, no source, no structured timestamp — only `"[" + type + "] : " + logString`.

**Why this matters:** the architectural failure is not the *absence* of an observer — it is that the observer is bolted to a **static global Unity API** (untestable), carries **no structured data** (cannot filter by level or source), and **couples observation, formatting, file I/O and UI-text rebuild in one method** (`HandleLog`). The refactor replaces the static hook with a typed `ILogStream` / `ILogObserver` + a `LogEntry` DTO, fed from the server via `log.emit`.

**Theory anchor:**
- **DIP + testability (Beck — "if it's hard to test, the design is wrong").** Subscribing to the *static* `Application.logMessageReceived` is an untestable seam: no fake can stand in for a global engine event. An `ILogStream` interface restores the seam.
- **Primitive obsession (Fowler).** A `Queue` of `"[type] : message"` lines is structured data flattened to text. Levels, source tags and filtering are impossible without a `LogEntry` DTO.
- **SRP.** `HandleLog` observes, formats, enqueues, writes to disk (`AutoSave`), rebuilds the whole `StringBuilder`, and force-sets the scrollbar — several reasons to change in one method.

**Evidence:**
- `Assets/Scripts/Debuggers/DebugLogging.cs` — `OnEnable` line 149 (`Application.logMessageReceived += HandleLog`); `HandleLog` lines 177–197 (non-generic `Queue`, O(N) `StringBuilder` rebuild, `debugScrollbar.value = 1.0f`); `AutoSave` lines 249–254 (per-message `StreamWriter` open/write/close).
- `requirements.md` §2 table — Debug row: behaviour "Scrollable Unity log readout, save log to `.txt`"; coupling "Subscribes to `Application.logMessageReceived` on the main thread with no thread guard"; defect **B-02 (CRITICAL)** tab-switch-during-load crash.
- [EVIDENCE-GAP-4.1] — `ex2-debug-tab/before-class-diagram.puml` and `before-dependency-graph.puml` are owed (the equivalents for File tab exist). Owner: TL. Due: Day 8.

**Speaker note:** "the Debug tab is not missing an observer — it *is* one, subscribed to `Application.logMessageReceived`. The problem is three-fold: the hook is a static Unity API, so nothing can be unit-tested; the data is an unstructured string queue, so you cannot filter by level or source; and one method does observation, disk I/O and an O(N) UI rebuild on every single log line. The refactor swaps the static hook for a typed `ILogStream` the ViewModel can subscribe to and a fake can replace."

**Risk if challenged — "it already subscribes to an event, so what is the win?":** the win is the *seam* and the *structure*. A static engine event cannot be faked in a unit test; `ILogStream` can. A `LogEntry` DTO turns flattened text back into filterable, structured data. Same Observer shape — testable and structured instead of static and stringly-typed.

---

### Slide 4.2 — After: `ILogStream` → `DebugTabViewModel` (Observer) → `DebugTabView` (~75 sec)

**Claim:** A `ILogStream` interface publishes `LogEntry` events; `DebugTabViewModel` subscribes (Observer) and maintains a bound `ObservableCollection<LogEntry>`; the View shows the collection via `ListView` virtualisation.

**Why this matters:** Observer is one of the canonical Gang of Four patterns. Its application here is not novel — the panel needs to see that we *recognised* the pattern, not that we invented one.

**Theory anchor:**
- **Observer (GoF).** Subject (`ILogStream`) notifies registered Observers (`DebugTabViewModel`) of state changes. Decouples producer from consumer.
- **OCP.** New log sinks (file, network) can register without modifying `DebugTabViewModel`.
- **GRASP — Polymorphism.** Every observer implements the same interface; the stream does not know its observers' types.

**Evidence:**
- `refactoring-examples/sub-team-6/debug-tab/skeleton/ILogStream.cs` + `ILogObserver.cs` (exist).
- `refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs` (exists; pure C#; subscribes to `ILogStream`).
- `uml-diagrams/after-debug-sequence-diagram.puml` (exists).

**Speaker note:** "Observer is the textbook fit. We did not pick it because it is fashionable — we picked it because the data flow is push and the producers can multiply (file sink, network sink) without the ViewModel changing. That is OCP in action."

**Risk if challenged — "why not use Rx / UniRx for this?":** named in ADR-0001 Alternatives — Rx is a compelling fit but adds a library dependency and a learning curve. The `ILogStream` event model is *forward-compatible* with an Rx migration later because the ViewModel surface does not change.

---

### Slide 4.3 — Threading: a producer thread is not a UI thread (~60 sec)

**Claim:** Log entries originate on background threads (server thread, native callback thread). The Observer pattern is correct; what makes it *safe* is the marshalling to the UI thread via `IUIDispatcher`.

**Why this matters:** this is the slide that proves we understand the *operational* cost of Observer, not just the diagram. The panel will probe threading because Unity's main-thread rule is non-negotiable.

**Theory anchor:**
- **Thread confinement (Goetz, *Java Concurrency in Practice*).** UI Toolkit (like every retained-mode UI) requires its tree to be mutated on a single thread.
- **Marshalling primitive.** Abstracted as `IUIDispatcher.Post(Action)` — testable; the production implementation lives in the View assembly, the ViewModel takes the interface (`mvvm-binding-policy.md` §4.2).
- **`ConfigureAwait(false)` discipline at Gateway boundaries** (`mvvm-binding-policy.md` §4.3) — every async hop is explicit.

**Evidence:**
- `mvvm-binding-policy.md` §4 — single-UI-thread rule, marshalling primitive, forbidden patterns (`UnityMainThreadDispatcher` static singleton).
- `mvvm-binding-policy.md` §3.2 — collection mutation rules (bulk loads atomic, UI thread only).

**Speaker note:** "the slide is one line of code that fails and one that passes. The failing line: `LogEntries.Add(entry)` from a background thread — Unity throws. The passing line: `dispatcher.Post(() => LogEntries.Add(entry))` — safe, testable because `IUIDispatcher` is an interface."

**Risk if challenged — "what if the dispatcher is slow under tracing rates?":** named as an open decision in `mvvm-binding-policy.md` §3.1 — vanilla `ObservableCollection` vs ring-buffer vs virtualised incremental collection. The pitch states the decision is owed; the choice is informed by load testing in Sprint 2.

---

### Slide 4.4 — CK delta — Debug tab numbers (~60 sec)

**Claim:** The Debug tab slice is *small* — and that is the point. After the split, `DebugTabViewModel` is a 30-line class with WMC ~4, CBO ~3, RFC ~10, LCOM ~0.20.

**Why this matters:** the File tab slide shows that CK numbers *drop*; the Debug tab slide shows that they *stay low* under a different pattern. Same architecture, different forces — same result.

**Theory anchor:**
- **Repeatability of metric improvement under the same architecture.** If the architecture is good, applying it to a different concern should also produce in-bounds CK numbers. If only the File tab improves, we got lucky; if both do, the architecture is the explanation.

**Evidence (projected from skeleton):**

| Class | WMC | CBO | RFC | LCOM | Threshold (domain) |
|---|---|---|---|---|---|
| DebugTabViewModel | ~4 | ~3 | ~10 | ~0.20 | ≤ 20 / ≤ 14 / ≤ 50 / ≤ 0.5 |
| ILogStream (interface) | n/a | n/a | n/a | n/a | ISP ≤ 7 members |
| LogStream (adapter) | ~6 | ~4 | ~15 | ~0.30 | ≤ 40 (adapter) |
| FakeDebugLogSource (test double) | ~3 | ~1 | ~5 | ~0.10 | n/a |

- Skeleton: `refactoring-examples/sub-team-6/debug-tab/skeleton/`.
- [EVIDENCE-GAP-4.4] — full Understand re-measure on the debug-tab skeleton owed by Day 13.

**Speaker note:** "every class in the after-state is small, focused, and within the §7.1 thresholds. The proof that the architecture is general is that two different patterns produce two different sets of small, focused classes."

**Risk if challenged — "30-line classes — over-decomposition?":** ISP and SRP both encourage many small interfaces and many small classes. The cost is one extra file per concern; the value is mockability. The trade-off is named in ADR-0001 Consequences ("Extra ceremony: three assemblies, interface boilerplate, `UnityBinder<T>` shim").

---

### Slide 4.5 — SOLID + GRASP audit on the debug-tab slice (~60 sec)

**Claim:** Different patterns, same principles. Observer adds two principles that File tab did not exercise: **OCP** at the stream surface (new sinks plug in) and **Polymorphism** at the observer surface (sinks have different types but conform to the same contract).

**Why this matters:** the panel can ask "did you really use two patterns or is this just the same example twice?" The audit shows the differential.

**Theory anchor:**

| Principle | File tab (3.5) | Debug tab |
|---|---|---|
| SRP | ViewModel / ViewModel / Adapter split | Stream / ViewModel / View split |
| OCP | `IFitsService` adapters | `ILogStream` sinks — **the differentiator** |
| LSP | Test double swap on `IFitsService` | Test double swap on `ILogStream` (`FakeDebugLogSource`) |
| ISP | `IFitsService` + `IFileDialogService` split | `ILogStream` + `ILogObserver` split |
| DIP | ViewModel ↔ interfaces only | ViewModel ↔ interfaces only |
| Information Expert | `SubsetBoundsViewModel` owns bounds | `DebugTabViewModel` owns the bound log collection |
| Polymorphism | `IFitsService.OpenImageAsync` dispatches by file type | **Multiple `ILogObserver` implementations**, dispatched uniformly |
| Indirection | `IFitsService` | `ILogStream` |
| Observer pattern | n/a | **the defining pattern of this example** |

**Evidence:**
- `refactoring-examples/sub-team-6/debug-tab/skeleton/`.
- `mvvm-binding-policy.md` §3.2 — Debug tab walkthrough [EVIDENCE-GAP-4.5, the `_TODO`].

**Speaker note:** "two patterns, one architecture. The SOLID matrix shows the differential — OCP and Polymorphism do real work in Debug that File tab does not exercise. If we only had one worked example, we could not claim the architecture is general."

**Risk if challenged — "do you actually need *two* worked examples?":** the assignment requires it (§6.6). The deeper reason: a single example proves only that the pattern fits one problem. Two examples with different forces prove the architecture is not bespoke.

**[EVIDENCE-GAP-4.5]** `mvvm-binding-policy.md` §3.2 walkthrough fully written. Owner: TL. Due: Day 7.

---

## Section 5 — Testability + Unity 6 migration (6 min, ~4–5 slides)

**Section goal:** answer "what does this architecture buy you that the god class did not?" The answer in two words: *unit tests* (NFR-TST family) and *a migration path* (LO5).

**Our contribution to the team-wide section:** the **client-shell testability slice** and the **UI Toolkit migration path**. Other sub-teams own their own coverage and migration; we are responsible for the desktop client's portion of both.

---

### Slide 5.1 — Three test layers, each with a Unity dependency budget (~75 sec)

**Claim:** The split produces three test layers — **ViewModel unit tests (no Unity)**, **gateway unit tests with mock transport (no Unity)**, **View integration tests via UI Toolkit page-object pattern (Unity required)**. The first two satisfy the strict coverage target (NFR-TST-1: ≥ 70 % branch + line); the third is tracked, not gated.

**Why this matters:** the panel will ask "how do you actually unit-test Unity code?" The answer is: we don't. We unit-test the code that we engineered to *not be* Unity code.

**Theory anchor:**
- **Test pyramid (Cohn).** Many unit tests at the bottom (fast, no Unity); fewer integration tests in the middle (slow, Unity required); a thin smoke layer at the top.
- **Page Object (Fowler, *Page Object*).** A View-side abstraction with intent-named queries (`ClickOpenCubeButton()`, `EnterPath(string)`); the integration test does not couple to UXML structure.
- **Test double taxonomy (Meszaros).** We use *stubs* (`StubFitsService`, `StubVolumeService`) and *fakes* (`FakeDebugLogSource`); no mocks-as-spies in ViewModel tests.

**Evidence:**
- `test-strategy.md` §2 — layered test approach table.
- `D5-testing/viewmodel-unit-tests.md` — ViewModel test pattern.
- `D5-testing/ui-toolkit.md` — page-object pattern.

**Speaker note:** "the test pyramid for our slice has a fat base of ViewModel + gateway unit tests that need no Unity. The middle layer is page-object integration tests in the Unity Test Framework. The smoke layer is manual end-to-end runs. The coverage gate hits only the base — because that is where the design forces are."

**Risk if challenged — "why not Edit Mode tests in the Unity Test Framework instead?":** Edit Mode tests still require Unity to be installed. NUnit + Moq runs on any CI agent without a Unity licence. The cost of Unity in CI is real — licences, agent time. Avoiding Unity for the ViewModel layer is a deliberate cost decision.

---

### Slide 5.2 — Mocking surface = ISP × DIP (~60 sec)

**Claim:** Tests are cheap to write because the mocking surface is small. Every interface a ViewModel depends on has ≤ 7 members (NFR-REU-2 / ISP); every dependency is injected via constructor (DIP). The cost of a new unit test is one `Mock<I…>()` line.

**Why this matters:** the difference between "we have an MVVM design" and "we have a *testable* MVVM design" is whether the interfaces are sized to be moqued in one line. The §7.2 testability family encodes this.

**Theory anchor:**
- **ISP (Robert Martin).** "Clients should not be forced to depend on methods they do not use." Operationalised here as ≤ 7 public members per interface.
- **Mocking-difficulty count (Section 7.2).** A custom NDepend rule counts static / Unity API calls per class — zero is the target on ViewModels. Quantitative.

**Evidence:**
- `requirements.md` NFR-REU-2 (ISP ≤ 7), NFR-TST-2 (mocking difficulty = 0).
- `test-strategy.md` §7 — Interface-size audit (BNCH-7).
- `test-strategy.md` §8 — Mocking-difficulty count (BNCH-6).
- BNCH-6 (`other/T2-baseline-benchmark/BNCH-6.md`) and BNCH-7 (`…/BNCH-7.md`) audit tables — both committed (EVIDENCE-GAP-5.2 CLOSED).

**Speaker note:** "the proof that the design is testable is not 'we wrote unit tests'. The proof is 'the interfaces are small enough that any developer can write a test in five lines'. ISP and DIP, working together, are the operational testability lever."

**Risk if challenged — "is ≤ 7 a magic number?":** it is the threshold the assignment specifies (§7.2). The literature varies — 5 to 10 are all defensible. We picked the assignment number and made it a hard gate.

**EVIDENCE-GAP-5.2 — CLOSED 2026-05-28.** BNCH-6 mocking-difficulty count (`other/T2-baseline-benchmark/BNCH-6.md` — `CanvassDesktop` 205 → ViewModel 0) and BNCH-7 ISP audit (`…/BNCH-7.md` — 11/12 interfaces ≤ 7; `IFileTabViewModel` facade is the documented trade-off) both committed with real numbers. Owner: Quality Champion.

---

### Slide 5.3 — Worked test specification: `BrowseImageCommand` / `LoadCommand` (~60 sec)

**Claim:** Three test cases that *would not exist* without the split. Browse contract, load contract, and error path — all running in NUnit, no Unity.

**Why this matters:** the panel wants concreteness. A three-row test table whose rows make sense in plain English is the proof that the design lets us *describe* behaviours, not just observe them.

**Theory anchor:**
- **Given–When–Then (Behaviour-Driven Development).** Each test has a clear arrangement, action, and assertion. The interface boundary is what makes Arrange-Act-Assert mechanical.
- **Equivalence partitioning + boundary value analysis.** Three tests are three partitions of behaviour space, not three random calls.

**Evidence:**
- `refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs` (Browse / Load cases):

```csharp
[Test] public async Task BrowseImage_ValidCube_SetsImagePathAndIsLoadable() { ... }
[Test] public async Task Load_ValidFile_PassesCorrectPathAndHduIndex() { ... }
[Test] public async Task Load_VolumeServiceThrows_SetsValidationMessage() { ... }
```

**Speaker note:** "test 1 is the browse contract — browsing a valid cube sets `ImagePath` and `IsLoadable` with no Unity in the loop. Test 2 is the load contract — the ViewModel hands `IVolumeService` the right path and 1-based HDU index. Test 3 is the error path — the volume service throws, the ViewModel exposes `ValidationMessage` without throwing out of the command (`mvvm-binding-policy.md` §2.3). All three are NUnit; none touches Unity."

**Risk if challenged — "where is the implementation of these tests?":** the skeleton is owed in `refactoring-examples/sub-team-6/file-tab/code/` by Day 10. Pre-commit; defensible if challenged on the day.

---

### Slide 5.4 — Unity 5 → Unity 6 UI Toolkit migration: scoped, not heroic (~75 sec)

**Claim:** Migration from `UnityEngine.UI` Canvas (Unity 2021.3) to UI Toolkit (Unity 6) is scoped to **one assembly** (`iDaVIE.Client.View`). ViewModel and Gateway are unchanged because they contain no Unity types.

**Why this matters:** LO5 requires us to demonstrate Unity 5 → Unity 6 migration with UML, dependency graphs, and worked examples. The architecture is what makes migration tractable — without the split, every line of UI code would need to be ported atomically.

**Theory anchor:**
- **Bounded change (Fowler — *Refactoring*).** A change with bounded blast radius is migration; a change with unbounded blast radius is rewrite. The split converts a rewrite into a migration.
- **Strangler Fig pattern (Fowler).** Old and new Views can coexist behind the same ViewModel — incremental panel-by-panel migration is feasible.

**Evidence:**
- `architecture.md` §9 — Unity 5 → Unity 6 migration plan placeholder.
- ADR-0003 [EVIDENCE-GAP-2.7a] — UI Toolkit as View tech + migration plan.
- `D5-testing/ui-toolkit.md` — page-object pattern in UI Toolkit.

**Speaker note:** "the migration is panel-by-panel. We can port the File tab to UI Toolkit, leave the Render tab on Canvas, ship both in the same build. The ViewModel is identical across both. This is the strangler fig pattern with the architecture as its enabler."

**Risk if challenged — "Unity 6 still allows uGUI; why migrate at all?":** UI Toolkit is the strategic direction for Unity's UI system, and the assignment specifies Unity 6 with UI Toolkit explicitly (§6.6). The migration is mandated; the architecture makes it cheap.

---

### Slide 5.5 — Coverage targets and CI gates (~45 sec)

**Claim:** ViewModel ≥ 70 % branch + line (NFR-TST-1); overall ≥ 50 %; Unity-bound code tracked but not gated; cycle detection and "no Unity in ViewModel" rules block merge by Day 10.

**Why this matters:** the panel asks "how is this enforced?" — the answer is the CI gates. Numbers are aspirational without gates.

**Theory anchor:**
- **Quality gate (Kruchten — Architectural Decision tracking).** A gate is the explicit threshold below which a PR is rejected.
- **CI feedback loop (Humble & Farley, *Continuous Delivery*).** Fast feedback at PR time is structurally more effective than fortnightly review.

**Evidence:**
- `requirements.md` NFR-TST-1 to NFR-TST-3.
- `deliverables-checklist.md` §5.4 — CI gates owed by Day 10.

**Speaker note:** "by Day 10, a PR that introduces a circular dependency, a CK threshold breach, or a `UnityEngine` import in a ViewModel assembly is blocked from merge. The Quality Guild owns the wiring; we own the rules."

**Risk if challenged — "what about flaky coverage?":** branch + line, not statement; numerator/denominator both reported; the trend (Day 2 → Day 13) is what we defend, not the absolute on a noisy day.

---

## Section 6 — Trade-offs + risk (5 min, ~4 slides)

**Section goal:** the panel will *not* believe a clean story. The honest section beats the polished one. Name three things we paid for, three things we are afraid of, and the mitigation for each.

**Our contribution to the team-wide section:** the **desktop client slice's trade-offs**. Other sub-teams' trade-offs are not ours to defend.

---

### Slide 6.1 — Trade-off 1: MVVM ceremony cost (~75 sec)

**Claim:** Three assemblies, `INotifyPropertyChanged` boilerplate, `UnityBinder<T>` shim, composition root wiring — all of these are *cost we did not pay before*. We paid it because the alternative (the god class) cost more.

**Why this matters:** an architecture with no costs is suspicious. Naming the cost — and showing the mitigation — converts the cost into a deliberate choice.

**Theory anchor:**
- **Cost of abstraction.** Every interface is a runtime indirection and a maintenance cost; the trade is mockability and substitutability.
- **Source generators as boilerplate erasers (CommunityToolkit.Mvvm `[ObservableProperty]`).** Recognised in `mvvm-binding-policy.md` §1.1 as the decision-pending mitigation.

**Evidence:**
- ADR-0001 Consequences — "Extra ceremony: three assemblies, interface boilerplate, `UnityBinder<T>` shim. Mitigation: code skeletons in `refactoring-examples/sub-team-6/` serve as the canonical pattern."
- `mvvm-binding-policy.md` §1.1 `_TODO` — source-gen vs hand-rolled.

**Speaker note:** "the ceremony is a cost. We mitigate with skeletons (one canonical example, copied for new panels) and with source-gen (decision pending — trade-off recorded in §1.1)."

**Risk if challenged — "this is over-engineering for a desktop tab refactor":** if the desktop client were the only consumer, yes. The roadmap (ARQ-1 Python console; ARQ-2 workspace persistence) and the team split (VR client, server kernel) make the ceremony pay for itself in the second consumer.

---

### Slide 6.2 — Trade-off 2: ViewModel leak risk (~60 sec)

**Claim:** A developer can accidentally re-introduce `UnityEngine` into a ViewModel through a transitive reference if an `.asmdef` is misconfigured. The architecture's central rule could fail by misclick.

**Why this matters:** any rule that is enforceable only by review fails. We must show that the rule is enforceable mechanically.

**Theory anchor:**
- **Fitness function (Ford).** Architectural rules must be automated, not aspirational.
- **Defence in depth.** Three independent enforcement layers — `.asmdef` reference list, NDepend CQLinq rule, Roslyn analyzer — each catches a different class of misconfiguration.

**Evidence:**
- ADR-0001 Operational — "NDepend CQLinq rule added in T6 (Quality Guild sprint): forbids `UnityEngine.*` import inside `iDaVIE.Client.ViewModel`. PR check blocks merge if any ViewModel class has a direct or transitive reference to `UnityEngine`, `Valve.VR`, or `System.Runtime.InteropServices.DllImportAttribute`."
- `mvvm-binding-policy.md` §10.1 — CQLinq rule sketch.
- `mvvm-binding-policy.md` §10.2 — Roslyn analyzer.

**Speaker note:** "the rule has three layers: the `.asmdef` doesn't list `UnityEngine` (build error), NDepend warns on transitive imports (PR warning), Roslyn analyzer flags suspicious patterns (IDE warning). A single layer would be aspirational; three layers is engineering."

**Risk if challenged — "what if NDepend / Roslyn produces false positives?":** acknowledged. The PR-check rule is owed by Day 10; tuning against false positives happens in Sprint 2. The CQLinq rule sketch is in `mvvm-binding-policy.md` §10.1.

---

### Slide 6.3 — Trade-off 3: Sub-team 1 dependency (~45 sec)

**Claim:** The `IServiceGateway` contract is owned by Sub-team 1 (Apaties I — Architecture/Micro-kernel). Until they ship the interface surface, our File tab worked example consumes a **proposed** contract, not a delivered one.

**Why this matters:** the panel will look for inter-sub-team integration evidence. We must name our dependencies and our mitigation.

**Theory anchor:**
- **Contract-first design (Bertrand Meyer, *Object-Oriented Software Construction*).** Both sides of a contract can design against the contract without either side being implemented. Used here.
- **Stub-and-mock pattern.** Our `StubFitsService` consumes our `IFitsService` interface; the real adapter lands when Sub-team 1's `IServiceGateway` is final.

**Evidence:**
- ADR-0001 — DEPS-1 recorded on integration risk register R01.
- `deliverables-checklist.md` §5.3 — "cross-sub-team integration review Day 8".

**Speaker note:** "we are blocked on nothing because the contract is the artefact. Sub-team 1's day 8 integration review is when their interface lands; ours conforms to it then. Until then, our skeleton consumes a fake."

**Risk if challenged — "what if Sub-team 1's interface differs from your assumption?":** acknowledged risk. ADR-0001 lists ARCH-8 (interface contracts proposed to Sub-team 1) as the seed; if their surface differs, we change our skeleton, not our architecture.

---

### Slide 6.4 — Risk: pitch defence + cohort buy-in (~45 sec)

**Claim:** The architecture is only good if a developer reading the after-state can produce a new panel without re-reading the ADR. We mitigate by skeletons (canonical example) and binding policy (rule sheet).

**Why this matters:** the panel sees a lot of architectures fail at the *team adoption* stage. We must show that we thought about it.

**Theory anchor:**
- **Cognitive load (Sweller).** A design with high intrinsic load (many concepts) needs low extraneous load (clear examples).
- **Conventions over configuration.** `mvvm-binding-policy.md` is the convention sheet; the skeletons are the convention exemplars.

**Evidence:**
- `mvvm-binding-policy.md` §8 — Forbidden patterns table (the convention sheet).
- `mvvm-binding-policy.md` §10.3 — PR checklist (reviewer-facing).
- Skeletons in `refactoring-examples/sub-team-6/`.

**Speaker note:** "a new developer writes a new tab by copying the file-tab skeleton, renaming the classes, and re-running the PR checklist. If the checklist passes, the binding policy is satisfied. If it does not, the rule that failed is named in plain English."

**Risk if challenged — "what if the team doesn't follow the policy?":** the NDepend / Roslyn rules block merge. Policy without mechanical enforcement is decoration; we have both.

---

## Section 7 — Summary (3 min, ~2–3 slides)

**Section goal:** restate the one sentence from §0 with the numbers and the names attached. Not new content — *resolved* content.

---

### Slide 7.1 — Before → after, four non-negotiables (~60 sec)

**Claim:** Every §4.2 non-negotiable failed in the before-state and passes in the after-state. Five sub-characteristics red → five green.

**Why this matters:** the close of the pitch is the close of the audit. Same table as Slide 1.3, with the right column flipped.

**Evidence:**

| §4.2 non-negotiable | Before | After (projected) |
|---|---|---|
| SOLID/GRASP | violated (SRP, OCP, DIP, LCOM) | satisfied with named audits (Slides 3.5, 4.5) |
| Zero cycles | suspected violation | zero — NDepend rule on every PR |
| No Unity in domain | direct `UnityEngine`, `Valve.VR` imports | ViewModel assembly contains no `UnityEngine` (NDepend rule) |
| Interface + test double on public API | zero interfaces | every public boundary an interface with ≥ 1 fake |

| ISO 25010 sub-char | Before (D rating) | After (target A) |
|---|---|---|
| Modularity | CBO 47, cycles | CBO ≤ 14 / 25, zero cycles |
| Reusability | no interfaces | ISP ≤ 7 on every boundary |
| Analysability | WMC 63, CC 31 | WMC ≤ 20 / 40, CC ≤ 15 |
| Modifiability | propagation cost high (DV8 owed) | ≥ 30 % drop (NFR-MOF-2) |
| Testability | 0 % coverage | ≥ 70 % branch + line on ViewModel |

**Speaker note:** "the close is the four boxes. They were red. They are now green. Every green is a number a tool produces, not a number we picked."

---

### Slide 7.2 — Forward look (~60 sec)

**Claim:** This architecture pays for itself in three places — gRPC remote mode (ADR-0002 forward-compat), Python console (ARQ-1), workspace persistence (ARQ-2). Each is a roadmap item §6.6 named explicitly.

**Why this matters:** the panel rewards architectures that *extend*. Naming three extensions, each already linked to a requirement, is the close.

**Evidence:**
- ADR-0002 §A.7 — gRPC `.proto` reuses method names and error codes; coordination with Sub-team 1.
- `requirements.md` ARQ-1 — Python console via non-Unity ViewModel.
- `requirements.md` ARQ-2 — workspace state via JSON-serialisable DTO.
- D13 state contract to Sub-team 7 — Day 9 exit criterion.

**Speaker note:** "the ViewModel layer is callable from a Python process today, in principle. The gateway is gRPC-ready today, in interface. The state is JSON-serialisable today, in DTO design. The roadmap is not 'we hope' — it is *what the architecture already allows*."

---

### Slide 7.3 — Named owners (~30 sec)

**Claim:** Every artefact in this pitch has a named author. Section 10.5 #4 is satisfied — every speaker can defend their slide cold.

**Why this matters:** the close ends with names. Section 8.4 interviews follow within 48 hours; any name on a slide is a name interviewed against that slide.

**Evidence:**
- `D2-Architecture/architecture.md §4 (ADR-0001)` — author named.
- `D2-Architecture/architecture.md` — ADR-0002 author owed [EVIDENCE-GAP-7.3a].
- `mvvm-binding-policy.md` — author owed [EVIDENCE-GAP-7.3b].
- Pitch speaker assignments per slide owed [EVIDENCE-GAP-7.3c].

**[EVIDENCE-GAP-7.3a/b]** Author names on ADR-0002 and mvvm-binding-policy. Owner: TL. Due: Day 7.
**[EVIDENCE-GAP-7.3c]** Per-slide speaker assignment table. Owner: SM. Due: Day 12 (rehearsal day).

---

## Appendix A — Evidence gap punch-list (the holes this spine surfaces)

Ranked by pitch-day visibility. Each gap names an artefact, an owner, and a due date.

| # | Gap | Artefact | Owner | Due | Slide(s) |
|---|---|---|---|---|---|
| 1 | DV8 / NDepend cycle report on 8-class slice — after-state assembly-level acyclicity now tool-backed (MSBuild); class-level + before-state tool confirmation still owed | [`../other/cycles-report.md`](../other/cycles-report.md) | Quality Champion | Day 10 | 1.3, 7.1 |
| 2 | PlantUML C4 Level 1 diagram for our slice | `docs/sub-team-6/diagrams/c4-context.puml` | TL | Day 8 (ARCH-3) | 2.1 |
| 3 | C4 Level 2 + Level 3 PlantUML | `docs/sub-team-6/diagrams/c4-container.puml`, `c4-component.puml` | TL | Day 8 | 2.2, 2.3 |
| 4 | NDepend rules wired into CI (cycles, no-Unity-in-VM, no-static-singleton) | `tools/ndepend/rules.cqlinq` | Quality Guild | Day 10 | 2.5, 6.2 |
| 5 | Concern map text-source (replace `.png`) — **CLOSED 2026-05-28** | `docs/sub-team-6/deliverables/D2-Architecture/concern-map.puml` | TL | Day 7 | 2.6 |
| 6 | ADR-0003 (ACL + Unity 6 UI Toolkit migration) | `docs/sub-team-6/adrs/0003-acl-uitk-migration.md` | TL | Day 8 | 2.7, 5.4 |
| 7 | `mvvm-binding-policy.md` §3.1 + §3.2 walkthroughs filled | `…/D3-MVVM-binding-policy/mvvm-binding-policy.md` | TL | Day 7 | 3.2, 4.5 |
| 8 | Before- and after-sequence diagrams for file-tab | `…/D4-worked-examples/ex1-file-tab/*-sequence-diagram.puml` | TL | Day 8 | 3.3 |
| 9 | Debug-tab before-state UML | `…/D4-worked-examples/ex2-debug-tab/before-*.puml` | TL | Day 8 | 4.1 |
| 10 | BNCH-6 mocking-difficulty + BNCH-7 ISP audit tables — **CLOSED 2026-05-28** (both committed; BNCH-6 `CanvassDesktop` 205 → VM 0; BNCH-7 11/12 interfaces ≤ 7, `IFileTabViewModel` facade documented trade-off) | `docs/sub-team-6/deliverables/other/T2-baseline-benchmark/BNCH-6.md`, `BNCH-7.md` | Quality Champion | ✅ Day 9 | 5.2 |
| 11 | File-tab skeleton + `BrowseImage`/`Load` unit tests | `refactoring-examples/sub-team-6/file-tab/tests/` | Sub-team | Day 10 | 5.3 |
| 12 | Day-13 CK re-measurement on skeleton (Understand re-run) | `docs/sub-team-6/metrics/projection.md` | Quality Champion | Day 13 | 3.4, 4.4 |
| 13 | Author names on ADR-0002, mvvm-binding-policy | inline | TL | Day 7 | 7.3 |
| 14 | Per-slide speaker assignment + rehearsal plan | `docs/sub-team-6/deliverables/T5-pitch/speakers.md` | SM | Day 12 | 7.3 |

---

## Appendix B — Anticipated Q & A

> **Purpose.** Research aid for the 20-min Q&A that follows the 40-min talk (§9.1 T5; Appendix C of the brief). Every slide in §1–§7 already carries a `Risk if challenged` line — those are the *first-order* answers. This appendix captures questions that go *beyond* the per-slide rebuttals: cross-cutting probes, process / AI / authorship questions, panel-specific angles, and hostile framings. Treat as rehearsal seed, not script.
>
> **Format reality (Appendix C of the brief).** 20 minutes, ~8–12 questions at panel pace. Panel may direct any question at any named member — §10.5 #4 makes "inability to explain a section a fail signal" — so each of the four of us must be able to field any question in this appendix. Tech Lead + Architecture sub-team must be present; for us that means **Tech Lead this sprint defends architecture; PO Liaison defends requirements / NFR mapping; Quality Champion defends metrics; Scrum Master defends process + AI log**.
>
> **What the panel is.** iDaVIE maintainers — domain astronomers + Unity/C++ engineers who *wrote* the code we are critiquing. Expect: (i) defensiveness about the existing code, (ii) deep Unity / native-interop knowledge, (iii) skepticism of "design proposals" that have not been built, (iv) a working knowledge of FITS, WCS, large-cube performance. Plan for it.

---

### B.1 — Top 12 most-likely questions (the must-rehearse set)

Ranked by signal — *probability × difficulty × cost of muffing it*. If we rehearse only these twelve, we cover ~70 % of likely Q&A pressure.

| # | Question (likely panel voice) | What they are really probing | First-line answer | Trap |
|---|---|---|---|---|
| **1** | *"You did not change a single line of production code. Why should we believe these CK numbers will land?"* | Projection ≠ measurement. §7 forbids "speculative numbers without evidence". | Point at skeleton: `refactoring-examples/sub-team-6/file-tab/`, `…/debug-tab/skeleton/`. The numbers are countable from skeleton, not from hope. Day 13 re-measurement on the skeleton is owed (Gap #12, Appendix A). | Promising the full refactor is delivered. It is not — design proposal only (§6.6). |
| **2** | *"Pick one slide. Defend it as if you wrote it alone."* | §10.5 #4 — inability to defend = fail signal. They will pick a slide *with our name on it*. | Whichever member is asked: lead with the **claim** sentence from that slide, then the **theory anchor**, then the **evidence pointer**. Do not improvise — the spine is the script. | Reading the slide back. They asked you to *defend*, not narrate. |
| **3** | *"Show me where in the brief MVVM is mandated, and defend the choice over MVP / MVU / MVC."* | LO4 + ADR-0001 quality. §6.6 says "MVVM-style split"; the choice between MVVM variants is ours. | Brief §6.6 names MVVM explicitly. ADR-0001 *Alternatives Considered* rejects MVP (no binding semantics — equivalent boilerplate without payoff), MVU (mismatch with UI Toolkit two-way binding; learning-curve risk), MVC (View pulls Model — defeats DIP for our threading model), Reactive MVVM (Rx adds a library dependency we have not justified). | Defending MVVM as "the obvious choice". The brief names it but does not justify it — we must. |
| **4** | *"Sub-team 1 owns the `IServiceGateway`. What happens if their contract differs from your assumption?"* | DEPS-1, integration risk (R01 in ADR-0001). | Slide 6.3. The contract *is* the artefact. Day 8 integration review (sprint plan) is the lock-in date. Our ViewModel depends only on `IFitsService` and `ILogStream` — those are *ours*. The seam to `IServiceGateway` is the adapter and is owned in our composition root. If Sub-team 1's shape changes, we rewrite the adapter, not the ViewModel. | Saying "we coordinated with them". Name the *artefact* and the *date*. |
| **5** | *"`CanvassDesktop` is a `MonoBehaviour` because Unity demands it. Doesn't your split fight the engine?"* | The panel wrote the engine integration. They know Unity. | The View *stays* a `MonoBehaviour` (or `UIDocument` in UI Toolkit) — that is the only assembly that touches Unity. The ViewModel is pure C# because it does *not* extend `MonoBehaviour`; it is held by reference, not by the scene graph. We are not fighting Unity — we are letting Unity own only what Unity must own. `mvvm-binding-policy.md` §5 (composition root). | Implying the View is also pure C#. It is not. |
| **6** | *"You claim 47 → ~4 CBO on the post-split shell. How? Show me the dependencies you removed."* | Quantitative challenge. CBO drops are easy to over-claim. | Walk the before-DSM (`before-dsm.md`) → after-DSM (`after-dsm.md`) for **CanvassDesktop (post-split)** row only. The shell's responsibility is *only* to instantiate the ViewModels and bind the `UIDocument`. Its CBO is now bounded by the count of ViewModels it composes (~4). The remaining dependencies (FITS, native, dialogs) moved *into* the adapters, where adapter thresholds (CBO ≤ 25) absorb them. | Quoting the gross number without showing where the coupling *went*. CBO does not disappear — it relocates to a class with a higher threshold. |
| **7** | *"You found a real bug (`UpdateMaxValue` writes `minVal`). Did you fix it? Did you tell the maintainers?"* | This is the trick question. The brief is design-only (§6.6) — but the panel *is* the maintainer. | Frame: this is evidence of cost in Slide 1.4, not a deliverable. We are now telling you — on a slide. We can raise a GitHub issue post-pitch if useful; we did not raise it during the assessment because doing so was out of §6.6 scope. | Either (a) hedging — say it cleanly. Or (b) claiming we will fix it during the assessment window. We will not. |
| **8** | *"JSON-RPC over named pipes is fine for local. What is your evidence that the same interface survives the gRPC switch?"* | OCP / Protected Variations on a real boundary. | ADR-0002 §A.6 — `wireVersion` semver discipline. ADR-0002 §A.7 — gRPC `.proto` reuses method names and error codes. The `IServiceGateway` interface is *transport-agnostic by construction*: method signatures take DTOs, return `Task<Result>`, no pipe-specific types leak into the contract. The cost of switching is one new adapter class, not a new interface. | Claiming we *will* migrate to gRPC. We are claiming the *interface* survives the migration. |
| **9** | *"Where exactly is the cycle? You list `HistogramHelper → CanvassDesktop → HistogramMenuController → HistogramHelper` as 'suspected'. Why not measured?"* | Evidence Gap #1 (DV8/NDepend cycle report on the 8-class slice). | Acknowledge openly: the cycle suspicion is from the SonarQube Rank 10 finding; full graph-level cycle detection is owed by Day 10 (Quality Champion). The other three §4.2 boxes are already failed without it, so the architectural argument holds. We do not need the cycle report to justify the split. | Bluffing the cycle. The panel uses NDepend; they can ask us to run it. |
| **10** | *"Your sub-team is 4 people. How is the work distributed and who owns each artefact?"* | Authorship + §10.5 #4. Panel cross-checks names on ADRs and slides. | Sprint-rotated roles (TL / SM / POL / QC). Per-slide speaker assignment (Gap #14, Appendix A) lands Day 12. Each ADR has a named author (Gap #13). The four of us split the deck: TL — Sections 2 / 3 / 4 / 5.4; QC — Sections 1 / 3.4 / 4.4 / 5.5; POL — Sections 1.1 / 5.1–5.3 / 7.2; SM — Sections 6 / 7 / process narrative. | Vagueness. Have the assignment table ready by Day 12. |
| **11** | *"Where did AI help and where did it fail in this pitch?"* | §10.5 #2 — AI tool log is a *required* artefact (T8). §10.5 #6 — pitch defence may not use AI in the moment. | "Helped: ADR template scaffolding, PlantUML stub generation, prose smoothing on the binding policy. Failed: produced confidently-wrong CK number projections in early drafts; we replaced with skeleton-derived projections. Failed: ADR Alternatives sections were too generic until we forced named alternatives." Cite T8 entry. | "AI did not write any of this." It did; the policy welcomes it; pretend otherwise and the panel infers dishonesty. |
| **12** | *"What did you decide *not* to do, and why?"* | Trade-off literacy. They want to see the rejected branches. | Three answers, in order: (i) **Did not rewrite the rendering tabs** — out of scope, blast radius of UI Toolkit migration is per-panel (strangler fig). (ii) **Did not pick Rx / UniRx** — ADR-0001 Alternatives, learning-curve risk for the cohort. (iii) **Did not pick source generators for INPC on day 1** — `mvvm-binding-policy.md` §1.1, decision deferred until Day 12 (skeleton stability first). | Reciting a generic "we considered alternatives" sentence. Name the three. |

---

### B.2 — Section-by-section deep cuts (beyond the per-slide *Risk if challenged*)

These questions sit alongside the Risk lines on the slides themselves. Where a slide already covers it, we cross-reference and add only the *follow-up* the panel is likely to escalate to.

#### B.2.1 — Pain & metrics (Section 1 — 4 min slot)

- **Q: "You audited 8 classes. Why those 8 and not the full 31 files under `Assets/Scripts/UI/`?"** — Scope decision in Slide 1.1. Answer: the 8 are the *behavioural element* §6.6 names (`CanvassDesktop` plus the panels and controllers it composes). The other ~23 files are sub-panels under VR / Stats / Render that are owned by the same sub-team but were de-scoped on Day 3 (concern-mapping). The 8 represent the worst CK numbers; CodeScene's hotspot map (Day 3) confirmed they are also the highest-churn. **Trap:** "those are the ones we had time for". Be deliberate.

- **Q: "Henderson-Sellers LCOM = 0.955. Why HS and not the original CK LCOM?"** — Theory probe. Answer: original CK LCOM is unbounded and size-sensitive — incomparable across classes of different LOC. HS normalises to [0, 1] and is the variant Understand reports by default. The threshold ≤ 0.5 in §7.1 is calibrated for HS. **Trap:** confusing LCOM1/2/3/4/HS. If unsure, say "HS, as Understand reports".

- **Q: "SonarQube default CC threshold is 15. You quote 31 as the violation. What number would *you* set, and why?"** — Threshold-vs-trend literacy. Answer: SonarQube default 15 is for general business code; for UI-orchestration methods 20 is defensible; 31 is double *any* defensible threshold. We are not arguing for a stricter threshold than SonarQube; we are pointing at a method that beats every plausible threshold. **Trap:** invent a number.

- **Q: "The brief lists ISO 25010 sub-characteristics. You picked five. Why not the other three (functional suitability, performance, security)?"** — Appendix A of brief lists *maintainability* sub-characteristics. Answer: ISO 25010 has eight top-level *characteristics*; maintainability is one, with five sub-characteristics. Those five are all of it, and they map 1:1 to NFR-MOD/REU/ANA/MOF/TST in `requirements.md` §3. Functional/performance/security are out of scope per §1 of the brief — assessment title names "maintainability". **Trap:** treating the sub-characteristics list as a menu.

- **Q: "You cite Robert Martin for SRP. Define SRP precisely."** — LO4. Answer: "A class should have one reason to change" *or, more precisely (Martin 2017),* "a module should be responsible to one and only one **actor**". The actor framing is the one that survives — it is why our split puts the file-load actor (`FileTabViewModel`) and the threshold-validation actor (`SubsetBoundsViewModel`) in different classes. **Trap:** the "single thing" definition — it is wrong and Martin disowned it.

#### B.2.2 — Target architecture (Section 2 — 10 min slot)

- **Q: "The brief says micro-kernel + plug-in + client–server + layered. You drew MVVM. Where is the micro-kernel in your slide?"** — The brief's macro style; our slice is the desktop client. Answer: micro-kernel is Sub-team 1's slice (§6.1). Our slice sits *above* the kernel as one of its clients. Slide 2.1 (C4 L1) shows the kernel as a single black box with us as a client adapter. MVVM is our **client-side** internal style. **Trap:** implying we invented the architecture independently of Sub-team 1.

- **Q: "`.asmdef`-level enforcement is one rule. What stops a junior developer adding `using UnityEngine` inside a `.cs` file in the ViewModel project?"** — Defence-in-depth probe. Answer: the compiler. If the `.asmdef` does not reference `UnityEngine`, the `using` statement fails to compile. That is *not* an aspirational rule; that is the build refusing the code. Slide 2.2 + Slide 6.2. **Trap:** offering NDepend as the *primary* defence. It is the *secondary* defence (transitive imports).

- **Q: "Composition root — where is it instantiated? What constructs the composition root?"** — Recursive turtle question. Answer: a single `MonoBehaviour` on the boot scene, attached to a GameObject. Unity itself instantiates it via `Awake()` — that is the *only* line of "magic" we accept. Everywhere else: explicit `new`. `mvvm-binding-policy.md` §5. **Trap:** inventing a DI container. We are not using one.

- **Q: "You reject in-process HTTP. iDaVIE already has a Python bridge — would HTTP not align with that?"** — Domain knowledge probe. Answer: ADR-0002 Alternatives. HTTP adds a port-bind, an auth concern, and request/response framing we do not need. Named pipes are filesystem-level — same security boundary as the loaded FITS files. The Python bridge is a separate concern (ARQ-1) and can use whatever wire fits it best; the gateway interface is transport-agnostic. **Trap:** dismissing HTTP as "heavy". Name the concrete cost (port-bind + auth).

- **Q: "ADR-0003 is owed. What is its proposed Decision?"** — Evidence Gap #6. Answer: ADR-0003 covers (a) UI Toolkit as the View tech for new panels, (b) Canvas → UI Toolkit migration via strangler fig (panel-by-panel coexistence), (c) the ACL placement (in Gateway, not in View). Due Day 8 — TL. **Trap:** inventing the Decision now. Acknowledge the gap; do not paper it over.

#### B.2.3 — Worked examples (Sections 3 & 4 — 12 min slot, the centrepiece)

- **Q: "I see `FileTabViewModel` WMC 27. Show me the method list."** — Skeleton probe. Answer: open the committed `refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs` and narrate: constructor, 9 non-trivial property setters (`ImagePath`, `MaskPath`, `SelectedHduIndex`, `SelectedZAxisIndex`, `SubsetEnabled`, `RatioMode`, `IsLoading`, `HeaderText`, `ValidationMessage`), the `IsLoadable` getter, 4 command bodies (`BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`, `ClearMask`), `Dispose`, `RefreshHduHeaderAsync`, `BuildMemoryWarning`, `PopulateZAxisOptions`, `UpdateZAxisMax`, `GetAxisMaxima`, `ComputeZScale`, `MaskAxesMatchImage`, plus the notify helpers — 27 total (hand-counted Day 6, `metrics.md §2.2`). WMC 27 is borderline over the ≤ 20 domain threshold; documented remediation: extract a `FileTabCommands` helper → WMC ~22. **Trap:** quoting a number without method names; quoting the stale "~12".

- **Q: "How does the View know when `ImagePath` changes?"** — Binding mechanics. Answer: `INotifyPropertyChanged` event. The ViewModel raises `PropertyChanged("ImagePath")`; the View's `UIDocument` has a binding registered against `ImagePath` via UI Toolkit's binding system (Unity 6) or via our `UnityBinder<T>` shim (Unity 2021). `mvvm-binding-policy.md` §2.1. **Trap:** "the View polls it". That would be the old design.

- **Q: "Observer pattern for the Debug tab. Who removes the observer when the View is destroyed?"** — Lifecycle / leak probe. Answer: `DebugTabViewModel` implements `IDisposable`. The composition root holds the reference and disposes on scene unload. `mvvm-binding-policy.md` §6 (lifecycle).[EVIDENCE-GAP-B.2.3b — §6 of binding policy currently a stub; confirm before pitch] **Trap:** "garbage collection handles it". It does not — the publisher's strong reference pins the observer.

- **Q: "You picked `ObservableCollection<LogEntry>`. At 1 000 entries / sec, does that scale?"** — Performance pressure. Answer: open issue, named in `mvvm-binding-policy.md` §3.1. Three options on the table: vanilla `ObservableCollection` (acceptable to ~100/sec), ring-buffer with virtualised UI Toolkit `ListView` (target choice, decision pending), or batched `Reset` events. Decision in Sprint 2 with a load test. **Trap:** claiming the current skeleton handles 1 000/sec. It does not — be honest about the open question.

- **Q: "You did *not* refactor the Render tab. Would your architecture survive it?"** — Generality test. Answer: yes — same MVVM frame, different forces. Render tab introduces (a) Unity-side OpenGL state via a `IRenderService`, (b) per-frame updates which probably want a different ViewModel surface (continuous, not command-based). The architecture generalises; the worked example would be a new third pattern (continuous data-bind) under the same MVVM frame. **Trap:** claiming "trivially" — the panel will press on per-frame perf.

- **Q: "You wrote two worked examples but the brief (§6.6) lists File tab and Debug tab specifically. Did you choose the easy ones?"** — Defensiveness probe. Answer: the brief *names* these two — we did not choose them. Slide 4 of the spine speaks to this — File tab exercises **command + async**; Debug tab exercises **Observer + threading**. They are the *forcing* examples for different stress dimensions. The brief picked well. **Trap:** suggesting we picked the easy ones. We did not pick them.

#### B.2.4 — Testability & Unity 6 migration (Section 5 — 6 min slot)

- **Q: "70 % branch + line on ViewModel. What is your *line* coverage on `FileTabViewModel` today?"** — Coverage reality. Answer: 0 %. The skeleton tests are owed by Day 10 (Gap #11). The target is reached when the `BrowseImage` and `Load` command tests + analogues for `BrowseMask`, `ClearMask`, `SubsetBoundsViewModel` bounds-clamping, the error paths, and PropertyChanged land. We project ~75 % from the test list. **Trap:** quoting a number we have not measured.

- **Q: "Mocking framework — Moq, NSubstitute, FakeItEasy?"** — Tooling probe. Answer: Moq + NUnit. Moq because it is the framework the cohort already knows; NUnit because it is the Unity Test Framework's default. Both run outside Unity in `dotnet test`. **Trap:** discussing the framework war. The choice is conventional; defend it as conventional.

- **Q: "How do you test the `UnityBinder<T>` shim itself?"** — Recursive test probe. Answer: the shim is in the View assembly, so it falls into the **integration test layer** (UI Toolkit page-object pattern, Slide 5.1) — not the ViewModel unit-test layer. Acknowledged in `mvvm-binding-policy.md` §4.2. **Trap:** claiming we unit-test it. The shim touches `VisualElement` — Unity-bound.

- **Q: "Edit Mode vs Play Mode tests in Unity Test Framework — which layer are you targeting?"** — Unity-specific probe. Answer: Play Mode for the integration tier (UI Toolkit rendering needs the runtime); Edit Mode for any test that needs the Unity scripting runtime but no scene. Most of our coverage gate (ViewModel) is *neither* — it is `dotnet test`, no Unity at all. `test-strategy.md` §2. **Trap:** lumping all Unity tests together.

- **Q: "UI Toolkit migration. iDaVIE has ~30 panels. At one panel per sprint, that is 30 sprints. Is that realistic?"** — Migration cost reality. Answer: panels are not equal effort; the File tab + Debug tab are the prototype. Once the shim and binding policy are stable, a typical panel is 1–3 days. The strangler-fig pattern means *no big-bang freeze* — partial migration ships. The architecture's value is that this is even possible; whether it is *finished* is a roadmap decision for the maintainer team. **Trap:** committing to a timeline.

#### B.2.5 — Trade-offs & risk (Section 6 — 5 min slot)

- **Q: "What is the runtime cost of the indirection — interface dispatch, async, INPC events?"** — Perf pressure. Answer: interface dispatch on .NET is virtually free (one vtable lookup, JIT-inlined where monomorphic); INPC is one delegate invocation per change; async is the Task allocation. None matter on UI thread frequencies (~60 Hz). The cost is dwarfed by FITS I/O. **Trap:** dismissing perf — the panel cares about large-cube load times. Name the dominator (I/O).

- **Q: "MVVM ceremony — `INotifyPropertyChanged` boilerplate is hated for a reason. What is your concrete plan?"** — Adoption pain. Answer: `mvvm-binding-policy.md` §1.1 — three options: hand-rolled (canonical example), `CommunityToolkit.Mvvm.SourceGenerators` (`[ObservableProperty]` attribute, no boilerplate), or a base class with `SetField<T>`. Decision pending Day 9; current skeletons use hand-rolled for clarity. **Trap:** claiming source-gen is decided. It is not.

- **Q: "Cognitive load — your design has 3 assemblies, 6 interfaces, a binder, an ACL, a composition root. Is a first-year cohort going to follow this?"** — Adoption / teachability. Answer: Slide 6.4. Each concept maps to exactly one *named file* — skeletons are the convention exemplar. PR checklist (`mvvm-binding-policy.md` §10.3) makes the rules reviewer-facing. The complexity is upfront; the *per-panel* cost is "copy the skeleton, rename, run checklist". **Trap:** "it is not that complex". It is — own the complexity, name the mitigation.

- **Q: "You list 14 evidence gaps. Which one would *kill* this pitch if challenged hard?"** — Honest self-assessment. Answer: **Gap #1 — DV8/NDepend cycle report on the 8-class slice**, due Day 10. Without it, §4.2 #2 (no cycles) is "suspected fail" — and the panel can refuse to grade the proposal as passing the constraint. Quality Champion owns it. Day 10 is hard. **Trap:** picking a soft gap. Pick the load-bearing one.

#### B.2.6 — Cross-team & integration

- **Q: "How does Sub-team 4 (VR menus) reuse your ViewModels?"** — Reuse claim under pressure. Answer: ARQ-3 in `requirements.md` §4 — ViewModel surface is pure C#, callable from VR menu code (which is Unity-side, but a *different* View) without modification. The interface that lets VR menus open a file is `IFitsService` — the same one our `FileTabViewModel` consumes. **Trap:** claiming the View is shared. It is not — only the ViewModel and below.

- **Q: "Sub-team 7 (Persistence). What does your slice contribute to workspace state?"** — D13 deliverable. Answer: every ViewModel exposes a JSON-serialisable `State` DTO. Persistence reads/writes those DTOs without touching Unity. The state contract is delivered to Sub-team 7 by Day 9 (sprint plan §8.2 exit criterion). **Trap:** "Persistence is their problem". Name our contribution (the DTOs).

- **Q: "The Architecture Guild — what cross-cutting commitments did your sub-team make there?"** — §10.1 Layer 3. Answer: TL sits on Architecture Guild daily. Our commitments: (i) `IServiceGateway` consumer signature stability across our 8 classes, (ii) zero `UnityEngine` reach into Gateway-facing types, (iii) Day 8 integration review participation, (iv) per-PR NDepend rule conformance. Recorded in integration risk register R01–R03. **Trap:** "we attend the meetings". Name the commitments.

#### B.2.7 — Process, AI, defensibility (§10.5)

- **Q: "Show me a paragraph from the report and tell me which sentences are AI-touched."** — §10.5 #3 + the verbatim-AI clause. Answer: Open T8 log. The AI-assisted scaffolding is named per artefact. Worked example: the ADR-0001 *Context* section was AI-scaffolded; the *Decision* and *Alternatives* sections were human-authored from the start (the alternatives required named-rejection judgement AI consistently muffed). **Trap:** "AI did not touch this paragraph" if it did. The panel can re-prompt the same model and compare cadence.

- **Q: "Your role rotation — who held Tech Lead in which sprint?"** — §10.1. Answer: by stand-up notes / role log. Sprint 1 TL: <name>. Sprint 2 TL: <name>. Sprint 3 TL: <name>. Each TL signs the architecture artefacts of their sprint. [EVIDENCE-GAP-B.2.7a — confirm role roster before Day 12.] **Trap:** "we do not remember". Have the table.

- **Q: "Daily stand-up notes — show me yesterday's."** — §9.2.6 audit. Answer: open the shared file (path?), show yesterday's entry. **Trap:** finding out at the pitch that the file is stale.

- **Q: "Peer rating is confidential and AI-prohibited (§10.5 #6). Confirm you did not use AI for it."** — Policy compliance. Answer: confirm cleanly. The peer rating is in Brightspace; AI was not used. Same for individual reflections. **Trap:** rambling. One sentence, no qualifiers.

#### B.2.8 — Panel-specific angles (iDaVIE maintainer hot buttons)

- **Q: "Large FITS cubes can be tens of gigabytes. Does your interface assume in-memory data?"** — Domain reality. Answer: no. `IFitsService.OpenImageAsync` returns a `FitsFileInfo` DTO carrying an `IFitsHandle` to the open file, not bytes. Slices, axes, and subset reads are separate calls. The native plug-in keeps the cube; the client side only sees metadata + the slices it requested. This matches the existing iDaVIE architecture. **Trap:** "we did not consider it". It is the first question.

- **Q: "The native plug-in is C++. Your interface is async C#. Where does the marshalling happen, and what is your cancellation story?"** — Real interop pain. Answer: `FitsServiceAdapter` is the only class that holds `[DllImport]`. Cancellation: `IFitsService.OpenImageAsync(path, CancellationToken)`. The adapter polls the token between native calls (the native side itself is not cancellable mid-call, which is a known limitation we inherit, not introduce). Documented in ADR-0003 §Consequences [EVIDENCE-GAP-B.2.8a — confirm once ADR-0003 lands]. **Trap:** claiming we cancel mid-native-call. We do not.

- **Q: "VR side already uses a different menu system. Are you proposing to replace that too?"** — Scope creep probe. Answer: no. VR menus are Sub-team 4's scope (§6.4). Our deliverable is the *desktop* client shell. The ViewModels are *reusable* from VR menu code, which is the architectural value; whether the VR side adopts them is Sub-team 4's call. **Trap:** offering to refactor VR. Out of scope.

- **Q: "You named SteamVR in §4.2 #3 but the desktop client does not run SteamVR. Why does this constraint apply to you?"** — Constraint pedantry. Answer: it applies because shared code (e.g., `Valve.VR` types in `CanvassDesktop` today) bleeds across the desktop/VR boundary in the current codebase. Our split removes that — the ViewModel layer references neither `UnityEngine` nor `Valve.VR`. The constraint is sub-team-relevant because we are the *enforcers* of the boundary. **Trap:** "it does not really apply". It does.

- **Q: "The current `CanvassDesktop` has worked for five years. Why fix it now?"** — The classic incumbent defence. Answer: incident risk (Slide 1.4 — the `UpdateMaxValue` bug). Scaling cost (every new panel deepens the god class). Strategic driver (the brief — §1.2 says Unity 6 + Python console + workspace persistence — none of those are tractable on the current shape). The current code has worked *despite* its structure, not *because* of it. **Trap:** sounding like we are insulting the original authors. Frame as "code that worked is allowed to need refactoring as the world changes".

---

### B.3 — Hostile / curveball questions

These are the questions designed to break composure or expose unfounded confidence. Each has a one-sentence answer; if asked, do not elaborate beyond that sentence unless invited.

- **Q: "Isn't this just textbook MVVM? Where is the *novelty*?"** — **A:** Novelty is not the assessment criterion (LO4: *apply* SOLID/GRASP, not invent). The right architecture for this code is well-known; the work is in showing the §4.2 constraints *force* this shape and the CK numbers *deliver* on it.

- **Q: "If your design is so good, why did the maintainers not write it that way?"** — **A:** Hindsight. The original code shipped under different constraints (Unity 5, no UI Toolkit, single-client assumption). The brief now asks "given today's constraints, what does the shape look like" — that is what we did. We are not critiquing past judgement; we are responding to current requirements.

- **Q: "You cite Robert Martin a lot. What is your *strongest* critique of him?"** — **A:** SRP's "single reason to change" framing is operationally vague — multiple readers disagree on what counts as a "reason". The "actor" framing (Martin 2017) is sharper; we use that one. Beyond that, the SOLID acronym sometimes obscures that ISP and DIP do most of the real work; SRP is more rhetoric than rule.

- **Q: "Pick the slide you would cut if you had only 30 minutes."** — **A:** Slide 2.6 (concern map redistribution). It is supporting evidence for Slide 2.2; if compressed, 2.2 carries the load and 2.6 lives in the appendix. (Do not cut a Section 3 / 4 slide — the brief allocates 12 min to worked examples.)

- **Q: "Did AI write your slides?"** — **A:** AI drafted scaffolding; humans authored the load-bearing prose (ADR Decisions, Alternatives, speaker notes). T8 log details what AI touched and where it failed.

- **Q: "Your sub-team is allocated to 'Desktop GUI and Client Shell'. The brief lists you as Sub-team 5 (Die Boks) in §5.5 but you call yourselves Sub-team 6. Why?"** — **A:** §5.5 numbers are *allocation IDs*; §6.x numbers are *work-package IDs*. We are allocation 5, work package 6. We refer to "Sub-team 6" because that is the work package we read; we are formally Die Boks / Sub-team 5 in cohort coordination. (Resolved 2026-05-19 — see CLAUDE.md project header.)

- **Q: "I don't believe your CK projections. Re-derive WMC for `FileTabViewModel` from first principles, live."** — **A:** WMC is the sum of method complexities. The committed skeleton has 27 methods/accessors, most of cyclomatic complexity 1–2 (validation lives in `SubsetBoundsViewModel`), so WMC 27 — hand-counted Day 6 (`metrics.md §2.2`), measured not projected. That is borderline over the ≤ 20 domain threshold and we say so; documented remediation is to extract a `FileTabCommands` helper → WMC ~22.

- **Q: "What is one thing about your design you are uncertain about?"** — **A:** The `IUIDispatcher` abstraction may be heavier than necessary if UI Toolkit's binding system already marshals to the UI thread reliably under all event sources. We carry the abstraction because the Unity 2021 path (without UI Toolkit) needs it; once we are Unity-6-only it may simplify. Open question for Sprint 3.

- **Q: "Can you defend a paragraph from `mvvm-binding-policy.md` chosen at random?"** — **A:** Yes — every section author is named (Gap #13). Whichever member is asked, navigate to the section, read the claim, then point at the binding rule that operationalises it.

- **Q: "Your retro notes say sprint 1 was 'rough'. What went wrong and what changed?"** — **A:** Whatever the retro actually says (read it before the pitch). Be specific; do not generalise. [EVIDENCE-GAP-B.3a — re-read `docs/sub-team-6/deliverables/Sprint-Documents/week1-sprint-retro.md` Day 12 morning before the pitch.]

---

### B.4 — Honest "we do not know" answers

Questions for which the right answer is to acknowledge a gap cleanly. The panel rewards calibrated uncertainty over false confidence (§4.2 #1 *or documented trade-off*).

- "What is your propagation cost (DV8) number?" — Owed by Day 12 (DV8 sprint snapshot). Quality Champion is the owner.
- "What is your mocking-difficulty count today on `CanvassDesktop`?" — Owed by Day 9 (Gap #10, BNCH-6 audit).
- "Show me the C4 Level 1 diagram for your slice." — PlantUML source owed by Day 8 (Gap #2). Acknowledge openly.
- "What is your runtime overhead from the indirection layer?" — Not measured. Argued from first principles (B.2.5). Measurement is post-pitch work if pursued.
- "Does the strangler-fig migration cause UI inconsistencies for the user?" — Yes, temporarily, panel-by-panel. The mitigation is the order in which panels migrate (File / Debug first because they are textual, Render last because it shares OpenGL state). Order is in ADR-0003 [EVIDENCE-GAP-B.4a — confirm once ADR lands].

---

### B.5 — Speaker roster for Q&A (who fields which class of question)

Per §10.5 #4 — every member must be able to defend the slides assigned to them. The roster below names the *primary* fielder; a backup is implied (whoever else is on stage).

| Question class | Primary | Backup | Rationale |
|---|---|---|---|
| Architecture / ADRs / C4 / transport | TL | SM | Sprint TL signs architecture artefacts |
| CK numbers / SonarQube / NDepend output | QC | TL | Quality Champion owns metrics |
| NFRs / requirements traceability | POL | TL | PO Liaison owns requirements ↔ NFRs |
| Process / role rotation / stand-ups / AI log | SM | POL | SM owns ceremony cadence and T8 entries |
| Worked example code / skeletons | TL | QC | TL authors the skeletons |
| Test strategy / coverage gates | QC | TL | Quality Champion owns testability metrics |
| Cross-team dependencies / risk register | TL | SM | TL sits on Architecture Guild |
| Trade-off slides / "what would you cut" | SM | TL | SM has the team-narrative perspective |

[EVIDENCE-GAP-B.5a] — confirm against the Sprint 3 role allocation (CLAUDE.md mentions sprint-rotated roles; the *current* sprint's holders are the day-of fielders). Owner: SM. Due: Day 12.

---

### B.6 — What we will *not* say

Phrases to bank-avoid in Q&A. Each has been a known anti-signal in past assessments of this kind:

- "I think…" — replace with "the slide claims, and the evidence is…"
- "AI wrote it" — never; T8 names *which artefact* and *where*.
- "We did not have time" — replace with "out of scope per §6.6" or name the deliberate de-scope.
- "It works in practice" — we have no production trial; do not claim one.
- "Trivially" — nothing in this design is trivial; the panel will press.
- "Obviously" — if obvious, the panel would not be asking.
- "Maybe / probably" — convert to "the open question is…" with a date.
- "We are still figuring it out" — convert to "decision is owed by Day X, named in `…`".

---

[EVIDENCE-GAP-B.2.3a] — Confirm `FileTabViewModel` skeleton method list against committed `refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs`. Owner: TL. Due: Day 9.
[EVIDENCE-GAP-B.2.3b] — `mvvm-binding-policy.md` §6 (lifecycle) fully written, including IDisposable contract for ViewModels with stream subscriptions. Owner: TL. Due: Day 8.
[EVIDENCE-GAP-B.2.7a] — Role rotation table (who held SM/TL/POL/QC across sprints 1–3). Owner: SM. Due: Day 12.
[EVIDENCE-GAP-B.2.8a] — Cancellation contract for native interop in ADR-0003 §Consequences. Owner: TL. Due: Day 8 (rolls into Gap #6).
[EVIDENCE-GAP-B.3a] — Re-read week-1 retro the morning of the pitch; have one specific lesson ready. Owner: every member. Due: Day 12 evening.
[EVIDENCE-GAP-B.4a] — Panel migration order documented in ADR-0003. Owner: TL. Due: Day 8.
[EVIDENCE-GAP-B.5a] — Day-of role roster. Owner: SM. Due: Day 12.

---

## Cross Reference

- ADR-0001 — `docs/sub-team-6/deliverables/D2-Architecture/architecture.md` (§4)
- ADR-0002 — `docs/sub-team-6/deliverables/D2-Architecture/architecture.md` (§4)
- D1 Requirements — `docs/sub-team-6/deliverables/D1-requirements/requirements.md`
- D2 Architecture — `docs/sub-team-6/deliverables/D2-Architecture/architecture.md`
- D3 MVVM Binding Policy — `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md`
- D4 File-tab worked example — `docs/sub-team-6/deliverables/D4-worked-examples/ex1-file-tab/`
- D4 Debug-tab skeleton — `refactoring-examples/sub-team-6/debug-tab/skeleton/`
- D5 Test strategy — `docs/sub-team-6/deliverables/D5-testing/test-strategy.md`
- D9 CK baseline — `docs/sub-team-6/archived/SK_BNCH.md`
- D9 SonarQube baseline — `docs/sub-team-6/archived/SonarQube Baseline report.md`
- Deliverables checklist — `docs/sub-team-6/deliverables/deliverables-checklist.md`
- Assignment spec — `Assignment-Docs/iDaVIE_Refactoring_Assignment_FINAL_1.md`
