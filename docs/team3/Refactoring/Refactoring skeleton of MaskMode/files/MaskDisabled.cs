/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * IMaskMode implementation: MaskDisabled
 *
 * BEFORE (VolumeDataSetRenderer.cs line 1094):
 *   _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.Disabled.GetHashCode());
 *   // scattered in Update() when _maskDataSet == null
 *
 * AFTER (this class):
 *   _activeMaskMode = new MaskDisabled();
 *   _activeMaskMode.Apply(volumeMat, maskMat, context);
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// MaskMode = 0 (MASK_DISABLED in BasicVolume.cginc)
    ///
    /// The mask texture is completely ignored by the ray-marching shader.
    /// Every voxel in the data cube is rendered regardless of its mask value.
    /// No point cloud pass is drawn.
    ///
    /// This is the safe default — used when no mask file is loaded,
    /// or when the user explicitly disables mask rendering.
    ///
    /// CK metrics (projected):
    ///   WMC = 3 (3 members)   DIT = 1 (implements interface)
    ///   CBO = 2 (Material, MaskRenderContext)   LCOM = 0
    /// </summary>
    public sealed class MaskDisabled : IMaskMode
    {
        // ── IMaskMode ────────────────────────────────────────────────

        /// <inheritdoc/>
        public int ShaderModeIndex => 0;

        /// <inheritdoc/>
        /// MaskDisabled never draws a point cloud — there is nothing to show.
        public bool RequiresPointCloudPass => false;

        /// <inheritdoc/>
        /// Sets MaskMode uniform to 0. No mask material configuration needed.
        /// The shader will skip all mask texture lookups entirely.
        public void Apply(Material volumeMat, Material maskMat, MaskRenderContext context)
        {
            // Push shader uniform only — no mask material state required
            // Corresponds to: _materialInstance.SetInt(MaterialID.MaskMode, 0)
            // previously at VolumeDataSetRenderer.cs line 1094
            volumeMat.SetInt(MaterialID.MaskMode, ShaderModeIndex);
        }
    }
}
