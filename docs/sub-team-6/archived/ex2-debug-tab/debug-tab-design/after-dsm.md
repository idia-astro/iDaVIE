# WE2 — Debug Tab: AFTER Design Structure Matrix (DSM)

**Proposed design.** The single `DebugLogging` god-class is decomposed into seven types across two assemblies; the DSM below collapses them into the five most architecturally significant participants. Three ACL interfaces (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) are inserted as buffers between the domain and the Unity platform.

An **X** in cell (row, col) means the row component depends on the column component.

| | 1. DebugTabViewModel | 2. ILogStream | 3. ILogObserver | 4. IDebugTabViewModel | 5. UnityEngine |
|---|:---:|:---:|:---:|:---:|:---:|
| **1. DebugTabViewModel** | — | X | X | X | |
| **2. UnityLogStreamAdapter** | | X | | | X |
| **3. DebugTabView** | | | | X | X |
| **4. DebugTabCompositionRoot** | X | | | | X |
| **5. UnityEngine** | | | | | — |

Row 1's X marks on columns 3 and 4 are *implements* dependencies (`DebugTabViewModel : ILogObserver, IDebugTabViewModel`); the X on column 2 is a *uses* dependency (constructor-injected `ILogStream`).

## Change from BEFORE

| Metric | Before | After |
|---|---|---|
| Domain class fan-out to platform (Row 1 → UnityEngine) | Yes (`DebugLogging → UnityEngine` direct) | **No** (`DebugTabViewModel` has zero `UnityEngine` references — verified by `dotnet build` on `DebugTabSkeleton.csproj`) |
| Domain class fan-out to native / BCL I/O | Yes (`StreamWriter`, `TMP_InputField`) | **No** (all I/O isolated in adapters) |
| Static-event back-edge (Application → consumer) | One untestable subscriber (`DebugLogging.HandleLog`) | One **swappable** subscriber (`UnityLogStreamAdapter.OnUnityLog`) behind `ILogStream` |
| Interfaces on the consumer side | 0 | **3** (`IDebugTabViewModel`, `ILogStream`, `ILogObserver`) — each replaceable with a test double |
| Implicit runtime deps (static events, `FindObjectOfType`) | 1 (`Application.logMessageReceived` static event) | **0** — the same event is now subscribed to by a single adapter, and the consumer side talks to `ILogStream`, not Unity |

- All three interface X marks in Row 1 are now **interface** dependencies — any of them can be replaced with a test double in a unit-test runner (29 NUnit tests prove this — see [`../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs)).
- All `UnityEngine` dependencies are confined to adapter rows (2, 3, 4); the only domain-side row (1) has no `UnityEngine` column X.
- The matrix is **acyclic** — no circular dependencies in either state. Topological order: `LogEntry, LogLevel, ILogObserver → ILogStream, IDebugTabViewModel → LogStream, DebugTabViewModel → UnityLogStreamAdapter, DebugTabView → DebugTabCompositionRoot`.

## Propagation cost (qualitative)

| Change scenario | Before | After |
|---|---|---|
| `Application.logMessageReceived` API changes | Touches `DebugLogging` directly | Contained in `UnityLogStreamAdapter.OnUnityLog` |
| Swap `StreamWriter` per-message for buffered I/O | Edit `DebugLogging.AutoSave` directly | New `FileLogObserver : ILogObserver` (one ~30-line class; one composition-root line) |
| Replace `TMP_InputField` with Unity UI Toolkit `ListView` | Edit `HandleLog` directly; the display contract is implicit | Edit `DebugTabView` only; the ViewModel and dispatch path are unchanged |
| Add a second log consumer (telemetry / network) | Subscribe to the same `Application` static event (no test seam) | New `ILogObserver`; subscribe in `CompositionRoot` (one line) |
| Unit-test entry filtering / clear-all behaviour | Requires Unity test runner (`MonoBehaviour`) | Plain NUnit test, no Unity — already implemented (29 tests, ~20 ms) |
| Unit-test `MaxEntries` cap (S3 eliminated) | Not possible — no cap in BEFORE | `MaxEntries_ExceededTrimsOldest` test in `DebugTabTests.cs` |

## CK metric projection

| Class | WMC (before) | WMC (after) | CBO (before) | CBO (after) | LCOM (before) | LCOM (after) | Threshold band |
|---|---|---|---|---|---|---|---|
| `DebugLogging` (full class) | 21 | n/a — deleted | 12 (non-BCL) | n/a | **0.95** ❌ | n/a | orchestrator (WMC ≤ 40, CBO ≤ 25) |
| `DebugTabViewModel` | — | 6 | — | 3 | — | 1 | domain (WMC ≤ 20, CBO ≤ 14) |
| `LogStream` | — | 3 | — | 2 | — | 1 | domain |
| `UnityLogStreamAdapter` | — | 6 | — | 4 | — | 1 | adapter (WMC ≤ 40, CBO ≤ 25) |
| `DebugTabView` | — | 3 | — | 7 | — | 1 | adapter |
| `DebugTabCompositionRoot` | — | 2 | — | 3 | — | 1 | adapter |

Headline movement: LCOM hs goes from **0.95 (fail)** to **≈ 0 per class (the structural win)**. WMC and CBO improvements are modest because `DebugLogging` already passed those thresholds individually — the case for the refactor is **structural and testability-driven**, not metric-driven. CK baseline source: [`../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md) — `DebugLogging` as-is: WMC 21, CBO 12 (non-BCL), RFC 46, LCOM_HS 0.95.

## Section 4.2 compliance

| Rule | Before | After |
|---|---|---|
| No SOLID/GRASP violations without trade-off | SRP + DIP violated by `DebugLogging` | ✅ |
| **Zero circular dependencies** | Acyclic in static graph (but runtime back-edge via static event) | ✅ — acyclic in both static and runtime senses |
| **Domain code must not depend on `UnityEngine` / `SteamVR`** | ❌ — `DebugLogging` directly references `UnityEngine.Application`, `TMPro`, `UnityEngine.UI` | ✅ — `DebugTabSkeleton.csproj` compiles with **zero `UnityEngine` references** |
| Every public API boundary expressed as an interface with ≥1 test double | ❌ — no interfaces in BEFORE | ✅ — `IDebugTabViewModel`, `ILogStream`, `ILogObserver` all exercised by NUnit test doubles |

## Traceability

| Item | Reference |
|---|---|
| After class diagram | [after-class-diagram.puml](after-class-diagram.puml) |
| After dependency graph | [after-dependency-graph.puml](after-dependency-graph.puml) |
| Before DSM | [before-dsm.md](before-dsm.md) |
| Before class diagram | [before-class-diagram.puml](before-class-diagram.puml) |
| Before dependency graph | [before-dependency-graph.puml](before-dependency-graph.puml) |
| Before sequence | [before-sequence.puml](before-sequence.puml) |
| Skeleton code | [../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/) |
| Adapter code | [../../../../refactoring-examples/sub-team-6/debug-tab/adapters/](../../../../refactoring-examples/sub-team-6/debug-tab/adapters/) |
| Tests (29 passing, ~20 ms, no Unity) | [../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) |
| CK baseline + projection | [../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) |
| Before metrics (LCOM derivation) | [../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md](../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md) |
| After trace narrative | [../../../../refactoring-examples/sub-team-6/debug-tab/after-trace.md](../../../../refactoring-examples/sub-team-6/debug-tab/after-trace.md) |
| Deliverables checklist | [../../deliverables-checklist.md](../../deliverables-checklist.md) |
