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
    public GameObject paintMenu;
    public GameObject histogramMenu;

    int maskstatus=0;
    int cropstatus = 0;
    int featureStatus = 0;
    string oldMaskLoaded = "";
    // Start is called before the first frame update
    void Start()
    {
       
        if ( volumeDatasetRendererObj!= null )
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

    }

    public void OnEnable()
    {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);
    }

    // Update is called once per frame
    void Update()
    {
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
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

        this.gameObject.transform.Find("Image_dis").gameObject.SetActive(false);
        this.gameObject.transform.Find("Image_en").gameObject.SetActive(false);

        if (_activeDataSet)
        {

            if (_activeDataSet.IsCropped)
            {
                this.gameObject.transform.Find("Image_dis").gameObject.SetActive(true);
                notificationText.GetComponent<Text>().text = "Crop disabled";
                _activeDataSet.ResetCrop();
            }
            else
            {
                this.gameObject.transform.Find("Image_en").gameObject.SetActive(true);
                notificationText.GetComponent<Text>().text = "Crop enabled";
                _activeDataSet.CropToRegion();
            }
        }
    }

    public void OpenPaintMenu()
    {
        // Prevent painting of downsampled data
        if (!_activeDataSet.IsFullResolution)
        {
            notificationText.GetComponent<Text>().text = "Cannot paint downsampled region";
            return;
        }
        paintMenu.transform.SetParent(this.transform.parent,false);
        paintMenu.transform.localPosition = this.transform.localPosition;
        paintMenu.transform.localRotation = this.transform.localRotation;
        paintMenu.transform.localScale = this.transform.localScale;
      
        gameObject.SetActive(false);
        paintMenu.SetActive(true);
    }

    public void OpenHistogramMenu()
    {
        /*
        histogramMenu.transform.SetParent(this.transform.parent, false);
        histogramMenu.transform.localPosition = this.transform.localPosition;
        histogramMenu.transform.localRotation = this.transform.localRotation;
        histogramMenu.transform.localScale = this.transform.localScale;

        this.gameObject.SetActive(false);
        histogramMenu.SetActive(true);
        */
        histogramMenu.SetActive(!histogramMenu.activeSelf);
    }
}
