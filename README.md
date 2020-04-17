z# IDIA Unity VR
Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. In addition, the SteamVR plugin (v2.2.0), TextMeshPro (v1.3.0) and Vectrosity (5.6) assets need to be added to the project before running.

You should also download and install [msys2](https://www.msys2.org/) and [CMake](https://cmake.org/download/). Run msys2 and install necessary packages with the following commands:
```
pacman -Syu
pacman -S mingw64/mingw-w64-x86_64-cfitsio mingw64/mingw-w64-x86_64-xalan-c
```

Next, use Windows Powershell to navigate to the native_plugins_cmake directory in the idia_unity_vr folder. Then build the plugins with the following commands:
```
cmake -DCMAKE_C_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-gcc.exe -DCMAKE_CXX_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-g++.exe -B./build -G "MinGW Makefiles"
mingw32-make.exe
```
Copy the libfits_reader.dll, libdata_analysis_tool.dll, and libvotable_reader.dll libraries into Unity by dragging and dropping into the Assets/Plugins directory when the project is open.
