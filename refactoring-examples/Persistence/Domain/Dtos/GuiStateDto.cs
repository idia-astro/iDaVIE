namespace iDaVIE.Persistence.Domain.Dtos;

/// <summary>
/// Minimal GUI-only persistent state.
/// All other "GUI" fields (FileName, SelectedHdu, SubsetBounds, MaskFileName, trueBounds)
/// are authoritative in DataIoStateDto and must not be duplicated here.
/// trueBounds is always recomputed from the FITS header on restore (stale if persisted).
/// Source: VolumeDataSetRenderer.CubeDepthAxis (line 127).
/// </summary>
public class GuiStateDto
{
    /// <summary>
    /// The axis used as the depth/Z axis in the cube view (0-indexed).
    /// Default 2 = standard FITS Z axis. Clamped to [0,2] by WorkspaceValidator.
    /// </summary>
    [PersistField] public int CubeDepthAxis { get; set; } = 2;
}
