# Rendering-Adjacent Classes — Catalogue
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Purpose:** Identify every class that directly feeds into, depends on, or interacts with the rendering pipeline. Required for understanding the full dependency surface of `VolumeDataSetRenderer`.

---

## How to Read This Document

Classes are grouped by how closely they touch rendering:

- **Tier 1 — Direct Rendering** — own GPU resources (textures, buffers, materials) or execute draw calls
- **Tier 2 — Rendering Data Providers** — own the data that feeds into GPU textures
- **Tier 3 — Rendering Controllers** — drive rendering parameters from the UI or input layer
- **Tier 4 — Rendering Support** — utility classes used by the rendering stack

---

## Tier 1 — Direct Rendering Classes

These classes own GPU resources or execute draw calls. They are the core rendering concern for Sub-team 3.

---

### VolumeDataSetRenderer.cs
**Location:** `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs`  
**Lines:** 1,402  
**Role:** God class. Owns the volume ray-marching material, pushes all shader uniforms every frame, executes mask point cloud draw calls.  
**GPU Resources Owned:** `_materialInstance` (Material), `_maskMaterialInstance` (Material)  
**Draw Calls:** `Graphics.DrawProceduralNow` × 2 (lines 1148, 1154)  
**Already fully annotated** — this is the primary refactoring target.

---

### MomentMapRenderer.cs
**Location:** `Assets/Scripts/VolumeData/MomentMapRenderer.cs`  
**Lines:** 386  
**Role:** Generates 2D moment map images from the 3D data cube using compute shaders. A moment map is a 2D projection of the 3D cube — moment 0 is total intensity, moment 1 is velocity field.  
**GPU Resources Owned:**
- `Moment0Map` — RenderTexture (2D intensity map)
- `Moment1Map` — RenderTexture (2D velocity map)
- `ImageOutput` — RenderTexture (final display image)
- `SpectrumBuffer` — ComputeBuffer (spectral data per pixel)

**Key issue:** Instantiated via `AddComponent` inside `VolumeDataSetRenderer._startFunc()` (line 517) — tight runtime coupling. Should be injected via `IMomentMapRenderer` interface.  
**Unity 6 issue:** Uses `RenderTexture.active` (static state) for texture readback — lines 251, 253, 280, 315, 316.

---

### CatalogDataSetRenderer.cs
**Location:** `Assets/Scripts/CatalogData/CatalogDataSetRenderer.cs`  
**Lines:** 694  
**Role:** Renders astronomical source catalogues as point clouds or line overlays inside the volume. A catalogue is a table of known sources (galaxies, stars) with positions and properties — shown as coloured points floating in the data cube.  
**GPU Resources Owned:**
- `ComputeBuffer[] _buffers` — position/colour/size data per source
- `_mappingConfigBuffer` — GPU mapping configuration
- `ColorMapTexture` — Texture2D colour map atlas
- `SpriteSheetTexture` — Texture2D point shape sprites

**Draw Calls:** `Graphics.DrawProceduralNow(MeshTopology.Points, _dataSet.N)` (line 677) — same legacy API as the volume mask renderer.  
**Notable:** Has its own vignette fields (`VignetteFadeStart`, `VignetteFadeEnd`, `VignetteIntensity`, `VignetteColor`) duplicated from `VolumeDataSetRenderer` — a clear violation of DRY and a candidate for a shared `IVignetteConsumer` interface.

---

### FeatureSetRenderer.cs
**Location:** `Assets/Scripts/FeatureData/FeatureSetRenderer.cs`  
**Lines:** 616  
**Role:** Renders feature bounding boxes and labels as GPU geometry (lines and billboards) overlaid on the volume. Each feature (a masked astronomical source) gets a coloured wireframe box and a floating label in VR.  
**GPU Resources Owned:**
- `ComputeBuffer` — feature vertex positions and colours
- `Material` — feature box material

**Dependency on renderer:** Holds a direct reference to `VolumeDataSetRenderer VolumeRenderer` — tight coupling that should become `IVolumeRenderer`.

---

### WorldSpaceLineRenderer.cs
**Location:** `Assets/Scripts/LineRenderer/WorldSpaceLineRenderer.cs`  
**Lines:** 320  
**Role:** Draws the wireframe outline boxes (`CuboidLine`) used for the cursor voxel highlight, region selection box, and the full data cube boundary. All the "you are here" visual guides in VR are drawn by this class.  
**GPU Resources Owned:** `MeshFilter` + `MeshRenderer` — builds mesh geometry at runtime from line endpoint arrays.  
**Relationship to Sub-team 3:** `VolumeCameraDriver` (target class) will drive this via position/bounds updates. `WorldSpaceLineRenderer` itself does not need to change — it is a pure presentation-layer class.

---

### VideoCameraController.cs
**Location:** `Assets/Scripts/VideoMaker/VideoCameraController.cs`  
**Lines:** 605  
**Role:** Captures rendered frames to video. Composites a watermark logo onto frames using `Graphics.Blit`.  
**GPU Resources Owned:** `RenderTexture` instances for video frame capture.  
**Legacy APIs:** `OnRenderImage` callback (line 511), `Graphics.Blit` (lines 513, 521), `RenderTexture.active` (line 522) — all require URP migration.  
**Relationship to Sub-team 3:** Peripheral — not part of the volume rendering pipeline but affected by the URP migration.

---

## Tier 2 — Rendering Data Providers

These classes own the data that gets uploaded to GPU textures. They don't draw anything themselves but are the direct upstream dependencies of the Tier 1 renderers.

---

### VolumeDataSet.cs
**Location:** `Assets/Scripts/VolumeData/VolumeDataSet.cs`  
**Lines:** 1,920  
**Role:** The most important upstream dependency of `VolumeDataSetRenderer`. Owns the raw FITS data in CPU memory and produces the `Texture3D` objects that the volume shader samples. Also owns the mask `ComputeBuffer` that the mask point cloud renderer consumes.

**GPU Resources Produced:**
- `Texture3D DataCube` — primary downsampled volume texture (line 95)
- `Texture3D RegionCube` — subcube crop texture (line 96)
- `ComputeBuffer ExistingMaskBuffer` — saved mask voxel entries (line 97)
- `ComputeBuffer AddedMaskBuffer` — newly painted mask entries (line 98)

**Key methods relevant to rendering:**
- `GenerateVolumeTexture()` (line 715) — uploads downsampled cube to GPU
- `FindDownsampleFactors()` — calculates XFactor/YFactor/ZFactor to fit VRAM budget
- `Graphics.CopyTexture()` (line 780) — slice-by-slice GPU upload

**Refactoring note:** `VolumeDataSet` is a data model class doing GPU work. The `GenerateVolumeTexture` responsibility should move to `VolumeTextureManager`. `VolumeDataSet` should expose raw data only, via an `IVolumeDataSet` interface. This is the most impactful interface boundary in the entire refactoring.

---

## Tier 3 — Rendering Controllers

These classes drive rendering parameters from the UI or input layer. They call methods on `VolumeDataSetRenderer` to change what is displayed.

---

### RenderingController.cs
**Location:** `Assets/Scripts/UI/Menus/RenderingController.cs`  
**Lines:** 315  
**Role:** The desktop GUI panel for rendering settings — colour map buttons, scaling type selector, threshold sliders, step count controls. Every button press calls a method on `VolumeDataSetRenderer`.  
**Dependency:** Holds `VolumeDataSetRenderer _activeDataSet` directly — should become `IVolumeRenderer`.  
**Notable:** Uses `FindObjectOfType<VolumeCommandController>()` (line 75) — legacy scene query.

---

### VolumeCommandController.cs
**Location:** `Assets/Scripts/VolumeData/VolumeCommandController.cs`  
**Role:** Orchestrates voice commands and controller button presses that affect rendering — colour map shifts, threshold changes, mask mode toggles, projection mode switches. Acts as the command dispatcher between user input and `VolumeDataSetRenderer`.  
**Dependency:** Holds `VolumeDataSetRenderer` reference directly.

---

### PaintMenuController.cs
**Location:** `Assets/Scripts/Menu/PaintMenuController.cs`  
**Role:** The VR paint menu — brush size, mask mode selector, paint/erase toggle. Drives `VolumeDataSetRenderer.MaskMode` and `VolumeDataSetRenderer.PaintMask()`.  
**Relationship to Sub-team 3:** Directly sets `MaskMode` — this is the consumer of the `IMaskMode` Strategy refactoring. Once `IMaskMode` is in place, `PaintMenuController` passes an `IMaskMode` instance rather than an enum value.

---

### QuickMenuController.cs
**Location:** `Assets/Scripts/Menu/QuickMenuController.cs`  
**Role:** VR quick-access menu for common rendering toggles — mask display, projection mode, colour map cycling.  
**Dependency:** `FindObjectOfType<VolumeInputController>()` — legacy scene query.

---

### HistogramHelper.cs
**Location:** `Assets/Scripts/Menu/HistogramHelper.cs`  
**Lines:** 101  
**Role:** Computes the brightness histogram of the data cube for display in the desktop GUI. Uses the same `ScaleMin`/`ScaleMax`/`ThresholdMin`/`ThresholdMax` values as the renderer to show the user where the current thresholds sit on the data distribution.  
**Rendering relevance:** Not a renderer itself, but reads rendering state to drive the histogram display. Should consume `IVolumeRenderer` rather than `VolumeDataSetRenderer` directly.

---

## Tier 4 — Rendering Support Classes

Utility and infrastructure classes used by the rendering stack.

---

### CameraControllerTool.cs
**Location:** `Assets/Scripts/Tools/CameraControllerTool.cs`  
**Lines:** 125  
**Role:** Manages secondary cameras used for screenshot and thumbnail capture. Reads `RenderTexture.active` (line 91) — legacy pattern.  
**Unity 6 concern:** `Camera.main` usage (needs caching) and `RenderTexture.active` pattern.

---

### Shape.cs / ShapesManager.cs
**Location:** `Assets/Scripts/Shapes/`  
**Role:** Renders user-defined geometric shapes (spheres, cylinders) placed inside the data cube in VR. Uses `MeshRenderer` and `MeshFilter` components. `ShapesManager` owns the collection of shapes and handles their lifecycle.  
**Rendering relevance:** Adjacent — uses Unity mesh rendering but independent of the volume shader pipeline.

---

### Colorbar.cs
**Location:** `Assets/Scripts/UI/Colorbar.cs`  
**Role:** UI colour scale indicator. Displays the active colour map as a sprite strip. Already fully annotated in the colour map session.

---

## Dependency Map — Who Calls What

```
PaintMenuController          ──► VolumeDataSetRenderer (MaskMode, PaintMask)
QuickMenuController          ──► VolumeDataSetRenderer (MaskMode, ProjectionMode)
RenderingController          ──► VolumeDataSetRenderer (ColorMap, Thresholds, ScaleType)
VolumeCommandController      ──► VolumeDataSetRenderer (ShiftColorMap, MaskMode)
                                  
VolumeDataSetRenderer        ──► VolumeDataSet (DataCube, MaskBuffer)
VolumeDataSetRenderer        ──► MomentMapRenderer (AddComponent → tight coupling)
VolumeDataSetRenderer        ──► FeatureSetRenderer (via FeatureSetManager)
VolumeDataSetRenderer        ──► WorldSpaceLineRenderer (_voxelOutline, _cubeOutline)

FeatureSetRenderer           ──► VolumeDataSetRenderer (VolumeRenderer field)
CatalogDataSetRenderer       ──► VolumeDataSet (texture data)
MomentMapRenderer            ──► VolumeDataSet (DataCube texture via property)
```

---

## Key Findings for the Refactoring Proposal

### 1. Vignette Parameters Are Duplicated
`VolumeDataSetRenderer` and `CatalogDataSetRenderer` both have identical `VignetteFadeStart`, `VignetteFadeEnd`, `VignetteIntensity`, `VignetteColor` fields. This should be a single shared `VignetteSettings` value object consumed by both.

### 2. Three Classes Use DrawProceduralNow
`VolumeDataSetRenderer` (mask), `CatalogDataSetRenderer` (catalogue points), and `FeatureSetRenderer` (feature boxes) all use the same deprecated `Graphics.DrawProceduralNow` pattern. All three need migrating to `CommandBuffer.DrawProcedural` for URP. A shared `ProceduralDrawPass : ScriptableRenderPass` could serve all three.

### 3. VolumeDataSet Is Doing GPU Work
At 1,920 lines, `VolumeDataSet` is itself close to a God Class. Its `GenerateVolumeTexture()` method creates `Texture3D` objects and calls `Graphics.CopyTexture` — GPU operations that belong in `VolumeTextureManager`, not a data model class.

### 4. FeatureSetRenderer Has a Circular-ish Dependency
`FeatureSetRenderer` holds `VolumeDataSetRenderer VolumeRenderer` and `VolumeDataSetRenderer` references `FeatureSetManager` which owns `FeatureSetRenderer` instances. This near-circular coupling is exactly what the interface boundaries are designed to break.

### 5. MomentMapRenderer Is Instantiated at Runtime
`AddComponent<MomentMapRenderer>()` inside `_startFunc()` means the moment map renderer cannot be tested or replaced without modifying `VolumeDataSetRenderer`. Injecting `IMomentMapRenderer` removes this coupling entirely.

---

## Summary Table

| Class | Lines | Tier | GPU Resources | Legacy APIs | Refactoring Priority |
|---|---|---|---|---|---|
| `VolumeDataSetRenderer` | 1,402 | 1 | Material × 2 | DrawProceduralNow, OnRenderObject, global keywords | 🔴 Primary target |
| `VolumeDataSet` | 1,920 | 2 | Texture3D × 2, ComputeBuffer × 2 | Graphics.CopyTexture | 🔴 Extract to VolumeTextureManager |
| `MomentMapRenderer` | 386 | 1 | RenderTexture × 3, ComputeBuffer | RenderTexture.active | 🟠 Inject via interface |
| `CatalogDataSetRenderer` | 694 | 1 | ComputeBuffer × N, Texture2D × 2 | DrawProceduralNow | 🟠 Shared render pass |
| `FeatureSetRenderer` | 616 | 1 | ComputeBuffer, Material | None critical | 🟠 Break circular dep |
| `WorldSpaceLineRenderer` | 320 | 1 | MeshFilter/MeshRenderer | None critical | 🟡 Driven by VolumeCameraDriver |
| `RenderingController` | 315 | 3 | None | FindObjectOfType | 🟡 Use IVolumeRenderer |
| `VolumeCommandController` | — | 3 | None | FindObjectOfType | 🟡 Use IVolumeRenderer |
| `VideoCameraController` | 605 | 1 | RenderTexture | OnRenderImage, Graphics.Blit | 🟡 URP migration |
| `CameraControllerTool` | 125 | 4 | RenderTexture | RenderTexture.active | 🟢 Low priority |
| `HistogramHelper` | 101 | 3 | None | None | 🟢 Low priority |
| `PaintMenuController` | — | 3 | None | FindObjectOfType | 🟢 IMaskMode consumer |

---

*Catalogue compiled by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1, Day 3 — iDaVIE Refactoring Assignment 2026*
