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
        Task LoadAsync(string filePath, int hduIndex);

        /// <summary>Releases the current volume and raises DatasetUnloaded.</summary>
        Task UnloadAsync();

        /// <summary>Crops the active volume to a new subcube. Raises SubcubeChanged.</summary>
        Task SetSubcubeAsync(SubcubeBounds bounds);

        event Kernel.DatasetLoadedHandler    DatasetLoaded;
        event Kernel.DatasetUnloadedHandler  DatasetUnloaded;
        event Kernel.SubcubeChangedHandler   SubcubeChanged;
    }
}
