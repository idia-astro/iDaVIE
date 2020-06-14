using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class HistogramMenuController : MonoBehaviour
{
    public GameObject volumeDataSetManager;

    private VolumeDataSetRenderer[] dataSets;
    private HistogramHelper histogramHelper;

    [SerializeField] private Image img;

    [SerializeField] private Slider minSlider;
    [SerializeField] private InputField minText;

    [SerializeField] private Slider maxSlider;
    [SerializeField] private InputField maxText;

    // Start is called before the first frame update
    void Start()
    {
        histogramHelper = FindObjectOfType<HistogramHelper>();

        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            if (getFirstActiveDataSet() != null)
            {
                VolumeDataSet dataSet = getFirstActiveDataSet().GetDatsSet();
                minSlider.minValue = dataSet.MinValue;
                minSlider.maxValue = dataSet.MaxValue;
                minSlider.value = histogramHelper.CurrentMin;
                minText.text = minSlider.value.ToString();

                maxSlider.minValue = dataSet.MinValue;
                maxSlider.maxValue = dataSet.MaxValue;
                maxSlider.value = histogramHelper.CurrentMax;
                maxText.text = maxSlider.value.ToString();
            }
        }
    }

    public void MinSliderHandler(float value)
    {
        minText.text = value.ToString();
    }

    public void MaxSliderHandler(float value)
    {
        maxText.text = value.ToString();
    }

    public void UpdateButtonHandler()
    {
        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().GetDatsSet(), minSlider.value, maxSlider.value);
        histogramHelper.CreateHistogramImg(getFirstActiveDataSet().GetDatsSet().Histogram, getFirstActiveDataSet().GetDatsSet().HistogramBinWidth, minSlider.value, maxSlider.value, getFirstActiveDataSet().GetDatsSet().MeanValue, getFirstActiveDataSet().GetDatsSet().StanDev);
    }

    public void ResetButtonHandler()
    {
        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().GetDatsSet(), getFirstActiveDataSet().GetDatsSet().MinValue, getFirstActiveDataSet().GetDatsSet().MaxValue);
        histogramHelper.CreateHistogramImg(getFirstActiveDataSet().GetDatsSet().Histogram, getFirstActiveDataSet().GetDatsSet().HistogramBinWidth, getFirstActiveDataSet().GetDatsSet().MinValue, getFirstActiveDataSet().GetDatsSet().MaxValue, getFirstActiveDataSet().GetDatsSet().MeanValue, getFirstActiveDataSet().GetDatsSet().StanDev);
    }

    public void UpdateUI(float min, float max, Sprite img)
    {
        minSlider.value = min;
        minText.text = min.ToString();
        maxSlider.value = max;
        maxText.text = max.ToString();
        this.img.sprite = img;
    }

    private VolumeDataSetRenderer getFirstActiveDataSet()
    {
        foreach (var dataSet in dataSets)
        {
            if (dataSet.isActiveAndEnabled)
            {
                return dataSet;
            }
        }
        return null;
    }
}
