# ADR-001: Team 1 Owns Kernel Contracts

## Status

Accepted

## Decision

Team 1 owns the canonical kernel contracts, value types, delegates, logging,
configuration, plug-in registry, volume registry, volume loader, and volume
aggregate state for the refactored slice.

## Consequences

Other teams consume Team 1 contracts through interfaces and shared value types.
Any shape conflict is resolved against `refactored/info/shared_interfaces.md`.
