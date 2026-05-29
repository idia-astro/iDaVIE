// SPDX-License-Identifier: LGPL-3.0-or-later
// InteractionValueTypes — supplementary ST4 value types not declared in
// IInteractionContracts.cs (which holds the cross-team boundary types) or
// LocomotionFSM.cs (which holds LocomotionConfig). Internal to ST4 — these
// flow through ST4's own services and the per-team capture port.

namespace iDaVIE.Interaction
{
    /// <summary>Identity of one of the (up to two) VR controllers. The integer
    /// channel passed to IControllerEventStream events resolves to this.</summary>
    public readonly record struct ControllerIdentity(int Index, string Hand);

    /// <summary>Quick-menu open/close + active-panel state.</summary>
    public sealed class QuickMenuState
    {
        public bool           IsOpen      { get; set; }
        public QuickMenuPanel ActivePanel { get; set; }
    }

    /// <summary>Per-frame thumbstick scroll state — drives source-list / parameter
    /// scrolling for the active menu.</summary>
    public sealed class ScrollState
    {
        public float Delta { get; set; }
        public float Accumulated { get; set; }
    }
}
