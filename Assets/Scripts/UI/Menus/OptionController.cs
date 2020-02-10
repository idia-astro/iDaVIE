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
    public Button ButtonPrevColorMap;
    public Button ButtonNextColorMap;
    public Text LabelColormap;
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
        colorIndex = defaultColorIndex;
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);


        LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";
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

        if (_activeDataSet.ColorMap != ColorMapUtils.FromHashCode(colorIndex))
        {
            colorIndex = (int)_activeDataSet.ColorMap;
            LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";
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

    public void SetNextColorMap()
    {
        if (colorIndex == ColorMapUtils.NumColorMaps - 1)
        {
            colorIndex = -1;
        }
        colorIndex++;

        SetColorMap(ColorMapUtils.FromHashCode(colorIndex));
    }

    public void SetPrevColorMap()
    {
        if (colorIndex == 0)
        {
            colorIndex = ColorMapUtils.NumColorMaps;
        }
        colorIndex--;

        SetColorMap(ColorMapUtils.FromHashCode(colorIndex));
    }

    public void SetColorMap(ColorMapEnum colorMap)
    {

        LabelColormap.gameObject.GetComponent<Text>().text = colorMap + "";
        if (_activeDataSet)
        {
            _activeDataSet.ColorMap = colorMap;
        }
    }

    public void ResetColorMap()
    {
        colorIndex = defaultColorIndex;
        SetColorMap(ColorMapUtils.FromHashCode(colorIndex));
    }

    public void LoadFeature()
    {

        if (_activeDataSet)
        {
            _activeDataSet.GetComponentInChildren<FeatureSetManager>().ImportFeatureSet();

        }
        //featureSetManager.ImportFeatureSet();

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
