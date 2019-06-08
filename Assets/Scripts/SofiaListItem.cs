using DataFeatures;
using System.Collections;
using System.Collections.Generic;
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

    public Feature feature;


    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

  


    public GameObject volumeDatasetRendererObj = null;


    private FeatureSetManager featureSetManager;
    private FeatureSetRenderer featureSetRenderer;


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
