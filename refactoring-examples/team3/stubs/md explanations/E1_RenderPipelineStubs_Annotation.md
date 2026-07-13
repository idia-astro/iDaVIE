# E1 — UrpRenderPipeline.cs and HdrpRenderPipeline.cs Stubs
**Sub-team 3 — Rendering Engine — Sprint 2**

---

## What These Files Are

These are **design-level stubs** — they show the correct architecture for replacing five broken legacy render calls with a pipeline-agnostic abstraction. Method bodies are not implemented (marked `TODO`), but every architectural decision is documented inline.

---

## The Problem Being Solved

Five classes in iDaVIE use `OnRenderObject()` and `Graphics.DrawProceduralNow()` — Built-In RP callbacks that are **silently ignored by URP and HDRP**. On Unity 6 the mask point cloud, catalogue points, feature boxes, wireframe outlines, and video watermark would all disappear without any error message.

| File | Legacy Call | Line |
|---|---|---|
| `VolumeDataSetRenderer.cs` | `OnRenderObject()` + `DrawProceduralNow` | 1142, 1148 |
| `CatalogDataSetRenderer.cs` | `OnRenderObject()` + `DrawProceduralNow` | 669, 677 |
| `FeatureSetRenderer.cs` | `OnRenderObject()` | 555 |
| `WorldSpaceLineRenderer.cs` | `OnRenderObject()` | 287 |
| `VideoCameraController.cs` | `OnRenderImage()` + `Graphics.Blit` | 511, 513 |

---

## The Three Files

### IRenderPipeline.cs — The Interface
The stable contract. 4 members (within the ≤7 ISP target from the brief):
- `SubmitProceduralDraw()` — replaces `DrawProceduralNow`
- `SubmitBlit()` — replaces `Graphics.Blit` / `OnRenderImage`
- `IsReady` — guard before submitting draws
- `PipelineName` — diagnostics

No URP or HDRP types anywhere in this file. All callers depend only on this interface.

### UrpRenderPipeline.cs — URP Concrete Implementation
Uses `ScriptableRenderPass` (URP's mechanism). Two inner passes:
- `MaskPointCloudPass` — executes `cmd.DrawProcedural()` at `AfterRenderingOpaques`
- `BlitWatermarkPass` — executes `Blitter.BlitCameraTexture()` at `AfterRenderingTransparents`

### HdrpRenderPipeline.cs — HDRP Concrete Implementation
Uses `CustomPass` (HDRP's mechanism — different from URP). Two inner passes:
- `MaskPointCloudCustomPass` — executes `ctx.cmd.DrawProcedural()` at `BeforeTransparent`
- `BlitWatermarkCustomPass` — executes `HDUtils.BlitCameraTexture()` at `BeforePostProcess`

---

## Key Design Point

The two concrete classes are **interchangeable** behind `IRenderPipeline`. Switching from URP to HDRP requires only swapping which implementation is registered at startup — zero changes to `VolumeMaterialBinder`, `CatalogDataSetRenderer`, or any other caller.

```
CALLER                         INTERFACE           CONCRETE
──────                         ─────────           ────────
VolumeMaterialBinder  ──────► IRenderPipeline ──► UrpRenderPipeline
CatalogDataSetRenderer          (stable)       OR  HdrpRenderPipeline
FeatureSetRenderer                             (swapped at composition root)
VideoCameraController
```

---

## Before vs After (VolumeDataSetRenderer mask draw call)

**Before:**
```csharp
// VolumeDataSetRenderer.cs line 1142 — silently does nothing in URP
void OnRenderObject()
{
    _maskMaterialInstance.SetPass(0);
    Graphics.DrawProceduralNow(MeshTopology.Points,
        _maskDataSet.ExistingMaskBuffer.count);
}
```

**After:**
```csharp
// VolumeMaterialBinder.cs — works on both URP and HDRP
_renderPipeline.SubmitProceduralDraw(
    _maskMaterialInstance,
    _maskDataSet.ExistingMaskBuffer,
    _maskDataSet.ExistingMaskBuffer.count,
    transform.localToWorldMatrix);
```

---

*Sub-team 3 — Rendering Engine — Team Alpha — Sprint 2*
