using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;

namespace iDaVIE.Persistence.Application;

/// <summary>Result of a load attempt.</summary>
public class LoadResult
{
    public bool Success { get; set; }
    public bool DatasetUnavailable { get; set; }
    public WorkspaceSnapshot? Snapshot { get; set; }
    public ValidationResult? Validation { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Loads and validates the most recent valid snapshot from the ring.
/// Does not perform restore (that is RestoreOrchestrator's responsibility).
/// </summary>
public class LoadWorkspaceUseCase
{
    private readonly SnapshotRing       _ring;
    private readonly SnapshotSerializer _serializer;
    private readonly WorkspaceValidator _validator;
    private readonly MigrationCoordinator _migrations;

    public LoadWorkspaceUseCase(
        SnapshotRing          ring,
        SnapshotSerializer    serializer,
        WorkspaceValidator    validator,
        MigrationCoordinator  migrations)
    {
        _ring       = ring;
        _serializer = serializer;
        _validator  = validator;
        _migrations = migrations;
    }

    /// <summary>
    /// Tries each ring slot newest-first until a valid snapshot is found.
    /// Returns a LoadResult with Success=false when all slots are exhausted.
    /// </summary>
    public LoadResult Execute()
    {
        var handles = _ring.GetAllNewestFirst();
        if (handles.Count == 0)
            return Fail("Snapshot ring is empty.");

        foreach (var handle in handles)
        {
            var attempt = TryLoad(handle);
            if (attempt.Success) return attempt;
        }

        return Fail("All snapshot slots are corrupted or invalid. Starting blank session.");
    }

    private LoadResult TryLoad(SnapshotHandle handle)
    {
        WorkspaceSnapshot snapshot;
        try
        {
            snapshot = _serializer.Deserialize(handle.FilePath);
        }
        catch (Exception ex)
        {
            return Fail($"Slot {handle.SlotIndex}: parse failure — {ex.Message}");
        }

        // Schema migration
        var migrated = _migrations.MigrateIfNeeded(snapshot);
        if (migrated is null)
            return Fail($"Slot {handle.SlotIndex}: schema version incompatible (newer tool required).");
        snapshot = migrated;

        // Validation + default application
        var validation = _validator.Validate(snapshot);

        return new LoadResult
        {
            Success           = true,
            DatasetUnavailable = validation.DatasetUnavailable,
            Snapshot          = snapshot,
            Validation        = validation,
        };
    }

    private static LoadResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
