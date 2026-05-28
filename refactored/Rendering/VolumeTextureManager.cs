// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeTextureManager — one of the four §6.3 splits of VolumeDataSetRenderer.
// Owns every Unity GPU resource the renderer uses: the instantiated ray-marching
// and mask Materials, the volume Texture3D handed up from ST2's IRawVoxelAccess,
// and the IMaskGpuBuffers handle the mask overlay draws against.
//
// Generation handshake: IRawVoxelAccess.CurrentGeneration is bumped by ST2 on
// every load / subcube change / unload (shared_interfaces.md §1.4). This class
// is the single place that re-uploads the Texture3D when the generation moves,
// so the rest of the rendering split treats the bound texture as stable for the
// life of one frame.

using System;
using UnityEngine;
using iDaVIE.Kernel.Contracts;            // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Plugins;    // IRawVoxelAccess, VoxelBufferDescriptor

namespace iDaVIE.Rendering
{
    /// <summary>ST3-internal port: GPU compute buffers backing the mask overlay.
    /// Realised in the Unity ACL beside the IMaskEditState adapter, so no Unity
    /// type crosses the cross-team IVolumeDataSet contract.</summary>
    public interface IMaskGpuBuffers
    {
        ComputeBuffer ExistingMaskBuffer  { get; }
        ComputeBuffer AddedMaskBuffer     { get; }
        int           AddedMaskEntryCount { get; }
    }

    public sealed class VolumeTextureManager : IDisposable
    {
        private static readonly int VolumeTexId = Shader.PropertyToID("_DataCube");

        private readonly Material        _rayTemplate;
        private readonly Material        _maskTemplate;
        private readonly IVolumeDataSet  _volume;
        private long                     _uploadedGeneration = -1;
        private Texture3D                _volumeTexture;

        public Material        RayMaterial  { get; }
        public Material        MaskMaterial { get; }
        public IMaskGpuBuffers MaskBuffers  { get; }

        public VolumeTextureManager(Material rayTemplate, Material maskTemplate,
                                    IVolumeDataSet volume, IMaskGpuBuffers maskBuffers)
        {
            _rayTemplate  = rayTemplate;
            _maskTemplate = maskTemplate;
            _volume       = volume;
            MaskBuffers   = maskBuffers;

            RayMaterial  = UnityEngine.Object.Instantiate(rayTemplate);
            MaskMaterial = UnityEngine.Object.Instantiate(maskTemplate);
        }

        /// <summary>
        /// Re-uploads the volume Texture3D iff ST2 has bumped CurrentGeneration since
        /// the last call. Caller invokes this once per frame before binding shader
        /// uniforms; the no-op path on a stable generation is a single long compare.
        /// </summary>
        public void EnsureVolumeTextureCurrent()
        {
            var access = _volume.RawVoxelAccess;
            if (access == null) return;
            if (access.CurrentGeneration == _uploadedGeneration) return;

            UploadVolumeTexture(access.Descriptor);
            _uploadedGeneration = access.CurrentGeneration;
        }

        private void UploadVolumeTexture(VoxelBufferDescriptor descriptor)
        {
            // The descriptor's DataPtr is owned by ST2's unmanaged allocator; the
            // Texture3D upload copies it into a GPU resource ST3 owns for as long
            // as the generation matches. Re-creating the Texture3D on every
            // generation change is required because the cube dimensions can also
            // change (subcube selection in ST1 IVolumeLoader.SetSubcubeAsync).
            if (_volumeTexture != null) UnityEngine.Object.Destroy(_volumeTexture);

            _volumeTexture = new Texture3D(descriptor.SizeX, descriptor.SizeY, descriptor.SizeZ,
                                           TextureFormat.RFloat, mipChain: false);
            // TODO: copy descriptor.Length floats from descriptor.DataPtr into the
            // Texture3D via Texture3D.SetPixelData<float>(...). Native-side copy
            // signature lives in ST2 (shared_interfaces.md §1.4 FitsVoxelBuffer).
            RayMaterial.SetTexture(VolumeTexId, _volumeTexture);
        }

        public void Dispose()
        {
            if (RayMaterial   != null) UnityEngine.Object.Destroy(RayMaterial);
            if (MaskMaterial  != null) UnityEngine.Object.Destroy(MaskMaterial);
            if (_volumeTexture != null) UnityEngine.Object.Destroy(_volumeTexture);
        }
    }
}
