# Four-Class Split — Rough Notes
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Purpose:** Sketch how VolumeDataSetRenderer (1,402 lines, 9 responsibilities) splits into four cohesive classes.

---

## The Core Idea

Right now everything lives in one class. The split is not arbitrary — each new class owns exactly one concern and talks to the others only through interfaces.

```
BEFORE                          AFTER
──────────────────────          ──────────────────────────────────────────
VolumeDataSetRenderer           VolumeMaterialBinder
  - shader uniforms               - shader uniforms only
  - texture management            
  - coordinate conversion       VolumeTextureManager
  - foveation                     - texture lifecycle only
  - vignette                    
  - mask logic                  VolumeCameraDriver
  - crop logic                    - spatial / coordinate only
  - WCS logic                   
  - file I/O                    FoveatedSamplingPolicy
  - Unity lifecycle               - step count calculation only
1,402 lines, 9 concerns         ~100–150 lines each, 1 concern each
```

---

## Class 1 — VolumeMaterialBinder
**One job: configure the GPU shader every frame**

Owns everything that touches `Material.SetFloat` / `SetInt` / `SetVector`. Nothing else.

**Fields it takes from VolumeDataSetRenderer:**

| Field | Current Line | Default |
|---|---|---|
| `RayMarchingMaterial` | 102 | — |
| `MaskMaterial` | 103 | — |
| `ColorMap` | 107 | `Inferno` |
| `ScalingType` | 108 | `Linear` |
| `ScalingBias` | 109 | 0 |
| `ScalingContrast` | 110 | 1 |
| `ScalingAlpha` | 111 | 1000 |
| `ScalingGamma` | 112 | 1 |
| `ThresholdMin` | 113 | 0 |
| `ThresholdMax` | 114 | 1 |
| `_materialInstance` | 225 | instantiated copy |
| `_maskMaterialInstance` | 226 | instantiated copy |
| `MaterialID` struct | 259–310 | all `Shader.PropertyToID` calls |

**Methods it takes:**

| Method | Current Line | What it does |
|---|---|---|
| `ShiftColorMap()` | 603 | cycles colour map enum |
| `ResetThresholds()` | 1136 | resets min/max to initial values |
| `SyncShaderState()` | — (NEW) | replaces the 25+ SetFloat calls in Update() |
| `SetMaskMode(IMaskMode)` | — (NEW) | our worked example |

**Replaces this in Update():**
Lines 1026–1109 — all 25+ `SetFloat`/`SetInt`/`SetVector`/`EnableKeyword` calls move here.

**Key design decisions:**
- Owns `MaterialID` struct (the integer uniform cache)
- Anti-corruption layer for URP — all `Shader.EnableKeyword` → `LocalKeyword` changes are in here only
- All `Graphics.DrawProceduralNow` → `CommandBuffer.DrawProcedural` for mask point cloud

**Unity dependency:** Thin — needs `Material` API only. Shader maths stays in HLSL.

---

## Class 2 — VolumeTextureManager
**One job: get FITS data into GPU memory**

Owns the decision of how much data fits in VRAM and manages the `Texture3D` lifecycle. Does not know how the texture will be rendered.

**Fields it takes from VolumeDataSetRenderer:**

| Field | Current Line | Default |
|---|---|---|
| `FileName` | 121 | — |
| `MaskFileName` | 122 | — |
| `MaximumCubeSizeInMB` | 68 | 250 |
| `TextureFilter` | 70 | `Point` |
| `XFactor`, `YFactor`, `ZFactor` | 126–128 | 1 |
| `subsetBounds` | 131 | full cube |
| `SelectedHdu` | 132 | 1 |
| `trueBounds` | 133 | — |
| `_dataSet` | 226 | — |
| `_maskDataSet` | 227 | — |

**Methods it takes:**

| Method | Current Line | What it does |
|---|---|---|
| `GenerateDownsampledCube()` | 567 | calculates factors, uploads texture |
| `RegenerateCubes()` | 580 | re-uploads after crop/mode change |

**Key design decisions:**
- Exposes `IVolumeDataSet DataSet` and `IVolumeDataSet MaskDataSet` — `VolumeMaterialBinder` consumes these via interface, never touches `VolumeDataSet` directly
- Enforces the 4 GB `Texture3D` ceiling — single enforcement point
- Mask texture always uploaded with `FilterMode.Point` — scientific invariant that cannot be bypassed
- `GenerateVolumeTexture()` currently inside `VolumeDataSet.cs` (line 715) — this moves here

**Unity dependency:** Moderate — needs `Texture3D` and `ComputeBuffer` APIs. The stride/downsample maths is pure C#.

---

## Class 3 — VolumeCameraDriver
**One job: own the spatial relationship between Unity world and data space**

Translates user interactions (cursor, region, teleport) from Unity world coordinates to data voxel coordinates. Drives the wireframe outline renderers. Manages the comfort vignette.

**Fields it takes from VolumeDataSetRenderer:**

| Field | Current Line | Default |
|---|---|---|
| `ProjectionMode` | 69 | `MaximumIntensityProjection` |
| `VignetteFadeStart` | 92 | 0.15 |
| `VignetteFadeEnd` | 94 | 0.40 |
| `VignetteIntensity` | 95 | 0.0 |
| `VignetteColor` | 96 | black |
| `CursorVoxel` | 188 | — |
| `CursorValue` | 189 | — |
| `InitialPosition` | 181 | — |
| `InitialScale` | 183 | — |
| `_cubeOutline` | 204 | `CuboidLine` |
| `_voxelOutline` | 204 | `CuboidLine` |
| `_regionOutline` | 204 | `CuboidLine` |
| `_videoCursorPositionOutline` | 204 | `CuboidLine` |

**Methods it takes:**

| Method | Current Line | What it does |
|---|---|---|
| `ConvertWorldPositionToDataCubePosition()` | 616 | world → normalised cube space |
| `ConvertWorldRotationToDatacubeRotation()` | 627 | world rotation → cube rotation |
| `GetVoxelPositionDataSpace()` | 740, 751 | world → integer voxel |
| `GetVoxelPositionWorldSpace()` | 762 | cursor → voxel |
| `SetCursorPosition()` | 639 | updates cursor + outline |
| `SetVideoCursorLocPosition()` | 699 | secondary cursor for video |
| `DeactivateVideoCursorLocPosition()` | 730 | hides video cursor |
| `TeleportToRegion()` | 1011 | moves transform to region centre |
| `VolumePositionToLocalPosition()` | 1246 | normalised → local Unity |
| `LocalPositionToVolumePosition()` | 1254 | local Unity → normalised |

**Key design decisions:**
- Thin Unity dependency — needs `Transform` only
- Coordinate maths extracted to `VolumeCoordinateService` (pure C#, zero Unity types) — fully testable in edit mode
- Vignette intensity driven by `IVignetteController` interface — Sub-team 4 calls `SetIntensityFromVelocity(speed)` during locomotion
- Fires `OnProjectionModeChanged` event — `VolumeMaterialBinder` listens and applies keyword

**Unity dependency:** Thin — needs `Transform`. All maths is pure C# in `VolumeCoordinateService`.

---

## Class 4 — FoveatedSamplingPolicy
**One job: calculate how many ray-march steps each pixel gets**

Pure C# class. Zero Unity dependencies. Takes the screen position and gaze point, returns a step count. That's it.

**Fields it takes from VolumeDataSetRenderer:**

| Field | Current Line | Default |
|---|---|---|
| `FoveatedRendering` | 82 | `false` |
| `FoveationStart` | 83 | 0.15 |
| `FoveationEnd` | 84 | 0.40 |
| `FoveationJitter` | 85 | 0.0 |
| `FoveatedStepsLow` | 86 | 64 |
| `FoveatedStepsHigh` | 87 | 384 |
| `MaxSteps` | 67 | 192 |
| `Jitter` | 71 | 1.0 |

**Methods it takes:**

| Method | Current Line | What it does |
|---|---|---|
| `ComputeSteps(float radialDistance)` | — (NEW) | returns step count for given radius |
| Block in `Update()` lines 1042–1053 | 1042 | pushes foveation uniforms to shader |

**Key design decisions:**
- **Zero `UnityEngine` dependencies** — the most testable class in the architecture
- `ComputeSteps()` is a pure function — input a radial distance, output an integer
- When `FoveatedRendering == false`, returns `MaxSteps` for both low and high — flat step count
- `VolumeMaterialBinder` calls `policy.ComputeSteps()` then pushes the result as uniforms

**Unity dependency:** None — pure C#. Fully unit testable in edit mode without a scene.

**Test example:**
```csharp
var policy = new FoveatedSamplingPolicy {
    FoveatedRendering = true,
    FoveationStart = 0.15f, FoveationEnd = 0.40f,
    FoveatedStepsLow = 64,  FoveatedStepsHigh = 384
};
Assert.AreEqual(64,  policy.ComputeSteps(0.45f)); // peripheral
Assert.AreEqual(384, policy.ComputeSteps(0.10f)); // foveal
```

---

## How They Talk to Each Other

```
VolumeTextureManager
  └─► exposes IVolumeDataSet (texture handles)
        └─► VolumeMaterialBinder consumes via interface
              (binds DataCube, MaskCube to material)

FoveatedSamplingPolicy
  └─► ComputeSteps() returns (stepsLow, stepsHigh)
        └─► VolumeMaterialBinder pushes as uniforms

VolumeCameraDriver
  └─► fires OnProjectionModeChanged event
        └─► VolumeMaterialBinder applies SHADER_AIP keyword
  └─► calls IVignetteController.SetIntensityFromVelocity() [Sub-team 4]
        └─► VolumeMaterialBinder pushes vignette uniforms
```

No class holds a direct reference to another. All communication is via interfaces or events.

---

## Before vs After — CK Metrics

| Metric | VolumeDataSetRenderer (now) | Per class (target) | Brief threshold |
|---|---|---|---|
| WMC | ~60 | < 10 | ≤ 20 domain |
| CBO | ~32 | < 8 | ≤ 14 domain |
| RFC | ~130 | < 30 | ≤ 50 |
| LCOM | ~0.90 | < 0.10 | ≤ 0.5 |
| Lines | 1,402 | ~100–200 | — |

---

## What Stays in a Thin MonoBehaviour Shell

Some Unity lifecycle code must stay in a MonoBehaviour. A thin `VolumeRenderer` shell keeps:
- `Start()` → delegates to `VolumeTextureManager.Load()`
- `Update()` → calls `VolumeMaterialBinder.SyncShaderState()` only
- `OnDestroy()` → calls `VolumeMaterialBinder.Dispose()` to destroy material instances
- Scene wiring — connects the four classes together at startup

This shell has **no domain logic** — it is pure Unity plumbing.

---

## Remaining Classes Identified (Not in Brief's Four)

From our "Rendering Adjacent Classes" task — these also need extracting but are not the four mandated classes:

| Class | What it extracts | From |
|---|---|---|
| `VolumeCoordinateService` | Pure C# coordinate maths | `VolumeCameraDriver` |
| `MaskEditor` | Paint, brush, commit stroke | `VolumeDataSetRenderer` |
| `CropService` | Crop/region/subcube loading | `VolumeDataSetRenderer` |
| `WCSService` | Rest frequency, AST frames | `VolumeDataSetRenderer` |
| `MaskPersistenceService` | FITS mask save/load | `VolumeDataSetRenderer` |
| `SubCubePersistenceService` | FITS subcube export | `VolumeDataSetRenderer` |

These are documented as additional proposal items — the brief's four classes are the headline deliverable.

---

*Rough notes by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1, Day 3 — iDaVIE Refactoring Assignment 2026*
