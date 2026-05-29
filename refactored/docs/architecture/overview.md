# Team 1 Refactored Architecture Overview

Team 1 owns the kernel contracts and the first runtime boundary around volume
loading, plug-in discovery, configuration, logging, benchmarking, and the
`VolumeDataSet` aggregate. The canonical contract source is
`refactored/info/shared_interfaces.md`.

The kernel exposes plain C# contracts in `iDaVIE.Kernel.Contracts`. Cross-team
payloads use `iDaVIE.Kernel.Contracts.Types` and stay free of Unity, Valve, and
scene object types. Unity-specific code is expected to sit in anti-corruption
adapters outside the contracts.

Key runtime services:

- `Config.LoadFromJson` loads immutable startup settings and applies defaults.
- `PluginRegistry` is the only lookup point for plug-in implementations.
- `DebugLogSink` stores bounded recent entries and broadcasts log events.
- `VolumeRegistry` tracks loaded and active volumes.
- `VolumeLoader` orchestrates FITS/WCS/raw voxel ports and populates
  `VolumeDataSet`.
- `BenchmarkHarness` provides a testable timing boundary for future Unity
  profiler adapters.

The Data adapters under `refactored/Data` replace the old `PluginInterface`
responsibilities for the refactored slice. Native DLL calls are isolated behind
`NativePluginLoader`; the adapters also provide managed fallback behavior for
tests and native-less smoke checks.
