# Moment Maps — CK Metrics (Chidamber & Kemerer Suite)

CK metrics for the Moment Maps refactoring example, captured before and after
the refactor.

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

## Before — single class `VolumeData.MomentMapRenderer` (god class / MonoBehaviour)

| Metric | Value | Range | Within range |
|---|---|---|---|
| WMC | 27 | ≤20 domain / ≤40 adapter | Yes as adapter (no if judged domain) |
| DIT | 2 | ≤4 | Yes |
| NOC | 0 | ≤5 | Yes |
| CBO | 17 | ≤14 domain / ≤25 orch | Yes as orchestrator (no if domain) |
| RFC | ~22 decl methods (+calls) | ≤50 | Yes |
| LCOM | 0.48 | ≤0.5 | Yes (borderline) |

This one class mixes pure calculation, Unity rendering, and UI plotting. It
passes most gates only because it reads as an adapter, and LCOM is right on the
edge.

## After — split into domain + application + infrastructure

| Class | Layer | WMC | DIT | NOC | CBO | RFC≈ | LCOM |
|---|---|---|---|---|---|---|---|
| `MomentMapCalculator` | Domain (static) | 22 | 1 | 0 | 2 | ~3 | 0.0 |
| `MomentMapService` | Application | 4 | 1 | 0 | 5 | ~2 | 0.0 |
| `MomentMapRequest` | Domain DTO | 6 | 1 | 0 | 2 | ~9 | 0.0 |
| `MomentMapResult` | Domain DTO | 5 | 1 | 0 | 2 | ~9 | 0.0 |
| `MomentMapRendererAdapter` | Infra (Unity) | 19 | 2 | 0 | 14 | ~8 | 0.75 |
| `IMomentMapAdapter` | Domain (iface) | 0 | 0 | 0 | 1 | – | – |
| `IMomentMapService` | App (iface) | 0 | 0 | 0 | 2 | – | – |

Two values to watch: `MomentMapCalculator` WMC = 22 is marginally over the ≤20
domain gate (the three moment-computation methods carry real cyclomatic
weight), and `MomentMapRendererAdapter` LCOM = 0.75 is over the ≤0.5 cohesion
gate — expected for a Unity adapter that holds several independently-used
UI/render fields.

## Before vs. after

The single 27-WMC, CBO-17 god class is replaced by a set of focused classes:
the calculation logic lands in a pure `MomentMapCalculator` with CBO 2 and zero
lack of cohesion, the request/result data is isolated in small DTOs, and the
Unity-specific work is pushed out to an adapter behind an interface. Coupling
and complexity are no longer concentrated in one place; the only costs are a
calculator that sits just above the domain WMC limit and an adapter whose
cohesion is low by nature, both of which are easier to reason about in
isolation than the original combined class.
