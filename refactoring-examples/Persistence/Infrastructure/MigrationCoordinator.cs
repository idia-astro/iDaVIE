using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure.Migrations;

namespace iDaVIE.Persistence.Infrastructure;

/// <summary>
/// Contract for a single schema migration step.
/// </summary>
public interface ISchemaMigration
{
    string FromVersion { get; }
    string ToVersion   { get; }
    WorkspaceSnapshot Migrate(WorkspaceSnapshot snapshot);
}

/// <summary>
/// Runs the registered migration chain to bring any older snapshot up to the current schema.
///
/// Semantic versioning rules:
///   Major bump → breaking; migration required.
///   If snapshot.Major > current.Major → reject (file written by a newer tool).
///   Minor/patch bump → additive; migrations add defaults for new fields.
/// </summary>
public class MigrationCoordinator
{
    public static readonly string CurrentVersion = "1.1.0";

    private readonly List<ISchemaMigration> _migrations = new()
    {
        new Migration_1_0_to_1_1(),
    };

    /// <summary>
    /// Returns the migrated snapshot, or null if the snapshot's major version is higher
    /// than the current major version (incompatible — newer tool required).
    /// </summary>
    public WorkspaceSnapshot? MigrateIfNeeded(WorkspaceSnapshot snapshot)
    {
        string version = snapshot.Metadata.SchemaVersion ?? "1.0.0";

        if (!TryParseVersion(version, out int snapMajor, out _, out _))
            return null;  // Unparseable version → reject

        if (!TryParseVersion(CurrentVersion, out int currMajor, out _, out _))
            throw new InvalidOperationException($"CurrentVersion '{CurrentVersion}' is not a valid semver.");

        if (snapMajor > currMajor)
            return null;  // Snapshot written by a newer tool — reject

        // Apply migrations in order until we reach CurrentVersion
        string current = version;
        while (!string.Equals(current, CurrentVersion, StringComparison.OrdinalIgnoreCase))
        {
            var migration = _migrations.FirstOrDefault(m =>
                string.Equals(m.FromVersion, current, StringComparison.OrdinalIgnoreCase));

            if (migration is null)
                break;  // No path forward — already at or past current

            snapshot = migration.Migrate(snapshot);
            current  = migration.ToVersion;
        }

        return snapshot;
    }

    private static bool TryParseVersion(string v, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        var parts = v.Split('.');
        if (parts.Length != 3) return false;
        return int.TryParse(parts[0], out major)
            && int.TryParse(parts[1], out minor)
            && int.TryParse(parts[2], out patch);
    }
}
