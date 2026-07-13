# GUI State Contract

## Owned By
GUI Sub-team

## Purpose
GUI persistence in iDaVIE is primarily derived from persisted domain/rendering state. Its main persistence responsibility is preserving the parameters utilized during the initial data upload/load workflows to bypass manual UI entry on restore.

## Persistent GUI State (File Loading Workflow)
| Field | Type | Description |
|---|---|---|
| `subsetBounds` | `int[]` | The user's crop regions entered in UI |
| `trueBounds` | `int[]` | Original file dimensions read from header |
| `FileName` | `string` | FITS path |
| `MaskFileName` | `string` | Mask path (if any) |
| `SelectedHdu` | `int` | Selected HDU index from dropdown |
| `CubeDepthAxis` | `int` | Selected Z-Axis from dropdown |

## Derived Runtime State (DO NOT PERSIST)
The following elements must never be serialized to the save file:
- Progress bars (`Slider` values)
- Runtime loading UI / Text labels
- Temporary loading indicators / Spinners
- Coroutine state or execution flags (`FileChanged` flags in transit)
- Runtime UI references (`GameObject` bindings)

## Recovery Rules
- **Missing Dataset:** Preserve the UI layout, mark dataset unavailable, allow the user to relink the file via the GUI.
- **Invalid Axis Selection:** Restore default axis, clamp invalid axis indices to valid ranges.

## Important Constraints
- GUI persistence must remain lightweight.
- GUI state should strictly derive from domain state whenever possible (MVVM pattern), rather than maintaining its own disconnected source of truth.
5. feature-state-contract.md
Markdown
# Feature System State Contract

## Owned By
Feature Sub-team

## Purpose
Defines the persistence of astronomical feature catalogs, masked regions, user-created features, and their associated geometry and spectral profiles. Strictly delineates persisted data from recomputed data.

## Persistent State

### Feature Set Level
| Field | Persist | Applies To |
|---|---|---|
| `FeatureSetType` | Yes | All |
| `SetName` | Yes | All |
| `FeatureColor` | Yes | All |
| `FeatureVisibility` | Yes | All |
| `FileName` | Yes | Imported Only |
| `ColumnMapping` | Yes | Imported Only |
| `ColumnsMask` | Yes | Imported Only |
| `ExcludeExternal` | Yes | Imported Only |

### Individual Feature Level
| Field | Persist | Applies To |
|---|---|---|
| `Id`, `Name`, `Flag`, `Comment`| Yes | All |
| `Visible`, `CubeColor` | Yes | All |
| `CornerMin` / `CornerMax` | Yes | Imported / New |
| `RawData` | Yes | New Only |

## Recomputed State (DO NOT PERSIST)
To maintain determinism and prevent stale data, the following must be recalculated upon load:
* **Mask bounding boxes:** Derived dynamically from DataAnalysis voxel masks.
* **Mask statistics (Peak, VSys, W20):** Derived dynamically via DataAnalysis.
* **Imported RawData:** Rebuilt by re-reading the source catalog file via the saved `FileName` and `ColumnMapping`.

## Recovery Rules
- **Missing Imported Catalog:** Preserve feature metadata, mark imported data unresolved, allow relinking without crashing the load sequence.
- **Invalid Feature Statistics:** Recompute from cube/mask data; preserve the feature's core identity.
- **Missing Feature Geometry:** Exclude the invalid feature safely while preserving the rest of the valid feature set.

## Important Constraints
- Features must remain pure domain-level objects.
- Unity scene references must **never** persist.
- Runtime selection (`Selected`) or transient state (`Temporary`) must **not** persist.
- Feature statistics must remain 100% reproducible through deterministic recomputation.