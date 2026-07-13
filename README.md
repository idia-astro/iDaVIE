# Refactoring Example 1: Splitting VolumeDataSetRenderer
**Team Alpha | Cache Me If You Can — Sub-team 3**  
*Brief reference: Section 6.3 Software Construction*

---

## What This Example Shows

The God Class problem: `VolumeDataSetRenderer` does four unrelated things.
We apply the Single Responsibility Principle to split it into four focused classes.

This example provides:
- Before/after class structure
- Dependency graph (before and after)
- CK metrics delta
- SOLID/GRASP violations addressed

---

## Before: VolumeDataSetRenderer

### Responsibilities (all in one class)
1. **Material binding** — sets shader keywords, colour map, exposure, transfer function
2. **Texture management** — uploads 3D textures, manages memory budget, handles eviction
3. **Camera driving** — calculates clip planes, ray origin/direction uniforms, projection matrix
4. **Foveated sampling** — reads gaze position, calculates per-frame sample rate

### Before Class Diagram (PlantUML)
*(See `../../diagrams/class-before.puml`)*

```
┌─────────────────────────────────────────┐
│         VolumeDataSetRenderer           │
│─────────────────────────────────────────│
│ - _material : Material                  │
│ - _volumeTexture : Texture3D            │
│ - _maskTexture : Texture3D              │
│ - _camera : Camera                      │
│ - _gazeProvider : EyeTrackingSDK        │  ← concrete SDK, not interface
│─────────────────────────────────────────│
│ + SetColourMap(map: ColourMap)          │
│ + SetMaskMode(mode: string)            │  ← switch statement inside
│ + UploadTexture(data: float[])         │
│ + EvictTexture()                        │
│ + UpdateCameraUniforms()               │
│ + CalculateSampleRate()                │
│ + OnRenderObject()                      │
│ ... (many more)                         │
└─────────────────────────────────────────┘
         |
         | depends on (~31 external classes)
         ▼
  UnityEngine.Rendering.Universal (direct import)
  EyeTrackingSDK (concrete class)
  DataAnalysis (native DLL)
  ... etc
```

### Before CK Metrics

*(Fill in actual measured values from Understand tool)*

| Metric | Value | Target | Violation? |
|--------|-------|--------|------------|
| WMC | TBC | ≤ 20 | ❌ |
| DIT | TBC | ≤ 4 | — |
| NOC | TBC | ≤ 5 | — |
| CBO | TBC | ≤ 14 | ❌ |
| RFC | TBC | ≤ 50 | ❌ |
| LCOM | TBC | ≤ 0.5 | ❌ |

### SOLID Violations

| Violation | Principle | Evidence |
|-----------|-----------|---------|
| Does material, texture, camera, foveation | SRP | 4 distinct responsibility clusters in one class |
| Mask mode switch statement | OCP | Adding mode requires editing this class |
| Depends on concrete `EyeTrackingSDK` | DIP | Can't mock for testing |
| Depends on `UnityEngine.Rendering.Universal` directly | DIP | Can't test outside Unity |

---

## After: Four Focused Classes

### After Class Diagram (PlantUML)
*(See `../../diagrams/class-after.puml`)*

```
┌──────────────────────────────────┐
│     VolumeRenderCoordinator      │  ← thin coordinator only
│──────────────────────────────────│
│ - _materialBinder                │
│ - _textureManager                │
│ - _cameraDriver                  │
│ - _foveatedPolicy                │
│──────────────────────────────────│
│ + Update()                       │  ← delegates everything
└──────────────────────────────────┘
         |          |          |         |
         ▼          ▼          ▼         ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
│VolumeMaterial│ │VolumeTexture │ │VolumeCameraD-│ │FoveatedSampling  │
│Binder        │ │Manager       │ │river         │ │Policy            │
│──────────────│ │──────────────│ │──────────────│ │──────────────────│
│-_renderPipe  │ │-_budget:int  │ │-_renderPipe  │ │-_gaze:IGazeProvi-│
│-_activeMask  │ │-_cache       │ │──────────────│ │ der              │
│──────────────│ │──────────────│ │+GetFrameParam│ │──────────────────│
│+BindFrame()  │ │+EnsureReady()│ │s()           │ │+GetSampleRate()  │
│+SetColourMap │ │+Evict()      │ │+UpdateUnifor-│ │                  │
│+SetMaskMode()│ │              │ │ms()          │ │                  │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────────┘
       |                                                     |
       ▼                                                     ▼
  <<interface>>                                        <<interface>>
  IMaskMode                                            IGazeProvider
       ▲                                                (Sub-team 4)
  ┌────┴────┐
Apply Inverse Isolate
```

### After CK Metrics (Projected)

*(Justify each number from the design — don't invent)*

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | TBC | TBC | TBC | TBC | TBC | TBC | — |
| `VolumeMaterialBinder` | TBC | TBC | TBC | TBC | TBC | TBC | — |
| `VolumeTextureManager` | TBC | TBC | TBC | TBC | TBC | TBC | — |
| `VolumeCameraDriver` | TBC | TBC | TBC | TBC | TBC | TBC | — |
| `FoveatedSamplingPolicy` | TBC | TBC | TBC | TBC | TBC | TBC | — |

### CK Delta Summary

| Metric | Before (single class) | After (avg per class) | Delta |
|--------|----------------------|----------------------|-------|
| WMC | TBC | TBC | TBC |
| CBO | TBC | TBC | TBC |
| RFC | TBC | TBC | TBC |
| LCOM | TBC | TBC | TBC |

---

## C# Skeleton: Before → After

### Before (problematic)
```csharp
// One class, 4 concerns mixed together
public class VolumeDataSetRenderer : MonoBehaviour {
    
    private Material _material;
    private Texture3D _volumeTexture;
    private Camera _camera;
    private EyeTrackingSDK _eyeTracking;  // concrete SDK

    // Concern 1: Material
    public void SetColourMap(ColourMap map) { /* ... */ }
    
    // Concern 2: Texture
    public void UploadVolume(float[] data, int w, int h, int d) { /* ... */ }
    
    // Concern 3: Camera
    private void UpdateCameraUniforms() { /* ... */ }
    
    // Concern 4: Foveation (interleaved with concern 3)
    private float CalculateSampleRate() {
        Vector2 gaze = _eyeTracking.GetGazePosition();  // concrete SDK call
        // ...
    }
    
    // Mask modes as switch
    public void SetMaskMode(string mode) {
        switch (mode) {
            case "apply":   /* 40 lines */ break;
            case "inverse": /* 40 lines */ break;
            case "isolate": /* 40 lines */ break;
        }
    }
}
```

### After (refactored)
```csharp
// Thin coordinator — delegates everything
public class VolumeRenderCoordinator : MonoBehaviour {
    private readonly VolumeMaterialBinder _materialBinder;
    private readonly VolumeTextureManager _textureManager;
    private readonly VolumeCameraDriver _cameraDriver;
    private readonly FoveatedSamplingPolicy _foveatedPolicy;

    // Constructor injection
    public VolumeRenderCoordinator(
        VolumeMaterialBinder materialBinder,
        VolumeTextureManager textureManager,
        VolumeCameraDriver cameraDriver,
        FoveatedSamplingPolicy foveatedPolicy) { /* ... */ }

    public void Update() {
        var cameraParams = _cameraDriver.GetFrameParameters();
        var sampleRate   = _foveatedPolicy.GetSampleRate();
        var texture      = _textureManager.EnsureTextureReady();
        _materialBinder.BindFrame(cameraParams, texture, sampleRate);
        _renderPipeline.ScheduleVolumeRenderPass(cameraParams);
    }
}

// One concern: material only
public class VolumeMaterialBinder {
    private IMaskMode _activeMaskMode;
    private readonly IRenderPipeline _renderPipeline;
    
    public void SetMaskMode(IMaskMode mode) => _activeMaskMode = mode;
    
    public void BindFrame(CameraParameters cam, Texture3D tex, float sampleRate) {
        _renderPipeline.SetShaderKeyword(_activeMaskMode.ShaderKeyword, true);
        _activeMaskMode.Apply(_material, _maskTexture);
        // set other material properties...
    }
}

// One concern: foveation only, IGazeProvider injected
public class FoveatedSamplingPolicy {
    private readonly IGazeProvider _gaze;  // interface, not concrete SDK
    
    public FoveatedSamplingPolicy(IGazeProvider gaze) => _gaze = gaze;
    
    public float GetSampleRate() {
        if (!_gaze.IsGazeAvailable) return 0.5f;  // fallback
        float distFromCentre = Vector2.Distance(_gaze.GazeFocusPoint, Vector2.one * 0.5f);
        return Mathf.Lerp(1.0f, 0.25f, distFromCentre * 2f);
    }
}
```
