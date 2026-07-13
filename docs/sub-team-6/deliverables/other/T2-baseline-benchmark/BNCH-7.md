# BNCH-7 — Interface-Size Audit (ISP ≤ 7 Public Members)

**Date:** 2026-05-28 (Day 9) · **Owner:** Quality Champion · **Requirement:** NFR-REU-2 (brief §7.2) — *every interface produced by the MVVM split exposes ≤ 7 public members (ISP).*

---

## Method

Counted the **own declared public members** (methods + properties + events; overloaded methods counted separately) of every `interface` defined in the file-tab, debug-tab, and contracts skeletons under `refactoring-examples/sub-team-6/`. Members inherited from framework base contracts — `IDisposable.Dispose`, `IAsyncDisposable.DisposeAsync`, `INotifyPropertyChanged.PropertyChanged` — are listed in the *Inherits* column and **not** counted toward the ISP budget: ISP governs the segregation surface a class introduces, not standard library base interfaces.

Source files (read-only ground truth):

| Interface | File |
|---|---|
| `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle`, `ICommand`, `IAsyncCommand`, `IFileTabViewModel` | `refactoring-examples/sub-team-6/file-tab/skeleton/` |
| `IDebugTabViewModel`, `ILogStream`, `ILogObserver` | `refactoring-examples/sub-team-6/debug-tab/skeleton/` |
| `IServiceGateway` | `refactoring-examples/sub-team-6/contracts-team1/IServiceGateway.cs` |

DTO / value types (`LogEntry`, `FitsFileInfo`, `HduInfo`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs`, `JsonRpcNotification`) are data carriers, not behavioural interfaces, and are out of ISP scope.

**Threshold:** ≤ 7 own public members (NFR-REU-2 / brief §7.2). Rating: 🟢 ≤ 7 · 🔴 > 7.

---

## Results

| Interface | Layer / role | Own public members | Count | Inherits | ≤ 7? |
|---|---|---|---:|---|:--:|
| `IFitsService` | domain seam (ACL over FITS P/Invoke) | `OpenImageAsync`, `OpenMaskAsync`, `GetHeaderTextAsync` | 3 | — | 🟢 |
| `IFileDialogService` | domain seam (ACL over file picker) | `PickFileAsync` | 1 | — | 🟢 |
| `IVolumeService` | domain seam (Sub-team 1 gateway) | `IsCubeLoaded`, `LoadCubeAsync`, `CubeLoaded` (event) | 3 | — | 🟢 |
| `IMemoryProbe` | domain seam (ACL over `SystemInfo`) | `TotalSystemBytes` | 1 | — | 🟢 |
| `IFitsHandle` | domain resource handle | `FilePath` | 1 | `IDisposable` | 🟢 |
| `ICommand` | domain (command pattern) | `CanExecute`, `Execute`, `CanExecuteChanged` (event) | 3 | — | 🟢 |
| `IAsyncCommand` | domain (async command) | `CanExecute`, `ExecuteAsync`, `CanExecuteChanged` (event) | 3 | — | 🟢 |
| `IDebugTabViewModel` | VM facade | `LogEntries`, `AppendEntry`, `ClearEntries`, `EntriesChanged` (event) | 4 | — | 🟢 |
| `ILogStream` | domain seam (Observer subject) | `Publish` (×2 overloads), `Subscribe`, `Unsubscribe` | 4 | — | 🟢 |
| `ILogObserver` | domain seam (Observer) | `OnNext` | 1 | — | 🟢 |
| `IServiceGateway` | transport seam (ADR-0002) | `ConnectAsync`, `SendAsync<T>`, `OnNotification` (event) | 3 | `IAsyncDisposable` | 🟢 |
| **`IFileTabViewModel`** | **VM facade** | **14 properties + 4 commands** | **18** | `INotifyPropertyChanged` | 🔴 |

**11 of 12 interfaces are within the ≤ 7 ISP budget.** Every *dependency seam* — the interfaces a ViewModel depends on and a test mocks — is ≤ 4 members. The single exception is the `IFileTabViewModel` binding facade (18).

---

## Finding — `IFileTabViewModel` (18 members, over the ≤ 7 threshold)

The 18 members are 14 read properties + 4 commands:

- **Paths:** `ImagePath`, `MaskPath`
- **HDU selection:** `HduOptions`, `SelectedHduIndex`
- **Z-axis selection:** `ZAxisOptions`, `SelectedZAxisIndex`
- **Subset:** `SubsetEnabled`, `Subset`
- **Aspect ratio:** `RatioModeOptions`, `RatioMode`
- **Derived state:** `IsLoadable`, `HeaderText`, `IsLoading`, `ValidationMessage`
- **Commands:** `BrowseImageCommand`, `BrowseMaskCommand`, `LoadCommand`, `ClearMaskCommand`

**Is this a true ISP violation?** No — the classic ISP statement ("no client is forced to depend on methods it does not use") is *not* breached. `IFileTabViewModel` has exactly one consumer, the `FileTabView` binding layer, and the View binds every property and command. No client carries an unused dependency. What is exceeded is the team's **hard numeric ceiling** (NFR-REU-2 ≤ 7), which we apply uniformly and therefore flag.

**Why it is shaped this way.** `IFileTabViewModel` is a *ViewModel binding facade*, not a role/dependency seam. The interfaces ISP and DIP actually target — the ones the ViewModel *depends on* and the test suite *mocks* — are the four service interfaces it composes (`IFitsService` 3, `IFileDialogService` 1, `IVolumeService` 3, `IMemoryProbe` 1), each ≤ 7. The facade aggregates the View's read surface; aggregation there is cohesion, not coupling. (`IDebugTabViewModel` shows a VM facade can be small — 4 members — when the panel is simpler.)

**Remediation options:**

1. **Segregate the facade into role interfaces** the View composes — e.g. `IFilePathsView`, `IHduSelectionView`, `ISubsetView`, `IFileTabStatus`, `IFileTabCommands`. Cost: 5 interfaces + a composite to satisfy a numeric metric that the single consumer does not benefit from; fragments a cohesive binding surface.
2. **Accept with a documented trade-off** (brief §4.2 #1 — "no SOLID/GRASP violation *without a documented trade-off*"). The facade is single-consumer; the segregation that matters for mockability and DIP already passes. This row *is* the documented trade-off.

**Recommendation:** Accept `IFileTabViewModel` as a single-consumer binding facade under option 2. Keep ≤ 7 as a **hard gate on dependency-seam interfaces** (where it guards one-line mockability) — all of which pass — and treat VM-facade size as advisory.

---

## Verdict

ISP target met on **11 / 12** interfaces. The eight dependency seams are uniformly ≤ 4 members, which is what keeps the mocking surface one `Mock<I>()` line per test (cross-ref [BNCH-6](BNCH-6.md): ViewModel mocking-difficulty = 0; each of the 47 file-tab tests + 29 debug-tab tests mocks these seams in one line). The one over-threshold interface is a single-consumer ViewModel facade, accepted with a documented trade-off per §4.2 #1 — no mockability, DIP, or testability consequence.

---

**Cross-references:** [`requirements.md`](../../D1-requirements/requirements.md) NFR-REU-2 · [`BNCH-6.md`](BNCH-6.md) (mocking-difficulty count) · file-tab / debug-tab `ck-metrics.md` (CBO, RFC, WMC).
