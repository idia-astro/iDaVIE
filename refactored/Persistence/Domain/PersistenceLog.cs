// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceLog — ST7 Domain. Append-only audit trail of save / load / delete
// / migrate events.

using System;
using System.Collections.Generic;

namespace iDaVIE.Persistence.Domain
{
    public enum PersistenceEventKind { Save, Load, Delete, Migrate }

    public readonly record struct PersistenceLogEntry(
        DateTime             Timestamp,
        PersistenceEventKind Kind,
        string               StateId,
        bool                 Success,
        string               Detail);

    internal sealed class PersistenceLog
    {
        public IReadOnlyList<PersistenceLogEntry> Entries => throw new NotImplementedException();
        public void Append(PersistenceLogEntry entry)    => throw new NotImplementedException();
    }
}
