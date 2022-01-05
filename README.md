# IDIA Unity VR
## Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. 

### Building plugins
You will need to download and install [vcpkg](https://github.com/microsoft/vcpkg) and [CMake](https://cmake.org/download/). Follow the install instructions and ensure you can run `vcpkg` and `cmake` from the commandline (or powershell). You may need to add folders to your PATH environmental variable.

Install dependencies using vcpkg:
```powershell
vcpkg install starlink-ast:x64-windows cfitsio:x64-windows
```

Compile plugins. You will need to know the path for your [vcpkg toolchain file](https://vcpkg.readthedocs.io/en/latest/examples/installing-and-using-packages/#cmake):
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

1) [SteamVR plugin (v2.6.1)](https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.6.1/steamvr_2_6_1.unitypackage)

2) [UnitySimpleFileBrowser (v1.3.0)](https://github.com/yasirkula/UnitySimpleFileBrowser/releases/download/v1.3.0/SimpleFileBrowser.unitypackage)

3) Vectrosity (5.6)

4) [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v2.0.0/NuGetForUnity.2.0.0.unitypackage)

5) [Recyclable Scroll Rect with scroll-to functionality](https://github.com/CosmicElysium/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage) 

6) [Unity Standalone File Browser](https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage). You will need to replace the existing `System.Windows.Forms` dll from `Assets/Plugins/AnotherFileBrowser/Plugins` folder and import v4.0 dll from `C:\Windows\Microsoft.NET\Framework\v4.0.x` folder


Remember to exit and relaunch the project in the Unity Editor after installing the plugins the first time.

You may also need to generate SteamVR action files before running the first time. In the Unity Editor go to **Window>SteamVR Input** and click the **Save and generate** button.
