// SPDX-License-Identifier: LGPL-3.0-or-later
// LoadUseCase — ST7 Application. Realises IWorkspaceLoadCommand. Reads
// StoredState, validates integrity, applies envelope migrations, dispatches
// per-team payloads to the matching Restore ports, logs the load, fires
// IPersistenceEvents.LoadStarted / LoadCompleted / LoadFailed.

using iDaVIE.Data.Contracts;
using iDaVIE.Features;
using iDaVIE.Interaction;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Persistence.Domain;
using iDaVIE.Rendering.Contracts;
using iDaVIE.UI.Contracts;

namespace iDaVIE.Persistence.Application
{
    internal sealed class LoadUseCase : IWorkspaceLoadCommand
    {
        public LoadUseCase(
            IVolumeStateCapture       volume,
            IMaskStateCapture         mask,
            IRenderStateCapture       render,
            IInteractionStateCapture  interaction,
            IFeatureStateCapture      features,
            IDesktopStateCapture      desktop,
            ValidationAndRecoveryService validation,
            PersistenceEventDispatcher events)
            => throw new System.NotImplementedException();

        public void Load(string stateId) => throw new System.NotImplementedException();
    }
}
