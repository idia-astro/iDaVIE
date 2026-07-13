// SUPERSEDED STUB — canonical finalised version is at:
//   refactoring-examples/team3/example2-MaskModes/after/IsolateMaskMode.cs
// This file can be removed with: git rm refactoring-examples/team3/stubs/IsolateMaskMode.cs
//
// IsolateMaskMode.cs
// Concrete implementation of IMaskMode — Strategy 3 of 3.
//
// BEHAVIOUR
// ---------
// Isolates the mask region by rendering voxels outside it at a heavily reduced
// opacity (0.15 — roughly 15% visible). The masked region stays at full
// opacity, so the user can see its position in context without the surrounding
// data dominating the view.
//
// SHADER SIDE
// -----------
// The HLSL variant "_MASK_ISOLATE" modulates the alpha of outside-mask samples
// by the _MaskAlpha uniform rather than discarding them entirely.
//
// CK NOTE
// -------
// WMC = 2, CBO = 1, LCOM = 0.
// The 0.15 constant is the only behavioural difference from the other two modes —
// encapsulated here rather than scattered across a switch statement.
// If the value ever needs to be configurable, a constructor parameter can be
// added to this class alone; nothing else changes.

using UnityEngine;

namespace iDaVIE.Rendering
{
    public sealed class IsolateMaskMode : IMaskMode
    {
        // Partial opacity for voxels outside the masked region.
        // 0.15 matches the value hardcoded in the original switch statement.
        private const float OutsideAlpha = 0.15f;

        public string ShaderKeyword => "_MASK_ISOLATE";

        public void Apply(Material material, Texture3D maskTexture)
        {
            material.DisableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_INVERSE");
            material.EnableKeyword("_MASK_ISOLATE");

            // Reduced opacity outside the mask — context visible but not dominant.
            material.SetFloat("_MaskAlpha", OutsideAlpha);
            material.SetTexture("_MaskTex", maskTexture);
        }
    }
}
