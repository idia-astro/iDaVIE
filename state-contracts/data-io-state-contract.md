# Data I/O State Contract

## Owned By
Data IO Sub-team (FitsReader, AstTool, DataAnalysis)

## Purpose
Manages the persistence of FITS cube loading parameters, WCS coordinate transformations, subcube extraction bounds, and target spectral coordinate systems.

## Required Persistent State

### 1. FITS Dataset State
| Field | Type | Required? | Description |
|---|---|---|---|
| `FileName` | `string` | Yes | Path to the primary FITS image |
| `SelectedHdu` | `int` | Yes | Which HDU to read |
| `subsetBounds` | `int[]` | Yes | User crop region `[xMin, xMax, yMin, yMax, zMin, zMax]` |
| `MaskFileName` | `string?` | Optional | Path to mask file (if any) |
| `index2` | `int` | Optional | For 4D files |
| `sliceDim` | `int` | Optional | For 4D files |

### 2. Spectral Transformation State
| Field | Type | Description | Shared With |
|---|---|---|---|
| `PrimarySpectralSystem` | `string` | Base system of the loaded cube | Persistence |
| `AlternativeSpectralTarget` | `string` | Target system (e.g., FREQ, VRAD) | Persistence |
| `AlternativeSpectralUnit` | `string` | Target unit (e.g., Hz, km/s) | Persistence |
| `StandardOfRest` | `string` | Rest-frame label | Persistence |
| `FormattedAltCoord` | `string` | Human-readable transformed axis | GUI |
| `TransformedSpectralValue`| `double` | Numeric transformed coordinate | Persistence |

## Supported vs. Unsupported Spectral Systems
* **Supported:** `FREQ`, `VRAD`, `VRADIO`, `VOPT`, `VOPTICAL`, `VELO`, `VREL`, `WAVN`, `WAVENUM`, `WAVE`, `WAVELEN`, `AWAV`, `AIRWAVE`.
* **Unsupported:** `ENER`, `ENERGY`, `ZOPT`, `REDSHIFT`, `BETA`.
*(Unsupported systems must log warnings, avoid invalid alternate frame generation, and trigger safe fallback behavior).*

## Validation Rules
- File paths must be valid strings.
- HDU indices must be positive.
- `subsetBounds` must remain strictly inside original cube dimensions.
- Spectral systems must match supported AST systems.

## Recovery Rules
- **Missing File Paths:** Mark dataset unavailable, preserve metadata, allow manual relinking.
- **Missing HDU/Subcube:** Attempt default HDU restore; restore nearest valid subset bounds; prevent invalid access.
- **Invalid Spectral Transform:** Fallback to `PrimarySpectralSystem`, preserve raw coordinate data, log conversion failure.

## Important Constraints
- **Zero Unity Dependencies:** This domain must remain pure.
- **Native Memory:** Native memory ownership must remain explicit. All native allocations (AST/DataAnalysis buffers) must be released safely.
- **Graceful Failure:** Invalid AST transforms must fail gracefully without crashing the application.

## Persistence DTO
```csharp
public class DataIoStateDto
{
    public string FileName { get; set; }
    public string MaskFileName { get; set; }
    public int[] SubsetBounds { get; set; }
    public int SelectedHdu { get; set; } = 1;
    public int? Index2 { get; set; }
    public int? SliceDim { get; set; }
    
    public string PrimarySpectralSystem { get; set; }
    public string AlternativeSpectralTarget { get; set; }
    public string AlternativeSpectralUnit { get; set; }
    public string StandardOfRest { get; set; }
}