using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Implemented in Assets/Scripts/Persistence/Adapters/DataIoStateAdapter.cs (Unity side).
/// Reads from VolumeDataSet via VolumeDataSetRenderer and AstTool attribute queries.
/// </summary>
public interface IDataIoStateAdapter
{
    DataIoStateDto Capture();
    void Restore(DataIoStateDto dto);
}
