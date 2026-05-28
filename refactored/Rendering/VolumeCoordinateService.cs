// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeCoordinateService — extracts ~7 coordinate-math methods from
// VolumeDataSetRenderer (lines 616-787 and 1240-1259).
//
// Refactor delta:
//   - Unity-free. No transform.InverseTransformPoint; the caller passes
//     local-space positions (the only place world↔local conversion legitimately
//     happens is the Unity ACL on the renderer's transform).
//   - Information Expert (GRASP) — coordinate math is owned by the service
//     that knows the cube dimensions and subset bounds, not the MonoBehaviour.
//   - Satisfies brief §4.2.3 (no UnityEngine in domain math).
//
// Legacy methods absorbed:
//   ConvertWorldPositionToDataCubePosition           → WorldToCubeLocal (input is now local space)
//   ConvertWorldRotationToDatacubeRotation            → CubeRotationFromLocalRotation
//   GetVoxelPositionDataSpace (×2)                    → VoxelFromCursorLocal / VoxelFromLocalPosition
//   GetVoxelPositionWorldSpace                        → ClampedVoxelFromLocalPosition
//   VolumePositionToLocalPosition                     → VolumeToLocal
//   LocalPositionToVolumePosition                     → LocalToVolume
//   GetCubeDimensions                                 → Replaced by IVolumeDataSet.Extents

using iDaVIE.Kernel.Contracts;           // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, VolumeExtents, SubcubeBounds

namespace iDaVIE.Rendering
{
    public readonly record struct LocalPosition(float X, float Y, float Z);

    public sealed class VolumeCoordinateService
    {
        private readonly IVolumeDataSet _volume;

        public VolumeCoordinateService(IVolumeDataSet volume) => _volume = volume;

        /// <summary>Local-space [-0.5, +0.5]³ position → continuous cube-space position.</summary>
        public LocalPosition LocalToCube(LocalPosition localPosition)
        {
            var e = _volume.Extents;
            return new LocalPosition((localPosition.X + 0.5f) * e.NAxis1,
                                     (localPosition.Y + 0.5f) * e.NAxis2,
                                     (localPosition.Z + 0.5f) * e.NAxis3);
        }

        /// <summary>Inverse of LocalToCube.</summary>
        public LocalPosition CubeToLocal(LocalPosition cubePosition)
        {
            var e = _volume.Extents;
            return new LocalPosition(cubePosition.X / e.NAxis1 - 0.5f,
                                     cubePosition.Y / e.NAxis2 - 0.5f,
                                     cubePosition.Z / e.NAxis3 - 0.5f);
        }

        /// <summary>Cube-space (continuous) → discrete voxel, including subcube offset.</summary>
        public CartesianCoord VoxelFromCubePosition(LocalPosition cubePosition)
        {
            var b = _volume.SubcubeBounds;
            return new CartesianCoord(
                (int)System.Math.Floor(cubePosition.X) + b.XMin,
                (int)System.Math.Floor(cubePosition.Y) + b.YMin,
                (int)System.Math.Floor(cubePosition.Z) + b.ZMin);
        }

        /// <summary>Clamped voxel — guarantees the result is inside the cube.</summary>
        public CartesianCoord ClampedVoxelFromCubePosition(LocalPosition cubePosition)
        {
            var e = _volume.Extents;
            var v = VoxelFromCubePosition(cubePosition);
            return new CartesianCoord(
                Clamp(v.X, 1, e.NAxis1),
                Clamp(v.Y, 1, e.NAxis2),
                Clamp(v.Z, 1, e.NAxis3));
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
