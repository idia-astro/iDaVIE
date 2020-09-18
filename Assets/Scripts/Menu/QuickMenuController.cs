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
    public GameObject voiceCommandsListCanvas;
    public GameObject savePopup;
    public GameObject ExitPopup;
    public GameObject ExitSavePopup;


    int maskstatus=0;
    int cropstatus = 0;
    int featureStatus = 0;
    string oldMaskLoaded = "";

    private VolumeInputController _volumeInputController = null;


    public float VibrationDuration = 0.25f;
    public float VibrationFrequency = 100.0f;
    public float VibrationAmplitude = 1.0f;


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

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();
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

    public VolumeInputController getVolumeInputController()
    {
        return _volumeInputController;
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

        if (_activeDataSet.FileChanged)
        {
            ExitSavePopup.GetComponent<ExitController>()._volumeInputController = _volumeInputController;
            ExitSavePopup.GetComponent<ExitController>()._activeDataSet = _activeDataSet;
            ExitSavePopup.GetComponent<ExitController>().VibrationAmplitude = VibrationAmplitude;
            ExitSavePopup.GetComponent<ExitController>().VibrationDuration = VibrationDuration;
            ExitSavePopup.GetComponent<ExitController>().VibrationFrequency = VibrationFrequency;

            ExitSavePopup.transform.SetParent(this.transform.parent, false);
            ExitSavePopup.transform.localPosition = this.transform.localPosition;
            ExitSavePopup.transform.localRotation = this.transform.localRotation;
            ExitSavePopup.transform.localScale = this.transform.localScale;

            gameObject.SetActive(false);
            ExitSavePopup.SetActive(true);
        }
        else
        {
            ExitPopup.transform.SetParent(this.transform.parent, false);
            ExitPopup.transform.localPosition = this.transform.localPosition;
            ExitPopup.transform.localRotation = this.transform.localRotation;
            ExitPopup.transform.localScale = this.transform.localScale;

            gameObject.SetActive(false);
            ExitPopup.SetActive(true);
        }
    }

    public void OpenMainMenu()
    {

        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);
    }

    public void OpenListOfVoiceCommands()
    {

        voiceCommandsListCanvas.SetActive(!voiceCommandsListCanvas.activeSelf);
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
        histogramMenu.SetActive(!histogramMenu.activeSelf);
    }


    public void SaveMask()
    {
        savePopup.transform.SetParent(this.transform.parent, false);
        savePopup.transform.localPosition = this.transform.localPosition;
        savePopup.transform.localRotation = this.transform.localRotation;
        savePopup.transform.localScale = this.transform.localScale;


        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.RemoveAllListeners();

        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.AddListener(SaveCancel);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.AddListener(SaveOverwriteMask);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.AddListener(SaveNewMask);
        
        _volumeInputController.SetInteractionState(VolumeInputController.InteractionState.SelectionMode);
        this.gameObject.SetActive(false);
        savePopup.SetActive(true);

    }

    public void SaveCancel()
    {

        savePopup.SetActive(false);
    }

    public void SaveOverwriteMask()
    {

        _activeDataSet.SaveMask(true);

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);
        SaveCancel();
    }

    public void SaveNewMask()
    {
        _activeDataSet.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand, VibrationDuration, VibrationFrequency, VibrationAmplitude);
        SaveCancel();
    }


}
