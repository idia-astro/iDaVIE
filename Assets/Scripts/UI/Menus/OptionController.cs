using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class OptionController : MonoBehaviour
{

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    // Color Map
    public Button ButtonPrevColorMap;
    public Button ButtonNextColorMap;
    public Text LabelColormap;
    public GameObject volumeDatasetRendererObj = null;


    int defaultColorIndex = 45;
    int colorIndex;



    void Start()
    {
        colorIndex = defaultColorIndex;
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);


        LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";

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

    public void SetNextColorMap()
    {
        if (colorIndex == ColorMapUtils.NumColorMaps-1)
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

}
