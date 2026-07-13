using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Implemented in Assets/Scripts/Persistence/Adapters/GuiStateAdapter.cs (Unity side).
/// Reads VolumeDataSetRenderer.CubeDepthAxis (line 127) only.
/// </summary>
public interface IGuiStateAdapter
{
    GuiStateDto? Capture();
    void Restore(GuiStateDto dto);
}
