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
/// Manages the shape selection and manipulation UI used in the painting workflow.
/// Allows users to create, modify, and apply shape-based masks to volume datasets.
/// </summary>
public class ShapeMenuController : MonoBehaviour
{
    /// <summary>
    /// Reference to the GameObject containing VolumeDataSetRenderer components.
    /// </summary>
    public GameObject volumeDatasetRendererObj = null;

    /// <summary>
    /// The currently active VolumeDataSetRenderer.
    /// </summary>
    public VolumeDataSetRenderer _activeDataSet;

    /// <summary>
    /// Reference to the controller handling feature-based painting options.
    /// </summary>
    public FeatureMenuController featureMenuController;
    private VolumeDataSetRenderer[] _dataSets;

    /// <summary>
    /// Reference to the paint mode menu UI.
    /// </summary>
    public GameObject paintMenu;

    /// <summary>
    /// Reference to the input controller managing volume painting interactions.
    /// </summary>
    public VolumeInputController _volumeInputController = null;

    /// <summary>
    /// Gets the VolumeInputController used for handling user interactions.
    /// </summary>
    public VolumeInputController VolumeInputController => _volumeInputController;

    /// <summary>
    /// Manages creation, selection, and manipulation of paint shapes.
    /// </summary>
    public ShapesManager shapesManager;
    private Text sourceIDText;

    /// <summary>
    /// Unity lifecycle method called when the object is enabled.
    /// Initialises dataset references and sets the shape manager to idle.
    /// </summary>
    void OnEnable()
    {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();

        sourceIDText = gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).GetComponent<Text>();
        shapesManager.MakeIdle();

    }

    /// <summary>
    /// Unity update loop called once per frame.
    /// Updates the active dataset and source ID text.
    /// </summary>
    void Update()
    {
        var firstActive = getFirstActiveDataSet();
        firstActive.DisplayMask = true;
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }
        if (_volumeInputController.SourceId <= 0)
        {
            sourceIDText.text = "Please select a Source ID to paint";
        }
        else
        {
            sourceIDText.text = $"Shape Mode (Source ID {_volumeInputController.SourceId})";
        }

    }

    /// <summary>
    /// Returns the first active and enabled VolumeDataSetRenderer.
    /// </summary>
    /// <returns>The first active dataset or null if none is active.</returns>
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
    /// Exits shape painting mode, destroys temporary shapes,
    /// and disables the mask display.
    /// </summary>
    public void ExitPaintMode()
    {
        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        _volumeInputController.ChangeShapeSelection();
        shapesManager.DestroyShapes();
        if (!shapesManager.isIdle()) shapesManager.DestroyCurrentShape();
        _activeDataSet.DisplayMask = false;
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Opens the main paint menu and restores the default painting state.
    /// </summary>
    public void OpenPaintMenu()
    {
        paintMenu.transform.SetParent(this.transform.parent, false);
        paintMenu.transform.localPosition = this.transform.localPosition;
        paintMenu.transform.localRotation = this.transform.localRotation;
        paintMenu.transform.localScale = this.transform.localScale;

        if (_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
        {
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        }

        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeEnabled);
        _volumeInputController.ChangeShapeSelection();
        shapesManager.DestroyShapes();
        if (!shapesManager.isIdle())
        {
            shapesManager.DestroyCurrentShape();
        }
        gameObject.SetActive(false);
        paintMenu.SetActive(true);
    }

    /// <summary>
    /// Begins shape selection mode, allowing the user to choose or place a shape.
    /// </summary>
    public void StartShapeSelection()
    {
        if (!shapesManager.isIdle())
        {
            return;
        }
        shapesManager.StartShapes();
        _volumeInputController.ShowSelectableShape(shapesManager.GetCurrentShape());
    }

    /// <summary>
    /// Deletes currently selected shapes in the shape manager.
    /// </summary>
    public void DeleteSelectedShapes()
    {
        shapesManager.DeleteSelectedShapes();
    }

    /// <summary>
    /// Switches between shape interaction modes (e.g., move, scale, rotate).
    /// </summary>
    public void ChangeModes()
    {
        shapesManager.ChangeModes();
    }

    /// <summary>
    /// Duplicates selected shapes in the scene.
    /// </summary>
    public void CopyShapes()
    {
        shapesManager.CopyShapes();
    }

    /// <summary>
    /// Duplicates selected shapes in the scene.
    /// </summary>
    public void Undo()
    {
        shapesManager.Undo();
        _activeDataSet.GetMomentMapRenderer().CalculateMomentMaps();
        //featureMenuController.UpdateInfo();    
    }

    /// <summary>
    /// Spawns the given menu GameObject in front of the player.
    /// </summary>
    /// <param name="menu">The UI menu to spawn.</param>
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
    /// Applies the shape-defined mask to the active dataset.
    /// Clears any painted shapes before and after application.
    /// </summary>
    public void applyMask()
    {
        shapesManager.ClearPaintedShapes();
        shapesManager.applyMask(_activeDataSet, _volumeInputController, true, false);
        shapesManager.applyMask(_activeDataSet, _volumeInputController, false, false);
        shapesManager.ClearShapes();
    }


}
