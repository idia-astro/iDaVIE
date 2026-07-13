# REQ-3: Long-Term Roadmap Drivers

The following two features have been identified as high-value additions to iDaVIE beyond the current refactoring scope. Both are contingent on the structural improvements being made during this refactor, and both inform the direction of future testing outlined in T4.

## Python Console

iDaVIE currently requires all user interaction to go through its graphical interface, limiting the ability of researchers to automate or script repetitive tasks. A Python console would allow users to programmatically:

- Load datasets
- Manipulate volumes
- Trigger analyses
- Batch process data without manual GUI interaction

This would significantly improve the tool's utility in research workflows.

This feature is only viable after the current refactor is complete. The separation of business logic from Unity `MonoBehaviour` lifecycle methods — a core goal of this refactor — is a prerequisite for exposing a clean scripting API. Without modular, well-defined classes, a Python interface would have nothing stable to bind to. This driver therefore reinforces the NFRs defined in REQ-2, particularly around **Modularity** and **Modifiability**.

## Workspace Save

Currently, closing iDaVIE results in complete loss of session state — loaded volumes, annotations, camera positions, and user settings are not persisted. A workspace save feature would allow users to save and restore their full session, enabling longer and more complex research workflows across multiple sessions.

This feature requires the system to have clean, serialisable state objects — something only achievable once tightly coupled and oversized classes have been broken down through refactoring. It will also require a defined data schema for saved files, which T4 must account for in future data integrity and regression test cases.
