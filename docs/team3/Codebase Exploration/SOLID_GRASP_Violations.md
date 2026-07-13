# SOLID / GRASP Violations — Evidence Catalogue
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Primary file analysed:** `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` (1,402 lines, 152 public members)  
**Supporting files:** `BasicVolume.cginc`, `FeatureSetRenderer.cs`, `VolumeDataSet.cs`

---

## How to Read This Document

Each violation entry contains:
- **What the principle says** — the rule being broken
- **What the code does** — the actual violation
- **Evidence** — exact file and line number
- **Impact** — why it matters
- **Fix** — what the refactoring proposal does about it

---

# SOLID Violations

---

## S — Single Responsibility Principle

> *A class should have only one reason to change.*

### Violation S1 — VolumeDataSetRenderer Has 9 Responsibilities (CRITICAL)

**What the principle says:** One class, one reason to change. If the class needs to change because of a UI update, a file format change, a shader change, or a VR input change — it has too many responsibilities.

**What the code does:** `VolumeDataSetRenderer` owns all of the following simultaneously:

| Responsibility | Evidence — Method | Line |
|---|---|---|
| FITS data loading | `_startFunc()` calls `VolumeDataSet.LoadDataFromFitsFile()` | 379 |
| GPU texture management | `GenerateDownsampledCube()`, `RegenerateCubes()` | 567, 580 |
| Shader uniform binding | `Update()` — 25+ `SetFloat`/`SetInt` calls | 1022–1109 |
| Mask painting | `PaintMask()`, `FinishBrushStroke()` | 1183, 1230 |
| Mask serialisation | `SaveMask()` — 90 lines, native plugin call, path logic | 1290 |
| 3D coordinate conversion | `ConvertWorldPositionToDataCubePosition()`, `GetVoxelPositionDataSpace()` | 616, 740 |
| Region / crop management | `CropToRegion()`, `ResetCrop()`, `UpdateRegionBounds()` | 909, 942, 832 |
| WCS / rest frequency | `PopulateRestFrequenyList()`, Update() WCS block | 549, 1113 |
| Unity lifecycle plumbing | `Start()`, `Update()`, `OnRenderObject()`, `OnDestroy()` | 353, 1022, 1142, 1390 |

**Impact:** Any change to file I/O, shader parameters, mask painting, coordinate maths, or VR comfort systems requires opening this one 1,402-line file. Every developer who touches it risks breaking unrelated functionality. This is the definition of high change-coupling.

**Fix:** Split into `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` (per brief) plus `MaskEditor`, `CropService`, `WCSService`, `MaskPersistenceService`.

---

### Violation S2 — SaveMask() Does Three Jobs in 90 Lines (HIGH)

**What the code does:** `SaveMask()` (line 1290) handles:
1. File path string manipulation and overwrite logic
2. Native plugin `IntPtr` calls (`FitsReader.FitsOpenFileReadOnly`)
3. UI toast notifications (`ToastNotification.ShowError`)

```csharp
// Line 1290 — file path logic inside a renderer method
string datasetFileName = _dataSet.FileName;
if (_dataSet.SelectedHdu != 1)
    datasetFileName += $"[{_dataSet.SelectedHdu}]";
FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, datasetFileName, out status); // native call
ToastNotification.ShowError("Could not find mask data!"); // UI in renderer
```

**Impact:** The renderer knows about the file system, the native ABI, and the UI notification system. Three separate concerns in one method.

**Fix:** `MaskPersistenceService.Save()` owns the native call and path logic. The renderer calls the service. The service fires an event that the UI observes.

---

### Violation S3 — SetCursorPosition() Does Four Jobs in 60 Lines (HIGH)

**What the code does:** `SetCursorPosition()` (line 639) handles:
1. World-to-voxel coordinate conversion
2. Voxel outline position update
3. Cursor value lookup from data
4. Mask source ID lookup and paint trigger

```csharp
// Line 639 — four concerns in one method
public void SetCursorPosition(Vector3 cursor, int brushSize)
{
    // 1. Coordinate conversion
    Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor);
    // 2. Voxel calculation
    Vector3Int newVoxelCursor = new Vector3Int(...);
    // 3. Data value lookup
    CursorValue = _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
    // 4. Outline update + paint trigger (further down)
}
```

**Fix:** `VolumeCoordinateService.WorldToVoxel()`, `VolumeCameraDriver.UpdateOutline()`, `MaskEditor.OnCursorMoved()`.

---

## O — Open/Closed Principle

> *A class should be open for extension but closed for modification.*

### Violation O1 — MaskMode Requires Modifying 4 Files to Add One New Mode (CRITICAL)

**What the principle says:** Adding new behaviour should not require changing existing, tested code.

**What the code does:** Adding a fifth mask mode (e.g. `Highlight`) requires:

1. **Edit** `VolumeDataSetRenderer.cs` line 47 — add enum value
2. **Edit** `VolumeDataSetRenderer.cs` lines 1072–1094 — add if/else branch  
3. **Edit** `BasicVolume.cginc` — add `#define` and shader branch (lines 281, 311, 343, 392, 404, 416)
4. **Edit** `PaintMenuController.cs` — add UI button for the new mode

```csharp
// BasicVolume.cginc lines 281–430 — 8 if/else branches for 4 modes × 2 projections
// The developer comment even acknowledges it:
// "TODO: this really needs to be improved using shader variants!"
if (MaskMode == MASK_DISABLED)      { ... }  // Line 281
else if (MaskMode == MASK_ENABLED)  { ... }  // Line 311
else if (MaskMode == MASK_ISOLATED) { ... }  // Line 343
// ... repeated again for AIP path lines 392–430
```

**Impact:** 4 files touched, 8 code blocks modified, for a single new mask mode. High regression risk each time.

**Fix:** `IMaskMode` Strategy pattern (our worked example). New mode = new class only. Zero existing files modified.

---

### Violation O2 — ProjectionMode Keyword Toggle Is Not Extensible (MEDIUM)

**What the code does:** `Update()` lines 1099–1103 hard-code the two projection modes as a binary keyword toggle:

```csharp
// Line 1099
if (ProjectionMode == ProjectionMode.AverageIntensityProjection)
    Shader.EnableKeyword("SHADER_AIP");
else
    Shader.DisableKeyword("SHADER_AIP");
```

Adding a third projection mode (e.g. `MinimumIntensityProjection`) requires editing this if/else and adding a new shader variant — modification of existing code, not extension.

**Fix:** `IProjectionMode` Strategy (same pattern as `IMaskMode`). Each mode applies its own keyword.

---

## L — Liskov Substitution Principle

### Violation L1 — No Violations Found (PASS)

`VolumeDataSetRenderer` inherits only from `MonoBehaviour` and introduces no domain inheritance hierarchy. LSP is not currently violated.

**Note for proposal:** LSP becomes relevant once `IMaskMode` and `IProjectionMode` are introduced. The worked example must demonstrate that all `IMaskMode` implementations are genuinely substitutable — any consumer of `IMaskMode` must work correctly with any implementation without knowing which one it has.

---

## I — Interface Segregation Principle

> *No client should be forced to depend on methods it does not use.*

### Violation I1 — No Interfaces Exist Anywhere in the Rendering Layer (CRITICAL)

**What the principle says:** Consumers should depend on narrow, focused interfaces — not fat concrete classes.

**What the code does:** `VolumeDataSetRenderer` has **152 public members**. Every class that needs to trigger a colour map change, a mask mode toggle, or a crop reset takes a full dependency on all 152 members:

```csharp
// FeatureSetRenderer.cs line 56 — gets the entire renderer
public VolumeDataSetRenderer VolumeRenderer { get; private set; }

// RenderingController.cs — direct field reference to full class
private VolumeDataSetRenderer _activeDataSet;

// PaintMenuController.cs — direct field reference
private VolumeDataSetRenderer _volumeRenderer;
```

`PaintMenuController` only needs `MaskMode` and `PaintMask()` — it is forced to depend on 150 other public members it never uses.

**ISP target (per brief):** Each interface must have ≤ 7 public members.

**Fix — proposed narrow interfaces:**

| Interface | Members | Consumers |
|---|---|---|
| `IVolumeRenderer` | `MaskMode`, `ProjectionMode`, `ColorMap`, `ThresholdMin`, `ThresholdMax` (5) | `RenderingController`, `QuickMenuController` |
| `IMaskEditor` | `PaintMask()`, `FinishBrushStroke()`, `SaveMask()`, `MaskMode` (4) | `PaintMenuController` |
| `ICursorProvider` | `CursorVoxel`, `CursorValue`, `CursorSource`, `SetCursorPosition()` (4) | `CanvassDesktop`, `FeatureSetRenderer` |
| `ICropService` | `CropToRegion()`, `CropToFeature()`, `ResetCrop()` (3) | `VolumeCommandController` |

---

## D — Dependency Inversion Principle

> *High-level modules should not depend on low-level modules. Both should depend on abstractions.*

### Violation D1 — Config.Instance Singleton (HIGH)

**What the code does:** `_startFunc()` reads configuration directly from a singleton:

```csharp
// Line 361
var config = Config.Instance;
TextureFilter = config.bilinearFiltering ? FilterMode.Bilinear : FilterMode.Point;
MaxSteps = config.maxRaymarchingSteps;
// ... 8 more direct field reads
```

And again at line 553:
```csharp
foreach (var emissionLine in Config.Instance.restFrequenciesGHz)
```

**Impact:** `VolumeDataSetRenderer` is hardwired to one specific configuration source. It cannot be tested with different configurations without modifying `Config.Instance` — a global state change that affects the entire application.

**Fix:** Inject `IAppConfig` via constructor or public property. The test can inject a mock config with predictable values.

---

### Violation D2 — FindObjectOfType for VolumeInputController (HIGH)

**What the code does:**
```csharp
// Line 381
volumeInputController = FindObjectOfType<VolumeInputController>();
// Line 522
_momentMapRenderer.momentMapMenuController = 
    FindObjectOfType<VolumeCommandController>().momentMapMenuController;
```

`FindObjectOfType` is a full scene graph search that returns a concrete type. It is removed in Unity 6 and prevents any form of injection or mocking.

**Fix:** Accept `IVolumeInputController` as an injected dependency. The concrete `VolumeInputController` is wired at the composition root (scene setup), not searched at runtime.

---

### Violation D3 — AddComponent Creates MomentMapRenderer at Runtime (HIGH)

**What the code does:**
```csharp
// Line 517
_momentMapRenderer = gameObject.AddComponent(typeof(MomentMapRenderer)) as MomentMapRenderer;
```

`VolumeDataSetRenderer` directly instantiates `MomentMapRenderer` — a concrete dependency created at runtime. This means:
- `MomentMapRenderer` cannot be replaced with a different implementation
- It cannot be mocked in tests
- The renderer depends on the concrete class, not an abstraction

**Fix:** Inject `IMomentMapRenderer`. The MonoBehaviour setup attaches and wires the component before the renderer needs it.

---

### Violation D4 — Domain Code Depends on UnityEngine Types (CRITICAL)

**What the code does:** Coordinate conversion methods use `UnityEngine.Vector3` and `UnityEngine.Quaternion` directly:

```csharp
// Line 616
public Vector3 ConvertWorldPositionToDataCubePosition(Vector3 worldLoc)
{
    return transform.InverseTransformPoint(worldLoc); // UnityEngine dependency
}

// Line 627
public Quaternion ConvertWorldRotationToDatacubeRotation(Quaternion worldRot)
{
    return transform.InverseTransformRotation(worldRot); // UnityEngine dependency
}
```

**Impact:** Per the brief's mandatory constraint — *"Domain code must not transitively depend on UnityEngine or SteamVR types."* These methods are domain logic (coordinate maths) but cannot be tested outside Unity because they depend on `transform` (a MonoBehaviour field).

**Fix:** `VolumeCoordinateService` takes the transform matrix as a `Matrix4x4` parameter (a pure value type). The coordinate maths becomes a pure function testable in edit mode.

---

# GRASP Violations

---

### GRASP 1 — Information Expert: VIOLATED

> *Assign responsibility to the class that has the information needed to fulfil it.*

**Violation:** `SetCursorPosition()` in `VolumeDataSetRenderer` (line 657) reads the voxel data value:

```csharp
// Line 657 — data value lookup inside the renderer
CursorValue = _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
```

The **expert** for voxel data values is `VolumeDataSet` — it holds the data. The renderer is reaching into the data set and pulling values out, rather than asking the expert to provide what it needs.

Similarly, `SourceStatsDict` (line 241) is a property on `VolumeDataSetRenderer` that simply delegates to `_maskDataSet.SourceStatsDict` — statistical information that belongs entirely with the data layer.

**Fix:** `VolumeDataSet` exposes `GetDataValue()` (it already does) but `VolumeCoordinateService` calls it directly and returns the result. The renderer receives the value, not the calculation.

---

### GRASP 2 — Creator: VIOLATED

> *Assign creation responsibility to the class that aggregates, contains, or closely uses the created object.*

**Violation:** `_startFunc()` directly constructs 5 unrelated objects:

```csharp
// Lines 430–468 — renderer creating unrelated objects
_cubeOutline            = new CuboidLine { ... };    // Line 430
_voxelOutline           = new CuboidLine { ... };    // Line 442
_videoCursorPositionOutline = new CuboidLine { ... };// Line 451
_regionOutline          = new CuboidLine { ... };    // Line 460
_measuringLine          = new PolyLine { ... };      // Line 468
// Plus:
_momentMapRenderer = gameObject.AddComponent(...)    // Line 517
_materialInstance  = Instantiate(RayMarchingMaterial)// Line 410
_maskMaterialInstance = Instantiate(MaskMaterial)   // Line 415
```

The renderer is acting as a factory for objects it does not truly own. A `CuboidLine` outline is a presentation-layer object owned by `VolumeCameraDriver`, not the renderer.

**Fix:** Dependency injection at composition root. Objects are constructed by factories or passed in as dependencies.

---

### GRASP 3 — Controller: VIOLATED

> *Assign the responsibility of handling system events to a class that represents the overall system or a use-case scenario.*

**Violation:** `VolumeDataSetRenderer` acts as both the **controller** (receiving user actions and coordinating responses) and the **domain model** (holding data state, performing calculations). There is no separation between use-case orchestration and domain logic.

For example, `CropToRegion()` (line 909):
1. Validates input (controller concern)
2. Loads subcube data (data concern)
3. Updates material textures (rendering concern)
4. Triggers outline update (presentation concern)

All four concerns are in one method.

**Fix:** `CropService` owns the use-case. It coordinates between `VolumeDataLoader`, `VolumeTextureManager`, and `VolumeCameraDriver` without any single class owning all concerns.

---

### GRASP 4 — Indirection: MISSING

> *Assign responsibility to an intermediate object to decouple two components.*

**Violation:** There is no abstraction layer between the Unity lifecycle and domain logic. Domain code calls Unity APIs directly:

```csharp
// Line 381 — domain setup calling Unity scene search
volumeInputController = FindObjectOfType<VolumeInputController>();

// Line 382 — domain setup searching scene hierarchy
_featureManager = GetComponentInChildren<FeatureSetManager>();

// Line 644 — coordinate logic reading Unity config singleton
if ((cursorInDataCube || Config.Instance.displayCursorInfoOutsideCube) && _dataSet != null)
```

**Fix:** The anti-corruption layer (`VolumeCameraDriver` thin Unity adapter, `VolumeMaterialBinder` shader anti-corruption layer) provides the indirection. Domain logic receives plain C# types, not Unity API results.

---

### GRASP 5 — Protected Variations: MISSING

> *Identify points of predicted variation and create a stable interface around them.*

**Violation:** Three known variation points have zero protection:

| Variation Point | Current State | Evidence |
|---|---|---|
| Render pipeline (Built-In → URP) | Zero abstraction — all `CGPROGRAM`, `DrawProceduralNow`, `OnRenderObject` directly in renderer | Lines 1142, 1148 |
| Input provider (SteamVR → Unity XR) | Zero abstraction — `FindObjectOfType<VolumeInputController>()` | Line 381 |
| Data format (FITS → future formats) | Zero abstraction — `LoadDataFromFitsFile` called directly | Line 379 |

When any of these variation points changes (and the Unity 6 migration guarantees the first one will), the change propagates through the entire renderer.

**Fix:** `VolumeMaterialBinder` anti-corruption layer for render pipeline. `IInputProvider` for input. `IVolumeDataSource` for data format.

---

### GRASP 6 — Low Coupling: VIOLATED

> *Assign responsibilities so that coupling remains low.*

**Violation:** `VolumeDataSetRenderer` directly references at least 12 concrete classes:

| Concrete Dependency | How Referenced | Line |
|---|---|---|
| `VolumeDataSet` | Direct field `_dataSet` | 226 |
| `VolumeInputController` | `FindObjectOfType<>()` | 381 |
| `FeatureSetManager` | `GetComponentInChildren<>()` | 382 |
| `MomentMapRenderer` | `AddComponent()` | 517 |
| `VolumeCommandController` | `FindObjectOfType<>()` | 522 |
| `Config` | `Config.Instance` singleton | 361, 553, 644 |
| `FitsReader` | Direct native plugin call | 1309 |
| `DataAnalysis` | Direct native plugin call | Various |
| `ToastNotification` | Direct static call | 1292 |
| `CuboidLine` | `new CuboidLine()` | 430–460 |
| `PolyLine` | `new PolyLine()` | 468 |
| `Feature` | Direct usage in feature methods | 886 |

**Estimated CBO: ~30–35** (acceptable range per brief: ≤14 domain / ≤25 orchestrators). **FAIL.**

**Fix:** Every dependency above becomes an interface. CBO per extracted module drops to ≤8.

---

### GRASP 7 — High Cohesion: VIOLATED

> *Assign responsibilities so that cohesion remains high — a class should do related things.*

**Violation:** The 9 responsibility clusters identified under S1 share almost no instance fields with each other. Methods in the mask painting cluster (`PaintMask`, `SaveMask`, `InitialiseMask`) share `_maskDataSet` and `_maskMaterialInstance`. Methods in the coordinate cluster (`ConvertWorldPosition`, `GetVoxelPosition`) share `transform` and `_dataSet`. These two clusters share zero fields — they are unrelated by any measure of cohesion.

**Estimated LCOM (Henderson-Sellers): ~0.85–0.95** (acceptable range: ≤0.5). **FAIL.**

**Fix:** Each extracted class is internally cohesive. `MaskEditor` methods all operate on `_maskDataSet` and `_maskBuffer`. `VolumeCoordinateService` methods all operate on the coordinate matrix. LCOM approaches 0 in each.

---

# Summary Table

| Principle | Violation | Severity | File | Lines | Fix |
|---|---|---|---|---|---|
| SRP | 9 responsibilities in one class | 🔴 Critical | `VolumeDataSetRenderer.cs` | 1–1402 | Split into 4+ classes |
| SRP | `SaveMask()` — 3 concerns in 90 lines | 🟠 High | `VolumeDataSetRenderer.cs` | 1290–1378 | `MaskPersistenceService` |
| SRP | `SetCursorPosition()` — 4 concerns in 60 lines | 🟠 High | `VolumeDataSetRenderer.cs` | 639–698 | 3 separate methods |
| OCP | MaskMode requires editing 4 files | 🔴 Critical | `VolumeDataSetRenderer.cs`, `BasicVolume.cginc` | 47, 1072, 281 | `IMaskMode` Strategy ✅ done |
| OCP | ProjectionMode binary toggle | 🟡 Medium | `VolumeDataSetRenderer.cs` | 1099–1103 | `IProjectionMode` Strategy |
| ISP | 152 public members, zero interfaces | 🔴 Critical | `VolumeDataSetRenderer.cs` | All | 4 narrow interfaces |
| DIP | `Config.Instance` singleton | 🟠 High | `VolumeDataSetRenderer.cs` | 361, 553, 644 | Inject `IAppConfig` |
| DIP | `FindObjectOfType` for dependencies | 🟠 High | `VolumeDataSetRenderer.cs` | 381, 522 | Constructor injection |
| DIP | `AddComponent` for `MomentMapRenderer` | 🟠 High | `VolumeDataSetRenderer.cs` | 517 | Inject `IMomentMapRenderer` |
| DIP | Domain code depends on `UnityEngine` types | 🔴 Critical | `VolumeDataSetRenderer.cs` | 616, 627 | `VolumeCoordinateService` |
| GRASP Information Expert | Data value lookup in renderer | 🟠 High | `VolumeDataSetRenderer.cs` | 657 | Move to data layer |
| GRASP Creator | Renderer constructs 8 unrelated objects | 🟠 High | `VolumeDataSetRenderer.cs` | 410–517 | DI / factories |
| GRASP Controller | Use-case + domain + rendering in one class | 🔴 Critical | `VolumeDataSetRenderer.cs` | 909 | `CropService` etc. |
| GRASP Indirection | No abstraction between Unity and domain | 🔴 Critical | `VolumeDataSetRenderer.cs` | 381, 644 | Anti-corruption layer |
| GRASP Protected Variations | No protection for 3 variation points | 🔴 Critical | All shader files + renderer | — | Interface seams |
| GRASP Low Coupling | CBO ~32, 12 concrete dependencies | 🔴 Critical | `VolumeDataSetRenderer.cs` | Multiple | Interface injection |
| GRASP High Cohesion | LCOM ~0.90, 9 unrelated clusters | 🔴 Critical | `VolumeDataSetRenderer.cs` | Multiple | Extract cohesive classes |

---

*Evidence catalogue compiled by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1, Day 3 — iDaVIE Refactoring Assignment 2026*
