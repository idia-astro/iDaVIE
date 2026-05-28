// SPDX-License-Identifier: LGPL-3.0-or-later
// IVolumeDataSet — OWNED BY ST1 (kameel-case / Terminal Ses). Reproduced here
// as a *reference declaration* so the ST3 / ST5 refactor skeletons in this
// directory compile-as-illustrated; the authoritative version lives in
// shared_interfaces.md §1.6 and ships with ST1's kernel assembly.
//
// Consumed in this refactor by:
//   • Rendering/VolumeDataSetRenderer.cs        (Bind injection)
//   • Rendering/VolumeCoordinateService.cs      (Extents, SubcubeBounds)
//   • Rendering/RegionSelection.cs              (RawVoxelAccess, MaskEditState)
//   • Rendering/VolumePersistenceService.cs     (FilePath, HduIndex)
//   • Features/FeatureSetService.cs             (lifecycle reset on DatasetUnloaded)
//   • Features/FeatureFactory.cs                (Extents bounds-check via Feature)
//
// Resolution lines 5, 6 / M-02, M-27.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, VolumeExtents, SubcubeBounds, AxisUnits, DataStats, HistogramData
using iDaVIE.Data;                       // IRawVoxelAccess, IMaskEditState

namespace iDaVIE.Kernel.Contracts
{
    public enum LoadStatus { Unloaded, Loading, Loaded, Error }

    /// <summary>Read-only cross-team view of the volume aggregate.
    /// Mutation goes through IVolumeLoader (ST1). Native sub-ports
    /// (RawVoxelAccess, MaskEditState) are reached through this aggregate per M-27.</summary>
    public interface IVolumeDataSet
    {
        LoadStatus    Status        { get; }
        string        FilePath      { get; }
        int           HduIndex      { get; }

        VolumeExtents Extents       { get; }
        SubcubeBounds SubcubeBounds { get; }

        IReadOnlyDictionary<string, string> HeaderDictionary { get; }

        DataStats     GetStats();
        HistogramData GetHistogram();
        AxisUnits     GetAxisUnits();

        string FormatCoord(CartesianCoord coord);

        IRawVoxelAccess RawVoxelAccess { get; }
        IMaskEditState  MaskEditState  { get; }
    }
}
