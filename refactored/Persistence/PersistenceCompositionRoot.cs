// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceCompositionRoot — sole site that calls `new` on ST7 concretes.
// Follows the same pattern as ST1's KernelCompositionRoot.
// ST7-internal; does not cross the cross-team boundary.
//
// Legacy: no equivalent exists. Object construction for the existing
// subsystems is scattered: VolumeDataSetRenderer.Start() (line 353) does
// FindObjectOfType, AddComponent, and Config.Instance reads inline — a
// Creator + DIP violation that this root corrects for ST7.
//
// Refactor delta:
//   - GRASP Creator: PersistenceCompositionRoot is the only class allowed to
//     instantiate WorkspaceRepository and WorkspaceService. All consumers
//     receive the four ST7 interfaces (SaveCommand, LoadCommand, IndexQuery,
//     Events) — never the concrete WorkspaceService type.
//   - DIP: all six capture ports arrive as constructor parameters (injected by
//     ST1's KernelCompositionRoot); no singleton access anywhere in ST7.
//   - The four ST7-owned interfaces are exposed as properties so
//     KernelCompositionRoot can register them in IPluginRegistry without
//     knowing the WorkspaceService concrete type.

using iDaVIE.Data;                              // IMaskStateCapture (ST2)
using iDaVIE.Features;                          // IFeatureStateCapture (ST5)
using iDaVIE.Interaction;                       // IInteractionStateCapture (ST4)
using iDaVIE.Kernel.Contracts;                  // Config, ILogSink
using iDaVIE.Kernel.Contracts.Persistence;      // IVolumeStateCapture (ST1)
using iDaVIE.Rendering.Contracts;               // IRenderStateCapture (ST3)
using iDaVIE.UI;                                // IDesktopStateCapture (ST6)

namespace iDaVIE.Persistence.Internal
{
    internal sealed class PersistenceCompositionRoot
    {
        // Single WorkspaceService instance realises all four ST7 interfaces.
        private readonly WorkspaceService _service;

        public IPersistenceEvents    Events      => _service;
        public IWorkspaceSaveCommand SaveCommand => _service;
        public IWorkspaceLoadCommand LoadCommand => _service;
        public IStateIndexQuery      IndexQuery  => _service;

        public PersistenceCompositionRoot(
            Config                   config,
            ILogSink                 log,
            IVolumeStateCapture      volumeCapture,
            IMaskStateCapture        maskCapture,
            IRenderStateCapture      renderCapture,
            IInteractionStateCapture interactionCapture,
            IFeatureStateCapture     featureCapture,
            IDesktopStateCapture     desktopCapture)
        {
            var repository = new WorkspaceRepository(config, log);

            _service = new WorkspaceService(
                volumeCapture,
                maskCapture,
                renderCapture,
                interactionCapture,
                featureCapture,
                desktopCapture,
                repository,
                log);
        }
    }
}