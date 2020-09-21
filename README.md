IDIA Unity VR
Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. 

You will need to download and install [msys2](https://www.msys2.org/) and [CMake](https://cmake.org/download/). Remember to add the binary directory `C:\msys64\mingw64\bin` to the system PATH.

Run `MSYS2 MinGW 64-bit` and install xalan-c package (and other dependencies needed later) with the following commands:
```
pacman -Syu --noconfirm
pacman -S mingw64/mingw-w64-x86_64-xalan-c mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake patch make --noconfirm
```
Take note of any instructions in the terminal during package update & installation as sometimes you must terminate the msys2 shell to proceed.

The cfitsio package needs a special compiler flag, so a patched version of the package is in the `native_plugins_cmake/mingw-w64-cfitsio` directory in the project folder. Navigate here with msys2 (note: the Windows C drive is under /c/ in msys2) and run the following commands:
```
dos2unix.exe ./*
makepkg-mingw
pacman -U mingw-w64-x86_64-cfitsio-3.450-1-any.pkg.tar.zst --noconfirm
```

Now build the AST plugin with the following commands in msys2:
```
cd <path to project>/native_plugins_cmake/ast_tool
wget https://github.com/Starlink/ast/releases/download/v9.1.1/ast-9.1.1.tar.gz
tar -xf ast-9.1.1.tar.gz
cp {ast_cpp.in,makeh} ast-9.1.1
cd ast-9.1.1
./configure CFLAGS=-DCMINPACK_NO_DLL --without-pthreads --without-fortran --without-stardocs --enable-shared=no star_cv_cnf_trail_type=long star_cv_cnf_f2c_compatible=no CC=x86_64-w64-mingw32-gcc AR=/opt/bin/x86_64-w64-mingw32-ar.exe --prefix="$PWD/../ast"
make
make install
```

Next, use Windows Powershell to navigate to the native_plugins_cmake directory in the idia_unity_vr folder. Then build the plugins with the following commands. Make sure the Unity project is not open during this step:

```
cmake -DCMAKE_C_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-gcc.exe -DCMAKE_CXX_COMPILER:FILEPATH=C:\msys64\mingw64\bin\x86_64-w64-mingw32-g++.exe -B./build -G "MinGW Makefiles"
cmake --build ./build --config Release --target install -- -j
```
The required DLL files will be copied to the `Assets/Plugins` directory automatically.

You will need to install the following Unity plugins as well before running:

1) [SteamVR plugin (v2.6.1)](https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.6.1/steamvr_2_6_1.unitypackage)

2) [UnitySimpleFileBrowser (v1.3.0)](https://github.com/yasirkula/UnitySimpleFileBrowser/releases/download/v1.3.0/SimpleFileBrowser.unitypackage)

3) Vectrosity (5.6)

4) [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v2.0.0/NuGetForUnity.2.0.0.unitypackage)



Remember to exit and relaunch the project in the Unity Editor after installing the plugins the first time.

You may also need to generate SteamVR action files before running the first time. In the Unity Editor go to **Window>SteamVR Input** and click the **Save and generate** button.
