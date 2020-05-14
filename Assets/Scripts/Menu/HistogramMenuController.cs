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

    private Slider minSlider;
    private TMP_InputField minText;
    
    private Slider maxSlider;
    private TMP_InputField maxText;

    // Start is called before the first frame update
    void Start()
    {
        histogramHelper = FindObjectOfType<HistogramHelper>();

        minSlider = gameObject.transform.Find("PanelContents").gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_1")
            .gameObject.transform.Find("Slider_min").GetComponent<Slider>();
        maxSlider = gameObject.transform.Find("PanelContents").gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_2")
            .gameObject.transform.Find("Slider_max").GetComponent<Slider>();

        minText = gameObject.transform.Find("PanelContents").gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_1")
            .gameObject.transform.Find("Text_min").GetComponent<TMP_InputField>();
        maxText = gameObject.transform.Find("PanelContents").gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_2")
            .gameObject.transform.Find("Text_max").GetComponent<TMP_InputField>();

        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            VolumeDataSet dataSet = getFirstActiveDataSet().GetDatsSet();
            if (dataSet != null)
            {
                minSlider.minValue = dataSet.MinValue;
                minSlider.maxValue = dataSet.MaxValue;
                minSlider.value = histogramHelper.CurrentMin;

                maxSlider.minValue = dataSet.MinValue;
                maxSlider.maxValue = dataSet.MaxValue;
                maxSlider.value = histogramHelper.CurrentMax;
            }
        }


        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        minText.text = minSlider.value.ToString();
        maxText.text = maxSlider.value.ToString();
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
