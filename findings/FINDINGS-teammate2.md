# Persistence Sub-team Findings — Teammate 2
## State Contract Analysis: Interaction & Rendering

**Prepared for:** Persistence & Workspace State Sub-team  
**Date:** 2026-05-27  
**Source contracts analysed:**
- `state-contracts/interaction-state-contract.md`
- `state-contracts/rendering-state-contract.md`

**Primary source files read:**
- `Assets/Scripts/VolumeData/VolumeInputController.cs`
- `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs`
- `Assets/Scripts/VolumeData/VolumeDataSet.cs`
- `Assets/Scripts/VolumeData/Config.cs`
- `Assets/Scripts/UI/Menus/RenderingController.cs`
- `Assets/Scripts/Tools/ColorMapEnum.cs`
- `Assets/Scripts/VolumeData/MomentMapRenderer.cs`
- `Assets/Scripts/VideoMaker/VideoPosRecorder.cs`
- `Assets/Scripts/Menu/QuickMenuController.cs`
- `Assets/Scripts/Menu/PaintMenuController.cs`

---

---

# Section 1 — Interaction State Contract

---

## State inventory (field name, type, required/optional)

### 1.1 Top-level DTO (as declared in contract)

| Field | Type | Required/Optional |
|---|---|---|
| `InteractionStateDto.Locomotion` | `LocomotionStateDto` | Required |
| `InteractionStateDto.ActiveInteractionMode` | `string` | Required |
| `InteractionStateDto.ActiveToolSelection` | `string` | Optional |

The contract declares `LocomotionStateDto` as a nested DTO but does **not** define its fields inline. The following fields are inferred from `VolumeInputController.cs`.

---

### 1.2 LocomotionState fields (inferred from VolumeInputController.cs)

The `LocomotionState` enum is **private** (line 48–56 of `VolumeInputController.cs`). Its values are:

| Enum Value | Ordinal | Notes |
|---|---|---|
| `Idle` | 0 | Default safe state |
| `Moving` | 1 | User translating volume via grip |
| `Scaling` | 2 | Two-hand scale/rotate gesture active |
| `EditingThresholdMin` | 3 | Voice/button command state for min threshold |
| `EditingThresholdMax` | 4 | Voice/button command state for max threshold |
| `EditingZAxis` | 5 | Voice command state for Z-axis aspect ratio |

No `LocomotionStateDto` class exists in the codebase. The persistence sub-team must define it.

---

### 1.3 InteractionState fields (inferred from VolumeInputController.cs)

The `InteractionState` enum is **public** (line 58–67 of `VolumeInputController.cs`). Its values are:

| Enum Value | Ordinal | Notes |
|---|---|---|
| `IdleSelecting` | 0 | Default, waiting for user interaction |
| `IdlePainting` | 1 | Painting mode active, no active brush stroke |
| `EditingSourceId` | 2 | User selecting source ID to paint with |
| `Creating` | 3 | User drawing a new region selection |
| `Editing` | 4 | User modifying existing region bounds via anchors |
| `Painting` | 5 | Active brush stroke in progress |
| `VideoCamPosRecording` | 6 | Recording camera/cursor positions for video |

`ActiveInteractionMode` in the DTO maps to this enum serialised as a `string`.

---

### 1.4 Inferred additional persistent fields (from VolumeInputController.cs)

These fields are not listed in the contract DTO but represent genuine user-configurable persistent state:

| Field | C# Type | Required/Optional | Source line |
|---|---|---|---|
| `AdditiveBrush` | `bool` | Optional | line 109 |
| `BrushSize` | `int` | Optional | line 110 |
| `SourceId` | `short` | Optional | line 111 |
| `PrimaryHand` | `SteamVR_Input_Sources` (enum) | Optional | line 94 |
| `InPlaceScaling` | `bool` | Optional | line 102 |
| `ScalingEnabled` | `bool` | Optional | line 103 |
| `VignetteFadeSpeed` | `float` | Optional | line 106 |
| `RotationAxisCutoff` | `float` | Optional | line 104 |

`PrimaryHand` is a `SteamVR_Input_Sources` enum but must **not** be serialised as a direct SteamVR reference. It must be wrapped as a string or simple enum value (e.g., `"LeftHand"` / `"RightHand"`) to honour the hardware-abstraction constraint stated in the contract.

---

### 1.5 Explicitly excluded runtime fields (must NOT be serialised)

Per the contract constraint "runtime controller references must never be serialized":

| Field | C# Type | Reason excluded |
|---|---|---|
| `_player` | `Player` (SteamVR) | Unity/SteamVR runtime object |
| `_hand`, `_otherHand` | `VRHand` / `Hand` (SteamVR) | SteamVR runtime object |
| `_currentGripPositions` | `Dictionary<SteamVR_Input_Sources, Vector3>` | Transient gesture state |
| `_hoveredFeature`, `_editingFeature` | `Shape` / `Feature` | Transient selection state |
| `_hoveredAnchor`, `_editingAnchor` | anchor objects | Transient selection state |
| `_videoPositions` | `List<videoRecLocation>` | Exported separately to IDVS files |

---

## Validation rules

| Field | Rule | Enforcement location |
|---|---|---|
| `ActiveInteractionMode` | Must be a valid `InteractionState` enum name; validated via `Enum.IsDefined()` on restore | Not yet enforced in code; must be added |
| `Locomotion.LocomotionState` | Must be a valid `LocomotionState` enum name; validated via `Enum.IsDefined()` on restore | Not yet enforced |
| `BrushSize` | Minimum 1; increments of 2; no enforced upper bound in code (`Math.Max(1, BrushSize - 2)` at line 504) | VolumeInputController line 504 |
| `SourceId` | `-1` = unset; `>= 0` = valid source ID; painting in additive mode gated on `SourceId >= 0` (line 327) | VolumeInputController line 327 |
| `PrimaryHand` | Must be `"LeftHand"` or `"RightHand"` in serialised form | Not yet enforced |
| `VignetteFadeSpeed` | Range `[0.1f, 5.0f]` per `[Range]` attribute at line 106 | Unity Inspector only; must be enforced in deserialiser |
| `InPlaceScaling`, `ScalingEnabled`, `AdditiveBrush` | Boolean; no range constraint | N/A |
| `RotationAxisCutoff` | No explicit bound in code; must be `> 0` to function as angle threshold (used at line 918) | Not enforced |

---

## Default fallbacks

| Field | Safe default | Source |
|---|---|---|
| `ActiveInteractionMode` | `"IdleSelecting"` | VolumeInputController line 309 |
| `Locomotion.LocomotionState` | `"Idle"` | VolumeInputController line 289 |
| `AdditiveBrush` | `true` | VolumeInputController line 109 |
| `BrushSize` | `1` | VolumeInputController line 110 |
| `SourceId` | `-1` | VolumeInputController line 111 |
| `PrimaryHand` | `"RightHand"` | VolumeInputController line 94 |
| `InPlaceScaling` | `true` | VolumeInputController line 102 |
| `ScalingEnabled` | `true` | VolumeInputController line 103 |
| `VignetteFadeSpeed` | `2.0f` | VolumeInputController line 106 |
| `RotationAxisCutoff` | Read from Inspector; no code-level default found | VolumeInputController line 104 |

---

## Recovery behavior

Per the contract:

| Condition | Required recovery action |
|---|---|
| `Locomotion` block missing entirely | Restore `LocomotionState = Idle` |
| `ActiveInteractionMode` missing or invalid enum value | Restore `IdleSelecting`; reset all transient interaction context |
| `ActiveToolSelection` missing | Restore no active tool; treat as null/empty |
| Invalid VR transform state (position/rotation unparseable or unsafe) | Recenter user to origin; disable interaction contexts that depend on spatial state |
| Corrupt `LocomotionState` value (unrecognised enum string) | Restore `Idle`; log warning |
| `SourceId` is corrupt or < -1 | Restore `-1` (unset) |
| `BrushSize` < 1 | Clamp to 1 |
| `VignetteFadeSpeed` out of `[0.1f, 5.0f]` | Clamp to range |
| Any field referencing Unity XR / SteamVR objects | Reject silently; never deserialise into runtime references |

The contract states: "preserve stable user preferences" on recovery. This means `PrimaryHand`, `InPlaceScaling`, `ScalingEnabled`, and `VignetteFadeSpeed` should survive recovery even when the rest of interaction state is reset.

---

## Existing serialization

**None.** There is no serialisation code for interaction state anywhere in the codebase.

- `LocomotionState` (private enum) is never serialised.
- `InteractionState` (public enum) is never serialised.
- `VolumeInputController` has no `Save()`, `Load()`, `ToJson()`, or DTO-mapping method.

The only serialisation infrastructure in the project is in `Config.cs`, which uses `Valve.Newtonsoft.Json` (`JsonConvert.SerializeObject` / `DeserializeObject`, line 229) with `StringEnumConverter` for enums. **This is the correct pattern to adopt for interaction state DTOs.**

---

## Integration notes

1. **LocomotionStateDto must be defined from scratch.** The contract names it but does not specify its fields. The implementation team should derive its fields from the `LocomotionState` private enum in `VolumeInputController.cs` (listed above in §1.2).

2. **ActiveInteractionMode and ActiveToolSelection are `string` in the contract DTO**, not typed enums. This is intentional for forward-compatibility but requires `Enum.IsDefined()` validation on restore. Consider a strongly-typed wrapper that rejects unknown values before passing to the runtime.

3. **SteamVR abstraction gap.** The contract requires `IInputProvider`, `IVoiceRecogniser`, `IHaptics`, and `IPointer` but **none of these interfaces exist in the codebase**. `VolumeInputController` is directly coupled to SteamVR (`using Valve.VR.InteractionSystem` at line 31). The persistence layer must not depend on this coupling. Serialise only the abstract state strings; never the SteamVR concrete types.

4. **User preferences vs session state distinction.** The contract's "preserve stable user preferences" rule implies two save scopes:
   - **User preferences** (survive crashes and fresh loads): `PrimaryHand`, `InPlaceScaling`, `ScalingEnabled`, `VignetteFadeSpeed`, `RotationAxisCutoff`.
   - **Session state** (restored within a session, reset on new session): `ActiveInteractionMode`, `LocomotionState`, `BrushSize`, `SourceId`, `AdditiveBrush`.

5. **TransientState exclusion must be explicit.** Fields like `_currentGripPositions`, `_hoveredFeature`, and `_editingAnchor` are runtime-only. The DTO class boundary is the only enforcement mechanism; the contract must document what is excluded.

6. **Dependency on VolumeDataSet.** Painting state (`SourceId`, `AdditiveBrush`, `BrushSize`) is only meaningful if a dataset is loaded. The persistence layer should apply interaction state **after** dataset state is restored and validated.

---

## Open questions

1. **What fields belong in `LocomotionStateDto`?** The contract names it as a nested type but never defines its fields. Is it just the state enum, or does it also include player world position (`Vector3`) and orientation (`Quaternion`)?

2. **Should player world position/orientation be persisted?** This is spatial state but arises from VR locomotion. Is it owned by Interaction state or Rendering/Spatial state?

3. **Multi-dataset support.** `VolumeInputController` operates on an `ActiveDataSet` reference. If multiple datasets are loaded, is there a per-dataset interaction state or one global interaction state?

4. **What constitutes an "unsafe VR transform"?** The recovery rule says "reset unsafe transforms, recenter user safely to origin." There is no definition of safe/unsafe bounds. Does this mean any non-finite float, or a position outside a defined world-space AABB?

5. **Should `VideoCamPosRecording` mode be restored across sessions?** It is one of the `InteractionState` enum values and would be persisted if active, but restoring into recording mode on load may be unexpected for users.

6. **`RotationAxisCutoff` default value.** No code-level default was found. The value appears to be set only via the Unity Inspector. What is the production default and should it be a user preference or a fixed constant?

7. **Voice command state.** `IVoiceRecogniser` is required by the contract but not implemented. Should confidence-level preferences (`voiceCommandConfidenceLevel` from `Config.cs`) be part of this contract or remain in Config?

8. **Is `ActiveToolSelection` the same concept as `InteractionState`?** The DTO declares both `ActiveInteractionMode` and `ActiveToolSelection` as separate strings. The distinction between them is not clear from the contract. Clarification needed from the Interaction sub-team.

---

---

# Section 2 — Rendering State Contract

---

## State inventory (field name, type, required/optional)

The contract declares five top-level state categories within `VolumeSessionState`. Each is expanded below with actual field names from `VolumeDataSetRenderer.cs`, `VolumeDataSet.cs`, `Config.cs`, and `MomentMapRenderer.cs`.

---

### 2.1 Top-level VolumeSessionState fields

| Field | Type | Required/Optional |
|---|---|---|
| `FitsFilePath` | `string` | Required |
| `Rendering` | `RenderingState` | Required |
| `VolumeData` | `VolumeDataState` | Required |
| `Spatial` | `SpatialState` | Required |
| `Foveation` | `FoveationState` | Optional |
| `Mask` | `MaskState` | Optional |

---

### 2.2 RenderingState fields

| Field | C# Type | Required/Optional | Valid Range / Values | Source |
|---|---|---|---|---|
| `ColorMap` | `ColorMapEnum` (enum, 80+ values) | Required | Any `ColorMapEnum` value; default `Inferno` | VolumeDataSetRenderer line 101 |
| `ScalingType` | `ScalingType` (enum: Linear, Log, Sqrt, Square, Power, Gamma) | Required | 6 values | VolumeDataSetRenderer line 102 |
| `ScalingBias` | `float` | Optional | `[-1.0, 1.0]` | VolumeDataSetRenderer; `[Range(-1,1)]` |
| `ScalingContrast` | `float` | Optional | `[0.0, 5.0]` | VolumeDataSetRenderer; `[Range(0,5)]` |
| `ScalingAlpha` | `float` | Optional | No enforced range; default `1000.0f` | VolumeDataSetRenderer |
| `ScalingGamma` | `float` | Optional | `[0.0, 5.0]` | VolumeDataSetRenderer; `[Range(0,5)]` |
| `ThresholdMin` | `float` | Required | `[0.0, 1.0]`; must be ≤ `ThresholdMax` | VolumeDataSetRenderer line 98 |
| `ThresholdMax` | `float` | Required | `[0.0, 1.0]`; must be ≥ `ThresholdMin` | VolumeDataSetRenderer line 99 |
| `ProjectionMode` | `ProjectionMode` (enum: MaximumIntensityProjection, AverageIntensityProjection) | Required | 2 values; default MIP | VolumeDataSetRenderer line 69 |
| `MaxSteps` | `int` | Optional | `[16, 512]`; default `192` | VolumeDataSetRenderer; `[Range(16,512)]` |
| `Jitter` | `float` | Optional | `[0.0, 1.0]`; default `1.0f` | VolumeDataSetRenderer; `[Range(0,1)]` |
| `TextureFilter` | `FilterMode` (enum: Point, Bilinear) | Optional | 2 values; default Point | VolumeDataSetRenderer; from Config |
| `VignetteFadeStart` | `float` | Optional | `[0.0, 0.5]`; default `0.15f` | VolumeDataSetRenderer |
| `VignetteFadeEnd` | `float` | Optional | `[0.0, 0.5]`; default `0.40f` | VolumeDataSetRenderer; from Config |
| `VignetteIntensity` | `float` | Optional | `[0.0, 1.0]`; default `0.0f` | VolumeDataSetRenderer |
| `VignetteColor` | `Color` (RGBA) | Optional | Any valid Color; default black | VolumeDataSetRenderer |

---

### 2.3 VolumeDataState fields

| Field | C# Type | Required/Optional | Notes | Source |
|---|---|---|---|---|
| `FileName` | `string` | Required | Absolute or relative path to FITS file | VolumeDataSet line 249 |
| `MaskFileName` | `string` | Optional | Path to mask FITS file; empty = no mask | VolumeDataSet |
| `SelectedHdu` | `int` | Optional | HDU index; default `1` | VolumeDataSet lines 261–264 |
| `SubsetBounds` | `int[6]` | Optional | `[xmin, xmax, ymin, ymax, zmin, zmax]`; default `[-1,-1,-1,-1,-1,-1]` = full cube | VolumeDataSet line 110 |
| `IsCropped` | `bool` | Optional | Whether a crop is currently active; default `false` | VolumeDataSet line 939 |
| `CurrentCropMin` | `Vector3Int` | Optional | Current crop region minimum | VolumeDataSet lines 238, 538 |
| `CurrentCropMax` | `Vector3Int` | Optional | Current crop region maximum | VolumeDataSet lines 239, 539 |
| `OverrideRestFrequency` | `bool` | Optional | Whether user is overriding FITS rest frequency | VolumeDataSet line 130 |
| `RestFrequencyGHz` | `double` | Optional | Active rest frequency; only meaningful if `OverrideRestFrequency = true` | VolumeDataSet lines 169–178 |
| `RestFrequencyGHzListIndex` | `int` | Optional | Index into RestFrequencyGHzList; `-1` or `Count` = custom | VolumeDataSet lines 139–167 |

**Read-only after load — do NOT persist, derive from FITS on reload:**

| Field | C# Type | Notes |
|---|---|---|
| `XDim`, `YDim`, `ZDim` | `long` | Set during `LoadDataFromFitsFile()`; must match on restore |
| `MinValue`, `MaxValue`, `MeanValue`, `StanDev` | `float` | Re-derived from data on load |
| `Histogram`, `FullHistogram` | `int[]` | Recalculated; never persist |

---

### 2.4 SpatialState fields

| Field | C# Type | Required/Optional | Notes | Source |
|---|---|---|---|---|
| `Position` | `Vector3` | Required | World position of the volume GameObject | VolumeDataSetRenderer transform |
| `Rotation` | `Quaternion` | Required | World rotation; must be normalised | VolumeDataSetRenderer transform |
| `Scale` | `Vector3` | Required | Local scale; all components must be `> 0` | VolumeDataSetRenderer transform |
| `SliceMin` | `Vector3` | Optional | Normalised cube-space slice minimum; default `-0.5 * Vector3.one` | VolumeDataSetRenderer line 126 |
| `SliceMax` | `Vector3` | Optional | Normalised cube-space slice maximum; default `+0.5 * Vector3.one` | VolumeDataSetRenderer line 126 |

**Note:** `InitialPosition`, `InitialRotation`, `InitialScale` are captured at first load (lines 424–426) and used by the reset function. They should not be persisted as session state — they are computed from the scene defaults. Persisted `Position/Rotation/Scale` represent the user's current view, not the initial anchor.

**Not serialisable:** `UnityEngine.Transform` (MonoBehaviour component). The persistence layer must extract raw `Vector3`/`Quaternion` values and store them as plain DTOs.

---

### 2.5 FoveationState fields

| Field | C# Type | Required/Optional | Valid Range | Source |
|---|---|---|---|---|
| `FoveatedRendering` | `bool` | Required | — ; default `false` (from Config) | VolumeDataSetRenderer; Config |
| `FoveationStart` | `float` | Optional | `[0.0, 0.5]`; default `0.15f` | VolumeDataSetRenderer |
| `FoveationEnd` | `float` | Optional | `[0.0, 0.5]`; default `0.40f` | VolumeDataSetRenderer |
| `FoveationJitter` | `float` | Optional | `[0.0, 0.5]`; default `0.0f` | VolumeDataSetRenderer |
| `FoveatedStepsLow` | `int` | Optional | `[16, 512]`; default `64` | VolumeDataSetRenderer |
| `FoveatedStepsHigh` | `int` | Optional | `[16, 512]`; default `384` | VolumeDataSetRenderer; from Config |

**Dependency:** `FoveatedStepsLow` and `FoveatedStepsHigh` are only applied to the shader if `FoveatedRendering == true`. If `false`, both are clamped to `MaxSteps` (lines 1052–1054 of `VolumeDataSetRenderer.cs`). Persisting foveation parameters when `FoveatedRendering = false` is harmless but they will be ignored until re-enabled.

---

### 2.6 MaskState fields

| Field | C# Type | Required/Optional | Notes | Source |
|---|---|---|---|---|
| `DisplayMask` | `bool` | Required | Whether mask voxels are rendered; default `false` | VolumeDataSetRenderer line 74 |
| `MaskMode` | `MaskMode` (enum: Disabled, Enabled, Inverted, Isolated) | Required | Default `Disabled` | VolumeDataSetRenderer line 75 |
| `MaskVoxelSize` | `float` | Optional | `[0.0, 1.0]`; default `1.0f` | VolumeDataSetRenderer |
| `MaskVoxelColor` | `Color` (RGBA) | Optional | Default `(0.5, 0.5, 0.5, 0.2)` | VolumeDataSetRenderer |
| `LastSavedMaskPath` | `string` | Optional | Path of last saved mask file; used to reload mask data | VolumeDataSet line 224 |

**Explicitly excluded — GPU runtime resources, must never be serialised:**

| Field | Type | Reason |
|---|---|---|
| `_maskDataSet.DataCube` | `Texture3D` | GPU-only |
| `_maskDataSet.RegionCube` | `Texture3D` | GPU-only |
| `_maskDataSet.ExistingMaskBuffer` | `ComputeBuffer` | GPU-only |
| `_maskDataSet.AddedMaskBuffer` | `ComputeBuffer` | GPU-only |
| `_maskDataSet.BrushStrokeHistory` | `List<BrushStrokeTransaction>` | Runtime undo stack; not persisted |

---

## Validation rules

| Field | Rule | Where enforced |
|---|---|---|
| `FitsFilePath` | Must not be null or empty; file existence checked at restore time | `VolumeDataSet.LoadDataFromFitsFile()` line 275 |
| `ThresholdMin` | `0.0 ≤ ThresholdMin ≤ ThresholdMax`; `ThresholdMax ≤ 1.0` | `RenderingController.cs` lines 222, 231, 240, 249 (clamp); must also clamp at deserialise |
| `ThresholdMax` | `ThresholdMin ≤ ThresholdMax ≤ 1.0` | Same as above |
| `ColorMap` | Must be a defined `ColorMapEnum` value; fallback to `Accent` if hash not found (`ColorMapUtils.FromHashCode` line 50) | `ColorMapUtils`; must validate via `Enum.IsDefined()` |
| `ScalingType` | Must be one of 6 defined values | `Enum.IsDefined()` required |
| `ProjectionMode` | Must be one of 2 defined values | `Enum.IsDefined()` required |
| `MaskMode` | Must be one of 4 defined values | `Enum.IsDefined()` required |
| `MaxSteps` | `[16, 512]` | `[Range]` attribute (Inspector only); must clamp at deserialise |
| `FoveatedStepsLow` | `[16, 512]` and `≤ FoveatedStepsHigh` | Not enforced in code; must add |
| `FoveatedStepsHigh` | `[16, 512]` and `≥ FoveatedStepsLow` | Not enforced in code; must add |
| `FoveationStart` | `[0.0, 0.5]` and `< FoveationEnd` | Not enforced in code; must add |
| `FoveationEnd` | `[0.0, 0.5]` and `> FoveationStart` | Not enforced in code; must add |
| `VignetteFadeStart` | `[0.0, 0.5]` and `< VignetteFadeEnd` | Not enforced in code; must add |
| `VignetteFadeEnd` | `[0.0, 0.5]` and `> VignetteFadeStart` | Not enforced in code; must add |
| `Scale` (all components) | Must be `> 0` to avoid division-by-zero in ray-marching (`Mathf.Max(1e-6f,...)` line 948) | Implicit; must clamp on deserialise |
| `Rotation` | Must be a normalised `Quaternion` | Not enforced; must normalise on deserialise |
| `SubsetBounds` | Each axis bound must be within `[0, TrueDimension]`; validated at load (VolumeDataSet line 320) | `VolumeDataSet.LoadDataFromFitsFile()` |
| `SelectedHdu` | Must be a valid HDU index present in the FITS file | Checked during load; abort if invalid |
| `ScalingBias` | `[-1.0, 1.0]` | `[Range]` attribute; must clamp at deserialise |
| `ScalingContrast` | `[0.0, 5.0]` | `[Range]` attribute; must clamp at deserialise |
| `ScalingGamma` | `[0.0, 5.0]` | `[Range]` attribute; must clamp at deserialise |

---

## Default fallbacks

| Field | Safe default | Source |
|---|---|---|
| `ColorMap` | `ColorMapEnum.Inferno` | VolumeDataSetRenderer line 101; Config.defaultColorMap |
| `ScalingType` | `ScalingType.Linear` | VolumeDataSetRenderer line 102; Config.defaultScalingType |
| `ScalingBias` | `0.0f` | VolumeDataSetRenderer |
| `ScalingContrast` | `1.0f` | VolumeDataSetRenderer |
| `ScalingAlpha` | `1000.0f` | VolumeDataSetRenderer |
| `ScalingGamma` | `1.0f` | VolumeDataSetRenderer |
| `ThresholdMin` | `0.0f` | VolumeDataSetRenderer line 98 |
| `ThresholdMax` | `1.0f` | VolumeDataSetRenderer line 99 |
| `ProjectionMode` | `MaximumIntensityProjection` | VolumeDataSetRenderer line 69 |
| `MaxSteps` | `192` | VolumeDataSetRenderer; overridden by Config.maxRaymarchingSteps |
| `Jitter` | `1.0f` | VolumeDataSetRenderer |
| `TextureFilter` | `FilterMode.Point` | VolumeDataSetRenderer; from Config.bilinearFiltering |
| `VignetteFadeStart` | `0.15f` | VolumeDataSetRenderer |
| `VignetteFadeEnd` | `0.40f` | VolumeDataSetRenderer; from Config |
| `VignetteIntensity` | `0.0f` | VolumeDataSetRenderer |
| `VignetteColor` | `Color.black` | VolumeDataSetRenderer |
| `Position` | `InitialPosition` (captured at load time) | VolumeDataSetRenderer line 424 |
| `Rotation` | `InitialRotation` (captured at load time) | VolumeDataSetRenderer line 426 |
| `Scale` | `InitialScale` (captured at load time) | VolumeDataSetRenderer line 425 |
| `SliceMin` | `-0.5 * Vector3.one` | VolumeDataSetRenderer line 126 |
| `SliceMax` | `+0.5 * Vector3.one` | VolumeDataSetRenderer line 126 |
| `FoveatedRendering` | `false` (or Config.foveatedRendering = `true`) | Config.cs |
| `FoveationStart` | `0.15f` | VolumeDataSetRenderer |
| `FoveationEnd` | `0.40f` | VolumeDataSetRenderer |
| `FoveationJitter` | `0.0f` | VolumeDataSetRenderer |
| `FoveatedStepsLow` | `64` | VolumeDataSetRenderer |
| `FoveatedStepsHigh` | `384` | VolumeDataSetRenderer; from Config.maxRaymarchingSteps |
| `DisplayMask` | `false` | VolumeDataSetRenderer line 74 |
| `MaskMode` | `MaskMode.Disabled` | VolumeDataSetRenderer line 75 |
| `MaskVoxelSize` | `1.0f` | VolumeDataSetRenderer |
| `MaskVoxelColor` | `(0.5, 0.5, 0.5, 0.2)` | VolumeDataSetRenderer |
| `SelectedHdu` | `1` | VolumeDataSet lines 261–264 |
| `SubsetBounds` | `[-1,-1,-1,-1,-1,-1]` (= full cube) | VolumeDataSet line 110 |
| `IsCropped` | `false` | VolumeDataSet |
| `OverrideRestFrequency` | `false` | VolumeDataSet |

---

## Recovery behavior

| Condition | Required recovery action |
|---|---|
| `FitsFilePath` missing or file not found on disk | Mark dataset as unresolved; preserve all rendering/spatial/mask state; display "dataset missing" UI; disable rendering safely. Do not discard the session. |
| `FitsFilePath` resolves but dimensions differ from saved state | Log mismatch warning; re-derive dimensions from file; clamp `SubsetBounds` and `CurrentCropMin/Max` to new dimensions; reset crop if bounds invalid. |
| `Rendering` block entirely missing | Apply all rendering defaults (see Default fallbacks table above). |
| Invalid enum value for `ColorMap` | Fall back to `ColorMapEnum.Inferno`; log warning. (Current code falls back to `Accent` via `FromHashCode`; unify to Inferno for persistence.) |
| Invalid enum value for `ScalingType`, `ProjectionMode`, `MaskMode` | Apply respective default. |
| `ThresholdMin > ThresholdMax` on load | Clamp: set `ThresholdMin = 0`, `ThresholdMax = 1`. |
| Any float field outside valid range | Clamp to valid range immediately after deserialisation. |
| `Scale` component ≤ 0 | Replace with `InitialScale` or `Vector3.one`. |
| `Rotation` not normalised (magnitude ≠ 1) | Call `Quaternion.Normalize()`. |
| `Foveation` block missing | Disable foveated rendering (`FoveatedRendering = false`); apply scalar defaults for all foveation params. |
| `FoveationStart >= FoveationEnd` | Restore defaults `(0.15, 0.40)`. |
| `FoveatedStepsLow > FoveatedStepsHigh` | Swap values; log warning. |
| `Mask` block missing or `MaskFileName` empty | Continue without mask; set `MaskMode = Disabled`, `DisplayMask = false`. |
| `LastSavedMaskPath` present but file not found | Continue without mask; log warning; do not crash. |
| `Unsupported shader configuration` (per contract) | Fall back to default rendering path (MIP + Linear + Inferno); preserve compatible settings that can still apply. |
| Any GPU resource reference encountered in save file | Reject and discard silently; GPU resources are never serialisable (contract constraint). |

---

## Existing serialization

**Partial — Config.cs only.**

`Config.cs` is the only class in the project with a full serialisation mechanism:
- Format: JSON via `Valve.Newtonsoft.Json` (`JsonConvert.SerializeObject`, line 229)
- Enum handling: `StringEnumConverter` (enums stored as strings, not integers)
- File: `config.json` adjacent to executable
- Load: `Config.FromFile()` (line 166); falls back to defaults on missing file
- Save: `Config.WriteToFile()` (line 222)
- Error handling: JSON `Error` event handler (line 194)

Config fields that overlap with rendering state (synchronized in `_startFunc()`, not session-save):

| Config field | Maps to renderer field | Line |
|---|---|---|
| `gpuMemoryLimitMb` | `MaximumCubeSizeInMB`, `MaxSteps` | ~361 |
| `maxRaymarchingSteps` | `MaxSteps`, `FoveatedStepsHigh` | ~361 |
| `bilinearFiltering` | `TextureFilter` | ~374 |
| `foveatedRendering` | `FoveatedRendering` | ~375 |
| `defaultColorMap` | `ColorMap` | ~376 |
| `defaultScalingType` | `ScalingType` | ~377 |
| `tunnellingVignetteEnd` | `VignetteFadeEnd` | ~379 |

**No serialisation exists for:**
- `VolumeDataSetRenderer` rendering state (ThresholdMin/Max, ProjectionMode, ScalingBias, etc.)
- Spatial transforms (Position, Rotation, Scale)
- Mask state (MaskMode, DisplayMask, MaskVoxelSize/Color)
- Foveation parameters (individual fields)
- SubsetBounds, crop state

`ISessionPersistenceService` is declared in the contract but does not exist in the codebase. The `VolumeSessionState` struct does not exist in the codebase. Both must be implemented from scratch.

---

## Integration notes

1. **`VolumeSessionState` is a `readonly struct` in the contract.** This is correct for a pure-data DTO but requires all nested types (`RenderingState`, `VolumeDataState`, `SpatialState`, `FoveationState`, `MaskState`) to also be value types or immutable reference types. The persistence layer must construct them fully at deserialisation time before passing to the renderer.

2. **Config vs session state separation.** `Config.cs` provides machine-wide defaults that are applied at renderer startup via `_startFunc()`. Session state represents per-session user choices that override config. The restore sequence must be: **load Config → load session state → apply session overrides**. Session values should take precedence over Config values for all rendering fields.

3. **Load ordering is strictly required:**
   1. FITS file must be loaded and validated first (`VolumeDataSet.LoadDataFromFitsFile()`).
   2. Renderer must be initialised and `started == true` before any rendering state is applied.
   3. Mask file (if present) must be loaded after main dataset (dimensions must match).
   4. Rendering state (colour map, thresholds, scaling) must be applied after the data is resident.
   5. Spatial transforms must be applied last (renderer must be active to accept transform updates).

4. **`Vector3` and `Quaternion` are Unity types.** They cannot be serialised directly using standard JSON without Unity-specific converters or wrapper DTOs. The persistence layer (which must be Unity-independent per SUBTEAM.md) must define plain-C# DTO equivalents (e.g., `SerializableVector3 { float X, Y, Z }`; `SerializableQuaternion { float X, Y, Z, W }`).

5. **`Color` is also a Unity type.** `VignetteColor` and `MaskVoxelColor` must be serialised as plain RGBA floats or hex strings.

6. **`FilterMode` is a Unity enum.** It must be mapped to a plain string or integer in the DTO.

7. **`SubsetBounds` constraint cross-dependency.** The `SubsetBounds` values are validated against `TrueSize` (actual FITS dimensions) at load time (VolumeDataSet line 320). If the FITS file is replaced and dimensions change, saved subset bounds may become invalid. Recovery must clamp or reset them.

8. **Shader parameter push is unconditional every frame.** `Update()` pushes all material properties on every frame regardless of change. This means restoring state is simply a matter of setting the fields — no explicit "dirty" or "apply" call is needed. The renderer will pick up changes in the next frame.

9. **Multiple datasets.** The codebase architecture implies a single active `VolumeDataSetRenderer` per scene, but `VolumeSessionState.FitsFilePath` is a single string. If multi-dataset support is required, `VolumeSessionState` must become an array or the contract must be revised. This is an open architectural question.

---

## Open questions

1. **`VolumeDataState` vs `VolumeSessionState.FitsFilePath` duplication.** The root struct has `FitsFilePath` as a top-level field, and `VolumeDataState` would also logically contain `FileName`. Which is canonical? Should `VolumeDataState.FileName` be removed?

2. **Moment map state.** `MomentMapRenderer` exposes `m0ColorMap`, `m0ScalingType`, `m1ColorMap`, `m1ScalingType`, `MomentMapThreshold`, `UseMask`, `UseZScale` — these are user-configurable per session. They are partially set via `Config.momentMaps` at startup but can be changed at runtime. Should moment map state be part of `RenderingState`, a separate contract section, or excluded from persistence V1?

3. **Brush stroke undo history.** `VolumeDataSet.BrushStrokeHistory` and `BrushStrokeRedoQueue` exist in memory but are never persisted. Should they be included in a future version of this contract, or is restoring to "last saved mask file" sufficient?

4. **`SelectionSaturateFactor` field** (VolumeDataSetRenderer, `[Range(0,1)]`, default `0.7f`) — present in renderer but not mentioned in the contract. Should it be included in `RenderingState`?

5. **`InitialPosition/Rotation/Scale` vs current transforms.** The reset button restores to `InitialPosition/Rotation/Scale`. If we persist the current transforms, restoring them will override the initial anchor on next load. Should the initial anchor also be saved, or always re-derived from the scene on first load?

6. **`RestFrequencyGHz` custom value persistence.** A user can set a custom rest frequency (index `= Count` in the list). The custom double value is stored in the `RestFrequencyGHz` property but not written to disk. Should this be part of `VolumeDataState`?

7. **Schema versioning strategy for `VolumeSessionState`.** The contract declares `ISessionPersistenceService` but says nothing about schema versioning. Given that `VolumeSessionState` is a `readonly struct`, adding fields is a breaking change in the binary layout (though not in JSON). What versioning strategy applies — `schemaVersion` field in the root JSON object, or a separate migration manifest?

8. **`CubeDepthAxis` and `CubeSlice`.** These spatial helper fields (VolumeDataSetRenderer lines 127–128) control which axis is the depth axis and which slice is shown. Are they session-persistent or scene-configuration?

9. **`MaskMode.Isolated` handling.** When `MaskMode = Isolated`, only masked voxels are rendered. If the mask file cannot be found on restore, falling back to `MaskMode.Disabled` would change the visual presentation significantly. Should the recovery rule for a missing mask file also force `MaskMode = Disabled`?

10. **Foveation only valid with compatible hardware.** `FoveatedRendering` requires eye-tracking hardware. If the session was saved on eye-tracking hardware but restored on standard VR hardware, should foveation be disabled automatically or left to the user?
