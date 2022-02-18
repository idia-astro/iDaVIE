Write-Progress "Building native plugins..."
Set-Location native_plugins_cmake
$BuildFolderName = "build"
if (Test-Path $BuildFolderName)
{
    Write-Host "build folder Exists"
}
else
{
    New-Item $BuildFolderName -ItemType Directory
    Write-Host "build folder created successfully"
}
Set-Location $BuildFolderName
cmake -DCMAKE_TOOLCHAIN_FILE=C:\vcpkg\scripts\buildsystems\vcpkg.cmake -DCMAKE_BUILD_TYPE=Release ../
cmake --build . --config Release --target install
Write-Progress "Native plugins built!"
Write-Progress "Downloading packages..."
Set-Location ../..
$PluginBuildFolderName = "plugin_build"
if (Test-Path $PluginBuildFolderName)
{
    Write-Host "plugin build folder exists"
}
else
{
    New-Item $PluginBuildFolderName -ItemType Directory
    Write-Host "plugin build folder created successfully"
}
Set-Location $PluginBuildFolderName
Invoke-WebRequest https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.6.1/steamvr_2_6_1.unitypackage -OutFile steamvr.unitypackage
Invoke-WebRequest https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v2.0.0/NuGetForUnity.2.0.0.unitypackage -OutFile nuget.unitypackage
Invoke-WebRequest https://github.com/CosmicElysium/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage -OutFile scroll_rect.unitypackage
Invoke-WebRequest https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage -OutFile file_browser.unitypackage
Write-Progress "Packages downloaded!"
Write-Progress "Importing packages into project..."
Start-Process "C:\Program Files\Unity\Hub\Editor\2019.4.35f1\Editor\Unity.exe" -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -executeMethod PackageImporter.ImportPackages -quit"
Set-Location ..
Write-Host "Finished!"