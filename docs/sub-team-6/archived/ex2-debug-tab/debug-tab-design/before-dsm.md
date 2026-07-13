# WE2 — Debug Tab: BEFORE Design Structure Matrix (DSM) Excerpt

**Source:** `Assets/Scripts/Debuggers/DebugLogging.cs` as-is (255 lines, branch `team6`).
**Participants:** the five most coupled components in the Debug tab flow.

An **X** in cell (row, col) means the row component depends on the column component.

| | 1. DebugLogging | 2. Application | 3. StreamWriter | 4. TMP_InputField | 5. UnityEngine |
|---|:---:|:---:|:---:|:---:|:---:|
| **1. DebugLogging** | — | X | X | X | X |
| **2. Application** | | — | | | |
| **3. StreamWriter** | | | — | | |
| **4. TMP_InputField** | | | | — | |
| **5. UnityEngine** | | | | | — |

## Interpretation

- Row 1 has **four X marks** — `DebugLogging` is the sole fan-out node; no other participant in this slice depends on it.
- The matrix is **triangular** (no cycles visible here), but `DebugLogging`'s real CBO from hand count is **~10–12 non-BCL types** (see [`../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md) §3 — full fan-out is `Application`, `UnityEngine.Debug`, `SystemInfo`, `PlayerPrefs`, `Canvas`, `LogType`, `TMP_InputField`, `Scrollbar`, `Button`, `Config`, `StandaloneFileBrowser`, `ExtensionFilter` plus 8 BCL types). The full DSM across all participants would show further X marks in row 1.
- **The runtime back-edge is invisible to static analysis.** `Application` does *not* compile-depend on `DebugLogging`, but at runtime `Application.logMessageReceived` dispatches *into* `DebugLogging.HandleLog`. This implicit dependency — Smell S1 in [`../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md`](../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md) — is the canonical untestable hook and does **not** appear in the static type graph. NDepend and DV8 will undercount this unless the static-event subscription is annotated.
- **44 unstructured `Debug.Log*` call sites** (catalogued in [`../../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md`](../../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md)) all funnel through `UnityEngine.Debug → Application` and arrive at `DebugLogging` via the same static event. From the DSM's perspective these are all collapsed into the single Row-1 → Col-2 X.

## Propagation cost (qualitative)

Any change to `Application.logMessageReceived`, `StreamWriter`, the `TMP_InputField` API, or Unity platform types must be handled **inside `DebugLogging`** — there is no interface buffer. The after-state design introduces `ILogStream` and `ILogObserver` to break the static-event coupling and `IDebugTabViewModel` to break the direct UI coupling (see [`after-dependency-graph.puml`](after-dependency-graph.puml)).

| Change scenario | BEFORE blast radius |
|---|---|
| Swap `Application.logMessageReceived` for a different log source | Rewrite `OnEnable/OnDisable` + `HandleLog` inside `DebugLogging` |
| Replace `StreamWriter` per-message with buffered I/O | Edit `AutoSave` directly; risk regressing the exception path |
| Replace `TMP_InputField` with UI Toolkit `ListView` | Edit `HandleLog` directly; the display contract is implicit |
| Add a second log consumer (autosave to network, telemetry) | Subscribe to the **same** static event — fan-out at the Unity event, not at an interface |

## Cohesion signal (LCOM)

The DSM's fan-out only tells half the story. The other half is **within** `DebugLogging`:

- **LCOM_HS = 0.95** (threshold ≤ 0.50) — ❌
- Four disjoint method-field clusters: lifecycle init, Unity event hook, hardware report, log display + autosave.
- `OnEnable`, `OnDisable`, and `DetermineHardware` share **zero fields** with any other method (see [`../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md`](../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md) §5.3).

The matrix Row 1 fan-out plus LCOM_HS failure together are the quantitative signal for Smell S8 (four concerns in one class).

## Traceability

| Item | Reference |
|---|---|
| Component diagram (same slice) | [before-dependency-graph.puml](before-dependency-graph.puml) |
| Class diagram (same slice) | [before-class-diagram.puml](before-class-diagram.puml) |
| Sequence diagram (same slice) | [before-sequence.puml](before-sequence.puml) |
| Before-state narrative (line-cited) | [../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md](../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md) |
| 44-call-site catalogue | [../../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md](../../../../refactoring-examples/sub-team-6/debug-tab/log-origin-trace.md) |
| CK baseline numbers | [../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md](../../../../refactoring-examples/sub-team-6/debug-tab/before-metrics.md) — `DebugLogging`: WMC 21, CBO 12 (non-BCL), RFC 46, LCOM_HS 0.95 |
| Feeds | T4 modularity chapter, D11 UML diagram set |
| Deliverables checklist item | Section 1.2, "Dependency graph — transitive Unity/native dependencies visible" |
