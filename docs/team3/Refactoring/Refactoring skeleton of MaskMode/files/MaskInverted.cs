/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * IMaskMode implementation: MaskInverted
 *
 * BEFORE (VolumeDataSetRenderer.cs):
 *   _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());  // == 2
 *   // same block as MaskEnabled (lines 1076–1089) — mode determined by enum value
 *   // no structural difference between Enabled and Inverted handling in C#;
 *   // the shader branches on the integer value
 *
 * AFTER (this class):
 *   _activeMaskMode = new MaskInverted();
 *   _activeMaskMode.Apply(volumeMat, maskMat, context);
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// MaskMode = 2 (MASK_INVERTED in BasicVolume.cginc)
    ///
    /// The inverse of MaskEnabled. Only voxels where the mask value is
    /// zero (background) are rendered. Labelled sources are hidden.
    /// This lets the astronomer inspect the data around their sources —
    /// useful for checking what the mask is excluding and verifying
    /// source boundaries are correctly drawn.
    ///
    /// The point cloud pass is still drawn so the astronomer can see
    /// where the hidden sources are while inspecting the background.
    ///
    /// CK metrics (projected):
    ///   WMC = 3 (3 members)   DIT = 1 (implements interface)
    ///   CBO = 3 (Material, MaskRenderContext, MaterialID)   LCOM = 0
    /// </summary>
    public sealed class MaskInverted : IMaskMode
    {
        // ── IMaskMode ────────────────────────────────────────────────

        /// <inheritdoc/>
        public int ShaderModeIndex => 2;

        /// <inheritdoc/>
        /// Point cloud is drawn so the astronomer can see where the
        /// inverted (hidden) source regions are while viewing background.
        public bool RequiresPointCloudPass => true;

        /// <inheritdoc/>
        /// Identical material configuration to MaskEnabled — the only
        /// difference is ShaderModeIndex = 2, which causes the shader to
        /// filter on maskValue == 0 instead of maskValue > 0.
        /// This structural similarity between Enabled and Inverted is
        /// exactly why the Strategy pattern is cleaner than enum branches:
        /// the difference is encapsulated in ShaderModeIndex, not in
        /// duplicated if/else blocks.
        public void Apply(Material volumeMat, Material maskMat, MaskRenderContext context)
        {
            // ── Volume shader uniform ─────────────────────────────────
            // Shader will render only voxels where maskValue == 0
            // (MASK_INVERTED branch in BasicVolume.cginc)
            volumeMat.SetInt(MaterialID.MaskMode, ShaderModeIndex);

            // ── Mask point cloud material ─────────────────────────────
            // Same configuration as MaskEnabled — point cloud shows
            // where the masked (now hidden) sources are
            maskMat.SetFloat(MaterialID.MaskVoxelSize, context.VoxelSize);
            maskMat.SetColor(MaterialID.MaskVoxelColor, context.VoxelColor);
            maskMat.SetInt(MaterialID.HighlightedSource, context.HighlightedSource);
            maskMat.SetVectorArray(MaterialID.MaskVoxelOffsets, context.VoxelOffsets);
            maskMat.SetMatrix(MaterialID.ModelMatrix, context.ModelMatrix);
        }
    }
}
