# D1 Requirements — Explainer & Decision Log

**Purpose:** internal study aid for Sub-team 6 (Die Boks / Team Alpha). It captures *what* is in our requirements, *why* we chose each item, *how* we elicited them, the *decisions* we made, and the requirements we *deliberately excluded*. It exists so every author can defend the requirements deliverable on their own to the iDaVIE panel.

> **AI-policy note.** This explainer was AI-assisted (reconstructing rationale from our own `requirements.md`, `CurrentGUIStateDoc.md`, and `Long-Term-Python-Console.md`) and is logged in `ai-log.md`. Per the brief, AI may **not** be used for the live pitch/interview defence — this document is preparation, not a script. Everything below must be defensible by a human author.

**Companion to:** `deliverables/D1-requirements/requirements.md` (the deliverable itself), `CurrentGUIStateDoc.md` (REQ-1 backing), `Long-Term-Python-Console.md` (REQ-3 backing).

---

## 1. What is in our requirements (the inventory)

Three requirement classes — 15 NFRs + 2 architectural drivers — plus scope and traceability.

| Block | Contents | Backing doc |
|---|---|---|
| **REQ-1** | As-is behaviour & coupling catalogue for all five tabs (File, Render, Stats, Sources, Debug), each with its coupling profile + worst-case defect. 13-row defect register B-01…B-13. | `CurrentGUIStateDoc.md` |
| **REQ-2** | 15 maintainability NFRs across all five ISO/IEC 25010 maintainability sub-characteristics: Modularity (MOD-1..3), Reusability (REU-1..3), Analysability (ANA-1..3), Modifiability (MOF-1..3), Testability (TST-1..3). Each: statement + acceptance metric + tool + MoSCoW priority + spec cross-reference. | `requirements.md §3` |
(They are all NFRs and not FRs as it is a refactoring proposal)
| **REQ-3** | 2 architectural drivers — ARQ-1 (Python console), ARQ-2 (workspace/state saving) — expressed as drivers on the architecture, not features to build. | `Long-Term-Python-Console.md` |
(We have 3 requirements, though the brief only has 2. This is because REQ1 explains the current state, REQ3 explains the long term python console and the workstate saving, REQ2 is the 15 maintainability NFRs (Mentioned in LO2 & §5))
| **Scope** | §1 scope + out-of-scope, with explicit sub-team boundaries. | `requirements.md §1` |
| **Traceability** | §5 LO2 evidence table: each NFR → ISO sub-char → tool → CI/sprint enforcement → reporting deliverable. | `requirements.md §5` |

---

## 2. Why each requirement exists

**Core argument:** the NFRs are not a wishlist. Each one is the precondition for fixing a documented, real defect, or it is mandated by a brief non-negotiable / CK threshold. This is our strongest defence line.

### Modularity
- **MOD-1 — separate assemblies, zero cycles.** Fixes the *root cause* of **B-02 (critical crash)**: the Debug readout binds to `Application.logMessageReceived` while the main thread is blocked inside CFITSIO. That is only possible because View, log handling and I/O share one MonoBehaviour. Separate assemblies + no cycles makes the bad binding structurally impossible. Mandated by §4.2.2.
- **MOD-2 — CBO ≤ 14 / ≤ 25.** The cause of the whole mess is `CanvassDesktop`'s coupling (`FindObjectOfType` everywhere, shared float fields). CK threshold from §7.1.
- **MOD-3 — instability gradient (Stable Dependencies Principle).** The architectural shape that keeps coupling low over time, not just at the snapshot.

### Reusability
- **REU-1 — interface + ≥1 test double on every boundary.** §4.2.4 non-negotiable; also the enabler for TST-1.
- **REU-2 — ISP, ≤ 7 members per interface.** Prevents the "God interface" failure mode that mirrors the God-canvas we are escaping.
- **REU-3 — zero transitive `UnityEngine`/`SteamVR` in ViewModel.** Our single highest-leverage NFR: cited by §4.2.3 non-negotiable, by ARQ-1 (a Python process cannot drag Unity in), and by testability.

### Analysability
- **ANA-1 (WMC), ANA-3 (duplication ≤ 3%)** — §7.1/§7.2 thresholds; the as-is 1899-LOC mixed-concern `CanvassDesktop` is the literal opposite.
- **ANA-2 — cognitive complexity ≤ 15.** SonarQube-enforceable per PR; captures "can a new maintainer read one method and understand it."

### Modifiability
- **MOF-1 — a new tab touches ≤ 3 files outside its own folder.** Operationalises "composable panels, not God-canvas" (§6.6) as a measurable change-coupling target.
- **MOF-2 — DV8 propagation cost −30% (Day 2 → Day 13).** Our headline before/after number.
- **MOF-3 — RFC ≤ 50, LCOM ≤ 0.5.** §7.1 thresholds.

### Testability
- **TST-1 — coverage ≥ 70% ViewModel / ≥ 50% overall.** §7.2 + coverage targets.
- **TST-2 — zero static/Unity calls per ViewModel class.** What makes the File-tab and Debug-tab worked examples testable without Unity (LO6).
- **TST-3 — DIT ≤ 4, NOC ≤ 5.** Keeps the mocking surface small. §7.1.

### REQ-3 drivers
- **ARQ-1 (Python console)** and **ARQ-2 (workspace save)** are framed as *drivers*, not features, because the assignment is **design-only** — we prove the architecture would not *block* them. ARQ-1's acceptance criterion is "NFR-REU-3 is necessary and sufficient" — we did not invent a new check; an existing NFR already covers it.

### Defect → NFR linkage (state this crisply)
- **B-02** (Debug crash) → MOD-1, REU-3, TST-2.
- **B-03** (slider sync) → MVVM binding policy (D3).
- **B-08** (UI freeze on load) → move long I/O behind the service gateway (D2).

---

## 3. How we went about it (elicitation method)

This lives in `CurrentGUIStateDoc.md §1` but is not summarised in the deliverable — be able to state it in one breath.

**Sources (elicitation inputs):**
1. The `zzalscv2/iDaVIE` source fork (reading `CanvassDesktop` directly).
2. Official Read the Docs GUI documentation.
3. The public GitHub issue tracker — issues **#464, #472, #456** are cited as real, open defects (B-05, B-12, B-13).
4. The iDaVIE v1.0 paper (Sivitilli et al. 2026, arXiv:2603.15490).

**Process (bidirectional — bottom-up evidence meets top-down standards):**
1. Catalogue the as-is first — every control, state flow, defect (REQ-1).
2. Build a severity-rated defect register (13 rows).
3. Derive NFRs top-down from three fixed sources: ISO 25010 maintainability sub-characteristics (LO2), the §7.1 CK thresholds, and the §4.2 non-negotiables.
4. Link defects → NFRs so each NFR has a real-world justification.
5. Make every NFR testable — acceptance metric + specific tool + CI/PR enforcement point.
6. Prioritise with MoSCoW.
7. Translate roadmap features into architectural drivers, not features.

The two halves are joined by the defect→NFR linkage paragraph: bottom-up evidence (defects) meeting top-down standards (ISO 25010 + thresholds).

---

## 4. Decisions we made (and own)

| Decision | Stated in deliverable? | Defensible rationale |
|---|---|---|
| **NFR-first, not functional-requirements-first** | implicit | LO2 and §6.6 frame our requirements competency around *non-functional* maintainability requirements. REQ-1's behaviour catalogue is the functional baseline; the feature set is not changing, maintainability is. |
| **Every NFR bound to a tool + CI gate** | yes (§5) | Satisfies LO2's "translate into testable architectural drivers"; ties to T6/T7. |
| **Cover all 5 ISO sub-characteristics, not "four"** | yes | ISO 25010 maintainability *has* 5. The brief §3 says "four" — we exceeded it deliberately, not by miscount. |
| **Long-term features as drivers, not features** | yes (REQ-3) | Design-only assignment; prove non-blocking, do not build. |
| **File + Debug as the two worked examples** | yes (§2 verdict) | Mandated by §6.6; we added *why the pair is good*: File exercises request/response RPC, Debug exercises the server-pushed stream — together they prove the transport contract has a real consumer on both paths. |
| **Only "M" and "S" priorities used** | implicit | No Could/Won't. Anything lower-priority was *cut* (became out-of-scope), not downgraded — every retained NFR is gate-enforced. |
| **MOF-2 target = −30%** | partially | Traces to §7, but the *number* is our chosen target. Be ready for "why 30 and not 50?" — it is the bar we commit to evidencing between the Day 2 baseline and Day 13 projection. |

---

## 5. Requirements we considered and deliberately excluded

A requirements deliverable that does not show what was *deliberately left out* reads as "we did not think about it." Each exclusion below is implied by our own evidence; the rationale is defensible.

**A. Functional/UX bug-fixes logged but NOT turned into requirements.**
- Excluded: **B-01** (Exit, no confirmation), **B-07** (4D dropdown grouping), **B-09/B-10** (log metadata/retention), **B-11** (silent out-of-bounds exclusion), **B-12** (toast notifications, #472).
- *Why:* these are functional/UX defects, not maintainability defects. Fixing them is a feature change; the assignment is a design-only **maintainability** refactor. They remain in the register as evidence of the cost of the current structure.

**B. Short/medium-term roadmap items from `CurrentGUIStateDoc §12`, left out of REQ-3.**
- Excluded: **subcube loading**, **HDU selection**, **camera-route video scripting**, **viz/analysis plugin separation**.
- *Why:* §6.6 names only two long-term features (Python console, workspace save). We scoped REQ-3 to exactly those to stay aligned with the brief and avoid scope creep. Subcube/HDU are short-term *feature* work; viz/analysis separation overlaps Sub-team 2/3.

**C. The other ISO 25010 top-level characteristics.**
- Excluded: performance, security, usability, reliability, compatibility NFRs.
- *Why:* LO2 and §6.6 scope us to **maintainability** sub-characteristics only. Performance/ABI NFRs are explicitly the micro-kernel / **Sub-team 1** kernel NFRs. We consume their contract; we do not own those NFRs.

**D. Cross-team defects.**
- Excluded: **B-13** (VR Previous-Mask button, #456) and the VR-side menu surface.
- *Why:* Sub-team 4 (Interaction System) owns VR menus. We note the touchpoint and hand off.

**E. Actually fixing B-02 in code.**
- *Why:* design-only; no production code under `Assets/` is changed (CLAUDE.md working-style rule). We wrote a requirement that prevents the crash structurally (MOD-1/REU-3/TST-2) rather than a patch.

---

## 6. Likely panel questions (self-test)

- "How did you decide what was a requirement vs. just a bug?" → §5A: maintainability defect → NFR; functional/UX defect → stays in register.
- "Why these two long-term drivers and not subcube/HDU?" → §5B: §6.6 names exactly these two; design-only, no scope creep.
- "Why five sub-characteristics when the brief says four?" → §4: ISO 25010 maintainability has five; deliberate.
- "Why −30% propagation cost?" → §4 MOF-2 row: our committed before/after bar.
- "Show me one NFR traced end-to-end." → REU-3: §4.2.3 non-negotiable → fixes B-02 → enables ARQ-1 → NDepend rule on every PR (§5).
- "What did you leave out, and why?" → all of §5.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal explainer for the D1 requirements deliverable. Not a deliverable itself.*
