# iDaVIE — Monolithic Components & Microkernel Migration Path

This document identifies the parts of iDaVIE's codebase that have grown monolithic, describes how they are coupled to the rest of the system today, and proposes a concrete microkernel architecture that can be adopted incrementally.

---

## 1. Overview of the Current Architecture

iDaVIE is structured in two physical layers:

| Layer | Technology | Location |
|---|---|---|
| Native computation | C++ shared library (`idavie_native.dll`) | `native_plugins_cmake/` |
| Application | Unity 2021.3 LTS / C# | `Assets/Scripts/` |

Within the Unity layer, the logical modules are:

- **VolumeData** — FITS loading, voxel data, WCS metadata, 3D texture upload, volume shader parameters, mask I/O
- **PluginInterface** — P/Invoke bindings to the native DLL (`FitsReader`, `DataAnalysis`, `AstTool`)
- **FeatureData** — Source/feature set management, VOTable I/O, selection state
- **CatalogData** — External point/line catalogue datasets, catalog shader management
- **Menu** — In-VR menus (quick menu, paint, moment maps, spectral profile, video record)
- **UI** — Desktop canvas components, laser pointer, color bar, keypad
- **VoiceCommands** — Voice command list display (recognition itself is in VolumeCommandController)
- **VRKeyboard** — In-VR keyboard input
- **Shapes** — In-VR geometry drawing
- **Tools** — Utility components (FPS display, benchmark, camera controller)

The native plugin layer is already well isolated. The application layer is where the monolithic patterns have accumulated.

---

## 2. Monolithic Components

### 2.1 `VolumeCommandController` — The God Object

**File:** `Assets/Scripts/VolumeData/VolumeCommandController.cs`

This is the single most entangled class in the project. It was originally a voice command handler but has accumulated responsibilities across the entire application.

#### What it owns directly

```csharp
// Four concrete menu controllers wired via Unity Inspector
public GameObject mainCanvassDesktop;
public QuickMenuController QuickMenuController;
public PaintMenuController PaintMenuController;
public MomentMapMenuController momentMapMenuController;
public FeatureMenuController featureMenuController;

// Discovered at runtime
private VolumeInputController _volumeInputController;    // FindObjectOfType
private VideoRecordMenuController _videoRecordMenuController; // FindObjectOfType
```

Every component it references is a concrete class — not an interface. There is no abstraction between the command dispatcher and the subsystems it commands.

#### The if-else command dispatch chain

`ExecuteVoiceCommand(string args)` is a 200-line if-else chain that directly calls methods on `VolumeDataSetRenderer`, `VolumeInputController`, `QuickMenuController`, `PaintMenuController`, `FeatureMenuController`, and `VideoRecordMenuController`. Adding any new voice command requires editing this single method and importing knowledge of whatever new subsystem handles it.

#### Hardcoded UI hierarchy navigation

Several methods navigate the Unity scene hierarchy via chained `Transform.Find()` calls, bypassing any logical interface:

```csharp
// From resetThreshold() — a 10-level deep path string
mainCanvassDesktop.gameObject.transform
    .Find("RightPanel").gameObject.transform
    .Find("Panel_container").gameObject.transform
    .Find("RenderingPanel").gameObject.transform
    .Find("Rendering_container").gameObject.transform
    .Find("Viewport").gameObject.transform
    .Find("Content").gameObject.transform
    .Find("Settings").gameObject.transform
    .Find("Threshold_container").gameObject.transform
    .Find("Threshold_min").gameObject.transform
    .Find("Slider").GetComponent<Slider>().value = _activeDataSet.ThresholdMin;
```

The same pattern repeats in `endThresholdEditing()` and `resetTransform()`. A rename of any node in this hierarchy chain silently breaks the feature at runtime with no compile-time warning.

#### Coupling summary

```
VolumeCommandController
    ├── VolumeDataSetRenderer   (direct method calls + property access)
    ├── VolumeInputController   (direct method calls + state machine access)
    ├── QuickMenuController     (direct method calls)
    ├── PaintMenuController     (direct method calls)
    ├── MomentMapMenuController (direct method calls)
    ├── FeatureMenuController   (direct method calls)
    ├── VideoRecordMenuController (direct method calls)
    └── mainCanvassDesktop      (Transform.Find() hierarchy traversal)
```

**Impact:** Any of the seven referenced systems cannot be refactored, renamed, or replaced without editing `VolumeCommandController`. It also cannot be tested in isolation.

---

### 2.2 `VolumeInputController` — Too Many Responsibilities

**File:** `Assets/Scripts/VolumeData/VolumeInputController.cs`

This MonoBehaviour handles SteamVR controller input but has accumulated responsibilities that belong in separate tools.

#### Responsibilities currently in one class

| Responsibility | Should belong to |
|---|---|
| SteamVR button binding (Oculus/Vive/WMR detection) | Input abstraction layer |
| Locomotion state machine (Idle / Moving / Scaling / EditingThreshold / EditingZAxis) | Locomotion service |
| Interaction state machine (Idle / Painting / EditingSourceId / Creating / Editing / VideoCamPosRecording) | Separate tool state machines |
| Brush painting (additive/subtractive, brush size) | PaintTool |
| Source creation and editing | FeatureTool |
| Video position recording (`AddNewLocation`) | VideoRecordTool |
| Controller haptics (`VibrateController`) | Input service |
| Screenshot capture (`TakePicture`) | Utility tool |
| Cursor info display toggle | UI service |

#### Exposed internal state

`VolumeCommandController` directly accesses the internal state machine:

```csharp
// VolumeCommandController.cs
_volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.StartEditSource);
```

This forces `InteractionStateMachine` to be `public`, making the state machine's internal transitions a de facto external API. Any refactoring of the state machine breaks remote callers.

#### Runtime discovery pattern

```csharp
// QuickMenuController.OnEnable()
_volumeInputController = FindObjectOfType<VolumeInputController>();

// VolumeCommandController.OnEnable()
_volumeInputController = FindObjectOfType<VolumeInputController>();
_videoRecordMenuController = FindObjectOfType<VideoRecordMenuController>(true);
```

`FindObjectOfType<>()` is called at runtime during `OnEnable()` in multiple classes. This is an ad-hoc service locator: the dependency graph is invisible until the application runs, and if the order of `OnEnable()` calls changes the components may get null references.

---

### 2.3 `QuickMenuController` — Hardcoded Menu Registry

**File:** `Assets/Scripts/Menu/QuickMenuController.cs`

The quick menu is the VR user's primary navigation hub, but every sub-panel it can show is hardcoded as a `public GameObject` field:

```csharp
public GameObject sourcesMenu;
public GameObject paintMenu;
public GameObject plotsMenu;
public GameObject voiceCommandsListCanvas;
public GameObject settingsMenu;
public GameObject videoRecordingMenu;
public GameObject colorMapListCanvas;
public GameObject savePopup;
public GameObject exitPopup;
public GameObject exitSavePopup;
public GameObject exportPopup;
```

Adding a new tool means: adding a new field, wiring it in the Unity Inspector, and adding a method to open it. There is no concept of a registered tool that provides its own panel.

---

### 2.4 Voice Command Registration

**File:** `Assets/Scripts/VolumeData/VolumeCommandController.cs` — `Keywords` struct

All voice command strings are hardcoded in a single `Keywords` struct inside `VolumeCommandController`:

```csharp
public struct Keywords
{
    public static readonly string PaintMode = "paint mode";
    public static readonly string ExitPaintMode = "exit paint mode";
    public static readonly string EnterVideoMode = "video mode";
    // ... 40+ more entries
    public static readonly string[] All = { ... };  // aggregated array passed to KeywordRecognizer
}
```

A new analysis tool cannot declare its own voice commands. All strings must be added to this central struct and the if-else dispatch chain extended.

---

### 2.5 Native Plugin — Already Modular, But Unversioned

**Files:** `Assets/Scripts/PluginInterface/`, `native_plugins_cmake/`

The native plugin layer is the healthiest part of the project. The `NativePluginLoader` uses reflection over custom attributes to wire C++ symbols to C# delegates at startup:

```csharp
[PluginAttr("idavie_native")]
public static class DataAnalysis
{
    [PluginFunctionAttr("FindMaxMin")]
    public static readonly FindMaxMinDelegate FindMaxMin = null;
    // ...
}
```

This means adding a new C++ function requires only adding a delegate and an attribute — the loader discovers it automatically. This is already a microkernel-style plugin mechanism.

The weakness is that the interface is unversioned. If the C++ function signature changes, the mismatch is only caught at runtime when the delegate is invoked.

---

## 3. Microkernel Architecture

### 3.1 Core Concept

In a microkernel design, the **kernel** is a minimal stable core that provides:
- A service registry (replacing `FindObjectOfType<>` and Inspector fields)
- A command bus (replacing the if-else dispatch chain)
- Stable interfaces to the volume renderer and input system

Everything else — paint tool, moment maps, spectral profiles, features, voice commands, catalog, video recording — becomes a **plugin** that registers against the kernel's interfaces. The kernel knows nothing about astronomy, painting, or moment maps.

This directly implements the medium-term roadmap goal stated in README.md:
> *"Separate the visualisation from the analysis tools... allowing the end user to download iDaVIE the visualisation tool and the analysis plugin relevant to their field."*

---

### 3.2 Kernel Components

#### AppKernel — Service Registry

Replaces all `FindObjectOfType<>()` calls. Components register themselves at `Awake()` time and resolve dependencies through the registry.

```csharp
public class AppKernel : MonoBehaviour
{
    public static AppKernel Instance { get; private set; }

    private readonly Dictionary<Type, object> _services = new();

    public void Register<T>(T service) => _services[typeof(T)] = service;

    public T Get<T>() => (T)_services[typeof(T)];

    public bool TryGet<T>(out T service)
    {
        if (_services.TryGetValue(typeof(T), out var obj)) { service = (T)obj; return true; }
        service = default; return false;
    }
}
```

Usage replaces Inspector wiring and runtime `FindObjectOfType<>`:

```csharp
// Before
_volumeInputController = FindObjectOfType<VolumeInputController>();

// After
_inputSource = AppKernel.Instance.Get<IInputSource>();
```

#### CommandBus — Typed Event Dispatcher

Replaces the if-else dispatch chain in `VolumeCommandController.ExecuteVoiceCommand()`. Any input source (voice, controller, desktop button) publishes typed command objects. Any subsystem subscribes to the commands it handles. Publishers and subscribers never reference each other.

```csharp
public class CommandBus : MonoBehaviour
{
    public static CommandBus Instance { get; private set; }

    private readonly Dictionary<Type, List<Action<ICommand>>> _handlers = new();

    public void Subscribe<T>(Action<T> handler) where T : ICommand
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type)) _handlers[type] = new List<Action<ICommand>>();
        _handlers[type].Add(cmd => handler((T)cmd));
    }

    public void Publish<T>(T command) where T : ICommand
    {
        if (_handlers.TryGetValue(typeof(T), out var handlers))
            foreach (var h in handlers) h(command);
    }
}
```

#### Kernel-Level Command Types

One typed record per action. These are defined in the kernel — they have no dependency on any analysis tool.

```csharp
public interface ICommand { }

// Rendering
public record SetColorMapCommand(ColorMapEnum Map) : ICommand;
public record SetScalingTypeCommand(ScalingType Type) : ICommand;
public record SetThresholdCommand(float Min, float Max) : ICommand;
public record SetProjectionModeCommand(ProjectionMode Mode) : ICommand;
public record SetMaskModeCommand(MaskMode Mode) : ICommand;
public record ResetTransformCommand : ICommand;
public record ResetThresholdCommand : ICommand;
public record ResetZAxisCommand : ICommand;

// Navigation
public record TeleportToSelectionCommand : ICommand;
public record CropToSelectionCommand : ICommand;
public record ResetCropCommand : ICommand;

// Tool activation (tools respond to these to self-activate)
public record ActivateToolCommand(string ToolName) : ICommand;
public record DeactivateToolCommand(string ToolName) : ICommand;

// Capture
public record TakeScreenshotCommand : ICommand;
public record ExportSubCubeCommand : ICommand;
```

#### IVolumeRenderer — Renderer Interface

`VolumeDataSetRenderer` implements this interface. Analysis tools hold `IVolumeRenderer` — never the concrete class.

```csharp
public interface IVolumeRenderer
{
    ColorMapEnum ColorMap { get; set; }
    ScalingType ScalingType { get; set; }
    float ThresholdMin { get; set; }
    float ThresholdMax { get; set; }
    MaskMode MaskMode { get; set; }
    ProjectionMode ProjectionMode { get; set; }
    bool DisplayMask { get; set; }
    VolumeDataSet DataSet { get; }

    void CropToFeature();
    void ResetCrop();
    void TeleportToRegion();
    void RegenerateCubes();

    event Action<ColorMapEnum> OnColorMapChanged;
    event Action<float, float> OnThresholdChanged;
}
```

#### IInputSource — Input Interface

`VolumeInputController` implements this for the parts other systems need. Its internal state machine is no longer public.

```csharp
public interface IInputSource
{
    SteamVR_Input_Sources PrimaryHand { get; }
    void VibrateController(SteamVR_Input_Sources hand);

    event Action PushToTalkPressed;
    event Action PushToTalkReleased;
}
```

---

### 3.3 Plugin Interface

Each analysis tool implements `IAnalysisTool` and registers with the kernel.

```csharp
public interface IAnalysisTool
{
    string Name { get; }
    string[] VoiceCommands { get; }           // phrases this tool handles
    void HandleCommand(string voicePhrase);   // called when its phrase is recognized
    GameObject BuildMenuPanel();              // returns its own VR panel prefab
    void Activate();
    void Deactivate();
}
```

A `ToolRegistry` component (part of the kernel) discovers registered tools, aggregates their voice command strings into the `KeywordRecognizer`, and routes recognized phrases to the owning tool.

```csharp
public class ToolRegistry : MonoBehaviour
{
    private readonly List<IAnalysisTool> _tools = new();

    public void Register(IAnalysisTool tool) => _tools.Add(tool);

    public IReadOnlyList<IAnalysisTool> Tools => _tools;

    // Called by QuickMenuController to build tabs dynamically
    public IEnumerable<(string name, GameObject panel)> GetMenuPanels() =>
        _tools.Select(t => (t.Name, t.BuildMenuPanel()));
}
```

The `QuickMenuController` no longer has hardcoded `public GameObject paintMenu` fields. It asks the `ToolRegistry` for registered tools and builds its tabs from their panels at startup.

---

### 3.4 Example: PaintTool as a Plugin

Before (today): Paint logic is split across `VolumeCommandController` (handles voice), `PaintMenuController` (owns the menu), and `VolumeInputController` (owns brush state and painting execution).

After: One `PaintTool` class owns all of it.

```csharp
public class PaintTool : MonoBehaviour, IAnalysisTool
{
    private IVolumeRenderer _renderer;
    private CommandBus _bus;

    public string Name => "Paint";
    public string[] VoiceCommands => new[] { "paint mode", "exit paint mode", "brush add", "brush erase", "undo", "redo" };

    void Awake()
    {
        _renderer = AppKernel.Instance.Get<IVolumeRenderer>();
        _bus = CommandBus.Instance;
        AppKernel.Instance.Register<PaintTool>(this);

        _bus.Subscribe<ActivateToolCommand>(cmd => { if (cmd.ToolName == Name) Activate(); });
        _bus.Subscribe<DeactivateToolCommand>(cmd => { if (cmd.ToolName == Name) Deactivate(); });
    }

    public void HandleCommand(string phrase)
    {
        switch (phrase)
        {
            case "paint mode":    Activate();           break;
            case "exit paint mode": Deactivate();       break;
            case "brush add":     AdditiveBrush = true; break;
            case "brush erase":   AdditiveBrush = false;break;
            case "undo":          _renderer.DataSet.Mask?.UndoBrushStroke(); break;
            case "redo":          _renderer.DataSet.Mask?.RedoBrushStroke(); break;
        }
    }

    public GameObject BuildMenuPanel() => Instantiate(_paintMenuPrefab);

    public void Activate()   { /* enable brush rendering */ }
    public void Deactivate() { /* restore idle state */     }
}
```

`VolumeCommandController` shrinks to: translate voice string → dispatch typed command → done. It no longer imports `PaintMenuController`, `MomentMapMenuController`, or any other concrete tool.

---

### 3.5 Handling the Transform.Find() Problem

The hardcoded hierarchy paths in `VolumeCommandController` should be replaced with events that the UI subscribes to:

```csharp
// VolumeDataSetRenderer publishes events when its state changes
public event Action<float, float> OnThresholdChanged;

// The threshold slider subscribes in its own Start()
_renderer.OnThresholdChanged += (min, max) =>
{
    _minSlider.value = min;
    _maxSlider.value = max;
};
```

The command controller never touches sliders. The sliders listen for state changes from the renderer and update themselves. This also eliminates the runtime crash risk from hierarchy renames.

---

### 3.6 Architecture Comparison

#### Today

```
VoiceInput ──────────────────────────────────────────────────┐
ControllerInput ──────────────────────────────────────────── VolumeCommandController ──► QuickMenuController
DesktopButtonClick ──────────────────────────────────────────┘        │                        │
                                                                       │                ┌───────┴───────┐
                                                              VolumeInputController  PaintMenuController
                                                                       │              MomentMapMenuController
                                                              VolumeDataSetRenderer  FeatureMenuController
                                                                                     VideoRecordMenuController
```

Every input source routes through `VolumeCommandController`. It calls concrete classes directly. Adding a tool means editing the dispatcher.

#### With Microkernel

```
VoiceInput ──────► ToolRegistry.RouteVoiceCommand() ──► IAnalysisTool.HandleCommand()
ControllerInput ─► CommandBus.Publish<ICommand>()   ──► Subscriber handlers
DesktopButton ───► CommandBus.Publish<ICommand>()   ──►     │
                                                             │
                                          ┌──────────────────┤
                                          │                  │
                               IVolumeRenderer         IAnalysisTool[]
                               (implemented by         (PaintTool, MomentMapTool,
                                VolumeDataSetRenderer)  SpectralProfileTool, ...)
```

The kernel (`AppKernel`, `CommandBus`, `ToolRegistry`) knows nothing about painting or moment maps. New tools are added by implementing `IAnalysisTool` and calling `ToolRegistry.Register(this)`.

---

## 4. Incremental Migration Path

The existing `NativePluginLoader` attribute mechanism proves this approach works in Unity. The migration can proceed in phases without a rewrite:

| Phase | Change | Risk |
|---|---|---|
| 1 | Introduce `AppKernel` and migrate all `FindObjectOfType<>()` call sites | Low — mechanical substitution |
| 2 | Introduce `CommandBus` and typed commands; have `VolumeCommandController.ExecuteVoiceCommand()` publish commands instead of calling methods directly | Low — subscribers can initially be the same concrete types |
| 3 | Replace `Transform.Find()` chains with UI event subscriptions on `IVolumeRenderer` events | Medium — requires adding events to `VolumeDataSetRenderer` |
| 4 | Extract `IVolumeRenderer` interface from `VolumeDataSetRenderer` | Low — interface extraction |
| 5 | Extract `IInputSource` from `VolumeInputController`; make `InteractionStateMachine` private | Medium — breaks `VolumeCommandController` direct access |
| 6 | Lift `PaintTool` into `IAnalysisTool` | Medium — first full tool extraction |
| 7 | Lift remaining tools one by one | Low per tool once pattern is established |
| 8 | `QuickMenuController` builds tabs dynamically from `ToolRegistry` | Medium — replaces hardcoded panel fields |

At each phase the application remains fully functional. Phases 1–3 can be done without any visible change to behaviour.

---

## 5. Summary

| Component | Problem | Microkernel solution |
|---|---|---|
| `VolumeCommandController` | God object; knows about every subsystem; 200-line if-else dispatch | Becomes a thin voice-string → `ICommand` translator; tools own their own command handling |
| `VolumeInputController` | Too many responsibilities; exposes internal state machine | Split: `IInputSource` (kernel) + per-tool state machines (plugins) |
| `QuickMenuController` | Hardcoded panel references; cannot discover new tools | Builds panels dynamically from `ToolRegistry` |
| Voice command strings | All strings in one struct in one class | Each `IAnalysisTool` declares its own strings; `ToolRegistry` aggregates |
| `Transform.Find()` chains | Silent runtime failures on UI rename | UI subscribes to renderer events; no outside traversal |
| `FindObjectOfType<>()` | Invisible dependencies; order-sensitive | `AppKernel` service registry; explicit registration in `Awake()` |
| Native plugin loader | Already modular; unversioned interfaces | Add interface version negotiation to `PluginAttr` |
