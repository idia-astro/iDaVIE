// SUPERSEDED STUB — canonical finalised version is at:
//   refactoring-examples/team3/example2-MaskModes/after/IMaskMode.cs
// This file can be removed with: git rm refactoring-examples/team3/stubs/IMaskMode.cs
//
// IMaskMode.cs
// Strategy interface — defines the contract every mask mode must fulfil.
//
// PURPOSE
// -------
// In the original code, VolumeDataSetRenderer.SetMaskMode() contains a
// switch statement that couples the class to every mask mode that exists.
// Adding a new mode (e.g. an iso-surface mask for FUT-01) means editing
// VolumeDataSetRenderer — an Open-Closed Principle violation.
//
// IMaskMode is the variation point that protects against that.
// VolumeMaterialBinder holds a reference to the *active* IMaskMode and calls
// Apply() each frame — it has zero knowledge of which concrete mode it holds.
// Adding a new mode = write a new class, nothing existing changes.
//
// IMPLEMENTATIONS (see refactoring-examples/example2-MaskModes/after/)
// ---------------------------------------------------------------------
//   ApplyMaskMode     — renders the mask region at full opacity
//   InverseMaskMode   — renders everything *outside* the mask at full opacity
//   IsolateMaskMode   — renders outside-mask voxels at reduced opacity (0.15)
//   IsoSurfaceMaskMode — future iso-contour mode (FUT-01); zero changes needed above
//
// TEST DOUBLE
// -----------
// NullMaskMode (below) is a do-nothing implementation for use in unit tests
// that exercise VolumeMaterialBinder without needing a real Material or GPU.

using UnityEngine;

namespace iDaVIE.Rendering
{
    /// <summary>
    /// Strategy contract for mask rendering modes.
    /// Each implementation encapsulates the shader-keyword and material-property
    /// changes required for one mask behaviour.
    /// </summary>
    public interface IMaskMode
    {
        // -------------------------------------------------------------------------
        // Core operation
        // Called by VolumeMaterialBinder once per frame (or whenever the material
        // needs to be re-bound after a pipeline event such as a resolution change).
        // The implementation must:
        //   1. Enable its own shader keyword on 'material'.
        //   2. Disable the keywords belonging to every other mode.
        //   3. Set any mode-specific float/texture properties.
        // -------------------------------------------------------------------------

        /// <summary>
        /// Apply this mask mode's shader keywords and properties to <paramref name="material"/>.
        /// </summary>
        /// <param name="material">The volume ray-march material to mutate.</param>
        /// <param name="maskTexture">The 3-D mask texture currently loaded in the renderer.</param>
        void Apply(Material material, Texture3D maskTexture);

        // -------------------------------------------------------------------------
        // Identity
        // Exposes the primary HLSL keyword so that tooling (metrics scripts,
        // shader variant stripping) can discover which keyword a mode owns
        // without having to instantiate it or call Apply() with a dummy Material.
        // -------------------------------------------------------------------------

        /// <summary>
        /// The HLSL shader keyword that uniquely identifies this mode
        /// (e.g. "_MASK_APPLY", "_MASK_INVERSE", "_MASK_ISOLATE").
        /// </summary>
        string ShaderKeyword { get; }
    }
}

// ---------------------------------------------------------------------------
// NullMaskMode — test double
// A do-nothing implementation. Inject this wherever VolumeMaterialBinder is
// constructed in edit-mode unit tests; no GPU, no Material asset required.
// ---------------------------------------------------------------------------

namespace iDaVIE.Rendering.Tests
{
    using UnityEngine;

    public sealed class NullMaskMode : IMaskMode
    {
        public string ShaderKeyword => "_MASK_NULL";

        // Intentional no-op — there is no real Material in a headless test context.
        public void Apply(Material material, Texture3D maskTexture) { }
    }
}
