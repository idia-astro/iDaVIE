# God-class inventory — iDaVIE (May 2026 baseline)

Catalogue of god-class candidates in the iDaVIE codebase, with a PlantUML class diagram per class showing the responsibility clusters and the surrounding collaborators. Produced by Sub-team 1 (Architecture & Micro-kernel Core) as supporting evidence for deliverables D1 (architecture overview), D5 (worked refactoring examples), and D7 (sub-team design doc).

> Design-only. No upstream `.cs` files are modified. See `CLAUDE.md §2`.

## Method

Candidates were triaged by combining:

- **LOC** — `find Assets/Scripts -name '*.cs' | xargs wc -l | sort -rn`.
- **Public-member count** — proxy for RFC.
- **Mixed concerns** — distinct responsibility clusters discoverable from `using` lists and grouped method clusters.
- **Fan-in** — `grep -rln ClassName Assets/Scripts/` as a proxy for CBO (incoming).

Every class in the inventory blows past the CK thresholds Sub-team 1 set in `CLAUDE.md §7` (WMC ≤ 20 / 40, RFC ≤ 50, CBO ≤ 14 / 25).

## Inventory (ranked, May 2026)

| # | Class | LOC | Public members (≈) | Responsibility clusters mixed | Verdict | Diagram |
|---|---|---|---|---|---|---|
| 1 | `VolumeDataSet` | 1920 | 100+ fields/props, ~40 methods | FITS I/O · AST/WCS coords · voxel buffer · mask I/O · histogram · brush-stroke transactions · subcube cropping · region cube | **Severe** | [`01-volume-data-set.puml`](01-volume-data-set.puml) |
| 2 | `CanvassDesktop` | 1899 | 62 public methods | Desktop UI · file browse · HDU pick · subset bounds · mask validation · source/feature mapping file · color-map UI · sigma · restore-defaults · application exit | **Severe** | [`02-canvass-desktop.puml`](02-canvass-desktop.puml) |
| 3 | `VolumeInputController` | 1634 | ~50 | SteamVR action plumbing · VR hardware family detection (Oculus / Vive / WMR) · two Stateless state machines (locomotion + interaction) · brush · feature anchor editing · video-pos recording · quick-menu toggle | **Severe** | [`03-volume-input-controller.puml`](03-volume-input-controller.puml) |
| 4 | `DesktopPaintController` | 1558 | ~40 | Painting · slice extraction · region cube · camera spawn · pointer/drag handling · zoom · source list · colour-map UI | **Severe** | [`04-desktop-paint-controller.puml`](04-desktop-paint-controller.puml) |
| 5 | `VolumeDataSetRenderer` | 1402 | 171 public members | Volume ray-march driver · threshold/scale/contrast/gamma · foveated rendering · vignette · color-map · mask · moment-map orchestration · feature-set ownership · cursor voxel readback · benchmark coupling | **Severe** — canonical D5 candidate (`CLAUDE.md §4`) | [`05-volume-data-set-renderer.puml`](05-volume-data-set-renderer.puml) |
| 6 | `CatalogDataSetRenderer` | 694 | ~30 | Catalog loading · column mapping · point/line shaders · color & sprite sheets · visibility · vignette | **Moderate** | [`06-catalog-data-set-renderer.puml`](06-catalog-data-set-renderer.puml) |
| 7 | `VolumeCommandController` | 685 | ~30 | Windows `KeywordRecognizer` lifecycle · 30+ verb handlers (threshold, crop, projection, mask mode, teleport, color map, sampling…) · push-to-talk · dataset registration | **Moderate** | [`07-volume-command-controller.puml`](07-volume-command-controller.puml) |

### Excluded (large but not true god classes)

- `FitsReader.cs` (730 LOC) — flat P/Invoke surface for CFITSIO. Long because the C API is wide, but cohesive. Address via ABI re-shape (ADR-2 / ADR-3 / ADR-5), not a god-class split.
- `VoTable.cs`, `FeatureSetRenderer.cs`, `VideoCameraController.cs`, `QuickMenuController.cs` — long, but single-concern. Flag for monitoring, not for D5.

## Diagram conventions

Every `.puml` follows the same template so the seven diagrams compare directly:

- The god class is rendered as a single PlantUML `class` (`#lightyellow`) with **responsibility groups** rendered as section separators (`.. heading ..`).
- **Outgoing edges**
  - `..>` — transient dependency (used inside a method body)
  - `-->` — held reference (aggregation, field or property)
  - `*--` — owned/composed lifetime (the god class allocates/destroys the collaborator)
- **Incoming edges** (clients) are drawn with `..>` so the fan-in is visible without dominating the page.
- Stable kernel / domain types are coloured `#palegreen`.
- Unity / SteamVR / TMP / `UnityEngine.Windows.Speech` boundary types (the ones our anti-corruption layer must wrap, per `CLAUDE.md §3`) are coloured `#mistyrose` so layer-policy hot spots are visually obvious.

## How to render

```sh
plantuml refactoring-examples/subteam-1/god-classes/*.puml
```

PlantUML is also rendered inline by JetBrains IDEs, VS Code (`jebbs.plantuml`), and the GitHub PR view via the standard preview plugin.

## Verification

1. Render every `.puml`; confirm no syntax errors and each PNG / SVG opens.
2. Spot-check fan-in counts against the live codebase, e.g.
   ```sh
   grep -rln VolumeDataSetRenderer Assets/Scripts/ | wc -l
   ```
   the count should match (within ±1) the number of inbound arrows in `05-volume-data-set-renderer.puml`.
3. Cross-reference each class against the CK thresholds in `CLAUDE.md §7`. Each entry above must show a numeric breach on RFC, CBO, or LCOM to justify the *god* label.
4. The two classes selected as D5 worked refactoring examples should be flagged here once the team confirms — default recommendation: **`VolumeDataSetRenderer`** (mandated canonical) + **`VolumeDataSet`** (highest CBO / fan-in of the non-MonoBehaviour types).

## Refactoring suggestions

For each of the seven god classes — what to extract, which design pattern, which layer it lands in, and which sub-team contract it shapes — see [`refactoring-suggestions.md`](refactoring-suggestions.md).
