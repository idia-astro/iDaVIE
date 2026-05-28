# iDaVIE — Shared Cross-Team Interface Definitions

Single source of truth for every C# type that crosses a sub-team boundary, after the conflict-resolution decisions in `interface_resolutions.md` are applied. This supersedes the per-team submissions in `C# Interfaces/`; teams must adopt these signatures verbatim.

## Reference: the brief

`iDaVIE_Refactoring_Assignment_FINAL_1.pdf` §4 — *The Target Architecture*. Every type below respects the five mandatory architectural constraints from §4.2:

1. **No SOLID / GRASP violation** without a documented trade-off. Read views and mutators are split; capture ports are narrow `Capture()/Restore()` pairs.
2. **No circular dependencies** between top-level components. Ownership follows `global_model.md` §2's acyclic graph (kernel-up: ST1 → ST2 → ST3 → ST4 → ST5 → ST6 → ST7).
3. **Domain code must not transitively depend on UnityEngine or SteamVR types.** All payloads below are plain C#. The only exception is `IDesktopShell` — see §1.5 for the explicit cast-token policy.
4. **Every public API boundary is an interface** and covered by at least one test double. Concretes named in `global_model.md` §1 are `internal sealed` inside their owning assembly; consumers hold only the interfaces here.
5. **Plug-in ABI is versioned (semver) and ABI-stable within a major version.** Every plug-in contract in §1.4 carries `string AbiVersion { get; }`; breaking changes require a new interface name.

## Resolutions applied (from `interface_resolutions.md`)

| Type | Picked design | Rationale per resolution doc |
|---|---|---|
| `LogEntry` | ST6 (readonly struct) | "use ST6 design for logentry, not ST1" |
| `ILogSink` | ST1 | "more things" |
| `IDesktopShell` | ST1 | "more general" |
| `LoadStatus` | ST1 (name + enum) | "LoadStatus is the name" |
| `IVolumeDataSet` | ST1 | resolution line 5 |
| `IVolumeLoader` | ST1 | resolution line 6 |
| `IRawVoxelAccess` | ST1 | resolution line 7 |
| `IDataAnalysisPlugin` | ST5 (narrow `ComputeRegionStats` only) | resolution line 8 |
| `MaskMode` | ST3 (`Disabled/Enabled/Inverted/Isolated`) | resolution line 9 |
| `IMaskEditState` | ST6 (read-slice surface) | resolution line 10 |
| `VolumeStateDto` | ST1 | resolution line 11 |
| `SubcubeBounds` | ST6 (readonly struct + ctor) | resolution line 12 |
| `MomentMapResult` | ST3 (record struct, ST3 owns) | resolution line 13 |
| `BrushStroke` | ST6 (axis + sliceIndex + voxel list + config) | resolution line 14 |
| `IMaskMutationService` | ST2 (incl. `PaintPolygon`) | resolution line 15 |
| `ISourceStatsProvider` | ST5 (`int originId`, dictionary) | resolution line 16 |
| `SourceStats` | ST5 (sealed class with bounds + spectral profile) | resolution line 17 |
| `MaskStateDto` | ST2 (RLE + schema version) | resolution line 18 |
| `IRenderSettings`, `IRenderSettingsMutator`, `RenderStateDto` | ST3 | resolution lines 19–21 |
| `LocomotionState`, `InteractionState`, `IInteractionStateProvider`, `InteractionStateDto` | ST4 | resolution lines 22–25 |
| `BrushConfig` | ST4 (Radius/Additive/SourceId/PaintMode) | resolution line 26 |
| `FeatureImportMapping`, `IFeatureImportService` | ST5 | resolution lines 27–28 |
| `DesktopStateDto` | ST6 | resolution line 29 |
| `IWorkspaceSaveCommand`, `IWorkspaceLoadCommand`, `IStateIndexQuery`, `SavedStateInfo`, `IPersistenceEvents` | ST7 | "for all ST7 section, ST7 wins by default" |

Ownership otherwise follows `global_model.md` §3. Where the brief's no-Unity rule and the resolution would diverge, the brief wins (notably `IDesktopShell` — see §1.5).

---

## Namespace plan

To keep cross-team imports legible and the acyclic graph from §2 visible at the namespace level:

| Namespace | Owner | Contents |
|---|---|---|
| `iDaVIE.Kernel.Contracts` | ST1 | Volume aggregate, registry, loader, log, desktop-shell port, delegates, Config |
| `iDaVIE.Kernel.Contracts.Types` | ST1 | Boundary value types crossing >2 teams |
| `iDaVIE.Kernel.Contracts.Plugins` | ST1 | Versioned plug-in ABI |
| `iDaVIE.Kernel.Contracts.Persistence` | ST1 | ST1's capture port + DTO |
| `iDaVIE.Data` | ST2 | Mask mutation, WCS facade, source stats, mask capture |
| `iDaVIE.Rendering.Contracts` | ST3 | Render settings, moment-map seam, render capture |
| `iDaVIE.Interaction` | ST4 | Controller / voice streams, FSM state, value types, interaction capture |
| `iDaVIE.Features` | ST5 | Feature domain, services, catalogue ports, feature capture |
| `iDaVIE.UI` | ST6 | Desktop capture port |
| `iDaVIE.Persistence` | ST7 | Save/load commands, state index, lifecycle events |

---

## 1. ST1 — Kernel & cross-cutting

### 1.1 Shared boundary value types

Declared once for all consumers per M-21. Plain C#; no `UnityEngine.Vector3` / `UnityEngine.Color`.

```csharp
namespace iDaVIE.Kernel.Contracts.Types
{
    using System;
    using System.Collections.Generic;

    /// <summary>Voxel-space position (X = RA, Y = Dec, Z = spectral).</summary>
    public readonly record struct CartesianCoord(int X, int Y, int Z);

    /// <summary>Full axis lengths of the loaded FITS cube (NAXIS1/2/3).</summary>
    public readonly record struct VolumeExtents(int NAxis1, int NAxis2, int NAxis3);

    /// <summary>
    /// Inclusive min/max voxel coordinates on each axis. Picked from ST6's design
    /// (resolution line 12): plain readonly struct with explicit constructor so
    /// it can be JSON-serialised without record-struct quirks.
    /// </summary>
    public readonly struct SubcubeBounds
    {
        public readonly int XMin, XMax, YMin, YMax, ZMin, ZMax;

        public SubcubeBounds(int xMin, int xMax, int yMin, int yMax, int zMin, int zMax)
        {
            XMin = xMin; XMax = xMax;
            YMin = yMin; YMax = yMax;
            ZMin = zMin; ZMax = zMax;
        }

        public int SizeX => XMax - XMin + 1;
        public int SizeY => YMax - YMin + 1;
        public int SizeZ => ZMax - ZMin + 1;

        public static SubcubeBounds FullVolume(VolumeExtents e) =>
            new(0, e.NAxis1 - 1, 0, e.NAxis2 - 1, 0, e.NAxis3 - 1);
    }

    /// <summary>Summary stats over a voxel distribution.</summary>
    public sealed class DataStats
    {
        public float Min        { get; init; }
        public float Max        { get; init; }
        public float Mean       { get; init; }
        public float Rms        { get; init; }
        public float ZScaleLow  { get; init; }
        public float ZScaleHigh { get; init; }
    }

    public sealed class HistogramData
    {
        public float RangeMin { get; init; }
        public float RangeMax { get; init; }
        public IReadOnlyList<long> Bins { get; init; } = Array.Empty<long>();
        public int BinCount => Bins.Count;
    }

    /// <summary>FITS axis unit strings (CUNIT1/2/3).</summary>
    public readonly record struct AxisUnits(string AxisX, string AxisY, string AxisZ);

    /// <summary>RGBA in [0, 1]. Plain C# — no UnityEngine.Color (brief §4.2 constraint 3).</summary>
    public readonly record struct FeatureColour(float R, float G, float B, float A = 1f);
}
```

> **Note on `MomentMapResult`:** moved to ST3-owned per resolution line 13. See §3.3.

### 1.2 `Config` and `Delegates`

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    /// <summary>Immutable application-wide configuration loaded from JSON at startup.</summary>
    public sealed class Config
    {
        // Rendering (ST3)
        public float  DefaultThresholdMin { get; init; } = 0.05f;
        public float  DefaultThresholdMax { get; init; } = 0.95f;
        public float  DefaultZAxisFactor  { get; init; } = 1.0f;
        public string DefaultColorMap    { get; init; } = "Plasma";

        // Volume I/O (ST1/ST2)
        public int MaxLoadedVolumes   { get; init; } = 4;
        public int DefaultSubcubeSize { get; init; } = 0;   // 0 = full cube

        // Logging (ST1)
        public int LogRingCapacity { get; init; } = 500;

        // Persistence (ST7)
        public string PersistenceRootPath { get; init; } = "Workspaces";
        public int    MaxSavedWorkspaces  { get; init; } = 20;

        // Interaction (ST4)
        public float DefaultBrushRadius { get; init; } = 3.0f;

        // Plug-in ABI (brief §4.2 constraint 5)
        public int ExpectedPluginAbiMajor { get; init; } = 1;

        public IReadOnlyDictionary<string, string> Extras { get; init; }
            = new Dictionary<string, string>();
    }
}
```

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System;
    using iDaVIE.Kernel.Contracts.Types;
    using iDaVIE.Kernel.Contracts.Plugins;
    using iDaVIE.Rendering.Contracts;   // MomentMapResult lives here per resolution line 13

    /// <summary>
    /// Central declaration site for every cross-team event delegate (M-15).
    /// Any new entry needs ADR-002 sign-off. No team declares cross-team delegates outside this file.
    /// </summary>
    public static class Delegates
    {
        public delegate void DatasetLoaded(IVolumeDataSet dataset);
        public delegate void DatasetUnloaded(IVolumeDataSet dataset);
        public delegate void SubcubeChanged(IVolumeDataSet dataset, SubcubeBounds newBounds);
        public delegate void RestFrequencyChanged(IVolumeDataSet dataset, double newFrequencyHz);

        public delegate void ConfigChanged(Config newConfig);

        public delegate void RenderSettingsChanged();
        public delegate void MomentMapReady(MomentMapResult result);

        public delegate void MaskBufferChanged(IVolumeDataSet dataset);
        public delegate void MaskModeChanged(IVolumeDataSet dataset, MaskMode newMode);
        public delegate void BrushHistoryChanged(bool canUndo, bool canRedo);

        public delegate void FeatureSetChanged();
        public delegate void SelectionChanged();
    }
}
```

### 1.3 `ILogSink` (ST1 design — resolution line 2)

`LogEntry` uses ST6's readonly-struct shape (resolution line 1) while the sink interface keeps ST1's richer surface. The Unity `Debug.Log` adapter is confined to ST1 Infrastructure per brief §4.2 constraint 3.

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System;
    using System.Collections.Generic;

    public enum LogLevel
    {
        Trace   = 0,
        Debug   = 1,
        Info    = 2,
        Warning = 3,
        Error   = 4,
        Fatal   = 5
    }

    /// <summary>ST6-shape readonly struct (resolution line 1). UTC timestamp set at construction.</summary>
    public readonly struct LogEntry
    {
        public readonly LogLevel  Level;
        public readonly string    Source;
        public readonly string    Message;
        public readonly DateTime  Timestamp;

        public LogEntry(LogLevel level, string source, string message)
        {
            Level = level; Source = source; Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>Domain-safe logging seam — no UnityEngine.Debug in domain code.</summary>
    public interface ILogSink
    {
        void Log(LogLevel level, string source, string message);

        void LogInfo   (string source, string message);
        void LogWarning(string source, string message);
        void LogError  (string source, string message);

        event Action<LogEntry> EntryLogged;

        IReadOnlyList<LogEntry> RecentEntries { get; }

        LogLevel MinimumStoredLevel { get; set; }
    }
}
```

### 1.4 Plug-in ABI

`ILogSink` and `IPluginRegistry` are not plug-ins; `IFitsPlugin`, `IWcsPlugin`, `IDataAnalysisPlugin`, `IRawVoxelAccess` are.

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System;

    public interface IPluginRegistry
    {
        T GetPlugin<T>() where T : class;
        void RegisterPlugin<T>(T plugin) where T : class;
        bool IsRegistered<T>() where T : class;
    }

    public sealed class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(Type contractType)
            : base($"No plug-in registered for contract '{contractType.FullName}'.") { }
    }
}
```

```csharp
namespace iDaVIE.Kernel.Contracts.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using iDaVIE.Kernel.Contracts.Types;

    public enum FitsOpenMode { ReadOnly, ReadWrite }

    public interface IFitsFileHandle : IDisposable
    {
        string FilePath    { get; }
        int    HduIndex    { get; }
        int    HduCount    { get; }
        bool   IsReadWrite { get; }
    }

    public sealed class FitsVoxelBuffer
    {
        public float[]        Data         { get; init; } = Array.Empty<float>();
        public int            SizeX        { get; init; }
        public int            SizeY        { get; init; }
        public int            SizeZ        { get; init; }
        public CartesianCoord RegionOffset { get; init; }
    }

    public interface IFitsPlugin
    {
        string AbiVersion { get; }                                              // brief §4.2 c.5

        Task<IFitsFileHandle> OpenAsync(string absolutePath, int hduIndex = 0,
            FitsOpenMode mode = FitsOpenMode.ReadOnly,
            CancellationToken cancellationToken = default);
        void Close(IFitsFileHandle handle);

        IReadOnlyDictionary<string, string> ReadHeader(IFitsFileHandle handle);
        string ReadRawHeader(IFitsFileHandle handle);

        void SelectHdu(IFitsFileHandle handle, int hduIndex);

        Task<FitsVoxelBuffer> ReadFullCubeAsync(IFitsFileHandle handle,
            CancellationToken cancellationToken = default);
        Task<FitsVoxelBuffer> ReadSubcubeAsync(IFitsFileHandle handle, SubcubeBounds region,
            CancellationToken cancellationToken = default);
        Task<float[]> ReadSliceAsync(IFitsFileHandle handle, int zSlice,
            CancellationToken cancellationToken = default);

        void WriteMaskVoxels(IFitsFileHandle handle, ReadOnlySpan<short> values,
            CartesianCoord origin, int sizeX, int sizeY, int sizeZ);
    }
}
```

```csharp
namespace iDaVIE.Kernel.Contracts.Plugins
{
    using System;
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    /// <summary>
    /// Full versioned ABI for the Starlink-AST WCS engine. Coordinate returns use
    /// primitive tuples so ST1 does not have to reference ST2's WorldCoord (no outbound
    /// cross-team edge from the kernel — §2 of global_model.md).
    /// </summary>
    public interface IWcsPlugin
    {
        string AbiVersion { get; }

        void InitialiseFromHeader(string rawFitsHeader);

        (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel);
        CartesianCoord? WorldToPixel(double longitude, double latitude, double spectral);

        void PixelToWorldBulk(ReadOnlySpan<CartesianCoord> pixels,
            Span<double> longitudes, Span<double> latitudes, Span<double> spectrals);

        IReadOnlyList<string> GetAvailableAltFrames();
        double ConvertSpectralValue(double nativeValue, string targetFrame);

        double AngularSeparationArcsec(double aLon, double aLat, double bLon, double bLat);

        string FormatAxisValue(int axis, double value);
    }

    /// <summary>Narrow read-only facade over IWcsPlugin; held inside ST1's VolumeDataSet.</summary>
    public interface IWcsMapping
    {
        (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel);
        string FormatAxisValue(int axis, double value);
        IReadOnlyList<string> AvailableAltFrames { get; }
    }
}
```

```csharp
namespace iDaVIE.Kernel.Contracts.Plugins
{
    using System;
    using iDaVIE.Kernel.Contracts.Types;

    /// <summary>Describes the layout of ST2's unmanaged voxel buffer.</summary>
    public sealed class VoxelBufferDescriptor
    {
        public IntPtr         DataPtr      { get; init; }
        public long           Length       { get; init; }
        public int            SizeX        { get; init; }
        public int            SizeY        { get; init; }
        public int            SizeZ        { get; init; }
        public CartesianCoord RegionOffset { get; init; }

        /// <summary>
        /// Monotonically increasing token, incremented by ST2 on every native buffer
        /// reallocation (load, subcube change, unload). Consumers MUST compare this
        /// value against <see cref="IRawVoxelAccess.CurrentGeneration"/> before
        /// dereferencing <see cref="DataPtr"/>. A mismatch means the pointer is stale.
        /// </summary>
        public long Generation { get; init; }
    }

    /// <summary>
    /// ST1's design (resolution line 7). Reached through IVolumeDataSet.RawVoxelAccess (M-27).
    /// Pointer is valid only while the volume is loaded; must not be cached across SetSubcubeAsync.
    /// </summary>
    public interface IRawVoxelAccess
    {
        VoxelBufferDescriptor Descriptor { get; }

        /// <summary>
        /// Current generation of the underlying native buffer. Changes after every
        /// LoadAsync / SetSubcubeAsync / UnloadAsync. Consumers holding a
        /// <see cref="VoxelBufferDescriptor"/> whose <see cref="VoxelBufferDescriptor.Generation"/>
        /// differs from this value MUST NOT dereference <see cref="VoxelBufferDescriptor.DataPtr"/>;
        /// refetch via <see cref="Descriptor"/> instead.
        /// </summary>
        long CurrentGeneration { get; }

        /// <summary>Copies a single XY slice at spectral channel <paramref name="zIndex"/> to managed memory.</summary>
        float[] GetSlice(int zIndex);

        /// <summary>Copies a rectangular XY region at channel <paramref name="zIndex"/> into <paramref name="destination"/>.</summary>
        void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination);
    }
}
```

> **`IDataAnalysisPlugin` — picked ST5's narrow design (resolution line 8).** Lives in ST5 (see §5.5) because the resolution chose the ST5 redeclaration. ST1's `VolumeDataSet` keeps lazy statistics/histogram on the read view (`GetStats()` / `GetHistogram()`) so no team is forced to take a broader analysis surface than it needs.

### 1.5 `IDesktopShell` (ST1 design — resolution line 3)

Declared by ST1 to dissolve the ST6 ↔ ST7 cycle (M-26). ST6's `CanvassDesktop` realises it. The shell host token is `object`; presentation assemblies cast it to the documented Unity type. **This is the only contract here that touches the Unity boundary indirectly, and it lives in ST1 Infrastructure rather than Domain — brief §4.2 constraint 3 governs domain code, not the kernel's UI-mount seam.**

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System;

    public enum PanelPlacement
    {
        LeftPane, RightPane, BottomPane, MenuBar, Floating
    }

    public interface IDesktopShell
    {
        void RegisterPanel(
            string panelId,
            string title,
            PanelPlacement placement,
            Action<object> onMount,
            Action onUnmount);

        void UnregisterPanel(string panelId);

        void ShowPanel(string panelId);
        void HidePanel(string panelId);
        bool IsPanelVisible(string panelId);

        event Action<string> PanelShown;
        event Action<string> PanelHidden;
    }
}
```

### 1.6 Volume aggregate

`LoadStatus` (resolution line 4) keeps ST1's name and four-value enum. `IVolumeDataSet`, `IVolumeLoader`, `IVolumeRegistry` are ST1's designs (resolution lines 5, 6 and global model §3.1). `IMaskEditState` is exposed by the aggregate but its **shape is ST6's** per resolution line 10 (see §1.7).

```csharp
namespace iDaVIE.Kernel.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using iDaVIE.Kernel.Contracts.Types;
    using iDaVIE.Kernel.Contracts.Plugins;

    public enum LoadStatus
    {
        Unloaded,
        Loading,
        Loaded,
        Error
    }

    /// <summary>
    /// Read-only cross-team view of the volume aggregate (resolution line 5; M-02, M-27).
    /// Sub-port handles (RawVoxelAccess, MaskEditState) are reached through this aggregate per M-27.
    /// </summary>
    public interface IVolumeDataSet
    {
        LoadStatus Status   { get; }
        string     FilePath { get; }
        int        HduIndex { get; }

        VolumeExtents Extents       { get; }
        SubcubeBounds SubcubeBounds { get; }

        IReadOnlyDictionary<string, string> HeaderDictionary { get; }

        DataStats     GetStats();
        HistogramData GetHistogram();
        AxisUnits     GetAxisUnits();

        /// <summary>Human-readable world-coordinate string (delegates to ST2 WCS internally).</summary>
        string FormatCoord(CartesianCoord coord);

        IRawVoxelAccess RawVoxelAccess { get; }   // realised by ST2 DataAnalysisPlugin
        IMaskEditState  MaskEditState  { get; }   // realised by ST2 MaskEditService
    }

    /// <summary>Mutation surface for load / unload / sub-cube. Mutations are funnelled here; readers hold IVolumeDataSet only.</summary>
    public interface IVolumeLoader
    {
        Task<IVolumeDataSet> LoadAsync(
            string path,
            int hduIndex = 0,
            SubcubeBounds? initialSubcube = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronous unload — acceptable only when the caller can guarantee no native I/O
        /// is in flight (e.g. application shutdown). Prefer <see cref="UnloadAsync"/> for
        /// normal runtime teardown; the native memory release and GPU texture teardown that
        /// happen in ST2's plug-in layer otherwise block the Unity main thread or silently
        /// swallow errors from the native boundary.
        /// </summary>
        void Unload(IVolumeDataSet volume);

        /// <summary>
        /// Async unload: releases native memory and GPU resources off the calling thread.
        /// Fires <see cref="Delegates.DatasetUnloaded"/> on completion.
        /// </summary>
        Task UnloadAsync(IVolumeDataSet volume,
            CancellationToken cancellationToken = default);

        Task SetSubcubeAsync(IVolumeDataSet volume, SubcubeBounds newSubcube,
            CancellationToken cancellationToken = default);
    }

    public sealed class VolumeLoadException : Exception
    {
        public VolumeLoadException(string message) : base(message) { }
        public VolumeLoadException(string message, Exception inner) : base(message, inner) { }
    }

    public interface IVolumeRegistry
    {
        IReadOnlyList<IVolumeDataSet> LoadedVolumes { get; }
        IVolumeDataSet?               ActiveVolume  { get; }

        void SetActive(IVolumeDataSet volume);

        event Action Changed;
    }
}
```

### 1.7 `IMaskEditState` (ST6 shape — resolution line 10)

Owned by ST1 (declaration), realised by ST2's `MaskEditService`. Picked ST6's narrow shape: just slice / value reads. Undo/redo button enablement is driven by `Delegates.BrushHistoryChanged` instead of via this port; mode/display toggles live on `IMaskMutationService` (§2.1).

```csharp
namespace iDaVIE.Kernel.Contracts
{
    public interface IMaskEditState
    {
        /// <summary>Mask value (source ID; 0 = unmasked) at the given voxel coordinate.</summary>
        short GetMaskValue(int x, int y, int z);

        /// <summary>
        /// Mask values for a 2-D slice. axis: 0 = X, 1 = Y, 2 = Z.
        /// Layout mirrors IRawVoxelAccess.GetSlice conventions.
        /// </summary>
        short[] GetMaskSlice(int axis, int sliceIndex);
    }
}
```

### 1.8 `IVolumeStateCapture` (ST1 design — resolution line 11)

```csharp
namespace iDaVIE.Kernel.Contracts.Persistence
{
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    public sealed class VolumeEntryDto
    {
        public string  FilePath              { get; init; } = string.Empty;
        public int     HduIndex              { get; init; }
        public SubcubeBoundsDto? SubcubeBounds { get; init; }
        public string? AltSpectralFrame      { get; init; }
        public double? RestFrequencyHz       { get; init; }
        public Dictionary<string, string> AxisAttributeOverrides { get; init; } = new();
    }

    public sealed class SubcubeBoundsDto
    {
        public int XMin { get; init; }
        public int XMax { get; init; }
        public int YMin { get; init; }
        public int YMax { get; init; }
        public int ZMin { get; init; }
        public int ZMax { get; init; }

        public static SubcubeBoundsDto From(SubcubeBounds b) =>
            new() { XMin = b.XMin, XMax = b.XMax, YMin = b.YMin,
                    YMax = b.YMax, ZMin = b.ZMin, ZMax = b.ZMax };

        public SubcubeBounds ToDomain() =>
            new(XMin, XMax, YMin, YMax, ZMin, ZMax);
    }

    public sealed class VolumeStateDto
    {
        public string SchemaVersion { get; init; } = "1.0.0";
        public List<VolumeEntryDto> Volumes { get; init; } = new();
        public int ActiveVolumeIndex { get; init; } = -1;
    }

    public interface IVolumeStateCapture
    {
        VolumeStateDto Capture();
        void Restore(VolumeStateDto dto);
    }
}
```

---

## 2. ST2 — Data I/O & FITS/WCS plug-ins

`IMaskMutationService` keeps ST2's full design (resolution line 15) including `PaintPolygon` for the desktop polygon path. `BrushStroke` is ST6's shape (resolution line 14); `BrushConfig` is ST4's (resolution line 26) — see §4.

```csharp
namespace iDaVIE.Data
{
    using System.Collections.Generic;
    using System.Numerics;                     // System.Numerics.Vector2 — plain .NET, not UnityEngine
    using iDaVIE.Interaction;                  // BrushConfig (ST4)
    using iDaVIE.Rendering.Contracts;          // MaskMode (ST3)

    /// <summary>
    /// BrushStroke takes ST6's resolution-line-14 shape: axis + slice index + pre-rasterised voxels
    /// in slice-local (U, V) coordinates + the subset of brush configuration ST2 actually consumes.
    /// The ST6-style polygon path rasterises locally and produces this struct; ST2 maps it into the
    /// mask buffer.
    /// </summary>
    public readonly struct BrushStroke
    {
        public readonly int                          Axis;
        public readonly int                          SliceIndex;
        public readonly IReadOnlyList<VoxelCoord2D>  VoxelCoords;
        public readonly StrokePaintConfig            PaintConfig;

        public BrushStroke(int axis, int sliceIndex,
                           IReadOnlyList<VoxelCoord2D> voxelCoords,
                           StrokePaintConfig paintConfig)
        {
            Axis = axis; SliceIndex = sliceIndex;
            VoxelCoords = voxelCoords; PaintConfig = paintConfig;
        }
    }

    /// <summary>
    /// The subset of <see cref="BrushConfig"/> that ST2 needs to apply a pre-rasterised stroke.
    /// <c>Radius</c> is intentionally excluded — it is already baked into the
    /// <see cref="BrushStroke.VoxelCoords"/> list, so carrying it here would be dead data and
    /// risk a double-radius bug if a future ST2 implementer reinterpreted it. ST6 (producer)
    /// constructs this from its local <see cref="BrushConfig"/>; ST4's gesture state still
    /// owns the full <see cref="BrushConfig"/> internally.
    /// </summary>
    public readonly record struct StrokePaintConfig(
        int            SourceId,
        bool           Additive,
        BrushPaintMode PaintMode);

    public readonly struct VoxelCoord2D
    {
        public readonly int U;
        public readonly int V;
        public VoxelCoord2D(int u, int v) { U = u; V = v; }
    }

    /// <summary>Minimal payload accompanying PaintPolygon when ST4's full BrushConfig isn't appropriate.</summary>
    public readonly record struct PaintConfig(short SourceId, bool Additive);

    /// <summary>Source-id entry returned by GetMaskedSources for ST6's source list panel.</summary>
    public readonly struct SourceEntry
    {
        public readonly short MaskValue;
        public SourceEntry(short maskValue) { MaskValue = maskValue; }
    }

    /// <summary>
    /// Single entry point for all mask edits (ST2 design — resolution line 15).
    /// PaintPolygon is retained because both desktop polygon paint and ST6's rasterised
    /// BrushStroke path are needed; ST6 picks the path appropriate to its UI.
    /// </summary>
    public interface IMaskMutationService
    {
        // Single-stroke editing
        void ApplyBrush(BrushStroke stroke);
        void FinishStroke();

        // Polygon paint (desktop)
        void PaintPolygon(
            int                    axis,
            int                    sliceIndex,
            IReadOnlyList<Vector2> polygon,
            PaintConfig            config);

        // History — undo/redo enablement is broadcast via Delegates.BrushHistoryChanged
        void Undo();
        void Redo();

        // Lifecycle
        void InitialiseMask();

        /// <summary>Writes the buffer to FITS. <paramref name="overwrite"/> = true ⇒ write to the open mask path.</summary>
        int  SaveMask(bool overwrite);

        // Display / mode state — readers can also receive Delegates.MaskModeChanged
        MaskMode MaskMode    { get; set; }
        bool     DisplayMask { get; set; }
        short    NewSourceId { get; set; }
        short    CursorSource { get; set; }

        /// <summary>Source IDs currently present in the mask, for ST6's source list.</summary>
        IReadOnlyList<SourceEntry> GetMaskedSources();
    }
}
```

```csharp
namespace iDaVIE.Data
{
    using iDaVIE.Kernel.Contracts.Types;

    /// <summary>
    /// Narrow ISP facade for ST5 (M-06). Realised by WcsTransformPlugin on the same engine
    /// that backs IWcsPlugin / IWcsMapping.
    /// </summary>
    public interface ICoordinateTransformer
    {
        /// <summary>
        /// Voxel → world coordinate. Returns WorldCoord.Invalid (all-NaN, empty unit) if the
        /// transform is undefined at the given position.
        /// </summary>
        WorldCoord Transform(CartesianCoord pixelCoord);
    }

    /// <summary>
    /// World (sky + spectral) coordinate. ST2↔ST5 two-team-only type per M-21, held with the producer.
    /// </summary>
    public readonly record struct WorldCoord(
        double RightAscension,
        double Declination,
        double SpectralValue,
        string SpectralUnit)
    {
        public static readonly WorldCoord Invalid = new(
            double.NaN, double.NaN, double.NaN, string.Empty);

        public bool IsValid =>
            !double.IsNaN(RightAscension) &&
            !double.IsNaN(Declination)    &&
            !double.IsNaN(SpectralValue);
    }
}
```

### 2.1 `IMaskStateCapture` (ST2 design — resolution line 18)

```csharp
namespace iDaVIE.Data
{
    /// <summary>RLE-encoded mask buffer. Encoding is ST2-internal and versioned by SchemaVersion.</summary>
    public sealed class MaskStateDto
    {
        public int    SchemaVersion { get; init; } = 1;
        public byte[] RleData       { get; init; } = System.Array.Empty<byte>();
        public bool   BrushHistory  { get; init; } = false;
        public long   SizeX         { get; init; }
        public long   SizeY         { get; init; }
        public long   SizeZ         { get; init; }
    }

    public interface IMaskStateCapture
    {
        MaskStateDto Capture();
        void         Restore(MaskStateDto dto);
    }
}
```

---

## 3. ST3 — Rendering engine

### 3.1 `MaskMode` (ST3 — resolution line 9)

This is the single canonical enum; ST1's `Off/ShowMasked/ShowUnmasked/Overlay` and ST2's `Hidden/Visible/Editing` are dropped. Already referenced by ST2's `IMaskMutationService`, ST3's `IRenderSettings`, and `Delegates.MaskModeChanged`.

```csharp
namespace iDaVIE.Rendering.Contracts
{
    public enum MaskMode
    {
        Disabled = 0,
        Enabled  = 1,
        Inverted = 2,
        Isolated = 3
    }

    public enum ScalingType
    {
        Linear = 0,
        Log    = 1,
        Sqrt   = 2,
        Square = 3,
        Power  = 4,
        Gamma  = 5
    }

    public enum ProjectionMode
    {
        MaximumIntensityProjection = 0,
        AverageIntensityProjection = 1
    }
}
```

> `ColorMapEnum` already exists in the source tree at `Assets/Scripts/Tools/ColorMapEnum.cs` and relocates into `iDaVIE.Rendering.Contracts` with no API change. Out of scope for this document.

### 3.2 `IRenderSettings` and `IRenderSettingsMutator` (ST3 — resolution lines 19, 20)

Read view + mutator split per M-09. One coalesced `SettingsChanged` event; consumers re-read.

```csharp
namespace iDaVIE.Rendering.Contracts
{
    using System;
    using iDaVIE.Kernel.Contracts.Types;          // FeatureColour

    public interface IRenderSettings
    {
        // Thresholds
        float ThresholdMin { get; }
        float ThresholdMax { get; }

        // Scaling pipeline
        ScalingType ScalingType     { get; }
        float       ScalingBias     { get; }
        float       ScalingContrast { get; }
        float       ScalingAlpha    { get; }
        float       ScalingGamma    { get; }

        // Colour map
        ColorMapEnum ColorMap { get; }

        // Projection
        ProjectionMode ProjectionMode { get; }

        // Z-axis
        float ZAxisFactor    { get; }
        float ZAxisMinFactor { get; }
        float ZAxisMaxFactor { get; }

        // LOD
        bool IsFullResolution { get; }
        int  MaxRayMarchSteps { get; }

        // Vignette
        float         VignetteIntensity { get; }
        float         VignetteFadeStart { get; }
        float         VignetteFadeEnd   { get; }
        FeatureColour VignetteColor     { get; }

        // Foveated rendering
        bool  FoveatedRendering { get; }
        float FoveationStart    { get; }
        float FoveationEnd      { get; }
        float FoveationJitter   { get; }
        int   FoveatedStepsLow  { get; }
        int   FoveatedStepsHigh { get; }

        // Mask display (read)
        MaskMode MaskMode      { get; }
        bool     DisplayMask   { get; }
        float    MaskVoxelSize { get; }

        /// <summary>Coalesced once per frame; consumers MUST re-read.</summary>
        event Action SettingsChanged;
    }

    public interface IRenderSettingsMutator
    {
        void SetThreshold(float min, float max);
        void ResetThreshold();

        void SetScaling(ScalingType type,
            float bias = 0f, float contrast = 1f, float alpha = 1000f, float gamma = 1f);

        void SetColorMap(ColorMapEnum colorMap);
        void ShiftColorMap(int delta);

        void SetFoveationJitter(float jitter);

        void SetProjection(ProjectionMode mode);

        void SetZAxisFactor(float factor);
        void ResetZAxis();

        void SetMaxRayMarchSteps(int steps);

        void SetVignetteIntensity(float intensity);
        void SetVignetteRange(float fadeStart, float fadeEnd);
        void SetVignetteColor(FeatureColour color);

        void SetFoveatedRendering(bool enabled);
        void SetFoveationRange(float start, float end);
        void SetFoveatedStepBudget(int low, int high);

        /// <summary>Restore the cube to the pose captured at volume load. No UnityEngine types cross the boundary.</summary>
        void ResetTransform();
    }
}
```

### 3.3 `IMomentMapRenderer` and `MomentMapResult` (ST3 — resolution line 13)

ST3 now owns `MomentMapResult` (no longer in ST1 shared types). Plain `float[]` output; no `RenderTexture` crosses the boundary.

```csharp
namespace iDaVIE.Rendering.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public enum MomentOrder
    {
        Moment0 = 0,
        Moment1 = 1
    }

    public readonly record struct MomentMapRequest(
        MomentOrder Order,
        float       Threshold,
        bool        UseMask,
        bool        UseZScale,
        bool        Inverted);

    /// <summary>
    /// Owned by ST3 per resolution line 13. Plain C# float[]; no UnityEngine.RenderTexture.
    /// Colour mapping is the caller's concern.
    /// </summary>
    public readonly record struct MomentMapResult(
        MomentOrder Order,
        int         Width,
        int         Height,
        float[]     Values,       // row-major, length = Width * Height
        float       MinValue,
        float       MaxValue);

    public interface IMomentMapRenderer
    {
        Task<MomentMapResult> RenderMomentMap(
            MomentMapRequest request,
            CancellationToken cancellationToken = default);

        bool IsRenderInProgress { get; }
        event Action RenderProgressChanged;
    }
}
```

### 3.4 `IRenderStateCapture` (ST3 — resolution line 21)

```csharp
namespace iDaVIE.Rendering.Contracts
{
    using System;
    using iDaVIE.Kernel.Contracts.Types;

    public interface IRenderStateCapture
    {
        RenderStateDto Capture();
        void Restore(RenderStateDto state);
    }

    public readonly record struct RenderStateDto(
        int SchemaVersion,

        // Thresholds
        float ThresholdMin,
        float ThresholdMax,

        // Scaling
        ScalingType ScalingType,
        float       ScalingBias,
        float       ScalingContrast,
        float       ScalingAlpha,
        float       ScalingGamma,

        // Colour map / projection / Z
        ColorMapEnum   ColorMap,
        ProjectionMode ProjectionMode,
        float          ZAxisFactor,

        // Vignette
        float         VignetteIntensity,
        float         VignetteFadeStart,
        float         VignetteFadeEnd,
        FeatureColour VignetteColor,

        // Foveation
        bool  FoveatedRendering,
        float FoveationStart,
        float FoveationEnd,
        float FoveationJitter,
        int   FoveatedStepsLow,
        int   FoveatedStepsHigh,

        // LOD
        int MaxRayMarchSteps);

    public sealed class RenderStateRestoreException : Exception
    {
        public int RequestedSchemaVersion { get; }
        public int CurrentSchemaVersion   { get; }

        public RenderStateRestoreException(int requested, int current, string message)
            : base(message)
        {
            RequestedSchemaVersion = requested;
            CurrentSchemaVersion   = current;
        }
    }
}
```

> Mask display state (`MaskMode`, `DisplayMask`, `MaskVoxelSize`) is mutated through ST2's `IMaskMutationService` and persisted via `IMaskStateCapture` — not duplicated in `RenderStateDto`.

---

## 4. ST4 — Interaction system

`LocomotionState`, `InteractionState`, `IInteractionStateProvider`, `InteractionStateDto`, `BrushConfig` — all ST4's designs (resolution lines 22–26).

### 4.1 Owned value types

```csharp
namespace iDaVIE.Interaction
{
    using iDaVIE.Kernel.Contracts.Types;       // CartesianCoord

    public readonly record struct ControllerIdentity
    {
        public int PrimaryIndex   { get; init; }
        public int SecondaryIndex { get; init; }

        public static readonly ControllerIdentity None =
            new() { PrimaryIndex = -1, SecondaryIndex = -1 };
    }

    public enum BrushPaintMode { Add, Remove, Replace }

    /// <summary>
    /// ST4's design (resolution line 26) — owns the full brush gesture (radius / additive / source / paint mode).
    /// Used as the Config field on ST2's BrushStroke and persisted (flattened) in InteractionStateDto.
    /// </summary>
    public readonly record struct BrushConfig
    {
        public float          Radius    { get; init; }
        public bool           Additive  { get; init; }
        public int            SourceId  { get; init; }
        public BrushPaintMode PaintMode { get; init; }
    }

    public sealed class DragGestureState
    {
        public CartesianCoord Origin    { get; set; }
        public CartesianCoord CurrentMin { get; set; }
        public CartesianCoord CurrentMax { get; set; }
        public bool           IsActive  { get; set; }
    }

    public enum ShapeType { Cube, Sphere, Cylinder, Cuboid }

    public sealed class ShapeGestureState
    {
        public ShapeType      ActiveShape { get; set; }
        public CartesianCoord Anchor      { get; set; }
        public CartesianCoord Extent      { get; set; }
        public bool           IsActive    { get; set; }
    }

    public sealed class QuickMenuState
    {
        public QuickMenuPanel ActivePanel            { get; set; } = QuickMenuPanel.None;
        public int            HighlightedOptionIndex { get; set; } = -1;
    }

    public sealed class ScrollState
    {
        public float AccumulatedX { get; set; }
        public float AccumulatedY { get; set; }
        public void Reset() { AccumulatedX = 0f; AccumulatedY = 0f; }
    }

    public readonly record struct LocomotionConfig
    {
        public float TranslationSensitivity { get; init; }
        public float RotationSensitivity    { get; init; }
        public float ScaleSensitivity       { get; init; }
        public float MinScale               { get; init; }
        public float MaxScale               { get; init; }
    }
}
```

### 4.2 FSM enums and `IInteractionStateProvider` (ST4 designs)

```csharp
namespace iDaVIE.Interaction
{
    using System;

    public enum LocomotionState
    {
        Idle,
        Moving,
        ScalingRotating
    }

    public enum InteractionState
    {
        Idle,
        ParameterEditing,
        CreatingSelection,
        EditingRegion,
        SourceEditing,
        PaintingStroke
    }

    public enum QuickMenuPanel
    {
        None,
        QuickMenu,
        PaintMenu
    }

    /// <summary>ST4's design (resolution line 24) — published for ST6 desktop GUI.</summary>
    public interface IInteractionStateProvider
    {
        LocomotionState  LocomotionState   { get; }
        InteractionState InteractionState  { get; }
        bool             IsPaintModeActive { get; }
        bool             IsQuickMenuOpen   { get; }
        QuickMenuPanel   ActiveMenuPanel   { get; }

        event Action InteractionStateChanged;
    }
}
```

### 4.3 Controller and voice streams (ST4-internal but declared for the contract surface)

```csharp
namespace iDaVIE.Interaction
{
    using System;
    using iDaVIE.Kernel.Contracts.Types;

    public interface IGripInput
    {
        event Action<int> GripPressed;
        event Action<int> GripReleased;
        bool IsAnyGripHeld   { get; }
        bool IsBothGripsHeld { get; }
    }

    public interface IPointerInput
    {
        event Action<int>                   TriggerPressed;
        event Action<int>                   TriggerReleased;
        event Action<int, float>            TriggerHeld;          // (controllerIndex, pressure)
        event Action<int, CartesianCoord>   PointerEntered;
        event Action<int>                   PointerExited;
    }

    public interface IHapticsOutput
    {
        void Pulse(int controllerIndex, float durationSeconds, float amplitude);
    }

    public interface IThumbstickInput
    {
        event Action<int, float, float> ThumbstickMoved;
        event Action<int>               ThumbstickClicked;
    }

    public interface ITeleportInput
    {
        event Action<CartesianCoord> TeleportRequested;
    }

    /// <summary>
    /// Full composite — for ST4's interaction FSM only. External consumers
    /// (e.g. ST6 haptics on paint confirm, future accessibility adapters) MUST depend
    /// on the narrowest sub-interface that covers their need:
    ///   - <see cref="IGripInput"/>       (grip events only)
    ///   - <see cref="IPointerInput"/>    (trigger + pointer events only)
    ///   - <see cref="IHapticsOutput"/>   (haptic pulse only)
    ///   - <see cref="IThumbstickInput"/> (thumbstick events only)
    ///   - <see cref="ITeleportInput"/>   (teleport events only)
    /// The composition root wires the same concrete implementation to all sub-interfaces
    /// via DI registration; no consumer outside ST4 should reference
    /// <see cref="IControllerEventStream"/> directly (brief §4.2 constraint 1, ISP).
    /// </summary>
    public interface IControllerEventStream
        : IGripInput, IPointerInput, IHapticsOutput, IThumbstickInput, ITeleportInput
    {
    }
}
```

```csharp
namespace iDaVIE.Interaction
{
    using System;

    public enum VoiceCommand
    {
        NextSource, PreviousSource,
        Confirm, Cancel, Undo,
        StartPainting, StopPainting,
        EmergencyStop
    }

    public enum VoiceActivationMode { Continuous, PushToTalk }

    public readonly record struct SpeechRecognitionConfig
    {
        public string?              Locale              { get; init; }
        public VoiceActivationMode  ActivationMode      { get; init; }
        public float                ConfidenceThreshold { get; init; }
    }

    public interface IVoiceCommandStream
    {
        event Action<VoiceCommand> CommandRecognised;
        bool IsListening { get; }
        SpeechRecognitionConfig Config { get; }

        void StartListening();
        void StopListening();
    }
}
```

### 4.4 `IInteractionStateCapture` (ST4 — resolution line 25)

```csharp
namespace iDaVIE.Interaction
{
    public sealed class InteractionStateDto
    {
        // FSM positions — serialised as enum name strings for forward compatibility.
        // On Restore, unknown values MUST fall back to the Idle member of the respective
        // enum rather than throwing. Use TryParseOrDefault below.
        public string CurrentLocomotionState  { get; set; } = nameof(LocomotionState.Idle);
        public string CurrentInteractionState { get; set; } = nameof(InteractionState.Idle);

        // BrushConfig flattened (ST4-owned per resolution line 26).
        // Stroke history is captured separately by IMaskStateCapture (M-04).
        public float  BrushRadius     { get; set; }
        public bool   BrushAdditive   { get; set; }
        public int    BrushSourceId   { get; set; }
        public string BrushPaintMode  { get; set; } = nameof(BrushPaintMode.Add);

        // Voice
        public string? ActiveVoiceLocale       { get; set; }
        public bool    PushToTalkEnabled       { get; set; }
        public float   VoiceConfidenceThreshold { get; set; }

        // Menu
        public string ActiveMenuPanel { get; set; } = nameof(QuickMenuPanel.None);

        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// Safe parser for forward-compatible enum strings. Returns
        /// <paramref name="fallback"/> when the stored string is null, empty, or not a
        /// known member of <typeparamref name="T"/>. Other capture-port DTOs that gain
        /// enum-as-string fields during IR-01 completion SHOULD adopt the same pattern.
        /// </summary>
        public static T TryParseOrDefault<T>(string? value, T fallback) where T : struct, System.Enum
            => System.Enum.TryParse<T>(value, ignoreCase: false, out var result) ? result : fallback;
    }

    public interface IInteractionStateCapture
    {
        InteractionStateDto Capture();
        void Restore(InteractionStateDto state);
    }
}
```

---

## 5. ST5 — Feature system & domain model

### 5.1 Domain enums and value types

```csharp
namespace iDaVIE.Features
{
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    public enum FeatureSetType { Mask, Imported, UserDefined, SelectionBox }

    public enum SourceMappingOptions
    {
        ID,
        X, Y, Z,
        Xmin, Xmax, Ymin, Ymax, Zmin, Zmax,
        Ra, Dec, Velo, Freq, Redshift,
        Flag
    }

    public readonly record struct FeatureColumnInfo(
        string Name,
        string Unit,
        string DataType,
        string Ucd);

    public readonly record struct SpectralProfileResult(
        IReadOnlyList<double> Profile,
        int                   ZStartChannel,
        double                TotalFlux,
        double                PeakFlux);

    public sealed class FeatureImportMapping
    {
        public IReadOnlyDictionary<SourceMappingOptions, string> ColumnAssignments { get; init; }
            = new Dictionary<SourceMappingOptions, string>();
        public IReadOnlyList<bool> ColumnMask { get; init; } = System.Array.Empty<bool>();
        public bool          ExcludeExternal  { get; init; }
        public string        SetName          { get; init; } = string.Empty;
        public FeatureColour DisplayColour    { get; init; }
    }

    public sealed class FeatureTable
    {
        public IReadOnlyList<FeatureColumnInfo>          Columns { get; init; }
            = System.Array.Empty<FeatureColumnInfo>();
        public IReadOnlyList<IReadOnlyList<string>>      Rows    { get; init; }
            = System.Array.Empty<IReadOnlyList<string>>();
    }
}
```

### 5.2 Read views

```csharp
namespace iDaVIE.Features
{
    using System;
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    public interface IFeatureStatistics
    {
        long           VoxelCount           { get; }
        double         TotalFlux            { get; }
        double         PeakFlux             { get; }
        CartesianCoord FluxWeightedCentroid { get; }
        double         ChannelW20           { get; }
        double         VeloW20              { get; }
        double         ChannelVsys          { get; }
        double         VeloVsys             { get; }
    }

    public interface IFeature
    {
        int                   OriginId       { get; }
        string                Name           { get; }
        string                Flag           { get; }
        CartesianCoord        Center         { get; }
        CartesianCoord        Size           { get; }
        bool                  IsSelected     { get; }
        IReadOnlyList<string> RawDataValues  { get; }
        IFeatureStatistics?   Statistics     { get; }    // null for non-Mask features
    }

    public interface IFeatureSet
    {
        int                   Index          { get; }
        string                FileName       { get; }
        FeatureSetType        Type           { get; }
        FeatureColour         DisplayColour  { get; }
        bool                  IsVisible      { get; }
        IReadOnlyList<IFeature> Features     { get; }
        IReadOnlyList<string> RawDataKeys    { get; }
    }
}
```

### 5.3 Application services

```csharp
namespace iDaVIE.Features
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using iDaVIE.Kernel.Contracts.Types;
    using iDaVIE.Rendering.Contracts;          // MomentMapResult (resolution line 13)

    public interface IFeatureSetQuery
    {
        IReadOnlyList<IFeatureSet> GetAllFeatureSets();
        IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type);

        void SetVisible(IFeatureSet featureSet, bool visible);
        void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour);

        void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax);
        void SetFeatureFlag(IFeature feature, string flag);

        void CopyFeatureToUserDefined(IFeature source);
        void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax);

        event Action FeatureSetChanged;
    }

    public interface IFeatureSelectionService
    {
        IFeature?    SelectedFeature    { get; }
        IFeatureSet? SelectedFeatureSet { get; }

        bool SelectAtCursor(CartesianCoord cursorWorldSpace);
        void SelectFeature(IFeature feature, IFeatureSet owningSet);
        void DeselectFeature();

        event Action<IFeature?> SelectionChanged;
    }

    public interface IFeatureListNavigation
    {
        void DisplayNextSet();
        void DisplayPreviousSet();
    }

    public interface IMomentMapService
    {
        Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask);
    }

    public interface ISpectralProfileService
    {
        Task<SpectralProfileResult> ComputeForRegionAsync(
            CartesianCoord boundsMin,
            CartesianCoord boundsMax);
    }

    /// <summary>
    /// ST5 design (resolution lines 27, 28). FeatureImportMapping (ST5's name) carries the column
    /// assignments; ST6 wires its UI rows to this type without renaming.
    /// </summary>
    public interface IFeatureImportService
    {
        IReadOnlyList<FeatureColumnInfo> GetColumns(string filePath);
        void                             ImportFromFile(string filePath, FeatureImportMapping mapping);
        FeatureImportMapping             LoadMappingFromFile(string mappingFilePath);
        void                             SaveMappingToFile(FeatureImportMapping mapping, string mappingFilePath);
    }
}
```

### 5.4 Catalogue ports

```csharp
namespace iDaVIE.Features
{
    public interface IFeatureCatalogueReader
    {
        FeatureTable Read(string filePath);
    }

    public interface IFeatureCatalogueWriter
    {
        void Write(IFeatureSet featureSet, string filePath);
    }
}
```

### 5.5 `SourceStats`, `ISourceStatsProvider`, `IDataAnalysisPlugin` (ST5 designs — resolution lines 8, 16, 17)

The resolution chose ST5's narrow `IDataAnalysisPlugin` (one method), ST5's `SourceStats` (sealed class with spectral profile and bounds), and ST5's `ISourceStatsProvider` (`int originId`, dictionary, `event Action<int>`). All three live in `iDaVIE.Features` because the resolution preferred ST5's design over ST2's — even though `global_model.md` §3 nominally credits ST2 as owner. Production responsibility for realising them stays with ST2's `DataAnalysisPlugin`.

```csharp
namespace iDaVIE.Features
{
    using System;
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Types;

    public sealed class SourceStats
    {
        public long           VoxelCount           { get; init; }
        public CartesianCoord BoundsMin            { get; init; }
        public CartesianCoord BoundsMax            { get; init; }
        public double         TotalFlux            { get; init; }
        public double         PeakFlux             { get; init; }
        public CartesianCoord FluxWeightedCentroid { get; init; }
        public double         ChannelW20           { get; init; }
        public double         VeloW20              { get; init; }
        public double         ChannelVsys          { get; init; }
        public double         VeloVsys             { get; init; }
        public IReadOnlyList<double> SpectralProfile { get; init; } = Array.Empty<double>();
        public int            ZStartChannel        { get; init; }
    }

    public interface ISourceStatsProvider
    {
        SourceStats? GetStatsForSource(int originId);
        IReadOnlyDictionary<int, SourceStats> GetAllStats();

        /// <summary>Fired with the source id whose stats were updated (or -1 for bulk reload).</summary>
        event Action<int> SourceStatsUpdated;
    }

    /// <summary>
    /// Narrow ST5 design (resolution line 8). ST1's VolumeDataSet uses GetStats() / GetHistogram()
    /// for cube-wide aggregates; this port covers region-bounded stats used by ST5's spectral
    /// profile service and ST5's feature annotation pipeline.
    /// </summary>
    public interface IDataAnalysisPlugin
    {
        SourceStats ComputeRegionStats(CartesianCoord boundsMin, CartesianCoord boundsMax);
    }
}
```

### 5.6 `IFeatureStateCapture`

Minimum viable field list — drafted to unblock ST7 round-trip testing (IR-01). All fields
have defaults, so existing callers using `new FeatureStateDto()` still compile. Teams extend
post Day-9 by adding fields with defaults (non-breaking under the schema-version pattern).

```csharp
namespace iDaVIE.Features
{
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts.Persistence;     // SubcubeBoundsDto
    using iDaVIE.Kernel.Contracts.Types;

    public sealed class FeatureStateDto
    {
        public int SchemaVersion { get; set; } = 1;

        /// <summary>One entry per persisted feature set.</summary>
        public List<FeatureSetEntryDto> FeatureSets { get; set; } = new();

        /// <summary>Selection-box bounds, if active.</summary>
        public SubcubeBoundsDto? SelectionBoxBounds { get; set; }
    }

    public sealed class FeatureSetEntryDto
    {
        public string        SetName       { get; set; } = string.Empty;
        public string        Type          { get; set; } = nameof(FeatureSetType.UserDefined);
        public FeatureColour DisplayColour { get; set; }
        public bool          IsVisible     { get; set; } = true;

        /// <summary>
        /// For Imported sets: file path + mapping. Features re-derived on Restore.
        /// For UserDefined sets: inline feature list in <see cref="Features"/>.
        /// For Mask sets: empty — mask data lives in MaskStateDto.
        /// </summary>
        public string?               ImportFilePath { get; set; }
        public FeatureImportMapping? ImportMapping  { get; set; }
        public List<FeatureEntryDto> Features       { get; set; } = new();
    }

    public sealed class FeatureEntryDto
    {
        public int    OriginId   { get; set; }
        public string Name       { get; set; } = string.Empty;
        public string Flag       { get; set; } = string.Empty;
        public int    CenterX    { get; set; }
        public int    CenterY    { get; set; }
        public int    CenterZ    { get; set; }
        public int    BoundsMinX { get; set; }
        public int    BoundsMinY { get; set; }
        public int    BoundsMinZ { get; set; }
        public int    BoundsMaxX { get; set; }
        public int    BoundsMaxY { get; set; }
        public int    BoundsMaxZ { get; set; }
    }

    public interface IFeatureStateCapture
    {
        FeatureStateDto Capture();
        void Restore(FeatureStateDto dto);
    }
}
```

---

## 6. ST6 — Desktop GUI & client shell

ST6 owns only one cross-team interface (`IDesktopStateCapture`) and realises ST1's `IDesktopShell` (§1.5). Everything else ST6 holds is a consumed interface owned elsewhere.

```csharp
namespace iDaVIE.UI
{
    /// <summary>
    /// ST6's design (resolution line 29). Minimum viable field list — drafted to unblock
    /// ST7 round-trip testing (IR-01). Plain C# — no UnityEngine in the DTO (brief §4.2
    /// constraint 3). Any enum-as-string field added post Day-9 SHOULD use
    /// <see cref="iDaVIE.Interaction.InteractionStateDto.TryParseOrDefault{T}"/> on restore.
    /// </summary>
    public sealed class DesktopStateDto
    {
        public int    SchemaVersion       { get; init; } = 1;
        public string ActiveTabName       { get; init; } = string.Empty;
        public bool   IsFileLoadPanelOpen { get; init; }

        /// <summary>Panel visibility keyed by panelId (matches IDesktopShell registrations).</summary>
        public System.Collections.Generic.Dictionary<string, bool> PanelVisibility { get; init; }
            = new();

        /// <summary>Debug-console scroll position and minimum filter level at time of save.</summary>
        public int    DebugConsoleScrollIndex { get; init; }
        public string DebugConsoleMinLevel    { get; init; }
            = nameof(iDaVIE.Kernel.Contracts.LogLevel.Info);
    }

    public interface IDesktopStateCapture
    {
        DesktopStateDto Capture();
        void Restore(DesktopStateDto dto);
    }
}
```

---

## 7. ST7 — Persistence & workspace state

ST7 wins by default for everything in its section (resolution line 31): the four command/query/event interfaces and `SavedStateInfo`. The per-team capture DTOs above (`VolumeStateDto`, `MaskStateDto`, `RenderStateDto`, `InteractionStateDto`, `FeatureStateDto`, `DesktopStateDto`) follow the owners chosen in resolution lines 11, 18, 21, 25, 29 — ST7 consumes them.

```csharp
namespace iDaVIE.Persistence
{
    using System;
    using System.Collections.Generic;

    public interface IWorkspaceSaveCommand
    {
        /// <summary>Fire-and-forget. Outcome reported via IPersistenceEvents.</summary>
        void Save();
    }

    public interface IWorkspaceLoadCommand
    {
        /// <summary>Triggers load by opaque stateId obtained from IStateIndexQuery.</summary>
        void Load(string stateId);
    }

    public sealed class SavedStateInfo
    {
        public string   StateId     { get; init; } = string.Empty;
        public string   DisplayName { get; init; } = string.Empty;
        public DateTime SavedAtUtc  { get; init; }
    }

    public interface IStateIndexQuery
    {
        IReadOnlyList<SavedStateInfo> GetAll();
        IReadOnlyList<SavedStateInfo> Search(string searchTerm);
    }

    public interface IPersistenceEvents
    {
        event Action         SaveStarted;
        event Action<string> SaveCompleted;   // stateId of the new state
        event Action<string> SaveFailed;      // human-readable error

        event Action         LoadStarted;
        event Action         LoadCompleted;
        event Action<string> LoadFailed;      // human-readable error
    }
}
```

---

## 8. Dependency check (brief §4.2 constraint 2)

After applying the resolutions, every consumer holds an interface owned upstream of it in `global_model.md` §2's graph. Inbound edges per team:

```
ST6 ──► ST1 (Volume aggregate / loader / registry, Config, ILogSink, IDesktopShell, IMaskEditState)
ST6 ──► ST2 (IMaskMutationService — incl. PaintPolygon for desktop polygon path)
ST6 ──► ST3 (IRenderSettings, IRenderSettingsMutator)
ST6 ──► ST4 (IInteractionStateProvider)
ST6 ──► ST5 (IFeatureImportService + feature query / selection / list services)
ST6 ──► ST7 (IWorkspaceSaveCommand, IWorkspaceLoadCommand, IStateIndexQuery, IPersistenceEvents)

ST5 ──► ST1 (IVolumeDataSet read view)
ST5 ──► ST2 (ICoordinateTransformer)
ST5 ──► ST3 (IMomentMapRenderer, MomentMapResult)

ST4 ──► ST1 (IVolumeRegistry, Config)
ST4 ──► ST2 (IMaskMutationService)
ST4 ──► ST3 (IRenderSettings, IRenderSettingsMutator)
ST4 ──► ST5 (IFeatureSetQuery, IFeatureSelectionService, IFeatureListNavigation,
             IFeatureCatalogueReader/Writer [optional])
ST4 ──► ST7 (IWorkspaceSaveCommand, IWorkspaceLoadCommand)

ST3 ──► ST1 (IVolumeDataSet read view, Config, Delegates, IRawVoxelAccess + IMaskEditState
             via the IVolumeDataSet aggregate)

ST2 ──► ST1 (IPluginRegistry, IFitsPlugin / IWcsPlugin / IRawVoxelAccess ABI host)
ST2 ──► ST4 (BrushPaintMode enum — referenced by StrokePaintConfig on BrushStroke; value-type only, no event-loop dependency)
ST2 ──► ST3 (MaskMode — enum only)

ST7 ──► ST1 (IVolumeStateCapture, Config, ILogSink, IDesktopShell)
ST7 ──► ST2 (IMaskStateCapture)
ST7 ──► ST3 (IRenderStateCapture)
ST7 ──► ST4 (IInteractionStateCapture)
ST7 ──► ST5 (IFeatureStateCapture)
ST7 ──► ST6 (IDesktopStateCapture)

ST1 ──► (no outbound cross-team edges; kernel is the floor)
```

The two new ST2 ↔ {ST3, ST4} edges are *value-type-only* references (no behavioural coupling, no event subscription). They do not introduce package-level cycles: `MaskMode` and `BrushPaintMode` are leaf enums with no dependencies of their own. Graph remains acyclic — brief §4.2 constraint 2 satisfied.

---

## 9. Open items

| ID | Item | Owner | Due |
|---|---|---|---|
| IR-01 | Field schemas for `VolumeStateDto`, `MaskStateDto`, `RenderStateDto`, `InteractionStateDto`, `FeatureStateDto`, `DesktopStateDto` | Each capture-port owner + ST7 | **Draft fields — pending Architecture Guild Day 9 sign-off.** All six DTOs now carry a minimum viable field list; teams extend post Day-9 by adding fields with defaults |
| IR-02 | `CatalogDataSetRenderer` consumer-facing interface | ST3 | Post-Day-9 iteration |
| IR-04 | Tension between ST4's `BrushConfig` (carries `Radius`) and pre-rasterised `BrushStroke` (which doesn't need it) | ST2 + ST4 + ST6 | **Closed** — `BrushStroke.Config` narrowed to `StrokePaintConfig` (§2); `BrushConfig` retained on ST4 for gesture state |
