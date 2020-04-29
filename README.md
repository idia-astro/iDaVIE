z# IDIA Unity VR
Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. In addition, the SteamVR plugin (v2.2.0), TextMeshPro (v1.3.0) and Vectrosity (5.6) assets need to be added to the project before running.

You should also download and install [msys2](https://www.msys2.org/) and [CMake](https://cmake.org/download/). Run msys2 and install xalan-c package with the following commands:
```
pacman -Syu
pacman -S mingw64/mingw-w64-x86_64-xalan-c
```
The cfitsio package needs a special compiler flag, so a patched version of the package is in the mingw-w64-cfitsio directory. Navigate here with msys2 and run the following commands, installing any necessary dependencies it asks for if there are errors:
```
makepkg-mingw
pacman -U mingw-w64-x86_64-cfitsio-3.450-1-any.pkg.tar.zst
```

Next, use Windows Powershell to navigate to the native_plugins_cmake directory in the idia_unity_vr folder. Then build the plugins with the following commands:
```
cmake -DCMAKE_C_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-gcc.exe -DCMAKE_CXX_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-g++.exe -B./build -G "MinGW Makefiles"
cmake --build c:/Users/sivi/Documents/idia_unity_vr_test/native_plugins_cmake/build --config Release --target all -- -j 18
```
Copy the libfits_reader.dll, libdata_analysis_tool.dll, and libvotable_reader.dll libraries into Unity by dragging and dropping into the Assets/Plugins directory when the project is open.
