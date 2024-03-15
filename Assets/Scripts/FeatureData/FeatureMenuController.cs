using DataFeatures;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VolumeData;
using System.IO;
using System;
using PolyAndCode.UI;
using UnityEngine.Serialization;

public class FeatureMenuController : MonoBehaviour
{
    public GameObject volumeDatasetRendererObj;
    public GameObject RecyclableScrollViewPrefab;
    public GameObject InfoWindow = null;

    public RecyclableScrollRect RecyclableScrollView;
    
    public TMP_Text ListTitle; 
    public Image ListColorDisplay;
    public TMP_Text SaveConfirmation;
    public int CurrentFeatureSetIndex {get; private set;}
    
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;
    
    private FeatureSetManager _featureSetManager;
    private List<FeatureSetRenderer> _featureSetRendererList;
    private bool _isListInitialized = false;
    
    //The type of feature list that this menu controller will display. Set from inspector.
    [FormerlySerializedAs("featureSetType")] [SerializeField]
    private FeatureSetType _featureSetType;
    
    /// <summary>
    /// When enabled, get the active VolumeDataSet and find its attached FeatureSetManager
    /// </summary>
    void OnEnable() {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }
        if (_activeDataSet != null)
        {
            _featureSetManager = _activeDataSet.GetComponentInChildren<FeatureSetManager>();
        }

        
    }

    /// <summary>
    /// When disabling, reset the active dataset and feature set manager variables to null
    /// </summary>
    private void OnDisable()
    {
        _activeDataSet = null;
        CurrentFeatureSetIndex = 0;
        _featureSetManager = null;
        _featureSetRendererList = null;
        _dataSets = null;
    }

    void Update()
    {
        //need to check if new FeatureSetManager is loaded
        if (!_featureSetManager || _featureSetRendererList == null)
        {
            _isListInitialized = false;
        }
        //if list is not initialized and featureSetManager is not null, reset the list
        //TODO: check the initialization processes of these lists
        if (!_isListInitialized && _featureSetManager)
        {
            if (RecyclableScrollView)
            {
                Destroy(RecyclableScrollView.gameObject);
                RecyclableScrollView = null;
            }

            ListTitle.text = "";
            RefreshListColor();
            
            switch (_featureSetType)
            {
                case FeatureSetType.Mask:
                    _featureSetRendererList = _featureSetManager.MaskFeatureSetList;
                    break;
                case FeatureSetType.Imported:
                    _featureSetRendererList = _featureSetManager.ImportedFeatureSetList;
                    break;
                case FeatureSetType.New:
                    _featureSetRendererList = _featureSetManager.NewFeatureSetList;
                    break;
            }            
            _isListInitialized = true;
        }
        if (!RecyclableScrollView && _featureSetRendererList != null && _featureSetRendererList.Count > 0)
        {
            RecyclableScrollView =  Instantiate(RecyclableScrollViewPrefab, this.transform).GetComponent<RecyclableScrollRect>();
            RecyclableScrollView.Initialize(_featureSetRendererList[0].FeatureMenuScrollerDataSource);
            ListTitle.text = _featureSetRendererList[CurrentFeatureSetIndex].name;
            _featureSetManager.NeedToRespawnMenuList = true;
        }
        if (_featureSetManager?.NeedToRespawnMenuList == true && RecyclableScrollView != null)
        {
            if (_featureSetManager.SelectedFeature?.Index != null && _featureSetManager.SelectedFeature.Index != -1)
            {
                UpdateInfo();
                DisplaySet(_featureSetManager.SelectedFeature.FeatureSetParent.Index);
                RecyclableScrollView.JumpToCell(_featureSetManager.SelectedFeature.Index);
            }
            else
                RecyclableScrollView.ReloadData();
            _featureSetManager.NeedToRespawnMenuList = false;
        }
        if (_featureSetManager?.NeedToUpdateInfo == true)
        {
            UpdateInfo();
            _featureSetManager.NeedToUpdateInfo = false;
        }
    }

    /// <summary>
    /// Get the first active VolumeDataSetRenderer from the list of VolumeDataSetRenderers
    /// </summary>
    /// <returns>Active VolumeDataSetRenderer</returns>
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

    /// <summary>
    /// Switch to the next set in menu controller's feature set list
    /// </summary>
    public void DisplayNextSet()
    {
        if (_featureSetRendererList?.Count > 1)
        {
            CurrentFeatureSetIndex++;
            if (CurrentFeatureSetIndex >= _featureSetRendererList.Count)
                CurrentFeatureSetIndex = 0;
            DisplaySet(CurrentFeatureSetIndex);
        }
    }

    /// <summary>
    /// Switch to the previous set in menu controller's feature set list
    /// </summary>
    public void DisplayPreviousSet()
    {
        if (_featureSetRendererList?.Count > 1)
        {
            CurrentFeatureSetIndex--;
            if (CurrentFeatureSetIndex < 0)
                CurrentFeatureSetIndex = _featureSetRendererList.Count - 1;
            DisplaySet(CurrentFeatureSetIndex);
        }
    }

    /// <summary>
    /// Switch to the designated set in menu controller's feature set list
    /// </summary>
    /// <param name="setIndex"></param>
    public void DisplaySet(int setIndex)
    {
        if (setIndex > _featureSetRendererList.Count - 1 || setIndex < 0)
        {
            Debug.Log("Invalid Source List to display!");
            return;
        }
        if (_featureSetRendererList.Count > 0)
        {
            CurrentFeatureSetIndex = setIndex;
            ListTitle.text = _featureSetRendererList[CurrentFeatureSetIndex].name;
            RecyclableScrollView.DataSource = _featureSetRendererList[CurrentFeatureSetIndex].FeatureMenuScrollerDataSource;
            RecyclableScrollView.ReloadData();
        }
        RefreshListColor();
    }

    public void RefreshListColor()
    {
        ListColorDisplay.color = _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor;
    }
        
    /// <summary>
    /// Change the color of the current feature set to the next color in the list of feature colors
    /// </summary>
    public void ChangeColor()
        {
            int nextIndex = System.Array.IndexOf(FeatureSetManager.FeatureColors, _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor) + 1;
            if (nextIndex >= FeatureSetManager.FeatureColors.Length)
            {
                nextIndex = 0;
            }
            _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor = FeatureSetManager.FeatureColors[nextIndex];
            _featureSetRendererList[CurrentFeatureSetIndex].UpdateColor();
            ListColorDisplay.color = _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor;
            RefreshListColor();

        }


    public void ToggleListVisibility()
    {
        if (!_featureSetRendererList[CurrentFeatureSetIndex].featureSetVisible)
        {
            MakeListVisible();
            GameObject.Find("RenderMenu")?.gameObject.transform.Find("PanelContents")?.gameObject.transform.Find("SofiaListPanel")?.gameObject.transform.Find("ListButtons")?.gameObject.transform.Find("ListVisibilityButton")?.gameObject.transform.Find("VisibleImage")?.gameObject.SetActive(false);
            GameObject.Find("RenderMenu")?.gameObject.transform.Find("PanelContents")?.gameObject.transform.Find("SofiaListPanel")?.gameObject.transform.Find("ListButtons")?.gameObject.transform.Find("ListVisibilityButton")?.gameObject.transform.Find("InvisibleImage")?.gameObject.SetActive(true);
        }
        else
        {
            MakeListInvisible();
            GameObject.Find("RenderMenu")?.gameObject.transform.Find("PanelContents")?.gameObject.transform.Find("SofiaListPanel")?.gameObject.transform.Find("ListButtons")?.gameObject.transform.Find("ListVisibilityButton")?.gameObject.transform.Find("VisibleImage")?.gameObject.SetActive(true);
            GameObject.Find("RenderMenu")?.gameObject.transform.Find("PanelContents")?.gameObject.transform.Find("SofiaListPanel")?.gameObject.transform.Find("ListButtons")?.gameObject.transform.Find("ListVisibilityButton")?.gameObject.transform.Find("InvisibleImage")?.gameObject.SetActive(false);  
        }
    }
    public void MakeListVisible()
    {
        _featureSetRendererList[CurrentFeatureSetIndex].SetVisibilityOn();
    }
    
    public void MakeListInvisible()
    {
        _featureSetRendererList[CurrentFeatureSetIndex].SetVisibilityOff();
    }

    public void ShowListInfo()
    {
        if (InfoWindow.activeSelf)
            InfoWindow.SetActive(false);
        else if (_featureSetManager.SelectedFeature != null)
            ShowInfo();
    }


    public void UpdateInfo()
    {
        var dataSet = _activeDataSet.GetDataSet();
        var textObject = InfoWindow.transform.Find("PanelContents").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform.Find("SourceInfoText").gameObject;
        textObject.GetComponent<TMP_Text>().text = "";
        if (_featureSetManager.SelectedFeature != null)
        {
            textObject.GetComponent<TMP_Text>().text +=
                $"Source # : {_featureSetManager.SelectedFeature.Id + 1}{Environment.NewLine}";
            if (_activeDataSet.HasWCS)
            {
                double centerX, centerY, centerZ, ra, dec, physz, normR, normD, normZ;
                if (_featureSetManager.VolumeRenderer.SourceStatsDict == null)
                {
                    centerX = _featureSetManager.SelectedFeature.Center.x;
                    centerY = _featureSetManager.SelectedFeature.Center.y;
                    centerZ = _featureSetManager.SelectedFeature.Center.z;
                }
                else
                {
                    centerX = _featureSetManager.VolumeRenderer
                        .SourceStatsDict[_featureSetManager.SelectedFeature.Index].cX;
                    centerY = _featureSetManager.VolumeRenderer
                        .SourceStatsDict[_featureSetManager.SelectedFeature.Index].cY;
                    centerZ = _featureSetManager.VolumeRenderer
                        .SourceStatsDict[_featureSetManager.SelectedFeature.Index].cZ;
                }

                AstTool.Transform3D(_activeDataSet.AstFrame, centerX, centerY, centerZ, 1, out ra, out dec,
                    out physz);
                AstTool.Norm(_activeDataSet.AstFrame, ra, dec, physz, out normR, out normD, out normZ);

                textObject.GetComponent<TMP_Text>().text +=
                    $"RA : {dataSet.GetFormattedCoord(normR, 1)}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text +=
                    $"Dec : {dataSet.GetFormattedCoord(normD, 2)}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text +=
                    FormattableString.Invariant($"{_activeDataSet.Data.GetAstAttribute("System(3)")} ({_activeDataSet.Data.GetAxisUnit(3)}) : {normZ:F3}{Environment.NewLine}");
            }
            if (_featureSetManager.SelectedFeature.FeatureSetParent.RawDataKeys != null)
            {
                for (var i = 0; i < _featureSetManager.SelectedFeature.FeatureSetParent.RawDataKeys.Length; i++)
                {
                    var dataToAdd = _featureSetManager.SelectedFeature.FeatureSetParent.RawDataTypes[i] == "float" ? FormattableString.Invariant($"{Convert.ToDouble(_featureSetManager.SelectedFeature.RawData[i]):F3}") : _featureSetManager.SelectedFeature.RawData[i];
                    textObject.GetComponent<TMP_Text>().text += $"{_featureSetManager.SelectedFeature.FeatureSetParent.RawDataKeys[i]} : {dataToAdd}{Environment.NewLine}";
                }
            }
            var flag = _featureSetManager.SelectedFeature.Flag;
            if (flag.Equals(" ") || flag.Equals(""))
                flag = "No flag";
            textObject.GetComponent<TMP_Text>().text += "Flag: " + flag + Environment.NewLine;
        }
        else
            textObject.GetComponent<TMP_Text>().text += "Please select a feature.";
    }

    public void ShowInfo()
    {
        InfoWindow.SetActive(true);
        UpdateInfo();
    }

    public void SaveListAsVoTable()
    {
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/Catalogs");
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
        var filename = string.Format("iDaVIE_cat_{0}.xml", DateTime.Now.ToString("yyyyMMdd_Hmmss"));
        var path = Path.Combine(directoryPath, filename);
        _featureSetRendererList[CurrentFeatureSetIndex].SaveAsVoTable(path);
        SaveConfirmation.text = $"Table saved as {filename}";
    }

    public void AddSelectedFeatureToNewSet()
    {
        _featureSetManager.AddSelectedFeatureToNewSet();
    }

}
