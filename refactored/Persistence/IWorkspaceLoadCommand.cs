// SPDX-License-Identifier: LGPL-3.0-or-later
// IWorkspaceLoadCommand — ST7 cross-team contract. Consumed by ST4 and ST6.
// StateId is an opaque string returned by IStateIndexQuery.

namespace iDaVIE.Persistence
{
    public interface IWorkspaceLoadCommand
    {
        /// <summary>Triggers a load by opaque StateId. Outcome reported via
        /// IPersistenceEvents.LoadCompleted / LoadFailed.</summary>
        void Load(string stateId);
    }
}
