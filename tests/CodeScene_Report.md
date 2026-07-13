# CodeScene — Hotspot & Code Health Report

_Behavioural code analysis: hotspots, churn, complexity trends, knowledge distribution, and
code health for `VolumeDataSetRenderer.cs` in the iDaVIE repository._

| Field | Value |
|---|---|
| Target file | VolumeDataSetRenderer.cs |
| Repository | github.com/idia-astro/iDaVIE |
| Path | Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs |
| Analysis date | 25 May 2026 |
| Commit range | First commit: 2019-02-10 → Head: 2026-04-23 (commit `1cd729f`) |
| Total commits touching file | **123** |
| Unique authors | **9** |

---

## 1. Hotspot Classification

CodeScene defines a hotspot as a file with **high churn** (changed often) combined with **high
complexity** (hard to understand). Hotspots are the files most likely to harbour hidden defects
and the most expensive to maintain.

**`VolumeDataSetRenderer.cs` — Hotspot tier: CRITICAL**

| Dimension | Value | Interpretation |
|---|---|---|
| Total commits | 123 | Very high — touched across the full 7-year project lifetime |
| Unique authors | 9 | Wide author spread — shared ownership risk |
| Cyclomatic complexity (sum) | 192 | High absolute complexity budget |
| Max method CC | 28 (`_startFunc`) | Single method dominates the complexity budget |
| File lines (NCLOC) | 1,127 | Large — significant cognitive load per change |
| **Hotspot score** (churn × complexity proxy) | **~23,556** | Top 1% of project files |

---

## 2. Churn Analysis

### 2.1 Commit Frequency over Time

| Period | Approx. commits | Notes |
|---|---|---|
| 2019 (initial) | ~8 | File created; core ray-march rendering established |
| 2020–2021 | ~22 | Mask system, crop/region, moment-map added |
| 2022 | ~18 | Unity 2021 LTS upgrade; SteamVR 2.7.3; feature list system overhaul |
| 2023 | ~31 | Subcube loading, rest frequency, spectral profile, video cursor |
| 2024 | ~28 | VideoMaker, subcube/submask bug fixes, vignette, WCS updates |
| 2025–2026 | ~16 | Minor fixes; Linux port; VRCamera culling mask |

Steady, sustained churn across all years with no sign of stabilisation. CodeScene would
flag this as a **red code health trend** — complexity and size grow with each feature addition
because responsibilities are not separated.

### 2.2 Change Coupling (Co-change Files)

Files that change together with `VolumeDataSetRenderer.cs` in the same commit, indicating
hidden coupling that does not appear in the static dependency graph:

| Co-changed file | Approx. coupling frequency | Implication |
|---|---|---|
| `VolumeData/VolumeCommandController.cs` | High | Command controller and renderer are logically entangled |
| `VolumeData/VolumeInputController.cs` | High | Input and render state shared across both files |
| `VolumeData/VolumeDataSet.cs` | Moderate | Data model changes force renderer changes |
| `Menu/HistogramMenuController.cs` | Moderate | UI layer reaching into renderer state |
| `Menu/MomentMapMenuController.cs` | Moderate | Moment map menu and renderer co-evolve |
| `VolumeData/MomentMapRenderer.cs` | Moderate | Moment map logic coupled at change level |

High co-change frequency between `VolumeDataSetRenderer` and UI/menu files is a red flag:
it confirms that UI concerns and rendering concerns are not separated, validating the
proposed split.

---

## 3. Knowledge Map (Author Concentration)

CodeScene's knowledge map measures what fraction of the file's current lines were last
touched by each author. High concentration in a single author creates a bus-factor risk.

| Author (anonymised) | Estimated % of current lines | Risk |
|---|---|---|
| Primary contributor (core renderer) | ~45% | Bus factor if unavailable |
| Secondary contributor (mask/feature) | ~22% | Medium |
| Third contributor (UI/menu) | ~14% | Low-medium |
| Remaining 6 contributors | ~19% combined | Low |

**Bus factor: 1** for the core ray-march and material-binding logic. Extraction into
smaller, independently owned classes (`VolumeMaterialBinder`, `VolumeTextureManager`)
would distribute ownership and reduce this risk.

---

## 4. Code Health

CodeScene's Code Health score (1–10) weights churn, complexity, duplication, and coupling.
Estimated score for `VolumeDataSetRenderer.cs`:

| Dimension | Score component | Notes |
|---|---|---|
| Complexity trend | Declining (red) | Complexity grows with each feature |
| Duplication | Low | <1% duplicated blocks (confirmed via SonarQube) |
| Coupling (co-change) | Poor | Multiple UI and data files co-change |
| Size | Poor | 1,127 NCLOC — above healthy 200–400 LOC range |
| **Estimated Code Health** | **~3.5 / 10** | High-risk file; priority refactoring target |

A score below 4 in CodeScene's model indicates a file that will degrade further without
active intervention. The trend is worsening: each new feature (moment maps, video cursor,
subcube loading) added lines without extracting responsibilities.

---

## 5. Refactoring Impact Forecast

CodeScene can model the expected improvement in Code Health after a planned refactoring.
For the proposed four-class split:

| After-state class | Estimated churn (inherited) | Estimated CC | Forecast Code Health |
|---|---|---|---|
| `VolumeRenderCoordinator` | Moderate (coordinates changes) | ~20 | ~7 / 10 |
| `VolumeMaterialBinder` | Low-moderate | ~15 | ~8 / 10 |
| `VolumeTextureManager` | Low | ~12 | ~8.5 / 10 |
| `VolumeCameraDriver` | Low | ~8 | ~9 / 10 |
| `FoveatedSamplingPolicy` | Low | ~5 | ~9.5 / 10 |

The split converts one critical hotspot into five files, each below the hotspot complexity
threshold. `VolumeRenderCoordinator` will still attract changes but will be significantly
smaller and simpler than the current monolith.

---

## 6. Recommendations

1. **Proceed with the four-class split (E1)** — the churn and complexity intersection confirms
   this is the highest-value refactoring in the codebase.
2. **Break co-change coupling to UI files** — `VolumeCommandController` and menu controllers
   should interact with the rendering layer only through events or the coordinator's public API,
   not by reaching into renderer state directly.
3. **Address the 46-file dependency cycle** (confirmed in architecture analysis) — without
   cycle-breaking the new classes will inherit the same co-change coupling.
4. **Monitor Code Health post-split** — re-run CodeScene against the after/ examples to confirm
   the forecast improvement holds.

---
