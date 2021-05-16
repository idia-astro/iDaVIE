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
public struct SofiaListItemInfo
{
    public string IdTextField;
    public string SourceName;
    //public bool IsVisible;
    public int ParentListIndex;
    public Feature Feature;
}

public class SofiaRecyclableScrollerDataSource : MonoBehaviour, IRecyclableScrollRectDataSource
{
    /*
    [SerializeField]
    RecyclableScrollRect _recyclableScrollRect;
*/
     //private int _dataLength;

    //private List<FeatureSetRenderer> _featureSetRendererList;

    //public List<SofiaListItemInfo> SofiaList { get; private set; } = new List<SofiaListItemInfo>();
    private List<SofiaListItemInfo> _sofiaList;
/*
    [SerializeField]
    private GameObject VolumeDataSetManager;
*/
    //private FeatureSetManager _featureSetManager;



    //private List<FeatureSetRenderer> _featureSetRendererList;
    //public int CurrentFeatureSetIndex {get; private set;} = -1;

    //public TMP_Text ListTitle; 

    [SerializeField]
    private FeatureSetRenderer FeatureSetRenderer;




    //Recyclable scroll rect's data source must be assigned in Awake.
    private void Awake()
    {
        //_featureSetManager =  VolumeDataSetManager.gameObject.transform.Find("CubePrefab(Clone)").gameObject.transform.Find("FeatureSetManager").GetComponent<FeatureSetManager>();
        //_featureSetRendererList = _featureSetManager.ImportedFeatureSetList;
        //_recyclableScrollRect.DataSource = this;

    }


    private void OnEnable()         //onenable, disable other data list
    {

    }
    

    void Start()
    {
        //if (_sofiaList == null)
        //{
        //    InitData();
        //}
    }

    //Initialising _contactList with dummy data 
    public void InitData()
    {
        _sofiaList = new List<SofiaListItemInfo>();
        //if (SofiaList != null) SofiaList.Clear();
        //SofiaMenuData
        for (int i = 0; i < FeatureSetRenderer.FeatureList.Count; i++)
        {
            SofiaListItemInfo obj = new SofiaListItemInfo();
            obj.IdTextField = (i+1).ToString();
            obj.SourceName = FeatureSetRenderer.FeatureList[i].Name;
            obj.Feature = FeatureSetRenderer.FeatureList[i];
            //obj.IsVisible = FeatureList[i].Visible;
            //obj.Name = i + "_Name";
            //obj.Gender = genders[Random.Range(0, 2)];
            //obj.id = "item : " + i;
            //_contactList.Add(obj);
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
        var item = cell as SofiaCell;
        item.ConfigureCell(_sofiaList[index], index);        //need to set status of visibility on button here...
    }

    #endregion



}

