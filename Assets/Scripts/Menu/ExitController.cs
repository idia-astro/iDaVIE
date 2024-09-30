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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class ExitController : MonoBehaviour
{
    public GameObject savePopup;


    public VolumeDataSetRenderer _activeDataSet;
    public VolumeInputController _volumeInputController = null;
    
    public void Exit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void Cancel()
    {
        this.gameObject.SetActive(false);
    }

    public void CancelSavePopup()
    {
        savePopup.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
    }

    public void SaveAndExit()
    {
        savePopup.transform.SetParent(this.transform.parent, false);
        savePopup.transform.localPosition = this.transform.localPosition;
        savePopup.transform.localRotation = this.transform.localRotation;
        savePopup.transform.localScale = this.transform.localScale;


        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.RemoveAllListeners();
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.RemoveAllListeners();

        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Cancel").GetComponent<Button>().onClick.AddListener(CancelSavePopup);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("Overwrite").GetComponent<Button>().onClick.AddListener(SaveOverwriteMask);
        savePopup.transform.Find("Content").gameObject.transform.Find("FirstRow").gameObject.transform.Find("NewFile").GetComponent<Button>().onClick.AddListener(SaveNewMask);


        gameObject.SetActive(false);
        savePopup.SetActive(true);
    }


    public void SaveOverwriteMask()
    {
        _activeDataSet?.SaveMask(true);

        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        Exit();
    }

    public void SaveNewMask()
    {
        _activeDataSet?.SaveMask(false);
        _volumeInputController.VibrateController(_volumeInputController.PrimaryHand);
        Exit();
    }
}
