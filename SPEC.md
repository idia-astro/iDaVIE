# iDaVIE Refactoring Specification

## Project Goal

Refactor the iDaVIE (Immersive Data Visualization Interactive Explorer) codebase to improve maintainability against ISO/IEC 25010:2023 and support migration from Unity 5-era architecture to Unity 6.

iDaVIE is an open-source VR application for immersive visualization of 3D astronomical FITS data cubes.

Primary goals:
- Improve modularity
- Improve analysability
- Improve modifiability
- Improve testability
- Reduce coupling and architectural debt
- Enable long-term maintainability
- Support future Unity 6 migration safely

---

# Tech Stack

## Core Technologies
- Unity 5 → Unity 6 migration
- C#
- Native C/C++ plugins
- SteamVR → Unity 6 Input System

## Rendering/UI
- Unity Scriptable Render Pipeline (URP/HDRP)
- Unity UI Toolkit
- GPU ray-marching
- 3D texture rendering

## Communication
- JSON-RPC over named pipes (local mode)
- gRPC (future remote mode)

## Tooling
- SonarQube
- NDepend
- CodeScene
- DV8
- GitHub Actions
- Understand

---

# Current Architecture

## Existing System

Current architecture relies heavily on:
- Single Unity scene
- MonoBehaviour-driven logic
- Native DLL plugins:
  - FitsReader
  - DataAnalysis
  - AstTool

## Current Problems

### Architectural Issues
- Tight coupling between MonoBehaviours and scene state
- Heavy singleton usage
- God classes and monolithic components
- Circular dependencies
- Mixed rendering and interaction concerns
- Thin abstraction around native plugins

### Maintainability Issues
- Unity lifecycle tightly coupled to business logic
- Low cohesion
- High coupling
- Minimal automated testing
- Difficult migration path to Unity 6

---

# Target Architecture

## System Architecture

### Client–Server Split

Client responsibilities:
- Rendering
- VR interaction
- Input handling
- Presentation/UI

Server responsibilities:
- Data management
- Domain logic
- Plugin execution
- Long-running computations

---

## Micro-kernel Backend

The backend uses a micro-kernel architecture:
- Stable kernel core
- Versioned plugin ABI
- Dynamically loaded C/C++ plugins

Plugins handle:
- FITS parsing
- WCS coordinate transforms
- Statistics
- Downsampling
- Data operations

---

## Layered Internal Architecture

Strict downward dependency flow:

```text
Domain
  ↓
Application
  ↓
Infrastructure
  ↓
Plugin Host
```

Rules:
- No upward dependencies
- No cyclic dependencies
- Domain layer must remain framework-independent

---

## Anti-Corruption Layer

Unity APIs must be isolated behind adapters/interfaces so:
- Domain logic is testable outside Unity
- Unity migration impact is minimized
- VR/input systems remain replaceable

---

# Mandatory Constraints

## Design Principles

All code must follow:
- SOLID principles
- GRASP principles
- Clear separation of concerns
- High cohesion
- Low coupling

---

## Dependency Rules

Forbidden:
- Circular dependencies
- UnityEngine dependencies inside domain code
- SteamVR dependencies inside business/domain logic

Required:
- Interface-based boundaries
- Dependency inversion
- Test doubles for public APIs

---

## Plugin Rules

C/C++ plugin contracts must:
- Use semantic versioning
- Maintain ABI stability
- Support future extensibility

---

# Key Domain Modules

## 1. Architecture & Micro-kernel Core

Responsibilities:
- Plugin ABI
- Layer dependency policies
- Integration architecture
- ADRs and architectural governance

Patterns:
- Dependency inversion
- Service locator (kernel boundary only)
- Constructor injection internally

---

## 2. Data I/O & FITS/WCS Plugins

Responsibilities:
- FITS reading/writing
- WCS transformations
- Statistics
- Downsampling
- Streaming

Patterns:
- Strategy pattern
- Adapter pattern
- SRP decomposition

Goals:
- Pure plugin architecture
- No Unity dependencies

---

## 3. Rendering Engine

Responsibilities:
- Ray-marching
- 3D textures
- Foveated rendering
- Shader management
- Unity 6 SRP migration

Refactoring target:

```text
VolumeDataSetRenderer
├── VolumeMaterialBinder
├── VolumeTextureManager
├── VolumeCameraDriver
└── FoveatedSamplingPolicy
```

Goals:
- Remove monolithic rendering logic
- Separate rendering concerns cleanly

---

## 4. Interaction System

Responsibilities:
- VR interaction
- Locomotion
- Voice commands
- Menus
- Controller abstraction

Patterns:
- State pattern
- Command pattern
- Interface segregation

Required abstractions:
- IInputProvider
- IVoiceRecogniser
- IHaptics
- IPointer

Goals:
- Unity-independent interaction logic
- Fully testable state machines

---

## 5. Feature System & Domain Model

Responsibilities:
- Feature management
- Statistics
- Moment maps
- Spectral profiles
- VOTable export

Feature types:
- Masked
- Imported
- User-defined

Goals:
- Promote features to first-class domain aggregates
- Remove Unity dependencies from domain logic

---

## 6. Desktop GUI & Client Shell

Responsibilities:
- Desktop GUI
- File loading
- Parameter panels
- Debug tooling
- Service composition root

Architecture:
- MVVM
- ViewModel-first design
- Service Gateway abstraction

Goals:
- Eliminate god-canvas UI architecture
- Fully testable ViewModels

---

## 7. Persistence & Workspace State

Responsibilities:
- Workspace save/load
- Autosave
- Recovery
- Versioning
- Migration support

Requirements:
- Pure C# persistence layer
- Deterministic round-trip testing
- Forward-compatible state migrations

---

# Maintainability Metrics

## Required CK Metrics

| Metric | Target |
|---|---|
| WMC | <= 20 |
| DIT | <= 4 |
| NOC | <= 5 |
| CBO | <= 14 |
| RFC | <= 50 |
| LCOM | <= 0.5 |

---

# Testing Requirements

## Testing Goals

Required:
- High branch coverage
- Interface-driven architecture
- Mockable dependencies
- No Unity dependency in domain tests
- CI-enforced quality gates

## Testing Types

- Unit testing
- Property-based testing
- Golden-image regression testing
- Architecture fitness tests
- Integration testing

---

# Required Deliverables

## Architecture Deliverables
- C4 diagrams
- UML diagrams
- SysML diagrams
- ADRs
- Dependency graphs

## Refactoring Deliverables
- Before/after refactoring examples
- CK metric deltas
- SOLID/GRASP audits
- Trade-off analysis

## CI/CD Deliverables
- Automated quality gates
- Metrics dashboard
- Architecture validation rules

---

# AI Usage Policy

AI usage is expected and encouraged.

Allowed:
- Requirements drafting
- Refactoring proposals
- Architecture suggestions
- Diagram generation
- Test generation
- ADR drafting
- Prose refinement

Not allowed:
- Peer evaluations
- Individual reflections
- Live defence assistance

All generated output must be explainable and defensible by the team.

---

# Success Criteria

The final architecture should:
- Eliminate architectural violations
- Reduce coupling
- Increase cohesion
- Improve testability
- Support Unity 6 migration
- Improve maintainability metrics
- Separate Unity concerns from domain logic
- Enable future extensibility and plugin growth