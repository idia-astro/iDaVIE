# CanvassDesktop.cs — Method Reference

**File:** `Assets/Scripts/UI/CanvassDesktop.cs`
**Class:** `CanvassDesktop : MonoBehaviour`
**Size:** ~1900 lines

This is the primary God-object for the desktop UI. It owns file loading, mask loading, source catalogue loading, rendering controls, statistics, subset selection, and paint-mode integration — all in a single `MonoBehaviour`. Everything described below is a code smell audit as well as a reference.

---

## Unity Lifecycle Methods

### `Awake()`
**Used:** Yes — called automatically by Unity before `Start`.

Sets the thread culture to `InvariantCulture` so float parsing is locale-independent. Subscribes to two events on the first active `VolumeDataSetRenderer`: `RestFrequencyGHzListIndexChanged` and `RestFrequencyGHzChanged`. These drive the rest-frequency UI in the Rendering tab.

---

### `OnDestroy()`
**Used:** Yes — called automatically by Unity when the object is destroyed.

Unsubscribes the same two rest-frequency events to avoid memory leaks and dangling delegates. Mirror of `Awake`.

---

### `Start()`
**Used:** Yes — called automatically by Unity on the first frame.

The heaviest initialisation method in the class. Does all of the following in sequence:
- Uses `FindObjectOfType<>` to locate `VolumeInputController`, `VolumeCommandController`, and `HistogramHelper` — the classic singleton-coupling smell.
- Calls `CheckCubesDataSet()` to populate the internal renderer array.
- Navigates deep scene-hierarchy paths with chained `transform.Find(…).GetComponent<…>()` calls to wire up `_minThreshold`, `_maxThreshold`, subset input fields, and the Z-axis dropdown. These paths are hardwired strings and break silently if the hierarchy changes.
- Registers `checkSubsetBounds` as a listener on all six subset input fields' `onEndEdit` events.
- Registers `updateSubsetZMax` on the Z-axis dropdown's `onValueChanged`.
- Sets initial text values for the subset inputs.
- Writes the app version string to `versionText`.

---

### `Update()`
**Used:** Yes — called automatically by Unity every frame.

Runs two unrelated concerns per frame:
1. **Threshold sync** — if a `VolumeDataSetRenderer` is active, syncs slider values and label text to the renderer's current `ThresholdMin`/`ThresholdMax` and `ScaleMin`/`ScaleMax`. Also tracks colormap changes and updates the dropdown.
2. **Tab-key cycling** — reads `Input.GetKeyDown(KeyCode.Tab)` and cycles focus through the `inputFields` list (Shift+Tab goes backwards). This is UI keyboard-nav that belongs in a dedicated input handler.

---

### `OnGUI()`
**Used:** Yes — called automatically by Unity's legacy IMGUI system every frame.

Renders a red modal pop-up window when `_showPopUp` is `true`. Uses the old Unity IMGUI API (`GUI.Window`). The pop-up is triggered whenever a FITS file or mask file validation fails.

---

### `ShowGUI(int windowID)`
**Used:** Yes — called as the window-function delegate inside `OnGUI`.

Draws the label and OK button inside the pop-up window. Clears `_showPopUp` and `_textPopUp` when the user clicks OK.

---

## Data Initialisation Helpers

### `CheckCubesDataSet()`
**Used:** Yes — called by `Start` and `LoadCubeCoroutine`.

Finds the `VolumeDataSetManager` `GameObject` by name and retrieves all `VolumeDataSetRenderer` children (including inactive ones). Updates the private `_volumeDataSetRenderers` array. If the manager does not exist, sets the array to empty.

---

### `GetFirstActiveRenderer()`
**Used:** Yes — called internally throughout the class and externally by `DesktopPaintController.cs`.

Iterates `_volumeDataSetRenderers` and returns the first one that is not null and is both active and enabled. Returns `null` if none qualify. This is the central accessor for the "current dataset" — every rendering and stats operation routes through it.

---

## File Tab — Image Loading

### `BrowseImageFile()`
**Used:** Yes — wired to the "Browse Image" button in the Unity scene (public).

Opens an async file picker (`StandaloneFileBrowser`) filtered to `.fits`/`.fit` files. On selection, persists the directory to `PlayerPrefs` and delegates to `_browseImageFile`.

---

### `_browseImageFile(string path)`
**Used:** Yes — called only by `BrowseImageFile`.

The main workhorse for image file selection:
- Reads the FITS file with `FitsReader` to enumerate HDUs.
- Populates the HDU dropdown if there are multiple HDUs.
- Updates the image file path label.
- Calls `UpdateHeaderFromFits` to show the FITS header in the scroll view.
- Calls `IsLoadable()` to check if the cube is valid.
- Enables or disables the mask-browse button, load button, and subset selector depending on validity.
- Stops any in-progress `_showLoadDialogCoroutine`.

---

### `IsLoadable()`
**Used:** Yes — called by `_browseImageFile` and `ChangeHduSelection`.

Validates whether the selected FITS file represents a loadable 3-D cube. Rules:
- `NAXIS` must be > 2.
- Exactly 3 axes must have size > 1 (for `NAXIS == 3`).
- For `NAXIS > 3`: if more than 3 axes have size > 1, enables the Z-axis dropdown and lets the user pick which axis to treat as depth.
- Sets `_subsetMax_X/Y/Z` to the discovered axis sizes.
- Populates the Z-axis dropdown regardless.
- Triggers the error pop-up if the cube is invalid.

Returns `true` if the file can be loaded.

---

### `UpdateHeaderFromFits(IntPtr fptr)`
**Used:** Yes — called by `_browseImageFile` and `ChangeHduSelection`.

Calls `FitsReader.ExtractHeaders` to get all FITS header key-value pairs. While iterating, intercepts `NAXIS`-prefixed keys to populate `_imageNAxis` and `_axisSize`. Writes the full header as formatted text into the scroll-view `TextMeshProUGUI` component, then resets the scrollbar to the top.

---

### `ChangeHduSelection(TMP_Dropdown dropdown)`
**Used:** Yes — wired to the HDU dropdown's `onValueChanged` in the Unity scene (public).

When the user picks a different HDU from the dropdown, re-opens the FITS file, moves to the selected HDU with `FitsMovabsHdu`, refreshes the header display, and re-runs `IsLoadable()` to update the load/mask button states.

---

## File Tab — Mask Loading

### `BrowseMaskFile()`
**Used:** Yes — wired to the "Browse Mask" button in the Unity scene (public).

Same pattern as `BrowseImageFile`: opens an async FITS file picker and delegates to `_browseMaskFile`.

---

### `_browseMaskFile(string path)`
**Used:** Yes — called only by `BrowseMaskFile`.

Reads the mask FITS file headers to extract `NAXIS` values into `_maskAxisSize`. Validates that the mask dimensions match the image cube dimensions (axis 1, axis 2, and the selected Z axis must be equal). Enables the load button on match; shows a pop-up and clears `_maskPath` on mismatch.

---

### `CheckImgMaskAxisSize()`
**Used:** Unclear — not referenced in any other script found. Appears to be dead code or a leftover that was once wired to the Z-axis dropdown as a secondary validator. Its logic duplicates part of `_browseMaskFile`.

Compares `_axisSize` against `_maskAxisSize` for the currently selected Z axis. Enables or disables the load button accordingly.

---

## File Tab — Subset Selection

### `onSubsetToggleSelected(bool val)`
**Used:** Yes — wired to the "Load Subset" toggle's `onValueChanged` in the Unity scene (public).

Shows or hides the subset label, min-input row, and max-input row when the toggle changes. Also focuses the currently indexed input field when the subset is enabled.

---

### `setSubsetBounds()`
**Used:** Yes — called by `_browseImageFile` and `ChangeHduSelection` after a valid file is chosen.

Resets all six subset input fields to their min/max values (`_subsetMin` and `_subsetMax_X/Y/Z`). Syncs both `_subset` and `_trueBounds` arrays to these values.

---

### `updateSubsetZMax(int val = 0)`
**Used:** Yes — registered as a listener on the Z-axis dropdown in `Start`; also called directly by `IsLoadable`.

When the Z-axis selection changes, looks up the new max-Z from `_axisSize` and clamps the current Z-max input field to the new range. Updates the `_subset` array.

---

### `checkSubsetBounds(string val1 = "")`
**Used:** Yes — registered as `onEndEdit` listener on all six subset input fields in `Start`.

Validates all six subset input fields in sequence. For each max field: clamps below `_subsetMin`, above the axis max, or below the corresponding min. For each min field: clamps below `_subsetMin`, above the axis max, or above the corresponding max. Parses and writes back corrected values, then updates the `_subset` array.

The method re-validates all fields every time any single field is edited, which is redundant and can produce confusing re-corrections.

---

## File Tab — Load Execution

### `LoadFileFromFileSystem()`
**Used:** Yes — wired to the "Load" button in the Unity scene (public).

Trivial entry point: starts `LoadCubeCoroutine` with the current `_imagePath`, `_maskPath`, and `_hduSelectionIndex + 1`.

---

### `CheckMemSpaceForCubes(string _imagePath, string _maskPath)`
**Used:** Yes — called only by `LoadCubeCoroutine`.

Calculates the expected in-memory size of the image cube subset (as `float`) and the mask (as `short`) in MB. Compares against `SystemInfo.systemMemorySize`. Returns `true` if the total would exceed available RAM (the caller shows a warning but proceeds anyway).

---

### `LoadCubeCoroutine(string _imagePath, string _maskPath, int hduSelection = 1)`
**Used:** Yes — started by `LoadFileFromFileSystem`; reference stored in `_loadCubeCoroutine` for cancellation.

The main data-loading coroutine. In order:
1. Shows the loading text and progress bar; warns if cube exceeds RAM.
2. Computes `zScale` for the non-uniform-axis ratio mode.
3. If a renderer already exists, deactivates it, removes it from `VolumeCommandController`, resets `VolumeInputController`, disables paint mode and editing, destroys the renderer and its data.
4. Instantiates the cube prefab, parents it to `VolumeDataSetManager`, sets all load parameters, and hands the progress bar reference to the new renderer.
5. Toggles `VolumeInputController` and `FeatureMenuController` off/on to force re-initialisation.
6. Calls `_volumeCommandController.AddDataSet` and starts the renderer's own `_startFunc` coroutine.
7. Waits for `started == true`, then calls `postLoadFileFileSystem()`.

This method directly couples file I/O, scene management, coroutine orchestration, and UI updates — the clearest SRP violation in the file.

---

### `postLoadFileFileSystem()`
**Used:** Yes — called only at the end of `LoadCubeCoroutine`.

Post-load UI clean-up:
- Stops any active `_loadCubeCoroutine`.
- Cycles `VolumePlayer` off/on.
- Enables the mask dropdown if a mask was loaded.
- Calls `populateColorMapDropdown`, `populateStatsValue`, `PopulateRestfreqencyDropdown`.
- Sets rest frequency input field and subscribes rest-frequency events on the new renderer.
- Hides loading text and progress bar.
- Enables the Rendering, Stats, Sources, and Paint tab buttons.
- Auto-clicks the Stats tab.
- Toggles the `MenuBarBehaviour` About/VRView sections.

---

## Rendering Tab — Rest Frequency

### `PopulateRestfreqencyDropdown()`
**Used:** Yes — called only by `postLoadFileFileSystem`.

Clears and repopulates the rest-frequency `TMP_Dropdown` from the active renderer's `RestFrequencyGHzList` keys.

---

### `OnRestFrequencyIndexOfDatasetChanged()`
**Used:** Yes — registered as event handler on `RestFrequencyGHzListIndexChanged` in `Awake` and `postLoadFileFileSystem`.

When the renderer's selected rest-frequency index changes (e.g. from a VR menu), calls `SetRestFrequencyDropdown` to sync the desktop dropdown.

---

### `OnRestFrequencyOfDatasetChanged()`
**Used:** Yes — registered as event handler on `RestFrequencyGHzChanged` in `Awake` and `postLoadFileFileSystem`.

When the renderer's rest frequency value changes, calls `SetRestFrequencyInputField` to sync the desktop text input.

---

### `OnRestFrequencyDropdownValueChanged(int optionIndex)`
**Used:** Yes — wired to the rest-frequency dropdown in the Unity scene (public).

Writes the selected index back to `GetFirstActiveRenderer().RestFrequencyGHzListIndex`.

---

### `OnRestFrequencyValueChanged(string val)`
**Used:** Yes — wired to the rest-frequency text input in the Unity scene (public).

Parses the string as a `double`, updates the `"Custom"` entry in `RestFrequencyGHzList`, and if `OverrideRestFrequency` is set, also writes it to `RestFrequencyGHz`.

---

### `SetRestFrequencyInputInteractable(bool isInteractable)`
**Used:** Yes — called by `postLoadFileFileSystem` and `SetRestFrequencyDropdown`.

Enables or disables the rest-frequency `TMP_InputField` via a long `transform.Find` chain.

---

### `SetRestFrequencyInputField(double restFrequency)`
**Used:** Yes — called by `postLoadFileFileSystem` and `OnRestFrequencyOfDatasetChanged`.

Sets the rest-frequency input field text to the given value.

---

### `SetRestFrequencyDropdown(int index)`
**Used:** Yes — called by `OnRestFrequencyIndexOfDatasetChanged`.

Sets the dropdown value and adjusts whether the custom input field is interactable: disabled for index 0 (default) or any preset, enabled only when "Custom" (last index) is selected.

---

## Rendering Tab — Colour Map

### `populateColorMapDropdown()`
**Used:** Yes — called only by `postLoadFileFileSystem`.

Clears the colormap dropdown and re-adds one option per value in `ColorMapEnum`. Sets the initial selection from `Config.Instance.defaultColorMap`.

---

### `ChangeColorMap()`
**Used:** Yes — wired to the colormap dropdown's `onValueChanged` in the Unity scene (public).

Reads the dropdown's current value, converts it to a `ColorMapEnum` via `ColorMapUtils.FromHashCode`, and assigns it to the active renderer's `ColorMap`.

---

## Rendering Tab — Aspect Ratio

### `OnRatioDropdownValueChanged(int optionIndex)`
**Used:** Yes — wired to the ratio dropdown in the Unity scene (public).

Stores the ratio mode index. If a renderer is active:
- Index 0 (X=Y=Z): sets `ZScale = XScale`.
- Index 1 (X=Y): calculates a proportional `ZScale` from cube depth vs width.

---

## Stats Tab

### `populateStatsValue()`
**Used:** Yes — called by `postLoadFileFileSystem` and `RestoreDefaults`.

Reads `MinValue`, `MaxValue`, `StanDev`, and `MeanValue` from the active dataset and writes them to the stats panel input fields/text components. Also calls `HistogramHelper.CreateHistogramImg` to regenerate the histogram image.

---

### `UpdateUI(float min, float max, Sprite img)`
**Used:** Yes — called externally by `HistogramHelper.cs`.

Updates the min/max input fields in the stats panel and sets the histogram image sprite. Acts as a callback from `HistogramHelper` after it redraws the histogram.

---

### `UpdateSigma(Int32 optionIndex)`
**Used:** Yes — wired to the sigma dropdown in the Unity scene (public).

Reads the current min/max from the stats input fields and regenerates the histogram image with a new sigma multiplier (`optionIndex + 1`).

---

### `RestoreDefaults()`
**Used:** Yes — wired to the "Restore Defaults" button in the Unity scene (public).

Resets the sigma dropdown to 0, calls `VolumeDataSet.UpdateHistogram` with the dataset's absolute min/max, resets renderer thresholds, and repopulates the stats panel.

---

### `UpdateScale(float min, float max)`
**Used:** Yes — called by `SetMaxMinPercentile`, `UpdateScaleMin`, and `UpdateScaleMax`.

Updates `ScaleMin`/`ScaleMax` on the active renderer, calls `VolumeDataSet.UpdateHistogram` and `HistogramHelper.CreateHistogramImg` to refresh the display, and resets thresholds.

---

### `SetMaxMinPercentile(float maxPercentile)`
**Used:** Yes — wired to percentile buttons in the Unity scene (public).

Calculates min/max data values at the given percentile pair (e.g. 99.5% → [0.5%, 99.5%]). Supports three modes: full range (100%), quick-mode from histogram bins, or precise mode from raw FITS data. Delegates to `UpdateScale`.

---

### `UpdateScaleMin(string minString)`
**Used:** Yes — wired to the min input field's `onEndEdit` in the Unity scene (public).

Reads the current max from the stats panel input field, parses the new min from the string argument, and calls `UpdateScale`.

---

### `UpdateScaleMax(string maxString)`
**Used:** Yes — wired to the max input field's `onEndEdit` in the Unity scene (public).

Mirror of `UpdateScaleMin` for the max value.

---

## Rendering Tab — Thresholds

### `UpdateThresholdMin(float value)`
**Used:** Yes — wired to the min threshold slider's `onValueChanged` in the Unity scene (public).

Clamps the value between 0 and `ThresholdMax` and writes it to the active renderer.

---

### `UpdateThresholdMax(float value)`
**Used:** Yes — wired to the max threshold slider's `onValueChanged` in the Unity scene (public).

Clamps the value between `ThresholdMin` and 1 and writes it to the active renderer.

---

### `ResetThresholds()`
**Used:** Yes — wired to a button in the Unity scene (public); also called internally by `RestoreDefaults` (indirectly via `firstActiveRenderer.ResetThresholds()`).

Resets both sliders and the renderer's threshold values to their initial values captured at load time.

---

## Sources Tab

### `BrowseSourcesFile()`
**Used:** Yes — wired to the "Browse Sources" button in the Unity scene (public).

Opens an async file picker for `.xml`/`.fits`/`.fit` files and delegates to `_browseSourcesFile`.

---

### `_browseSourcesFile(string path)`
**Used:** Yes — called only by `BrowseSourcesFile`.

Loads the source catalogue file, reads its column names via `FeatureTable.GetFeatureTableFromFile`, and dynamically instantiates a `SourceRowPrefab` per column in the sources scroll view. Stores the row GameObjects in `_sourceRowObjects`. Enables the mapping-file browse button and sources-load button.

---

### `BrowseMappingFile()`
**Used:** Yes — wired to the "Browse Mapping" button in the Unity scene (public).

Opens an async file picker for `.json` files and delegates to `_browseMappingFile`.

---

### `_browseMappingFile(string path)`
**Used:** Yes — called only by `BrowseMappingFile`.

Loads a `FeatureMapping` JSON and applies it to the source row GameObjects: sets each row's `Coord_dropdown` and `Import_toggle` to match the saved mapping. Handles parse errors per-row and logs them without aborting.

---

### `SaveMappingFile()`
**Used:** Yes — wired to the "Save Mapping" button in the Unity scene (public).

Opens an async save-file dialog for `.json` and delegates to `_saveMappingFile`.

---

### `_saveMappingFile(string path)`
**Used:** Yes — called only by `SaveMappingFile`.

Iterates `_sourceRowObjects`, collects current `SourceMappingOptions` values and import-toggle states, builds a `Mapping` object, wraps it in a `FeatureMapping`, and serialises it to the chosen path.

---

### `ChangeSourceMapping(int sourceIndex, SourceMappingOptions option)`
**Used:** Yes — called externally by `SourceRow.cs` when a row's dropdown changes.

Enforces mutual exclusivity: iterates all source rows except the changed one and clears any mapping that is incompatible with the new selection, using `AreMappingsIncompatible`.

---

### `AreMappingsIncompatible(SourceMappingOptions option1, SourceMappingOptions option2)`
**Used:** Yes — called only by `ChangeSourceMapping`.

Returns `true` if two mapping options cannot coexist: same option selected twice, pixel coordinates mixed with WCS coordinates, or velocity/frequency/redshift used simultaneously.

---

### `AreMinimalMappingsSet()`
**Used:** Yes — called only by `LoadSourcesFile`.

Checks that the minimum set of spatial mappings is present: either (X, Y, Z), or (RA, Dec, + one of Freq/Velo/Redshift), or at least Xmin. Also validates that if any bounding-box corner is set, all six corners must be set.

---

### `LoadSourcesFile()`
**Used:** Yes — wired to the "Load Sources" button in the Unity scene (public).

Validates minimal mappings, then calls `featureSetManager.ImportFeatureSetFromTable` with the final column-to-role mapping and import mask. Updates the load status text to green on success or red if mappings are incomplete.

---

## Paint Tab

### `paintTabSelected()`
**Used:** Yes — called externally by `TabsManager.cs` when the Paint tab is activated.

Opens the quick-menu paint submenu, hides the VR map display, shows `RegionCubeDisplay`, and starts desktop paint selection.

---

### `paintTabLeft()`
**Used:** Yes — called externally by `TabsManager.cs` when the Paint tab is deactivated.

Calls `paintMenuController.ExitPaintMode()` to clean up paint state.

---

## Input Handling

### `SetInputIndex(int newIndex)`
**Used:** Yes — intended to be wired to each input field's `onSelect` event in the Unity scene (public).

Updates `_inputIndex` so that the Tab-key cycling in `Update` starts from the correct field.

---

## Navigation

### `DismissFileLoad()`
**Used:** Yes — wired to the "Dismiss" button on the file-load overlay in the Unity scene (public).

Hides `fileLoadCanvassDesktop` and shows `mainCanvassDesktop`.

---

### `Exit()`
**Used:** Yes — wired to the "Exit" button in the Unity scene (public).

Stops all coroutines, shuts down OpenVR if it was not already initialised by SteamVR, and calls `Application.Quit()`.

---

## Dataset Accessors

### `getActiveDataSet()`
**Used:** Unclear — public, but no external callers found in the `Assets/Scripts` directory. May be called from Unity scene event bindings not visible in source, or may be dead code.

Returns `GetFirstActiveRenderer().Data` — the active `VolumeDataSet`.

---

### `getActiveMaskSet()`
**Used:** Unclear — same situation as `getActiveDataSet`. No external callers found in source.

Returns `GetFirstActiveRenderer().Mask` — the active mask `VolumeDataSet`.

---

## Summary Table

| Method | Access | Used By |
|---|---|---|
| `Awake` | private (Unity) | Unity engine |
| `OnDestroy` | private (Unity) | Unity engine |
| `Start` | private (Unity) | Unity engine |
| `Update` | private (Unity) | Unity engine |
| `OnGUI` | private (Unity) | Unity engine |
| `ShowGUI` | private | `OnGUI` |
| `CheckCubesDataSet` | private | `Start`, `LoadCubeCoroutine` |
| `GetFirstActiveRenderer` | public | Internally throughout; `DesktopPaintController` |
| `BrowseImageFile` | public | UI button |
| `_browseImageFile` | private | `BrowseImageFile` |
| `IsLoadable` | private | `_browseImageFile`, `ChangeHduSelection` |
| `UpdateHeaderFromFits` | private | `_browseImageFile`, `ChangeHduSelection` |
| `ChangeHduSelection` | public | UI dropdown |
| `BrowseMaskFile` | public | UI button |
| `_browseMaskFile` | private | `BrowseMaskFile` |
| `CheckImgMaskAxisSize` | public | **No callers found — likely dead code** |
| `onSubsetToggleSelected` | public | UI toggle |
| `setSubsetBounds` | public | `_browseImageFile`, `ChangeHduSelection` |
| `updateSubsetZMax` | public | Z-axis dropdown listener (registered in `Start`) |
| `checkSubsetBounds` | public | Subset input field listeners (registered in `Start`) |
| `LoadFileFromFileSystem` | public | UI button |
| `CheckMemSpaceForCubes` | public | `LoadCubeCoroutine` |
| `LoadCubeCoroutine` | public (IEnumerator) | `LoadFileFromFileSystem` |
| `postLoadFileFileSystem` | private | `LoadCubeCoroutine` |
| `PopulateRestfreqencyDropdown` | private | `postLoadFileFileSystem` |
| `OnRestFrequencyIndexOfDatasetChanged` | private | Event handler (registered in `Awake`, `postLoadFileFileSystem`) |
| `OnRestFrequencyOfDatasetChanged` | private | Event handler (registered in `Awake`, `postLoadFileFileSystem`) |
| `OnRestFrequencyDropdownValueChanged` | public | UI dropdown |
| `OnRestFrequencyValueChanged` | public | UI input field |
| `SetRestFrequencyInputInteractable` | private | `postLoadFileFileSystem`, `SetRestFrequencyDropdown` |
| `SetRestFrequencyInputField` | private | `postLoadFileFileSystem`, `OnRestFrequencyOfDatasetChanged` |
| `SetRestFrequencyDropdown` | private | `OnRestFrequencyIndexOfDatasetChanged` |
| `populateColorMapDropdown` | private | `postLoadFileFileSystem` |
| `ChangeColorMap` | public | UI dropdown |
| `OnRatioDropdownValueChanged` | public | UI dropdown |
| `populateStatsValue` | private | `postLoadFileFileSystem`, `RestoreDefaults` |
| `UpdateUI` | public | `HistogramHelper` (external) |
| `UpdateSigma` | public | UI dropdown |
| `RestoreDefaults` | public | UI button |
| `UpdateScale` | public | `SetMaxMinPercentile`, `UpdateScaleMin`, `UpdateScaleMax` |
| `SetMaxMinPercentile` | public | UI buttons |
| `UpdateScaleMin` | public | UI input field |
| `UpdateScaleMax` | public | UI input field |
| `UpdateThresholdMin` | public | UI slider |
| `UpdateThresholdMax` | public | UI slider |
| `ResetThresholds` | public | UI button |
| `BrowseSourcesFile` | public | UI button |
| `_browseSourcesFile` | private | `BrowseSourcesFile` |
| `BrowseMappingFile` | public | UI button |
| `_browseMappingFile` | private | `BrowseMappingFile` |
| `SaveMappingFile` | public | UI button |
| `_saveMappingFile` | private | `SaveMappingFile` |
| `ChangeSourceMapping` | public | `SourceRow` (external) |
| `AreMappingsIncompatible` | private | `ChangeSourceMapping` |
| `AreMinimalMappingsSet` | private | `LoadSourcesFile` |
| `LoadSourcesFile` | public | UI button |
| `paintTabSelected` | public | `TabsManager` (external) |
| `paintTabLeft` | public | `TabsManager` (external) |
| `SetInputIndex` | public | UI input field `onSelect` events |
| `DismissFileLoad` | public | UI button |
| `Exit` | public | UI button |
| `getActiveDataSet` | public | **No source callers found — possibly dead code** |
| `getActiveMaskSet` | public | **No source callers found — possibly dead code** |

---

## I/O That Belongs Server-Side

In the target architecture (per [ADR 0002](../../adrs/0002-transport.md) — JSON-RPC over named pipes for local mode, gRPC for future remote streaming) the client shell must not initiate domain data I/O directly. All filesystem reads/writes of FITS cubes, source catalogues, and mapping files, all native plug-in calls (cfitsio P/Invoke), and all raw-voxel analytics belong on the server. This section enumerates every such site in `CanvassDesktop.cs`, classifies it, and points each one at the service-gateway interface that will absorb it.

### Classification rules

- **Server-side.** Anything that reads or writes domain data files, invokes the native FITS plug-in, or operates on raw cube voxels. These violate NFR-MOD-2 (no transitive `UnityEngine` / native-plug-in dependency from domain code) and must move behind a gateway.
- **Client-side (stays).** OS file-picker dialogs and `PlayerPrefs` last-path persistence — these are intrinsically client-machine concerns. In remote-server mode the dialog is replaced by a *remote-browse* RPC, but the dialog responsibility remains on the client.

### Server-side I/O sites

| Method (line) | Call | Category | Why it belongs server-side | Gateway interface |
|---|---|---|---|---|
| `_browseImageFile` (349–407) | `FitsReader.FitsOpenFile` / `FitsGetHduCount` / `FitsMovabsHdu` / `FitsReadKey` / `FitsCloseFile` | FITS header read (native) | Direct P/Invoke into cfitsio on the UI coroutine; couples the client to a native ABI | `IFitsService.OpenAndDescribe(path) → FileDescriptor` |
| `UpdateHeaderFromFits` (544) | `FitsReader.ExtractHeaders` | FITS header read (native) | Reads the full HDU header through cfitsio | `IFitsService.GetHeader(handle, hduIndex) → IDictionary<string,string>` |
| `ChangeHduSelection` (1441–1447) | `FitsReader.FitsOpenFile` / `FitsMovabsHdu` / `FitsCloseFile` (via `UpdateHeaderFromFits`) | FITS header read (native) | Re-opens the file from disk on every HDU change — wasteful and out-of-place on the client | `IFitsService.GetHeader(handle, hduIndex)` (no reopen — handle is server-resident) |
| `_browseMaskFile` (842–856) | `FitsReader.FitsOpenFile` / `ExtractHeaders` / `FitsCloseFile` | FITS header read (native) | Same coupling as image read; also performs axis-size validation against the image cube | `IFitsService.ValidateMaskAgainst(maskHandle, imageHandle, zAxis) → ValidationResult` |
| `LoadCubeCoroutine` (full method) | `VolumeDataSet` constructor → native cube loader; `CheckMemSpaceForCubes` (RAM check against `SystemInfo.systemMemorySize`) | FITS cube read (heavy, native) | Streams up to multi-GB voxel data through the native plug-in; the memory check interrogates *client* RAM for what is fundamentally a server resource | `IDataSetService.LoadCube(handle, subset, hduIndex) → DataSetHandle` (server reports its own capacity) |
| `_browseSourcesFile` (1270) | `FeatureTable.GetFeatureTableFromFile` | Catalogue read (XML / FITS) | Parses source catalogues; same disk + parse coupling as cube load | `ISourceCatalogueService.OpenCatalogue(path) → CatalogueDescriptor` |
| `LoadSourcesFile` (1607) | `FeatureTable.GetFeatureTableFromFile` (called *again* on the same path) | Catalogue read (XML / FITS) | Duplicates the browse-time read — gateway should let the client reuse the descriptor from browse | `ISourceCatalogueService.ImportInto(featureSetHandle, descriptor, mapping, mask)` |
| `_browseMappingFile` (1326) | `FeatureMapping.GetMappingFromFile` | JSON read | Mapping JSON is domain configuration; persistence and schema versioning belong server-side | `IMappingService.Load(path) → FeatureMapping` |
| `_saveMappingFile` (1523) | `FeatureMapping.SaveMappingToFile` (via `featureMappingObject.SaveMappingToFile`) | JSON write | Same reason as load; also needs server-side validation before write | `IMappingService.Save(mapping, path) → SaveResult` |
| `SetMaxMinPercentile` (1788, 1799) | `DataAnalysis.GetPercentileValuesFromHistogram` / `GetPercentileValuesFromData(dataSet.FitsData, …)` | Raw-voxel scan (native) | Scans the full in-memory cube to compute percentiles — the quintessential server query | `IDataSetService.GetPercentiles(handle, lowerPct, upperPct, mode) → (min, max)` |

### Sites that stay client-side

| Method (line) | Call | Why it stays |
|---|---|---|
| `BrowseImageFile` (317), `BrowseMaskFile` (817), `BrowseSourcesFile` (1246), `BrowseMappingFile` (1310), `SaveMappingFile` (1477) | `StandaloneFileBrowser.OpenFilePanelAsync` / `SaveFilePanelAsync` | OS-native file dialog — the picker UI is a client-machine concern. In remote-server mode the dialog is replaced by a remote-browse RPC, but the dialog responsibility remains in the client. |
| Same callbacks | `PlayerPrefs.SetString("LastPath", …)` | Last-used directory is a per-client preference, not session state |

### Open questions for the gateway contract

1. **Handle vs. path.** Should `IFitsService.OpenAndDescribe` return an opaque server-held handle (preferred — paths never cross the boundary again) or a path string the client re-passes on every call? Decision determines whether `IDataSetService.LoadCube` takes a `FileDescriptor` or a `string path`.
2. **Header caching.** `ChangeHduSelection` currently re-opens the file for every HDU switch. The gateway should let the client cache the `FileDescriptor` and request headers by `(handle, hduIndex)` without re-opening.
3. **Memory check ownership.** `CheckMemSpaceForCubes` checks `SystemInfo.systemMemorySize` on the client. In a remote-server topology this is meaningless; in local-pipe mode it's still meaningful but should be answered by the server, which knows its own constraints. Move to `IDataSetService.CanFit(subset, hduIndex) → MemoryEstimate`.

### Traceability

- Closes the "direct file I/O behaviour" gap flagged against **REQ-1 / D1** in [`deliverables-checklist.md`](../deliverables-checklist.md) §2.
- Provides concrete evidence for **NFR-MOD-2** (ViewModel must not transitively depend on `UnityEngine` or the native FITS plug-in) in [`requirements.md`](../../requirements.md) §3.
- Drives the `IFitsService` / `IDataSetService` / `IMappingService` / `ISourceCatalogueService` interface design for the File-tab worked example in [`D4-worked-examples/ex1-file-tab/`](../D4-worked-examples/ex1-file-tab/).
- `ISourceCatalogueService` shape feeds back to **Sub-team 5** (Feature domain) at the next cross-sub-team integration window.
