// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeDataSetRenderer — thin Unity MonoBehaviour that composes the four
// brief §6.3 splits and the IMaskMode strategy. It owns the Unity lifecycle
// (Awake / Update / OnRenderObject / OnDestroy) and the per-frame dirty flag;
// every concern with its own name has its own class:
//
//   VolumeMaterialBinder      — threshold / scaling / colour-map / projection / vignette
//   VolumeTextureManager      — material instances + volume Texture3D + IMaskGpuBuffers
//   VolumeCameraDriver        — model matrix + region highlight + mask voxel offsets
//   FoveatedSamplingPolicy    — foveation step budget
//   IMaskMode (Strategy)      — Apply / Inverse / Isolate / Disabled (MaskModeRegistry)
//
// SOLID/GRASP impact:
//   • SRP — this class only drives the Unity lifecycle and the dirty-flag pull.
//   • OCP — projection / scaling / mask modes change through their owning
//           class; nothing in this file switches on settings enums.
//   • DIP — every collaborator is injected via Bind; no Config / singleton reach.
//   • GRASP Controller — single Unity-side entry point that delegates work down.

using UnityEngine;
using iDaVIE.Kernel.Contracts;           // IVolumeDataSet
using iDaVIE.Rendering.Contracts;        // IRenderSettings

namespace iDaVIE.Rendering
{
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class VolumeDataSetRenderer : MonoBehaviour
    {
        [SerializeField] private Material rayMarchingMaterial;
        [SerializeField] private Material maskMaterial;
        [SerializeField] private float    selectionSaturateFactor = 2.5f;

        // Injected by the composition root in Bind.
        private IRenderSettings        _settings;
        private IVolumeDataSet         _volume;
        private RegionSelection        _region;
        private VolumeTextureManager   _textures;
        private VolumeMaterialBinder   _materialBinder;
        private VolumeCameraDriver     _cameraDriver;
        private FoveatedSamplingPolicy _foveation;
        private MaskModeRegistry       _maskModes;
        private bool                   _settingsDirty;

        public void Bind(IRenderSettings settings, IVolumeDataSet volume,
                         RegionSelection region, IMaskGpuBuffers maskBuffers)
        {
            _settings       = settings;
            _volume         = volume;
            _region         = region;
            _textures       = new VolumeTextureManager(rayMarchingMaterial, maskMaterial,
                                                       volume, maskBuffers);
            _materialBinder = new VolumeMaterialBinder();
            _cameraDriver   = new VolumeCameraDriver(volume, region, selectionSaturateFactor);
            _foveation      = new FoveatedSamplingPolicy();
            _maskModes      = new MaskModeRegistry();

            GetComponent<MeshRenderer>().material = _textures.RayMaterial;

            _settings.SettingsChanged += OnSettingsChanged;
            _region.RegionChanged     += OnSettingsChanged;
            _region.RegionCleared     += OnSettingsChanged;
            _settingsDirty = true;
        }

        private void OnSettingsChanged() => _settingsDirty = true;

        private void Update()
        {
            _textures.EnsureVolumeTextureCurrent();
            if (!_settingsDirty) return;

            var ray  = _textures.RayMaterial;
            var mask = _textures.MaskMaterial;

            _materialBinder.Apply(ray, _settings);
            _foveation.Apply     (ray, _settings);
            _cameraDriver.Apply  (ray, mask, transform, _settings);

            _maskModes.ForEnum(_settings.MaskMode).BindToRayMaterial(ray);
            BindMaskMaterialUniforms(mask);

            _settingsDirty = false;
        }

        // Mask-material uniforms that are independent of the IMaskMode strategy.
        // Strategy-specific bindings live on the strategy itself.
        private void BindMaskMaterialUniforms(Material mask)
        {
            if (_volume.MaskEditState == null) return;
            mask.SetFloat(ShaderIds.MaskVoxelSize,     _settings.MaskVoxelSize);
            mask.SetColor(ShaderIds.MaskVoxelColor,    new Color(_settings.VignetteColor.R,
                                                                  _settings.VignetteColor.G,
                                                                  _settings.VignetteColor.B,
                                                                  _settings.VignetteColor.A));
            mask.SetInt  (ShaderIds.HighlightedSource, _region.HighlightedSource);
        }

        private void OnRenderObject()
        {
            if (!_settings.IsFullResolution || !_settings.DisplayMask) return;
            if (!_maskModes.ForEnum(_settings.MaskMode).RequiresOverlayDispatch) return;

            var buffers = _textures.MaskBuffers;
            if (buffers == null) return;

            var mask     = _textures.MaskMaterial;
            var existing = buffers.ExistingMaskBuffer;
            if (existing != null)
            {
                mask.SetBuffer(ShaderIds.MaskEntries, existing);
                mask.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, existing.count);
            }

            var added = buffers.AddedMaskBuffer;
            if (added != null && buffers.AddedMaskEntryCount > 0)
            {
                mask.SetBuffer(ShaderIds.MaskEntries, added);
                mask.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, buffers.AddedMaskEntryCount);
            }
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.SettingsChanged -= OnSettingsChanged;
            if (_region   != null)
            {
                _region.RegionChanged -= OnSettingsChanged;
                _region.RegionCleared -= OnSettingsChanged;
            }
            _textures?.Dispose();
        }
    }
}
