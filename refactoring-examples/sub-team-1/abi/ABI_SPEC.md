# iDaVIE Plug-in ABI Specification

**Status:** DRAFT v0.1.0
**Owner:** Sub-team 1 (Architecture and Micro-kernel Core), Team Alpha
**Companion artefacts:** `CONFORMANCE.md`, ADRs 001–005
The C binding header that realises this contract (`idavie_abi.h`) is a **Sub-team 2**
deliverable ("C ABI binding header samples", assignment §6.2), produced against this spec.

This document specifies the C Application Binary Interface (ABI) between the
iDaVIE micro-kernel and its C/C++ plug-ins. It is a design-only deliverable
produced as part of the iDaVIE refactoring proposal; no production code is
modified.

This specification is the normative contract; Sub-team 2's binding header sample
(`idavie_abi.h`) realises it in C. This document explains the *why* behind each
design decision and states the rules a plug-in must follow to be considered
ABI-conformant.

---

## 1. Scope and Goals

The ABI replaces the current ad-hoc plug-in interface, which mixes:

- Two binding styles on the C# side (custom reflection loader + `[DllImport]`).
- Third-party types (`fitsfile*`, `AstFrameSet*`) leaked through public headers.
- Multiple `Free*` functions per module.
- Side-channel `int*` status outputs combined with mixed integer return-code
  conventions.
- Struct-layout drift between the C++ and C# sides (e.g. `SourceStats.spectralProfileSize`
  is `int64_t` natively and `int` in C# — a latent corruption bug).

The revised ABI is designed to:

1. Be **stable** within a major version, with explicit SemVer.
2. Be **third-party-free** at the boundary — opaque handles only.
3. Be **single-error-path** — one status enum, no side channels.
4. Be **single-allocator** — one `idavie_free`, one buffer descriptor.
5. Be **C# friendly** — every type is blittable, every struct size is
   `static_assert`ed on both sides.
6. Be **observable** — a thread-local last-error message accompanies every
   non-OK return.

ISO/IEC 25010:2023 mapping: **Modularity** (decoupling Unity from the data
plumbing), **Reusability** (versioned interface stability), **Modifiability**
(plug-ins can evolve without breaking the host).

---

## 2. Symbol Versioning

### 2.1 SemVer scheme

The ABI follows Semantic Versioning 2.0.0:

- **MAJOR** — any change visible at the binary level. Includes: removing or
  renaming an exported function, changing a function signature, reordering or
  resizing any struct that crosses the ABI, renumbering any value in
  `idavie_status_t`, changing calling convention.
- **MINOR** — backwards-compatible additions only. New exported functions,
  new status codes appended to the end of `idavie_status_t`, new fields at
  the end of struct types that are *only* used as out-parameters from the
  ABI (never as inputs).
- **PATCH** — documentation, tightened semantics within existing wording,
  performance fixes. No code-visible change.

### 2.2 Compile-time version

The binding header defines:

```c
#define IDAVIE_ABI_VERSION_MAJOR 0
#define IDAVIE_ABI_VERSION_MINOR 1
#define IDAVIE_ABI_VERSION_PATCH 0
```

These are the version a translation unit was compiled against.

### 2.3 Runtime version export

Every conformant plug-in exports:

```c
IDAVIE_API uint32_t idavie_abi_version(void);
```

Returning the packed version it was built against. The host calls this
**immediately after `dlopen`/`LoadLibrary`** and **before binding any other
symbol**. If `MAJOR` differs from the host's compile-time `IDAVIE_ABI_VERSION_MAJOR`,
the host refuses to load the plug-in and emits a structured log entry.

### 2.4 Calling convention and symbol naming

The `IDAVIE_API` macro pins:

- **Visibility**: `__declspec(dllexport/dllimport)` on Windows, `visibility("default")`
  on Linux/macOS.
- **Calling convention**: `__cdecl` on x86 (no-op on x64; written for protection
  if anyone ever builds 32-bit).

All exported symbols are prefixed `idavie_` and `snake_case`. No symbol
shorter than 8 characters; no symbol in the unprefixed global namespace
(the current `Show`, `Format`, `Set`, `Clear`, `Dump`, `Norm` exports of
AstTool are explicitly forbidden under the revised ABI).

### 2.5 Deprecation

Symbols may be marked `[[deprecated]]` in a MINOR release; they are not
removed until the next MAJOR release. The replacement must be specified in
the deprecation note.

---

## 3. Error Model

### 3.1 Single status enum

Every function returns `idavie_status_t`. There are no out-parameter
`int*` status flags (which the current `fits_reader.h` uses on every call).

```c
typedef int32_t idavie_status_t;
enum {
    IDAVIE_OK = 0,
    IDAVIE_ERR_INVALID_ARGUMENT = 1,
    /* … */
};
```

`IDAVIE_OK` is guaranteed to be `0`; all error codes are positive. Callers
test with `if (status != IDAVIE_OK)`.

### 3.2 Thread-local last error

For human-readable context after a non-OK return, callers may consult:

```c
IDAVIE_API const char* idavie_last_error_message(void);
```

- Always returns a valid C string (never NULL).
- The buffer is owned by the plug-in and lives until the next ABI call on
  the **same thread**.
- Thread-local storage — concurrent calls on different threads never
  observe each other's messages.

### 3.3 What does NOT belong in the error model

- No exceptions cross the ABI. Implementations must catch all C++
  exceptions at the boundary and translate to `IDAVIE_ERR_INTERNAL`.
- No `errno`. ABI status codes are self-contained.
- No CFITSIO error codes leaking through. The FITS plug-in translates
  them to the `idavie_status_t` taxonomy and exposes the original code in
  the last-error message text only.

---

## 4. Threading Model

### 4.1 Default contract

Unless documented otherwise:

- Functions are **re-entrant**: simultaneous calls on different handles
  from different threads are safe.
- Functions are **NOT** thread-safe on the same handle: simultaneous calls
  with the same `idavie_*_t*` first argument are undefined behaviour. The
  host is responsible for serialising access to a handle.

### 4.2 Exceptions to the default

- `idavie_last_error_message()` is thread-local and always safe.
- `idavie_abi_version()` is pure and always safe.
- `idavie_free()` is safe to call from any thread, but never concurrently
  with another ABI call that may still hold a pointer into the buffer.
- The AST plug-in is **not** internally thread-safe even across handles
  (legacy AST library limitation). The plug-in serialises with a private
  mutex; the host should still avoid contending it.

### 4.3 Progress and cancellation

Long-running operations (FITS reads of multi-GB cubes, full-volume
statistics) accept an optional `idavie_progress_fn`:

```c
typedef int32_t (*idavie_progress_fn)(double fraction_done, void* user_data);
```

- Called from the worker thread, never the caller's thread.
- Returning non-zero requests cooperative cancellation; the operation
  returns `IDAVIE_ERR_CANCELLED` at the next safe checkpoint.
- The callback may be `NULL` to opt out.

---

## 5. Memory Ownership

### 5.1 Two allocation patterns

The ABI defines exactly two patterns. No `T**` out-parameters; no per-module
free functions (the current ABI has three: `FreeDataAnalysisMemory`,
`FreeFitsPtrMemory`, `FreeAstMemory`).

**Pattern A — caller-allocated (preferred):**

```c
IDAVIE_API idavie_status_t idavie_fits_get_image_size(
    idavie_fits_t* h, int64_t* sizes_out, int32_t capacity);
```

The caller allocates; the plug-in writes into the buffer. If `capacity` is
insufficient, the function returns `IDAVIE_ERR_OUT_OF_RANGE` and writes
the required capacity to `sizes_out[0]` (or a dedicated out-parameter,
depending on the function).

**Pattern B — plug-in-allocated descriptor:**

```c
typedef struct {
    void*    data;
    int64_t  length;
    uint32_t elem_size;
    uint32_t reserved;
} idavie_buffer_t;

IDAVIE_API void idavie_free(idavie_buffer_t buf);
```

The plug-in allocates and populates the descriptor. The caller MUST free
it by passing the value (not a pointer to it) to `idavie_free`. Used only
when the caller cannot know the size in advance.

### 5.2 The single-allocator invariant

All memory that crosses the ABI is allocated by the plug-in and freed
through `idavie_free`. This guarantees a single CRT heap is responsible
for the lifetime, which removes the Windows DLL-boundary heap-mismatch
class of bugs entirely.

### 5.3 Handle lifetime

Handles (`idavie_fits_t*`, `idavie_ast_t*`, `idavie_analysis_t*`) are
created by a `_open`/`_create` function and destroyed by exactly one
`_close` function per type. Double-close is a programming error and is
defined to return `IDAVIE_ERR_NULL_HANDLE`. The C# side wraps each handle
in a `SafeHandle` so GC cleanup is automatic.

### 5.4 Input pointer lifetimes

Pointers passed *into* the ABI (`const float* data`, `const char* path`)
must remain valid for the duration of the call. They are never retained
by the plug-in after the call returns unless the function's documentation
explicitly says so (and no current function does).

---

## 6. ABI Compatibility Policy

### 6.1 What is part of the ABI

The contract includes:

1. The set of exported symbols and their full signatures (parameter types,
   return type, calling convention).
2. The layout of every struct that crosses the ABI, including padding.
   Struct sizes are `static_assert`ed.
3. The numeric values of every enumerator in `idavie_status_t`.
4. The semantics each symbol must obey (documented per-function).

### 6.2 What is allowed within a major version

- Adding new exported functions.
- Appending new enumerators to `idavie_status_t`.
- Tightening documented semantics in a backwards-compatible way (e.g.
  promising thread-safety on a function that was previously unspecified).
- Adding new opaque handle types and the functions that operate on them.

### 6.3 What requires a major bump

- Removing or renaming an exported function.
- Changing any existing function signature, including parameter types,
  argument count, return type, or calling convention.
- Reordering, resizing, or repurposing any field of any ABI struct.
- Changing the numeric value of any existing `idavie_status_t` enumerator.
- Changing the semantics of an existing function in a way that would
  surprise a correct existing caller.

### 6.4 Detection at load time

The host's plug-in loader executes the following sequence on every load:

1. `dlopen` / `LoadLibrary` the plug-in.
2. Resolve `idavie_abi_version`. If missing → reject as non-conformant.
3. Compare `MAJOR` to the host's compile-time `IDAVIE_ABI_VERSION_MAJOR`.
   On mismatch → reject with `IDAVIE_ERR_VERSION_MISMATCH` logged.
4. Resolve every other symbol from the host's required-symbol manifest.
   On any missing symbol → reject; emit a list of missing symbols.
5. Bind and proceed.

This replaces the current loader behaviour (`NativePluginLoader.cs:147-150`)
which logs missing symbols as warnings and continues, deferring the failure
to a downstream NRE at first call.

---

## 7. Conformance

A plug-in is **ABI-conformant** if and only if it:

1. Exports `idavie_abi_version` and returns a value whose `MAJOR` matches
   the version it was compiled against.
2. Exports `idavie_last_error_message` and `idavie_free` with the signatures
   given here.
3. Returns only `IDAVIE_OK` or a defined error code from every function.
4. Catches all C++ exceptions at the boundary.
5. Allocates every returned buffer such that it is freeable via `idavie_free`.
6. Honours the threading contract (default re-entrancy; documented
   exceptions only).
7. Compiles cleanly against the binding header with `-Wall -Wextra -Werror`
   (or `/W4 /WX`) and with `-fvisibility=hidden` (Linux/macOS) so only
   `IDAVIE_API` symbols are exported.
8. Passes the contract test suite specified in `CONFORMANCE.md`.

A non-conformant plug-in is rejected at load time. The full conformance
checklist and contract-test description live in `CONFORMANCE.md`.

---

## 8. Open Questions for Sprint 1 Review

These will be resolved at the Architecture Guild review on Wed 20 May
or the Sprint 1 review on Fri 22 May:

1. **Async dispatch boundary.** Do we keep all calls synchronous and let
   the host manage threading, or expose an `idavie_future_t` abstraction?
   Provisional answer: synchronous + progress/cancellation callback. Will
   confirm with Sub-team 2.
2. **String encoding.** UTF-8 is the proposal. Need to confirm Windows
   filename handling for non-ASCII paths under .NET Framework.
3. **Mutability of input mask buffers.** Some current call sites mutate
   the mask in-place. Should the ABI require copy-on-write, or expose an
   explicit `_inplace` variant?
4. **VOTable and TIFF plug-ins.** Sub-team 2's design will exercise
   whether the buffer descriptor handles row-oriented data well, or
   whether a separate `idavie_table_t` handle type is warranted.

---

## 9. Related Documents

- `CONFORMANCE.md` — conformance definition and contract test suite specification.
- `idavie_abi.h` — the C binding header sample realising this contract; a
  Sub-team 2 deliverable (assignment §6.2, "C ABI binding header samples").
- `docs/adr/ADR-001-c-abi-over-grpc.md` and subsequent ADRs.
- iDaVIE Refactoring Assignment §4.2 (mandatory constraints), §6.1
  (Sub-team 1 deliverables).
