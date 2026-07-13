/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// =============================================================================
// AFTER — Service: VolumeRenderingService (design-level worked example)
//
// Single responsibility: push rendering parameters to the GPU material pipeline.
// Owns the Material instances and the MaterialID property-ID cache.
// All coupling to UnityEngine.Material is contained here — zero other classes
// need to import Material.
//
// CK AFTER (estimated):
//   WMC  ≈ 22  (6 public + 3 private methods, avg complexity 2.4)
//   CBO  ≈ 8   (Material×2, VolumeDataSet, Texture3D, Shader, FilterMode,
//               ColorMapUtils, DownsampleConfig)
//   RFC  ≈ 24
//   LCOM ≈ 0.15  (all methods share _material or _maskMaterial)
// =============================================================================

using UnityEngine;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Manages the ray-marching and mask materials, pushing all GPU-visible
    /// rendering parameters as a unit.  Constructed by the MonoBehaviour
    /// orchestrator and injected as <see cref="IVolumeRenderer"/>.
    /// </summary>
    public sealed class VolumeRenderingService : IVolumeRenderer
    {
        private readonly Material _material;
        private readonly Material _maskMaterial;

        private VolumeDataSet _dataSet;
        private VolumeDataSet _maskDataSet;
        private long _maxCubeSizeMb;

        private int _currentXFactor, _currentYFactor, _currentZFactor;
        private int _baseXFactor, _baseYFactor, _baseZFactor;

        // ── Material property IDs — owned exclusively by this service ──────────
        private static class Ids
        {
            public static readonly int DataCube              = Shader.PropertyToID("_DataCube");
            public static readonly int MaskCube              = Shader.PropertyToID("MaskCube");
            public static readonly int MaskMode              = Shader.PropertyToID("MaskMode");
            public static readonly int NumColorMaps          = Shader.PropertyToID("_NumColorMaps");
            public static readonly int SliceMin              = Shader.PropertyToID("_SliceMin");
            public static readonly int SliceMax              = Shader.PropertyToID("_SliceMax");
            public static readonly int ThresholdMin          = Shader.PropertyToID("_ThresholdMin");
            public static readonly int ThresholdMax          = Shader.PropertyToID("_ThresholdMax");
            public static readonly int Jitter                = Shader.PropertyToID("_Jitter");
            public static readonly int MaxSteps              = Shader.PropertyToID("_MaxSteps");
            public static readonly int ColorMapIndex         = Shader.PropertyToID("_ColorMapIndex");
            public static readonly int ScaleMin              = Shader.PropertyToID("_ScaleMin");
            public static readonly int ScaleMax              = Shader.PropertyToID("_ScaleMax");
            public static readonly int ScaleType             = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleBias             = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast         = Shader.PropertyToID("ScaleContrast");
            public static readonly int ScaleAlpha            = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma            = Shader.PropertyToID("ScaleGamma");
            public static readonly int FoveationStart        = Shader.PropertyToID("FoveationStart");
            public static readonly int FoveationEnd          = Shader.PropertyToID("FoveationEnd");
            public static readonly int FoveationJitter       = Shader.PropertyToID("FoveationJitter");
            public static readonly int FoveatedStepsLow      = Shader.PropertyToID("FoveatedStepsLow");
            public static readonly int FoveatedStepsHigh     = Shader.PropertyToID("FoveatedStepsHigh");
            public static readonly int VignetteFadeStart     = Shader.PropertyToID("VignetteFadeStart");
            public static readonly int VignetteFadeEnd       = Shader.PropertyToID("VignetteFadeEnd");
            public static readonly int VignetteIntensity     = Shader.PropertyToID("VignetteIntensity");
            public static readonly int VignetteColor         = Shader.PropertyToID("VignetteColor");
            public static readonly int MaskVoxelSize         = Shader.PropertyToID("MaskVoxelSize");
            public static readonly int MaskVoxelColor        = Shader.PropertyToID("MaskVoxelColor");
            public static readonly int MaskVoxelOffsets      = Shader.PropertyToID("MaskVoxelOffsets");
            public static readonly int ModelMatrix           = Shader.PropertyToID("ModelMatrix");
            public static readonly int HighlightedSource     = Shader.PropertyToID("HighlightedSource");
            public static readonly int HighlightMin          = Shader.PropertyToID("HighlightMin");
            public static readonly int HighlightMax          = Shader.PropertyToID("HighlightMax");
            public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");
        }

        public VolumeRenderingService(Material rayMarchingMaterial, Material maskMaterial)
        {
            _material     = Object.Instantiate(rayMarchingMaterial);
            _maskMaterial = Object.Instantiate(maskMaterial);
            _material.SetInt(Ids.NumColorMaps, ColorMapUtils.NumColorMaps);
        }

        // ── IVolumeRenderer ────────────────────────────────────────────────────

        public void ApplyRenderingParameters(RenderingParameters p)
        {
            _material.SetFloat(Ids.ThresholdMin,  p.ThresholdMin);
            _material.SetFloat(Ids.ThresholdMax,  p.ThresholdMax);
            _material.SetFloat(Ids.Jitter,        p.Jitter);
            _material.SetFloat(Ids.MaxSteps,      p.MaxSteps);
            _material.SetFloat(Ids.ColorMapIndex, p.ColorMap.GetHashCode());
            _material.SetInt  (Ids.ScaleType,     p.ScalingType.GetHashCode());
            _material.SetFloat(Ids.ScaleBias,     p.ScalingBias);
            _material.SetFloat(Ids.ScaleContrast, p.ScalingContrast);
            _material.SetFloat(Ids.ScaleAlpha,    p.ScalingAlpha);
            _material.SetFloat(Ids.ScaleGamma,    p.ScalingGamma);

            if (p.ProjectionMode == ProjectionMode.AverageIntensityProjection)
                Shader.EnableKeyword("SHADER_AIP");
            else
                Shader.DisableKeyword("SHADER_AIP");
        }

        public void ApplyMaskParameters(MaskParameters p)
        {
            _material.SetInt     (Ids.MaskMode,      p.DisplayMask ? p.Mode.GetHashCode() : MaskMode.Disabled.GetHashCode());
            _maskMaterial.SetFloat(Ids.MaskVoxelSize, p.VoxelSize);
            _maskMaterial.SetColor(Ids.MaskVoxelColor, p.VoxelColor);
        }

        public void ApplyFoveationParameters(FoveationParameters p)
        {
            _material.SetFloat(Ids.FoveationStart,   p.Start);
            _material.SetFloat(Ids.FoveationEnd,     p.End);
            int stepsLow  = p.Enabled ? p.StepsLow  : (int)_material.GetFloat(Ids.MaxSteps);
            int stepsHigh = p.Enabled ? p.StepsHigh : (int)_material.GetFloat(Ids.MaxSteps);
            _material.SetFloat(Ids.FoveationJitter,  p.Enabled ? p.Jitter : 0f);
            _material.SetInt  (Ids.FoveatedStepsLow,  stepsLow);
            _material.SetInt  (Ids.FoveatedStepsHigh, stepsHigh);
        }

        public void ApplyVignetteParameters(VignetteParameters p)
        {
            _material.SetFloat(Ids.VignetteFadeStart, p.FadeStart);
            _material.SetFloat(Ids.VignetteFadeEnd,   p.FadeEnd);
            _material.SetFloat(Ids.VignetteIntensity, p.Intensity);
            _material.SetColor(Ids.VignetteColor,     p.Color);
        }

        public void RegenerateCubes()
        {
            GenerateDownsampledCube();
        }

        public void ShiftColorMap(int delta)
        {
            int numMaps      = ColorMapUtils.NumColorMaps;
            int currentIndex = (int)_material.GetFloat(Ids.ColorMapIndex);
            int newIndex     = (currentIndex + delta + numMaps) % numMaps;
            _material.SetFloat(Ids.ColorMapIndex, newIndex);
        }

        // ── Internal helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Computes downsample factors and (re)uploads the volume texture to the GPU.
        /// Called once at load time and again after crop or resolution change.
        /// </summary>
        private void GenerateDownsampledCube()
        {
            _dataSet.FindDownsampleFactors(_maxCubeSizeMb,
                out _currentXFactor, out _currentYFactor, out _currentZFactor);
            _dataSet.GenerateVolumeTexture(FilterMode.Point,
                _currentXFactor, _currentYFactor, _currentZFactor);
            _baseXFactor = _currentXFactor;
            _baseYFactor = _currentYFactor;
            _baseZFactor = _currentZFactor;
            _material.SetTexture(Ids.DataCube, _dataSet.DataCube);
        }

        /// <summary>Binds data and mask datasets after a load; called by the orchestrator.</summary>
        public void BindDataSets(VolumeDataSet dataSet, VolumeDataSet maskDataSet, long maxCubeSizeMb)
        {
            _dataSet       = dataSet;
            _maskDataSet   = maskDataSet;
            _maxCubeSizeMb = maxCubeSizeMb;
            _material.SetTexture(Ids.DataCube, dataSet.DataCube);
            if (maskDataSet != null)
                _material.SetTexture(Ids.MaskCube, maskDataSet.DataCube);
        }

        /// <summary>Exposes the material instance so the orchestrator can assign it to the MeshRenderer.</summary>
        public Material GetMaterial() => _material;

        /// <summary>Exposes the mask material for procedural draw calls in OnRenderObject.</summary>
        public Material GetMaskMaterial() => _maskMaterial;
    }
}
