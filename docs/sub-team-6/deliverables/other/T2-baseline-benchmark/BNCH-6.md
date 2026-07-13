# BNCH-6 — Mocking-Difficulty Count (Static + Unity API Calls per Class)


---

## Method

Three signal types were grep-counted across every `.cs` file under `Assets/Scripts/UI/` and `Assets/Scripts/Menu/` (30 classes total):

| Signal | Pattern | Why it matters for mocking |
|---|---|---|
| **A — Runtime singleton lookup** | `FindObjectOfType`, `FindObjectsOfType`, `GameObject.Find` | Implicit global state; no seam without a service-locator abstraction |
| **B — Scene-graph traversal** | `transform.Find(`, `GetComponent<`, `GetComponentsInChildren<`, `GetComponentInChildren<`, `GetComponentInParent<` | Requires a live Unity scene to resolve; untestable without fake hierarchy |
| **C — Static external call** | `StandaloneFileBrowser.`, `FitsReader.`, `DllImport`, `Resources.Load`, `PlayerPrefs.`, `Application.` | P/Invoke and static library calls have no mock seam at all |

**Total = A + B + C.** Rating: 🔴 High ≥ 20 · 🟡 Medium 5–19 · 🟢 Low < 5.

---

## Results — UI layer (`Assets/Scripts/UI/`)

| Class | A — Singleton lookup | B — Scene-graph | C — Static external | **Total** | Rating |
|---|:---:|:---:|:---:|:---:|:---:|
| `CanvassDesktop` | 6 | 163 | 36 | **205** | 🔴 |
| `DesktopPaintController` | 2 | 27 | 0 | **29** | 🔴 |
| `Menus/RenderingController` | 1 | 24 | 0 | **25** | 🔴 |
| `ToastNotification` | 1 | 8 | 0 | **9** | 🟡 |
| `PointerController` | 0 | 5 | 0 | **5** | 🟡 |
| `Colorbar` | 0 | 4 | 1 | **5** | 🟡 |
| `MenuBarBehaviour` | 0 | 0 | 4 | **4** | 🟡 |
| `LaserPointer` | 1 | 3 | 0 | **4** | 🟡 |
| `Menus/OptionController` | 0 | 2 | 0 | **2** | 🟢 |
| `UserSelectableItem` | 0 | 3 | 0 | **3** | 🟢 |
| `UserScrollableItem` | 1 | 2 | 0 | **3** | 🟢 |
| `CustomDragHandler` | 0 | 1 | 0 | **1** | 🟢 |
| `UserConfirmationPopupController` | 0 | 1 | 0 | **1** | 🟢 |
| `BrushSizeTooltip` | 0 | 0 | 0 | **0** | 🟢 |
| `ButtonHoverBehaviour` | 0 | 0 | 0 | **0** | 🟢 |
| `KeypadController` | 0 | 0 | 0 | **0** | 🟢 |
| `PngExporter` | 0 | 0 | 0 | **0** | 🟢 |
| `PopUpButtonController` | 0 | 0 | 0 | **0** | 🟢 |
| `UserDraggableMenu` | 0 | 0 | 0 | **0** | 🟢 |

---

## Results — Menu layer (`Assets/Scripts/Menu/`)

| Class | A — Singleton lookup | B — Scene-graph | C — Static external | **Total** | Rating |
|---|:---:|:---:|:---:|:---:|:---:|
| `QuickMenuController` | 1 | 27 | 0 | **28** | 🔴 |
| `PaintMenuController` | 1 | 24 | 0 | **25** | 🔴 |
| `MomentMapMenuController` | 0 | 8 | 6 | **14** | 🟡 |
| `ExitController` | 0 | 6 | 2 | **8** | 🟡 |
| `HistogramMenuController` | 1 | 6 | 0 | **7** | 🟡 |
| `VideoRecordMenuController` | 1 | 3 | 1 | **5** | 🟡 |
| `TabsManager` | 0 | 4 | 0 | **4** | 🟡 |
| `ShapeMenuController` | 1 | 2 | 0 | **3** | 🟢 |
| `SourceRow` | 0 | 2 | 0 | **2** | 🟢 |
| `SpectralProfileMenuController` | 0 | 1 | 1 | **2** | 🟢 |
| `SpectralProfileHelper` | 0 | 1 | 0 | **1** | 🟢 |
| `VideoRecPointListController` | 0 | 1 | 0 | **1** | 🟢 |
| `HistogramHelper` | 0 | 0 | 0 | **0** | 🟢 |

---

## Summary

| Rating | Count | Classes |
|---|---|---|
| 🔴 High (≥ 20) | 5 | `CanvassDesktop`, `DesktopPaintController`, `RenderingController`, `QuickMenuController`, `PaintMenuController` |
| 🟡 Medium (5–19) | 9 | `ToastNotification`, `PointerController`, `Colorbar`, `MenuBarBehaviour`, `LaserPointer`, `MomentMapMenuController`, `ExitController`, `HistogramMenuController`, `VideoRecordMenuController`, `TabsManager` |
| 🟢 Low (< 5) | 16 | All others |

**Sub-team 6 primary concern:** `CanvassDesktop` at 205 total call sites dwarfs every other class. Column B (scene-graph traversal) accounts for 163 of those — all `transform.Find(…).GetComponent<T>()` chains that are irreplaceable without a live Unity scene. Column C (36 static-external calls) covers the direct `FitsReader` P/Invoke and `StandaloneFileBrowser` calls that have no mock seam at all.

---

## Implication for testability (feeds T2)

Classes rated 🔴 cannot be unit-tested without either:
- A live Unity Editor test (Play Mode / Edit Mode) — slow, not CI-friendly, counts against the ≥ 70 % branch/line gate on domain code; OR
- An MVVM split that pushes all Unity-API and static calls into the View layer or an adapter, leaving a Unity-free ViewModel testable with NUnit + Moq.

The MVVM refactoring (ADR-01) directly reduces `CanvassDesktop`'s column B and C counts to **zero** in the ViewModel layer. Column A (`FindObjectOfType`) is eliminated by constructor-injecting `IFileService`, `IDialogService`, and `ILogStream` interfaces.

---

## Traceability

| Item | Reference |
|---|---|
| CK baseline (CBO / RFC / WMC for same classes) | [SK_BNCH.md](SK_BNCH.md) |
| MVVM split decision | [adrs/0001-mvvm-split.md](../adrs/0001-mvvm-split.md) |
| ViewModel unit-test strategy | [TEST-1.md](TEST-1.md) |
| Dependency graph (CanvassDesktop File tab) | [ex1-file_tab/before-dependency-graph.puml](ex1-file_tab/before-dependency-graph.puml) |
| T2 team benchmark report | feeds `docs/sub-team-6/deliverables/SonarQube Baseline report.md` |
| Assignment spec reference | §7.2 coverage targets, §6.6 testability strategy |
