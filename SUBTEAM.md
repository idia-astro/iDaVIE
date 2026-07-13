# Persistence & Workspace State Sub-team Specification

## Sub-team Overview

This sub-team owns the persistence architecture and workspace lifecycle management of the iDaVIE system.

Primary responsibility:
- Capture, persist, validate, restore, and recover complete iDaVIE workspace state.

The persistence system must support:
- Reliable save/load workflows
- Autosave and crash recovery
- Corrupted and partial state recovery
- Missing-value fallback handling
- Schema versioning and migrations
- Deterministic and testable persistence behavior

The persistence layer must remain fully independent from Unity-specific APIs and be implemented as a pure C# domain/infrastructure system.

---

# Behavioral Element Owned

This sub-team owns the lifecycle of a user's iDaVIE session.

Responsibilities include:
- Capturing active workspace state
- Durable storage and restoration
- Recovery after crashes or corruption
- State migration across schema versions
- Safe handling of incompatible or incomplete save files

Managed state includes:
- Loaded FITS cubes
- Mask edits
- Paint strokes
- Defined features
- View parameters
- Selection boxes
- Render settings
- GUI state
- Interaction state

---

# Core Component Responsibilities

## Saving States

The persistence system must:
- Serialize all required workspace state
- Validate state before saving
- Ensure deterministic save behavior
- Support autosave snapshots
- Support future schema evolution
- Use atomic save operations where possible

---

## Corrupted / Partial State Recovery

The system must:
- Detect corrupted save data
- Detect incomplete save files
- Recover valid portions of state safely
- Isolate invalid state sections
- Prevent undefined runtime behavior
- Preserve user work wherever possible

Examples:
- Partial feature recovery
- Recovery after interrupted save operations
- Graceful degradation of invalid rendering state

---

## Missing Required Value Handling

The system must safely handle:
- Missing required fields
- Invalid enum values
- Broken references
- Missing plugin data
- Missing rendering parameters

Recovery behavior:
- Apply safe defaults
- Trigger recovery policies
- Disable invalid components safely
- Preserve remaining valid state

Examples:
- Missing render settings → restore defaults
- Missing feature metadata → isolate feature
- Missing plugin state → disable dependent functionality safely

---

# Workspace Definition

A workspace represents the complete recoverable state of an iDaVIE session.

---

## Dataset State
- Loaded FITS cubes
- Dataset metadata
- HDU selections
- Coordinate transforms
- Streaming/loading state

---

## Rendering State
- Camera state
- Render settings
- Colour maps
- Lighting configuration
- Mask modes
- Foveated rendering parameters

---

## Feature State
- User-defined features
- Imported features
- Masked features
- Feature statistics
- Spectral profiles
- Moment map state

---

## Interaction State
- Current interaction mode
- Selection state
- VR state
- Controller preferences
- Tool/menu state

---

## GUI State
- Open tabs
- Panel layouts
- Window state
- Debug panel state
- User preferences

---

## Persistence Metadata
- Save timestamp
- Schema version
- Plugin compatibility versions
- Recovery metadata
- Migration information

---

# Shared Requirements, Design, and Testing

## Workspace Definition

The persistence subsystem must formally define:
- Workspace domain models
- Aggregates
- Invariants
- Serializable boundaries

The workspace model must remain:
- Framework-independent
- Deterministic
- Version-safe

---

## Persistence Boundary

The system must define:
- Persistence formats
- Serialization schema
- Versioning strategy
- Migration strategy
- Compatibility rules

Requirements:
- Semantic versioning
- Forward compatibility
- Backward migration support where possible
- Explicit schema evolution

---

## Lifecycle Flow

The persistence lifecycle must include:
- Autosave cadence
- Transactional save semantics
- Conflict resolution
- Snapshot recovery
- Crash recovery orchestration

Goals:
- Prevent data loss
- Minimize corrupted writes
- Ensure restore consistency

---

## State Contracts

Every major subsystem must expose:
- Serializable DTOs/contracts
- Explicit persistence boundaries
- Recovery behavior
- Default fallback behavior

Dependent sub-teams:
- Rendering Engine
- Feature System
- Interaction System
- Desktop GUI
- Data I/O Plugins

Each subsystem must define:
- Required state
- Optional state
- Validation rules
- Recovery policies

---

## Recovery Path Implementation

The system must support recovery for:
- Partial state
- Corrupted state
- Version mismatches
- Missing dependencies
- Invalid references
- Incomplete migrations

Recovery strategies:
- Partial restoration
- Safe fallback defaults
- Invalid-state isolation
- Controlled degradation

The system must never:
- Crash from invalid persistence state
- Enter undefined runtime behavior
- Leak corrupted references into runtime systems

---

# Architectural Requirements

## Core Design Constraints

The persistence system must:
- Be fully independent from UnityEngine
- Be testable outside Unity
- Use interface-driven boundaries
- Support dependency injection
- Avoid circular dependencies
- Follow SOLID and GRASP principles

---

## Recommended Architecture

```text
Domain
  ├── Workspace Aggregate
  ├── State Models
  ├── Validation Rules
  └── Recovery Policies

Application
  ├── Save Workspace Use Case
  ├── Load Workspace Use Case
  ├── Migration Coordination
  └── Recovery Orchestration

Infrastructure
  ├── Serialization
  ├── Compression
  ├── Schema Handlers
  ├── Integrity Validation
  └── Storage Providers

Persistence Host
  ├── File Adapters
  ├── Plugin State Bridges
  └── External Storage Connectors