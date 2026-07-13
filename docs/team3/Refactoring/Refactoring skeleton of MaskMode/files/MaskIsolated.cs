/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * IMaskMode implementation: MaskIsolated
 *
 * BEFORE (VolumeDataSetRenderer.cs):
 *   _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());  // == 3
 *   // same block as MaskEnabled (lines 1076–1089)
 *   // MaskIsolated is behaviourally different from Enabled/Inverted:
 *   //   - the volume shader ignores science data entirely in this mode
 *   //   - it only renders where mask > 0, as a flat colour (no transfer function)
 *   //   - the point cloud is the PRIMARY visual, not an overlay
 *   // But in the current code this difference is invisible in C# —
 *   // it's buried in the shader's MASK_ISOLATED branch.
 *
 * AFTER (this class):
 *   _activeMaskMode = new MaskIsolated();
 *   _activeMaskMode.Apply(volumeMat, maskMat, context);
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// MaskMode = 3 (MASK_ISOLATED in BasicVolume.cginc)
    ///
    /// The most visually distinct mode. The science data (brightness values)
    /// are not used at all. The shader instead renders masked regions as a
    /// flat uniform colour, making source boundaries immediately obvious.
    /// Background voxels (mask == 0) are fully transparent.
    ///
    /// The point cloud pass is the main visual in this mode — it draws
    /// every masked voxel as a coloured cube, giving the astronomer a
    /// pure 3D boundary view of all their labelled sources with no data
    /// underneath to obscure the boundaries.
    ///
    /// Use cases:
    ///   - Checking source completeness (are all expected sources masked?)
    ///   - Verifying mask edges look correct after painting
    ///   - Presenting source detections to collaborators in VR
    ///
    /// CK metrics (projected):
    ///   WMC = 3 (3 members)   DIT = 1 (implements interface)
    ///   CBO = 3 (Material, MaskRenderContext, MaterialID)   LCOM = 0
    /// </summary>
    public sealed class MaskIsolated : IMaskMode
    {
        // ── IMaskMode ────────────────────────────────────────────────

        /// <inheritdoc/>
        public int ShaderModeIndex => 3;

        /// <inheritdoc/>
        /// Always true for Isolated — the point cloud IS the primary rendering
        /// in this mode. Without it, the mode would show nothing meaningful.
        public bool RequiresPointCloudPass => true;

        /// <inheritdoc/>
        /// Same material configuration as MaskEnabled and MaskInverted.
        /// The visual difference from those modes is entirely in the shader
        /// (ShaderModeIndex = 3 → MASK_ISOLATED branch ignores science data).
        ///
        /// Note: in the current codebase the accumulateMaskIsolated() shader
        /// function (BasicVolume.cginc line 205) does NOT sample _DataCube —
        /// it only samples MaskCube and returns 1.0 if any mask is present.
        /// The transfer function is then skipped and a flat colour is output.
        /// This behaviour is determined by ShaderModeIndex, not by C# logic.
        public void Apply(Material volumeMat, Material maskMat, MaskRenderContext context)
        {
            // ── Volume shader uniform ─────────────────────────────────
            // Shader will render masked voxels as flat colour only
            // (MASK_ISOLATED branch in BasicVolume.cginc line 205)
            volumeMat.SetInt(MaterialID.MaskMode, ShaderModeIndex);

            // ── Mask point cloud material ─────────────────────────────
            // In Isolated mode the point cloud is the primary visual output.
            // Full configuration required.
            maskMat.SetFloat(MaterialID.MaskVoxelSize, context.VoxelSize);
            maskMat.SetColor(MaterialID.MaskVoxelColor, context.VoxelColor);
            maskMat.SetInt(MaterialID.HighlightedSource, context.HighlightedSource);
            maskMat.SetVectorArray(MaterialID.MaskVoxelOffsets, context.VoxelOffsets);
            maskMat.SetMatrix(MaterialID.ModelMatrix, context.ModelMatrix);
        }
    }
}
