// SPDX-License-Identifier: LGPL-3.0-or-later
// IMaskMode — Strategy interface mandated by brief §6.3 for the mask-display
// modes (Apply / Inverse / Isolate, plus the Disabled no-op). The cross-team
// MaskMode enum (shared_interfaces.md §3.1, resolution line 9) is the public
// dispatch key; each enum member maps to one IMaskMode that knows how to
// bind its mode-specific shader state and whether the overlay compute buffers
// should be dispatched in OnRenderObject.
//
// Adding a fifth mode is a new MaskMode enum value + a new IMaskMode class +
// a new entry in MaskModeRegistry — no edits to VolumeDataSetRenderer's bind
// or OnRenderObject paths (OCP).

using UnityEngine;
using iDaVIE.Rendering.Contracts;        // MaskMode

namespace iDaVIE.Rendering
{
    public interface IMaskMode
    {
        MaskMode Mode { get; }

        /// <summary>Bind mode-specific uniforms on the ray-marching material.</summary>
        void BindToRayMaterial(Material rayMaterial);

        /// <summary>True when the renderer should dispatch the mask overlay compute
        /// buffers this frame. False when no overlay geometry is drawn (Disabled).</summary>
        bool RequiresOverlayDispatch { get; }
    }
}
