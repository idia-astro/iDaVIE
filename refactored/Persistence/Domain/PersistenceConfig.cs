// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceConfig — ST7 Domain. Defaults & policies: autosave cadence,
// retention, compression, default storage root, schema-migration policy.
// Loaded by Tier-2 PersistenceConfigLoader from ST1's IConfig.

namespace iDaVIE.Persistence.Domain
{
    public readonly record struct PersistenceConfig(
        StorageLocation DefaultLocation,
        int             MaxSavedWorkspaces,
        int             AutosaveIntervalSeconds,
        bool            CompressionEnabled,
        bool            MigrateOlderSchemasOnLoad);
}
