/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * IMaskMode implementation: MaskEnabled
 *
 * BEFORE (VolumeDataSetRenderer.cs line 1076):
 *   _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());  // == 1
 *   _maskMaterialInstance.SetFloat(MaterialID.MaskVoxelSize, MaskVoxelSize);
 *   _maskMaterialInstance.SetColor(MaterialID.MaskVoxelColor, MaskVoxelColor);
 *   _maskMaterialInstance.SetInt(MaterialID.HighlightedSource, HighlightedSource);
 *   _maskMaterialInstance.SetVectorArray(MaterialID.MaskVoxelOffsets, offsets);
 *   _maskMaterialInstance.SetMatrix(MaterialID.ModelMatrix, modelMatrix);
 *   // all scattered across Update() lines 1076–1089
 *
 * AFTER (this class):
 *   _activeMaskMode = new MaskEnabled();
 *   _activeMaskMode.Apply(volumeMat, maskMat, context);
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// MaskMode = 1 (MASK_ENABLED in BasicVolume.cginc)
    ///
    /// Only voxels where the mask value is greater than zero are rendered
    /// by the ray-marching shader. Background voxels (mask == 0) are
    /// fully transparent. The astronomer sees only the labelled sources.
    ///
    /// Additionally, the mask point cloud pass renders each mask voxel
    /// as a small coloured cube (via VolumeMask.shader geometry shader),
    /// giving the astronomer a clear 3D boundary view of their sources.
    ///
    /// CK metrics (projected):
    ///   WMC = 3 (3 members)   DIT = 1 (implements interface)
    ///   CBO = 3 (Material, MaskRenderContext, MaterialID)   LCOM = 0
    /// </summary>
    public sealed class MaskEnabled : IMaskMode
    {
        // ── IMaskMode ────────────────────────────────────────────────

        /// <inheritdoc/>
        public int ShaderModeIndex => 1;

        /// <inheritdoc/>
        /// MaskEnabled draws the point cloud so the astronomer can see
        /// each source's voxel boundary as coloured cubes in VR.
        public bool RequiresPointCloudPass => true;

        /// <inheritdoc/>
        /// Configures both materials:
        ///   volumeMat  — shader filters to masked voxels only
        ///   maskMat    — point cloud rendered with voxel size, colour, offsets
        public void Apply(Material volumeMat, Material maskMat, MaskRenderContext context)
        {
            // ── Volume shader uniform ─────────────────────────────────
            // Corresponds to: _materialInstance.SetInt(MaterialID.MaskMode, 1)
            // previously at VolumeDataSetRenderer.cs line 1076
            volumeMat.SetInt(MaterialID.MaskMode, ShaderModeIndex);

            // ── Mask point cloud material ─────────────────────────────
            // All of the following were previously scattered in Update()
            // lines 1077–1089 of VolumeDataSetRenderer.cs

            // Voxel cube size in world space (default 1.0)
            maskMat.SetFloat(MaterialID.MaskVoxelSize, context.VoxelSize);

            // RGBA tint for mask voxel cubes (default grey, semi-transparent)
            maskMat.SetColor(MaterialID.MaskVoxelColor, context.VoxelColor);

            // Source ID to highlight with a different colour in the mask
            maskMat.SetInt(MaterialID.HighlightedSource, context.HighlightedSource);

            // 4 corner offset vectors — used by the geometry shader to extrude
            // each point into a world-space box aligned to the voxel grid
            maskMat.SetVectorArray(MaterialID.MaskVoxelOffsets, context.VoxelOffsets);

            // Model matrix — transforms voxel indices to world space
            maskMat.SetMatrix(MaterialID.ModelMatrix, context.ModelMatrix);
        }
    }
}
