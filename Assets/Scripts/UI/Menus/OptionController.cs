using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VolumeData;

public class OptionController : MonoBehaviour
{

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    // Color Map
    public Text LabelHand;
    public GameObject volumeDatasetRendererObj = null;



    int defaultColorIndex = 33;
    int colorIndex = -1;
    int hand = 0;


    public enum Hand
    {
        Right, Left
    }

    void Start()
    {
       
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);


       
        LabelHand.gameObject.GetComponent<Text>().text = (Hand)0 + "";


    }

    // Update is called once per frame
    void Update()
    {

        if (_dataSets != null)
        {
            //Debug.Log("_dataSets size " + _dataSets.Length);
        }

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
        LabelHand.gameObject.GetComponent<Text>().text = (Hand)hand + "";
        


    }

}
