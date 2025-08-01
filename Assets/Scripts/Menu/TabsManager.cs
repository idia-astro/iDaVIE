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
using UnityEngine.UI;

/// <summary>
/// Manages tab-based navigation in a Unity UI, toggling between panels and updating visual states.
/// Also notifies the desktop canvas when the paint tab is selected or left.
/// </summary>
public class TabsManager : MonoBehaviour
{
    /// <summary>
    /// Index of the paint tab. Special logic is applied when this tab is selected.
    /// </summary>
    const int PAINT_TAB_INDEX = 4;

    /// <summary>
    /// UI tab buttons (expected to be GameObjects with an Image component).
    /// </summary>
    public GameObject[] tabs;

    /// <summary>
    /// Panels corresponding to each tab, shown or hidden based on the active tab.
    /// </summary>
    public GameObject[] panels;

    /// <summary>
    /// GameObject shown when not in paint mode (e.g., VR map display).
    /// </summary>
    public GameObject vrMapDisplay;

    /// <summary>
    /// Color used for unselected tabs.
    /// </summary>
    public Color defaultColor;

    /// <summary>
    /// Color used for the currently selected tab.
    /// </summary>
    public Color selectedColor;

    /// <summary>
    /// GameObject used for painting in desktop mode.
    /// </summary>
    public GameObject RegionCubeDisplay;

    /// <summary>
    /// Reference to the canvas UI manager for desktop mode.
    /// </summary>
    public CanvassDesktop _canvasDesktop;

    /// <summary>
    /// Index of the currently active tab.
    /// </summary>
    private int activeTabIndex = 0;

    /// <summary>
    /// Index of the previously active tab.
    /// </summary>
    private int old_activeTabIndex = -1;

    /// <summary>
    /// Index of the tab that should be active when the app starts.
    /// </summary>
    public int defaultTabIndex = 0;

    /// <summary>
    /// Initializes the tabs and sets the default active tab on start.
    /// </summary>
    void Start()
    {
        foreach (GameObject tab in tabs)
        {
            tab.GetComponent<Image>().color = defaultColor;
        }

        tabs[defaultTabIndex].GetComponent<Image>().color = selectedColor;
        panels[defaultTabIndex].SetActive(true);

    }

    /// <summary>
    /// Updates the currently active tab, changes visuals, toggles panels,
    /// and triggers canvas-specific behaviors for the paint tab.
    /// </summary>
    /// <param name="newActiveTab">The index of the tab to activate.</param>
    public void UpdateActiveTab(int newActiveTab)
    {
        old_activeTabIndex = activeTabIndex;
        activeTabIndex = newActiveTab;

        if (old_activeTabIndex != activeTabIndex)
        {
            tabs[old_activeTabIndex].GetComponent<Image>().color = defaultColor;
            tabs[activeTabIndex].GetComponent<Image>().color = selectedColor;

            panels[old_activeTabIndex].SetActive(false);
            panels[activeTabIndex].SetActive(true);
        }

        if (newActiveTab == PAINT_TAB_INDEX)
        {
            _canvasDesktop.paintTabSelected();  //lets the canvas desktop know the paint tab is active
        }
        else
        {
            if (RegionCubeDisplay.activeSelf)  //else if in a different tab and the region cube is displayed (for desktop paint) then deselect it and activate vr map display
            {
                RegionCubeDisplay.SetActive(false);
                vrMapDisplay.SetActive(true);
            }
        }

        if (old_activeTabIndex == PAINT_TAB_INDEX) _canvasDesktop.paintTabLeft();


    }

    /// <summary>
    /// Should be called when entering paint mode manually.
    /// Ensures canvas is notified if the current tab is the paint tab.
    /// </summary>
    public void paintModeEntered()
    {
        if (activeTabIndex == PAINT_TAB_INDEX)
        {
            _canvasDesktop.paintTabSelected();
        }
    }
}


