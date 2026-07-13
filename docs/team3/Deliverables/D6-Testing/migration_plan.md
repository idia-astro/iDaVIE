# Unity 5 → Unity 6 (URP) Migration Plan
**iDaVIE — Rendering Layer**
**Sub-team 3 — Cache Me If You Can | Team Alpha**
*Reference commit: `1cd729f` | Last updated: 27 May 2026*

---

## How to Read This Document

Each breaking change is listed with:
- The **current (Unity 5) API** and the exact file/line where it lives
- The **Unity 6 URP replacement**
- **Why the change is required**
- **Before/after code examples**

The migration is ordered by risk — lowest-risk changes first so the codebase stays in a runnable state at every step. There are four categories:

1. [Render Loop](#category-1--render-loop-critical) — renderer goes dark without these
2. [Shader Keywords](#category-2--shader-keywords-high) — mask modes silently use wrong state
3. [C# API Deprecations](#category-3--c-api-deprecations-medium) — compile but fail at runtime
4. [Shader Language (CGPROGRAM → HLSL)](#category-4--shader-language-cgprogram--hlsl)

The key architectural point throughout: the `IRenderPipeline` abstraction (DD-01) means **all Category 1 changes are confined to two new files** — `UrpRenderPipeline.cs` and `VolumeRenderFeature.cs`. No domain class (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`) changes during the Unity 6 migration.

---

## Category 1 — Render Loop (Critical)

These three changes are the most urgent. Without them, the volume render produces nothing in Unity 6 URP — no error, no crash, just a black viewport.

---

### 1.1 `OnRenderObject` → `ScriptableRenderPass`

| Field | Detail |
|---|---|
| **Current code** | `void OnRenderObject()` at `VolumeDataSetRenderer.cs` line 1142 |
| **What it does** | Unity calls this after the camera renders the scene; the renderer submits its ray-march draw call here |
| **Why it breaks in URP** | URP's render loop does not call `OnRenderObject`. The method exists but is never invoked. The ray-march draw call never fires. |
| **Unity 6 replacement** | Create a `ScriptableRenderPass` subclass and inject it via a `ScriptableRendererFeature`. The pass is enqueued each frame via `EnqueuePass()` in `AddRenderPasses()`. |

```csharp
// BEFORE — VolumeDataSetRenderer.cs line 1142
void OnRenderObject()
{
    if (!_isReady) return;
    Graphics.DrawProceduralNow(_material, _bounds, MeshTopology.Triangles, 3);
}

// AFTER — VolumeRenderPass.cs (new file, URP assembly)
public class VolumeRenderPass : ScriptableRenderPass
{
    private Material _material;

    public override void Execute(ScriptableRenderContext context,
                                 ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("VolumeRaymarch");
        cmd.DrawProcedural(Matrix4x4.identity, _material, 0,
                           MeshTopology.Triangles, 3);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}

// VolumeRenderFeature.cs (new file, URP assembly)
public class VolumeRenderFeature : ScriptableRendererFeature
{
    private VolumeRenderPass _pass;

    public override void Create() => _pass = new VolumeRenderPass();

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                         ref RenderingData data)
        => renderer.EnqueuePass(_pass);
}
```

---

### 1.2 `Graphics.DrawProceduralNow` → `CommandBuffer.DrawProcedural`

| Field | Detail |
|---|---|
| **Current code** | `Graphics.DrawProceduralNow(material, bounds, MeshTopology.Triangles, 3)` at lines 1148 and 1154 |
| **Why it breaks in URP** | `DrawProceduralNow` is an immediate-mode call that executes outside any command buffer. URP batches all rendering via `CommandBuffer`s within a `ScriptableRenderPass`. The immediate call is silently dropped. |
| **Unity 6 replacement** | `cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, vertexCount)` inside `Execute()` of the `ScriptableRenderPass` |

```csharp
// BEFORE — line 1148
Graphics.DrawProceduralNow(_materialInstance, 1);
// BEFORE — line 1154
Graphics.DrawProceduralNow(_maskMaterialInstance, 1);

// AFTER — inside VolumeRenderPass.Execute()
cmd.DrawProcedural(Matrix4x4.identity, _materialInstance, 0,
                   MeshTopology.Triangles, 3);
cmd.DrawProcedural(Matrix4x4.identity, _maskMaterialInstance, 0,
                   MeshTopology.Triangles, 3);
```

---

### 1.3 `Camera.AddCommandBuffer` / `Camera.RemoveCommandBuffer` → `ScriptableRenderPass` injection

| Field | Detail |
|---|---|
| **Current code** | `camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _commandBuffer)` — Built-In pattern for injecting command buffers at named camera events |
| **Why it breaks in URP** | `CameraEvent` does not exist in URP. The `camera.AddCommandBuffer()` method is removed from the URP camera. The buffer is never executed. |
| **Unity 6 replacement** | The `ScriptableRenderPass` receives a `CommandBuffer` from the pool in `Execute()`. The injection point is set by `renderPassEvent` — `RenderPassEvent.BeforeRenderingTransparents` is the closest equivalent to `BeforeForwardAlpha`. |

```csharp
// BEFORE
_commandBuffer = new CommandBuffer { name = "VolumeRaymarch" };
camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _commandBuffer);

// AFTER — in VolumeRenderPass constructor
renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
// No manual camera registration — the RendererFeature handles injection.
```

---

## Category 2 — Shader Keywords (High)

### 2.1 `Shader.EnableKeyword` / `Shader.DisableKeyword` → `material.SetKeyword` with `LocalKeyword`

| Field | Detail |
|---|---|
| **Current code** | `Shader.EnableKeyword("SHADER_AIP")` at line 1099; `Shader.DisableKeyword("SHADER_AIP")` at line 1103 |
| **Current code** | `_materialInstance.EnableKeyword("_MASK_APPLY")` etc. at lines 1072–1094 (mask mode block) |
| **Why it breaks in URP** | `Shader.EnableKeyword()` sets **global** keyword state. URP uses per-material `LocalKeyword`s to avoid cross-material contamination. Global keywords do not propagate to URP materials — the shader variant selector never fires. |
| **Unity 6 replacement** | `material.SetKeyword(new LocalKeyword(shader, keyword), enabled)` |

```csharp
// BEFORE — line 1099
Shader.EnableKeyword("SHADER_AIP");
// BEFORE — line 1103
Shader.DisableKeyword("SHADER_AIP");

// AFTER — inside VolumeMaterialBinder
var keyword = new LocalKeyword(_material.shader, "SHADER_AIP");
_material.SetKeyword(keyword, isAip);

// BEFORE — mask mode block lines 1072–1094
_materialInstance.EnableKeyword("_MASK_APPLY");
_materialInstance.DisableKeyword("_MASK_INVERSE");
_materialInstance.DisableKeyword("_MASK_ISOLATE");

// AFTER — inside ApplyMaskMode.Apply() (IMaskMode Strategy pattern)
material.SetKeyword(new LocalKeyword(material.shader, "_MASK_APPLY"),   true);
material.SetKeyword(new LocalKeyword(material.shader, "_MASK_INVERSE"), false);
material.SetKeyword(new LocalKeyword(material.shader, "_MASK_ISOLATE"), false);
```

> **Note:** The `IMaskMode` Strategy pattern (DD-02) was specifically designed for this change. Each `Apply()` implementation wraps the correct `LocalKeyword` calls for its mode. The keyword migration is done once per mode class and never touched again.

---

### 2.2 Keyword declarations in shader files

In Unity 5 shaders, keywords are declared with `#pragma multi_compile`. In URP they must use `#pragma shader_feature_local` if they are material-local. Without `_local`, the shader compiler treats them as global and the `LocalKeyword` assignment has no effect.

```hlsl
// BEFORE — VolumeRender.shader (Built-In)
#pragma multi_compile _MASK_APPLY _MASK_INVERSE _MASK_ISOLATE

// AFTER — VolumeRaymarch.shader (URP)
#pragma shader_feature_local _MASK_APPLY _MASK_INVERSE _MASK_ISOLATE
#pragma shader_feature_local SHADER_AIP
```

---

## Category 3 — C# API Deprecations (Medium)

### 3.1 `FindObjectOfType<T>()` → `FindAnyObjectByType<T>()`

| Field | Detail |
|---|---|
| **Current code** | `FindObjectOfType<VolumeInputController>()` at line 381; `FindObjectOfType<VolumeCommandController>()` at line 522 |
| **Why it changes** | `Object.FindObjectOfType<T>()` is deprecated in Unity 6 (warning → error progression). The replacement distinguishes between finding any instance (`FindAnyObjectByType`) and finding the first sorted by instance ID (`FindFirstObjectByType`). |
| **Unity 6 replacement** | `Object.FindAnyObjectByType<T>()` — or, better, constructor injection (DD-03) which eliminates the scene search entirely. |

```csharp
// BEFORE — line 381
_volumeInputController = FindObjectOfType<VolumeInputController>();
// BEFORE — line 522
_volumeCommandController = FindObjectOfType<VolumeCommandController>();

// IMMEDIATE FIX — keeps existing structure
_volumeInputController  = FindAnyObjectByType<VolumeInputController>();
_volumeCommandController = FindAnyObjectByType<VolumeCommandController>();

// FULL FIX — DD-03 architecture, inject at composition root
public VolumeRenderCoordinator(IVolumeInputController input,
                                IVolumeCommandController command, ...) { ... }
```

---

### 3.2 `GetComponentInChildren<T>()` — inactive object behaviour change

| Field | Detail |
|---|---|
| **Current code** | `GetComponentInChildren<FeatureSetManager>()` at line 382 |
| **Why it changes** | Unity 6 changes the default `includeInactive` behaviour. If `FeatureSetManager` is disabled during startup the reference will be `null`, causing a `NullReferenceException` that would not have occurred in Unity 5. |
| **Fix** | Pass `includeInactive: true` explicitly to match Unity 5 behaviour. |

```csharp
// BEFORE — line 382
_featureManager = GetComponentInChildren<FeatureSetManager>();

// AFTER — explicit inactive inclusion
_featureManager = GetComponentInChildren<FeatureSetManager>(includeInactive: true);
if (_featureManager == null)
    Debug.LogError("[VolumeRenderer] FeatureSetManager not found in hierarchy.");
```

---

### 3.3 `AddComponent(typeof(MomentMapRenderer))` → prefab or DI

| Field | Detail |
|---|---|
| **Current code** | `AddComponent(typeof(MomentMapRenderer))` at line 517 |
| **Why it changes** | Unity 6 introduces stricter warnings about adding components with serialised state at runtime — it can cause data loss on scene reload. |
| **Fix** | Inject `IMomentMapRenderer` at the composition root (DD-03). `MomentMapRenderer` is placed in the scene as a prefab component; `VolumeRenderCoordinator` receives it via the inspector or constructor. |

```csharp
// BEFORE — line 517
_momentMapRenderer = gameObject.AddComponent(typeof(MomentMapRenderer))
                         as MomentMapRenderer;

// AFTER — inspector-injected, no runtime AddComponent
[SerializeField] private MomentMapRenderer _momentMapRenderer;
```

---

### 3.4 `Config.Instance` singleton → `IAppConfig` injection

| Field | Detail |
|---|---|
| **Current code** | `Config.Instance.maxRaymarchingSteps` (line 361), `Config.Instance.restFrequenciesGHz` (line 553), `Config.Instance.displayCursorInfoOutsideCube` (line 644) |
| **Why it changes** | Unity 6's incremental domain reload can cause static singletons to persist across domain reloads or be destroyed prematurely, depending on `RuntimeInitializeOnLoadMethod` order. The `Config.Instance` reference can become stale or null mid-session. |
| **Fix** | Inject `IAppConfig` via constructor into `VolumeTextureManager` (DD-03). No class reads a global singleton directly. |

```csharp
// BEFORE — line 361
var steps = Config.Instance.maxRaymarchingSteps;

// AFTER — VolumeTextureManager constructor injection
public VolumeTextureManager(IRawVolumeDataSource dataSource, IAppConfig config)
{
    _config = config;
}
// usage:
var steps = _config.MaxRaymarchingSteps;
```

---

## Category 4 — Shader Language (CGPROGRAM → HLSL)

This is the largest volume of changes but the most mechanical — mostly find-and-replace. All changes apply to `VolumeRender.shader` and `ColourMap*.shader`.

---

### 4.1 Program blocks

```hlsl
// BEFORE — VolumeRender.shader (Built-In)
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
ENDCG

// AFTER — VolumeRaymarch.shader (URP)
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
ENDHLSL
```

---

### 4.2 Include file replacements

| Built-In include | URP replacement |
|---|---|
| `#include "UnityCG.cginc"` | `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"` |
| `#include "UnityLightingCommon.cginc"` | `#include ".../Lighting.hlsl"` |
| `#include "AutoLight.cginc"` | Not needed for unlit volume render |
| `#include "UnityStandardUtils.cginc"` | `#include ".../ShaderVariablesFunctions.hlsl"` |

---

### 4.3 Matrix and transform macros

| Built-In (CG) | URP (HLSL) | Purpose |
|---|---|---|
| `UnityObjectToClipPos(v)` | `TransformObjectToHClip(v)` | Object → clip space |
| `mul(unity_ObjectToWorld, v)` | `TransformObjectToWorld(v)` | Object → world space |
| `_WorldSpaceCameraPos` | `GetCameraPositionWS()` | Camera world position |
| `unity_ObjectToWorld` | `UNITY_MATRIX_M` | Model matrix |
| `unity_WorldToObject` | `UNITY_MATRIX_I_M` | Inverse model matrix |
| `UNITY_MATRIX_VP` | `UNITY_MATRIX_VP` | Unchanged |
| `_ProjectionParams` | `_ProjectionParams` | Unchanged |

---

### 4.4 Texture and sampler declarations

The 3D texture used for volume ray-marching must change from CG sampler syntax to HLSL macro syntax:

```hlsl
// BEFORE — VolumeRender.shader (Built-In)
sampler3D _VolumeTex;
fixed4 col = tex3D(_VolumeTex, uvw);

// AFTER — VolumeRaymarch.shader (URP)
TEXTURE3D(_VolumeTex);
SAMPLER(sampler_VolumeTex);
float4 col = SAMPLE_TEXTURE3D(_VolumeTex, sampler_VolumeTex, uvw);
```

For the colour map 2D lookup textures (`ColourMap*.shader`):

```hlsl
// BEFORE
sampler2D _ColourMap;
fixed4 mapped = tex2D(_ColourMap, float2(intensity, 0.5));

// AFTER
TEXTURE2D(_ColourMap);
SAMPLER(sampler_ColourMap);
float4 mapped = SAMPLE_TEXTURE2D(_ColourMap, sampler_ColourMap,
                                 float2(intensity, 0.5));
```

---

### 4.5 Precision types

CG's `fixed` and `half` types are not present in HLSL. Replace all with `float` (URP targets modern GPUs that use 32-bit floats throughout):

```hlsl
// BEFORE
fixed4 frag(v2f i) : SV_Target { ... }
half3 normal = ...;

// AFTER
float4 frag(v2f i) : SV_Target { ... }
float3 normal = ...;
```

---

### 4.6 Depth texture sampling

The ray-march shader reads `_CameraDepthTexture` to terminate marching when it hits solid geometry. The sampling macro changes in URP:

```hlsl
// BEFORE
sampler2D_float _CameraDepthTexture;
float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV).r);

// AFTER — Core.hlsl provides SampleSceneDepth automatically
float rawDepth = SampleSceneDepth(screenUV);
float depth    = LinearEyeDepth(rawDepth, _ZBufferParams);
```

> **Required URP asset setting:** `Depth Texture` must be set to `Enabled` in the URP Renderer asset. The `DepthTextureAvailable` property on `IRenderPipeline` surfaces this check so `VolumeMaterialBinder` can configure the depth-occlusion shader branch at startup without branching on pipeline type.

---

### 4.7 Nearest-neighbour filtering — INV-04 preservation

**INV-04** (nearest-neighbour / blocky filtering) must survive the migration. In the Built-In pipeline this was enforced by `filterMode = FilterMode.Point` on the `Texture3D`. In URP, `FilterMode.Point` still works, but the URP sample macros use the sampler state declared alongside the texture. The sampler state must be explicitly set to point:

```hlsl
// AFTER — explicit point sampler to preserve INV-04
TEXTURE3D(_VolumeTex);
// Built-in point/clamp sampler state in URP — no separate declaration needed
SamplerState sampler_point_clamp;
float4 col = SAMPLE_TEXTURE3D(_VolumeTex, sampler_point_clamp, uvw);
```

Also verify on the C# side that `VolumeTextureManager` still sets `FilterMode.Point` when uploading:

```csharp
// This line must be preserved in VolumeTextureManager after extraction
texture.filterMode = FilterMode.Point;  // INV-04 — do not remove
```

---

## Migration Sequence — Recommended Order

Changes should be made in this order to keep the codebase building at every step.

| Step | Change | File(s) | Risk | Testable without GPU |
|---|---|---|---|---|
| 1 | Install URP package; switch Graphics Settings to URP | Project settings | — | — |
| 2 | Replace `FindObjectOfType` with `FindAnyObjectByType` (lines 381, 522) | `VolumeDataSetRenderer.cs` | Low | ✅ compile |
| 3 | Add `includeInactive: true` to `GetComponentInChildren` (line 382) | `VolumeDataSetRenderer.cs` | Low | ✅ compile |
| 4 | Replace `Config.Instance` reads with injected `IAppConfig` (lines 361, 553, 644) | `VolumeDataSetRenderer.cs` | Low | ✅ unit test |
| 5 | Replace global `Shader.EnableKeyword`/`DisableKeyword` with `material.SetKeyword(new LocalKeyword(...))` (lines 1099, 1103) | `VolumeDataSetRenderer.cs` | Medium | ✅ unit test |
| 6 | Replace mask-mode `EnableKeyword`/`DisableKeyword` with `LocalKeyword` in each `IMaskMode.Apply()` (lines 1072–1094) | `ApplyMaskMode.cs` etc. | Medium | ✅ unit test |
| 7 | Create `VolumeRenderFeature.cs` + `VolumeRenderPass.cs`; move draw call from `OnRenderObject` into `Execute()` | New files | High | ❌ Unity player |
| 8 | Replace `Graphics.DrawProceduralNow` (lines 1148/1154) with `cmd.DrawProcedural` inside pass | `VolumeRenderPass.cs` | High | ❌ Unity player |
| 9 | Remove `camera.AddCommandBuffer` calls; set `renderPassEvent` in pass | `VolumeRenderPass.cs` | High | ❌ Unity player |
| 10 | Convert shaders: `CGPROGRAM` → `HLSLPROGRAM`, all includes, matrix macros, sampler macros, precision types | `VolumeRender.shader`, `ColourMap*.shader` | High | ❌ shader compiler |
| 11 | Update depth texture sampling (`tex2D(_CameraDepthTexture,...)` → `SampleSceneDepth(...)`) | `VolumeRender.shader` | High | ❌ visual verify |
| 12 | Enable `Depth Texture` in URP Renderer asset | URP Renderer asset | Config | ❌ |
| 13 | Add `#pragma shader_feature_local` for all `_MASK_*` and `SHADER_AIP` keywords | `VolumeRaymarch.shader` | Medium | ❌ shader compiler |
| 14 | Verify `sampler_point_clamp` preserves nearest-neighbour filtering (INV-04) | `VolumeRaymarch.shader` | High | ❌ visual verify |
| 15 | Run 90 fps performance gate (INV-01) | CI | Final gate | ❌ |

Steps 1–6 can be completed in one session and verified without launching the Unity player. Steps 7–15 require the full Unity 6 player and a test machine.

---

## Summary of Affected Code

| Category | Functions / lines affected | Estimated effort |
|---|---|---|
| Render loop (`OnRenderObject`, `DrawProceduralNow`, `AddCommandBuffer`) | 3 call sites, ~20 lines | New files required (~2 hrs) |
| Shader keywords (global → `LocalKeyword`) | 4 call sites (lines 1072–1103) | ~30 min |
| C# deprecations (`FindObjectOfType`, `AddComponent`, `Config.Instance`) | 6 call sites (lines 361, 381, 382, 517, 553, 644) | ~1 hr |
| Shader language (`CGPROGRAM` → `HLSL`, macros, types, depth) | Entire `VolumeRender.shader` + `ColourMap*.shader` | ~2–3 hrs per shader |
| URP asset configuration | Graphics Settings + Renderer asset | ~15 min |

---

## Architectural Guarantee

Because the `IRenderPipeline` abstraction (DD-01) is in place before this migration begins, **all Category 1 render-loop changes are confined to two new files**: `UrpRenderPipeline.cs` and `VolumeRenderFeature.cs`. The five domain classes — `VolumeRenderCoordinator`, `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` — are untouched during the Unity 6 migration. A future HDRP migration would touch only `HdrpRenderPipeline.cs`. No domain class ever changes because of a pipeline upgrade.

---

*Sub-team 3 — Rendering Engine — Team Alpha*
*iDaVIE Refactoring Assignment 2026*
