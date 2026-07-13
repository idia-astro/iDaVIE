# URP / HDRP No-Op Analysis — iDaVIE Rendering Code

**Date:** 2026-05-28  
**Branch:** Team3-Docs-and-Examples  
**Prepared by:** Team 3 (Rendering Engine, Team Alpha)

---

## Summary

The codebase currently uses Unity's **Built-In Render Pipeline** APIs in four files. These APIs are either completely ignored or unavailable under **URP** (Universal Render Pipeline) and **HDRP** (High Definition Render Pipeline). The result is that several rendering features **silently produce no output** when running on either of those pipelines — no errors, no warnings, just missing visuals.

The refactoring stubs already provided in `refactoring-examples/team3/stubs/` (`IRenderPipeline`, `UrpRenderPipeline`, `HdrpRenderPipeline`, `NullRenderPipeline`) are the intended fix. The four callsites below are the concrete migration targets.

---

## Affected Files and What Breaks

### 1. `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` — Line 1142

**Problem:** `OnRenderObject()` is never called by URP or HDRP.  
**Impact:** The **mask point cloud never renders** under either pipeline.

```csharp
void OnRenderObject()  // ← silently skipped by URP/HDRP
{
    if (IsFullResolution && DisplayMask && _maskDataSet?.ExistingMaskBuffer != null)
    {
        _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.ExistingMaskBuffer);
        _maskMaterialInstance.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.ExistingMaskBuffer.count);
    }
    if (IsFullResolution && DisplayMask && _maskDataSet?.AddedMaskBuffer != null && _maskDataSet?.AddedMaskEntryCount > 0)
    {
        _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.AddedMaskBuffer);
        _maskMaterialInstance.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.AddedMaskEntryCount);
    }
}
```

**Fix:** Move draw calls into `_renderPipeline.SubmitProceduralDraw(...)`, consumed inside `MaskPointCloudPass.Execute()` (URP) or `MaskPointCloudCustomPass.Execute()` (HDRP).

---

### 2. `Assets/Scripts/FeatureData/FeatureSetRenderer.cs` — Line 555

**Problem:** `OnRenderObject()` is never called by URP or HDRP.  
**Impact:** **Feature bounding boxes never render** under either pipeline.

```csharp
void OnRenderObject()  // ← silently skipped by URP/HDRP
{
    _materialInstance.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
    _materialInstance.SetBuffer("inputData", _computeBufferVertices);
    _materialInstance.SetPass(0);
    Graphics.DrawProceduralNow(MeshTopology.Lines, FeatureList.Count * VerticesPerFeature);
}
```

**Fix:** Same pattern — delegate to `_renderPipeline.SubmitProceduralDraw(...)` with the appropriate material and buffer.

---

### 3. `Assets/Scripts/CatalogData/CatalogDataSetRenderer.cs` — Line 669

**Problem:** `OnRenderObject()` is never called by URP or HDRP.  
**Impact:** **Catalog point clouds never render** under either pipeline.

```csharp
void OnRenderObject()  // ← silently skipped by URP/HDRP
{
    _catalogMaterial.SetMatrix(_idDataSetMatrix, transform.localToWorldMatrix);
    _catalogMaterial.SetFloat(_idScalingFactor, transform.localScale.x);
    _catalogMaterial.SetPass(DataMapping.Spherical ? 1 : 0);
    Graphics.DrawProceduralNow(MeshTopology.Points, _dataSet.N);
}
```

**Fix:** Delegate to `_renderPipeline.SubmitProceduralDraw(...)`.

---

### 4. `Assets/Scripts/VideoMaker/VideoCameraController.cs` — Line 511

**Problem:** `OnRenderImage()` is never called by URP or HDRP.  
**Impact:** The **video watermark is never composited** and **frame capture is broken** under either pipeline.

```csharp
private void OnRenderImage(RenderTexture source, RenderTexture destination)  // ← silently skipped by URP/HDRP
{
    Graphics.Blit(source, destination, _logoMaterial);
    // ... frame capture logic (also never runs)
}
```

**Fix:** Delegate to `_renderPipeline.SubmitBlit(source, destination, _logoMaterial)`, consumed inside `BlitWatermarkPass.Execute()` (URP, using `Blitter.BlitCameraTexture`) or `BlitWatermarkCustomPass.Execute()` (HDRP, using `HDUtils.BlitCameraTexture`). Note: `Graphics.Blit` must not be used inside URP/HDRP render passes.

---

### Additional note: `WorldSpaceLineRenderer.cs` — Line 287

`OnRenderObject()` is also used here with `GL.PushMatrix` / `GL.PopMatrix` / `GL.MultMatrix` calls for shape/line rendering. GL immediate mode calls are unreliable in URP and unsupported in HDRP without being inside a proper render pass. This should be treated as a lower-priority migration but is still broken under both pipelines.

---

## Why `Graphics.DrawProceduralNow` Must Go

Even if `OnRenderObject` were somehow re-wired, `Graphics.DrawProceduralNow` was **removed from the URP RenderGraph path** (Unity 6+). It must be replaced with `CommandBuffer.DrawProcedural` called inside:

- **URP:** `ScriptableRenderPass.Execute()` via `CommandBufferPool`
- **HDRP:** `CustomPass.Execute()` using `ctx.cmd` directly (do not use `CommandBufferPool` in HDRP)

---

## Migration Pattern

The stubs in `refactoring-examples/team3/stubs/` define the full architecture. The short version:

1. Each renderer holds a reference to `IRenderPipeline` (injected, not hard-coded).
2. Instead of calling `Graphics.DrawProceduralNow(...)` directly, call `_renderPipeline.SubmitProceduralDraw(material, buffer, count, matrix)`.
3. `UrpRenderPipeline` queues the draw into `MaskPointCloudPass` and executes it at `AfterRenderingOpaques`.
4. `HdrpRenderPipeline` queues it into `MaskPointCloudCustomPass` at `BeforeTransparent`.
5. `NullRenderPipeline` (all no-ops) is used in edit-mode unit tests — no GPU required.

---

## Files to Change

| File | Lines | Issue | Replacement |
|---|---|---|---|
| `VolumeDataSetRenderer.cs` | 1142–1155 | `OnRenderObject` + `DrawProceduralNow` | `SubmitProceduralDraw` |
| `FeatureSetRenderer.cs` | 555–563 | `OnRenderObject` + `DrawProceduralNow` | `SubmitProceduralDraw` |
| `CatalogDataSetRenderer.cs` | 669–677 | `OnRenderObject` + `DrawProceduralNow` | `SubmitProceduralDraw` |
| `VideoCameraController.cs` | 511–530 | `OnRenderImage` + `Graphics.Blit` | `SubmitBlit` |
| `WorldSpaceLineRenderer.cs` | 287–310 | `OnRenderObject` + GL calls | Lower priority — needs custom pass |

---

## Reference Stubs

```
refactoring-examples/team3/stubs/
├── IRenderPipeline.cs       ← interface all renderers depend on
├── UrpRenderPipeline.cs     ← URP concrete implementation (ScriptableRenderPass)
├── HdrpRenderPipeline.cs    ← HDRP concrete implementation (CustomPass)
└── NullRenderPipeline.cs    ← test double (all no-ops, for unit tests)
```
