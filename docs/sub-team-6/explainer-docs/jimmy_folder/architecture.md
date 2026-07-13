# CanvassDesktop Refactoring Proposal

## Current State: `CanvassDesktop.cs`

`CanvassDesktop.cs` is the main desktop UI controller — a ~1,900-line `MonoBehaviour` responsible for the desktop canvas, menu coordination, user input, and multiple sub-controllers.

Its responsibilities currently include:

- **FILE tab** — opening data and mask files
- **RENDER, STATS, and SOURCES tabs** — adjusting settings
- **DEBUG tab** — displaying logs and diagnostic information
- **Client startup** — connecting the UI to the server when the application launches

## ISO Quality Problems

The current design performs poorly against several ISO software quality characteristics.

### Modularity

`CanvassDesktop.cs` owns eight unrelated concerns in a single class. As a result, changing one tab can accidentally break another. The goal is to replace the current design with multiple small, focused classes connected through clear interface contracts.

### Analysability

The class has a **WMC (Weighted Methods per Class)** of 63 and an **RFC (Response For a Class)** of 118. Both values are very high and make the class difficult to understand, reason about, and maintain. The target is **WMC ≤ 27** and **RFC ≤ 50** per class.

### Modifiability

The class relies heavily on `transform.Find` chains to locate UI objects. These hard-coded paths are fragile: if a UI element is renamed or moved, multiple lookup paths must be updated manually. This increases the likelihood of regressions.
<img width="1999" height="1168" alt="Screenshot 2026-06-01 212254" src="https://github.com/user-attachments/assets/261f8de8-654e-45c9-98fa-ca5b1cd2138e" />


### Testability

There are effectively no clean, isolated methods that can be unit tested. Most behaviour is tightly coupled to the Unity lifecycle and scene state. The target of at least 70% branch coverage through automated testing is unrealistic without first moving logic into testable classes.

### Overall Problem

`CanvassDesktop.cs` is too large and too tightly coupled to be maintainable, testable, or ISO-compliant. It is difficult to modify, difficult to analyse, and risky to extend.

## Proposed Architecture: View, ViewModel, Gateway, External Systems

### View

The View is the part the user sees and interacts with. It contains no business logic. Its job is to display data, capture user input such as button clicks or text entry, notify the ViewModel when something happens, and then update itself when the ViewModel state changes.

### ViewModel

The ViewModel is the decision-making layer of the feature. It contains business logic, validation, state management, and application behaviour.

For example, when the View reports that the user clicked **Open File**, the ViewModel:

- Validates the input
- Coordinates with services such as FITS loading
- Updates its own state

The ViewModel performs this work through interfaces such as `IFitsService` and `IFileDialogService`. It does not directly call Unity APIs or communicate with the server itself. Because ViewModels are pure C#, they can be unit tested without launching Unity.

### Gateway

The Gateway implements the interfaces used by the ViewModel and acts as a translator between the core application logic and external systems. It accepts clean interface calls such as `IFitsService.OpenFile(path)` and handles the underlying technical details, including:

- Building JSON-RPC messages
- Sending them over named pipes or another transport
- Calling Unity APIs where necessary
- Deserialising responses

The Gateway is split into two main forms:

- **Gateway proxies (pure C#)** — communicate with the server through JSON-RPC
- **Unity adapters** — the only classes allowed to call Unity APIs directly

This layer hides whether data comes from the server, local disk, or another source. The ViewModel only sees a clean abstraction.

### External Systems

External systems perform the actual work outside the core architecture. Examples include:

- **Server** — loads FITS files using native CFITSIO libraries
- **Unity** — provides file picker dialogs and UI framework behaviour
- **PlayerPrefs** — stores local user settings

These systems do not need to know anything about the architecture.

### Flow Summary

1. The user performs an action in the View.
2. The View notifies the ViewModel.
3. The ViewModel decides what to do and calls the appropriate interface.
4. The Gateway translates that call and routes it to an external system.
5. The external system executes the request and returns a result.
6. The ViewModel updates its state.
7. The View updates automatically through data binding.

Because the ViewModels are pure C#, fast NUnit tests can run without launching Unity.

## Unity 2021.3 to Unity 6 Migration

The project is moving from **Unity 2021.3 using uGUI Canvas** to **Unity 6 using UI Toolkit**.

With this architecture:

- Only the **View layer** needs to change
- The UI documents are rewritten in the new framework
- The ViewModels remain unchanged because they are UI-agnostic
- Business logic does not need to be rewritten or re-tested

This significantly reduces migration effort and lowers the risk of introducing bugs during the upgrade.

## Client to Server Communication

### Current Approach: JSON-RPC

The client currently communicates with the server through JSON-RPC. ViewModels call Gateway interfaces, and the Gateway converts those interface calls into JSON-RPC messages sent to the server.

### Considered Alternative: gRPC

A possible alternative is gRPC. gRPC uses Protocol Buffers (`protobuf`), which is a compact binary format that is generally more efficient than JSON. Instead of writing request payloads manually, developers define messages and services in a `.proto` file.

gRPC also supports duplex streaming and uses HTTP/2 to send multiple messages in parallel over a single connection. However, it introduces additional setup and debugging complexity, so it was not adopted for this assignment.

## ACL and Unity/SteamVR Coupling

The current iDaVIE desktop client allows core logic such as ViewModels to import Unity and SteamVR types directly. At present, this includes around **13 Unity types** and **4 SteamVR types**. This creates two major problems:

- Tests cannot run without starting Unity because logic depends on the Unity runtime
- Any Unity API change can affect the broader codebase instead of remaining isolated to framework-specific code

To solve this, the design introduces an **ACL (Anti-Corruption Layer)**.

The ACL works like a border crossing:

- Domain and ViewModel code live on one side
- Unity and SteamVR live on the other side
- ViewModels communicate only through clean interfaces
- Unity and SteamVR are accessed only through adapter classes in the ACL

Static analysis tools such as **NDepend** or **DV8** can enforce this architecture automatically by failing a build when a ViewModel assembly introduces forbidden Unity references. The result is that every ViewModel can be instantiated and tested without starting a Unity process.

## Why the Current Design Blocks Testing

`CanvassDesktop` only runs inside Unity, so it cannot be properly unit tested with standard tools such as NUnit. It is also tightly coupled to scene objects, object lookups, and native calls, which makes mocking and isolation extremely difficult.

As a result, strong automated test coverage is not realistic unless the important logic is moved out of `CanvassDesktop` and into normal, testable C# classes.

### Before

The old `CanvassDesktop` script talked directly to many scene objects and low-level system calls. That made it extremely hard to fake dependencies in tests.

### After

The refactored design moves important logic into separate ViewModels. Anything external — such as files, FITS loading, or logs — is accessed through small interfaces. In tests, those interfaces can be replaced with fake implementations. This means tests can run on a normal machine without Unity, a GPU, VR hardware, or real files, and they behave consistently.

## Why the Current Design Increases Upgrade Risk

The move from Canvas UI to UI Toolkit means the UI layer must change. If UI code and core logic remain mixed together inside `CanvassDesktop`, both layers must be rewritten at the same time.

That creates more work, more risk of bugs, and a harder maintenance path in future.

### Before / Goal

The project is moving from the old Canvas UI in Unity 2021.3 to UI Toolkit in Unity 6. The goal is for only the **presentation layer** to change, not the underlying logic.

### After

Only the View layer — screens, buttons, and layout — is rewritten. The ViewModel and Gateway layers remain the same and do not depend on the UI system. This reduces both effort and migration risk because the business logic and server communication stay untouched.

## Extensibility Benefits

The architecture is designed to make future additions easier without forcing changes to the core logic.

### `IServiceGateway`

`IServiceGateway` acts as a middleman between the ViewModel and the backend. Whether calls are local or sent over JSON-RPC, the ViewModel remains unchanged. New features such as a Python console or save/load support can reuse the same ViewModel and Gateway structure rather than requiring a redesign.

### `IMenuRouter`

`IMenuRouter` is a shared command contract used by both desktop and VR for actions such as **Open File** or **Toggle Paint Mode**. VR can issue the same commands without knowing anything about desktop-specific UI controls. This keeps desktop and VR behaviour aligned while allowing the interface details to evolve independently.

## Enforcing the Architecture in CI

The design also relies on an automated build-time rule in **CI (Continuous Integration)** to protect the architecture.

### What It Does

It checks the code during the build and fails or warns if any ViewModel references forbidden libraries such as Unity.

### How It Works

This rule uses **NDepend + CQLinq**:

- **NDepend** is a static analysis tool
- **CQLinq** is its query language for inspecting code structure and dependencies

This acts as a guardrail that catches accidental architectural violations — for example, someone importing Unity into a ViewModel — before the code is merged.

## Rejected Alternatives

### Unity Test Framework Only

One option was to rely only on Unity's built-in testing tools. The problem is that this does not remove the coupling to Unity. It adds tests around tightly coupled code but does not solve the underlying architectural issue.

### Partial Classes

Another option was to split the large `MonoBehaviour` into several partial files. In practice, this is still one class with Unity and non-Unity code mixed together. It improves file organisation, but not architecture.

### Wrapper Base Class for `CanvassDesktop`

A third option was to create a base class that wraps Unity functionality and have other classes inherit from it. While this reduces some direct Unity usage, it still creates a strong inheritance-based dependency on Unity and does not properly isolate business logic.

## Applying SOLID Principles

| Principle | Meaning | Application |
|---|---|---|
| **SRP** | **Single Responsibility Principle** | Each tab, such as File or Debug, has its own ViewModel with one clear responsibility instead of one class handling everything. |
| **DIP** | **Dependency Inversion Principle** | ViewModels depend on abstractions such as `IFitsService` instead of concrete FITS readers or Unity APIs, which makes testing much easier. |
| **OCP** | **Open/Closed Principle** | New panels can be added by implementing `IPanel` without modifying existing shell code. |
| **ISP** | **Interface Segregation Principle** | `IPanel` exposes only the lifecycle methods a panel actually needs, so classes are not forced to implement irrelevant behaviour. |

## Applying GRASP Patterns

### Information Expert

Behaviour should live where the relevant data lives. `SubsetBoundsViewModel` stores subset bounds, so it also owns the validation logic for those values. That gives one class clear ownership of the rules.

### Creator

The object that coordinates others should create them. `CanvassDesktopShell` constructs adapters, ViewModels, and views because it knows which parts exist and how they are connected.

### Low Coupling

Classes should depend as little as possible on specific frameworks. ViewModels contain no `UnityEngine` references, while Unity-specific details are isolated in adapters. This improves flexibility and testability.

### High Cohesion

Each class should have a focused purpose. Each ViewModel handles one tab rather than one god class managing eight unrelated concerns. This makes the code easier to understand, test, and maintain.
