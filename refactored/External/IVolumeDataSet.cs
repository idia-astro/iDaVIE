// SPDX-License-Identifier: LGPL-3.0-or-later
// Kernel-owned types (ST1) — reproduced here as *reference declarations* so the
// ST3 / ST5 refactor skeletons compile-as-illustrated. The authoritative versions
// live in shared_interfaces.md §1.4, §1.6, §1.7 and ship with ST1's kernel assembly.
//
// Consumed in this refactor by:
//   • Rendering/VolumeDataSetRenderer.cs        (Bind injection)
//   • Rendering/VolumeCoordinateService.cs      (Extents, SubcubeBounds)
//   • Rendering/VolumeTextureManager.cs         (RawVoxelAccess, generation handshake)
//   • Rendering/RegionSelection.cs              (RawVoxelAccess, MaskEditState)
//   • Rendering/VolumePersistenceService.cs     (FilePath, HduIndex)
//   • Features/FeatureSetService.cs             (lifecycle reset on DatasetUnloaded)
//   • Features/FeatureFactory.cs                (Extents bounds-check via Feature)

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, VolumeExtents, SubcubeBounds, AxisUnits, DataStats, HistogramData
using iDaVIE.Kernel.Contracts.Plugins;   // IRawVoxelAccess (sub-port held by IVolumeDataSet per M-27)

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

    /// <summary>Mask reading sub-port held by IVolumeDataSet (shared_interfaces §1.7,
    /// resolution line 10 — ST6 shape). Realised by ST2's MaskEditService.</summary>
    public interface IMaskEditState
    {
        /// <summary>Mask value (source ID; 0 = unmasked) at the given voxel coordinate.</summary>
        short GetMaskValue(int x, int y, int z);

        /// <summary>Mask values for a 2-D slice. axis: 0 = X, 1 = Y, 2 = Z.
        /// Layout mirrors IRawVoxelAccess.GetSlice conventions.</summary>
        short[] GetMaskSlice(int axis, int sliceIndex);
    }
}

namespace iDaVIE.Kernel.Contracts.Plugins
{
    /// <summary>Describes the layout of ST2's unmanaged voxel buffer
    /// (shared_interfaces.md §1.4). Generation MUST be compared against
    /// IRawVoxelAccess.CurrentGeneration before dereferencing DataPtr.</summary>
    public sealed class VoxelBufferDescriptor
    {
        public IntPtr         DataPtr      { get; init; }
        public long           Length       { get; init; }
        public int            SizeX        { get; init; }
        public int            SizeY        { get; init; }
        public int            SizeZ        { get; init; }
        public CartesianCoord RegionOffset { get; init; }
        public long           Generation   { get; init; }
    }

    /// <summary>Raw voxel sampling — sub-port held by IVolumeDataSet (M-27).
    /// ST1's design per resolution line 7 / shared_interfaces.md §1.4.
    /// Pointer is valid only while the volume is loaded; must not be cached
    /// across SetSubcubeAsync.</summary>
    public interface IRawVoxelAccess
    {
        VoxelBufferDescriptor Descriptor { get; }

        /// <summary>Monotonically incremented on every load / subcube / unload.
        /// A descriptor whose Generation differs from this value MUST NOT be
        /// dereferenced; refetch via Descriptor.</summary>
        long CurrentGeneration { get; }

        /// <summary>Copies a single XY slice at spectral channel zIndex to managed memory.</summary>
        float[] GetSlice(int zIndex);

        /// <summary>Copies a rectangular XY region at channel zIndex into destination.</summary>
        void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination);
    }
}
