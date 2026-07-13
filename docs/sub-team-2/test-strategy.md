# Sub-Team 2 — Persistence & Data — Test Strategy

**Scope:** Native plugin layer — `FitsReader` SRP split, `WcsTransformer` Unity decoupling.  
**Test files:** `refactoring-examples/Sub-Team-2/tests/`  
**Runner:** NUnit 3 in a plain .NET 8 project (no Unity installation required for unit tests).

---

## Why we can test without Unity

The core design goal of the refactoring is that `FitsFile`, `FitsHeader`, `FitsImage`, `FitsMask`, and `FitsTable` have **zero `UnityEngine` imports**. This means they compile and run in a standard .NET test project. Integration tests that call the native DLLs still need the `.dll` files on the path, but they do not need a Unity Editor.

`PluginBootstrapper` is the only class that legitimately keeps a Unity dependency (`MonoBehaviour`) — it is excluded from the strict test target.

---

## Test catalogue

Each test is labelled with its type:

| Label | Meaning |
|---|---|
| **Unit** | Single class or method, no external I/O, no DLL calls |
| **Integration** | Calls the real native DLL against a real FITS file |
| **Structural** | Inspects the compiled assembly via reflection to verify design rules |
| **White-box** | Test was written knowing the internal implementation (branching, field names) |
| **Black-box** | Test only cares about observable inputs and outputs, not internals |

---

### Group 1 — `FitsHeader` pure-logic unit tests

These require **no native DLL** — they test C# code only.

#### T-01 `FitsHeader_ErrorCodes_ContainsAllCfitsioRanges`
- **Type:** Unit · White-box
- **Class:** `FitsHeader`
- **What it tests:** The `ErrorCodes` dictionary covers the five CFITSIO error ranges (1xx file I/O, 2xx header, 3xx data access, shared memory 15x, URL 12x).
- **Why it matters:** If a new CFITSIO status code is returned at runtime and is missing from the dictionary, `FitsErrorMessage()` will throw a `KeyNotFoundException` and crash the application with no useful message.
- **Pass criterion:** `ErrorCodes` contains at least one key in each of: 101–159, 201–263, 301–323.

#### T-02 `FitsHeader_FitsErrorMessage_FormatsCodeAndDescription`
- **Type:** Unit · White-box
- **Class:** `FitsHeader`
- **What it tests:** `FitsErrorMessage(104)` returns a string that contains both `"104"` and `"could not open"`.
- **Why it matters:** The original `FitsReader` had an inline format string with no test. This pins the format so a future refactor cannot silently drop the numeric code.
- **Pass criterion:** Return value matches `*104*could not open*` (case-insensitive).

#### T-03 `FitsHeader_ExtractHeaders_DuplicateKeysConcatenated`
- **Type:** Unit · White-box
- **Class:** `FitsHeader.ExtractHeaders`
- **What it tests:** The branching on line 176 — when two reads return the same key name, the values are concatenated into the existing entry rather than throwing or overwriting.
- **Why it matters:** FITS `HISTORY` and `COMMENT` cards legitimately repeat. The before-code silently dropped duplicate keys; the after-code concatenates. This test pins the new behaviour.
- **How:** We call `ExtractHeaders` via a thin fake that returns two reads with the same key (`COMMENT`) and different values. The resulting dictionary entry must contain both values.
- **Pass criterion:** `dict["COMMENT"]` == `"value1value2"`.

---

### Group 2 — `NativePluginLoader` unit tests

These test pure C# guard logic — no DLL is loaded.

#### T-04 `NativePluginLoader_Initialize_SecondCallIsIgnored`
- **Type:** Unit · White-box
- **Class:** `NativePluginLoader`
- **What it tests:** The guard on line 62: calling `Initialize` a second time writes a warning and returns without changing `_path` or calling `LoadAll` again.
- **Why it matters:** In the before-code there was no guard — double initialisation caused access violations in production. The guard is the fix; this test verifies it survives.
- **How:** Call `Initialize("fake_path_1/")`, then `Initialize("fake_path_2/")`. Check that `_path` is still `"fake_path_1/"` (via `Shutdown` resetting state so we can re-run).
- **Pass criterion:** No exception thrown; second path is silently rejected.

#### T-05 `NativePluginLoader_Shutdown_ResetsPathToNull`
- **Type:** Unit · White-box
- **Class:** `NativePluginLoader`
- **What it tests:** After `Shutdown()` the loader accepts a fresh `Initialize` call without complaint.
- **Why it matters:** Ensures the lifecycle is correctly reset between scenes in the Unity editor.
- **Pass criterion:** `Initialize` → `Shutdown` → `Initialize` succeeds without exception.

---

### Group 3 — `FitsFile` + `FitsHeader` integration tests

These **require `idavie_fits.dll`** on the native path and the sample FITS file at `Data/SampleData/test_volume.fits`.

#### T-06 `FitsFile_OpenReadOnly_RealFile_StatusIsZero`
- **Type:** Integration · Black-box
- **Class:** `FitsFile`
- **What it tests:** Opening the sample FITS volume in read-only mode returns status code 0 (CFITSIO success).
- **Why it matters:** Verifies the DLL binding and marshalling are intact after the SRP split. In the before-code the open call was buried inside `FitsReader`; it is now the single responsibility of `FitsFile`.
- **Pass criterion:** `status == 0` and `fptr != IntPtr.Zero`.

#### T-07 `FitsHeader_ExtractHeaders_RealFile_ContainsBitpixAndNaxis`
- **Type:** Integration · Black-box
- **Class:** `FitsHeader`
- **What it tests:** Reading all headers from `test_volume.fits` returns a dictionary that contains the mandatory keys `BITPIX`, `NAXIS`, `NAXIS1`.
- **Why it matters:** If the DllImport signature for `FitsReadKeyN` is broken (wrong marshalling, wrong calling convention), this will return garbage or status ≠ 0.
- **Pass criterion:** Dictionary contains keys `"BITPIX"`, `"NAXIS"`, `"NAXIS1"`.

#### T-08 `FitsImage_GetDimensions_RealFile_Returns3Axes`
- **Type:** Integration · Black-box
- **Class:** `FitsImage`
- **What it tests:** `FitsGetImageDims` returns 3 for the 3-D test volume.
- **Why it matters:** Axis-count is used throughout the render pipeline. A miscounted axis silently produces wrong slice geometry in VR.
- **Pass criterion:** `dims == 3`.

#### T-09 `FitsFile_OpenAndClose_LeavesNoLeak`
- **Type:** Integration · Black-box
- **Class:** `FitsFile`
- **What it tests:** Open the file 50 times in a loop and close each handle. If any close fails, the CFITSIO "too many open files" error (103) appears on the next open.
- **Why it matters:** The original `FitsReader` had conditional close paths that could leak handles. The refactored `FitsFile.FitsCloseFile` is always called unconditionally. This test pins that invariant.
- **Pass criterion:** All 50 open/close cycles return status 0.

---

### Group 4 — Structural tests (no DLL, no FITS file)

These use .NET reflection to verify design rules that are invisible at runtime.

#### T-10 `FitsFile_Assembly_HasNoUnityEngineReference`
- **Type:** Structural · White-box
- **Class:** `FitsFile` (and `FitsHeader`, `FitsImage`, `FitsMask`, `FitsTable`)
- **What it tests:** The compiled assembly for the refactored classes does not reference `UnityEngine`.
- **Why it matters:** This is architectural non-negotiable #3 from the spec: *"Domain code must not transitively depend on UnityEngine."* If someone accidentally adds a `using UnityEngine;` import to one of the data classes, this test fails immediately.
- **How:** `Assembly.GetReferencedAssemblies()` on the test assembly; assert no entry has name `"UnityEngine"`.
- **Pass criterion:** Referenced assemblies list contains no entry named `"UnityEngine"`.

#### T-11 `WcsTransformer_StillHasUnityDependency_KnownTechnicalDebt`
- **Type:** Structural · White-box
- **Class:** `WcsTransformer`
- **What it tests:** Documents the known remaining Unity dependency in `WcsTransformer` — it still uses `Vector3`.
- **Why it matters:** The refactoring goal was to remove Unity from WCS transforms; this was only partially achieved. The test is marked `[Ignore("Tech debt: WcsTransformer.cs still imports UnityEngine via Vector3")]` so the panel can see the gap explicitly rather than having it hidden.
- **Pass criterion:** Test is explicitly skipped with an explanatory message (not silently passing).

---

## Coverage targets

| Layer | Target | Method |
|---|---|---|
| Pure C# logic in `FitsHeader` | ≥ 80 % branch | NUnit + Coverlet |
| `NativePluginLoader` guard logic | 100 % branch | NUnit + Coverlet |
| Native DLL bindings | Not in strict target | Integration tests confirm wiring |
| `PluginBootstrapper` (Unity MonoBehaviour) | Excluded | Unity-bound, tracked not measured |

Overall target: **≥ 70 % branch/line on non-Unity code** (matches spec Section 7.1).

---

## How to run

```bash
# from refactoring-examples/Sub-Team-2/tests/
dotnet test --filter "Category!=Integration"   # unit + structural only, no DLL needed
dotnet test                                     # all tests — needs idavie_fits.dll on PATH
```

Set `IDAVIE_FITS_DLL_PATH` environment variable to the folder containing `idavie_fits.dll` before running integration tests.
