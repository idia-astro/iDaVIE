// SPDX-License-Identifier: LGPL-3.0-or-later
// ST2-OWNED ports consumed by the ST5 Feature System and by the ST3 Rendering
// Engine. Reproduced as reference declarations only — authoritative versions
// live in shared_interfaces.md §2 and ship with ST2's data-plugin assembly.
//
// SourceStats / ISourceStatsProvider / IDataAnalysisPlugin live in
// iDaVIE.Features per shared_interfaces.md §5.5 (resolution lines 8, 16, 17) —
// declared in Features/SourceStats.cs.
//
// Consumed in this refactor by:
//   • Features/FeatureFactory.cs            (ICoordinateTransformer for WCS projection)
//   • Features/VoTableSaver.cs              (ICoordinateTransformer — sky coords)
//   • Rendering/MaskEditingService.cs       (IMaskMutationService)
//   • Rendering/VolumePersistenceService.cs (IFitsMaskWriter, IFitsCubeReader — ST3-internal)

using System.Collections.Generic;
using System.Numerics;                       // System.Numerics.Vector2 — plain .NET, not UnityEngine
using iDaVIE.Kernel.Contracts.Types;         // CartesianCoord, SubcubeBounds
using iDaVIE.Rendering.Contracts;            // MaskMode (ST3)

namespace iDaVIE.Data
{
    /// <summary>World (sky + spectral) coordinate per shared_interfaces.md §2.
    /// Single spectral unit; RA/Dec carry no unit string because the WCS engine
    /// always normalises to degrees at this seam.</summary>
    public readonly record struct WorldCoord(
        double RightAscension,
        double Declination,
        double SpectralValue,
        string SpectralUnit)
    {
        public static readonly WorldCoord Invalid =
            new(double.NaN, double.NaN, double.NaN, string.Empty);

        public bool IsValid =>
            !double.IsNaN(RightAscension) &&
            !double.IsNaN(Declination)    &&
            !double.IsNaN(SpectralValue);
    }

    /// <summary>Narrow ISP facade for ST5 (M-06). Realised by WcsTransformPlugin
    /// on the same engine that backs IWcsPlugin / IWcsMapping.</summary>
    public interface ICoordinateTransformer
    {
        /// <summary>Voxel → world coordinate. Returns WorldCoord.Invalid if the
        /// transform is undefined at the given position.</summary>
        WorldCoord Transform(CartesianCoord pixelCoord);

        /// <summary>World → voxel coordinate (nearest-integer). Used by FeatureFactory
        /// on the Imported flow to project Ra/Dec + spectral catalogue inputs back to
        /// cube voxel space. Throws InvalidOperationException if no WCS frame is loaded.</summary>
        CartesianCoord PixelOf(WorldCoord worldCoord);
    }

    // ── Mask mutation (shared_interfaces.md §2, resolution line 15) ─────────

    /// <summary>BrushStroke takes ST6's resolution-line-14 shape: axis + slice
    /// index + pre-rasterised voxels in slice-local (U,V) coordinates + the
    /// subset of brush configuration ST2 actually consumes.</summary>
    public readonly struct BrushStroke
    {
        public readonly int                          Axis;
        public readonly int                          SliceIndex;
        public readonly IReadOnlyList<VoxelCoord2D>  VoxelCoords;
        public readonly StrokePaintConfig            PaintConfig;

        public BrushStroke(int axis, int sliceIndex,
                           IReadOnlyList<VoxelCoord2D> voxelCoords,
                           StrokePaintConfig paintConfig)
        {
            Axis = axis; SliceIndex = sliceIndex;
            VoxelCoords = voxelCoords; PaintConfig = paintConfig;
        }
    }

    public readonly struct VoxelCoord2D
    {
        public readonly int U;
        public readonly int V;
        public VoxelCoord2D(int u, int v) { U = u; V = v; }
    }

    public enum BrushPaintMode { Add, Remove, Replace }

    /// <summary>The subset of BrushConfig that ST2 needs to apply a pre-rasterised
    /// stroke. Radius is excluded — it is already baked into the voxel list.</summary>
    public readonly record struct StrokePaintConfig(
        int            SourceId,
        bool           Additive,
        BrushPaintMode PaintMode);

    /// <summary>Minimal payload accompanying PaintPolygon when ST4's full
    /// BrushConfig isn't appropriate.</summary>
    public readonly record struct PaintConfig(short SourceId, bool Additive);

    /// <summary>Source-id entry returned by GetMaskedSources for ST6's source list panel.</summary>
    public readonly struct SourceEntry
    {
        public readonly short MaskValue;
        public SourceEntry(short maskValue) { MaskValue = maskValue; }
    }

    /// <summary>Single entry point for all mask edits (ST2 design — resolution
    /// line 15). PaintPolygon is retained because both desktop polygon paint and
    /// ST6's rasterised BrushStroke path are needed.</summary>
    public interface IMaskMutationService
    {
        void ApplyBrush(BrushStroke stroke);
        void FinishStroke();

        void PaintPolygon(
            int                    axis,
            int                    sliceIndex,
            IReadOnlyList<Vector2> polygon,
            PaintConfig            config);

        void Undo();
        void Redo();

        void InitialiseMask();

        /// <summary>Writes the buffer to FITS. overwrite=true ⇒ write to the open mask path.</summary>
        int SaveMask(bool overwrite);

        MaskMode MaskMode     { get; set; }
        bool     DisplayMask  { get; set; }
        short    NewSourceId  { get; set; }
        short    CursorSource { get; set; }

        IReadOnlyList<SourceEntry> GetMaskedSources();
    }

    // ── ST3-internal ports (not in shared_interfaces.md) ────────────────────

    /// <summary>FITS mask writer port — three save modes serve VolumePersistenceService.</summary>
    public interface IFitsMaskWriter
    {
        void WriteNew(string cubeFilePath, int hduIndex, string outputPath);
        void WriteCopy(string sourceMaskPath, string targetPath);
        void Overwrite(string maskPath);
    }

    /// <summary>FITS cube reader port — exposes the SaveSubCubeFromOriginal capability.</summary>
    public interface IFitsCubeReader
    {
        void SaveSubCube(string cubeFilePath, SubcubeBounds bounds,
                         string? maskFilePath, string outputPath);
    }
}
