# VolumeMaterialBinder — Design Decisions

*Sub-team 3 | Refactoring Example 1 | 2026-05-26*

---

## What this file is

A short record of the key design calls made when drafting `VolumeMaterialBinder.cs`, so reviewers and future sprints can see *why* things are shaped the way they are, not just *what* they are.

---

## Decision 1 — Three declarations, one file

`VolumeMaterialBinder.cs` contains `VolumeRenderState`, `IVolumeMaterialBinder`, and `VolumeMaterialBinder` in the same file. They are cohesive enough that separating them would just add navigation friction — the struct and interface only make sense next to the class that uses them.

---

## Decision 2 — `VolumeRenderState` as a readonly struct

All per-frame shader data is bundled into a single value struct that the coordinator builds and passes to `Tick()`. The alternatives were rejected for these reasons:

- **Individual parameters** — `Tick(float threshMin, float threshMax, ...)` would need 20+ arguments and would change signature every time a new uniform is added. That is an OCP violation at the call site.
- **Mutable reference object** — a class would allow the coordinator to mutate the state after passing it, causing hard-to-reproduce frame-timing bugs.
- **Readonly struct** — immutable by construction, zero heap allocation per frame, and adding a new uniform only requires a new field here; `Tick()`'s call sites in the coordinator are unaffected.

---

## Decision 3 — `IVolumeMaterialBinder` capped at 7 members

The interface fixes V-06 (ISP): `VolumeDataSetRenderer` had 152 public members, and consumers like `PaintMenuController` depended on the whole surface to use three of them. The 7-member contract is:

| # | Member | Replaces |
|---|--------|---------|
| 1 | `Initialise` | Material setup in `_startFunc()` |
| 2 | `Tick` | Shader uniform block in `Update()` |
| 3 | `BindDataTexture` | `SetTexture(DataCube)` scattered across `CropToRegion`, `ResetCrop` |
| 4 | `BindMaskTexture` | `SetTexture(MaskCube)` scattered across `_startFunc`, `InitialiseMask` |
| 5 | `SetActiveMaskMode` | The mask-mode `switch` statement |
| 6 | `SubmitMaskGeometry` | `OnRenderObject + Graphics.DrawProceduralNow` |
| 7 | `Dispose` | Material lifecycle (was implicit / leaked) |

`BindMaskCropUniforms` and `ApplyToRenderer` are public on the concrete class but not on the interface — they are coordinator-only plumbing, not consumer-facing contract.

---

## Decision 4 — `ShaderID` is a private nested class

In the original code, `MaterialID` lived inside the 1,402-line God Class and was technically visible to every method in the file. Moving it to a `private static class ShaderID` inside `VolumeMaterialBinder` means raw property IDs are invisible outside this class. Callers can never accidentally push a shader property from `VolumeTextureManager` or `VolumeCameraDriver` — the compiler enforces the access boundary.

---

## Decision 5 — Mask mode via `IMaskMode.Apply()`, not `SetInt`

The original code called `_materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode())`, relying on the enum integer matching a shader `#define`. Adding a new mask mode meant editing the enum, `Update()`, `BasicVolume.cginc`, and `PaintMenuController.cs` — four files for one new behaviour (V-04, OCP violation).

With the Strategy pattern, `_activeMaskMode.Apply(material, maskTexture)` dispatches polymorphically. The binder has no knowledge of which mode is active. Adding a new mode means writing a new class that implements `IMaskMode`; nothing in `VolumeMaterialBinder` changes.

---

## Decision 6 — Projection keyword via `IRenderPipeline.SetPipelineKeyword`

The original code called `Shader.EnableKeyword("SHADER_AIP")` — a global keyword that URP deprecates in favour of material-local `LocalKeyword`. Routing the call through `IRenderPipeline.SetPipelineKeyword` means:

- The URP adapter (`UrpRenderPipeline`) can call the correct local-keyword API.
- The built-in RP adapter can call the global API.
- `VolumeMaterialBinder` changes nothing when the pipeline is swapped.

This also fixes V-15 (GRASP Protected Variations): the render-pipeline variation point is now protected by an interface.

---

## Decision 7 — `SubmitMaskGeometry` replaces `OnRenderObject`

`OnRenderObject` is a Unity callback that is not invoked by the URP render loop. The mask point-cloud draw (`Graphics.DrawProceduralNow`) was therefore silently broken on any URP project. `SubmitMaskGeometry` delegates to `IRenderPipeline.SubmitProceduralDraw`, which the `UrpRenderPipeline` adapter implements using `CommandBuffer.DrawProcedural` inside a `ScriptableRenderPass`. The domain code is unchanged between pipelines.

---

## Projected CK delta

| Metric | Before (VDSR) | After (this class) | Target | Status |
|--------|-------------|-------------------|--------|--------|
| WMC | 74 | 16 | ≤ 20 | ✅ |
| CBO | 31 | ≤ 11 | ≤ 14 | ✅ |
| RFC | 89 | ≤ 22 | ≤ 50 | ✅ |
| LCOM | 0.81 | ≈ 0.05 | ≤ 0.5 | ✅ |
