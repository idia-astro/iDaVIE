using UnityEngine;
using UnityEngine.UI;
using PolyAndCode.UI;
using DataFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using VolumeData;

//Cell class corresponding to data features for Recyclable Scroll Rect
public class FeatureMenuCell : MonoBehaviour, ICell
{
    //UI
    
    public Text idTextField = null;
    public Text sourceName = null;
    public GameObject checkboxImg = null;
    public int ParentListIndex {get; set;}
    public Feature feature;
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;
    public GameObject volumeDatasetRendererObj = null;
    private FeatureSetManager featureSetManager;

    private static Color _lightGrey = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
    private static Color _darkGrey = new Color(0.2384301f, 0.3231786f, 0.3584906f, 1f);


    //Model
    private FeatureMenuListItemInfo _featureMenuListItemInfo;

    public int CellIndex {get; private set;} 

    public float CellHeight {get; private set;}


    //This is called from the SetCell method in DataSource
    public void ConfigureCell(FeatureMenuListItemInfo featureMenuListItemInfo ,int cellIndex)
    {
        CellIndex = cellIndex;
        _featureMenuListItemInfo = featureMenuListItemInfo;
        idTextField.text = featureMenuListItemInfo.IdTextField;
        sourceName.text = featureMenuListItemInfo.SourceName;
        feature = featureMenuListItemInfo.Feature;
        if (feature.Flag == null)
            SetFlag("");
        if (feature.Visible)
            SetVisibilityIconsOn();
        else
            SetVisibilityIconsOff();
        if (featureMenuListItemInfo.Feature.Selected)
            GetComponent<Image>().color = Color.red;
        else if (cellIndex%2!=0)
            GetComponent<Image>().color = _lightGrey;
        else if (cellIndex%2!=1)
            GetComponent<Image>().color = _darkGrey;
    }

    public void SetVisible()
    {
        if (!feature.Visible)
        {
            checkboxImg.SetActive(true);
        }
        else
        {
            checkboxImg.SetActive(false);
        }
        feature.Visible = !feature.Visible;
    }

    /// <summary>
    /// Function sets visibility icon to hidden
    /// </summary>
    public void SetVisibilityIconsOff()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Function sets visibility icon to visible
    /// </summary>
    public void SetVisibilityIconsOn()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Function toggles icon on the visibility image shown on the FeatureMenuCell.
    /// </summary>
    public void ToggleVisibilityIcon()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        if(feature.Visible)
            visibleButton.Find("Image_VIS")?.gameObject.SetActive(true);
        else
            visibleButton.Find("Image_HIDE")?.gameObject.SetActive(true);
    }

    /// <summary>
    /// Function toggles the visibility of this feature in the scene.
    /// </summary>
    public void ToggleVisibility()
    {
        feature.Visible = !feature.Visible;
        ToggleVisibilityIcon();
    }

    public void ToggleFlagIndex()
    {
        var flags = Config.Instance.flags;
        var flag = feature.Flag;
        int flagIndex = Array.IndexOf(flags, flag) + 1;
        var newFlag = (flagIndex >= flags.Length) ? " " : flags[flagIndex];
        SetFlag(newFlag);
    }

    /// <summary>
    /// Function sets the flag on the Feature and also changes the label in the menu.
    /// </summary>
    /// <param name="f">The new flag for the feature.</param>
    public void SetFlag(string f)
    {
        feature.Flag = f;
        Debug.Log("Setting flag of feature " + feature.Id.ToString() + " to " + f);
        var flagLabel = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("FlagButton")?.gameObject.transform.Find("FlagLabel").GetComponent<TMP_Text>();
        string lbl = (f.Length > 1) ? f.Substring(0, 2) : (" " + f.Substring(0, 1));
        flagLabel.SetText(lbl);
        featureSetManager.NeedToUpdateInfo = true;
    }

   public void GoTo()
    {
        Teleport(feature.CornerMin, feature.CornerMax);
    }

    public void Select()
    {
        featureSetManager.SelectFeature(feature);
        int siblingCount = this.gameObject.transform.parent.childCount;
        for (int i = 0; i < siblingCount; i++)
        {
            var cell = this.gameObject.transform.parent.GetChild(i).GetComponent<FeatureMenuCell>();
            if (cell.feature.Selected)
                cell.GetComponent<Image>().color = Color.red;
            else if (cell.CellIndex%2!=0)
                cell.GetComponent<Image>().color = _lightGrey;
            else if (cell.CellIndex%2!=1)
                cell.GetComponent<Image>().color = _darkGrey;
        }
        featureSetManager.NeedToUpdateInfo = true;
    }
    
    public void Teleport(Vector3 boundsMin, Vector3 boundsMax)
    {
        float targetSize = 0.3f;
        float targetDistance = 0.5f;
        var activeDataSet = getFirstActiveDataSet();
        if (activeDataSet != null && Camera.main != null)
        {
            var dataSetTransform = activeDataSet.transform;
            var cameraTransform = Camera.main.transform;
            Vector3 boundsMinObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMin);
            Vector3 boundsMaxObjectSpace = activeDataSet.VolumePositionToLocalPosition(boundsMax);
            Vector3 deltaObjectSpace = boundsMaxObjectSpace - boundsMinObjectSpace;
            Vector3 deltaWorldSpace = dataSetTransform.TransformVector(deltaObjectSpace);
            float lengthWorldSpace = deltaWorldSpace.magnitude;
            float scalingRequired = targetSize / lengthWorldSpace;
            dataSetTransform.localScale *= scalingRequired;
            Vector3 cameraPosWorldSpace = cameraTransform.position;
            Vector3 cameraDirWorldSpace = cameraTransform.forward.normalized;
            Vector3 targetPosition = cameraPosWorldSpace + cameraDirWorldSpace * targetDistance;
            Vector3 centerWorldSpace = dataSetTransform.TransformPoint((boundsMaxObjectSpace + boundsMinObjectSpace) / 2.0f);
            Vector3 deltaPosition = targetPosition - centerWorldSpace;
            dataSetTransform.position += deltaPosition;
        }
    }
    
    void Start()
    {
        volumeDatasetRendererObj = GameObject.Find("VolumeDataSetManager");
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
        CellHeight = GetComponent<RectTransform>().rect.height;
    }

    void Update()
    {
        if (feature.StatusChanged)
        {
            feature.StatusChanged = false;
            ToggleVisibilityIcon();
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
}

