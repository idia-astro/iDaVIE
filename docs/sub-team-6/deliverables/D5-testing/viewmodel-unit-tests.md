# ViewModel Unit-Test Strategy (Tier 1)

**Parent doc:** [`test-strategy.md`](test-strategy.md) · **Spec refs:** §6.6 ST · §9.2.4 · §4.2 #4 · §7.1–7.2 · LO6

---

## 1. Goal

Establish a repeatable, Unity-free unit-test strategy for all ViewModel classes produced by the MVVM split (rationale in [D2 architecture doc](../D2-Architecture/architecture.md)). Prove that business logic is testable in isolation — no Unity Editor, no scene, no native plug-ins — and achieve **≥ 70 % branch and line coverage** on domain code.

---

## 2. Scope

| In scope | Out of scope |
|---|---|
| All classes under the proposed `ViewModel/` namespace | `View/` layer (Unity Test Framework handles that — [`ui-toolkit.md`](ui-toolkit.md)) |
| `IFileTabViewModel`, `IDebugTabViewModel`, `ILogStream`, `ILogObserver`, `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle` interface contracts | Unity MonoBehaviours / UI Toolkit wiring |
| Pure-C# domain logic and Observer wiring | **Client-side gateway and gateway-proxy adapters** (`IServiceGateway`, `FitsServiceAdapter`, `GatewayLogStreamAdapter`, `LengthPrefixFraming`) — Tier 2 in [`test-strategy.md` §4](test-strategy.md). **Server-side** producers (FITS plug-in, `log.emit` publisher) — owned by Sub-team 1. |

---

## 3. Framework stack

| Tool | Version | Role |
|---|---|---|
| **NUnit 3** | ≥ 3.14 | Test runner and assertion library |
| **Moq 4** | ≥ 4.20 | Interface mocking |
| **.NET 7 SDK** (standalone) | match Unity 2021 Mono runtime | Build + run tests outside Unity |
| **Coverlet** | latest | Branch + line coverage collection |
| **ReportGenerator** | latest | HTML + Cobertura reports for CI |

Tests live in plain **.NET class library projects** under [`refactoring-examples/sub-team-6/`](../../../../refactoring-examples/sub-team-6/): `file-tab/tests/FileTabTests.csproj` and `debug-tab/tests/DebugTabTests.csproj`. Each references only:
- the ViewModel skeleton csproj (no `UnityEngine` dependency — `dotnet build` succeeds with 0 warnings, 0 errors);
- `NUnit`, `Moq`, `Coverlet` NuGet packages.

Unity is never imported. This is the hard guarantee that enforces the anti-corruption layer. The 76 committed tests (47 file-tab + 29 debug-tab) run in **~20 ms total** on the debug-tab side; the file-tab side has not yet been timed but uses the same stack.

---

## 4. Dependency isolation rules

1. **No `UnityEngine` in `ViewModel/`** — any ViewModel that `using UnityEngine` fails the build (`<Nullable>enable</Nullable>` + `<WarningsAsErrors>` in the `.csproj`).
2. **All external services behind interfaces** — `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `IFitsHandle` (file tab); `ILogStream`, `ILogObserver` (debug tab). Moq mocks every one of these in the committed suites — §4.2 #4 satisfied.
3. **No `FindObjectOfType` / `MonoBehaviour` in ViewModel** — enforced by the standalone project: these types don't exist.
4. **Static clock / randomness injected** — `ISystemClock` interface so time-dependent logic is deterministic.

---

## 5. Test categories and naming convention

```
[Category("ViewModel")]
public class FileTabViewModelTests
{
    // Arrange / Act / Assert structure.
    // Method name: MethodUnderTest_Scenario_ExpectedBehaviour
    [Test]
    public void OpenCube_HappyPath_CallsGatewayWithExpectedArgs() { ... }
    [Test]
    public void OpenCube_GatewayThrows_SetsErrorMessageProperty() { ... }
    [Test]
    public void OpenCube_WhileLoading_CommandIsIgnoredAndNoDoubleCall() { ... }
}
```

Categories:
- `ViewModel` — pure ViewModel logic (the committed 76 tests)
- `LogStream` — `LogStream` Observer-dispatch class (debug tab)

---

## 6. Mock patterns

### 6.1 Happy path — split service interfaces

This pattern tests the normal success case: each service succeeds, the ViewModel updates its state correctly, and it delegates to the services exactly once with the right arguments.

The four split interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`) separate ViewModel logic from native plug-ins, the OS file dialog, Unity's volume renderer, and the OS memory query. Mocking each with Moq lets the test drive the success path without a single Unity API or P/Invoke call. After `BrowseImageCommand` executes, the test asserts the dialog and FITS service were called as expected, then asserts that `IsLoadable` flips to `true` and `ErrorMessage` is `null`, proving the ViewModel cleaned up and surfaced no spurious error.

```csharp
var fits   = new Mock<IFitsService>();
var dialog = new Mock<IFileDialogService>();
var volume = new Mock<IVolumeService>();
var memory = new Mock<IMemoryProbe>();

dialog.Setup(d => d.PickFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
      .ReturnsAsync("/data/cube.fits");
fits.Setup(f => f.OpenImageAsync("/data/cube.fits"))
    .ReturnsAsync(new FitsFileInfo { /* 3-axis cube */ });

var vm = new FileTabViewModel(fits.Object, dialog.Object, volume.Object, memory.Object);
await vm.BrowseImageCommand.ExecuteAsync(null);

dialog.Verify(d => d.PickFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
fits  .Verify(f => f.OpenImageAsync("/data/cube.fits"), Times.Once);
Assert.That(vm.IsLoadable,   Is.True);
Assert.That(vm.ErrorMessage, Is.Null);
```

### 6.2 Error path — service throws

This pattern tests fault handling: when a service raises an exception (bad header, missing file, OS failure), the ViewModel must catch it, surface a human-readable message through `ErrorMessage`, and leave `IsLoadable` as `false` so the Load button stays disabled.

The mock is configured with `ThrowsAsync` so the awaited call throws `FitsServiceException` instead of returning a value. The ViewModel is expected to catch this internally — the test itself should not throw — and map the exception message to the `ErrorMessage` property. This verifies that error handling lives in the ViewModel rather than leaking into the View or going unhandled.

```csharp
fits.Setup(f => f.OpenImageAsync(It.IsAny<string>()))
    .ThrowsAsync(new FitsServiceException("invalid header"));

await vm.BrowseImageCommand.ExecuteAsync(null);

Assert.That(vm.ErrorMessage, Is.EqualTo("invalid header"));
Assert.That(vm.IsLoadable,   Is.False);
```

### 6.3 Observer test — `ILogStream` (Debug tab)

This pattern tests the Debug tab's role as a **passive observer** of the application's logging infrastructure. `DebugTabViewModel` implements `ILogObserver` and subscribes to `ILogStream` via `Subscribe(this)`; each `OnNext(LogEntry)` appends to a bindable `LogEntries` collection. It must not poll or own the log source — it simply reacts.

The test publishes through a concrete `LogStream` (a pure-C# `ILogStream`); the ViewModel, having subscribed itself, receives the entry via `OnNext`. The test then asserts that the ViewModel's `LogEntries` collection contains exactly one item with the correct message. This confirms that the subscription is wired up, that the entry is appended (not replaced), and that the ViewModel does not filter or silently drop the event. Running this without Unity proves the Observer wiring is pure C# with no scene or engine dependency.

```csharp
var logStream = new LogStream();            // concrete ILogStream — pure C#, no Unity
var vm = new DebugTabViewModel(logStream);  // ctor calls logStream.Subscribe(this)

logStream.Publish(LogLevel.Warning, "VR init slow");

Assert.That(vm.LogEntries, Has.Count.EqualTo(1));
Assert.That(vm.LogEntries[0].Message, Is.EqualTo("VR init slow"));
```

---

## 7. Coverage targets and measurement

| Namespace | Branch target | Line target | Measured by |
|---|---|---|---|
| `ViewModel/` | ≥ 70 % | ≥ 70 % | Coverlet + CI gate |
| `View/` (Unity) | tracked | tracked | Unity Test Framework, not gated |

Run locally:

```
dotnet test DesktopClient.Tests/ --collect:"XPlat Code Coverage"
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

CI (Quality Guild pipeline) will fail the build if either gate is missed on `main`.

---

## 8. What is NOT unit-tested here

| Concern | Why excluded | Handled by |
|---|---|---|
| UI Toolkit bindings | Requires Unity runtime | [`ui-toolkit.md`](ui-toolkit.md) (page-object pattern) |
| Named-pipe transport | I/O boundary | Gateway integration test with test server stub |
| Unity MonoBehaviour lifecycle | Requires scene | Manual smoke test list |

---

## 9. Traceability

| Item | Reference |
|---|---|
| MVVM split decision | [D2 architecture doc](../D2-Architecture/architecture.md) (ADR rationale lives there — no separate ADR files) |
| Interface contracts | [`file-tab/skeleton/IFileTabViewModel.cs`](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/IFileTabViewModel.cs), [`debug-tab/skeleton/IDebugTabViewModel.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/skeleton/IDebugTabViewModel.cs) |
| Parent test strategy | [`test-strategy.md`](test-strategy.md) |
| Worked test suites | [`FileTabViewModelTests.cs`](../../../../refactoring-examples/sub-team-6/file-tab/tests/FileTabViewModelTests.cs) (47), [`DebugTabTests.cs`](../../../../refactoring-examples/sub-team-6/debug-tab/tests/DebugTabTests.cs) (29) |
| CK thresholds this strategy defends | §7.1 — RFC ≤ 50, LCOM ≤ 0.5 on ViewModel classes |
| Assignment spec reference | §9.2.4, §6.6 ST, LO6 |

---
