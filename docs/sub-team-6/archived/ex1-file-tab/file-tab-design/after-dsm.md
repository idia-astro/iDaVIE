# WE1-2 — File Tab: AFTER Design Structure Matrix (DSM)

**Proposed design.** The five original participants plus three ACL interfaces inserted as buffers.

An **X** in cell (row, col) means the row component depends on the column component.

| | 1. FileTabViewModel | 2. IVolumeService | 3. IFileDialogService | 4. IFitsService | 5. UnityEngine |
|---|:---:|:---:|:---:|:---:|:---:|
| **1. FileTabViewModel** | — | X | X | X | |
| **2. VolumeServiceAdapter** | | — | | | X |
| **3. StandaloneFileDialogAdapter** | | | — | | X |
| **4. FitsServiceAdapter** | | | | — | |
| **5. UnityEngine** | | | | | — |

## Change from BEFORE

| Metric | Before | After |
|---|---|---|
| Row 1 (CanvassDesktop / FileTabViewModel) X count | 4 | 3 |
| Row 1 depends on UnityEngine | Yes (direct) | **No** |
| Row 1 depends on concrete classes | Yes (StandaloneFileBrowser, FitsReader, VolumeCommandController) | **No — interfaces only** |
| Implicit runtime deps (FindObjectOfType) | 3 | **0** |

- The three X marks in Row 1 are now **interface** dependencies — any can be replaced with a test double.
- All `UnityEngine` dependencies are confined to adapter rows (2, 3, 4); `FitsServiceAdapter` does not need `UnityEngine` at all (pure P/Invoke).
- The matrix remains **acyclic** — no circular dependencies in either state.

## Propagation cost (qualitative)

| Change scenario | Before | After |
|---|---|---|
| FitsReader API changes | Touches CanvassDesktop directly | Contained in FitsServiceAdapter |
| StandaloneFileBrowser updated | Touches CanvassDesktop directly | Contained in StandaloneFileDialogAdapter |
| Swap SFB for OS-native dialog | Rewrite methods inside God-class | New adapter implementing IFileDialogService |
| Unit-test IsLoadable logic | Requires Unity test runner (MonoBehaviour) | Plain xUnit / NUnit test, no Unity |
| Unit-test subset clamping | Requires Unity test runner | Plain xUnit / NUnit test, no Unity |

## CK metric projection

| Class | WMC (before) | WMC (after) | CBO (before) | CBO (after) | Target |
|---|---|---|---|---|---|
| CanvassDesktop (full class) | 63 | ~8 (composition root shell) | 47 | ~4 | WMC ≤ 40, CBO ≤ 25 |
| FileTabViewModel | — | ~12 | — | ~5 | WMC ≤ 20, CBO ≤ 14 |
| SubsetBoundsViewModel | — | ~8 | — | ~1 | WMC ≤ 20, CBO ≤ 14 |
| FitsServiceAdapter | — | ~10 | — | ~6 | WMC ≤ 40, CBO ≤ 25 |

CK baseline source: [SK_BNCH.md](../SK_BNCH.md) — CanvassDesktop as-is: CBO 47, RFC 118, WMC 63.

## Traceability

| Item | Reference |
|---|---|
| After class diagram | [after-class-diagram.puml](after-class-diagram.puml) |
| After dependency graph | [after-dependency-graph.puml](after-dependency-graph.puml) |
| Before DSM | [before-dsm.md](before-dsm.md) |
| Before class diagram | [before-class-diagram.puml](before-class-diagram.puml) |
| Before dependency graph | [before-dependency-graph.puml](before-dependency-graph.puml) |
| Skeleton code | [../../../../refactoring-examples/sub-team-6/file-tab/skeleton/](../../../../refactoring-examples/sub-team-6/file-tab/skeleton/) |
| CK baseline numbers | [SK_BNCH.md](../SK_BNCH.md) |
| Deliverables checklist | [deliverables-checklist.md](../deliverables-checklist.md) |
