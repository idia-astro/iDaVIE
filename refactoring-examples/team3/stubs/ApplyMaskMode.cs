// SUPERSEDED STUB — canonical finalised version is at:
//   refactoring-examples/team3/example2-MaskModes/after/ApplyMaskMode.cs
// This file can be removed with: git rm refactoring-examples/team3/stubs/ApplyMaskMode.cs
//
// ApplyMaskMode.cs
// Concrete implementation of IMaskMode — Strategy 1 of 3.
//
// BEHAVIOUR
// ---------
// Renders the mask region at full opacity; voxels outside the mask are hidden.
// This is the default mode when a mask file is first loaded.
//
// SHADER SIDE
// -----------
// The HLSL variant "_MASK_APPLY" must be listed under the shader's
// #pragma multi_compile block. When this keyword is enabled, the ray-march
// loop discards samples whose mask value == 0.
//
// CK NOTE
// -------
// WMC = 2 (Apply + ShaderKeyword).
// CBO = 1 (depends on UnityEngine.Material only).
// LCOM = 0 — both members operate on the same injected material; fully cohesive.
// Compare to the equivalent switch-case in VolumeDataSetRenderer:
// that block contributed to WMC, CBO, and LCOM of a 1400-line class.

using UnityEngine;

namespace iDaVIE.Rendering
{
    public sealed class ApplyMaskMode : IMaskMode
    {
        public string ShaderKeyword => "_MASK_APPLY";

        public void Apply(Material material, Texture3D maskTexture)
        {
            // Enable this mode's keyword; disable the other two so only one
            // variant is active at a time (Unity shader variants are exclusive here).
            material.EnableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_INVERSE");
            material.DisableKeyword("_MASK_ISOLATE");

            // Full opacity — voxels inside the mask are rendered normally.
            material.SetFloat("_MaskAlpha", 1.0f);
            material.SetTexture("_MaskTex", maskTexture);
        }
    }
}
