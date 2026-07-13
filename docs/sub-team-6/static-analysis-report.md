# Static Analysis Report — Sub-team 6 Slice (8 Classes)

**Date:** 2026-05-25  
**Author:** Rory Harrington (RoryH06)  
**Scope:** CanvassDesktop, DesktopPaintController, PaintMenuController, VideoUiManager, HistogramMenuController, HistogramHelper, SourceRow, TabsManager

---

## CK Metrics

| Class | Lines | WMC | DIT | NOC | CBO | RFC | LCOM | Status |
|---|---|---|---|---|---|---|---|---|
| **CanvassDesktop** | 1900 | 64 | 1 | 0 | 47 | 118 | 0.955 | 🔴 CRITICAL |
| **DesktopPaintController** | 1558 | 57 | 1 | 0 | ~30 | ~85 | ~0.72 | 🔴 HIGH |
| **PaintMenuController** | 371 | 24 | 1 | 0 | ~15 | ~48 | ~0.48 | 🟡 MEDIUM |
| **VideoUiManager** | 439 | 17 | 1 | 0 | ~12 | ~35 | ~0.30 | 🟢 OK |
| **HistogramMenuController** | 222 | 13 | 1 | 0 | ~10 | ~30 | ~0.40 | 🟢 OK |
| **HistogramHelper** | 101 | 3 | 1 | 0 | ~9 | ~15 | ~0.20 | 🟡 MEDIUM* |
| **SourceRow** | 61 | 3 | 1 | 0 | 4 | ~8 | ~0.10 | 🟢 OK |
| **TabsManager** | 110 | 3 | 1 | 0 | 4 | ~10 | ~0.20 | 🟢 OK |

> DIT = 1 across the board (all inherit `MonoBehaviour` only). NOC = 0 for all (no subclasses in this slice).  
> *HistogramHelper is small but has a structural coupling issue — see below.

**Thresholds (Section 7.1):** WMC ≤ 20 domain / ≤ 40 adapters · CBO ≤ 14 domain / ≤ 25 orchestrators · RFC ≤ 50 · LCOM ≤ 0.5

---

## Code Smells by Class

### CanvassDesktop — Critical

- **God Class**: 1900 lines, 64 methods, 5+ distinct responsibilities: file I/O, FITS header parsing, UI wiring, source/mapping management, threshold/histogram control, paint tab lifecycle.
- **Shotgun Surgery risk**: 47 external dependencies — a change in almost any system ripples here.
- **Deep transform.Find chains**: e.g. `renderingPanelContent.gameObject.transform.Find("Rendering_container").gameObject.transform.Find("Viewport")...` repeated dozens of times. Scene hierarchy changes silently break these at runtime with no compile-time error.
- **FindObjectOfType coupling**: `FindObjectOfType<VolumeInputController>()`, `FindObjectOfType<VolumeCommandController>()`, `FindObjectOfType<HistogramHelper>()` — no DI, untestable.
- **Duplicated file-browse pattern**: `BrowseImageFile`/`_browseImageFile`, `BrowseMaskFile`/`_browseMaskFile`, `BrowseSourcesFile`/`_browseSourcesFile`, `BrowseMappingFile`/`_browseMappingFile` — 4× near-identical open/callback/delegate pairs.
- **Long method**: `checkSubsetBounds` is ~80 lines of copy-pasted bounds-checking logic for 6 input fields.
- **Magic string**: `"LastPath"` player pref key used inline in 4 places, no constant defined.

### DesktopPaintController — High

- **Large class**: 1558 lines, 57 methods. Mixes texture rendering, camera management, mask voxel logic, polygon math, and UI event handling in one `MonoBehaviour`.
- **Repeated axis switch pattern**: `if(axis == 0) ... if(axis == 1) ... if(axis == 2)` copy-pasted verbatim in at least 10 methods — adding a 4th axis requires editing every one.
- **Real bug** (line 305–307): `UpdateMaxValue(float value)` sets `minVal = value` instead of `maxVal = value`.
- **FindObjectOfType**: `FindObjectOfType<CanvassDesktop>()` in `StartPaintSelection` — no injection.
- **Texture allocation in hot path**: `new Texture2D(...)`, `new Color[size]` inside `GetSlice` — called on every scroll or slice change, creating GC pressure.

### PaintMenuController — Medium

- **Repeated `getFirstActiveDataSet()` calls**: called 2–3× per method (e.g. `ShowOutline`, `UpdateButtonHandler`) with no local caching.
- **Deep Find chains**: `this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow")...` — same fragility as `CanvassDesktop`.
- **FindObjectOfType**: `FindObjectOfType<VolumeInputController>()` in `OnEnable`.

### HistogramHelper — Medium (structural, not size)

- **Bidirectional coupling / logical cycle**: holds direct references to both `HistogramMenuController` and `CanvassDesktop` and calls `UpdateUI` on both. Creates a `CanvassDesktop → HistogramHelper → CanvassDesktop` cycle.
- **No interface/event boundary**: `UpdateUI` is called directly on concrete types — violates DIP. An `IHistogramDisplay` interface or event would eliminate the cycle.
- **Empty `Start()` and `Update()` stubs** — dead weight.

### HistogramMenuController — Low-Medium

- **`float.Parse` without error handling**: `float.Parse(minText.text)` in `_decreaseMinScale`, `UpdateButtonHandler`, etc. — throws if field is blank or non-numeric.
- **`getFirstActiveDataSet()` called multiple times per method**: in `UpdateButtonHandler` it's called 5× — could return `null` between calls if data changes mid-frame.

### VideoUiManager — Mostly Clean

- Cleanest class in the set. Event-driven design (`PlaybackUpdated`, `PlaybackFinished`), single clear responsibility.
- `_isPaused` field declared but never used — dead code.
- `ReloadVideoScriptFile` has a TODO for missing visual feedback — known gap, not a smell.

### SourceRow / TabsManager — Acceptable

- Both are small and focused.
- `SourceRow` uses `GetComponentInParent<CanvassDesktop>()` — tight parent-hierarchy dependency that would break under any restructuring of the scene.

---

## SOLID Violations Summary

| Principle | Violating Classes | Issue |
|---|---|---|
| **SRP** | CanvassDesktop, DesktopPaintController | Multiple responsibilities per class |
| **OCP** | DesktopPaintController | Axis switch repeated — new axis requires modifying 10+ methods |
| **DIP** | CanvassDesktop, DesktopPaintController, PaintMenuController, HistogramHelper | `FindObjectOfType`, concrete field references — no interfaces used |
| **ISP** | HistogramHelper → CanvassDesktop | `UpdateUI(float,float,Sprite)` is an implicit fat interface called from outside the class boundary |

No LSP violations found (no inheritance hierarchies in this slice).  
No circular package dependencies, but `HistogramHelper ↔ CanvassDesktop` is a logical reference cycle.

---

## Priority Fixes for After-State

1. **Split `CanvassDesktop`** into at minimum: `FileTabViewModel`, `StatsTabViewModel`, `SourcesTabViewModel`, `CanvassDesktopView` — eliminates the God Class and brings WMC, CBO, and LCOM within thresholds.
2. **Fix the `UpdateMaxValue` bug** in `DesktopPaintController` (line 306: sets `minVal` not `maxVal`).
3. **Replace axis switch chains** in `DesktopPaintController` with an `IAxisStrategy` or axis-indexed delegate table — closes the OCP violation.
4. **Break `HistogramHelper`'s cycle** via an `IHistogramDisplay` interface or event — both `CanvassDesktop` and `HistogramMenuController` subscribe, `HistogramHelper` publishes.
5. **Replace all `FindObjectOfType` calls** with constructor/field injection — required for any ViewModel unit test to run without Unity.
6. **Cache `transform.Find` results** in `Awake`/`Start` — currently re-traversed on every call, fragile and slow.
