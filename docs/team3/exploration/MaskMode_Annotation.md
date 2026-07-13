# VolumeDataSetRenderer — MaskMode Annotation
**Sub-team 3 — Rendering Engine — Sprint 1**  
**Note:** `VolumeDataSetRendererMaskMode.cs` does not exist as a standalone file. The `MaskMode` enum is defined at line 47 of `VolumeDataSetRenderer.cs`. This document covers what it is, how it works, and what it should become.

---

## What Is a Mask?

A mask is a **second 3D data cube** the same dimensions as the science FITS cube. Where the science cube stores brightness values (floats), the mask cube stores **integer source labels** (Int16 / short).

```
Science cube:   [0.4] [1.2] [0.8] [0.1] [2.3]  ← radio brightness values
Mask cube:      [  0] [  3] [  3] [  0] [  7]  ← source IDs (0 = background)
```

- `0` = background — this voxel belongs to no source
- Any non-zero value = this voxel belongs to a named astronomical source

Astronomers use the mask to **annotate the 3D volume** — identifying individual galaxies or gas clouds, measuring their properties, and editing their boundaries by painting voxels in VR with the controller.

---

## What Is Ray Marching? (Context for Mask Rendering)

Ray marching is how iDaVIE renders the volume. For every pixel on screen, the GPU fires a ray from the camera into the 3D data cube and walks along it in small steps. At each step it samples the voxel value and accumulates brightness. The total becomes the colour of that pixel.

Think of it as **shining a torch through fog** — you see the combined brightness of everything the light passes through. More steps = smoother image but more GPU cost. This is why the step count is carefully managed (foveated rendering drops to 64 steps in peripheral vision to hit the 90 FPS target).

The mask is a second 3D texture the ray sampler checks at every step alongside the science data — asking both "how bright is this voxel?" and "does this voxel belong to a source?"

---

## The Current MaskMode Enum

**Location:** `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs`, line 47

```csharp
public enum MaskMode
{
    Disabled = 0,
    Enabled  = 1,
    Inverted = 2,
    Isolated = 3
}
```

Four values. No behaviour. The integer values map directly to constants in the shader.

### What Each Mode Does

| Mode | Integer | What the Astronomer Sees |
|---|---|---|
| `Disabled` | 0 | Mask ignored — full science cube rendered normally |
| `Enabled` | 1 | Only voxels where mask > 0 are rendered (sources only, background hidden) |
| `Inverted` | 2 | Only voxels where mask == 0 are rendered (background only, sources hidden) |
| `Isolated` | 3 | Full cube rendered but masked voxels highlighted as a flat colour overlay |

These modes let the astronomer inspect their sources from different perspectives in VR — switching modes in real time during a session.

---

## How MaskMode Is Used Today

### In Update() — VolumeDataSetRenderer.cs (~line 1073)

```csharp
if (_maskDataSet != null)
{
    _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());
    // ... voxel offset matrix maths for mask point cloud
}
else
{
    _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.Disabled.GetHashCode());
}
```

The mode is pushed as an integer uniform to the shader every frame. If no mask is loaded (`_maskDataSet == null`), it forces `Disabled` regardless of what `MaskMode` is set to.

### In the Shader — BasicVolume.cginc

The shader receives the integer and branches on it inside the ray marching loop:

```hlsl
#define MASK_DISABLED  0
#define MASK_ENABLED   1
#define MASK_INVERTED  2
#define MASK_ISOLATED  3
```

Because there are also two projection modes (MIP and AIP), the shader ends up with **8 near-identical loop bodies** — one for every combination of projection mode and mask mode. This is the biggest code duplication issue in the entire renderer.

---

## Problems with the Current Design

### 1. Violates Open/Closed Principle (OCP)
Adding a fifth mask mode (e.g. `Highlight` — show sources in colour while background stays greyscale) requires:
- Editing the `enum` in `VolumeDataSetRenderer.cs`
- Editing the `if/else` block in `Update()`
- Editing `BasicVolume.cginc` to add another branch
- Editing `VolumeMask.cginc` for the point cloud behaviour

Four files touched for one new mode. This is the definition of a closed system that should be open for extension.

### 2. No Behaviour Encapsulation
The enum is pure data. The logic of *what each mode means* — which shader integer to push, whether alpha blending changes, how the mask point cloud should render — is scattered across `Update()`, the shader, and `PaintMenuController.cs`.

### 3. Not Testable
You cannot unit test the behaviour of `MaskMode.Isolated` in isolation. The only way to verify it works is to run the full Unity scene with a loaded FITS file.

---

## The Refactoring Target — IMaskMode Strategy Pattern

The assignment brief mandates replacing the enum with an OCP-compliant Strategy pattern. Each mode becomes its own class implementing a shared interface.

### Proposed Interface

```csharp
public interface IMaskMode
{
    int ShaderModeIndex { get; }       // Integer pushed to MaskMode uniform
    bool RequiresAlphaBlend { get; }   // Whether mask material needs blend state change
    void Apply(Material volumeMat, Material maskMat, IVolumeDataSet mask);
}
```

### Four Concrete Implementations

```csharp
public class MaskDisabled : IMaskMode
{
    public int ShaderModeIndex => 0;
    public bool RequiresAlphaBlend => false;
    public void Apply(Material volumeMat, Material maskMat, IVolumeDataSet mask)
        => volumeMat.SetInt(MaterialID.MaskMode, 0);
}

public class MaskEnabled : IMaskMode
{
    public int ShaderModeIndex => 1;
    public bool RequiresAlphaBlend => false;
    public void Apply(Material volumeMat, Material maskMat, IVolumeDataSet mask)
        => volumeMat.SetInt(MaterialID.MaskMode, 1);
}

public class MaskInverted : IMaskMode
{
    public int ShaderModeIndex => 2;
    public bool RequiresAlphaBlend => false;
    public void Apply(Material volumeMat, Material maskMat, IVolumeDataSet mask)
        => volumeMat.SetInt(MaterialID.MaskMode, 2);
}

public class MaskIsolated : IMaskMode
{
    public int ShaderModeIndex => 3;
    public bool RequiresAlphaBlend => true;
    public void Apply(Material volumeMat, Material maskMat, IVolumeDataSet mask)
    {
        volumeMat.SetInt(MaterialID.MaskMode, 3);
        maskMat.SetFloat(MaterialID.MaskVoxelSize, ...);
    }
}
```

### How VolumeMaterialBinder Uses It

```csharp
// Before (scattered in Update())
_materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());

// After (OCP compliant)
_activeMaskMode.Apply(_materialInstance, _maskMaterialInstance, _maskDataSet);
```

Adding a new mode now requires only a new class implementing `IMaskMode`. No existing code changes.

---

## CK Metrics Impact

| Metric | Before (in monolith) | After (Strategy pattern) |
|---|---|---|
| WMC on VolumeDataSetRenderer | ~60 (mask logic adds ~5 methods) | Removed entirely |
| CBO on VolumeDataSetRenderer | ~32 (enum + direct shader calls) | 1 (depends on IMaskMode interface only) |
| NOC on IMaskMode | 0 (no interface exists) | 4 (one per mode) |
| Testability | Not testable without Unity runtime | Each mode unit testable in edit mode |

---

## Where Mask Data Lives in the Codebase

| Concern | Location |
|---|---|
| Mask loaded from FITS | `VolumeDataSetRenderer._startFunc()` |
| Mask uploaded to GPU | `VolumeDataSetRenderer.GenerateDownsampledCube()` → `VolumeTextureManager` (target) |
| Mask mode pushed to shader | `VolumeDataSetRenderer.Update()` → `VolumeMaterialBinder` (target) |
| Mask painted by user | `VolumeDataSetRenderer.PaintMask()` → `MaskEditor` (target) |
| Mask saved to FITS | `VolumeDataSetRenderer.SaveMask()` → `MaskPersistenceService` (target) |
| Mask rendered as point cloud | `VolumeDataSetRenderer.OnRenderObject()` + `VolumeMask.shader` |
| Mode toggled by user | `PaintMenuController.cs`, `QuickMenuController.cs` |

---

*Annotation by Sub-team 3 — Rendering Engine — Team Alpha*
