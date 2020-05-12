IDIA Unity VR
Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. In addition, the SteamVR plugin (v2.2.0), TextMeshPro (v1.3.0), Vectrosity (5.6), and [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v2.0.0/NuGetForUnity.2.0.0.unitypackage) assets need to be added to the project before running.

You should also download and install [msys2](https://www.msys2.org/) and [CMake](https://cmake.org/download/). Remember to add the binary directory `/mingw64/bin/` to the system PATH.

Run msys2 and install xalan-c package (and other dependencies needed later) with the following commands:
```
pacman -Syu --noconfirm
pacman -S mingw64/mingw-w64-x86_64-xalan-c mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake patch make --noconfirm
```
Take note of any instructions in the terminal during package update & installation as sometimes you must terminate the msys2 shell to proceed.

The cfitsio package needs a special compiler flag, so a patched version of the package is in the `native_plugins_cmake/mingw-w64-cfitsio` directory in the project folder. Navigate here with msys2 (note: the Windows C drive is under /c/ in msys2) and run the following commands:
```
makepkg-mingw
pacman -U mingw-w64-x86_64-cfitsio-3.450-1-any.pkg.tar.zst --noconfirm
```

If you get an error regarding CRLF, you will need to replace them with LF. [We recommend using Notepad++](http://sql313.com/index.php/43-main-blogs/maincat-dba/62-using-notepad-to-change-end-of-line-characters).

Next, use Windows Powershell to navigate to the native_plugins_cmake directory in the idia_unity_vr folder. Then build the plugins with the following commands:
```
cmake -DCMAKE_C_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-gcc.exe -DCMAKE_CXX_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-g++.exe -B./build -G "MinGW Makefiles"
cmake --build ./build --config Release --target all -- -j
```
Copy the libfits_reader.dll, libdata_analysis_tool.dll, and libvotable_reader.dll libraries into Unity by dragging and dropping into the Assets/Plugins directory when the project is open.
