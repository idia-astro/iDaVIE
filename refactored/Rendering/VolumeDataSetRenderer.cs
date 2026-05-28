// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeDataSetRenderer — post-refactor MonoBehaviour. Owned by ST3.
//
// Legacy: 1402 LOC, WMC 142, CBO 44, LCOM 96% (CRITICAL — T2 Baseline §3).
// Post-refactor target: ≤ 200 LOC, WMC ≤ 40, CBO ≤ 14, LCOM ≤ 50%.
//
// This class now does ONE thing: it drives the ray-marching shader from a
// snapshot of IRenderSettings every frame, and forwards the per-frame
// VolumeData / MaskData GPU textures supplied by ST2. Every other concern
// the legacy class carried has been extracted:
//
//   Bootstrap / coroutine progress  → composition root (ST1) + IVolumeLoader
//   Shader-parameter binding        → THIS class (Update + OnRenderObject only)
//   Cursor / region state           → Rendering/RegionSelection.cs
//   Mask paint / brush              → Rendering/MaskEditingService.cs
//   Coordinate maths                → Rendering/VolumeCoordinateService.cs
//   SaveSubCube / SaveMask          → Rendering/VolumePersistenceService.cs
//   Rest frequency catalogue        → Rendering/IRestFrequencyCatalogue.cs
//   Feature interaction             → ST5 (IFeatureSelectionService consumer)
//
// SOLID/GRASP impact:
//   • SRP — single concern (shader binding from settings snapshot).
//   • OCP — projection / scaling / mask modes drive the shader through
//     IRenderSettings; adding a mode is an additive change to the enum.
//   • DIP — depends on IRenderSettings + IVolumeDataSet (interfaces), not on
//     VolumeDataSet / Config concretes.
//   • GRASP Indirection — IRenderSettings absorbs the formerly direct fields.

using UnityEngine;
using iDaVIE.Kernel.Contracts;           // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;     // FeatureColour
using iDaVIE.Rendering.Contracts;        // IRenderSettings, ProjectionMode, MaskMode

namespace iDaVIE.Rendering
{
    /// <summary>ST3-internal port: GPU compute buffers backing the mask overlay.
    /// Realised in the Unity ACL beside the IMaskEditState adapter, so no Unity
    /// type crosses the cross-team IVolumeDataSet contract.</summary>
    internal interface IMaskGpuBuffers
    {
        ComputeBuffer ExistingMaskBuffer  { get; }
        ComputeBuffer AddedMaskBuffer     { get; }
        int           AddedMaskEntryCount { get; }
    }

    [RequireComponent(typeof(MeshRenderer))]
    public sealed class VolumeDataSetRenderer : MonoBehaviour
    {
        [SerializeField] private Material rayMarchingMaterial;
        [SerializeField] private Material maskMaterial;
        [SerializeField] private float    selectionSaturateFactor = 2.5f;

        // Injected by the composition root in Awake/Start.
        private IRenderSettings _settings;
        private IVolumeDataSet  _volume;
        private RegionSelection _region;
        private IMaskGpuBuffers _maskBuffers;
        private Material        _rayMaterialInstance;
        private Material        _maskMaterialInstance;
        private bool            _settingsDirty;

        internal void Bind(IRenderSettings settings, IVolumeDataSet volume,
                           RegionSelection region, IMaskGpuBuffers maskBuffers)
        {
            _settings    = settings;
            _volume      = volume;
            _region      = region;
            _maskBuffers = maskBuffers;

            _rayMaterialInstance  = Instantiate(rayMarchingMaterial);
            _maskMaterialInstance = Instantiate(maskMaterial);
            GetComponent<MeshRenderer>().material = _rayMaterialInstance;

            _settings.SettingsChanged += OnSettingsChanged;
            _region.RegionChanged     += OnSettingsChanged;
            _region.RegionCleared     += OnSettingsChanged;
            _settingsDirty = true;
        }

        private void OnSettingsChanged() => _settingsDirty = true;

        private void Update()
        {
            if (!_settingsDirty) return;
            ApplyToShader();
            _settingsDirty = false;
        }

        private void ApplyToShader()
        {
            // Threshold + step budget (legacy 1028-1034).
            _rayMaterialInstance.SetFloat(MaterialID.ThresholdMin,  _settings.ThresholdMin);
            _rayMaterialInstance.SetFloat(MaterialID.ThresholdMax,  _settings.ThresholdMax);
            _rayMaterialInstance.SetFloat(MaterialID.MaxSteps,      _settings.MaxRayMarchSteps);
            _rayMaterialInstance.SetFloat(MaterialID.ColorMapIndex, (int)_settings.ColorMap);

            // Scaling pipeline (legacy 1036-1040).
            _rayMaterialInstance.SetInt  (MaterialID.ScaleType,     (int)_settings.ScalingType);
            _rayMaterialInstance.SetFloat(MaterialID.ScaleBias,     _settings.ScalingBias);
            _rayMaterialInstance.SetFloat(MaterialID.ScaleContrast, _settings.ScalingContrast);
            _rayMaterialInstance.SetFloat(MaterialID.ScaleAlpha,    _settings.ScalingAlpha);
            _rayMaterialInstance.SetFloat(MaterialID.ScaleGamma,    _settings.ScalingGamma);

            // Foveated rendering (legacy 1042-1054). When disabled, both step budgets
            // are pinned to MaxRayMarchSteps so the shader needs no conditional itself.
            _rayMaterialInstance.SetFloat(MaterialID.FoveationStart, _settings.FoveationStart);
            _rayMaterialInstance.SetFloat(MaterialID.FoveationEnd,   _settings.FoveationEnd);
            if (_settings.FoveatedRendering)
            {
                _rayMaterialInstance.SetFloat(MaterialID.FoveationJitter,   _settings.FoveationJitter);
                _rayMaterialInstance.SetInt  (MaterialID.FoveatedStepsLow,  _settings.FoveatedStepsLow);
                _rayMaterialInstance.SetInt  (MaterialID.FoveatedStepsHigh, _settings.FoveatedStepsHigh);
            }
            else
            {
                _rayMaterialInstance.SetInt(MaterialID.FoveatedStepsLow,  _settings.MaxRayMarchSteps);
                _rayMaterialInstance.SetInt(MaterialID.FoveatedStepsHigh, _settings.MaxRayMarchSteps);
            }

            BindRegionHighlight();
            BindMaskParameters();

            // Projection mode keyword (legacy 1097-1104).
            if (_settings.ProjectionMode == ProjectionMode.AverageIntensityProjection)
                Shader.EnableKeyword("SHADER_AIP");
            else
                Shader.DisableKeyword("SHADER_AIP");

            // Vignette (legacy 1106-1109).
            _rayMaterialInstance.SetFloat(MaterialID.VignetteFadeStart, _settings.VignetteFadeStart);
            _rayMaterialInstance.SetFloat(MaterialID.VignetteFadeEnd,   _settings.VignetteFadeEnd);
            _rayMaterialInstance.SetFloat(MaterialID.VignetteIntensity, _settings.VignetteIntensity);
            _rayMaterialInstance.SetColor(MaterialID.VignetteColor,     ToColor(_settings.VignetteColor));
        }

        // Legacy 1056-1071. Voxel→object-space normalisation reads cube extents
        // from the IVolumeDataSet boundary; subcube offset was already folded in
        // by VolumeCoordinateService when RegionSelection captured the corners.
        private void BindRegionHighlight()
        {
            if (!_region.IsActive)
            {
                _rayMaterialInstance.SetFloat(MaterialID.HighlightSaturateFactor, 1f);
                return;
            }

            var e     = _volume.Extents;
            var start = NormalisedObjectSpace(_region.RegionStartVoxel, e);
            var end   = NormalisedObjectSpace(_region.RegionEndVoxel,   e);
            var pad   = new Vector3(1f / e.NAxis1, 1f / e.NAxis2, 1f / e.NAxis3);
            _rayMaterialInstance.SetVector(MaterialID.HighlightMin, Vector3.Min(start, end) - pad);
            _rayMaterialInstance.SetVector(MaterialID.HighlightMax, Vector3.Max(start, end));
            _rayMaterialInstance.SetFloat (MaterialID.HighlightSaturateFactor, selectionSaturateFactor);
        }

        // Legacy 1074-1095. MaskEditState == null ⇒ the cube has no mask, shader
        // branch is disabled and no mask-material work is needed.
        private void BindMaskParameters()
        {
            if (_volume.MaskEditState == null)
            {
                _rayMaterialInstance.SetInt(MaterialID.MaskMode, (int)MaskMode.Disabled);
                return;
            }

            _rayMaterialInstance.SetInt   (MaterialID.MaskMode,          (int)_settings.MaskMode);
            _maskMaterialInstance.SetFloat(MaterialID.MaskVoxelSize,     _settings.MaskVoxelSize);
            _maskMaterialInstance.SetColor(MaterialID.MaskVoxelColor,    ToColor(_settings.VignetteColor));
            _maskMaterialInstance.SetInt  (MaterialID.HighlightedSource, _region.HighlightedSource);

            var model = transform.localToWorldMatrix;
            var e     = _volume.Extents;
            var half  = 0.5f * _settings.MaskVoxelSize;
            var offsets = new Vector4[4]
            {
                model.MultiplyVector(half * new Vector3(-1f / e.NAxis1, -1f / e.NAxis2, -1f / e.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / e.NAxis1, -1f / e.NAxis2, +1f / e.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / e.NAxis1, +1f / e.NAxis2, -1f / e.NAxis3)),
                model.MultiplyVector(half * new Vector3(-1f / e.NAxis1, +1f / e.NAxis2, +1f / e.NAxis3)),
            };
            _maskMaterialInstance.SetVectorArray(MaterialID.MaskVoxelOffsets, offsets);
            _maskMaterialInstance.SetMatrix     (MaterialID.ModelMatrix,      model);
        }

        // Mask-overlay procedural draw (legacy 1142-1156). Pure GPU dispatch.
        private void OnRenderObject()
        {
            if (!_settings.IsFullResolution || !_settings.DisplayMask || _maskBuffers == null)
                return;

            var existing = _maskBuffers.ExistingMaskBuffer;
            if (existing != null)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, existing);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, existing.count);
            }

            var added = _maskBuffers.AddedMaskBuffer;
            if (added != null && _maskBuffers.AddedMaskEntryCount > 0)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, added);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskBuffers.AddedMaskEntryCount);
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
        }

        private static Vector3 NormalisedObjectSpace(CartesianCoord v, VolumeExtents e)
            => new((float)v.X / e.NAxis1 - 0.5f,
                   (float)v.Y / e.NAxis2 - 0.5f,
                   (float)v.Z / e.NAxis3 - 0.5f);

        private static Color ToColor(FeatureColour c) => new(c.R, c.G, c.B, c.A);

        // Shader property-id cache. Names mirror the legacy MaterialID block verbatim
        // so the binding contract with RayMarchedVolume.shader stays unchanged.
        private static class MaterialID
        {
            public static readonly int ThresholdMin            = Shader.PropertyToID("_ThresholdMin");
            public static readonly int ThresholdMax            = Shader.PropertyToID("_ThresholdMax");
            public static readonly int MaxSteps                = Shader.PropertyToID("_MaxSteps");
            public static readonly int ColorMapIndex           = Shader.PropertyToID("_ColorMapIndex");
            public static readonly int ScaleType               = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleBias               = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast           = Shader.PropertyToID("ScaleContrast");
            public static readonly int ScaleAlpha              = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma              = Shader.PropertyToID("ScaleGamma");
            public static readonly int FoveationStart          = Shader.PropertyToID("FoveationStart");
            public static readonly int FoveationEnd            = Shader.PropertyToID("FoveationEnd");
            public static readonly int FoveationJitter         = Shader.PropertyToID("FoveationJitter");
            public static readonly int FoveatedStepsLow        = Shader.PropertyToID("FoveatedStepsLow");
            public static readonly int FoveatedStepsHigh       = Shader.PropertyToID("FoveatedStepsHigh");
            public static readonly int HighlightMin            = Shader.PropertyToID("HighlightMin");
            public static readonly int HighlightMax            = Shader.PropertyToID("HighlightMax");
            public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");
            public static readonly int MaskMode                = Shader.PropertyToID("MaskMode");
            public static readonly int MaskEntries             = Shader.PropertyToID("MaskEntries");
            public static readonly int MaskVoxelSize           = Shader.PropertyToID("MaskVoxelSize");
            public static readonly int MaskVoxelOffsets        = Shader.PropertyToID("MaskVoxelOffsets");
            public static readonly int MaskVoxelColor          = Shader.PropertyToID("MaskVoxelColor");
            public static readonly int ModelMatrix             = Shader.PropertyToID("ModelMatrix");
            public static readonly int HighlightedSource       = Shader.PropertyToID("HighlightedSource");
            public static readonly int VignetteFadeStart       = Shader.PropertyToID("VignetteFadeStart");
            public static readonly int VignetteFadeEnd         = Shader.PropertyToID("VignetteFadeEnd");
            public static readonly int VignetteIntensity       = Shader.PropertyToID("VignetteIntensity");
            public static readonly int VignetteColor           = Shader.PropertyToID("VignetteColor");
        }
    }
}
