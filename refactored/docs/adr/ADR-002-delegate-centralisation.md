# ADR-002: Cross-Team Delegates Are Centralised

## Status

Accepted

## Decision

Cross-team delegate declarations live in `iDaVIE.Kernel.Contracts.Delegates`.
Compatibility handler aliases may remain in `iDaVIE.Kernel` while skeleton
consumers migrate.

## Consequences

New cross-team events require an ADR update and cannot be declared ad hoc by
feature, rendering, UI, or persistence code.
