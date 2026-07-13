# Sub-team 4 — Interaction System — AI tool usage log

See [`README.md`](README.md) for schema. Newest entries on top.

---


2026-05-27 Interface contract documents for cross-team dependencies

Author: Liang Chen Yu (Sprint 2 — PO Liaison)

Tool: Claude

Model: Sonnet 4.6

Where used: Interface contract messages sent to Sub-teams 1, 3, 5, and 7 (Architecture, Rendering, Feature System, Persistence)
Prompt summary: Asked Claude to draft tailored interface contract messages for each dependency direction, covering what each sub-team needed from Koffiewinkel and what Koffiewinkel needed in return; also requested an InteractionSystemState schema for the Persistence sub-team including fields, version, and fallback behaviour.

Where it helped: Produced four separate tailored contracts and drafted the InteractionSystemState schema with version field, fallback behaviour, and initial field list; saved roughly 45 minutes of drafting time across all four contracts.

Where it failed / was wrong: Consistently used wrong sub-team numbers throughout labelled sub-teams by spec-assigned number (1–7) rather than the internal sub-team identities used within Team Alpha (e.g. confused Sub-team 13 Rendering with a different numbering).

Human reviewer: Corrected all sub-team numbers against spec before sending; reviewed the state contract schema and added missing paint sub-state fields (brush size, active source ID, mask mode) before the contracts were sent.


2026-05-27 — NUnit unit test generation for all three worked examples

Author: Arnav (Sprint 2 — Quality Champion)
Tool: Claude
Model: Sonnet 4.6

Where used: refactoring-examples/sub-team-4/ — GazeProviderTests.cs, CollaboratorTests.cs, VoiceCommandTests.cs

Prompt summary: Asked Claude to generate NUnit unit test prompts for Cursor with exact class names, method signatures, and assert conditions for all three test files across both worked examples.

Where it helped: Produced all three test files with correct class names, method signatures, and assert conditions — directly usable as the unit test evidence in the worked examples.

Where it failed / was wrong: Generated a test asserting GazeRay.direction which became invalid after IGaze was updated to remove GazeRay and replace it with GazeFocusPoint — the test did not match the final interface.

2026-05-26 — VolumeInputController decomposition into collaborator classes

Author: Arnav (Sprint 2 — Quality Champion)
Tool: Cursor (Claude backend)
Model: Sonnet 4.6

Where used: refactoring-examples/sub-team-4/2-volume-input/ — LocomotionController.cs, InteractionController.cs, BrushController.cs, VignetteController.cs, CursorInfoFormatter.cs, QuickMenuPositioner.cs

Prompt summary: Asked Cursor to decompose VolumeInputController into six focused collaborator classes using a detailed extraction prompt specifying responsibilities, interface names, and the delegation wiring pattern.

Where it helped: Produced correct interface structure, delegation pattern, and BuildCollaborators() wiring for all six classes; VolumeInputController was reduced from a god class to a thin router.

Where it failed / was wrong: InteractionController was generated with a 16-parameter constructor (telescope constructor smell); IBrushController had 10 public members, exceeding the ISP limit of 7.

Human reviewer: Arnav — manually restructured parameter passing using Action and Func delegates to eliminate the telescope constructor and avoid circular references; documented the IBrushController trade-off explicitly in the worked example.



2026-05-26 — VolumeInputController refactor to SteamVR router

Author: Colin Forde (Sprint 2 — Tech Lead)
Tool: Cursor
Model: Agent (Composer)

Where used: Assets/Scripts/VolumeData/VolumeInputController.cs, Assets/Scripts/Interaction/ (all collaborator files)

Prompt summary: Asked Cursor to refactor VolumeInputController to a SteamVR router only, extracting locomotion, interaction, brush, vignette, cursor, menu, and gaze into separate collaborators under Assets/Scripts/Interaction/.

Where it helped: Created the full Interaction/ folder with ~631-line router, all 7 interfaces, and all collaborator implementations in one session.

Where it failed / was wrong: Editor sometimes showed the old 1635-line buffer rather than the updated file on disk, causing apparent regressions mid-session.

Human reviewer: Colin Forde reloaded each file from disk after generation; verified the project compiled in Unity before committing.

2026-05-26 — State pattern applied to LocomotionState for Worked Example 1

Author: Shea (Sprint 2 — Scrum Master)
Tool: Claude
Model: Sonnet 4.6

Where used: Worked Example 1 — refactoring-examples/sub-team-4/2-volume-input/, UML state machine diagram

Prompt summary: Asked Claude to explain why the current enum switch is not a real State pattern and produce the ILocomotionState interface and C# skeletons for IdleState, MovingState, and ScalingState.

Where it helped: Explained the distinction between an enum switch and a formal State pattern, produced the ILocomotionState interface definition, and gave correct C# skeletons for all three concrete state classes — directly usable as the Worked Example 1 code skeleton.

Where it failed / was wrong: None, output was accurate and usable without correction.

Human reviewer: Shea reviewed all three state class skeletons against the existing LocomotionState enum transitions in the source to confirm all six states and entry/exit actions were covered.

2026-05-25 — Voice command refactor implementation

Author: Colin Forde (Sprint 2 — Tech Lead)
Tool: Cursor
Model: Agent (Composer)

Where used: Assets/Scripts/VolumeData/Voice/ (all files), Assets/Scripts/VolumeData/VolumeCommandController.cs

Prompt summary: Asked Cursor to refactor voice commands using the Command pattern and IVoiceRecogniser interface with minimal changes outside the interaction/voice scope.

Where it helped: Created the full Voice/ package (IVoiceCommand, IVoiceRecogniser, WindowsVoiceRecogniser, VoiceCommandRegistry, VoiceCommandContext, DelegateVoiceCommand, VoiceDesktopUiSync) and thinned VolumeCommandController to an orchestrator.

Where it failed / was wrong: Generated a static VoiceCommandRegistry API which conflicted with the test spec expecting an instance-based Register/Lookup pattern.

Human reviewer: Colin Forde — kept the static production API; added a test-local registry class inside the test file to bridge the mismatch without changing production code.


2026-05-20 CK metrics interpretation for god class argument

Author: Liang Chen Yu (Sprint 1 — Quality Champion)
Tool: Claude
Model: Sonnet 4.6

Where used: Codebase Analysis Report — Section 5.1 (P-01 God Class), docs/sub-team-4/

Prompt summary: Asked Claude to explain WMC=79, CBO=31, RFC=79 in plain language and contextualise why all three breach the spec thresholds (WMC ≤ 20 domain, CBO ≤ 14 domain, RFC ≤ 50) for the god class argument in the Sprint 1 analysis report.

Where it helped: Produced clear plain-language explanations of all three metrics and articulated why the combined breach of all three simultaneously strengthens the god class argument; output was used directly to frame Section 5.1 of the report.

Where it failed / was wrong: Could not independently verify the numbers — worked only from values reported in the prompt rather than running its own static analysis on the source files.

Human reviewer: Cross-checked all quoted CK numbers against the Understand tool output and the Tech Lead's Day 2 baseline document before inclusion in the report.


2026-05-19 — AI tool usage log template generation

Author: Liang Chen Yu (Sprint 1 — Quality Champion)
Tool: Claude
Model: Opus 4.7

Where used: AI Tool Usage Log document (initial template)

Prompt summary: Asked Claude to create a template for the team's AI tool usage log with six columns: tool, model, prompt class, where it helped, where it failed, and what the human did instead.

Where it helped: Produced a Word document template with all six requested columns, saving approximately 30 minutes of manual formatting.

Where it failed / was wrong: Chose .docx format without being asked; added 14 empty placeholder rows and extra boilerplate entries that were not requested.

Human reviewer: Liang Chen Yu reviewed the output, removed all unrequested rows and entries, and modified the template to match the sub-team's actual needs and the repo schema.


2026-05-19 — Initial repo scope mapping

Author: Colin Forde (Sprint 1 — Scrum Master)
Tool: Cursor
Model: Agent (Composer)

Where used: Sprint 1 codebase exploration — team scope mapping

Prompt summary: Asked Cursor to map the repo with focus on the interaction system: VolumeInputController, menus, voice, VR keyboard, and links to rendering.

Where it helped: Mapped the interaction system scope across all relevant files and identified cross-team rendering dependencies used in the dependency table.

Where it failed / was wrong: Could not replace reading the SteamVR input bindings directly in the Unity scene — not visible from source files alone.

Human reviewer: Colin Forde traced SteamVR references in the Unity project and cross-checked against brief §6.4 before finalising the scope map.



