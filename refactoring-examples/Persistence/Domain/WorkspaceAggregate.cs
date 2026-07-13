using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Domain;

/// <summary>
/// Aggregate root that holds the live sub-DTOs during a session.
/// Owns invariant enforcement and snapshot creation.
/// </summary>
public class WorkspaceAggregate
{
    public DataIoStateDto      DataIo      { get; set; } = new();
    public RenderingStateDto   Rendering   { get; set; } = new();
    public FeatureStateDto?    Features    { get; set; }
    public InteractionStateDto Interaction { get; set; } = new();
    public GuiStateDto?        Gui         { get; set; }

    /// <summary>
    /// Infers the WorkspaceProfile from which sub-states are populated.
    /// </summary>
    public WorkspaceProfile InferProfile()
    {
        bool hasMask     = Rendering.Mask is { MaskMode: not null } && Rendering.Mask.MaskMode != "Disabled";
        bool hasFeatures = Features is { FeatureSets.Count: > 0 };
        bool hasFoveat   = Rendering.Foveation?.FoveatedRendering == true;

        return (hasMask || hasFeatures || hasFoveat) switch
        {
            _ when hasMask && hasFeatures => WorkspaceProfile.FullWorkspace,
            _ when hasFeatures            => WorkspaceProfile.DataWithFeatures,
            _ when hasMask                => WorkspaceProfile.DataWithMask,
            _                             => WorkspaceProfile.DataOnly,
        };
    }

    /// <summary>
    /// Produces an immutable snapshot from the current aggregate state.
    /// </summary>
    public WorkspaceSnapshot Capture(WorkspaceProfile? profileOverride = null)
    {
        var profile = profileOverride ?? InferProfile();
        return new WorkspaceSnapshot
        {
            Metadata    = WorkspaceMetadata.Now(profile),
            DataIo      = DataIo,
            Rendering   = Rendering,
            Features    = Features,
            Interaction = Interaction,
            Gui         = Gui,
        };
    }
}
