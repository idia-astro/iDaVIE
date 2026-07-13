/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Refactoring proposal — Sub-team 3, Rendering Engine, Team Alpha
 * Sprint 2 worked example: MaskMode enum → IMaskMode Strategy pattern
 *
 * BEFORE (current state):
 *   public enum MaskMode { Disabled = 0, Enabled = 1, Inverted = 2, Isolated = 3 }
 *   Logic scattered across VolumeDataSetRenderer.Update() lines 1072–1094
 *
 * AFTER (this file):
 *   Each mode is a self-contained class. Adding a new mode = new class only.
 *   No changes to VolumeMaterialBinder, no changes to the shader.
 *   OCP compliant: open for extension, closed for modification.
 */

using UnityEngine;
using VolumeData;

namespace VolumeData.Rendering
{
    /// <summary>
    /// Defines the behaviour of a single mask rendering mode.
    ///
    /// SOLID compliance:
    ///   SRP  — each implementation owns exactly one mode's rendering behaviour
    ///   OCP  — new modes added by implementing this interface, not editing existing code
    ///   LSP  — all implementations are substitutable; VolumeMaterialBinder only calls Apply()
    ///   ISP  — 3 members, all essential; no fat interface (brief target: ≤7 public members)
    ///   DIP  — VolumeMaterialBinder depends on this interface, not on concrete mode classes
    ///
    /// GRASP compliance:
    ///   Protected Variations — hides the variation point (mask rendering mode) behind a stable interface
    ///   Indirection          — decouples VolumeMaterialBinder from concrete mode classes
    /// </summary>
    public interface IMaskMode
    {
        /// <summary>
        /// The integer pushed to the MaskMode shader uniform.
        /// Maps to the #define constants in BasicVolume.cginc:
        ///   MASK_DISABLED = 0
        ///   MASK_ENABLED  = 1
        ///   MASK_INVERTED = 2
        ///   MASK_ISOLATED = 3
        /// </summary>
        int ShaderModeIndex { get; }

        /// <summary>
        /// Whether this mode requires the mask point cloud to be drawn
        /// via the VolumeMask.shader / DrawProcedural pass.
        /// Only Isolated mode uses the point cloud; all others render
        /// mask influence through the volume shader directly.
        /// </summary>
        bool RequiresPointCloudPass { get; }

        /// <summary>
        /// Apply this mode's shader state to the provided materials.
        /// Called by VolumeMaterialBinder.SyncShaderState() each frame.
        ///
        /// Implementations must set MaterialID.MaskMode on volumeMat.
        /// Implementations may additionally configure maskMat for the
        /// point cloud pass (voxel size, colour, offsets, model matrix).
        /// </summary>
        /// <param name="volumeMat">The instanced ray-marching material (_materialInstance)</param>
        /// <param name="maskMat">The instanced mask point-cloud material (_maskMaterialInstance)</param>
        /// <param name="context">Read-only mask rendering context (voxel size, colour, matrix, source ID)</param>
        void Apply(Material volumeMat, Material maskMat, MaskRenderContext context);
    }

    /// <summary>
    /// Read-only value object carrying the per-frame mask rendering parameters.
    /// Replaces the direct field reads from VolumeDataSetRenderer in Update().
    /// Pure data — no Unity lifecycle, fully constructable in tests.
    /// </summary>
    public readonly struct MaskRenderContext
    {
        /// <summary>VolumeDataSetRenderer.MaskVoxelSize — size of each painted voxel cube in world space</summary>
        public readonly float VoxelSize;

        /// <summary>VolumeDataSetRenderer.MaskVoxelColor — RGBA tint of mask point cloud voxels</summary>
        public readonly Color VoxelColor;

        /// <summary>VolumeDataSetRenderer.HighlightedSource — source ID to highlight in the mask shader</summary>
        public readonly short HighlightedSource;

        /// <summary>Pre-calculated 4-corner voxel offset vectors (in world space) for the geometry shader</summary>
        public readonly Vector4[] VoxelOffsets;

        /// <summary>transform.localToWorldMatrix of the volume game object</summary>
        public readonly Matrix4x4 ModelMatrix;

        public MaskRenderContext(
            float voxelSize,
            Color voxelColor,
            short highlightedSource,
            Vector4[] voxelOffsets,
            Matrix4x4 modelMatrix)
        {
            VoxelSize        = voxelSize;
            VoxelColor       = voxelColor;
            HighlightedSource = highlightedSource;
            VoxelOffsets     = voxelOffsets;
            ModelMatrix      = modelMatrix;
        }
    }
}
