# Unity 5 / Built-In RP API Catalogue
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Purpose:** Baseline inventory of all deprecated, Unity-5-era, and Built-In Render Pipeline APIs in the iDaVIE codebase. Required evidence for the Unity 6 migration plan.

---

## Why This Matters

Unity 6 introduces breaking changes in three areas:
1. **Render Pipeline** — Built-In RP is deprecated from Unity 6.5+. All `CGPROGRAM`, `UnityCG.cginc`, and `Graphics.DrawProceduralNow` usages must be replaced.
2. **Scene Queries** — `FindObjectOfType` is marked obsolete in Unity 2023+ and removed in Unity 6. Every usage is a compile error in the target version.
3. **Input** — `SteamVR_Action` / `Valve.VR` and `UnityEngine.Windows.Speech.KeywordRecognizer` are platform-locked legacy APIs incompatible with Unity 6 XR and the new Input System.

The refactoring proposal must contain a migration path for every item in this catalogue.

---

## Category 1 — Shader / Render Pipeline APIs

These are the most critical for Sub-team 3. All shader files use Built-In RP exclusively.

### 1.1 CGPROGRAM Blocks (All Shaders)

Every shader in the project uses the Built-In RP shader model. None use `HLSLPROGRAM`.

| Shader File | Built-In RP Usage | Unity 6 / URP Replacement |
|---|---|---|
| `BasicVolume.shader` | `CGPROGRAM/ENDCG`, `UnityCG.cginc` | `HLSLPROGRAM/ENDHLSL`, URP `Core.hlsl` |
| `VolumeMask.shader` | `CGPROGRAM/ENDCG`, `UnityCG.cginc` | Same as above |
| `CatalogLine.shader` | `CGPROGRAM/ENDCG` × 2, `UnityCG.cginc` | Same as above |
| `CatalogPoint.shader` | `CGPROGRAM/ENDCG` × 2, `UnityCG.cginc` | Same as above |
| `VideoWatermark.shader` | `CGPROGRAM/ENDCG`, `UnityCG.cginc` | Same as above |

**Impact:** All 5 shader files require a rewrite of the shader program block. The rendering logic inside does not change — only the wrapper and includes.

---

### 1.2 Built-In RP Shader Functions and Macros

| Current API | File | URP Replacement |
|---|---|---|
| `ObjSpaceViewDir(v.vertex)` | `BasicVolume.cginc:132` | Manual calculation using `TransformWorldToObject` |
| `ComputeScreenPos(v2f.vertex)` | `BasicVolume.cginc:135` | `ComputeScreenPos` in URP with different conventions |
| `COMPUTE_EYEDEPTH(v2f.projPos.z)` | `BasicVolume.cginc:136` | `-TransformWorldToView(worldPos).z` |
| `LinearEyeDepth(...)` | `BasicVolume.cginc:214` | `LinearEyeDepth(depth, _ZBufferParams)` |
| `SAMPLE_DEPTH_TEXTURE_PROJ(...)` | `BasicVolume.cginc:214` | `SampleSceneDepth(uv)` from `DeclareDepthTexture.hlsl` |
| `UNITY_PROJ_COORD(...)` | `BasicVolume.cginc:214` | Removed in URP — not needed |
| `UNITY_DECLARE_DEPTH_TEXTURE(...)` | `BasicVolume.cginc:67` | `#include "DeclareDepthTexture.hlsl"` |
| `tex3Dlod(sampler, float4)` | `BasicVolume.cginc` × 10+ | `SAMPLE_TEXTURE3D_LOD(tex, sampler, uv, lod)` |
| `tex2D(sampler, uv)` | `BasicVolume.cginc:489` | `SAMPLE_TEXTURE2D(tex, sampler, uv)` |
| `tex2Dlod(sampler, float4)` | `CatalogPoint.cginc`, `CatalogLine.cginc` | `SAMPLE_TEXTURE2D_LOD(tex, sampler, uv, lod)` |
| `sampler3D _DataCube` | `BasicVolume.cginc:58` | `TEXTURE3D(_DataCube); SAMPLER(sampler_DataCube);` |
| `sampler2D _ColorMap` | `BasicVolume.cginc:59` | `TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap);` |
| `fixed4` return type | Fragment shaders | `half4` or `float4` — `fixed` removed in URP |

---

### 1.3 Global Shader Keywords (C# Side)

Global keywords affect all materials in the scene. Deprecated in URP.

| Current API | File | Line | URP Replacement |
|---|---|---|---|
| `Shader.EnableKeyword("SHADER_AIP")` | `VolumeDataSetRenderer.cs` | 1099 | `material.EnableKeyword(new LocalKeyword(shader, "SHADER_AIP"))` |
| `Shader.DisableKeyword("SHADER_AIP")` | `VolumeDataSetRenderer.cs` | 1103 | `material.DisableKeyword(new LocalKeyword(shader, "SHADER_AIP"))` |

**Owner in refactored architecture:** `VolumeMaterialBinder` — both calls live inside the anti-corruption layer.

---

### 1.4 Graphics.DrawProceduralNow

Executes immediately in the current render pass. Incompatible with URP's `ScriptableRenderPass` architecture.

| File | Line | URP Replacement |
|---|---|---|
| `VolumeDataSetRenderer.cs` | 1148 | `CommandBuffer.DrawProcedural(...)` in a `ScriptableRenderPass` |
| `VolumeDataSetRenderer.cs` | 1154 | Same |
| `CatalogDataSetRenderer.cs` | 677 | Same |

---

### 1.5 OnRenderObject / OnRenderImage Callbacks

Not called by URP's render pipeline.

| Current API | File | Line | URP Replacement |
|---|---|---|---|
| `void OnRenderObject()` | `VolumeDataSetRenderer.cs` | 1142 | Custom `ScriptableRenderPass` at correct URP frame point |
| `void OnRenderImage(RenderTexture, RenderTexture)` | `VideoCameraController.cs` | 511 | `ScriptableRendererFeature` + `ScriptableRenderPass` |

---

### 1.6 RenderTexture.active (Static GPU State)

Manipulates global GPU state. URP prefers `CommandBuffer` operations.

| File | Lines | Usage | Replacement |
|---|---|---|---|
| `MomentMapMenuController.cs` | 228, 232, 251, 254 | Screenshot capture | `AsyncGPUReadback.Request` |
| `MomentMapRenderer.cs` | 251, 253, 280, 315, 316, 350, 359 | Texture readback | `AsyncGPUReadback.Request` |
| `VideoCameraController.cs` | 522 | Video frame capture | `AsyncGPUReadback.Request` |
| `CameraControllerTool.cs` | 91 | Camera texture read | `AsyncGPUReadback.Request` |

---

### 1.7 Graphics.Blit

Works in URP but bypasses the render pipeline pass structure.

| File | Line | Usage |
|---|---|---|
| `VideoCameraController.cs` | 513, 521 | Watermark compositing onto video frames |

---

### 1.8 Material Assignment Pattern

`renderer.material` assignment is incompatible with URP's SRP Batcher.

| File | Line | Replacement |
|---|---|---|
| `VolumeDataSetRenderer.cs` | 421 | `MaterialPropertyBlock` or ensure SRP Batcher compatibility |

---

## Category 2 — Scene Query APIs

### 2.1 FindObjectOfType — 23 Usages Across the Codebase

Obsolete in Unity 2023.1, removed in Unity 6. Every call is a future compile error.

| File | Count | Type Searched |
|---|---|---|
| `VolumeDataSetRenderer.cs` | 2 | `VolumeInputController`, `VolumeCommandController` |
| `VolumeCommandController.cs` | 2 | `VolumeInputController`, `VideoRecordMenuController` |
| `CanvassDesktop.cs` | 4 | `VolumeInputController`, `VolumeCommandController`, `HistogramHelper`, `FeatureMenuController` |
| `PaintMenuController.cs` | 1 | `VolumeInputController` |
| `QuickMenuController.cs` | 1 | `VolumeInputController` |
| `ShapeMenuController.cs` | 1 | `VolumeInputController` |
| `VideoRecordMenuController.cs` | 1 | `VolumeInputController` |
| `DesktopPaintController.cs` | 2 | `CanvassDesktop`, `PaintMenuController` |
| `RenderingController.cs` | 1 | `VolumeCommandController` |
| `LaserPointer.cs` | 1 | `VolumeInputController` |
| `ToastNotification.cs` | 1 | `VolumeInputController` |
| `UserScrollableItem.cs` | 1 | `VolumeInputController` |
| `CameraControllerTool.cs` | 1 | `VolumeInputController` |
| `Shape.cs` | 2 | `VolumeInputController`, `ShapesManager` |
| `FeatureAnchor.cs` | 2 | `VolumeInputController` |
| `VoiceCommandListCreator.cs` | 1 | `VolumeCommandController` |
| `ColourMapListCreator.cs` | 1 | `VolumeCommandController` |
| `HistogramMenuController.cs` | 1 | `HistogramHelper` |

**Replacement:** Constructor or field injection. `VolumeInputController` is the most searched type (14 usages) and is highest priority to wrap behind an interface.

---

### 2.2 AddComponent at Runtime — 7 Usages

| File | Line | Component Added | Replacement |
|---|---|---|---|
| `VolumeDataSetRenderer.cs` | 517 | `MomentMapRenderer` | Inject `IMomentMapRenderer` |
| `UserSelectableItem.cs` | 47 | `BoxCollider` | Require on prefab at design time |
| `UserScrollableItem.cs` | 40, 47 | `BoxCollider`, `Button` | Require on prefab |
| `LaserPointer.cs` | 87 | `Rigidbody` | Require on prefab |
| `ToastNotification.cs` | 126 | `StaticToastNotification` | Inject or pre-attach |
| `WorldSpaceLineRenderer.cs` | 246 | `WorldSpaceLineRenderer` | Singleton registration |
| `NativePluginLoader.cs` | 61 | `NativePluginLoader` | Same |

---

## Category 3 — Legacy Input APIs

### 3.1 SteamVR / Valve.VR — 6 Files

Direct dependency on SteamVR SDK. The brief requires an `IInputProvider` abstraction to remove this.

| File | Usage |
|---|---|
| `CanvassDesktop.cs` | `Valve.VR`, `SteamVR.active`, `OpenVR.Shutdown()` |
| `MenuBarBehaviour.cs` | `Valve.VR`, `SteamVR.active`, `OpenVR.Shutdown()` |
| `UserDraggableMenu.cs` | `SteamVR_Action_Boolean`, `SteamVR_Input.GetAction<>()` |
| `OptionController.cs` | `SteamVR_Input_Sources` |
| `VideoRecordMenuController.cs` | `Valve.VR.SteamVR_Input_Sources` |
| `PointerController.cs` | `Valve.VR`, `Valve.VR.InteractionSystem` |

**Replacement:** Unity XR Interaction Toolkit. Sub-team 4 owns this via `IInputProvider` / `IPointer` / `IGripInput` interfaces.

---

### 3.2 Windows.Speech.KeywordRecognizer — Windows-Only

Not compatible with Unity 6 cross-platform builds.

| File | Usage |
|---|---|
| `VolumeCommandController.cs` | `KeywordRecognizer` construction, start, stop, phrase event (6 lines) |
| `Config.cs` | `using UnityEngine.Windows.Speech` |
| `Audio_VR_button.cs` | `using UnityEngine.Windows.Speech` |

**Replacement:** `IVoiceRecogniser` interface with `WindowsKeywordRecognizerAdapter`. Sub-team 4 owns this.

---

### 3.3 Camera.main — 14 Usages

Performs a scene search by tag on every call. Slow and deprecated pattern.

| File | Occurrences |
|---|---|
| `PaintMenuController.cs` | 3 |
| `VideoRecordMenuController.cs` | 3 |
| `QuickMenuController.cs` | 3 |
| `ShapeMenuController.cs` | 3 |
| `ToastNotification.cs` | 3 |
| `VolumeInputController.cs` | 2 |

**Replacement:** Cache in `Awake()` or inject via interface.

---

## Summary — Migration Priority Table

| Priority | Issue | Count | Blocking Unity 6? | Owner |
|---|---|---|---|---|
| 🔴 Critical | `CGPROGRAM` / `UnityCG.cginc` in all shaders | 5 shaders | YES | Sub-team 3 |
| 🔴 Critical | `tex3Dlod` / `tex2D` / `sampler3D` legacy sampling | 15+ | YES | Sub-team 3 |
| 🔴 Critical | `FindObjectOfType` (removed in Unity 6) | 23 | YES | All sub-teams |
| 🔴 Critical | `OnRenderObject` / `OnRenderImage` (not in URP) | 2 | YES | Sub-team 3 |
| 🟠 High | `Graphics.DrawProceduralNow` (not in URP) | 3 | YES | Sub-team 3 |
| 🟠 High | `Shader.EnableKeyword` global keywords | 2 | Functional but wrong | Sub-team 3 |
| 🟠 High | `KeywordRecognizer` (Windows-only) | 6 | Platform-locked | Sub-team 4 |
| 🟠 High | `SteamVR` / `Valve.VR` direct coupling | 6 files | Architecture violation | Sub-team 4 |
| 🟡 Medium | `RenderTexture.active` static state | 10 | Works but unsafe | Sub-team 3/6 |
| 🟡 Medium | `AddComponent` at runtime | 7 | Works but blocks DIP | All sub-teams |
| 🟡 Medium | `Camera.main` repeated calls | 14 | Works but slow | Sub-team 4/6 |
| 🟢 Low | `Graphics.Blit` | 2 | Works with caveats | Sub-team 6 |
| 🟢 Low | `renderer.material` direct assignment | 1 | SRP Batcher risk | Sub-team 3 |

---

## Key Architectural Insight

The `VolumeMaterialBinder` anti-corruption layer is the mechanism that **contains all Critical shader items** for Sub-team 3. Once extracted:

- All `CGPROGRAM` → `HLSLPROGRAM` changes are isolated to shader files
- All `Shader.EnableKeyword` → `LocalKeyword` changes are in `VolumeMaterialBinder` only
- All `Graphics.DrawProceduralNow` → `CommandBuffer` changes are in `VolumeMaterialBinder` only

The domain logic classes (`FoveatedSamplingPolicy`, `VolumeCoordinateService`) have zero Unity API dependencies and require no changes for the Unity 6 migration.

---

*Catalogue compiled by Sub-team 3 — Rendering Engine — Team Alpha*  
*Sprint 1, Day 3 — iDaVIE Refactoring Assignment 2026*
