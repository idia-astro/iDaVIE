# iDaVIE Build Troubleshooting (Windows)

Issues encountered while following `BUILD.md` and how they were diagnosed and fixed. Each section lists the **symptom** you'll see, the **root cause**, and the **fix**. Replace `<project>` with your local iDaVIE path (the directory containing `Assets/`, `Packages/`, `Configure.ps1`).

## TL;DR (the issues you're most likely to hit)

1. `Packages/manifest.json` has a UTF-8 BOM that Unity rejects → rewrite without BOM.
2. `manifest.json` contains Unity 6 packages a 2021.3 project can't resolve → rewrite with 2021.3-valid packages only.
3. `Configure.ps1`'s download of `steamvr.unitypackage` was silently truncated → re-download with BITS instead of `Invoke-WebRequest`.
4. `Configure.ps1` doesn't install `cminpack` (CMakeLists.txt requires it) → install it manually via vcpkg.
5. Newly-imported plugin `.dll.meta` files have **Editor disabled** by default → flip to enabled.
6. `CMakeLists.txt:74` looks for `zlib1.dll` but vcpkg now ships zlib as `z.dll` → manually copy `z.dll` to `Assets/Plugins/`.

---

## Phase 1: `manifest.json` has a UTF-8 BOM

**Symptom (Unity Hub dialog):**
```
Failed to resolve packages: The file [<project>\Packages\manifest.json] is not valid JSON:
  Non-whitespace before {[.
  Line: 1
  Column: 1
  Char: 65279
```

**Cause:** Character 65279 is U+FEFF, the UTF-8 BOM. `Configure.ps1`'s last step writes the manifest with `Out-File -Encoding utf8`, which on Windows PowerShell 5.1 means **UTF-8 *with* BOM**. Unity's strict JSON parser rejects it.

**Diagnose:**
```powershell
$bytes = [System.IO.File]::ReadAllBytes('<project>\Packages\manifest.json')
'{0:X2} {1:X2} {2:X2}' -f $bytes[0], $bytes[1], $bytes[2]
# If output is "EF BB BF", file has a BOM
```

**Fix:**
```powershell
$path = '<project>\Packages\manifest.json'
$content = [System.IO.File]::ReadAllText($path)         # .NET auto-strips BOM into the string
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
```

---

## Phase 2: `manifest.json` references Unity 6 packages

**Symptom (in `import1.log` after Configure.ps1 runs SteamVR import, or in Unity console):**
```
Project has invalid dependencies:
    com.unity.modules.accessibility: Package [com.unity.modules.accessibility@1.0.0] cannot be found
    com.unity.modules.adaptiveperformance: Package [com.unity.modules.adaptiveperformance@1.0.0] cannot be found
    com.unity.modules.vectorgraphics: Package [com.unity.modules.vectorgraphics@1.0.0] cannot be found
    com.unity.multiplayer.center: Package [com.unity.multiplayer.center@1.0.1] cannot be found
```

**Cause:** Unity Hub may auto-open the project in a newer Unity version (Unity 6 / 2022+) on first open, which rewrites `manifest.json` with packages that don't exist in 2021.3. `Configure.ps1` then preserves that bad manifest. Other tell-tale signs that this happened:
- `ProjectSettings/MultiplayerManager.asset` appears as untracked (Unity 6 feature).
- `ProjectSettings/boot.config` shows as deleted (was a 2021.3 file).
- `ProjectSettings/ProjectVersion.txt` was modified.

**Prevention:** In Unity Hub, always confirm the project is set to open with **2021.3.x LTS** (the version dropdown next to the project entry). Never click "Open with newer version".

**Fix:** Replace `Packages/manifest.json` with a clean 2021.3-compatible one. Known-good baseline for iDaVIE on 2021.3.45f2:

```json
{
  "dependencies": {
    "com.unity.ide.visualstudio": "2.0.22",
    "com.unity.mathematics": "1.3.2",
    "com.unity.test-framework": "1.1.33",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.timeline": "1.6.5",
    "com.unity.ugui": "1.0.0",
    "com.unity.visualscripting": "1.7.8",
    "com.unity.xr.legacyinputhelpers": "2.1.11",
    "com.unity.xr.management": "4.5.2",
    "com.valvesoftware.unity.openvr": "file:../Assets/SteamVR/OpenVRUnityXRPackage/Editor/com.valvesoftware.unity.openvr-1.1.4.tgz",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

Notes on what each non-module package is for:
- `com.unity.ugui` — provides `UnityEngine.UI` (the project's `using UnityEngine.UI` won't compile without it).
- `com.unity.textmeshpro` — TMP, used widely in the UI scenes.
- `com.unity.visualscripting` — `Unity.VisualScripting` namespace, used by the VideoMaker scripts.
- `com.unity.xr.legacyinputhelpers` — provides `UnityEngine.SpatialTracking`, which SteamVR's assembly references.
- `com.unity.xr.management` — XR plugin manager (required for SteamVR runtime).
- `com.valvesoftware.unity.openvr` — Valve's OpenVR Unity XR plugin. References a `.tgz` shipped inside the SteamVR Unity package; only valid AFTER the SteamVR `.unitypackage` has been imported.
- The `com.unity.modules.*` entries are all built-in Unity engine modules and are version `1.0.0` for the entire 2021.3 line.

After saving the manifest, restart Unity. The package manager should resolve without errors.

---

## Phase 3: `steamvr.unitypackage` won't import — "Couldn't decompress package"

**Symptom (Unity console after `Assets → Import Package → Custom Package → steamvr.unitypackage`):**
```
Failed to import package with error: Couldn't decompress package
```

Unity's full Editor.log shows:
```
Error decompressing GZ: ...
ERROR: Unexpected end of data : archtemp.tar
```

**Cause:** `Configure.ps1` line 93 uses `Invoke-WebRequest` to download SteamVR (~76 MB). `Invoke-WebRequest` can silently return a truncated file if the connection drops mid-download — no exception is raised. The truncated file is a valid gzip (it decompresses up to some point), but Unity's stricter 7-Zip extractor catches the missing trailer.

**Diagnose:**
```powershell
# Check actual size vs. expected
$p = '<project>\plugin_build\steamvr.unitypackage'
(Get-Item $p).Length   # Should be ~76,530,020 bytes for v2.7.3. Anything materially smaller is truncated.

# Check gzip trailer integrity
$fs = [System.IO.File]::OpenRead($p); $fs.Position = $fs.Length - 16
$tail = New-Object byte[] 16; $fs.Read($tail, 0, 16) | Out-Null; $fs.Close()
$isize = [BitConverter]::ToUInt32($tail[12..15], 0)
$isize  # Should be 102,105,088 for v2.7.3. Garbage value = truncated file.
```

**Fix:** Re-download via BITS (resilient to interruptions, won't return success on a partial file):
```powershell
$p = '<project>\plugin_build\steamvr.unitypackage'
Remove-Item $p -Force
Start-BitsTransfer -Source 'https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage' -Destination $p
```

Then retry **Assets → Import Package → Custom Package…**.

---

## Phase 4: After SteamVR import, compile errors about missing types

**Symptoms:** Three flavors, in this order as you fix each:

(a) `UnityEngine.UI` not found:
```
Assets\Unity-UI-Rounded-Corners\UiRoundedCorners\ImageWithRoundedCorners.cs(3,19):
  error CS0234: The type or namespace name 'UI' does not exist in the namespace 'UnityEngine'
```
Cause: `com.unity.ugui` not in manifest. Add `"com.unity.ugui": "1.0.0"`.

(b) `Unity.VisualScripting` not found:
```
Assets\Scripts\VideoMaker\VideoCameraController.cs(12,13):
  error CS0234: The type or namespace name 'VisualScripting' does not exist in the namespace 'Unity'
```
Cause: `com.unity.visualscripting` not in manifest. Add `"com.unity.visualscripting": "1.7.8"`.

(c) SteamVR types (`SteamVR_Input_Sources`, `SteamVR_Action_Boolean`, `Player`) not found, even though SteamVR is imported:
```
Assets\Scripts\VolumeData\VolumeInputController.cs:
  error CS0246: The type or namespace name 'SteamVR_Input_Sources' could not be found
```
Cause: `Assets/SteamVR/SteamVR.asmdef` references `Unity.XR.OpenVR` and `UnityEngine.SpatialTracking`. Without them, the SteamVR assembly itself can't compile, and downstream code can't see SteamVR types. Need:
- `com.unity.xr.legacyinputhelpers` (for `UnityEngine.SpatialTracking`)
- `com.unity.xr.management` (XR plugin manager)
- `com.valvesoftware.unity.openvr` (provides `Unity.XR.OpenVR`)

For the OpenVR package, SteamVR ships it as a `.tgz` inside its own folder. After importing SteamVR, this path exists:
```
<project>/Assets/SteamVR/OpenVRUnityXRPackage/Editor/com.valvesoftware.unity.openvr-1.1.4.tgz
```
Add to manifest via a file: URL (path is relative to `Packages/`):
```json
"com.valvesoftware.unity.openvr": "file:../Assets/SteamVR/OpenVRUnityXRPackage/Editor/com.valvesoftware.unity.openvr-1.1.4.tgz"
```

Alternatively, when you first open the project after the SteamVR import, SteamVR shows a popup *"Would you like to install OpenVR Unity XR?"* — clicking Install does the same thing.

---

## Phase 5: OxyPlot.WindowsForms version mismatch warning

**Symptom:**
```
Assembly 'Assets/Packages/OxyPlot.WindowsForms.2.0.0/lib/net45/OxyPlot.WindowsForms.dll' will not be loaded due to errors:
OxyPlot.WindowsForms references strong named System.Windows.Forms Assembly references: 4.0.0.0 Found in project: 2.0.0.0.
```

**Cause:** OxyPlot.WindowsForms is built against .NET Framework's `System.Windows.Forms` 4.0, but Unity's Mono ships 2.0. The DLL is actually used in two places (`HistogramHelper.cs:92`, `SpectralProfileHelper.cs:141`) for PNG export, so we want it to load.

**Fix:** **Edit → Project Settings → Player → Other Settings → uncheck "Assembly Version Validation"**. This persists in `ProjectSettings/ProjectSettings.asset`. Restart the Editor to make sure the loader re-evaluates DLLs.

---

## Phase 6: Native plugin (`idavie_native`) — vcpkg dependencies missing

**Symptom (at runtime when you Browse → select a `.fits` file):**
```
DllNotFoundException: idavie_native assembly:<unknown assembly> type:<unknown type> member:(null)
FitsReader.FitsOpenFile (...)
```

**Cause 1:** `Configure.ps1` line 53 only attempts `vcpkg install starlink-ast:x64-windows cfitsio:x64-windows`. It silently keeps going if vcpkg fails. **`cminpack` is missing from this command entirely**, but `CMakeLists.txt:49` does `find_package(cminpack CONFIG REQUIRED)`, so the CMake configure fails on cminpack regardless of whether the other two succeeded.

**Diagnose:**
```powershell
# Verify what vcpkg actually installed
cmd /c 'dir /A "C:\vcpkg\installed\x64-windows\bin"'
# You should see cfitsio.dll, cminpack.dll, z.dll. If empty, install failed.
```

**Fix:**
```powershell
# Install all three packages explicitly (takes 20-40 min from cold)
C:\vcpkg\vcpkg.exe install cfitsio:x64-windows starlink-ast:x64-windows cminpack:x64-windows

# Then build the native plugin
cd <project>\native_plugins_cmake\build
cmake --fresh -DCMAKE_TOOLCHAIN_FILE='C:\vcpkg\scripts\buildsystems\vcpkg.cmake' -DCMAKE_BUILD_TYPE=Release ..\
cmake --build . --config Release --target install
```

When the install step finishes, you should see four DLLs in `<project>\Assets\Plugins`:
- `idavie_native.dll`
- `cfitsio.dll`
- `cminpack.dll`
- `zlib1.dll` (or `z.dll` — see Phase 8 below)

---

## Phase 7: Unity refuses to load native plugins in the Editor

**Symptom:** `DllNotFoundException: idavie_native` continues even though the DLLs exist in `Assets/Plugins`.

**Cause:** When Unity first imports a native DLL it creates a `.meta` file with **Editor explicitly disabled**:
```yaml
- first:
    Editor: Editor
  second:
    enabled: 0
    settings:
      DefaultValueInitialized: true
```
"Any Platform" being enabled doesn't override this — per-platform settings win. Unity refuses to load the plugin when running inside the Editor (which is what happens in Play mode).

**Diagnose:**
```powershell
Get-ChildItem '<project>\Assets\Plugins' -Filter '*.dll.meta' | ForEach-Object {
  $c = Get-Content $_.FullName -Raw
  $m = [regex]::Match($c, 'Editor:\s*Editor\s*\n\s*second:\s*\n\s*enabled:\s*(\d)')
  if ($m.Success) { '{0}: Editor enabled = {1}' -f $_.Name, $m.Groups[1].Value }
}
```

**Fix (per-file):** Open the DLL in Unity's Project window → Inspector → tick the **Editor** checkbox under Platforms, set CPU = **x86_64**, OS = **Windows**, Apply.

Or edit the `.meta` directly. The Editor block should look like:
```yaml
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
```

Note the indentation: `settings:` is a child of `second:` (6 spaces) and its values are 8 spaces. Wrong indentation here produces a silent YAML parse failure with no visible effect.

Apply to all four meta files: `idavie_native.dll.meta`, `cfitsio.dll.meta`, `cminpack.dll.meta`, `zlib1.dll.meta`. (You technically only need the one Unity P/Invokes directly, but iDaVIE has both Unity's loader and a custom `NativePluginLoader.cs` — being consistent avoids further confusion.)

---

## Phase 8: zlib filename mismatch — the showstopper for the custom loader

**Symptom:** Unity console:
```
Exception: Failed to load plugin [<project>/Assets/Plugins/idavie_native.dll]
fts.NativePluginLoader.LoadAll () (at Assets/Scripts/PluginInterface/NativePluginLoader.cs:162)
fts.NativePluginLoader.Awake () (at Assets/Scripts/PluginInterface/NativePluginLoader.cs:85)
```
This comes from iDaVIE's **own** `NativePluginLoader`, which calls Win32 `LoadLibrary` directly — independent of Unity's plugin import settings.

**Cause:** Current vcpkg ships zlib as `z.dll`, but `CMakeLists.txt:74` looks for `zlib1`:
```cmake
find_library(ZLIB_RUNTIME "zlib1")
```
This finds nothing in modern vcpkg, so the install rule doesn't copy zlib into `Assets/Plugins`. The freshly-built `cfitsio.dll` imports `z.dll` by name. When `idavie_native.dll` loads, Windows tries to resolve `cfitsio.dll` → which tries to load `z.dll` → not found → entire chain fails with `ERROR_MOD_NOT_FOUND` (Win32 error 126).

**Diagnose:** Strip cfitsio.dll's imports to see what DLL name it actually wants:
```powershell
$bytes = [System.IO.File]::ReadAllBytes('<project>\Assets\Plugins\cfitsio.dll')
$text = [System.Text.Encoding]::ASCII.GetString($bytes)
[regex]::Matches($text, '[A-Za-z0-9_\-]+\.dll') | ForEach-Object { $_.Value } | Sort-Object -Unique
# Look for z.dll or zlib1.dll in the list
```

Or test the load chain directly:
```powershell
Add-Type -Namespace W -Name K -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32", SetLastError=true, CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
public static extern System.IntPtr LoadLibraryExW(string lpFileName, System.IntPtr h, uint flags);
'@
$plugins = '<project>\Assets\Plugins'
foreach ($name in 'cfitsio.dll','cminpack.dll','idavie_native.dll') {
  $full = Join-Path $plugins $name
  $h = [W.K]::LoadLibraryExW($full, [System.IntPtr]::Zero, 0x8)  # LOAD_WITH_ALTERED_SEARCH_PATH
  $err = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
  if ($h -eq [System.IntPtr]::Zero) { "FAIL  $name  err=$err" } else { "OK    $name" }
}
```
If you see `FAIL cfitsio.dll err=126`, the missing module is downstream of cfitsio (most likely `z.dll`).

**Fix:** Copy `z.dll` from vcpkg's bin folder next to the other plugins:
```powershell
Copy-Item 'C:\vcpkg\installed\x64-windows\bin\z.dll' '<project>\Assets\Plugins\z.dll'
```

Restart Unity (or Ctrl+R to refresh). The custom loader's chain now resolves.

**Permanent fix (recommended for the repo):** Update `CMakeLists.txt:74` so future rebuilds Just Work:
```cmake
find_library(ZLIB_RUNTIME NAMES zlib1 z)
```
This finds either the legacy name (`zlib1.dll`) or the modern vcpkg name (`z.dll`).

---

## Phase 9: Scene-level reference errors (cosmetic, not blocking)

**Symptom (only when entering Play mode in the Editor):**
```
UnassignedReferenceException: The variable renderingPanelContent of CanvassDesktop has not been assigned.
  CanvassDesktop.Start () (at Assets/Scripts/UI/CanvassDesktop.cs:163)

NullReferenceException
  ButtonHoverBehaviour.OnHoverExit () (at Assets/Scripts/UI/ButtonHoverBehaviour.cs:38)

NullReferenceException
  DebugLogging.HandleLog () (at Assets/Scripts/Debuggers/DebugLogging.cs:195)
```

**Cause:** Inspector references in the `ui.unity` scene aren't connected on some MonoBehaviours. The third one (`DebugLogging`) is a cascade — the project's custom log handler dereferences a null `logOutput` text field every time another error fires, amplifying the noise.

**Impact on the build flow:** None for the `Window → SteamVR Input → Save and generate` step, which runs in Edit mode. Only matters when you Play in the Editor.

**Fix (when you actually need to run from inside the Editor):** Select the relevant GameObjects in the `ui.unity` scene and look in the Inspector for any field labelled "None (XYZ)" that should reference a real object. The committed scene file should have these wired up; if yours doesn't, the scene was likely modified by a different Unity version that lost references.

The deployed builds (the `.exe` released on GitHub) don't suffer this — these issues are local-scene artefacts.

---

## OneDrive caveat (general)

Storing the project under `C:\Users\<you>\OneDrive\...` works but causes occasional pain:
- OneDrive sometimes re-saves files with a UTF-8 BOM during conflict resolution (this is part of how Phase 1 happens).
- For large imports (SteamVR, NuGet packages, asset reimports), OneDrive can lock files Unity is mid-write to and produce misleading errors.

If you hit weird, transient build/import errors after the above fixes, **pause OneDrive sync** before running Unity or `Configure.ps1`. Long-term, consider moving the project to a non-synced folder (e.g. `C:\Code\iDaVIE`).

---

## Quick recovery script

If you're setting up a fresh machine, this is the sequence that worked end-to-end:

```powershell
# 1. After cloning and following BUILD.md prereqs (Unity 2021.3.x LTS, VS with MSVC v142, CMake, vcpkg, Steam+SteamVR)

# 2. Install all three vcpkg packages (not just the two Configure.ps1 attempts)
C:\vcpkg\vcpkg.exe install cfitsio:x64-windows starlink-ast:x64-windows cminpack:x64-windows

# 3. Build the native plugin
cd <project>\native_plugins_cmake
if (-not (Test-Path build)) { New-Item build -ItemType Directory | Out-Null }
cd build
cmake --fresh -DCMAKE_TOOLCHAIN_FILE='C:\vcpkg\scripts\buildsystems\vcpkg.cmake' -DCMAKE_BUILD_TYPE=Release ..\
cmake --build . --config Release --target install

# 4. Copy z.dll alongside the other plugins (CMakeLists doesn't do this for modern vcpkg)
Copy-Item 'C:\vcpkg\installed\x64-windows\bin\z.dll' '<project>\Assets\Plugins\z.dll' -Force

# 5. Download SteamVR via BITS (more reliable than Invoke-WebRequest)
if (-not (Test-Path '<project>\plugin_build')) { New-Item '<project>\plugin_build' -ItemType Directory | Out-Null }
Start-BitsTransfer -Source 'https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage' -Destination '<project>\plugin_build\steamvr.unitypackage'

# 6. Open the project in Unity Hub with 2021.3.x LTS (NOT a newer version)
#    - If manifest.json gets a BOM, strip it (Phase 1 fix above)
#    - Replace manifest.json with the Phase 2 baseline
#    - Import steamvr.unitypackage via Assets > Import Package > Custom Package
#    - In Project window, select each DLL in Assets/Plugins, tick Editor + CPU x86_64 + OS Windows, Apply
#    - Edit > Project Settings > Player > Other Settings > uncheck Assembly Version Validation
#    - Window > SteamVR Input > Save and generate
```

## Loading data after the project runs

- **Volume cubes (3D FITS):** Use the **Browse** button in the rendering / import panel. Sample: `Data/SampleData/test_volume.fits`.
- **Source catalogs (2D FITS tables / VOTable XML):** Use the **Sources tab → Browse** in the right-hand panel — *not* the volume Browse, which rejects tables with *"Not enough dimensions in selected image"*. After picking the catalog, map at least (X,Y,Z) or (RA,Dec + Freq/Velo/Redshift) in the column dropdowns, or load `test_catalog.fits.json` via the "Mapping File" button to auto-fill mappings. Then click **Load Sources**.
