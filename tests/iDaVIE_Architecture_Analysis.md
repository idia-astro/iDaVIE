# iDaVIE — Cross-File Dependency & Architecture Analysis

_Whole-codebase static analysis of the C# source tree, centered on `VolumeDataSetRenderer.cs`._

| Field | Value |
|---|---|
| Scope | Assets/Scripts (entire C# source tree) |
| Commit | 1cd729f — "Add Cube to VRCamera Culling Mask (#473)" |
| Files analysed | 101 .cs files |
| Types declared | 200 |
| Parser | tree-sitter C# (full AST parse) |
| Analysis date | 25 May 2026 |

## 1. Method

The complete iDaVIE repository was cloned and every C# file under `Assets/Scripts` was parsed into a syntax tree. From the trees a project-wide symbol table (type → declaring file) was built, then capitalised identifier references in each file were resolved back to declared types to construct a directed file-to-file dependency graph. From that graph: coupling (Ca/Ce), instability, strongly-connected components (cycles), propagation cost, and package metrics. All figures are measured from source.

_Caveat: the resolver matches by type name, so project types that shadow engine/BCL names (the project declares its own `Color` and `Path` types) absorb some references that actually target framework types. This shadowing is itself reported as a finding._

## 2. Architecture Health Summary

| Indicator | Value | Reading |
|---|---|---|
| Files / types | 101 / 200 | — |
| Dependency edges (file→file) | 341 | Dense |
| Propagation cost | 39.8% | High — a change can reach ~40% of files |
| Largest dependency cycle | 46 files | Severe — ~46% of codebase entangled |
| Files inside any cycle | 50 of 101 | Half the codebase |
| Abstractness (interfaces/abstract) | 8 / 206 types (3.9%) | Very low — concrete-on-concrete |
| Parse errors | 0 | All files syntactically valid |

## 3. Dependency Cycles

A strongly-connected component (SCC) is a set of files that all transitively depend on each other. iDaVIE contains two cyclic components.

| Component | Files | Members (selected) |
|---|---|---|
| SCC 1 (core tangle) | 46 | FeatureData, Menu, UI, VideoMaker, Shapes, Tools, VolumeData files — incl. `VolumeDataSetRenderer`, `VolumeInputController`, `VolumeDataSet`, `Config`, `FeatureSetManager`, `CanvassDesktop` |
| SCC 2 (keyboard) | 4 | VRKeyboard: `Abstract_VR_button`, `Char_VR_button`, `KeyboardManager`, `VRKeyboard_Text` |

The 46-file SCC is the central architectural problem — it spans seven top-level modules, meaning those boundaries are crossed by cyclic dependencies. No file in the SCC can be unit-tested without the other 45.

## 4. Coupling Hubs

Files ranked by afferent coupling (Ca = how many other files depend on them).

| File | Ca (in) | Ce (out) | Instability I |
|---|---|---|---|
| `Debuggers/FitsReaderDebug.cs` | 29 | 0 | 0.0 |
| `VolumeData/VolumeDataSetRenderer.cs` | 28 | 17 | 0.38 |
| `VolumeData/VolumeInputController.cs` | 24 | 17 | 0.41 |
| `VideoMaker/Path.cs` | 17 | 1 | 0.06 |
| `Tools/ColorMapEnum.cs` | 14 | 0 | 0.0 |
| `VolumeData/Config.cs` | 14 | 5 | 0.26 |
| `FeatureData/Feature.cs` | 10 | 3 | 0.23 |
| `FeatureData/FeatureSetManager.cs` | 10 | 9 | 0.47 |
| `UI/ToastNotification.cs` | 10 | 2 | 0.17 |
| `FeatureData/FeatureSetRenderer.cs` | 8 | 10 | 0.56 |

- **`VolumeDataSetRenderer.cs`** is the 2nd most depended-upon file (Ca=28) while depending on 17 others (Ce=17), I=0.38.
- **`FitsReaderDebug.cs`** shows the highest raw Ca (29) but is inflated by the shadowed `Color` type; true weight is lower.
- **`CanvassDesktop.cs`** has the highest fan-out (Ce=21, I=0.78) — a god-controller smell.

## 5. Focus: VolumeDataSetRenderer.cs in the Graph

Depended on by 28 files across 8 packages; depends on 17 files across 8 packages.

### 5.1 Top dependents (who breaks if VDR changes)

| Depends on VDR | Weight |
|---|---|
| `VolumeData/VolumeCommandController.cs` | 22 |
| `UI/Menus/RenderingController.cs` | 14 |
| `Menu/QuickMenuController.cs` | 11 |
| `Menu/PaintMenuController.cs` | 10 |
| `UI/CanvassDesktop.cs` | 8 |
| `VolumeData/MomentMapRenderer.cs` | 8 |
| `VolumeData/VolumeInputController.cs` | 8 |
| `Editor/VolumeCommandControllerEditor.cs` | 6 |

### 5.2 Top dependencies (what VDR needs)

| VDR depends on | Weight |
|---|---|
| `Debuggers/FitsReaderDebug.cs` | 16 |
| `UI/ToastNotification.cs` | 10 |
| `VolumeData/VolumeDataSet.cs` | 8 |
| `LineRenderer/WorldSpaceLineRenderer.cs` | 7 |
| `Tools/ColorMapEnum.cs` | 5 |
| `VideoMaker/Path.cs` | 5 |
| `VolumeData/MomentMapRenderer.cs` | 4 |
| `PluginInterface/FitsReader.cs` | 4 |
| `FeatureData/FeatureSetManager.cs` | 3 |
| `VolumeData/Config.cs` | 3 |

VDR both consumes and is consumed by `VolumeInputController`, `MomentMapRenderer`, `VolumeCommandController`, and `Config` — these mutual references place it inside the 46-file cycle.

## 6. Package-Level Structure

Instability I = Ce/(Ce+Ca); Abstractness A = abstract+interface / total types; Distance from main sequence |D| = |A + I − 1| (0 ideal).

| Package | Files | Ca | Ce | I | A | |D| |
|---|---|---|---|---|---|---|
| UI | 19 | 5 | 7 | 0.58 | 0.04 | 0.38 |
| Menu | 13 | 5 | 7 | 0.58 | 0.00 | 0.42 |
| VideoMaker | 11 | 7 | 5 | 0.42 | 0.09 | 0.50 |
| FeatureData | 10 | 5 | 6 | 0.55 | 0.00 | 0.45 |
| VRKeyboard | 10 | 0 | 0 | 0.00 | 0.10 | 0.90 |
| CatalogData | 6 | 2 | 8 | 0.80 | 0.00 | 0.20 |
| Tools | 6 | 6 | 5 | 0.45 | 0.17 | 0.38 |
| VolumeData | 6 | 10 | 9 | 0.47 | 0.00 | 0.53 |
| Editor | 5 | 0 | 5 | 1.00 | 0.00 | 0.00 |
| PluginInterface | 4 | 5 | 0 | 0.00 | 0.07 | 0.93 |
| Shapes | 4 | 2 | 3 | 0.60 | 0.00 | 0.40 |
| VoiceCommands | 4 | 0 | 3 | 1.00 | 0.00 | 0.00 |
| Debuggers | 2 | 11 | 2 | 0.15 | 0.00 | 0.85 |
| LineRenderer | 1 | 3 | 1 | 0.25 | 0.00 | 0.75 |

- **VolumeData** sits in the zone of pain: Ca=10, A=0.00, |D|=0.53 — load-bearing yet with no abstractions to absorb change.
- **PluginInterface** (|D|=0.93) and **Debuggers** (|D|=0.85) are maximally stable but essentially concrete.
- **Project-wide abstractness is 3.9%** (8 of 206 types) — the biggest contributor to the cycle and propagation-cost numbers.

## 7. Cross-Package Cyclic Couplings

Module pairs that depend on each other in both directions — each a broken layering boundary.

| Module A ↔ Module B | A→B | B→A |
|---|---|---|
| CatalogData ↔ Tools | 11 | 1 |
| Debuggers ↔ VideoMaker | 6 | 4 |
| Debuggers ↔ VolumeData | 1 | 23 |
| FeatureData ↔ VolumeData | 19 | 21 |
| FeatureData ↔ Menu | 26 | 5 |
| Menu ↔ VolumeData | 107 | 23 |
| Menu ↔ UI | 31 | 137 |
| Menu ↔ VideoMaker | 23 | 1 |
| Menu ↔ Shapes | 2 | 1 |
| Shapes ↔ VolumeData | 4 | 4 |

Menu ↔ VolumeData, UI ↔ Menu, UI ↔ VolumeData, and FeatureData ↔ VolumeData are the heaviest, confirming UI, Menu, FeatureData and VolumeData form one inseparable knot.

## 8. Recommendations (Highest Leverage First)

1. Break the 46-file SCC at VolumeData by introducing interfaces (`IVolumeRenderer`, `IMaskProvider`, `IFeatureSource`) so Menu/UI/FeatureData depend on abstractions.
2. Invert the VDR ↔ VolumeInputController ↔ MomentMapRenderer ↔ VolumeCommandController mutual references using events or a mediator.
3. Decompose the two god-hubs: `VolumeDataSetRenderer` (extract mask handling) and `CanvassDesktop` (split per subsystem).
4. Sever the UI ↔ Menu and Menu ↔ VolumeData back-edges first — heaviest cross-package cycles.
5. Rename the shadowing `Color` and `Path` types (or fully namespace-qualify) to remove ambiguity with `UnityEngine.Color` / `System.IO.Path`.

