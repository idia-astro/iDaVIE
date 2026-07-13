/*
 * iDaVIE — Refactoring Proposal
 * Sub-team 3, Rendering Engine, Team Alpha
 *
 * VolumeMaterialBinder — partial stub showing IMaskMode integration.
 *
 * This file shows ONLY the mask-related portion of VolumeMaterialBinder
 * to demonstrate how IMaskMode is consumed in the refactored architecture.
 * It is NOT the full VolumeMaterialBinder implementation.
 *
 * BEFORE — all of this was in VolumeDataSetRenderer.Update() lines 1072–1094:
 *
 *   if (_maskDataSet != null)
 *   {
 *       _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());
 *       _maskMaterialInstance.SetFloat(MaterialID.MaskVoxelSize, MaskVoxelSize);
 *       _maskMaterialInstance.SetColor(MaterialID.MaskVoxelColor, MaskVoxelColor);
 *       _maskMaterialInstance.SetInt(MaterialID.HighlightedSource, HighlightedSource);
 *       // ... 4 more lines for offsets and matrix
 *   }
 *   else
 *   {
 *       _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.Disabled.GetHashCode());
 *   }
 *
 * AFTER — VolumeMaterialBinder.SetMaskMode() + SyncMaskState() below:
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// Partial stub: VolumeMaterialBinder — mask mode section only.
    /// Demonstrates IMaskMode Strategy consumption.
    /// Full class also owns: colour map, transfer function, foveation,
    /// vignette, projection keyword, and texture binding.
    /// </summary>
    public partial class VolumeMaterialBinder : MonoBehaviour
    {
        // ── Injected dependencies ─────────────────────────────────────

        [SerializeField] private Material _rayMarchingMaterial;
        [SerializeField] private Material _maskMaterial;

        private Material _materialInstance;
        private Material _maskMaterialInstance;

        // ── Mask mode state ───────────────────────────────────────────

        /// <summary>
        /// The currently active mask mode.
        /// Defaults to MaskDisabled — safe state when no mask is loaded.
        /// Set via SetMaskMode() — never assign directly.
        /// </summary>
        private IMaskMode _activeMaskMode = new MaskDisabled();

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Switch to a new mask mode.
        /// Called by PaintMenuController, QuickMenuController, VolumeCommandController.
        ///
        /// OCP in action: caller passes any IMaskMode implementation.
        /// VolumeMaterialBinder does not know or care which concrete class it is.
        ///
        /// Example usage:
        ///   _materialBinder.SetMaskMode(new MaskEnabled());
        ///   _materialBinder.SetMaskMode(new MaskIsolated());
        ///
        /// Adding a future MaskHighlight mode requires zero changes here.
        /// </summary>
        public void SetMaskMode(IMaskMode mode)
        {
            // Null guard — fall back to disabled rather than throw
            _activeMaskMode = mode ?? new MaskDisabled();
        }

        /// <summary>
        /// Returns whether the current mode requires the point cloud draw call.
        /// Called by the render pass / OnRenderObject replacement to decide
        /// whether to submit Graphics.DrawProcedural for the mask buffers.
        /// </summary>
        public bool RequiresMaskPointCloud => _activeMaskMode.RequiresPointCloudPass;

        // ── Frame sync ────────────────────────────────────────────────

        /// <summary>
        /// Push current mask mode state to the GPU materials.
        /// Called once per frame from SyncShaderState().
        ///
        /// Replaces the 12-line if/else block in VolumeDataSetRenderer.Update()
        /// lines 1072–1094 with a single method call.
        /// </summary>
        /// <param name="maskDataSet">
        /// Null if no mask is loaded — forces MaskDisabled regardless of _activeMaskMode.
        /// This preserves the existing safety behaviour from line 1094.
        /// </param>
        /// <param name="transform">
        /// Volume game object transform — used to build the MaskRenderContext.
        /// Only the Transform is needed here; no other MonoBehaviour fields.
        /// </param>
        public void SyncMaskState(IVolumeDataSet maskDataSet, Transform transform)
        {
            if (maskDataSet == null)
            {
                // No mask loaded — always force disabled regardless of requested mode
                // Preserves existing behaviour from VolumeDataSetRenderer.cs line 1094
                new MaskDisabled().Apply(_materialInstance, _maskMaterialInstance,
                    default);
                return;
            }

            // Build the context from current field values
            // These replace the direct field reads scattered across Update()
            var context = BuildMaskRenderContext(maskDataSet, transform);

            // Delegate to the active mode — no switch, no if/else
            _activeMaskMode.Apply(_materialInstance, _maskMaterialInstance, context);
        }

        // ── Private helpers ───────────────────────────────────────────

        /// <summary>
        /// Constructs the MaskRenderContext from current state.
        /// Extracted from the inline calculation at VolumeDataSetRenderer.cs lines 1083–1087.
        /// Pure function — testable without a running scene.
        /// </summary>
        private MaskRenderContext BuildMaskRenderContext(IVolumeDataSet maskDataSet, Transform volumeTransform)
        {
            var modelMatrix = volumeTransform.localToWorldMatrix;

            // Voxel corner offset vectors (geometry shader input)
            // Extracted from VolumeDataSetRenderer.cs lines 1083–1087
            var offsets = new Vector4[4];
            offsets[0] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize *
                new Vector3(-1.0f / maskDataSet.XDim, -1.0f / maskDataSet.YDim, -1.0f / maskDataSet.ZDim));
            offsets[1] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize *
                new Vector3(-1.0f / maskDataSet.XDim, -1.0f / maskDataSet.YDim, +1.0f / maskDataSet.ZDim));
            offsets[2] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize *
                new Vector3(-1.0f / maskDataSet.XDim, +1.0f / maskDataSet.YDim, -1.0f / maskDataSet.ZDim));
            offsets[3] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize *
                new Vector3(-1.0f / maskDataSet.XDim, +1.0f / maskDataSet.YDim, +1.0f / maskDataSet.ZDim));

            return new MaskRenderContext(
                voxelSize:        MaskVoxelSize,
                voxelColor:       MaskVoxelColor,
                highlightedSource: HighlightedSource,
                voxelOffsets:     offsets,
                modelMatrix:      modelMatrix
            );
        }

        // ── Fields referenced above (owned by full VolumeMaterialBinder) ──
        // These would be injected or set via public properties in the real class.
        // Shown here for completeness only.

        public float MaskVoxelSize = 1.0f;
        public Color MaskVoxelColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public short HighlightedSource = 0;
    }
}
