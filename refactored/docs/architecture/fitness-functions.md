# Fitness Functions

These checks protect the Team 1 slice:

1. Team 1-owned refactored files must not contain throw-only
   `NotImplementedException` stubs.
2. Kernel contracts and the volume aggregate must not reference `UnityEngine`
   or `Valve.*`.
3. Team 1 code must not use config singletons, scene object searches, or direct
   static native wrapper calls.
4. Required architecture and ADR files must be present under `refactored/docs`.
5. A compile/static validation step should run when a refactored project file is
   available. Until then, the workflow performs static scans and reports the
   missing project explicitly.

The GitHub Actions workflow in `refactored/.github/workflows` encodes these
checks for the refactored slice.
