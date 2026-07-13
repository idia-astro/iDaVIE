using System;
using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;

namespace iDaVIE.Persistence.Application;

/// <summary>
/// Orchestrates a complete save cycle:
///   collect → stub → validate → serialize → ring-push.
/// Called by AutosaveService (timer) and by the UI Save button.
/// </summary>
public class SaveWorkspaceUseCase
{
    private readonly Func<IWorkspaceStateCollector> _collectorFactory;
    private readonly WorkspaceStubFactory           _stubFactory;
    private readonly WorkspaceValidator             _validator;
    private readonly SnapshotSerializer             _serializer;
    private readonly SnapshotRing                   _ring;

    private volatile bool _saveInProgress;

    public SaveWorkspaceUseCase(
        Func<IWorkspaceStateCollector> collectorFactory,
        WorkspaceStubFactory           stubFactory,
        WorkspaceValidator             validator,
        SnapshotSerializer             serializer,
        SnapshotRing                   ring)
    {
        _collectorFactory = collectorFactory;
        _stubFactory      = stubFactory;
        _validator        = validator;
        _serializer       = serializer;
        _ring             = ring;
    }

    /// <summary>
    /// Executes one save cycle. Returns false (and does nothing) if a save is already running.
    /// This method is synchronous so it can be called directly from the Unity main thread
    /// or awaited on a background thread after state collection is complete.
    /// </summary>
    public bool Execute()
    {
        if (_saveInProgress) return false;
        _saveInProgress = true;
        try
        {
            return RunSave();
        }
        finally
        {
            _saveInProgress = false;
        }
    }

    private bool RunSave()
    {
        // 1. Resolve collector — returns null if no dataset is loaded yet
        var collector = _collectorFactory();
        if (collector == null)
            return false;

        var aggregate = collector.Collect();

        // 2. Build profile-appropriate stub and fill with captured DTOs
        var profile  = aggregate.InferProfile();
        var snapshot = _stubFactory.CreateStub(profile) with
        {
            Metadata    = WorkspaceMetadata.Now(profile),
            DataIo      = aggregate.DataIo,
            Rendering   = aggregate.Rendering,
            Features    = aggregate.Features,
            Interaction = aggregate.Interaction,
            Gui         = aggregate.Gui,
        };

        // 3. Validate (non-blocking: mark warnings, do not abort)
        var validationResult = _validator.Validate(snapshot);
        if (validationResult.HasWarnings)
        {
            snapshot = snapshot with
            {
                Metadata = snapshot.Metadata with { HasValidationWarnings = true }
            };
        }

        // 4 & 5. Serialize + push to ring (includes atomic write + checksum)
        _ring.Push(snapshot, _serializer);
        return true;
    }
}
