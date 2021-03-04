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
    public Text LabelStep;
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
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
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

    public void decreaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep -= 0.00025f;
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";

    }
    public void increaseMomThresholdStep()
    {
        getFirstActiveDataSet().GetMomentMapRenderer().momstep += 0.00025f;
        LabelStep.gameObject.GetComponent<Text>().text = (float)getFirstActiveDataSet().GetMomentMapRenderer().momstep + "";
    }
}
