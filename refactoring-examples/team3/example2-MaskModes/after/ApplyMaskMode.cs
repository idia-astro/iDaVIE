// ApplyMaskMode.cs  —  Finalised implementation  (S2-E2-03)
// Refactoring proposal — Sub-team 3, Rendering Engine, Team Alpha
// Design doc reference: docs/design-document.md §5.4 DD-02
//
// ─────────────────────────────────────────────────────────────────────────────
// CONTEXT
// ─────────────────────────────────────────────────────────────────────────────
// Source behaviour extracted from:
//   VolumeData/VolumeDataSetRendererMaskMode.cs  (if-branch MaskMode.Enabled = 1,
//   lines 1072–1094 of VolumeDataSetRenderer)
//
// In the original code the shader-keyword writes for this mode are one branch of
// an if/else chain inside VolumeDataSetRenderer.Update(). Adding a new mode means
// editing VolumeDataSetRenderer — an OCP violation (V-04, audit §8.1). That block
// contributed to WMC=74, CBO=45, and LCOM=0.81 for a 1,400-line class.
//
// This class is the extracted, self-contained form of that one branch.
//
// ─────────────────────────────────────────────────────────────────────────────
// RESPONSIBILITY
// ─────────────────────────────────────────────────────────────────────────────
// Apply() has exactly two jobs:
//   1. Set the correct shader keywords on the material (mutual exclusion).
//   2. Set _MaskAlpha to 1.0 (full opacity for inside-mask voxels).
//
// It does NOT bind the mask texture. The texture lifecycle is VolumeMaterialBinder's
// responsibility alone (design doc §5.2: "Only this class may call Material.SetTexture").
// VolumeMaterialBinder.BindMaskTexture() binds the 3D mask texture once on load;
// Apply() is then called each frame with maskTexture = null by design.
//
// ─────────────────────────────────────────────────────────────────────────────
// BEHAVIOUR
// ─────────────────────────────────────────────────────────────────────────────
// Enables _MASK_APPLY so the ray-march loop discards samples whose mask texel == 0.
// Voxels inside the mask render at full opacity; voxels outside are invisible.
// This is the default mode when a mask file is first loaded
// (MaskMode.Enabled = 1 in the source enum).
//
// ─────────────────────────────────────────────────────────────────────────────
// SHADER SIDE
// ─────────────────────────────────────────────────────────────────────────────
// The HLSL variant "_MASK_APPLY" must appear in the shader's
//   #pragma multi_compile _ _MASK_APPLY _MASK_INVERSE _MASK_ISOLATE _MASK_DISABLED
// block. When active, the ray-march loop discards samples whose mask texel == 0.
// Only one keyword from this group should be active at a time — Apply() enforces
// mutual exclusion by disabling the other three before enabling its own.
//
// ─────────────────────────────────────────────────────────────────────────────
// CALL SITE (VolumeMaterialBinder.Tick — design doc §5.4 / §5.8 Phase 1)
// ─────────────────────────────────────────────────────────────────────────────
//   _activeMaskMode.Apply(_material, null);
//   // maskTexture = null because BindMaskTexture() already bound the texture.
//
// The maskTexture parameter is part of the IMaskMode contract for completeness
// (future modes such as IsoSurfaceMaskMode may need to inspect the texture at
// bind time). Active keyword-only modes ignore it.
//
// ─────────────────────────────────────────────────────────────────────────────
// CK METRICS — PROJECTED (Day 13)  (design doc §5.4 table)
// ─────────────────────────────────────────────────────────────────────────────
//   Metric  | This class  | Target (domain)
//   ─────────────────────────────────────────
//   WMC     |  2          | ≤ 20   (Apply CC=1 + ShaderKeyword get = 1)
//   DIT     |  1          | ≤ 4    (implements IMaskMode; no base class)
//   NOC     |  0          | ≤ 5    (sealed — no subclasses by design)
//   CBO     |  1          | ≤ 14   (UnityEngine.Material only; Texture3D is a
//                                   parameter type, not a field dependency)
//   RFC     |  3          | ≤ 50   (ShaderKeyword; Apply; + Material keyword calls)
//   LCOM    |  0.00       | ≤ 0.5  (both members operate on the same material arg;
//                                   no instance fields → LCOM = 0 by definition)
//
// Compare to the equivalent if-branch in VolumeDataSetRenderer: that class
// measured WMC=74, CBO=31, LCOM=0.81 on Day 2. Each mask branch contributed
// directly to all three numbers. Three sealed, single-responsibility classes
// each at WMC=2 replace the contribution of three branches inside a God Class.
//
// ─────────────────────────────────────────────────────────────────────────────
// SOLID / GRASP ALIGNMENT  (design doc §8.2)
// ─────────────────────────────────────────────────────────────────────────────
//   SRP  — one reason to change: the "apply" mask render behaviour changes.
//           Camera logic, texture upload, and other mask modes are each in
//           their own class. Resolves V-01 (SRP) in part.
//   OCP  — VolumeMaterialBinder calls Apply() through IMaskMode; it has zero
//           knowledge that ApplyMaskMode exists. Adding IsoSurfaceMaskMode
//           (FUT-01) requires zero edits to this file. Resolves V-04 (OCP).
//   LSP  — fully substitutable for any IMaskMode reference; postconditions
//           of Apply() (exactly one keyword enabled, _MaskAlpha set) are met.
//   ISP  — the two-member interface imposes minimum coupling. CBO = 1.
//   DIP  — callers depend on IMaskMode, not on this class. Resolves V-14 / V-15.
//   Protected Variations (GRASP) — the mask-mode variation point is hidden
//           behind IMaskMode; VolumeMaterialBinder never branches on mode type.
//   Indirection (GRASP) — this class is the indirection layer between
//           VolumeMaterialBinder and the HLSL keyword "_MASK_APPLY".

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Mask rendering strategy: shows the masked region at full opacity and
    /// discards all voxels outside it.
    /// </summary>
    /// <remarks>
    /// Maps to <c>MaskMode.Enabled = 1</c> in the source enum. Activated by
    /// default when a mask file is loaded.
    /// <para>
    /// Texture binding is handled by <c>VolumeMaterialBinder.BindMaskTexture()</c>;
    /// this class only manages shader keywords and the <c>_MaskAlpha</c> uniform.
    /// </para>
    /// See <c>docs/design-document.md §5.4</c> for the full Strategy-pattern
    /// rationale and CK metric projections.
    /// </remarks>
    public sealed class ApplyMaskMode : IMaskMode
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Must match the <c>#pragma multi_compile</c> keyword in
        /// <c>VolumeRender.shader</c>. Referenced inside <see cref="Apply"/> so
        /// the enable call and this property can never diverge.
        /// </remarks>
        public string ShaderKeyword => "_MASK_APPLY";

        // ── Per-frame operation ───────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Enables <c>_MASK_APPLY</c> and disables the other three mode keywords
        /// so exactly one shader variant is live. Sets <c>_MaskAlpha</c> to 1.0
        /// (full opacity inside the mask region).
        /// <para>
        /// <paramref name="maskTexture"/> is unused — the mask texture is bound
        /// once by <c>VolumeMaterialBinder.BindMaskTexture()</c> and persists on
        /// the material between frames. The parameter exists to satisfy the
        /// <see cref="IMaskMode"/> contract, which future modes may use.
        /// </para>
        /// </remarks>
        public void Apply(Material material, Texture3D maskTexture)
        {
            // ── Keyword management ───────────────────────────────────────────
            // Disable every other mode keyword before enabling our own.
            // Unity keyword selection is undefined when multiple keywords from
            // the same multi_compile group are enabled simultaneously; explicit
            // disable here prevents the silent incorrect-variant bug that was
            // possible in the original switch statement.
            material.DisableKeyword("_MASK_DISABLED");
            material.DisableKeyword("_MASK_INVERSE");
            material.DisableKeyword("_MASK_ISOLATE");
            material.EnableKeyword(ShaderKeyword);  // "_MASK_APPLY"

            // ── Shader uniform ───────────────────────────────────────────────
            // _MaskAlpha = 1.0 — inside-mask voxels render at full opacity.
            // The HLSL variant discards outside-mask samples; _MaskAlpha has no
            // effect on discarded samples but must be set to a defined value.
            material.SetFloat("_MaskAlpha", 1.0f);

            // Texture is already on the material — see VolumeMaterialBinder.BindMaskTexture().
            // Do not call SetTexture here: maskTexture is null at this call site
            // and SetTexture(null) produces a Unity warning.
        }
    }
}
