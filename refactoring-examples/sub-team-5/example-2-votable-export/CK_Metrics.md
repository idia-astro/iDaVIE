# VoTable Export — CK Metrics (Chidamber & Kemerer Suite)

CK metrics for the VoTable Export refactoring example, captured before and
after the refactor.

## How these numbers were produced

The metrics come from **SciTools Understand**, a static analysis tool we ran
over the C# sources. Understand parses the project and emits a per-class
metrics table, which we exported to CSV (one before snapshot, one after). We
then read the relevant rows out of those CSVs and mapped Understand's column
names onto the CK suite, since Understand does not label its columns with the
CK names directly. The mapping we used:

| CK metric | Understand source | Notes |
|---|---|---|
| WMC | `SumCyclomatic` | Sum of method cyclomatic complexity. Swap in `CountDeclMethod` if WMC is meant as a plain method count. |
| DIT | `MaxInheritanceTree` | |
| NOC | `CountClassDerived` | |
| CBO | `CountClassCoupled` | |
| LCOM | `PercentLackOfCohesionModified` ÷ 100 | Henderson–Sellers variant, expressed as a fraction. |
| RFC | not exported | Estimated from declared methods plus fan-out calls — see caveat below. |

Understand's CSV export has no native RFC column, so the RFC figures here are
estimates rather than measured values. The method counts are small enough in
every class that RFC stays well under 50 regardless; if RFC needs to be
audited, Understand can be re-run with that metric explicitly enabled.

## Acceptance gates

| CK metric | Meaning | Acceptable range |
|---|---|---|
| WMC | Weighted Methods per Class | ≤ 20 domain; ≤ 40 adapters |
| DIT | Depth of Inheritance Tree | ≤ 4 |
| NOC | Number of Children | ≤ 5 |
| CBO | Coupling Between Object classes | ≤ 14 domain; ≤ 25 orchestrators; cycles forbidden |
| RFC | Response For a Class | ≤ 50 |
| LCOM | Lack of Cohesion in Methods (Henderson–Sellers) | ≤ 0.5 |

---

## Before — single class `VoTableReader.VoTableSaver`

| Metric | Value | Range | Within range |
|---|---|---|---|
| WMC | 7 | ≤20 domain | Yes |
| DIT | 1 | ≤4 | Yes |
| NOC | 0 | ≤5 | Yes |
| CBO | 6 | ≤14 domain | Yes |
| RFC | ~1 decl method (+calls) | ≤50 | Yes |
| LCOM | 0.0 | ≤0.5 | Yes |

Every CK metric passes here, but the smell is one CK does not capture: a single
90-line static method (`SaveFeatureSetAsVoTable`, cyclomatic 7) doing document
building, headers, rows, and I/O at once. The numbers look clean only because
there is one method.

## After — split into domain + infrastructure with ports

| Class | Layer | WMC | DIT | NOC | CBO | RFC≈ | LCOM |
|---|---|---|---|---|---|---|---|
| `FeatureCatalog` | Domain | 4 | 1 | 0 | 3 | ~2 | 0.0 |
| `VoTableExportService` | Infra (Persistence) | 14 | 1 | 0 | 8 | ~4 | 0.0 |
| `IVoTableExporter` | Domain (iface) | 0 | 0 | 1 | 2 | – | – |
| `ICoordinateTransformer` | Domain (iface) | 0 | 0 | 0 | 1 | – | – |
| `IAstFrame` | Domain (iface) | 0 | 0 | 0 | 0 | – | – |

## Before vs. after

Because the original class already passed every gate, the CK numbers barely
move — every class stays within range before and after, with ideal DIT, NOC,
and LCOM throughout. The real change is structural: the monolithic method is
broken into `BuildDocument`, `BuildHeaders`, `BuildRow`, and `Export`, and the
export work moves behind the `IVoTableExporter` port so the domain
`FeatureCatalog` depends only on the abstraction. CBO is spread across a few
small collaborators rather than one method, and no inheritance cycles are
introduced.
