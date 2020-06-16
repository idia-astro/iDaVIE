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

    [SerializeField] private Button DecreaseMinScaleButton;
    [SerializeField] private Button IncreaseMinScaleButton;
    [SerializeField] private Text minText;

    [SerializeField] private Button DecreaseMaxScaleButton;
    [SerializeField] private Button IncreaseMaxScaleButton;
    [SerializeField] private Text maxText;

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
             
                minText.text = histogramHelper.CurrentMin.ToString();  
                maxText.text = histogramHelper.CurrentMax.ToString();
            }
        }
    }

    void Update()
    {
        if (DecreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
           minText.text = Mathf.Clamp(float.Parse(minText.text) - 0.01f, getFirstActiveDataSet().GetDatsSet().MinValue* 2, float.Parse(maxText.text)).ToString(); 
        }

        if (DecreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            maxText.text = Mathf.Clamp(float.Parse(maxText.text) - 0.01f, float.Parse(minText.text), getFirstActiveDataSet().GetDatsSet().MaxValue* 2).ToString();
        }

        if (IncreaseMinScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            minText.text = Mathf.Clamp(float.Parse(minText.text) + 0.01f, getFirstActiveDataSet().GetDatsSet().MinValue * 2, float.Parse(maxText.text)).ToString();
        }

        if (IncreaseMaxScaleButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            maxText.text = Mathf.Clamp(float.Parse(maxText.text) + 0.01f, float.Parse(minText.text), getFirstActiveDataSet().GetDatsSet().MaxValue * 2).ToString();
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
      VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().GetDatsSet(), float.Parse(minText.text), float.Parse(maxText.text));
      histogramHelper.CreateHistogramImg(getFirstActiveDataSet().GetDatsSet().Histogram, getFirstActiveDataSet().GetDatsSet().HistogramBinWidth, float.Parse(minText.text), float.Parse(maxText.text), getFirstActiveDataSet().GetDatsSet().MeanValue, getFirstActiveDataSet().GetDatsSet().StanDev);
    }

    public void ResetButtonHandler()
    {
        VolumeDataSet.UpdateHistogram(getFirstActiveDataSet().GetDatsSet(), getFirstActiveDataSet().GetDatsSet().MinValue, getFirstActiveDataSet().GetDatsSet().MaxValue);
        histogramHelper.CreateHistogramImg(getFirstActiveDataSet().GetDatsSet().Histogram, getFirstActiveDataSet().GetDatsSet().HistogramBinWidth, getFirstActiveDataSet().GetDatsSet().MinValue, getFirstActiveDataSet().GetDatsSet().MaxValue, getFirstActiveDataSet().GetDatsSet().MeanValue, getFirstActiveDataSet().GetDatsSet().StanDev);
    }

    public void UpdateUI(float min, float max, Sprite img)
    {
        minText.text = min.ToString();
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
