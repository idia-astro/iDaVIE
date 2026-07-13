# MVVM Strategy — Desktop Client Shell

- **Status:** final
- **Date:** 2026-05-27
- **Authors:** Sub-team 6 (Desktop GUI & Client Shell)
- **Backlog:** ARCH-1, ARCH-9
- **Supersedes:** ADR-009 draft (decision rationale folded in)
- **Related:** [ADR-0002 — Client–server transport](../D2-Architecture/architecture.md#adr-0002--clientserver-transport-json-rpc-over-named-pipes--grpc)
- **Numbering:** local `ADR-0001`..`ADR-0004` ↔ central `ADR-001`..`ADR-012` — see the cross-walk table at the top of [D2 §4](../D2-Architecture/architecture.md#4-architecture-decisions). Note especially that local `ADR-0003` ↔ central `ADR-002` (number reversal).

---

## 1. Why MVVM: The Problem We Are Solving

### 1.1 CanvassDesktop is a God-Object

`Assets/Scripts/UI/CanvassDesktop.cs` is a 1,899-line `MonoBehaviour` that mixes at least eight distinct concerns in a single class:

| Concern | Evidence in file |
|---|---|
| FITS file I/O and HDU parsing | `_browseImageFile`, `UpdateHeaderFromFits`, `FitsReader.*` calls |
| Scene-hierarchy wiring | 20+ `transform.Find("A/B/C").GetComponent<T>()` chains in `Start()` |
| Rendering state sync | `Update()` pushes threshold/colormap values from renderer into sliders every frame |
| Subset bounds validation logic | `checkSubsetBounds`, `updateSubsetZMax`, `IsLoadable` |
| File-dialog orchestration | `BrowseImageFile`, `BrowseMaskFile` — direct `StandaloneFileBrowser` calls |
| Coroutine lifecycle | `_loadCubeCoroutine`, `_showLoadDialogCoroutine` managed inline |
| Singleton hunting | `FindObjectOfType<VolumeInputController>()` etc. in `Start()` |
| Paint-mode state | `inPaintMode`, `PaintSelectionContainer` toggle logic |

### 1.2 Measured CK Violations (Day 2 Baseline)

The Day 2 CK measurement (SK_BNCH Sprint 1) confirms the problem is not just qualitative:

| Metric | CanvassDesktop (measured) | §7.1 threshold | Status |
|---|---|---|---|
| WMC | 63 | ≤ 40 (orchestrator) | **violation** |
| DIT | 1 | ≤ 4 | within |
| NOC | 0 | ≤ 5 | within |
| CBO | 47 | ≤ 25 (orchestrator) | **violation** |
| RFC | 118 | ≤ 50 | **violation** |
| LCOM_HS | 0.955 | ≤ 0.50 | **violation** |

CBO of 47 breaks down as: 23 project types (e.g. `VolumeDataSetRenderer`, `FitsReader`, `DataAnalysis`, `FeatureMapping`), 13 Unity/TMPro UI types, 7 System library types, and 4 `Valve.VR` types. Three fields (`_restFrequency`, `inPaintMode`, `_tabsManager`) are declared but never accessed — dead weight confirmed by the LCOM field-access count of 189 across 63 methods and 67 fields.

### 1.3 Forces That Drive the Decision

Three forces make the status quo unsustainable:

**Testability vs MonoBehaviour lifecycle.** `MonoBehaviour` cannot be instantiated without a live engine, so any logic inside it cannot be unit-tested with NUnit. CanvassDesktop's mocking-difficulty index of 205 (163 scene-graph traversals + 36 P/Invoke calls) makes the ≥ 70% branch/line target (NFR-TST-1) unreachable without decomposition.

**Unity 2021.3 → Unity 6 migration.** The legacy Canvas system must migrate to UI Toolkit. Any UI code not isolated behind an interface must be rewritten in full; mixing view logic and domain logic in one `MonoBehaviour` doubles that migration surface.

**Assignment constraints (non-negotiable).**
- §4.2.3: Domain code must not transitively depend on `UnityEngine` or `SteamVR`.
- §4.2.4: Every public API boundary expressed as an interface with at least one test double.
- §6.6: MVVM split is explicitly prescribed.
- §4.2.2: Zero circular dependencies between top-level components.

### 1.4 Alternatives Considered and Rejected

| Pattern | Why rejected |
|---|---|
| **Status quo (God-canvas `MonoBehaviour`)** | Fails §4.2.3 (domain depends on `UnityEngine` and native P/Invoke) and §4.2.4 (no testable interfaces). Not a valid target. |
| **MVP (Model–View–Presenter)** | Presenter holds a direct reference to the View interface, making View replacement harder. UI Toolkit's binding model is built around observable properties, not presenter calls; MVVM is the natural fit. |
| **MVU / Elm-style unidirectional flow** | Elegant but mismatches UI Toolkit's two-way binding primitives. Adds unfamiliarity risk for a first-year team defending the pattern under interview. Could be revisited for a future sub-system. |
| **Classical MVC** | Controller and View conflate in Unity practice (the `MonoBehaviour` ends up being both). Drawing a clean ACL boundary is harder when the controller still holds scene references. |
| **Reactive MVVM (Rx/UniRx)** | Compelling fit for the Debug tab's log push model, but Rx adds a library dependency and steep learning curve. Deferred: the `ILogStream` event model in WE2 is compatible with an Rx migration later without changing the ViewModel interface. |

---

## 2. How We Are Doing It: Architecture

### 2.1 Three-Assembly MVVM Split

The decision (ADR-0001, 2026-05-19) is to adopt a three-layer split, each in its own C# assembly:

| Assembly | Contents | Unity dependency |
|---|---|---|
| `iDaVIE.Client.View` | `UIDocument` components, USS/UXML, Unity event wiring | Yes — allowed |
| `iDaVIE.Client.ViewModel` | Observable properties, `ICommand` implementations, validation logic | **None** |
| `iDaVIE.Client.Gateway` | `IServiceGateway` adapters, transport, anti-corruption layer | Adapter layer only |

**Dependency direction (CI-enforced via NDepend):**

```
iDaVIE.Client.View  →  iDaVIE.Client.ViewModel  →  iDaVIE.Client.Gateway
```

Reverse-direction references are a CI failure. This enforces §4.2.2 (no cycles) and §4.2.3 (no `UnityEngine` in domain).

### 2.2 Layer Map

```
┌──────────────────────────────────────────────────────────┐
│  View Layer  (Unity 6 UI Toolkit / MonoBehaviour thin)   │
│  FileTabView  ·  DebugTabView  ·  RenderingTabView  etc. │
│  Only: bind to VM properties, forward UI events          │
└────────────────────┬─────────────────────────────────────┘
                     │ data-binding / ICommand
┌────────────────────▼─────────────────────────────────────┐
│  ViewModel Layer  (pure C#, no UnityEngine references)   │
│  FileTabViewModel  ·  DebugTabViewModel  ·  etc.         │
│  Owns: state, validation, commands, INotifyPropertyChanged│
└────────────────────┬─────────────────────────────────────┘
                     │ calls IServiceGateway interfaces
┌────────────────────▼─────────────────────────────────────┐
│  Service Gateway  (interface + adapter)                  │
│  IFitsService  ·  IVolumeService  ·  ILogStream          │
│  Translates domain calls → server RPC / native plugin    │
└──────────────────────────────────────────────────────────┘
```

> **Where is the Model?** This is a client–server MVVM: the authoritative domain model lives **server-side** (Sub-team 1's kernel + native plugins), so there is **no client `Model` assembly**. On the client the Model appears only as the immutable DTOs returned through the Gateway (`FitsFileInfo`, `LogEntry`, etc.); the Gateway is the access path to a remote Model, not the Model itself. Moving the Model off the client is precisely the §6.6 "direct file I/O that belongs server-side" goal — and the fix for B-02 / B-08.

---

## 3. How We Are Doing It: Per-Tab ViewModel Design

### 3.1 FileTabViewModel

**Owns:**
- `ImagePath`, `MaskPath`, `SourcesPath` (observable properties)
- `HduOptions` list + `SelectedHduIndex`
- `SubsetBounds` (XMin/XMax/YMin/YMax/ZMin/ZMax)
- `SubsetEnabled` toggle
- `IsLoadable` computed property (currently inline logic in `IsLoadable()`)
- `LoadCommand`, `BrowseImageCommand`, `BrowseMaskCommand` (`ICommand`)

**Does NOT own:**
- Any `Transform.Find(...)` wiring — stays in the View adapter
- The `StandaloneFileBrowser` call — goes behind `IFileDialogService`
- The `FitsReader` P/Invoke — goes behind `IFitsService`

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `BrowseImageFile()` UI wiring | `FileTabView.OnBrowseClicked()` |
| `_browseImageFile(path)` FITS open + HDU parse | `IFitsService.OpenImageAsync(path)` |
| `IsLoadable()` axis dimension logic | `FileTabViewModel.IsLoadable` (pure C#, testable) |
| `checkSubsetBounds()` clamping | `SubsetBoundsViewModel.Validate()` |
| `updateSubsetZMax()` dropdown sync | `FileTabViewModel.OnZAxisChanged(int)` |
| `UpdateHeaderFromFits()` scroll view write | `FileTabView` bound to `HeaderText` property |

### 3.2 DebugTabViewModel (Observer Pattern)

The debug console becomes a **passive observer of a structured log stream**, not a direct writer. It uses the GoF Observer pattern (matching D2 §6 and the debug-tab worked-example skeleton), not a C# `event`.

```
ILogStream  ──publishes──►  DebugTabViewModel  ──binds──►  DebugTabView
                             (ILogObserver subscriber)     (scrolling text area)
```

- `ILogStream` exposes `Subscribe(ILogObserver)` / `Unsubscribe(ILogObserver)` / `Publish(level, message)` / `Publish(level, message, timestamp)`.
- `DebugTabViewModel` implements `ILogObserver`; it calls `_logStream.Subscribe(this)` in its constructor, and `ILogObserver.OnNext(LogEntry)` appends to the bound `Entries` collection (exposed as `IReadOnlyList<LogEntry> LogEntries`).
- The View scrolls to bottom when `Entries` changes — no manual scroll logic in ViewModel.
- Existing `Debug.Log` calls in CanvassDesktop are replaced by injecting `ILogStream` and calling `ILogStream.Publish(level, message)`.

**Split from current code:**

| Current | Proposed home |
|---|---|
| `Debug.Log(...)` scattered across CanvassDesktop | `ILogStream.Publish()` calls |
| Manual scroll/text updates | `DebugTabView` reactive binding |
| No structured log context | `LogEntry { Level, Message, Timestamp }` (record — see D2 §6 / debug-tab skeleton) |

### 3.3 RenderingTabViewModel

**Owns:**
- `MinThreshold`, `MaxThreshold` (float, two-way bindable)
- `ActiveColorMap` (enum)
- `RestFrequencyOptions` list
- Commands: `ApplyThreshold`, `ChangeColorMap`

The current `Update()` (Unity's per-frame `MonoBehaviour.Update()` engine-lifecycle callback — *not* an MVU/Elm `update` function) loop polls `firstActiveRenderer.ThresholdMin` every frame and pushes it to a slider. In the refactored design:
- `IVolumeService` raises a `ThresholdChanged` event
- `RenderingTabViewModel` subscribes and updates its own properties
- `RenderingTabView` binds sliders to those properties — no per-frame sync needed

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `Update()` polls `firstActiveRenderer.ThresholdMin/Max` every frame | `IVolumeService.ThresholdChanged` event → `RenderingTabViewModel` subscribes |
| Slider wiring in `Start()` | `RenderingTabView` bound to `MinThreshold` / `MaxThreshold` |
| `_restFrequency` field (declared, never read) | Removed — dead code confirmed by LCOM field-access analysis |
| Colormap dropdown wiring in `Start()` | `RenderingTabView.OnColorMapChanged()` → `ChangeColorMapCommand` |

### 3.4 StatsTabViewModel

The Stats tab today is eight methods on `CanvassDesktop` (`populateStatsValue`, `UpdateUI`, `UpdateSigma`, `RestoreDefaults`, `UpdateScale`, `SetMaxMinPercentile`, `UpdateScaleMin`, `UpdateScaleMax`) that read in-memory float buffers and write sliders / labels directly. They share state with the Render tab via `CanvassDesktop` fields — moving the slider in Render silently changes Stats. That cross-tab coupling is the proximate cause of defect **B-03** (slider sync) named in `D1/requirements.md` §2.

**Owns:**
- `Histogram` (`IReadOnlyList<HistogramBin>`) — bound to the chart control
- `PercentilePresets` (list of `{Label, Lower, Upper}`) — the quick-pick row
- `ManualMin`, `ManualMax` (float, two-way bindable)
- `ScaleMode` (enum: `Linear` / `Log`)
- `SigmaOverlay` (float — multiplier for the σ band)
- Commands: `ApplyPercentileCommand`, `RestoreDefaultsCommand`, `RecomputeHistogramCommand`

The `IsExactPercentile` toggle currently freezes the UI on large cubes (defect **B-04**); the ViewModel exposes `IsRecomputing` so the View can show a progress indicator instead.

Histogram and percentile work belong to the same domain as the volume renderer, so `IVolumeService` is the natural producer. Two surface additions over the WE1 contract:
- `Task<IReadOnlyList<HistogramBin>> ComputeHistogramAsync(int bins, CancellationToken ct)` — exact percentiles on demand.
- `event Action<HistogramChangedEventArgs>? HistogramChanged` — fires when the loaded cube or thresholds change, replacing the per-frame `Update()` poll.

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `populateStatsValue()` reads buffer on tab show | `StatsTabViewModel` subscribes to `IVolumeService.HistogramChanged`; no tab-show side effect |
| `UpdateUI()` mutates sliders + labels directly | View binds to `Histogram`, `ManualMin`, `ManualMax`, `ScaleMode` — no imperative UI writes |
| `SetMaxMinPercentile(p)` freezes UI for exact computation | `ApplyPercentileCommand` issues `ComputeHistogramAsync` with `IProgress<float>`; View shows progress |
| `UpdateScale()` reaches into Render tab's threshold fields | `IVolumeService.SetThresholds(min, max)` — Render tab observes the same event |
| `RestoreDefaults()` re-reads renderer state | `RestoreDefaultsCommand` calls `IVolumeService.ResetThresholds()` |

### 3.5 SourcesTabViewModel

The Sources tab today is ten methods on `CanvassDesktop` (`BrowseSourcesFile`, `_browseSourcesFile`, `BrowseMappingFile`, `_browseMappingFile`, `SaveMappingFile`, `_saveMappingFile`, `ChangeSourceMapping`, `AreMappingsIncompatible`, `AreMinimalMappingsSet`, `LoadSourcesFile`) that handle two file dialogs, JSON mapping I/O, per-column validation, and a singleton-lookup feature-set load. Mapping-rule logic is intermixed with UI updates. Defect **B-05** (WCS vs (x,y,z) one-voxel offset, open issue #464) sits in this code.

**Owns:**
- `CataloguePath`, `MappingPath` (observable strings)
- `AvailableColumns` (`IReadOnlyList<ColumnDescriptor>`) — populated after a catalogue is opened
- `ColumnMappings` (`IDictionary<MappingRole, string>`) — bound to per-role dropdowns
- `MinimalMappingsSet` (bool, computed) — gates the Load button
- `ValidationMessage` (string?) — surfaces "WCS ↔ voxel mismatch" etc.
- Commands: `BrowseCatalogueCommand`, `BrowseMappingCommand`, `SaveMappingCommand`, `LoadCatalogueCommand`

Catalogue parsing is server-side (VOTable / FITS table — the same brief-§6.6 logic that puts FITS reading server-side). Mapping JSON is small enough to stay client-local but consistent with persistence, it should route through `IConfigService` rather than direct file I/O.

Two domain interfaces are needed (out-of-scope for the worked examples; gestures only):
- `ICatalogueService` — `OpenCatalogueAsync(path)` returns a `CatalogueInfo { Columns, RowCount, SuggestedMappings }`. Implemented by a `CatalogueServiceAdapter` gateway proxy that dispatches `catalogue.open` / `catalogue.close` over `IServiceGateway`.
- `IFeatureSetService` — `LoadFromCatalogueAsync(datasetId, mappings)` materialises features in the running session.

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `BrowseSourcesFile()` opens dialog, then calls `_browseSourcesFile` | `BrowseCatalogueCommand` → `IFileDialogService.PickFileAsync` → `ICatalogueService.OpenCatalogueAsync` |
| `_browseSourcesFile()` reads VOTable / FITS catalogue via native reader | `ICatalogueService` server-side; client sees only `CatalogueInfo` DTO |
| `AreMinimalMappingsSet()` / `AreMappingsIncompatible()` validation | Computed property `MinimalMappingsSet` + private `Validate()` method |
| `SaveMappingFile()` / `_saveMappingFile()` JSON serialisation | `SaveMappingCommand` → `IConfigService.SaveMappingAsync(path, ColumnMappings)` |
| `LoadSourcesFile()` via `FindObjectOfType<FeatureSetManager>()` | `LoadCatalogueCommand` → `IFeatureSetService.LoadFromCatalogueAsync` |
| WCS ↔ voxel offset bug **B-05** | Mapping role enum makes WCS vs voxel coordinate an explicit choice rather than an implicit field-order assumption; `Validate()` rejects incompatible combinations |

These two sections are **design gestures**, not worked examples — the brief mandates only File and Debug as full worked examples (D4). They show the same MVVM split applies, and they identify the new interfaces (`IStatsService` extension, `ICatalogueService`, `IFeatureSetService`) the Architecture Guild needs to ratify before any of these tabs ships.

---

## 4. How We Are Doing It: Binding Mechanics

### 4.1 Property Change Notification

`INotifyPropertyChanged` is mandatory on every observable ViewModel. The canonical shape uses a `ViewModelBase` helper with a `SetField<T>` helper and `[CallerMemberName]`:

```csharp
public sealed class FileTabViewModel : ViewModelBase
{
    private string _selectedPath;
    public string SelectedPath
    {
        get => _selectedPath;
        set => SetField(ref _selectedPath, value);
    }
}
```

**Naming rules:**
- Public properties: PascalCase, no `m_` / `_` prefix.
- Backing fields: `_camelCase`.
- Booleans: `IsX` / `HasX` / `CanX`.
- No `Get`/`Set` verb prefix on properties.

**Forbidden:**
- Bare public fields (`public T Field;`) — cannot raise `PropertyChanged`.
- Side-effects in getters — getters must be pure.
- Heavy work in setters — push to a command.

### 4.2 Commands

`ICommand` is the **only** way the View invokes ViewModel behaviour. The View must not call `viewModel.DoTheThing()` directly.

**Async commands:**
- Long-running work returns `Task`; the command surface remains `ICommand`.
- ViewModel exposes `IsBusy` so the View can disable controls / show progress.
- Cancellation: `CancellationToken` flows from a companion `CancelCommand`.

**Error propagation:**
- Gateway exceptions are caught **inside** the command body.
- Surfaced to the View via a bound `ErrorMessage` (string) and a domain-typed `LastError` enum.
- ViewModels do **not** throw out of commands — the View has no handler.

**Replay and undo (cross-reference to ADR-010):** ADR-009 names commands as "reified, replayable, testable — consistent with ADR-010". Reified and testable are covered above; **replayability is owned by ADR-010** (Sub-team 4's State and Command Patterns for the Interaction System, see central registry). The desktop tab commands share the GoF `ICommand` shape with the VR-side commands by design, so a command-log observer wired at the `ICommand.Execute` boundary by Sub-team 4 will capture every desktop-tab command without any change in this ViewModel layer. **Undo** — ADR-010 makes Undo *"where applicable"*; most desktop tab commands are not naturally undoable (file open, log filter change). Where a desktop tab adds a reversible operation later (e.g. mask paint stroke) the relevant `ICommand` implementation supplies an `Undo()` method matching the ADR-010 contract. Sub-team 6 does **not** own the command-log mechanism; we own the contract that makes logging possible.

### 4.3 Collections

`ObservableCollection<T>` is used for all bound lists. The Debug tab is the load test: the log stream can produce ≥ 1k entries/sec under tracing, so a bounded ring-buffer wrapper or virtualised incremental collection may be needed.

**Mutation rules:**
- All mutations on the UI thread only (see §4.4).
- Bulk loads: clear + re-add atomically, not item-by-item, to avoid UI thrash.
- Do not replace the `ObservableCollection<T>` **instance** — prefer in-place mutation. Instance replacement breaks View bindings unless explicitly re-bound.

### 4.4 Threading Model

Every bound property change and collection mutation must fire on the Unity main thread. ViewModels receiving Gateway callbacks on background threads must marshal explicitly via an `IUIDispatcher` interface:

```csharp
public interface IUIDispatcher
{
    void Post(Action action);
    void Invoke(Action action);
}
```

The concrete implementation lives in the View assembly; ViewModels take the interface via constructor injection. This avoids the `UnityMainThreadDispatcher` static singleton pattern.

**Forbidden:**
- `UnityMainThreadDispatcher.Instance.Enqueue(...)` — singleton, untestable.
- Captured `SynchronizationContext` without explicit `ConfigureAwait(false)` discipline at Gateway boundaries.

### 4.5 Data-Binding Approach (Unity Version Transition)

Unity 6 UI Toolkit supports `INotifyValueChanged<T>` and data binding natively. For the legacy Canvas system (current Unity 2021.3) the shim is:

1. ViewModel implements `INotifyPropertyChanged` (standard .NET).
2. A thin `UnityBinder<T>` helper subscribes to `PropertyChanged` and sets the UI element value.
3. Views register bindings in `Awake` / `OnEnable` and unregister in `OnDisable`.

```csharp
// View assembly only — never imported by ViewModel
public sealed class UnityBinder<T> : IDisposable
{
    private readonly INotifyPropertyChanged _source;
    private readonly string   _propertyName;
    private readonly Func<T>   _getter;
    private readonly Action<T> _setter;

    public UnityBinder(
        INotifyPropertyChanged source, string propertyName,
        Func<T> getter, Action<T> setter)
    {
        _source = source; _propertyName = propertyName;
        _getter = getter; _setter = setter;
        source.PropertyChanged += OnChanged;
    }

    private void OnChanged(object _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyName) _setter(_getter());
    }

    public void Dispose() => _source.PropertyChanged -= OnChanged;
}

// Usage in FileTabView.Awake():
_binders = new List<IDisposable>
{
    new UnityBinder<bool>(_vm, nameof(_vm.IsLoadable),
        () => _vm.IsLoadable,   v => _loadButton.interactable = v),
    new UnityBinder<string>(_vm, nameof(_vm.HeaderText),
        () => _vm.HeaderText,   v => _headerScrollText.text = v),
};
```

This eliminates the manual `transform.Find(...)` chains and the per-frame `Update()` sync.

---

## 5. How We Are Doing It: Composition Root

### 5.1 Purpose

The existing `CanvassDesktop.Start()` does singleton hunting via `FindObjectOfType<>`. In the refactored design a single **composition root** `MonoBehaviour` wires everything at startup:

```csharp
// Composition root (pseudo-code — design only)
var gateway        = new JsonRpcServiceGateway(sessionPipeName);   // ADR-0002 transport
await gateway.ConnectAsync();

var fitsService    = new FitsServiceAdapter(gateway);              // gateway proxy
var logStream      = new GatewayLogStreamAdapter(gateway);         // gateway proxy
var volumeService  = new VolumeServiceAdapter();                   // Unity adapter
var dialogService  = new FileDialogAdapter();                      // Unity adapter (wraps StandaloneFileBrowser)
var memoryProbe    = new MemoryProbeAdapter();                     // Unity adapter
var catalogue     = new CatalogueServiceAdapter(gateway);          // gateway proxy (gesture — §3.5)
var dispatcher     = new UnityUIDispatcher();                      // IUIDispatcher impl (View assembly) — NOT the forbidden static singleton (§4.4)

var fileVM     = new FileTabViewModel(fitsService, dialogService, volumeService, memoryProbe);
var debugVM    = new DebugTabViewModel(logStream, dispatcher);
var renderVM   = new RenderingTabViewModel(volumeService);
var statsVM    = new StatsTabViewModel(volumeService);
var sourcesVM  = new SourcesTabViewModel(catalogue, dialogService);

fileTabView.BindTo(fileVM);
debugTabView.BindTo(debugVM);
renderingTabView.BindTo(renderVM);
statsTabView.BindTo(statsVM);
sourcesTabView.BindTo(sourcesVM);
```

### 5.2 Wiring Rules

- One `MonoBehaviour` in `iDaVIE.Client.View` constructs ViewModels at scene start.
- **Constructor injection** for all dependencies. No `FindObjectOfType<>`, no service locator inside ViewModels.
- DataContext convention (UI Toolkit): `rootVisualElement.userData = viewModel` set once during composition.
- ViewModels do not hold a reference to their View. View → ViewModel is one-way.

### 5.3 Lifecycle

- ViewModels implementing `IDisposable` are disposed by the composition root on scene unload.
- Subscriptions to `ILogStream`, `IFitsService`, etc. must be unsubscribed on dispose — leaks block deterministic unit tests.
- Panel ViewModels are scoped to their `UIDocument`; the root ViewModel is scoped to the scene.

---

## 6. How We Are Doing It: Anti-Corruption Layer

The ACL lives at the Service Gateway boundary only:

```
Domain code (ViewModels)         Service adapters (Gateway Layer, ACL)
─────────────────────────        ──────────────────────────────────────────
IFitsService                ◄──── FitsServiceAdapter         (gateway proxy — JSON-RPC)
ILogStream                  ◄──── GatewayLogStreamAdapter    (gateway proxy — JSON-RPC notifications)
IVolumeService              ◄──── VolumeServiceAdapter       (Unity adapter — VolumeCommandController)
IFileDialogService          ◄──── FileDialogAdapter           (Unity adapter — SFB)
IConfigService              ◄──── ConfigServiceAdapter       (Unity adapter — PlayerPrefs)
```

Rule: nothing left of the `◄────` line may `using UnityEngine`, `using Valve.VR`, or `[DllImport]`. The two **gateway proxies** also forbid those usings on their own side — they compile against `iDaVIE.Client.Gateway` only. The three **Unity adapters** are the only classes permitted to touch the Unity SDK (see ADR-0001 Decision item 3 in `D2/architecture.md`).

**DTO contract (Gateway ↔ ViewModel):**
- DTOs are immutable plain C# records.
- Primitive, string, or nested DTO types only.
- **No** `UnityEngine.*`, **no** `Valve.VR.*`, **no** `[JsonProperty]` or other transport attributes.
- Wire-format validation lives in the Gateway; domain validation lives in the ViewModel.

---

## 7. Worked Examples

### 7.1 File Tab — Command-Driven Round-Trip

**Before (current CanvassDesktop):**

```
CanvassDesktop.BrowseImageFile()
  └─ StandaloneFileBrowser.OpenFilePanelAsync(...)
       └─ _browseImageFile(path)
            ├─ FitsReader.FitsOpenFile(...)         // P/Invoke — untestable
            ├─ FitsReader.FitsGetHduCount(...)
            ├─ transform.Find("...Hdu_dropdown").GetComponent<TMP_Dropdown>()  // scene coupling
            ├─ IsLoadable()                         // axis math buried in 1899-line class
            └─ UpdateHeaderFromFits(fptr)           // scroll view write
```

**After (MVVM, design only):**

```
FileTabView.OnBrowseButtonClicked()
  └─ FileTabViewModel.BrowseImageCommand.Execute()
       └─ IFileDialogService.PickFileAsync(...)
            └─ IFitsService.OpenImageAsync(path)
                 returns: FitsFileInfo { HduList, NAxis, AxisSizes, HeaderText }
            └─ FileTabViewModel updates: HduOptions, IsLoadable, HeaderText

[data bindings — no transform.Find]
FileTabView.HduDropdown  ◄── FileTabViewModel.HduOptions
FileTabView.LoadButton   ◄── FileTabViewModel.IsLoadable   (computed, NUnit-testable)
FileTabView.HeaderText   ◄── FileTabViewModel.HeaderText
```

Full before/after UML and dependency graph: [`refactoring-examples/sub-team-6/file-tab/`](../../../../refactoring-examples/sub-team-6/file-tab/)

### 7.2 Debug Tab — Observer / Push Collection Binding

**Before:**

```csharp
// Scattered across CanvassDesktop:
Debug.Log("Fits open failure... code #" + status);
Debug.Log(val + " is less than the minimum...");
// No structured format, no UI component, no stream abstraction
```

**After:**

```csharp
// In ViewModels / Services (producer side):
_logStream.Publish(LogLevel.Error, $"Open failure status={status}");

// DebugTabViewModel (pure C#, no Unity) — Observer of the stream:
//   sealed class DebugTabViewModel : IDebugTabViewModel, ILogObserver
//   ctor: _logStream.Subscribe(this);
void ILogObserver.OnNext(LogEntry entry) => _dispatcher.Post(() => AppendEntry(entry));

// DebugTabView (Unity):
// Bound to DebugTabViewModel.LogEntries — ScrollView refreshes automatically
```

Full before/after UML and dependency graph: [`refactoring-examples/sub-team-6/debug-tab/`](../../../../refactoring-examples/sub-team-6/debug-tab/)

---

## 8. Benefits

### 8.1 CK Metric Projections (Achieved by Decomposition)

| Class | WMC now | WMC after | CBO now | CBO after | Source |
|---|---|---|---|---|---|
| CanvassDesktop (shell only) | 63 | ~15 (projected) | 47 | ~4 (projected) | projection |
| **FileTabViewModel** | — | **27 (measured)** | — | **9 (measured)** | hand-count |
| SubsetBoundsViewModel | — | 12 (measured) | — | 1 (measured) | hand-count, Day 6 |
| FitsServiceAdapter | — | 6 (measured) | — | 5 (measured) | hand-count, Day 6 (post-gateway-rewire) |
| **DebugTabViewModel** | — | **7 (projected)** | — | **3 (projected)** | `D4/metrics.md §3.2` |
| GatewayLogStreamAdapter | — | 5 (projected) | — | 4 (projected) | step-3 rewire |
| RenderingTabViewModel | — | ~8 (projected) | — | ~4 (projected) | gesture, §3.3 |

Notes on the FileTabViewModel WMC = 27 measurement: this is hand-counted from the committed skeleton on Day 6 and is **borderline** against the ≤ 20 domain threshold from brief §7.1. The remediation path is documented in `refactoring-examples/sub-team-6/file-tab/ck-metrics.md`: extracting a `FileTabCommands` helper from the four async command bodies (`BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`, `ClearMask`) drops the count to ~22. The audit accepts the borderline value rather than masking it because §7 of the brief explicitly bans speculative numbers without evidence. All other measured values fall within §7.1 thresholds. Full before/after delta tables are in the D4 worked examples.

### 8.2 Testability

- `iDaVIE.Client.ViewModel` is unit-testable with NUnit + Moq, no Unity Editor required — satisfies LO6 and NFR-TST-1 (≥ 70% branch/line on domain code).
- Mocking-difficulty drops from 205 (status quo: 163 scene-graph traversals + 36 static P/Invoke) to **0** in the ViewModel assembly — all dependencies are injected interfaces.
- Test doubles for `IFitsService`, `IFileDialogService`, `ILogStream` can be in-memory stubs running without a GPU, VR headset, or file system.

### 8.3 Migration Safety

- Unity 2021.3 → Unity 6 UI Toolkit migration is scoped to `iDaVIE.Client.View` only.
- ViewModels and Gateway adapters survive unchanged across the engine upgrade.

### 8.4 Explicit Dependencies

- Composition root replaces all `FindObjectOfType<>` singleton lookups.
- Every dependency is visible at construction time — no hidden global state.

### 8.5 Long-Term Extensibility

- The `IServiceGateway` façade can run local (in-process) or over JSON-RPC named pipes without changing ViewModels — satisfying the roadmap driver for Python console and workspace-save features (ARQ-1, ARQ-2).
- A thin `IMenuRouter` interface at the VR/desktop boundary lets Sub-team 4 (Interaction System) share the command vocabulary without coupling to Unity UI types.

---

## 9. Risks and Trade-offs

| Risk | Mitigation |
|---|---|
| Unity 2021.3 has no native UI Toolkit binding | Use lightweight `UnityBinder<T>` shim; migrate to UI Toolkit binding when upgrading to Unity 6. |
| Extra ceremony: three assemblies, interface boilerplate | Code skeletons in `refactoring-examples/sub-team-6/` serve as the canonical pattern. |
| VR-side menus (Sub-team 4 dependency) | Thin `IMenuRouter` interface at the boundary — both VR and desktop implement it. |
| IPC/RPC with Sub-team 1's service gateway (DEPS-1) | `IVolumeService` façade can run local or over named pipes without changing ViewModels. Risk tracked as R01 on integration risk register. |
| "Leaky" ViewModels accidentally importing `UnityEngine` via transitive references | NDepend CQLinq rule fails the build on any forbidden import. |
| `INotifyPropertyChanged` more verbose than Unity's `[SerializeField]` + `OnValidate` | `ViewModelBase.SetField<T>` helper reduces per-property boilerplate to one line. |
| Frame-rate-sensitive rendering sync | `RenderingTabViewModel` can expose a `Tick(float dt)` method called only by the View — keeps the VM testable while frame sync stays in the adapter. |
| Log stream throughput (≥ 1k entries/sec) | Evaluate bounded ring-buffer wrapper or virtualised `ObservableCollection` for Debug tab. |

---

## 10. Forbidden Patterns

| Pattern | Why forbidden | Replacement |
|---|---|---|
| Any `UnityEngine` type in ViewModel (`using UnityEngine`, `Vector3` field, `[SerializeField]`) | Violates §4.2.3; domain code must not depend on Unity | DTO with primitives; plain C# properties; constructor injection |
| Coroutine in ViewModel | Unity-bound execution model | `async Task` + `IUIDispatcher` |
| `FindObjectOfType<>` anywhere | Singleton coupling | Constructor injection at composition root |
| `static` state on ViewModels (mutable field or event) | Breaks test isolation; shared state leaks between test runs | Instance state or instance event; inject shared state via constructor |
| `viewModel.Property = x` from View code | Bypasses command layer | `ICommand` binding |
| `Thread.Sleep` / `WaitForSeconds` in ViewModel | Blocks UI thread or Unity-bound | `Task.Delay` + `CancellationToken` |
| `event EventHandler` on ViewModel as substitute for `ICommand` | Circumvents binding contract | `ICommand` |

---

## 11. CI Enforcement

### 11.1 NDepend CQLinq — ViewModel Purity Rule

```csharp
// warnif count > 0
from t in Application.Assemblies
   .WithNameLike("iDaVIE.Client.ViewModel")
   .ChildTypes()
where t.IsUsing("UnityEngine")
   || t.IsUsing("Valve.VR")
   || t.IsUsing("System.Runtime.InteropServices")
select new {
    t,
    Issue = "ViewModel assembly forbids Unity/SteamVR/native interop (ADR-0001)"
}
```

### 11.2 PR Checklist (Reviewer-Facing)

- [ ] No `UnityEngine` / `Valve.VR` / `System.Runtime.InteropServices` import in any file under `iDaVIE.Client.ViewModel`.
- [ ] Every new public ViewModel property fires `PropertyChanged` (or is a `record` DTO field on a different type).
- [ ] Every command is bound via `ICommand`, not via direct method invocation from the View.
- [ ] No `static` mutable state on ViewModels.
- [ ] All `IDisposable` ViewModel dependencies subscribed in the constructor are disposed in `Dispose()`.
- [ ] Background-thread Gateway callbacks marshal to UI thread via `IUIDispatcher`.

---

## 12. Glossary

| Term | Meaning in this codebase |
|---|---|
| View | UI Toolkit `UIDocument` + USS/UXML; the only place `UnityEngine` types are allowed. |
| ViewModel | Pure C# class implementing `INotifyPropertyChanged`. No Unity, no SteamVR, no `DllImport`. |
| Model | Domain data; in our slice usually a DTO returned by the Gateway. |
| Gateway | The `IServiceGateway` adapter; talks to the server over the transport defined in ADR-0002. |
| DTO | Immutable transfer object across the Gateway ↔ ViewModel boundary; primitive + string + nested DTO fields only. |
| Command | `ICommand` implementation invoked by a View binding; sync or async. |
| Composition root | The single `MonoBehaviour` in the View assembly that wires ViewModels to their dependencies at scene start. |
| ACL | Anti-corruption layer — the Gateway assembly that prevents Unity/SteamVR types from leaking into domain code. |
| `IUIDispatcher` | Interface for marshalling work onto the Unity main thread from background Gateway callbacks. |

---

## 13. References

- [ADR-0002 — Client–server transport](../D2-Architecture/architecture.md#adr-0002--clientserver-transport-json-rpc-over-named-pipes--grpc)
- [D4 — File-tab worked example](../D4-worked-examples/README.md#example-1--file-tab)
- [D4 — Debug-tab worked example](../D4-worked-examples/README.md#example-2--debug-tab)
- [D5 — ViewModel unit tests](../D5-testing/viewmodel-unit-tests.md)
- [D5 — UI Toolkit page-object pattern](../D5-testing/ui-toolkit.md)
- [Integration risk register](../../../team-alpha/integration-risk-register.md) — R01 (Sub-team 1 gateway contract), R04 (Sub-team 4 VR menus)
- Assignment spec §4.1 (target architectural style), §4.2.2 (no cycles), §4.2.3 (no Unity in domain), §4.2.4 (interfaces on public APIs), §6.6 (MVVM prescription + binding-policy deliverable), §7.1 (CK thresholds).
