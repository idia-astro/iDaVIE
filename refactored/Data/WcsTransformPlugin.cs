// SPDX-License-Identifier: LGPL-3.0-or-later
// WcsTransformPlugin — ST2 plug-in. Realises IWcsPlugin + IWcsMapping (both ST1)
// and ICoordinateTransformer (ST2 cross-team — narrow ISP facade for ST5, M-06).
// Replaces the static PluginInterface/AstTool wrapper (93 LOC).

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class WcsTransformPlugin : IWcsPlugin, IWcsMapping, ICoordinateTransformer
    {
        public string AbiVersion => "1.0.0";

        // ── IWcsPlugin ───────────────────────────────────────────────────────
        public void InitialiseFromHeader(string rawFitsHeader) => throw new NotImplementedException();

        public (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel)
            => throw new NotImplementedException();
        public CartesianCoord? WorldToPixel(double longitude, double latitude, double spectral)
            => throw new NotImplementedException();
        public void PixelToWorldBulk(ReadOnlySpan<CartesianCoord> pixels,
            Span<double> longitudes, Span<double> latitudes, Span<double> spectrals)
            => throw new NotImplementedException();

        public IReadOnlyList<string> GetAvailableAltFrames() => throw new NotImplementedException();
        public double ConvertSpectralValue(double nativeValue, string targetFrame)
            => throw new NotImplementedException();

        public double AngularSeparationArcsec(double aLon, double aLat, double bLon, double bLat)
            => throw new NotImplementedException();

        public string FormatAxisValue(int axis, double value) => throw new NotImplementedException();

        // ── IWcsMapping (narrow facade held inside VolumeDataSet) ────────────
        public IReadOnlyList<string> AvailableAltFrames => GetAvailableAltFrames();

        // ── ICoordinateTransformer (narrow ISP facade for ST5; M-06) ─────────
        public WorldCoord Transform(CartesianCoord pixelCoord) => throw new NotImplementedException();
        public CartesianCoord PixelOf(WorldCoord worldCoord) => throw new NotImplementedException();
    }
}
