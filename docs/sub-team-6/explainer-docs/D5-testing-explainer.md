# D5 Testing — Vocabulary Explainer (Sub-team 6 / Team Alpha)

**Purpose.** A plain-language glossary of the testing terms we use, each tied to
the *exact place in our own code or docs* it points at. If a panel member asks
"what do you mean by that?", the answer is in the **Where it lives** and
**Defence** lines. Every term here maps to a file we actually wrote — nothing is
decoration, and nothing is borrowed jargon we don't practise.

> **AI-policy note.** This explainer was AI-assisted (reconstructed from our own
> `D5-testing/` docs and the committed suites under
> `refactoring-examples/sub-team-6/`) and is logged in `ai-log.md`. AI may **not**
> be used for the live pitch/interview defence — this is preparation, not a
> script. Everything below must be defensible by a human author.

**Companion to:** `D5-testing/test-strategy.md` (the deliverable),
`viewmodel-unit-tests.md` (Tier 1), `ui-toolkit.md` (Tier 3),
`coverage-report.md`.

Scope note: this only covers concepts that are real in our slice (the desktop
client — File tab, Debug tab, and the service-gateway contract). Terms that
sound good but we don't actually do (characterization tests, mutation testing,
the "test diamond") are deliberately left out — using them would not survive a
follow-up question.

---

## 1. The one-sentence story

> The MVVM-over-gateway split exists **so the logic became testable without
> starting Unity** — and we can prove it: the before-state `CanvassDesktop`
> scored **205** on the mocking-difficulty index; the after-state ViewModel
> layer scores **0**, enforced by the project file, not by promise.

Every term below is a name for one move we made to get from 205 to 0.

---

## 2. The headline term: mocking-difficulty index (our own metric)

- **What it means.** A count of the call sites in a class that *can't be faked*
  in a plain test — scene-graph traversals (`transform.Find(...)`),
  `FindObjectOfType`, and native `[DllImport]` / `StandaloneFileBrowser` calls.
  The higher the number, the more of Unity you must boot to test the class.
- **Where it lives.** `BNCH-6.md` (the baseline) and `test-strategy.md` §2/§9.
  **Before:** 205 (163 scene-graph + 6 `FindObjectOfType` + 36 P/Invoke).
  **After (ViewModel layer):** 0.
- **Defence.** "We didn't just claim the God-class was untestable — we counted
  the exact things that made it untestable, and our refactor drives that count
  to zero by construction." This is the strongest single number we own; lead
  with it.

---

## 3. The big structural ideas

- **Headless / engine-free / no-Unity unit tests.** Tests that run on plain
  .NET, no Unity Editor, no PlayMode, no headset.
  **Where it lives:** every test project under `refactoring-examples/sub-team-6/`
  is a standalone .NET project; `dotnet test` runs the whole Tier 1 + Tier 2
  suite (**95 tests, ~200 ms** — 76 Tier-1 pass anywhere; the 19 Tier-2 need a
  host without Smart App Control, see `../deliverables/D5-testing/test-strategy.md` §4.4). The skeleton csproj has *no* `UnityEngine`
  reference, so a `using UnityEngine` is a **compile error, not a runtime
  surprise**.
  **Defence.** "Anti-corruption isn't a guideline for us — it's enforced by the
  project file."

- **Four-tier test pyramid.** Our named layering (not a generic 'pyramid'):
  | Tier | Under test | Unity? | Gated? |
  |---|---|---|---|
  | 1 — ViewModel unit | pure-C# ViewModels | No | ≥70% branch+line — met locally; CI gate Day-10 |
  | 2 — Gateway & adapter | wire framing, `IServiceGateway`, gateway proxies | No | tracked |
  | 3 — View integration | UXML panels via page-objects | Yes | tracked, not gated |
  | 4 — Smoke | full shell, real file, VR scene | Yes | manual pass/fail |
  **Defence.** "Each tier is a separate project so Unity literally cannot bleed
  upward into the gated layers."

- **Seam.** A place to swap behaviour without editing the code under test. Our
  seams are interfaces (Tiers 1–2) and page objects (Tier 3).
  **Where it lives:** `IServiceGateway`, `IFitsService`, `IVolumeService`,
  `IFileDialogService`, `IMemoryProbe`, `IFitsHandle`, `ILogStream`,
  `ILogObserver`, `IInteractionGateway`; `ui-toolkit.md` even calls the page
  object "the seam".
  **Defence.** "Every boundary is an interface with ≥1 double — architectural
  non-negotiable #4."

- **Humble Object.** (Our name for what the split does — not a term already in
  the docs, but accurate.) Push logic out of the hard-to-test Unity shell into a
  plain object that *is* testable; what's left in the shell is too thin to need a
  unit test.
  **Where it lives:** `FileTabViewModel.cs` / `DebugTabViewModel.cs` (testable)
  vs `adapters/FileTabView.cs` / `adapters/DebugTabView.cs` (the thin humble
  part).

---

## 4. Test doubles — use the precise word

Umbrella term: **test double**. We hand-write ours (no Moq in the committed
suites — see the note in §8). The specific kinds, with exact files:

- **Stub** — returns pre-set data so the test can proceed.
  **Where it lives:** `StubFitsService`, `StubFileDialogService`,
  `StubMemoryProbe`, `StubFitsHandle` in `FileTabViewModelTests.cs`.
  **Defence.** "We stub the file-dialog seam so the test returns the path we
  want — it never opens a real OS dialog."

- **Fake** — a *working* in-memory implementation, not just a canned answer.
  **Where it lives:** `FakeGateway.cs` (in-memory `IServiceGateway` with real
  JSON round-tripping), `FakeLogStream` (in `DebugTabTests.cs`),
  `FakeInteractionGateway.cs` (Sub-team 4 boundary).
  **Defence.** "`FakeGateway` is a *fake*, not a mock — it behaves like the real
  gateway in memory, so the adapter test exercises real call flow without a pipe
  or a server."

- **Spy / recording double** — records calls so the test can assert on them.
  **Where it lives:** `FakeGateway.Sent` (records every method + params element);
  `StubVolumeService.LastRequest` (captures the `LoadCubeRequest`).
  **Defence.** "We assert the adapter sent `file.open` → `dataset.getAxes` in
  that order with the exact camelCase params — that's a spy on the wire."

> Getting "stub" vs "fake" right is the cheapest way to read as competent rather
> than buzzword-y. We don't say "mock" generically.

---

## 5. What each tier actually covers (point at the file)

- **ViewModel unit test (Tier 1).** The ViewModel with all collaborators
  doubled. These are our **domain tests** — the ones that hit the ≥70% gate.
  **Where it lives:** `FileTabViewModelTests.cs` (47 tests),
  `DebugTabTests.cs` (29 tests). Naming:
  `MethodUnderTest_Scenario_ExpectedBehaviour`; structure: **Arrange–Act–Assert**.

- **Adapter / wire-shape test (Tier 2).** Tests the thin class translating our
  interface to JSON-RPC, against `FakeGateway`.
  **Where it lives:** `FitsServiceAdapterTests.cs` (pins `file.open` →
  `dataset.getAxes` → `dataset.getHeader` → `file.close`, params shape, and
  best-effort cleanup on failure), `GatewayLogStreamAdapterTests.cs` (pins
  `log.emit` → `LogEntry`).
  **Defence.** "These close audit findings F9/F10 — they prove the transport
  contract has a real consumer."

- **Protocol / framing test (Tier 2).** Pure-byte tests on the wire format, each
  quoting the ADR-0002 clause it pins.
  **Where it lives:** `LengthPrefixFramingTests.cs` (round-trip, `0\n`
  empty-frame, leading-zero/CR/non-digit rejection, mid-payload truncation),
  `FakeGatewayTests.cs`.
  **Defence.** "Framing is pure byte logic — exhaustively testable against a
  `MemoryStream`, no transport needed."

- **Contract double (cross-team).** We hold a double of another team's side of an
  interface so we can build before they exist.
  **Where it lives:** `IServiceGateway` + `FakeGateway` (Sub-team 1);
  `IInteractionGateway` + `FakeInteractionGateway` (Sub-team 4).

- **View integration via Page Object (Tier 3).** One `{Panel}Page` per UXML
  panel; selector strings (`root.Q<T>("name")`) live *only* in the page, intent
  methods (`ClickLoad()`) face the test.
  **Where it lives:** spec in `ui-toolkit.md` (`FileTabPage`, `DebugTabPage`).
  Design-only — the rig isn't checked in (no `Assets/` changes allowed).

- **Smoke test (Tier 4).** Manual pass/fail checklist (SM-1…SM-7) over the full
  shell with a real file and VR scene. `test-strategy.md` §6.

---

## 6. Patterns we can name (and point at)

- **Observer pattern.** The Debug tab *subscribes* to a log stream rather than
  polling — it reacts, it doesn't own the source.
  **Where it lives:** `ILogStream` / `ILogObserver` / `LogStream` →
  `DebugTabViewModel` (`Subscribe(this)` / `OnNext`). Tested in `DebugTabTests.cs`
  (subscribe, order, dispose-unsubscribes, 2000-entry cap).
  **Defence.** "Debug tab as Observer of a structured logging stream — mandatory
  worked-example #2."

- **Command pattern.** A user action is an executable object, not a hardwired
  call into a native plugin.
  **Where it lives:** `ICommand.cs`, `RelayCommand`, `AsyncRelayCommand`;
  `BrowseImageCommand` / `LoadCommand` on `FileTabViewModel`. The bodies are
  private, so the View *can only* reach them via the command — and the test does
  too (`await vm.LoadCommand.ExecuteAsync()`).
  **Defence.** "File tab went from a direct native-plugin call to a ViewModel
  command through the gateway — worked-example #1. `CanExecute` even drives the
  button's enabled state, so the View makes no decisions."

- **Dependency Inversion (DIP) + composition root.** ViewModels depend on
  interfaces; the concrete adapter is supplied once at startup, never `new`-ed
  inside the VM.
  **Where it lives:** `FileTabCompositionRoot.cs`, `DebugTabCompositionRoot.cs`.
  **Defence.** "Pushing construction to the composition root is exactly what
  makes the doubles injectable in tests."

---

## 7. How we judge the tests

- **Line vs branch coverage — and why we lead with branch.** *Line* coverage =
  the fraction of executable lines that ran at least once. *Branch* coverage =
  the fraction of decision-*edges* (each `if` / `&&` / `?:` / `switch` / loop's
  true **and** false outcome) that were taken. Branch is the stricter, more
  honest number: a line like `if (info != null && info.NAxis >= 2)` can be 100%
  line-covered by one happy-path test while its `null` and `NAxis < 2` edges are
  never taken — so its branch coverage is far lower. That gap is exactly why
  FileTabSkeleton reads **89.4% line but only 77.2% branch**, and why the Day-8
  follow-up added 13 tests aimed at specific uncovered edges (null-guards, NAXIS
  mismatch, BrowseMask cancel/replace).
  **Where it lives:** `coverage-report.md`. Both numbers are produced by
  **Coverlet 6.0.2** (instruments the assemblies during `dotnet test
  --collect:"XPlat Code Coverage"` → per-project `coverage.cobertura.xml`) and
  merged by **ReportGenerator 5.5.10**. Gate-target assemblies: DebugTabSkeleton
  100/100, FileTabSkeleton 89.4/77.2 — both clear ≥70% branch.
  **How it's monitored — be precise.** Today this runs **locally** (the
  `test-strategy.md` §7.3 script) and the numbers are frozen into
  `coverage-report.md`. There is **no CI coverage job yet**: `ci.yaml` never runs
  `dotnet test` on the ViewModel projects and has no ≥70% threshold step — wiring
  that gate in is the Quality Guild's Day-10 task (`test-strategy.md` §7.1–7.2).
  So ≥70% is a target we **measured and met**, not yet one CI **enforces**.
  **Defence.** "We don't chase 100% — the gateway transport is tracked not gated
  (≈41%), and `JsonRpcPipeGateway`'s end-to-end pipe paths stay untested until a
  Sub-team 1 server exists. The brief bans speculative numbers; we report what we
  measured, and we're explicit that the gate is local-until-Day-10, not yet a CI
  check."

- **Determinism via injected clock.** Time-dependent logic takes an
  `ISystemClock` so tests don't depend on wall-clock.
  **Where it lives:** `test-strategy.md` §9 rule 4.

- **Settle rule (flake avoidance).** Tier-3 tests `yield return null` once to
  flush UI Toolkit's binding pass before asserting; polling sleeps are forbidden.
  **Where it lives:** `ui-toolkit.md` §7.
  **Defence.** "We design out flake from day one — no `Thread.Sleep` in tests."

---

## 8. The honest caveats (say these before you're caught out)

- **View is not unit-tested.** UI Toolkit binding is exercised at Tier 3 / Tier
  4, tracked but not gated. Testing the engine's binding is testing Unity, not
  our logic (`ui-toolkit.md` §8).
- **Doc drift — Moq vs hand-written doubles (verified 2026-05-31).** The
  *committed* suites use hand-written stubs/fakes only — a `grep` over
  `refactoring-examples/sub-team-6/` finds **zero** `using Moq`, `new Mock<>`, or
  Moq `PackageReference`. So **§4 above is the accurate account.** The stale
  "Moq 4" claim lives in `test-strategy.md` (§2 tier table and the §3.2 code
  sample, `new Mock<IFitsService>()`) and `viewmodel-unit-tests.md`. Fix it
  *there* before the freeze: either rewrite those to the hand-rolled doubles
  (true, zero extra deps) or actually adopt Moq. Don't show Moq on a slide and
  then open a file with none.
- **Path drift — fixed 2026-05-31.** The strategy docs used to reference
  `contracts/`; the real folder is `contracts-team1/`. Every live-doc reference
  has been corrected (the only surviving `contracts/` mentions are in
  `archived/`). Kept here only as a record of the catch.
- **We do not retrofit tests onto `CanvassDesktop`.** We can't (that's the whole
  point of the 205 score). The before/after argument rests on the
  mocking-difficulty delta and CK metrics, **not** on characterization tests —
  don't claim we pinned the old behaviour with tests, because we didn't.

---

## Quick map: term → file (cheat sheet)

| Term | Point at this |
|---|---|
| Mocking-difficulty index (205→0) | `BNCH-6.md`, `test-strategy.md` §2/§9 |
| Headless / no-Unity unit test | any test `.csproj` (net, no `UnityEngine`) |
| Four-tier pyramid | `test-strategy.md` §2 |
| Humble Object | `FileTabViewModel.cs` vs `adapters/FileTabView.cs` |
| Seam | the `I*.cs` interfaces; page objects |
| Stub | `StubFitsService` etc. in `FileTabViewModelTests.cs` |
| Fake | `FakeGateway.cs`, `FakeLogStream`, `FakeInteractionGateway.cs` |
| Spy / recording | `FakeGateway.Sent`, `StubVolumeService.LastRequest` |
| Wire-shape / adapter test | `FitsServiceAdapterTests.cs`, `GatewayLogStreamAdapterTests.cs` |
| Protocol / framing test | `LengthPrefixFramingTests.cs` |
| Contract double (cross-team) | `FakeGateway`, `FakeInteractionGateway` |
| Observer | `ILogStream`/`ILogObserver` → `DebugTabViewModel` |
| Command | `ICommand.cs`, `RelayCommand` → `FileTabViewModel` |
| DIP / composition root | `FileTabCompositionRoot.cs` |
| Page Object | `ui-toolkit.md` (`FileTabPage`, `DebugTabPage`) |
| Coverage floor / Coverlet | `coverage-report.md` |
| AAA + naming convention | `viewmodel-unit-tests.md` §5 |

**Framework, for the record:** NUnit 3, .NET standalone projects, Coverlet +
ReportGenerator for coverage. Tier 3 would use Unity Test Framework (Play Mode).

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal explainer for the D5 test
strategy. Not a deliverable itself.*
