using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Infrastructure;

/// <summary>
/// Creates a minimal snapshot stub for the given session profile.
/// Avoids serialising null-heavy generic objects — only the sub-sections
/// required by the profile are allocated.
/// </summary>
public class WorkspaceStubFactory
{
    public WorkspaceSnapshot CreateStub(WorkspaceProfile profile)
    {
        var metadata    = WorkspaceMetadata.Now(profile);
        var dataIo      = new DataIoStateDto();
        var interaction = new InteractionStateDto();

        return profile switch
        {
            WorkspaceProfile.DataOnly => new WorkspaceSnapshot
            {
                Metadata    = metadata,
                DataIo      = dataIo,
                Rendering   = new RenderingStateDto(),
                Features    = null,
                Interaction = interaction,
                Gui         = null,
            },

            WorkspaceProfile.DataWithMask => new WorkspaceSnapshot
            {
                Metadata    = metadata,
                DataIo      = dataIo,
                Rendering   = new RenderingStateDto
                {
                    Mask = new MaskStateDto { MaskMode = "Enabled" }
                },
                Features    = null,
                Interaction = interaction,
                Gui         = null,
            },

            WorkspaceProfile.DataWithFeatures => new WorkspaceSnapshot
            {
                Metadata    = metadata,
                DataIo      = dataIo,
                Rendering   = new RenderingStateDto(),
                Features    = new FeatureStateDto(),
                Interaction = interaction,
                Gui         = new GuiStateDto(),
            },

            WorkspaceProfile.FullWorkspace => new WorkspaceSnapshot
            {
                Metadata    = metadata,
                DataIo      = dataIo,
                Rendering   = new RenderingStateDto
                {
                    Mask      = new MaskStateDto { MaskMode = "Enabled" },
                    Foveation = new FoveationStateDto(),
                    MomentMaps = new MomentMapStateDto(),
                },
                Features    = new FeatureStateDto(),
                Interaction = interaction,
                Gui         = new GuiStateDto(),
            },

            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown WorkspaceProfile.")
        };
    }
}
