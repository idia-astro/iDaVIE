# DaVIE Refactoring - Rendering Engine Sub-team | VolumeDataSetRenderer.cs Annotation
* [cite_start]**Team:** Rendering Engine Sub-team (Team Alpha, Sub-team 3) [cite: 1, 3]
* [cite_start]**Assignment:** iDaVIE Refactoring Assignment [cite: 5]
* [cite_start]**Timeline:** Sprint 1, 21 May 2026 [cite: 5]
* [cite_start]**Deliverable:** Annotated Code Review [cite: 4]

---

## 1. File Overview

| Property | Value |
| :--- | :--- |
| **File path** | [cite_start]`Assets/Scripts/Volume Data/Volume DataSetRenderer.cs` [cite: 7] |
| **Lines of code** | [cite_start]1,402 [cite: 7] |
| **Class** | [cite_start]`VolumeDataSetRenderer: MonoBehaviour` [cite: 7] |
| **Namespace** | [cite_start]`VolumeData` [cite: 7] |
| **Public methods** | [cite_start]~32 [cite: 7] |
| **Private methods** | [cite_start]~8 [cite: 7] |
| **Public fields / properties** | [cite_start]~60+ [cite: 7] |
| **Unity lifecycle methods** | [cite_start]`Start()`, `Update()`, `OnRenderObject()`, `OnDestroy()` [cite: 7] |
| **Design pattern (current)** | [cite_start]None monolithic God Class [cite: 7] |
| **Known concerns (brief)** | [cite_start]God Class, mixed concerns, tight Unity coupling, no interfaces [cite: 7] |

[cite_start]`VolumeDataSetRenderer` is the largest and most complex class in the iDaVIE codebase[cite: 8]. [cite_start]At 1,402 lines, it violates every CK threshold relevant to the Rendering Engine work package[cite: 7, 9]. [cite_start]It acts simultaneously as a data loader, a texture manager, a material binder, a mask editor, a camera coordinate converter, a crop/region manager, a feature selector, a subcube exporter, and a Unity `MonoBehaviour` lifecycle host[cite: 10]. 

[cite_start]This annotation maps every method to a responsibility and flags all SOLID/GRASP violations that the refactoring proposal must resolve[cite: 11].

---

## 2. Method Responsibility Map

[cite_start]Each method below is classified by its primary responsibility and the target class it should migrate to in the refactored architecture[cite: 13].

### 2.1 Unity Lifecycle Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `Start()` | 353 | [cite_start]Bootstraps the coroutine; sets `started=false`[cite: 15]. | [cite_start]SRP: Lifecycle mixed with initialization logic[cite: 15]. | [cite_start]Keep thin Unity adapter[cite: 15]. |
| `startFunc()` <br>*(IEnumerator)* | 358 | [cite_start]God-method: loads FITS data, sets texture, wires materials, initializes outlines, feature manager, WCS, rest frequency, moment map renderer, crop (~190 lines)[cite: 15, 18]. | [cite_start]**SRP:** (Does everything)[cite: 15]. [cite_start]<br>**OCP:** (Not extensible)[cite: 15]. [cite_start]<br>**DIP:** (Depends on concrete types throughout)[cite: 18]. | [cite_start]Split across `VolumeDataLoader`, `VolumeTextureManager`, `VolumeMaterialBinder`, and `FeatureSetManager`[cite: 15, 18]. |
| `Update()` | 1022 | [cite_start]Pushes ~25 shader uniform values every frame; handles mask voxel offsets; manages WCS rest frequency change; controls projection mode logic[cite: 18]. | [cite_start]**SRP:** Shader state, mask math, WCS, all in one loop[cite: 18]. | [cite_start]`VolumeMaterialBinder.SyncShaderState()`[cite: 18]. |
| `OnRenderObject()` | 1142 | [cite_start]Draws mask point geometry via `Graphics.DrawProceduralNow` for two mask buffers[cite: 18]. | [cite_start]**SRP:** Rendering concern leaking into lifecycle[cite: 18]. | [cite_start]`VolumeMaskRenderer.Render()`[cite: 18]. |
| `OnDestroy()` | 1390 | [cite_start]Destroys material instances[cite: 18]. | [cite_start]Acceptable cleanup[cite: 18]. | [cite_start]Keep in thin adapter[cite: 18]. |

### 2.2 Data Loading Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `_startFunc()` *(partial)* | 358-548 | Calls `VolumeDataSet.LoadDataFromFitsFile()` directly and stores result. [cite_start]Configures Config settings inline[cite: 20]. | [cite_start]**DIP:** Depends on concrete `VolumeDataSet`, not an interface[cite: 20]. [cite_start]<br>**SRP:** Config reading mixed with loading[cite: 20]. | [cite_start]`VolumeDataLoader` wrapping `IVolumeDataSet`[cite: 20]. |
| `GenerateDownsampledCube()` | 567 | Calls `FindDownsampleFactors` and `GenerateVolumeTexture`. [cite_start]Decides downsample factors[cite: 20]. | [cite_start]**SRP:** Downsample policy mixed with renderer[cite: 20]. | [cite_start]`VolumeTextureManager`[cite: 20]. |
| `RegenerateCubes()` | 580 | [cite_start]Re-runs downsampling and conditional crop after mode changes[cite: 20]. | [cite_start]**SRP:** Orchestration concern[cite: 20]. | [cite_start]`VolumeTextureManager` service[cite: 20]. |
| `GetDataSet()` | 593 | [cite_start]Returns `_dataSet` reference[cite: 20]. | **ISP:** Exposes entire `VolumeDataSet`[cite: 20]. | Return typed read-only `IVolumeDataSet`[cite: 20]. |
| `LoadRegionData()` | 975 | [cite_start]Loads a subcube region from native plugin into a new `VolumeDataSet`[cite: 20]. | [cite_start]**SRP, DIP:** Direct native coupling inside renderer[cite: 20]. | [cite_start]`VolumeDataLoader`[cite: 20]. |

### 2.3 Material / Shader Binding Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `Update()` *(shader block)* | 1022-1120 | [cite_start]Sets 20+ shader uniforms per frame including thresholds, color map, foveation params, mask mode, vignette, projection keyword[cite: 24]. | [cite_start]**SRP:** Shader state management belongs elsewhere; not unit testable[cite: 24]. | [cite_start]`VolumeMaterialBinder.SyncShaderState()`[cite: 24]. |
| `ShiftColorMap()` | 603 | [cite_start]Cycles color map enum and updates material directly[cite: 24]. | [cite_start]**SRP:** UI action mixed with shader state[cite: 24]. | [cite_start]`VolumeMaterialBinder.SetColorMap()`[cite: 24]. |
| `ResetThresholds()` | 1136 | [cite_start]Resets `ThresholdMin/Max` to initial values[cite: 24]. | [cite_start]Minor SRP leak[cite: 24]. | [cite_start]`VolumeMaterialBinder` or `RenderingState`[cite: 24]. |
| `InitialiseMask()` | 1158 | [cite_start]Creates mask `VolumeDataSet`, generates texture, wires material texture slot[cite: 24]. | [cite_start]**SRP:** Mask initialization mixes loading and material binding[cite: 24]. | [cite_start]Split: `VolumeDataLoader.LoadMask()` + `VolumeMaterialBinder.BindMask()`[cite: 24]. |

### 2.4 Mask Painting Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `PaintMask()` | 1183 | [cite_start]Writes short values into native mask buffer at a 3D position with brush size logic[cite: 26]. | [cite_start]**SRP:** Edit logic inside renderer; not unit testable without Unity[cite: 26]. | [cite_start]`MaskEditor.Paint()`[cite: 26]. |
| `PaintCursor()` | 1213 | [cite_start]Gets cursor voxel and delegates to `PaintMask`[cite: 26]. | [cite_start]**SRP:** Interaction glue in renderer[cite: 26]. | [cite_start]`MaskEditor` or interaction adapter[cite: 26]. |
| `FinishBrushStroke()` | 1230 | [cite_start]Finalizes a brush stroke; triggers mask buffer update[cite: 26]. | [cite_start]**SRP**[cite: 26]. | [cite_start]`MaskEditor.CommitStroke()`[cite: 26]. |
| `SaveMask()` | 1290 | [cite_start]Serializes mask buffer to FITS file via native plugin; handles overwrite logic; formats file paths (~90 lines)[cite: 26]. | [cite_start]**SRP:** Save + path logic + native call[cite: 26]. [cite_start]<br>**DIP:** Direct native call[cite: 26]. | [cite_start]`MaskPersistenceService.Save()`[cite: 26]. |
| `GetMaskSavedFilePath()` | 1379 | [cite_start]Returns last saved mask path[cite: 29]. | [cite_start]Acceptable accessor[cite: 29]. | [cite_start]`MaskPersistenceService`[cite: 29]. |

### 2.5 Coordinate and Voxel Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `ConvertWorldPositionToDataCubePosition()` | 616 | [cite_start]Converts Unity world-space `Vector3` to normalized data-cube position[cite: 31]. | [cite_start]**SRP:** Coordinate math inside renderer[cite: 31]. | [cite_start]`VolumeCoordinateService` *(pure C#, no Unity dep)*[cite: 31]. |
| `ConvertWorldRotationToDatacubeRotation()` | 627 | [cite_start]Converts Unity world-space `Quaternion` to data-cube rotation[cite: 31]. | [cite_start]**SRP + DIP:** Depends on `UnityEngine.Quaternion`[cite: 31]. | [cite_start]`VolumeCoordinateService`[cite: 31]. |
| `GetVoxelPositionDataSpace()` *(x2)* | 740, 751 | [cite_start]Converts cursor position to integer voxel coordinates in data space[cite: 31]. | [cite_start]**SRP**[cite: 31]. | [cite_start]`VolumeCoordinateService`[cite: 31]. |
| `GetVoxelPositionWorldSpace()` | 762 | [cite_start]Converts cursor world-space position to voxel coordinates[cite: 31]. | [cite_start]**SRP**[cite: 31]. | [cite_start]`VolumeCoordinateService`[cite: 31]. |
| `VolumePositionToLocalPosition()` | 1246 | [cite_start]Maps normalized volume position to local Unity position[cite: 31]. | [cite_start]**SRP + DIP**[cite: 31]. | [cite_start]`VolumeCoordinateService`[cite: 31]. |
| `LocalPositionToVolumePosition()` | 1254 | [cite_start]Inverse of the above[cite: 31]. | [cite_start]**SRP + DIP**[cite: 31]. | [cite_start]`VolumeCoordinateService`[cite: 31]. |
| `SetCursorPosition()` | 639 | [cite_start]Updates cursor voxel, outline, paint logic, source lookup (~60 lines)[cite: 31]. | [cite_start]**SRP:** Cursor state, outline rendering, paint side-effect all mixed[cite: 31]. | [cite_start]Split: `CursorTracker` + `VolumeOutlineRenderer` + `MaskEditor`[cite: 31]. |
| `SetVideoCursorLocPosition()` | 699 | [cite_start]Updates a secondary cursor for video recording use cases[cite: 31]. | [cite_start]**SRP**[cite: 31]. | [cite_start]`CursorTracker`[cite: 31]. |
| `DeactivateVideoCursorLocPosition()` | 730 | [cite_start]Hides video cursor outline[cite: 31]. | [cite_start]**SRP**[cite: 31]. | [cite_start]`CursorTracker`[cite: 31]. |

### 2.6 Region and Crop Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `SetRegionPosition()` | 794 | [cite_start]Sets region start/end voxel from world cursor; triggers outline update (~30 lines)[cite: 35]. | [cite_start]**SRP:** Interaction + outline in renderer[cite: 35]. | [cite_start]`RegionSelector.SetBound()`[cite: 35]. |
| `SetRegionBounds()` | 824 | [cite_start]Sets region min/max directly; draws outline[cite: 35]. | [cite_start]**SRP**[cite: 35]. | [cite_start]`RegionSelector.SetBounds()`[cite: 35]. |
| `UpdateRegionBounds()` | 832 | [cite_start]Recomputes region outline geometry (~35 lines)[cite: 35]. | [cite_start]**SRP:** Visual concern in domain method[cite: 35]. | [cite_start]`VolumeOutlineRenderer.UpdateRegion()`[cite: 35]. |
| `ClearRegion()` | 867 | [cite_start]Hides region outline[cite: 35]. | [cite_start]Minor SRP[cite: 35]. | [cite_start]`RegionSelector.Clear()`[cite: 35]. |
| `CropToFeature()` | 896 | [cite_start]Crops volume to a feature's bounding box; reloads cube data[cite: 35]. | [cite_start]**SRP + DIP:** Crop + data reload mixed[cite: 35]. | [cite_start]`CropService.CropToFeature()`[cite: 35]. |
| `CropToRegion()` | 909 | [cite_start]Crops volume to a manual bounding box; triggers subcube load (~33 lines)[cite: 35]. | [cite_start]**SRP + DIP**[cite: 35]. | [cite_start]`CropService.CropToRegion()`[cite: 35]. |
| `ResetCrop()` | 942 | [cite_start]Restores full volume extent; reloads full cube[cite: 35]. | [cite_start]**SRP**[cite: 35]. | [cite_start]`CropService.Reset()`[cite: 35]. |
| `TeleportToRegion()` | 1011 | [cite_start]Moves Unity transform to region center[cite: 35]. | [cite_start]**SRP:** Spatial logic in renderer[cite: 35]. | [cite_start]`LocomotionAdapter` or `SceneManager`[cite: 35]. |
| `SaveSubCube()` | 1261 | [cite_start]Serializes current crop region to FITS via native plugin (~30 lines)[cite: 35]. | [cite_start]**SRP + DIP:** File I/O in renderer[cite: 35]. | [cite_start]`SubCubePersistenceService.Save()`[cite: 35]. |

### 2.7 Feature Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `SelectFeature()` *(Vector3)* | 877 | [cite_start]Finds feature at cursor world position; delegates to `FeatureSetManager`[cite: 39]. | [cite_start]**SRP:** Selection logic in renderer[cite: 39]. | [cite_start]`FeatureSelectionService`[cite: 39]. |
| `SelectFeature()` *(Feature)* | 886 | [cite_start]Highlights a specific feature; updates `HighlightedSource` shader param[cite: 39]. | [cite_start]**SRP + DIP:** Direct shader call[cite: 39]. | [cite_start]`FeatureSelectionService` + `VolumeMaterialBinder`[cite: 39]. |
| `AddSelectionToList()` | 1385 | [cite_start]Delegates to `FeatureSetManager`[cite: 39]. | [cite_start]Acceptable thin delegation[cite: 39]. | [cite_start]`FeatureSetManager` *(already exists)*[cite: 39]. |
| `GetCubeDimensions()` | 1240 | [cite_start]Returns `XDim/YDim/ZDim` as a `Vector3Int`[cite: 39]. | [cite_start]SRP leak[cite: 39]. | [cite_start]`IVolumeDataSet` property[cite: 39]. |

### 2.8 WCS / Rest Frequency Methods

| Method | Line | Responsibility | SOLID Violation | Target Class |
| :--- | :--- | :--- | :--- | :--- |
| `RestFrequencyGHz` *(property)* | 169 | [cite_start]Get/set with change notification; triggers `RecreateFrameSet` in `Update`[cite: 41]. | [cite_start]**SRP:** WCS logic triggered via setter side-effect[cite: 41]. | [cite_start]`WCSService.SetRestFrequency()`[cite: 41]. |
| `RestFrequencyGHzListIndex` *(property)* | 139 | [cite_start]Index into rest frequency dictionary[cite: 41]. | [cite_start]SRP[cite: 41]. | [cite_start]`WCSService`[cite: 41]. |
| `PopulateRestFrequenyList()` | 549 | [cite_start]Builds dictionary of rest frequencies from Config[cite: 41]. | [cite_start]**SRP:** Config access in renderer[cite: 41]. | [cite_start]`WCSService` or `ConfigService`[cite: 41]. |
| `ResetRestFrequency()` | 1121 | [cite_start]Resets to FITS default or zero[cite: 41]. | [cite_start]**SRP**[cite: 41]. | [cite_start]`WCSService.Reset()`[cite: 41]. |
| `Update()` *(WCS block)* | 1113-1119 | [cite_start]Calls `RecreateFrameSet` and `CreateAltSpecFrame` inside the `Update` loop when flag is set[cite: 41]. | [cite_start]**SRP:** WCS logic parsed inside frame loops[cite: 41]. | [cite_start]`WCSService` *(triggered via application layer call)*[cite: 41]. |

---

## 3. SOLID Violations Summary

### [cite_start]3.1 Single Responsibility Principle (SRP) – CRITICAL [cite: 43]
[cite_start]`VolumeDataSetRenderer` has at minimum **9 distinct responsibilities** combined within a single class[cite: 44]:
* [cite_start]FITS data loading and subcube management [cite: 47]
* [cite_start]GPU texture generation and downsampling [cite: 47]
* [cite_start]Shader uniform binding (20+ parameters per frame) [cite: 47]
* [cite_start]Mask data loading, painting, and serialization [cite: 47]
* [cite_start]3D coordinate and voxel space conversion [cite: 47]
* [cite_start]Region selection and crop management [cite: 47]
* [cite_start]Feature highlighting and selection [cite: 47]
* [cite_start]WCS and rest frequency management [cite: 47]
* [cite_start]Unity scene lifecycle and `MonoBehaviour` plumbing [cite: 48]

[cite_start]**Refactoring Target:** Split into `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, and `FoveatedSamplingPolicy` (as mandated by the architectural brief), plus `VolumeCoordinateService`, `MaskEditor`, `CropService`, and `WCSService`[cite: 49].

### [cite_start]3.2 Open/Closed Principle (OCP) – HIGH [cite: 50]
* [cite_start]**Mask Modes:** `MaskMode` (Disabled/Enabled/Inverted/Isolated) is explicitly parsed via integer hash comparisons passed straight to the shader[cite: 51]. [cite_start]Adding a new mask mode requires modifying `Update()` and rewriting sections of the shader[cite: 52]. [cite_start]The project brief mandates an `IMaskMode` Strategy interface so each mode operates as an isolated, substitutable unit[cite: 53].
* [cite_start]**Projection Modes:** `ProjectionMode` (MIP vs AIP) is handled by manually toggling a global Shader keyword in `Update()`[cite: 54]. [cite_start]Adding a third projection mode requires editing the concrete `Update()` loop method directly[cite: 55].

### [cite_start]3.3 Dependency Inversion Principle (DIP) – CRITICAL [cite: 56]
[cite_start]The class depends directly on concrete types throughout[cite: 57]:
* [cite_start]`VolumeDataSet` (concrete) $\rightarrow$ should be abstracted to `IVolumeDataSet` [cite: 58]
* [cite_start]`MomentMapRenderer` (found via runtime `AddComponent`) $\rightarrow$ should be injected [cite: 59]
* [cite_start]`VolumeInputController` (found via `FindObjectOfType`) $\rightarrow$ should be decoupled behind `IVolumeInputController` [cite: 59, 60]
* [cite_start]`Config.Instance` singleton $\rightarrow$ should be injected via an `IAppConfig` interface abstraction [cite: 61]
* [cite_start]Native plugin calls via `IntPtr` pointer handles $\rightarrow$ should be isolated behind `IFitsReader` or `IAstTool` interfaces [cite: 62]
* [cite_start]`FeatureSetManager` (found via `GetComponentInChildren`) $\rightarrow$ should be systematically injected [cite: 62]

[cite_start]Furthermore, core domain code (coordinate mathematics, WCS transformations, and cropping logic) transitively depends on `UnityEngine.Vector3`, `UnityEngine.Quaternion`, and `UnityEngine.IntPtr`[cite: 63]. [cite_start]This layout makes domain processing untestable outside of the Unity engine, violating the brief's requirement that domain code must not rely on `UnityEngine` types[cite: 64].

### [cite_start]3.4 Interface Segregation Principle (ISP) – HIGH [cite: 65]
[cite_start]There are no abstractions or interfaces utilized[cite: 66]. [cite_start]Every external consumer of this class is exposed to the full 60+ member public surface area[cite: 66]. [cite_start]For example, classes that only require tracking cursor state are forced to take on dependencies targeting mask painting controls and subcube FITS data exporting[cite: 67]. 

[cite_start]**Target:** Define narrow, isolated interfaces (`IVolumeRenderer`, `ICursorProvider`, `IRegionProvider`, `IMaskEditor`, `ICropService`), each adhering to a constraint of $\le7$ public members to fulfill testability metrics[cite: 68].

### [cite_start]3.5 Liskov Substitution Principle (LSP) – LOW RISK [cite: 69]
[cite_start]The class inherits from `MonoBehaviour` only and features no domain inheritance chains[cite: 72]. [cite_start]LSP is not currently violated, but it will become a key structural focus when the Strategy pattern is introduced to clean up `MaskMode` and `ProjectionMode` processing[cite: 73].

---

## [cite_start]4. GRASP Violations [cite: 74]

| GRASP Pattern | Violation Status | Evidence within Monolith |
| :--- | :--- | :--- |
| **Information Expert** | **VIOLATED** | Statistical calculations on voxel data are performed within `SetCursorPosition()` inside the renderer, rather than inside `VolumeDataSet` where the underlying source arrays actually live. [cite_start]The expert for voxel statistics is `VolumeDataSet`[cite: 75]. |
| **Creator** | **VIOLATED** | `startFunc()` explicitly constructs instances of `VolumeDataSet`, `MomentMapRenderer`, `CuboidLine`, `PolyLine`, and `FeatureSetManager` directly. [cite_start]Object creation should instead be delegated to a dedicated Factory or DI container framework[cite: 75]. |
| **Controller** | **VIOLATED** | `VolumeDataSetRenderer` simultaneously acts as both an application use-case controller and a domain model container that holds active state. [cite_start]These system concerns must be split[cite: 75]. |
| **Indirection** | **MISSING** | There is no abstraction layer separating the Unity engine lifecycle from the internal domain logic. [cite_start]Domain operations make direct, unmediated calls to Unity engine APIs such as `FindObjectOfType`, `GetComponentInChildren`, and `AddComponent` directly[cite: 75]. |
| **Protected Variations** | **MISSING** | No isolation boundaries are built to handle structural changes in downstream graphics drivers (Built-in RP vs URP), input providers (SteamVR vs Unity XR), or file structures (FITS vs alternative arrays). [cite_start]Every configuration is hardcoded[cite: 75]. |
| **Low Coupling** | **VIOLATED** | Coupling Between Objects (CBO) is estimated to be excessively high ($>30$). [cite_start]The renderer references concrete classes directly: `VolumeDataSet`, `VolumeInputController`, `FeatureSetManager`, `MomentMapRenderer`, `Config`, `CuboidLine`, `PolyLine`, `Feature`, `ColorMapUtils`, `MaterialID`, and `VolumeCommandController`[cite: 75]. |
| **High Cohesion** | **VIOLATED** | Lack of Cohesion in Methods (LCOM) is estimated near $1.0$. [cite_start]Class methods share almost zero common fields across the 9 distinct operational responsibility areas[cite: 75]. |
| **Pure Fabrication** | **OPPORTUNITY** | Services like `VolumeCoordinateService`, `MaskEditor`, `CropService`, and `WCSService` should be extracted as Pure Fabrication classes. [cite_start]They must contain zero dependencies on Unity engine types to allow full validation inside edit-mode environments[cite: 75]. |

---

## [cite_start]5. Preliminary CK Metrics Baseline [cite: 76]

[cite_start]The following baseline metrics are derived from manual inspection of the source files[cite: 78]. *Note: Full automated metrics will be formally validated using Understand and NDepend analysis software.*

| Metric | Acceptable Range | Estimated Baseline | Status | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **WMC** *(Weighted Methods per Class)* | [cite_start]$\le20$ (Domain)<br>$\le40$ (Adapters) [cite: 82] | [cite_start]**~55-65** [cite: 82] | [cite_start]<span style="color:red; font-weight:bold;">FAIL</span> [cite: 82] | Contains over 40 discrete methods. [cite_start]Key blocks (`Update`, `startFunc`, `SetCursorPosition`, and `SaveMask`) exhibit high cyclomatic complexity ($\ge10$)[cite: 82]. |
| **DIT** *(Depth of Inheritance Tree)* | [cite_start]$\le4$ [cite: 82] | [cite_start]**1** [cite: 82] | [cite_start]<span style="color:green; font-weight:bold;">PASS</span> [cite: 82] | [cite_start]Inherits solely from `MonoBehaviour` parent[cite: 82]. |
| **NOC** *(Number of Children)* | [cite_start]$\le5$ [cite: 82] | [cite_start]**0** [cite: 82] | [cite_start]<span style="color:green; font-weight:bold;">PASS</span> [cite: 82] | [cite_start]No active subclasses exist[cite: 82]. |
| **CBO** *(Coupling Between Objects)* | [cite_start]$\le14$ (Domain)<br>$\le25$ (Orchestrators) [cite: 82] | [cite_start]**~30-35** [cite: 82] | [cite_start]<span style="color:red; font-weight:bold;">FAIL</span> [cite: 82] | [cite_start]Maintains direct references to over 12 concrete classes simultaneously[cite: 82]. |
| **RFC** *(Response For a Class)* | [cite_start]$\le50$ [cite: 82] | [cite_start]**~120-140** [cite: 82] | [cite_start]<span style="color:red; font-weight:bold;">FAIL</span> [cite: 82] | [cite_start]Counts its own 40 methods plus all interconnected tracking calls into `VolumeDataSet`, `Material`, and `Config` routines[cite: 82]. |
| **LCOM** *(Lack of Cohesion)* | [cite_start]$\le0.5$ [cite: 82] | [cite_start]**~0.85-0.95** [cite: 82] | [cite_start]<span style="color:red; font-weight:bold;">FAIL</span> [cite: 82] | [cite_start]Comprises 9 entirely unrelated responsibility clusters sharing little to no overlap in internal state fields[cite: 82]. |

[cite_start]All four key metrics fail their acceptable thresholds, providing strong quantitative justification for the refactoring proposal[cite: 83].

---

## [cite_start]6. Proposed Refactoring Target Classes [cite: 84]

[cite_start]Per the assignment brief (Section 6.3), `VolumeDataSetRenderer` will be decoupled into four distinct classes[cite: 85]. [cite_start]The table below lists the properties and methods slated for migration[cite: 86]:

| Target Class | Core Structural Responsibility | Methods Migrated From Legacy Source | Unity Dependency Profile |
| :--- | :--- | :--- | :--- |
| **`VolumeMaterialBinder`** | [cite_start]Manages active `Material` instances; synchronizes all shader uniform variables every frame; binds textures[cite: 87]. | [cite_start]`Update()` *(shader block)*, `ShiftColorMap()`, `ResetThresholds()`, `SelectFeature(Feature)`, and the binding sections of `InitialiseMask()`[cite: 87]. | Thin layer requiring Unity's `Material` API. [cite_start]Testable by defining an abstract `IMaterial` interface[cite: 87]. |
| **`VolumeTextureManager`** | [cite_start]Manages the lifecycle of heavy 3D textures; calculates downsampling factors; orchestrates volume texture regeneration loops[cite: 87]. | [cite_start]`GenerateDownsampledCube()`, `RegenerateCubes()`, and the texture instantiation logic inside `_startFunc()`[cite: 87]. | Pure C# for core domain rules. [cite_start]Interacts with `UnityEngine.Texture3D` strictly at boundary interfaces[cite: 87]. |
| **`VolumeCameraDriver`** | [cite_start]Tracks spatial coordinate states (Transform position, scale, and rotation); handles region teleport operations; converts coordinates between world space and data space[cite: 87]. | [cite_start]`ConvertWorldPosition*()`, `ConvertWorldRotation*()`, `GetVoxelPosition*()`, `VolumePosition*`, `LocalPosition*`, `TeleportToRegion()`, and initial scale/position/rotation properties[cite: 87]. | Thin adapter layer requiring `Transform` access. [cite_start]Internal coordinate conversion logic remains pure C#[cite: 87]. |
| **`FoveatedSamplingPolicy`** | [cite_start]Encapsulates foveated rendering parameters and logic configurations; computes optimal raymarching step counts relative to the current eye tracking focus region[cite: 87, 90]. | [cite_start]The foveated parameter block located inside `Update()`[cite: 87]. | [cite_start]**None.** Operates as a pure parameter policy class[cite: 87]. |

### [cite_start]Additional Domain Classes Required [cite: 91]
* [cite_start]**`VolumeCoordinateService`:** Encapsulates pure C# coordinate mathematical processing entirely stripped of `UnityEngine` types[cite: 92].
* [cite_start]**`MaskEditor`:** Handles mask painting procedures, brush parameters, undo logic, and commit hooks[cite: 92].
* [cite_start]**`CropService`:** Manages manual crop selections, regional constraints, and subcube loading[cite: 92].
* [cite_start]**`WCSService`:** Tracks World Coordinate System (WCS) parameters, rest frequency arrays, and AST framework alignments[cite: 92].
* [cite_start]**`MaskPersistenceService`:** Handles FITS file format serialization for drawn mask overlays[cite: 92].
* [cite_start]**`SubCubePersistenceService`:** Exports cropped data subsets out to standard FITS format files[cite: 93].

---

## [cite_start]7. Key In-Line Annotations (Selected Methods) [cite: 94]

### [cite_start]7.1 `_startFunc()` – God Method [Lines 358–548] [cite: 95]
[cite_start]This 190-line coroutine is the most critical refactoring target[cite: 96]. [cite_start]It violates SRP by performing a long list of completely separate actions in immediate sequence[cite: 96]:
* [cite_start]Pulls global configurations (`Config.Instance`) and updates raw public instance fields inline[cite: 97].
* [cite_start]Imports heavy FITS arrays via `VolumeDataSet.LoadDataFromFitsFile()`[cite: 97].
* [cite_start]Invokes `GenerateDownsampledCube()` to produce and register the GPU texture asset[cite: 98].
* [cite_start]Allocates secondary mask files if an overlay path is provided[cite: 99].
* [cite_start]Instantiates active materials and hooks up texture slots on the graphics card[cite: 100].
* [cite_start]Records initial object transform parameters to support camera resets[cite: 101].
* [cite_start]Generates procedural debug geometry (`CuboidLine` and `PolyLine` visual layout helpers)[cite: 102].
* [cite_start]Initializes `FeatureSetManager` and builds active data listings[cite: 102].
* [cite_start]Validates WCS attributes by checking AST frames[cite: 102].
* [cite_start]Extracts rest frequencies straight out of raw FITS headers[cite: 102].
* [cite_start]Dynamically links and sets up a `MomentMapRenderer` component[cite: 103].
* [cite_start]Runs an initial full-cube crop sequence via `CropToRegion()`[cite: 103].
* [cite_start]Commands the graphics API to warm up shaders[cite: 103].

[cite_start]**Refactoring Strategy:** This routing should be rewritten as a clean orchestrating use-case (a Command or Service class method) that handles coordination by making clean calls into your newly separated domain modules[cite: 104]. [cite_start]The outer Unity coroutine wrapper must remain a thin adapter layer[cite: 105].

### [cite_start]7.2 `Update()` – Frame-Rate Shader Push [Lines 1022–1120] [cite: 106]
[cite_start]This routine pushes ~25 independent shader uniforms to `_materialInstance` every frame via explicit `SetFloat`, `SetInt`, and `SetVector` calls[cite: 107]. 

[cite_start]While functionally correct, this architecture is a massive anti-pattern[cite: 108]. [cite_start]It mixes distinct concerns into a single tight loop: shader uniform tracking (`VolumeMaterialBinder`), mask voxel offset matrix computations (belongs inside `VolumeMaterialBinder` or a dedicated `MaskRenderState` value object), WCS change detection polling (belongs inside `WCSService`), and keyword-based projection mode swapping (belongs inside `VolumeMaterialBinder`)[cite: 108].

[cite_start]**Refactoring Strategy:** Migrate this entire shader block to `VolumeMaterialBinder.SyncShaderState()`[cite: 109]. [cite_start]WCS state validation must move to a clean event-driven paradigm (e.g., updating the `RestFrequencyGHz` setter fires a structural notification event that is intercepted and handled natively by `WCSService`) instead of being polled inside the frame loop[cite: 109].

### [cite_start]7.3 `SaveMask()` – Native Plugin Direct Call [Lines 1290–1378] [cite: 110]
[cite_start]At ~90 lines, `SaveMask()` is the longest single method in the class[cite: 112]. [cite_start]It opens direct connections to a native plugin binary (`idavie_native.dll`) via raw `IntPtr` memory pointers, constructs system file paths using manual string concatenation, parses data-overwrite flags, and handles immediate UI progress sliders[cite: 113].

[cite_start]This is a textbook example of a **Dependency Inversion Principle (DIP) violation**—the renderer class is explicitly coupled to the local file system and low-level native ABIs[cite: 114].

[cite_start]**Refactoring Strategy:** Extract this entire process into a standalone `MaskPersistenceService` that implements an explicit `IMaskPersistence` interface[cite: 115]. [cite_start]The renderer will make clean, high-level calls to `service.Save(maskData, path)`[cite: 115]. [cite_start]The service class will encapsulate the native plugin communication, path handling, and error logging internally[cite: 116]. [cite_start]This completely isolates the renderer, allowing it to be unit-tested without a compiled native plugin, while the file saving operations can be validated independently[cite: 117].

### [cite_start]7.4 `SetCursorPosition()` – Mixed Concerns [Lines 639–698] [cite: 118]
[cite_start]This ~60-line method handles cursor voxel array index calculation, procedural voxel wireframe boundary alignment, hover-paint checks, direct data lookups within the mask buffer, and active `HighlightedSource` identifier mapping[cite: 118]. 

[cite_start]Combining these four separate responsibilities inside a single method violates single-responsibility design[cite: 119]. [cite_start]It should be split cleanly into `CursorTracker.Update()`, `VolumeOutlineRenderer.SetVoxelOutline()`, and `MaskEditor.OnCursorMoved()`[cite: 119].

---

## [cite_start]8. Unity 6 Migration Flags [cite: 120]

| Current Legacy Code Pattern | Line(s) | Unity 6 Engine Impact | Mitigation & Refactoring Pattern |
| :--- | :--- | :--- | :--- |
| `Shader.EnableKeyword` / `DisableKeyword` *(Global)* | 1107–1112 | [cite_start]Global shader keywords are deprecated in modern URP/HDRP pipelines[cite: 120]. | [cite_start]Shift configuration patterns to utilize local material keywords via `LocalKeyword` on the material instance instead[cite: 120, 121]. [cite_start]`VolumeMaterialBinder` uses `material.EnableKeyword()`[cite: 121]. |
| `Graphics.DrawProceduralNow()` | 1148–1156 | [cite_start]This calling convention is completely replaced by structural `CommandBuffer` pathways or the `RenderGraph` system within URP/HDRP pipelines[cite: 121]. | [cite_start]Shift rendering operations to the `VolumeMaskRenderer` module utilizing `CommandBuffer.DrawProcedural()`[cite: 122]. |
| `MeshRenderer.material` *(Direct Instantiation)* | ~430 | [cite_start]While still functional under URP, direct material sets alter optimization batching profiles and conflict with advanced instancing techniques[cite: 122]. | [cite_start]`VolumeMaterialBinder` will encapsulate this data flow via the highly efficient `MaterialPropertyBlock` pattern[cite: 123]. |
| `FindObjectOfType<>()` | ~387, ~528 | [cite_start]Imposes significant performance penalties and operates as a deprecated lookup pattern within Unity 6 environments[cite: 123]. | [cite_start]Eliminate runtime scene lookups entirely[cite: 124]. [cite_start]Replace with constructor or field dependency injection[cite: 124]. |
| `GetComponentInChildren<>()` | ~385 | [cite_start]Works at runtime but couples code logic to the rigid layout of the scene hierarchy tree[cite: 124]. | [cite_start]Replace scene layout assumptions with explicitly injected references[cite: 125]. |
| `AddComponent<>()` | ~527 | [cite_start]Works at runtime but introduces heavy coupling during execution cycles[cite: 125]. | [cite_start]Replace manual components creation with an injected reference to an abstract `IMomentMapRenderer` implementation[cite: 125]. |
| **Built-In Render Pipeline Shaders** (`BasicVolume.shader`) | - | The Built-In Render Pipeline is deprecated in Unity 6.5+. [cite_start]Shaders will fail to compile and render[cite: 126, 127]. | [cite_start]Port raymarching routines to URP ShaderGraph or native HLSL[cite: 127]. [cite_start]Build a protective anti-corruption layer inside `VolumeMaterialBinder` to isolate shader changes[cite: 128]. |

---

## [cite_start]9. Next Steps for Sub-team 3 [cite: 128]

1. [cite_start]Run automated code metrics analysis software (Understand and NDepend) on the legacy `VolumeDataSetRenderer.cs` file to secure exact cyclomatic complexity baseline metrics (Day 2 Deliverable T2)[cite: 128].
2. [cite_start]Build a comprehensive before/after UML class diagram visually modeling the decoupling of the monolith into the four target adapter classes (Sprint 2, Day 7)[cite: 128].
3. [cite_start]Draft the core architecture for the `IMaskMode` Strategy interface alongside its decoupled concrete implementations (`Apply`, `Inverse`, `Isolate`, and `Disabled`)[cite: 128].
4. [cite_start]Define an explicit `IVolumeDataSet` interface layer to remove direct compile-time coupling inside `VolumeMaterialBinder` and `VolumeTextureManager`[cite: 128].
5. [cite_start]Chart an explicit sequence diagram modeling how a single rendering frame updates cleanly across the four decoupled components without stalling the CPU/GPU pipelines (Sprint 2, Day 8)[cite: 128].
6. [cite_start]Validate and ensure that the decoupled implementations achieve the following target code health metrics: $WMC \le 20$, $CBO \le 14$, $RFC \le 50$, and $LCOM \le 0.5$[cite: 129].
7. [cite_start]Document and design an anti-corruption layer pattern within the material system to manage and wrap the graphics API changes forced by migration from the legacy pipeline to URP (`BasicVolume.shader` $\rightarrow$ URP port)[cite: 129].