//
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT
//
// This file is part of the iDaVIE project.
//
// iDaVIE is free software: you can redistribute it and/or modify it under the terms 
// of the GNU Lesser General Public License (LGPL) as published by the Free Software 
// Foundation, either version 3 of the License, or (at your option) any later version.
//
// iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
// PURPOSE. See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with 
// iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
//
// Additional information and disclaimers regarding liability and third-party 
// components can be found in the DISCLAIMER and NOTICE files included with this project.
//
//
using System;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VolumeData;

public class MomentMapMenuController : MonoBehaviour
{

    public TMP_Text ThresholdTypeText;
    public TMP_Text LimitTypeText;

    public TMP_Text ThresholdValueText;
    public TMP_Text MomentMap0Title;
    public TMP_Text MomentMap1Title;
    public GameObject volumeDataSetManager;
    private VolumeDataSetRenderer[] dataSets;
    private float _threshold;
    // <summary>
    /// This attribute dictates by how much the threshold value changes when the user clicks the button.
    /// </summary>
    private float _thresholdIncrement = 0.01f;
    private float _cached_threshold;
    /// <summary>
    /// Timer for the increment and decrement buttons for min and max values.
    /// </summary>
    private float _deltaT = 0.00000f;

    /// <summary>
    /// If _deltaT > this, reset _deltaT to 0.
    /// </summary>
    private float _resetTime = 0.2f;
    
    [SerializeField] private Button DecreaseThresholdButton;
    [SerializeField] private Button IncreaseThresholdButton;
    
    public Camera MomentMapCamera;
    int thresholdType = 0;
    [SerializeField] private GameObject keypadPrefab = null;
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

                float dataMin = dataSet.MinValue;
                float dataMax = dataSet.MaxValue;
                int increments = Config.Instance.momentMapThresholdSteps;
                _thresholdIncrement = (dataMax - dataMin) / increments;
                _resetTime = 1.0f / Config.Instance.momentMapStepsPerSecond;

                LimitTypeText.text = $"{_limitType}";

                if (getFirstActiveDataSet().Mask != null)
                {
                    ThresholdTypeText.text = (ThresholdType)0 + "";
                    this.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.SetActive(false);
                }
                else
                {
                    ThresholdTypeText.text = (ThresholdType)1 + "";
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

    void Update()
    {
        _threshold = float.Parse(ThresholdValueText.text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        if (_cached_threshold != _threshold)
        {
            SetMomentMapThreshold();
        }

        // Add previous frame time to timer
        _deltaT += Time.smoothDeltaTime;
        // If timer has not yet reached reset, don't activate any buttons.
        if (_deltaT < _resetTime)
            return;
        // Else, reset timer and activate buttons where appropriate
        _deltaT = 0.00000f;
        if (DecreaseThresholdButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
           DecreaseMomentMapThreshold();
        }

        if (IncreaseThresholdButton.gameObject.GetComponent<UI.UserSelectableItem>().isPressed)
        {
            IncreaseMomentMapThreshold();
        }
    }

    /// <summary>
    /// Increases the moment map threshold.
    /// </summary>
    public void IncreaseMomentMapThreshold()
    {
        float dataMin = getFirstActiveDataSet().GetDataSet().MinValue;
        float dataMax = getFirstActiveDataSet().GetDataSet().MaxValue;
        _threshold = Mathf.Clamp(_threshold + _thresholdIncrement, dataMin, dataMax);
        ThresholdValueText.text = _threshold.ToString();
        SetMomentMapThreshold();
    }

    /// <summary>
    /// Decreases the moment map threshold.
    /// summary>
    public void DecreaseMomentMapThreshold()
    {
        float dataMin = getFirstActiveDataSet().GetDataSet().MinValue;
        float dataMax = getFirstActiveDataSet().GetDataSet().MaxValue;
        _threshold = Mathf.Clamp(_threshold - _thresholdIncrement, dataMin, dataMax);
        ThresholdValueText.text = _threshold.ToString();
        SetMomentMapThreshold();
    }

    public void SetMomentMapThreshold()
    {
        _cached_threshold = _threshold;
        if (getFirstActiveDataSet().GetMomentMapRenderer() != null)
        {
            getFirstActiveDataSet().GetMomentMapRenderer().MomentMapThreshold = _threshold;
        }
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
        ThresholdTypeText.text = (ThresholdType)thresholdType + "";
    }

    /// <summary>
    /// Saves the moment maps 0 and 1 to a single png file, with graph axes values added.
    /// </summary>
    public void SaveToImage()
    {
        getFirstActiveDataSet().volumeInputController.VibrateController(getFirstActiveDataSet().volumeInputController.PrimaryHand);
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

        Debug.Log($"Moment maps saved to {path} as single PNG.");
        ToastNotification.ShowSuccess($"Moment maps saved to {path} as single PNG.");
    }
    
    /// <summary>
    /// Converts a RenderTexture to a float array and returns an IntPtr to the array
    /// </summary>
    /// <param name="renderTexture">RenderTexture that will be converted</param>
    /// <returns>IntPtr that points the float array</returns>
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
    
    /// <summary>
    /// Exports the two moment maps to FITS files
    /// </summary>
    public void SaveToFits()
    {
        getFirstActiveDataSet().volumeInputController.VibrateController(getFirstActiveDataSet().volumeInputController.PrimaryHand);
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
            getFirstActiveDataSet().GetMomentMapRenderer().Moment0Map.height, 0);
        FitsReader.WriteMomentMap(mainFitsFilePtr, path1, moment1Array,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment1Map.width,
            getFirstActiveDataSet().GetMomentMapRenderer().Moment1Map.height, 1);
        
        FitsReader.FitsCloseFile(mainFitsFilePtr, out status);
        
        Debug.Log($"Moment maps saved to {path0} and {path1}");
        ToastNotification.ShowSuccess($"Moment map 0 saved to {path0}");
        ToastNotification.ShowSuccess($"Moment map 1 saved to {path1}");
    }

    /// <summary>
    /// Opens a keypad in VR space. Once the user is satisfied with the number, it sends the value back to target.
    /// </summary>
    /// <param name="target">The text field that the keypad number will be sent to.</param>
    public void OpenKeypad(TextMeshProUGUI target)
    {
        // If any keypads are open, destroy them first.
        if (GameObject.FindGameObjectWithTag("keypad") != null)
        {
            GameObject[] keypads = GameObject.FindGameObjectsWithTag("keypad");
            foreach(GameObject kp in keypads)
                Destroy(kp);
        }
        Vector3 pos = new Vector3(this.transform.parent.position.x, this.transform.parent.position.y - 0.4f, this.transform.parent.position.z);
        //instantiate item
        GameObject SpawnedItem = Instantiate(keypadPrefab, pos, this.transform.parent.rotation);
        SpawnedItem.transform.localRotation = this.transform.parent.rotation;
        SpawnedItem.GetComponent<KeypadController>().targetText = target;
    }
    
    public void ChangeLimitType()
    {
        _limitType = _limitType == LimitType.MinMax ? LimitType.ZScale : LimitType.MinMax;
        getFirstActiveDataSet().GetMomentMapRenderer().UseZScale = _limitType == LimitType.ZScale;
        LimitTypeText.text = $"{_limitType}";
    }

}
