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
using System.Collections.Generic;
using DataFeatures;
using PolyAndCode.UI;
using UnityEngine;

/// <summary>
/// Data Source class for Recyclable Scroll Rect.
/// </summary>


public struct FeatureMenuListItemInfo
{
    public string IdTextField;
    public string SourceName;

    public string FlagName;
    public int ParentListIndex;
    public Feature Feature;
}

public class FeatureMenuDataSource : MonoBehaviour, IRecyclableScrollRectDataSource
{
    private List<FeatureMenuListItemInfo> _sofiaList;

    [SerializeField]
    private FeatureSetRenderer FeatureSetRenderer;

    public void InitData()
    {
        _sofiaList = new List<FeatureMenuListItemInfo>();
        for (int i = 0; i < FeatureSetRenderer.FeatureList.Count; i++)
        {
            FeatureMenuListItemInfo obj = new FeatureMenuListItemInfo();
            obj.IdTextField = (i+1).ToString();
            obj.SourceName = FeatureSetRenderer.FeatureList[i].Name;
            obj.Feature = FeatureSetRenderer.FeatureList[i];
            obj.FlagName = FeatureSetRenderer.FeatureList[i].Flag;
            _sofiaList.Add(obj);
        }
    }

    #region DATA-SOURCE

    /// <summary>
    /// Data source method. return the list length.
    /// </summary>
    public int GetItemCount()
    {
        return _sofiaList?.Count ?? 0;
    }

    /// <summary>
    /// Data source method. Called for a cell every time it is recycled.
    /// Implement this method to do the necessary cell configuration.
    /// </summary>
    public void SetCell(ICell cell, int index)
    {
        //Casting to the implemented Cell
        var item = cell as FeatureMenuCell;
        item.ConfigureCell(_sofiaList[index], index); 
    }

    #endregion



}

