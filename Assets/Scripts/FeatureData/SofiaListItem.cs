using DataFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class SofiaListItem : MonoBehaviour
{

    public Text idTextField = null;
    public Text sourceName = null;
    public GameObject checkboxImg = null;
    public GameObject SofiaNewListPrefab = null;
    public GameObject SofiaNewListController = null;
    private bool isVisible = true;

    public int ParentListIndex {get; set;}

    public Feature feature;


    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

  


    public GameObject volumeDatasetRendererObj = null;


    private FeatureSetManager featureSetManager;
    private FeatureSetRenderer featureSetRenderer;

    public GameObject InfoWindow;


    private int visibilityStatus=0;

    public void SetVisible()
    {
        
        if (!isVisible)
        {
            checkboxImg.SetActive(true);
        }
        else
        {
            checkboxImg.SetActive(false);
        }
        isVisible = !isVisible;
    }

    public void AddToNewList()
    {
        SofiaNewListController.GetComponent<SofiaNewListController>().CreateAndAddNewElement();
    }



    public void ToggleVisibilityIcon()
    {

        if (visibilityStatus == 1)
            visibilityStatus = -1;
        visibilityStatus++;

        this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_VIS").gameObject.SetActive(false);
        this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_HIDE").gameObject.SetActive(false);

        if(feature.Visible)
            this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_VIS").gameObject.SetActive(true);
        else
            this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_HIDE").gameObject.SetActive(true);


    }

    public void ToggleVisibility()
    {
        
        if (visibilityStatus == 1)
            visibilityStatus = -1;
        visibilityStatus++;


        this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_VIS").gameObject.SetActive(false);
        this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_HIDE").gameObject.SetActive(false);
        

        switch (visibilityStatus)
        {
            case 0:
                //setMask(MaskMode.Disabled);
                this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_VIS").gameObject.SetActive(true);
           
                
                feature.Visible = true;
                break;
            case 1:
                //  setMask(MaskMode.Enabled);

                this.gameObject.transform.Find("GameObject").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_HIDE").gameObject.SetActive(true);
             
                feature.Visible = false;
                break;
         
        }

        
        
    }

    public void GoTo()
    {
        // VolumeInputController.Teleport(feature.CornerMin,feature.CornerMax);
        Teleport(feature.CornerMin, feature.CornerMax);
        // Teleport(Vector3 boundsMin, Vector3 boundsMax);

    }

    public void UpdateInfo()
    {
        var textObject = InfoWindow.transform.Find("PanelContents").gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport")
            .gameObject.transform.Find("Content").gameObject.transform.Find("SourceInfoText").gameObject;
        textObject.GetComponent<TMP_Text>().text = "";
        textObject.GetComponent<TMP_Text>().text += $"Source # : {feature.Index + 1}{Environment.NewLine}";  
        if (_activeDataSet.HasWCS)
        {
            double ra, dec, physz, normR, normD, normZ;
            AstTool.Transform3D(_activeDataSet.AstFrame, feature.Center.x, feature.Center.y, feature.Center.z, 1, out ra, out dec, out physz);
            AstTool.Norm(_activeDataSet.AstFrame, ra, dec, physz, out normR, out normD, out normZ);
            textObject.GetComponent<TMP_Text>().text += $"RA: {(180f * normR / Math.PI).ToString()}{Environment.NewLine}";
            textObject.GetComponent<TMP_Text>().text += $"Dec: {(180f * normD / Math.PI).ToString()}{Environment.NewLine}";
            textObject.GetComponent<TMP_Text>().text += $"{_activeDataSet.Data.GetAstAttribute("System(3)")}: {normZ.ToString()}{Environment.NewLine}";
        }
        for (int i = 0; i < feature.FeatureSetParent.RawDataKeys.Length; i++)
        {
            textObject.GetComponent<TMP_Text>().text += $"{feature.FeatureSetParent.RawDataKeys[i]} : {feature.RawData[i]}{Environment.NewLine}";
        }
    }

    public void ShowInfo()
    {
        InfoWindow.SetActive(true);
        UpdateInfo();
    }

    public void Select()
    {
        featureSetManager.SelectedFeature = feature;
        var _sofiaList = GameObject.Find("RenderMenu");
        if (_sofiaList != null)
        {
            int sourceIndex = featureSetManager.SelectedFeature.Index;
            var scrollView = _sofiaList.gameObject.transform.Find("PanelContents").gameObject.transform.Find("SofiaListPanel").gameObject.transform.Find("Scroll View").gameObject;
            int sourceListIndex = featureSetManager.SelectedFeature.LinkedListItem.GetComponent<SofiaListItem>().ParentListIndex;
            if (scrollView.GetComponent<SofiaListCreator>().CurrentFeatureSetIndex != sourceListIndex)
                scrollView.GetComponent<SofiaListCreator>().DisplaySet(sourceListIndex);
            scrollView.GetComponent<CustomDragHandler>().FocusOnFeature(feature, false);
            var infoWindow = transform.root.Find("SourceInfoWindow").gameObject;
            _activeDataSet.SetRegionBounds(Vector3Int.FloorToInt(featureSetManager.SelectedFeature.GetMinBounds()), Vector3Int.FloorToInt(featureSetManager.SelectedFeature.GetMaxBounds()), false);
            if(infoWindow.activeSelf)
                ShowInfo();

        }
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
    

      // Start is called before the first frame update
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
           // featureSetManager.ImportFeatureSet();
        }

    }

      // Update is called once per frame
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
