// SPDX-License-Identifier: LGPL-3.0-or-later
// Boundary value-types module (M-21). Canonical declarations for the value types
// referenced via `iDaVIE.Kernel.Contracts.Types` throughout refactored/.
// Replaces ad-hoc UnityEngine.Vector3 / Color use at every cross-team seam.

using System;
using System.Collections.Generic;

namespace iDaVIE.Kernel.Contracts.Types
{
    /// <summary>Integer voxel-space position.</summary>
    public readonly record struct CartesianCoord(int X, int Y, int Z);

    /// <summary>RGBA in [0,1]; replaces UnityEngine.Color at cross-team boundaries.</summary>
    public readonly record struct FeatureColour(float R, float G, float B, float A);

    /// <summary>Full-cube extents in voxel units.</summary>
    public readonly record struct VolumeExtents(int SizeX, int SizeY, int SizeZ);

    /// <summary>Inclusive min, exclusive max — same convention as the legacy SubcubeBounds.</summary>
    public readonly record struct SubcubeBounds(CartesianCoord Min, CartesianCoord Max);

    /// <summary>Cube-wide voxel statistics.</summary>
    public readonly record struct DataStats(double Min, double Max, double Mean, double StdDev);

    /// <summary>Histogram payload: bin counts plus the [Min, Max] range they cover.</summary>
    public readonly record struct HistogramData(IReadOnlyList<long> Counts, double Min, double Max);

    /// <summary>Per-axis WCS unit strings (X, Y, Z) — populated from FITS headers.</summary>
    public readonly record struct AxisUnits(string X, string Y, string Z);
}
