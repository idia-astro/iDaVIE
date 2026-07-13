# VolumeDataSetRenderer — Class-Level Dependency Map
**Sub-team 3 — Rendering Engine — Sprint 1/2**  
**File:** `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs`  
**Class:** `VolumeDataSetRenderer : MonoBehaviour`

---

## Complete Dependency Map

Every class, struct, interface, and system that `VolumeDataSetRenderer` directly depends on, with exact evidence.

---

## Layer 1 — Data Layer Dependencies

These are the classes that own the actual scientific data. `VolumeDataSetRenderer` reaches into them constantly.

| Dependency | Type | How Referenced | Line(s) | Problem |
|---|---|---|---|---|
| `VolumeDataSet` | Concrete class | `_dataSet`, `_maskDataSet` fields; `LoadDataFromFitsFile()`, `GetDataSet()` | 220, 221, 379, 399, 593 | No interface — impossible to mock or swap |
| `DataAnalysis.SourceStats` | Concrete struct | `SourceStatsDict` property returns `Dictionary<int, DataAnalysis.SourceStats>` | 241 | Native plugin type leaking into renderer |
| `IntPtr AstFrame` | Native handle | `AstFrame` property exposes raw native pointer | 229 | WCS native handle exposed at wrong layer |

---

## Layer 2 — Native Plugin Dependencies

Direct calls into C/C++ DLLs. The renderer knows about the native ABI.

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `FitsReader.FitsOpenFileReadOnly()` | Direct static call | 1309, 1327 | Renderer calls native FITS API directly |
| `FitsReader.FitsOpenFileReadWrite()` | Direct static call | 1356 | File I/O inside renderer |
| `FitsReader.FitsCloseFile()` | Direct static call | 1368, 1371 | Native resource management in renderer |
| `IntPtr cubeFitsPtr` | Local variable | 1297 | Raw native pointer in renderer method |

---

## Layer 3 — Scene Query Dependencies

These are found at runtime by searching the scene — the pattern removed in Unity 6.

| Dependency | How Found | Line | Problem |
|---|---|---|---|
| `VolumeInputController` | `FindObjectOfType<>()` | 381 | Full scene search; removed in Unity 6 |
| `VolumeCommandController` | `FindObjectOfType<>()` | 522 | Full scene search; removed in Unity 6 |
| `FeatureSetManager` | `GetComponentInChildren<>()` | 382 | Hierarchy search; tight scene coupling |
| `MeshRenderer` | `GetComponent<>()` | 409 | Scene component search |
| `MomentMapRenderer` | `AddComponent()` | 517 | Runtime instantiation; untestable |

---

## Layer 4 — Configuration Dependencies

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `Config.Instance` | Global singleton access | 361, 553, 644 | Static global state; untestable |
| `Config.bilinearFiltering` | Direct field read | 361 | Renderer reads config directly |
| `Config.maxRaymarchingSteps` | Direct field read | 361 | Same |
| `Config.restFrequenciesGHz` | Direct field read | 553 | WCS config in renderer |
| `Config.displayCursorInfoOutsideCube` | Direct field read | 644 | UI config in renderer |

---

## Layer 5 — Feature / Domain Dependencies

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `FeatureSetManager` | `_featureManager` field | 206, 879, 882, 886, 890 | Renderer owns feature selection |
| `Feature` | `Feature.SetCubeColors()` static call | 438, 860 | Visual logic in wrong class |
| `Feature` | `feature.GetMinBounds()`, `GetMaxBounds()` | 882 | Domain query in renderer |

---

## Layer 6 — Rendering Support Dependencies

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `MomentMapRenderer` | `_momentMapRenderer` field | 211, 517, 522, 598 | Added at runtime; concrete dep |
| `MaterialID` struct | Used throughout `Update()` | 259–310, 1026–1109 | Owned inside same class |
| `ColorMapEnum` | `ColorMap` field | 101, 608 | Enum index used as shader integer |
| `ColorMapUtils` | `NumColorMaps`, `FromHashCode()` | 412, 605, 608 | Utility accessed directly |
| `MeshRenderer` | `_renderer` field | 208, 421 | Direct material assignment |

---

## Layer 7 — Visual / Geometry Dependencies

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `CuboidLine` | `_cubeOutline`, `_voxelOutline`, `_regionOutline`, `_videoCursorPositionOutline` | 204, 430–468 | Outline objects constructed inside renderer |
| `PolyLine` | `_measuringLine` | 203, 468 | Line renderer constructed inside renderer |

---

## Layer 8 — UI Dependencies

These are the most surprising — the renderer directly drives UI elements.

| Dependency | How Referenced | Line(s) | Problem |
|---|---|---|---|
| `TextMeshProUGUI loadText` | Public field; `.text = label` | 196, 562 | UI text driven from renderer |
| `Slider progressBar` | Public field; `.value = progress` | 197, 563 | UI slider driven from renderer |
| `ToastNotification` | Static method calls | 1292 | UI notifications from renderer |
| `updateStatus()` coroutine | Drives `loadText` and `progressBar` | 560–565 | UI update loop inside renderer |

---

## Layer 9 — Unity Engine Dependencies

| Dependency | How Used | Problem |
|---|---|---|
| `MonoBehaviour` | Base class | Acceptable — but lifecycle methods should be thin |
| `UnityEngine.Vector3` | Coordinate methods (line 616, 627) | Domain maths depends on Unity type |
| `UnityEngine.Quaternion` | Rotation methods (line 627) | Domain maths depends on Unity type |
| `UnityEngine.Matrix4x4` | Transform calculations | Acceptable value type |
| `UnityEngine.Material` | `_materialInstance`, `_maskMaterialInstance` | Should stay in `VolumeMaterialBinder` |
| `UnityEngine.Texture3D` | Via `VolumeDataSet.DataCube` | Should stay in `VolumeTextureManager` |
| `System.IO` | File path operations in `SaveMask()` | File I/O in renderer |
| `System.Text.RegularExpressions` | String parsing in `SaveMask()` | String ops in renderer |

---

## Full Dependency Diagram (ASCII)

```
                    ┌─────────────────────────────────────────┐
                    │         VolumeDataSetRenderer            │
                    │              (1,402 lines)               │
                    │            CBO estimated ~32             │
                    └──────────────────┬──────────────────────┘
                                       │
          ┌──────────┬─────────────────┼──────────────────┬────────────┐
          │          │                 │                  │            │
          ▼          ▼                 ▼                  ▼            ▼
   DATA LAYER    NATIVE PLUGINS   SCENE QUERIES      CONFIG        UI LAYER
   ───────────   ──────────────   ─────────────      ──────        ────────
   VolumeDataSet  FitsReader      VolumeInputCtrl    Config        loadText
   (concrete)     (static DLL)    (FindObjectOfType) .Instance     (TMP)
                                                     (singleton)
   DataAnalysis   DataAnalysis    VolumeCommandCtrl              progressBar
   .SourceStats   (static DLL)    (FindObjectOfType)             (Slider)

                                  FeatureSetManager              ToastNotification
                                  (GetComponentIn
                                   Children)

                                  MomentMapRenderer
                                  (AddComponent)

          ▼                   ▼                  ▼
   FEATURE DOMAIN        VISUAL/GEOMETRY     RENDERING SUPPORT
   ──────────────        ──────────────      ─────────────────
   FeatureSetManager     CuboidLine ×4       MeshRenderer
   Feature               PolyLine            Material ×2
   (static methods)                          ColorMapEnum
                                             ColorMapUtils
                                             MaterialID struct
                                             MomentMapRenderer
```

---

## Dependency Count Summary

| Category | Count | Acceptable (Brief) |
|---|---|---|
| Concrete class dependencies | 12 | — |
| Interface dependencies | 0 | All public APIs must use interfaces |
| Native plugin calls | 4 | Should be behind `IFitsReader` |
| Global singleton accesses | 3 | Should be injected `IAppConfig` |
| Scene query calls | 5 | All removed in Unity 6 |
| UI component references | 3 | Must not be in domain class |
| **Estimated CBO** | **~32** | **≤14 domain / ≤25 orchestrators** |

---

## After Refactoring — Target Dependency Map

Each extracted class has a narrow, interface-only dependency surface.

```
VolumeMaterialBinder ──► IMaskMode (interface)
                     ──► IVolumeDataSet (interface)
                     ──► IRenderPipeline (interface)
                     ──► MaterialID (own struct)
                     CBO target: ≤8

VolumeTextureManager ──► IVolumeDataSource (interface)
                     ──► IAppConfig (interface)
                     CBO target: ≤6

VolumeCameraDriver   ──► IVolumeDataSet (interface)
                     ──► IVignetteController (interface)
                     ──► VolumeCoordinateService (pure C#)
                     CBO target: ≤6

FoveatedSamplingPolicy ──► (nothing — pure C#)
                     CBO target: 0
```

---

## Key Insight for the Pitch

Every line in the dependency map above is a **reason the class might need to change**:

- FITS file format changes → renderer changes
- UI framework changes → renderer changes
- Config system changes → renderer changes
- Native plugin ABI changes → renderer changes
- Render pipeline changes → renderer changes

That is **5 independent reasons to change** for one class. The Single Responsibility Principle says there should be exactly **1**. The four-class split reduces each class to one reason to change.

---

*Compiled by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1/2 — iDaVIE Refactoring Assignment 2026*
