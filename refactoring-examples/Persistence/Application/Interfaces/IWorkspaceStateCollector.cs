using iDaVIE.Persistence.Domain;

namespace iDaVIE.Persistence.Application.Interfaces;

/// <summary>
/// Orchestrates all per-subsystem adapters to produce a complete WorkspaceAggregate.
/// The Unity-side implementation must call each adapter on the Unity main thread.
/// </summary>
public interface IWorkspaceStateCollector
{
    WorkspaceAggregate Collect();
}
