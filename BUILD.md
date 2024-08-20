# Building iDaVIE from source
As a Unity project, there are several steps to follow to compile iDaVIE from source.

Unfortunately, due to the limitations on VR headset drivers on Unix operating systems, we can only support Windows at the moment. We keep a close eye on developments in the VR space and will support Unix as soon as it becomes feasible.

## Prerequisites
1. Install Unity
    * Download [Unity Hub for Windows](https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe) from Unity's website and install it.
    * From the Unity Hub, install Unity version 2021.3.xf1, where x is the highest number available.

2. Install CMake
    * Download [CMake for Windows](https://cmake.org/download/) and install it.
    * Make sure you can run `cmake` from the PowerShell terminal (or command line).
        - `cmake --version` is a good test.

3. Install vcpkg
    * Download [vcpkg](https://github.com/microsoft/vcpkg) and install it.
    * Make sure to note the path to the vcpkg root folder, found at `C:\vcpkg` for default installations.
## Install
4. Download iDaVIE source code
    * Download the iDaVIE source code from the [GitHub repository](https://github.com/idia-astro/idia_unity_vr).
    * (Optional) You can do this through a Git client, such as [GitHub Desktop](https://desktop.github.com/download/) or [Git Extensions](https://github.com/gitextensions/gitextensions/releases/latest).

5. Run the configuration script
    * Open a PowerShell terminal in the iDaVIE root folder
    * Run the `configure.ps1` script. This script takes two arguments: the vcpkg root folder, and the Unity executable. The default assumption is positional arguments.
    * For example: `.\configure.ps1 "C:\vcpkg" "C:\Program Files\Unity\2021.3.xf1\Editor\Unity.exe"`
    * (Optional) You can specify the vcpkg root with the `-v` or `-vcpkg` flags.
    * (Optional) You can specify the Unity executable with `-u` or `-unity` flags.
    * (Optional) For example: `.\configure.ps1 -v "C:\vcpkg" -u "C:\Program Files\Unity\2021.3.xf1\Editor\Unity.exe"`
  
6. Generate SteamVR actions
    * Open iDaVIE in the Unity Editor.
    * Under **Window->SteamVR Input**, click the **Save and generate** button.
    ![Window->SteamVR Input](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/build/Build_SteamVRInput_1.png)
    ![Save and generate SteamVR actions](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/build/Build_SteamVRInput_2.png)
  
7. Build iDaVIE
    * Open iDaVIE in the Unity Editor.
    * Open the build settings menu under **File->Build Settings**.
    ![Open build settings](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/build/Build_Build_1.png)
    * Click on the Player Settings button on the bottom left.
    ![Open Player settings](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/build/Build_Build_2.png)
    * Under XR Plug-in Management (scroll down on the left), make sure that OpenVR Loader is selected in the list of Plug-in Providers.
    ![Select OpenVR Loader](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/build/Build_Build_3.png)
    * Click the **Build** button and select your destination folder.
