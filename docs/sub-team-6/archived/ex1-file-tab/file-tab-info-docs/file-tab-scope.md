# File Tab — Scope Inventory

## TL;DR — Everything associated with the File tab, in one page

**Primary code location**
- All File-tab **orchestration** lives in **`Assets/Scripts/UI/CanvassDesktop.cs`** (1899 lines, single `MonoBehaviour`) — UI wiring, axis-validation rules, mask compatibility checks, popup state, the load coroutine, cross-tab post-load choreography.
- The **actual work fans out** to at least six collaborators that `CanvassDesktop` reaches via `FindObjectOfType<>` or direct static calls: `FitsReader` (13 call sites — native `[DllImport]` FITS plug-in), `StandaloneFileBrowser` (5 call sites — OS file dialog), `VolumeDataSetRenderer` (cube I/O on the instantiated prefab), `VolumeCommandController`, `VolumeInputController`, `FeatureMenuController`. Full list in §6.
- The smell isn't that one file owns *everything* — it's that the file owns **all the orchestration with no seams**, while reaching across the scene graph for collaborators it can't substitute in a test.
- "File tab" = the image-loading workflow inside the `informationPanelContent` panel of the `fileLoadCanvassDesktop` canvas. The user clicks **Browse Image**, optionally **Browse Mask**, picks an HDU, optionally enables subset selection, picks a Z-axis, picks an aspect ratio, then clicks **Load**.
- Strictly speaking there is no user-facing "File" tab — the workflow lives on a separate modal canvas (`FileLoadCanvassDesktop` / `InformationPanel`). See the Terminology note in the body for the full disclaimer.

**User-visible controls (11)**
Browse Image · Browse Mask · HDU dropdown · Z-axis dropdown · Load-Subset toggle · 6 × Subset Min/Max inputs · Aspect-ratio (Ratio_Dropdown) · Load button · Dismiss/X button · Tab/Shift-Tab cycling between inputs.

**Eight responsibility groups inside `CanvassDesktop.cs`**
1. Native OS file dialog (`BrowseImageFile` 306, `BrowseMaskFile` 807)
2. FITS header / HDU inspection (`_browseImageFile` 329, `UpdateHeaderFromFits` 538, `ChangeHduSelection` 1435)
3. Cube-validity gating (`IsLoadable` 431) — also computes `_imageSize`, subset-max, Z-axis options, popup messages
4. Mask validation (`_browseMaskFile` 829, `CheckImgMaskAxisSize` 905)
5. Subset bounds editor (`onSubsetToggleSelected` 575, `setSubsetBounds` 595, `updateSubsetZMax` 614, `checkSubsetBounds` 641 — a ~165-line clamping routine)
6. Memory / load-feasibility check (`CheckMemSpaceForCubes` 995)
7. Load orchestration (`LoadFileFromFileSystem` 927 → `LoadCubeCoroutine` 1015 → `postLoadFileFileSystem` 935)
8. Canvas / popup chrome (`DismissFileLoad` 1613, `_showPopUp` / `_textPopUp` flags rendered in-file by `OnGUI` 1646 + `ShowGUI` 1656)

**External collaborators (the seams where the anti-corruption layer goes)**
- `FitsReader` (native C++ plugin) — **13 call sites**, every header read goes through `[DllImport]`
- `StandaloneFileBrowser` (SFB native dialog plugin) — 5 call sites, shared with Sources/Mapping/Debug tabs
- `VolumeCommandController` — receives `AddDataSet`, `RemoveDataSet`, `DisablePaintMode`, `endThresholdEditing`, `endZAxisEditing`
- `VolumeInputController` — activation-bounced during cube swap
- `VolumeDataSetRenderer` — instantiated from `cubeprefab`, configured with subset bounds, file paths, HDU, depth axis
- `VolumeDataSet` — `Data.CleanUp` / `Mask?.CleanUp` during teardown
- `FeatureMenuController` — activation-bounced so the new cube's source list re-binds
- `PlayerPrefs["LastPath"]`, `SystemInfo.systemMemorySize`, `FileInfo.Length`, `MenuBarBehaviour`

**Scene-hierarchy coupling (the brittle part)**
~30 hard-coded `transform.Find("informationPanelContent/…/…")` chains. Each one is an untestable string-path GetComponent walk. All listed in §7.

**Persistence**
A single `PlayerPrefs` key — `"LastPath"` — shared with the Sources tab and Mapping tab. The Debug tab uses `"LastDebugPath"` separately. No `Config` consumption.

**Cross-tab effects when a load completes (`postLoadFileFileSystem`, lines 935–987)**
The File tab reaches into other tabs to: enable the Rendering-tab mask dropdown, repopulate the colormap, populate stats (and regenerate the histogram image), populate the rest-frequency dropdown and input field, **re-subscribe** rest-frequency change handlers (without unsubscribing the prior renderer's — a real leak; see §10), unlock the Rendering / Stats / Sources / Paint tab buttons, click-invoke the Stats tab, and toggle the VR view on the menu bar. This is the single biggest cross-tab coupling in the file.

**Adjacent but NOT File tab (§9)**
`BrowseSourcesFile`, `BrowseMappingFile`, `SaveMappingFile`, `LoadSourcesFile`, `ChangeSourceMapping`, `AreMappingsIncompatible`, `AreMinimalMappingsSet`, `_browseSourcesFile`, `_browseMappingFile`, `_saveMappingFile` — all live in `CanvassDesktop.cs` but populate `sourcesPanelContent`, so they belong to the **Sources tab**. They reuse `StandaloneFileBrowser` and `"LastPath"`, so a clean `IFileDialogService` from this worked example becomes reusable for them.

**Out of scope (§11)**
Debug tab (lives in `Assets/Scripts/Debuggers/DebugLogging.cs` — that's Worked Example 2) · threshold sliders · colormap · stats · rest-frequency · tab switching (`TabsManager.cs`) · paint-mode · `Exit()`/SteamVR shutdown · WelcomeMenu lifecycle.

**Resolved anomalies (full detail in §10)**
1. `_showLoadDialogCoroutine` — confirmed dead state; only `StopCoroutine`'d, never started, no external references.
2. `_showPopUp` / `_textPopUp` — **not** externally rendered; `OnGUI()`/`ShowGUI(int)` at lines 1646–1667 in this same file draw the "Invalid Cube" IMGUI window.
3. `CheckImgMaskAxisSize`, `SetInputIndex`, `OnRatioDropdownValueChanged` — all confirmed Inspector-wired via scene grep: `CheckImgMaskAxisSize` at `ui.unity:35385`; `SetInputIndex` at 6 sites (11298, 25425, 36405, 43465, 68717, 94378); `OnRatioDropdownValueChanged` at 15411 and 96249.
4. `_ratioDropdownIndex` — set by `Ratio_Dropdown` on the **file-load modal** (not the Rendering tab as previously assumed) and read in `LoadCubeCoroutine` at line 1029. Intra-File-tab UI-coupling rather than cross-tab.
5. **New finding:** `postLoadFileFileSystem` re-subscribes rest-frequency change handlers (958–959) without unsubscribing the previous renderer's — every reload leaks one subscription. Cross-tab defect to fix in the worked example.

---

> **Purpose.** Enumerate every responsibility currently performed by the File-tab workflow in `Assets/Scripts/UI/CanvassDesktop.cs`, line-range by line-range, so that the "responsibility → new owner" table in the worked-example document has no missing rows.
>
> **Source of truth.** `Assets/Scripts/UI/CanvassDesktop.cs` at HEAD of branch `team6` (1899 lines). All line numbers below refer to that revision. Re-verify after any merge.
>
> **Boundary definition adopted in this doc.** The "File tab" = the *image-loading workflow* exposed through the `informationPanelContent` panel inside the `fileLoadCanvassDesktop` canvas. It runs from the moment the user clicks **Browse Image** until `postLoadFileFileSystem()` returns control to the main canvas. Sources-tab and Mapping-tab file browsing reuse the same file-dialog plumbing but populate a *different* panel (`sourcesPanelContent`); they are listed in §9 as **adjacent but out of scope** for Worked Example 1.
>
> **Terminology note.** "File tab" is a label of convenience. The main canvas `Tabs_ container` (referenced at `CanvassDesktop.cs:966–972`) contains only four buttons: `Rendering_Button`, `Stats_Button`, `Sources_Button`, `Paint_Button`. There is no user-facing tab labelled "File". The image-loading workflow lives on a *separate modal canvas*: `fileLoadCanvassDesktop` (scene name **`FileLoadCanvassDesktop`**, `Assets/Scenes/ui.unity:35799`), whose content panel is `informationPanelContent` (scene name **`InformationPanel`**, `ui.unity:59619`). Treat "File tab" throughout this document as shorthand for the *File-load (InformationPanel) workflow on FileLoadCanvassDesktop*.

---

## 1. File-tab UI surface — what the user sees

The user-facing controls of the File tab and what they bind to:

| UI control | Binding (method on `CanvassDesktop`) | Source line |
|---|---|---|
| **Browse Image** button | `BrowseImageFile()` | 306 |
| **Browse Mask** button | `BrowseMaskFile()` | 807 |
| **HDU** dropdown (`Hdu_dropdown`) | `ChangeHduSelection(TMP_Dropdown)` | 1435 |
| **Z-axis** dropdown (`Z_Dropdown`) | `updateSubsetZMax(int)` via `onValueChanged` | 614 (listener wired at 191) |
| **Load Subset** toggle (`LoadSubset_Toggle`) | `onSubsetToggleSelected(bool)` | 575 |
| **Subset Min/Max** inputs (6 × `TMP_InputField`) | `checkSubsetBounds(string)` via `onEndEdit` | 641 (listeners wired at 179–189) |
| **Load** button (`Loading_container/Button`) | `LoadFileFromFileSystem()` | 927 |
| **Aspect-ratio** dropdown (`Ratio_Dropdown`, options "X=Y=Z" / "X=Y") | `OnRatioDropdownValueChanged(int)` | 1134 (wired in scene at `ui.unity:15411` and `ui.unity:96249` — two `Ratio_Dropdown` instances) |
| **Dismiss / X** button on the file-load canvas | `DismissFileLoad()` | 1613 |
| **Tab cycling** (Tab / Shift-Tab between input fields) | `Update()` keyboard handler | 270–294 |
| **Input focus tracking** (each input calls on focus) | `SetInputIndex(int)` | 301 |

Note that the **Exit** button (`Exit()`, line 1619) lives on the same `fileLoadCanvassDesktop` but is **app-lifecycle**, not file loading. Listed here because it sits on the same canvas; it belongs in the *Client Shell* responsibility table, not the File-tab one.

---

## 2. Inspector-wired GameObjects / public fields used by the File tab

These are `public` fields on `CanvassDesktop` populated in the Unity Inspector. The File-tab workflow reads or mutates each of them:

| Field | Type | Line | Role in File tab |
|---|---|---|---|
| `cubeprefab` | `GameObject` | 44 | Instantiated at line 1078 to materialise the loaded cube |
| `informationPanelContent` | `GameObject` | 45 | **Root of the File-tab UI** — every `transform.Find(...)` chain below traverses this |
| `renderingPanelContent` | `GameObject` | 46 | Touched only at end of load (line 945) to enable mask dropdown — cross-tab coupling |
| `statsPanelContent` | `GameObject` | 47 | Touched at end of load via `populateStatsValue()` (line 950 → 1673) to write min/max/std/mean and rebuild the histogram image — cross-tab coupling, parallel to `renderingPanelContent` |
| `sourcesPanelContent` | `GameObject` | 48 | Not touched by File tab proper (Sources tab only) |
| `mainCanvassDesktop` | `GameObject` | 49 | Re-activated after load completes (line 1616 in `DismissFileLoad`, and line 966–976 button enables) |
| `fileLoadCanvassDesktop` | `GameObject` | 50 | The canvas the File tab lives on; deactivated at line 1615 |
| `VolumePlayer` | `GameObject` | 51 | Toggled off/on at lines 940–941 during post-load reset |
| `SourceRowPrefab` | `GameObject` | 52 | Sources tab only — out of scope |
| `WelcomeMenu` | `GameObject` | 54 | Hidden once load completes (line 964) |
| `LoadingText` | `GameObject` | 55 | Toggled at lines 308, 423, 962, 1017, 1437, 1462 |
| `loadTextLabel` | `TextMeshProUGUI` | 56 | Status text updated through the load coroutine (lines 422, 1020, 1023, 1042, 1073, 1121, 1461) |
| `versionText` | `TMP_Text` | 58 | Written once in `Start()` (line 207) — arguably not File tab; listed because it shares the File-tab canvas |
| `progressBar` | `GameObject` | 60 | Slider-bearing progress indicator manipulated during `LoadCubeCoroutine` (lines 963, 1018, 1025, 1043, 1074, 1092, 1129) |
| `inputFields` | `List<TMP_InputField>` | 113 | Powers Tab-cycling between subset input fields (Update lines 271–294) |
| `MenuBarBehaviour` | `MenuBarBehaviour` | 120 | Toggled at end of load (lines 979–986) to switch to VR view |
| `_tabsManager` | `TabsManager` | 128 | Not used by File tab (paint-mode only) — listed for completeness |

---

## 3. Private state owned by the File tab

These instance fields encode the working state of an in-progress file load:

| Field | Type | Line | Meaning |
|---|---|---|---|
| `_volumeDataSetRenderers` | `VolumeDataSetRenderer[]` | 40 | Cached scene query result; refreshed by `CheckCubesDataSet()` |
| `_volumeDataSetManager` | `GameObject` | 41 | Parent for instantiated cube prefabs |
| `_sourceRowObjects` | `GameObject[]` | 42 | Sources tab — out of scope |
| `_showPopUp` | `bool` | 64 | File-tab error-popup latch (set at 507, 896, 915; read each frame by `OnGUI()` at 1648; cleared at 1663) |
| `_textPopUp` | `string` | 65 | File-tab error-popup message body (set at 508, 897, 916; rendered by `ShowGUI(int)` at 1659; cleared at 1664) |
| `_volumeInputController` | `VolumeInputController` | 66 | Cached via `FindObjectOfType` — toggled during cube replacement (1054–1056, 1100–1102) |
| `_volumeCommandController` | `VolumeCommandController` | 67 | Cached via `FindObjectOfType` — receives `RemoveDataSet` (1050), `AddDataSet` (1113), `DisablePaintMode` (1058), `endThresholdEditing` (1059), `endZAxisEditing` (1060) |
| `_imagePath` | `string` | 68 | The selected FITS cube path |
| `_maskPath` | `string` | 69 | The selected mask path (`""` if none) |
| `_sourcesPath` | `string` | 70 | Sources tab — out of scope |
| `_hduSelectionIndex` | `int` | 73 | 0-based index into the HDU dropdown |
| `_imageNAxis` | `double` | 75 | Number of axes parsed from the image FITS `NAXIS` keyword |
| `_imageSize` | `double` | 76 | Product of all `NAXIS*` values for the image cube |
| `_maskNAxis` | `double` | 77 | Same, for the mask |
| `_maskSize` | `double` | 78 | Same, for the mask |
| `_subsetMin` | `int` | 80 | Lower clamp for all subset inputs (constant 1) |
| `_subsetMax_X` / `_Y` / `_Z` | `int` | 81–83 | Per-axis upper clamps derived from `NAXIS*` |
| `_subset` | `int[6]` | 84 | Current X/Y/Z min/max subset bounds chosen by the user |
| `_trueBounds` | `int[6]` | 86 | Original cube bounds (before any subset clamping) — used by the renderer |
| `_axisSize` | `Dictionary<double,double>` | 88 | `axisIndex → NAXIS<axisIndex>` for the image FITS |
| `_maskAxisSize` | `Dictionary<double,double>` | 89 | Same for the mask |
| `_ratioDropdownIndex` | `int` | 91 | 0 = X=Y=Z, 1 = X=Y, scaled Z — drives `zScale` at line 1029 |
| `_subsetToggle` | `Toggle` | 104 | Cached reference resolved in `Start()` (line 177) |
| `_subset_XMin_input` … `_subset_ZMax_input` | `TMP_InputField` ×6 | 105–110 | Cached references resolved in `Start()` (lines 178–189) |
| `_zAxisDropdown` | `TMP_Dropdown` | 111 | Cached reference resolved in `Start()` (line 190) |
| `_inputIndex` | `int` | 115 | Tab-cycle cursor over `inputFields` |
| `_loadCubeCoroutine` | `Coroutine` | 117 | Handle to the running `LoadCubeCoroutine` |
| `_showLoadDialogCoroutine` | `Coroutine` | 118 | Handle to a "show loading dialog" coroutine (referenced at 427, 901; the start of this coroutine is **not present** — see §10 *Dead/dangling references*) |

---

## 4. Lifecycle entry points that touch File-tab state

Only the File-tab-relevant slices of each Unity lifecycle method are listed; rendering-tab and stats-tab work in the same methods is excluded.

### 4.1 `Awake()` — lines 130–141
- Sets `CultureInfo.InvariantCulture` on the running thread (affects all subsequent FITS header parsing in §6).
- Subscribes to `RestFrequencyGHzListIndexChanged` / `RestFrequencyGHzChanged` on the first active renderer (rest-frequency belongs to the Rendering tab, but the subscription is made here — cross-tab coupling, **out of scope but flagged**).

### 4.2 `Start()` — lines 155–208
File-tab-specific work in `Start`:
- `FindObjectOfType<VolumeInputController>()` (line 157)
- `FindObjectOfType<VolumeCommandController>()` (line 158)
- `CheckCubesDataSet()` (line 161) — populates `_volumeDataSetRenderers` from the scene
- Lines 177–192: resolve and cache the six subset inputs, the subset toggle, and the Z-axis dropdown via a chain of `transform.Find("informationPanelContent/.../...")` calls; wire `checkSubsetBounds` to each input's `onEndEdit`, and `updateSubsetZMax` to the Z dropdown's `onValueChanged`.
- Lines 194–206: initialise the 6 subset input text values and the `_subset` / `_trueBounds` int arrays.

**Not File-tab work in `Start`:** lines 159 (`HistogramHelper` lookup — Rendering tab), 163–175 (threshold sliders — Rendering tab), 207 (`versionText` — Client shell).

### 4.3 `Update()` — lines 234–295
File-tab-relevant slice:
- Lines 270–294: Tab / Shift-Tab keyboard cycling between `inputFields` — used by the subset inputs.

**Not File-tab work in `Update`:** lines 236–268 — threshold slider clamping and colormap dropdown sync (Rendering tab).

### 4.4 `OnDestroy()` — lines 143–151
- Unsubscribes the rest-frequency handlers wired in `Awake`. Rendering-tab concern, listed for completeness.

---

## 5. Responsibility groups inside `CanvassDesktop.cs`

The eight concerns the File tab currently bundles, each with the exact methods that implement it:

### 5.1 Native OS file dialog (Open Image / Open Mask)
- `BrowseImageFile()` — 306–327
- `BrowseMaskFile()` — 807–827

Both methods follow the identical pattern: read `PlayerPrefs.GetString("LastPath")`, build a `new ExtensionFilter[]` for `.fits` / `.fit`, call `StandaloneFileBrowser.OpenFilePanelAsync(...)`, persist the new directory back into `PlayerPrefs`, and invoke the `_browseImageFile` / `_browseMaskFile` continuation.

### 5.2 FITS header / HDU inspection (called inside the dialog continuations)
- `_browseImageFile(string)` — 329–429
  - Opens the FITS file via `FitsReader.FitsOpenFile` (349)
  - Walks all HDUs via `FitsReader.FitsGetHduCount` + `FitsReader.FitsMovabsHdu` + `FitsReader.FitsReadKey` (358–379) — extracts `EXTNAME` or `HDUNAME`, falls back to `"HDU N"`
  - Populates the HDU dropdown (382–399)
  - Sets the file-path label (402–403)
  - Calls `UpdateHeaderFromFits(fptr)` (405)
  - Closes the file with `FitsReader.FitsCloseFile` (407)
  - Enables/disables Mask + Load buttons based on `IsLoadable()` (410–424)
- `UpdateHeaderFromFits(IntPtr)` — 538–569
  - Extracts every header key via `FitsReader.ExtractHeaders` (544)
  - Parses `NAXIS` and `NAXIS<n>` entries into `_imageNAxis` and `_axisSize` (552–559)
  - Writes the full header text into the scroll-view label (565–568)
- `ChangeHduSelection(TMP_Dropdown)` — 1435–1464
  - Re-opens the FITS file, moves to the user-selected HDU, re-runs the header display, and re-evaluates `IsLoadable()` to gate the Load button.

### 5.3 Cube-validity gating
- `IsLoadable()` — 431–532
  - Mixed responsibilities packed into one method:
    1. Validates that ≥ 3 axes have size > 1.
    2. Computes `_imageSize` (cumulative product).
    3. Computes per-axis subset upper bounds (`_subsetMax_X/Y/Z`).
    4. Rebuilds the Z-axis dropdown options.
    5. Sets `_showPopUp` / `_textPopUp` for the error-message popup.

### 5.4 Mask validation
- `_browseMaskFile(string)` — 829–903
  - Opens the mask FITS, extracts headers, parses `NAXIS` / `NAXIS<n>` into `_maskNAxis` / `_maskAxisSize`.
  - Compares `(_axisSize[1], _axisSize[2], _axisSize[i2+1])` against `(_maskAxisSize[1], _maskAxisSize[2], _maskAxisSize[3])` and rejects the mask with `_showPopUp = true` if mismatched (lines 891–898).
- `CheckImgMaskAxisSize()` — 905–925
  - Re-runs the same axis-size check after the user changes the Z-axis. **Inspector-wired** to a dropdown's `onValueChanged` at `Assets/Scenes/ui.unity:35385` (mode 1, no argument) — almost certainly the `Z_Dropdown`.

### 5.5 Subset bounds editor
- `onSubsetToggleSelected(bool)` — 575–590 (show/hide subset Min/Max containers)
- `setSubsetBounds()` — 595–608 (write defaults into the 6 inputs after a cube is selected)
- `updateSubsetZMax(int)` — 614–635 (rebuild Z-axis max when the Z dropdown changes)
- `checkSubsetBounds(string)` — 641–805 (a ~165-line clamping routine: per-axis Min and Max, parse-then-validate, recover from out-of-range / non-numeric input). All six inputs share this single handler.

### 5.6 Memory / load-feasibility check
- `CheckMemSpaceForCubes(string, string)` — 995–1013
  - Calls `SystemInfo.systemMemorySize`, `new FileInfo(_imagePath).Length`, and computes `nelem * sizeof(float|short)`.
  - Returns `true` if the projected cube + mask would exceed RAM (used to trigger a warning, not to block the load).

### 5.7 Load orchestration (the actual "Load" button)
- `LoadFileFromFileSystem()` — 927–930 — thin wrapper that starts `LoadCubeCoroutine`.
- `LoadCubeCoroutine(string, string, int)` — 1015–1132 — the big one. Phases:
  1. Show loading UI, warn on RAM overflow (1017–1022)
  2. Compute `zScale` based on `_ratioDropdownIndex` (1028–1039)
  3. Tear down any existing renderer: deactivate, `_volumeCommandController.RemoveDataSet`, `DisablePaintMode`, `endThresholdEditing`, `endZAxisEditing`, `Data.CleanUp`, `Mask.CleanUp`, `Destroy(firstActiveRenderer)` (1041–1071)
  4. Instantiate `cubeprefab`, set subset bounds, file paths, HDU, load text, progress bar, depth axis (1078–1095)
  5. `CheckCubesDataSet()` to refresh the cached renderer list (1097)
  6. Bounce `VolumeInputController` activation, bounce `FeatureMenuController` activation (1100–1111)
  7. `_volumeCommandController.AddDataSet(...)`, start `_startFunc()` on the new renderer, wait for `started == true` (1113–1119)
  8. Final UI text + progress bar tick (1121–1129)
  9. `postLoadFileFileSystem()` (1131)
- `postLoadFileFileSystem()` — 935–987
  - Bounces `VolumePlayer` activation (940–941)
  - Enables the Rendering-tab mask dropdown if a mask was loaded (945–947)
  - Calls `populateColorMapDropdown()` — Rendering-tab cross-tab effect (949)
  - Calls `populateStatsValue()` (950) — Stats-tab cross-tab effect; also regenerates the histogram image via `_histogramHelper.CreateHistogramImg(...)` at line 1679
  - `PopulateRestfreqencyDropdown()` (953) — Rendering-tab dropdown rebuild
  - `SetRestFrequencyInputInteractable(false)` (954) and `SetRestFrequencyInputField(...)` (955) — two further mutations of the Rendering-tab rest-frequency input
  - **Re-subscribes** `RestFrequencyGHzListIndexChanged` and `RestFrequencyGHzChanged` to the new active renderer (958–959) **without first unsubscribing the previous renderer's handlers** — repeated cube loads accumulate subscriptions. Cross-tab leak; see §10 anomaly 8
  - Hides `LoadingText`, `progressBar`, `WelcomeMenu` (962–964)
  - Enables the Rendering / Stats / Sources / Paint tab buttons (966–973) — the File tab is responsible for unlocking the other tabs after a successful load
  - Click-invokes the Stats tab button (975–976)
  - Switches the menu bar away from the About section and toggles the VR view (979–986)

### 5.8 Canvas / popup chrome
- `DismissFileLoad()` — 1613–1617 — swaps from `fileLoadCanvassDesktop` to `mainCanvassDesktop`.
- `_showPopUp` / `_textPopUp` — set in three places (lines 507, 896, 915). **Rendered inside the same file** by Unity's legacy IMGUI:
  - `OnGUI()` — 1646–1654 — every frame, if `_showPopUp` is true, draws a red `GUI.Window` titled "Invalid Cube".
  - `ShowGUI(int)` — 1656–1667 — window contents: a `GUI.Label` showing `_textPopUp` and an "OK" button that clears both fields.
  - These IMGUI methods are themselves a File-tab responsibility and a target for replacement in the worked example (legacy `OnGUI` → UI Toolkit / ViewModel-bound popup view).

---

## 6. External collaborators called from the File tab

Anything `CanvassDesktop.cs` invokes that lives outside the file. These must each appear in the worked-example responsibility table even if owned by another sub-team — they are the seams where the anti-corruption layer goes.

| Collaborator | Where used | Notes |
|---|---|---|
| `StandaloneFileBrowser` (SFB, third-party native dialog plugin) | 317, 817, 1246, 1310, 1477 | `OpenFilePanelAsync` and `SaveFilePanelAsync`. Natural target for an `IFileDialogService` abstraction shared with the Sources tab and the Debug tab. |
| `FitsReader` (native C++ plugin wrapper) | 349, 358, 363, 365, 369, 381, 407, 538-544, 842, 855, 856, 1441, 1445, 1447 | 13 distinct call sites across the File tab. **The single biggest reason the File tab is untestable today** — every header read goes straight into a `[DllImport]`. |
| `VolumeCommandController` | 158 (lookup), 1050, 1058, 1059, 1060, 1113 | Owned by Sub-team 1 (architecture). The File tab's primary command sink. |
| `VolumeInputController` | 157 (lookup), 1054–1056, 1100–1102 | Activation-bouncing during cube swap. |
| `VolumeDataSetRenderer` | 40, 1085–1095, 1113, 1116 | The renderer scripted onto the instantiated cube prefab; File tab populates its fields directly (`subsetBounds`, `trueBounds`, `FileName`, `MaskFileName`, `SelectedHdu`, `CubeDepthAxis`, etc.). |
| `VolumeDataSet` | 1068–1069 | `Data.CleanUp` and `Mask?.CleanUp` during teardown. |
| `FeatureMenuController` | 1105 (`FindObjectOfType`) | Activation bounce during cube swap (so the new cube's source list re-binds). |
| `Config` | not directly called by File tab; called indirectly via `DebugLogging` only | Listed here only to mark its **absence** — the File tab does not currently route any setting through `Config`. |
| `PlayerPrefs` | 309, 321–322, 809, 821–822, 1238, 1250–1251, 1302, 1314–1315, 1468, 1481–1482 | The `"LastPath"` string key. Shared with the Sources tab and Mapping tab; the Debug tab uses a separate `"LastDebugPath"` key. |
| `SystemInfo.systemMemorySize` | 997 | Used by `CheckMemSpaceForCubes`. |
| `FileInfo(...).Length` | 998 | Used by `CheckMemSpaceForCubes`. |
| `MenuBarBehaviour` (`AboutSection`, `VRViewDisplay`, `ToggleAboutSection`, `ToggleVRViewDisplay`) | 979–986 | Post-load UI choreography. |
| `SteamVR`, `OpenVR.Shutdown()` | 1623–1625 | Only used by `Exit()` — **not** part of File-tab proper; flagged. |
| `Application.Quit()` | 1627 | Same as above. |
| Coroutine machinery (`StartCoroutine`, `StopCoroutine`, `WaitForSeconds`, `IEnumerator`) | 117–118, 427–428, 901–902, 929, 938, 1017–1130 passim | Used to gate UI updates during the multi-second load. |
| `UnityEngine.Object.Instantiate(cubeprefab, ...)` | 1078 | Materialises the loaded cube. |
| `UnityEngine.Object.Destroy(firstActiveRenderer)` | 1070 | Teardown. |
| `GetFirstActiveRenderer()` (internal helper, public method on this same class) | 943, 1041 in the File-tab load flow (also 1137, 1158, 1166, 1175, 1185, 1709, 1737, 1751, 1772, 1836, 1845, 1854, 1878, 1883 elsewhere) | Walks `_volumeDataSetRenderers` and returns the first `isActiveAndEnabled` one. The File-tab teardown/setup at `postLoadFileFileSystem` and `LoadCubeCoroutine` both depend on this. Worth abstracting behind an `IActiveDatasetProvider` in the worked example. |
| `Unity legacy IMGUI` (`GUI.Window`, `GUI.Label`, `GUI.Button`) | 1651, 1659, 1661 | The "Invalid Cube" error popup rendered by `OnGUI()`/`ShowGUI(int)`. Legacy `UnityEngine`-namespaced API; target for replacement by a ViewModel-bound popup view. |

---

## 7. Scene-hierarchy assumptions (the `transform.Find` chains)

The File tab hard-codes ~30 paths into the Unity scene. Each is a brittle, untestable coupling. Listed here exhaustively so they can each be replaced by a binding in the worked example.

Rooted at `informationPanelContent`:
- `HeaderTitle_container/Hdu_container` (used at 382–394)
- `HeaderTitle_container/Hdu_container/Hdu_dropdown` (used at 384, 385, 391, 394)
- `ImageFile_container/ImageFilePath_text` (402–403)
- `MaskFile_container/Button` (339–340, 412, 419, 451, 458, 884, 917, 921)
- `MaskFile_container/MaskFilePath_text` (341–342, 847, 894, 914)
- `Loading_container/Button` (343–344, 413, 420, 452, 459, 884, 917, 921)
- `SubsetSelection_container` (414, 421, 453, 460, 885, 922)
- `Axes_container` (485)
- `Header_container/Scroll View/Viewport/Content/Header` (565–566)
- `Header_container/Scroll View/Scrollbar Vertical` (567–568)
- `SubsetLabel_container` (579, 586)
- `SubsetMin_container` (178, 180, 182, 580, 587) — children `SubsetX_min`, `SubsetY_min`, `SubsetZ_min`
- `SubsetMax_container` (184, 186, 188, 581, 588) — children `SubsetX_max`, `SubsetY_max`, `SubsetZ_max`
- `SubsetSelection_container/LoadSubset_Toggle` (177)
- `Axes_container/Z_Dropdown` (190)

Rooted at `mainCanvassDesktop`:
- `RightPanel/Tabs_ container/Rendering_Button`, `Stats_Button`, `Sources_Button`, `Paint_Button` (966–976)

Rooted at `renderingPanelContent` (touched as **cross-tab effect** at end of load):
- `Rendering_container/Viewport/Content/Settings/Mask_container/Dropdown_mask` (945–947) — enabled iff a mask was loaded.

These string paths are also the reason every File-tab interaction triggers a deep `GetComponent` walk and cannot be unit-tested without the full Unity scene loaded.

---

## 8. Persistence touch points

| Mechanism | Key / path | Where set | Where read |
|---|---|---|---|
| `PlayerPrefs` string | `"LastPath"` | 321, 822, 1251, 1314, 1481 | 309, 809, 1238, 1302, 1468 |
| `PlayerPrefs.Save()` | — | 322, 822, 1251, 1315, 1482 | n/a |
| In-memory cube data | `_imagePath`, `_maskPath` | 335, 837 | passed into `LoadCubeCoroutine` at 929; written onto `VolumeDataSetRenderer.FileName` / `MaskFileName` at 1088–1089 |
| Filesystem read | the FITS files themselves | n/a | every `FitsReader.*` call in §6 |

The File tab does **not** read or write any configuration file (`Config.json`, `PlayerPrefs` apart from `"LastPath"`, or `Application.persistentDataPath`). Settings that affect file loading (e.g., `numberOfLogsToKeep` in `Config`) are not consumed here.

---

## 9. Adjacent workflows that share plumbing but are **not** File tab

These live in `CanvassDesktop.cs` and look superficially like File-tab code, but they populate `sourcesPanelContent`, not `informationPanelContent`. They are formally part of the **Sources tab** workflow.

| Method | Lines | Belongs to |
|---|---|---|
| `BrowseSourcesFile()` | 1236–1256 | Sources tab |
| `_browseSourcesFile(string)` | 1258–1298 | Sources tab |
| `BrowseMappingFile()` | 1300–1320 | Sources tab |
| `_browseMappingFile(string)` | 1322–1428 | Sources tab |
| `SaveMappingFile()` | 1466–1487 | Sources tab |
| `_saveMappingFile(string)` | 1489–1524 | Sources tab |
| `ChangeSourceMapping(int, SourceMappingOptions)` | 1526–1539 | Sources tab |
| `AreMappingsIncompatible(...)` | 1541–1553 | Sources tab |
| `AreMinimalMappingsSet()` | 1555–1577 | Sources tab |
| `LoadSourcesFile()` | 1579–1611 | Sources tab |

**Why they appear here at all:** they share `StandaloneFileBrowser`, the `"LastPath"` PlayerPref, and the same `FitsReader.*` calls (for VOTable-vs-FITS-binary-table detection inside `FeatureTable`). A clean refactor of the File tab into an `IFileDialogService` and an `IFitsReader` should make these reusable for the Sources tab too — that is the architectural payoff of Worked Example 1, even though only the File tab is the formal worked example.

---

## 10. Anomalies / dead references / things to verify with a Unity scene reviewer

Items that the static reading of `CanvassDesktop.cs` cannot resolve:

1. **`_showLoadDialogCoroutine` is never started.** It is only `StopCoroutine`'d at lines 427–428 and 901–902. A repo-wide grep confirms no `StartCoroutine` assignment anywhere in `Assets/`. **Verdict: dead state**, safe to delete in the worked example.
2. **`_showPopUp` / `_textPopUp` are *not* read externally — they are rendered in this same file.** Earlier draft mis-stated this. `OnGUI()` at line 1648 reads `_showPopUp`; `ShowGUI(int)` at line 1659 reads `_textPopUp` and clears both at 1663–1664. The popup is an in-file IMGUI window. See §5.8 for the corrected responsibility entry.
3. **`CheckImgMaskAxisSize()` is `public` but never invoked from within `CanvassDesktop.cs`.** Confirmed Inspector-wired to a dropdown's `onValueChanged` at `Assets/Scenes/ui.unity:35385` (mode 1, no argument — consistent with the `Z_Dropdown`). Must appear as an Inspector binding in the responsibility map.
4. **`SetInputIndex(int)` is `public` and called from somewhere external** — confirmed Inspector-wired **six times** at `ui.unity:11298, 25425, 36405, 43465, 68717, 94378`, one per subset input field's `OnSelect`.
5. **`OnRatioDropdownValueChanged(int)` is `public`** and called externally — confirmed Inspector-wired at `ui.unity:15411` and `ui.unity:96249` to two GameObjects both named `Ratio_Dropdown` (one inactive, one active) whose options are literally "X=Y=Z" / "X=Y". These dropdowns sit on the **file-load modal**, not the Rendering tab; their value (`_ratioDropdownIndex`) is consumed inside `LoadCubeCoroutine` at line 1029 to derive `zScale`. The coupling is *intra-File-tab*, not cross-tab — but the dropdown is hard-wired to a UI element rather than passed through a ViewModel command, which is the smell to break in the worked example.
6. **`paintTabSelected()` / `paintTabLeft()`** (lines 1886, 1896) are called by `TabsManager` but unrelated to File-tab work. Listed for completeness only.
7. **The "Information" panel name in code (`informationPanelContent`) vs. the user-facing label "File tab".** Confirmed: the scene names are `InformationPanel` (`ui.unity:59619`) and `FileLoadCanvassDesktop` (`ui.unity:35799`). The main canvas `Tabs_ container` contains only Rendering / Stats / Sources / Paint buttons — there is no user-facing "File" tab. The image-loading workflow is a *separate modal canvas*, not a tab. See the Terminology note at the top of this document.
8. **Event-handler subscription leak in `postLoadFileFileSystem`.** Lines 958–959 add `OnRestFrequencyIndexOfDatasetChanged` and `OnRestFrequencyOfDatasetChanged` to the new active renderer **without unsubscribing the previous renderer's handlers** (the only unsubscribe is in `OnDestroy` at lines 143–151, against the renderer captured at `Awake`). Each cube reload therefore accumulates one extra subscription. Cross-tab defect introduced by the File-tab load path; should be called out as something the worked-example refactor fixes by routing the subscription through a ViewModel/service with a clear lifecycle.

---

## 11. Out of scope (explicitly **not** File tab)

To prevent scope creep in the responsibility table, the following are documented as **explicitly excluded** from Worked Example 1:

| Concern | Owner |
|---|---|
| Debug tab UI / log subscription | `Assets/Scripts/Debuggers/DebugLogging.cs` — Worked Example 2 |
| Native-plugin debug bridge | `Assets/Scripts/Debuggers/FitsReaderDebug.cs` — Worked Example 2 |
| Threshold slider wiring (Rendering tab) | `CanvassDesktop.cs` lines 95–99, 163–175, 1719–1864 (separate worked example or out of scope entirely) |
| Colormap dropdown | `CanvassDesktop.cs` 1687–1717 |
| Stats panel population | `CanvassDesktop.cs` `populateStatsValue` 1669–1686 |
| Rest-frequency dropdown logic | `CanvassDesktop.cs` 210–218, 1156–1234 |
| Tab switching itself | `Assets/Scripts/Menu/TabsManager.cs` |
| Paint-mode entry/exit | `CanvassDesktop.cs` 1886–1898 |
| `Exit()` / SteamVR shutdown | `CanvassDesktop.cs` 1619–1628 |
| Sources tab file browsing / mapping (see §9) | Sources tab — adjacent only |
| WelcomeMenu visibility lifecycle | Co-owned; touched at line 964 but not File-tab business logic |

---

## 12. Coverage claim

Every responsibility group in §5, every collaborator in §6, and every scene path in §7 maps directly back to one or more line ranges in `Assets/Scripts/UI/CanvassDesktop.cs`. There are **no `File`-, `Image`-, `Mask`-, `Fits`-, `Subset`- or `Hdu`-related methods** in that file that are not listed here. The verifier prompt in `file-tab-scope-verification-prompt.md` should be run against this document and the source file to confirm.
