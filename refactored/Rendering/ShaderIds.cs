// SPDX-License-Identifier: LGPL-3.0-or-later
// ShaderIds — single property-id cache shared by the four §6.3 binder classes
// (VolumeMaterialBinder, VolumeTextureManager, VolumeCameraDriver,
// FoveatedSamplingPolicy) and the IMaskMode strategies. Centralising the
// Shader.PropertyToID calls keeps the binding contract with the ray-marching
// and mask shaders in one place, and means a renamed uniform is a single-file
// change regardless of which binder writes it.

using UnityEngine;

namespace iDaVIE.Rendering
{
    internal static class ShaderIds
    {
        // VolumeMaterialBinder uniforms.
        public static readonly int ThresholdMin            = Shader.PropertyToID("_ThresholdMin");
        public static readonly int ThresholdMax            = Shader.PropertyToID("_ThresholdMax");
        public static readonly int MaxSteps                = Shader.PropertyToID("_MaxSteps");
        public static readonly int ColorMapIndex           = Shader.PropertyToID("_ColorMapIndex");
        public static readonly int ScaleType               = Shader.PropertyToID("ScaleType");
        public static readonly int ScaleBias               = Shader.PropertyToID("ScaleBias");
        public static readonly int ScaleContrast           = Shader.PropertyToID("ScaleContrast");
        public static readonly int ScaleAlpha              = Shader.PropertyToID("ScaleAlpha");
        public static readonly int ScaleGamma              = Shader.PropertyToID("ScaleGamma");
        public static readonly int VignetteFadeStart       = Shader.PropertyToID("VignetteFadeStart");
        public static readonly int VignetteFadeEnd         = Shader.PropertyToID("VignetteFadeEnd");
        public static readonly int VignetteIntensity       = Shader.PropertyToID("VignetteIntensity");
        public static readonly int VignetteColor           = Shader.PropertyToID("VignetteColor");

        // FoveatedSamplingPolicy uniforms.
        public static readonly int FoveationStart          = Shader.PropertyToID("FoveationStart");
        public static readonly int FoveationEnd            = Shader.PropertyToID("FoveationEnd");
        public static readonly int FoveationJitter         = Shader.PropertyToID("FoveationJitter");
        public static readonly int FoveatedStepsLow        = Shader.PropertyToID("FoveatedStepsLow");
        public static readonly int FoveatedStepsHigh       = Shader.PropertyToID("FoveatedStepsHigh");

        // VolumeCameraDriver uniforms (transform-derived state).
        public static readonly int HighlightMin            = Shader.PropertyToID("HighlightMin");
        public static readonly int HighlightMax            = Shader.PropertyToID("HighlightMax");
        public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");
        public static readonly int ModelMatrix             = Shader.PropertyToID("ModelMatrix");
        public static readonly int MaskVoxelOffsets        = Shader.PropertyToID("MaskVoxelOffsets");

        // IMaskMode + VolumeTextureManager uniforms.
        public static readonly int MaskMode                = Shader.PropertyToID("MaskMode");
        public static readonly int MaskEntries             = Shader.PropertyToID("MaskEntries");
        public static readonly int MaskVoxelSize           = Shader.PropertyToID("MaskVoxelSize");
        public static readonly int MaskVoxelColor          = Shader.PropertyToID("MaskVoxelColor");
        public static readonly int HighlightedSource       = Shader.PropertyToID("HighlightedSource");

        // Projection-mode keyword (toggled, not written as a uniform).
        public const string AipKeyword = "SHADER_AIP";
    }
}
