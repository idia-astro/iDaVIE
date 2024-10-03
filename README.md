# iDaVIE
<p style="text-align:center;"><img src="https://github.com/idia-astro/iDaVIE/blob/main/.github/static/iDaVIE_logo.png"></p>

## The immersive Data Visualisation Interactive Explorer
iDaVIE is a data visualisation tool for 3D volumetric data, with analysis tools aimed at astronomical data in particular (e.g. spectral line data cube analysis, such as HI or CO data cubes). It renders 3D volumetric data as a cube within VR space. This provides profound insight for a multitude of scientific disciplines. While our focus has been on astronomical data and the included analysis tools are tailored to that domain, the visualisation on its own provides substantial benefit to other disciplines. This includes 3D models of neurological systems constructed from MRI images and 3D models of ice cores constructed from microscope images. See our [documentation](https://idavie.readthedocs.io/en/latest/multidisciplinary.html) for showcase videos of iDaVIE in use.

### Installing
The compiled executable can be downloaded from this repository's [releases](https://github.com/idia-astro/iDaVIE/releases/latest). Unzip the download and run the executable. The prerequisites to run this tool are described below. Alternatively, the program can be compiled from source, with instructions described in the [BUILD](https://github.com/idia-astro/iDaVIE/blob/main/BUILD.md) file.

#### Prerequisites
**Hardware:**
In order to run iDaVIE, a system capable of running SteamVR is required. The system requirements are given on the [SteamVR store page](https://store.steampowered.com/app/250820/SteamVR/).

iDaVIE requires at least an NVIDIA GTX 1080, or an AMD RX 5700 XT, or above. For full performance, we recommend an NVIDIA RTX 3070 or AMD RX 6800 XT GPU or newer.

A quad-core or higher CPU is recommended. At least 16 GB of RAM is required. However, the size of the data cubes usable will depend heavily on system memory capacity. We recommend 32 GB, or 64 GB to support large data cubes.

As a VR application, iDaVIE requires a VR headset to operate. Any VR headset compatible with SteamVR should function. The following VR headsets are recommended (**tested**):

 - **Meta Rift S**
 - **Meta Quest 2**
 - **Meta Quest 3**
 - HTC Vive
 - HTC Vive Pro
 - Valve Index
 - Samsung Odyssey

Note: All of these headsets should work, but you might have to [change the control bindings](https://steamcommunity.com/sharedfiles/filedetails/?id=2029205314) in the SteamVR interface.

In the IDIA Visualisation Lab we use the Meta Quest 2 and the Meta Rift S headsets, with a dedicated machine in the lab and GPU-powered laptops while on the road.

**Software:**
In order to run iDaVIE, the following software needs to be installed:

* Windows 10 (version 1903 or newer) is required.
* The SteamVR runtime is required. Note that in order to start SteamVR, some headsets (e.g., Meta headsets) require additional software to be installed. Instructions should be provided with the headset's manual.
* The latest 64-bit (X64) [Visual C++ redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) needs to be installed.

### Contributing
Full documentation is available in our [contribution document](https://github.com/idia-astro/iDaVIE/blob/main/CONTRIBUTING.md). To summarise, we welcome contributions in the form of:
1. Reporting bugs or requesting new features by [creating an issue](https://github.com/idia-astro/idia_unity_vr/issues).
2. Adding your own contributions through a [pull request](https://github.com/idia-astro/iDaVIE/compare).
3. Assisting other users by participating in the [iDaVIE discussions](https://github.com/idia-astro/iDaVIE/discussions).

### Future Plans
While iDaVIE has many useful features already, there are still many features that would be of significant use to researchers, both in astronomy as well as the broader scientific community. We therefore have a roadmap that will continue to direct iDaVIE's development along, and provide an overview of our future plans.

In the **short** term, we list features that we are actively working on and will be included in the next major release. This is not an exhaustive list and will be added to as development progresses. **Medium**-term goals are major feature additions that will require several weeks to months of dedicated development, with an initial idea or prototype already available. **Long**-term plans are major features that are dreams still, with no concrete plans on how to implement them yet.
#### Short-term
* Address bugs that arise after the release of 1.0.
* Add the ability to load a subcube. That is, load a contiguous portion of the cube, specified by the user by providing the bounds of the subcube. This includes a major rework of all file operations. This will allow users to use less memory when viewing large cubes, and could result in faster load times. See the relevant [pull request](https://github.com/idia-astro/iDaVIE/pull/320), [branch](https://github.com/idia-astro/iDaVIE/tree/cilliers/fix-issue-307), and [discussion](https://github.com/idia-astro/iDaVIE/discussions/402) for more information.
* Add the ability to select a different HDU (rather than the default first). Some instruments, noticeably integral field spectrographs (IFUs) such as MUSE, NIRSpec, or MIRI, produce cubes where the data is stored in the second HDU. To load this, the rework mentioned for adding subcubes is required. Therefore it will be added along with that feature. See the relevant [issue](https://github.com/idia-astro/iDaVIE/issues/290) and [branch](https://github.com/idia-astro/iDaVIE/tree/alex/hdu_selection) for more information.

#### Medium-term
* Allow users to switch between rendering for emission or absorption. At the moment, the rendering shader (a ray-marching algorithm) samples values along the ray and returns either the maximum or the average value. To account for absorption in the foreground, a new shader will have to be developed, possibly based on [radiative transfer](https://en.wikipedia.org/wiki/Radiative_transfer) equations. See the relevant [issue](https://github.com/idia-astro/iDaVIE/issues/256) and [discussion](https://github.com/idia-astro/iDaVIE/discussions/403) for more information.
* Release a version of iDaVIE that allows for particle datasets to be visualised. In particular, sparse multiparameter datasets, such as simulations, benefit greatly from visualisation in VR. A prototype has been created and sample images can be found on our [documentation pages](). A publicly available prototype will be available in due course.
* Create a scripting wrapper or API to allow for smooth videos flying through the data to be generated. Currently, recording the user's view of the data is done through recording the screen showing the view of one of the user's eyes. This is invariably jittery and not of good quality. Recording a video from a Unity camera moving through the data results in smoother and better quality video. A prototype was created that utilised a hard-coded route for the camera to follow. It is desirable to create a way for the user to control the movement of this camera, without sacrificing the quality. One way to do this would be by having a script with commands that the camera will follow, and a button on the desktop interface to have it execute. See the [relevant](https://github.com/idia-astro/iDaVIE/issues/132) [issues](https://github.com/idia-astro/iDaVIE/issues/240) and [discussion](https://github.com/idia-astro/iDaVIE/discussions/406) for more information.
* Allow for a movable projection plane that highlights a 2D cross-section in a separate window, akin to the existing moment maps feature. This can be a single channel, or a position-velocity graph, or potentially overlaying one dataset on another. See [the](https://github.com/idia-astro/iDaVIE/issues/74) [relevant](https://github.com/idia-astro/iDaVIE/issues/197) [issues](https://github.com/idia-astro/iDaVIE/issues/404) and [discussion](https://github.com/idia-astro/iDaVIE/discussions/407) for more information.
* Separate the visualisation from the analysis tools. At the moment, much of the radio astronomy analysis tools and the visualisation are hardcoded together, such as the menus. The actual analysis code is contained within a `.dll` plugin. Unity allows for dynamically created menus (see the generic popups in the subcube [pull request](https://github.com/idia-astro/iDaVIE/pull/320)), which opens the possibility of moving all analysis tools into a separate plugin, while the visualisation code remains as is. Creators of the analysis tools can then potentially specify their menus through a script or API file, while iDaVIE (the visualisation tool) creates the menus dynamically from prefabs. More work will be required to look at how Unity deals with dynamic code, as well as a major refactor once the framework is made possible. This will be of significacnt use in the multidisciplinary domain, allowing the end user to download iDaVIE the visualisation tool and the analysis plugin relevant to their field. See the related [issue](https://github.com/idia-astro/iDaVIE/issues/405) and [discussion](https://github.com/idia-astro/iDaVIE/discussions/408) for more information.

#### Long-term
* Integrate a Python console into the application. The idea will be to have a PYTHON tab in the desktop GUI where the user could interact with the console. The Python interpreter will have access to different states of the iDaVIE session through an API that will allow both reading states (e.g. user location, source information, and the loaded cube data) and executing actions (e.g. transporting user, drawing adding new ROI as sources, or reloading the cube with a filter applied). There will also be the ability run custom Python scripts either in the GUI or by voice command.
* Add additional visualisation techniques, (e.g. isocontours, blinking between cubes).
* Create an online VR multiplayer or desktop spectator mode. For spectator mode, one user will be the director, and any subsequent user will only see the cube as the director sees it, though they can move around separately. This would also expand to our iDaVIE:VR2Dome digital planetarium spectator mode that we have recently prototyped. 
* Integrate a VO system, allowing for a dynamic retrieval of images from data catalogues in other wavelengths for the patch of sky corresponding to the currently loaded cube. This will require sending cutout requests to the servers hosting said catalogues, which might require authentication.
* Visualise multiple cubes simultaneously, or visualising a time-series. This will require significant work to be performant, given the size of existing cubes and the bottleneck provided by limited memory.
* Allow for state or workspace saving. This is a fairly standard feature in many visualisation software (such as [CARTA](https://cartavis.org/)), and would be good to have for iDaVIE. This will require a careful design to take care of what is saved, and how to call the relevant functions when loading the saved state.

### About iDaVIE
iDaVIE (the immersive Data Visualisation Interactive Explorer) was conceived by and is being developed by the IDIA Visualisation Laboratory, who serves as the custodian for this open-source project. The documentation for iDaVIE and tutorials can be found on [Read the Docs](https://idavie.readthedocs.io/en/latest/).

#### Contributors
The development of the iDaVIE project is a joint effort from the following institutes:
 * [The Inter-university Institute for Data Intensive Astronomy (IDIA)](https://www.idia.ac.za)
 * [The IDIA Visualisation Laboratory](https://vislab.idia.ac.za)
 * [Osservatorio Astrofisico di Catania, Istituto Nazionale di Astrofisica (INAF-OACT)](https://www.oact.inaf.it)
 * [Osservatorio Astronomico di Cagliari, Istituto Nazionale di Astrofisica (INAF-OACA)](http://www.oa-cagliari.inaf.it/) – testing
![IDIA logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/static/IDIA_logo.png)
![IDIA Vislab logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/static/Vislab_logo.png)
![INAF logo](https://github.com/idia-astro/iDaVIE/blob/main/.github/static/INAF_logo.png)

#### Citing iDaVIE
Please use the following DOI as a citation when using iDaVIE for publications:
[<img src="https://zenodo.org/badge/DOI/10.5281/zenodo.4614115.svg">](https://zenodo.org/doi/10.5281/zenodo.4614115)
#### Other references
Other relevant references are:

* "Exploring and Interrogating Astrophysical Data in Virtual Reality", [Jarrett et al. 2021](https://www.sciencedirect.com/science/article/pii/S2213133721000561?via%3Dihub>)
* "iDaVIE: immersive Data Visualisation Interactive Explorer for volumetric rendering", [Marchetti et al. 2021](https://ui.adsabs.harvard.edu/abs/2020arXiv201211553M/abstract)
* "Virtual Reality and Immersive Collaborative Environments: the New Frontier for Big Data Visualisation", [Sivitilli et al. 2021](https://ui.adsabs.harvard.edu/abs/2021arXiv210314397S/abstract)

A library of publications that utilised iDaVIE can be found on the SAO Astrophysics Data System at [this link](https://ui.adsabs.harvard.edu/user/libraries/4GkpXb-OQD2v79CvQuNxRw).

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
