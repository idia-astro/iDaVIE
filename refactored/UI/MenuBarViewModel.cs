// SPDX-License-Identifier: LGPL-3.0-or-later
// MenuBarViewModel — ST6 ViewModel replacing the menu state in
// Assets/Scripts/UI/MenuBarBehaviour.cs (85 LOC). Drives the top menu bar's
// open / save / import / exit commands.

using System;
using iDaVIE.Features;                  // IFeatureImportService
using iDaVIE.Kernel.Contracts;          // IVolumeLoader
using iDaVIE.Persistence;               // IWorkspaceSaveCommand

namespace iDaVIE.UI
{
    public sealed class MenuBarViewModel
    {
        public MenuBarViewModel(IVolumeLoader loader,
                                IFeatureImportService imports,
                                IWorkspaceSaveCommand save)
            => throw new NotImplementedException();

        public event Action FileDialogRequested;
        public event Action ImportDialogRequested;
        public event Action ExitRequested;

        public void OnOpenFile()        => throw new NotImplementedException();
        public void OnCloseFile()       => throw new NotImplementedException();
        public void OnSaveWorkspace()   => throw new NotImplementedException();
        public void OnImportCatalogue() => throw new NotImplementedException();
        public void OnExit()            => throw new NotImplementedException();
    }
}
