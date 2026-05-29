// SPDX-License-Identifier: LGPL-3.0-or-later
// Boundary value-types module (M-21). Canonical declarations for the value types
// referenced via `iDaVIE.Kernel.Contracts.Types` throughout refactored/.
// Replaces ad-hoc engine vector / colour use at every cross-team seam.

using System.Collections.Generic;

namespace iDaVIE.Kernel.Contracts.Types
{
    /// <summary>Voxel-space position (X = RA, Y = Dec, Z = spectral).</summary>
    public readonly record struct CartesianCoord(int X, int Y, int Z);

    /// <summary>RGBA in [0, 1]; replaces engine colour structs at cross-team boundaries.</summary>
    public readonly record struct FeatureColour(float R, float G, float B, float A = 1f);

    /// <summary>Full axis lengths of the loaded FITS cube (NAXIS1/2/3).</summary>
    public readonly record struct VolumeExtents(int NAxis1, int NAxis2, int NAxis3)
    {
        // Compatibility aliases for earlier skeleton drafts.
        public int SizeX => NAxis1;
        public int SizeY => NAxis2;
        public int SizeZ => NAxis3;
    }

    /// <summary>Inclusive min/max voxel coordinates on each axis.</summary>
    public readonly struct SubcubeBounds
    {
        public readonly int XMin;
        public readonly int XMax;
        public readonly int YMin;
        public readonly int YMax;
        public readonly int ZMin;
        public readonly int ZMax;

        public SubcubeBounds(int xMin, int xMax, int yMin, int yMax, int zMin, int zMax)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
            ZMin = zMin;
            ZMax = zMax;
        }

        public SubcubeBounds(CartesianCoord min, CartesianCoord max)
            : this(min.X, max.X, min.Y, max.Y, min.Z, max.Z)
        {
        }

        public CartesianCoord Min => new(XMin, YMin, ZMin);
        public CartesianCoord Max => new(XMax, YMax, ZMax);

        public int SizeX => XMax - XMin + 1;
        public int SizeY => YMax - YMin + 1;
        public int SizeZ => ZMax - ZMin + 1;

        public static SubcubeBounds FullVolume(VolumeExtents e) =>
            new(0, e.NAxis1 - 1, 0, e.NAxis2 - 1, 0, e.NAxis3 - 1);
    }

    /// <summary>Summary stats over a voxel distribution.</summary>
    public sealed class DataStats
    {
        public float Min { get; init; }
        public float Max { get; init; }
        public float Mean { get; init; }
        public float Rms { get; init; }
        public float ZScaleLow { get; init; }
        public float ZScaleHigh { get; init; }

        // Compatibility alias for earlier skeleton drafts.
        public float StdDev => Rms;
    }

    public sealed class HistogramData
    {
        public float RangeMin { get; init; }
        public float RangeMax { get; init; }
        public IReadOnlyList<long> Bins { get; init; } = System.Array.Empty<long>();
        public int BinCount => Bins.Count;

        // Compatibility aliases for earlier skeleton drafts.
        public IReadOnlyList<long> Counts => Bins;
        public double Min => RangeMin;
        public double Max => RangeMax;
    }

    /// <summary>Per-axis WCS unit strings (X, Y, Z) — populated from FITS headers.</summary>
    public readonly record struct AxisUnits(string AxisX, string AxisY, string AxisZ)
    {
        public string X => AxisX;
        public string Y => AxisY;
        public string Z => AxisZ;
    }
}
