using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Implemented in Assets/Scripts/Persistence/Adapters/RenderingStateAdapter.cs (Unity side).
/// Calls VolumeDataSetRenderer.CaptureState() and wraps Unity transform fields.
/// </summary>
public interface IRenderingStateAdapter
{
    RenderingStateDto Capture();
    void Restore(RenderingStateDto dto);
}
