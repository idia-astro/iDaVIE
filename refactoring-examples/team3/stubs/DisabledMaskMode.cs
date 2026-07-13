// DisabledMaskMode.cs
// Null Object implementation of IMaskMode — production class, placed in stubs/
// so it is co-located with the test doubles that use it.
//
// Used by: Example2_MaskModeTests.cs (DisabledMaskModeTests, StrategySubstitutabilityTests)
// Full impl also at: example2-MaskModes/after/ (same code, different folder context)
//
// PURPOSE
// -------
// In the original VolumeDataSetRenderer, when no mask file was loaded the code
// reached the else-branch of an if(_maskDataSet != null) guard inside Update()
// and wrote MaskMode.Disabled (integer 0) directly to the shader. This "disabled"
// state was implicit — there was no object representing it and no way to test the
// branch in isolation.
//
// DisabledMaskMode makes that state explicit and independently testable:
//   • VolumeMaterialBinder initialises _activeMaskMode = new DisabledMaskMode()
//     at startup so it never holds null.
//   • Apply() clears all mask keywords and does NOT call SetTexture — which would
//     produce a Unity warning if called with null. This is safe by design.
//   • The class is sealed and stateless, making it a canonical Null Object.
//
// SOLID / GRASP
//   Null Object (GRASP) — eliminates the null check at the call site.
//   SRP              — one reason to change: the "no mask" render state changes.
//   OCP              — callers never branch on "is mask disabled?" — they just
//                      call Apply() and the correct thing happens.
//
// CK METRICS — PROJECTED (Day 13)
//   WMC  2   (Apply CC=1, ShaderKeyword get=1)
//   CBO  1   (UnityEngine.Material only)
//   RFC  6   (ShaderKeyword + Apply + 4× DisableKeyword)
//   LCOM 0.0 (no instance fields)

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Null Object implementation of <see cref="IMaskMode"/>.
    /// Active when no mask file is loaded. Clears all mask shader keywords so
    /// the ray-march shader renders the data volume without any mask overlay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Apply"/> does <b>not</b> call <c>Material.SetTexture</c> — the
    /// only mode where this is safe, because no mask texture exists at this point.
    /// Calling <c>SetTexture(null)</c> would generate a Unity warning.
    /// </para>
    /// <para>
    /// <c>VolumeMaterialBinder</c> holds <c>_activeMaskMode = new DisabledMaskMode()</c>
    /// at startup, eliminating the null-check that previously lived inside
    /// <c>VolumeDataSetRenderer.Update()</c>.
    /// </para>
    /// </remarks>
    public sealed class DisabledMaskMode : IMaskMode
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public string ShaderKeyword => "_MASK_DISABLED";

        // ── Per-frame operation ───────────────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Disables all four mask keywords. Does not set <c>_MaskAlpha</c> or
        /// call <c>SetTexture</c> — there is no mask to bind. Calling this method
        /// with <paramref name="maskTexture"/> = <c>null</c> is explicitly safe
        /// and is the expected call pattern when no mask is loaded.
        /// </remarks>
        public void Apply(Material material, Texture3D maskTexture)
        {
            // Disable every mask keyword — the shader renders the raw data volume.
            // All four are disabled (not just our own) to guarantee a clean state
            // regardless of what was enabled before this call.
            material.DisableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_INVERSE");
            material.DisableKeyword("_MASK_ISOLATE");
            material.DisableKeyword("_MASK_DISABLED");

            // Intentionally no SetTexture call — maskTexture is null and
            // Material.SetTexture(null) produces a Unity editor warning.
            // The mask texture slot on the material simply retains whatever
            // value it had previously (irrelevant since all keywords are off).
        }
    }
}
