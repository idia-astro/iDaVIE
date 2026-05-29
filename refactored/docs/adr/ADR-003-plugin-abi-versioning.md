# ADR-003: Plug-in ABI Versioning

## Status

Accepted

## Decision

Managed plug-in contracts expose an `AbiVersion` string, and `Config` declares
the expected ABI major version. Native symbols are bound only behind
`NativePluginLoader`.

## Consequences

Native library changes can be checked at the managed boundary. Tests can use
managed fakes or adapter fallbacks without loading native DLLs.
