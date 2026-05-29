// SPDX-License-Identifier: LGPL-3.0-or-later
// IFitsPlugin — ST1 cross-team contract; realised by ST2's FitsReaderPlugin.
// ABI-stable within a major version per brief §4.2 c.5. Replaces the static
// PluginInterface/FitsReader P/Invoke wrapper.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel.Contracts.Plugins
{
    public enum FitsOpenMode { ReadOnly, ReadWrite }

    /// <summary>Opaque handle returned by IFitsPlugin.OpenAsync.</summary>
    public interface IFitsFileHandle : System.IDisposable
    {
        string FilePath { get; }
        int HduIndex { get; }
        int HduCount { get; }
        bool IsReadWrite { get; }
    }

    /// <summary>Managed payload returned by ReadFullCubeAsync / ReadSubcubeAsync.</summary>
    public sealed class FitsVoxelBuffer
    {
        public float[] Data { get; init; } = System.Array.Empty<float>();
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public int SizeZ { get; init; }
        public CartesianCoord RegionOffset { get; init; }

        // Compatibility helpers for earlier skeleton drafts.
        public IntPtr DataPtr { get; init; }
        public long Length => Data.Length > 0 ? Data.Length : SizeX * SizeY * SizeZ;
        public VolumeExtents Extents => new(SizeX, SizeY, SizeZ);
    }

    public interface IFitsPlugin
    {
        string AbiVersion { get; }

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
