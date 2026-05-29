// SPDX-License-Identifier: LGPL-3.0-or-later
// SaveUseCase — ST7 Application. Realises IWorkspaceSaveCommand. Orchestrates
// capture: invokes all six per-team capture ports, composes payloads into a
// StoredState envelope, stamps metadata + integrity, writes via the Tier-2
// storage backend, updates StateIndex and PersistenceLog, fires
// IPersistenceEvents.

using iDaVIE.Data.Contracts;
using iDaVIE.Features;
using iDaVIE.Interaction;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Persistence.Domain;
using iDaVIE.Rendering.Contracts;
using iDaVIE.UI.Contracts;

namespace iDaVIE.Persistence.Application
{
    internal sealed class SaveUseCase : IWorkspaceSaveCommand
    {
        public SaveUseCase(
            IVolumeStateCapture       volume,
            IMaskStateCapture         mask,
            IRenderStateCapture       render,
            IInteractionStateCapture  interaction,
            IFeatureStateCapture      features,
            IDesktopStateCapture      desktop,
            StateIndex                index,
            PersistenceEventDispatcher events)
            => throw new System.NotImplementedException();

        public void Save(string label = null) => throw new System.NotImplementedException();
    }
}
