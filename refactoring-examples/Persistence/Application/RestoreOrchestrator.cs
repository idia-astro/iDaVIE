using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Application;

/// <summary>
/// Enforces the strict subsystem restore ordering required to avoid dependency violations:
///
///   1. Data I/O   — open FITS, validate SubsetBounds, init AST frames
///   2. Rendering  — apply RenderingState (Config defaults first, session overrides second)
///   3. Foveation  — (if FoveationState present)
///   4. Mask       — load mask file (if MaskState present)
///   5. MomentMaps — apply ColorMapM0/M1 etc.
///   6. Features   — restore identity fields, trigger async recomputation
///                   (mask bboxes, mask stats via DataAnalysis; imported RawData via catalog)
///   7. Interaction — apply after dataset validated
///   8. GUI        — CubeDepthAxis last (derives from domain state)
///
/// The adapter Restore() methods on the Unity side must be called on the Unity main thread.
/// </summary>
public class RestoreOrchestrator
{
    private readonly IDataIoStateAdapter      _dataIoAdapter;
    private readonly IRenderingStateAdapter   _renderingAdapter;
    private readonly IFeatureStateAdapter     _featureAdapter;
    private readonly IInteractionStateAdapter _interactionAdapter;
    private readonly IGuiStateAdapter         _guiAdapter;

    public RestoreOrchestrator(
        IDataIoStateAdapter      dataIoAdapter,
        IRenderingStateAdapter   renderingAdapter,
        IFeatureStateAdapter     featureAdapter,
        IInteractionStateAdapter interactionAdapter,
        IGuiStateAdapter         guiAdapter)
    {
        _dataIoAdapter      = dataIoAdapter;
        _renderingAdapter   = renderingAdapter;
        _featureAdapter     = featureAdapter;
        _interactionAdapter = interactionAdapter;
        _guiAdapter         = guiAdapter;
    }

    /// <summary>
    /// Executes the full restore sequence. Throws if a critical sub-restore fails.
    /// All steps must run on the Unity main thread.
    /// </summary>
    public void Restore(WorkspaceSnapshot snapshot)
    {
        // 1. Data I/O (must be first — everything else depends on the loaded dataset)
        _dataIoAdapter.Restore(snapshot.DataIo);

        // 2–5. Rendering, foveation, mask, moment maps
        _renderingAdapter.Restore(snapshot.Rendering);

        // 6. Features (requires Data I/O to be complete for recomputation)
        if (snapshot.Features is not null)
            _featureAdapter.Restore(snapshot.Features);

        // 7. Interaction
        _interactionAdapter.Restore(snapshot.Interaction);

        // 8. GUI — last; derives from domain state
        if (snapshot.Gui is not null)
            _guiAdapter.Restore(snapshot.Gui);
    }
}
