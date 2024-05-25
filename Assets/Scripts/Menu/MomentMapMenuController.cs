using System;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    
    public IntPtr RenderTextureToArray(RenderTexture renderTexture)
    {
        IntPtr arrayToReturn = Marshal.AllocHGlobal(renderTexture.width * renderTexture.height * sizeof(float));
        
        // Create a new Texture2D and set its pixel values
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RFloat, false);
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Get the raw texture data
        var data = tex.GetRawTextureData<float>();

        Marshal.Copy(data.ToArray(), 0, arrayToReturn, data.Length);
        
        // Return the array
        return arrayToReturn;
    }
    
    public void SaveToFits()
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
        var path0 = Path.Combine(directoryPath, string.Format("Moment_map_0_{0}.fits", DateTime.Now.ToString("yyyyMMdd_Hmmss")));
        var path1 = Path.Combine(directoryPath, string.Format("Moment_map_1_{0}.fits", DateTime.Now.ToString("yyyyMMdd_Hmmss")));

        var moment0Array = RenderTextureToArray(getFirstActiveDataSet().GetMomentMapRenderer().Moment0Map);
        var moment1Array = RenderTextureToArray(getFirstActiveDataSet().GetMomentMapRenderer().Moment1Map);

        IntPtr mainFitsFilePtr = IntPtr.Zero;
        FitsReader.FitsOpenFile(out mainFitsFilePtr, getFirstActiveDataSet().FileName, out int status, true);

        FitsReader.WriteMomentMap(mainFitsFilePtr, path0, moment0Array,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment0Map.width,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment0Map.height);
        FitsReader.WriteMomentMap(mainFitsFilePtr, path1, moment1Array,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment1Map.width,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment1Map.height);
        
        FitsReader.FitsCloseFile(mainFitsFilePtr, out status);
        
        Debug.Log($"Moment maps saved to {path0} and {path1}");
    }
    
    public void ChangeLimitType()
    {
        _limitType = _limitType == LimitType.MinMax ? LimitType.ZScale : LimitType.MinMax;
        getFirstActiveDataSet().GetMomentMapRenderer().UseZScale = _limitType == LimitType.ZScale;
        LimitTypeText.gameObject.GetComponent<Text>().text = $"{_limitType}";
    }

}
