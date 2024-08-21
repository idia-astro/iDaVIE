<#
	Automated build script to configure iDaVIE for building.
	Run this script to download starlink-ast and cfitsio packages, the various Unity packages needed, install and configure the project before the manual build.
	Usage: 
		.\build.ps1 <path/to/vcpkg_toolchain> <path/to/unityEXE> [path/to/buildFolder]
		.\build.ps1 -vcpkg|vcpkgcmake <path/to/vcpkg_toolchain> -u|unity <path/to/unityEXE> -d|destination [path/to/buildFolder]
	@Param VCPKGCMAKE: the toolchain file from vcpkg, used for building the native plugins.
	@Param UNITYPATH: the Unity executable file, used for building the project.
#>

param (
	[Parameter(Mandatory, Position=0)]
	[Alias("vcpkg", "v")]
	[System.String]
	$VCPKGROOT,
	
	[Parameter(Mandatory, Position=1)]
	[Alias("unity", "u")]
	[System.String]
	$UNITYPATH
)

Set-Variable -Name VCPKGCMAKE -Value "$VCPKGROOT\scripts\buildsystems\vcpkg.cmake" -Scope Script
Set-Variable -Name VCPKGEXE -Value "$VCPKGROOT\vcpkg.exe" -Scope Script
#Test that vcpkg cmake exists and is a file
if (-not ((Test-Path $VCPKGCMAKE) -and (Test-Path -Path $VCPKGCMAKE -PathType Leaf)))
{
    Write-Host "vcpkg's Cmake file can't be found at $VCPKGCMAKE, exiting..." -ForegroundColor Red
	Set-Location ../..
	exit
}

#Test that vcpkg cmake exists and is a file
if (-not ((Test-Path $VCPKGEXE) -and (Test-Path -Path $VCPKGEXE -PathType Leaf)))
{
    Write-Host "vcpkg's executable file can't be found at $VCPKGEXE, exiting..." -ForegroundColor Red
	Set-Location ../..
	exit
}

#Test that Unity.exe exists and is a file
if (-not ((Test-Path $UNITYPATH) -and (Test-Path -Path $UNITYPATH -PathType Leaf)))
{
    Write-Host "Unity's executable file can't be found at $UNITYPATH, exiting..." -ForegroundColor Red
	Set-Location ../..
	exit
}

Write-Host "Setting vcpkg to release values..."
Add-Content -Path $VCPKGROOT\triplets\*.cmake -Value 'set(VCPKG_BUILD_TYPE release)'

Write-Progress "Installing starlink and cfitsio..."
Start-Process "$VCPKGEXE" -Wait -ArgumentList "install starlink-ast:x64-windows cfitsio:x64-windows" 

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
cmake --fresh -DCMAKE_TOOLCHAIN_FILE="$VCPKGCMAKE" -DCMAKE_BUILD_TYPE=Release ../
cmake --build . --config Release --target install
Write-Host "Native plugins built!"
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

Write-Host "Downloading packages..."
Write-Progress -Activity "Downloading packages..." -Status "0% complete" -PercentComplete 0
if (Test-Path ".\steamvr.unitypackage")
{
	Write-Progress "steamvr.unitypackage exists already" -Status "25% complete" -PercentComplete 25
}
else
{
	Write-Progress "fetching steamvr.unitypackage... " -Status "0% complete" -PercentComplete 0
	Invoke-WebRequest https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage -OutFile steamvr.unitypackage
	Write-Progress "Done." -Status "25% complete" -PercentComplete 25
}

if (Test-Path ".\nuget.unitypackage")
{
	Write-Progress "nuget.unitypackage exists already" -Status "50% complete" -PercentComplete 50
}
else
{
	Write-Progress "fetching nuget.unitypackage... " -Status "25% complete" -PercentComplete 25
	Invoke-WebRequest https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v3.0.5/NugetForUnity.3.0.5.unitypackage -OutFile nuget.unitypackage
	Write-Progress "Done." -Status "50% complete" -PercentComplete 50
}

if (Test-Path ".\scroll_rect.unitypackage")
{
	Write-Progress "scroll_rect.unitypackage exists already" -Status "75% complete" -PercentComplete 75
}
else
{
	Write-Progress "fetching scroll_rect.unitypackage... " -Status "50% complete" -PercentComplete 50
	Invoke-WebRequest https://github.com/idia-astro/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage -OutFile scroll_rect.unitypackage
	Write-Progress "Done." -Status "75% complete" -PercentComplete 75
}

if (Test-Path ".\file_browser.unitypackage")
{
	Write-Progress "file_browser.unitypackage exists already" -Status "100% complete" -PercentComplete 100
}
else
{
	Write-Progress "fetching file_browser.unitypackage... " -Status "75% complete" -PercentComplete 75
	Invoke-WebRequest https://github.com/idia-astro/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage -OutFile file_browser.unitypackage
	Write-Progress "Done." -Status "100% complete" -PercentComplete 100
}

Write-Host "Packages downloaded!"
Write-Host "Importing packages into project..."
if (Test-Path ".\unitypackage_extractor-x64.zip")
{
	Write-Progress "unitypackage_extractor-x64.zip exists already" -Status "100% complete" -PercentComplete 100
}
else
{
	Write-Progress "fetching unitypackage_extractor-x64.zip... "
	Invoke-WebRequest https://github.com/Cobertos/unitypackage_extractor/releases/download/v1.1.0/unitypackage_extractor-x64.zip -OutFile unitypackage_extractor-x64.zip
	Write-Progress "Done." -Status "100% complete" -PercentComplete 100
}

Expand-Archive ".\unitypackage_extractor-x64.zip" -DestinationPath .
Remove-Item ".\unitypackage_extractor-x64.zip"
if (Test-Path "Assets")
{
    Write-Host "extract target folder Exists"
}
else
{
    New-Item "Assets" -ItemType Directory
    Write-Host "extract target folder created successfully"
}

Write-Progress "Extracting steamvr.unitypackage... " -Status "0% complete" -PercentComplete 0
Start-Process ".\extractor.exe" -Wait -ArgumentList ".\steamvr.unitypackage ."
Write-Progress "steamvr.unitypackage extracted"

Write-Progress "Extracting nuget.unitypackage... " -Status "20% complete" -PercentComplete 20
Start-Process ".\extractor.exe" -Wait -ArgumentList ".\nuget.unitypackage ."
Write-Progress "nuget.unitypackage extracted"

Write-Progress "Extracting file_browser.unitypackage... " -Status "40% complete" -PercentComplete 40
Start-Process ".\extractor.exe" -Wait -ArgumentList ".\file_browser.unitypackage ."
Write-Progress "file_browser.unitypackage extracted"

Write-Progress "Extracting scroll_rect.unitypackage... " -Status "60% complete" -PercentComplete 60
Start-Process ".\extractor.exe" -Wait -ArgumentList ".\scroll_rect.unitypackage ."
Write-Progress "scroll_rect.unitypackage extracted" -Status "80% complete" -PercentComplete 80

Remove-Item ".\extractor.exe"

Copy-Item -Path ".\Assets\*" -Destination "..\Assets" -Recurse -Force
Remove-Item ".\Assets\*" -Recurse -Force

Set-Location ..

Write-Progress "Importing TextMeshPro package... " -Status "80% complete" -PercentComplete 80
Start-Process "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -logfile $PSScriptRoot\import.log -importPackage $PSScriptRoot\plugin_build\textMeshPro-3.0.6.unitypackage -quit"
Write-Progress "TextMeshPro package imported" -Status "100% complete" -PercentComplete 100

Write-Host "Packages imported."
Write-Host "Configuration complete!"
