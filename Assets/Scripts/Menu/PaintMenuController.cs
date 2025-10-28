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
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class PaintMenuController : MonoBehaviour
{
    public GameObject volumeDatasetRendererObj = null;
    public GameObject notificationText = null;

    public GameObject userConfirmPopupPrefab;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject sourcesMenu;
    public GameObject plotsMenu;
    public GameObject paintMenu;
    public GameObject shapeMenu; // Menu for shape selection
    public GameObject savePopup;
    int maskstatus = 0;
    int cropstatus = 0;
    int featureStatus = 0;

    private VolumeInputController _volumeInputController = null;
    public VolumeInputController VolumeInputController => _volumeInputController;

    private Text _topPanelText;
    private Button _exitButton;
    private GameObject _shapeSelectionButton;
    private string oldSaveText = "";

    void OnEnable()
    {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();
        
        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeEnabled);

        _topPanelText = gameObject.transform.Find("TopPanel").gameObject.transform.Find("Text").GetComponent<Text>();
        _exitButton = gameObject.transform.Find("Content/SecondRow/ExitButton")?.GetComponent<Button>();
        _shapeSelectionButton = gameObject.transform.Find("Content/SecondRow/ShapeMenu").gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }

        if (!_volumeInputController.AdditiveBrush)
        {
            _topPanelText.text = "Erase Mode";
        }
        else if (_volumeInputController.SourceId <= 0)
        {
            _topPanelText.text = "Please select a Source ID to paint";
            _shapeSelectionButton.GetComponent<Button>().enabled = false;
            _shapeSelectionButton.GetComponent<Image>().color = Color.gray;
        }
        else
        {
            _topPanelText.text = $"Paint Mode (Source ID {_volumeInputController.SourceId})";
            _shapeSelectionButton.GetComponent<Button>().enabled = true;
            _shapeSelectionButton.GetComponent<Image>().color = Color.white;
        }

    }
    
    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in _dataSets)
        {
            if (dataSet && dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;
    }

    /// <summary>
    /// Function that is called to toggle the mask outline.
    /// </summary>
    public void ShowOutline()
    {

        var maskOn = this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_on");
        var maskOff = this.gameObject.transform.Find("Content").gameObject.transform.Find("SecondRow").gameObject.transform.Find("Outline").gameObject.transform.Find("Image_out_off");
        maskOn.gameObject.SetActive(false);
        maskOff.gameObject.SetActive(false);

        if (getFirstActiveDataSet().DisplayMask)
        {
            getFirstActiveDataSet().DisplayMask = false;
            notificationText.GetComponent<Text>().text = "Outline disabled";
            maskOff.gameObject.SetActive(true);

        }
        else
        {
            getFirstActiveDataSet().DisplayMask = true;
            notificationText.GetComponent<Text>().text = "Outline enabled";
            maskOn.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Function that is called when the user cycles through the mask modes using the menu button.
    /// </summary>
    public void ToggleMask()
    {
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

        Debug.Log("Toggling mask to maskstatus of " + maskstatus + ".");

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
                notificationText.GetComponent<Text>().text = "Mask isolated";
                Image_f3.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Function that is called when the user sets the mask mode through voice commands.
    /// </summary>
    /// <param name="mode">The mask mode to switch to.</param>
    private void setMask(MaskMode mode)
    {
        if (_activeDataSet)
        {
            _activeDataSet.MaskMode = mode;
        }
    }

    public void ExitPaintMode()
    {
        if(_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        
        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
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

    public void UndoBrushStroke()
    {
        _volumeInputController.UndoBrushStroke(_volumeInputController.PrimaryHand);
    }

    public void RedoBrushStroke()
    {
        _volumeInputController.RedoBrushStroke(_volumeInputController.PrimaryHand);
    }


    public void BrushSizeReset()
    {
        _volumeInputController.ResetBrushSize();
    }


    public void PaintingAdditive()
    {
        _volumeInputController.SetBrushAdditive();
    }

    public void PaintingSubtractive()
    {
        _volumeInputController.SetBrushSubtractive();
    }

    public void OpenSourcesMenu()
    {
        spawnMenu(sourcesMenu);
    }
    
    public void OpenPlotsWindow()
    {
        spawnMenu(plotsMenu);
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

    /// <summary>
    /// Function used to generate popup to save or overwrite existing mask data.
    /// </summary>
    public void SaveMask()
    {
        if (_activeDataSet.IsMaskNew)
        {
            SaveNewMask();
        }
        else
        {
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

            if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.Painting)
            {
                _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
            }
            gameObject.SetActive(false);
            newSavePopup.SetActive(true);
        }
    }

    /// <summary>
    /// Function called when SaveMask()'s popup returns a cancel command.
    /// </summary>
    public void SaveCancel()
    {
        gameObject.SetActive(true);
        // savePopup.SetActive(false);
    }


    /// <summary>
    /// Function called when SaveMask()'s popup returns an overwrite command.
    /// </summary>
    public void SaveOverwriteMask()
    {
        _activeDataSet?.SaveMask(true);

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }


    /// <summary>
    /// Function called when SaveMask()'s popup returns a new mask command.
    /// </summary>
    public void SaveNewMask()
    {
        _activeDataSet?.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }

    public void AddNewSource()
    {
        _volumeInputController.AddNewSource();
    }

    /// <summary>
    /// Function that is called when the user presses the button to select a source ID.
    /// Forwards input to the input controller for state management.
    /// </summary>
    public void EditSourceId()
    {
        _volumeInputController.StartEditSourceID();
    }

    public void OpenShapeMenu()
    {
        shapeMenu.transform.SetParent(this.transform.parent, false);
        shapeMenu.transform.localPosition = this.transform.localPosition;
        shapeMenu.transform.localRotation = this.transform.localRotation;
        shapeMenu.transform.localScale = this.transform.localScale;

        if(_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        
        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
        _volumeInputController.ChangeShapeSelection();
        PaintingAdditive();
        gameObject.SetActive(false);
        shapeMenu.SetActive(true);
        
    }

}
