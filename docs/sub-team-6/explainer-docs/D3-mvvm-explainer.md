# D3 MVVM Binding Policy — Explainer & Defence Prep

**Purpose:** internal study aid for Sub-team 6 (Die Boks / Team Alpha). It explains the *concepts* behind the D3 deliverable (`mvvm-binding-policy.md`) — the mental model, the design decisions, the worked-example mechanics, and the questions a panel is likely to probe — so every author can defend the binding policy on their own.

> **AI-policy note.** This explainer was AI-assisted (reconstructing rationale from our own `mvvm-binding-policy.md`, `D2-Architecture/architecture.md`, and the `refactoring-examples/sub-team-6/file-tab/` + `debug-tab/` skeleton code) and is logged in `ai-log.md`. Per the brief, AI may **not** be used for the live pitch/interview defence — this document is preparation, not a script. Everything below must be defensible by a human author.

**Companion to:** `deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md` (the deliverable), `deliverables/D2-Architecture/architecture.md` (the architecture it implements), and the compiled worked examples under `refactoring-examples/sub-team-6/`.

---

## 1. What D3 is, and what's in it

D3 is the **MVVM binding policy** — the named §6.6 deliverable. Its job is to state *the rules* by which the desktop View binds to the ViewModel, plus the architecture and worked examples that make those rules concrete. Contents:

| Block | What it covers |
|---|---|
| §1 Why MVVM | The problem: `CanvassDesktop` is a 1,899-line God-object (8 concerns); Day-2 CK violations (WMC 63, CBO 47, RFC 118, LCOM 0.955); the three forces (testability, Unity 6 migration, §4.2 constraints); and the 5 rejected alternatives (MVP, MVU, MVC, Rx, status quo). |
| §2 Architecture | The three-assembly split (View / ViewModel / Gateway), CI-enforced dependency direction, layer map, and the "where is the Model" callout. |
| §3 Per-tab design | Owns / does-NOT-own + "split from current code" for File, Debug, Rendering, Stats, Sources. File & Debug are full worked examples; Stats & Sources are **design gestures**. |
| §4 Binding mechanics | **The actual policy:** `INotifyPropertyChanged`, `ICommand`, `ObservableCollection`, the `IUIDispatcher` threading model, and the Unity-version data-binding shim. |
| §5–§6 Composition root + ACL | How the object graph is wired once at startup (no `FindObjectOfType`); the anti-corruption boundary. |
| §7 Worked examples | File tab (command round-trip) and Debug tab (Observer of a log stream). |
| §8–§13 | Benefits (CK projections), risks, **forbidden patterns**, **CI enforcement**, glossary, references. |

The genuinely *binding-policy*-specific content is **§4, §10, §11**. The rest is context that overlaps D2/D4 (a known structural choice — see §7 caveats below).

---

## 2. The core mental model — MVVM as we apply it

### 2.1 Three layers, one rule each

- **View** (`iDaVIE.Client.View`, Unity) — *only* binds to VM properties and forwards UI events. No logic, no decisions, no I/O.
- **ViewModel** (`iDaVIE.Client.ViewModel`, pure C#) — owns **state + decisions + commands**. No `UnityEngine`, no `SteamVR`, no `[DllImport]`.
- **Gateway** (`iDaVIE.Client.Gateway`, the ACL) — the access path to the server and to Unity. Two kinds of class: **gateway proxies** (pure C#, own the JSON-RPC wire shape) and **Unity adapters** (the only classes allowed to touch the Unity SDK).

### 2.2 Where the Model lives (the question that trips people up)

This is a **client–server MVVM**, so there is **no client `Model` assembly**. The authoritative domain model — voxel buffers, FITS parsing, WCS transforms, analysis, the source catalogue — lives **server-side** (Sub-team 1's kernel + native plugins). On the client the Model appears only as **immutable DTOs** returned through the Gateway (`FitsFileInfo`, `LogEntry`, `HduInfo`…).

**The Gateway is not the Model** — it's the pipe; the DTOs flowing through it are the Model. (This is now stated in D3 §2.2's callout.)

The headline: **today the Model lives *in* the client** (`CanvassDesktop` P/Invokes CFITSIO and holds the cube in a managed `float[]` on the main thread — the cause of B-02 and B-08). **The refactor moves it server-side**; the client keeps only DTOs. "Direct file I/O that belongs server-side" (§6.6) literally means "move the Model out of the client."

### 2.3 Dependency direction (CI-enforced)

```
iDaVIE.Client.View → iDaVIE.Client.ViewModel → iDaVIE.Client.Gateway
```

Reverse references fail CI (NDepend). This is how §4.2.2 (no cycles) and §4.2.3 (no Unity in domain) are *enforced*, not just promised — the ViewModel assembly lists no Unity reference, so a `using UnityEngine` in a VM won't compile.

---

## 3. Key concepts a panel will probe

### 3.1 The Gateway is not the Model
See §2.2. Defence line: *"The Gateway is our anti-corruption/service layer. The domain Model is server-side; on the client it surfaces as immutable DTOs through the Gateway. No client Model assembly, by design."*

### 3.2 ViewModels are classes; `I` is the interface convention
- The **ViewModel** is a concrete class: `FileTabViewModel` (no `I`).
- `IFileTabViewModel` is the **interface** it implements — the boundary the View binds to (§4.2.4 requires every boundary to be an interface + test double).
- The `I` prefix marks *interfaces* generally (`ICommand`, `IFitsService`, `IDisposable`), not ViewModels.
- **Not every VM gets an interface:** `SubsetBoundsViewModel` is exposed as a concrete type because it's an internal child of `FileTabViewModel`, not an independently-bound boundary.

### 3.3 The `ICommand` rule — and how it's *enforced*
The View invokes ViewModel behaviour **only** through commands, never `viewModel.DoTheThing()`. In our worked example this isn't just convention:
- The command bodies (`BrowseImageAsync`, `LoadAsync`) are **`private`** on `FileTabViewModel`.
- The interface `IFileTabViewModel` exposes actions **only** as command properties (`BrowseImageCommand`, `LoadCommand`…), no action methods.
- So `viewModel.LoadAsync()` **won't compile** — the only reach is `LoadCommand.ExecuteAsync()`.

Why it earns its keep: testability (`await vm.LoadCommand.ExecuteAsync()` against stubs, no Unity), auto-gating (`CanExecute`/`CanExecuteChanged` drives button enabled-state — the View makes no decisions), and uniformity (every action is a sync `ICommand` or async `IAsyncCommand`). Distinction: *data* flows via property get/set + `PropertyChanged`; *behaviour* flows via `ICommand`. The rule governs behaviour, not bound data.

### 3.4 Unity `Update()` ≠ MVU `update` (a word collision)
- **Unity `Update()`** is a per-frame `MonoBehaviour` engine-lifecycle callback. The current code abuses it to **poll** the renderer every frame and push values to sliders — an anti-pattern.
- **MVU's `update`** is a pure `update(model, message) → newModel` function, called per message.
- They share only the word. The refactor replaces the poll with **event-driven property binding** (`IVolumeService.ThresholdChanged` → VM property → View) — that's **MVVM**, not MVU. We explicitly **rejected MVU** (§1.4). *Don't claim MVU at the pitch.* (D3 §3.3 now carries a parenthetical making this explicit.)

### 3.5 "Owns / does NOT own" — the SRP cut line
The rule: **the ViewModel owns state and decisions (pure C#); it does not own mechanisms that touch Unity, the OS, or native code.** Those mechanisms are pushed across a boundary:
- `Transform.Find(...)` → the **View** (replaced by binding).
- `StandaloneFileBrowser` → behind **`IFileDialogService`** (Unity adapter).
- `FitsReader` P/Invoke → behind **`IFitsService`** (→ server).

Those three "does NOT own" items are exactly the three coupling smells of the as-is `CanvassDesktop` (scene-graph, OS dialog, native interop).

### 3.6 How to read a "Split from current code" table
Each table takes one tangled God-method and shows its fragments landing in their correct layer. The pattern across File/Stats:
- **UI mechanics** (clicks, text display) → View, usually via binding not code.
- **Decisions & state** (`IsLoadable`, bounds validation, threshold sync) → ViewModel, pure testable C#.
- **Actual I/O / native** → behind an interface, often server-side.

The Stats table additionally shows the **cross-tab fix**: Render and Stats stop sharing `CanvassDesktop` fields and instead both observe `IVolumeService` (single source of truth, broadcast via events). That is what makes **B-03** structurally fixable rather than patched.

### 3.7 Binding mechanics (the actual policy, §4)
- **`INotifyPropertyChanged`** on every observable VM (via `ViewModelBase.SetField` + `[CallerMemberName]`). Forbidden: bare public fields, side-effects in getters, heavy work in setters.
- **`ICommand`** is the only action surface (§3.3 above).
- **`ObservableCollection<T>`** for bound lists; mutate in place, never replace the instance; bulk-load atomically.
- **Threading:** all bound mutations on the Unity main thread. Background gateway callbacks marshal via an injected **`IUIDispatcher`** — *not* the forbidden `UnityMainThreadDispatcher.Instance` static singleton. (Composition root constructs the concrete `UnityUIDispatcher`.)
- **Version shim:** Unity 6 has native UI Toolkit binding; for legacy 2021.3 a thin `UnityBinder<T>` subscribes to `PropertyChanged` and sets the widget. Lives in the View assembly only.

---

## 4. How a click flows end-to-end (the File-tab worked example)

**File map (the three layers as real files):**

| Layer | Files |
|---|---|
| View (Unity) | `adapters/FileTabView.cs`, `adapters/FileTabCompositionRoot.cs` |
| ViewModel (pure C#) | `skeleton/FileTabViewModel.cs`, `IFileTabViewModel.cs`, `ICommand.cs`, `SubsetBoundsViewModel.cs`, DTOs |
| Gateway (ACL) | `adapters/FitsServiceAdapter.cs` (gateway proxy → JSON-RPC), `FileDialogServiceAdapter.cs`, `VolumeServiceAdapter.cs`, `MemoryProbeAdapter.cs` |

**One Browse/Load click, top to bottom:**

```
click → FileTabView (forwards click)
      → BrowseImageCommand.ExecuteAsync()      ← View→VM via a command, not a method
      → IFileDialogService.PickFileAsync(...)   ← interface (stubbed in tests)
      → IFitsService.OpenImageAsync(path)       ← interface; adapter → file.open + dataset.getAxes (JSON-RPC) → server
      → returns FitsFileInfo DTO                ← the client-side Model projection
      → VM updates HduOptions / IsLoadable / HeaderText  (each setter Notifies)
      → PropertyChanged → FileTabView re-renders; Load button auto-enables via CanExecuteChanged
```

Directionality to remember:
- **View → VM:** behaviour via commands; data via property setters.
- **VM → View:** only via `PropertyChanged` (VM never references the View).
- **VM → Gateway:** only via injected interfaces (`IFitsService`, `IVolumeService`…), wired in `FileTabCompositionRoot`.
- **Gateway → Server:** JSON-RPC (`FitsServiceAdapter` owns the `file.*`/`dataset.*` method names, ADR-0002).

The VM is a **coordinator over interfaces** — which is exactly why it unit-tests with no Unity, no OS dialog, no server. The whole tab is green in `dotnet test` (part of the 95-test no-Unity suite; the 19 gateway/adapter tests need a host without Smart App Control — see D5 `test-strategy.md` §4.4).

**Debug tab (second worked example):** `DebugTabViewModel` implements **`ILogObserver`**, calls `_logStream.Subscribe(this)`, and `OnNext(LogEntry)` appends to the bound list. This is the GoF **Observer** pattern (matches the brief's "Debug tab as Observer of a structured logging stream"), consuming server-pushed `log.emit` notifications. Together the two examples exercise the transport on **both** paths — File = request/response, Debug = server push.

---

## 5. How the architecture fixes each known defect

This is the strongest line of argument — every structural choice maps to a real, documented bug:

| Defect | Cause | Fix in this design |
|---|---|---|
| **B-02** (critical crash: Debug tab during load) | Log readout binds to `Application.logMessageReceived` while the main thread is blocked in CFITSIO | Separate assemblies + I/O moved server-side (no main-thread block); Debug becomes an Observer of `ILogStream`, marshalled via `IUIDispatcher`. (MOD-1, REU-3, TST-2) |
| **B-08** (UI freeze during load) | Synchronous native read on the main thread | `LoadAsync` awaits `IVolumeService` behind the gateway; `IsLoading` drives a spinner; main thread never blocks |
| **B-03** (slider sync between Render/Stats/VR) | Render & Stats share mutable `CanvassDesktop` fields with no notification | Both observe `IVolumeService` events (single source of truth); `SetThresholds` broadcasts to all |
| **B-04** (percentile freeze on large cubes) | Exact percentile computed synchronously | `ApplyPercentileCommand` → `ComputeHistogramAsync` with `IProgress<float>`; VM exposes `IsRecomputing` for progress |
| **B-05** (WCS vs voxel one-voxel offset, #464) | Implicit field-order coordinate assumption; inconsistent transforms | `MappingRole` enum makes the coordinate system an explicit, typed choice; `Validate()` rejects mixed/incomplete mappings → bug becomes a unit-testable rule (transform arithmetic itself is server-side, `IFeatureSetService`) |

---

## 6. Decisions we made and own

| Decision | Rationale |
|---|---|
| **MVVM over MVP / MVU / MVC / Rx** | §1.4. UI Toolkit's binding model is observable-property-based → MVVM is the natural fit; MVU mismatches two-way binding and adds defence risk; Rx adds a dependency (deferred, `ILogStream` stays Rx-compatible). |
| **Three assemblies, not three folders** | Assembly boundaries let NDepend *enforce* the dependency direction and the no-Unity-in-VM rule at build time. |
| **Gateway proxies vs Unity adapters** | Splits the ACL: proxies (pure C#, own the wire) are unit-testable; Unity adapters quarantine the SDK at the bottom of the graph. |
| **`ICommand` as the only behaviour surface** | Testable, auto-gated, uniform; enforced by private bodies + interface. |
| **Observer (not C# event) for `ILogStream`** | Matches the brief's "Observer" wording and the tested skeleton (`Subscribe(ILogObserver)` / `OnNext`). |
| **File + Debug as the two full examples** | Mandated by §6.6; chosen because they exercise request/response *and* server-push, proving the transport contract has real consumers on both paths. |
| **Stats + Sources as design gestures only** | The brief mandates exactly two full worked examples; gestures show the pattern scales and name the new contracts (`ICatalogueService`, `IFeatureSetService`) for the Architecture Guild to ratify. |
| **Sources: catalogue server-side, mapping JSON client-local via `IConfigService`** | Heavy native parse = data-tier (server); tiny preference file = client config, but routed through `IConfigService` so the VM never does direct file I/O. |

---

## 7. Honest caveats — what *not* to overclaim

A panel rewards candour about limits. Be ready with these:

- **Stats & Sources are not built or measured.** They're design gestures; only File & Debug have committed, tested skeletons and CK deltas. Don't imply otherwise.
- **`FileTabViewModel` WMC = 27 is borderline** against the ≤ 20 domain threshold (§7.1). It's hand-counted on Day 6 and *accepted as-is* with a documented remediation (extract `FileTabCommands`) rather than masked — §7 of the brief bans speculative numbers.
- **We do not patch B-05's transform math.** The design makes the coordinate choice explicit and testable; the actual 0/1-index correction is server-side (`IFeatureSetService`). It's a design proposal, not a code fix.
- **Residual D3↔skeleton mismatch:** D3 §5.1 constructs `DebugTabViewModel(logStream, dispatcher)`, but the tested skeleton ctor takes only `ILogStream`. Reconcile before the freeze (decide whether the Debug VM takes an injected dispatcher).
- **Contract names propagated into D5/D4.** We fixed the `ILogStream` Observer contract and adapter names in D2/D3, but `D5/test-strategy.md` and `D5/viewmodel-unit-tests.md` still describe `ILogStream.OnLogEntry` (the event form that won't compile against the real interface), and `D4/metrics.md` still says `StandaloneFileDialogAdapter`. These need the same fix.
- **Doc title vs deliverable name.** The H1 reads "MVVM Strategy"; the deliverable/folder is "MVVM binding policy." Cosmetic, but worth aligning so the panel can map artifact → deliverable.
- **D3 duplicates D2/D4 heavily** (§1 concerns, §1.2 CK baseline, §2.1 split, §6 ACL, §7 examples). Defensible as context, but the CK baseline now lives in three docs and can drift.

---

## 8. Self-test — likely panel questions

- "Is the Gateway the Model?" → No; §3.1 / §2.2. Model is server-side; Gateway returns DTOs.
- "Where does the Model live, then?" → Authoritative model server-side; client sees DTOs. Today it's wrongly in the client (cause of B-02/B-08).
- "Is your ViewModel an interface?" → No; the VM is the class `FileTabViewModel`; `IFileTabViewModel` is the boundary interface it implements (§4.2.4). `I` = interface convention.
- "How does the View call the ViewModel?" → Only via `ICommand`; command bodies are private, so direct calls don't compile.
- "Isn't the `Update()` loop MVU?" → No; Unity engine tick vs MVU pure function. We use event-driven MVVM and explicitly rejected MVU.
- "Show one defect fixed end-to-end." → B-02 or B-03 from §5.
- "Did you implement all five tabs?" → File & Debug fully (tested); Render/Stats/Sources are gestures.
- "Why MVVM over MVP/MVU?" → §1.4 / §6.
- "How is 'no Unity in the ViewModel' enforced?" → Assembly boundary + NDepend CQLinq rule on every PR (§11.1), build fails on `using UnityEngine` in the VM assembly.
- "What does the Gateway actually do on the wire?" → JSON-RPC 2.0 over named pipes; `FitsServiceAdapter` maps `OpenImageAsync` → `file.open` + `dataset.getAxes` (ADR-0002).

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal explainer for the D3 MVVM binding policy. Not a deliverable itself.*
