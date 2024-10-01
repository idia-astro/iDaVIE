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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SourceMappingOptions
{
    none, ID, X, Y, Z, Ra, Dec, Freq, Velo, Redshift, Xmin, Xmax, Ymin, Ymax, Zmin, Zmax, Flag
}

public class SourceRow : MonoBehaviour
{

    public string SourceName;
    public int SourceIndex;
    public SourceMappingOptions CurrentMapping = SourceMappingOptions.none;
    public CanvassDesktop CanvassDesktopParent;

    void Start()
    {
        CanvassDesktopParent = GetComponentInParent<CanvassDesktop>();
        PopulateDropDown();
    }

    void PopulateDropDown()
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        optionNames[0] = "";
        var dropdown = transform.Find("Coord_dropdown").gameObject.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(optionNames));
        dropdown.RefreshShownValue();
    }

    public void MapCoordInParent(int coord)
    {
        string[] optionNames = System.Enum.GetNames(typeof(SourceMappingOptions));
        CurrentMapping = (SourceMappingOptions) coord;
        CanvassDesktopParent.ChangeSourceMapping(SourceIndex, CurrentMapping);        
    }
}
