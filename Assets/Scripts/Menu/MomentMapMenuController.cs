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
      
        
        Image im0 = this.gameObject.transform.Find("Map_container").gameObject.transform.Find("MomentMap0").GetComponent<Image>();
        Image im1 = this.gameObject.transform.Find("Map_container").gameObject.transform.Find("MomentMap1").GetComponent<Image>();

        Texture2D mom0 = im0.sprite.texture;
        Texture2D mom1 = im1.sprite.texture;

        int width = im0.sprite.texture.width;
        int height = im0.sprite.texture.height + im1.sprite.texture.height;
     

        //Create new textures
        Texture2D textureResult = new Texture2D(width, height);
        //create clone form texture
       // textureResult.SetPixels(mom0.GetPixels()+mom1.GetPixels());
        for (int x = 0; x < mom0.width; x++)
        {
            for (int y = 0; y < mom0.height; y++)
            {
                Color c = mom0.GetPixel(x, y);
                if (c.a > 0.0f) //Is not transparent
                {
                    textureResult.SetPixel(x, +mom0.height+y, c);
                }
                else
                {
                    textureResult.SetPixel(x, y, Color.white);
                }
            }
        }
        
        for (int x = 0; x < mom1.width; x++)
        {
            for (int y =0; y < mom1.height; y++)
            {
                Color c = mom1.GetPixel(x, y);
                if (c.a > 0.0f) //Is not transparent
                {
                    textureResult.SetPixel(x, y, c);
                }
                else
                {
                    textureResult.SetPixel(x, y, Color.white);
                }
            }
        }
        
        //Apply colors
        textureResult.Apply();
      
        byte[] bytes_mom = textureResult.EncodeToPNG();

        File.WriteAllBytes(path, bytes_mom);

    }
    
    public void ChangeLimitType()
    {
        _limitType = _limitType == LimitType.MinMax ? LimitType.ZScale : LimitType.MinMax;
        getFirstActiveDataSet().GetMomentMapRenderer().UseZScale = _limitType == LimitType.ZScale;
        LimitTypeText.gameObject.GetComponent<Text>().text = $"{_limitType}";
    }

}
