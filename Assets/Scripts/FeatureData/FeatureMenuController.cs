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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataFeatures;
using iDaVIE.Domain.Feature;
using iDaVIE.Infrastructure.Unity;
using PolyAndCode.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

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

    private FeatureVisualiser _featureVisualiser;
    private List<FeatureSetRenderer> _featureSetRendererList;
    private bool _isListInitialized = false;
    private bool _needToRespawnMenuList = false;
    private bool _needToUpdateInfo = false;
    
    //The type of feature list that this menu controller will display. Set from inspector.
    public FeatureSetType FeatureSetType;

    /// <summary>
    /// When enabled, get the active VolumeDataSet and find its attached FeatureVisualiser
    /// </summary>
    void OnEnable() {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
            _activeDataSet = firstActive;

        if (_activeDataSet != null)
            _featureVisualiser = _activeDataSet.FeatureVisualiser;

        if (_featureVisualiser?.Service != null)
            _featureVisualiser.Service.FeatureSelectionChanged += OnFeatureSelectionChanged;
    }

    private void OnFeatureSelectionChanged(Feature previous, Feature next)
    {
        _needToRespawnMenuList = true;
        _needToUpdateInfo = true;
    }

    /// <summary>
    /// When disabling, reset the active dataset and feature visualiser variables to null
    /// </summary>
    private void OnDisable()
    {
        if (_featureVisualiser?.Service != null)
            _featureVisualiser.Service.FeatureSelectionChanged -= OnFeatureSelectionChanged;
        _activeDataSet = null;
        CurrentFeatureSetIndex = 0;
        _featureVisualiser = null;
        _featureSetRendererList = null;
        _dataSets = null;
    }

    void Update()
    {
        if (_featureVisualiser == null || _featureSetRendererList == null)
            _isListInitialized = false;

        if (!_isListInitialized && _featureVisualiser != null)
        {
            if (RecyclableScrollView)
            {
                Destroy(RecyclableScrollView.gameObject);
                RecyclableScrollView = null;
            }
            ListTitle.text = "";
            switch (FeatureSetType)
            {
                case FeatureSetType.Mask:
                    _featureSetRendererList = _featureVisualiser.GetComponentsInChildren<FeatureSetRenderer>()
                        .Where(r => r.FeatureSetType == FeatureSetType.Mask).ToList();
                    break;
                case FeatureSetType.Imported:
                    _featureSetRendererList = _featureVisualiser.GetComponentsInChildren<FeatureSetRenderer>()
                        .Where(r => r.FeatureSetType == FeatureSetType.Imported).ToList();
                    break;
                case FeatureSetType.New:
                    _featureSetRendererList = _featureVisualiser.GetComponentsInChildren<FeatureSetRenderer>()
                        .Where(r => r.FeatureSetType == FeatureSetType.New).ToList();
                    break;
            }
            _isListInitialized = true;
        }

        if (!RecyclableScrollView && _featureSetRendererList != null && _featureSetRendererList.Count > 0)
        {
            RecyclableScrollView = Instantiate(RecyclableScrollViewPrefab, this.transform).GetComponent<RecyclableScrollRect>();
            RecyclableScrollView.Initialize(_featureSetRendererList[0].FeatureMenuScrollerDataSource);
            ListTitle.text = _featureSetRendererList[CurrentFeatureSetIndex].name;
            RefreshListColor();
            _needToRespawnMenuList = true;
        }

        var selectedFeature = _featureVisualiser?.Service?.SelectedFeature;

        if (_needToRespawnMenuList && RecyclableScrollView != null)
        {
            if (selectedFeature != null && selectedFeature.Index != -1)
            {
                UpdateInfo();
                if (_featureSetRendererList.Contains(selectedFeature.FeatureSetParent))
                {
                    DisplaySet(selectedFeature.FeatureSetParent.Index);
                    RecyclableScrollView.JumpToCell(selectedFeature.Index);
                }
                else if (FeatureSetType == FeatureSetType.New)
                {
                    RecyclableScrollView.ReloadData();
                }
            }
            else
                RecyclableScrollView.ReloadData();
            _needToRespawnMenuList = false;
        }

        if (_needToUpdateInfo)
        {
            UpdateInfo();
            _needToUpdateInfo = false;
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
        var allColors = FeatureCatalog.FeatureColors.Select(c => new Color(c.R, c.G, c.B, c.A)).ToArray();
        Color currentColor = _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor;

        if (!allColors.Contains(currentColor))
            _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor = allColors[0];

        Color nextColor;
        var allRenderers = _featureVisualiser != null
            ? _featureVisualiser.GetComponentsInChildren<FeatureSetRenderer>()
            : System.Array.Empty<FeatureSetRenderer>();
        List<Color> forbiddenColors = allRenderers.Select(r => r.FeatureColor).ToList();

        if (forbiddenColors.Count > allColors.Length)
        {
            Debug.Log("All colors are used! Assigning new random color.");
            nextColor = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f));
        }
        else
        {
            currentColor = _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor;
            int nextColorIndex = System.Array.IndexOf(allColors, currentColor) + 1;
            do
            {
                if (nextColorIndex >= allColors.Length)
                    nextColorIndex = 0;
                nextColor = allColors[nextColorIndex];
                nextColorIndex++;
            } while (forbiddenColors.Contains(nextColor));
        }
        _featureSetRendererList[CurrentFeatureSetIndex].FeatureColor = nextColor;
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
        else if (_featureVisualiser?.Service?.SelectedFeature != null)
            ShowInfo();
    }


    public void UpdateInfo()
    {
        var dataSet = _activeDataSet.GetDataSet();
        var textObject = InfoWindow.transform.Find("PanelContents").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform.Find("SourceInfoText").gameObject;
        textObject.GetComponent<TMP_Text>().text = "";
        var selectedFeature = _featureVisualiser?.Service?.SelectedFeature;
        if (selectedFeature != null)
        {
            textObject.GetComponent<TMP_Text>().text +=
                $"Source # : {selectedFeature.Id + 1}{Environment.NewLine}";

            double centerX, centerY, centerZ, ra, dec, physz, normR, normD, normZ;

            // if the selected feature is from a mask, get the centroid from the sourceStats dictionary
            if (selectedFeature.FeatureSetParent.FeatureSetType == FeatureSetType.Mask
                && _activeDataSet.SourceStatsDict != null)
            {
                centerX = _activeDataSet.SourceStatsDict.ElementAt(selectedFeature.Index).Value.cX;
                centerY = _activeDataSet.SourceStatsDict.ElementAt(selectedFeature.Index).Value.cY;
                centerZ = _activeDataSet.SourceStatsDict.ElementAt(selectedFeature.Index).Value.cZ;
                textObject.GetComponent<TMP_Text>().text += $"Centroid : {Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  x : {centerX:F5}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  y : {centerY:F5}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  z : {centerZ:F5}{Environment.NewLine}";
            }
            // otherwise, use the center of the feature cuboid
            else
            {
                centerX = selectedFeature.Center.x;
                centerY = selectedFeature.Center.y;
                centerZ = selectedFeature.Center.z;
                textObject.GetComponent<TMP_Text>().text += $"Center : {Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  x : {centerX}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  y : {centerY}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text += $"  z : {centerZ}{Environment.NewLine}";
            }

            // if there is an associated WCS, transform the designated center to RA, Dec, and physical z coords
            if (_activeDataSet.HasWCS)
            {
                AstTool.Transform3D(_activeDataSet.AstFrame, centerX, centerY, centerZ, 1, out ra, out dec,
                    out physz);
                AstTool.Norm(_activeDataSet.AstFrame, ra, dec, physz, out normR, out normD, out normZ);

                textObject.GetComponent<TMP_Text>().text +=
                    $"  RA : {dataSet.GetFormattedCoord(normR, 1)}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text +=
                    $"  Dec : {dataSet.GetFormattedCoord(normD, 2)}{Environment.NewLine}";
                textObject.GetComponent<TMP_Text>().text +=
                    FormattableString.Invariant($"  {_activeDataSet.Data.GetAstAttribute("System(3)")} ({_activeDataSet.Data.GetAxisUnit(3)}) : {normZ:F3}{Environment.NewLine}");
            }

            // if there are raw data associated with the feature, add to the info window
            if (selectedFeature.FeatureSetParent.RawDataKeys != null)
            {
                for (var i = 0; i < selectedFeature.FeatureSetParent.RawDataKeys.Length; i++)
                {
                    var key = selectedFeature.FeatureSetParent.RawDataKeys[i];
                    var dataToAdd = selectedFeature.FeatureSetParent.RawDataTypes[i] == "float"
                        ? FormattableString.Invariant($"{Convert.ToDouble(selectedFeature.RawData[i]):F3}")
                        : selectedFeature.RawData[i];
                    if (FeatureCatalog.UnitisedKeys.Contains(key.ToUpper()))
                        dataToAdd += $" {_activeDataSet.GetDataSet().GetPixelUnit()}";
                    textObject.GetComponent<TMP_Text>().text += $"{key} : {dataToAdd}{Environment.NewLine}";
                }
            }
            var flag = selectedFeature.Flag;
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
        _featureVisualiser?.Service?.AddSelectedFeatureToUserSet();
    }

}
