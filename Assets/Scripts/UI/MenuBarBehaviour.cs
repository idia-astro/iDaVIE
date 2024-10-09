/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Class used for mapping the menu bar buttons to their respective actions
/// </summary>
public class MenuBarBehaviour : MonoBehaviour
{

    public GameObject VRViewDisplay;
    public GameObject IdavieLogo;
    public GameObject AboutSection;
    
    public void Start()
    {
        VRViewDisplay.SetActive(false);
        IdavieLogo.SetActive(true);
        AboutSection.SetActive(false);
    }
    
    /// <summary>
    /// Method to toggle the VR map display on and off
    /// </summary>
    public void ToggleVRViewDisplay()
    {
        VRViewDisplay.SetActive(!VRViewDisplay.activeSelf);
        if (VRViewDisplay.activeSelf)
        {
            AboutSection.SetActive(false);
        }
    }
    
    public void ToggleAboutSection()
    {
        AboutSection.SetActive(!AboutSection.activeSelf);
        if (AboutSection.activeSelf)
        {
            VRViewDisplay.SetActive(false);
        }
    }
    
    public void ToggleIdavieLogo()
    {
        IdavieLogo.SetActive(!IdavieLogo.activeSelf);
    }
    
    public void QuitApplication()
    {
        StopAllCoroutines();

        var initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
        if (initOpenVR)
            OpenVR.Shutdown();

        
        #if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
