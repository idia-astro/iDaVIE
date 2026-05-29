// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeLoader — ST1 application service realising IVolumeLoader.
// All FITS/WCS/native work is delegated to registered plug-in interfaces.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel
{
    internal sealed class VolumeLoader : IVolumeLoader
    {
        private readonly IPluginRegistry _plugins;
        private readonly IVolumeRegistry _registry;
        private readonly ILogSink _log;
        private readonly Dictionary<IVolumeDataSet, IFitsFileHandle> _handles = new();

        public VolumeLoader(IPluginRegistry plugins, IVolumeRegistry registry, ILogSink log)
        {
            _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public event DatasetLoadedHandler DatasetLoaded;
        public event DatasetUnloadedHandler DatasetUnloaded;
        public event SubcubeChangedHandler SubcubeChanged;

        public async Task<IVolumeDataSet> LoadAsync(
            string path,
            int hduIndex = 0,
            SubcubeBounds? initialSubcube = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path must not be empty.", nameof(path));

            var fits = _plugins.GetPlugin<IFitsPlugin>();
            var handle = await fits.OpenAsync(path, hduIndex, FitsOpenMode.ReadOnly, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var header = fits.ReadHeader(handle);
                var rawHeader = fits.ReadRawHeader(handle);
                var buffer = initialSubcube.HasValue
                    ? await fits.ReadSubcubeAsync(handle, initialSubcube.Value, cancellationToken).ConfigureAwait(false)
                    : await fits.ReadFullCubeAsync(handle, cancellationToken).ConfigureAwait(false);

                var rawAccess = _plugins.TryGetPlugin<IRawVoxelAccess>(out var registeredRaw)
                    ? registeredRaw
                    : new MemoryRawVoxelAccess(buffer);

                var maskState = _plugins.TryGetPlugin<IMaskEditState>(out var registeredMask)
                    ? registeredMask
                    : EmptyMaskEditState.Instance;

                var wcs = _plugins.TryGetPlugin<IWcsPlugin>(out var wcsPlugin)
                    ? InitialiseWcs(wcsPlugin, rawHeader)
                    : NullWcsMapping.Instance;

                var extents = buffer.SizeX > 0 && buffer.SizeY > 0 && buffer.SizeZ > 0
                    ? new VolumeExtents(buffer.SizeX, buffer.SizeY, buffer.SizeZ)
                    : ReadExtents(header);

                var bounds = initialSubcube ?? SubcubeBounds.FullVolume(extents);
                var volume = new VolumeDataSet(path, hduIndex, extents, bounds, header, rawAccess, maskState, wcs);

                _handles[volume] = handle;
                _registry.Add(volume);
                _registry.SetActive(volume);
                _log.LogInfo(nameof(VolumeLoader), $"Loaded volume '{path}' HDU {hduIndex}.");
                DatasetLoaded?.Invoke();
                return volume;
            }
            catch
            {
                fits.Close(handle);
                throw;
            }
        }

        public Task LoadAsync(string filePath, int hduIndex) =>
            LoadAsync(filePath, hduIndex, null);

        public void Unload(IVolumeDataSet volume)
        {
            if (volume == null)
                return;

            if (_handles.TryGetValue(volume, out var handle))
            {
                _plugins.GetPlugin<IFitsPlugin>().Close(handle);
                _handles.Remove(volume);
            }

            if (volume is VolumeDataSet concrete)
                concrete.MarkUnloaded();

            _registry.Remove(volume);
            _log.LogInfo(nameof(VolumeLoader), $"Unloaded volume '{volume.FilePath}'.");
            DatasetUnloaded?.Invoke();
        }

        public Task UnloadAsync(IVolumeDataSet volume, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Unload(volume);
            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            if (_registry.ActiveVolume != null)
                Unload(_registry.ActiveVolume);
            return Task.CompletedTask;
        }

        public async Task SetSubcubeAsync(
            IVolumeDataSet volume,
            SubcubeBounds newSubcube,
            CancellationToken cancellationToken = default)
        {
            if (volume == null)
                throw new ArgumentNullException(nameof(volume));
            if (!_handles.TryGetValue(volume, out var handle))
                throw new InvalidOperationException("Cannot set a subcube on a volume that was not loaded by this loader.");

            var fits = _plugins.GetPlugin<IFitsPlugin>();
            var buffer = await fits.ReadSubcubeAsync(handle, newSubcube, cancellationToken).ConfigureAwait(false);

            if (volume.RawVoxelAccess is MemoryRawVoxelAccess memory)
                memory.Replace(buffer);
            if (volume is VolumeDataSet concrete)
                concrete.ReplaceSubcube(newSubcube);

            _log.LogInfo(nameof(VolumeLoader), $"Subcube changed for '{volume.FilePath}'.");
            SubcubeChanged?.Invoke(newSubcube);
        }

        public Task SetSubcubeAsync(SubcubeBounds bounds)
        {
            if (_registry.ActiveVolume == null)
                throw new InvalidOperationException("No active volume is registered.");
            return SetSubcubeAsync(_registry.ActiveVolume, bounds);
        }

        private static IWcsMapping InitialiseWcs(IWcsPlugin plugin, string rawHeader)
        {
            plugin.InitialiseFromHeader(rawHeader);
            return plugin is IWcsMapping mapping ? mapping : new WcsPluginMappingAdapter(plugin);
        }

        private static VolumeExtents ReadExtents(IReadOnlyDictionary<string, string> header) =>
            new(ReadInt(header, "NAXIS1"), ReadInt(header, "NAXIS2"), ReadInt(header, "NAXIS3"));

        private static int ReadInt(IReadOnlyDictionary<string, string> header, string key)
        {
            foreach (var pair in header)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(pair.Value, out var result))
                    return result;
            }
            return 0;
        }

        private sealed class MemoryRawVoxelAccess : IRawVoxelAccess
        {
            private float[] _data;
            private VoxelBufferDescriptor _descriptor;
            private long _generation;

            public MemoryRawVoxelAccess(FitsVoxelBuffer buffer)
            {
                _data = Array.Empty<float>();
                _descriptor = new VoxelBufferDescriptor();
                Replace(buffer);
            }

            public VoxelBufferDescriptor Descriptor => _descriptor;
            public long CurrentGeneration => _generation;

            public void Replace(FitsVoxelBuffer buffer)
            {
                _data = buffer.Data ?? Array.Empty<float>();
                _generation++;
                _descriptor = new VoxelBufferDescriptor
                {
                    Length = _data.Length,
                    SizeX = buffer.SizeX,
                    SizeY = buffer.SizeY,
                    SizeZ = buffer.SizeZ,
                    RegionOffset = buffer.RegionOffset,
                    Generation = _generation
                };
            }

            public float[] GetSlice(int zIndex)
            {
                if (zIndex < 0 || zIndex >= _descriptor.SizeZ)
                    return Array.Empty<float>();
                var sliceSize = _descriptor.SizeX * _descriptor.SizeY;
                var slice = new float[sliceSize];
                var offset = zIndex * sliceSize;
                if (offset < _data.Length)
                    Array.Copy(_data, offset, slice, 0, Math.Min(sliceSize, _data.Length - offset));
                return slice;
            }

            public void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination)
            {
                var cursor = 0;
                var slice = GetSlice(zIndex);
                for (var y = yMin; y <= yMax && cursor < destination.Length; y++)
                {
                    for (var x = xMin; x <= xMax && cursor < destination.Length; x++)
                    {
                        var index = y * _descriptor.SizeX + x;
                        destination[cursor++] = index >= 0 && index < slice.Length ? slice[index] : 0f;
                    }
                }
            }
        }

        private sealed class EmptyMaskEditState : IMaskEditState
        {
            public static readonly EmptyMaskEditState Instance = new();
            public short GetMaskValue(int x, int y, int z) => 0;
            public short[] GetMaskSlice(int axis, int sliceIndex) => Array.Empty<short>();
        }

        private sealed class WcsPluginMappingAdapter : IWcsMapping
        {
            private readonly IWcsPlugin _plugin;
            public WcsPluginMappingAdapter(IWcsPlugin plugin) => _plugin = plugin;
            public (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel) =>
                _plugin.PixelToWorld(pixel);
            public string FormatAxisValue(int axis, double value) => _plugin.FormatAxisValue(axis, value);
            public IReadOnlyList<string> AvailableAltFrames => _plugin.GetAvailableAltFrames();
        }

        private sealed class NullWcsMapping : IWcsMapping
        {
            public static readonly NullWcsMapping Instance = new();
            public (double Longitude, double Latitude, double Spectral) PixelToWorld(CartesianCoord pixel) =>
                (pixel.X, pixel.Y, pixel.Z);
            public string FormatAxisValue(int axis, double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            public IReadOnlyList<string> AvailableAltFrames { get; } = Array.Empty<string>();
        }
    }
}
