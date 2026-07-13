# Cycle-Dependency Report — Sub-team 6 Desktop Slice (§4.2 #2)

**Date:** 2026-05-28
**Author:** Sub-team 6 (Team Alpha)
**Constraint under test:** §4.2 non-negotiable #2 — *zero circular dependencies between top-level components.*
**Scope:** the 8-class desktop slice (before-state) and its MVVM after-state skeleton.

This report addresses pitch **Gap #1** and **QA QH.8** ("cycle-detection evidence is our
single weakest argument"). It separates what is **tool-backed today** from what is still
**owed**. No numbers are quoted without a tool behind them.

---

## 1. Tool availability (verified on this machine, 2026-05-28)

| Tool | Status | How verified |
|---|---|---|
| .NET SDK | **8.0.421 present** | `dotnet --version` |
| NDepend (`NDepend.Console`) | **not installed** | `where NDepend.Console` → not found |
| DV8 (`dv8-console`) | **not installed** | `where dv8-console` → not found |

Consequence: a full DV8/NDepend **class-level** cycle scan of the before-state Unity slice
**cannot be run on this machine**. What *can* be done now is documented below; the rest is
owed (§5).

---

## 2. After-state: assembly-level acyclicity (TOOL-BACKED)

The MVVM after-state skeleton lives in `refactoring-examples/sub-team-6/` as ten
pure-C# (`net8.0`, zero `UnityEngine` references) projects. **All ten build clean** with
`dotnet build -c Debug`:

| Project | csproj | Build |
|---|---|---|
| Gateway contracts | `contracts-team1/GatewayContracts.csproj` | ✅ 0 errors |
| File-tab ViewModel | `file-tab/skeleton/FileTabSkeleton.csproj` | ✅ 0 errors |
| Debug-tab ViewModel | `debug-tab/skeleton/DebugTabSkeleton.csproj` | ✅ 0 errors |
| File-tab adapter | `file-tab/adapters/FileTabAdapters.csproj` | ✅ 0 errors |
| Debug-tab adapter | `debug-tab/adapters/DebugTabAdapters.csproj` | ✅ 0 errors |
| Gateway tests | `contracts-team1/tests/GatewayContractsTests.csproj` | ✅ 0 errors |
| File-tab tests | `file-tab/tests/FileTabTests.csproj` | ✅ 0 errors |
| Debug-tab tests | `debug-tab/tests/DebugTabTests.csproj` | ✅ 0 errors |
| File-tab adapter tests | `file-tab/adapters/tests/FileTabAdaptersTests.csproj` | ✅ 0 errors |
| Debug-tab adapter tests | `debug-tab/adapters/tests/DebugTabAdaptersTests.csproj` | ✅ 0 errors |

**Why a clean build is real cycle evidence.** MSBuild resolves the `<ProjectReference>`
graph as a directed acyclic graph and **refuses to build a cyclic project graph**, emitting
`error : A circular dependency involving the following projects…`. A clean restore + build
of all ten projects is therefore a positive, reproducible, tool-backed proof that the
after-state graph is **acyclic at assembly granularity**.

### 2.1 Assembly reference graph (extracted from the `.csproj` `<ProjectReference>` lists)

Production assemblies (the shipped graph):

```
GatewayContracts            (iDaVIE.Client.Gateway)        — references: none        [SINK]
FileTabSkeleton             (iDaVIE.Desktop.FileTab)       — references: none        [SINK]
DebugTabSkeleton            (iDaVIE.Desktop.DebugTab)      — references: none        [SINK]
FileTabAdapters    ──┬─────▶ FileTabSkeleton
                     └─────▶ GatewayContracts
DebugTabAdapters   ──┬─────▶ DebugTabSkeleton
                     └─────▶ GatewayContracts
```

(Views — `FileTabView.cs`, `DebugTabView.cs`, composition roots — are Unity-bound and are
**not** in this build; in the design they depend inward on the ViewModel skeleton, adding no
back-edge.)

Adjacency list (production):

| From | To |
|---|---|
| `FileTabAdapters` | `FileTabSkeleton`, `GatewayContracts` |
| `DebugTabAdapters` | `DebugTabSkeleton`, `GatewayContracts` |
| `GatewayContracts` | — |
| `FileTabSkeleton` | — |
| `DebugTabSkeleton` | — |

**There are no back-edges.** Every edge points from an adapter toward a ViewModel skeleton
or the shared gateway contract; the skeletons and contract reference nothing in-graph. This
is the Ports-and-Adapters shape the proposal targets: the **ViewModel** and the **Gateway
contract** are the stable sinks, **Adapters** depend on both, **Views** (Unity) depend only
on the ViewModel. Topologically sortable ⇒ acyclic. This is consistent with the
`View → ViewModel → Gateway, Contracts shared` layering claimed in the architecture doc.

### 2.2 Reproduce

```
cd refactoring-examples/sub-team-6
for p in contracts-team1/GatewayContracts.csproj \
         file-tab/skeleton/FileTabSkeleton.csproj \
         debug-tab/skeleton/DebugTabSkeleton.csproj \
         file-tab/adapters/FileTabAdapters.csproj \
         debug-tab/adapters/DebugTabAdapters.csproj; do
  dotnet build "$p" -c Debug --nologo -v q
done
```

---

## 3. Before-state: 2 cycles documented (MANUAL — tool confirmation owed)

The before-state slice is the eight production classes — **CanvassDesktop,
DesktopPaintController, PaintMenuController, VideoUiManager, HistogramMenuController,
HistogramHelper, SourceRow, TabsManager** — which live under `Assets/` and cannot be built
standalone (Unity `MonoBehaviour` dependencies). Two cycles are already documented by
**manual** source-level dependency extraction:

| # | Cycle | Source artifact |
|---|---|---|
| 1 | `CanvassDesktop ↔ DesktopPaintController` (bidirectional concrete fields) | [BNCH-4 §"Circular Dependencies"](T2-baseline-benchmark/BNCH-4.md) |
| 2 | `CanvassDesktop ↔ HistogramHelper ↔ HistogramMenuController` (3-node) | [BNCH-4 §"Circular Dependencies"](T2-baseline-benchmark/BNCH-4.md), [static-analysis-report.md §HistogramHelper](../../static-analysis-report.md) |

These are derived from a manually-triangulated 8×8 DSM (BNCH-4, against commit `7bade5c`)
and the static-analysis CK report. **BNCH-4 explicitly flags that it is manually derived and
conservative** — it does not capture the three `FindObjectOfType<>` runtime couplings, which
a reflection-aware tool (DV8/NDepend) would surface. So the before-state is best stated as
**"2 cycles identified manually; tool confirmation owed"**, not "suspected" — the evidence
exists, but it is not yet from a sanctioned metric tool.

---

## 4. Honest scope of the tool-backed claim

| Claim | Granularity | Evidence today |
|---|---|---|
| After-state graph is acyclic **between assemblies** | assembly / project | ✅ MSBuild clean build of 10 projects (§2) |
| After-state graph is acyclic **between classes within an assembly** | class | ❌ owed — needs NDepend/DV8/Understand (§5) |
| Before-state has cycles | class | ⚠️ manual DSM only (BNCH-4); tool confirmation owed (§5) |

MSBuild proves the *project* graph has no cycles; it says nothing about class-level cycles
*inside* a single assembly. For our after-state that residual risk is low (the skeletons are
small, interface-fronted, and split by concern) but it is **not measured**. We do not claim
class-level acyclicity until a tool reports it.

---

## 5. Owed (Gap #1 / QH.8)

| Item | Owner | Due | Output path |
|---|---|---|---|
| DV8/NDepend class-level cycle scan of the 8-class **before-state** slice (confirm the 2 manual cycles + the `FindObjectOfType` runtime couplings) | Quality Champion | Day 10 sprint review | append to this file (§3) |
| NDepend `WarnIf cycle exists` CQLinq rule run on the **after-state** skeleton (class-level confirmation of §2) | Quality Champion | Day 10 sprint review | append to this file (§4) |
| NDepend cycle rule wired into CI as a merge gate | Quality Guild | Day 10 | `.github/workflows/ci.yml` |

Until the Day-10 scan lands, the defensible position is:
- **After-state, assembly level:** PASS (tool-backed, §2).
- **After-state, class level:** projected pass, not yet measured.
- **Before-state:** 2 cycles identified by manual DSM; tool confirmation owed.

---

## 6. Method

1. Probed for NDepend/DV8 (`where`), recorded `dotnet --version` (§1).
2. Built all ten `refactoring-examples/sub-team-6` projects with `dotnet build -c Debug` (§2).
3. Extracted the assembly reference graph from the `<ProjectReference>` entries of each
   `.csproj` and checked it for back-edges by topological inspection (§2.1).
4. Cited the existing manual before-state DSM (BNCH-4) and static-analysis report rather than
   re-deriving or inventing numbers (§3).

---

*Prepared by Sub-team 6 (Team Alpha) · addresses pitch Gap #1 / QA QH.8 · iDaVIE Refactoring Assessment 2026*
