# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What iDaVIE is

iDaVIE (immersive Data Visualisation Interactive Explorer) is a Unity VR application for visualising 3D astronomical data cubes (FITS files). It runs on Windows only (no Linux/Mac VR driver support). The primary rendering technique is GPU ray-marching of volumetric data.

**Native plugins** are C++ and built separately via CMake (`native_plugins_cmake/`). They wrap CFITSIO (FITS file I/O), Starlink AST (WCS coordinate transforms), and a data analysis library. The `Configure.ps1` script handles this step.

## Scene / Project Structure

- `Assets/Scenes/` — Unity scenes. `ui.unity` is the main scene; `volumes.unity`, `catalogs.unity`, etc. are feature-specific scenes. `benchmark.unity` is for performance testing.
- `Assets/Scripts/` — All C# gameplay code, organised by subsystem (see below).
- `Assets/Prefabs/` — Unity prefabs for UI elements, volume rendering, shapes, etc.
- `Assets/Resources/` — Runtime-loaded assets (colour maps, shaders).
- `Assets/Materials/` — Rendering materials (ray-marching volume, masks, line renderer).
- `native_plugins_cmake/` — C++ source for the native `.dll` plugins.

## C# Script Architecture

All scripts live under `Assets/Scripts/` and are split into namespaces by subsystem:

| Directory | Namespace | Purpose |
|---|---|---|
| `VolumeData/` | `VolumeData` | Core volume rendering: `VolumeDataSet` (data + WCS), `VolumeDataSetRenderer` (Unity MonoBehaviour, ray-marching shader control, mask editing), `VolumeInputController`/`VolumeCommandController` (VR input), `Config` (JSON config singleton) |
| `FeatureData/` | `DataFeatures` | Source/feature overlays: `FeatureSetManager` (manages sets of features), `FeatureSetRenderer` (GPU line-drawing of bounding boxes via `ComputeBuffer`), `Feature` (individual source), `FeatureTable`/`VoTable` (file parsing) |
| `CatalogData/` | `CatalogData` | Point-cloud catalog rendering (IPAC/FITS tables): `CatalogDataSetRenderer`, `CatalogDataSetManager` |
| `PluginInterface/` | — | P/Invoke bindings to native DLLs: `FitsReader` (CFITSIO wrapper), `AstTool` (Starlink AST wrapper), `DataAnalysis` (source finding/stats), `NativePluginLoader` (runtime DLL loading) |
| `Menu/` | — | All in-VR menu panel controllers (histogram, moment maps, spectral profile, paint, shape, video recorder) |
| `UI/` | — | Desktop 2D UI components (`CanvassDesktop`) and VR pointer/laser interaction; `UI/Menus/` holds `RenderingController` and `OptionController` |
| `Shapes/` | — | User-drawn 3D shapes (cube, sphere, cylinder, cuboid) for masking regions |
| `VideoMaker/` | — | Scripted fly-through video recording (IDVS script format parser) |
| `VoiceCommands/` | — | SteamVR/Windows speech recognition integration |
| `VRKeyboard/` | — | In-VR floating keyboard (button hierarchy: `Abstract_VR_button` → `Char_VR_button`, `Backspace_VR_button`, etc.) |
| `LineRenderer/` | `LineRenderer` | `WorldSpaceLineRenderer` for drawing lines in 3D space |
| `Tools/` | — | Utilities: colour map enum, delegates, camera controller, benchmarking |
| `Debuggers/` | — | `DebugLogging` and `FitsReaderDebug` helpers (Editor / development only) |
| `Editor/` | — | Unity Editor custom inspectors for `FeatureSetManager`, `FeatureSetRenderer`, `VolumeCommandController`, `VolumeInputController` |

### Key data flow

1. `VolumeDataSet` loads a FITS file via `FitsReader` (native DLL) and stores raw voxel data + WCS metadata via `AstTool`.
2. `VolumeDataSetRenderer` uploads voxel data to a 3D GPU texture and drives the ray-marching shader (`RayMarchedVolume.mat`).
3. `FeatureSetManager` manages multiple `FeatureSetRenderer` instances (one per feature set type: Mask, Imported, New, Selection). Each `FeatureSetRenderer` uses a `ComputeBuffer` of `FeatureVertex` structs and draws axis-aligned bounding boxes via `Graphics.DrawProceduralNow` in `OnRenderObject`.
4. `CatalogDataSetRenderer` renders point/line catalogs in a separate pass using compute buffers.
5. `Config` (singleton, loaded from JSON) controls rendering defaults (colour map, scaling, GPU memory limits, voice command confidence).

### Coordinate systems

Features loaded from VOTable catalogs can use four Z-axis coordinate types (`CoordTypes` in `FeatureSetRenderer`): `cartesian` (pixel), `velz` (velocity), `freqz` (frequency), or `redz` (redshift). `AstTool.GetAltSpecSet` / `AstTool.Transform3D` converts to pixel coordinates via the Starlink AST library.

## In-Depth Architecture Notes

### Known architectural pressures (current codebase)

The current production architecture has several documented maintainability issues relevant when reading or refactoring the code:

- **God classes**: `VolumeDataSet` (~1920 lines, no namespace inheritance) owns file I/O, WCS transforms, histogram computation, GPU texture upload, mask voxel editing, undo/redo history, and source statistics — all in one plain C# class. `VolumeDataSetRenderer` is similarly overloaded as a MonoBehaviour.
- **Singleton coupling**: `Config.Instance` is accessed directly throughout the codebase. There is no injection point.
- **Domain logic tied to UnityEngine**: `VolumeDataSet` creates `Texture3D` and `ComputeBuffer` objects directly, meaning it cannot be tested or reasoned about outside a Unity context.
- **Thin plug-in abstraction**: `FitsReader`, `AstTool`, and `DataAnalysis` are static classes with raw P/Invoke signatures and `IntPtr` everywhere. There is no versioning or isolation boundary between the Unity layer and the native DLLs.
- **Two GUIs with no shared abstraction**: The desktop GUI (`CanvassDesktop`) and the in-VR GUI (Menu/ scripts) share state via direct MonoBehaviour references rather than through interfaces.
- **No automated tests**: The thin abstraction over native plug-ins and the tight coupling to Unity lifecycle methods make unit testing practically impossible without significant refactoring.

### VolumeDataSet internals

`VolumeDataSet` (not a MonoBehaviour) maintains two parallel data representations:

- **Full-resolution**: `FitsData` (raw `IntPtr` in unmanaged memory, allocated by CFITSIO via `FitsReader`). This is the source of truth.
- **Cropped region for editing**: `RegionCube` (`Texture3D`, GPU-side), `_regionMaskVoxels` (`short[]`, CPU-side), `ExistingMaskBuffer` and `AddedMaskBuffer` (`ComputeBuffer`, GPU-side). The region is a sub-volume centred on the user's paint brush.

Mask voxels are `Int16`: 0 = unmasked, positive integer = source ID. The GPU compute buffers store `VoxelEntry` structs (flat index + compound value where the upper 15 bits encode which of the 6 cube faces are "active" for rendering outlines).

Brush stroke undo/redo uses `BrushStrokeHistory` / `BrushStrokeRedoQueue` (lists of `BrushStrokeTransaction`), each transaction recording prior voxel values before painting.

### VolumeDataSetRenderer enums (shader control)

| Enum | Values |
|---|---|
| `ScalingType` | Linear, Log, Sqrt, Square, Power, Gamma |
| `MaskMode` | Disabled, Enabled, Inverted, Isolated |
| `ProjectionMode` | MaximumIntensityProjection, AverageIntensityProjection |

### Native plugin layer

Three static C# wrapper classes in `PluginInterface/` talk to Windows DLLs via P/Invoke:

| Class | Native library | Responsibility |
|---|---|---|
| `FitsReader` | CFITSIO | Open/read/write FITS files; extract headers; crop sub-images; update mask voxels |
| `AstTool` | Starlink AST | Create `AstFrameSet` from FITS header; pixel ↔ sky coordinate transforms; spectral unit conversion; alternate spectral frame (frequency ↔ velocity) |
| `DataAnalysis` | custom | Statistics (min, max, mean, σ); histogram; crop + downsample; source finding; voxel value lookup |

`NativePluginLoader` handles runtime DLL loading so the correct platform binaries are selected.

### FeatureSetType taxonomy

`FeatureSetManager` maintains one `FeatureSetRenderer` per type:

| Type | Meaning |
|---|---|
| `Mask` | Sources derived from the loaded mask FITS file |
| `Imported` | Sources loaded from a VOTable/FeatureTable file |
| `New` | Sources created by the user in-session |
| `Selection` | The currently highlighted source subset |

### Config singleton

`Config` is a plain C# class (not MonoBehaviour), loaded from a JSON file using `Valve.Newtonsoft.Json`. Key fields: `gpuMemoryLimitMb`, `maxRaymarchingSteps`, `maxModeDownsampling`, `foveatedRendering`, `bilinearFiltering`, `defaultColorMap`, `defaultScalingType`, `angleCoordFormat`, `velocityUnit`, `voiceCommandConfidenceLevel`. Accessed everywhere as `Config.Instance`.

### Dual GUI architecture

iDaVIE has two simultaneous GUIs:
- **Desktop GUI** (`CanvassDesktop`): 2D Unity UI rendered on a monitor. Entry point for file loading and high-level controls.
- **VR GUI**: In-headset menus (all scripts under `Menu/`): histogram, moment maps, spectral profile, paint brush, shape tools, video recorder. Interaction via VR controller laser pointer (`LaserPointer`, `PointerController`).

### Assignment context (ISE Refactoring Assessment)

The proposal is a description of the final state after the refactor, no records of changes between versions("was X, now Y") of the proposal should be kept.
Minimizing redundancy and complexity in the deliverables is critical, the larger the API surface exposed the more changes will likely be necessary to align with other sub-teams.
If you are given any directive regarding how you should approach the refactor, the wording of the deliverables or design approach, ask if it should be added to CLAUDE.md.
State your assumptions explicitly. If uncertain, ask.
If multiple interpretations exist, present them - don't pick silently.
If a simpler approach exists, say so. Push back when warranted.
If something is unclear, stop. Name what's confusing. Ask.
Simplicity is a priority.

## Code Style Notes

- All source files carry the LGPL-3.0 header block — preserve it in new files.
- Namespaces follow directory names (`VolumeData`, `DataFeatures`, `CatalogData`, `fts` for plugin loader).
- Pull requests must include documentation in the code and a clear description of changes.

# Needed additional documents
Any refactor effort must be accompanied by the assignment specification PDF. If it is not provided, the user should be notified that it is necessary.

# Current refactor
The worked refactor is focused on refactoring VolumeDataSetRenderer and FeatureSetRenderer, as well as any related and dependent classes owned by other sub-teams.
Every sub-team must have at least one class refactored.
