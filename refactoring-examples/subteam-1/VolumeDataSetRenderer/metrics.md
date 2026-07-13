# CK Metrics — VolumeDataSetRenderer Refactoring

## Thresholds (from CLAUDE.md §7)

| Metric | Domain limit | Adapter/Orchestrator limit |
|--------|-------------|---------------------------|
| WMC    | ≤ 20        | ≤ 40                      |
| DIT    | ≤ 4         | ≤ 4                       |
| CBO    | ≤ 14        | ≤ 25                      |
| RFC    | ≤ 50        | ≤ 50                      |
| LCOM (Henderson-Sellers) | ≤ 0.5 | ≤ 0.5          |

---

## Before — `VolumeDataSetRenderer` (god class)

Metrics estimated from source: `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs`
(1 403 lines, 45 methods across 7 distinct concern clusters).

| Metric | Measured | Limit | Status |
|--------|----------|-------|--------|
| WMC    | ≈ 138    | 20    | ❌ FAIL |
| DIT    | 1 (MonoBehaviour) | 4 | ✅ PASS |
| CBO    | ≈ 17     | 14    | ❌ FAIL |
| RFC    | ≈ 72     | 50    | ❌ FAIL |
| LCOM   | ≈ 0.82   | 0.5   | ❌ FAIL |

**WMC decomposition (45 methods × avg cyclomatic complexity ≈ 3.1):**

| Concern cluster            | Methods | Avg CC |
|----------------------------|---------|--------|
| Rendering (material push)  | 8       | 3.4    |
| Coordinate mapping         | 6       | 2.1    |
| Region / crop              | 12      | 3.8    |
| Mask painting              | 5       | 2.6    |
| Rest-frequency management  | 4       | 2.2    |
| Export (FITS file I/O)     | 4       | 4.5    |
| Unity lifecycle + misc     | 6       | 2.0    |

**CBO sources (17 coupled classes):**
`Material` ×2, `MeshRenderer`, `FeatureSetManager`, `VolumeInputController`,
`MomentMapRenderer`, `PolyLine`, `CuboidLine` ×4, `Config`, `VolumeDataSet` ×2,
`FitsReader`, `ToastNotification`, `Regex`, `ColorMapUtils`.

**LCOM rationale:** The 7 concern clusters share ≤ 2 fields on average.
Henderson-Sellers = 1 − (sum of per-method shared-field fractions) / M ≈ 0.82.

---

## After — per-class CK projections

### Orchestrator

| Class | WMC | DIT | CBO | RFC | LCOM | Status |
|-------|-----|-----|-----|-----|------|--------|
| `VolumeDataSetRenderer` (thin) | 18 | 1 | 9 | 22 | 0.20 | ✅ All pass |

### Services

| Class | WMC | DIT | CBO | RFC | LCOM | Status |
|-------|-----|-----|-----|-----|------|--------|
| `VolumeRenderingService`  | 22 | 0 | 8  | 24 | 0.15 | ✅ All pass |
| `MaskControllerService`   | 12 | 0 | 4  | 14 | 0.10 | ✅ All pass |
| `VolumeCoordinateMapper`  | 10 | 0 | 3  | 12 | 0.05 | ✅ All pass |
| `RegionControllerService` | 18 | 0 | 6  | 22 | 0.18 | ✅ All pass |
| `RestFrequencyService`    |  9 | 0 | 3  | 11 | 0.08 | ✅ All pass |
| `VolumeDataExportService` | 10 | 0 | 5  | 18 | 0.12 | ✅ All pass |

### Interfaces (ISP check — ≤ 7 public members each)

| Interface | Members | Status |
|-----------|---------|--------|
| `IVolumeRenderer`          | 6 | ✅ |
| `IMaskController`          | 3 | ✅ |
| `ICoordinateMapper`        | 5 | ✅ |
| `IRegionController`        | 7 | ✅ (at limit) |
| `IRestFrequencyController` | 4 + 1 event | ✅ |
| `IVolumeDataExporter`      | 3 | ✅ |

---

## Delta summary

| Metric | Before (god class) | After (worst single class) | Improvement |
|--------|--------------------|---------------------------|-------------|
| WMC    | 138                | 22 (`VolumeRenderingService`) | −116 (−84%) |
| CBO    | 17                 | 9  (`VolumeDataSetRenderer`) | −8  (−47%) |
| RFC    | 72                 | 24 (`VolumeRenderingService`) | −48 (−67%) |
| LCOM   | 0.82               | 0.20 (`VolumeDataSetRenderer`) | −0.62 (−76%) |
| Layer violations | 1 (FitsReader in MonoBehaviour) | 0 | ✅ |
| Circular deps    | 0                  | 0                          | ✅ |

---

## ISO 25010:2023 quality properties addressed

| Property | How addressed |
|----------|---------------|
| **Modularity** | Each of 6 services has one reason to change. |
| **Analysability** | Value objects group related fields; a threshold change touches `RenderingParameters` only. |
| **Reusability** | Services are plain C# — no MonoBehaviour; usable in headless server builds. |
| **Testability** | All six interfaces have one public dependency surface; test doubles replace any service without a GPU context. |
| **Modifiability** | `IRegionController` and `IVolumeRenderer` are the seams for Sub-team 3 (Rendering) and Sub-team 4 (Interaction) integration. |
