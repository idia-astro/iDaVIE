# Week 2 — Tech Lead Role Explainer (Con Kirby)

**Purpose:** internal study aid documenting the **Tech Lead** work Con Kirby owned during **Sprint 2 (Week 2, Mon 25 – Fri 29 May 2026, Days 6–10)**, focused on the **File-tab worked example**, with the architecture/design and requirements work that fed it. It captures *what* was done, *why* each technical decision was taken, and *how* the File-tab example was driven to completion — so the work can be defended to the iDaVIE panel.

> **Role note.** Per the standard rotation (§10.1), Con moved from Scrum Master (Sprint 1) to **Sub-team 6 Tech Lead** for Sprint 2. TL responsibilities: technical decisions within the sub-team; attend the **09:15 cross-sub-team stand-up**; **sit on the Architecture Guild** (the 7 sub-team TLs, chaired by the Integration Lead — daily at 09:15, plus the weekly 1-hour review on Wednesdays). See [`week1-scrum-master-explainer.md`](week1-scrum-master-explainer.md) for the Sprint 1 hat.

> **AI-policy note.** This explainer was AI-assisted (reconstructing the role narrative from our own `week2-sprint-review.md`, `standups.md`, the File-tab artefacts under `D4-worked-examples/ex1-file-tab/`, `architecture.md`, and `requirements.md`) and is logged in `ai-log.md`. It is a **process/role study aid, not an individual reflection, peer-rating, or contribution log** — those remain human-authored per the brief, and AI may **not** be used for the live pitch/interview defence. Everything below must be defensible by Con on his own.

**Companion to:** `deliverables/D4-worked-examples/ex1-file-tab/`, `refactoring-examples/sub-team-6/file-tab/skeleton/`, `D2-Architecture/architecture.md`, and `D1-requirements/requirements.md`.

---

## 1. Focus: the File-tab worked example

Sprint 2's goal was *"produce the design proposal in full."* As TL, Con's primary technical job was to take the **File tab** — worked example 1, mandated by §6.6 — from a set of Sprint-1 diagrams to a complete, defensible before/after example, and to make sure the supporting docs and the gateway contract it depends on actually lined up with it.

The File tab is the right anchor for the proposal: it exercises the **request/response** path of the transport contract (open an image → describe it → load it), so it proves the whole MVVM → ViewModel → service-gateway chain end to end.

**File-tab artefact set (state at Sprint-2 boundary):**

| Artefact | What it is | Status |
|---|---|---|
| `before-class-diagram.puml` | God-class structure of `CanvassDesktop` (file-tab slice) | Complete (Sprint 1) |
| `after-class-diagram.puml` | MVVM target: `FileTabView` → `IFileTabViewModel` (+ `SubsetBoundsViewModel`, DTO records) → `IFitsService` / `IFileDialogService` / `IVolumeService` → adapters | Complete (Sprint 1) |
| `before-dependency-graph.puml` / `after-dependency-graph.puml` | Dependency fan-out before vs after (CBO 47 → ~4–5) | Complete |
| `before-dsm.md` / `after-dsm.md` | DSM + CK projection | Complete |
| **`file-tab-sequence-diagram.puml`** | **WE1-4 — Con's Sprint-2 deliverable** | **Complete (Sprint 2)** |
| `ck-delta-worksheet.md` | WE1-6 — CK before/after delta | Complete (Sprint 2) |
| `skeleton/` (10 `.cs` files) | Interfaces + ViewModel + DTO stubs, compile under `dotnet build` with no Unity | Complete (Sprint 1) |

---

## 2. What Con did on the File tab (Sprint 2)

1. **Brought the File-tab example into scope.** Tightened the worked example so it reflects our actual slice and updated its supporting documentation to match (per `standups.md` Day 7). This removed drift between the diagrams, the skeleton, and the gateway interfaces.
2. **Authored the File-tab sequence diagram (WE1-4).** Models the open-image happy path:
   `FileTabView` → `IFileTabViewModel.BrowseImageCommand` → `IFileDialogService.PickFileAsync` → `IFitsService.OpenAndDescribeAsync` → `IVolumeService.LoadAsync` → property-change notifications back to the view. The **error path** (`IFitsService` throws `FitsException`) is shown as an `alt` fragment: `ErrorMessage` set, `LoadingState` → `Error`.
3. **Walked the sequence/phasing documentation and reviewed the worked examples** (Day 7–8) so the sequence diagram, class diagram, and skeleton tell one consistent story.
4. **Confirmed the CK delta headline** for the File-tab class: `CanvassDesktop` full class → composition-root shell **WMC 63 → ~8, CBO 47 → ~4, RFC 118 → ~18, LCOM 0.955 → ~0.12**, all within §7.1 thresholds; `FileTabViewModel` projected at WMC ~12 / CBO ~5.

---

## 3. Architecture & design (a tad)

The File tab only works as an example because of three architecture decisions Con owned/finalised as TL — each shown here only insofar as it touches the file-open path:

- **MVVM split (ADR-0001) applied to the File tab.** `FileTabView` (Unity `MonoBehaviour`) holds no logic; `FileTabViewModel` is pure C# behind `IFileTabViewModel`; all I/O sits behind `IFitsService` / `IFileDialogService` / `IVolumeService`. The ViewModel has **zero `UnityEngine` dependency** — which is what makes the File-tab ViewModel unit-testable without the Editor.
- **ADR-0002 wire questions resolved for the open path.** Three open questions on the transport contract were closed during Sprint 2, all of which the File tab depends on:
  - *Handle vs path across the pipe →* **path string** (simplest correct v1; no server-side handle lifecycle).
  - *Header caching →* **`FitsService` owns the cache** (client stays stateless on FITS metadata).
  - *Memory-check ownership →* **client-side guard retained in the adapter** (the memory probe stays in the ACL adapter, not the ViewModel, so the domain layer keeps no platform dependency).
- **`IUIDispatcher` seam.** The two-method contract (`RunOnUI`, `RunOnUIAsync`) is the single point where the File-tab ViewModel marshals async load results back to the UI thread without referencing UnityEngine.

---

## 4. Requirements (a tad)

The File-tab example traces straight back to the requirements work:

- The **"I/O That Belongs Server-Side"** map in `D1-requirements/CanvassDesktop.md` enumerates every server-side I/O site in the file — including `_browseImageFile`, `UpdateHeaderFromFits`, and `ChangeHduSelection` — and names the target gateway interface for each (`IFitsService.OpenAndDescribe`, etc.). That map *is* the spec the File-tab after-diagram implements.
- Con contributed to the **requirements document** during Sprint 2 (Days 8–9), cross-checking that the File-tab interfaces and the NFRs line up — in particular **REU-3** (zero transitive `UnityEngine`/`SteamVR` in the ViewModel) and **TST-1/TST-2** (the File-tab ViewModel testable without Unity), which the File-tab example is the concrete evidence for.

---

## 5. Decisions made (and owned) as TL

| Decision | Rationale |
|---|---|
| **Anchor the proposal on the File tab as the request/response example** | Proves the MVVM → gateway chain end to end on the open-image path. |
| **Path string over handle across the pipe** | Simplest correct v1 wire contract; defers handle-lifecycle complexity. |
| **Memory check stays in the ACL adapter, not the ViewModel** | Keeps the File-tab ViewModel free of any platform/probe dependency (NFR-REU-3). |
| **Show the error path explicitly in the sequence diagram** | A worked example that only shows the happy path isn't defensible; the `FitsException` `alt` fragment shows the failure contract. |
| **Bring the example into scope before adding to it** | Aligning diagrams, skeleton, and interfaces first prevents a polished-but-inconsistent example. |

---

## 6. Likely panel questions (self-test)

- "Walk me through what happens when a user opens an image." → §2: `BrowseImageCommand` → `IFileDialogService.PickFileAsync` → `IFitsService.OpenAndDescribeAsync` → `IVolumeService.LoadAsync` → property-change back to the view; error → `FitsException` `alt` path.
- "How is the File-tab ViewModel kept free of Unity?" → §3: ViewModel behind `IFileTabViewModel`, all I/O behind service interfaces, UI-thread marshalling via the `IUIDispatcher` seam — no `UnityEngine` reference.
- "Why path-string, not a handle, across the pipe?" → §3: simplest correct v1; avoids server-side handle lifecycle.
- "What's the headline CK improvement for the File tab?" → §2: CanvassDesktop full class WMC 63 → ~8, CBO 47 → ~4, RFC 118 → ~18, LCOM 0.955 → ~0.12.
- "Where do the File-tab interfaces come from?" → §4: the server-side I/O map in `CanvassDesktop.md`, traced to NFRs REU-3 / TST-1 / TST-2.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal role explainer for Con Kirby's Week 2 (Tech Lead) File-tab work. Not a deliverable itself, and not a substitute for the human-authored individual reflection / contribution log.*
