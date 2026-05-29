// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// PersistenceCompositionRoot: the sole place that calls `new` on ST7 concretes.
// Follows the same pattern as ST1's KernelCompositionRoot.
// ST7-internal — does not cross the cross-team boundary.

namespace iDaVIE.Persistence.Internal
{
    using iDaVIE.Data;                              // IMaskStateCapture (ST2)
    using iDaVIE.Features;                          // IFeatureStateCapture (ST5)
    using iDaVIE.Interaction;                       // IInteractionStateCapture (ST4)
    using iDaVIE.Kernel.Contracts;                  // Config, ILogSink
    using iDaVIE.Kernel.Contracts.Persistence;      // IVolumeStateCapture (ST1)
    using iDaVIE.Rendering.Contracts;               // IRenderStateCapture (ST3)
    using iDaVIE.UI;                                // IDesktopStateCapture (ST6)

    /// <summary>
    /// Constructs the ST7 object graph from injected cross-team interfaces.
    /// Called once at application startup by ST1's <c>KernelCompositionRoot</c>.
    ///
    /// <para>
    /// Exposes the four ST7-owned interfaces as properties so the composition
    /// root can register them in <c>IPluginRegistry</c> without knowing the
    /// <see cref="WorkspaceService"/> concrete type.
    /// </para>
    /// </summary>
    internal sealed class PersistenceCompositionRoot
    {
        // The single WorkspaceService instance realises all four ST7 interfaces.
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