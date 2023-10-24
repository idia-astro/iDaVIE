<#
	Automated build script for i-DaVIE-v Unity project. Currently, assumes vcpkg is installed, and that starlink-ast:x64-windows and cfitsio:x64-windows
	packages are installed via vcpkg.
	Run this script to download the various Unity packages needed, configure the project, and build the final executable.
	Usage: 
		.\build.ps1 <path/to/vcpkg_toolchain> <path/to/unityEXE> [path/to/buildFolder]
		.\build.ps1 -vcpkg|vcpkgcmake <path/to/vcpkg_toolchain> -u|unity <path/to/unityEXE> -d|destination [path/to/buildFolder]
	@Param VCPKGCMAKE: the toolchain file from vcpkg, used for building the native plugins.
	@Param UNITYPATH: the Unity executable file, used for building the project.
	@Param DestFolder: the folder where the final executable will end up, defaults to ..\build\.
#>

param (
	[Parameter(Mandatory, Position=0)]
	[Alias("vcpkg")]
	[System.String]
	$VCPKGCMAKE,
	
	[Parameter(Mandatory, Position=1)]
	[Alias("unity", "u")]
	[System.String]
	$UNITYPATH,
	
	[Parameter(Mandatory=$false, Position=2)]
	[Alias("d", "destination")]
	[System.String]
	$DestFolder = "..\build",
    
        [Parameter(Mandatory=$false, Position=3)]
	[Alias("un", "username")]
	[System.String]
        $UNITY_USERNAME = "__not_init__",

        [Parameter(Mandatory=$false, Position=4)]
	[Alias("pw", "password")]
	[System.String]
        $UNITY_PASSWORD = "__not_init__",

        [Parameter(Mandatory=$false, Position=5)]
	[Alias("sk", "serial")]
	[System.String]
        $UNITY_SERIAL = "__not_init__"
)

#Test that vcpkg cmake exists and is a file
if (-not ((Test-Path $VCPKGCMAKE) -and (Test-Path -Path $VCPKGCMAKE -PathType Leaf)))
{
    Write-Host "vcpkg's Cmake file can't be found at $VCPKGCMAKE, exiting..." -ForegroundColor Red
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

<#
Write-Host "Installing vcpkg packages..."
Start-Process $VCPKG -Wait -ArgumentList "install starlink-ast:x64-windows cfitsio:x64-windows"
Write-Host "vcpkg packages installed!"
#>

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
	Write-Progress "steamvr.unitypackage exists already" -Status "20% complete" -PercentComplete 20
}
else
{
	Write-Progress "fetching steamvr.unitypackage... " -Status "0% complete" -PercentComplete 0
	Invoke-WebRequest https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage -OutFile steamvr.unitypackage
	Write-Progress "Done." -Status "20% complete" -PercentComplete 20
}

if (Test-Path ".\nuget.unitypackage")
{
	Write-Progress "nuget.unitypackage exists already" -Status "40% complete" -PercentComplete 40
}
else
{
	Write-Progress "fetching nuget.unitypackage... " -Status "20% complete" -PercentComplete 20
	Invoke-WebRequest https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v3.0.5/NugetForUnity.3.0.5.unitypackage -OutFile nuget.unitypackage
	Write-Progress "Done." -Status "40% complete" -PercentComplete 40
}

if (Test-Path ".\scroll_rect.unitypackage")
{
	Write-Progress "scroll_rect.unitypackage exists already" -Status "60% complete" -PercentComplete 60
}
else
{
	Write-Progress "fetching scroll_rect.unitypackage... " -Status "40% complete" -PercentComplete 40
	Invoke-WebRequest https://github.com/CosmicElysium/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage -OutFile scroll_rect.unitypackage
	Write-Progress "Done." -Status "60% complete" -PercentComplete 60
}

if (Test-Path ".\file_browser.unitypackage")
{
	Write-Progress "file_browser.unitypackage exists already" -Status "80% complete" -PercentComplete 80
}
else
{
	Write-Progress "fetching file_browser.unitypackage... " -Status "60% complete" -PercentComplete 60
	Invoke-WebRequest https://github.com/gkngkc/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage -OutFile file_browser.unitypackage
	Write-Progress "Done." -Status "80% complete" -PercentComplete 80
}

if (Test-Path ".\com.unity.xr.management-4.4.0.tar.gz")
{
	Write-Progress "OpenXR Management plugin exists already" -Status "100% complete" -PercentComplete 100
}
else 
{
	Write-Progress "fetching com.unity.xr.management-4.4.0.tar.gz... " -Status "80% complete" -PercentComplete 80
	Invoke-WebRequest https://download.packages.unity.com/com.unity.xr.management/-/com.unity.xr.management-4.4.0.tgz -OutFile com.unity.xr.management-4.4.0.tar.gz
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
Write-Progress "scroll_rect.unitypackage extracted"

Write-Progress "Extracting com.unity.xr.management-4.4.0.tar.gz... " -Status "80% complete" -PercentComplete 80
Start-Process ".\extractor.exe" -Wait -ArgumentList ".\com.unity.xr.management-4.4.0.tar.gz ."
Write-Progress "com.unity.xr.management-4.4.0.tar.gz extracted" -Status "100% complete" -PercentComplete 100

Remove-Item ".\extractor.exe"

Copy-Item -Path ".\Assets\*" -Destination "..\Assets" -Recurse -Force

Set-Location ..

#Activate Unity
if (($UNITY_USERNAME -eq "__not_init__") -or ($UNITY_PASSWORD -eq "__not_init__") -or ($UNITY_SERIAL -eq "__not_init__"))
{
    Write-Host "No login details, assuming that licence is already active."
}
else
{
    Write-Host "$UNITYPATH" -Wait -ArgumentList "-batchmode -username $UNITY_USERNAME -password $UNITY_PASSWORD -serial $UNITY_SERIAL -quit"
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-batchmode -username $UNITY_USERNAME -password $UNITY_PASSWORD -serial $UNITY_SERIAL -quit"
}

#Import packages
if (($UNITY_USERNAME -eq "__not_init__") -or ($UNITY_PASSWORD -eq "__not_init__") -or ($UNITY_SERIAL -eq "__not_init__"))
{
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -logfile $PSScriptRoot\import.log -importPackage $PSScriptRoot\plugin_build\textMeshPro-3.0.6.unitypackage -quit"
}
else
{
    Write-Host "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -username $UNITY_USERNAME -password $UNITY_PASSWORD -logfile $PSScriptRoot\import.log -importPackage $PSScriptRoot\plugin_build\textMeshPro-3.0.6.unitypackage -quit"
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -username $UNITY_USERNAME -password $UNITY_PASSWORD -logfile $PSScriptRoot\import.log -importPackage $PSScriptRoot\plugin_build\textMeshPro-3.0.6.unitypackage -quit"
}

Write-Host "Packages imported."

Write-Host "Building player..."
if (Test-Path $DestFolder)
{
    Write-Host "build folder Exists"
}
else
{
    New-Item $DestFolder -ItemType Directory
    Write-Host "build folder created successfully"
}

if (($UNITY_USERNAME -eq "__not_init__") -or ($UNITY_PASSWORD -eq "__not_init__") -or ($UNITY_SERIAL -eq "__not_init__"))
{
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -logfile $PSScriptRoot\build.log -buildWindows64Player $DestFolder\iDaVIE-v.exe -quit"
}
else
{
    Write-Host "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -username $UNITY_USERNAME -password $UNITY_PASSWORD -logfile $PSScriptRoot\build.log -buildWindows64Player $DestFolder\iDaVIE-v.exe -quit"
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-projectPath $PSScriptRoot -batchmode -nographics -ignorecompilererrors -username $UNITY_USERNAME -password $UNITY_PASSWORD -logfile $PSScriptRoot\build.log -buildWindows64Player $DestFolder\iDaVIE-v.exe -quit"
}
Write-Host "Finished!"

Write-Host "Returning licence..."
if (($UNITY_USERNAME -eq "__not_init__") -or ($UNITY_PASSWORD -eq "__not_init__") -or ($UNITY_SERIAL -eq "__not_init__"))
{

}
else
{
    Start-Process "$UNITYPATH" -Wait -ArgumentList "-batchmode -returnlicense -username $UNITY_USERNAME -password $UNITY_PASSWORD"
}
Write-Host "All done."
