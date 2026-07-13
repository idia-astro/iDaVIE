# Sprint 1 Retrospective — Sub-team 6 (Die Boks / Team Alpha)

- **Sprint window:** Mon 18 May – Fri 22 May 2026 (Days 1–5)
- **Retro date:** Fri 22 May 2026 (Day 5) — 1 h sub-team retro + 30-min cross-sub-team slot
- **Facilitator:** Con (SM)
- **Members:** Con (SM) · Mark (TL) · Jimmy (POL) · Rory (QC)
- **Format:** Start / Stop / Continue + action items

---

## 1. Velocity summary

| Metric | Value |
|---|---|
| Cards pulled | 21 (~75 % capacity by design) |
| Cards done | 18 |
| Cards partial | 1 (ARCH-8 — interface contracts; hand-off owed Day 8) |
| Cards carried to Sprint 2 | 2 (BNCH-3 CodeScene body, BNCH-4 NDepend DSM) |
| Sprint-2 cards pulled forward | 11 (see §4 of sprint review) |

All `backlog.md` Part B exit criteria met.

---

## 2. What went well (Continue)

- **Over-delivery on worked examples.** Both File-tab and Debug-tab AFTER skeletons were produced and compile under `dotnet build` with no Unity Editor — 11 Sprint-2 cards effectively started or completed.
- **Baseline data is strong.** CK + SonarQube baselines across 8 classes are substantive; the silent data-corruption bug found in `DesktopPaintController.UpdateMaxValue` is pitch-grade evidence.
- **ADRs written to a full standard.** ADR-0001 and ADR-0002 (with Appendix A wire spec) are complete Nygard-format records with 9 JSON-RPC methods catalogued.
- **Mocking-difficulty analysis (BNCH-6).** The 205-call-site count on `CanvassDesktop` with breakdown by signal type directly supports the testability NFR argument.
- **Roles were clear and respected.** SM, TL, POL, QC responsibilities separated cleanly; no significant rework from ownership confusion.
- **Standups were mostly substantive.** Days 3–5 have real content; Day 2 partial presence was unavoidable (tooling setup day).

---

## 3. What didn't go well (Stop / Fix)

- **BNCH-3 and BNCH-4 carried.** CodeScene body remained a blank PDF placeholder; NDepend DSM not produced despite NDepend being operational by Day 4. Root cause: QC workload front-loaded on CK + SonarQube, leaving no time for the churn/DSM analysis.
- **Retro notes not written on Day 5.** This file is being completed retrospectively. Sprint 2: SM writes the retro during the retro slot, not after.
- **Day 5 standups thinly recorded.** Jimmy and Rory have no "Today" entries for Day 5. Stand-up cadence needs enforcing at 09:00 sharp.
- **`concern-map.png` is binary-only.** Violates §10.4 (no binary-only diagrams). A `.puml` or `.mmd` companion is owed before pitch freeze.
- **ADR-0002 author line left as placeholder.** Still reads "Sub-team 6 TL — fill in"; Mark's name must be added before artefact freeze.
- **MVVM binding policy has 6 open `_TODO_`s.** CommunityToolkit-Mvvm vs hand-rolled, dispatcher interface shape, lifecycle ownership, NDepend rule wording — all deferred to Sprint 2 planning.

---

## 4. Action items for Sprint 2

| # | Action | Owner | Due |
|---|---|---|---|
| A1 | Complete CodeScene hotspot/churn report (BNCH-3) | Rory (QC → now Jimmy as QC) | Day 6–7 |
| A2 | Produce NDepend DSM + propagation cost (BNCH-4) | Rory | Day 6–7 |
| A3 | Add `.puml`/`.mmd` source for concern map (DESN-1 gap) | Mark | Day 7 |
| A4 | Fill Mark's name in ADR-0002 author line | Mark | Day 6 |
| A5 | Resolve 6 `_TODO_`s in MVVM binding policy | Con (new TL) | Day 7 |
| A6 | SM enforces 09:00 stand-up; all four fill Yesterday + Today same day | Con → Rory (new SM) | Every day |
| A7 | Write Sprint 2 retro notes during the retro slot on Day 10 | Rory (new SM) | Day 10 |
| A8 | Formal interface-contract hand-off to Sub-team 1 at integration review | Con (new TL) | Day 8 |

---

## 5. Sprint 2 role rotation (confirmed)

| Role | Sprint 1 | Sprint 2 |
|---|---|---|
| SM | Con | Rory |
| TL | Mark | Con |
| POL | Jimmy | Mark |
| QC | Rory | Jimmy |

---

## 6. One-sentence sprint verdict

We hit every exit criterion and pulled Sprint-2 work forward, but let two benchmark tasks slip and failed to write this document on the day — both fixable with tighter time-boxing in Sprint 2.
