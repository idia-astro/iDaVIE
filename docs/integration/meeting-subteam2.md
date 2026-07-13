# Quick Sync — Sub-team 3 × Sub-team 2 (Data I/O)
**Thu 4 June 2026 | ~5 min | Cathal (Tech Lead, Team 3)**

> ✅ **RESOLVED — 2 June 2026.** Two field types corrected per Sub-team 2 feedback. See outcome table below.

---

## Context

Our `VolumeTextureManager` (refactoring example 1) needs to receive voxel data from your layer. Right now we have a placeholder that still depends on `VolumeDataSet` directly. We need to swap that out for a shared `RawVolumeData` struct — one that both teams code against.

---

## What we proposed

A simple struct your layer produces and ours consumes:

```csharp
public readonly struct RawVolumeData
{
    public float[]    Voxels     { get; init; }   // flat row-major array
    public int        XDim       { get; init; }
    public int        YDim       { get; init; }
    public int        ZDim       { get; init; }
    public DataFormat Format     { get; init; }   // Float32, Int16, etc.
}
```

Constructor: `(VolumeTextureConfig config, RawVolumeData dataCube, RawVolumeData? maskCube)`

---

## Agreed outcome — 2 June 2026

| # | Question | Agreed answer |
|---|----------|---------------|
| 1 | Voxel element type | `byte[]` — data cube is `Float32` (`FitsReadSubImageFloat`), mask cube is `Int16` (`FitsReadSubImageInt16`). Cannot share a `float[]` without silent upcast. `DataFormat` enum tells the consumer how to interpret the bytes. |
| 2 | Dimension types | `long` — `VolumeDataSet` already uses `long`; astronomical cubes can exceed `int` range. Using `int` would create a silent narrowing conversion at the boundary. |
| 3 | Downsample factors | We compute them — Sub-team 2 does not pre-compute. |
| 4 | Mask cube type | Same `RawVolumeData` struct, nullable. ✅ Matched our assumption. |

Confirmed struct:

```csharp
public readonly struct RawVolumeData
{
    public byte[]      Voxels  { get; init; }  // interpret via Format
    public long        XDim    { get; init; }
    public long        YDim    { get; init; }
    public long        ZDim    { get; init; }
    public DataFormat  Format  { get; init; }  // Float32 | Int16
}
```

Constructor shape confirmed: `(VolumeTextureConfig config, RawVolumeData dataCube, RawVolumeData? maskCube)` ✅

**`VolumeTextureManager.cs` placeholder comment updated to reflect confirmed contract.**

Sprint 3 TODO: swap `VolumeDataSet` constructor params for `RawVolumeData`; branch on `DataFormat` for `Texture3D.SetPixelData<float>` vs `SetPixelData<short>`.
