using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Implemented in Assets/Scripts/Persistence/Adapters/InteractionStateAdapter.cs (Unity side).
/// Reads VolumeInputController public fields plus GetLocomotionStateString().
/// </summary>
public interface IInteractionStateAdapter
{
    InteractionStateDto Capture();
    void Restore(InteractionStateDto dto);
}
