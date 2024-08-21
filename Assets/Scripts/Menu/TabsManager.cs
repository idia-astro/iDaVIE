/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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

public class TabsManager : MonoBehaviour
{
    public GameObject[] tabs;
    public GameObject[] panels;

    public Color defaultColor;
    public Color selectedColor;

    private int activeTabIndex=0;
    private int old_activeTabIndex = -1;

    public int defaultTabIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject tab in tabs)
        {
            tab.GetComponent<Image>().color = defaultColor;
        }

        tabs[defaultTabIndex].GetComponent<Image>().color = selectedColor;
        panels[defaultTabIndex].SetActive(true);

    }

    // Update is called once per frame
    void Update()
    {

    } 

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

    }
}


