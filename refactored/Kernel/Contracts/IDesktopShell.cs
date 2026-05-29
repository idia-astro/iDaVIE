// SPDX-License-Identifier: LGPL-3.0-or-later
// IDesktopShell — ST1 cross-team contract; realised by ST6's CanvassDesktop
// (M-26). Mount-point for any panel that other sub-teams need to surface in
// the desktop GUI — including ST7's save/load dialogs.

namespace iDaVIE.Kernel.Contracts
{
    /// <summary>Opaque handle for a registered panel. Returned by RegisterPanel
    /// and accepted by ShowPanel / HidePanel / UnregisterPanel.</summary>
    public readonly record struct PanelHandle(int Id);

    public interface IDesktopShell
    {
        PanelHandle RegisterPanel(string title, object viewModel);
        void        ShowPanel(PanelHandle handle);
        void        HidePanel(PanelHandle handle);
        void        UnregisterPanel(PanelHandle handle);
    }
}
