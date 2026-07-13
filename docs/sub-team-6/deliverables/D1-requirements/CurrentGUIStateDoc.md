# iDaVIE — Sub-team 6: Desktop GUI and Client Shell

**Current GUI Behaviour — Detailed Requirements Document**  
Source: [zzalscv2/iDaVIE](https://github.com/zzalscv2/iDaVIE)

---

## 1. Purpose and Scope

This document is the requirements engineering baseline for Sub-team 6. Its scope is the **current, as-built Desktop GUI** of iDaVIE: every visible control, every state transition, every piece of data it reads or writes, and every known defect or behavioural issue. Nothing is proposed or changed here. The document draws on the zzalscv2/iDaVIE source repository (a fork of idia-astro/iDaVIE), the official Read the Docs documentation, the published GitHub issue tracker, and the iDaVIE v1.0 paper (Sivitilli et al. 2026, arXiv:2603.15490).

The Desktop GUI is the first thing a user sees when iDaVIE launches and the only interface available throughout the entire VR session. Despite this centrality, it is implemented as a single Unity MonoBehaviour (`CanvassDesktop`) that owns tab switching, file I/O, histogram computation, rendering control, source catalogue loading, log display, and the VR view mirror simultaneously. This document captures all of that behaviour in enough detail to drive refactoring and test design.

| Property | Value |
|---|---|
| Repository | zzalscv2/iDaVIE (fork of idia-astro/iDaVIE, main branch) |
| Unity version | 2021.3.x LTS |
| Scene file | `Assets/Scenes/ui.unity` |
| Root component | `CanvassDesktop` (MonoBehaviour) |
| Language split | C# 83.4% · C++ 10.5% · HLSL 4.0% · ShaderLab 1.2% · Other |
| Native plugins | CFITSIO (FITS I/O), Starlink AST (WCS), `data_analysis_tool.dll` |
| Log location | `iDaVIE/Outputs/Logs/` (max 2 files retained by Unity) |

---

## 2. Overall Window Layout

On launch, a single non-resizable windowed application appears on the desktop monitor. The window is split into two horizontal regions that remain fixed for the lifetime of the session:

- **Left panel — Display area.** Shows either the live VR View (a render-texture mirror of the headset camera, updated every frame) or the About screen (software version, author list, LGPL-3 licence text). The two states are toggled by the VR View and About buttons in the top bar. VR View activates automatically once a cube finishes loading; before that point the panel is blank.
- **Right panel — Control panel.** Contains the five tabs (FILE, RENDER, STATS, SOURCES, DEBUG) arranged horizontally across the top. Below the tab bar is the active tab's content area. Switching tabs is instantaneous and does not reload data or reset state.
- **Top bar (persistent).** Three buttons always visible above the tab selector: VR View, About, and Exit. Exit calls `Application.Quit()` with no save prompt or confirmation dialog — any unsaved mask edits or flag changes are lost silently.

> ⚠️ The Exit button terminates immediately with no confirmation dialog. Unsaved mask edits and flagged source changes are permanently lost with no warning to the user.

---

## 3. FILE Tab

The FILE tab is visible on startup. It is the gate for all data loading and is the only part of the Desktop GUI that triggers heavy computation. Loading a cube occupies the main thread — the window is unresponsive until loading completes. A progress bar at the bottom of the window is the only feedback mechanism during this period.

### 3.1 Controls

| # | Control | Behaviour |
|---|---|---|
| 1 | VR View button (persistent) | Switches the left panel to the live VR render texture. Automatically activates on cube load completion. |
| 2 | About button (persistent) | Switches left panel to the About screen: version string, author list, LGPL-3 licence. |
| 3 | Exit button (persistent) | Calls `Application.Quit()` immediately. No save prompt. See known issue B-01. |
| 4 | Image File — Browse | Opens the OS native file-picker (UnityStandaloneFileBrowser). Accepts `.fits`, `.fit`, and `.fits.gz`. On file selection, triggers FITS header parsing via CFITSIO P/Invoke and populates the header display (control 5). |
| 5 | Header display | Read-only scrollable text area showing the raw FITS header key=value pairs. Allows the user to verify header interpretation before loading. No editing is possible. |
| 6 | Mask File — Browse | Opens a second file-picker for an optional FITS mask. The mask must have identical spatial and spectral dimensions to the image cube. No dimension validation occurs until Load is pressed. |
| 7 | Load button | Initiates the full load pipeline: reads voxel data via CFITSIO, runs downsampling if the cube exceeds `gpuMemoryLimitMb` (default 384 MB), uploads to GPU as a `Texture3D`, then activates the VR scene. A progress bar at the window bottom shows load progress. The GUI is unresponsive during loading. |

### 3.2 4D Cube Handling

When CFITSIO detects `NAXIS=4` in the header, the FILE tab dynamically inserts a dropdown menu between the header display and the Load button. The dropdown lists the 3rd and 4th axis labels (read from the header). The user must select which axis to treat as the spectral Z axis before pressing Load. The selection is passed directly to the native plugin with no further validation.

> ⚠️ The 4D axis dropdown appears conditionally with no visual grouping or label separating it from the standard controls. First-time users frequently miss it and press Load before making a selection, resulting in the default axis being used silently.

### 3.3 Load Pipeline — Direct File I/O in the Client Process

All heavy data operations triggered from the FILE tab run in the Unity client process via direct P/Invoke calls into `data_analysis_tool.dll`. The sequence is:

- **Header parse:** On Browse selection, CFITSIO opens the file and enumerates all header key-value pairs into a managed string. This is synchronous and blocks the render thread briefly for large headers.
- **Voxel read:** On Load, CFITSIO reads the full data cube into a managed `float[]` buffer. For large cubes this can take tens of seconds. The window is unresponsive throughout.
- **Downsampling (conditional):** If the buffer size exceeds `gpuMemoryLimitMb`, the native plugin performs spatial downsampling (max-mode: takes the maximum value per N×N×N voxel block; or average-mode). The mode is set in `config.json`. Downsampling runs on the CPU in the client process.
- **GPU upload:** The float buffer is converted to a Unity `Texture3D` and uploaded to the GPU. This is also synchronous.
- **Mask read (if selected):** The mask FITS file is read identically via CFITSIO. Dimension mismatch errors are detected at this point and surfaced as a Unity log error only — there is no GUI-level error dialog.

> ❌ **B-01 (High)** — No dimension mismatch dialog: if a mask file with wrong dimensions is selected, the error surfaces only as a Unity log message. The user receives no GUI feedback and must open the DEBUG tab to discover the failure.

> ❌ **B-02 (Critical)** — Switching to the DEBUG tab during cube loading causes a system crash. The log readout attempts to bind to the Unity log stream mid-load, while the main thread is blocked inside the native CFITSIO plugin. This results in a thread-safety violation in Unity's IMGUI system and the process terminates. Workaround: do not switch tabs during loading.

---

## 4. RENDER Tab

The RENDER tab controls how the loaded data cube is visualised. All changes apply immediately to the running VR scene by updating Unity Material property blocks and shader uniforms. No file I/O occurs in this tab. The tab is inaccessible (greyed out) before a cube is loaded.

### 4.1 Controls

| # | Control | Behaviour |
|---|---|---|
| 1 | Cube Size dropdown | Sets the rendered side-length proportions. Default: X=Y=Z (rendered as a 1×1×1 cube). Alternate: X=Y (X and Y are equal; Z is scaled to preserve the data aspect ratio between X and Z). |
| 2 | Colour Map dropdown | Selects the colour map applied to the volume. Default: Inferno. The full list matches matplotlib 2.2.4 colour maps. The selection persists for the session but is not saved between sessions. |
| 3 | Min Threshold slider | Sets the lower data bound: voxels at or below this value are fully transparent (alpha=0) and map to the low end of the colour map. |
| 4 | Max Threshold slider | Sets the upper data bound: voxels at or above this value are fully opaque (alpha=1) and map to the high end of the colour map. |
| 5 | Reset Thresholds button | Resets both threshold sliders to the values computed from the cube's data statistics on load (based on the STATS histogram Min/Max). |
| 6 | Rest Frequency dropdown | Selects the rest frequency for spectral unit conversion (frequency or wavelength to velocity). Options: Default (from FITS header), Custom (user text input), plus named frequencies from `config.json` (e.g. HI=1.420406 GHz, 12CO(1-0)=115.271 GHz). |
| 7 | Custom Rest Frequency input | Numeric text input active only when 'Custom' is selected. Accepts a value in GHz. Input is not validated for range or format; non-numeric entry silently falls back to the previous value. |
| 8 | VR View render texture | Live mirror of the headset camera rendered within the RENDER tab content area. Same texture as the left panel VR View. |

### 4.2 Threshold and Colour Map — Shared State

The Min and Max threshold sliders in the RENDER tab and the histogram Min/Max scale inputs in the STATS tab control the same underlying state. Changes in either tab are reflected in the other. This shared state is mediated implicitly inside `CanvassDesktop` with no dedicated ViewModel. If the values are updated from the VR Settings Window, the Desktop GUI sliders do not visually update until the RENDER tab is next opened.

> ⚠️ **B-03 (Medium)** — RENDER tab sliders do not refresh when threshold values are changed from the VR Settings Window. The slider positions lag behind the actual rendering state until the tab is closed and re-opened.

---

## 5. STATS Tab

The STATS tab provides a histogram of the cube's voxel data distribution. It is used to understand data spread and to set the rendering threshold range. The histogram is computed client-side in managed C# code over the in-memory float buffer.

### 5.1 Controls

| # | Control | Behaviour |
|---|---|---|
| 1 | Histogram display | Rendered bar chart of the voxel value distribution, binned using the current Min and Max scale values. The chart redraws on any parameter change. |
| 2 | Percentile quick-select buttons | A row of preset buttons (e.g. 99%, 99.5%, 100%). Clicking sets Min scale to 0% and Max scale to the selected percentile, then redraws. Uses the fast approximate algorithm by default (`useQuickModeForPercentiles=true` in `config.json`). |
| 3 | Min scale input | Numeric text field for the lower histogram bound. Default: cube's global data minimum. Press Enter to apply and redraw. |
| 4 | Max scale input | Numeric text field for the upper histogram bound. Default: cube's global data maximum. Press Enter to apply and redraw. |
| 5 | Sigma level dropdown | Overlays vertical sigma-level markers on the histogram. Default: 1 sigma. Options: 1, 2, 3 sigma. |
| 6 | Reset button | Resets all histogram controls and the shared threshold state to their default values, then redraws. |

### 5.2 Histogram Computation

The histogram is computed from the full in-memory float buffer every time the Min or Max scale values change. For large cubes this can cause a noticeable frame hitch. The fast approximate percentile mode reduces this by computing percentiles over a random subsample of voxels rather than the full buffer. The exact mode iterates the complete buffer, which for cubes at or near the GPU memory limit can take several seconds.

> ⚠️ **B-04 (Low)** — With exact percentile mode enabled (`useQuickModeForPercentiles=false`), clicking a percentile quick-select button on a large cube freezes the window for several seconds with no loading indicator.

---

## 6. SOURCES Tab

The SOURCES tab manages the loading of external source catalogues into the VR scene. Supported formats are VOTable XML (`.xml`) and FITS binary table (`.fits`). Loaded sources appear in the VR scene as spatial markers and are listed in the VR Source List Window.

### 6.1 Controls

| # | Control | Behaviour |
|---|---|---|
| 1 | Catalogue file — Browse | Opens the OS file-picker to select a VOTable (`.xml`) or FITS table (`.fits`) catalogue. On selection, column names are read from the file and listed in the column mapping area. |
| 2 | Mapping file — Browse | Opens a second file-picker to load a previously saved column-mapping `.json` file. Applying this restores the prior session's mapping dropdowns and import tick state automatically. |
| 3 | Column name list | Displays all column names from the selected catalogue. Each row = one column. The list is scrollable for large catalogues. |
| 4 | Mapping dropdown (per column) | Assigns each column to a semantic role: image voxel position (x, y, z), WCS position (RA, Dec, spectral), bounding-box corners (image coords only), or Name. Unmapped columns are ignored on load. |
| 5 | Import tick-box (per column) | When ticked, the column's data values are imported and displayed in the VR Source Info Window when a source is selected in the scene. |
| 6 | Save Mapping button | Serialises the current dropdown and import tick state to a `.json` file via the OS save dialog. |
| 7 | Load button | Reads the catalogue with the configured mappings and injects sources into the VR scene. Greyed out if required position mappings are absent. |
| 8 | Exclude out-of-bounds tick-box | When ticked, sources with mapped coordinates outside the cube's spatial extent are silently excluded. No count or list of excluded sources is shown. |
| 9 | Status message | A text label reporting success or failure after pressing Load. Error messages are generic (e.g. 'Error loading sources') with no detail about the cause. |

### 6.2 Known Coordinate Issue (GitHub #464)

A confirmed open bug (idia-astro/iDaVIE issue #464) reports that when a catalogue generated by SoFiA is imported, the displayed source positions are off by one voxel depending on whether cartesian (x, y, z) or WCS (RA, Dec, velocity) coordinate mapping is used. The x, y, z and WCS paths apply coordinate transforms inconsistently, causing a systematic one-voxel offset between the two mapping modes. The issue is open and unresolved.

> ❌ **B-05 (High)** — WCS and pixel coordinate mappings for VOTable catalogues produce positions inconsistent by one voxel. Affects SoFiA-derived catalogues. [GitHub issue #464](https://github.com/idia-astro/iDaVIE/issues/464).

---

## 7. DEBUG Tab

The DEBUG tab presents a scrollable readout of Unity's application log for the current session. It is the primary (and only) diagnostic surface available to a user without access to the raw log file. In the current implementation it is a passive, unstructured display. It is also the source of the most critical crash in the application.

### 7.1 Controls

| # | Control | Behaviour |
|---|---|---|
| 1 | Log readout area | Scrollable text area displaying all Unity log messages since launch: `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`, and exception stack traces. All entries appear in the same plain text style with no severity distinction, no colour coding, no timestamps, and no subsystem tags. |
| 2 | Save Log button | Opens the OS save dialog. Writes the full current log text to a user-chosen `.txt` file. The saved file contains no metadata header (no version number, no session ID, no timestamp). File naming is entirely up to the user. |

### 7.2 Critical Crash: Tab Switch During Cube Load (B-02)

The most severe known GUI defect is a reliable system crash triggered by switching to the DEBUG tab while a cube is loading. The root cause is a thread-safety violation:

1. When Load is pressed, the native CFITSIO plugin is called synchronously on the Unity main thread.
2. The Unity main thread is blocked inside the P/Invoke call for the duration of the file read.
3. If the user clicks the DEBUG tab during this window, Unity's IMGUI system attempts to bind the log readout text area to the `Application.logMessageReceived` event on the main thread.
4. Because the main thread is already blocked inside unmanaged code, this event registration is non-reentrant. The result is a memory access violation in Unity's native renderer, terminating the process.
5. The VR session state, any in-progress mask work, and any unsaved source flags are lost.

> ❌ **B-02 (Critical)** — Clicking the DEBUG tab at any point after pressing Load and before the loading bar reaches 100% reliably crashes the process. There is no guard, no greying-out of the tab, and no warning. All unsaved session data is lost.

> ℹ️ **Root cause:** the DEBUG tab log readout is connected directly to `Application.logMessageReceived` with no thread guard. The MVVM refactor must make the ViewModel subscribe to the log stream and push updates to the View on the main thread only after the load pipeline releases it.

### 7.3 Additional DEBUG Tab Deficiencies

- **No severity filtering.** `Debug.Log` (info), `Debug.LogWarning`, and `Debug.LogError` entries are rendered identically. A user troubleshooting a crash must read the entire log to find the relevant error.
- **No colour coding.** Warnings and errors have no visual distinction from informational messages.
- **No timestamps.** Entries appear in order but have no time attached, making it impossible to correlate a log entry with a specific user action without external timing.
- **No subsystem tags.** Messages from the native plugin, the Unity engine, SteamVR, and application code are mixed together with no filtering capability.
- **No structured log stream.** The native plugin (`data_analysis_tool.dll`) does not write to a structured stream; its output goes through Unity's `Debug.Log`, losing any structure the plugin may have internally.
- **Save file has no metadata.** The saved `.txt` log file contains no version number, session ID, or timestamp header. If a user saves two logs from different sessions with the same filename, there is no way to distinguish them.
- **Log file limit.** Unity retains a maximum of two log files. The official documentation explicitly warns users to copy the log file before starting a new session, or the previous session log is overwritten. There is no GUI reminder of this limitation.

---

## 8. Persistent Controls (Top Bar)

Three controls sit in the top bar above the tab selector and are always visible regardless of the active tab.

| Control | Behaviour |
|---|---|
| VR View | Switches the left panel to the live VR render texture. State is remembered: if a cube is loaded, the left panel defaults to VR View. |
| About | Switches the left panel to the About screen. Shows software version string, author list, LGPL-3 licence summary. |
| Exit | Calls `Application.Quit()`. Immediate process termination. No confirmation dialog, no save prompt. Any unsaved mask edits, flagged source changes, or in-progress operations are lost without warning. |

---

## 9. VR GUI — Interactions That Affect the Desktop GUI

The VR GUI and Desktop GUI share the same MonoBehaviour composition root and therefore share state directly. The following VR-side actions have observable effects on the Desktop GUI.

### 9.1 Quick Menu

The VR Quick Menu is invoked by holding the secondary controller's thumb button. It provides VR-side controls for source lists, plots, voice commands, settings, colour maps, mask painting, mask save, cube cropping, mask application mode, screenshot, and exit. The Exit button in the Quick Menu terminates the process identically to the Desktop Exit button.

### 9.2 Settings Window — Threshold and Colour Map

The VR Settings Window exposes colour map selection, scaling function (linear, log, sqrt, x², power, gamma), min/max threshold adjustment, primary-hand setting, and moment map step size. Threshold changes made here update the shared state, but the RENDER tab sliders on the desktop do not visually update until the tab is deactivated and reactivated (see B-03).

### 9.3 Source List Window

The VR Source List Window shows mask sources, imported catalogue sources, and New List sources. Changes made here (flagging, adding to New List) update the shared source list that the Desktop SOURCES tab reflects on next tab switch. The list has 19 interactive elements: white buttons (1–5, 19) for per-source interactions, and yellow buttons (9–13, 16–17) for list-level operations including save, visibility, colour, and catalogue navigation.

### 9.4 Mask Painting Mode

Mask painting is VR-only. It is unavailable if the cube is at reduced resolution due to downsampling — the user must first crop the cube to a subregion that fits at full resolution. Mask source IDs start at 1000 and increment for each new source. There is no desktop-side mask save path; saving is done via the Quick Menu or Mask Painting Menu in VR.

### 9.5 Controller Cursor Info

The 3D cursor in VR shows: WCS sky coordinates, WCS spectral coordinate, image voxel coordinates (1-indexed), data value, alternate spectral coordinate (from rest frequency), source ID (if inside a mask), and voice command/window focus status icons. Threshold adjustment mode overlays Min/Max values; selection mode overlays region dimensions, sky angle, and spectral depth.

---

## 10. Known Defects — Full Register

Severities: **Critical** = data loss or process crash; **High** = incorrect output or major usability failure; **Medium** = visible inconsistency; **Low** = usability friction with workaround available.

| ID | Severity | Tab | Description |
|---|---|---|---|
| B-01 | High | Persistent | Exit button terminates with no confirmation dialog. Unsaved mask edits, source flags, and in-progress operations are lost silently. No save prompt exists anywhere in the application. |
| B-02 | Critical | DEBUG | Clicking the DEBUG tab while a cube is loading reliably crashes the process. Root cause: `Application.logMessageReceived` binding attempted while the main thread is blocked inside the CFITSIO P/Invoke call, causing a non-reentrant IMGUI memory violation. All session state is lost. |
| B-03 | Medium | RENDER | RENDER tab threshold sliders do not reflect updates made from the VR Settings Window. Sliders lag behind the actual rendering state until the tab is closed and re-opened. |
| B-04 | Low | STATS | With exact percentile mode enabled (`useQuickModeForPercentiles=false`), clicking a percentile quick-select button on a large cube freezes the window for several seconds with no loading indicator. |
| B-05 | High | SOURCES | WCS and pixel (x,y,z) coordinate mappings for VOTable catalogues are off by one voxel relative to each other. Systematic offset affects SoFiA-derived catalogues. Open issue [#464](https://github.com/idia-astro/iDaVIE/issues/464). |
| B-06 | High | FILE | Mask file dimension mismatch is not surfaced as a GUI dialog. The error appears only in the Unity log (DEBUG tab), giving the user no actionable GUI feedback. |
| B-07 | Medium | FILE | The 4D axis selection dropdown appears conditionally inline with no visual grouping. Users frequently miss it and load with the default axis selection applied silently. |
| B-08 | High | FILE | The window is fully unresponsive during cube loading. There is no cancel button, no ETA, and no background-thread alternative. Loading a large cube can block the UI for minutes. |
| B-09 | Low | DEBUG | The Save Log file has no metadata header (no version, no session ID, no timestamp). Multiple saved logs from different sessions are indistinguishable by filename alone. |
| B-10 | Low | DEBUG | Unity retains a maximum of two log files. The previous session log is silently overwritten on the third launch. There is no GUI reminder. The official docs warn users to manually copy the log, but there is no in-application mechanism to do so. |
| B-11 | Low | SOURCES | The Exclude out-of-bounds tick-box silently excludes sources with no feedback on how many were excluded or which sources they were. |
| B-12 | Medium | All tabs | Toast notifications (e.g. successful save confirmations) do not always appear. Open issue [#472](https://github.com/idia-astro/iDaVIE/issues/472). Reported specifically when saving a subcube with a mask: only one of two expected notifications appears. |
| B-13 | Medium | VR Sources | The Previous Mask button in the VR Source List Window is non-functional. Open issue [#456](https://github.com/idia-astro/iDaVIE/issues/456). |

---

## 11. State and Data Flows

All flows currently pass through shared fields on `CanvassDesktop` with no intermediary ViewModel.

| Source | Destination | Data transferred | Mechanism |
|---|---|---|---|
| FILE — Browse | Header display | FITS header key-value string | Synchronous CFITSIO call; managed string copy |
| FILE — Load | VR scene (GPU) | Voxel float buffer, downsampled if needed | P/Invoke into `data_analysis_tool.dll`; Unity `Texture3D` upload |
| FILE — Load | VR scene (CPU) | Cube metadata (dimensions, WCS) | Shared C# fields on `CanvassDesktop` |
| RENDER sliders | VR shaders | Min/Max threshold, colour map index | Direct Material property block set on the shared Material |
| STATS histogram inputs | RENDER sliders | Min/Max threshold | Shared float fields; both tabs read the same state |
| VR Settings Window | RENDER sliders | Min/Max threshold, colour map | Shared fields updated in VR; desktop sliders stale until tab reinit (B-03) |
| SOURCES — Load | VR Source List | Source list (positions, names, columns) | Shared `List<Source>` field on `CanvassDesktop` |
| VR flagging / New List | SOURCES status | Flag strings, New List membership | Shared source objects; tab reflects state on switch |
| Unity log stream | DEBUG readout | All Unity log messages | `Application.logMessageReceived` event (no thread guard — B-02) |

---

## 12. Long-Term Roadmap — Desktop GUI Implications

The following planned features from the official iDaVIE roadmap have direct implications for the Desktop GUI and are noted here as future requirements inputs.

- **Python console tab (long-term):** A new PYTHON tab in the Desktop GUI providing an interactive Python interpreter with read/write API access to session state (user location, source lists, cube data) and the ability to execute actions (teleport user, add ROI, reload cube with filter). Scripts runnable from the GUI or by voice command.
- **Workspace / state saving (long-term):** Saving and restoring a complete session state covering cube path, mask path, threshold values, source mappings, and rendering settings. Would require a Save/Load mechanism that does not yet exist anywhere in the Desktop GUI.
- **Subcube loading (short-term):** A rework of all file operations to allow loading a contiguous spatial/spectral subregion specified by the user. Will significantly change the FILE tab workflow: bounds entry fields, partial load progress reporting, and HDU selection will need to be added.
- **HDU selection (short-term):** Allows selection of a specific FITS HDU rather than always the first. Required for MUSE, NIRSpec, and MIRI data where science data is stored in the second HDU. Will be added as part of the subcube loading rework.
- **Scripting / video generation (medium-term):** A button in the Desktop GUI to execute a camera-route script for high-quality video recording.
- **Separation of visualisation and analysis (medium-term):** Moving all analysis tool menus into a dynamically-loaded plugin while iDaVIE creates menus from prefabs. Would require significant changes to how the Desktop GUI is composed.

---

## 13. References

- [zzalscv2/iDaVIE GitHub repository](https://github.com/zzalscv2/iDaVIE)
- [idia-astro/iDaVIE (upstream)](https://github.com/idia-astro/iDaVIE)
- [iDaVIE GUI Documentation](https://idavie.readthedocs.io/en/latest/gui.html)
- [iDaVIE Installation and Configuration](https://idavie.readthedocs.io/en/latest/installation_and_configuration.html)
- [GitHub issue #464 — WCS and pixel coordinates inconsistent for VOTable catalogues](https://github.com/idia-astro/iDaVIE/issues/464)
- [GitHub issue #472 — Toast notification not showing](https://github.com/idia-astro/iDaVIE/issues/472)
- [GitHub issue #456 — Previous mask button not working](https://github.com/idia-astro/iDaVIE/issues/456)
- Sivitilli et al. 2026 — iDaVIE v1.0: A Virtual Reality Tool for Interactive Analysis of Astronomical Data Cubes (arXiv:2603.15490)
- Marchetti et al. 2021 — iDaVIE: immersive Data Visualisation Interactive Explorer for volumetric rendering
- Jarrett et al. 2021 — Exploring and Interrogating Astrophysical Data in Virtual Reality
- Source stats calculation: `idia-astro/iDaVIE` — `native_plugins_cmake/data_analysis_tool.cpp` L451
- Volume scaling shader: `idia-astro/iDaVIE` — `Assets/Shaders/Volumes/BasicVolume.cginc` L439
- Moment map compute shader: `idia-astro/iDaVIE` — `Assets/Resources/MomentMapGenerator.compute` L23

---

*End of document. Sub-team 6 — Desktop GUI and Client Shell. Scope: current as-built GUI behaviour only. Nothing is proposed or changed herein.*
