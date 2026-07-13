# D5 — Branch and Line Coverage Report

**Generated:** 2026-05-27 (Day 8) · **Tool:** Coverlet 6.0.2 + ReportGenerator 5.5.10 · **Source:** merged Cobertura from all 5 test projects (95 tests; all green on a host without Smart App Control — the 19 Tier-2 tests are blocked at load on SAC-enforced Windows hosts, see [`test-strategy.md` §4.4](test-strategy.md))

**Reproduce locally:**

```pwsh
# Run every test project with coverage collection
foreach ($p in @(
    'file-tab/tests/FileTabTests',
    'file-tab/adapters/tests/FileTabAdaptersTests',
    'debug-tab/tests/DebugTabTests',
    'debug-tab/adapters/tests/DebugTabAdaptersTests',
    'contracts-team1/tests/GatewayContractsTests')) {
    dotnet test "refactoring-examples/sub-team-6/$p.csproj" `
        --collect:'XPlat Code Coverage' `
        --results-directory $env:TEMP\cov\$($p -replace '/','_')
}
# Merge into a single HTML / markdown report
dotnet tool install -g dotnet-reportgenerator-globaltool   # one-off
reportgenerator -reports:"$env:TEMP\cov\**\coverage.cobertura.xml" `
                -targetdir:"$env:TEMP\cov-report" `
                -reporttypes:"Html;MarkdownSummary" `
                -classfilters:"-*Tests"
```

---

## 1. Headline (audit F14 close)

After the F14 follow-up — 13 targeted branch-coverage tests added on 2026-05-27 (Day 8) to hit the gaps identified in §2.2 below:

| Assembly | Line | Branch | D5 §7 ≥ 70 % gate |
|---|---:|---:|---|
| **DebugTabSkeleton** (VM) | **100 %** | **100 %** | ✅ both met |
| **FileTabSkeleton** (VM) | **89.4 %** | **77.2 %** | ✅ **both met** (was 82.8 % / 67.4 %; branch +9.8 pp) |
| **iDaVIE.Client.Gateway** (Tier 2) | 41.2 % | 41.6 % | tracked, not gated |
| **Aggregate (all measured)** | **71.3 %** | **66.5 %** | — |

**Before / after on FileTabSkeleton:** 34 tests → 47 tests; line 82.8 → 89.4 %; branch 67.4 → 77.2 %. The 13 new tests target the lines coverlet flagged as 0 % covered (constructor null-guards, `Dispose` on an empty VM, setters before image load, two-axis non-trivial cube) and the 50 %-covered short-circuits in `MaskAxesMatchImage` (NAXIS1 and NAXIS2 mismatches that the existing single mismatch test did not hit).

**The honest answer to "what is your current coverage" is the table above.** The brief §7 ban on speculative numbers cuts both ways: we report what we measured.

## 2. Per-class drill-down

Sorted by line coverage within each assembly.

### 2.1 DebugTabSkeleton — clean

| Class | Line | Branch | Lines covered |
|---|---:|---:|---:|
| `DebugTabViewModel` | 100 % | 100 % | 24 / 24 |
| `LogStream` | 100 % | 100 % | 17 / 17 |
| `LogEntry` (record) | 100 % | — | 1 / 1 |

29 NUnit tests in `DebugTabTests.cs` exercise the entire VM + LogStream surface. No remediation needed.

### 2.2 FileTabSkeleton — gate cleared

State after the Day 8 follow-up (13 new tests):

| Class | Line | Branch | Notes |
|---|---:|---:|---|
| `FileTabViewModel` | **86.8 %** | **78.3 %** | Was 78.1 % / 68.7 %. The added tests covered constructor null-guards, the `_selectedHduIndex == value` and `_selectedZAxisIndex == value` no-op setters, `Dispose` on an empty VM, the `_currentImageInfo is null` short-circuits in `RefreshHduHeaderAsync` and `UpdateZAxisMax`, the `nonTrivialCount < 3` rule in `IsLoadable`, and the previously untested NAXIS1 / NAXIS2 short-circuits inside `MaskAxesMatchImage`. |
| `SubsetBoundsViewModel` | 92.6 % | 66.6 % | Unchanged from Day 6. 8 / 12 branches; clamp-on-update edge cases (NaN / inverted ranges) are still untested. Below the gate at the class level, but the assembly aggregate clears the gate. |
| `AsyncRelayCommand` | 100 % | 50 % | 3 / 6 branches. The remaining gap is in the concurrent-call guards. |
| `RelayCommand` | 87.5 % | 50 % | The `CanExecute = null` constructor path is unexercised. |
| `FitsFileInfo`, `HduInfo`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs` | 100 % | — | DTOs, no branches. |

The 13 new tests live in `refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs` under the comment header **"Branch-coverage gate close (audit F14 follow-up, 2026-05-27)"**. They run alongside the original 34 in `dotnet test` and add ~30 ms to the suite (47 tests total in 37 ms).

**Remaining gap to chase (optional, not gate-blocking):** the `SubsetBoundsViewModel` clamp edge cases (NaN, inverted range update) and the `AsyncRelayCommand` concurrent-call guards. Each would add ~2 lines of branch coverage but neither is gate-blocking now.

### 2.3 iDaVIE.Client.Gateway — Tier 2 (tracked, not gated)

| Class | Line | Branch | Notes |
|---|---:|---:|---|
| `JsonRpcNotification` | 100 % | 50 % | Simple DTO with a generic `Deserialize<T>` helper. |
| `JsonRpcException` | 85.7 % | — | Constructor-only class. |
| `FakeGateway` | 81.6 % | 83.3 % | Heavily exercised by both gateway-contract tests and the two adapter test projects. |
| `LengthPrefixFraming` | 81.2 % | **73 %** | The 5 framing tests in `LengthPrefixFramingTests.cs` cover all spec-anchored rules; uncovered branches are defensive fall-through paths. |
| `JsonRpcPipeGateway` | **0 %** | **0 %** | **No coverage — by design.** A real named-pipe end-to-end test requires a Sub-team 1 server handler; deferred to integration sprints per [D5 §4.3](test-strategy.md). The class compiles and is production-shaped; correctness is bounded by `LengthPrefixFraming` (covered) and `FakeGateway` (covered, mirrors its public surface). |

If `JsonRpcPipeGateway` were excluded (it has no in-process testable surface), the Gateway assembly's effective coverage rises to ~80 %.

## 3. Risk hotspots (cyclomatic complexity × uncovered)

ReportGenerator flagged these as the highest-leverage methods to add tests against next:

| Class | Method | Cyclomatic | Coverage gap |
|---|---|---:|---|
| `JsonRpcPipeGateway` | `DispatchInbound` | 24 | 0 % — needs the integration test described in D5 §4.3 |
| `LengthPrefixFraming` | `ReadOneAsync` | 26 | 73 % branch — defensive paths |
| `FileTabViewModel` | `MaskAxesMatchImage` | 16 | partial — see §2.2 candidate 3 |
| `JsonRpcPipeGateway` | `DisposeAsync` | 6 | 0 % |

## 4. CI gate posture

Per D5 §7, the CI gate is:

- **Hard fail:** `iDaVIE.Client.ViewModel` (the VM assemblies — `FileTabSkeleton`, `DebugTabSkeleton`) below 70 % branch **or** line.
- **Tracked:** everything else.

**Right now both gated assemblies clear the gate** (DebugTabSkeleton 100 / 100, FileTabSkeleton 89.4 / 77.2). Wiring the gate into CI as the Quality Guild's Day-10 task (per ADR-0005) will not block any current PR. The branch gap at the class level on `SubsetBoundsViewModel` (66.6 %) and the two `*RelayCommand` helpers (50 %) does **not** fail the assembly gate — gates apply at the assembly level. We can re-target the gate at class level later if the panel wants finer granularity.
