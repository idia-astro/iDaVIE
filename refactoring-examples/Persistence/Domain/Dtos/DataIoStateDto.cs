namespace iDaVIE.Persistence.Domain.Dtos;

/// <summary>
/// Persistent state for the Data I/O sub-system.
/// Source fields: VolumeDataSet.FileName, SelectedHdu, subsetBounds, and AstFrameSet attributes.
/// NOT persisted: IntPtr handles (FitsData, FitsHeader, AstFrameSet, AstAltSpecSet) — rebuilt on restore.
/// </summary>
public class DataIoStateDto
{
    /// <summary>Absolute path to the primary FITS image.</summary>
    [PersistField]
    public string? FileName { get; set; }

    /// <summary>Path to the mask FITS file, if any.</summary>
    [PersistField(Optional = true)]
    public string? MaskFileName { get; set; }

    /// <summary>User crop region [xMin, xMax, yMin, yMax, zMin, zMax] (1-indexed).</summary>
    [PersistField]
    public int[]? SubsetBounds { get; set; }

    /// <summary>FITS HDU index (1-indexed).</summary>
    [PersistField]
    public int SelectedHdu { get; set; } = 1;

    /// <summary>For 4-D cubes: index of dimension shown on Z-axis (2 or 3).</summary>
    [PersistField(Optional = true)]
    public int? Index2 { get; set; }

    /// <summary>For 4-D cubes: slice index within the 4th dimension.</summary>
    [PersistField(Optional = true)]
    public int? SliceDim { get; set; }

    /// <summary>Primary AST spectral system (e.g. "FREQ", "VRAD").</summary>
    [PersistField]
    public string? PrimarySpectralSystem { get; set; }

    /// <summary>Target alternative spectral system.</summary>
    [PersistField]
    public string? AlternativeSpectralTarget { get; set; }

    /// <summary>Target alternative spectral unit (e.g. "km/s", "Hz").</summary>
    [PersistField]
    public string? AlternativeSpectralUnit { get; set; }

    /// <summary>AST rest-frame label (e.g. "Heliocentric").</summary>
    [PersistField]
    public string? StandardOfRest { get; set; }

    /// <summary>
    /// Numeric transformed spectral coordinate (gap field — absent from original contract DTO).
    /// Revalidated against the restored AST frame after load.
    /// </summary>
    [PersistField]
    public double TransformedSpectralValue { get; set; }
}
