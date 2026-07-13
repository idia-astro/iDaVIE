using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Infrastructure.Migrations;

/// <summary>
/// Adds the TransformedSpectralValue field (absent from the original contract DTO).
/// Defaults to 0.0 — the value will be revalidated against the restored AST frame on load.
/// </summary>
public class Migration_1_0_to_1_1 : ISchemaMigration
{
    public string FromVersion => "1.0.0";
    public string ToVersion   => "1.1.0";

    public WorkspaceSnapshot Migrate(WorkspaceSnapshot old)
    {
        // TransformedSpectralValue defaults to 0.0 when missing (double default).
        // The DataIo adapter will revalidate it against the restored AST frame after load.
        // Mutate in place — DTOs are mutable classes and the snapshot is already a deserialized copy.
        old.DataIo.TransformedSpectralValue = 0.0;

        return old with
        {
            Metadata = old.Metadata with { SchemaVersion = ToVersion },
        };
    }
}
