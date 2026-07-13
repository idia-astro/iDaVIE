# NDepend-Equivalent Baseline — Sub-team 6 (Desktop GUI & Client Shell)

**Date:** 2026-05-25  
**Sprint:** 2, Day 6  
**Author:** Sub-team 6 Quality Champion  
**Status:** Baseline (before refactoring)

---

## Methodology note

NDepend requires compiled .NET assemblies (`.dll` files). iDaVIE is a Unity 2021.3 project — no build output exists in the repository (the `Library/` folder is gitignored), and NDepend is not installed in the CI environment. This document derives the equivalent metrics from:

- **NDepend v2026.1.5 (CanvassDesktop)** — real metrics from NDepend analysis on 2026-05-26: `NbTypesUsed` (Ce), `NbTypesUsingMe` (Ca), `LCOMHS`, `NbMethodsAndConstructors`, `CyclomaticComplexity`, `DepthOfInheritance`, `NbChildren`. These replace all previously-derived figures for `CanvassDesktop`. Marked **[NDepend]**.
- **CK baseline figures** — Understand static analysis export (`SK_BNCH.md`), which provides WMC, DIT, NOC, CBO, RFC, and LCOM for the remaining 7 in-scope classes.
- **Afferent coupling (Ca)** — grep-based count of files in `Assets/Scripts/` that reference each class (excluding the class's own file). Replaced by NDepend `NbTypesUsingMe` for `CanvassDesktop`.
- **Source inspection** — `using` directives and `FindObjectOfType<>` patterns in each `.cs` file to identify namespace dependencies and circular runtime coupling.
- **NDepend formulas** — instability, abstractness, and distance from main sequence computed per the standard NDepend definitions.

Figures are marked **[derived]** where computed from source rather than produced by the tool directly. The CK figures are **[Understand export]** and are identical to those used in `SK_BNCH.md`. CanvassDesktop figures updated to **[NDepend]** where real data is available.

---

## 1. Classes in Scope

| Class | File | Role |
|---|---|---|
| `CanvassDesktop` | `Assets/Scripts/UI/CanvassDesktop.cs` (1 899 file lines / 836 NDepend LOC) | Orchestrator (O) |
| `DesktopPaintController` | `Assets/Scripts/UI/DesktopPaintController.cs` (1 558 lines) | Adapter (A) |
| `PaintMenuController` | `Assets/Scripts/Menu/PaintMenuController.cs` (371 lines) | Orchestrator (O) |
| `VideoUiManager` | `Assets/Scripts/VideoMaker/VideoUiManager.cs` (439 lines) | Adapter (A) |
| `HistogramMenuController` | `Assets/Scripts/Menu/HistogramMenuController.cs` (222 lines) | Adapter (A) |
| `HistogramHelper` | `Assets/Scripts/Menu/HistogramHelper.cs` (101 lines) | Domain helper (D) |
| `SourceRow` | `Assets/Scripts/Menu/SourceRow.cs` (61 lines) | Domain helper (D) |
| `TabsManager` | `Assets/Scripts/Menu/TabsManager.cs` (109 lines) | Domain helper (D) |

Total `.cs` files in `Assets/Scripts/`: **101**  
Interfaces defined anywhere in scope: **0**  
Abstract classes defined anywhere in scope: **0**

---

## 2. Instability / Abstractness / Distance from Main Sequence

NDepend computes these per-namespace or per-assembly. Applied here at class level for the in-scope slice.

**Definitions:**
- **Ce** (efferent coupling) = types this class depends on. `NbTypesUsed` from NDepend for `CanvassDesktop` [NDepend]; `CBO` from `SK_BNCH.md` for remaining 7 classes [Understand export]. Note: NDepend counts all referenced types including Unity/System namespaces, so Ce values are higher than Understand's project-scoped CBO.
- **Ca** (afferent coupling) = number of types referencing this class. `NbTypesUsingMe` from NDepend for `CanvassDesktop` [NDepend]; grep-based file count for remaining 7 classes [derived].
- **Instability I** = Ce / (Ca + Ce). Range 0 (stable) → 1 (unstable).
- **Abstractness A** = abstract types in this type / total types. All 8 classes are concrete `MonoBehaviour` subclasses with no abstract members. **A = 0.00** for all.
- **Distance from main sequence D** = |A + I − 1|. With A = 0: D = 1 − I. Ideal D = 0 (on the main sequence line). D near 1 indicates the "Zone of Pain" (stable + concrete) or "Zone of Uselessness" (abstract + unstable).

| Class | Ce (CBO) | Ca | I (instability) | A | D (distance) | Zone |
|---|:---:|:---:|:---:|:---:|:---:|---|
| `CanvassDesktop` | 117 | 4 | **0.97** | 0.00 | 0.03 | Highly unstable concrete — extreme Ce ⚠ [NDepend] |
| `DesktopPaintController` | 21 | 1 | **0.95** | 0.00 | 0.05 | On main sequence |
| `PaintMenuController` | 9 | 4 | **0.69** | 0.00 | 0.31 | Moderate — concrete, somewhat stable |
| `VideoUiManager` | 17 | 1 | **0.94** | 0.00 | 0.06 | On main sequence |
| `HistogramMenuController` | 12 | 1 | **0.92** | 0.00 | 0.08 | On main sequence |
| `HistogramHelper` | 13 | 2 | **0.87** | 0.00 | 0.13 | Acceptable |
| `SourceRow` | 3 | 1 | **0.75** | 0.00 | 0.25 | Moderate — small class |
| `TabsManager` | 4 | 1 | **0.80** | 0.00 | 0.20 | Acceptable |

**Reading:** All classes are concrete (A = 0) and mostly unstable (high I). On their own, high-I concrete classes are healthy leaf/adapter nodes. The architectural concern is not the instability value but the **magnitude of Ce** — `CanvassDesktop` (Ce = 117 [NDepend]) is an unstable class with an enormous outgoing fan-out, making it highly sensitive to changes anywhere in the system. The Ce figure is higher than the Understand CBO (47) because NDepend counts all referenced types including Unity and System namespaces; both measures agree that the fan-out is pathological.

**Propagation cost [derived]:** Direct afferent impact = Ca / 101 × 100%.

| Class | Direct afferent impact | Note |
|---|---|---|
| `CanvassDesktop` | 4 / 243 = **1.6%** | Direct fan-in lower than grep estimate; transitive impact substantially higher — dependents include central VolumeData classes [NDepend] |
| `PaintMenuController` | 4 / 101 = **4.0%** | 4 classes reference it directly |
| All others | ≤ 2.0% | Low direct fan-in |

`CanvassDesktop`'s transitive propagation cost is significantly higher than its 1.6% direct figure (4 of 243 types in Assembly-CSharp [NDepend]) because its dependents (`VolumeCommandController`, `VolumeDataSetRenderer`) are themselves widely referenced across the codebase.

---

## 3. Namespace / Assembly Dependency Map

`using` directives per class, categorised by layer:

| Class | `System.*` | `UnityEngine` | Third-party | Project namespaces |
|---|:---:|:---:|:---:|:---:|
| `CanvassDesktop` | ✓ (7 types) | ✓ + `UnityEngine.UI` | `SFB`, `TMPro`, `Valve.VR`, `VolumeData`, `DataFeatures` | `VolumeData`, `DataFeatures` |
| `DesktopPaintController` | ✓ | ✓ + `UnityEngine.UI` | `TMPro`, `Unity.Collections`, `Unity.Mathematics`, `VolumeData` | `VolumeData` |
| `PaintMenuController` | ✓ | ✓ + `UnityEngine.UI` | `SFB`, `JetBrains.Annotations`, `Unity.VisualScripting` | `VolumeData` |
| `VideoUiManager` | ✓ | ✓ + `UnityEngine.UI` | `TMPro` | `VolumeData` |
| `HistogramMenuController` | — | ✓ + `UnityEngine.UI` | `TMPro` | `VolumeData` |
| `HistogramHelper` | `System.IO` | ✓ | `OxyPlot` (3 namespaces) | — |
| `SourceRow` | `System.Collections.Generic` | ✓ | `TMPro` | — |
| `TabsManager` | — | ✓ + `UnityEngine.UI` | — | — |

**Key finding:** Every class in scope imports `UnityEngine`. None defines or implements a domain interface. This means:
- Domain logic (e.g. `IsLoadable()` in `CanvassDesktop`, `checkSubsetBounds()`) is entangled with Unity platform types.
- No class can be unit-tested without a Unity runtime.
- Section 4.2 #3 ("domain code must not transitively depend on `UnityEngine` or `SteamVR`") is violated by the entire slice in the current state.

---

## 4. Circular Dependency Analysis

NDepend's circular dependency detection is one of its primary value adds. Source inspection confirms **two cycles** involving classes in our scope:

### Cycle 1: `CanvassDesktop` ↔ `VolumeCommandController`

```
CanvassDesktop.cs (line ~Start()):
    _volumeCommandController = FindObjectOfType<VolumeCommandController>();

VolumeCommandController.cs:
    private CanvassDesktop canvassDesktop;
    canvassDesktop = FindObjectOfType<CanvassDesktop>();
```

Both classes hold a typed field reference to each other. This is a mutual runtime coupling masquerading as Unity's `FindObjectOfType<>` pattern — but it is a real type dependency; the field declaration `private CanvassDesktop canvassDesktop` in `VolumeCommandController.cs` creates a compile-time dependency.

**Severity:** Critical. This violates Section 4.2 #2 ("zero circular dependencies between top-level components") and defeats any attempt to test or extract either class in isolation.

### Cycle 2: `CanvassDesktop` ↔ `DesktopPaintController`

```
CanvassDesktop.cs:
    RegionCubeDisplay.GetComponent<DesktopPaintController>().StartPaintSelection();

DesktopPaintController.cs:
    private CanvassDesktop canvassDesktop;
    canvassDesktop = FindObjectOfType<CanvassDesktop>();
```

Same pattern. `DesktopPaintController` holds a typed `CanvassDesktop` field; `CanvassDesktop` calls `GetComponent<DesktopPaintController>()`. Mutual compile-time dependency.

**Severity:** Critical. Same violation as Cycle 1.

### Summary

| Cycle | Classes involved | Type | Severity |
|---|---|---|---|
| 1 | `CanvassDesktop` ↔ `VolumeCommandController` | Compile-time (typed fields) | 🔴 Critical |
| 2 | `CanvassDesktop` ↔ `DesktopPaintController` | Compile-time (typed fields + GetComponent) | 🔴 Critical |

Both cycles will be eliminated by the MVVM refactoring: `CanvassDesktop` is replaced by thin View adapters that depend only on ViewModel interfaces, and `VolumeCommandController` is replaced by the `IVolumeService` gateway. Neither ViewModel nor gateway holds a reference back to the View.

---

## 5. Architecture Rule Violations (CQLinq-Equivalent)

These are the rules NDepend would encode as CQLinq queries. Results are derived from `SK_BNCH.md` CK data and source inspection.

### Rule 1 — God Types (WMC threshold)

*Threshold: WMC > 40 for orchestrators/adapters, > 20 for domain helpers.*

| Class | WMC | Threshold | Violation |
|---|:---:|:---:|:---:|
| `CanvassDesktop` | 64 | 40 (O) | 🔴 +24 [NDepend] |
| `DesktopPaintController` | 57 | 40 (A) | 🔴 +17 |
| `PaintMenuController` | 24 | 40 (O) | ✅ |
| `VideoUiManager` | 17 | 40 (A) | ✅ |
| `HistogramMenuController` | 13 | 40 (A) | ✅ |
| `HistogramHelper` | 3 | 20 (D) | ✅ |
| `SourceRow` | 3 | 20 (D) | ✅ |
| `TabsManager` | 3 | 20 (D) | ✅ |

**Violations: 2**

### Rule 2 — High Efferent Coupling (CBO threshold)

*Threshold: CBO > 25 for orchestrators, > 14 for domain helpers.*

| Class | CBO | Threshold | Violation |
|---|:---:|:---:|:---:|
| `CanvassDesktop` | 117 | 25 (O) | 🔴 +92 [NDepend] |
| `DesktopPaintController` | 21 | 25 (A) | ✅ |
| `PaintMenuController` | 9 | 25 (O) | ✅ |
| `VideoUiManager` | 17 | 25 (A) | ✅ |
| `HistogramMenuController` | 12 | 25 (A) | ✅ |
| `HistogramHelper` | 13 | 14 (D) | ⚠ borderline |
| `SourceRow` | 3 | 14 (D) | ✅ |
| `TabsManager` | 4 | 14 (D) | ✅ |

**Violations: 1 critical + 1 borderline**

### Rule 3 — High Response For a Class (RFC threshold)

*Threshold: RFC > 50. Note: NDepend does not compute RFC directly. Understand RFC figures are retained below; NDepend Cyclomatic Complexity (CC) is added as an additional data point where available.*

| Class | RFC (Understand) | CC (NDepend) | Violation |
|---|:---:|:---:|:---:|
| `CanvassDesktop` | 118 | **298** | 🔴 RFC +68 / CC far exceeds any threshold [NDepend] |
| `DesktopPaintController` | 99 | — | 🔴 +49 |
| `VideoUiManager` | 64 | — | 🔴 +14 |
| `PaintMenuController` | 56 | — | 🔴 +6 |
| `HistogramMenuController` | 36 | — | ✅ |
| `HistogramHelper` | 23 | — | ✅ |
| `SourceRow` | 11 | — | ✅ |
| `TabsManager` | 7 | — | ✅ |

**Violations: 4**

### Rule 4 — Poor Cohesion (LCOM threshold)

*Threshold: LCOM (Henderson-Sellers) > 0.50.*

| Class | LCOM | Violation |
|---|:---:|:---:|
| `CanvassDesktop` | 0.97 | 🔴 [NDepend LCOMHS] |
| `DesktopPaintController` | 0.940 | 🔴 |
| `PaintMenuController` | 0.919 | 🔴 |
| `VideoUiManager` | 0.863 | 🔴 |
| `HistogramMenuController` | 0.812 | 🔴 |
| `HistogramHelper` | 0.667 | 🔴 |
| `SourceRow` | 0.667 | 🔴 |
| `TabsManager` | 0.467 | ✅ |

**Violations: 7 of 8** (only `TabsManager` is clean)

### Rule 5 — No Interface Backing for Public API Boundaries

*Every public API boundary must be expressed as an interface with at least one test double (Section 4.2 #4).*

Result: **0 interfaces exist** anywhere in the in-scope slice. All 8 classes expose concrete types only. Any caller must reference the concrete `MonoBehaviour`.

**Violations: 8 of 8**

### Rule 6 — Domain Code Depends on `UnityEngine` / `SteamVR`

*Domain code must not transitively depend on `UnityEngine` or `SteamVR` (Section 4.2 #3).*

All 8 classes import `UnityEngine`. `CanvassDesktop` additionally imports `Valve.VR`. This is expected in the current state (all classes are `MonoBehaviour` subclasses) and is the primary target of the MVVM split refactoring.

**Violations: 8 of 8 (all current-state; resolved in after-state ViewModel layer)**

### Rule 7 — Circular Dependencies (architectural non-negotiable)

As documented in Section 4 above.

**Violations: 2 cycles (both critical)**

### Rule 8 — Dead Fields (unreferenced private state)

| Class | Dead fields |
|---|---|
| `CanvassDesktop` | `_restFrequency`, `inPaintMode`, `_tabsManager` |
| `DesktopPaintController` | `firstEnable`, `colormapHeight`, `minZoom` |
| `PaintMenuController` | `cropstatus`, `featureStatus`, `oldSaveText`, `paintMenu`, `savePopup` |
| `VideoUiManager` | `_isPaused` |
| `HistogramMenuController` | `editMinScale`, `editMaxScale` |

**12 dead fields across 5 classes.**

---

## 6. Violation Summary

| Rule category | Violations | Severity |
|---|:---:|---|
| God Types (WMC) | 2 | 🔴 High |
| High CBO | 1 (+1 borderline) | 🔴 Critical |
| High RFC | 4 | 🔴 High |
| Poor Cohesion (LCOM) | 7 | 🔴 High |
| No interface backing | 8 | 🔴 Critical (arch. non-negotiable) |
| Domain → UnityEngine dependency | 8 | 🔴 Critical (arch. non-negotiable) |
| Circular dependencies | 2 cycles | 🔴 Critical (arch. non-negotiable) |
| Dead fields | 12 fields, 5 classes | 🟡 Medium |
| **Total** | **34** | |

The three architectural non-negotiables (Sections 4.2 #2, #3, #4) all have violations in the current state. The two confirmed dependency cycles are the most urgent findings because they prevent incremental extraction — any attempt to move `CanvassDesktop` logic to a ViewModel will be blocked by the back-reference from `VolumeCommandController` and `DesktopPaintController`.

---

## 7. Implications for Refactoring Target

The circular dependencies (Cycles 1 and 2) must be broken **before** any MVVM extraction can succeed cleanly:

1. **Break Cycle 1** — introduce `ICanvassDesktopEvents` (or equivalent notification interface) so `VolumeCommandController` subscribes to events rather than holding a typed back-reference. Alternatively, move the callback logic into a mediator.
2. **Break Cycle 2** — `DesktopPaintController` should receive paint-mode state via an injected `IPaintModeService` rather than fetching `CanvassDesktop` via `FindObjectOfType`.

Once cycles are broken, the MVVM decomposition described in `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md` and `adrs/0001-mvvm-split.md` proceeds with no further cyclic risk.

**Projected Day 13 snapshot** (after MVVM split) — see `docs/sub-team-6/metrics/projection.md` (to be created, feeds T2 and T4).

---

## 8. Traceability

| Artefact | Path |
|---|---|
| CK baseline (Understand) | `docs/sub-team-6/deliverables/other/D9-ck-baseline/SK_BNCH.md` |
| SonarQube smells baseline | `docs/sub-team-6/deliverables/other/D9-ck-baseline/SonarQube Baseline report.md` |
| MVVM refactoring proposal | `docs/sub-team-6/deliverables/D3-MVVM-binding-policy/mvvm-binding-policy.md` |
| ADR — MVVM split | `docs/sub-team-6/adrs/0001-mvvm-split.md` |
| Before-state DSM (File tab) | `docs/sub-team-6/deliverables/D4-worked-examples/ex1-file-tab/before-dsm.md` |
| Deliverables checklist (D9, T2) | `docs/sub-team-6/deliverables/deliverables-checklist.md` |
| Feeds | T2 (benchmark report), T4 (consolidated report ch. metrics), T7 (integration & metrics) |
