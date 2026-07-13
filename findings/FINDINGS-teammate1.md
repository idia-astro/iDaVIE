# Persistence Sub-team: State Contract Findings

**Author:** Teammate 1 (sub-team 14 — Persistence & Workspace State)
**Branch:** Team-7-Persistance
**Date:** 2026-05-27
**Scope:** data-io-state-contract, feature-state-contract, gui-state-contract

---

## Contract 1: Data I/O State Contract

Source file: `state-contracts/data-io-state-contract.md`

---

### State inventory (field name, type, required/optional)

#### FITS Dataset State

| Field Name | Type | Required/Optional |
|---|---|---|
| `FileName` | `string` | **Required** |
| `SelectedHdu` | `int` | **Required** (default `1`) |
| `subsetBounds` | `int[]` (6-element: `[xMin, xMax, yMin, yMax, zMin, zMax]`) | **Required** |
| `MaskFileName` | `string?` | Optional (null if no mask loaded) |
| `index2` | `int?` | Optional (4D cubes only; must be `2` or `3`) |
| `sliceDim` | `int?` | Optional (4D cubes only; valid slice index within 4th dimension) |

#### Spectral Transformation State

| Field Name | Type | Required/Optional |
|---|---|---|
| `PrimarySpectralSystem` | `string` | **Required** |
| `AlternativeSpectralTarget` | `string` | **Required** |
| `AlternativeSpectralUnit` | `string` | **Required** |
| `StandardOfRest` | `string` | **Required** |
| `TransformedSpectralValue` | `double` | **Required** |
| `FormattedAltCoord` | `string` | **DO NOT PERSIST** (GUI-only output; recalculate on load) |

#### Persistence DTO (as defined in contract)

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
```

**Gap identified:** `TransformedSpectralValue` (type `double`) is required per the contract but absent from the DTO definition. Must be added before implementation.

---

### Validation rules

| Field | Rule |
|---|---|
| `FileName` | Must be a valid non-empty string. No file-existence check at save time, but must exist at restore time. |
| `MaskFileName` | Must be a valid non-empty string when present; null is a valid absent value. |
| `SelectedHdu` | Must be `>= 1` (FITS HDUs are 1-indexed). |
| `subsetBounds` | 6-element array; must satisfy `xMin <= xMax`, `yMin <= yMax`, `zMin <= zMax`; all bounds must be strictly inside original cube dimensions (`trueBounds`). |
| `index2` | When present: must be exactly `2` or `3` (VolumeDataSet.cs line 302). |
| `sliceDim` | When present: must be a valid index within the file's 4th dimension. |
| `PrimarySpectralSystem` | Must match a supported AST system. Supported: `FREQ`, `VRAD`, `VRADIO`, `VOPT`, `VOPTICAL`, `VELO`, `VREL`, `WAVN`, `WAVENUM`, `WAVE`, `WAVELEN`, `AWAV`, `AIRWAVE`. Unsupported (must log warning, do not transform): `ENER`, `ENERGY`, `ZOPT`, `REDSHIFT`, `BETA`. |
| `AlternativeSpectralTarget` | Must match a supported AST system (same set as above). |
| `AlternativeSpectralUnit` | Must be a unit string compatible with the `AlternativeSpectralTarget` system (e.g., `Hz` for `FREQ`, `km/s` for `VRAD`). |
| `StandardOfRest` | Must be a valid AST rest-frame label. Fallback default is `"Heliocentric"`. |
| `TransformedSpectralValue` | Any finite `double`; must be revalidated against the restored AST frame after load. |

---

### Default fallbacks

| Field | Safe Default | Source |
|---|---|---|
| `FileName` | No default — dataset marked unavailable; user must relink | Recovery rule |
| `SelectedHdu` | `1` | DTO initializer (`= 1`) |
| `subsetBounds` | Reconstruct from FITS header dimensions; if reconstruction fails, clamp to largest valid bounding box | Recovery rule |
| `MaskFileName` | `null` (proceed without mask; log warning if file was previously present) | Optional field |
| `index2` | `2` (3D cube assumption) | Convention |
| `sliceDim` | `1` (first slice of 4th dimension) | Convention |
| `PrimarySpectralSystem` | Extract from FITS CTYPE3 header keyword via FitsReader; if unavailable, default to `FREQ` or `VRAD` based on file signature | Recovery rule |
| `AlternativeSpectralTarget` | Recompute via `GetAltSpecSystemWithUnit()` based on restored `PrimarySpectralSystem` | Derived |
| `AlternativeSpectralUnit` | Recompute via `GetAltSpecSystemWithUnit()`, paired with `AlternativeSpectralTarget` | Derived |
| `StandardOfRest` | `"Heliocentric"` — explicitly logged by VolumeDataSet.cs line 1691: "No standard of rest found... defaulting to Heliocentric" | Contract + source |
| `TransformedSpectralValue` | Recompute from `PrimarySpectralSystem` via AST transform after frames are rebuilt | Derived |

---

### Recovery behavior

| Scenario | Recovery Action |
|---|---|
| `FileName` missing or file not found | Mark dataset unavailable; preserve all other saved metadata; do not crash; allow user to relink path via UI |
| `SelectedHdu` missing or invalid | Retry with `SelectedHdu = 1`; if HDU 1 is invalid, mark dataset unavailable |
| `subsetBounds` missing | Reconstruct from FITS header; if file unavailable, mark bounds as unresolved |
| `subsetBounds` exceeds file dimensions | Clamp to largest valid bounding box using actual file header dimensions |
| `MaskFileName` present but file missing | Proceed with main file only; log warning; allow user to relink mask |
| Unsupported `PrimarySpectralSystem` (e.g. `REDSHIFT`, `ENER`) | Log warning; revert to raw primary system without transformation; do not apply alternative frame |
| Unsupported `AlternativeSpectralTarget` | Do not apply transformation; preserve raw coordinate in `PrimarySpectralSystem`; log conversion failure |
| `StandardOfRest` missing or unrecognised | Fallback to `"Heliocentric"` |
| AST frame cannot be rebuilt from FITS header | Log error; disable spectral transform functionality; preserve dataset access without spectral conversion |
| Partial save (file truncated mid-write) | Discard partial state; trigger full recovery path; do not attempt partial restore of spectral state without complete FITS dataset state |

**Hard constraint from contract:** The system must never crash from invalid persistence state, must never enter undefined runtime behavior, and must never leak corrupted references into runtime systems.

**Native memory constraint:** `IntPtr` handles (AstFrameSet, AstAltSpecSet, FitsData, FitsHeader) must never be serialized. These are runtime-only; rebuild from FITS header on every restore.

---

### Existing serialization

A `DataIoStateDto` C# class is defined in the contract (see State inventory above). No serializer, file format, or read/write methods are defined. The DTO is a contract skeleton only.

---

### Integration notes

The Data I/O state is the **prerequisite** for all other subsystems. The following restore order is mandatory:

```
1. Restore FileName, SelectedHdu, subsetBounds, MaskFileName
2. Open FITS file → read header → validate subsetBounds against trueBounds
3. Call FitsReader.FitsCreateHdrPtrForAst() → load FITS header into AST
4. Call AstTool.InitAstFrameSet() → initialize primary spectral frame
5. Restore PrimarySpectralSystem, StandardOfRest
6. Restore AlternativeSpectralTarget, AlternativeSpectralUnit
7. Call CreateAltSpecFrame() → rebuild alternative AST frame
8. Restore TransformedSpectralValue → validate against rebuilt frame
```

Steps 3–8 depend on step 2 completing successfully. Steps 6–8 depend on step 5. No spectral state can be restored before the FITS file is open and the AST frame is initialized.

The contract explicitly requires **zero Unity dependencies** in this domain.

---

### Open questions

1. `TransformedSpectralValue` is absent from the published DTO — must this be added as a breaking schema change, or appended as a v2 field?
2. What is the maximum valid value for `sliceDim` — is this validated against a file-level count or a fixed constant?
3. Should `subsetBounds` be re-validated on every load (comparing against file dimensions), or only when `trueBounds` has changed?
4. For 4D files where `index2` or `sliceDim` are missing: is `index2 = 2` a safe assumption or should the load fail to unsafe territory?
5. Is lenient matching of equivalent spectral labels (`VRAD` vs `VRADIO`) formally specified anywhere in the AST integration, or is this an undocumented convention?
6. Is the DTO format intended to be JSON or binary? No serialization format is specified.

---

---

## Contract 2: Feature State Contract

Source file: `state-contracts/feature-state-contract.md.md`

---

### State inventory (field name, type, required/optional)

#### Feature Set Level

| Field Name | Type | Required/Optional | Applicability |
|---|---|---|---|
| `FeatureSetType` | unspecified | **Required** | All sets |
| `SetName` | `string` (implied) | **Required** | All sets |
| `FeatureColor` | color type (unspecified) | **Required** | All sets |
| `FeatureVisibility` | `bool` (implied) | **Required** | All sets |
| `FileName` | `string` | **Required** | Imported sets only |
| `ColumnMapping` | unspecified | **Required** | Imported sets only |
| `ColumnsMask` | unspecified | **Required** | Imported sets only |
| `ExcludeExternal` | `bool` (implied) | **Required** | Imported sets only |

#### Individual Feature Level

| Field Name | Type | Required/Optional | Applicability |
|---|---|---|---|
| `Id` | unspecified | **Required** | All features |
| `Name` | `string` (implied) | **Required** | All features |
| `Flag` | unspecified | **Required** | All features |
| `Comment` | `string` (implied) | **Required** | All features |
| `Visible` | `bool` (implied) | **Required** | All features |
| `CubeColor` | color type (unspecified) | **Required** | All features |
| `CornerMin` | numeric array/vector (unspecified) | **Required** | Imported and New features only |
| `CornerMax` | numeric array/vector (unspecified) | **Required** | Imported and New features only |
| `RawData` | unspecified | **Required** | New features only |

#### Fields explicitly excluded from persistence

| Field | Reason |
|---|---|
| `Selected` | Runtime selection state — transient |
| `Temporary` | Transient state — not domain-persistent |
| Mask bounding boxes | Derived dynamically from DataAnalysis voxel masks |
| Mask statistics (`Peak`, `VSys`, `W20`) | Derived dynamically via DataAnalysis — must be recomputed, not persisted |
| Imported `RawData` | Rebuilt by re-reading source catalog via `FileName` + `ColumnMapping` |
| Unity scene references | Must never persist — hard constraint |

---

### Validation rules

| Field | Rule |
|---|---|
| All feature fields | Must be pure domain-level objects; Unity scene references must never be present |
| `Id` | Must uniquely identify a feature within its set; exact format unspecified |
| `FileName` (imported) | Must be a valid string; file existence checked at restore time |
| `ColumnMapping` (imported) | Must align with the structure of the source catalog at `FileName` |
| `CornerMin` / `CornerMax` | Must form a valid bounding box; both must be present or the feature is excluded |
| `RawData` (new features) | Must be serializable; exact format unspecified |
| `FeatureSetType` | Must be one of the recognised feature set types (Masked, Imported, User-defined); exact enum values unspecified |

**Note:** The contract does not define concrete types, numeric ranges, or format constraints for most fields. This is a significant gap.

---

### Default fallbacks

The contract does not explicitly specify default values for any field. Inferred safe defaults from recovery rules:

| Field | Safe Default | Source |
|---|---|---|
| `FileName` (imported, missing) | Mark set as "unresolved"; allow user to relink | Recovery rule |
| `CornerMin` / `CornerMax` (missing) | Exclude the feature entirely; preserve remainder of set | Recovery rule |
| `RawData` (new feature, missing) | No recovery path defined; exclude feature | Inferred |
| Mask statistics (`Peak`, `VSys`, `W20`) | Recompute from cube/mask via DataAnalysis after load | Explicit contract rule |
| Mask bounding boxes | Recompute from DataAnalysis voxel mask after load | Explicit contract rule |
| Imported `RawData` | Rebuild by re-reading `FileName` with `ColumnMapping` after load | Explicit contract rule |
| `Visible` (missing) | No default specified — **must be resolved by persistence team** | Gap |
| `FeatureColor` / `CubeColor` (missing) | No default specified — **must be resolved by persistence team** | Gap |

---

### Recovery behavior

| Scenario | Recovery Action |
|---|---|
| `FileName` missing for imported feature set | Preserve all feature metadata (`Id`, `Name`, `Flag`, etc.); mark set as unresolved; allow relinking without crashing the load sequence |
| Invalid feature statistics | Recompute from cube/mask data via DataAnalysis; preserve feature core identity |
| `CornerMin` or `CornerMax` missing | Exclude the invalid feature safely; continue loading the rest of the feature set |
| `RawData` missing (new feature) | No recovery path defined in contract; exclusion is the safest behavior |
| Partial feature set (some features corrupt) | Load all valid features; silently exclude corrupt features; log exclusions |
| Unity scene reference found in saved state | Reject and discard the affected feature record; log error; do not leak reference into runtime |
| `Selected` or `Temporary` found in saved state | Discard those fields silently; do not restore them |

**Hard constraint from contract:** "Feature statistics must remain 100% reproducible through deterministic recomputation." This means the persistence layer must never cache statistics that could become stale.

---

### Existing serialization

None. The contract defines the conceptual boundary only. No serialization format, encoding, or read/write methods are specified. The persistence team must define the JSON schema or equivalent DTO classes for all feature-level and set-level fields before implementation.

---

### Integration notes

Restore ordering for feature state:

```
1. Restore FeatureSetType, SetName, FeatureColor, FeatureVisibility
2. For Imported sets: restore FileName, ColumnMapping, ColumnsMask, ExcludeExternal
3. Restore per-feature identity fields: Id, Name, Flag, Comment, Visible, CubeColor
4. For Imported/New features: restore CornerMin, CornerMax (validate pair integrity)
5. For New features: restore RawData
6. Trigger recomputation:
   a. Mask bounding boxes — recompute from DataAnalysis voxel masks
   b. Mask statistics (Peak, VSys, W20) — recompute from cube/mask via DataAnalysis
   c. Imported RawData — rebuild from FileName + ColumnMapping via FitsReader/catalog reader
```

Steps 6a–6b require DataAnalysis to be available and the cube/mask data to be loaded. This means **Data I/O state must be fully restored before feature recomputation is triggered.** Feature state has a hard runtime dependency on Data I/O state.

The contract requires features to be first-class domain aggregates with no Unity dependencies.

---

### Open questions

1. What are the concrete types for `Id`, `Flag`, `FeatureSetType`, `CubeColor`, `FeatureColor`, `CornerMin`, `CornerMax`, `ColumnMapping`, `ColumnsMask`, and `RawData`? The contract does not define them.
2. What is the exact enum for `FeatureSetType`? (Masked, Imported, User-defined are mentioned but not formally typed.)
3. What are the safe defaults for `Visible`, `FeatureColor`, and `CubeColor` when missing?
4. Should excluded (invalid) features be logged to a recovery report for user review, or silently dropped?
5. Is there a maximum feature count per set that bounds the array allocation during restore?
6. When `RawData` is missing for a New feature, should the feature be excluded or should the user be prompted to re-create it?
7. Is the recomputation of mask statistics expected to be synchronous on load or deferred asynchronously?
8. What is the authority for `CornerMin`/`CornerMax` coordinate system — cube voxel space, world space, or WCS? This affects cross-version migration.

---

---

## Contract 3: GUI State Contract

Source file: `state-contracts/gui-state-contract.md`

---

### State inventory (field name, type, required/optional)

#### Persistent Fields

| Field Name | Type | Required/Optional |
|---|---|---|
| `FileName` | `string` | **Required** (core dataset reference) |
| `SelectedHdu` | `int` | **Required** |
| `CubeDepthAxis` | `int` | **Required** |
| `subsetBounds` | `int[]` | Optional (user crop; may be absent if no crop applied) |
| `trueBounds` | `int[]` | **Required** (full cube dimensions from FITS header) |
| `MaskFileName` | `string?` | Optional (null if no mask loaded) |

#### Fields explicitly excluded from persistence

| Field | Reason |
|---|---|
| Progress bar / Slider values | Runtime loading UI state — transient |
| Runtime loading text labels | Transient |
| Temporary loading indicators / Spinners | Transient |
| Coroutine execution flags / `FileChanged` flags in transit | Runtime execution state — transient |
| Runtime UI references (`GameObject` bindings) | Unity scene references — must never persist |

**Architectural constraint:** "GUI state should strictly derive from domain state whenever possible (MVVM pattern), rather than maintaining its own disconnected source of truth." This means the GUI persistence layer must remain lightweight and defer to the domain/rendering layers for authoritative state.

---

### Validation rules

| Field | Rule |
|---|---|
| `FileName` | Must be a non-empty string; file existence validated at restore time |
| `SelectedHdu` | Must fall within valid HDU range for the file; clamp invalid indices to valid range (contract: "clamp invalid axis indices to valid ranges") |
| `CubeDepthAxis` | Must be a valid axis index for the selected HDU; clamp invalid values to valid range |
| `subsetBounds` | Implicit: must be within or equal to `trueBounds`; `min <= max` for each pair; exact format `[xMin, xMax, yMin, yMax, zMin, zMax]` (consistent with Data I/O contract) |
| `trueBounds` | Must have positive dimensions; should match the actual file dimensions on reload |
| `MaskFileName` | Must be a non-empty string when present; null is a valid absent value |

**Note:** The contract does not specify concrete validation rules for `subsetBounds` beyond the implied consistency with `trueBounds`. Validation against `trueBounds` must be cross-checked with the Data I/O contract, which is the authoritative source for these bounds.

---

### Default fallbacks

| Field | Safe Default | Source |
|---|---|---|
| `FileName` | No default — mark dataset unavailable; allow user to relink via GUI | Recovery rule |
| `SelectedHdu` | "Restore default axis" — exact value unspecified in contract; recommend `1` (consistent with Data I/O DTO default) | Recovery rule + inferred |
| `CubeDepthAxis` | "Restore default axis" — exact value unspecified; recommend `2` (z-axis, standard for FITS cubes) | Recovery rule + inferred |
| `subsetBounds` | `null` or copy of `trueBounds` (no cropping applied) | Inferred |
| `trueBounds` | Must be recomputed from FITS file header on reload — not a persisted fallback | Derived |
| `MaskFileName` | `null` — proceed without mask | Optional field |

---

### Recovery behavior

| Scenario | Recovery Action |
|---|---|
| `FileName` missing or file not found | Preserve the UI layout state; mark dataset unavailable; allow user to relink file path via GUI; do not crash |
| `SelectedHdu` invalid or out of range | Clamp to valid HDU range; restore default axis (`1` recommended) |
| `CubeDepthAxis` invalid or out of range | Clamp to valid axis range; restore default axis |
| `subsetBounds` corrupt or exceeding `trueBounds` | Reset `subsetBounds` to `trueBounds` (full cube, no crop) |
| `trueBounds` missing | Recompute from FITS file header; if file unavailable, mark bounds unresolved |
| `trueBounds` in saved state conflicts with actual file dimensions | Trust the file; use actual dimensions; log discrepancy; do not crash |
| `MaskFileName` present but file missing | Proceed without mask; log warning; allow user to relink mask |
| Unity `GameObject` reference found in saved state | Reject and discard; do not restore runtime references into scene |

**Architectural constraint:** The GUI recovery path must preserve layout/panel state even when the dataset is unavailable. The GUI should be displayable in a "disconnected" mode while the user relinks files.

---

### Existing serialization

None. The contract is definitional only. No serialization format, DTO class, or read/write methods are specified. The GUI contract has fewer fields than the Data I/O contract, and the contract itself notes that most GUI state should be derived from domain state rather than independently serialized.

---

### Integration notes

The GUI state contract overlaps directly with the Data I/O contract for the fields `FileName`, `MaskFileName`, `SelectedHdu`, `subsetBounds`, and `trueBounds`. These fields are duplicated in the GUI contract but the **Data I/O contract is the authoritative source.** The GUI layer must derive these values from the domain/data-io layer at restore time rather than loading them independently.

Restore ordering for GUI state:

```
1. Data I/O state must be fully restored first (FileName, SelectedHdu, subsetBounds, trueBounds)
2. Derive GUI bound display from restored domain state
3. Restore CubeDepthAxis (validated against dimensions from Data I/O layer)
4. Restore MaskFileName if present (optional; may be absent)
5. Reconstruct UI layout from restored domain state (MVVM binding)
```

GUI state is the **last** subsystem to restore in the workspace load sequence, after Data I/O, Rendering, and Feature state are all resolved.

---

### Open questions

1. What are the exact default values for `SelectedHdu` and `CubeDepthAxis`? Contract says "restore default axis" but does not define the value.
2. If `trueBounds` in saved state differs from actual file dimensions at reload (e.g., file was replaced or partially reprocessed), should this trigger a warning-only path or a full recovery flow?
3. Should `subsetBounds` be independently persisted in the GUI contract at all, given the Data I/O contract already persists it authoritatively? Duplicate persistence risks divergence.
4. What is the serialization format for GUI state? No format is specified.
5. Are save operations atomic? What is the rollback behavior if the GUI state write is interrupted mid-save?
6. Is the "disconnected mode" UI (layout preserved, dataset unavailable) a defined UI state in the GUI sub-team's contract, or does the persistence team need to define it?
7. Should `trueBounds` be persisted at all, or should it always be recomputed from the FITS header at load time? Persisting it risks stale values if the source file changes.

---

---

## Cross-contract Integration Notes

The three contracts have the following dependency ordering for restore operations:

```
Data I/O State
  ↓  (must be loaded first; provides FileName, trueBounds, AST frames)
Feature State
  ↓  (requires cube/mask data to be available for recomputation)
GUI State
     (last; derives from domain state; must not duplicate authoritative fields)
```

Shared fields that appear in multiple contracts:

| Field | Authoritative Contract | Also Referenced In |
|---|---|---|
| `FileName` | Data I/O | GUI |
| `MaskFileName` | Data I/O | GUI |
| `SelectedHdu` | Data I/O | GUI |
| `subsetBounds` | Data I/O | GUI |
| `trueBounds` | Data I/O (derived from FITS header) | GUI |

The persistence team must define a single serialized representation for these shared fields and ensure the GUI layer reads them from the domain layer rather than from an independent GUI save record.

No contract defines serialization format, compression strategy, schema versioning, or atomic write semantics. These are entirely open for the persistence team to specify and implement.
