// SPDX-License-Identifier: LGPL-3.0-or-later
// StateIndex — ST7 Domain. Catalogue of all StoredState instances. Owns
// lookup, ordering, retention policy. Drives the "list of saves" UI surface.

using System.Collections.Generic;

namespace iDaVIE.Persistence.Domain
{
    internal sealed class StateIndex
    {
        public IReadOnlyList<SavedStateInfo> Entries => throw new System.NotImplementedException();

        public void Add(SavedStateInfo entry)        => throw new System.NotImplementedException();
        public void Remove(string stateId)           => throw new System.NotImplementedException();
        public void Rename(string stateId, string newLabel) => throw new System.NotImplementedException();
        public SavedStateInfo? Find(string stateId)  => throw new System.NotImplementedException();

        /// <summary>Applies the retention policy from PersistenceConfig, removing
        /// the oldest entries past the cap.</summary>
        public void Prune(int maxEntries)            => throw new System.NotImplementedException();
    }
}
