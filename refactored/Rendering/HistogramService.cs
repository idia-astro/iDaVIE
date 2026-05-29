// SPDX-License-Identifier: LGPL-3.0-or-later
// HistogramService — ST3 Application service backed by IRawVoxelAccess.
// Replaces the inline histogram code in Assets/Scripts/Menu/HistogramHelper.cs
// (101 LOC). Realises IHistogramService (declared below); held by ST6's
// StatsTabViewModel and by the VR HistogramMenuController (refactored/UI/).

using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Rendering
{
    public interface IHistogramService
    {
        /// <summary>Computes a histogram of the active volume across <paramref name="binCount"/>
        /// bins covering the [min, max] range reported by IVolumeDataSet.GetStats.</summary>
        Task<HistogramData> ComputeAsync(int binCount);
    }

    internal sealed class HistogramService : IHistogramService
    {
        private readonly IRawVoxelAccess _voxels;

        public HistogramService(IRawVoxelAccess voxels) { _voxels = voxels; }

        public Task<HistogramData> ComputeAsync(int binCount) => throw new System.NotImplementedException();
    }
}
