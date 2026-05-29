// SPDX-License-Identifier: LGPL-3.0-or-later
// IVolumeStateCapture — ST1 persistence port (M-16). Consumed by ST7's
// SaveUseCase / LoadUseCase to snapshot the currently loaded volume.

using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel.Contracts.Persistence
{
    public sealed class VolumeEntryDto
    {
        public string FilePath { get; init; } = string.Empty;
        public int HduIndex { get; init; }
        public SubcubeBoundsDto? SubcubeBounds { get; init; }
        public string? AltSpectralFrame { get; init; }
        public double? RestFrequencyHz { get; init; }
        public System.Collections.Generic.Dictionary<string, string> AxisAttributeOverrides { get; init; } = new();
    }

    public sealed class SubcubeBoundsDto
    {
        public int XMin { get; init; }
        public int XMax { get; init; }
        public int YMin { get; init; }
        public int YMax { get; init; }
        public int ZMin { get; init; }
        public int ZMax { get; init; }

        public static SubcubeBoundsDto From(SubcubeBounds b) =>
            new()
            {
                XMin = b.XMin,
                XMax = b.XMax,
                YMin = b.YMin,
                YMax = b.YMax,
                ZMin = b.ZMin,
                ZMax = b.ZMax
            };

        public SubcubeBounds ToDomain() =>
            new(XMin, XMax, YMin, YMax, ZMin, ZMax);
    }

    /// <summary>DTO captured per ST1's persistence design (shared_interfaces.md §1.8).</summary>
    public sealed class VolumeStateDto
    {
        public string SchemaVersion { get; init; } = "1.0.0";
        public System.Collections.Generic.List<VolumeEntryDto> Volumes { get; init; } = new();
        public int ActiveVolumeIndex { get; init; } = -1;

        // Compatibility properties for earlier skeleton drafts.
        public string FilePath
        {
            get => Volumes.Count > 0 ? Volumes[0].FilePath : string.Empty;
            init
            {
                if (!string.IsNullOrEmpty(value) && Volumes.Count == 0)
                    Volumes.Add(new VolumeEntryDto { FilePath = value });
            }
        }

        public int HduIndex => Volumes.Count > 0 ? Volumes[0].HduIndex : 0;
        public SubcubeBounds SubcubeBounds => Volumes.Count > 0 && Volumes[0].SubcubeBounds != null
            ? Volumes[0].SubcubeBounds!.ToDomain()
            : default;
    }

    public interface IVolumeStateCapture
    {
        VolumeStateDto Capture();
        void           Restore(VolumeStateDto dto);
    }
}
