# Week 1 — Scrum Master Role Explainer (Con Kirby)

**Purpose:** internal study aid documenting the **Scrum Master** work Con Kirby owned during **Sprint 1 (Week 1, Mon 18 – Fri 22 May 2026, Days 1–5)**. It captures *what* the role covered, *why* the ceremonies were run the way they were, *how* the board and cadence were set up, and the *decisions* and *impediments* handled — so the work can be defended to the iDaVIE panel.

> **Scope note (two hats).** Con held the Scrum Master role at **two levels** this sprint: (1) **Sub-team 6** Scrum Master (09:00 sub-team stand-up, sub-team Kanban, impediments — per the Sprint 1 role table) and (2) **overall Team Alpha** Scrum Master, which added the **09:15 Scrum of Scrums** (cross-sub-team stand-up), and team-wide sprint review + retrospective facilitation. Both hats are documented below and kept distinct where they differ.

> **AI-policy note.** This explainer was AI-assisted (reconstructing the role narrative from our own `week1-sprint-review.md`, `week1-sprint-retro.md`, `week1-kanban-snapshot.md`, `standups.md`, and `role-log.md`) and is logged in `ai-log.md`. It is a **process/role study aid, not an individual reflection, peer-rating, or contribution log** — those remain human-authored per the brief, and AI may **not** be used for the live pitch/interview defence. Everything below must be defensible by Con on his own.

**Companion to:** `deliverables/Sprint-Documents/week1-sprint-review.md`, `week1-sprint-retro.md`, `week1-kanban-snapshot.md`, `week1-snapshot.png`, `standups.md`, and `archived/role-log.md`.

---

## 1. What the role covered (the inventory)

| Hat | Responsibility | Evidence |
|---|---|---|
| **Sub-team 6 SM** | 09:00 daily stand-up (facilitation + notes) | `standups.md` Days 2–5 |
| **Sub-team 6 SM** | Sub-team Kanban board (ClickUp) set up + maintained | `week1-snapshot.png`, board *"Team Space / Team 6 - GUI - week 1"* |
| **Sub-team 6 SM** | Sprint 1 role assignments documented | `role-log.md` §"Roles — Sprint 1" |
| **Sub-team 6 SM** | Impediment / dependency tracking | DEPS-1, DEPS-2 cards |
| **Sub-team 6 SM** | Sprint 1 review slides (15-min slot, Day 5) | `week1-sprint-review.md` |
| **Sub-team 6 SM** | Sprint 1 retro facilitation + notes | `week1-sprint-retro.md` |
| **Team Alpha SM** | **ClickUp set up for *all* sub-teams** (team-wide Kanban) | ClickUp Team Space (per Day 3 stand-up entry) |
| **Team Alpha SM** | **09:15 Scrum of Scrums** (cross-sub-team stand-up) | `standups.md` "Raised at 09:15 cross-sub-team stand-up" rows |
| **Team Alpha SM** | Team-wide sprint review + retro facilitation | Day 5 stand-up entry ("Conducted sprint review and retro") |

Backlog cards owned as SM this sprint: **MGMT-1** (Kanban set up), **MGMT-2** (standups file), **MGMT-3** (role assignments), **MGMT-4** (review slides), **MGMT-5** (snapshot + retro), **DEPS-1** (gateway dependency raised), **DEPS-2** (VR-menu coordination raised).

---

## 2. Why the ceremonies were run this way

**Core argument:** the Scrum Master job in Week 1 was to *stand up the process scaffolding for a 27-person, 7-sub-team cohort from a cold start* and protect a diagnosis-focused sprint from coordination overhead. Each ceremony maps to a concrete need.

- **One ClickUp Team Space for all sub-teams, one board per sub-team.** Setting up the cohort-wide board (not just ours) was the Team Alpha SM duty. A single shared tool means the Scrum of Scrums can read every sub-team's board at 09:15 without tool-hopping, and end-of-sprint Kanban snapshots (a §9.2.6 deliverable) come straight from the board.
- **Two-tier stand-up (09:00 then 09:15).** 09:00 sub-team stand-up surfaces local blockers fast; 09:15 Scrum of Scrums escalates only the cross-team items (dependencies on Sub-team 1's gateway contract, Sub-team 4's VR menus) so 27 people are never in one stand-up. This is the standard Scrum-of-Scrums scaling pattern.
- **Diagnosis-first sprint goal.** Sprint 1's committed goal was *"diagnose the current state"* — baseline numbers, requirements draft, two ADRs, gateway contract into Sub-team 1's hands. The SM's job was to keep the board reflecting that goal and resist pulling implementation work early (capacity deliberately held at ~75 %, 21 cards).
- **Impediments raised as cards, not chat.** DEPS-1 (gateway dependency on Sub-team 1) and DEPS-2 (VR-menu coordination with Sub-team 4) were logged as tracked cards and onto the integration risk register (R01, R04) so they survive past the stand-up they were mentioned in.

---

## 3. How the work was done (mechanics)

**ClickUp / Kanban set-up (Day 2–3):**
1. Created the Team Alpha **Team Space** in ClickUp and a board per sub-team (ours: *"Team 6 - GUI - week 1"*).
2. Columns: **To Do (ZU ERLEDIGEN) · In Progress (IN BEARBEITUNG) · Dependency · Done (VOLLSTÄNDIG)** — the explicit **Dependency** column is what makes cross-team blockers visible on the board rather than buried in To Do.
3. Imported the Sprint 1 backlog cards; tagged owners (CK/M/J/RH) and sizes (S/M).

**Stand-up cadence:**
- 09:00 sub-team stand-up, three questions (yesterday / today / blockers), notes into `standups.md`.
- 09:15 Scrum of Scrums: carried sub-team status + blockers up; brought cross-team decisions back down.

**Sprint 1 review (Day 5, 15-min slot):**
- Produced `week1-sprint-review.md` as a **file-by-file walkthrough** of the deliverables tree — every artefact, what's in it, how complete. Headline slide numbers pulled out (CanvassDesktop WMC 63 / CBO 47 / RFC 118 / LCOM 0.955; silent `UpdateMaxValue` bug; 205 mocking call-sites).

**Sprint 1 retro (Day 5, 1 h sub-team + 30-min cross-team):**
- Facilitated **Start / Stop / Continue + action items**. Captured 8 action items (A1–A8) with owners and due-days, and confirmed the Sprint 2 role rotation (Con → Tech Lead).

---

## 4. Sprint 1 outcome the SM is accountable for

| Metric | Value |
|---|---|
| Cards pulled | 21 (~75 % capacity, by design) |
| Cards done | 18 |
| Cards partial | 1 (ARCH-8 — interface contracts; hand-off owed Day 8) |
| Cards carried to Sprint 2 | 2 (BNCH-3 CodeScene, BNCH-4 NDepend DSM) |
| Sprint-2 cards pulled forward | 11 |

All `backlog.md` Part B exit criteria met. Snapshot captured (`week1-snapshot.png`); review and retro delivered.

---

## 5. Decisions made (and owned) as SM

| Decision | Rationale |
|---|---|
| **Hold capacity at ~75 % (21 cards)** | Sprint 1 is a cold-start diagnosis sprint; under-committing protected against tooling-setup churn (SonarQube/CodeScene/NDepend all came up mid-sprint). |
| **Single ClickUp Team Space, board-per-sub-team** | Lets the Scrum of Scrums read all boards in one tool; feeds the §9.2.6 snapshot deliverable directly. |
| **Explicit "Dependency" column** | Makes cross-team blockers first-class on the board, not hidden in To Do. |
| **Log impediments as cards (DEPS-1/2) + onto risk register** | Ensures cross-team risks (gateway, VR menus) persist beyond the stand-up. |
| **Carry BNCH-3 / BNCH-4 rather than force them** | QC workload was front-loaded on CK + SonarQube baselines; carrying two benchmark tasks was the honest call, recorded with root cause in the retro. |

---

## 6. Impediments / risks raised this sprint

1. **Sub-team 1 gateway contract not finalised** (DEPS-1 / R01) — our skeletons use our own drafted interfaces; rework risk if their surface differs. Mitigation: integration review Day 8.
2. **Sub-team 4 VR-menu coordination** (DEPS-2 / R04) — raised on the risk register; no active design conversation yet.
3. **`concern-map.png` binary-only** — violates §10.4; `.puml`/`.mmd` companion owed.
4. **Day-5 stand-ups thinly recorded** (Jimmy + Rory missing "Today") — fix: enforce 09:00 cadence in Sprint 2 (became action item A6).
5. **CodeScene + NDepend tail items** — carried to Sprint 2 (A1, A2).

---

## 7. Likely panel questions (self-test)

- "You were SM at two levels — what did the *team-wide* hat add over the sub-team one?" → §1: the cohort-wide ClickUp set-up, the 09:15 Scrum of Scrums, and team-wide review/retro facilitation, on top of the sub-team stand-up/board/impediments.
- "Why only 21 cards in Sprint 1?" → §5: deliberate ~75 % capacity for a cold-start diagnosis sprint.
- "Two tasks were carried — is that a process failure?" → §4/§5: planned honesty, not slip; root cause recorded (QC front-loaded on baselines), both cleared Days 6–7 of Sprint 2.
- "How do cross-team dependencies not get lost?" → §3/§6: explicit Dependency column + DEPS cards + risk register, escalated at the 09:15 Scrum of Scrums.
- "Show the Week-1 process deliverables." → `week1-snapshot.png` (Kanban snapshot), `week1-sprint-review.md`, `week1-sprint-retro.md`, `standups.md`.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal role explainer for Con Kirby's Week 1 (Scrum Master) work. Not a deliverable itself, and not a substitute for the human-authored individual reflection / contribution log.*
