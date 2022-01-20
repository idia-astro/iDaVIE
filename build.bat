@ECHO OFF
ECHO Building native plugins...
cd native_plugins_cmake
mkdir build
cd build
cmake -DCMAKE_TOOLCHAIN_FILE=C:\vcpkg\scripts\buildsystems\vcpkg.cmake -DCMAKE_BUILD_TYPE=Release ../
cmake --build . --config Release --target install
ECHO Native plugins built!
ECHO Downloading packages...
cd ../..
mkdir plugin_build
cd plugin_build
powershell "Invoke-WebRequest https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.6.1/steamvr_2_6_1.unitypackage -OutFile steamvr.unitypackage"
powershell "Invoke-WebRequest https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v2.0.0/NuGetForUnity.2.0.0.unitypackage -OutFile nuget.unitypackage"
powershell "Invoke-WebRequest https://github.com/CosmicElysium/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage -OutFile scroll_rect.unitypackage"
powershell "Invoke-WebRequest https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage -OutFile file_browser.unitypackage"
ECHO Packages downloaded!
ECHO Importing packages into project...
"C:\Program Files\Unity\Hub\Editor\2019.4.34f1\Editor\Unity.exe" -projectPath C:\Users\sivi\Documents\GitHub\idia_unity_vr -batchmode -nographics -executeMethod PackageImporter.ImportPackages -quit
cd ..
ECHO Deleting downloaded packages
del /Q plugin_build 
ECHO Finished!