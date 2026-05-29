// SPDX-License-Identifier: LGPL-3.0-or-later
// IMaskStateCapture — ST2 persistence port (M-16). Consumed by ST7's
// SaveUseCase / LoadUseCase to snapshot the mask buffer.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data.Contracts
{
    /// <summary>RLE-encoded mask buffer per ST2's persistence design
    /// (shared_interfaces.md §2.1).</summary>
    public sealed class MaskStateDto
    {
        public int           SchemaVersion { get; set; } = 1;
        public VolumeExtents Extents       { get; set; }
        /// <summary>(maskValue, runLength) pairs.</summary>
        public List<(short MaskValue, int RunLength)> Rle { get; set; } = new();
    }

    public interface IMaskStateCapture
    {
        MaskStateDto Capture();
        void         Restore(MaskStateDto dto);
    }
}
