// SPDX-License-Identifier: LGPL-3.0-or-later
// WcsTransformPlugin — ST2 plug-in. Realises IWcsPlugin + IWcsMapping (both ST1)
// and ICoordinateTransformer (ST2 cross-team — narrow ISP facade for ST5, M-06).
// Replaces the static PluginInterface/AstTool wrapper (93 LOC).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class WcsTransformPlugin : IWcsPlugin, IWcsMapping, ICoordinateTransformer
    {
        public string AbiVersion => "1.0.0";

        private readonly Dictionary<string, string> _header =
            new(StringComparer.OrdinalIgnoreCase);

        // ── IWcsPlugin ───────────────────────────────────────────────────────
        public void InitialiseFromHeader(string rawFitsHeader)
        {
            _header.Clear();
            if (string.IsNullOrWhiteSpace(rawFitsHeader))
                return;

            if (rawFitsHeader.Length >= 80 && !rawFitsHeader.Contains(Environment.NewLine))
            {
                for (var offset = 0; offset + 80 <= rawFitsHeader.Length; offset += 80)
                    ParseCard(rawFitsHeader.Substring(offset, 80));
                return;
            }

            foreach (var line in rawFitsHeader.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                ParseCard(line);
        }

        public (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel)
        {
            return (
                PixelAxisToWorld(1, pixel.X),
                PixelAxisToWorld(2, pixel.Y),
                PixelAxisToWorld(3, pixel.Z));
        }

        public CartesianCoord? WorldToPixel(double longitude, double latitude, double spectral)
        {
            if (double.IsNaN(longitude) || double.IsNaN(latitude) || double.IsNaN(spectral))
                return null;

            return new CartesianCoord(
                WorldAxisToPixel(1, longitude),
                WorldAxisToPixel(2, latitude),
                WorldAxisToPixel(3, spectral));
        }

        public void PixelToWorldBulk(ReadOnlySpan<CartesianCoord> pixels,
            Span<double> longitudes, Span<double> latitudes, Span<double> spectrals)
        {
            var count = Math.Min(pixels.Length, Math.Min(longitudes.Length, Math.Min(latitudes.Length, spectrals.Length)));
            for (var i = 0; i < count; i++)
            {
                var world = PixelToWorld(pixels[i]);
                longitudes[i] = world.Longitude;
                latitudes[i] = world.Latitude;
                spectrals[i] = world.Spectral;
            }
        }

        public IReadOnlyList<string> GetAvailableAltFrames()
        {
            var frames = _header.Keys
                .Where(key => key.Length > 6 && key.StartsWith("CTYPE", StringComparison.OrdinalIgnoreCase))
                .Select(key => key.Substring(6))
                .Where(suffix => suffix.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return frames.Length > 0 ? frames : new[] { "native" };
        }

        public double ConvertSpectralValue(double nativeValue, string targetFrame)
        {
            var nativeUnit = Unit(3).ToLowerInvariant();
            var target = (targetFrame ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(target) || target == "native" || target == nativeUnit)
                return nativeValue;

            if (nativeUnit == "hz" && target == "ghz")
                return nativeValue / 1_000_000_000d;
            if (nativeUnit == "ghz" && target == "hz")
                return nativeValue * 1_000_000_000d;
            if (nativeUnit == "m/s" && (target == "km/s" || target == "km s-1"))
                return nativeValue / 1000d;
            if ((nativeUnit == "km/s" || nativeUnit == "km s-1") && target == "m/s")
                return nativeValue * 1000d;

            return nativeValue;
        }

        public double AngularSeparationArcsec(double aLon, double aLat, double bLon, double bLat)
        {
            var lon1 = DegreesToRadians(aLon);
            var lat1 = DegreesToRadians(aLat);
            var lon2 = DegreesToRadians(bLon);
            var lat2 = DegreesToRadians(bLat);

            var deltaLon = lon2 - lon1;
            var deltaLat = lat2 - lat1;
            var sinLat = Math.Sin(deltaLat / 2d);
            var sinLon = Math.Sin(deltaLon / 2d);
            var haversine = sinLat * sinLat + Math.Cos(lat1) * Math.Cos(lat2) * sinLon * sinLon;
            var radians = 2d * Math.Asin(Math.Min(1d, Math.Sqrt(Math.Max(0d, haversine))));
            return radians * 180d / Math.PI * 3600d;
        }

        public string FormatAxisValue(int axis, double value)
        {
            var unit = Unit(axis);
            var formatted = value.ToString("G8", CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";
        }

        // ── IWcsMapping (narrow facade held inside VolumeDataSet) ────────────
        public IReadOnlyList<string> AvailableAltFrames => GetAvailableAltFrames();

        // ── ICoordinateTransformer (narrow ISP facade for ST5; M-06) ─────────
        public WorldCoord Transform(CartesianCoord pixelCoord)
        {
            var world = PixelToWorld(pixelCoord);
            return new WorldCoord(world.Longitude, world.Latitude, world.Spectral, Unit(3));
        }

        public CartesianCoord PixelOf(WorldCoord worldCoord)
        {
            var pixel = WorldToPixel(worldCoord.RightAscension, worldCoord.Declination, worldCoord.SpectralValue);
            if (pixel == null)
                throw new InvalidOperationException("World coordinate cannot be projected to a voxel coordinate.");
            return pixel.Value;
        }

        private double PixelAxisToWorld(int axis, int pixel)
        {
            var crPix = ReadDouble($"CRPIX{axis}", 1d);
            var crVal = ReadDouble($"CRVAL{axis}", 0d);
            var cdelt = ReadDouble($"CDELT{axis}", 1d);
            return crVal + (((double)pixel + 1d - crPix) * cdelt);
        }

        private int WorldAxisToPixel(int axis, double world)
        {
            var crPix = ReadDouble($"CRPIX{axis}", 1d);
            var crVal = ReadDouble($"CRVAL{axis}", 0d);
            var cdelt = ReadDouble($"CDELT{axis}", 1d);
            if (Math.Abs(cdelt) < double.Epsilon)
                return 0;
            return (int)Math.Round(((world - crVal) / cdelt) + crPix - 1d);
        }

        private string Unit(int axis) => ReadString($"CUNIT{axis}", string.Empty);

        private double ReadDouble(string key, double fallback)
        {
            if (!_header.TryGetValue(key, out var value))
                return fallback;

            return double.TryParse(value.Replace('D', 'E'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
        }

        private string ReadString(string key, string fallback) =>
            _header.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim().Trim('\'', '"')
                : fallback;

        private void ParseCard(string card)
        {
            if (string.IsNullOrWhiteSpace(card))
                return;

            var key = card.Length >= 8 ? card.Substring(0, 8).Trim() : card.Trim();
            if (string.IsNullOrWhiteSpace(key) || key == "END")
                return;

            string value;
            if (card.Length > 10 && card[8] == '=')
            {
                value = card.Substring(10);
            }
            else
            {
                var separator = card.IndexOf('=');
                if (separator < 0)
                    return;
                value = card.Substring(separator + 1);
            }

            var slash = FindCommentSlash(value);
            if (slash >= 0)
                value = value.Substring(0, slash);
            _header[key] = value.Trim().Trim('\'', '"');
        }

        private static int FindCommentSlash(string value)
        {
            var inQuote = false;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\'')
                    inQuote = !inQuote;
                if (!inQuote && value[i] == '/')
                    return i;
            }
            return -1;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
    }
}
