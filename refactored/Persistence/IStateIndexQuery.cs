// SPDX-License-Identifier: LGPL-3.0-or-later
// IStateIndexQuery — ST7 cross-team contract. Enumerates saved workspaces for
// ST6's load dialog and state-list panel.

using System;
using System.Collections.Generic;

namespace iDaVIE.Persistence
{
    /// <summary>Metadata for a single saved workspace.</summary>
    public readonly record struct SavedStateInfo(
        string   StateId,
        string   Label,
        DateTime CreatedAt,
        long     SizeBytes,
        int      SchemaVersion);

    public interface IStateIndexQuery
    {
        IReadOnlyList<SavedStateInfo> GetAll();
        IReadOnlyList<SavedStateInfo> Search(string term);
        SavedStateInfo?               GetById(string stateId);
    }
}
