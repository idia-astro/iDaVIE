# IDIA Unity VR
## Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. 

### Building plugins
You will need to download and install [vcpkg](https://github.com/microsoft/vcpkg) and [CMake](https://cmake.org/download/). Follow the install instructions and ensure you can run `vcpkg` and `cmake` from the commandline (or powershell). You may need to add folders to your PATH environmental variable.

Install dependencies using vcpkg:
```powershell
vcpkg install starlink-ast:x64-windows cfitsio:x64-windows
```

## Automatic build and installation
1) Clone the git repository, either from the github.com webhost or from the command line (e.g., `git clone https://github.com/idia-astro/idia_unity_vr.git`).
2) Run the `build.ps1` script from the `idia_unity_vr` folder, e.g., `.\build.ps1 -vcpkg <path/to/vcpkg_toolchain> -u <path/to/unityEXE> -d [path/to/buildFolder]`. See the header in `build.ps1` for more details. 
3) You have to provide the script with the path to your [vcpkg toolchain file](https://vcpkg.readthedocs.io/en/latest/examples/installing-and-using-packages/#cmake) (this will often be located at `C:\vcpkg\scripts\buildsystems\vcpkg.cmake` for most installations),
as well as the path to your Unity.exe file ([How to find your Unity.exe file] (https://docs.unity3d.com/hub/manual/AddEditor.html#locate-the-editor-program-file). Optionally, you can provide the path to the build folder (where iDavie-v will be built). By default, it is `../build` relative to the `idia_unity_vr` folder.

## Manual installation

Compile plugins. You will need to know the path for your [vcpkg toolchain file](https://vcpkg.readthedocs.io/en/latest/examples/installing-and-using-packages/#cmake) (this will often be located at `C:\vcpkg\scripts\buildsystems\vcpkg.cmake` for most installations):
```powershell
# From repo's root directory
cd native_plugins_cmake
mkdir build
cd build
cmake -DCMAKE_TOOLCHAIN_FILE=<path_to_vcpkg.cmake> -DCMAKE_BUILD_TYPE=Release ../
cmake --build . --config Release --target install
```

The required DLL files (along with their dependencies) will be copied to the `Assets/Plugins` directory automatically.


### Other plugins and assets
You will need to install the following Unity plugins as well before running:

1) [SteamVR plugin (v2.7.3)](https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage)
2) [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v3.0.5/NugetForUnity.3.0.5.unitypackage)
3) [Recyclable Scroll Rect with scroll-to functionality](https://github.com/CosmicElysium/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage) 
4) [Unity Standalone File Browser](https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage)


Remember to exit and relaunch the project in the Unity Editor after installing the plugins the first time.

You may also need to generate SteamVR action files before running the first time. In the Unity Editor go to **Window>SteamVR Input** and click the **Save and generate** button.
