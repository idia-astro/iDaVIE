﻿using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class PaintMenuController : MonoBehaviour
{

    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject mainMenuCanvas;
    int maskstatus = 0;
    int cropstatus = 0;
    int featureStatus = 0;

    private VolumeInputController _volumeInputController = null;

    // Start is called before the first frame update
    void Start()
    {








    }

    void OnEnable()
    {

        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();

        getFirstActiveDataSet().DisplayMask = true;

        _volumeInputController.SetInteractionState(VolumeInputController.InteractionState.PaintMode);

    }

    // Update is called once per frame
    void Update()
    {
        if (_dataSets != null)
        {

        }

        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            // Debug.Log("in foreach --- Update");
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

    public void ShowOutline()
    {

        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_on").gameObject.SetActive(false);
        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_off").gameObject.SetActive(false);

        if (getFirstActiveDataSet().DisplayMask)
        {
            getFirstActiveDataSet().DisplayMask = false;
            notificationText.GetComponent<Text>().text = "Outline disabled";
            this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_on").gameObject.SetActive(true);

        }
        else
        {
            getFirstActiveDataSet().DisplayMask = true;
            notificationText.GetComponent<Text>().text = "Outline enabled";
            this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_off").gameObject.SetActive(true);
        }
    }

    public void ToggleMask()
    {
        if (maskstatus == 3)
            maskstatus = -1;
        maskstatus++;


        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_nf").gameObject.SetActive(false);
        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f1").gameObject.SetActive(false);
        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f2").gameObject.SetActive(false);
        this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f3").gameObject.SetActive(false);

        switch (maskstatus)
        {
            case 0:
                setMask(MaskMode.Disabled);
                notificationText.GetComponent<Text>().text = "Mask disabled";
                this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_nf").gameObject.SetActive(true);
                break;
            case 1:
                setMask(MaskMode.Enabled);
                notificationText.GetComponent<Text>().text = "Mask enabled";
                this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f1").gameObject.SetActive(true);
                break;
            case 2:
                setMask(MaskMode.Inverted);
                notificationText.GetComponent<Text>().text = "Mask inverted";
                this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f2").gameObject.SetActive(true);
                break;
            case 3:
                setMask(MaskMode.Isolated);
                notificationText.GetComponent<Text>().text = "Mask Isolated";
                this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f3").gameObject.SetActive(true);
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


    public void ExitPaintMode()
    {
        _volumeInputController.SetInteractionState(VolumeInputController.InteractionState.SelectionMode);
        getFirstActiveDataSet().DisplayMask = false;
        this.gameObject.SetActive(false);

    }


    public void BrushSizeIncrease()
    {
        _volumeInputController.IncreaseBrushSize();
        this.gameObject.transform.Find("BottomPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Increase brush size (actual: " + _volumeInputController.BrushSize + ")";

    }

    public void BrushSizeDecrease()
    {

        _volumeInputController.DecreaseBrushSize();
        this.gameObject.transform.Find("BottomPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Decrease brush size (actual: " + _volumeInputController.BrushSize + ")";

    }

    public void BrushSizeReset()
    {
        _volumeInputController.ResetBrushSize();
    }

    public void PaintingAdditive()
    {
        this.gameObject.transform.Find("TopPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Additive Paint Mode";
        _volumeInputController.AdditiveBrush = true; ;
        //_volumeInputController.ResetBrushSize();
    }

    public void PaintingSubtractive()
    {
        this.gameObject.transform.Find("TopPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Subtractive Paint Mode";
        _volumeInputController.AdditiveBrush = false;
        // _volumeInputController.ResetBrushSize();
    }

    public void OpenMainMenu()
    {
        mainMenuCanvas.SetActive(!mainMenuCanvas.activeSelf);


    }

}
