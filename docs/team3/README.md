# Sub-team 3: Rendering Engine — Document Index
**Team Alpha | Cache Me If You Can**
*Artefact freeze: Thu 4 June 2026 11:00*

This file maps every document in this folder to the brief section that requires it.

---

## Assessed Deliverables (`deliverables/`)

| File | Brief reference | Status |
|---|---|---|
| `deliverables/requirements.md` | §9.2 Deliverable 1 — Sub-team requirements (1–2 pages) | ✅ |
| `deliverables/design-document.md` | §9.2 Deliverable 2 — Sub-team design document (5–10 pages) + §6.3 Rendering layer design doc | ✅ |
| `deliverables/shader-asset-policy.md` | §6.3 — Shader/asset organisation policy for Unity 6 | ✅ |
| `deliverables/metrics-worksheet.md` | §6.3 — Before/after CK metrics worksheet | ✅ |
| `deliverables/test-strategy.md` | §9.2 Deliverable 4 — Sub-team test strategy (2–4 pages) | ✅ |

Kanban snapshots (§9.2 Deliverable 5) → `kanban/sprint1-snapshot.md`, `sprint2-snapshot.md`, `sprint3-snapshot.md`

Daily stand-up notes (§9.2 Deliverable 6) → `standup/standup-log.md`

Worked refactoring examples (§9.2 Deliverable 3) → `refactoring-examples/team3/`

Diagrams (PlantUML/Mermaid) → `diagrams/`

---

## Cross-team Integration Notes (`integration/`)

| File | Contents |
|---|---|
| `integration/meeting-subteam2.md` | `RawVolumeData` texture format contract (confirmed 2 June 2026) |
| `integration/meeting-subteam4.md` | `IGaze` interface contract (confirmed 2 June 2026) |
| `integration/meeting-subteam7.md` | `ISessionPersistenceService` / `VolumeSessionState` contract (pending sign-off) |

---

## Background Research (`exploration/`)

Not directly assessed, but cited as evidence in the deliverables above.

| File | Purpose |
|---|---|
| `SOLID_GRASP_Violations.md` | Sprint 1 violation audit — source for design-document §8.1 |
| `RenderFrame_CallSequence(1).md` | 9-step render frame call sequence |
| `RenderingAdjacentClasses.md` | Classes adjacent to `VolumeDataSetRenderer` |
| `Unity5_BuiltInRP_API_Catalogue.md` | 5 Unity 5 API touchpoints requiring SRP migration |
| `VolumeDataSetRendererExplanation.md` | Plain-language walkthrough of the God Class |
| `VolumeRender.md` | Shader-level rendering notes |
| `ColourMap_Annotation.md` | Colour map shader annotation |
| `MaskMode_Annotation.md` | Mask mode annotation |
| `migration_plan.md` | Full 15-step Unity 6 migration sequence |
| `testing-tools-guide.md` | Setup notes for SonarQube, Understand, NDepend, CodeScene, DV8 |
| `urp_hdrp_no-ops_analysis.md` | URP/HDRP no-op analysis |
| `VDSR_DependencyMap.md` | Class-level dependency map of `VolumeDataSetRenderer` |
| `FourClassSplit_RoughNotes.md` | Early design notes for the four-class split |
