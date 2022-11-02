using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class MomentMapMenuController : MonoBehaviour
{

    public Text ThresholdTypeText;
    public Text LimitTypeText;
    public TMP_Text MomentMap0Title;
    public TMP_Text MomentMap1Title;
    public GameObject volumeDataSetManager;
    private VolumeDataSetRenderer[] dataSets;
    public Camera MomentMapCamera;
    int thresholdType = 0;
    public enum ThresholdType
    {
        Mask, Threshold
    }

    public enum LimitType
    {
        ZScale, MinMax
    }

    private LimitType _limitType = LimitType.ZScale;

    void Start()
    {
        if (volumeDataSetManager != null)
        {
            dataSets = volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true);

            if (getFirstActiveDataSet() != null)
            {
                VolumeDataSet dataSet = getFirstActiveDataSet().GetDataSet();
                getFirstActiveDataSet().GetMomentMapRenderer().UpdatePlotWindow();

                LimitTypeText.gameObject.GetComponent<Text>().text = $"{_limitType}";

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
                MomentMap0Title.text += $" ({dataSet.GetPixelUnit()})";
                if (dataSet.AstframeIsFreq)
                    MomentMap1Title.text = $" ({dataSet.GetAltAxisUnit(3)})";
                else 
                    MomentMap1Title.text = $" ({dataSet.GetAxisUnit(3)})";
            }
        }
    }

    public void IncreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold <= 0.1)
        {
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold += getFirstActiveDataSet().GetMomentMapRenderer().momstep;
        }
    }

    public void DecreaseMomentMapThreshold()
    {
        if (getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold >= -0.1)
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold -= getFirstActiveDataSet().GetMomentMapRenderer().momstep;
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

    public void SaveToImage()
    {
        var directory = new DirectoryInfo(Application.dataPath);
        var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/MomentMaps");
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
        }
        var path = Path.Combine(directoryPath, string.Format("Moment_maps_{0}.png", DateTime.Now.ToString("yyyyMMdd_Hmmss")));
        int image_height = 1000;
        int image_width = 1000;
        RenderTexture screenTexture = new RenderTexture(image_width, image_height, 32);
        MomentMapCamera.targetTexture = screenTexture;
        RenderTexture.active = screenTexture;
        MomentMapCamera.Render();
        Texture2D renderedTexture = new Texture2D(image_width, image_height);
        renderedTexture.ReadPixels(new Rect(0, 0, image_width, image_height), 0, 0);
        RenderTexture.active = null;
        byte[] bytes_mom = renderedTexture.EncodeToPNG();
        File.WriteAllBytes(path, bytes_mom);
    }
    
    public void ChangeLimitType()
    {
        _limitType = _limitType == LimitType.MinMax ? LimitType.ZScale : LimitType.MinMax;
        getFirstActiveDataSet().GetMomentMapRenderer().UseZScale = _limitType == LimitType.ZScale;
        LimitTypeText.gameObject.GetComponent<Text>().text = $"{_limitType}";
    }

}
