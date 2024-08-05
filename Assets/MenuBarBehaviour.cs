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
