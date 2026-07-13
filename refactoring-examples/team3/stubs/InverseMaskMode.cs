// SUPERSEDED STUB — canonical finalised version is at:
//   refactoring-examples/team3/example2-MaskModes/after/InverseMaskMode.cs
// This file can be removed with: git rm refactoring-examples/team3/stubs/InverseMaskMode.cs
//
// InverseMaskMode.cs
// Concrete implementation of IMaskMode — Strategy 2 of 3.
//
// BEHAVIOUR
// ---------
// Inverts the mask: voxels *outside* the mask region are rendered at full
// opacity; voxels inside are hidden. Useful when the region of interest is
// everything surrounding a masked structure.
//
// SHADER SIDE
// -----------
// The HLSL variant "_MASK_INVERSE" flips the discard condition in the
// ray-march loop: samples are discarded when mask value != 0 instead of == 0.
//
// CK NOTE
// -------
// WMC = 2, CBO = 1, LCOM = 0 — identical profile to ApplyMaskMode.
// Each strategy class is independently testable with a single Assert on
// ShaderKeyword; no Unity player loop or GPU required.

using UnityEngine;

namespace iDaVIE.Rendering
{
    public sealed class InverseMaskMode : IMaskMode
    {
        public string ShaderKeyword => "_MASK_INVERSE";

        public void Apply(Material material, Texture3D maskTexture)
        {
            material.DisableKeyword("_MASK_APPLY");
            material.EnableKeyword("_MASK_INVERSE");
            material.DisableKeyword("_MASK_ISOLATE");

            // Full opacity outside the mask region.
            material.SetFloat("_MaskAlpha", 1.0f);
            material.SetTexture("_MaskTex", maskTexture);
        }
    }
}
