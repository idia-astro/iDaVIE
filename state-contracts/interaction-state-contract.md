# Interaction State Contract

## Owned By
Interaction System (VR, Voice) Sub-team

## Purpose
Manages the persistence of VR locomotion, interaction state machines, tool selection, and controller abstraction context across sessions.

## Persistent State
* **LocomotionState:** Player position, orientation, movement mode, teleportation state.
* **InteractionState:** Active interaction mode, current tool selection, interaction context, selection state.

## Required Abstractions
To decouple from hardware APIs, interaction state relies on these abstractions:
```csharp
public interface IInputProvider
public interface IVoiceRecogniser
public interface IHaptics
public interface IPointer

##Recovery Rules
* **Missing Interaction State: Restore default locomotion mode, reset transient interaction state, preserve stable user preferences.

* **Invalid VR State:** Reset unsafe transforms, recenter user safely to origin, disable invalid interaction contexts to prevent motion sickness or soft-locks.

##Important Constraints
* **Interaction state** must not depend directly on Unity XR objects or SteamVR SDK classes.

* **Runtime controller** references must never be serialized.



##Persistence DTO
public class InteractionStateDto
{
    public LocomotionStateDto Locomotion { get; set; }
    public string ActiveInteractionMode { get; set; }
    public string ActiveToolSelection { get; set; }
}