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