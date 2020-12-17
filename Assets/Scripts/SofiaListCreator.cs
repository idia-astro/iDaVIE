using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VolumeData;

public class SofiaListCreator : MonoBehaviour
{
    
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    [SerializeField]
    private Transform SpawnPoint = null;

    [SerializeField]
    private GameObject item = null;

    [SerializeField]
    private RectTransform content = null;

    public List<GameObject> SofiaObjectsList {get; private set;}

    public GameObject volumeDatasetRendererObj = null;
    public GameObject SofiaNewListController = null;
    public GameObject InfoWindow = null;

    public TMP_Text ListTitle; 

    private FeatureSetManager featureSetManager;
    private FeatureSetRenderer featureSetRenderer;
    private int numberFeatureSetRenderers;
    private List<FeatureSetRenderer> _featureSetRendererList;

    public bool ShowsImportedList;

    public int NumberOfFeatures {get; private set;}
    private bool _initialized = false;
    public int CurrentFeatureSetIndex {get; private set;}

    // Start is called before the first frame update
    void Start()
    {
        SofiaObjectsList = new List<GameObject>();
       
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);


        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }

        if (_activeDataSet != null)
        {
            featureSetManager = _activeDataSet.GetComponentInChildren<FeatureSetManager>();
          //  featureSetManager.ImportFeatureSet();
        }
        _featureSetRendererList = (ShowsImportedList ? featureSetManager.ImportedFeatureSetList: featureSetManager.GeneratedFeatureSetList);
        //var featureSetRendererList = featureSetManager.GetComponentsInChildren<FeatureSetRenderer>();
        for (int i = 0; i < _featureSetRendererList.Count; i++)
            SpawnList(i);
        DisplaySet(0);
        //transform.Find("ListName").gameObject.transform.Find("Text").GetComponent<TMP_Text>().text = _featureSetRendererList[0].name;

        var selectedFeature = _activeDataSet.gameObject.GetComponentInChildren<VolumeDataSetRenderer>().FeatureSetManagerPrefab.SelectedFeature;
        if (selectedFeature != null)
            GetComponent<CustomDragHandler>().FocusOnFeature(selectedFeature);
        _initialized = true;
    
    }


    // Update is called once per frame
    void OnEnable()
    {
        if (_initialized)
        {
            var selectedFeature = _activeDataSet.gameObject.GetComponentInChildren<VolumeDataSetRenderer>().FeatureSetManagerPrefab.SelectedFeature;
            if (selectedFeature != null)
                GetComponent<CustomDragHandler>().FocusOnFeature(selectedFeature);
        }
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {

        foreach (var dataSet in _dataSets)
        {

            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;

    }

    public void DisplayNextSet()
    {
        if (_featureSetRendererList.Count > 1)
        {
            featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex++;
            if (CurrentFeatureSetIndex >= _featureSetRendererList.Count)
                CurrentFeatureSetIndex = 0;
            if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
                SpawnList(CurrentFeatureSetIndex);
            else
            {
                featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
                featureSetRenderer.MenuList.SetActive(true);
                ListTitle.text = featureSetRenderer.name;
                NumberOfFeatures = featureSetRenderer.NumberFeatures;
            }
        }
    }

    public void DisplayPreviousSet()
    {
        if (_featureSetRendererList.Count > 1)
        {
            featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex--;
            if (CurrentFeatureSetIndex < 0)
                CurrentFeatureSetIndex = _featureSetRendererList.Count - 1;
            if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
                SpawnList(CurrentFeatureSetIndex);
            else
            {
                featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
                featureSetRenderer.MenuList.SetActive(true);
                ListTitle.text = featureSetRenderer.name;
                NumberOfFeatures = featureSetRenderer.NumberFeatures;
            }
        }
    }

        public void ChangeColor()
        {
            int nextIndex = System.Array.IndexOf(FeatureSetManager.FeatureColors, featureSetRenderer.FeatureColor) + 1;
            if (nextIndex >= FeatureSetManager.FeatureColors.Length)
                nextIndex = 0;
            _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor = FeatureSetManager.FeatureColors[nextIndex];
            _featureSetRendererList[CurrentFeatureSetIndex].UpdateColor();
            if( featureSetManager.SelectedFeature != null)
                featureSetManager.SelectedFeature.LinkedListItem.GetComponent<Image>().color = featureSetRenderer.FeatureColor;
        }

        public void DisplaySet(int i)
    {
        if (i > _featureSetRendererList.Count - 1 || i < 0)
        {
            Debug.Log("Invalid Source List to display!");
            return;
        }
        if (_featureSetRendererList.Count > 1)
        {
            featureSetRenderer.MenuList.SetActive(false);
            CurrentFeatureSetIndex = i;
            if (_featureSetRendererList[CurrentFeatureSetIndex].MenuList == null)
                SpawnList(CurrentFeatureSetIndex);
            else
            {
                featureSetRenderer =  _featureSetRendererList[CurrentFeatureSetIndex];
                featureSetRenderer.MenuList.SetActive(true);
                ListTitle.text = featureSetRenderer.name;
                NumberOfFeatures = featureSetRenderer.NumberFeatures;
            }
        }
    }

    private void SpawnList(int index)
    {
        featureSetRenderer = _featureSetRendererList[index];
        ListTitle.text = featureSetRenderer.name;
        //Debug.Log($"Number of items: {numberOfItems}");
        NumberOfFeatures = featureSetRenderer.NumberFeatures;
        //setContent Holder Height;
        content.sizeDelta = new Vector2(0, NumberOfFeatures * 100);

        GameObject SourceListObject = new GameObject();
        SourceListObject.name = "Source List #" + index;
        Instantiate(SourceListObject, new Vector3(SpawnPoint.position.x+300, 0, SpawnPoint.position.z), SpawnPoint.localRotation);
        SourceListObject.transform.SetParent(SpawnPoint, false);

        for (int i = 0; i < NumberOfFeatures; i++)
        {
            // 100 Height of item
            float spawnY = i * 100;
            //newSpawn Position
            Vector3 pos = new Vector3(SpawnPoint.position.x+300, -spawnY, SpawnPoint.position.z);
            //instantiate item
            GameObject SpawnedItem = Instantiate(item, pos, SpawnPoint.localRotation);
            //add to list of spawned Sofia items
            SofiaObjectsList.Add(SpawnedItem);
            //setParent
            SpawnedItem.transform.SetParent(SourceListObject.transform, false);
            //get ItemDetails Component
            SofiaListItem itemDetails = SpawnedItem.GetComponent<SofiaListItem>();

            itemDetails.SofiaNewListController = SofiaNewListController;
            //set name
            itemDetails.idTextField.text = (i+1).ToString();

            itemDetails.feature = featureSetRenderer.FeatureList[i];

            itemDetails.feature.LinkedListItem = SpawnedItem;

            itemDetails.InfoWindow = InfoWindow;

            itemDetails.ParentListIndex = index;

            if (!itemDetails.feature.Visible)
                itemDetails.ToggleVisibility();

            itemDetails.sourceName.text = itemDetails.feature.Name;
            SpawnedItem.name = "ListItem: " + itemDetails.feature.Name;


            if (i%2!=0)
                itemDetails.GetComponent<Image>().color = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);


           
        }
        featureSetRenderer.MenuList = SourceListObject;
    } 

    public void MakeListVisible()
    {
        featureSetRenderer.SetVisibilityOn();
    }
    
    public void MakeListInvisible()
    {
        featureSetRenderer.SetVisibilityOff();
    }

    public void ShowListInfo()
    {
        if (InfoWindow.activeSelf)
            InfoWindow.SetActive(false);
        else if (featureSetManager.SelectedFeature != null)
            featureSetManager.SelectedFeature.LinkedListItem.GetComponent<SofiaListItem>().ShowInfo();
    }
}
