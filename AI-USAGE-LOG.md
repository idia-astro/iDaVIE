# AI Usage Log — Sub-team 3: Rendering Engine (Cache Me If You Can)

> **Purpose:** Documents all AI-assisted work completed by each team member for transparency and academic integrity.
> Each member should add their own section below in the same format.

---

## Ciallian Bain

### Session 1 — iDaVIE Setup & Requirements
**Tool:** Claude (Cowork)
**Task:** Understanding how to build iDaVIE from source.
**Prompt summary:** Asked how to set up and build iDaVIE on Unity 2021, including all dependencies.
**Output:** Full build requirements list (Unity 2021.3 LTS, Visual Studio C++ workload, CMake, vcpkg, SteamVR/OpenVR). Sourced from official iDaVIE docs and GitHub BUILD.md.
**Files affected:** None (informational).

---

### Session 2 — SOLID Violation Audit
**Tool:** Claude (Cowork)
**Task:** Structured SOLID audit of `VolumeDataSetRenderer`.
**Prompt summary:** Requested a SOLID violation audit using CONTEXT.md and the dependency map as source material. Asked AI to clarify scope before starting.
**Output:** Produced a structured SOLID audit document covering all five SOLID principles with specific line-number references to violations in `VolumeDataSetRenderer`.
**Files affected:** SOLID audit document (docs/team3/).

---

### Session 3 — Architecture Diagram Syntax Fixes
**Tool:** Claude (Cowork)
**Task:** Fix PlantUML syntax errors across diagram files.
**Prompt summary:** Diagrams were failing to render due to multiple syntax errors.
**Output:** Fixed 14+ issues across `architecture.puml` — replaced Unicode dashes with ASCII `--`, escaped angle brackets (`~<`, `~>`), fixed arrow glyphs, added component aliases for multi-line names.
**Files affected:** `diagrams/architecture.puml`.

---

### Session 4 — CK Metrics Tooling & Golden Image Testing
**Tool:** Claude (Cowork)
**Task:** Two questions: (1) what is golden image regression testing and can AI produce one; (2) how to document that CK metrics tools failed.
**Prompt summary:** Asked what "golden image regression suite across mask modes and colour maps" means and whether it could be done. Also asked how to honestly document that Understand, NDepend, and SonarQube free trials failed to work with the Unity project structure.
**Output:** Explanation of golden image testing and its scope.

---

### Session 5 — CK Metrics in NDepend
**Tool:** Claude (Cowork)
**Task:** Locating CK metrics within NDepend's interface.
**Prompt summary:** Asked where to find CK metrics in an NDepend report.
**Output:** Explained where each CK metric maps in NDepend (dashboard, Queries & Rules panel, CQLinq), and provided a sample CQLinq query to extract per-type metrics directly.
**Files affected:** None (informational).

---


### Session 6 — Metrics Consistency Pass (Correct Understand Values)
**Tool:** Claude (Cowork)
**Task:** Apply verified Understand metric values consistently across all documents.
**Prompt summary:** Correct CK values (from the Understand tool run) needed to replace earlier estimates across all docs and code files.
**Output:** Updated metrics-worksheet, design-document, both refactoring example READMEs, all five `after/*.cs` files, PROGRESS.md, CONTEXT.md, and two PlantUML diagrams. Documented three classes (VolumeRenderCoordinator, VolumeTextureManager, VolumeMaterialBinder) exceeding LCOM 0.5 target as a lifecycle-phase artefact, not a design flaw.
**Files affected:** `docs/team3/metrics-worksheet.md`, `docs/team3/design-document.md`, both example READMEs, all `after/*.cs` files, `PROGRESS.md`, `CONTEXT.md`, `diagrams/class-before.puml`, `diagrams/vdsr-dependencies.puml`.

---

### Session 7 — Sprint 2 Task Triage & Document Completion 
**Tool:** Claude (Cowork)
**Task:** Multiple tasks — ClickUp task status review, document sections written, merge conflict resolved, interview study guide created.

**Prompt summary (multiple prompts in one session):**
1. Tracked ClickUp to-do list and ticked off which tasks were done vs remaining.
2. Verfied work done on "compute CK metrics" and "SOLID/GRASP annotation pass".
3. Assisted in doing various tasks assigned on kanban board.

**Output:**
- Task triage: 8 tasks confirmed complete, 4 identified as unblocked, 3 flagged as blocked on team action.
- Explanation of verification tasks: after/ code already contained full CK projection tables and SOLID/GRASP annotations inline; READMEs surfaced and summarised this evidence.
- `docs/team3/design-document.md` §6.2 and §6.3 written: Day 13 10-class projection table (all 6 CK metrics) and delta narrative (WMC 74→20, CBO cycle broken, LCOM 0.81→0 per class).
- `docs/team3/metrics-worksheet.md` populated: Day 13 projected column, before/worst/best delta table, justification prose for WMC, CBO, and LCOM.
- `PROGRESS.md` merge conflict (lines 64–112) resolved, sprint tasks marked done.

**Files affected:** `docs/team3/design-document.md`, `docs/team3/metrics-worksheet.md`, `PROGRESS.md`, both example READMEs.

---

## Cathal Ging

_Add your AI usage log here. Use the same format as above — one entry per session, with prompt summary, output description, and files affected._

---

## Chris 

### Session 1 — SonarQube Setup & Code Health
**Tool:** Claude Code
**Task:** Configure SonarQube to scan the iDaVIE project locally.
**Prompt summary:** Asked Claude to make SonarQube work on the full project so results could be viewed on localhost.
**Output:** Configured the SonarQube scanner to run against the iDaVIE codebase and display results via the local SonarQube server.
**Files affected:** SonarQube configuration files (scanner setup).

---

### Session 2 — Unit Tests for Refactoring Examples
**Tool:** Claude Code
**Task:** Write unit tests against the refactored C# code and document what is being tested.
**Prompt summary:** Asked Claude to find the refactoring examples and generate unit tests for the new refactored code, place them in the refactoring folder, and create a markdown file explaining what each test covers and why.
**Output:** Unit test file(s) for refactored classes; accompanying markdown explanation of test coverage rationale.
**Files affected:** `refactoring-examples/team3/` (new test file and markdown).

---

### Session 3 — Test Strategy Updated to Reflect New Tests
**Tool:** Claude Code
**Task:** Align `test-strategy.md` with the unit tests written in Session 3.
**Prompt summary:** Asked Claude to update the test strategy document to reflect the new tests in the refactoring examples, keeping it concise, useful, and relevant, and to note short- and long-term codebase impact.
**Output:** Revised `test-strategy.md` with updated test coverage description and impact analysis. Committed to branch.
**Files affected:** `docs/team3/test-strategy.md`.

---

### Session 4 — Revised Design Document (Section 5 Merge)
**Tool:** Claude Code
**Task:** Merge Section 5 content from the original design document into the revised design document.
**Prompt summary:** Asked Claude to read the design document, extract Section 5, and write it into the revised design document in a clear, concise style matching the existing content. Also asked Claude to check the document wasn't too long before committing.
**Output:** `revised_design_document.md` updated with Section 5 content; committed to branch.
**Files affected:** `docs/team3/revised_design_document.md` (or equivalent).

---

### Session 5 — VolumeDataSetRenderer Top-Down Explanation
**Tool:** Claude Code
**Task:** Understand the existing `VolumeDataSetRenderer.cs` class before beginning refactoring work.
**Prompt summary:** Asked for a top-down explanation of what `VolumeDataSetRenderer.cs` does, its purpose, its dependencies, and how it affects other parts of the system.
**Output:** Detailed written explanation of the class covering its role in the rendering pipeline, key dependencies (Unity, SteamVR, shader property bindings), and downstream effects on camera, masking, and texture systems.
**Files affected:** None (informational).

---

### Session 6 — Sequence Diagrams (Before & After)
**Tool:** Claude Code
**Task:** Create PlantUML sequence diagrams for one render frame, both before and after refactoring.
**Prompt summary:** Asked Claude to create a new sequence diagram using the revised design document, the previous sequence diagram file, and the refactored examples. Also asked for an initial sequence diagram based on the original `VolumeDataSetRenderer`.
**Output:** Two PlantUML sequence diagrams: one representing the original monolithic render frame flow, one showing the refactored multi-class flow.
**Files affected:** `diagrams/sequence-render-frame.puml` (new/updated), additional diagram file for before state.

---

### Session 7 — Brief Compliance Evaluation & Document Push
**Tool:** Claude Code
**Task:** Evaluate whether Sub-team 3 has met all requirements in the assignment brief; push deliverable document to GitHub.
**Prompt summary:** Provided the assignment brief PDF and asked Claude to evaluate whether Team 3's rendering engine deliverables met everything in the brief. Followed up asking Claude to re-check after a pull and then action the first identified gap.
**Output:** Compliance gap analysis against the brief. First identified gap actioned. Deliverable document pushed to GitHub.
**Files affected:** Relevant deliverable document (docs/team3/); GitHub push.

---

### Session 8 — Unity 6 Migration Plan in Test Strategy
**Tool:** Claude Code
**Task:** Ensure the Unity 6 migration plan was properly reflected in `test-strategy.md`.
**Prompt summary:** Asked Claude to check if a migration plan for Unity 6 was included in `test-strategy.md`. Confirmed adding a Section 10 covering Unity 6 migration testing after verifying it was also referenced in the design document.
**Output:** Section 10 (Unity 6 Migration Testing) added to `test-strategy.md`.
**Files affected:** `docs/team3/test-strategy.md`.

---

### Session 9 — AI Usage Log Population (current session)
**Tool:** Claude Code
**Task:** Populate the AI usage log for Chris Jo from Claude session history.
**Prompt summary:** Asked Claude to read the existing AI usage log for format reference and fill in all sessions from Claude history.
**Output:** Sessions 1–10 written into `AI-USAGE-LOG.md` under Chris Jo's section.
**Files affected:** `AI-USAGE-LOG.md`.

---

## Damien O Brien

### Session 1 — Design Document Questions (URP/HDRP & No-ops)
**Tool:** Claude (Cowork)
**Task:** Understand URP vs HDRP rendering pipelines and the concept of no-ops before working on the design document.
**Prompt summary:** Asked what the difference is between the URP and HDRP rendering pipelines, and what "no-ops" means in reference to them.
**Output:** Explanation of URP (broad compatibility, lower overhead, fewer features) vs HDRP (high-end visuals, more features, higher hardware requirements), and a definition of no-ops in the rendering context (operations that execute but have no effect on output).
**Files affected:** None (informational).

---

### Session 2 — URP/HDRP Redundant Code Identification
**Tool:** Claude (Cowork)
**Task:** Identify redundant code in the iDaVIE codebase specific to URP and HDRP render pipeline no-ops.
**Prompt summary:** Asked Claude to find bits of code that are redundant and could be removed, specifically in relation to no-ops in URP and HDRP render pipelines. Followed up by asking whether the flagged functions are used anywhere else in the codebase.
**Output:** Identified four affected files with code snippets that silently break under URP/HDRP (`OnRenderObject`, `OnRenderImage`, `Graphics.DrawProceduralNow`, `Graphics.Blit`). Confirmed via codebase search that none of the flagged calls appear elsewhere — removal is fully localised. A summary document was produced.
**Files affected:** `docs/urp_hdrp_no-ops_analysis.md` (new).

---

### Session 3 — Mask Mode Strategy Pattern (Design Doc §5.4 & IMaskMode.cs)
**Tool:** Claude (Cowork)
**Task:** Write the Mask Mode Strategy Pattern design document section and finalise `IMaskMode.cs`.
**Prompt summary:** Asked Claude to write §5.4 of the design document covering the OCP violation, Strategy pattern decision, interface design, four implementations, and pattern-choice rationale. Also asked to finalise `IMaskMode.cs` with `DisabledMaskMode` as a Null Object, and promote `ApplyMaskMode`, `InverseMaskMode`, and `IsolateMaskMode` from stubs to fully annotated implementations with XML docs, null-guards, and mutual keyword exclusion.
**Output:** `docs/design-document.md` §5.4 written. `IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`, `DisabledMaskMode.cs` finalised with SOLID/GRASP annotations and projected CK metrics.
**Files affected:** `docs/design-document.md` §5.4, `refactoring-examples/team3/example2-MaskModes/after/IMaskMode.cs`, `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`.

---

### Session 4 — VolumeMaterialBinder Draft & VolumeTextureManager Documentation
**Tool:** Claude (Cowork)
**Task:** Draft `VolumeMaterialBinder.cs` and document `VolumeTextureManager`.
**Prompt summary:** Asked Claude to draft `VolumeMaterialBinder.cs` with a `VolumeRenderState` readonly struct, a 7-member `IVolumeMaterialBinder` interface, and the sealed implementation with full SOLID/GRASP inline annotations and projected CK metrics. Also asked for documentation of `VolumeTextureManager`.
**Output:** `VolumeMaterialBinder.cs` drafted with WMC=16, CBO≤11 confirmed against targets. `VolumeTextureManager` documented.
**Files affected:** `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/VolumeMaterialBinder.cs`, `refactoring-examples/team3/example1-VolumeDataSetRenderer/after/Rationale/VolumeMaterialBinder-decisions.md`.

---

### Session 5 — Remaining Tasks Assessment & Sprint 2 Progress
**Tool:** Claude (Cowork)
**Task:** Review remaining sprint tasks and assess what was done vs. outstanding.
**Prompt summary:** Asked Claude to assess what sprint tasks were complete and what remained outstanding based on PROGRESS.md and the project files.
**Output:** Status summary of completed vs. remaining tasks, identifying unblocked work and items blocked on other sub-teams.
**Files affected:** None (informational/planning).

---

### Session 6 — Design Document Scope & Requirements Sections
**Tool:** Claude (Cowork)
**Task:** Write and revise §3 (Scope) and §4 (Requirements Recap) of the design document.
**Prompt summary:** Asked for the markdown code for §3 shortened significantly, and §4 without bullet points to condense presentation. Multiple iterations — first draft of §3 produced, then shortened; §4 written with CK metrics targets table and design standards, then condensed to remove bullet points and unnecessary information.
**Output:** §3 Scope (Sub-team 3 ownership, explicit out-of-scope items) and §4 Requirements Recap (invariants, functional requirements, CK metric targets, design standards) written in condensed prose.
**Files affected:** `docs/design-document.md` §3, §4.

---

### Session 7 — Design Document Condensed & Word Document Export
**Tool:** Claude (Cowork)
**Task:** Condense the full design document and export as a Word (.docx) file.
**Prompt summary:** Asked Claude to condense the design document (down from ~1,400 lines to ~290) keeping all tables, code interfaces, and brief requirements. Then asked for a Word document export of the condensed version.
**Output:** `docs/design-document.md` rewritten from 1,406 lines to 290. `iDaVIE-Rendering-Design-Document.docx` generated with blue headings, header/footer, and page numbers. `PROGRESS.md` updated with design document tasks marked complete.
**Files affected:** `docs/design-document.md`, `iDaVIE-Rendering-Design-Document.docx` (new), `PROGRESS.md`.

---

### Session 8 — Deliverables Review & Baseline CK Metrics Fix
**Tool:** Claude (Cowork)
**Task:** Review sprint deliverables status and fix TBD baseline CK metrics in `rendering-layer-design.md`.
**Prompt summary:** Pasted git output showing a rebase/detached HEAD situation and asked for help resolving it. Then asked "so how are we looking now in respect to the deliverables?" to get a status check. Followed up by asking Claude to fix TBD entries in `rendering-layer-design.md`.
**Output:** Deliverables status summary (design doc, refactoring examples, diagrams, metrics worksheet — all confirmed in good shape). TBD baseline CK metrics in `rendering-layer-design.md` replaced with measured Understand values (WMC=44, CBO=45, RFC=89, LCOM=~0.81, DIT=1).
**Files affected:** `docs/rendering-layer-design.md`.

---

### Session 9 — Standup Notes
**Tool:** Claude (Cowork)
**Task:** Create and format standup notes for the full project period (Weeks 1–3).
**Prompt summary:** Asked Claude to generate a 5-day standup notes template for the team across three weeks, with rotating Scrum Master roles. Followed up pasting the content back and asking to fix formatting (missing spacing between team member sections).
**Output:** `standup-notes.md` with Weeks 1–3, all four team members, rotating roles (Cathal SM Week 1, Damien SM Week 2, Ciallian SM Week 3), and blank templates for Week 3.
**Files affected:** `docs/standup notes.md`, `standup/standup-log.md`.

---

### Session 10 — Golden-Image Regression Testing Explanation
**Tool:** Claude (Cowork)
**Task:** Understand what "golden-image regression suite across mask modes and colour maps" means in the context of the test strategy.
**Prompt summary:** Asked what the phrase "Golden-image regression suite across mask modes and colour maps" means in regard to testing.
**Output:** Explanation of golden-image regression testing: comparing current output against approved baseline images to catch unintended visual regressions, applied across all mask mode and colour map combinations.
**Files affected:** None (informational).

---

### Session 11 — Unity Test Framework Setup
**Tool:** Claude (Cowork)
**Task:** Set up Unity Test Framework with Edit Mode and Play Mode tests for the refactored code.
**Prompt summary:** Asked how to implement software testing including play-mode tests under Unity Test Framework for renderer behaviour and edit-mode tests for non-Unity parts. Asked Claude to check the tests folder inside refactoring-examples. Subsequently pasted partial Unity test file setup and asked Claude to write `EditModeTests.cs` and `PlayModeTests.cs` covering mask mode shader keywords, colour map enum sanity, and play-mode material apply behaviour.
**Output:** `EditModeTests.cs` (7 tests covering `ColorMapEnumTests` and `MaskModeShaderKeywordTests`) and `PlayModeTests.cs` (play-mode tests covering apply, disable, swap, and frame-persistence behaviour). Guidance on assembly definition setup and resolving naming conflicts (`iDaVIE.Tests.EditMode`, `iDaVIE.Tests.PlayMode`).
**Files affected:** `refactoring-examples/team3/tests/EditModeTests.cs`, `refactoring-examples/team3/tests/PlayModeTests.cs`.

---

### Session 12 — CodeScene Report Guidance
**Tool:** Claude (Cowork)
**Task:** Understand how to generate a CodeScene report for the refactored code.
**Prompt summary:** Asked how to write a CodeScene report for the refactored code given an existing CodeScene account.
**Output:** Step-by-step guidance on generating an on-demand CodeScene PDF report, using Code Health scores to measure before/after refactoring impact, and triggering a fresh analysis once commits are pushed.
**Files affected:** None (informational).

---

### Session 13 — Git Workflow Support
**Tool:** Claude (Cowork)
**Task:** Git commands for pulling, branching, pushing, and resolving conflicts throughout the project.
**Prompt summary:** Multiple git-related questions across sessions: how to pull the latest branch version, how to see branches, how to discard unstaged changes, how to push to a branch, resolving a detached HEAD state during a rebase, and how to unstage/discard changes.
**Output:** Git commands provided for all scenarios. Resolved detached HEAD by running `git rebase --continue` then `git push origin HEAD:Team3-Docs-and-Examples`. Resolved push rejection by running `git pull origin Team3-Docs-and-Examples --rebase` before pushing.
**Files affected:** None (git operations only).

---

### Session 14 — Presentation Preparation
**Tool:** Claude (Cowork)
**Task:** Prepare the team for the Sprint 2 presentation/interview.
**Prompt summary:** Asked Claude to assess the project files and identify what was already done vs. still open for Sprint 3. Asked for guidance on where to start for the presentation.
**Output:** Status summary: design decisions, CK metrics deltas (WMC 97→12, LCOM 0.95→0.0, 46-file cycle broken), and identification of the four core design decisions (DD-01 to DD-04) as the likely focus. Recommended a team walkthrough of the design document.
**Files affected:** None (informational/planning).

---

### Session 15 — AI Usage Log (current session)
**Tool:** Claude (Cowork)
**Task:** Review all Claude sessions and git commits for the iDaVIE project and populate the AI usage log.
**Prompt summary:** Asked Claude to review all prompts and commits made regarding the iDaVIE project and update the existing team AI usage log with Damien's work.
**Output:** Sessions 1–15 written into `AI-USAGE-LOG.md` under Damien O Brien's section, drawn from session transcript history and git commit log.
**Files affected:** `AI-USAGE-LOG.md`.
