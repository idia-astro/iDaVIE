# WE1-2 — File Tab: BEFORE Design Structure Matrix (DSM) Excerpt

**Source:** `CanvassDesktop.cs` as-is (1 899 lines, commit `a485d6a`).
**Participants:** the five most coupled components in the File tab flow.

An **X** in cell (row, col) means the row component depends on the column component.

| | 1. CanvassDesktop | 2. VolumeCommandController | 3. StandaloneFileBrowser | 4. FitsReader | 5. UnityEngine |
|---|:---:|:---:|:---:|:---:|:---:|
| **1. CanvassDesktop** | — | X | X | X | X |
| **2. VolumeCommandController** | | — | | | |
| **3. StandaloneFileBrowser** | | | — | | |
| **4. FitsReader** | | | | — | |
| **5. UnityEngine** | | | | | — |

## Interpretation

- Row 1 has **four X marks** — CanvassDesktop is the sole fan-out node; no other participant depends on it.
- The matrix is **triangular** (no cycles shown here), but CanvassDesktop's real CBO from Understand is **47**, meaning the full DSM across all iDaVIE classes would show far more X marks in that row.
- The three `FindObjectOfType<>` calls (VolumeCommandController, VolumeInputController, HistogramHelper) are **implicit runtime dependencies** that do not appear in the static type graph — DV8 and NDepend will undercount CBO unless the runtime coupling is annotated or the skeleton code is analysed.

## Propagation cost (qualitative)

Any change to `FitsReader`, `StandaloneFileBrowser`, or the Unity platform types must be handled **inside CanvassDesktop** — there is no interface buffer. The after-state design introduces `IFileService` and `IDialogService` to break these direct links (see `after-dependency-graph.puml` when created).

## Traceability

| Item | Reference |
|---|---|
| Component diagram (same slice) | [before-dependency-graph.puml](before-dependency-graph.puml) |
| Class diagram (same slice) | [before-class-diagram.puml](before-class-diagram.puml) |
| CK baseline numbers | [SK_BNCH.md](../SK_BNCH.md) — CanvassDesktop: CBO 47, RFC 118, WMC 63 |
| Feeds | T4 modularity chapter, D11 UML diagram set |
| Deliverables checklist item | Section 1.2, "Dependency graph — transitive Unity/SteamVR/native dependencies visible" |
