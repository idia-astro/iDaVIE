using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Domain;

/// <summary>
/// Immutable point-in-time value object representing a complete workspace state.
/// Declared as a non-positional record so Newtonsoft.Json can deserialise via the
/// parameterless constructor + property setters, while still supporting `with` expressions.
///
/// Sub-sections that are null were absent in the saved session profile
/// (e.g. Features == null when no feature sets were loaded).
/// </summary>
public sealed record WorkspaceSnapshot
{
    public WorkspaceMetadata   Metadata    { get; set; } = new() { Profile = WorkspaceProfile.DataOnly };
    public DataIoStateDto      DataIo      { get; set; } = new();
    public RenderingStateDto   Rendering   { get; set; } = new();
    public FeatureStateDto?    Features    { get; set; }
    public InteractionStateDto Interaction { get; set; } = new();
    public GuiStateDto?        Gui         { get; set; }
}
