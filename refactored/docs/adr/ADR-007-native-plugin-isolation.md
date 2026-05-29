# ADR-007: Native Plug-ins Are Isolated

## Status

Accepted

## Decision

Native plug-in loading and symbol binding are isolated in `NativePluginLoader`.
FITS, WCS, and data-analysis behavior is reached through managed contracts.

## Consequences

Direct static native calls are forbidden in Team 1-owned refactored code. The
adapters can be smoke-tested with managed fallbacks when the native libraries
are unavailable.
