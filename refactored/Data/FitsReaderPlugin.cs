// SPDX-License-Identifier: LGPL-3.0-or-later
// FitsReaderPlugin — ST2 plug-in. Realises IFitsPlugin (ST1), IRawVoxelAccess
// (ST1, sub-port held by IVolumeDataSet per M-27), and IFitsBinaryTableSource
// (consumed by ST5's FitsTableReader). Replaces the static
// PluginInterface/FitsReader P/Invoke wrapper (730 LOC).

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Features;                       // IFitsBinaryTableSource
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class FitsReaderPlugin : IFitsPlugin, IRawVoxelAccess, IFitsBinaryTableSource
    {
        public string AbiVersion => "1.0.0";

        // ── IFitsPlugin ──────────────────────────────────────────────────────
        public Task<IFitsFileHandle> OpenAsync(string absolutePath, int hduIndex = 0,
            FitsOpenMode mode = FitsOpenMode.ReadOnly,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public void Close(IFitsFileHandle handle) => throw new NotImplementedException();
        public IReadOnlyDictionary<string, string> ReadHeader(IFitsFileHandle handle)
            => throw new NotImplementedException();
        public string ReadRawHeader(IFitsFileHandle handle) => throw new NotImplementedException();
        public void SelectHdu(IFitsFileHandle handle, int hduIndex) => throw new NotImplementedException();
        public Task<FitsVoxelBuffer> ReadFullCubeAsync(IFitsFileHandle handle,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<FitsVoxelBuffer> ReadSubcubeAsync(IFitsFileHandle handle, SubcubeBounds region,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<float[]> ReadSliceAsync(IFitsFileHandle handle, int zSlice,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public void WriteMaskVoxels(IFitsFileHandle handle, ReadOnlySpan<short> values,
            CartesianCoord origin, int sizeX, int sizeY, int sizeZ)
            => throw new NotImplementedException();

        // ── IRawVoxelAccess ──────────────────────────────────────────────────
        public VoxelBufferDescriptor Descriptor => throw new NotImplementedException();
        public long CurrentGeneration => throw new NotImplementedException();
        public float[] GetSlice(int zIndex) => throw new NotImplementedException();
        public void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination)
            => throw new NotImplementedException();

        // ── IFitsBinaryTableSource (consumed by ST5's FitsTableReader) ───────
        public IReadOnlyList<FeatureColumnInfo> ReadColumns(string filePath)
            => throw new NotImplementedException();
        public IReadOnlyList<IReadOnlyList<string>> ReadRows(string filePath, int columnCount)
            => throw new NotImplementedException();
    }
}
