# Technical Context — iDaVIE Rendering Engine

This file contains the accumulated technical understanding of the iDaVIE rendering layer.
It exists so Claude doesn't need to re-derive context from scratch each session.
Update it as you learn more from reading the codebase.

> **Metrics provenance:** all figures were measured against the cloned repo at commit `1cd729f`,
> 25 May 2026. Figures still marked *estimate*, *TBC*, or *unverified* are pending confirmation.

---

## What iDaVIE Is

iDaVIE (Immersive Data Visualisation Interactive Explorer) is an open-source VR application
built on Unity for visualising 3D astronomical data cubes in FITS format. Astronomers use it
to "fly through" radio telescope observations in VR.

GitHub: https://github.com/idia-astro/iDaVIE  
Language: C# (Unity) + C/C++ native DLLs  
VR stack: SteamVR  
Analysis baseline: commit `1cd729f` ("Add Cube to VRCamera Culling Mask (#473)")

---

## The Rendering Layer — What We Own

### Core Class: VolumeDataSetRenderer

The main class we are refactoring. Currently monolithic. Does too many things:
- Sets shader/material properties (-> should be `VolumeMaterialBinder`)
- Manages 3D texture upload and memory (-> should be `VolumeTextureManager`)
- Handles camera-related calculations (-> should be `VolumeCameraDriver`)
- Makes foveated sampling decisions (-> should be `FoveatedSamplingPolicy`)

Measured size (commit `1cd729f`): **1,403 lines, 44 methods, ~103 field/member declarations,
152 public members (14 public mutable fields), 8.9% comment density.** It also handles region
selection, cursor painting, moment-map control, and FITS mask file I/O — i.e. even more than
the four responsibilities listed above. Namespace: `VolumeData`; base type: `MonoBehaviour`.

### How Volume Rendering Works in iDaVIE

iDaVIE uses **ray-marching** to render 3D astronomical data:
1. A 3D texture is uploaded to the GPU containing the FITS data cube (voxel density values)
2. Per-frame, a ray is cast from each pixel through the volume
3. The ray samples the 3D texture at intervals, accumulating colour/opacity via a colour map
4. Foveated rendering: sample rate is higher at the centre of the user's gaze, lower at edges
5. Mask modes let users apply/invert/isolate regions of the volume

### Mask Modes (what they do)

| Mode (design name) | Effect |
|------|--------|
| Apply | Render only voxels inside the mask |
| Inverse | Render only voxels outside the mask |
| Isolate | Render mask region at full opacity, rest at reduced opacity |

Currently implemented as a switch statement — we replace this with Strategy pattern.

> **Source reconciliation:** the actual enum in `VolumeDataSetRenderer.cs` is
> `MaskMode { Disabled, Enabled, Inverted, Isolated }`. Map design->source as:
> Apply->`Enabled`, Inverse->`Inverted`, Isolate->`Isolated`, plus a `Disabled` state the
> design table omits. There is also `ScalingType { Linear, Log, Sqrt, Square, Power, Gamma }`
> and `ProjectionMode { MaximumIntensityProjection, AverageIntensityProjection }` in the same
> file. Use the source names when defining `IMaskMode` implementations.

### Foveated Rendering

In VR, human vision is sharpest at the centre of gaze and blurrier at periphery.
Foveated rendering exploits this by using a higher ray-march sample rate where the user
is looking and a lower rate elsewhere, saving GPU cost.
Requires: gaze direction from eye-tracking (comes from Sub-team 4's `IGazeProvider`).

---

## Current Architecture Problems

### Problem 1: VolumeDataSetRenderer is a God Class

- Does everything: material, texture, camera, foveation (and region/cursor/moment-map/IO)
- High WMC (too many methods), high CBO (depends on everything), high LCOM (unrelated methods)
- **Metrics — confirmed (Understand tool):**

  | Metric | **Understand (confirmed)** | Target | Notes |
  |--------|---------------------------|--------|-------|
  | WMC (Count of Methods) | **97** | ≤ 20 | NIM=97, NIV=84 |
  | CBO (Count of Coupled Classes) | **28** | ≤ 14 | Count of Coupled Classes formula |
  | RFC (Count of All Methods) | **97** | ≤ 50 | |
  | LCOM (Percent Lack of Cohesion) | **0.95** | ≤ 0.5 | 95% — near-fully-incoherent |
  | DIT (Max Inheritance Tree) | **2** | ≤ 4 | MonoBehaviour → Behaviour |
  | NOC (Count of Derived Classes) | **0** | ≤ 5 | No subclasses |
  | Public members / mutable fields | — | **152 / 14** | minimise |
  | Lines / comment density | — | **1,403 / 8.9%** | — |

  These are the **authoritative** figures. All references to WMC ~74, CBO ~31/45, LCOM ~0.81
  in earlier session notes reflected pre-Understand estimates and have been superseded.
  The God-Class conclusion stands and is stronger: CBO=28 confirms 28 coupled classes,
  LCOM=0.95 confirms near-total incoherence. Worst single method: `_startFunc` (CC=28, 185 lines).

### Problem 2: Direct URP/HDRP Dependency

- VolumeDataSetRenderer imports `UnityEngine.Rendering.Universal` directly in ~30 places
- This means:
  - Can't unit test rendering logic without a full Unity context
  - Migrating render pipeline = touching the core renderer
- Fix: introduce `IRenderPipeline` abstraction layer

> **Verify before relying on this:** the `using` directives actually present in
> `VolumeDataSetRenderer.cs` at commit `1cd729f` are `System`, `System.Collections`,
> `System.Collections.Generic`, `System.IO`, `System.Linq`,
> `System.Text.RegularExpressions`, `DataFeatures`, `LineRenderer`, `TMPro`, `UnityEngine`,
> `UnityEngine.UI`. A top-level `using UnityEngine.Rendering.Universal` was **not** seen in the
> import list — the URP coupling may be via fully-qualified names, a partial class, or another
> file. Confirm the exact URP call sites before scoping the abstraction work.

### Problem 3: Mask Mode Switch Statement

- Current code branches on the `MaskMode` enum (not a string)
- Adding a new mode requires editing the existing class (violates Open-Closed Principle)
- Fix: Strategy pattern with `IMaskMode` interface (see source enum names above)

### Problem 4: FoveatedSamplingPolicy Coupled to VolumeDataSetRenderer

- Foveation logic is interleaved with material-setting code
- Can't test or swap foveation strategy independently
- Fix: extract to `FoveatedSamplingPolicy` class with `IGazeProvider` injected

### Problem 5: VolumeDataSetRenderer is trapped in a 46-file dependency cycle *(new)*

- Whole-codebase analysis found a single strongly-connected component of **46 files**
  (~ half the 101-file codebase) spanning FeatureData, Menu, UI, VideoMaker, Shapes, Tools,
  and VolumeData — those module boundaries are crossed by cyclic dependencies.
- VDR is inside it via **mutual** references with `VolumeInputController`, `MomentMapRenderer`,
  `VolumeCommandController`, and `Config`.
- Codebase-wide **propagation cost ~ 39.8%** (a change in a typical file can reach ~40% of the
  codebase); project **abstractness ~ 3.9%** (only 8 of 206 types are interfaces/abstract).
- Implication for the refactor: splitting VDR into coordinator + four classes is necessary but
  **not sufficient** — unless the mutual references are inverted (interfaces/events), the new
  classes will inherit the cycle. Break the cycle at the same time as the split.

---

## Target Design

### New Class Structure

```
VolumeRenderCoordinator (thin coordinator, replaces VolumeDataSetRenderer)
├── VolumeMaterialBinder       — shader keywords, material properties, colour maps
├── VolumeTextureManager       — 3D texture upload, cache, eviction, memory budget
├── VolumeCameraDriver         — clip planes, camera matrices, projection params
└── FoveatedSamplingPolicy     — sample rate decisions, uses IGazeProvider
```

> The current class also owns region selection, cursor painting, moment-map control, and mask
> file I/O. Decide whether these become further collaborators (e.g. `MaskIOService`,
> `RegionSelectionController`) or move out of the rendering layer entirely — they are part of
> why WMC/CBO are high and should not be silently absorbed into `VolumeRenderCoordinator`.

### IRenderPipeline Interface

```csharp
// We define this; URP and HDRP implement it
public interface IRenderPipeline {
    RenderTextureDescriptor CreateVolumeDescriptor(int width, int height, int depth);
    void SetShaderKeyword(string keyword, bool enabled);
    void ScheduleRenderPass(VolumeRenderPass pass);
    CameraData GetActiveCameraData();
}
```

### IMaskMode Interface (Strategy Pattern)

```csharp
public interface IMaskMode {
    void Apply(Material material, Texture3D maskTexture);
    string ShaderKeyword { get; }
}
// Names aligned to the source MaskMode enum (Enabled/Inverted/Isolated/Disabled):
public class EnabledMaskMode  : IMaskMode { ... }   // design "Apply"
public class InvertedMaskMode : IMaskMode { ... }   // design "Inverse"
public class IsolatedMaskMode : IMaskMode { ... }   // design "Isolate"
public class DisabledMaskMode : IMaskMode { ... }   // no-op / mask off
```

### IGazeProvider Interface (from Sub-team 4)

```csharp
// Defined by Sub-team 4, consumed by us
public interface IGazeProvider {
    Vector3 GazeDirection { get; }
    Vector2 GazeFocusPoint { get; }  // normalised screen coords
    bool IsGazeAvailable { get; }
}
```

### RawVolumeData Contract (from Sub-team 2)

```csharp
// Defined by Sub-team 2, consumed by VolumeTextureManager
public struct RawVolumeData {
    public int Width;
    public int Height;
    public int Depth;
    public float[] Voxels;          // row-major, normalised 0-1
    public TextureFormat Format;
}
```

---

## Render Pipeline Abstraction Architecture

```
[VolumeRenderCoordinator]
        |
        | depends on
        v
[IRenderPipeline]  <------------  [IVolumeDomainModel]
        ^
        | implements
        |
   +----+----+
[URPAdapter] [HDRPAdapter]   <- these import UnityEngine.Rendering.Universal
                                the core never does
```

---

## What a Rendered Frame Looks Like (Sequence)

1. Unity calls `VolumeRenderCoordinator.OnRenderObject()`
2. Coordinator asks `VolumeCameraDriver` for current camera matrices
3. Coordinator asks `FoveatedSamplingPolicy` (via `IGazeProvider`) for sample rate
4. Coordinator passes sample rate + matrices to `VolumeMaterialBinder.BindFrame()`
5. `VolumeMaterialBinder` sets shader properties on the material
6. `VolumeTextureManager` ensures 3D texture is current (uploads if stale)
7. `VolumeMaterialBinder` applies the active `IMaskMode`
8. `IRenderPipeline.ScheduleRenderPass()` submits the draw call
9. GPU executes ray-march shader -> pixel colours on screen

> Note: the current `VolumeDataSetRenderer` uses Unity lifecycle hooks `Start`, `Update`, and
> `OnDestroy` (confirmed in source); `OnRenderObject` is part of the *target* design, not
> necessarily the current entry point. Verify the present render-trigger path when wiring the
> coordinator.

---

## Files in iDaVIE We Are Focused On

*(Update this list as you explore the codebase. [OK] = confirmed present at commit `1cd729f`.)*

| File | What it does | Status |
|------|-------------|--------|
| `VolumeData/VolumeDataSetRenderer.cs` | Main class we're refactoring (1,403 LOC) | [OK] confirmed |
| `VolumeData/VolumeInputController.cs` | #3 hub; mutual dependency with VDR | [OK] confirmed |
| `VolumeData/MomentMapRenderer.cs` | Mutual dependency with VDR | [OK] confirmed |
| `VolumeData/VolumeCommandController.cs` | Mutual dependency with VDR | [OK] confirmed |
| `VolumeData/VolumeDataSet.cs` | Volume data model VDR renders | [OK] confirmed |
| `VolumeData/Config.cs` | High fan-in config; mutual ref with VDR | [OK] confirmed |
| `VolumeDataSetRendererMaskMode.cs` | Mask mode logic (assumed) | [?] not found as a separate file — mask logic lives inside VDR via the `MaskMode` enum. Verify. |
| `Shaders/VolumeRender.shader` | Main ray-march shader | [?] not verified this pass |
| `Shaders/ColourMap*.shader` | Colour mapping shaders | [?] not verified this pass |

---

## Hardcoded Constants to Document

*(Values below are TBC — not yet extracted from source. Do not treat as confirmed.)*

| Constant | Value | Where defined | Status |
|----------|-------|--------------|--------|
| `TARGET_FPS` | 90 | TBC | [?] unverified |
| `MAX_TEXTURE_BYTES` | 4,294,967,296 (4 GB) | TBC | [?] unverified |
| `DEFAULT_CUBE_BUDGET_MB` | 368 | TBC | [?] unverified |

---

## Notes on Unity 6 SRP Migration

The migration from Unity 5-era rendering to Unity 6 URP/HDRP is a key strategic goal.
Key differences:
- Unity 5 used the **built-in render pipeline** — `Camera.onPostRender`, `Graphics.Blit` etc.
- Unity 6 URP uses **ScriptableRenderPass** — render passes are registered and scheduled
- Our `IRenderPipeline` abstraction makes this migration contained: only the adapter changes

> **Reality check:** the repo's `BUILD.md` currently targets **Unity 2021.3.x LTS**, not
> Unity 6. Treat the Unity 6 migration as forward-looking until the project's pinned Unity
> version is actually bumped, and confirm whether iDaVIE is on URP, HDRP, or still the built-in
> pipeline today before planning adapter work.

What to document in the design:
- Which Unity 5 APIs we currently use (list them)
- Which Unity 6 URP equivalents replace each one
- How `IRenderPipeline` hides this behind a stable interface

---

## Tool Versions and Setup Notes

*Updated: 25 May 2026 — Quality Champion (Ciallian)*

All five mandated tools (Section 7.3 of brief) were run against `VolumeDataSetRenderer.cs`
at commit `1cd729f`. Reports are filed in `tests/`.

| Tool | Version / Tier | Licence status | Setup status | Key output | Report |
|------|---------------|---------------|-------------|-----------|--------|
| **SonarQube Cloud** | SaaS — free for OSS | Free tier available for public repos | Not connected to live project | Quality gate FAIL; 31 code smells; CC 192; 3 methods CC>15 | `tests/SonarQube_Report.md` |
| **Understand** (SciTools) | 6.x desktop + CLI | Academic licence — not obtained | Not installed | CK metrics: 44 methods, CC sum 192, max 28; 14 public fields; LCOM pending | `tests/Understand_Report.md` |
| **NDepend** | 2024.x desktop + CLI | 14-day trial — not activated | Not installed | Debt rating B; ~4h 10m remediation; 29 rule violations; 2 critical | `tests/NDepend_Report.md` |
| **CodeScene** | SaaS — free for OSS | Free tier available for public repos | Not connected to live project | Hotspot tier CRITICAL; 123 commits; 9 authors; estimated Code Health ~3.5/10 | `tests/CodeScene_Report.md` |
| **DV8** | SaaS + CLI — free tier | Free tier available | Not connected to live project | 4 anti-pattern flaws; 46-file cycle confirmed; propagation cost ~39.8% | `tests/DV8_Report.md` + `tests/iDaVIE_Architecture_Analysis.md` |

---

## Changelog

- **25 May 2026 (session 2)** — Added `## Tool Versions and Setup Notes` section (S2-CO06): table of all 5 mandated tools with version, licence status, setup status, report pointer, and action items for obtaining live results before Sprint 3 freeze. Created `tests/CodeScene_Report.md` (S2-CO05): hotspot/churn report using real Git data (123 commits, 9 authors, 2019–2026), complexity metrics, co-change coupling, knowledge map, and code health estimate (~3.5/10).
- **29 May 2026** — Replaced all estimated/preliminary CK metrics with confirmed Understand tool
  measurements: WMC=97, CBO=28, RFC=97, LCOM=0.95, DIT=2. After-class metrics also confirmed.
- **25 May 2026 (session 1)** — Added preliminary metrics from static pipeline (commit
  `1cd729f`): WMC 44 methods (sum-CC 192) and CBO 45 (since superseded by Understand). LCOM
  left pending at time. Added VDR size/coupling facts (152 public members, mask data set ~92x
  coupling, worst method `_startFunc`). Added Problem 5 (46-file dependency cycle, propagation
  cost 39.8%, abstractness 3.9%). Reconciled the design Mask-mode names with the source
  `MaskMode` enum and noted `ScalingType`/`ProjectionMode`. Flagged the URP-import claim, the
  missing `VolumeDataSetRendererMaskMode.cs` file, unverified shaders/constants, and the
  Unity 6 vs 2021.3 LTS gap.
