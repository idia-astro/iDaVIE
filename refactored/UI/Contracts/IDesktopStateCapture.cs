// SPDX-License-Identifier: LGPL-3.0-or-later
// IDesktopStateCapture — ST6 persistence port (M-16). Consumed by ST7's
// SaveUseCase / LoadUseCase to snapshot the desktop UI layout.

using System.Collections.Generic;

namespace iDaVIE.UI.Contracts
{
    public sealed class DesktopStateDto
    {
        public int                          SchemaVersion { get; set; } = 1;
        public string                       ActiveTab     { get; set; } = "";
        public Dictionary<string, bool>     PanelVisibility { get; set; } = new();
        public bool                         DebugConsoleOpen { get; set; }
    }

    public interface IDesktopStateCapture
    {
        DesktopStateDto Capture();
        void            Restore(DesktopStateDto dto);
    }
}
