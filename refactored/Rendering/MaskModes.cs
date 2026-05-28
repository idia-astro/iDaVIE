// SPDX-License-Identifier: LGPL-3.0-or-later
// Concrete IMaskMode strategies + lookup table. Each strategy sets the
// MaskMode uniform on the ray material and declares whether the mask overlay
// compute buffers should be dispatched this frame.
//
// The integer values written to the shader follow the legacy mapping the
// RayMarchedVolume.shader already compiles against; the cross-team MaskMode
// enum (shared_interfaces.md §3.1, resolution line 9) is the source of truth
// for those values.

using System.Collections.Generic;
using UnityEngine;
using iDaVIE.Rendering.Contracts;        // MaskMode

namespace iDaVIE.Rendering
{
    internal sealed class DisabledMaskMode : IMaskMode
    {
        public MaskMode Mode => MaskMode.Disabled;
        public bool RequiresOverlayDispatch => false;

        public void BindToRayMaterial(Material rayMaterial)
            => rayMaterial.SetInt(ShaderIds.MaskMode, (int)MaskMode.Disabled);
    }

    internal sealed class EnabledMaskMode : IMaskMode
    {
        public MaskMode Mode => MaskMode.Enabled;
        public bool RequiresOverlayDispatch => true;

        public void BindToRayMaterial(Material rayMaterial)
            => rayMaterial.SetInt(ShaderIds.MaskMode, (int)MaskMode.Enabled);
    }

    internal sealed class InvertedMaskMode : IMaskMode
    {
        public MaskMode Mode => MaskMode.Inverted;
        public bool RequiresOverlayDispatch => true;

        public void BindToRayMaterial(Material rayMaterial)
            => rayMaterial.SetInt(ShaderIds.MaskMode, (int)MaskMode.Inverted);
    }

    internal sealed class IsolatedMaskMode : IMaskMode
    {
        public MaskMode Mode => MaskMode.Isolated;
        public bool RequiresOverlayDispatch => true;

        public void BindToRayMaterial(Material rayMaterial)
            => rayMaterial.SetInt(ShaderIds.MaskMode, (int)MaskMode.Isolated);
    }

    /// <summary>
    /// Single dispatch surface mapping MaskMode → IMaskMode. The dictionary is
    /// the only place that knows the full set of mask modes; adding a fifth is
    /// an additive entry here plus a new strategy class.
    /// </summary>
    public sealed class MaskModeRegistry
    {
        private readonly Dictionary<MaskMode, IMaskMode> _byEnum;

        public MaskModeRegistry()
        {
            _byEnum = new Dictionary<MaskMode, IMaskMode>
            {
                [MaskMode.Disabled] = new DisabledMaskMode(),
                [MaskMode.Enabled]  = new EnabledMaskMode(),
                [MaskMode.Inverted] = new InvertedMaskMode(),
                [MaskMode.Isolated] = new IsolatedMaskMode(),
            };
        }

        public IMaskMode ForEnum(MaskMode mode) => _byEnum[mode];
    }
}
