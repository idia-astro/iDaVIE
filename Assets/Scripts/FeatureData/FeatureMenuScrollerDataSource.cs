using System.Collections.Generic;
using UnityEngine;
using DataFeatures;
using VolumeData;
using PolyAndCode.UI;
using TMPro;

/// <summary>
/// Demo controller class for Recyclable Scroll Rect. 
/// A controller class is responsible for providing the scroll rect with datasource. Any class can be a controller class. 
/// The only requirement is to inherit from IRecyclableScrollRectDataSource and implement the interface methods
/// </summary>

//Dummy Data model for demostraion
public struct FeatureMenuListItemInfo
{
    public string IdTextField;
    public string SourceName;
    //public bool IsVisible;
    public int ParentListIndex;
    public Feature Feature;
}

public class FeatureMenuScrollerDataSource : MonoBehaviour, IRecyclableScrollRectDataSource
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
        //return _featureSetRendererList[CurrentFeatureSetIndex].FeatureList.Count;
        return _sofiaList.Count;
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

