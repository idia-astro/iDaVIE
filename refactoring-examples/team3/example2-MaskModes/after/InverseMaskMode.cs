// InverseMaskMode.cs  —  Finalised implementation  (S2-E2-03)
// Refactoring proposal — Sub-team 3, Rendering Engine, Team Alpha
// Design doc reference: docs/design-document.md §5.4 DD-02
//
// ─────────────────────────────────────────────────────────────────────────────
// CONTEXT
// ─────────────────────────────────────────────────────────────────────────────
// Source behaviour extracted from:
//   VolumeData/VolumeDataSetRendererMaskMode.cs  (if-branch MaskMode.Inverted = 2,
//   lines 1072–1094 of VolumeDataSetRenderer)
//
// In the original if/else chain all three mode branches are co-located in one
// method body and share the same _materialInstance reference. A change to the
// discard logic for one mode risks silently affecting the others because there is
// nothing to prevent a developer from reading or modifying a sibling branch.
// Extracting each branch into its own class enforces isolation at the language
// level: InverseMaskMode can only affect the material inside Apply().
//
// ─────────────────────────────────────────────────────────────────────────────
// RESPONSIBILITY
// ─────────────────────────────────────────────────────────────────────────────
// Apply() has exactly two jobs:
//   1. Set the correct shader keywords on the material (mutual exclusion).
//   2. Set _MaskAlpha to 1.0 (full opacity for outside-mask voxels).
//
// It does NOT bind the mask texture. The texture lifecycle is VolumeMaterialBinder's
// responsibility alone (design doc §5.2). VolumeMaterialBinder.BindMaskTexture()
// binds the texture once on load; Apply() is called each frame with
// maskTexture = null by design (see CALL SITE below).
//
// ─────────────────────────────────────────────────────────────────────────────
// BEHAVIOUR
// ─────────────────────────────────────────────────────────────────────────────
// Inverts the mask: voxels *outside* the mask render at full opacity; voxels
// *inside* are discarded by the ray-march loop. Useful when the region of
// interest is the volume surrounding a masked structure — e.g. studying the
// filamentary gas distribution around a dense molecular core.
// (MaskMode.Inverted = 2 in the source enum.)
//
// ─────────────────────────────────────────────────────────────────────────────
// SHADER SIDE
// ─────────────────────────────────────────────────────────────────────────────
// The HLSL variant "_MASK_INVERSE" flips the discard condition: samples are
// discarded when mask texel != 0 (inside the mask), rather than == 0 (outside)
// as in ApplyMaskMode. Only one keyword from the multi_compile group should be
// live at a time — Apply() enforces mutual exclusion by disabling the other
// three before enabling its own.
//
// ─────────────────────────────────────────────────────────────────────────────
// CALL SITE (VolumeMaterialBinder.Tick — design doc §5.4 / §5.8 Phase 1)
// ─────────────────────────────────────────────────────────────────────────────
//   _activeMaskMode.Apply(_material, null);
//   // maskTexture = null because BindMaskTexture() already bound the texture.
//
// The maskTexture parameter is part of the IMaskMode contract for completeness
// (future modes such as IsoSurfaceMaskMode may need it). Active keyword-only
// modes ignore it.
//
// ─────────────────────────────────────────────────────────────────────────────
// CK METRICS — PROJECTED (Day 13)  (design doc §5.4 table)
// ─────────────────────────────────────────────────────────────────────────────
//   Metric  | This class  | Target (domain)
//   ─────────────────────────────────────────
//   WMC     |  2          | ≤ 20   (Apply CC=1 + ShaderKeyword get = 1)
//   DIT     |  1          | ≤ 4    (implements IMaskMode; no base class)
//   NOC     |  0          | ≤ 5    (sealed — no subclasses by design)
//   CBO     |  1          | ≤ 14   (UnityEngine.Material only)
//   RFC     |  3          | ≤ 50   (ShaderKeyword; Apply; + Material keyword calls)
//   LCOM    |  0.00       | ≤ 0.5  (no instance fields; both members use material arg)
//
// Identical profile to ApplyMaskMode. Three classes at WMC=2, CBO=1, LCOM=0 is
// the expected result of a correct Strategy decomposition: each mode has one job,
// one coupling, and zero cohesion deficit.
//
// ─────────────────────────────────────────────────────────────────────────────
// SOLID / GRASP ALIGNMENT  (design doc §8.2)
// ─────────────────────────────────────────────────────────────────────────────
//   SRP  — sole reason to change: the inverted-mask render behaviour changes.
//           Resolves V-01 (SRP) in part.
//   OCP  — VolumeMaterialBinder is closed for modification; InverseMaskMode
//           is the open extension point for this behaviour. Resolves V-04.
//   LSP  — satisfies all IMaskMode postconditions; substitutable for any
//           IMaskMode reference.
//   ISP  — two-member interface; minimum CBO impact on this implementor.
//   DIP  — callers hold an IMaskMode reference; InverseMaskMode is invisible
//           to VolumeMaterialBinder. Resolves V-14 / V-15.
//   Protected Variations (GRASP) — mask-mode variation hidden behind IMaskMode.
//   Indirection (GRASP) — indirection layer between VolumeMaterialBinder and
//           the HLSL keyword "_MASK_INVERSE".

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Mask rendering strategy: shows everything <em>outside</em> the masked
    /// region at full opacity and discards voxels inside it.
    /// </summary>
    /// <remarks>
    /// Maps to <c>MaskMode.Inverted = 2</c> in the source enum. Useful for
    /// studying the surrounding volume context without the masked structure
    /// occluding the view.
    /// <para>
    /// Texture binding is handled by <c>VolumeMaterialBinder.BindMaskTexture()</c>;
    /// this class only manages shader keywords and the <c>_MaskAlpha</c> uniform.
    /// </para>
    /// See <c>docs/design-document.md §5.4</c> for the full Strategy-pattern
    /// rationale and CK metric projections.
    /// </remarks>
    public sealed class InverseMaskMode : IMaskMode
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Must match the <c>#pragma multi_compile</c> keyword in
        /// <c>VolumeRender.shader</c>. Referenced inside <see cref="Apply"/> so
        /// the enable call and this property can never diverge.
        /// </remarks>
        public string ShaderKeyword => "_MASK_INVERSE";

        // ── Per-frame operation ───────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Enables <c>_MASK_INVERSE</c> and disables the other three mode keywords
        /// so exactly one shader variant is live. Sets <c>_MaskAlpha</c> to 1.0
        /// (full opacity outside the mask region).
        /// <para>
        /// <paramref name="maskTexture"/> is unused — the mask texture is already
        /// bound to the material by <c>VolumeMaterialBinder.BindMaskTexture()</c>.
        /// </para>
        /// </remarks>
        public void Apply(Material material, Texture3D maskTexture)
        {
            // ── Keyword management ───────────────────────────────────────────
            // Disable every other mode keyword before enabling our own.
            // _MASK_INVERSE discards inside-mask samples; _MASK_APPLY discards
            // outside-mask samples. If both were live simultaneously Unity would
            // select one at shader-compile time and the other would silently have
            // no effect — a difficult runtime bug. Explicit disable prevents it.
            material.DisableKeyword("_MASK_DISABLED");
            material.DisableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_ISOLATE");
            material.EnableKeyword(ShaderKeyword);  // "_MASK_INVERSE"

            // ── Shader uniform ───────────────────────────────────────────────
            // _MaskAlpha = 1.0 — outside-mask voxels render at full opacity.
            // The HLSL variant discards inside-mask samples; _MaskAlpha plays no
            // role for discarded samples but must be set to a defined value.
            material.SetFloat("_MaskAlpha", 1.0f);

            // Texture is already on the material — see VolumeMaterialBinder.BindMaskTexture().
        }
    }
}
