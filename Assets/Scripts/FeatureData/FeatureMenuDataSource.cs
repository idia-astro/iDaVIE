using System.Collections.Generic;
using UnityEngine;
using DataFeatures;
using VolumeData;
using PolyAndCode.UI;
using TMPro;

/// <summary>
/// Data Source class for Recyclable Scroll Rect.
/// </summary>


public struct FeatureMenuListItemInfo
{
    public string IdTextField;
    public string SourceName;
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

