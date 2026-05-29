// SPDX-License-Identifier: LGPL-3.0-or-later
// MigrationRule — ST7 Domain. Strategy applied during load when the stored
// envelope is older than the current SchemaVersion. ST7 owns envelope-level
// migrations only; per-team payload migrations are owned by each team.

namespace iDaVIE.Persistence.Domain
{
    internal interface IMigrationRule
    {
        int FromVersion { get; }
        int ToVersion   { get; }
        StoredState Apply(StoredState input);
    }

    internal sealed class MigrationRule : IMigrationRule
    {
        public int FromVersion { get; init; }
        public int ToVersion   { get; init; }

        public StoredState Apply(StoredState input) => throw new System.NotImplementedException();
    }
}
