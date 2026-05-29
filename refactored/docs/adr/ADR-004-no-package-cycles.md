# ADR-004: No Package Cycles

## Status

Accepted

## Decision

Team 1 contracts sit at the dependency root. Higher layers may depend inward on
kernel contracts; kernel contracts must not depend back on rendering, UI,
interaction, persistence implementations, Unity, or Valve APIs.

## Consequences

Back-edges are rejected by review and by static scans. Shared payloads remain
plain C# value types.
