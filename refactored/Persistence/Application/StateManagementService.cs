// SPDX-License-Identifier: LGPL-3.0-or-later
// StateManagementService — ST7 Application. Realises IStateIndexQuery.
// Create / rename / delete / enumerate StoredState entries via StateIndex.
// Enforces retention policy from PersistenceConfig.

using System.Collections.Generic;
using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Application
{
    internal sealed class StateManagementService : IStateIndexQuery
    {
        public StateManagementService(StateIndex index, PersistenceConfig config)
            => throw new System.NotImplementedException();

        public IReadOnlyList<SavedStateInfo> GetAll()                  => throw new System.NotImplementedException();
        public IReadOnlyList<SavedStateInfo> Search(string term)       => throw new System.NotImplementedException();
        public SavedStateInfo?               GetById(string stateId)   => throw new System.NotImplementedException();

        public void Rename(string stateId, string newLabel) => throw new System.NotImplementedException();
        public void Delete(string stateId)                  => throw new System.NotImplementedException();
    }
}
