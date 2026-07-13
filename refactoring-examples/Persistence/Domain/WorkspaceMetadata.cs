namespace iDaVIE.Persistence.Domain;

/// <summary>
/// Envelope metadata written to the root of every snapshot JSON file.
/// Declared as a record so WorkspaceSnapshot `with` expressions can produce modified copies.
/// </summary>
public record WorkspaceMetadata
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public DateTime SavedAt { get; set; }
    public WorkspaceProfile Profile { get; set; }

    /// <summary>Plugin versions at save time (e.g. "idavie_native" → "3.2.1").</summary>
    public Dictionary<string, string> PluginVersions { get; set; } = new();

    /// <summary>SHA-256 checksum over the JSON body (excluding this field).</summary>
    public string? Checksum { get; set; }

    /// <summary>True when WorkspaceValidator found and auto-corrected issues at save time.</summary>
    public bool HasValidationWarnings { get; set; }

    /// <summary>True when not all sub-states could be recovered (partial restore).</summary>
    public bool PartialRecovery { get; set; }

    public static WorkspaceMetadata Now(WorkspaceProfile profile) => new()
    {
        SchemaVersion = "1.0.0",
        SavedAt       = DateTime.UtcNow,
        Profile       = profile,
    };
}
