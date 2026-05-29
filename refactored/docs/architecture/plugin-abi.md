# Plug-in ABI Summary

The refactored plug-in surface is expressed through managed contracts:

- `IFitsPlugin` owns FITS file handles, headers, voxel reads, subcube reads,
  slice reads, and mask writes.
- `IWcsPlugin` owns WCS initialization, pixel/world transforms, bulk transforms,
  formatting, alternate frames, and spectral conversion.
- `IRawVoxelAccess` exposes the current voxel buffer descriptor, generation,
  slices, and rectangular regions.
- `IMaskEditState` exposes mask value and slice reads.

Native function binding is isolated in `NativePluginLoader`. It scans loaded
assemblies for `[PluginAttr]` and `[PluginFunctionAttr]`, resolves a
platform-appropriate library name, and binds delegate fields when symbols are
available. Missing native libraries do not prevent native-less tests from
exercising the managed adapter behavior.

ABI versioning is reported by plug-ins through `AbiVersion`. The current
expected major version is `1`.
