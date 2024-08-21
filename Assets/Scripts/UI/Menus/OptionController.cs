/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VolumeData;

public class OptionController : MonoBehaviour
{
    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    // Color Map
    public TextMeshProUGUI LabelHand;
    public TextMeshProUGUI LabelStep;
    public GameObject volumeDatasetRendererObj = null;
    int defaultColorIndex = 33;
    int colorIndex = -1;
    int hand = 0;
    
    [SerializeField]
    public GameObject keypadPrefab = null;
    private float default_threadshold_step = 0.00025f;
    public enum Hand
    {
        Right, Left
    }

    void Start()
    {
       
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        LabelHand.text = (Hand)0 + "";
        LabelStep.text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    // Update is called once per frame
    void Update()
    {
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
        }

        if (LabelStep.text != getFirstActiveDataSet().GetMomentMapRenderer().momstep.ToString())
        {
            getFirstActiveDataSet().GetMomentMapRenderer().momstep = float.Parse(LabelStep.text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat); 
        }
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in _dataSets)
        {
            if (dataSet?.isActiveAndEnabled == true)
            {
                return dataSet;
            }
        }
        return null;
    }


    public void SetPrimaryHand()
    {
        if (hand == 0)
        {
            hand = 1;
            _activeDataSet._volumeInputController.PrimaryHand = SteamVR_Input_Sources.LeftHand;
        }
        else
        {
            hand = 0;
            _activeDataSet._volumeInputController.PrimaryHand = SteamVR_Input_Sources.RightHand;
        }
        LabelHand.text = (Hand)hand + "";
    }

    public void decreaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep -= default_threadshold_step;
        LabelStep.text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    public void increaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep += default_threadshold_step;
        LabelStep.text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }

    public void OpenKeypad()
    {
        if (GameObject.FindGameObjectWithTag("keypad") == null)
        {
            Vector3 pos = new Vector3(this.transform.parent.position.x, this.transform.parent.position.y-0.5f, this.transform.parent.position.z);
            //instantiate item
            Debug.Log(""+this.transform.parent.rotation.x+" "+this.transform.parent.rotation.y+" "+this.transform.parent.rotation.z);
            Debug.Log(""+this.transform.parent.position.x+" "+this.transform.parent.position.y+" "+this.transform.parent.position.z);
            GameObject SpawnedItem = Instantiate(keypadPrefab, pos, this.transform.parent.rotation);
            SpawnedItem.transform.localRotation = this.transform.parent.rotation;
            SpawnedItem.GetComponent<KeypadController>().targetText = LabelStep;
        }
    }

}
