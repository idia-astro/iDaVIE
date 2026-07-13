# VolumeTextureManager — Design Decisions

*Sub-team 3 | Refactoring Example 1 | 2026-05-26*

---

## What this file is

A record of the key design calls made when drafting `VolumeTextureManager.cs`, so
reviewers and future sprints can see *why* things are shaped the way they are, not
just *what* they are. Companion to `VolumeMaterialBinder-decisions.md` in the same
folder.

---

## Why this class exists at all

In `VolumeDataSetRenderer`, GPU texture management is one of at least eight
responsibilities crammed into a single 1,402-line class. The relevant code is
scattered across:

| Before/ location | What it does |
|-----------------|-------------|
| `_startFunc()` lines 479–513 | Calls `GenerateVolumeTexture`, uploads initial data cube |
| `GenerateDownsampledTexture()` lines 657–681 | Computes downsample factors, calls `GenerateVolumeTexture` |
| `LoadRegionData()` lines 1057–1078 | Calls `GenerateCroppedVolumeTexture` for data + mask |
| `ResetCrop()` lines 1024–1054 | Decides whether to re-upload full cube or cropped cube |
| `InitialiseMask()` line 1310 | Calls `GenerateVolumeTexture` for mask data set |
| Fields lines 299–320 | `MaximumCubeSizeInMB`, downsample factors, crop state |

None of this code shares fields with the shader-binding code or the camera-maths
code — it is a textbook LCOM cluster living in the wrong class (V-01, SRP). Pulling
it into `VolumeTextureManager` eliminates those field clusters from VDSR entirely,
which is why LCOM is expected to drop from ~0.81 to ~0.14 across the split as a
whole.

---

## Decision 1 — Sole ownership of `Texture3D` creation

`VolumeTextureManager` is the only class in `iDaVIE.Rendering` that may:

- Call `VolumeDataSet.GenerateVolumeTexture()`
- Call `VolumeDataSet.GenerateCroppedVolumeTexture()`
- Allocate or release a `Texture3D` object
- Apply the `FilterMode` to a texture at creation time

No other class creates or resizes a `Texture3D`. `VolumeMaterialBinder` receives a
finished `Texture3D` reference via `BindDataTexture()` — it never calls any generation
method. This boundary is enforced architecturally: `VolumeMaterialBinder` does not hold
a reference to `VolumeDataSet` and cannot reach the generation methods.

Why this matters: the 4 GB Unity texture limit and the 368 MB default memory budget
(INV-03) are checked once, in one place. There is no risk of a second path to texture
creation that bypasses the budget guard.

---

## Decision 2 — The `IVolumeTextureManager` interface shape

The interface exposes six members, chosen to cover every call site in the coordinator
without leaking `VolumeDataSet`, `FilterMode`, or raw dimension integers:

| # | Member | Replaces |
|---|--------|---------|
| 1 | `Initialise(VolumeDataSet dataSet, VolumeDataSet maskDataSet)` | Texture generation inside `_startFunc()` lines 479–521 |
| 2 | `LoadFullCube()` | Re-upload after `ResetCrop()` lines 1024–1054 |
| 3 | `LoadRegion(Vector3Int start, Vector3Int end)` | `LoadRegionData()` lines 1057–1078 |
| 4 | `GenerateDownsampled()` | `GenerateDownsampledTexture()` lines 657–681 |
| 5 | `CurrentDataTexture` (property) | `_dataSet.DataCube` / `_dataSet.RegionCube` — the ready-to-bind result |
| 6 | `Dispose()` | No equivalent — texture lifecycle was never explicitly managed before |

`VolumeRenderCoordinator` depends only on this interface. It never imports
`VolumeDataSet` for texture purposes; the data-set reference is held entirely inside
`VolumeTextureManager`. This breaks one of the two strongest coupling edges in the
original VDSR dependency graph and is the primary driver of the projected CBO
reduction for the coordinator.

---

## Decision 3 — Memory budget enforcement lives here, nowhere else

The original `MaximumCubeSizeInMB` field (before/ line 68) is a public Inspector field
on `VolumeDataSetRenderer`. Any method in the 1,402-line class can read or mutate it.
The actual budget check is only performed inside `GenerateDownsampledTexture()` — but
because the field is public, nothing prevents a caller from uploading an oversized
texture through a different path.

In the after/ design, the memory budget is a constructor parameter to
`VolumeTextureManager` (sourced from a `ScriptableObject` config asset, same as
`FoveatedSamplingConfig`). The budget guard runs inside every `Load*` method before any
`GenerateVolumeTexture` call. The coordinator cannot bypass it because it never has
access to the generation API directly.

This also means the budget figure appears in exactly one place in the codebase. If the
maintainer team change the default from 368 MB to 512 MB, they change one config asset,
not a public field on a God Class.

---

## Decision 4 — Filter mode is fixed at construction, not a per-frame uniform

`FilterMode.Point` (nearest-neighbour) is an invariant of the iDaVIE rendering contract
(INV-04): the blocky voxel appearance is scientifically intentional — interpolated
textures would misrepresent the underlying FITS data. In the original code,
`TextureFilter = FilterMode.Point` is a public Inspector field that a developer could
accidentally change to `FilterMode.Bilinear`.

`VolumeTextureManager` accepts the filter mode at construction time and applies it
inside every generation call. It is not exposed on `IVolumeTextureManager` — consumers
cannot query or change it at runtime. If a future requirement introduces a legitimate
second filter mode (e.g. bilinear for a scientific preview mode), that is a new
constructor parameter and a deliberate API decision, not an Inspector checkbox.

---

## Decision 5 — `VolumeTextureManager` is not `VolumeMaterialBinder`

The most important question during design was whether to fold texture creation into
`VolumeMaterialBinder`, since the two classes hand off `Texture3D` between them. We
chose to keep them separate for two reasons:

**Reason A — different rates of change.** Shader property names and uniform values
change whenever the GLSL shader is edited. Texture memory budgets and downsample
factors change when the target hardware platform changes. These are independent
variation axes. Merging the classes would mean a shader change touches texture
logic and vice versa — exactly the SRP problem we are solving.

**Reason B — testability.** `VolumeTextureManager` can be unit-tested in edit mode
without a GPU context: it calls `VolumeDataSet` generation methods whose outputs can
be stubbed. `VolumeMaterialBinder` needs a GPU to call `Material.SetTexture`. Keeping
them separate means each can be tested independently. A test for texture budget
enforcement does not require a material instance.

The handoff is clean: `VolumeRenderCoordinator` calls
`textureManager.LoadRegion(start, end)`, reads `textureManager.CurrentDataTexture`,
and passes it to `materialBinder.BindDataTexture(...)`. Neither class knows the other
exists.

---

## Decision 6 — Crop state is owned here, not on the coordinator

`IsCropped`, `CurrentCropMin`, and `CurrentCropMax` (before/ lines 319–321) are public
properties on `VolumeDataSetRenderer`. At least four methods read them; `CropToRegion`
and `ResetCrop` mutate them. In the after/ design, these live as private fields inside
`VolumeTextureManager`, because they are consequences of which texture is currently
loaded. A crop is simply the state of the texture manager — the coordinator does not
need to know about it except to query `IsCropped` when deciding whether to call
`LoadFullCube()` or `LoadRegion()`.

Exposing `IsCropped` as a read-only property on `IVolumeTextureManager` is the one
concession to the coordinator's decision-making needs. `CurrentCropMin` /
`CurrentCropMax` are not on the interface — if the coordinator or another class needs
them (e.g. for moment-map bounds), that is a separate query method to be added when
the need is confirmed, not speculatively.

---

## Decision 7 — `RawVolumeData` is the Sub-team 2 boundary

The current code calls `VolumeDataSet.GenerateVolumeTexture()` and
`VolumeDataSet.GenerateCroppedVolumeTexture()` directly, coupling the renderer to the
full `VolumeDataSet` class (including FITS I/O, WCS, and moment-map state). This is one
of the largest coupling edges in the VDSR dependency graph.

In the target architecture, Sub-team 2 (Data I/O) will publish a `RawVolumeData` struct
containing only the fields `VolumeTextureManager` needs: the voxel array, the dimensions
(X, Y, Z), and a `TextureFormat`. When that contract arrives, `VolumeTextureManager`'s
`Initialise()` signature changes from `(VolumeDataSet, VolumeDataSet)` to
`(RawVolumeData dataCube, RawVolumeData? maskCube)` — and the `VolumeDataSet` coupling
edge is severed entirely.

Until Sub-team 2 confirms the interface, the `VolumeDataSet` version is used as a
placeholder and is clearly annotated in `VolumeTextureManager.cs` (same pattern as the
`IGazeProvider` placeholder in `FoveatedSamplingPolicy.cs`). This keeps the blocker
visible without blocking drafting.

---

## Projected CK delta

| Metric | Before (VDSR) | After (this class) | Target | Status |
|--------|-------------|-------------------|--------|--------|
| WMC | 74 | ~12 | ≤ 20 | ✅ |
| CBO | 31 | ≤ 8 | ≤ 14 | ✅ |
| RFC | 89 | ≤ 18 | ≤ 50 | ✅ |
| LCOM | 0.81 | ~0.05 | ≤ 0.5 | ✅ |

WMC breakdown (estimated):
- `Initialise` → complexity 2 (mask null check)
- `LoadFullCube` → complexity 2 (budget check)
- `LoadRegion` → complexity 3 (budget check + mask branch)
- `GenerateDownsampled` → complexity 3 (factor computation branches)
- `CurrentDataTexture` property → complexity 1
- `Dispose` → complexity 1
- Total ≈ 12 ✅ (target ≤ 20)

CBO sources (estimated ≤ 8):
- `UnityEngine` (Texture3D, FilterMode, Vector3Int)
- `VolumeDataSet` (placeholder until `RawVolumeData` arrives)
- `IVolumeTextureManager` (our interface)
- Internal config struct (memory budget + filter mode)
