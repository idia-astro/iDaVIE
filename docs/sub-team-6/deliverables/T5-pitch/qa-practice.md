# Q&A Practice Set — Pitch Day 4 June 2026

> **How to use this.** Rehearsal material for the 20-min Q&A after the 40-min pitch. Each question has a *short sample answer* — the answer you would give in 20–40 seconds. Drill it until the answer flows without reading. For the strategic framing (probe / trap / fielder) go to `pitch-spine.md` Appendix B.
>
> **Rules of engagement.** Lead with the claim, then the evidence pointer, then stop. Do not elaborate unless asked. If you do not know, say "owed by Day X, owner Y" — never bluff a number.
>
> **Section index follows the pitch-spine sections.** Cross-references like `Slide 2.2` point into `pitch-spine.md`.

---

## Section 1 — Pain points with metrics

### Q1.1 — "Why ISO/IEC 25010 and not, say, the Maintainability Index?"
ISO 25010:2023 is the standard the brief names (Appendix A). It gives us five named sub-characteristics with tool-backed metrics each, instead of a single composite score that hides which dimension is failing. The Maintainability Index *is* one of the metrics we report under Modifiability — we use it; we do not let it summarise the whole picture.

### Q1.2 — "1 899 lines for `CanvassDesktop.cs` — does that include generated code or comments?"
Physical lines, including comments and blanks, as Understand counts them. Sloc-without-comments is ~1 510. The thing being criticised is not the line count itself — it is the WMC of 63 across 67 fields and 63 methods inside one `MonoBehaviour`.

### Q1.3 — "CBO 47 sounds bad. What does it mean operationally?"
Forty-seven distinct types are referenced by methods or fields of `CanvassDesktop`: 23 project classes, 13 Unity / TMPro types, 7 System types, 4 Valve.VR types. Operationally: a change anywhere in any of those 47 types can require a change here. The class is downstream of half the codebase.

### Q1.4 — "Henderson-Sellers LCOM vs the original Chidamber-Kemerer LCOM — why your variant?"
HS is normalised to [0, 1], so it is comparable across classes of different size. Original CK LCOM is unbounded. Understand reports HS by default; the §7.1 threshold of 0.5 is calibrated against the HS variant.

### Q1.5 — "Cyclomatic complexity 31 on `checkSubsetBounds` — what does that suggest in practice?"
At least 31 linearly independent paths through one method. Branch coverage on it is effectively impossible without 31+ tests. The SonarQube default threshold is 15; this method is double any defensible threshold.

### Q1.6 — "Did you find the `UpdateMaxValue → minVal` bug yourselves or did the tool find it?"
Sonar surfaced the symbol mismatch (Rank 3 in the baseline report). A human read the line and confirmed the bug. It is in `DesktopPaintController.cs` line 306 and ships today.

### Q1.7 — "Are you implying iDaVIE has untested data-corruption bugs in production?"
We are pointing at *one* method where a unit test would have caught a bug, and showing the class is structurally untestable. The implication is about the architecture, not about the development practices. The fix is architectural.

### Q1.8 — "How many other bugs of that class are in the codebase?"
We have not run an exhaustive audit. We surfaced this one because Sonar flagged a name mismatch on a setter. There is no reason to think it is unique; there is also no claim that it is widespread. The architectural argument does not need a count.

---

## Section 2 — Target architecture

### Q2.1 — "Why MVVM and not MVP?"
MVP would still split the UI from the logic but the View pulls from the Presenter via method calls — the View remains imperative. MVVM adds **binding semantics** (`INotifyPropertyChanged` + `ICommand`), so the View is *declarative*. UI Toolkit (Unity 6) has built-in binding; MVP would leave that affordance unused.

### Q2.2 — "Why not MVU (Model-View-Update / Elm-style)?"
ADR-0001 Alternatives. MVU is a strong fit conceptually but mismatches UI Toolkit's two-way binding model and adds a learning curve we cannot defend for a first-year cohort under interview pressure (§10.5 #4). Defensibility was an explicit decision criterion.

### Q2.3 — "Why three assemblies? Why not two, or four?"
The dependency budget of each layer is different. View needs `UnityEngine`; ViewModel must *not* see it; Gateway needs transport types but not Unity. Three buckets, three different forbidden-references lists. Two assemblies cannot enforce the no-Unity-in-ViewModel rule mechanically. Four would not buy a new dependency rule for the cost.

### Q2.4 — "JSON-RPC over named pipes — why not gRPC on day 1?"
Debuggability and cohort accessibility. Anyone can `tail` a JSON pipe. Protobuf needs `grpcurl` and a `.proto` to inspect. The `IServiceGateway` interface is transport-agnostic, so the gRPC switch is a composition-root decision later, not a redesign.

### Q2.5 — "Why named pipes and not TCP loopback?"
Named pipes share the file-system permission boundary — same as the FITS files the user already opened. TCP loopback adds a port-bind and an auth concern we do not need locally. Rejected with reason in ADR-0002 Alternatives.

### Q2.6 — "Where is the composition root, exactly?"
A single `MonoBehaviour` on the boot scene. Unity instantiates it via `Awake()`. That is the only object Unity constructs implicitly; everywhere else, we `new` explicitly. `mvvm-binding-policy.md` §5.

### Q2.7 — "How does the composition root know the wiring order?"
Constructor-injection order is type-driven: gateway first (no dependencies), then ViewModels (need gateway), then the binder (needs both ViewModels and the `UIDocument`). The wiring is one method in the boot `MonoBehaviour` and reads top-to-bottom.

### Q2.8 — "How do you prevent another `FindObjectOfType<>` creeping in?"
NDepend CQLinq rule forbidding the API in any assembly. The rule is sketched in `mvvm-binding-policy.md` §10.1, owed in CI by Day 10. Defence in depth: the convention is documented in §10.3 PR checklist; the rule blocks merge.

### Q2.9 — "What is the difference between an Anti-Corruption Layer and just an interface?"
The interface is the *contract*. The ACL is the *translation* — converting Unity vocabulary (`Vector3`, `GameObject`) into DTOs the ViewModel can consume. Both exist; they sit at the same boundary; they do different jobs.

### Q2.10 — "Show me a §4.2 non-negotiable and the tool that enforces it."
*"§4.2 #3 — domain code free of Unity. Tool: NDepend CQLinq rule on the `iDaVIE.Client.ViewModel` assembly. Rule is drafted in `mvvm-binding-policy.md` §10.1 and wired to CI by Day 10."* (Slide 2.5 has the full table.)

### Q2.11 — "What if NDepend and DV8 disagree on cycle detection?"
They use different models — NDepend on the static reference graph, DV8 on a DSM with runtime annotations. Both must be green. Disagreements are surfaced in the Day-12 DV8 report and reconciled before pitch freeze. We have not encountered one yet.

### Q2.12 — "Where is your C4 Level 1 diagram?"
The PlantUML source is owed by Day 8 (Gap #2 in Appendix A, owner TL). The Level-1 placement is described in `architecture.md` §3; the slide will carry the diagram once committed.

---

## Section 3 — Worked example: File tab

### Q3.1 — "Walk me through what happens today when the user clicks 'Open Cube'."
`CanvassDesktop` resolves the button via a `transform.Find` chain, calls `StandaloneFileBrowser` directly, passes the path into `FitsReader` directly, mutates fields on `VolumeCommandController` directly, and updates ~30 UI elements imperatively. No unit test can cover any step because every dependency is a concrete type held in a `MonoBehaviour`.

### Q3.2 — "Walk me through what happens after the refactor."
The View's `Browse` button is bound to `FileTabViewModel.BrowseImageCommand`; the `Load` button to `LoadCommand`. `BrowseImageCommand` calls `IFitsService.OpenImageAsync(path)`, awaits the result, and sets `ImagePath` (and populates `HduOptions`, `HeaderText`, `IsLoadable`). `LoadCommand` then builds a `LoadCubeRequest` and calls `IVolumeService.LoadCubeAsync`. The View observes the property changes via `INotifyPropertyChanged` and re-renders. The FITS read sits behind the `IFitsService` boundary; the ViewModel never sees the wire.

### Q3.3 — "Show me where `BrowseImageCommand` is defined."
In the skeleton at `refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs`. It is an `IAsyncCommand` property wired in the constructor to `BrowseImageAsync`; `ExecuteAsync` calls `_fitsService.OpenImageAsync(path)` with the path returned by the file dialog.

### Q3.4 — "What if the user clicks Open twice before the first call completes?"
`IsBusy` is set when the command begins. The command's `CanExecute` returns false while busy. UI Toolkit disables the bound button automatically. The third test in `test-strategy.md` §6 covers this contract.

### Q3.5 — "The native FITS plug-in is synchronous. How can `OpenImageAsync` be async?"
After the gateway rewire the FITS read is no longer a client-side P/Invoke. `OpenImageAsync` issues a JSON-RPC request (`file.open` → `dataset.getAxes`) over the named pipe to the server, which performs the FITS parse and returns the metadata. The call is asynchronous by nature — the pipe round-trip returns a `Task` the ViewModel awaits — so no `Task.Run`-wrapped synchronous P/Invoke is needed and the UI thread never blocks. (Phase B, the volume load, stays client-side via `VolumeServiceAdapter`.) ADR-0002 wire spec; `mvvm-binding-policy.md` §4.3.

### Q3.6 — "How do you cancel a load mid-call?"
The `OpenImageAsync` signature takes a `CancellationToken`. Between native calls we poll the token; mid-native-call cancellation is not possible because the existing C plug-in does not support it. This is an inherited limitation, not one we introduce.

### Q3.7 — "What about errors — the file is corrupt, the path is invalid?"
The adapter throws a typed exception. The ViewModel catches it in the command, sets `Error` to the message, and `IsBusy` to false. The View is bound to `Error` and shows it. The command itself never propagates the exception. `mvvm-binding-policy.md` §2.3.

### Q3.8 — "Your table lists WMC 27 for `FileTabViewModel` — that is over your ≤ 20 domain threshold. Explain."
WMC 27, hand-counted from the committed skeleton on Day 6 (`metrics.md §2.2`), not projected. It is borderline over the ≤ 20 domain threshold and we flag it as such — we do not hide it. Documented remediation: extract a `FileTabCommands` helper for the four command bodies, dropping the ViewModel to WMC ~22. Every other class in the file-tab slice is within threshold.

### Q3.9 — "Which SOLID principle does `SubsetBoundsViewModel` exemplify?"
SRP and GRASP Information Expert — the class owns the bounds *data*, so it owns the bounds *validation*. Plus DIP: the file ViewModel depends on the bounds ViewModel via interface, not concrete type.

### Q3.10 — "Why split bounds validation into its own ViewModel?"
Because bounds validation has its own *actor* (the data scientist tweaking subset extents), independent of the file-load actor. Different reasons to change → different classes. Also: bounds validation is reusable in the Render tab and the Stats tab without dragging in file-load concerns.

---

## Section 4 — Worked example: Debug tab

### Q4.1 — "What is wrong with the current Debug tab specifically?"
`DebugLogging.cs` already subscribes to a Unity event (`Application.logMessageReceived` in `OnEnable`) — it is event-driven, not polled. The problems are structural: the hook is a *static* Unity API (untestable — no fake can replace a global engine event); entries are *unstructured* strings in a non-generic `Queue` (no level, no source, no timestamp — just `[type] : message`); and one handler also does per-message disk I/O and rebuilds the whole output `StringBuilder` (O(N)) on every line. Three real defects: static/Unity-coupled, unstructured, untestable. The fix is a typed `ILogStream`/`ILogObserver` + `LogEntry` DTO fed from the server via `log.emit`.

### Q4.2 — "Why Observer pattern here?"
Log entries are produced by background threads at unpredictable rates and consumed by an arbitrary number of sinks (UI, file, network). Observer decouples producer rate from consumer count. GoF textbook fit.

### Q4.3 — "Why not just databind to a logger property?"
The Debug tab is not the only consumer. Multiple `ILogObserver` implementations register against the same `ILogStream`. Databinding is one-to-one between a property and a view; this is one-to-many between a stream and observers.

### Q4.4 — "Log entries come from background threads. How do you update the UI safely?"
`IUIDispatcher.Post(Action)` marshals to the UI thread. The dispatcher is an interface so tests can replace it with a synchronous fake. `mvvm-binding-policy.md` §4.2.

### Q4.5 — "At 1 000 entries per second, does `ObservableCollection<LogEntry>` keep up?"
Vanilla `ObservableCollection` will not — it raises one `CollectionChanged` per add and the bound `ListView` re-renders. Open decision in `mvvm-binding-policy.md` §3.1: ring-buffer with virtualised list, or batched reset events. Decision pending Sprint 2 load testing.

### Q4.6 — "Who removes the observer when the Debug tab closes?"
`DebugTabViewModel` implements `IDisposable`. The composition root holds the reference and disposes on scene unload. Without dispose, the stream's strong reference would pin the observer and leak it.

### Q4.7 — "What if the log stream blocks because a downstream observer is slow?"
Open question. The current contract assumes fast observers; a slow observer would back-pressure the producer. The mitigation is producer-side buffering — explicitly out of the current skeleton, named in the trade-off slide.

### Q4.8 — "Why is this a *worked example* if so much is open?"
The architecture is fixed (Observer, MVVM, threading discipline). The collection-strategy choice is a *tuning* decision, not a *design* decision. Worked examples demonstrate the architecture; tuning lives in Sprint 2.

### Q4.9 — "Different patterns in two examples — does the architecture handle both, or did you tune it twice?"
Both examples share the same three-assembly split, the same composition root, the same dispatcher rule, the same INPC convention. Only the *application* of the ViewModel pattern differs — Command for File tab, Observer collection for Debug tab. The architecture absorbs both unchanged.

---

## Section 5 — Testability + Unity 6 migration

### Q5.1 — "How many tests do you have today?"
Zero in the new style. The skeleton is committed; the `BrowseImage`/`Load` command tests are owed by Day 10. That is the gate at which CI begins enforcing the coverage rule.

### Q5.2 — "70 % branch + line on ViewModel — how do you guarantee that gate is hit?"
The `FileTabViewModel` is ~27 mostly-trivial methods and property accessors (CC 1–2; `metrics.md §2.2`), so the path count is bounded. A small fixed test set (happy / error / cancel / threading for each command) covers the branches by construction — the 34 committed NUnit tests already do. The number is not aspirational.

### Q5.3 — "Why NUnit and not xUnit?"
NUnit is the Unity Test Framework's default; using the same runner inside and outside Unity reduces our tooling surface. xUnit is technically newer; the cost of switching is greater than the value.

### Q5.4 — "Mocking framework — Moq, NSubstitute, FakeItEasy?"
Moq. Two reasons: cohort familiarity, and `Mock<IInterface>()` is the lowest-syntax-cost mock-creation in .NET. We are not running into the corners where NSubstitute would matter.

### Q5.5 — "How do you test the `UnityBinder<T>` shim?"
Integration layer — UI Toolkit page-object pattern, runs inside Unity. Not in the ViewModel coverage gate. The shim is tested at the boundary it serves, not in isolation.

### Q5.6 — "What is the page-object pattern, exactly?"
A class per panel that exposes intent-named methods (`ClickOpen()`, `EnterPath(s)`, `WaitForCubeLoaded()`) wrapping UI Toolkit element queries. Tests call the methods; the UXML structure changes do not break tests as long as the page object adapts.

### Q5.7 — "Unity 6 UI Toolkit — what is the migration cost in days?"
Per panel: 1–3 days once shim + binding policy are stable. iDaVIE has ~30 panels; the architecture allows panel-by-panel migration (strangler fig), so no single big-bang freeze. Total wall-clock is a roadmap decision, not a hard estimate.

### Q5.8 — "What if Unity 6's UI Toolkit binding system changes between 2026.1 and 2027?"
The binder is behind our `UnityBinder<T>` shim. Surface-level changes in the engine binding API land in the shim, not in every ViewModel. We pay the cost once.

### Q5.9 — "Can your tests run in CI without a Unity licence?"
The ViewModel + Gateway tier: yes, via `dotnet test` on any agent. The integration tier: no, Unity is required. The cost-bearing tier is the integration tier — we deliberately put the coverage gate on the cheap tier.

### Q5.10 — "What is the *mocking difficulty* metric and what is your number?"
Custom NDepend rule: count of static or Unity API calls per class. Target zero on ViewModels. Current measurement on `FileTabViewModel` skeleton: 0. On `CanvassDesktop` today: 60+. The audit table (BNCH-6) is owed Day 9.

---

## Section 6 — Trade-offs + risk

### Q6.1 — "What is the runtime cost of the indirection — interfaces, async, INPC?"
Negligible relative to FITS I/O. Interface dispatch is one vtable lookup. INPC is one delegate invocation per change. Task allocation is one heap object per async call. None matter at UI frequencies; all are dwarfed by disk + native parsing.

### Q6.2 — "What is the *memory* cost of three assemblies and the binder?"
Three assemblies cost ~30 KB load overhead each (JIT cache, type tables). The binder is one delegate per binding. On a 16 GB FITS cube, the overhead is in the noise.

### Q6.3 — "What is the biggest risk to this proposal?"
Sub-team 1's `IServiceGateway` contract shape. Until they freeze it (Day 8 integration review), our File tab worked example consumes a *proposed* contract. Mitigation: our seam to their interface is one adapter; if their shape differs, we rewrite the adapter, not the ViewModel.

### Q6.4 — "What is the *second*-biggest risk?"
Cohort adoption. A binding policy that needs reading takes time the cohort does not have. Mitigation: canonical skeletons + a one-page PR checklist. Convention by exemplar.

### Q6.5 — "Did you consider *not* refactoring at all?"
Yes — recorded in ADR-0001 Alternatives as "status quo". Rejected because §4.2 #3 and #4 are unsatisfiable on the current code without a split, and §1.4 shows real cost is already shipping (the `UpdateMaxValue` bug). Status quo is not a defensible position against the brief's constraints.

### Q6.6 — "What is the most uncertain decision in this proposal?"
Whether to commit to source-generators (`CommunityToolkit.Mvvm`) for INPC boilerplate. Decision is owed Day 9. The skeleton uses hand-rolled INPC for clarity; the production choice could go either way.

### Q6.7 — "What would you do differently if you had another two weeks?"
Run the full refactor on the File tab against production code (not just skeleton) and re-measure CK on the actual artefact. Currently the numbers are projected from skeleton size; an end-to-end run would convert projection to measurement.

### Q6.8 — "What did Sub-team 1 push back on?"
The shape of the gateway's notification channel — they prefer a single server-pushed `IObservable<Event>` over our per-topic subscribe API. Resolution: still open; both shapes satisfy our ViewModel needs. Day 8 review.

---

## Section 7 — Summary + forward look

### Q7.1 — "If we approve this, what do you build first?"
Composition root + `IServiceGateway` adapter + `FileTabViewModel` against the real `IFitsService`. The File tab is the smallest end-to-end vertical that exercises the whole architecture. Two-week sprint, single owner.

### Q7.2 — "How does the Python console actually use a C# ViewModel?"
The ViewModel layer is `System.*`-only — it can be hosted in any .NET process. A Python bridge (Python.NET or a thin RPC) instantiates the same ViewModel objects. They behave identically to the Unity case because no Unity types touch them. ARQ-1 in `requirements.md` §4.

### Q7.3 — "How does workspace persistence work?"
Every ViewModel exposes a JSON-serialisable `State` DTO. Sub-team 7 (Persistence) reads/writes those DTOs without touching Unity. Restore is a reverse population of the DTOs into ViewModels at boot. ARQ-2.

### Q7.4 — "What is the one slide you would defend on its own to win this assessment?"
Slide 2.5 — the §4.2 enforcement table. Every non-negotiable mapped to a tool that refuses the violating PR. That converts the assignment's constraints from policy into mechanism. That is what the brief actually asks for.

---

## Cross-cutting — process, AI, integration

### QC.1 — "Show me your AI usage log."
T8 log. Per artefact: tool, model, where it helped, where it failed. Worked example: ADR-0001 Context was AI-scaffolded; Decision and Alternatives were human-authored because AI produced too-generic alternatives. CK projections were initially AI-generated and *wrong* — we replaced with skeleton-derived numbers.

### QC.2 — "Did AI write this slide / paragraph?"
*(Identify which.)* If AI-scaffolded: "scaffolded by Claude, edited by [name], reviewed by [name]." If human-only: "human-authored from the start because [reason]." Never claim AI-free if it is not.

### QC.3 — "Who in your sub-team wrote the architecture document?"
TL of Sprint 2 drafted sections 1–8; QC of Sprint 2 wrote the metrics appendix. Sprint 3 TL took over for Section 9 (migration) and the integration review. Names are on the document footer.

### QC.4 — "How did you communicate with Sub-team 1?"
Daily Architecture Guild stand-up at 09:15 (TL attends). Plus a weekly 1-hour review on Wednesdays. Integration risk register entries R01 (gateway shape) and R02 (notification channel) are the artefacts. Day 8 cross-sub-team integration review is the lock-in.

### QC.5 — "What happened in your sprint retros?"
*(Read the retro before the pitch.)* Sprint 1 lesson: under-scoped the ADR effort; Sprint 2 we time-boxed ADR drafting. Sprint 2 lesson: too much scope on Day 1, slipped Day 3 — Sprint 3 we front-loaded the spine.

### QC.6 — "Why are you sub-team 6 in some docs and sub-team 5 in the allocation table?"
Section 5.5 numbers are *allocation IDs*; Section 6.x numbers are *work-package IDs*. We are allocation 5 (Die Boks) covering work package 6 (Desktop GUI). We use "Sub-team 6" internally because we read §6.6. Resolved 2026-05-19.

### QC.7 — "Confirm AI was not used in peer rating or individual reflection."
Confirmed. §10.5 #6 is explicit; neither was AI-assisted. Same for live pitch defence.

### QC.8 — "What is the strongest evidence in this entire proposal?"
The §4.2 enforcement table (Slide 2.5) tied to the CK before/after delta (Slide 3.4). The first shows the rules are mechanically enforced; the second shows the numbers move. Together they are not a promise — they are a measured trajectory.

---

## Hostile / curveball — the warm-up answers

### QH.1 — "This is just textbook MVVM. Where is the novelty?"
The assessment criterion is *applying* SOLID and GRASP (LO4), not inventing patterns. The work is showing that §4.2's constraints force this shape and the numbers deliver on it. Conventional was the right answer.

### QH.2 — "Why did the original maintainers not write it this way?"
Different constraints — Unity 5, no UI Toolkit, single-client assumption. The brief now asks "given today's constraints, what does the shape look like." We answered today's question, not yesterday's.

### QH.3 — "Pick the slide you would cut."
Slide 2.6 (concern map redistribution). It supports Slide 2.2; if compressed, 2.2 carries the load and 2.6 lives in the appendix. Never cut a Section 3 or 4 slide — the brief allocates 12 minutes there.

### QH.4 — "I do not believe your CK numbers. Re-derive WMC for `FileTabViewModel` live."
WMC 27, hand-counted from the committed skeleton: constructor + ~9 non-trivial property setters + 4 command bodies + the HDU/Z-axis/memory helpers + the notify methods (`metrics.md §2.2`). That is borderline **over** the ≤ 20 domain threshold — we do not hide it. Documented remediation: extract a `FileTabCommands` helper, dropping the ViewModel to WMC ~22.

### QH.5 — "Defend your worst slide."
*(Be ready: probably Slide 2.7 if ADR-0003 has not landed yet, or Slide 1.3 if the cycle report is still pending.)* The slide is honest about the gap; the architectural argument does not depend on the missing artefact; the gap is owed by [date] with named owner.

### QH.6 — "What is one thing you got wrong this sprint?"
*(Pick a real one.)* Underestimated the time the binding policy needed; what was scoped as a half-day on Day 6 was three days. Sprint 3 we descoped the §6 lifecycle section and rolled it forward.

### QH.7 — "If I gave you a fifth sprint, what would you do with it?"
Run the File tab refactor on production code, measure real CK numbers against projections, and write up the delta as the empirical wing of the proposal. Currently the numbers are skeleton-derived; an end-to-end pass would convert projection to measurement.

### QH.8 — "What is your single weakest argument?"
The cycle-detection evidence — though it is now partly closed. As of 2026-05-28 the after-state *assembly-level* acyclicity is tool-backed: a clean `dotnet build` of all 10 pure-C# skeleton/adapter/test projects passes, and MSBuild refuses to build a cyclic `<ProjectReference>` graph, so the build itself is the proof, backed by the documented reference graph ([`../other/cycles-report.md`](../other/cycles-report.md)). What is still owed for Day 10 is (a) *class-level* DV8/NDepend confirmation on the after-state, and (b) tool confirmation of the 2 before-state cycles currently documented only by a manual DSM (BNCH-4). So §4.2 #2 is now "2 cycles identified manually, tool confirmation owed" on the before-state and "assembly-level pass (tool-backed), class-level pass projected" on the after-state — not the blanket "both unverified" it was before.

---

## How to drill this

1. **One member, one section.** Each of the four of us takes Section 1+2 / 3 / 4 / 5+6+7. Drill until you can answer each question without rereading.
2. **Rotate the asker.** The asker reads the question; the answerer responds without looking at the answer. The asker grades pass/fail against the sample.
3. **Then cross-train.** Trade sections. By Day 12 every member should be able to field every question — that is the §10.5 #4 bar.
4. **Time the answer.** Target 20–40 seconds. Longer = rambling. Shorter = leaving information on the table.
5. **One pass through the curveball section as a group.** These are the answers we cannot afford to compose in the moment.
