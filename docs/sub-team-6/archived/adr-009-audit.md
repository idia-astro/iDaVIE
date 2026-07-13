# ADR-009 Audit — Desktop Client MVVM Split

**Audit target:** ADR-009 "Adopt MVVM for the Desktop GUI Client Shell" (see [`../../team-alpha/ADR_Log_Improved.md`](../../team-alpha/ADR_Log_Improved.md#adr-009-adopt-mvvm-for-the-desktop-gui-client-shell))
**Auditor:** Sub-team 6 (Die Boks / Team Alpha)
**Date:** 2026-05-27 (Day 8)
**Status:** **ARCHIVED 2026-05-31** — historical Day-8 snapshot, retained for traceability only (see banner below).

---

> **⚠ ARCHIVED 2026-05-31 — historical Day-8 snapshot.** This audit was a one-off landscape check run on Day 8 (2026-05-27) to find gaps between ADR-009's claims and the D-deliverables. It did its job: findings F4, F6, F9, F10, F16–F20 were folded back into D1–D5 and drove the gateway rewire. It is **not a mandated deliverable** (Section 6.6 / 9.2) and is kept only as a paper trail. Two cautions for any reader:
> - **F15 (Day-6 Sub-team 1 transport handshake) is the one substantive item still open** — the contract remains unilateral; see `Sprint-Documents/standups.md`.
> - The repeated **"95 / 95 green"** figure (F12, F14) holds only on a host *without* **Smart App Control**. On a SAC-enforced Windows host the 19 Tier-2 tests are blocked at assembly load (`FileLoadException 0x800711C7`) and the live count is **76 / 95** (the 76 Tier-1 tests pass anywhere). The gateway contracts now live under `refactoring-examples/sub-team-6/contracts-team1/` (was `contracts/`). The current, caveated statement is in `D5-testing/test-strategy.md` §4.4.

---

## 1. Claims under audit

ADR-009 makes seven binding claims. Each is referenced as **C1–C7** below.

| # | Claim | Source line(s) in ADR-009 |
|---|---|---|
| C1 | Decompose `CanvassDesktop` into MVVM triad: View (UI Toolkit) / ViewModel (pure C#) / Service Gateway | Decision §1 |
| C2 | One View/ViewModel pair per panel: File, Render, Stats, Sources, Debug — no God-canvas | Decision §2 |
| C3 | Commands in the ViewModel follow the GoF Command pattern (reified, replayable, testable) | Decision §3 |
| C4 | Transport contract = JSON-RPC 2.0 over named pipes (local), gRPC upgrade path (remote) | Decision §4 |
| C5 | ViewModels carry zero `UnityEngine` / `SteamVR` imports; testable in xUnit/NUnit headless | Decision §5 |
| C6 | ViewModel branch coverage ≥ 70 % | Positive consequence §1 |
| C7 | Transport contract agreed with Sub-team 1 by **Day 6** | Notes & Traceability |

---

## 2. Findings table

Legend: ✅ matches · ⚠ partial · ❌ contradicts · ⛔ missing.

| # | Claim | File(s) checked | Finding | Status |
|---|---|---|---|---|
| F1 | C1 — MVVM triad | `D3/mvvm-binding-policy.md` §2.1 (l.76–88) | Three assemblies named exactly: `iDaVIE.Client.{View, ViewModel, Gateway}`. Dependency direction declared View → ViewModel → Gateway with NDepend enforcement. | ✅ |
| F2 | C1 — MVVM triad | `D2/architecture.md` §3 (l.63–118), §4 ADR-0001 (l.126–138) | C4 L3 Mermaid shows all three boundaries; ADR-0001 names the three tiers explicitly. | ✅ |
| F3 | C2 — 5 panels File/Render/Stats/Sources/Debug | `D2/architecture.md` §3 (l.79–80) | All five panels listed: `FileTabViewModel`, `DebugTabViewModel`, `RenderingTabViewModel`, `StatsTabViewModel`, `SourcesTabViewModel`. | ✅ |
| F4 | C2 — 5 panels | `D3/mvvm-binding-policy.md` §3.1–3.5 | ~~Only 4 ViewModels defined: File, Debug, Rendering, **Information**. Stats and Sources missing; "Information" invented and not in ADR-009.~~ **CLOSED 2026-05-27.** §3.4 replaced with `StatsTabViewModel` (histogram, percentile presets, scale mode); new §3.5 added for `SourcesTabViewModel` (catalogue + mapping I/O). Composition root pseudo-code in §5.1 + ACL boundary diagram in §6 updated to match. Five panels now line up across D1 / D2 / D3 / D4. | ✅ |
| F5 | C3 — Command pattern reified | `D3/mvvm-binding-policy.md` §4.2 (l.234–246), §10 (l.501–512) | `ICommand` is the sole binding mechanism; async commands return `Task`; events as ICommand substitutes explicitly forbidden. | ✅ |
| F6 | C3 — Replayable / undo | `D3` §4.2, `D5/test-strategy.md` §3.2 | ~~Replay/undo not stated.~~ **CLOSED 2026-05-27.** D3 §4.2 now has a "Replay and undo (cross-reference to ADR-010)" paragraph. Names replayability as Sub-team 4's command-log concern that the desktop VM inherits by sharing the `ICommand` shape. Adds the Undo "where applicable" clause from ADR-010 verbatim and acknowledges most desktop commands aren't naturally undoable. | ✅ |
| F7 | C4 — JSON-RPC 2.0 over named pipes | `D2/architecture.md` §4 ADR-0002 (l.144–239) | Wire spec is complete: pipe naming, length-prefix framing, JSON-RPC 2.0 message shape, method catalogue (`file.open`, `log.subscribe`, …), error codes, `wireVersion` discipline. | ✅ |
| F8 | C4 — gRPC upgrade path | `D2/architecture.md` §4 ADR-0002 (l.152–157) | "Remote mode (post-MVP): gRPC over HTTP/2 with the same `IServiceGateway` interface surface." | ✅ |
| F9 | C4 — Transport has a real consumer | `file-tab/adapters/FitsServiceAdapter.cs`, `file-tab/adapters/tests/FitsServiceAdapterTests.cs`; `D4/README.md` §Example 1, `D5/test-strategy.md` §4.4 | ~~**The File-tab worked example does NOT use the transport.** It depends on four local Unity-side adapter interfaces; ADR-009's "integrate via the contract" is unmet — there is no integration.~~ **CLOSED 2026-05-27.** WE1 reshaped (path (a)): `FitsServiceAdapter` implements `IFitsService` by forwarding through `IServiceGateway`, mapping `OpenImageAsync`/`OpenMaskAsync` → `file.open` + `dataset.getAxes`, `GetHeaderTextAsync` → `dataset.getHeader`, and handle `Dispose` → `file.close` (ADR-0002 catalogue v1). Tier-2 `FitsServiceAdapterTests` assert these wire shapes against a `FakeGateway`. The transport now has a real consumer. | ✅ |
| F10 | C4 — Debug stream over transport | `debug-tab/adapters/GatewayLogStreamAdapter.cs`, `debug-tab/adapters/tests/GatewayLogStreamAdapterTests.cs`; `D4/README.md` §Example 2 | ~~Debug tab uses an in-process `UnityLogStreamAdapter` subscribing to `Application.logMessageReceived`, **not** server-pushed `log.emit` notifications; the implementation is local.~~ **CLOSED 2026-05-27.** WE2 reshaped: `GatewayLogStreamAdapter` replaces `UnityLogStreamAdapter`, subscribing to `IServiceGateway.OnNotification`, filtering `log.emit`, and republishing through `ILogStream` to `DebugTabViewModel`. Tier-2 `GatewayLogStreamAdapterTests` assert the ADR-0002 `log.emit` params shape `{ level, msg, ts }`. Debug now consumes the server-pushed stream. | ✅ |
| F11 | C5 — No Unity in ViewModel | `D3` §11.1 NDepend rule (l.518–531), `D5/test-strategy.md` §9 (l.244–247), `D5/viewmodel-unit-tests.md` §4 | Structurally enforced: ViewModel `.csproj` has no Unity reference, NDepend CQLinq rule forbids `UnityEngine`/`Valve.VR`/`System.Runtime.InteropServices`. `dotnet build` on skeleton csproj passes with zero Unity refs (per `D4` evidence). | ✅ |
| F12 | C5 — Headless test execution | `D5/test-strategy.md` §3.4, §4.4 (l.137–187) | The full no-Unity suite is **95 tests** — 76 tier-1 (ViewModel) + 19 tier-2 (gateway/adapter) — run via `dotnet test` in ~200 ms. Zero Unity dependency. (Was 63 tier-1-only before the F14 branch-coverage tests and the gateway-proxy adapter suites.) **[Archive note: all 95 green only on a non-SAC host; 76/95 where Smart App Control is enforced — see banner.]** | ✅ |
| F13 | C6 — ≥ 70 % VM branch coverage | `D5/test-strategy.md` §7 (l.203–209) | ViewModel branch and line target ≥ 70 %, gated in CI via Coverlet+ReportGenerator. View tracked-not-strict. | ✅ |
| F14 | C6 — Coverage **measured** vs **targeted** | All D4/D5 | ~~No actual percentage reported.~~ **CLOSED 2026-05-27.** Coverlet 6.0.2 wired into all 5 test projects; full Cobertura → ReportGenerator merge committed at `D5/coverage-report.md`. **Day-8 follow-up: 13 targeted branch-coverage tests added to close the 67.4 % FileTabSkeleton branch gap. Final measurements: DebugTabSkeleton 100 % / 100 %, FileTabSkeleton 89.4 % / 77.2 % (branch +9.8 pp, gate cleared), iDaVIE.Client.Gateway 41.2 % / 41.6 % (tracked, not gated).** Total suite: 95 tests, ~200 ms (all green on a non-SAC host; see banner). D5 §7 rewritten to cite the measured numbers and the gate-pass state. | ✅ |
| F15 | C7 — Day-6 contract handshake with Sub-team 1 | `D2/architecture.md` §4 ADR-0002 | No Day-6 date or handshake artefact. Method catalogue (l.213–223) is written unilaterally by Sub-team 6. No record of Sub-team 1 sign-off in `Sprint-Documents/` either. | ⛔ |
| F16 | D1 ↔ ADR-009 traceability | `D1/requirements.md` §3 NFR table | ~~Spec column points only at brief §; ADR not cited.~~ **CLOSED 2026-05-27.** Four ADR-009-driven NFRs now cite both their brief section and `ADR-009` in the Spec column: NFR-MOD-1 (`§4.2.2; ADR-009`), NFR-REU-3 (`§4.2.3; ADR-009`), NFR-TST-1 (`§7.2; ADR-009`), NFR-TST-2 (`§7.2; ADR-009`). The 10 generic-CK / SOLID-only rows are left single-sourced. | ✅ |
| F17 | D1 promise vs D4 delivery | `D1/requirements.md` §2 (l.23) vs `D4/README.md`, `file-tab/adapters/`, `debug-tab/adapters/` | ~~D1 states *"File for request/response RPC, Debug for the server-pushed stream — confirming the contract has a real consumer."* Neither delivery achieves this.~~ **CLOSED 2026-05-27.** Both halves of the D1 promise now hold via the gateway rewire: File-tab does request/response RPC through `FitsServiceAdapter` (see F9) and Debug-tab consumes the server-pushed stream through `GatewayLogStreamAdapter` (see F10). The contract has a real consumer on both paths. | ✅ |
| F18 | ADR numbering map | `D3` frontmatter, `D2` §4, central `ADR_Log_Improved.md` | ~~No reader-facing map links central ADR-009 ↔ local ADR-0001.~~ **CLOSED 2026-05-27.** D2 §4 now opens with a "Numbering map" cross-walk table covering all four local ADRs. **Specifically flags the local `ADR-0003` ↔ central `ADR-002` number-reversal trap** that a careless reader would miss. D3 frontmatter adds a one-line pointer to the D2 table. | ✅ |
| F19 | Duplicate `BNCH-6.md` | `deliverables/BNCH-6.md`, `deliverables/other/T2-baseline-benchmark/BNCH-6.md` | ~~Byte-identical duplicate.~~ **CLOSED 2026-05-27.** Top-level `deliverables/BNCH-6.md` deleted. Canonical location is `deliverables/other/T2-baseline-benchmark/BNCH-6.md` (alongside its BNCH-1..5 / ck-metrics siblings — matches the sprint-review history that introduced it). The 2 inbound links in `D5/test-strategy.md` (§11 evidence table, §12 traceability) updated to the surviving path. | ✅ |
| F20 | CK number consistency across deliverables | `D2` §2.2, `D3` §8.1; `D4/metrics.md` | ~~D3 §8.1 lists FileTabViewModel WMC ~12 (projected); D4 §2.2 hand-counts 27 (measured, borderline). Self-contradicting.~~ **CLOSED 2026-05-27.** D3 §8.1 table rewritten to cite the Day-6 hand-counted measurements (FileTabViewModel WMC=27, CBO=9) with explicit "projected" vs "measured" column and a note about the brief-§7 ban on speculative numbers. Audit accepts the borderline WMC=27 rather than masking it; remediation path (extract `FileTabCommands` helper → ~22) is documented. | ✅ |
| F21 | Cycle rule (NFR-MOD-1) | `D2` §3 (l.120), §8 (l.450); `D3` §2.1 (l.88) | "Zero circular dependencies" declared and CI-enforced via NDepend in both deliverables. | ✅ |
| F22 | Interface count + test doubles | `D2` §6 (10 interfaces); `D5/test-strategy.md` §9 rule 2 | Every interface has a Moq test double in the committed suites. §4.2 #4 satisfied. | ✅ |

---

## 3. Fixes needed (prioritised)

### Severity ❌ — contradicts ADR-009 or D1

1. **F9 + F10 + F17 — Transport contract has no consumer. ✅ RESOLVED 2026-05-27 — path (a) taken.** Both worked examples were reshaped to route through `IServiceGateway`: WE1's `FitsServiceAdapter` proxies `file.open` + `dataset.getAxes` + `dataset.getHeader` + `file.close`, and WE2's `GatewayLogStreamAdapter` consumes server-pushed `log.emit` notifications. Tier-2 `FitsServiceAdapterTests` and `GatewayLogStreamAdapterTests` assert the ADR-0002 wire shapes against a `FakeGateway`. The contract now has a real consumer on both the request/response and push-stream paths; the softer fallbacks (specify-only, or a throwaway `session.hello` round-trip) were not needed.
2. **F4 — D3 panel inventory drift.** Replace `InformationTabViewModel` (§3.4) with `StatsTabViewModel` and `SourcesTabViewModel` sections, matching D1/D2/D4. Either rename the existing §3.4 to `StatsTabViewModel` (FITS header dump arguably belongs there) and add a new §3.5 for `SourcesTabViewModel`, or drop §3.4 entirely if Sub-team 6 only intends to design two panels in detail and gesture at the rest.

### Severity ⛔ — missing

3. **F15 — Day-6 Sub-team 1 handshake.** Either record the transport contract sign-off in `Sprint-Documents/standups.md` or in a new `D2/transport-handshake.md` artefact, or amend ADR-009's Notes & Traceability if the Day-6 deadline slipped. Without this the contract is unilateral.

### Severity ⚠ — partial / numbering / hygiene

4. **F18 — ADR numbering map.** Add a one-line table at the top of `D2/architecture.md` §4: *"`ADR-0001` ↔ central ADR-009 · `ADR-0002` ↔ extends ADR-009 (transport detail) · `ADR-0003` ↔ central ADR-002 · `ADR-0004` ↔ no central equivalent (UI Toolkit choice)."* Same map in `D3` frontmatter.
5. **F20 — FileTabViewModel WMC number drift.** `D3` §8.1 projects ~12; `D4/metrics.md` §2.2 hand-counts 27 with documented remediation. Update `D3` §8.1 to cite the measured 27 with the remediation note, so the two docs don't disagree.
6. **F16 — D1 spec column.** Add ADR-009 as a cited source on the NFR-MOD-1, NFR-REU-3, NFR-TST-1, NFR-TST-2 rows.
7. **F19 — Duplicate `BNCH-6.md`.** Delete `deliverables/BNCH-6.md` (or `other/T2-baseline-benchmark/BNCH-6.md`) and update the broken link to point at the surviving copy. `D5/test-strategy.md` §11 currently links to `../BNCH-6.md` — pick that or fix it.
8. **F6 — Replayability of commands.** Either cross-reference ADR-010 from `D3` §4.2 (command replay log is owned there), or add a one-sentence "replay scope" note clarifying that command logging is an ADR-010 concern that ADR-009 inherits.
9. **F14 — Coverage number not yet measured.** Track the Day-13 tool-verification task explicitly in `Sprint-Documents/`; this is expected to close before pitch on 4 June.

---

## 4. Audit method (for reproduction)

Six passes, fixed checklist per ADR-009 claim, single findings row per check. Files read in full:

| Pass | File | Outcome |
|---|---|---|
| 1 | `D3/mvvm-binding-policy.md` (568 lines) | F1, F4, F5, F6, F11, F20 |
| 2 | `D2/architecture.md` (453 lines) | F2, F3, F7, F8, F15, F18, F21, F22 |
| 3 | `D1/requirements.md` (67 lines) | F16, F17 |
| 4 | `D4/README.md` (132 lines) + `D4/metrics.md` (429 lines) | F9, F10, F12, F20 |
| 5 | `D5/test-strategy.md` (299 lines), `D5/ui-toolkit.md` (198 lines), `D5/viewmodel-unit-tests.md` (183 lines) | F9, F11, F12, F13, F14 |
| 6 | Cross-cutting | F18, F19, F20 |

No production code under `Assets/` was inspected — this is a documentation audit, consistent with the design-only nature of the assignment.
