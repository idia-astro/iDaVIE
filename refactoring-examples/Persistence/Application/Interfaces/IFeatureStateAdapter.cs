using iDaVIE.Persistence.Domain.Dtos;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Implemented in Assets/Scripts/Persistence/Adapters/FeatureStateAdapter.cs (Unity side).
/// Calls FeatureSetManager.CaptureState(); serialises ColumnMapping as Dictionary&lt;string,string&gt;.
/// </summary>
public interface IFeatureStateAdapter
{
    /// <summary>Returns null if no feature sets are currently loaded.</summary>
    FeatureStateDto? Capture();
    void Restore(FeatureStateDto dto);
}
