// SPDX-License-Identifier: LGPL-3.0-or-later
// IVolumeLoader — ST1 cross-team contract. Consumed by ST6 (file dialogs) and
// ST7 (workspace restore). Realised in ST1; delegates to ST2 plug-ins for the
// actual FITS I/O.

using System.Threading.Tasks;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel.Contracts
{
    public interface IVolumeLoader
    {
        /// <summary>Loads a FITS cube from disk. Raises DatasetLoaded once complete.</summary>
        Task<IVolumeDataSet> LoadAsync(
            string path,
            int hduIndex = 0,
            SubcubeBounds? initialSubcube = null,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Synchronous unload for shutdown paths that can guarantee no native I/O is in flight.</summary>
        void Unload(IVolumeDataSet volume);

        /// <summary>Releases a loaded volume and raises DatasetUnloaded.</summary>
        Task UnloadAsync(IVolumeDataSet volume,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Crops the active volume to a new subcube. Raises SubcubeChanged.</summary>
        Task SetSubcubeAsync(IVolumeDataSet volume, SubcubeBounds newSubcube,
            System.Threading.CancellationToken cancellationToken = default);

        // Compatibility overloads retained for earlier ST6 skeletons.
        Task LoadAsync(string filePath, int hduIndex);
        Task UnloadAsync();
        Task SetSubcubeAsync(SubcubeBounds bounds);

        event Kernel.DatasetLoadedHandler    DatasetLoaded;
        event Kernel.DatasetUnloadedHandler  DatasetUnloaded;
        event Kernel.SubcubeChangedHandler   SubcubeChanged;
    }
}
