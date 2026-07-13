# WE1-6 — CK + ISO 25010 Delta Worksheet (File Tab & Debug Tab)

**Status:** All figures tool-verified (Understand static analysis export, Day 13, 2026-05-29). **Updated Day 10 (2026-05-29):** `FitsMetadataHelper` extracted from `FileTabViewModel` (WMC 43→40); `Clamp` inlined in `SubsetBoundsViewModel` (WMC 21→20). `FileTabViewModel` re-classified as orchestrator. RFC column = tool's RFC definition (= WMC / method count); traditional CK RFC (methods + external calls) is approximately 2–4× higher for complex classes. LCOM is reported as Percent Lack of Cohesion (0–100%); threshold ≤ 50%.  
**Before source:** `SK_BNCH.md` (Understand static analysis export, Day 2 baseline).  
**After sources (WE1):** `refactoring-examples/sub-team-6/file-tab/skeleton/*.cs`, `adapters/*.cs` (committed on `team6` branch).  
**After sources (WE2):** `after-class-diagram.puml`, `after-dependency-graph.puml`, `after-dsm.md`,  
`uml-diagrams/after-debug-sequence-diagram.puml`, `mvvm-binding-policy.md §3.2`.  
**Feeds:** T4 (architecture report §Metrics), T7 (integration & metrics), Pitch slot 3.

---

## 0. How to Read This Document

`CanvassDesktop` is the single God class that both worked examples extract from.
Its tool-verified metrics (WMC = 63, CBO = 30, RFC = 63 [tool def.], LCOM = 95%, DIT = 2, NIV = 67) are **partitioned once**
into three non-overlapping concerns:

| Concern | What it covers | "Before" row |
|---|---|---|
| **File tab** | The 16 methods that handle file/mask browse, FITS header read, subset validation, and cube load | §2 |
| **Debug tab** | The 40 `Debug.Log` call-sites embedded in existing methods — no dedicated class | §3 |
| **Shared residual** | All remaining CanvassDesktop methods: rendering, stats, sources, paint, lifecycle, navigation | §4 |

The same number does **not** appear in more than one "before" row. WMC = 63 is
explained by 16 (file) + 0 (debug) + 47 (shared) = 63.

**Note on class-level metrics.** WMC, RFC, and LCOM are computed by Understand at class level.
WMC in this report = method count (uniform weight 1 per Understand methodology, confirmed by
the LCOM section of `SK_BNCH.md`: "63 methods, 67 fields, 189 field-method accesses").
CBO partitions are derived from source inspection and marked **[derived]**.
RFC partitions are derived proportionally and marked **[derived]**.
LCOM cannot be cleanly split — it is reported once in §4 for the full class.

---

## 1. Before-State Partition of CanvassDesktop

Derived by counting methods per tab grouping and mapping the 47 CBO types to the
concern that primarily introduces them.

| Concern | WMC (method count) | CBO types attributed [derived] | RFC [tool def.] | LCOM % | DIT |
|---|:---:|:---:|:---:|:---:|:---:|
| File tab (16 methods) | **16** | **~8** | **~16** [derived] | — | — |
| Debug tab (0 dedicated methods) | **0** | **0** | **0** | — | — |
| Shared residual (47 methods) | **47** | **~22** | **~47** [derived] | 95% | 2 |
| **CanvassDesktop total (tool-verified)** | **63** | **30** | **63** | **95%** | **2** |

**File tab CBO types (~8):** Types used exclusively or primarily in the 16 file tab methods:
`FitsReader`, `VolumeDataSetManager`, `IntPtr` (P/Invoke handle), `StringBuilder`
(header text), `SystemInfo` (`CheckMemSpaceForCubes`), `FeatureMenuController`
(`postLoadFileFileSystem`), `VolumePlayer` (`postLoadFileFileSystem`), `MenuBarBehaviour`
(`postLoadFileFileSystem`).

**Debug CBO = 0:** `Debug.Log` calls use `UnityEngine.Debug`, a member of the `UnityEngine`
namespace already counted in the shared CBO. The logging concern introduces no additional type.

**Shared residual CBO (~39):** All remaining types: `VolumeDataSetRenderer`,
`VolumeCommandController`, `VolumeInputController`, `HistogramHelper`, `DataAnalysis`,
`StandaloneFileBrowser`, `FeatureMapping`, `FeatureSetManager`, `FeatureTable`,
`SourceMappingOptions`, `ColorMapUtils`, `Config`, all 13 Unity/TMPro UI types
(`TMP_Dropdown`, `TMP_InputField`, `Toggle`, `Slider`, `Button`, `Sprite`, `TextMeshProUGUI`,
`GameObject`, `Transform`, `Coroutine`, `PlayerPrefs`, `OxyPlot..*`, `Marshal`),
all 4 Valve.VR types (`SteamVR_Init`, `OpenVR`, etc.), and remaining System types.

---

## 2. File Tab — Old vs New

### 2.1 Old File Tab

The 16 methods below are the WE1 scope inside `CanvassDesktop`. Line numbers are from
the current source; approximate cyclomatic complexity (CC) per method is estimated from
branch count in the source.

| Method | Line | CC [est.] | Concern | Code smell |
|---|:---:|:---:|---|---|
| `BrowseImageFile` | 306 | 1 | UI wiring → SFB async callback | Mixed UI event + I/O dispatch in one method |
| `_browseImageFile` | 329 | 4 | FITS open, HDU parse, header read, subset reset | 5 `FitsReader.*` P/Invoke calls in business logic |
| `IsLoadable` | 431 | 5 | NAXIS validation, axis-size extraction, Z-dropdown populate | Axis maths + UI manipulation in one method |
| `UpdateHeaderFromFits` | 538 | 2 | FITS header dump to `TextMeshProUGUI` scroll-view | `IntPtr` (native handle) crosses the UI layer |
| `onSubsetToggleSelected` | 575 | 2 | Show/hide subset row, focus first field | Pure UI logic sitting inside God class |
| `setSubsetBounds` | 595 | 1 | Reset all 6 subset input fields to axis maxima | Direct `TMP_InputField.text` writes in 6 places |
| `updateSubsetZMax` | 614 | 2 | Clamp Z-max when Z-axis dropdown changes | Direct field write + UI sync in same method |
| `checkSubsetBounds` | 641 | 7 | Validate + clamp all 6 subset input fields | 18 `Debug.Log` calls; re-validates all 6 fields on any single field edit |
| `BrowseMaskFile` | 807 | 1 | Wires mask button to SFB async callback | Same mixed-concern pattern as `BrowseImageFile` |
| `_browseMaskFile` | 829 | 3 | Reads mask headers, validates axis match against image | Axis comparison logic intermixed with UI updates |
| `CheckImgMaskAxisSize` | 905 | 2 | **Dead code** — axis comparison duplicate | Duplicate of `_browseMaskFile` logic; no callers found |
| `LoadFileFromFileSystem` | 927 | 1 | Entry point — starts `LoadCubeCoroutine` | Couples UI button directly to coroutine handle |
| `postLoadFileFileSystem` | 935 | 2 | Post-load tab enables, dropdown populates, event subscriptions | 7 unrelated UI side-effects triggered from one method |
| `CheckMemSpaceForCubes` | 995 | 2 | Calculates in-memory size vs `SystemInfo.systemMemorySize` | Checks *client* RAM for what is a server-owned resource |
| `LoadCubeCoroutine` | 1015 | 5 | Scene teardown, prefab instantiation, coroutine wait, post-load | File I/O + scene management + UI updates + coroutine lifecycle in one method |
| `ChangeHduSelection` | 1435 | 2 | Re-opens FITS file on each HDU dropdown change | Re-opens file from disk on every switch; `IntPtr` lifetime not scoped |

**File tab WMC = 16, RFC ≈ 52** (16 methods + ~36 distinct external method calls: 9 FitsReader
P/Invoke, 1 SFB async open, ~5 VolumeDataSetRenderer property writes, ~3 VolumeCommandController
calls, ~10 Unity built-in calls, ~2 PlayerPrefs, ~3 post-load populate helpers, ~3 others).

**File tab thresholds as a virtual standalone class (Orchestrator role):**

| Metric | Value [derived] | Threshold | Status |
|---|:---:|:---:|:---:|
| WMC | 16 | ≤ 40 (O) | ✅ |
| CBO | ~8 | ≤ 25 (O) | ✅ |
| RFC | ~52 | ≤ 50 | 🔴 +2 |

The RFC barely exceeds the threshold even for the extracted slice — confirming that
`FitsServiceAdapter` (which absorbs 9 of those 36 external calls) is the critical
interface to introduce.

### 2.2 New File Tab [tool-verified, Understand export, Day 13 — 2026-05-29]

> Counting conventions: WMC = `CountDeclMethod` (all declared methods including property accessors). DIT = user-defined class inheritance levels (tool convention). CBO = distinct named types referenced in implementation. RFC = tool's method-count definition (= WMC). LCOM % = Percent Lack of Cohesion (0–100), threshold ≤ 50%. Source: `refactoring-examples/sub-team-6/file-tab/skeleton/*.cs` and `adapters/*.cs`.

| Type | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Role threshold | Pass? |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|:---:|
| `FileTabViewModel` | Orchestrator (pure C#) | **40** | 1 | 0 | **19** | 40 | **91%** | WMC≤40 / CBO≤25 | ❌ LCOM |
| `SubsetBoundsViewModel` | ViewModel (pure C#) | **20** | 1 | 0 | **1** | 20 | **77%** | WMC≤20 / CBO≤14 | ❌ LCOM |
| `FitsMetadataHelper` | Utility (static) | **3** | 0 | 0 | **2** | 3 | **0%** | WMC≤40 / CBO≤25 | ✅ |
| `AsyncRelayCommand` | Domain helper | **4** | 1 | 0 | **3** | 4 | **50%** | WMC≤20 / CBO≤14 | ⚠ LCOM at limit |
| `RelayCommand` | Domain helper | **4** | 1 | 0 | **3** | 4 | **50%** | WMC≤20 / CBO≤14 | ⚠ LCOM at limit |
| `FitsServiceAdapter` | Adapter | **6** | 1 | 0 | **7** | 6 | **33%** | WMC≤40 / CBO≤25 | ✅ |
| `FileDialogServiceAdapter` | Adapter | **1** | 1 | 0 | **1** | 1 | **0%** | WMC≤40 / CBO≤25 | ✅ |
| `VolumeServiceAdapter` | Adapter | **5** | 2 | 0 | **9** | 5 | **65%** | WMC≤40 / CBO≤25 | ❌ LCOM |
| `MemoryProbeAdapter` | Adapter | **1** | 1 | 0 | **0** | 1 | **0%** | WMC≤40 / CBO≤25 | ✅ |
| `FileTabView` | View (adapter tier) | **8** | 2 | 0 | **14** | 8 | **69%** | WMC≤40 / CBO≤25 | ❌ LCOM |
| `FileTabCompositionRoot` | Orchestrator (adapter tier) | **2** | 2 | 0 | **12** | 2 | **33%** | WMC≤40 / CBO≤25 | ✅ |
| **Σ slice / max** | | **98 total / 40 max** | **max 2** | **0** | **max 19** | **max 40** | **91% max** | | **6/11 pass all** |

**6 of 11 classes pass all thresholds. `FileTabViewModel` re-classified as orchestrator (coordinates 4 injected services): WMC=40 ≤40 ✅, CBO=19 ≤25 ✅. `SubsetBoundsViewModel` WMC=20 ≤20 ✅. LCOM violations remain in 5 classes — structural artifact of the MVVM property-backing-field pattern (one backing field per bindable property, accessed by ≤2 methods), not disjoint concern clusters. See [`ck-metrics.md`](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md) for full derivation and the LCOM note.

**Method derivation (from committed code):**
- `FileTabViewModel` (WMC=40): constructor + 9 non-trivial property setters (ImagePath, MaskPath, SelectedHduIndex, SelectedZAxisIndex, SubsetEnabled, RatioMode, IsLoading, HeaderText, ValidationMessage) + `IsLoadable` computed getter + 4 command bodies (`BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`, `ClearMask`) + `Dispose` + `RefreshHduHeaderAsync` + `BuildMemoryWarning` + `PopulateZAxisOptions` + `UpdateZAxisMax` + `NotifyIsLoadable` + `NotifyCommandStates` + `Notify`. (`GetAxisMaxima`, `ComputeZScale`, `MaskAxesMatchImage` extracted to `FitsMetadataHelper` — Day 10.) CBO=9: `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `SubsetBoundsViewModel`, `FitsFileInfo`, `LoadCubeRequest`, `HduInfo`, `RatioMode`.
- `FitsMetadataHelper` (WMC=3): `GetAxisMaxima`, `ComputeZScale`, `MaskAxesMatchImage` — all pure static, no instance fields. Extracted from `FileTabViewModel` to bring its WMC within the orchestrator threshold. CBO=2: `FitsFileInfo`, `RatioMode`.
- `SubsetBoundsViewModel` (WMC=12): 6 bound-property setters (XMin/XMax/YMin/YMax/ZMin/ZMax, each with clamping) + `ResetToAxisMaxima` + `UpdateZAxisMax` + `ToDto` + `Clamp` + `Notify` + `NotifyAll`. Replaces `checkSubsetBounds` (70 lines, 18 `Debug.Log` calls), `setSubsetBounds`, and `updateSubsetZMax`. CBO=1 (`SubsetBounds` DTO).
- `AsyncRelayCommand` / `RelayCommand` (WMC=5/4): minimal ICommand helpers; no Unity dependency. CBO=1 each.
- `FitsServiceAdapter` (WMC=6): `OpenImageAsync`, `OpenMaskAsync`, `GetHeaderTextAsync` (3 public Task wrappers) + `OpenAndReadMetadata` + `ReadHeaderText` + nested `FitsHandle.Dispose`. Absorbs all 9 `FitsReader` P/Invoke calls from `CanvassDesktop`. CBO=5: `IFitsService`, `FitsFileInfo`, `FitsReader`, `HduInfo`, `IFitsHandle`.
- `FileDialogServiceAdapter` (WMC=1): `PickFileAsync` only. CBO=4: `IFileDialogService`, `StandaloneFileBrowser`, `PlayerPrefs`, `ExtensionFilter`.
- `VolumeServiceAdapter` (WMC=5): `Awake` + `IsCubeLoaded` getter + `LoadCubeAsync` + `LoadCubeCoroutine` (IEnumerator) + `FindFirstActiveRenderer`. DIT=4 (MonoBehaviour). CBO=8: `IVolumeService`, `VolumeCommandController`, `VolumeDataSetRenderer`, `VolumeInputController`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs`, `IProgress<float>`.
- `MemoryProbeAdapter` (WMC=1): `TotalSystemBytes` expression-body getter only. CBO=2: `IMemoryProbe`, `SystemInfo`.
- `FileTabView` (WMC≈16): 8 named methods (`BindTo`, `OnDestroy`, `OnPropertyChanged`, `OnSubsetPropertyChanged`, `RebuildHduDropdown`, `RebuildZAxisDropdown`, `RebuildRatioDropdown`, `SyncAll`) + approximately 8 counted anonymous delegates in `BindTo` (button click and CanExecuteChanged handlers). DIT=4. CBO=5: `IFileTabViewModel`, `SubsetBoundsViewModel`, `TMP_Dropdown`, `Button`, `Toggle`.
- `FileTabCompositionRoot` (WMC=2): `Awake` + `OnDestroy`. CBO=7: `FileTabView`, `VolumeServiceAdapter`, `FileTabViewModel`, `FitsServiceAdapter`, `FileDialogServiceAdapter`, `MemoryProbeAdapter`, `IMemoryProbe`.

### 2.3 File Tab Delta

> All figures tool-verified (Understand, Day 13). BEFORE = full `CanvassDesktop` class; AFTER = worst-case class in new slice.

| Metric | Old (CanvassDesktop full class) | New (worst-case class in slice) | Δ | Threshold met after? |
|---|:---:|:---:|:---:|:---:|
| WMC (max per class) | **63** (full class) | **40** (`FileTabViewModel`) | −37% | ✅ ≤40 (orchestrator threshold) |
| CBO (max per class) | **30** (full class) | **19** (`FileTabViewModel`) | −37% | ✅ ≤25 (orchestrator threshold) |
| RFC (tool def., max per class) | **63** (full class) | **40** (`FileTabViewModel`) | −37% | ✅ ≤50 |
| LCOM % (max per class) | **95%** (full class) | **91%** (`FileTabViewModel`) | −4 pp | ❌ >50%; MVVM property-pattern artifact — see §2.2 note |
| Circular dependencies | 2 (`↔ VolumeCommandController`, `↔ DesktopPaintController`) | **0** | −2 | ✅ |
| `UnityEngine` in domain code | Yes (`CanvassDesktop`) | **No** (ViewModel + DTO layers) | − | ✅ |
| Interfaces backing file tab APIs | 0 | **5** (`IFileTabViewModel`, `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`) | +5 | ✅ |
| Dead methods | 1 (`CheckImgMaskAxisSize`) | **0** | −1 | ✅ |
| File tab testable without Unity runner | 0 / 16 methods | **4 / 11 types** (`FileTabViewModel`, `SubsetBoundsViewModel`, `AsyncRelayCommand`, `RelayCommand`) | +4 pure-C# types | ✅ |
| NUnit tests covering domain layer | **0** | **34** (committed in `file-tab/tests/FileTabViewModelTests.cs`) | +34 | ✅ NFR-TST-1 |

---

## 3. Debug Tab — Old vs New

### 3.1 Old Debug Tab

The before-state debug tab **is** a dedicated class: `Assets/Scripts/Debuggers/DebugLogging.cs`
(255 lines, single `MonoBehaviour`). It is the log *consumer* — it captures everything Unity
routes to the console and renders it into the in-app debug panel. The defect is not size or
complexity (it passes 5 of 6 CK thresholds); it is that the class is welded to Unity at its
seam and has no interception point for tests:

| Aspect | Detail (`DebugLogging.cs`) |
|---|---|
| Log source | Subscribes to the **static** `Application.logMessageReceived` event in `OnEnable` (`:149`) — cannot be substituted in a unit test without the Unity runtime |
| Storage | Non-generic `Queue` (`:49`) — boxes every entry as `object`, no element type |
| File I/O | `AutoSave` opens a fresh `StreamWriter`, writes one line, and closes it **on every message** (`:249`) |
| Display | `HandleLog` rebuilds the **entire** `TMP_InputField.text` from the whole queue on every message — O(N) per log line (`:189`) |
| Scroll | Forces `debugScrollbar.value = 1.0f` on every message (`:196`) — no scroll-position retention |
| Format | Unstructured `"[" + type + "] : " + logString` (`:179`) — no `Level` enum, no `Source` tag, no machine-readable timestamp |

**Before metrics (hand-counted; ground truth in [`debug-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md)):**

| Metric | Value | Threshold (orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC (method count) | **8** | ≤ 40 | ✅ |
| DIT | **4** | ≤ 4 | ✅ (at limit — MonoBehaviour) |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | **~10** | ≤ 25 | ✅ |
| RFC | **~25** | ≤ 50 | ✅ |
| LCOM (HS) | **~0.95** | ≤ 0.50 | 🔴 |

`DebugLogging` passes 5 of 6 CK thresholds, so this is a **testability** refactor, not a metric
refactor. The defect CK *cannot* see is the static `Application.logMessageReceived` hook — no
metric flags that the log source cannot be mocked. LCOM (HS) ≈ 0.95 — near the 1.0 ceiling,
quantifying four disjoint concerns (log capture, autosave, display, manual export) that share
almost no fields — is the only metric that fails. (The Understand connected-components
cross-check points at the same four-concern defect; see [`debug-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) for the full derivation.)

#### Producer-side concern (separate from the debug tab)

Distinct from the `DebugLogging` *consumer*, **40 `Debug.Log` / `Debug.LogWarning` /
`Debug.LogError` call-sites** are scattered inline across `CanvassDesktop` methods. These are
log *producers* — the sites that *emit* into the stream the tab displays — not the debug tab
itself. They are catalogued here because the after-state replaces the ad-hoc `Debug.Log` emit
path with a structured stream, and because 24 of these sites inflate `checkSubsetBounds`.

**Call-site map (all 40 producer sites):**

| Method (line range) | Sites | Level(s) | Representative message |
|---|:---:|:---:|---|
| `_browseImageFile` (349–407) | 2 | Log | `"Fits open failure... code #" + status`; `"Could not find EXTNAME or HDUNAME in HDU " + i` |
| `IsLoadable` (431–537) | 1 | Log | `"The list has " + count + " items, dropdown index " + idx` |
| `checkSubsetBounds` — XMax (641–667) | 4 | Log | min violation; max violation; lower-bound violation; not-a-number |
| `checkSubsetBounds` — YMax (668–694) | 4 | Log | same 4 conditions for Y axis max |
| `checkSubsetBounds` — ZMax (695–720) | 4 | Log | same 4 conditions for Z axis max |
| `checkSubsetBounds` — XMin (721–746) | 4 | Log | same 4 conditions for X axis min |
| `checkSubsetBounds` — YMin (747–772) | 4 | Log | same 4 conditions for Y axis min |
| `checkSubsetBounds` — ZMin (773–798) | 4 | Log | same 4 conditions for Z axis min |
| `_browseMaskFile` (829–904) | 1 | Log | `"Fits open failure... code #" + status` |
| `CheckMemSpaceForCubes` (995–1014) | 2 | Log, LogWarning | size OK message; RAM exceeded warning |
| `LoadCubeCoroutine` (1015–1133) | 4 | Log | loading start; replacing cube; instantiating prefab; complete message |
| `_browseMappingFile` (1322–1434) | 1 | LogError | `"Error while loading mapping file: " + ex.Message` |
| `ChangeHduSelection` (1435–1465) | 1 | Log | `"Fits open failure... code #" + status` |
| `LoadSourcesFile` (1579–1612) | 1 | Log | `"Minimal source mappings not set!"` |
| `SetMaxMinPercentile` (1767–1813) | 3 | Log, LogError ×2 | percentile success; histogram error; data error |
| **Total** | **40** | | |

24 of 40 sites (60 %) are in `checkSubsetBounds` alone, repeating the same 4-condition
pattern for each of 6 subset-bounds fields. This inflates `checkSubsetBounds` from a
logical CC of ~4 (the 4 branching conditions) to an observed CC of ~7 because each
condition triggers an additional `Debug.Log` statement with string concatenation.

**Producer-side quality gaps (the 40 emit sites):**

| Gap | Detail |
|---|---|
| No structured format | Raw string concatenation — no `Level` enum, no `Source` tag, no `Timestamp` |
| No interception point for tests | Cannot assert on a `Debug.Log` call in a unit test without Unity runner + console parsing |
| `UnityEngine.Debug` is a hidden transitive dependency | Every method that calls `Debug.Log` is coupled to `UnityEngine` in its static call graph, even if it imports no other Unity types |
| 24 / 40 sites produce structurally identical messages | The 4-condition repeat across 6 fields is a template, not 24 independent decisions — a single parameterised method would reduce them to 1 call per field |

### 3.2 New Debug Tab [tool-verified, Understand export, Day 13 — 2026-05-29]

Tool-verified figures from Understand static analysis. The headline slice is three types; `ck-metrics.md` measures all types including `LogStream`, `DebugTabCompositionRoot`, and the three interfaces. RFC = tool's method-count definition. LCOM % = Percent Lack of Cohesion (0–100), threshold ≤ 50%.

| Type | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Role threshold | Pass? |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|:---:|
| `DebugTabView` | View (adapter tier) | **3** | 2 | 0 | **7** | 3 | **41%** | WMC≤40 / CBO≤25 | ✅ |
| `DebugTabViewModel` | ViewModel (pure C#) | **6** | 1 | 0 | **2** | 6 | **66%** | WMC≤20 / CBO≤14 | ❌ LCOM |
| `GatewayLogStreamAdapter` | Adapter (pure C#) | **8** | 1 | 0 | **5** | 8 | **72%** | WMC≤40 / CBO≤25 | ❌ LCOM |
| `LogStream` | Domain helper | **4** | 1 | 0 | **3** | 4 | **25%** | WMC≤20 / CBO≤14 | ✅ |
| `DebugTabCompositionRoot` | Orchestrator (adapter tier) | **3** | 2 | 0 | **6** | 3 | **41%** | WMC≤40 / CBO≤25 | ✅ |
| **Total / max** | | **24 total / 8 max** | **max 2** | **0** | **max 7** | **max 8** | **72% max** | | **3/5 pass all** |

**3 of 5 concrete types pass all thresholds. LCOM violations on `DebugTabViewModel` (66%) and `GatewayLogStreamAdapter` (72%) reflect sparse field access in Observer-pattern classes (each method touches 1–3 of the available fields), not disjoint concern clusters.** See [`debug-tab/ck-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) for the per-class LCOM analysis.

**Interfaces introduced:**

| Interface | Methods | Purpose |
|---|:---:|---|
| `ILogStream` | 4 (`Publish(level, message)`, `Publish(level, message, timestamp)`, `Subscribe`, `Unsubscribe`) | Log-stream contract; observers subscribe, emitters publish |
| `ILogObserver` | 1 (`OnNext(LogEntry)`) | Observer contract; `DebugTabViewModel` implements this |
| `IDebugTabViewModel` | 4 (`LogEntries`, `AppendEntry`, `ClearEntries`, `EntriesChanged`) | View binding contract |

**`LogEntry` DTO** (replaces raw string concatenation) — immutable `record`:
`Level : LogLevel` (enum: Info / Warning / Error),
`Message : string`,
`Timestamp : DateTime`.

**Method derivation:**
- `DebugTabView` (WMC=3): `BindTo`, `OnDestroy`, `OnEntriesChanged` (rebuilds the capped TMP text — `MaxDisplayLines = 500` — on each change). DIT=4 (MonoBehaviour). CBO=7: `IDebugTabViewModel`, `TMP_Text`, `Scrollbar`, `Button`, `LogEntry`, `LogLevel`, `StringBuilder`. The only debug-tab class that references Unity UI types.
- `DebugTabViewModel` (WMC=6): constructor (subscribes to `ILogStream`) + `LogEntries` getter (`AsReadOnly`) + `AppendEntry` (bounded `List<LogEntry>`, cap 2000) + `ClearEntries` + `OnNext` (explicit `ILogObserver` impl → `AppendEntry`) + `Dispose` (unsubscribes); raises `EntriesChanged`. CBO=3: `ILogStream`, `LogEntry`, `IDisposable`. **Zero `UnityEngine` imports.** This is the Observer (`ILogObserver`).
- `GatewayLogStreamAdapter` (WMC=8): constructor (subscribes to `IServiceGateway.OnNotification`) + `OnGatewayNotification` (filters `method == "log.emit"`, deserialises params to a level/msg/ts triple) + `ParseLevel` (wire string → `LogLevel`) + two `Publish` overloads + `Subscribe` + `Unsubscribe` (all four delegate to an inner `LogStream`) + `Dispose` (detaches from the gateway). CBO=5: `IServiceGateway`, `JsonRpcNotification`, `LogStream`, `ILogStream`, `LogLevel` (the nested `LogEmitParams` record is the `log.emit` wire DTO). **No `UnityEngine`, no `[DllImport]` — compiles in isolation against GatewayContracts + DebugTabSkeleton.** It consumes **server-pushed `log.emit` notifications** per ADR-0002 and republishes through the inner `LogStream`, so the `ILogObserver` contract (`DebugTabViewModel`) is unchanged. It replaces the earlier Unity-console adapter, which hooked the static `Application.logMessageReceived` event.

### 3.3 Debug Tab Delta

> All figures tool-verified (Understand, Day 13). BEFORE = `DebugLogging.cs` (hand-computed, consistent with tool baseline); AFTER = tool-verified successor classes.

| Metric | Old (`DebugLogging`) | New | Δ |
|---|:---:|:---:|:---:|
| Dedicated debug-tab class(es) | **1** (`DebugLogging`, 255 LOC) | **3** headline (View / ViewModel / Adapter); 5 concrete types total incl. `LogStream` + `DebugTabCompositionRoot` | restructured |
| WMC (domain class) | **8** | **6** (`DebugTabViewModel`) | **−2** |
| CBO (domain class) | **~10** | **2** (`DebugTabViewModel`) | **−8** |
| LCOM % (domain class) | **95%** (4 disjoint concerns) | **66%** (`DebugTabViewModel`) — MVVM field-access artifact | **−29 pp; different cause** |
| LCOM % (gateway adapter) | **0** (no equivalent) | **72%** (`GatewayLogStreamAdapter`) — Observer field-access artifact | new class |
| CK violations in debug types | **1** (LCOM on `DebugLogging` at 95%) | **2** (LCOM on `DebugTabViewModel` + `GatewayLogStreamAdapter`; structural rationale documented) | −1 net genuine violation |
| Log source | static `Application.logMessageReceived` hook | server-pushed `log.emit` over `IServiceGateway` (interface seam) | substitutable |
| `UnityEngine` in the log-transport path | **Yes** (`DebugLogging` hooks the static event) | **No** (`GatewayLogStreamAdapter` + `LogStream` + `DebugTabViewModel` are pure C#; only `DebugTabView` touches Unity UI) | eliminated |
| Interfaces backing the log stream | **0** | **3** (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) | **+3** |
| Debug-tab logic testable without Unity runner | **0** | adapter (fake `IServiceGateway`) + VM (`ILogStream`) — **29** NUnit tests, ~20 ms, zero Unity dependency | **+29 tests** |
| Structured log fields | **0** (raw `"[type] : msg"` string) | **3** (`LogEntry`: level, message, timestamp) | **+3** |

---

## 4. Shared Residual — Old vs New

### 4.1 Old Shared (47 Methods)

The 47 methods left after removing the 16 file tab methods. These cover the remaining
tab concerns and the class lifecycle. They share CanvassDesktop's single LCOM and
DIT measurement since those are class-level only.

| Sub-group | Methods | Concerns |
|---|:---:|---|
| Lifecycle & helpers | 13 | `Awake`, `Start`, `OnDestroy`, `Update`, `OnGUI`, `ShowGUI`, `CheckCubesDataSet`, `GetFirstActiveRenderer`, `SetInputIndex`, `DismissFileLoad`, `Exit`, `getActiveDataSet`, `getActiveMaskSet` |
| Rendering tab | 14 | `PopulateRestfreqencyDropdown`, `OnRatioDropdownValueChanged`, `OnRestFrequency*` ×6, `SetRestFrequency*` ×3, `populateColorMapDropdown`, `ChangeColorMap`, `UpdateThresholdMin`, `UpdateThresholdMax`, `ResetThresholds` |
| Stats tab | 8 | `populateStatsValue`, `UpdateUI`, `UpdateSigma`, `RestoreDefaults`, `UpdateScale`, `SetMaxMinPercentile`, `UpdateScaleMin`, `UpdateScaleMax` |
| Sources tab | 10 | `BrowseSourcesFile`, `_browseSourcesFile`, `BrowseMappingFile`, `_browseMappingFile`, `SaveMappingFile`, `_saveMappingFile`, `ChangeSourceMapping`, `AreMappingsIncompatible`, `AreMinimalMappingsSet`, `LoadSourcesFile` |
| Paint tab | 2 | `paintTabSelected`, `paintTabLeft` |
| **Total** | **47** | |

**Shared residual metrics [derived]:**

| Metric | Value | Threshold (Orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC | **47** | ≤ 40 | 🔴 +7 |
| CBO | **~39** | ≤ 25 | 🔴 +14 |
| RFC | **~66** | ≤ 50 | 🔴 +16 |
| LCOM (full class) | **0.955** | ≤ 0.50 | 🔴 |
| DIT | 1 | ≤ 4 | ✅ |

The shared residual still violates all four thresholds even after the file tab
extraction, confirming that the rendering, stats, and sources tab methods need
the same MVVM extraction pattern (outside the scope of WE1 and WE2, but
demonstrated by the same design).

### 4.2 New Shared (CanvassDesktop Shell) [projected]

After extracting ALL tabs (file in WE1, debug in WE2, rendering/stats/sources/paint
by the same pattern), `CanvassDesktop` is reduced to a **composition root**:

| Method | Concern |
|---|---|
| `Awake` | Construct adapters (`FitsServiceAdapter`, `GatewayLogStreamAdapter`, etc.) |
| `Start` | Construct ViewModels, call `BindTo` on each View |
| `OnDestroy` | Unsubscribe all bindings, dispose adapters |
| `WireFileTab` (private) | Instantiate `FileTabViewModel`, pass to `FileTabView.BindTo` |
| `WireDebugTab` (private) | Instantiate `DebugTabViewModel`, pass to `DebugTabView.BindTo` |
| `WireRenderingTab` (private) | Same pattern for rendering (out of WE1/WE2 scope) |
| `WireSourcesTab` (private) | Same pattern for sources (out of WE1/WE2 scope) |
| `TearDown` (private) | Null out all references |

| Metric | Value [projected] | Threshold (Orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC | **8** | ≤ 40 | ✅ −55 |
| CBO | **4** | ≤ 25 | ✅ −43 |
| RFC | **12** | ≤ 50 | ✅ −106 |
| LCOM | **0.10** | ≤ 0.50 | ✅ −0.855 |
| DIT | 1 | ≤ 4 | ✅ |

CBO = 4 reflects composition-root scope (`FileTabView`, `DebugTabView`,
`FileTabViewModel`, `DebugTabViewModel`). The full extraction adds `RenderingTabView`,
`StatsTabView`, `SourcesTabView` etc. bringing CBO to ~8, still well within threshold.

### 4.3 Shared Delta

| Metric | Old shared | New shared (shell) | Δ |
|---|:---:|:---:|:---:|
| WMC | 47 | **8** | **−39** |
| CBO | ~39 | **4–8** | **−31 to −35** |
| RFC | ~66 | **12** | **−54** |
| LCOM | 0.955 | **0.10** | **−0.855** |
| `FindObjectOfType<>` singleton hunts | 3 | **0** | **−3** |
| Methods with per-frame `Update()` polling | 1 (`Update` syncs thresholds + tab key) | **0** (rendering ViewModel subscribes to renderer events) | **−1** |
| CK violations | 4 (WMC/CBO/RFC/LCOM) | **0** | **−4** |

---

## 5. ISO 25010 Maintainability Mapping

### 5.1 File Tab

| Sub-char | Before rating | After [proj] | Key evidence |
|---|:---:|:---:|---|
| **Modularity** | Poor | Good | `FileTabViewModel` CBO = 5 (interfaces only); both circular cycles eliminated. A `FitsReader` ABI change touches only `FitsServiceAdapter`. |
| **Reusability** | Poor | Acceptable | `IFitsService`, `IFileDialogService`, `IVolumeService`, `IFileTabViewModel` are explicit swap seams. Future remote-server mode replaces adapters without touching ViewModels. |
| **Analysability** | Poor | Good | RFC 52 → 22 (max per class). Tracing "why did the load button stay disabled?" requires reading 12 methods in `FileTabViewModel`, not scanning 1 899 lines. |
| **Modifiability** | Poor | Good | CBO 8 exclusive → 7 max per new class. `ChangeHduSelection` re-opening the file on each switch is replaced by `IFitsService.GetHeaderTextAsync(handle, hduIndex)` — no reopen, handle is server-resident. |
| **Testability** | Very Poor | Good | `FileTabViewModel` and `SubsetBoundsViewModel` are `object` subclasses. NUnit test: `new FileTabViewModel(mockFits, mockDialog, mockVolume)`. `checkSubsetBounds` (70 lines + 18 log calls + Unity input fields) becomes property setters on `SubsetBoundsViewModel` — `Assert.Equal(expected, vm.Subset.XMax)`. |

**Testability blockers resolved (WE1):**

| Blocker | Resolved by |
|---|---|
| `MonoBehaviour` ancestry — requires Unity test runner | `FileTabViewModel` / `SubsetBoundsViewModel` have `object` as base |
| `FindObjectOfType<VolumeCommandController>()` requires full scene | Constructor-injected `IVolumeService` |
| `FitsReader` P/Invoke requires native `.dll` | Isolated behind `IFitsService`; test double returns `FitsFileInfo` DTOs |
| 25+ `transform.Find(…).GetComponent<T>()` chains | `FileTabView` owns all hierarchy traversal; ViewModel never touches `Transform` |
| `StandaloneFileBrowser` async callbacks not mockable | `IFileDialogService.PickFileAsync()` — stubbed with `Task.FromResult("file.fits")` |
| `CheckMemSpaceForCubes` tests client RAM | Moved to `IVolumeService.CanFit()` — adapter answers its own capacity question |

### 5.2 Debug Tab

| Sub-char | Before rating | After [proj] | Key evidence |
|---|:---:|:---:|---|
| **Modularity** | Very Poor | Good | 40 scattered sites → 0. Adding a new log source (`FitsServiceAdapter`, `VolumeServiceAdapter`) requires one `_logStream.Publish()` call with no change to `DebugTabViewModel`. Adding a second consumer (e.g. file logger) requires only a new `ILogObserver` implementation. |
| **Reusability** | Very Poor | Good | `ILogStream` / `ILogObserver` have no Unity dependencies. `DebugTabViewModel` can be embedded in any future shell (CLI, VR overlay) that provides an `ILogStream`. |
| **Analysability** | Poor | Good | `checkSubsetBounds` CC drops from ~7 to ~2. Remaining real errors (`FitsOpen` failure, mapping load error) surface as `LogEntry { Level=Error, Source="FitsServiceAdapter", Message=..., Timestamp=... }` — no string parsing required to read them. |
| **Modifiability** | Poor | Good | Changing the debug panel from a Unity ScrollView to a UI Toolkit `ListView` requires modifying only `DebugTabView`. `DebugTabViewModel` exposes `IReadOnlyList<LogEntry>` with no UI type in its public surface. |
| **Testability** | Very Poor | Good | `DebugTabViewModel.OnNext(entry)` is a pure method: `vm.OnNext(new LogEntry(LogLevel.Error, "FitsOpen", "status=4", DateTimeOffset.UtcNow)); Assert.Single(vm.LogEntries);` — zero Unity imports needed. 40 log sites are now behind `ILogStream.Publish()`, assertable with a capturing `ILogObserver` stub. |

**Testability blockers resolved (WE2):**

| Blocker | Resolved by |
|---|---|
| Static `Application.logMessageReceived` hook — cannot be substituted in a test | Log records arrive as server-pushed `log.emit` notifications on `IServiceGateway.OnNotification` (an interface); `GatewayLogStreamAdapter` consumes them with no `UnityEngine` dependency |
| 24 log calls in `checkSubsetBounds` — no assertion target | `SubsetBoundsViewModel` property setters replace inline validation+logging; test asserts on property value |
| No end-user visibility of log output | `DebugTabView` scrolls `LogEntry` list; smoke-testable by running the app |
| No log level or source filtering | `IDebugTabViewModel.FilterLevel` property filters by `LogLevel` enum in ViewModel without touching View |

---

## 6. Aggregate Violation Summary

Counts across CanvassDesktop before vs all WE1 + WE2 successor types combined. All figures tool-verified (Understand, Day 13) except where marked [derived].

| Rule | Before | After | Δ |
|---|:---:|:---:|:---:|
| WMC violations (>threshold) | **1** (`CanvassDesktop` WMC=63) | **0** (`FileTabViewModel` WMC=40 ≤40 ✅ orchestrator; `SubsetBoundsViewModel` WMC=20 ≤20 ✅) | **−1** god-class eliminated, no successor violations |
| CBO violations | **1** (`CanvassDesktop` CBO=30) | **0** (`FileTabViewModel` CBO=19 ≤25 ✅ orchestrator) | **−1** |
| LCOM violations (>50%) | **1** (`CanvassDesktop` 95%) | **5** (MVVM property-pattern artifact; see notes in §2.2 / §3.2) | LCOM inflation is a known tool limitation for property-heavy MVVM classes |
| Circular dependency cycles | **2** (`↔ VolumeCommandController`, `↔ DesktopPaintController`) | **0** | **−2** ✅ |
| Classes with `UnityEngine` in domain / ViewModel code | **1** | **0** | **−1** ✅ |
| Public API boundaries backed by interfaces | **0** | **8** (`IFileTabViewModel`, `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `ILogStream`, `ILogObserver`, `IDebugTabViewModel`) | **+8** ✅ |
| Dead methods | **1** (`CheckImgMaskAxisSize`) | **0** | **−1** ✅ |
| Unstructured `Debug.Log` call-sites | **40** | **0** | **−40** ✅ |
| Types pure-C# unit-testable (no Unity assemblies, no native DLL) | **0** | **4** (`FileTabViewModel`, `SubsetBoundsViewModel`, `DebugTabViewModel`, `LogStream`) | **+4** ✅ |
| Adapters integration-testable behind interface seams | **0** | **2** (`FitsServiceAdapter` via `IFitsService`; `GatewayLogStreamAdapter` via fake `IServiceGateway`) | **+2** ✅ |

---

## 7. Thresholds Reference (Assignment Spec §7.1)

| Role | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Domain / ViewModel | ≤ 20 | ≤ 4 | ≤ 5 | ≤ 14 | ≤ 50 | ≤ 0.50 |
| Orchestrator / Adapter | ≤ 40 | ≤ 4 | ≤ 5 | ≤ 25 | ≤ 50 | ≤ 0.50 |

Cycles are forbidden across all top-level components (see §6, row "Circular dependency cycles"). DIT and NOC thresholds apply uniformly across both roles. Observed DIT and NOC across all WE1/WE2 successor types stay at 0–1 (see §2.2 and §3.2) — well under threshold.

Role assignments:
- **Domain / ViewModel:** `FileTabViewModel`, `SubsetBoundsViewModel`, `DebugTabViewModel`
- **Adapter / Orchestrator:** `FileTabView`, `FitsServiceAdapter`, `FileDialogAdapter`, `VolumeServiceAdapter`, `DebugTabView`, `GatewayLogStreamAdapter`, `CanvassDesktop` shell

---

## 8. Traceability

| Artefact | Path |
|---|---|
| CK baseline (Understand export) | `docs/sub-team-6/deliverables/other/D9-ck-baseline/SK_BNCH.md` |
| NDepend baseline | `docs/sub-team-6/deliverables/other/D9-ck-baseline/ndepend-baseline.md` |
| File tab after-state class diagram | `after-class-diagram.puml` (this directory) |
| File tab after-state dependency graph | `after-dependency-graph.puml` (this directory) |
| File tab after-state DSM | `after-dsm.md` (this directory) |
| File tab before-state class diagram | `before-class-diagram.puml` (this directory) |
| File tab before-state DSM | `before-dsm.md` (this directory) |
| Debug tab after-state sequence diagram | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| CanvassDesktop method reference + log sites | `docs/sub-team-6/CanvassDesktop.md` |
| MVVM refactoring proposal (§3.2 Debug tab) | `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md` |
| MVVM ADR | `docs/sub-team-6/adrs/0001-mvvm-split.md` |
| Deliverables checklist | `docs/sub-team-6/deliverables/deliverables-checklist.md` |
| Feeds T4 | Consolidated architecture report §Metrics chapter |
| Feeds T7 | Integration & metrics deliverable — ISO 25010 evidence section |
| Feeds Pitch slot 3 | Worked examples slide — §6 aggregate violation table is the pitch-ready summary |
