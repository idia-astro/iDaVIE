# Sub-team 6 — Desktop Client Architecture (5–10 pp)

**Status:** stub — ARCH-10 in the backlog. Final by Day 10 (Fri 29 May 2026).

## 1. Context

One-paragraph statement of the desktop client's role in the iDaVIE client–server + micro-kernel architecture (§4.1).

## 2. Architectural drivers

Reference `requirements.md` §3 NFR table. Cite the four ISO 25010 sub-characteristics explicitly.

## 3. C4 model

- **C4 Level 1 — Context** (ARCH-3): _(PlantUML in `diagrams/c4-context.puml`)_
- **C4 Level 2 — Container** (ARCH-4): _(PlantUML in `diagrams/c4-container.puml`)_
- **C4 Level 3 — Component** (ARCH-5): _(PlantUML in `diagrams/c4-component.puml`)_

## 4. Architecture decisions

See [`adrs/`](adrs/). At minimum:

- ADR-0001 MVVM split (View / ViewModel / Service Gateway).
- ADR-0002 Transport — JSON-RPC over named pipes (local), gRPC (future remote).
- ADR-0003 Anti-corruption layer around Unity 6 APIs.
- ADR-0004 Unity 6 UI Toolkit as View technology + migration plan from legacy Canvas.

## 5. MVVM binding policy

_(ARCH-9 — 1–2 pp. Cover: where data flows from, how commands flow back, how UI Toolkit data binding is configured, what is forbidden in the View, what is forbidden in the ViewModel.)_

## 6. Interface contracts

See [`../../refactoring-examples/sub-team-6/contracts/`](../../refactoring-examples/sub-team-6/contracts/) for C# headers:

- `IServiceGateway` — talks to server kernel.
- `IFileTabViewModel`, `IDebugTabViewModel` — bound by View.
- `ILogStream`, `ILogObserver` — Debug tab Observer surface.
- `IPanel` — composable panel contract (replaces God-canvas).

## 7. SRP audit — CanvassDesktop concerns separated

_(DESN-2 — one diagram + half-page commentary showing menu structure / panel state / file dialogs / configuration in distinct units.)_

## 8. State contract to Sub-team 7 (Persistence)

_(ARCH-11. Schema of desktop shell state: open panels, file history, log filter, layout. Delivered by Day 9.)_

## 9. Unity 5 → Unity 6 migration plan

_(ARCH-7. Mapping of every Canvas-based panel to UI Toolkit equivalents; phased migration steps.)_

## 10. Compliance check vs mandatory constraints (§4.2)

| Constraint | Compliance |
|---|---|
| 1 — No SOLID/GRASP violations | _(SOLID + GRASP audit reference)_ |
| 2 — No circular dependencies | _(NDepend cycle report)_ |
| 3 — Domain code has no UnityEngine/SteamVR dep | _(ViewModel assembly references)_ |
| 4 — Every public API has interface + test double | _(coverage map)_ |
| 5 — Plug-in ABI versioned | _(out of scope — owned by Sub-team 1)_ |
