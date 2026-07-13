# BNCH-1 — Day 2 CK Baseline Metrics Snapshot

**Measurement Date:** May 23, 2026 (Day 2)  
Static analysis performed using Understand 6.1 on eight target classes: `CanvassDesktop`, `DesktopPaintController`, `PaintMenuController`, `VideoUiManager`, `HistogramMenuController`, `HistogramHelper`, `SourceRow`, `TabsManager`.  
Source commit: `7bade5c` (commit before any refactoring commenced).

---

## Methodology

CK metrics (Chidamber & Kemerer) are measured per ISO/IEC 20246 interpretation via Understand. All six metrics are computed at class level, with hand-verification for method counts and approximate call counts estimated from AST traversal and tool help.

**Metric definitions:**

- **WMC (Weighted Methods per Class):**  
  Understand method count (all declared methods, uniform weight 1). Also confirms the method-count basis for Henderson-Sellers LCOM.

- **DIT (Depth of Inheritance Tree):**  
  1 for a direct `System.Object` child, 4 for `MonoBehaviour` (Object → MarshalByRefObject → MonoBehaviour chain). No multiple inheritance (C# limitation).

- **NOC (Number of Children):**  
  Count of direct subclass declarations in this eight-class slice (expected 0 for all, as no inheritance hierarchy exists within the slice).

- **CBO (Coupling Between Objects):**  
  Number of distinct named types referenced by the class, excluding primitive types (`int`, `string`, `bool`) and collections (`List<>`, `T[]`). Reflects static type graph only; runtime `FindObjectOfType<>` calls are tracked separately.

- **RFC (Response For Class):**  
  Method count plus distinct external public method calls directly reachable from the class’s public API. Conservative count (does not include transitive expansion).

- **LCOM (Lack of Cohesion of Methods):**  
  Henderson-Sellers LCOM-HS = \(1 - \Sigma(\mu(A_j)) / (m \times n)\), where `m` = method count, `n` = field count, and `μ(Aj)` is the average number of method pairs sharing field `j`. Range [0, 1]; 0 = perfectly cohesive, 1 = zero cohesion. Reported only for classes with ≥ 3 methods.

---

## Threshold Targets (Assignment Spec §7.1)

| Role              | WMC  | CBO  | RFC  | LCOM  |
|-------------------|------|------|------|-------|
| Domain / ViewModel| ≤ 20 | ≤ 14 | ≤ 50 | ≤ 0.50 |
| Orchestrator      | ≤ 40 | ≤ 25 | ≤ 50 | ≤ 0.50 |

All eight classes are treated as **Orchestrators** because they act as tab facades or UI adapters within the monolithic `CanvassDesktop`. None are pure domain or ViewModel types (those will be introduced post-refactor).

---

## Day 2 CK Metrics — Eight Target Classes

| Class                   | Role         | WMC | DIT | NOC | CBO | RFC | LCOM-HS | WMC ok? | CBO ok? | RFC ok? | LCOM ok? | Status     |
|-------------------------|-------------|-----|-----|-----|-----|-----|---------|---------|---------|---------|----------|------------|
| CanvassDesktop          | Orchestrator| 63  | 1   | 0   | 47  | 118 | 0.955   | 🔴 +23  | 🔴 +22  | 🔴 +68  | 🔴       | CRITICAL   |
| DesktopPaintController  | Adapter     | 29  | 1   | 0   | 18  | 64  | 0.834   | 🔴 +0*  | 🟡 +0*  | 🔴 +14  | 🔴       | VIOLATION  |
| PaintMenuController     | Adapter     | 22  | 1   | 0   | 12  | 48  | 0.712   | 🔴 +2   | 🟢 ✅   | 🟢 ✅   | 🔴       | VIOLATION  |
| VideoUiManager          | Adapter     | 18  | 1   | 0   | 8   | 35  | 0.445   | ✅      | ✅      | ✅      | ✅       | PASS       |
| HistogramMenuController | Adapter     | 17  | 1   | 0   | 6   | 38  | 0.489   | ✅      | ✅      | ✅      | ✅       | PASS       |
| HistogramHelper         | Helper      | 14  | 1   | 0   | 11  | 32  | 0.623   | ✅      | ✅      | ✅      | 🟡       | WARNING    |
| SourceRow               | Helper      | 12  | 1   | 0   | 9   | 28  | 0.534   | ✅      | ✅      | ✅      | ✅       | PASS       |
| TabsManager             | Helper      | 11  | 1   | 0   | 7   | 26  | 0.478   | ✅      | ✅      | ✅      | ✅       | PASS       |

`DesktopPaintController` WMC = 29 is 1 below the 30 “safety margin” within the Orchestrator role (≤ 40). CBO = 18 is within the Orchestrator threshold (≤ 25). It is flagged because RFC = 64 is +14 over the limit, and LCOM = 0.834 is high, indicating tight temporal coupling within the class.

---

## Summary by Threshold Violation

**Rule violations:**

| Rule          | Violations | Classes exceeding threshold |
|---------------|-----------:|-----------------------------|
| WMC ≥ 21      | 3          | `CanvassDesktop` (+23), `DesktopPaintController` (+0 but borderline), `PaintMenuController` (+2) |
| CBO ≥ 15      | 2          | `CanvassDesktop` (+22), `DesktopPaintController` (18; marginal) |
| RFC ≥ 51      | 2          | `CanvassDesktop` (+68), `DesktopPaintController` (+14) |
| LCOM ≥ 0.51   | 4          | `CanvassDesktop` (0.955), `DesktopPaintController` (0.834), `PaintMenuController` (0.712), `HistogramHelper` (0.623) |

In aggregate, four types carry critical status; refactoring is non-negotiable.

---

## Circular Dependencies (Non-Negotiable Violations per §4.2)

Two cycles are present in the eight-class slice:

**Cycle 1: `CanvassDesktop` ↔ `DesktopPaintController`**

- `CanvassDesktop` holds public field `_paintController : DesktopPaintController` (initialized in `Awake`).
- `DesktopPaintController` holds private field `canvassDesktop : CanvassDesktop` (set via constructor parameter or `GetComponent<>`).

**Impact:** Neither class can be unit-tested in isolation; compilation and refactoring of one forces recompilation of both.

**Cycle 2: `CanvassDesktop` ↔ `HistogramHelper` ↔ `HistogramMenuController`**

- `CanvassDesktop` field: `private HistogramHelper _histogramHelper`.
- `HistogramHelper` field: `public CanvassDesktop canvassDesktop` (bidirectional reference).
- `HistogramMenuController` calls `FindObjectOfType()` → implicit dependency on `CanvassDesktop` without a static type declaration.

**Impact:** `HistogramHelper` and `HistogramMenuController` cannot be moved cleanly to separate assemblies or test harnesses.

---

## CBO Breakdown — CanvassDesktop (God Class Exemplar)

The 47 distinct types represent tight coupling across five architectural concerns (file I/O, UI rendering, stats computation, debug logging, scene management):

| Category                    | Type Count | Representative types |
|----------------------------|-----------:|----------------------|
| File I/O & Headers         | 9          | `FitsReader`, `IntPtr`, `StringBuilder`, `SystemInfo`, `LibraryLoadUtils`, `FeatureMapping`, `FeatureSetManager`, `DataAnalysis` (native FITS wrappers & P/Invoke handles) |
| UI Rendering & Display     | 13         | `TMP_Dropdown`, `TMP_InputField`, `Toggle`, `Slider`, `Button`, `Sprite`, `TextMeshProUGUI`, `GameObject`, `Transform`, `Coroutine`, `Image`, `RawImage`, `LayoutElement` |
| Volume & Command Controller| 4          | `VolumeDataSetRenderer`, `VolumeCommandController`, `VolumeInputController`, `VolumePlayer` |
| Menu & Dialogs             | 5          | `DesktopPaintController`, `PaintMenuController`, `HistogramHelper`, `MenuBarBehaviour`, `StandaloneFileBrowser` |
| Stats & Rendering Config   | 6          | `ColorMapUtils`, `Config`, `OxyPlot.Plot`, `OxyPlot.Series` (histogram graphs), `FeatureTable`, `SourceMappingOptions` |
| VR & Input                 | 4          | `Valve.VR.SteamVR_Init`, `Valve.VR.OpenVR`, `Valve.VR.CVRRenderModels`, `Valve.VR.CVRCompositor` |
| Utility & System           | 2          | `PlayerPrefs`, `Debug`, `Application` |
| Other BCL & System         | 4          | `Exception`, `IEnumerator`, `IDisposable`, `System` (nested types) |

Unity built-ins are counted as one aggregate (the `UnityEngine` namespace group) by Understand; the 47 types above are distinct top-level class types only.

---

## RFC Breakdown — CanvassDesktop Method Call Sites

RFC ≈ 63 methods + ~55 distinct external method calls = ~118 total response elements. Major external dependencies:

| Method Category             | Count | Examples |
|-----------------------------|------:|----------|
| `FitsReader` P/Invoke       | 9     | `FitsReader.OpenImageFile()`, `.ReadPrimaryHdu()`, `.ReadExtensionHdu()`, etc. |
| `VolumeDataSetRenderer` writes | 5 | `renderer.ColorMap`, `.Threshold`, `.MaxValue` accessors |
| UI control updates          | 18    | `_tmpDropdown.SetValueWithoutNotify()`, `_toggle.isOn = ...`, `_slider.value = ...` |
| `VolumeCommandController`   | 4     | `_volCmd.SetFileIndex()`, `.UpdateRenderParams()`, `.Load()`, `.Unload()` |
| `VolumeInputController`     | 3     | `_volInput.GetMagnify()`, `.GetPan()`, `.GetRotation()` |
| Menu controller delegation  | 6     | `_paintMenu.SelectTab()`, `_renderingMenu.Show()`, etc. |
| `FindObjectOfType<>` hunts  | 3     | `FindObjectOfType<>()` calls (runtime-only dependencies) |
| Helper method calls         | 7     | `HistogramHelper.Populate()`, `ColorMapUtils.MapValue()`, `Config.GetValue()`, etc. |

---

## LCOM Analysis — CanvassDesktop (High Coupling, Low Cohesion)

`CanvassDesktop` has WMC = 63 and ≈ 67 fields (35 public, 32 private). LCOM-HS = 0.955 indicates that most methods operate on disjoint subsets of fields. Principal field clusters:

- **Field group 1 (File tab):**  
  `_volumeDataSetRenderers`, `_selectedHduIndex`, `_imagePath`, `_maskPath`, `_subsetXMin/Max`, etc. (16 methods access these exclusively; 47 methods never touch them).

- **Field group 2 (Rendering tab):**  
  `_colorMap`, `_threshold`, `_maxPercentile`, etc. (14 methods use only this group).

- **Field group 3 (Stats tab):**  
  `_statScale`, `_restFrequency`, `_sigma`, etc. (8 methods access this group).

- **Field group 4 (Debug/logging):**  
  Uses the static `Debug` class only (40 `Debug.Log` call sites, no persistent fields).

- **Singleton/transient collaborators:**  
  `_paintController`, `_histogramHelper`, `_tabsManager` (shared across method clusters, but not tied to a single cohesive concern).

Design smell: each UI tab is effectively implemented as a separate method cluster inside a single class, with minimal field-level overlap. Post-refactor, extracting `FileTabViewModel`, `RenderingTabViewModel`, etc. should reduce LCOM-HS to ≈ 0.1–0.2 per class.

---

## Comparison to Historical / Industry Benchmarks

| Metric | CanvassDesktop | Typical domain class | Typical legacy class | Typical average C# class | Assessment |
|--------|----------------|----------------------|----------------------|--------------------------|------------|
| WMC    | 63             | ≤ 12                 | 20–30                | ≤ 7                      | +53 over typical → god class |
| CBO    | 47             | ≤ 5                  | 8–12                 | ≤ 4                      | +43 over typical → high fan-out |
| RFC    | 118            | ≤ 15                 | 25–35                | ≤ 12                     | +106 over typical → extreme complexity |
| LCOM   | 0.955          | ≥ 0.5 problematic    | ≤ 0.3 good           | ≤ 0.1 excellent          | +0.855 over “good” → zero cohesion |

---

## Non-Violating Classes (4 of 8 — Baseline Healthy)

`VideoUiManager`, `HistogramMenuController`, `SourceRow`, and `TabsManager` all sit within the WMC/CBO/RFC/LCOM thresholds. They demonstrate that pure adapter/helper patterns are viable. Post-refactor, responsibilities from `CanvassDesktop` and `DesktopPaintController` will be extracted into a structure similar to these passing classes.

---

## Traceability

| Item                                          | Reference                                                                 |
|----------------------------------------------|---------------------------------------------------------------------------|
| Full DSM (all 8 classes + propagation cost)  | `BNCH-4.md` (companion report)                                           |
| Mocking-difficulty count (testability signals)| `BNCH-6.md` (companion report)                                          |
| Worked example: File Tab CK deltas           | `docs/sub-team-6/deliverables/D4-worked-examples/metrics.md` §2 (WE1)    |
| Worked example: Debug Tab CK deltas          | `docs/sub-team-6/deliverables/D4-worked-examples/metrics.md` §3 (WE2)    |
| Target: Day 13 projected metrics             | `BNCH-5.md` (to be re-measured post sprint 2)                            |
| Feeds T4 consolidated architecture report    | Metrics chapter will cite Day 2 vs Day 13 deltas as refactoring evidence |
| Feeds NFR-MOF-2                              | Modularity & fan-out reduction target (≥ 30% CBO reduction across slice) |
| Assignment spec thresholds                   | §7.1 WMC/CBO/RFC/LCOM targets per role                                   |

---

_Report prepared by Sub-team 6 Quality Champion · BNCH-1 — iDaVIE Day 2 Baseline · 2026-05-23_
