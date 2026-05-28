// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeMaterialBinder — one of the four §6.3 splits of VolumeDataSetRenderer.
// Owns the per-frame push of the ray-marching shader uniforms that come straight
// from IRenderSettings: thresholds, scaling pipeline, colour map, projection
// mode, vignette. Foveation is handled by FoveatedSamplingPolicy; mask-mode
// state by an IMaskMode strategy; transform-derived state by VolumeCameraDriver.
//
// The class is intentionally stateless and Unity-light (only depends on
// UnityEngine.Material). A new scaling type or projection mode is an additive
// change (new enum value + shader branch); this binder does not switch on the
// enum, so it satisfies OCP against settings-driven changes.

using UnityEngine;
using iDaVIE.Kernel.Contracts.Types;     // FeatureColour
using iDaVIE.Rendering.Contracts;        // IRenderSettings, ProjectionMode

namespace iDaVIE.Rendering
{
    public sealed class VolumeMaterialBinder
    {
        public void Apply(Material rayMaterial, IRenderSettings settings)
        {
            rayMaterial.SetFloat(ShaderIds.ThresholdMin,  settings.ThresholdMin);
            rayMaterial.SetFloat(ShaderIds.ThresholdMax,  settings.ThresholdMax);
            rayMaterial.SetFloat(ShaderIds.MaxSteps,      settings.MaxRayMarchSteps);
            rayMaterial.SetFloat(ShaderIds.ColorMapIndex, (int)settings.ColorMap);

            rayMaterial.SetInt  (ShaderIds.ScaleType,     (int)settings.ScalingType);
            rayMaterial.SetFloat(ShaderIds.ScaleBias,     settings.ScalingBias);
            rayMaterial.SetFloat(ShaderIds.ScaleContrast, settings.ScalingContrast);
            rayMaterial.SetFloat(ShaderIds.ScaleAlpha,    settings.ScalingAlpha);
            rayMaterial.SetFloat(ShaderIds.ScaleGamma,    settings.ScalingGamma);

            // The shader has separate paths for MIP and AIP; the keyword is the
            // toggle the shader compiles against. Average-intensity needs the
            // accumulator initialisation that MIP omits, so swapping projections
            // is a shader-permutation switch rather than a uniform write.
            if (settings.ProjectionMode == ProjectionMode.AverageIntensityProjection)
                Shader.EnableKeyword(ShaderIds.AipKeyword);
            else
                Shader.DisableKeyword(ShaderIds.AipKeyword);

            rayMaterial.SetFloat(ShaderIds.VignetteFadeStart, settings.VignetteFadeStart);
            rayMaterial.SetFloat(ShaderIds.VignetteFadeEnd,   settings.VignetteFadeEnd);
            rayMaterial.SetFloat(ShaderIds.VignetteIntensity, settings.VignetteIntensity);
            rayMaterial.SetColor(ShaderIds.VignetteColor,     ToColor(settings.VignetteColor));
        }

        private static Color ToColor(FeatureColour c) => new(c.R, c.G, c.B, c.A);
    }
}
