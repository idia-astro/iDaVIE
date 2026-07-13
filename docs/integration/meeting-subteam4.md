# Quick Sync — Sub-team 3 × Sub-team 4 (Interaction)
**Thu 4 June 2026 | ~5 min | Cathal (Tech Lead, Team 3)**

> ✅ **RESOLVED — 2 June 2026.** Sub-team 4 uses a unified `IGaze` interface. Two name changes applied; no structural changes needed.

---

## Context

Our `FoveatedSamplingPolicy` consumes gaze data from your interaction layer. We wrote a placeholder `IGazeProvider` interface and needed you to confirm the signatures.

---

## What we proposed

```csharp
public interface IGazeProvider
{
    Vector3 GazeDirection   { get; }  // world-space
    Vector2 GazeFocusPoint  { get; }  // normalised screen coords [0,1]
    bool    IsGazeAvailable { get; }  // false = no HMD / not worn / not calibrated
}
```

---

## Agreed outcome — 2 June 2026

Sub-team 4 owns a unified `IGaze` interface used by both teams. Two name changes only — no structural differences:

| Our name | Their canonical name | Notes |
|----------|---------------------|-------|
| `IGazeProvider` | `IGaze` | Interface renamed |
| `IsGazeAvailable` | `IsTracking` | Identical semantics |
| `GazeFocusPoint` | `GazeFocusPoint` | ✅ Unchanged |
| `GazeDirection` | `GazeDirection` | ✅ Unchanged |

`FoveatedSamplingPolicy.IsGazeAvailable` (our own property) is kept as-is — it wraps `_gazeProvider.IsTracking` internally.

**Files updated:** `FoveatedSamplingPolicy.cs`, `diagrams/architecture.puml`, `diagrams/class-after.puml`, `tests/TESTS.md`.

Wire-up in `VolumeRenderCoordinator` (Sprint 3):
```csharp
[SerializeField] private IGaze _gazeProvider;
```
Unity 6 migration: swap the component in the Inspector — renderer code unchanged.
