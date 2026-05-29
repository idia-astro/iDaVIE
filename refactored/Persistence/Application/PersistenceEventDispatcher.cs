// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceEventDispatcher — ST7 Application. Realises IPersistenceEvents.
// Invoked from SaveUseCase / LoadUseCase to broadcast progress + outcomes.

using System;

namespace iDaVIE.Persistence.Application
{
    internal sealed class PersistenceEventDispatcher : IPersistenceEvents
    {
        public event Action         SaveStarted;
        public event Action<string> SaveCompleted;
        public event Action<string> SaveFailed;
        public event Action         LoadStarted;
        public event Action         LoadCompleted;
        public event Action<string> LoadFailed;

        internal void RaiseSaveStarted()              => throw new NotImplementedException();
        internal void RaiseSaveCompleted(string id)   => throw new NotImplementedException();
        internal void RaiseSaveFailed(string error)   => throw new NotImplementedException();
        internal void RaiseLoadStarted()              => throw new NotImplementedException();
        internal void RaiseLoadCompleted()            => throw new NotImplementedException();
        internal void RaiseLoadFailed(string error)   => throw new NotImplementedException();
    }
}
