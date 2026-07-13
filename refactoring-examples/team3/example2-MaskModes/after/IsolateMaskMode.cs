// IsolateMaskMode.cs  —  Finalised implementation  (S2-E2-03)
// Refactoring proposal — Sub-team 3, Rendering Engine, Team Alpha
// Design doc reference: docs/design-document.md §5.4 DD-02
//
// ─────────────────────────────────────────────────────────────────────────────
// CONTEXT
// ─────────────────────────────────────────────────────────────────────────────
// Source behaviour extracted from:
//   VolumeData/VolumeDataSetRendererMaskMode.cs  (if-branch MaskMode.Isolated = 3,
//   lines 1072–1094 of VolumeDataSetRenderer)
//
// In the original code the 0.15f constant is a bare literal in the if-branch body.
// It cannot be found by a search for any meaningful name and has no comment explaining
// why 0.15 was chosen. Extracting this branch into its own class means:
//   · The constant is named (OutsideAlpha) and documented in the one place
//     that owns this behaviour.
//   · It is no longer co-located with unrelated mask logic.
//   · If the value ever needs to be per-session configurable, a constructor
//     parameter is added to THIS class alone; nothing else changes (OCP).
//
// ─────────────────────────────────────────────────────────────────────────────
// RESPONSIBILITY
// ─────────────────────────────────────────────────────────────────────────────
// Apply() has exactly two jobs:
//   1. Set the correct shader keywords on the material (mutual exclusion).
//   2. Set _MaskAlpha to OutsideAlpha (0.15) so the HLSL variant attenuates
//      outside-mask samples rather than discarding them.
//
// It does NOT bind the mask texture. The texture lifecycle is VolumeMaterialBinder's
// responsibility alone (design doc §5.2). VolumeMaterialBinder.BindMaskTexture()
// binds the texture once on load; Apply() is called each frame with
// maskTexture = null by design (see CALL SITE below).
//
// ─────────────────────────────────────────────────────────────────────────────
// BEHAVIOUR
// ─────────────────────────────────────────────────────────────────────────────
// Isolates the mask region visually:
//   · Voxels inside the mask  → full opacity (rendered normally).
//   · Voxels outside the mask → 15 % opacity (faintly visible for context).
//
// The 15 % outside-mask opacity lets the user see where the masked structure
// sits within the full data cube without the surrounding volume dominating the
// view. This is distinct from ApplyMaskMode (outside = invisible) and
// InverseMaskMode (inside = invisible).
// (MaskMode.Isolated = 3 in the source enum.)
//
// ─────────────────────────────────────────────────────────────────────────────
// SHADER SIDE
// ─────────────────────────────────────────────────────────────────────────────
// The HLSL variant "_MASK_ISOLATE" does NOT discard outside-mask samples. Instead
// it multiplies their accumulated alpha by the _MaskAlpha uniform before the final
// composite step. Setting _MaskAlpha = 0.15 therefore reduces outside-mask voxels
// to 15 % of their natural brightness.
//
// This is the only active mode where _MaskAlpha carries a value other than 1.0.
// The named constant OutsideAlpha makes that explicit and prevents the value from
// silently drifting in a future refactor.
//
// ─────────────────────────────────────────────────────────────────────────────
// CALL SITE (VolumeMaterialBinder.Tick — design doc §5.4 / §5.8 Phase 1)
// ─────────────────────────────────────────────────────────────────────────────
//   _activeMaskMode.Apply(_material, null);
//   // maskTexture = null because BindMaskTexture() already bound the texture.
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
// OutsideAlpha is a compile-time constant (not an instance field), so CK tools do
// not count it toward LCOM or CBO. WMC remains 2. If OutsideAlpha were promoted
// to a constructor parameter, WMC would rise to 3 (constructor CC=1 added) and
// CBO would remain at 1 — both still well inside targets.
//
// ─────────────────────────────────────────────────────────────────────────────
// SOLID / GRASP ALIGNMENT  (design doc §8.2)
// ─────────────────────────────────────────────────────────────────────────────
//   SRP  — sole reason to change: the isolated-mode render behaviour changes.
//           The 0.15 constant is owned here and nowhere else. Resolves V-01 in part.
//   OCP  — OutsideAlpha can be made configurable by adding a constructor
//           parameter to THIS class; VolumeMaterialBinder needs no change.
//           Resolves V-04.
//   LSP  — satisfies all IMaskMode postconditions; substitutable for any
//           IMaskMode reference.
//   ISP  — two-member interface; minimum CBO impact on this implementor.
//   DIP  — callers hold an IMaskMode reference; IsolateMaskMode is invisible
//           to VolumeMaterialBinder. Resolves V-14 / V-15.
//   Protected Variations (GRASP) — the "what opacity do outside-mask voxels get?"
//           variation is hidden inside this class. VolumeMaterialBinder never
//           reads or compares the _MaskAlpha value.
//   Pure Fabrication (GRASP) — IsolateMaskMode does not correspond to a domain
//           entity; it is a fabricated class whose sole purpose is to keep
//           VolumeDataSetRenderer's WMC and LCOM in check.

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Mask rendering strategy: renders the masked region at full opacity and
    /// outside-mask voxels at reduced opacity for spatial context.
    /// </summary>
    /// <remarks>
    /// Maps to <c>MaskMode.Isolated = 3</c> in the source enum. The 15 % outside
    /// opacity lets astronomers see where the masked structure sits within the
    /// full data cube without losing it in the background noise.
    /// <para>
    /// Texture binding is handled by <c>VolumeMaterialBinder.BindMaskTexture()</c>;
    /// this class only manages shader keywords and the <c>_MaskAlpha</c> uniform.
    /// </para>
    /// See <c>docs/design-document.md §5.4</c> for the full Strategy-pattern
    /// rationale and CK metric projections.
    /// </remarks>
    public sealed class IsolateMaskMode : IMaskMode
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>
        /// Opacity applied to voxels outside the masked region (0–1 range).
        /// </summary>
        /// <remarks>
        /// 0.15 matches the hardcoded literal in the original if-branch. Naming it
        /// here documents the intent, localises future changes to a single line, and
        /// makes it trivially configurable via a constructor parameter if needed
        /// (OCP: change only this class, nothing else).
        /// </remarks>
        private const float OutsideAlpha = 0.15f;

        // ── Identity ─────────────────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Must match the <c>#pragma multi_compile</c> keyword in
        /// <c>VolumeRender.shader</c>. The HLSL variant reads <c>_MaskAlpha</c>
        /// as a continuous attenuation factor — the only active mode where
        /// <c>_MaskAlpha</c> is not 1.0.
        /// </remarks>
        public string ShaderKeyword => "_MASK_ISOLATE";

        // ── Per-frame operation ───────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Enables <c>_MASK_ISOLATE</c>, disables the other three mode keywords,
        /// and sets <c>_MaskAlpha</c> to <see cref="OutsideAlpha"/> (0.15).
        /// The HLSL variant attenuates outside-mask sample alpha by this value
        /// rather than discarding the samples entirely.
        /// <para>
        /// <paramref name="maskTexture"/> is unused — the mask texture is already
        /// bound to the material by <c>VolumeMaterialBinder.BindMaskTexture()</c>.
        /// </para>
        /// </remarks>
        public void Apply(Material material, Texture3D maskTexture)
        {
            // ── Keyword management ───────────────────────────────────────────
            // Disable every other mode keyword before enabling our own.
            // _MASK_ISOLATE attenuates outside-mask alpha; if _MASK_APPLY or
            // _MASK_INVERSE were also active, Unity would silently prefer one
            // variant and the isolate attenuation would have no effect.
            material.DisableKeyword("_MASK_DISABLED");
            material.DisableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_INVERSE");
            material.EnableKeyword(ShaderKeyword);  // "_MASK_ISOLATE"

            // ── Shader uniform ───────────────────────────────────────────────
            // _MaskAlpha = 0.15 — the HLSL variant multiplies outside-mask sample
            // alpha by this value. Inside-mask samples are unaffected (full opacity).
            //
            // This is the only active mode where _MaskAlpha is not 1.0; the named
            // constant makes that intent explicit and prevents copy-paste drift.
            material.SetFloat("_MaskAlpha", OutsideAlpha);

            // Texture is already on the material — see VolumeMaterialBinder.BindMaskTexture().
        }
    }
}
