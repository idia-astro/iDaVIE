# Dependency Policy

Team 1 contracts are the inward-facing kernel boundary. They may depend on
system libraries and shared value types only.

Allowed:

- `Kernel.Contracts` -> `Kernel.Contracts.Types`
- `Delegates` -> shared payload contracts named by `shared_interfaces.md`
  (`IFeature`, `MomentMapResult`, and `MaskMode`)
- `Kernel` services -> `Kernel.Contracts`, `Kernel.Contracts.Plugins`
- `Data` adapters -> kernel plug-in contracts and ST5 data contracts they
  realise
- Unity-facing layers -> Team 1 contracts through injection

Forbidden:

- `Kernel.Contracts` depending on `UnityEngine`, `Valve.*`, scene objects, or
  concrete UI classes
- Runtime code reaching plug-ins through static native wrapper calls
- Service code reading a global config singleton
- Scene searches such as `GameObject.Find` in Team 1-owned refactored code
- Package cycles that route back into Team 1 through higher layers

The composition root is the intended construction point. New cross-team
delegates must be added only in `Delegates` and documented by ADR.
