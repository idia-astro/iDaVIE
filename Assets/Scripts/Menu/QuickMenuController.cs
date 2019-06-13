using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class QuickMenuController : MonoBehaviour
{

    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject mainMenuCanvas;
    int maskstatus=0;
    int cropstatus = 0;
    int featureStatus = 0;
    // Start is called before the first frame update
    void Start()
    {
       
        if ( volumeDatasetRendererObj!= null )
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

   

    }

    // Update is called once per frame
    void Update()
    {

        if (_dataSets != null)
        {
            //Debug.Log("_dataSets size " + _dataSets.Length);
        }

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            Debug.Log("in foreach --- Update");
            _activeDataSet = firstActive;
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



    public void Exit()
    {
        Application.Quit();
    }

    public void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);
    }

    public void ToggleFeatures()
    {
        if (featureStatus == 1)
            featureStatus = -1;
        featureStatus++;

        this.gameObject.transform.Find("Image_fet_on").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_fet_off").gameObject.SetActive(false);

        if (_activeDataSet)
        {
            switch (featureStatus)
            {
                case 0:
                    this.gameObject.transform.Find("Image_fet_on").gameObject.SetActive(true);
                    notificationText.GetComponent<Text>().text = "Features enabled";
                    _activeDataSet.GetComponentInChildren<FeatureSetManager>().GetComponentsInChildren<FeatureSetRenderer>()[1].ToggleVisibility();
                    break;
                case 1:
                    this.gameObject.transform.Find("Image_fet_off").gameObject.SetActive(true);
                    notificationText.GetComponent<Text>().text = "Features disabled";
                    _activeDataSet.GetComponentInChildren<FeatureSetManager>().GetComponentsInChildren<FeatureSetRenderer>()[1].ToggleVisibility();
                  
                    break;
            }
        }
    }

    public void ToggleMask()
    {
        if (maskstatus == 3)
            maskstatus = -1;
        maskstatus++;


        this.gameObject.transform.Find("Image_nf").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_f1").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_f2").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_f3").gameObject.SetActive(false);

        switch (maskstatus)
        {
            case 0:
                setMask(MaskMode.Disabled);
                notificationText.GetComponent<Text>().text = "Mask disabled";
                this.gameObject.transform.Find("Image_nf").gameObject.SetActive(true);
                break;
            case 1: 
                setMask(MaskMode.Enabled);
                notificationText.GetComponent<Text>().text = "Mask enabled";
                this.gameObject.transform.Find("Image_f1").gameObject.SetActive(true);
                break;
            case 2:
                setMask(MaskMode.Inverted);
                notificationText.GetComponent<Text>().text = "Mask inverted";
                this.gameObject.transform.Find("Image_f2").gameObject.SetActive(true);
                break;
            case 3:
                setMask(MaskMode.Isolated);
                notificationText.GetComponent<Text>().text = "Mask Isolated";
                this.gameObject.transform.Find("Image_f3").gameObject.SetActive(true);
                break;
        }
        
    }


    private void setMask(MaskMode mode)
    {
       
        if (_activeDataSet)
        {
            _activeDataSet.MaskMode = mode;
        }
    }


    public void cropDataSet()
    {

        if (cropstatus == 1)
            cropstatus = -1;
        cropstatus++;

        this.gameObject.transform.Find("Image_dis").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_en").gameObject.SetActive(false);

        if (_activeDataSet)
        {
            switch (cropstatus)
            {
                case 0:
                    this.gameObject.transform.Find("Image_dis").gameObject.SetActive(true);
                    notificationText.GetComponent<Text>().text = "Crop disabled";
                    _activeDataSet.ResetCrop();
                    break;
                case 1:
                    this.gameObject.transform.Find("Image_en").gameObject.SetActive(true);
                    notificationText.GetComponent<Text>().text = "Crop enabled";
                    _activeDataSet.CropToRegion();
                    break;
            }
        }
    }
}
