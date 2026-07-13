// IMaskMode.cs  —  Finalised interface  (S2-E2-02)
// Refactoring proposal — Sub-team 3, Rendering Engine, Team Alpha
// Design doc reference: docs/design-document.md §5.4 DD-02
//
// ─────────────────────────────────────────────────────────────────────────────
// CONTEXT
// ─────────────────────────────────────────────────────────────────────────────
// Source file examined:
//   VolumeData/VolumeDataSetRenderer.cs  (lines 1072–1094)
//   VolumeData/VolumeDataSetRendererMaskMode.cs
//
// The original code selects mask behaviour with an if/else chain on a
// MaskMode enum value inside VolumeDataSetRenderer.Update().  Every branch
// writes directly to UnityEngine.Material property IDs.  Adding a new mode
// requires editing VolumeDataSetRenderer — an Open-Closed Principle violation
// (V-04 in the SOLID/GRASP audit, docs/design-document.md §7.1).
//
// ─────────────────────────────────────────────────────────────────────────────
// SOLUTION — Strategy Pattern  (design doc §5.4, DD-02)
// ─────────────────────────────────────────────────────────────────────────────
// Each mask behaviour is extracted into a sealed, stateless class that
// implements this interface.  VolumeMaterialBinder holds a reference to the
// active IMaskMode and calls Apply() once per frame — no branching, no
// knowledge of which concrete mode is held.
//
//   SOURCE enum value          AFTER class
//   ─────────────────────────────────────────
//   MaskMode.Disabled  (0)  →  DisabledMaskMode   (no-op in production)
//   MaskMode.Enabled   (1)  →  ApplyMaskMode
//   MaskMode.Inverted  (2)  →  InverseMaskMode
//   MaskMode.Isolated  (3)  →  IsolateMaskMode
//
// Future modes (e.g. IsoSurfaceMaskMode for FUT-01) are added by creating
// a new class.  Zero existing files change.  OCP fulfilled.
//
// ─────────────────────────────────────────────────────────────────────────────
// METHOD SIGNATURES — LOCKED  (design doc §5.4)
// ─────────────────────────────────────────────────────────────────────────────
//   void   Apply(Material material, Texture3D maskTexture)
//   string ShaderKeyword { get; }
//
// These two members are the complete public API.  Both are essential:
//   · Apply()        — per-frame operation; called by VolumeMaterialBinder
//   · ShaderKeyword  — identity property; allows tooling (shader-variant
//                      stripping, metrics scripts) to inspect the active mode
//                      without instantiating it or calling Apply() with a
//                      dummy Material.
//
// Rationale for NOT adding a third member here:
//   · ISP — fat interfaces raise CBO on every implementor; 2 members keeps
//     each concrete class at WMC=2, CBO=1, LCOM=0.
//   · Strategy vs State — mask modes are mutually exclusive per frame and do
//     not transition into each other; a plain two-method strategy is the
//     simplest correct fit.
//   · See design doc §5.4 "Pattern-choice rationale" for the full
//     Strategy vs Decorator vs State comparison.
//
// ─────────────────────────────────────────────────────────────────────────────
// CK METRICS — PROJECTED (Day 13)
// ─────────────────────────────────────────────────────────────────────────────
//   Metric  | Each concrete class | Target (domain class)
//   ─────────────────────────────────────────────────────
//   WMC     |  2                  | ≤ 20
//   CBO     |  1 (UnityEngine)    | ≤ 14
//   RFC     |  2                  | ≤ 50
//   LCOM    |  0.00               | ≤ 0.5
//   NOC     |  0                  | ≤ 5
//   DIT     |  1 (interface only) | ≤ 4
//
// Compare to the equivalent switch block in VolumeDataSetRenderer, which
// contributed to WMC=44, CBO=45, LCOM=0.81 for a 1400-line class.

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Strategy contract for volume mask rendering modes.
    ///
    /// <para>
    /// Each implementation encapsulates the shader-keyword writes and
    /// material-property changes required for exactly one mask behaviour.
    /// <see cref="VolumeMaterialBinder"/> holds a reference to the active
    /// implementation and calls <see cref="Apply"/> once per frame —
    /// no branching, no switch statement.
    /// </para>
    ///
    /// <para><b>SOLID / GRASP alignment (design doc §5.4):</b></para>
    /// <list type="bullet">
    ///   <item>SRP  — each implementation owns exactly one mode's render behaviour</item>
    ///   <item>OCP  — new mode = new class; no existing file changes</item>
    ///   <item>LSP  — all implementations are substitutable; callers only use Apply()</item>
    ///   <item>ISP  — two members, both essential; no fat interface</item>
    ///   <item>DIP  — VolumeMaterialBinder depends on this interface, not on concrete classes</item>
    ///   <item>Protected Variations (GRASP) — hides the mask-mode variation point</item>
    ///   <item>Indirection (GRASP) — decouples VolumeMaterialBinder from concrete classes</item>
    /// </list>
    /// </summary>
    public interface IMaskMode
    {
        // ─────────────────────────────────────────────────────────────────────
        // SIGNATURE 1 — per-frame operation
        // Called by VolumeMaterialBinder once per Update() (or whenever the
        // material needs rebinding after a pipeline event such as a resolution
        // change or HMD reconnect).
        //
        // The implementation MUST:
        //   1. Enable its own shader keyword on 'material'.
        //   2. Disable the keywords belonging to every other active mode so
        //      only one shader variant is live at a time.
        //   3. Set any mode-specific float / texture properties (e.g. _MaskAlpha,
        //      _MaskTex) so the HLSL ray-march loop receives correct uniforms.
        //
        // 'material' is the volume ray-march material (_materialInstance) owned
        // by VolumeMaterialBinder.  It is never null at the call site; callers
        // guarantee this in the precondition on VolumeMaterialBinder.SyncShaderState().
        //
        // 'maskTexture' may be null when no mask file has been loaded (the
        // Disabled mode is the only mode that is valid in this state).
        // Active-mode implementations (Apply / Inverse / Isolate) assert
        // maskTexture != null in DEBUG builds only; they silently no-op in
        // RELEASE to avoid a crash on an unexpected null.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Apply this mask mode's shader keywords and properties to
        /// <paramref name="material"/>.
        /// </summary>
        /// <param name="material">
        /// The volume ray-march material to mutate.  Must not be null.
        /// </param>
        /// <param name="maskTexture">
        /// The 3-D mask texture currently loaded in the renderer.
        /// May be <c>null</c> when no mask is active; only
        /// <see cref="DisabledMaskMode"/> is valid in that state.
        /// </param>
        void Apply(Material material, Texture3D maskTexture);

        // ─────────────────────────────────────────────────────────────────────
        // SIGNATURE 2 — identity property
        // Exposes the primary HLSL multi_compile keyword so that:
        //   · Shader variant stripping tools can enumerate which keywords are
        //     in use without constructing a real Material.
        //   · Unit tests can assert the correct keyword is enabled without
        //     a GPU or a Material asset (compare ShaderKeyword to the expected
        //     string constant).
        //   · Logging and diagnostics can identify the active mode cheaply.
        //
        // The returned string MUST match the keyword listed in the
        // #pragma multi_compile block of VolumeRender.shader.  Mismatches
        // produce silent no-ops (the shader ignores unknown keywords), which
        // are harder to debug than a compile error; keep the string constants
        // in a shared static class if needed to prevent divergence.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The HLSL shader keyword that uniquely identifies this mode
        /// (e.g. <c>"_MASK_APPLY"</c>, <c>"_MASK_INVERSE"</c>,
        /// <c>"_MASK_ISOLATE"</c>, <c>"_MASK_DISABLED"</c>).
        /// Must match the corresponding <c>#pragma multi_compile</c> entry
        /// in <c>VolumeRender.shader</c>.
        /// </summary>
        string ShaderKeyword { get; }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DisabledMaskMode — production no-op  (maps to MaskMode.Disabled = 0)
// ─────────────────────────────────────────────────────────────────────────────
// This is the starting state of VolumeMaterialBinder: no mask file is loaded,
// no mask keywords should be active on the material.
//
// In the source code the Disabled state is handled implicitly by the absence
// of a mask texture.  Making it an explicit class means:
//   · VolumeMaterialBinder is initialised with a valid IMaskMode reference
//     (never null), removing a null-check from the hot path.
//   · The Null Object pattern — callers do not need to branch on "is a mask
//     even loaded?"; they just call Apply() and DisabledMaskMode does nothing.
//
// CK: WMC=2, CBO=1, LCOM=0.

namespace iDaVIE.Rendering
{
    using UnityEngine;

    public sealed class DisabledMaskMode : IMaskMode
    {
        /// <inheritdoc/>
        public string ShaderKeyword => "_MASK_DISABLED";

        /// <summary>
        /// Disables all mask keywords on <paramref name="material"/>.
        /// This is a safe no-op when <paramref name="maskTexture"/> is null
        /// (i.e. no mask has been loaded).
        /// </summary>
        public void Apply(Material material, Texture3D maskTexture)
        {
            // Ensure all mode keywords are off; do not write _MaskTex when
            // maskTexture is null — SetTexture(null) triggers a Unity warning.
            material.DisableKeyword("_MASK_APPLY");
            material.DisableKeyword("_MASK_INVERSE");
            material.DisableKeyword("_MASK_ISOLATE");
            material.DisableKeyword("_MASK_DISABLED");
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// NullMaskMode — test double
// ─────────────────────────────────────────────────────────────────────────────
// A do-nothing implementation for use in edit-mode unit tests that exercise
// VolumeMaterialBinder without a real Material asset, GPU, or Unity player loop.
//
// Usage:
//   var binder = new VolumeMaterialBinder(new NullRenderPipeline(), new NullMaskMode());
//   binder.SyncShaderState(someState); // no Material calls fire; no GPU needed
//
// Placed in iDaVIE.Rendering.Tests — must NOT be included in production builds.
// Unity's asmdef 'testables' field ensures test assemblies are stripped on export.

namespace iDaVIE.Rendering.Tests
{
    using UnityEngine;

    public sealed class NullMaskMode : IMaskMode
    {
        /// <summary>Sentinel keyword; never matches a real shader keyword.</summary>
        public string ShaderKeyword => "_MASK_NULL";

        /// <summary>
        /// Intentional no-op — no Material or GPU context exists in a headless test.
        /// </summary>
        public void Apply(Material material, Texture3D maskTexture) { }
    }
}
