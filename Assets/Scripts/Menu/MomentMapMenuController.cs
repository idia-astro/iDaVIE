using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class MomentMapMenuController : MonoBehaviour
{

    public Text ThresholdTypeText;
    public GameObject volumeDataSetManager;
    private VolumeDataSetRenderer[] dataSets;
    int thresholdType = 0;
    public enum ThresholdType
    {
        Mask, Threshold
    }

    // Start is called before the first frame update
    void Start()
    {
        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            if (getFirstActiveDataSet() != null)
            {
                VolumeDataSet dataSet = getFirstActiveDataSet().GetDataSet();
                getFirstActiveDataSet().GetMomentMapRenderer().UpdatePlotWindow();

                if (getFirstActiveDataSet().Mask != null)
                {
                    ThresholdTypeText.gameObject.GetComponent<Text>().text = (ThresholdType)0 + "";
                    this.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.SetActive(false);
                }
                else
                {
                    ThresholdTypeText.gameObject.GetComponent<Text>().text = (ThresholdType)1 + "";
                    this.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.SetActive(true);
                }
            }
        }

       


    }

    public void IncreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold <= 0.1)
        {
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold += 0.001f;
        }
    }

    public void DecreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold >= -0.1)
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold -= 0.001f;
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

    public void SetThresholdType()
    {
        if (thresholdType == 0)
        {
            thresholdType = 1;
            this.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.SetActive(true);
            getFirstActiveDataSet().GetMomentMapRenderer().UseMask = false;

        }
        else if (getFirstActiveDataSet().Mask != null)
        {
            thresholdType = 0;
            this.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.SetActive(false);
            getFirstActiveDataSet().GetMomentMapRenderer().UseMask = true;

        }
        getFirstActiveDataSet().GetMomentMapRenderer().CalculateMomentMaps();
        ThresholdTypeText.gameObject.GetComponent<Text>().text = (ThresholdType)thresholdType + "";
    }

}
