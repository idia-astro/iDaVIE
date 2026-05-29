// SPDX-License-Identifier: LGPL-3.0-or-later
// IVolumeStateCapture — ST1 persistence port (M-16). Consumed by ST7's
// SaveUseCase / LoadUseCase to snapshot the currently loaded volume.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel.Contracts
{
    /// <summary>DTO captured per ST1's persistence design (shared_interfaces.md §1.8).</summary>
    public sealed class VolumeStateDto
    {
        public int           SchemaVersion { get; set; } = 1;
        public string        FilePath      { get; set; } = "";
        public int           HduIndex      { get; set; }
        public SubcubeBounds SubcubeBounds { get; set; }
        public double?       RestFrequencyGHz { get; set; }
        public Dictionary<string, string> HeaderOverrides { get; set; } = new();
    }

    public interface IVolumeStateCapture
    {
        VolumeStateDto Capture();
        void           Restore(VolumeStateDto dto);
    }
}
