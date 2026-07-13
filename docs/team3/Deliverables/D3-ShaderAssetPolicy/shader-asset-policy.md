# Shader and Asset Organisation Policy for Unity 6
**Team Alpha | Cache Me If You Can — Sub-team 3**  
*Brief reference: Section 6.3 Sub-team Deliverables*

---

## 1. Purpose

This document defines the naming, folder structure, and lifecycle conventions for all
shader and asset files owned by the rendering sub-team in the Unity 6 project.

---

## 2. Folder Structure

```
Assets/
└── Rendering/
    ├── Shaders/
    │   ├── Volume/
    │   │   ├── VolumeRaymarch.shader         ← main ray-march shader
    │   │   ├── VolumeRaymarch.hlsl           ← shared HLSL includes
    │   │   ├── Foveated/
    │   │   │   └── FoveatedSampling.hlsl
    │   │   └── Masks/
    │   │       ├── MaskApply.hlsl
    │   │       ├── MaskInverse.hlsl
    │   │       └── MaskIsolate.hlsl
    │   └── ColourMaps/
    │       ├── ColourMap_Viridis.shader
    │       ├── ColourMap_Plasma.shader
    │       └── ColourMap_Grayscale.shader
    ├── Materials/
    │   └── Volume/
    │       └── VolumeMaterial.mat            ← runtime material instance
    ├── RenderPipeline/
    │   ├── VolumeRenderFeature.cs            ← URP ScriptableRenderFeature
    │   └── VolumeRenderPass.cs               ← URP ScriptableRenderPass
    └── ShaderVariants/
        └── VolumeVariants.shadervariants     ← variant collection for stripping
```

---

## 3. Naming Conventions

| Asset type | Convention | Example |
|-----------|------------|---------|
| Shader files | `PascalCase` noun phrase | `VolumeRaymarch.shader` |
| HLSL includes | `PascalCase` + feature name | `FoveatedSampling.hlsl` |
| Colour map shaders | `ColourMap_{Name}.shader` | `ColourMap_Viridis.shader` |
| Materials | Match the shader they use | `VolumeMaterial.mat` |
| Render features | `{Name}RenderFeature.cs` | `VolumeRenderFeature.cs` |

---

## 4. Shader Variant Stripping

Unity 6 compiles all shader keyword combinations by default, causing large build sizes.
We strip unused variants using a `ShaderVariantCollection`:

- Only variants for the three mask modes (APPLY / INVERSE / ISOLATE) are included
- Only variants for supported colour maps are included
- No debug variants ship in production builds
- Variant collection lives at `Assets/Rendering/ShaderVariants/VolumeVariants.shadervariants`

---

## 5. Runtime vs Baked Assets

| Asset | Runtime (created in code) or Baked (saved as .asset) |
|-------|------------------------------------------------------|
| `Texture3D` volumes | Runtime — created by `VolumeTextureManager`, never saved as .asset |
| `RenderTexture` targets | Runtime — created by `VolumeRenderPass` each frame |
| Colour map LUT textures | Baked — 256×1 RGBA PNG files, imported as Texture2D |
| Materials | Baked — one base material, properties set at runtime |

Rationale: 3D textures can be hundreds of MB and change with each dataset load.
Saving them as assets would pollute version control and the build.

---

## 6. URP Integration (Unity 6)

The rendering layer integrates with URP via:
- `VolumeRenderFeature` — registered in the URP Renderer asset, adds the volume pass
- `VolumeRenderPass` — a `ScriptableRenderPass` that executes the ray-march draw call

This replaces the legacy built-in pipeline pattern of calling `Graphics.Blit` from `Camera.onPostRender`.

The `VolumeRenderFeature` and `VolumeRenderPass` live in the `RenderPipeline/` folder
and are the **only** files allowed to import `UnityEngine.Rendering.Universal`.
All other rendering code imports only our `IRenderPipeline` interface.

---

## 7. Version Control Rules

- All shader source files are text-based and committed to Git
- No binary `.asset` files for materials generated from script — the hand-authored `VolumeMaterial.mat` is the exception and should be committed normally
- Colour map LUT textures committed as PNG + their `.meta` files (imported by Unity as Texture2D)
- ShaderVariantCollection committed and reviewed on any shader keyword change
