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
using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

/// <summary>
/// Controls the UI and input for the paint mode.
/// Manages brush size, mask modes, painting behavior, and menu navigation.
/// </summary>

public class PaintMenuController : MonoBehaviour
{
    /// <summary>
    /// Reference to the GameObject containing VolumeDataSetRenderer components.
    /// </summary>
    public GameObject volumeDatasetRendererObj = null;
    /// <summary>
    /// Text UI element for displaying user notifications.
    /// </summary>
    public GameObject notificationText = null;

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    /// <summary>
    /// Reference to the sources menu UI.
    /// </summary>
    public GameObject sourcesMenu;
    /// <summary>
    /// Reference to the plots menu UI.
    /// </summary>
    public GameObject plotsMenu;
    /// <summary>
    /// Reference to the paint menu UI.
    /// </summary>
    public GameObject paintMenu;
    /// <summary>
    /// Reference to the shape selection menu UI.
    /// </summary>
    public GameObject shapeMenu;
    /// <summary>
    /// Reference to the save mask popup UI.
    /// </summary>
    public GameObject savePopup;
    int maskstatus = 0;
    int cropstatus = 0;
    int featureStatus = 0;

    private VolumeInputController _volumeInputController = null;

    /// <summary>
    /// Gets the VolumeInputController instance used for brush input and state management.
    /// </summary>
    public VolumeInputController VolumeInputController => _volumeInputController;

    private Text _topPanelText;
    private Button _exitButton;
    private GameObject _shapeSelectionButton;
    private string oldSaveText = "";

    /// <summary>
    /// Unity event called when the object becomes enabled and active.
    /// Initializes dataset references and sets paint mode state.
    /// </summary>
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

    /// <summary>
    /// Unity update loop to update UI based on brush mode and dataset activity.
    /// </summary>
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

    /// <summary>
    /// Returns the first active and enabled VolumeDataSetRenderer in the scene.
    /// </summary>
    /// <returns>The first active VolumeDataSetRenderer or null if none found.</returns>
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
    /// Toggles the visibility of the outline (mask overlay) for the current dataset.
    /// </summary>
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

    /// <summary>
    /// Cycles through different mask modes: Disabled, Enabled, Inverted, and Isolated.
    /// Updates the mask UI accordingly.
    /// </summary>
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

    /// <summary>
    /// Sets the mask mode on the currently active dataset.
    /// </summary>
    /// <param name="mode">The mask mode to apply.</param>
    private void setMask(MaskMode mode)
    {
        if (_activeDataSet)
        {
            _activeDataSet.MaskMode = mode;
        }
    }

    /// <summary>
    /// Exits paint mode and disables this controller.
    /// </summary>
    public void ExitPaintMode()
    {
        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);

        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Increases the brush size and updates the bottom panel UI text.
    /// </summary>
    public void BrushSizeIncrease()
    {
        _volumeInputController.IncreaseBrushSize();
        this.gameObject.transform.Find("BottomPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Increase brush size (actual: " + _volumeInputController.BrushSize + ")";
    }

    /// <summary>
    /// Decreases the brush size and updates the bottom panel UI text.
    /// </summary>
    public void BrushSizeDecrease()
    {
        _volumeInputController.DecreaseBrushSize();
        this.gameObject.transform.Find("BottomPanel").gameObject.transform.Find("Text").GetComponent<Text>().text = "Decrease brush size (actual: " + _volumeInputController.BrushSize + ")";
    }

    /// <summary>
    /// Undoes the last brush stroke using the primary hand.
    /// </summary>
    public void UndoBrushStroke()
    {
        _volumeInputController.UndoBrushStroke(_volumeInputController.PrimaryHand);
    }

    /// <summary>
    /// Redoes the last undone brush stroke using the primary hand.
    /// </summary>
    public void RedoBrushStroke()
    {
        _volumeInputController.RedoBrushStroke(_volumeInputController.PrimaryHand);
    }

    /// <summary>
    /// Resets the brush size to its default value.
    /// </summary>
    public void BrushSizeReset()
    {
        _volumeInputController.ResetBrushSize();
    }

    /// <summary>
    /// Sets the painting mode to additive (brush adds to the mask).
    /// </summary>
    public void PaintingAdditive()
    {
        _volumeInputController.SetBrushAdditive();
    }

    /// <summary>
    /// Sets the painting mode to subtractive (brush erases from the mask).
    /// </summary>
    public void PaintingSubtractive()
    {
        _volumeInputController.SetBrushSubtractive();
    }

    /// <summary>
    /// Sets the painting mode to subtractive (brush erases from the mask).
    /// </summary>
    public void OpenSourcesMenu()
    {
        spawnMenu(sourcesMenu);
    }

    /// <summary>
    /// Opens the plots menu in front of the player.
    /// </summary>
    public void OpenPlotsWindow()
    {
        spawnMenu(plotsMenu);
    }

    /// <summary>
    /// Spawns a UI menu in front of the player at a fixed distance.
    /// </summary>
    /// <param name="menu">The GameObject representing the menu to spawn.</param>
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
    /// Initiates the save mask process. If it's a new mask, saves it immediately;
    /// otherwise, shows a popup for overwrite confirmation.
    /// </summary>
    public void SaveMask()
    {
        if (_activeDataSet.IsMaskNew)
        {
            SaveNewMask();
        }
        else
        {
            savePopup.transform.SetParent(this.transform.parent, false);
            savePopup.transform.localPosition = this.transform.localPosition;
            savePopup.transform.localRotation = this.transform.localRotation;
            savePopup.transform.localScale = this.transform.localScale;

            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("Cancel").GetComponent<Button>().onClick.RemoveAllListeners();
            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("Overwrite").GetComponent<Button>().onClick.RemoveAllListeners();
            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("NewFile").GetComponent<Button>().onClick.RemoveAllListeners();

            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("Cancel").GetComponent<Button>().onClick.AddListener(SaveCancel);
            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("Overwrite").GetComponent<Button>().onClick.AddListener(SaveOverwriteMask);
            savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform
                .Find("NewFile").GetComponent<Button>().onClick.AddListener(SaveNewMask);

            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents
                .PaintModeDisabled);
            this.gameObject.SetActive(false);
            savePopup.SetActive(true);
        }

    }

    /// <summary>
    /// Cancels the save popup and returns to the paint menu.
    /// </summary>
    public void SaveCancel()
    {
        paintMenu.SetActive(true);
        savePopup.SetActive(false);
    }

    /// <summary>
    /// Saves the current mask and overwrites the existing one.
    /// </summary>
    public void SaveOverwriteMask()
    {
        _activeDataSet?.SaveMask(true);

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }

    /// <summary>
    /// Saves the current mask and overwrites the existing one.
    /// </summary>
    public void SaveNewMask()
    {
        _activeDataSet?.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        SaveCancel();
    }

    /// <summary>
    /// Adds a new painting source ID via the input controller.
    /// </summary>
    public void AddNewSource()
    {
        _volumeInputController.AddNewSource();
    }

    /// <summary>
    /// Enables editing of the current source ID.
    /// </summary>
    public void EditSourceId()
    {
        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.StartEditSource);
    }

    /// <summary>
    /// Opens the shape selection menu and prepares for switching paint shape.
    /// </summary>
    public void OpenShapeMenu()
    {
        shapeMenu.transform.SetParent(this.transform.parent, false);
        shapeMenu.transform.localPosition = this.transform.localPosition;
        shapeMenu.transform.localRotation = this.transform.localRotation;
        shapeMenu.transform.localScale = this.transform.localScale;

        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);

        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeDisabled);
        _volumeInputController.ChangeShapeSelection();
        PaintingAdditive();
        gameObject.SetActive(false);
        shapeMenu.SetActive(true);

    }

}
