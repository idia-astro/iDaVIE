# D4 — Worked Refactoring Examples

Two worked examples showing before/after for the Desktop GUI refactoring proposal. Each example includes comprehensive before/after documentation with UML class diagrams, dependency graphs, CK metrics, sequence diagrams, and skeleton implementations.

All source lives under [`refactoring-examples/sub-team-6/`](../../../../refactoring-examples/sub-team-6/).

---

## Example 1 — File Tab

**Objective:** Refactoring the file-open flow from a direct native-plugin call to a ViewModel command via the service gateway. This is the worked example called for verbatim by brief §6.6 (*"File tab from direct native-plugin call to ViewModel command via service gateway"*) and by ADR-009.

**Problem:** `CanvassDesktop` (1,899 lines) is a monolithic God class that directly couples UI logic to native DLL calls via `FitsReader`, scene-graph mutations via `VolumeDataSetRenderer`, and global object lookups via `FindObjectOfType`. No testability, no separation of concerns, no dependency injection. Brief §6.6 names this directly: *"direct file I/O that belongs server-side."*

**Solution:** Split into focused classes with the MVVM + Anti-Corruption-Layer pattern, and route FITS reads through the **service gateway** (ADR-0002 transport contract):
- **ViewModel** (pure C#): `FileTabViewModel`, `SubsetBoundsViewModel`, and the four domain interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`). The ViewModel sees no `IServiceGateway` and no Unity types.
- **Gateway proxy** (pure C#): `FitsServiceAdapter` implements `IFitsService` by dispatching `file.open`, `dataset.getAxes`, `dataset.getHeader`, and `file.close` through `IServiceGateway`. The native FITS plug-in (`FitsReader`) now lives **server-side** per brief §6.6; on the client, the dataset is referenced through an opaque `IFitsHandle` wrapping a server-assigned `datasetId`.
- **Unity adapters**: `FileTabView`, `FileDialogServiceAdapter`, `VolumeServiceAdapter`, `MemoryProbeAdapter`, `FileTabCompositionRoot` — the parts that genuinely need Unity (UI Toolkit, OS file picker, volume renderer, `SystemInfo`).
- **Transport** (pure C#): `JsonRpcServiceGateway` speaks JSON-RPC 2.0 over a named pipe per ADR-0002.

**Key Metrics (BEFORE → AFTER):**

| Metric | Before (CanvassDesktop) | After |
|--------|--------|-------|
| LOC | 1899 | ~1438 across 8 classes |
| WMC | 63 | 27 (FileTabViewModel) |
| CBO | 47 | 9 (FileTabViewModel) |
| RFC | 118 | ~50 (FileTabViewModel) |
| LCOM-HS | 0.955 | ≈0.20 (FileTabViewModel) |

> Before-state figures are `CanvassDesktop`'s measured Day-2 baseline (`SK_BNCH.md`; WMC 63 / CBO 47 / RFC 118 / LCOM-HS 0.955 — *not* `DesktopPaintController`'s 57). After-state figures are hand-counted Day 6 (`metrics.md §2.2`). `FileTabViewModel` WMC 27 is borderline over the ≤ 20 domain threshold; documented remediation: extract a `FileTabCommands` helper → WMC ~22.

### Documentation

- [**Before sequence**](../../../../refactoring-examples/sub-team-6/file-tab/before-sequence.md) — Mermaid sequence diagram showing the tangled flow, direct DLL coupling, coroutine reentry, busy-wait loops
- [**After sequence**](../../../../refactoring-examples/sub-team-6/file-tab/after-sequence.md) — Mermaid sequence diagram showing clean MVVM separation with ACL boundary
- [**Class diagram**](../../../../refactoring-examples/sub-team-6/file-tab/class-diagram.md) — Mermaid before/after UML showing god-class decomposition
- [**Dependency graph**](../../../../refactoring-examples/sub-team-6/file-tab/dependency-graph.md) — Module-level DSM proving Section 4.2 compliance (Domain ↛ UnityEngine)
- [**CK metrics**](../../../../refactoring-examples/sub-team-6/file-tab/ck-metrics.md) — Hand-counted deltas (WMC, DIT, NOC, CBO, RFC, LCOM) with thresholds
- [**FileTabViewModel.cs**](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/FileTabViewModel.cs) — ~480-line domain class, fully testable
- [**FileTabViewModelTests.cs**](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) — 47 NUnit tests, zero Unity dependency

### Smells Addressed

| Smell | Before | After |
|-------|--------|-------|
| Direct native-plugin calls from UI | `CD → FR → DLL` arrows in the client | `VM → Fits → Gateway → Server` — FITS reads run server-side, client speaks JSON-RPC |
| Native `IntPtr` lifetime on the client | Manual close-on-failure, reopen-per-HDU defect | Opaque `IFitsHandle` wraps a server-assigned `datasetId`; `Dispose()` fires best-effort `file.close` |
| `ChangeHduSelection` reopens FITS file per dropdown change | `FitsOpenFile` on every switch | Single durable dataset; `dataset.getHeader(datasetId, hduIndex)` server call |
| No interface layer | Direct dependencies on concrete types | All public boundaries are interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IServiceGateway`); each has ≥ 1 test double |
| Busy-wait coroutine loop | `while (!started) yield WaitForSeconds` | Event-driven `yield return StartCoroutine(_startFunc())` |
| Direct field writes on `VolumeDataSetRenderer` | Scattered across `CanvassDesktop` | Contained inside `VolumeServiceAdapter`; eliminated at VM level |
| Untestable without Unity | `CanvassDesktop` cannot be instantiated outside scene | 47 VM tests + 4 gateway-routed adapter tests + 11 framing / gateway-double tests, all `dotnet test`, zero `UnityEngine` |

### Compliance

- **Section 4.2 (Domain independence):** ✅ ViewModel assembly + the `FitsServiceAdapter` gateway proxy build with zero `UnityEngine` references (`dotnet build` on `FileTabSkeleton.csproj` and `FileTabAdapters.csproj` both succeed in isolation).
- **Section 4.2 (Interface + test double on every public boundary):** ✅ Five domain interfaces, every one has a Moq stub or `FakeGateway` programmation in the committed suites.
- **Unit testability (NFR-TST-1):** 0 → **47** VM tests + **4** gateway-routed adapter tests (`FitsServiceAdapterTests`); under ~80 ms total.
- **Transport contract has a real consumer (audit close, F9):** the four adapter tests assert wire-shape — method names, params, error codes, handle disposal — directly against ADR-0002 §"Method catalogue (v1)" and §"Error model".
- **Brief §6.6 compliance:** the worked example demonstrates *"File tab from direct native-plugin call to ViewModel command via service gateway"* verbatim.
- **Decomposition:** 1 God class → focused classes, single responsibility each.

---

## Example 2 — Debug Tab

**Objective:** Refactoring the debug console from inline `Debug.Log*` calls to an Observer of a **server-pushed structured logging stream** (brief §6.6 verbatim: *"Debug tab as Observer of a structured logging stream"*). The transport is the same `IServiceGateway` used by Example 1 — Debug consumes server-pushed notifications (`log.emit`) rather than client-issued requests.

**Problem:** `DebugLogging` (255 lines) subscribes to the process-global static event `Application.logMessageReceived` with zero abstraction, no source/timestamp capture, unbounded non-generic Queue, per-message file I/O, O(N) full-queue rebuild on every log line, and wholesale TMP text replacement. **Untestable without Unity.** 40+ `Debug.Log*` call sites are scattered with no central coordination.

**Solution:** Introduce Observer pattern with `ILogStream` / `ILogObserver` interfaces, and route the log producer through the gateway (ADR-0002 §"Method catalogue (v1)" — `log.emit` notification):
- **ViewModel layer** (pure C#): `LogStream` (thread-safe in-process dispatcher used by tests and any client-local emitter), `DebugTabViewModel` (Observer), `LogEntry` record (DTO).
- **Gateway proxy** (pure C#): `GatewayLogStreamAdapter` implements `ILogStream` by subscribing to `IServiceGateway.OnNotification`, filtering for `log.emit`, deserialising the `{ level, msg, ts }` payload, and republishing through an inner `LogStream`.
- **Unity-side wiring**: `DebugTabView` (thin view) and `DebugTabCompositionRoot` (constructs the gateway-backed adapter at scene start; disposes it on `OnDestroy`).

**Key Metrics (BEFORE → AFTER):**

| Metric | Before | After |
|--------|--------|-------|
| LOC | 255 | 382 total across 7 types |
| WMC | 8 | 6 (DebugTabViewModel) / 8 (UnityLogStreamAdapter) |
| CBO | ~10 | 3–7 per class (VM: 3, View: 7) |
| LCOM hs | ≈0.95 (incoherent) | ≈0 per class (cohesive) |
| Test surface | 0 | 29 NUnit tests |

### Documentation

- [**Before trace**](../../../../refactoring-examples/sub-team-6/debug-tab/before-trace.md) — Code-anchored walkthrough of `DebugLogging.cs` with 9 smells identified (S1–S9)
- [**After sequence**](../../../../refactoring-examples/sub-team-6/debug-tab/after-sequence.md) — Mermaid sequence diagram showing non-invasive refactor (44 `Debug.Log*` callers unchanged)
- [**Class diagram**](../../../../refactoring-examples/sub-team-6/debug-tab/class-diagram.md) — Mermaid before/after UML with Observer pattern
- [**Dependency graph**](../../../../refactoring-examples/sub-team-6/debug-tab/dependency-graph.md) — Module-level DSM proving Section 4.2 compliance + producer-side non-invasiveness
- [**CK metrics**](../../../../refactoring-examples/sub-team-6/debug-tab/ck-metrics.md) — Hand-counted deltas with testability analysis
- [**DebugTabViewModel.cs**](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/DebugTabViewModel.cs) — ~77-line domain class
- [**DebugTabTests.cs**](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) — 29 NUnit tests, zero Unity dependency

### Smells Addressed

| Smell | Before | After |
|-------|--------|-------|
| Static untestable event hook | `Application.logMessageReceived` consumed directly across the codebase | `ILogStream` interface; `GatewayLogStreamAdapter` is the only class that talks to the transport |
| Client-side coupling to the log producer | 40+ `Debug.Log*` callers on the client | Server-side producer issues `log.emit` notifications; client subscribes via the gateway |
| Unstructured log tuple | `(string, string, LogType)` — no source/timestamp | `LogEntry(Level, Message, Timestamp)` DTO; ADR-0002 wire shape preserves server-emitted `ts` |
| Unbounded memory growth | Non-generic `Queue`, no cap | Generic `List<LogEntry>` capped at 2000 |
| Per-message file I/O | `StreamWriter` new/write/close per log | Removed from hot path; optional autosave is a separate `ILogObserver` |
| O(N) full rebuild | Entire queue rebuilt every message | Capped at 500-line display slice |
| Forced scroll | Scroll to bottom on every message | Contained (remains S7, marked for future fix) |
| Four responsibilities in one class | Capture, store, display, export | Five types, single responsibility each |

### Compliance

- **Section 4.2 (Domain independence):** ✅ `DebugTabSkeleton.csproj` and `DebugTabAdapters.csproj` build with zero `UnityEngine` references — `GatewayLogStreamAdapter` is pure C#.
- **Unit testability (NFR-TST-1):** 0 → **29** VM tests + **4** gateway-routed adapter tests (`GatewayLogStreamAdapterTests`).
- **Transport contract has a real consumer (audit close, F10):** adapter tests assert that `log.emit` notifications on the gateway materialise as `LogEntry` records with the level/message/timestamp shape mandated by ADR-0002 §"Message shape".
- **Observer pattern:** ✅ New observers (autosave, telemetry, filtering) can attach to `ILogStream` without touching the gateway or the producer.
- **Brief §6.6 compliance:** the worked example demonstrates *"Debug tab as Observer of a structured logging stream"* verbatim.

---

## Metrics Summary

CK metric deltas for both examples side-by-side: [**metrics.md**](metrics.md)

### Key Findings

1. **Testability Driven:** Both refactors are primarily testability plays, not metric-driven. BEFORE already passes 5/6 CK thresholds individually; the case is structural.
2. **Domain Extraction:** Both achieve clean domain/adapter split with ACL boundaries enforced by assembly-level dependency rules.
3. **Zero Unit Tests → Full Coverage:** Example 1 goes 0 → 47 tests; Example 2 goes 0 → 29 tests. Both run without Unity.
4. **Production Unchanged:** Example 2's producer side (all `Debug.Log*` call sites) remains untouched — non-invasive refactor.

---

## Implementation Status

| Item | Status |
|------|--------|
| BEFORE diagrams (Mermaid) — class diagrams and sequence/trace docs | ✅ |
| AFTER diagrams (Mermaid) — class diagrams and sequence diagrams | ✅ |
| Dependency graphs — text-based DSM with Section 4.2 compliance | ✅ |
| CK metrics — hand-counted (pending Day 13 tool verification) | ✅ |
| Skeleton code — `FileTabViewModel`, `DebugTabViewModel`, adapters, composition roots | ✅ |
| Unit tests — 47 file-tab tests, 29 debug-tab tests | ✅ |
| Tool verification — NDepend, Understand, DV8, CodeScene (Day 13) | ⏳ |
