# SciTools Understand — Static Analysis Report

_Code metrics, structural cross-reference, and standards (CodeCheck) analysis for a single C# source, modeled on SciTools Understand's metric and CodeCheck reporting._

| Field | Value |
|---|---|
| Target file | VolumeDataSetRenderer.cs |
| Repository | github.com/idia-astro/iDaVIE |
| Path | Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs |
| Language | C# (Unity / .NET) |
| Analysis date | 25 May 2026 |

## 1. Metrics Summary

| Understand metric | Abbrev. | Value |
|---|---|---|
| Lines (Count Line) | CountLine | 1403 |
| Lines of Code | CountLineCode | 1127 |
| Lines Blank | CountLineBlank | 166 |
| Lines Comment | CountLineComment | 110 |
| Ratio Comment to Code | RatioCommentToCode | 0.1 |
| Number of Methods/Functions (non-accessor) | CountDeclFunction | 44 |
| Count of Methods (WMC — all incl. properties, ctors, accessors) | CountMethod | 97 |
| Count of Instance Methods | CountMethodInstance | 97 |
| Count of Instance Variables | CountDeclInstanceVariable | 84 |
| Number of Classes/Types | CountDeclClass | 4 |
| Sum Cyclomatic Complexity | SumCyclomatic | 192 |
| Average Cyclomatic Complexity | AvgCyclomatic | 4.36 |
| Max Cyclomatic Complexity | MaxCyclomatic | 28 |
| Max Nesting | MaxNesting | 3 |

## 1b. CK Metrics (Chidamber–Kemerer Suite)

These are the confirmed CK values produced by the Understand tool for `VolumeDataSetRenderer`. All other files in the project (design document, metrics worksheet, worked examples) use these as the authoritative baseline.

| Understand metric | Abbrev. | Value | Brief target | Status |
|---|---|---|---|---|
| Count of Methods (WMC) | CountMethod | **97** | ≤ 20 | ❌ |
| Count of Instance Methods | CountMethodInstance | 97 | — | — |
| Count of Instance Variables | CountDeclInstanceVariable | 84 | — | — |
| Percent Lack of Cohesion (LCOM) | PercentLackOfCohesion | **95** (0.95) | ≤ 0.5 | ❌ |
| Max Inheritance Tree (DIT) | MaxInheritanceTree | 2 | ≤ 4 | ✅ |
| Count of Base Classes (IFANIN) | CountClassBase | 1 | — | — |
| Count of Coupled Classes (CBO) | CountClassCoupled | **28** | ≤ 14 | ❌ |
| Count of Derived Classes (NOC) | CountClassDerived | 0 | ≤ 5 | ✅ |
| Count of All Methods (RFC) | CountMethodAll | **97** | ≤ 50 | ❌ |

> **Note on WMC:** `CountDeclFunction = 44` (Section 1) counts only non-accessor function declarations. `CountMethod = 97` counts all methods including property accessors, constructors, and auto-generated members — this is the figure Understand uses for WMC in the CK suite, and is the value used throughout all project deliverables.

---

## 2. Function-Level Metrics

Per-method metrics ordered by Cyclomatic complexity. CodeCheck flags functions exceeding configurable complexity and length limits.

| Function | Cyclomatic | CountLineCode | CountParams | MaxNesting |
|---|---|---|---|---|
| `_startFunc` | 28 | 185 | 0 | 2 |
| `SaveMask` | 19 | 83 | 1 | 3 |
| `SetCursorPosition` | 17 | 54 | 2 | 3 |
| `Update` | 12 | 97 | 0 | 2 |
| `SetRegionPosition` | 8 | 28 | 2 | 3 |
| `UpdateRegionBounds` | 8 | 33 | 0 | 1 |
| `LoadRegionData` | 8 | 34 | 2 | 2 |
| `PaintMask` | 8 | 23 | 2 | 1 |
| `InitialiseMask` | 7 | 23 | 0 | 2 |
| `OnDestroy` | 7 | 9 | 0 | 0 |
| `SetVideoCursorLocPosition` | 5 | 26 | 1 | 2 |
| `RegenerateCubes` | 4 | 11 | 0 | 1 |
| `TeleportToRegion` | 4 | 8 | 0 | 1 |
| `ResetRestFrequency` | 4 | 13 | 0 | 1 |
| `PaintCursor` | 4 | 15 | 1 | 3 |
| `SaveSubCube` | 4 | 27 | 0 | 2 |

## 3. CodeCheck Standards Violations

| Check | Threshold | Violations |
|---|---|---|
| Cyclomatic complexity per function | > 15 | 3 |
| Function length (CountLineCode) | > 60 | 4 |
| Nesting depth | > 3 | 0 (3 at limit) |
| Parameters per function | > 5 | 0 |
| Comment-to-code ratio (file) | < 0.20 | 1 (file) |
| Avoid public data members | any | 14 |
| Line length | > 120 chars | 60 |

## 4. Structure & Cross-Reference

Understand builds a cross-reference database (the `.und` project) linking declarations, calls, and uses. For this file the dominant outbound dependencies are:

| Referenced entity | Use count | Kind |
|---|---|---|
| `_maskDataSet` | 92 | Field (instance) |
| `_featureManager` | 29 | Field (instance) |
| `_momentMapRenderer` | 14 | Field (instance) |
| `VolumeDataSet` | 8 | Type / object |
| `volumeInputController` | 4 | Type / object |
| `FeatureSetManager` | 3 | Type / object |
| `MeshRenderer` | 2 | Type / object |
| `VolumeInputController` | 2 | Type / object |

These relationships are navigable via Understand's Butterfly and Dependency graphs. The high reference count to a small number of collaborators indicates concentrated coupling.

## 5. Declared Dependencies (using directives)

The file imports 11 namespaces. External (non-System) dependencies indicate the modules this file is bound to:

| Namespace | Origin |
|---|---|
| System, System.IO, System.Linq, System.Collections(.Generic) | BCL |
| System.Text.RegularExpressions | BCL (regex) |
| UnityEngine, UnityEngine.UI | Unity engine |
| TMPro | TextMeshPro (3rd party) |
| DataFeatures | Project module |
| LineRenderer | Project module |

## 6. Observations

- **WMC = 97** (Count of Methods, CK suite): exceeds the ≤ 20 domain target by 4.9×. The 44 non-accessor functions (CountDeclFunction) carry a SumCyclomatic of 192; the remainder are property accessors and constructors counted by CountMethod.
- `SumCyclomatic` of 192 is concentrated: `_startFunc` alone accounts for 15% of total complexity, making it the primary maintenance risk.
- **CBO = 28** (Count of Coupled Classes): exceeds the ≤ 14 domain target by 2×. Dominant coupling targets are `_maskDataSet` (92 references) and `_featureManager` (29 references).
- **LCOM = 0.95** (95% Percent Lack of Cohesion): exceeds ≤ 0.5 target by 1.9×. Confirms four structurally disjoint field clusters (mask, texture, camera, foveation) coexisting in one class.
- **RFC = 97** (Count of All Methods): exceeds ≤ 50 target by 1.9×.
- **DIT = 2**: within target. Reflects `VolumeDataSetRenderer → MonoBehaviour → Behaviour`.
- `RatioCommentToCode` of 0.1 is below the common 0.20 guideline once the license header is excluded.
- Four functions exceed 60 code lines (`_startFunc`, `Update`, `SaveMask`, `SetCursorPosition`), each a decomposition candidate.
- 14 public data members reduce encapsulation; the 'avoid public data' CodeCheck would list each with its declaration cross-reference.
