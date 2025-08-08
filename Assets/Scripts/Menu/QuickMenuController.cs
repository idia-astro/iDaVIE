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
using System.Diagnostics;
using DataFeatures;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VolumeData;

public class QuickMenuController : MonoBehaviour
{
    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject sourcesMenu;
    public GameObject paintMenu;
    public GameObject plotsMenu;
    public GameObject voiceCommandsListCanvas;
    public GameObject settingsMenu;


    public GameObject colorMapListCanvas;
    public GameObject savePopup;
    public GameObject exitPopup;
    public GameObject exitSavePopup;
    public GameObject exportPopup;

    public GameObject userConfirmPopupPrefab;

    int maskstatus = 0;
    int cropstatus = 0;
    int featureStatus = 0;
    string oldMaskLoaded = "";

    private VolumeInputController _volumeInputController = null;

    // Start is called before the first frame update
    void Start()
    {
        if (volumeDatasetRendererObj != null)
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

    /// <summary>
    /// Function that gets called if a function related to the mask is called without a mask that is loaded.
    /// </summary>
    private void throwMissingMaskError()
    {
        ToastNotification.ShowError("No mask loaded for this functionality!");
    }

    public void Exit()
    {
        if (_activeDataSet.FileChanged)
        {
            exitSavePopup.GetComponent<ExitController>()._volumeInputController = _volumeInputController;
            exitSavePopup.GetComponent<ExitController>()._activeDataSet = _activeDataSet;

            exitSavePopup.transform.SetParent(this.transform.parent, false);
            exitSavePopup.transform.localPosition = this.transform.localPosition;
            exitSavePopup.transform.localRotation = this.transform.localRotation;
            exitSavePopup.transform.localScale = this.transform.localScale;

            gameObject.SetActive(false);
            exitSavePopup.SetActive(true);
        }
        else
        {
            exitPopup.transform.SetParent(this.transform.parent, false);
            exitPopup.transform.localPosition = this.transform.localPosition;
            exitPopup.transform.localRotation = this.transform.localRotation;
            exitPopup.transform.localScale = this.transform.localScale;

            gameObject.SetActive(false);
            exitPopup.SetActive(true);
        }
    }

    public void OpenSourcesMenu()
    {
        spawnMenu(sourcesMenu);
    }

    public void OpenSettingsMenu()
    {
        spawnMenu(settingsMenu);
    }
    
    public void OpenListOfVoiceCommands()
    {
        spawnMenu(voiceCommandsListCanvas);
    }

    /// <summary>
    /// Function that is called when user selects the colour map button on the main menu
    /// </summary>
    public void OpenListOfColourMaps()
    {
        spawnMenu(colorMapListCanvas);
    }

    public void spawnMenu(GameObject menu)
    {
        Vector3 playerPos = Camera.main.transform.position;
        Vector3 playerDirection = Camera.main.transform.forward;
        Quaternion playerRotation = Camera.main.transform.rotation;
        float spawnDistance = 1.5f;

        Vector3 spawnPos = playerPos + playerDirection * spawnDistance;

        menu.transform.position = spawnPos;
        menu.transform.rotation = Quaternion.LookRotation(new Vector3(spawnPos.x - playerPos.x, 0, spawnPos.z - playerPos.z));

        if (!menu.activeSelf)
        {
            menu.SetActive(true);
        }
    }

    //TODO: Make this compatible with the new feature list system. Might be best to remove.
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
                    _activeDataSet.GetComponentInChildren<FeatureSetManager>()?.GetComponentsInChildren<FeatureSetRenderer>()?[1]?.ToggleVisibility();
                    break;
                case 1:
                    this.gameObject.transform.Find("Image_fet_off").gameObject.SetActive(true);
                    notificationText.GetComponent<Text>().text = "Features disabled";
                    _activeDataSet.GetComponentInChildren<FeatureSetManager>()?.GetComponentsInChildren<FeatureSetRenderer>()?[1]?.ToggleVisibility();
                    break;
            }
        }
    }

    public void ToggleMask()
    {
        if (_activeDataSet.Mask == null)
        {
            throwMissingMaskError();
            return;
        }

        if (maskstatus == 3)
            maskstatus = -1;
        maskstatus++;

        var Image_nf = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_nf").gameObject;
        var Image_f1 = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f1").gameObject;
        var Image_f2 = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f2").gameObject;
        var Image_f3 = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Mask").gameObject.transform.Find("Image_f3").gameObject;

        Image_nf.SetActive(false);
        Image_f1.SetActive(false);
        Image_f2.SetActive(false);
        Image_f3.SetActive(false);

        UnityEngine.Debug.Log("Toggling mask to maskstatus of " + maskstatus + ".");

        switch (maskstatus)
        {
            case 0:
                setMask(MaskMode.Disabled);
                notificationText.GetComponent<Text>().text = "Mask disabled";
                Image_nf.SetActive(true);
                break;
            case 1:
                setMask(MaskMode.Enabled);
                notificationText.GetComponent<Text>().text = "Mask enabled";
                Image_f1.SetActive(true);
                break;
            case 2:
                setMask(MaskMode.Inverted);
                notificationText.GetComponent<Text>().text = "Mask inverted";
                Image_f2.SetActive(true);
                break;
            case 3:
                setMask(MaskMode.Isolated);
                notificationText.GetComponent<Text>().text = "Mask Isolated";
                Image_f3.SetActive(true);
                break;
        }
    }

    private void setMask(MaskMode mode)
    {
        if (_activeDataSet.Mask == null)
        {
            throwMissingMaskError();
            return;
        }

        if (_activeDataSet)
        {
            _activeDataSet.MaskMode = mode;
        }
    }

    public void cropDataSet()
    {
        var imageDis = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Crop").gameObject.transform.Find("Image_dis").gameObject;
        var imageEn = gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Crop").gameObject.transform.Find("Image_en").gameObject;
        imageDis.SetActive(false);
        imageEn.SetActive(false);
        if (_activeDataSet)
        {
            if (_activeDataSet.IsCropped)
            {
                imageDis.SetActive(true);
                notificationText.GetComponent<Text>().text = "Crop disabled";
                _activeDataSet.ResetCrop();
            }
            else
            {
                imageEn.SetActive(true);
                notificationText.GetComponent<Text>().text = "Crop enabled";
                _activeDataSet.CropToFeature();
            }
        }
    }

    public void OpenPaintMenu()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Update();
        }

        // Prevent painting of downsampled data
        if (!_activeDataSet.IsFullResolution)
        {
            ToastNotification.ShowError("Cannot paint downsampled mask!\nPlease select a smaller region");
        }
        else {
            
            paintMenu.transform.SetParent(this.transform.parent, false);
            paintMenu.transform.localPosition = this.transform.localPosition;
            paintMenu.transform.localRotation = this.transform.localRotation;
            paintMenu.transform.localScale = this.transform.localScale;

            gameObject.SetActive(false);
            paintMenu.SetActive(true);
        }
    }

    public void OpenPlotsMenu()
    {
        spawnMenu(plotsMenu);
    }

    public void SaveMask()
    {
        if (_activeDataSet.Mask == null)
        {
            throwMissingMaskError();
            return;
        }

        if (exportPopup.activeSelf)
            exportPopup.SetActive(false);
        
        // savePopup.transform.SetParent(this.transform.parent, false);
        // savePopup.transform.localPosition = this.transform.localPosition;
        // savePopup.transform.localRotation = this.transform.localRotation;
        // savePopup.transform.localScale = this.transform.localScale;

        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.RemoveAllListeners();
        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.RemoveAllListeners();
        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.RemoveAllListeners();

        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.AddListener(SaveCancel);
        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.AddListener(SaveOverwriteMask);
        // savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.AddListener(SaveNewMask);

        var newSavePopup = Instantiate(userConfirmPopupPrefab, this.transform.parent);
        newSavePopup.transform.localPosition = this.transform.localPosition;
        newSavePopup.transform.localRotation = this.transform.localRotation;
        newSavePopup.transform.localScale = this.transform.localScale;

        var control = newSavePopup.GetComponent<UserConfirmationPopupController>();
        control.setMessageBody("");
        control.setHeaderText("Save mask");
        control.addButton("New file", "Save the current mask as a new file", this.SaveNewMask);
        control.addButton("Overwrite", "Overwrite the existing mask file", this.SaveOverwriteMask);
        control.addButton("Cancel", "Cancel the save and return to painting", this.SaveCancel);

        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.Painting )
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
        
        this.gameObject.SetActive(false);
        newSavePopup.SetActive(true);
    }

    public void ExportData()
    {
        var newExportPopup = Instantiate(userConfirmPopupPrefab, this.transform.parent);
        newExportPopup.transform.localPosition = this.transform.localPosition;
        newExportPopup.transform.localRotation = this.transform.localRotation;
        newExportPopup.transform.localScale = this.transform.localScale;

        var control = newExportPopup.GetComponent<UserConfirmationPopupController>();
        control.setMessageBody("");
        control.setHeaderText("Export data");
        control.addButton("Mask", "Save mask", this.SaveMask);
        control.addButton("Subcube", "Export selection as a subcube", this.SaveSubCube);
        control.addButton("Cancel", "Cancel the export and return", this.ExportCancel);

        this.gameObject.SetActive(false);
        newExportPopup.SetActive(true);
    }

    public void ExportCancel()
    {
        exportPopup.SetActive(false);
    }

    public void SaveSubCube()
    {
        _activeDataSet.SaveSubCube();
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        ExportCancel();
    }

    public void SaveCancel()
    {
        savePopup.SetActive(false);
    }

    public void SaveOverwriteMask()
    {
        if (_activeDataSet.Mask == null)
        {
            throwMissingMaskError();
            return;
        }

        _activeDataSet.SaveMask(true);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }

    public void SaveNewMask()
    {
        if (_activeDataSet.Mask == null)
        {
            throwMissingMaskError();
            return;
        }
        
        _activeDataSet.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }

    /// <summary>
    /// Function to test out the generic confirmation popup
    /// </summary>
    public void testConfirmPopup()
    {
        var popup = Instantiate(userConfirmPopupPrefab, this.transform.parent);
        popup.transform.localPosition = this.transform.localPosition;
        popup.transform.localRotation = this.transform.localRotation;
        popup.transform.localScale = this.transform.localScale;

        var control = popup.GetComponent<UserConfirmationPopupController>();
        control.setMessageBody("This a test case of the generic user choice/confirmation popup.");
        control.setHeaderText("Testing user confirmation popup");
        control.addButton("Test1", "Should show a warning toast notification", this.testFunc1);
        control.addButton("Test2", "Should show an error toast notification", this.testFunc2);
        control.addButton("Test3", "Should show a success toast notification", this.testFunc3);
        gameObject.SetActive(false);
        popup.SetActive(true);
    }

    public void testFunc1()
    {
        ToastNotification.ShowWarning("testFunc1 has been called!");
    }

    public void testFunc2()
    {
        ToastNotification.ShowError("testFunc2 has been called!");
    }
    
    public void testFunc3()
    {
        ToastNotification.ShowSuccess("testFunc3 has been called!");
    }
}