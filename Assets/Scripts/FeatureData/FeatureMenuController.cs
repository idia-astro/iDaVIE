using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VolumeData;
using System.IO;
using System;
using PolyAndCode.UI;

public class FeatureMenuController : MonoBehaviour
{
    
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    [SerializeField]
    private RectTransform content = null;

    public List<GameObject> SofiaObjectsList {get; private set;}

    public GameObject volumeDatasetRendererObj = null;
    public RecyclableScrollRect RecyclableScrollView;
    public GameObject InfoWindow = null;

    public TMP_Text ListTitle; 
    public Image ListColorDisplay;
    public TMP_Text SaveConfirmation;

    private FeatureSetManager featureSetManager;

    public int CurrentFeatureSetIndex {get; private set;}


    void OnEnable() {
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
        }
        if (featureSetManager.ImportedFeatureSetList.Count > 0)
        {
            RecyclableScrollView.Initialize(featureSetManager.ImportedFeatureSetList[0].FeatureMenuScrollerDataSource);
            ListTitle.text = featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].name;
            featureSetManager.NeedToRespawnMenuList = true;
        }   
    }

    void Update()
    {
        if (featureSetManager != null && featureSetManager.NeedToRespawnMenuList)
        {
            if (featureSetManager.SelectedFeature != null)
            {
                UpdateInfo();
                DisplaySet(featureSetManager.SelectedFeature.FeatureSetParent.Index);
                RecyclableScrollView.JumpToCell(featureSetManager.SelectedFeature.Index);
            }
            else
                RecyclableScrollView.ReloadData();
            featureSetManager.NeedToRespawnMenuList = false;
        }
        if (featureSetManager.NeedToUpdateInfo)
        {
            UpdateInfo();
            featureSetManager.NeedToUpdateInfo = false;
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
        if ( featureSetManager.ImportedFeatureSetList.Count > 1)
        {
            CurrentFeatureSetIndex++;
            if (CurrentFeatureSetIndex >= featureSetManager.ImportedFeatureSetList.Count)
                CurrentFeatureSetIndex = 0;
            DisplaySet(CurrentFeatureSetIndex);
        }
    }

    public void DisplayPreviousSet()
    {
        if (featureSetManager.ImportedFeatureSetList.Count > 1)
        {
            CurrentFeatureSetIndex--;
            if (CurrentFeatureSetIndex < 0)
                CurrentFeatureSetIndex = featureSetManager.ImportedFeatureSetList.Count - 1;
            DisplaySet(CurrentFeatureSetIndex);
        }
    }

    public void DisplaySet(int i)
    {
        if (i > featureSetManager.ImportedFeatureSetList.Count - 1 || i < 0)
        {
            Debug.Log("Invalid Source List to display!");
            return;
        }
        if (featureSetManager.ImportedFeatureSetList.Count > 0)
        {
            CurrentFeatureSetIndex = i;
            ListTitle.text = featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].name;
            RecyclableScrollView.DataSource = featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].FeatureMenuScrollerDataSource;
            RecyclableScrollView.ReloadData();
        }
    }

    public void ChangeColor()
        {
            int nextIndex = System.Array.IndexOf(FeatureSetManager.FeatureColors, featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].FeatureColor) + 1;
            if (nextIndex >= FeatureSetManager.FeatureColors.Length)
                nextIndex = 0;
            featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].FeatureColor = FeatureSetManager.FeatureColors[nextIndex];
            featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].UpdateColor();
            ListColorDisplay.color = featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].FeatureColor;
        }


    public void ToggleListVisibility()
    {
        if (!featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].featureSetVisible)
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
        featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].SetVisibilityOn();
    }
    
    public void MakeListInvisible()
    {
        featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].SetVisibilityOff();
    }

    public void ShowListInfo()
    {
        if (InfoWindow.activeSelf)
            InfoWindow.SetActive(false);
        else if (featureSetManager.SelectedFeature != null)
            ShowInfo();
    }


        public void UpdateInfo()
    {
        var textObject = InfoWindow.transform.Find("PanelContents").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform.Find("SourceInfoText").gameObject;
        textObject.GetComponent<TMP_Text>().text = "";
        textObject.GetComponent<TMP_Text>().text += $"Source # : {featureSetManager.SelectedFeature.Id + 1}{Environment.NewLine}";  
        if (_activeDataSet.HasWCS)
        {
            double centerX, centerY, centerZ, ra, dec, physz, normR, normD, normZ;
            if (featureSetManager.VolumeRenderer.SourceStatsDict == null)
            {
                centerX = featureSetManager.SelectedFeature.Center.x;
                centerY = featureSetManager.SelectedFeature.Center.y;
                centerZ = featureSetManager.SelectedFeature.Center.z;
            }
            else
            {
                centerX = featureSetManager.VolumeRenderer.SourceStatsDict[featureSetManager.SelectedFeature.Index].cX;
                centerY = featureSetManager.VolumeRenderer.SourceStatsDict[featureSetManager.SelectedFeature.Index].cY;
                centerZ = featureSetManager.VolumeRenderer.SourceStatsDict[featureSetManager.SelectedFeature.Index].cZ;
            }
            AstTool.Transform3D(_activeDataSet.AstFrame, centerX, centerY, centerZ, 1, out ra, out dec, out physz);
            AstTool.Norm(_activeDataSet.AstFrame, ra, dec, physz, out normR, out normD, out normZ);
            textObject.GetComponent<TMP_Text>().text += $"RA: {(180f * normR / Math.PI).ToString()}{Environment.NewLine}";
            textObject.GetComponent<TMP_Text>().text += $"Dec: {(180f * normD / Math.PI).ToString()}{Environment.NewLine}";
            textObject.GetComponent<TMP_Text>().text += $"{_activeDataSet.Data.GetAstAttribute("System(3)")}: {normZ.ToString()}{Environment.NewLine}";
        }
        for (int i = 0; i < featureSetManager.SelectedFeature.FeatureSetParent.RawDataKeys.Length; i++)
        {
            textObject.GetComponent<TMP_Text>().text += $"{featureSetManager.SelectedFeature.FeatureSetParent.RawDataKeys[i]} : {featureSetManager.SelectedFeature.RawData[i]}{Environment.NewLine}";
        }
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
        var filename = string.Format("iDaVIE_cat_{0}.xml", DateTime.Now.ToString("yyyyMMdd_Hmmssffff"));
        var path = Path.Combine(directoryPath, filename);
        featureSetManager.ImportedFeatureSetList[CurrentFeatureSetIndex].SaveAsVoTable(path);
        SaveConfirmation.text = $"Table saved as {filename}";
    }

}
