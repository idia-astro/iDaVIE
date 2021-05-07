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
    [SerializeField]
    RecyclableScrollRect _recyclableScrollRect;

     private int _dataLength;

    //private List<FeatureSetRenderer> _featureSetRendererList;

    //public List<SofiaListItemInfo> SofiaList { get; private set; } = new List<SofiaListItemInfo>();
    private List<SofiaListItemInfo> _sofiaList;

    [SerializeField]
    private GameObject VolumeDataSetManager;

    private FeatureSetManager _featureSetManager;

    public GameObject InfoWindow = null;

    private List<FeatureSetRenderer> _featureSetRendererList;
    public int CurrentFeatureSetIndex {get; private set;} = -1;

    public TMP_Text ListTitle; 






    //Recyclable scroll rect's data source must be assigned in Awake.
    private void Awake()
    {
        _featureSetManager =  VolumeDataSetManager.gameObject.transform.Find("CubePrefab(Clone)").gameObject.transform.Find("FeatureSetManager").GetComponent<FeatureSetManager>();
        _featureSetRendererList = _featureSetManager.ImportedFeatureSetList;
        _recyclableScrollRect.DataSource = this;
        if (_featureSetRendererList.Count > 0)
        {
            DisplaySet(0);
        }
        else
            InitData(-1);
    }

    private void OnEnable()         //onenable, disable other data list
    {
        if (CurrentFeatureSetIndex == -1 && _featureSetRendererList.Count > 0)
        {
            DisplaySet(0);
        }
    }
    

    void Start()
    {
    }

    //Initialising _contactList with dummy data 
    private void InitData(int listIndex)
    {
        if (listIndex >= 0)
            _sofiaList = _featureSetRendererList[listIndex].SofiaList;
        else
            _sofiaList = new List<SofiaListItemInfo>();
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

 public void DisplayNextSet()
    {
        if (_featureSetRendererList.Count > 1)
        {
            //featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex++;
            if (CurrentFeatureSetIndex >= _featureSetRendererList.Count)
                CurrentFeatureSetIndex = 0;
            DisplaySet(CurrentFeatureSetIndex);
            //if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
                //SpawnList(CurrentFeatureSetIndex);
            //featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
            //_featureSetRendererList[CurrentFeatureSetIndex].MenuList.SetActive(true);
            //ListTitle.text = featureSetRenderer.name;
            //ListColorDisplay.color = featureSetRenderer.FeatureColor;
            //NumberOfFeatures = featureSetRenderer.FeatureList.Count;
        }
    }

    public void DisplayPreviousSet()
    {
        if (_featureSetRendererList.Count > 1)
        {
            //featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex--;
            if (CurrentFeatureSetIndex < 0)
                CurrentFeatureSetIndex = _featureSetRendererList.Count - 1;
            DisplaySet(CurrentFeatureSetIndex);
            //if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
             //   SpawnList(CurrentFeatureSetIndex);
            //else
            //{
            //    featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
             //   featureSetRenderer.MenuList.SetActive(true);
             //   ListTitle.text = featureSetRenderer.name;
             //   ListColorDisplay.color = featureSetRenderer.FeatureColor;
             //   NumberOfFeatures = featureSetRenderer.FeatureList.Count;
            }
        }
    

        public void ChangeColor()
        {
            int nextIndex = System.Array.IndexOf(FeatureSetManager.FeatureColors, _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor) + 1;
            if (nextIndex >= FeatureSetManager.FeatureColors.Length)
                nextIndex = 0;
            _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor = FeatureSetManager.FeatureColors[nextIndex];
            _featureSetRendererList[CurrentFeatureSetIndex].UpdateColor();
            //ListColorDisplay.color = featureSetRenderer.FeatureColor;
            //Uncomment to change source's color in list
            //if( featureSetManager.SelectedFeature != null)
            //    featureSetManager.SelectedFeature.LinkedListItem.GetComponent<Image>().color = featureSetRenderer.FeatureColor;
        }

        public void DisplaySet(int i)
    {
        if (i > _featureSetRendererList.Count - 1 || i < 0)
        {
            Debug.Log("Invalid Source List to display!");
            return;
        }
        if (_featureSetRendererList.Count > 0)
        {
            //featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex = i;
            //if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
            //    SpawnList(CurrentFeatureSetIndex);
            //else
            //{
            //    featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
                //featureSetRenderer.MenuList.SetActive(true);
                ListTitle.text = _featureSetRendererList[CurrentFeatureSetIndex].name;
                //ListColorDisplay.color = _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor;
                //_featureSetRendererList[CurrentFeatureSetIndex]
                InitData(CurrentFeatureSetIndex);
                _recyclableScrollRect.ReloadData();
                //NumberOfFeatures = featureSetRenderer.FeatureList.Count;
            
        }
    }

    public void ToggleListVisibility()
    {
        if (!_featureSetRendererList[CurrentFeatureSetIndex].featureSetVisible)
        {
            MakeListVisible();
            GameObject.Find("RenderMenu").gameObject.transform.Find("PanelContents").gameObject.transform.Find("SofiaListPanel").gameObject.transform.Find("ListButtons").gameObject.transform.Find("ListVisibilityButton").gameObject.transform.Find("VisibleImage").gameObject.SetActive(false);
            GameObject.Find("RenderMenu").gameObject.transform.Find("PanelContents").gameObject.transform.Find("SofiaListPanel").gameObject.transform.Find("ListButtons").gameObject.transform.Find("ListVisibilityButton").gameObject.transform.Find("InvisibleImage").gameObject.SetActive(true);
        }
        else
        {
            MakeListInvisible();
            GameObject.Find("RenderMenu").gameObject.transform.Find("PanelContents").gameObject.transform.Find("SofiaListPanel").gameObject.transform.Find("ListButtons").gameObject.transform.Find("ListVisibilityButton").gameObject.transform.Find("VisibleImage").gameObject.SetActive(true);
            GameObject.Find("RenderMenu").gameObject.transform.Find("PanelContents").gameObject.transform.Find("SofiaListPanel").gameObject.transform.Find("ListButtons").gameObject.transform.Find("ListVisibilityButton").gameObject.transform.Find("InvisibleImage").gameObject.SetActive(false);
        }
    }

        public void MakeListVisible()
    {
        _featureSetRendererList[CurrentFeatureSetIndex].SetVisibilityOn();
        _featureSetRendererList[CurrentFeatureSetIndex].CreateMenuList();
        //_recyclableScrollRect.ReloadData();

    }
    
    public void MakeListInvisible()
    {
        _featureSetRendererList[CurrentFeatureSetIndex].SetVisibilityOff();
        _featureSetRendererList[CurrentFeatureSetIndex].CreateMenuList();
        //_recyclableScrollRect.ReloadData();
    }

    public void ShowListInfo()
    {
        if (InfoWindow.activeSelf)
            InfoWindow.SetActive(false);
        else if (_featureSetManager.SelectedFeature != null)
            _featureSetManager.SelectedFeature.LinkedListItem.GetComponent<SofiaCell>().ShowInfo();
    }
}

