# Team Alpha — Scrum of Scrums (09:15 cross-sub-team stand-up)

**What this is:** the cohort-level daily sync that runs straight after each sub-team's 09:00 stand-up. One representative per sub-team (usually the SM) brings up only the things that cross a team boundary — shared contracts, blockers on another team, tooling that affects everyone. Local in-team work stays in the 09:00 stand-up.

**Facilitator (Sprint 1 / Week 1):** Con Kirby (Team Alpha Scrum Master).
**Cadence:** 09:15–09:30, Mon–Fri.

---

## Day 1 — Mon 18 May 2026 (kickoff)

- Cohort kickoff. Walked the whole brief together; confirmed the 7 work-package split and which sub-team owns what.
- Agreed the two-tier stand-up: 09:00 in-team, 09:15 Scrum of Scrums.
- SM (Con) to set up a shared ClickUp Team Space + a board per sub-team.
- Quality Guild to publish metric-tooling setup notes (SonarQube / Understand / NDepend / CodeScene / DV8) by EOD so everyone can confirm Day 2.

**Cross-team items opened:** target architecture is shared (client–server + micro-kernel + layered + plug-in, ACL around Unity) — every team codes to the same contracts, so the gateway and ABI contracts are the things to watch.

---

## Day 2 — Tue 19 May 2026

**Round the room:**
- **Sub-team 1 (Architecture):** owns the service-gateway contract; will draft an `IServiceGateway` interface stub + JSON-RPC schema. Confirmed as cohort Integration Lead.
- **Sub-team 6 (us):** UI rendering up and working; starting the CanvassDesktop concern map + File-tab diagrams. Flagged we're blocked on Sub-team 1's gateway contract for the ViewModel layer.
- **Quality Guild:** tooling notes out; teams to confirm their tools work Day 2.
- Others: getting systems building, reading into their slices.

**Decisions / actions:**
- **Seeded the Integration Risk Register** (R01–R07 + DEPS-1). Gateway-contract drift (R01) and ACL leakage (R05) logged as High.
- **Risk register handed to Sub-team 1 (Integration Lead) at EOD** — they own it from Day 3; SM keeps a read-only watch and raises new risks at this stand-up.
- Con to finish the ClickUp boards.

---

## Day 3 — Wed 20 May 2026

**Round the room**
- **Con (SM):** ClickUp configured for all sub-teams; sprint boards up. Sprint plans loaded.
- **Sub-team 1:** gateway interface stub + JSON-RPC schema being drafted (R01 mitigation — draft by Day 3, freeze v0.1 by Day 5).
- **Sub-team 6:** ADRs being written (MVVM split, transport).
- **Sub-team 4 (Interaction):** raised against us — VR vs Desktop menu vocabulary must not diverge (R04); agree a shared command catalogue by Day 4.
- **Sub-team 2 / 5 / 6:** state-ownership ambiguity (R03) — who persists what; Sub-team 2 to write a state-ownership table by Day 4.

**Decisions / actions:**
- Sub-team 6 proceeds against its own gateway stub; Sub-team 1 ratifies or replaces.
- R03, R04 mitigations to land Day 4.

---

## Day 4 — Thu 21 May 2026

**Round the room:**
- **Quality Guild:** metric tools coming online — CodeScene + NDepend reported working on our side (Rory). CK + SonarQube baselines in progress (R06 closing).
- **Sub-team 1:** gateway contract progressing toward the Day-5 v0.1 freeze.
- **Sub-team 6:** before-state UML + CK baselines underway; refactor planning continuing.
- **Sub-team 4 ↔ 6:** command-catalogue conversation flagged again — still needs a real working session (carried, no active design conversation yet).
- **Sub-team 7:** plug-in C ABI (R02) — ABI version header + CI exports check to be set up.

**Decisions / actions:**
- Each team to have a Day-2→Day-5 baseline ready for Friday's review.
- R04 (VR↔Desktop menus) explicitly carried — needs a Sprint-2 sync, not resolved this week.

---

## Day 5 — Fri 22 May 2026 (Sprint 1 review + retro)

- **Cross-sub-team Sprint 1 review** (15-min slot per team) — each sub-team walked its Day-2→Day-5 progress and headline numbers.
- **Cross-sub-team retro** (30-min slot after the in-team retros). Went over how we communicated and how it could be improved.
- **Gateway contract:** Sub-team 1 targeting v0.1 freeze today — clients (3/4/5/6) to code against it from Sprint 2.

**Sprint-1 cross-team status:**
- R01 (gateway contract) — moving to v0.1 freeze; DEPS-1 still open until Sub-team 1 ratifies our stub.
- R03 (state contract) — state-ownership table owed; rolls into Sprint 2 (Sub-team 6 owes its state shape to Sub-team 7 by Day 9).
- R04 (VR↔Desktop menus) — open, needs Sprint-2 sync between Sub-teams 4 and 6.
- R06 (tooling) — effectively closed; baselines produced.

**Handover:** Sprint 2 SM rotation begins Monday; this stand-up's facilitation passes on with the role. Risk register stays with Sub-team 1 (Integration Lead).

---

*Team Alpha — cross-sub-team coordination record. Week 1 (Sprint 1) only.*
