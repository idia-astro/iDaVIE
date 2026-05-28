// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeCameraDriver — one of the four §6.3 splits of VolumeDataSetRenderer.
// Encapsulates the transform-derived shader state: model matrix, region
// highlight bounds in object space, and the four mask-voxel offsets that the
// mask shader uses to slip the cube corners through the local-to-world matrix.
//
// Pulling these out of the shader-binder keeps VolumeMaterialBinder a pure
// settings-to-uniforms map. Any uniform whose value reads from the renderer's
// Transform lives here; the binder never touches transform.

using UnityEngine;
using iDaVIE.Kernel.Contracts;            // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;      // CartesianCoord, VolumeExtents
using iDaVIE.Rendering.Contracts;         // IRenderSettings

namespace iDaVIE.Rendering
{
    public sealed class VolumeCameraDriver
    {
        private readonly IVolumeDataSet  _volume;
        private readonly RegionSelection _region;
        private readonly float           _selectionSaturateFactor;

        public VolumeCameraDriver(IVolumeDataSet volume, RegionSelection region,
                                  float selectionSaturateFactor)
        {
            _volume                   = volume;
            _region                   = region;
            _selectionSaturateFactor  = selectionSaturateFactor;
        }

        public void Apply(Material rayMaterial, Material maskMaterial,
                          Transform rendererTransform, IRenderSettings settings)
        {
            BindRegionHighlight(rayMaterial);
            BindMaskTransform(maskMaterial, rendererTransform, settings);
        }

        private void BindRegionHighlight(Material rayMaterial)
        {
            if (!_region.IsActive)
            {
                rayMaterial.SetFloat(ShaderIds.HighlightSaturateFactor, 1f);
                return;
            }

            var extents = _volume.Extents;
            var start   = NormalisedObjectSpace(_region.RegionStartVoxel, extents);
            var end     = NormalisedObjectSpace(_region.RegionEndVoxel,   extents);
            // Pad by half a voxel on the low side so a single-voxel region renders
            // as a visible cell rather than a degenerate plane.
            var pad     = new Vector3(1f / extents.NAxis1, 1f / extents.NAxis2, 1f / extents.NAxis3);
            rayMaterial.SetVector(ShaderIds.HighlightMin, Vector3.Min(start, end) - pad);
            rayMaterial.SetVector(ShaderIds.HighlightMax, Vector3.Max(start, end));
            rayMaterial.SetFloat (ShaderIds.HighlightSaturateFactor, _selectionSaturateFactor);
        }

        private void BindMaskTransform(Material maskMaterial, Transform rendererTransform,
                                       IRenderSettings settings)
        {
            if (_volume.MaskEditState == null) return;

            var model   = rendererTransform.localToWorldMatrix;
            var extents = _volume.Extents;
            var half    = 0.5f * settings.MaskVoxelSize;
            var offsets = new Vector4[4]
            {
                model.MultiplyVector(half * new Vector3(-1f / extents.NAxis1, -1f / extents.NAxis2, -1f / extents.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / extents.NAxis1, -1f / extents.NAxis2, +1f / extents.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / extents.NAxis1, +1f / extents.NAxis2, -1f / extents.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / extents.NAxis1, +1f / extents.NAxis2, +1f / extents.NAxis3)),
            };
            maskMaterial.SetVectorArray(ShaderIds.MaskVoxelOffsets, offsets);
            maskMaterial.SetMatrix     (ShaderIds.ModelMatrix,      model);
        }

        private static Vector3 NormalisedObjectSpace(CartesianCoord voxel, VolumeExtents extents)
            => new((float)voxel.X / extents.NAxis1 - 0.5f,
                   (float)voxel.Y / extents.NAxis2 - 0.5f,
                   (float)voxel.Z / extents.NAxis3 - 0.5f);
    }
}
