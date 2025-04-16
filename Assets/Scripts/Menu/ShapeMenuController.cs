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

public class ShapeMenuController : MonoBehaviour
{
    public GameObject volumeDatasetRendererObj = null;
    public VolumeDataSetRenderer _activeDataSet;
    public FeatureMenuController featureMenuController;
    private VolumeDataSetRenderer[] _dataSets;

    public GameObject paintMenu;

    public VolumeInputController _volumeInputController = null;
    public VolumeInputController VolumeInputController => _volumeInputController;
    public ShapesManager shapesManager;
    private Text sourceIDText;

    void OnEnable()
    {
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        if (_volumeInputController == null)
            _volumeInputController = FindObjectOfType<VolumeInputController>();
        
        sourceIDText = gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).GetComponent<Text>();
        shapesManager.MakeIdle();
        
    }

    // Update is called once per frame
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

    public void ExitPaintMode()
    {
        if(_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        _volumeInputController.ChangeShapeSelection();
        shapesManager.DestroyShapes();
        if(!shapesManager.isIdle()) shapesManager.DestroyCurrentShape();
        _activeDataSet.DisplayMask = false;
        this.gameObject.SetActive(false);
    }

    public void OpenPaintMenu() {
        paintMenu.transform.SetParent(this.transform.parent, false);
        paintMenu.transform.localPosition = this.transform.localPosition;
        paintMenu.transform.localRotation = this.transform.localRotation;
        paintMenu.transform.localScale = this.transform.localScale;

        if(_volumeInputController.InteractionStateMachine.State == VolumeInputController.InteractionState.EditingSourceId)
            _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.CancelEditSource);
        
        _volumeInputController.InteractionStateMachine.Fire(VolumeInputController.InteractionEvents.PaintModeEnabled);
        _volumeInputController.ChangeShapeSelection();
        shapesManager.DestroyShapes();
        if(!shapesManager.isIdle()) shapesManager.DestroyCurrentShape();
        gameObject.SetActive(false);
        paintMenu.SetActive(true);
    }

    public void StartShapeSelection() {
        if(!shapesManager.isIdle()) return; 
        shapesManager.StartShapes();
        _volumeInputController.ShowSelectableShape(shapesManager.GetCurrentShape());
    }

    public void DeleteSelectedShapes() {
        shapesManager.DeleteSelectedShapes();
    }

    public void ChangeModes() {
        shapesManager.ChangeModes();
    }

    public void CopyShapes() {
        shapesManager.CopyShapes();
    }

    public void Undo() {
        shapesManager.Undo();
        _activeDataSet.GetMomentMapRenderer().CalculateMomentMaps();
        //featureMenuController.UpdateInfo();    
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

    public void applyMask()
    {
        shapesManager.ClearPaintedShapes();
        shapesManager.applyMask(_activeDataSet, _volumeInputController, true, false);
        shapesManager.applyMask(_activeDataSet, _volumeInputController, false, false);
        shapesManager.ClearShapes();    
        _activeDataSet.GetMomentMapRenderer().CalculateMomentMaps();
        //featureMenuController.UpdateInfo();  
    }


}
