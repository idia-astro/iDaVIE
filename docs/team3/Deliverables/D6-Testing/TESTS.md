# Unit Tests — Sub-team 3 Refactoring Examples

**Team Alpha | Cache Me If You Can**
**Design doc reference:** `docs/team3/design-document.md §5`

---

## Overview

These tests verify the correctness of the two refactored designs without requiring a
running Unity scene, VR hardware, or GPU. They are written for Unity's built-in
**NUnit Test Runner** (Edit Mode).

| File | Example | Classes tested |
|------|---------|---------------|
| `Example1_RendererSplitTests.cs` | VolumeDataSetRenderer split | `FoveatedSamplingPolicy`, `VolumeCoordinateService`, `StubCameraDriver`, config structs |
| `Example2_MaskModeTests.cs` | Mask Mode Strategy pattern | `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`, `DisabledMaskMode`, `NullMaskMode` |

---

## Test Tiers

### Tier 1 — Pure NUnit (no Unity runtime)

`ShaderKeyword` property tests and all `FoveatedSamplingPolicy` /
`VolumeCoordinateService` tests run with zero Unity-runtime dependency.
`UnityEngine.Vector2`, `Vector3`, and `Matrix4x4` are blittable value types —
safe in any C# context including headless CI.

### Tier 2 — Unity Edit Mode (Material required)

The `Apply()` tests create a real `Material` in the Unity Editor using a built-in
shader. They do **not** need a player loop or GPU — only the Editor context, which
the Unity Test Runner's Edit Mode provides.

```csharp
// Pattern used in SetUp() for all Tier 2 fixtures:
var shader = Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
_mat = new Material(shader);
```

---

## Example 1 — VolumeDataSetRenderer Split

### What the tests verify

#### `FoveatedSamplingPolicy`

The core foveation decision class. Extracted from the ~30-line block in
`VolumeDataSetRenderer.Update()` (before/ lines 1139–1165).

| Test group | What is checked | Why it matters |
|---|---|---|
| **Constructor guard** | `ArgumentNullException` when `gazeProvider` is null | Prevents a silent null-ref crash inside the per-frame hot path |
| **IsGazeAvailable** | Delegates faithfully to the injected `IGazeProvider` | Any code branching on this property must get the true hardware state |
| **ComputeParameters — active** | `FoveationActive = true`, step counts and radii match `FoveatedSamplingConfig` | Shader receives correct per-zone values each frame |
| **ComputeParameters — fallback** | `FoveationActive = false`, both step counts equal `MaxSteps`, gaze centred | Mirrors the before/ else-branch (lines 1163–1165); preserves full quality when no HMD is present |
| **ComputeMipBias** | Returns 0 when gaze unavailable or `maxMips ≤ 1`; result clamped to `[0, maxMips−1]` | Prevents out-of-bounds mip requests; confirms the new LOD capability (absent in before/) |

**Key design property being tested:** the policy never touches `Material` — it only
returns a value struct. `VolumeMaterialBinder` does the shader push. This separation
means neither class can be tested without the other in the before/ code; here they
are independently testable.

---

#### `FoveationParameters.Uniform()`

| Test | What is checked |
|---|---|
| `FoveationActiveIsFalse` | Factory correctly signals that full-quality rendering is active |
| `BothStepCountsEqualMaxSteps` | Both shader uniforms collapse to `MaxSteps` |
| `GazeFocusPointIsScreenCentre` | Shader has a safe default focal point when no gaze is available |
| `ZoneRadiiAreZero` | No zone geometry is pushed in fallback mode |

---

#### `FoveatedSamplingConfig.Default`

These tests ensure the named constants extracted from `VolumeDataSetRenderer`'s
Inspector fields retain their original values. A regression here would silently
change the renderer's frame rate behaviour.

| Before/ field | Default value tested |
|---|---|
| `FoveationStart` | `InnerRadius = 0.15f` |
| `FoveationEnd` | `OuterRadius = 0.40f` |
| `FoveatedStepsLow` | `StepsLow = 64` |
| `FoveatedStepsHigh` | `StepsHigh = 384` |
| Implicit `MaxSteps` | Equals `StepsHigh = 384` |

---

#### `VolumeCoordinateService`

Pure-math static class. Replaces all `transform.InverseTransformPoint()` calls from
domain logic (before/ lines 713, 739, 857) — Violation V-10 (DIP, brief §4.2
mandatory constraint 3). These tests confirm the math is correct with no
Unity runtime dependency.

| Test | What is checked |
|---|---|
| `WorldToObjectSpace_IdentityMatrix` | Point unchanged through identity transform |
| `WorldToObjectSpace_TranslationMatrix` | Inverse translation applied correctly |
| `ObjectToNormalisedVolume_ZeroPoint` | Object-space origin maps to UV centre (0.5, 0.5, 0.5) |
| `ObjectToNormalisedVolume_NegativeHalf` | Bottom-left-back corner → UV (0, 0, 0) |
| `ObjectToNormalisedVolume_PositiveHalf` | Top-right-front corner → UV (1, 1, 1) |
| `ExtractFrustumPlanes_Returns6Planes` | Gribb-Hartmann extraction always produces 6 planes |
| `ExtractFrustumPlanes_LeftPlane/RightPlane/NearPlane` | Correct coefficients for identity VP matrix |

**Why these tests matter:** Before the refactoring, testing the coordinate math
required a full Unity scene with a `Transform`. Now it is verifiable in isolation.

---

#### `StubCameraDriver`

The test double for `IVolumeCameraDriver`. Confirms it is a faithful, controllable
substitute for the real camera adapter.

| Test | What is checked |
|---|---|
| `Default_LocalToWorldIsIdentity` | Predictable initial state for dependent tests |
| `Default_AverageIntensityProjectionIsFalse` | MIP is the default mode (matches before/) |
| `SetProjectionMode_True` | AIP flag round-trips through `ComputeFrame()` |
| `SetProjectionMode_FalseAfterTrue` | Flag can be cleared — UI toggle works in both directions |
| `FixedStateConstructor` | Supplied matrices and clip values are preserved verbatim |

---

## Example 2 — Mask Mode Strategy Pattern

### What the tests verify

#### `ShaderKeyword` properties (Tier 1)

Every `IMaskMode` implementation must return a string that exactly matches a
`#pragma multi_compile` keyword in `VolumeRender.shader`. A mismatch produces a
silent no-op — the shader ignores unknown keywords without generating an error.

| Test | Why it exists |
|---|---|
| Each implementation returns its exact keyword | Prevents typos that would break shader variant selection |
| `NullMaskMode` keyword is not a real shader keyword | Ensures the test double cannot accidentally activate a production code path |
| All keywords are distinct | Multiple enabled keywords from the same `multi_compile` group cause undefined variant selection |

---

#### `Apply()` — keyword mutual exclusion (Tier 2)

The most critical behaviour of the Strategy pattern: **exactly one** keyword must
be active at a time. The before/ code was an if/else chain where a developer could
accidentally enable two branches (e.g. by forgetting to add a `break`-equivalent).
The after/ design enforces exclusion in each class's `Apply()` method.

For each of `ApplyMaskMode`, `InverseMaskMode`, and `IsolateMaskMode`, the tests:

1. Pre-enable all other keywords on the material (worst-case starting state).
2. Call `Apply()`.
3. Assert the **own** keyword is enabled.
4. Assert **every other** keyword is disabled.
5. Count total enabled keywords and assert the count is exactly 1.

The `SwitchingBetweenAllModes_AllTransitionsLeaveExactlyOneKeywordEnabled` test
exercises a full round-robin sequence of six mode switches, checking the invariant
after every transition.

---

#### `_MaskAlpha` uniform (Tier 2)

| Mode | Expected `_MaskAlpha` | Shader effect |
|---|---|---|
| `ApplyMaskMode` | 1.0 | Outside-mask voxels are discarded entirely |
| `InverseMaskMode` | 1.0 | Inside-mask voxels are discarded entirely |
| `IsolateMaskMode` | **0.15** | Outside-mask voxels rendered at 15% opacity for spatial context |
| `DisabledMaskMode` | not set | No mask active |

The `Apply_MaskAlphaDifferentFromApplyAndInverseModes` regression test guards
against a copy-paste error in `IsolateMaskMode` where the 0.15 literal could be
accidentally replaced with 1.0 — the only active mode where the value differs.

---

#### `DisabledMaskMode` — Null Object pattern (Tier 2)

`DisabledMaskMode` implements the Null Object pattern: a valid `IMaskMode` that
`VolumeMaterialBinder` holds at startup (before any mask is loaded). Tests confirm:

- All four mask keywords are disabled after `Apply()`, even if they were previously
  enabled (pre-condition check).
- Calling `Apply(material, null)` does **not** throw when `maskTexture` is null.
  This is the only mode where `null` is a valid argument — the before/ code had an
  implicit null check embedded in the God Class; here the safety is tested explicitly.
- Two sequential calls produce the same result (idempotence).

---

#### Strategy substitutability — LSP (Tier 2)

The `StrategySubstitutabilityTests` fixture verifies Liskov Substitution Principle:
every `IMaskMode` can replace any other `IMaskMode` through the interface reference
without the caller needing to know the concrete type. This mirrors the exact runtime
behaviour of `VolumeMaterialBinder.SetActiveMaskMode(mode)`.

---

## Running the Tests

In a live Unity project these tests would be placed under a folder with an
`.asmdef` configured as:

```json
{
  "name": "iDaVIE.Rendering.Tests",
  "references": ["iDaVIE.Rendering"],
  "includePlatforms": ["Editor"],
  "testables": ["iDaVIE.Rendering.Tests"]
}
```

Open **Window → General → Test Runner → Edit Mode** and click **Run All**.

All Tier 1 tests also pass in a plain `dotnet test` environment (no Unity Editor
required) if the project is set up with the Unity NUnit package as a NuGet
reference.

---

## Coverage Summary

| Class | Properties tested | Methods tested | Tiers |
|---|---|---|---|
| `FoveatedSamplingPolicy` | `IsGazeAvailable` | `ComputeParameters`, `ComputeMipBias` | 1 |
| `FoveationParameters` | — | `Uniform()` | 1 |
| `FoveatedSamplingConfig` | All 6 fields | Constructor | 1 |
| `VolumeCoordinateService` | — | `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes` | 1 |
| `StubCameraDriver` | — | `ComputeFrame`, `SetProjectionMode` | 1 |
| `ApplyMaskMode` | `ShaderKeyword` | `Apply` | 1 + 2 |
| `InverseMaskMode` | `ShaderKeyword` | `Apply` | 1 + 2 |
| `IsolateMaskMode` | `ShaderKeyword` | `Apply` | 1 + 2 |
| `DisabledMaskMode` | `ShaderKeyword` | `Apply` | 1 + 2 |
| `NullMaskMode` | `ShaderKeyword` | — | 1 |
