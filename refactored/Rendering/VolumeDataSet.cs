// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeDataSet — refactored from Assets/Scripts/VolumeData/VolumeDataSet.cs
// (1920 LOC god class). Per global_model.md §1 ST1 line 13, the aggregate
// itself moves to ST1 ownership (not ST3 — despite the file living under
// refactored/Rendering/ alongside the renderer decomposition). It holds
// *references* to ST2-realised sub-ports, not raw data.
//
// ⚠ Skeleton authored with ASSUMPTION blocks because refactor_plan.md does
//    NOT contain a per-method hotspot table for VolumeDataSet. Every open
//    design question is recorded inline so the assumption is auditable.
//
// Realises IVolumeDataSet (declared in External/IVolumeDataSet.cs); cross-team
// consumers receive IVolumeDataSet only.

using System.Collections.Generic;
using System.Globalization;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel
{
    // ASSUMPTION (file path): the canonical home for this class is
    // refactored/Kernel/VolumeDataSet.cs. The skeleton currently lives at
    // refactored/Rendering/ so it sits alongside its peer renderer. If ST1's
    // Kernel/ directory is later promoted as the boundary, move the file —
    // no namespace change required.

    internal sealed class VolumeDataSet : IVolumeDataSet
    {
        // ASSUMPTION (file I/O ownership): per global_model.md §1 ST1 line 13
        // ("Holds *references* (not data) to raw voxel / mask / WCS / brush-history
        // services realised by ST2"), file open / close / SetSubcube orchestration
        // lives on IVolumeLoader (ST1), not on this aggregate. This class is
        // *populated by* IVolumeLoader and exposes a read-only snapshot.

        // ASSUMPTION (histogram lazy evaluation): GetHistogram / GetStats delegate
        // to IDataAnalysisPlugin on first call; results are cached on this aggregate
        // until DatasetUnloaded fires. The IDataAnalysisPlugin contract in
        // shared_interfaces.md §5.5 only declares ComputeRegionStats — cube-wide
        // stats would need an extension to that interface (raise as a Tier-2
        // contract change against ST5/ST2).

        // ASSUMPTION (WCS frame ownership): the AstFrameSet is held inside
        // WcsTransformPlugin (ST2); this aggregate exposes only the narrow
        // IWcsMapping facade via the sub-ports listed in IVolumeDataSet.

        // ── IVolumeDataSet ──────────────────────────────────────────────────
        public LoadStatus    Status        { get; private set; }
        public string        FilePath      { get; private set; } = "";
        public int           HduIndex      { get; private set; }
        public VolumeExtents Extents       { get; private set; }
        public SubcubeBounds SubcubeBounds { get; private set; }
        public IReadOnlyDictionary<string, string> HeaderDictionary { get; private set; }
            = new Dictionary<string, string>();

        public IRawVoxelAccess RawVoxelAccess { get; private set; }
        public IMaskEditState  MaskEditState  { get; private set; }

        // ── Internal sub-port held but not exposed on IVolumeDataSet ────────
        // ASSUMPTION (WCS exposure): consumers reach WCS via the ST2-owned
        // ICoordinateTransformer rather than via this aggregate. Held internally
        // so FormatCoord / GetAxisUnits can delegate to IWcsMapping without
        // exposing the full plug-in interface across the boundary.
        internal IWcsMapping WcsMapping { get; private set; }

        private DataStats? _cachedStats;
        private HistogramData? _cachedHistogram;

        public VolumeDataSet(
            string filePath,
            int hduIndex,
            VolumeExtents extents,
            SubcubeBounds subcubeBounds,
            IReadOnlyDictionary<string, string> headerDictionary,
            IRawVoxelAccess rawVoxelAccess,
            IMaskEditState maskEditState,
            IWcsMapping? wcsMapping = null)
        {
            FilePath = filePath ?? string.Empty;
            HduIndex = hduIndex;
            Extents = extents;
            SubcubeBounds = subcubeBounds;
            HeaderDictionary = headerDictionary ?? new Dictionary<string, string>();
            RawVoxelAccess = rawVoxelAccess ?? EmptyRawVoxelAccess.Instance;
            MaskEditState = maskEditState ?? EmptyMaskEditState.Instance;
            WcsMapping = wcsMapping ?? NullWcsMapping.Instance;
            Status = LoadStatus.Loaded;
        }

        public DataStats GetStats()
        {
            if (_cachedStats != null)
                return _cachedStats;

            var descriptor = RawVoxelAccess.Descriptor;
            if (descriptor.SizeX <= 0 || descriptor.SizeY <= 0 || descriptor.SizeZ <= 0)
                return _cachedStats = new DataStats();

            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            var sum = 0d;
            var sumSquares = 0d;
            var count = 0L;

            for (var z = 0; z < descriptor.SizeZ; z++)
            {
                foreach (var value in RawVoxelAccess.GetSlice(z))
                {
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        continue;
                    min = value < min ? value : min;
                    max = value > max ? value : max;
                    sum += value;
                    sumSquares += (double)value * value;
                    count++;
                }
            }

            if (count == 0)
                return _cachedStats = new DataStats();

            var mean = sum / count;
            var variance = System.Math.Max(0d, (sumSquares / count) - (mean * mean));
            return _cachedStats = new DataStats
            {
                Min = min,
                Max = max,
                Mean = (float)mean,
                Rms = (float)System.Math.Sqrt(variance),
                ZScaleLow = min,
                ZScaleHigh = max
            };
        }

        public HistogramData GetHistogram()
        {
            if (_cachedHistogram != null)
                return _cachedHistogram;

            var stats = GetStats();
            const int binCount = 256;
            var bins = new long[binCount];
            var range = stats.Max - stats.Min;
            if (range <= 0f)
                return _cachedHistogram = new HistogramData { RangeMin = stats.Min, RangeMax = stats.Max, Bins = bins };

            var descriptor = RawVoxelAccess.Descriptor;
            for (var z = 0; z < descriptor.SizeZ; z++)
            {
                foreach (var value in RawVoxelAccess.GetSlice(z))
                {
                    if (float.IsNaN(value) || float.IsInfinity(value))
                        continue;
                    var normalised = (value - stats.Min) / range;
                    var index = (int)System.Math.Floor(normalised * (binCount - 1));
                    bins[Clamp(index, 0, binCount - 1)]++;
                }
            }

            return _cachedHistogram = new HistogramData { RangeMin = stats.Min, RangeMax = stats.Max, Bins = bins };
        }

        public AxisUnits GetAxisUnits() =>
            new(ReadHeader("CUNIT1", string.Empty),
                ReadHeader("CUNIT2", string.Empty),
                ReadHeader("CUNIT3", string.Empty));

        public string FormatCoord(CartesianCoord coord)
        {
            try
            {
                var world = WcsMapping.PixelToWorld(coord);
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}, {1}, {2}",
                    world.Longitude,
                    world.Latitude,
                    world.Spectral);
            }
            catch
            {
                return $"{coord.X}, {coord.Y}, {coord.Z}";
            }
        }

        internal void MarkUnloaded()
        {
            Status = LoadStatus.Unloaded;
            _cachedStats = null;
            _cachedHistogram = null;
        }

        internal void ReplaceSubcube(SubcubeBounds bounds)
        {
            SubcubeBounds = bounds;
            _cachedStats = null;
            _cachedHistogram = null;
        }

        private string ReadHeader(string key, string fallback)
        {
            if (HeaderDictionary.TryGetValue(key, out var value))
                return value;
            foreach (var pair in HeaderDictionary)
            {
                if (string.Equals(pair.Key, key, System.StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
            return fallback;
        }

        private static int Clamp(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;

        private sealed class EmptyRawVoxelAccess : IRawVoxelAccess
        {
            public static readonly EmptyRawVoxelAccess Instance = new();
            public VoxelBufferDescriptor Descriptor { get; } = new();
            public long CurrentGeneration => 0;
            public float[] GetSlice(int zIndex) => System.Array.Empty<float>();
            public void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, System.Span<float> destination) =>
                destination.Clear();
        }

        private sealed class EmptyMaskEditState : IMaskEditState
        {
            public static readonly EmptyMaskEditState Instance = new();
            public short GetMaskValue(int x, int y, int z) => 0;
            public short[] GetMaskSlice(int axis, int sliceIndex) => System.Array.Empty<short>();
        }

        private sealed class NullWcsMapping : IWcsMapping
        {
            public static readonly NullWcsMapping Instance = new();
            public (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel) =>
                (pixel.X, pixel.Y, pixel.Z);
            public string FormatAxisValue(int axis, double value) =>
                value.ToString(CultureInfo.InvariantCulture);
            public IReadOnlyList<string> AvailableAltFrames { get; } = System.Array.Empty<string>();
        }
    }
}
