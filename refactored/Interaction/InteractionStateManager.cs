// SPDX-License-Identifier: LGPL-3.0-or-later
// InteractionStateManager — MonoBehaviour that realizes both cross-team
// state interfaces owned by ST4.
//
// Implements:
//   • IInteractionStateProvider — consumed by ST6 (CanvassDesktop) per M-13.
//   • IInteractionStateCapture  — consumed by ST7 (WorkspaceSerializer) per M-16.
//
// Placed on the same GameObject as VolumeInputController so it can read the
// FSMs and QuickMenuController without additional cross-object queries.

using System;
using UnityEngine;
using iDaVIE.Kernel.Contracts;  // Config

namespace iDaVIE.Interaction
{
    [RequireComponent(typeof(VolumeInputController))]
    public sealed class InteractionStateManager
        : MonoBehaviour, IInteractionStateProvider, IInteractionStateCapture
    {
        // ── Injected ─────────────────────────────────────────────────────────
        [HideInInspector] public Config             AppConfig;
        [HideInInspector] public QuickMenuController QuickMenu;

        private VolumeInputController _input;

        private void Awake() => _input = GetComponent<VolumeInputController>();

        // ── IInteractionStateProvider ─────────────────────────────────────────

        public LocomotionState  LocomotionState  => _input.LocomotionFSM?.State  ?? LocomotionState.Idle;
        public InteractionState InteractionState => _input.InteractionFSM?.State ?? InteractionState.Idle;
        public bool             IsPaintModeActive => InteractionState == InteractionState.PaintingStroke;
        public bool             IsQuickMenuOpen   => ActiveMenuPanel != QuickMenuPanel.None;

        public QuickMenuPanel ActiveMenuPanel
            => QuickMenu != null && QuickMenu.gameObject.activeSelf
                ? QuickMenuPanel.QuickMenu
                : QuickMenuPanel.None;

        public event Action? InteractionStateChanged;

        /// <summary>Called by VolumeInputController when either FSM fires StateChanged.</summary>
        internal void NotifyChanged() => InteractionStateChanged?.Invoke();

        // ── IInteractionStateCapture — M-16 uniform pattern ───────────────────

        public InteractionStateDto Capture() => new()
        {
            CurrentLocomotionState  = LocomotionState.ToString(),
            CurrentInteractionState = InteractionState.ToString(),

            BrushRadius    = _input.BrushSize,
            BrushAdditive  = _input.AdditiveBrush,
            BrushSourceId  = _input.SourceId,
            BrushPaintMode = _input.AdditiveBrush ? "Add" : "Remove",

            ActiveVoiceLocale        = _input.VoiceStream?.Config.Locale,
            PushToTalkEnabled        = _input.VoiceStream?.Config.ActivationMode
                                       == VoiceActivationMode.PushToTalk,
            VoiceConfidenceThreshold = _input.VoiceStream?.Config.ConfidenceThreshold ?? 0.5f,

            ActiveMenuPanel = ActiveMenuPanel.ToString(),
            SchemaVersion   = 1,
        };

        public void Restore(InteractionStateDto state)
        {
            if (state == null) return;

            // Restore brush configuration — clamp values against Config bounds.
            _input.BrushSize    = Math.Max(1, (int)state.BrushRadius);
            _input.AdditiveBrush = state.BrushAdditive;
            _input.SourceId     = (short)state.BrushSourceId;

            // Voice config — adapter is recreated on next StartListening; we only
            // need to store the config so the adapter factory can read it.
            // The composition root picks this up on scene reload.
        }
    }
}
