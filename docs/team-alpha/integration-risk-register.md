# Team Alpha — Integration risk register

**Owner from Day 2 EOD onwards:** Integration Lead (Sub-team 1 / Architecture).
**Seeded by:** Team Alpha SM on 2026-05-19.
**Purpose:** track cross-sub-team interface risks per Section 10.1 of the assignment spec. Each row names the affected sub-teams and a single owner. Severity is initial gut-check, not final.

| ID  | Risk                                                                                      | Affected sub-teams        | Owner       | Severity | Mitigation (initial)                                                          | Status |
|-----|-------------------------------------------------------------------------------------------|---------------------------|-------------|----------|-------------------------------------------------------------------------------|--------|
| R01 | Service-gateway contract not frozen — every client codes against a moving target           | 1 ↔ 3, 4, 5, 6            | Sub-team 1  | H        | Draft interface stub + JSON-RPC schema by Day 3; freeze v0.1 by Day 5.        | Open   |
| R02 | Plug-in C ABI semver not enforced — native calls drift, clients break silently             | 7 ↔ 3, 5, 6               | Sub-team 7  | H        | ABI version header on every export; CI check that exports list is unchanged.   | Open   |
| R03 | State / persistence contract ambiguous — what the server persists vs. what the client owns | 2 ↔ 5, 6                  | Sub-team 2  | M        | Write a state-ownership table by Day 4; one DTO schema per persisted entity.   | Open   |
| R04 | VR ↔ Desktop menu vocabulary diverges — same command, different shapes                     | 4 ↔ 6                     | Sub-team 4  | M        | Agree on a shared command catalogue (verb + args schema) by Day 4.             | Open   |
| R05 | UnityEngine / SteamVR types leak across the anti-corruption layer                          | Cross-cutting (all)       | Sub-team 1  | H        | Architectural fitness function in CI: forbid `UnityEngine.*` in domain assemblies. | Open   |
| R06 | CK metric tooling (SonarQube, Understand, NDepend, CodeScene, DV8) not operational Day 2   | Quality Guild ↔ all       | Quality Guild | H      | Quality Guild publishes setup notes Day 1 EOD; each sub-team confirms Day 2 AM. | Open   |
| R07 | Worked-example before/after diffs require Unity 6 packages we don't have                   | 6 (and any UI-touching)   | Sub-team 6  | M        | Use Unity-free pseudocode in the "after" worked examples; flag UI-Toolkit deps. | Open   |
| DEPS-1 | Sub-team 1 gateway contract not available — Sub-team 6 ViewModel layer depends on `IServiceGateway` interface to be defined by Sub-team 1 (ARCH-8). Until frozen, WE1/WE2 and the composition root use a placeholder stub. | 1 ↔ 6 | Sub-team 1 | H | Sub-team 6 defines a minimal `IServiceGateway` stub in `refactoring-examples/sub-team-6/contracts-team1/` by Day 5; Sub-team 1 ratifies or replaces it by Day 7 (T3 feeds this). | Open |

## Handoff note

This file moves to Sub-team 1's ownership at **Day 2 EOD (2026-05-20)**. After that, edits go through the Integration Lead. Team Alpha SM keeps a read-only watch and raises new risks via stand-up or Slack.
