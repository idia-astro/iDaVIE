# Quick Sync — Sub-team 3 × Sub-team 7 (Persistence)
**Thu 4 June 2026 | ~5 min | Cathal (Tech Lead, Team 3)**

---

## Context

We've designed the full persistence contract on our side (`docs/team3/` — see `team7-persistence-contract.md` if it exists, otherwise the design is in `design-document.md` §10). It just needs your sign-off — specifically one structural question neither team can answer alone.

---

## The contract (already designed)

Sub-team 7 owns `ISessionPersistenceService` — the save/load service.
Sub-team 3 owns the four nested state structs (one per extracted class).
`VolumeRenderCoordinator` assembles them into `VolumeSessionState` and hands it to your service.

```csharp
// You own this interface
ISessionPersistenceService.Save(VolumeSessionState state, string path);
ISessionPersistenceService.Load(string path) → VolumeSessionState;

// We own these nested structs — you don't need to look inside them
VolumeSessionState {
    RenderingState  Rendering;   // VolumeMaterialBinder
    VolumeDataState VolumeData;  // VolumeTextureManager
    SpatialState    Spatial;     // VolumeCameraDriver
    FoveationState  Foveation;   // FoveatedSamplingPolicy
    MaskState       Mask;        // VolumeMaterialBinder
}
```

No `UnityEngine` types anywhere — plain floats only, fully serialisable.

---

## One thing we need to agree

**Which assembly owns `VolumeSessionState`?**

It can't live in our assembly (you'd depend on us) or yours (we'd depend on you).

| Option | Notes |
|--------|-------|
| A — New shared `iDaVIE.Contracts` assembly | Clean, no circular deps; someone has to create it |
| B — Existing Team Alpha shared base assembly | No new project; depends on whether one is planned |
| **C — Note as TBD in both design docs (recommended)** | Zero work now; unblocks both teams for the pitch |

---

## What you need to do

**Option C (recommended):** just confirm — we add one line to our design doc. Done.

**Option A or B:** agree the assembly name; we reference it in our after/ code headers. ~10 min.

If this affects your refactoring examples, the full contract is already specified — you can copy struct definitions directly into your after/ code.

---

## Note

Integration is deferred to Sprint 3. This meeting is only about making both teams' design docs consistent before the pitch.

**Deadline: artefact freeze Thu 4 June 11:00.**
