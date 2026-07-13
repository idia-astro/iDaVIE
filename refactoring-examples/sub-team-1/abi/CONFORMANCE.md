# iDaVIE Plug-in ABI Conformance

**Status:** DRAFT v0.1.0
**Owner:** Sub-team 1 (Architecture and Micro-kernel Core), Team Alpha
**Companion artefacts:** `ABI_SPEC.md` (the normative contract)

This document defines what it means for a C/C++ plug-in to be **ABI-conformant**
against the contract in `ABI_SPEC.md`, and **specifies the contract test suite**
the kernel uses to certify a candidate plug-in before it is allowed to load. It
satisfies the Sub-team 1 testing activity *"Contract test suite for any C/C++
plug-in to be considered conformant"* and expands `ABI_SPEC.md` §7.

This is a **design-only** deliverable. It specifies the suite — the checks, their
pass criteria, and how they gate CI — rather than shipping a runnable harness.
The concrete C binding header the suite tests against (`idavie_abi.h`) is a
**Sub-team 2** deliverable ("C ABI binding header samples", assignment §6.2); the
*per-plug-in* conformance test plan is likewise Sub-team 2's. This suite is the
generic contract that applies to *any* plug-in regardless of what it does.

---

## 1. Conformance definition

A plug-in is **conformant** if and only if it satisfies every clause below.
Each clause records *how* it is checked: at **build time** (compiler/linker),
**statically** (inspection of the binary), or **dynamically** (a harness loads
the plug-in and exercises it).

| # | Clause (from `ABI_SPEC.md` §7) | Verified |
|---|--------------------------------|----------|
| C1 | Exports `idavie_abi_version`; its `MAJOR` matches the version it was compiled against and the host's `IDAVIE_ABI_VERSION_MAJOR`. | Dynamic |
| C2 | Exports `idavie_last_error_message` and `idavie_free` with the signatures defined by the ABI contract. | Dynamic |
| C3 | Every function returns only `IDAVIE_OK` or a value defined in `idavie_status_t` (`IDAVIE_OK == 0`; errors positive). | Dynamic |
| C4 | Catches all C++ exceptions at the boundary and translates to `IDAVIE_ERR_INTERNAL` — no exception unwinds across the ABI. | Dynamic (via test hook) |
| C5 | Every buffer it returns is freeable via `idavie_free`; no per-module free functions; no DLL-boundary heap mismatch. | Dynamic (+ sanitizer) |
| C6 | Honours the threading contract: re-entrant by default; `idavie_last_error_message` is thread-local. | Dynamic |
| C7 | Compiles cleanly against the binding header with `-Wall -Wextra -Werror` (`/W4 /WX`) and `-fvisibility=hidden`, so only `IDAVIE_API` symbols are exported. | Build time |
| C8 | Resolves every symbol in the host's **required-symbol manifest** (§3). Missing symbols are rejected *at load*, not deferred to first call. | Dynamic |

A plug-in that fails any clause is **rejected at load time** by the kernel's
plug-in host (`ABI_SPEC.md` §6.4), with a structured log entry naming the failed
clause.

---

## 2. The contract test suite

The suite is the executable embodiment of §1. A conformance harness takes the
path to a candidate plug-in, loads it exactly as the kernel would, and runs the
checks below. Each check yields `[PASS]`, `[FAIL]`, or `[SKIP]`; any failure
fails the run.

| Check | Clause | Pass criteria |
|-------|--------|---------------|
| `version.symbol` | C1 | `idavie_abi_version` resolves. |
| `version.major`  | C1 | Returned `MAJOR` equals host `IDAVIE_ABI_VERSION_MAJOR`. |
| `manifest`       | C8 | Every symbol in the required-symbol manifest resolves; missing symbols are listed. |
| `error.nonnull`  | C2/C6 | `idavie_last_error_message()` is never `NULL`; returns `""` when no error is recorded on the calling thread. |
| `error.tls`      | C6 | Errors raised on worker threads do **not** appear in the calling thread's last-error buffer. |
| `error.codes`    | C3 | A deliberately invalid call returns a *defined, non-OK* `idavie_status_t` and leaves a non-empty last-error message. |
| `buffer.roundtrip` | C5 | A function returning an `idavie_buffer_t` produces a sane descriptor (`data != NULL`, `length > 0`, `elem_size > 0`, `reserved == 0`) that is freed via `idavie_free` without fault. |
| `exception.isolation` | C4 | If the optional self-test hook (§4) is present, calling it returns `IDAVIE_ERR_INTERNAL` rather than crashing or unwinding. Otherwise `[SKIP]`. |

Functional, plug-in-specific behaviour (e.g. "does the FITS reader read the
right pixels") is **out of scope** here — that is covered by Sub-team 2's
per-plug-in test corpus and conformance test plan. This suite tests only the
*contract*, so it runs unchanged against every plug-in regardless of domain.

---

## 3. Required-symbol manifest

Every conformant plug-in, whatever its domain, must export the three universal
contract symbols:

```
idavie_abi_version
idavie_last_error_message
idavie_free
```

Domain plug-ins additionally export their declared surface from the ABI binding
header (e.g. the `idavie_fits_*`, `idavie_ast_*`, `idavie_analysis_*` families).
The host composes the manifest per plug-in *kind*: the three universal symbols
plus the functional symbols that kind is required to provide.

---

## 4. The exception self-test hook (optional, test-only)

C4 cannot be observed from the outside through the normal surface — a correct
plug-in simply never lets an exception escape, which is indistinguishable from a
plug-in that happens not to throw. To make C4 *testable*, a plug-in may export a
**test-only** hook:

```c
/* Deliberately raises an internal C++ exception, catches it at the boundary,
 * and returns IDAVIE_ERR_INTERNAL. Present only in conformance builds. */
IDAVIE_API idavie_status_t idavie_selftest_throw(void);
```

Convention: real plug-ins compile this symbol **only** under
`-DIDAVIE_CONFORMANCE_BUILD`, so it never ships in a release binary. The harness
treats it as optional: present → verify it returns `IDAVIE_ERR_INTERNAL`;
absent → `exception.isolation` is reported `[SKIP]` and C4 must instead be argued
by code inspection in the plug-in's review.

---

## 5. Reference implementation shape

A conformance harness implementing this suite is expected to:

1. Load the candidate plug-in with `dlopen`/`LoadLibrary` exactly as the kernel
   host does (`ABI_SPEC.md` §6.4).
2. Resolve and run the checks of §2, in dependency order (version and manifest
   first; functional probes after).
3. Exit non-zero if any check fails, so it drops directly into a CI gate.
4. Ship with a small **reference conformant plug-in** (a test double) so the
   suite's own happy path is exercised, and run under AddressSanitizer/UBSan to
   make the heap-ownership (C5) and threading (C6) checks bite.

The harness is host-side C/C++ built with CMake/CTest, matching the team's
existing native-test convention. It is intentionally not included as production
code in this design-only proposal.

---

## 6. CI integration

The conformance gate is intended to run in two places, consistent with the
architecture-violation policy (`ABI_SPEC.md` §6):

1. **Per-plug-in CI** — every plug-in repository runs the harness against its own
   build artefact; a non-zero exit fails the pipeline.
2. **Integration CI** — the kernel's plug-in host runs the harness against every
   bundled plug-in before packaging, so a non-conformant plug-in can never reach
   a release.

A plug-in that has not passed the current suite is treated as non-conformant and
is refused at load by the host.

---

## 7. Limitations and manual review

- **C4 without the hook** is established by inspection, not by the harness.
- **C7** is a property of the plug-in's *own* build configuration; the harness
  cannot retro-actively prove a binary was built with the required flags, so the
  flags are mandated in the plug-in's build template and spot-checked in review.
- **C6 re-entrancy across distinct handles** is exercised opportunistically; full
  stress/concurrency testing of a specific plug-in is that plug-in's
  responsibility, layered on top of this contract suite.

---

## 8. Related documents

- `ABI_SPEC.md` — the prose specification (§7 defines conformance; this document
  operationalises it).
- `idavie_abi.h` — the C binding header sample the suite tests against; a
  Sub-team 2 deliverable (assignment §6.2).
- iDaVIE Refactoring Assignment §6.1 — Sub-team 1 deliverables.
