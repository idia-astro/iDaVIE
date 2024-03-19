using UnityEngine;
using UnityEngine.UI;
using PolyAndCode.UI;
using DataFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;
using VolumeData;

//Cell class corresponding to data features for Recyclable Scroll Rect
public class FeatureMenuCell : MonoBehaviour, ICell
{
    //UI
    
    [FormerlySerializedAs("idTextField")] public Text IDTextField = null;
    [FormerlySerializedAs("sourceName")] public Text SourceName = null;
    [FormerlySerializedAs("checkboxImg")] public GameObject CheckboxImg = null;

    public GameObject AddButton;
    public GameObject RemoveButton;
    
    public Feature Feature;
    [FormerlySerializedAs("volumeDatasetRendererObj")]
    public GameObject VolumeDatasetRendererObj;
    
    public int CellIndex {get; private set;} 
    public float CellHeight {get; private set;}

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;
    private FeatureSetManager _featureSetManager;
    

    private static readonly Color _lightGrey = new Color(0.4039216f, 0.5333334f, 0.5882353f, 1f);
    private static readonly Color _darkGrey = new Color(0.2384301f, 0.3231786f, 0.3584906f, 1f);
    
    //Model
    private FeatureMenuListItemInfo _featureMenuListItemInfo;
    

    void Start()
    {
        VolumeDatasetRendererObj = GameObject.Find("VolumeDataSetManager");
        if (VolumeDatasetRendererObj != null)
            _dataSets = VolumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }
        if (_activeDataSet != null)
        {
            _featureSetManager = _activeDataSet.GetComponentInChildren<FeatureSetManager>();
        }
        CellHeight = GetComponent<RectTransform>().rect.height;
    }

    void Update()
    {
        if (Feature.StatusChanged)
        {
            Feature.StatusChanged = false;
            ToggleVisibilityIcon();
        }
    }
    
    //This is called from the SetCell method in DataSource
    public void ConfigureCell(FeatureMenuListItemInfo featureMenuListItemInfo, int cellIndex)
    {
        CellIndex = cellIndex;
        _featureMenuListItemInfo = featureMenuListItemInfo;
        IDTextField.text = featureMenuListItemInfo.IdTextField;
        SourceName.text = featureMenuListItemInfo.SourceName;
        Feature = featureMenuListItemInfo.Feature;
        if (Feature.Flag == null)
            SetFlag("");
        else
            SetFlag(Feature.Flag);
        if (Feature.Visible)
            SetVisibilityIconsOn();
        else
            SetVisibilityIconsOff();
        if (featureMenuListItemInfo.Feature.Selected)
            GetComponent<Image>().color = Color.red;
        else if (cellIndex%2!=0)
            GetComponent<Image>().color = _lightGrey;
        else if (cellIndex%2!=1)
            GetComponent<Image>().color = _darkGrey;
        // If feature menu cell corresponds to a NewList feature, replace Add button with Remove button
        if (Feature.FeatureSetParent.FeatureSetType == FeatureSetType.New)
        {
            AddButton.SetActive(false);
            RemoveButton.SetActive(true);
        }
        else
        {
            AddButton.SetActive(true);
            RemoveButton.SetActive(false);
        }
    }

    public void SetVisible()
    {
        if (!Feature.Visible)
        {
            CheckboxImg.SetActive(true);
        }
        else
        {
            CheckboxImg.SetActive(false);
        }
        Feature.Visible = !Feature.Visible;
    }

    /// <summary>
    /// Function sets visibility icon to hidden and changes the color of the button to grey.
    /// </summary>
    public void SetVisibilityIconsOff()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(true);
        visibleButton.GetComponent<Image>().color = Color.grey;
    }

    /// <summary>
    /// Function sets visibility icon to visible and sets the color to white
    /// </summary>
    public void SetVisibilityIconsOn()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(true);
        visibleButton.GetComponent<Image>().color = Color.white;
    }

    /// <summary>
    /// Function toggles icon on the visibility image shown on the FeatureMenuCell.
    /// </summary>
    public void ToggleVisibilityIcon()
    {
        var visibleButton = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("Mask")?.gameObject.transform;
        visibleButton.Find("Image_VIS")?.gameObject.SetActive(false);
        visibleButton.Find("Image_HIDE")?.gameObject.SetActive(false);
        if (Feature.Visible)
        {
            visibleButton.Find("Image_VIS")?.gameObject.SetActive(true);
            visibleButton.GetComponent<Image>().color = Color.white;
        }
        else
        {
            visibleButton.Find("Image_HIDE")?.gameObject.SetActive(true);
            visibleButton.GetComponent<Image>().color = Color.grey;

        }
    }

    /// <summary>
    /// Function toggles the visibility of this feature in the scene.
    /// </summary>
    public void ToggleVisibility()
    {
        Feature.Visible = !Feature.Visible;
        ToggleVisibilityIcon();
    }

    public void ToggleFlagIndex()
    {
        _activeDataSet.FileChanged = true;
        var flags = Config.Instance.flags;
        var flag = Feature.Flag;
        int flagIndex = 0;
        if (!flag.Equals(""))
            flagIndex = Array.IndexOf(flags, flag) + 1;
        var newFlag = (flagIndex >= flags.Length) ? " " : flags[flagIndex];
        SetFlag(newFlag);
    }

    /// <summary>
    /// Function sets the flag on the Feature and also changes the label in the menu.
    /// </summary>
    /// <param name="f">The new flag for the feature.</param>
    public void SetFlag(string f)
    {
        if (f == null)
            f = "";
        Feature.Flag = f;
        var flagLabel = this.gameObject.transform.Find("GameObject")?.gameObject.transform.Find("FlagButton")?.gameObject.transform.Find("FlagLabel").GetComponent<TMP_Text>();
        string lbl;
        if (f.Equals(""))
            lbl = " ";
        else
            lbl = (f.Length > 1) ? f.Substring(0, 2) : (" " + f.Substring(0, 1));
        flagLabel.SetText(lbl);
        if (_featureSetManager != null)
            _featureSetManager.NeedToUpdateInfo = true;
    }

   public void GoTo()
    {
        Teleport(Feature.CornerMin, Feature.CornerMax);
    }

    public void Select()
    {
        _featureSetManager.SelectFeature(Feature);
        int siblingCount = this.gameObject.transform.parent.childCount;
        for (int i = 0; i < siblingCount; i++)
        {
            var cell = this.gameObject.transform.parent.GetChild(i).GetComponent<FeatureMenuCell>();
            if (cell.Feature.Selected)
                cell.GetComponent<Image>().color = Color.red;
            else if (cell.CellIndex%2!=0)
                cell.GetComponent<Image>().color = _lightGrey;
            else if (cell.CellIndex%2!=1)
                cell.GetComponent<Image>().color = _darkGrey;
        }
        _featureSetManager.NeedToUpdateInfo = true;
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

    public void AddToNewList()
    {
        _featureSetManager.AddFeatureToNewSet(Feature);
    }

    public void RemoveFromList()
    {
        if (Feature.Selected)
        {
            _featureSetManager.DeselectFeature();
        }
        Feature.FeatureSetParent.RemoveFeature(Feature);
        _featureSetManager.NeedToRespawnMenuList = true;
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

