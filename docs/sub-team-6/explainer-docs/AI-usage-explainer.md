# AI Usage — Explainer & Decision Log

**Purpose:** internal study aid for Sub-team 6 (Die Boks / Team Alpha). It captures *how* we used AI, *why* we used it, the *decisions* we made about its use, where it *failed* us, and the boundaries we *deliberately did not cross*. It exists so every author can defend our AI usage on their own to the iDaVIE panel.

> **AI-policy note.** This explainer was itself AI-assisted (synthesised from our own `ai-log.md` / `ai-log-final.md`) and is logged in `ai-log.md`. Per the brief §10.5.6, AI may **not** be used for the live pitch/interview defence — this document is preparation, not a script. Everything below must be defensible by a human author.

**Companion to:** `deliverables/` AI logs — `ai-log-final.md` (team-level table, all four members) and `ai-log.md` (the running Con/Claude-Code version). The logs are the evidence; this is the argument.

---

## 1. How we used AI (the inventory)

**Four tools, split by person and task:**

| Person | Primary tool(s) | Mostly used for |
|---|---|---|
| **Con** | Claude Code (Opus 4.7 → 4.8 from 29 May) | Repo-side work: drafting, refactor worked examples, CK metrics, repo-vs-brief audits, git |
| **Rory** | Claude chat (Opus 4.6); one Python static-analysis pass | Metric interpretation + tool setup (SonarQube, CodeScene, NDepend) |
| **Jimmy** | Perplexity (Comet + Sonar) | Diagrams, prose-editing the brief, gap-analysis reviews |
| **Mark** | Claude Code (Sonnet 4.6); Gemini (image gen) | ADRs, skeleton fill, one visual diagram |

**Eleven prompt classes** (the agreed legend at the foot of both logs):
requirements-drafting · ADR-drafting · code-skeleton · test-generation · refactoring-proposal · diagram-generation · metric-interpretation · prose-editing · backlog-drafting · review · tool-setup.

**Not used for** (the four AI-free zones the brief carves out — §10.5.6, expanded in §5 below):
peer-rating / peer-review · contribution log · individual reflection · live pitch & interview defence. A fifth rule (§10.5.3) sits across all eleven classes above: no verbatim AI prose may be passed off as human-authored in the final report — everything AI drafts is edited and owned by a named author.

**The single workflow pattern across every row: *AI drafts, human verifies against ground truth.*** This is the most important "how." Almost every log row carries a real *where it failed* and a *what the human did instead* — the verification step is logged, not assumed. Concrete examples:

- Con built the File/Debug skeletons, **then installed the dotnet SDK and compiled them** before committing.
- Diagrams were AI-generated **but rendered on plantuml.com / in VS Code and checked against `architecture.md`** before they entered a deliverable.
- Metric figures were AI-estimated **but cross-checked against source line counts** (RFC ±, LCOM, the line-306 `UpdateMaxValue`-assigns-`minVal` copy-paste bug Claude surfaced).
- Con walked the **live GUI** to capture the real scene button names because the first trace used placeholder labels.

---

## 2. Why we used AI (the justification)

**Core argument:** AI was used where it removes drudgery or unblocks analysis, *never* as the final author of record. Each use falls into one of five defensible categories.

1. **Speed on mechanical / structural work.** Backlog scaffolding, PDF→markdown conversion, the deliverables folder reorg, skeleton generation, formatting the stand-up presets. Low-judgement, high-volume — the human cost of doing it by hand buys nothing.

2. **Drafting prose we then own.** ADRs, requirements, the pitch spine, sprint reviews. AI produces a first pass; the named human edits it and is accountable for every claim (§10.5.3 — verbatim AI prose must not pass as human-authored).

3. **Analysis the mandated tools couldn't give us — the strongest "why."** NDepend / DV8 **could not run**: `Assembly-CSharp.dll` does not compile outside Unity (≈2 hrs lost, logged as a Sprint-1 retro item). So Claude's static DSM / CK analysis became our *analytical baseline*, committed **with an explicit "tool-confirmation outstanding" note** rather than passed off as official tool output.

4. **Review / gap-finding.** Repeated read-only audits of repo-vs-brief to surface missing deliverables (state contract D13, trade-off D14, `concern-map.png` as a §10.4 binary-diagram violation, role-rotation matrix). AI is good at exhaustive cross-referencing; the human decides what is genuinely missing vs. already implicit.

5. **Tool setup help.** SonarQube Cloud, CodeScene, and NDepend onboarding on Windows — environment variables, scanner commands, project keys.

---

## 3. Where AI failed us (own these — the panel respects this more than the wins)

| Failure | What AI did wrong | How we caught / fixed it |
|---|---|---|
| **Team identity** | Confused "Team 6" (numeric allocation) with §6.6 (work package) on day 1 | Clarified identity; corrected CLAUDE.md + memory entries |
| **NDepend / DV8** | Confidently suggested multiple setups (Build Solution, regenerate project files, open from Unity) that all failed; ~2 hrs lost | Confirmed the DLL doesn't exist in `Library/ScriptAssemblies/`; switched to static source analysis; logged as a retro item |
| **File-tab scope** | Claimed "100% of File-tab logic lives in CanvassDesktop.cs" when a teammate said 95% with fan-out | Con challenged the figure; had Claude re-measure and reconcile |
| **Invented names** | Guessed Sub-team 4 command verbs (`LoadCube`, `CreateSelection`…) and SonarQube project keys | Flagged as examples; raised real questions with Team 4; pulled correct keys from the SonarQube project page |
| **Metric definition drift** | Reported integer LCOM4 instead of the brief's normalised LCOM (0–1, ≤0.5) | Con caught the mismatch; switched every metric file to the brief's LCOM |
| **D2-vs-skeleton conflict** | Assumed D2 architecture doc was canonical; D2 was itself wrong on `IMemoryProbe` / `LogEntry` | Treated the compiled, tested skeleton as source of truth; corrected D2 *and* D3 rather than degrading D3 to match a wrong doc |
| **Closing unmet gaps** | Told to close gap #12 (Day-13 CK re-measurement) that hasn't happened | Claude refused and flagged it ("fix to reality"); we accepted the correction |
| **Stale self-reference** | A D1 explainer header asserted it was "logged in ai-log.md" before any row existed | Added the log row; reconciled the claim |

The pattern in every one of these: **AI's confidence is not evidence.** It is caught by compiling the code, rendering the diagram, reading the source line, or asking the other team.

---

## 4. Decisions we made (and own)

| Decision | Rationale |
|---|---|
| **AI drafts, human verifies — always logged** | Each substantive assist gets a row with where-it-helped / where-it-failed / what-the-human-did. The verification is the deliverable, not the draft. |
| **Different tools for different members** | Not enforced top-down; each member used what they were fluent in (Claude Code / Claude chat / Perplexity / Gemini). The log normalises across them. |
| **Source-of-truth hierarchy: compiled code > deliverable doc > AI claim** | Settled after the D2-vs-skeleton conflict — tested code wins over any document, and any document wins over an unverified AI assertion. |
| **AI analysis accepted as baseline only with a tooling caveat** | When NDepend/DV8 wouldn't run, AI CK/DSM output was committed *labelled* as analytical, not official — never disguised as mandated-tool output. |
| **Hard policy boundary on §10.5.6 work** | Peer-rating, contribution log, individual reflection, and the live pitch/interview defence are AI-free by rule. Prep material is explicitly tagged as rehearsal, not script. |
| **Back-fill the log honestly from transcripts** | Con's Claude Code rows were reconstructed from session transcripts (model confirmed per day); Mark/Jimmy's Perplexity/Gemini use is out of his visibility and left for them to confirm. |

---

## 5. Boundaries we deliberately did not cross (§10.5.6)

AI was **not** used for, and the logs say so explicitly:

- **Peer-rating** — human-only.
- **Contribution log** — human-only.
- **Individual reflection** — human-only.
- **Live pitch / interview defence** — human-only. The interview-prep and pitch-prep log rows are flagged "rehearsal framework; answers in the actual interview will be the student's own."
- **Passing verbatim AI prose as human-authored** in the final report (§10.5.3) — every drafted doc is edited and owned by a named author.

The two technical explainer docs (D1, D3) and this one all carry the same disclaimer header for exactly this reason: they are *preparation aids*, not deliverables, and not scripts to read from on the day.

---

## 6. Likely panel questions (self-test)

- **"How did you use LLMs?"** → §1: four tools, eleven prompt classes, one pattern — AI drafts, human verifies, every assist logged.
- **"Where did LLMs fail you?"** → §3, pick any two: NDepend couldn't run (we caught it by checking for the DLL); the 100%/95% File-tab figure (we re-measured); LCOM4-vs-LCOM drift (we matched the brief).
- **"How do we know the AI output is trustworthy?"** → §4 source-of-truth hierarchy: we compiled the skeletons, rendered the diagrams, and cross-checked metrics against source — AI confidence is not evidence.
- **"Isn't using AI for the analysis a problem when the brief mandates NDepend/DV8?"** → §2.3: the mandated tool physically couldn't run on a Unity project; we committed the AI analysis *labelled* as a baseline with the gap logged, not disguised as official output.
- **"What did you refuse to use AI for?"** → §5: peer-rating, contribution log, individual reflection, live defence — all human-only per §10.5.6.
- **"Can you defend this section without the AI?"** → yes — that is the whole point of this document.

---

*Sub-team 6 — Desktop GUI & Client Shell. Internal explainer for our AI usage. Not a deliverable itself; preparation only (§10.5.6).*
