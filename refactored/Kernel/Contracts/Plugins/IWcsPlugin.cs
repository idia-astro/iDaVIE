// SPDX-License-Identifier: LGPL-3.0-or-later
// IWcsPlugin + IWcsMapping — ST1 cross-team contracts; realised by ST2's
// WcsTransformPlugin (M-06). Replaces the static PluginInterface/AstTool wrapper.
// Returns primitive tuples so ST1 does not depend on ST2's WorldCoord (no outbound
// kernel edge — global_model.md §2).

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel.Contracts.Plugins
{
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

    /// <summary>Narrow read-only facade over IWcsPlugin; held inside the
    /// VolumeDataSet aggregate.</summary>
    public interface IWcsMapping
    {
        (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel);
        string FormatAxisValue(int axis, double value);
        IReadOnlyList<string> AvailableAltFrames { get; }
    }
}
