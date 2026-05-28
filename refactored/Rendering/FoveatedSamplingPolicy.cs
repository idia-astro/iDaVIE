// SPDX-License-Identifier: LGPL-3.0-or-later
// FoveatedSamplingPolicy — one of the four §6.3 splits of VolumeDataSetRenderer.
// Owns the foveation step-budget decision: given an IRenderSettings snapshot,
// it picks the (low, high) ray-march step counts the shader will use this frame
// and writes them (plus jitter / fade range) onto the ray-marching material.
//
// Kept Unity-light (only depends on UnityEngine.Material) and free of state so
// the policy is unit-testable against an injected fake Material wrapper or by
// reading back UniformLog values. When foveation is disabled both step budgets
// pin to MaxRayMarchSteps so the shader needs no conditional of its own.

using UnityEngine;
using iDaVIE.Rendering.Contracts;       // IRenderSettings

namespace iDaVIE.Rendering
{
    public sealed class FoveatedSamplingPolicy
    {
        public void Apply(Material rayMaterial, IRenderSettings settings)
        {
            rayMaterial.SetFloat(ShaderIds.FoveationStart, settings.FoveationStart);
            rayMaterial.SetFloat(ShaderIds.FoveationEnd,   settings.FoveationEnd);

            if (settings.FoveatedRendering)
            {
                rayMaterial.SetFloat(ShaderIds.FoveationJitter,   settings.FoveationJitter);
                rayMaterial.SetInt  (ShaderIds.FoveatedStepsLow,  settings.FoveatedStepsLow);
                rayMaterial.SetInt  (ShaderIds.FoveatedStepsHigh, settings.FoveatedStepsHigh);
            }
            else
            {
                rayMaterial.SetInt(ShaderIds.FoveatedStepsLow,  settings.MaxRayMarchSteps);
                rayMaterial.SetInt(ShaderIds.FoveatedStepsHigh, settings.MaxRayMarchSteps);
            }
        }
    }
}
