# IDIA Unity VR
Base project for IDIA's VR experiments with VR

No datasets are currently packaged with the repo, to keep the size down to a minimum. In addition, the SteamVR plugin (v2.2.0), TextMeshPro (v1.3.0) and Vectrosity (5.6) assets need to be added to the project before running.

You should also use [vcpkg](https://github.com/microsoft/vcpkg) to install the cfitsio, xerces-c, and xalan-c libraries. You can then build the native_plugins/native_plugins.sln solution with Visual Studio 2019.
