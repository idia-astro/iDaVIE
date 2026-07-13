# Rendering State Contract

## Owned By
Rendering Sub-team

## Purpose
Defines all serializable state required by the rendering engine to fully restore volume rendering, ray marching, spatial transforms, foveated rendering, and shader configurations.

## Root State Contract & Interfaces
The rendering team owns the canonical representation of the visual session:

```csharp
public readonly struct VolumeSessionState
{
    public string FitsFilePath { get; init; }
    public RenderingState Rendering { get; init; }
    public VolumeDataState VolumeData { get; init; }
    public SpatialState Spatial { get; init; }
    public FoveationState Foveation { get; init; }
    public MaskState Mask { get; init; }
}

public interface ISessionPersistenceService
{
    void Save(VolumeSessionState state, string path);
    VolumeSessionState Load(string path);
}

##Persistent State Categories
* ** RenderingState:** Shader configuration, colour maps, render modes, sampling settings, lighting parameters.

* ** VolumeDataState:** Dataset linkage, cube dimensions, loaded subcube state, active volume configuration.

* ** SpatialState:** Camera transforms, rotation, scale, translation, user positioning.

* ** FoveationState: Foveated rendering settings, sampling density, dynamic rendering configuration.

* ** MaskState:** Active mask configuration, visibility, rendering parameters.

##Recovery Rules
* ** Missing FITS Dataset:** Preserve rendering configuration, mark dataset unresolved, disable rendering safely.

* ** Invalid Rendering Settings**: Restore defaults, clamp invalid numeric ranges, preserve remaining valid state.

* ** Unsupported Shader Configuration:** Fallback to supported rendering path, preserve compatible settings.

##Important Constraints
Rendering state must remain strictly deterministic.

* ** Rendering state ** must be serializable without any Unity scene references (GameObject, MonoBehaviour, etc.).

* ** Runtime-only GPU resources **(e.g., active textures, compute buffers) must never be serialized directly.