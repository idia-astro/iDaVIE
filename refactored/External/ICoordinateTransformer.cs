// SPDX-License-Identifier: LGPL-3.0-or-later
// ST2-OWNED ports consumed by the ST5 Feature System and by the ST3 Rendering
// Engine. Reproduced as reference declarations only — authoritative versions
// live in shared_interfaces.md §2 / §5.5 and ship with ST2's data-plugin assembly.
//
// Consumed in this refactor by:
//   • Features/FeatureFactory.cs            (ICoordinateTransformer for WCS projection;
//                                            ISourceStatsProvider for Mask features)
//   • Features/FeatureSetService.cs         (ISourceStatsProvider.SourceStatsUpdated)
//   • Features/VoTableSaver.cs              (both — flux-weighted centroids + sky coords)
//   • Rendering/MaskEditingService.cs       (IMaskMutationService)
//   • Rendering/VolumePersistenceService.cs (IFitsMaskWriter, IFitsCubeReader)

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord

namespace iDaVIE.Data
{
    /// <summary>WCS world-coordinate payload — RA/Dec/spectral with unit strings.
    /// Inbound only across the ST2/ST5 boundary; not re-exposed by ST5.</summary>
    public readonly record struct WorldCoord(
        double Ra, double Dec, double Spectral,
        string RaUnit, string DecUnit, string SpectralUnit)
    {
        public static readonly WorldCoord Invalid =
            new(double.NaN, double.NaN, double.NaN, "", "", "");
    }

    public interface ICoordinateTransformer
    {
        /// <summary>Voxel → world coordinate. Returns WorldCoord.Invalid if the
        /// transform is undefined at the given position.</summary>
        WorldCoord Transform(CartesianCoord pixelCoord);
    }

    /// <summary>Stats payload POCO; mapped from legacy DataAnalysis.SourceStats
    /// fields per the "// ← x" comments in ST5_interface.md §3.</summary>
    public sealed class SourceStats
    {
        public long           VoxelCount           { get; init; }   // ← numVoxels
        public CartesianCoord BoundsMin            { get; init; }   // ← minX/Y/Z
        public CartesianCoord BoundsMax            { get; init; }   // ← maxX/Y/Z
        public double         TotalFlux            { get; init; }   // ← sum
        public double         PeakFlux             { get; init; }   // ← peak
        public CartesianCoord FluxWeightedCentroid { get; init; }   // ← cX/Y/Z
        public double         ChannelW20           { get; init; }
        public double         VeloW20              { get; init; }
        public double         ChannelVsys          { get; init; }
        public double         VeloVsys             { get; init; }
        public IReadOnlyList<double> SpectralProfile { get; init; } = Array.Empty<double>();
        public int            ZStartChannel        { get; init; }
    }

    public interface ISourceStatsProvider
    {
        SourceStats?                            GetStatsForSource(int originId);
        IReadOnlyDictionary<int, SourceStats>   GetAllStats();
        event Action<int>                       SourceStatsUpdated;
    }

    // --- Forward declarations for completeness; full schemas in shared_interfaces.md ---

    /// <summary>Raw voxel sampling — sub-port held by IVolumeDataSet (M-27).</summary>
    public interface IRawVoxelAccess
    {
        float GetValue(CartesianCoord voxel);
    }

    /// <summary>Mask reading sub-port held by IVolumeDataSet (shared_interfaces §1.7).</summary>
    public interface IMaskEditState
    {
        short GetMaskValue(int x, int y, int z);
        short[] GetMaskSlice(int axis, int sliceIndex);
    }

    /// <summary>Mask mutation surface — kept narrow per resolution line 15.
    /// Used by ST3's MaskEditingService.</summary>
    public interface IMaskMutationService
    {
        void CreateEmpty();
        void PaintVoxels(IReadOnlyList<CartesianCoord> voxels, short value);
        void FlushStroke();
        bool CanUndo { get; }
        bool CanRedo { get; }
        void Undo();
        void Redo();
    }

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
        void SaveSubCube(string cubeFilePath, iDaVIE.Kernel.Contracts.Types.SubcubeBounds bounds,
                         string? maskFilePath, string outputPath);
    }
}
