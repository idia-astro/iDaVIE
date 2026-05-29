# ADR-006: Logging Goes Through ILogSink

## Status

Accepted

## Decision

Kernel and domain services log through `ILogSink`. `DebugLogSink` stores a
bounded recent history and raises events for UI/debug consumers.

## Consequences

The kernel stays independent of Unity console APIs. UI layers can subscribe to
log events rather than scraping external state.
