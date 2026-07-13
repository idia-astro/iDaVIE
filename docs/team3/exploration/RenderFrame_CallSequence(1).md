# Render-Frame Call Sequence
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Files traced:**
- `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` — C# CPU side
- `Assets/Shaders/Volumes/BasicVolume.cginc` — GPU shader side

---

## Overview

Every frame in iDaVIE has two phases — a **CPU phase** where Unity prepares the shader uniforms, and a **GPU phase** where the shaders actually draw pixels. This document traces exactly what happens in each phase, in order, with exact line numbers so you can find everything in the code.

---

## Phase 1 — One-Time Setup (First Frame Only)

This runs once when the scene loads via `Start()` → `_startFunc()`.

```
[VolumeDataSetRenderer.cs]

Start()                                                             Line 353
  └─► _startFunc() [Coroutine]                                     Line 358
        ├─ VolumeDataSet.LoadDataFromFitsFile()                     Line 379
        ├─ If mask file exists:
        │     └─ LoadDataFromFitsFile() for mask                   Line 399
        │     └─ _maskDataSet.GenerateVolumeTexture(Point)         Line 402
        ├─ FindDownsampleFactors()                                  Line 571
        ├─ GenerateVolumeTexture()  — upload 3D texture to GPU     Line 574
        ├─ Instantiate(_materialInstance)                          Line 410
        ├─ Instantiate(_maskMaterialInstance)                      Line 415
        ├─ SetTexture(DataCube, _dataSet.DataCube)                 Line 411
        ├─ SetInt(NumColorMaps, 80)                                 Line 412
        ├─ SetFloat(FoveationStart, ...)                            Line 413
        ├─ SetFloat(FoveationEnd, ...)                              Line 414
        ├─ _renderer.material = _materialInstance                  Line 421
        └─ If mask: SetTexture(MaskCube, _maskDataSet.DataCube)    Line 419
```

After this, the GPU has the 3D texture in VRAM and the material is ready. Everything below runs every frame.

---

## Phase 2 — Every Frame (CPU Side)

Unity calls `Update()` once per frame on the CPU **before** the GPU draws anything.

### Step 1 — Slice and Threshold Uniforms
```
[VolumeDataSetRenderer.cs]

Update()                                                            Line 1022
  ├─ SetVector(SliceMin, ...)        — crop region lower bound     Line 1026
  ├─ SetVector(SliceMax, ...)        — crop region upper bound     Line 1027
  ├─ SetFloat(ThresholdMin, ...)     — lower visibility clip       Line 1028
  ├─ SetFloat(ThresholdMax, ...)     — upper visibility clip       Line 1029
  ├─ SetFloat(Jitter, ...)           — anti-banding noise amount   Line 1030
  └─ SetFloat(MaxSteps, ...)         — fallback step count         Line 1031
```

### Step 2 — Transfer Function Uniforms
```
  ├─ SetFloat(ColorMapIndex, ...)    — which colour strip to use   Line 1032
  ├─ SetFloat(ScaleMax, ...)                                        Line 1033
  ├─ SetFloat(ScaleMin, ...)                                        Line 1034
  ├─ SetInt(ScaleType, ...)          — Linear/Log/Sqrt/etc.        Line 1036
  ├─ SetFloat(ScaleBias, ...)                                       Line 1037
  ├─ SetFloat(ScaleContrast, ...)                                   Line 1038
  ├─ SetFloat(ScaleAlpha, ...)                                      Line 1039
  └─ SetFloat(ScaleGamma, ...)                                      Line 1040
```

### Step 3 — Foveated Rendering Uniforms
```
  ├─ SetFloat(FoveationStart, ...)                                  Line 1042
  ├─ SetFloat(FoveationEnd, ...)                                    Line 1043
  ├─ If FoveatedRendering ON:
  │     ├─ SetFloat(FoveationJitter, ...)                           Line 1046
  │     ├─ SetInt(FoveatedStepsLow, 64)   — peripheral steps       Line 1047
  │     └─ SetInt(FoveatedStepsHigh, 384) — foveal steps           Line 1048
  └─ If FoveatedRendering OFF:
        ├─ SetInt(FoveatedStepsLow, MaxSteps)   — flat step count  Line 1052
        └─ SetInt(FoveatedStepsHigh, MaxSteps)                      Line 1053
```

### Step 4 — Feature Highlight Uniforms
```
  ├─ If feature selected:
  │     ├─ SetVector(HighlightMin, ...)                             Line 1064
  │     ├─ SetVector(HighlightMax, ...)                             Line 1065
  │     └─ SetFloat(HighlightSaturateFactor, dimFactor)            Line 1066
  └─ If no feature selected:
        └─ SetFloat(HighlightSaturateFactor, 1.0)                  Line 1070
```

### Step 5 — Mask Uniforms
```
  ├─ If _maskDataSet != null:
  │     ├─ SetInt(MaskMode, MaskMode.GetHashCode())  — 0/1/2/3    Line 1076
  │     ├─ Calculate 4 voxel corner offsets from transform matrix
  │     ├─ SetVectorArray(MaskVoxelOffsets, offsets[4])            Line 1088
  │     └─ SetMatrix(ModelMatrix, localToWorldMatrix)              Line 1089
  └─ If _maskDataSet == null:
        └─ SetInt(MaskMode, 0)   — force Disabled                  Line 1094
```

### Step 6 — Projection Mode Keyword
```
  ├─ If AverageIntensityProjection:
  │     └─ Shader.EnableKeyword("SHADER_AIP")   ← global (legacy)  Line 1099
  └─ Else:
        └─ Shader.DisableKeyword("SHADER_AIP")                      Line 1103
```

### Step 7 — Vignette Uniforms
```
  ├─ SetFloat(VignetteFadeStart, ...)                               Line 1106
  ├─ SetFloat(VignetteFadeEnd, ...)                                 Line 1107
  ├─ SetFloat(VignetteIntensity, ...)                               Line 1108
  └─ SetColor(VignetteColor, ...)                                   Line 1109
```

### Step 8 — WCS Rest Frequency (if changed)
```
  └─ If _restFrequencyGHzChanged && HasWCS:
        ├─ VolumeDataSet.RecreateFrameSet(RestFrequencyGHz)        Line 1113
        ├─ VolumeDataSet.CreateAltSpecFrame()                       Line 1114
        └─ _restFrequencyGHzChanged = false
```

---

## Phase 3 — Every Frame (GPU Side — Vertex Shader)

After `Update()` finishes, Unity submits the draw call. The GPU runs `vertexShaderVolume` once per vertex of the cube mesh.

```
[BasicVolume.cginc]

vertexShaderVolume(vertex)                                          Line 127
  ├─ worldPos = UNITY_MATRIX_M × vertex    — object → world space
  ├─ clipPos  = UNITY_MATRIX_VP × worldPos — world → clip space
  ├─ ray.direction = -ObjSpaceViewDir(vertex)  — ray from camera
  ├─ ray.origin    = vertex - ray.direction
  ├─ projPos = ComputeScreenPos(clipPos)    — for depth buffer read
  └─ COMPUTE_EYEDEPTH(projPos.z)           — eye depth for occlusion
```

---

## Phase 4 — Every Frame (GPU Side — Fragment Shader)

The fragment shader runs **once per pixel** that the cube mesh covers on screen. This is the most expensive part.

```
[BasicVolume.cginc]

fragmentShaderRayMarch(pixel)                                       Line 211
  │
  ├─ READ depth buffer → sceneZ            — where solid objects are Line 214
  ├─ GET vignette weight                                             Line 216
  ├─ EARLY EXIT if vignetteWeight >= 1.0                            Line 221
  │
  ├─ foveatedSamples = numSamples(pixel.xy)                         Line 224
  │     [numSamples() defined at]                                   Line 152
  │       └─ radius = distance from screen centre
  │       └─ lerp(StepsLow, StepsHigh, 1 - smoothstep(Start,End,r))
  │
  ├─ IntersectBox(ray, SliceMin, SliceMax)                          Line 228
  │     [IntersectBox() defined at]                                 Line 97
  ├─ EARLY EXIT if no hit or occluded                               Line 232
  │
  ├─ Calculate step size:
  │     stepLength = sqrt(maxLength) / foveatedSamples
  │
  ├─ Apply temporal jitter:                                          Line 259
  │     [nrand() defined at]                                        Line 142
  │     currentPos += nrand(pixel + Time) * stepVector * Jitter
  │
  ├─ THE MAIN LOOP (runs foveatedSamples times):
  │     [AIP path starts at]                                        Line 272
  │     [MIP path starts at]                                        Line 381
  │
  │     ├─ [MIP + DISABLED]  accumulateSample()                     Line 167
  │     │     → keep highest voxel value found
  │     ├─ [MIP + ENABLED]   accumulateSampleMasked()               Line 179
  │     │     → keep highest where mask > 0
  │     ├─ [MIP + INVERTED]  accumulateSampleInverseMasked()        Line 192
  │     │     → keep highest where mask == 0
  │     ├─ [MIP + ISOLATED]  accumulateMaskIsolated()               Line 205
  │     │     → track if any mask present (ignores science data)
  │     ├─ [AIP + DISABLED]  inline accumulate                      Line 285
  │     │     → sum all values × stepLength
  │     ├─ [AIP + ENABLED]   inline accumulate                      Line 313
  │     │     → sum where mask > 0
  │     ├─ [AIP + INVERTED]  inline accumulate                      Line 358
  │     │     → sum where mask == 0
  │     └─ [AIP + ISOLATED]  accumulateMaskIsolated()               Line 349
  │           → same as MIP isolated
  │
  ├─ EARLY EXIT if no value found                                   Line 446
  │
  ├─ TRANSFER FUNCTION:                                              Line 449
  │     ├─ normalise to [0,1] using ScaleMin/ScaleMax
  │     ├─ threshold clip to [ThresholdMin, ThresholdMax]
  │     ├─ stretch to fill [0,1]
  │     ├─ apply curve (Log/Sqrt/Gamma/Power/Square/Linear)
  │     ├─ bias + contrast modifiers
  │     ├─ colour map lookup: tex2D(_ColorMap, float2(x, offset))
  │     └─ highlight blend: lerp(greyscale, colour, satFactor)
  │
  └─ OUTPUT: GetVignetteFromWeight(vignetteWeight, colour)          Line 493
```

---

## Phase 5 — Every Frame (GPU Side — Mask Point Cloud)

After the main draw, `OnRenderObject()` fires and draws the mask as a point cloud overlay. Only runs when `IsFullResolution && DisplayMask`.

```
[VolumeDataSetRenderer.cs]

OnRenderObject()                                                    Line 1142
  ├─ If ExistingMaskBuffer != null:
  │     ├─ SetBuffer(MaskEntries, ExistingMaskBuffer)               Line 1146
  │     ├─ _maskMaterialInstance.SetPass(0)                         Line 1147
  │     └─ Graphics.DrawProceduralNow(Points, buffer.count)         Line 1148
  │           └─ VolumeMask.shader runs once per mask voxel
  │
  └─ If AddedMaskBuffer has entries:
        ├─ SetBuffer(MaskEntries, AddedMaskBuffer)                  Line 1152
        ├─ _maskMaterialInstance.SetPass(0)                         Line 1153
        └─ Graphics.DrawProceduralNow(Points, AddedMaskEntryCount)  Line 1154
```

---

## Complete Frame Timeline

```
FRAME START
│
├─ [CPU] Update()                              VolumeDataSetRenderer.cs Line 1022
│     ├─ 25+ SetFloat/SetInt/SetVector         Lines 1026–1109
│     ├─ Shader.EnableKeyword/DisableKeyword   Lines 1099–1103
│     └─ WCS RecreateFrameSet if changed       Lines 1113–1114
│
├─ [GPU] Unity submits draw call for cube mesh
│     └─ vertexShaderVolume() × 8 vertices    BasicVolume.cginc Line 127
│
├─ [GPU] fragmentShaderRayMarch() × every pixel  BasicVolume.cginc Line 211
│     ├─ Depth + vignette early exits          Lines 214–232
│     ├─ numSamples() foveation calc           Line 152
│     ├─ IntersectBox() ray-cube test          Line 97
│     ├─ nrand() jitter                        Line 142
│     ├─ Main loop — 8 branches                Lines 272–440
│     ├─ Transfer function                     Lines 449–491
│     └─ Output pixel colour                   Line 493
│
└─ [GPU] OnRenderObject() — mask point cloud  VolumeDataSetRenderer.cs Line 1142
      └─ DrawProceduralNow() × 1 or 2 passes  Lines 1148, 1154

FRAME END → compositor → VR headset → repeat in 11.1ms
```

---

## Problems This Sequence Reveals

| Issue | File | Lines | Impact |
|---|---|---|---|
| 25+ uniform calls every frame unconditionally | `VolumeDataSetRenderer.cs` | 1026–1109 | Wasted CPU even when nothing changed |
| `Shader.EnableKeyword` global keyword | `VolumeDataSetRenderer.cs` | 1099, 1103 | Affects all materials in the scene |
| `OnRenderObject` not called by URP | `VolumeDataSetRenderer.cs` | 1142 | Mask point cloud silently disappears on Unity 6 |
| 8 duplicated loop bodies | `BasicVolume.cginc` | 272–440 | GPU branch divergence, maintenance burden |
| WCS check polled every frame | `VolumeDataSetRenderer.cs` | 1113 | Should be event-driven |
| All uniform logic in one class | `VolumeDataSetRenderer.cs` | 1022–1119 | Untestable without a full Unity scene |

---

## Refactored Frame Sequence (Target State)

```
FRAME START
│
├─ [CPU] FoveatedSamplingPolicy.Compute()      — pure C#, ~0.01ms
│     └─ outputs (stepsLow, stepsHigh, jitter)
│
├─ [CPU] VolumeCameraDriver.Update()           — thin Unity layer
│     └─ outputs (vignetteParams, projectionMode, cursorState)
│
├─ [CPU] VolumeTextureManager.CheckDirty()     — only if texture changed
│     └─ uploads new texture async if needed
│
├─ [CPU] VolumeMaterialBinder.SyncShaderState()
│     ├─ Only pushes changed uniforms (dirty flags)
│     ├─ LocalKeyword instead of Shader.EnableKeyword
│     └─ ~0.05ms total vs current unconditional push
│
├─ [GPU] vertexShaderVolume()                  — logic unchanged
│
├─ [GPU] fragmentShaderRayMarch()              — logic unchanged
│     └─ shader variants replace 8 branching loop bodies
│
└─ [GPU] ScriptableRenderPass.Execute()        — replaces OnRenderObject
      └─ CommandBuffer.DrawProcedural()

FRAME END
```

---

*Sequence traced by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1, Day 3 — iDaVIE Refactoring Assignment 2026*
