# Team Alpha — AI tool usage log (T8)

Mandatory artefact per Section 9.2 (T8) of the assignment spec. Every AI-assisted artefact must be defensible by a human author on the pitch panel. This folder is the canonical log location for Team Alpha.

## Layout

One file per sub-team, by **work-package number** (Section 6.1–6.7):

- `sub-team-1.md` — Architecture / Micro-kernel
- `sub-team-2.md` — Persistence & Data
- `sub-team-3.md` — Rendering & Compute
- `sub-team-4.md` — Interaction System
- `sub-team-5.md` — Networking / Server
- `sub-team-6.md` — Desktop GUI & Client Shell *(us)*
- `sub-team-7.md` — Native Plug-ins

Edit only your own sub-team's file. PR review is light-touch — the goal is completeness, not gatekeeping.

## Entry schema

Append entries to your sub-team's file in **reverse chronological order** (newest first). Use this shape:

```
## YYYY-MM-DD — <short title>

- **Author:** <name>
- **Tool:** <e.g. Claude Code, ChatGPT-5, Copilot, Cursor>
- **Where used:** <artefact name + path>
- **Prompt summary:** <2–3 sentences — not the full prompt, the gist>
- **Where it helped:** <what landed in the artefact>
- **Where it failed / was wrong:** <what you discarded or had to fix>
- **Human reviewer:** <who validated the output before it shipped>
```

## What NOT to log here

Per the spec, AI may **not** be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence. If you used AI for any of these, that's a problem to flag, not log.

## Why per-sub-team files

Reduces merge conflicts when sub-teams write concurrently. The grading panel will read all 7 files together — keep entries skimmable.
