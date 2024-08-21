# iDaVIE
![iDaVIE Logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/iDaVIE_logo.png)
## immersive Data Visualisation Interactive Explorer
iDaVIE is a data visualisation tool for 3D volumetric data, with analysis tools aimed at astronomical data in particular.

### Installing
The compiled executable can be downloaded from the GitHub [releases](https://github.com/idia-astro/iDaVIE/releases/latest). Unzip the download and run the executable. The prerequisites to run this tool are described below. Alternatively, the program can be compiled from source, with instructions described in the [BUILD](https://github.com/idia-astro/iDaVIE/blob/main/BUILD.md) file.

#### Prerequisites
**Hardware:**
In order to run iDaVIE, a system capable of running SteamVR is required. The system requirements are given on the [SteamVR store page](https://store.steampowered.com/app/250820/SteamVR/).

iDaVIE requires at least an NVIDIA GTX 1080, or an AMD RX 5700 XT, or above. For full performance, we recommend an NVIDIA RTX 3080 or AMD RX 6800 XT GPU or newer.

A quad-core or higher CPU is recommended. At least 16 GB of RAM is required. However, the size of the data cubes usable will depend heavily on system memory capacity. We recommend 32 GB, or 64 GB to support large data cubes.

As a VR application, iDaVIE requires a VR headset to operate. Any VR headset compatible with SteamVR should function. The following VR headsets are recommended (**tested**):

 - **Meta Rift S**
 - **Meta Quest 2**
 - **Meta Quest 3**
 - HTC Vive
 - HTC Vive Pro
 - Valve Index
 - Samsung Odyssey

Note: All of these headsets should work, but you might have to change the control bindings in the SteamVR interface.

In the IDIA Visualisation Lab we use the Meta Quest 2 and the Meta Rift S headsets, with a dedicated machine in the lab and GPU-powered laptops while on the road.

**Software:**
In order to run iDaVIE-v the following software needs to be installed:

Windows 10 (version 1903 or newer) is required.
The SteamVR runtime is required. Note that in order to start SteamVR, some headsets (e.g., Meta headsets) require additional software to be installed. Instructions should be provided with the headset's manual.
The latest 64-bit (X64) [Visual C++ redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) needs to be installed.

### Contributing

### Future Plans

### About iDaVIE
iDaVIE (the immersive Data Visualisation Interactive Explorer) was conceived by and is being developed by the IDIA Visualisation Laboratory, who serves as the custodian for this open-source project.

#### Contributors
The development of the iDaVIE project is a joint effort from the following institutes:
 * [The Inter-university Institute for Data Intensive Astronomy (IDIA)](https://www.idia.ac.za)
 * [The IDIA Visualisation Laboratory](https://vislab.idia.ac.za)
 * [Osservatorio Astrofisico di Catania, Istituto Nazionale di Astrofisica (INAF-OACT)](https://www.oact.inaf.it)
 * [Osservatorio Astronomico di Cagliari, Istituto Nazionale di Astrofisica (INAF-OACA)](http://www.oa-cagliari.inaf.it/) – testing
![IDIA Vislab logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/Vislab_logo.png)
![INAF logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/samples/INAF_logo.png)
#### Citing iDaVIE
Please use the following DOI as a citation when using iDaVIE for publications:
[<img src="https://zenodo.org/badge/DOI/10.5281/zenodo.4614116.svg">](https://doi.org/10.5281/zenodo.4614116)
#### Other references
Other relevant references are:

* "Exploring and Interrogating Astrophysical Data in Virtual Reality", [Jarrett et al. 2021](https://www.sciencedirect.com/science/article/pii/S2213133721000561?via%3Dihub>)
* "iDaVIE: immersive Data Visualisation Interactive Explorer for volumetric rendering", [Marchetti et al. 2021](https://ui.adsabs.harvard.edu/abs/2020arXiv201211553M/abstract)
* "Virtual Reality and Immersive Collaborative Environments: the New Frontier for Big Data Visualisation", [Sivitilli et al. 2021 (in press.)](https://ui.adsabs.harvard.edu/abs/2021arXiv210314397S/abstract)

#### Acknowledgements
* The Inter-University Institute for Data Intensive Astronomy is a partnership of the University of Cape Town, the University of Pretoria, and the University of the Western Cape. 
* This project received support from the National Research Foundation under the Research Career Advancement and South African Research Chair Initiative programs (SARChI).
* This project received support from the Italian Ministry of Foreign Affairs and International Cooperation (MAECI Grant Number ZA18GR02) and the South African NRF (Grant Number 113121) as part of the ISARP RADIOSKY2020 Joint Research Scheme.
* This project received support from the South African Department of Science and Innovation’s National Research Foundation under the ISARP RADIOMAP Joint Research Scheme (DSI-NRF Grant Number 150551).
* The team acknowledges the support from the broad astronomical community for testing the software and providing feedback.

#### Notice
iDaVIE is mainly built using the [Unity game engine](https://unity.com/) and with the following third-party libraries or adaptations of third-party libraries:

* [SteamVR plug-in](https://github.com/ValveSoftware/steamvr_unity_plugin)
* [NVIDIA OpenGL SDK Render to 3D Texture](http://developer.download.nvidia.com/SDK/10/opengl/samples.html)
* [NuGet for Unity plug-in](https://github.com/GlitchEnzo/NuGetForUnity)
* [UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser)
* [Recyclable-Scroll-Rect plug-in](https://github.com/CosmicElysium/Recyclable-Scroll-Rect)
* [CFITSIO](https://heasarc.gsfc.nasa.gov/docs/software/fitsio/fitsio.html)
* [Starlink AST Library](https://github.com/Starlink/ast)
* [World-wide Telescope VOTable](https://github.com/WorldWideTelescope/wwt-windows-client/blob/master/WWTExplorer3d/VOTable.cs)

See our [NOTICE](https://github.com/idia-astro/iDaVIE/blob/main/NOTICE.md) file for full licence information of the third-party libraries we use.

#### Copyright and Licence
Copyright (C) 2024 IDIA, INAF-OACT. This program is free software; you can redistribute it and/or modify it under the terms of the [GNU Lesser General Public License (LGPL) version 3](https://github.com/idia-astro/iDaVIE/blob/main/LICENSE.md) as published by the Free Software Foundation.
