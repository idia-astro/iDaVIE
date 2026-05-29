# ADR-005: Configuration Is Injected

## Status

Accepted

## Decision

Startup configuration is loaded through `Config.LoadFromJson` and provided by
the composition root. Runtime code does not read a global singleton.

## Consequences

Tests can construct deterministic configuration objects, and runtime behavior
does not depend on scene-global mutable state.
