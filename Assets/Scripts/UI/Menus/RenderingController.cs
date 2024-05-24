using DataFeatures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VolumeData;

using Valve.VR.InteractionSystem;
using System;
using System.Globalization;
using System.Linq;

public class RenderingController : MonoBehaviour
{

    private VolumeDataSetRenderer _activeDataSet;
    private VolumeDataSetRenderer[] _dataSets;

    // Color Map
    public Button ButtonPrevColorMap;
    public Button ButtonNextColorMap;
    public Text LabelColormap;

    //Scaling type
    public Button ButtonPrevScalingType;
    public Button ButtonNextScalingType;
    public Text LabelScalingType;
    
    //Rest Frequency
    public Text LabelRestFrequencyIndex;
    public Text LabelRestFrequencyValue;

    public GameObject volumeDatasetRendererObj = null;

    int defaultColorIndex = 33;
    int defaultScalingIndex = 0;
    int colorIndex = -1;
    int scalingIndex = -1;
    private VolumeCommandController _volumeCommandController;

    void Awake()
    {
        if (getFirstActiveDataSet() != null)
        {
            getFirstActiveDataSet().RestFrequencyGHzListIndexChanged += OnRestFrequencyIndexOfDatasetChanged;
            getFirstActiveDataSet().RestFrequencyGHzChanged += OnRestFrequencyOfDatasetChanged;
        }
    }
    
    void Start()
    {
        colorIndex = defaultColorIndex;
        scalingIndex = defaultScalingIndex;
        if (volumeDatasetRendererObj != null)
            _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";
        LabelScalingType.gameObject.GetComponent<Text>().text = (ScalingType)scalingIndex + "";
       
        _volumeCommandController = FindObjectOfType<VolumeCommandController>();
    }

// Update is called once per frame
	void Update()
    {
        var firstActive = getFirstActiveDataSet();
        if (firstActive && _activeDataSet != firstActive)
        {
            _activeDataSet = firstActive;
            UpdateRestFrequencyDisplay();
        }
        if (_activeDataSet.ColorMap != ColorMapUtils.FromHashCode(colorIndex))
        {
            colorIndex = (int)_activeDataSet.ColorMap;
            LabelColormap.gameObject.GetComponent<Text>().text = ColorMapUtils.FromHashCode(colorIndex) + "";
        }

        if ( (_activeDataSet.ScaleMin + _activeDataSet.ThresholdMin * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString() != this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("minThresholdValue").gameObject.GetComponent<Text>().text )
        {
            this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("minThresholdValue").gameObject.GetComponent<Text>().text = (_activeDataSet.ScaleMin + _activeDataSet.ThresholdMin * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString();
        }
        if ((_activeDataSet.ScaleMin + _activeDataSet.ThresholdMax * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString() != this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("maxThresholdValue").gameObject.GetComponent<Text>().text)
        {
            this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("maxThresholdValue").gameObject.GetComponent<Text>().text = (_activeDataSet.ScaleMin + _activeDataSet.ThresholdMax * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString();
        }

        if ( this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("ButtonDecreaseMinThreshold").gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            DecreaseMinThreshold();
        }

        if (this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("ButtonIncreaseMinThreshold").gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            IncreaseMinThreshold();
        }

        if (this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("ButtonDecreaseMaxThreshold").gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            DecreaseMaxThreshold();
        }

        if (this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("ButtonIncreaseMaxThreshold").gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            IncreaseMaxThreshold();
        }
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        _dataSets = volumeDatasetRendererObj.GetComponentsInChildren<VolumeDataSetRenderer>(true);

        if (_dataSets != null)
        {
            foreach (var dataSet in _dataSets)
            {
                if (dataSet.isActiveAndEnabled)
                {
                    return dataSet;
                }
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
         _volumeCommandController.setColorMap(colorMap);
    }

    public void ResetColorMap()
    {
        colorIndex = defaultColorIndex;
        SetColorMap(ColorMapUtils.FromHashCode(colorIndex));
    }

    public void SetNextScalingType()
    {
        
        if (scalingIndex == Enum.GetNames(typeof(ScalingType)).Length - 1)
        {
            scalingIndex = -1;
        }
        scalingIndex++;

        _volumeCommandController.ChangeScalingType((ScalingType)scalingIndex);
        LabelScalingType.gameObject.GetComponent<Text>().text = (ScalingType)scalingIndex + "";
        //SetColorMap(ColorMapUtils.FromHashCode(colorIndex));
    }

    public void SetPrevScalingType()
    {
        if (scalingIndex == 0)
        {
            scalingIndex = Enum.GetNames(typeof(ScalingType)).Length;
        }
        scalingIndex--;

        _volumeCommandController.ChangeScalingType((ScalingType)scalingIndex);
        LabelScalingType.gameObject.GetComponent<Text>().text = (ScalingType)scalingIndex + "";
    }


    public void ResetScalingType()
    {
        scalingIndex = defaultScalingIndex;
        _volumeCommandController.ChangeScalingType((ScalingType)scalingIndex);
        LabelScalingType.gameObject.GetComponent<Text>().text = (ScalingType)scalingIndex + "";
    }

    public void SetThreshold()
    {     
        if (_activeDataSet)
        {
            this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("minThresholdValue").gameObject.GetComponent<Text>().text = (_activeDataSet.ScaleMin + _activeDataSet.ThresholdMin * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString();
            this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("maxThresholdValue").gameObject.GetComponent<Text>().text = (_activeDataSet.ScaleMin + _activeDataSet.ThresholdMax * (_activeDataSet.ScaleMax - _activeDataSet.ScaleMin)).ToString();
        }
    }

    public void IncreaseMinThreshold()
    {
        if (_activeDataSet)
        {
            _activeDataSet.ThresholdMin = Mathf.Clamp(_activeDataSet.ThresholdMin + 0.001f, 0, _activeDataSet.ThresholdMax);
            SetThreshold();
        }
    }

    public void IncreaseMaxThreshold()
    {
        if (_activeDataSet)
        {
            _activeDataSet.ThresholdMax = Mathf.Clamp(_activeDataSet.ThresholdMax + 0.001f, _activeDataSet.ThresholdMin, 1);
            SetThreshold();
        }
    }

    public void DecreaseMinThreshold()
    {
        if (_activeDataSet)
        {
            _activeDataSet.ThresholdMin = Mathf.Clamp(_activeDataSet.ThresholdMin - 0.001f, 0, _activeDataSet.ThresholdMax);
            SetThreshold();
        }
    }

    public void DecreaseMaxThreshold()
    {
        if (_activeDataSet)
        {
            _activeDataSet.ThresholdMax = Mathf.Clamp(_activeDataSet.ThresholdMax-0.001f, _activeDataSet.ThresholdMin, 1);
            SetThreshold();
        }
    }

    public void ResetMinThreshold()
    {
        if (_activeDataSet)
        {
            _activeDataSet.ThresholdMin = _activeDataSet.InitialThresholdMin;
            this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Min_thresholdObject").gameObject.transform.Find("minThresholdValue").gameObject.GetComponent<Text>().text = _activeDataSet.ThresholdMin.ToString();
        }
    }

    public void ResetMaxThreshold()
    {
        _activeDataSet.ThresholdMax = _activeDataSet.InitialThresholdMax;
        this.gameObject.transform.Find("Scroll View").gameObject.transform.Find("Viewport").gameObject.transform.Find("Content").gameObject.transform.Find("SpawnPoint").gameObject.transform.Find("Max_thresholdObject").gameObject.transform.Find("maxThresholdValue").gameObject.GetComponent<Text>().text = _activeDataSet.ThresholdMax.ToString();
    }

    public void NextRestFrequency()
    {
        _activeDataSet.RestFrequencyGHzListIndex = (_activeDataSet.RestFrequencyGHzListIndex + 1) % _activeDataSet.RestFrequencyGHzList.Count;
        
    }

    public void PreviousRestFrequency()
    {
        _activeDataSet.RestFrequencyGHzListIndex = (_activeDataSet.RestFrequencyGHzListIndex - 1 + _activeDataSet.RestFrequencyGHzList.Count) % _activeDataSet.RestFrequencyGHzList.Count;
    }

    /// <summary>
    /// Refresh the rest frequency name (from index) and value in the display
    /// </summary>
    public void UpdateRestFrequencyDisplay()
    {
        LabelRestFrequencyIndex.GetComponent<Text>().text = _activeDataSet.RestFrequencyGHzList
            .ElementAt(_activeDataSet.RestFrequencyGHzListIndex).Key;
        LabelRestFrequencyValue.GetComponent<Text>().text = _activeDataSet.RestFrequencyGHz.ToString("F5", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// When the rest frequency index of the dataset changes, update the display.
    /// </summary>
    private void OnRestFrequencyIndexOfDatasetChanged()
    {
        UpdateRestFrequencyDisplay();
    }

    /// <summary>
    /// When the rest frequency of the dataset changes, update the display.
    /// </summary>
    private void OnRestFrequencyOfDatasetChanged()
    {
        UpdateRestFrequencyDisplay();
    }

    /*
    public void LoadFeature()
    {
        if (_activeDataSet)
        {
            _activeDataSet.GetComponentInChildren<FeatureSetManager>().ImportFeatureSet();
        }
    }
    */
}
