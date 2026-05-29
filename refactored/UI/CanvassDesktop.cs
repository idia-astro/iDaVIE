// SPDX-License-Identifier: LGPL-3.0-or-later
// CanvassDesktop — refactored thin shell. Replaces the 1899 LOC god class in
// Assets/Scripts/UI/CanvassDesktop.cs. Holds one ViewModel per tab; binds
// Unity prefab elements to the ViewModel properties / events.
//
// Realises ST1's IDesktopShell (M-26) so other sub-teams' panels (notably ST7's
// save / load dialogs) can mount through the shell without referencing this
// class directly. Also realises ST6's IDesktopStateCapture (M-16) for ST7.

using UnityEngine;
using iDaVIE.Kernel.Contracts;          // IDesktopShell, PanelHandle
using iDaVIE.UI.Contracts;              // IDesktopStateCapture, DesktopStateDto

namespace iDaVIE.UI
{
    internal sealed class CanvassDesktop : MonoBehaviour, IDesktopShell, IDesktopStateCapture
    {
        // One ViewModel per tab — wired by composition root via Inject.
        public RenderTabViewModel       RenderTab      { get; private set; }
        public SourcesTabViewModel      SourcesTab     { get; private set; }
        public StatsTabViewModel        StatsTab       { get; private set; }
        public PaintTabViewModel        PaintTab       { get; private set; }
        public InformationTabViewModel  InformationTab { get; private set; }
        public MenuBarViewModel         MenuBar        { get; private set; }
        public DebugTabViewModel        DebugTab       { get; private set; }

        public void Inject(
            RenderTabViewModel renderTab, SourcesTabViewModel sourcesTab,
            StatsTabViewModel statsTab,   PaintTabViewModel paintTab,
            InformationTabViewModel informationTab,
            MenuBarViewModel menuBar,     DebugTabViewModel debugTab)
            => throw new System.NotImplementedException();

        // ── IDesktopShell ───────────────────────────────────────────────────
        public PanelHandle RegisterPanel(string title, object viewModel) => throw new System.NotImplementedException();
        public void        ShowPanel(PanelHandle handle)                 => throw new System.NotImplementedException();
        public void        HidePanel(PanelHandle handle)                 => throw new System.NotImplementedException();
        public void        UnregisterPanel(PanelHandle handle)           => throw new System.NotImplementedException();

        // ── IDesktopStateCapture (M-16) ─────────────────────────────────────
        public DesktopStateDto Capture()                  => throw new System.NotImplementedException();
        public void            Restore(DesktopStateDto dto) => throw new System.NotImplementedException();
    }
}
