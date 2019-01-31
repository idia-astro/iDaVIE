using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class VolumeDataSet : MonoBehaviour
{
    [Header("Rendering Settings")]
    // Step control
    [Range(16, 512)] public int MaxSteps = 192;
    // Jitter factor
    [Range(0, 1)] public float Jitter = 1.0f;

    // Foveated rendering controls
    [Header("Foveated Rendering Controls")]
    public bool FoveatedRendering = false;
    [Range(0, 0.5f)] public float FoveationStart = 0.15f;
    [Range(0, 0.5f)] public float FoveationEnd = 0.40f;
    [Range(0, 0.5f)] public float FoveationJitter = 0.0f;
    [Range(16, 512)] public int FoveatedStepsLow = 64;
    [Range(16, 512)] public int FoveatedStepsHigh = 384;

    // Vignette Rendering
    [Header("Vignette Rendering Controls")]
    [Range(0, 0.5f)] public float VignetteFadeStart = 0.15f;
    [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
    [Range(0, 1)] public float VignetteIntensity = 0.0f;
    public Color VignetteColor = Color.black;
    
    [Header("Thresholds")]
    // Spatial thresholding
    public Vector3 SliceMin = Vector3.zero;
    public Vector3 SliceMax = Vector3.one;

    // Scale max n' min
    public float ScaleMax;
    public float ScaleMin;

    // Value thresholding
    [Range(0, 1)] public float ThresholdMin = 0;
    [Range(0, 1)] public float ThresholdMax = 1;
    
    [Space(10)]
    public Material RayMarchingMaterial;
    public string FileName;
    public Texture3D DataCube;
    public ColorMapEnum ColorMap = ColorMapEnum.Inferno;

    private MeshRenderer _renderer;
    private Material _materialInstance;

    private IntPtr _fitsCubeData;
    private long _numberDataPts;

    // Material property IDs
    private int _idSliceMin, _idSliceMax, _idThresholdMin, _idThresholdMax, _idJitter, _idMaxSteps, _idColorMapIndex, _idScaleMin, _idScaleMax;
    private int _idFoveationStart, _idFoveationEnd, _idFoveationJitter, _idFoveatedStepsLow, _idFoveatedStepsHigh;
    private int _idVignetteFadeStart, _idVignetteFadeEnd, _idVignetteIntensity, _idVignetteColor, _idScreenWidth, _idScreenHeight;

    private void GetPropertyIds()
    {
        _idSliceMin = Shader.PropertyToID("_SliceMin");
        _idSliceMax = Shader.PropertyToID("_SliceMax");
        _idThresholdMin = Shader.PropertyToID("_ThresholdMin");
        _idThresholdMax = Shader.PropertyToID("_ThresholdMax");
        _idJitter = Shader.PropertyToID("_Jitter");
        _idMaxSteps = Shader.PropertyToID("_MaxSteps");
        _idColorMapIndex = Shader.PropertyToID("_ColorMapIndex");
        _idScaleMin = Shader.PropertyToID("_ScaleMin");
        _idScaleMax = Shader.PropertyToID("_ScaleMax");

        _idFoveationStart = Shader.PropertyToID("FoveationStart");
        _idFoveationEnd = Shader.PropertyToID("FoveationEnd");
        _idFoveationJitter = Shader.PropertyToID("FoveationJitter");
        _idFoveatedStepsLow = Shader.PropertyToID("FoveatedStepsLow");
        _idFoveatedStepsHigh = Shader.PropertyToID("FoveatedStepsHigh");
        
        _idVignetteFadeStart = Shader.PropertyToID("VignetteFadeStart");
        _idVignetteFadeEnd = Shader.PropertyToID("VignetteFadeEnd");
        _idVignetteIntensity = Shader.PropertyToID("VignetteIntensity");
        _idVignetteColor = Shader.PropertyToID("VignetteIntensity");
        _idScreenWidth = Shader.PropertyToID("ScreenWidth");
        _idScreenHeight = Shader.PropertyToID("ScreenHeight");
    }
    
    void Start()
    {        
        LoadFits(FileName);
        FindMinAndMax();
        GetPropertyIds();
        _renderer = GetComponent<MeshRenderer>();
        _materialInstance = Instantiate(RayMarchingMaterial);
        _materialInstance.SetTexture("_DataCube", DataCube);
        _materialInstance.SetInt("_NumColorMaps", ColorMapUtils.NumColorMaps);
        _materialInstance.SetFloat(_idFoveationStart, FoveationStart);
        _materialInstance.SetFloat(_idFoveationEnd, FoveationEnd);
        _renderer.material = _materialInstance;                
    }

    public void ShiftColorMap(int delta)
    {
        int numColorMaps = ColorMapUtils.NumColorMaps;
        int currentIndex = ColorMap.GetHashCode();
        int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;       
        ColorMap = ColorMapUtils.FromHashCode(newIndex);
    }

    // Update is called once per frame
    public void Update()
    {
        _materialInstance.SetVector(_idSliceMin, SliceMin);
        _materialInstance.SetVector(_idSliceMax, SliceMax);
        _materialInstance.SetFloat(_idThresholdMin, ThresholdMin);
        _materialInstance.SetFloat(_idThresholdMax, ThresholdMax);
        _materialInstance.SetFloat(_idJitter, Jitter);
        _materialInstance.SetFloat(_idMaxSteps, MaxSteps);
        _materialInstance.SetFloat(_idColorMapIndex, ColorMap.GetHashCode());
        _materialInstance.SetFloat(_idScaleMax, ScaleMax);
        _materialInstance.SetFloat(_idScaleMin, ScaleMin);

        _materialInstance.SetFloat(_idFoveationStart, FoveationStart);
        _materialInstance.SetFloat(_idFoveationEnd, FoveationEnd);        
        if (FoveatedRendering)
        {
            _materialInstance.SetFloat(_idFoveationJitter, FoveationJitter);
            _materialInstance.SetInt(_idFoveatedStepsLow, FoveatedStepsLow);
            _materialInstance.SetInt(_idFoveatedStepsHigh, FoveatedStepsHigh);
        }
        else
        {
            _materialInstance.SetInt(_idFoveatedStepsLow, MaxSteps);
            _materialInstance.SetInt(_idFoveatedStepsHigh, MaxSteps);
        }
        
        _materialInstance.SetFloat(_idVignetteFadeStart, VignetteFadeStart);
        _materialInstance.SetFloat(_idVignetteFadeEnd, VignetteFadeEnd);
        _materialInstance.SetFloat(_idVignetteIntensity, VignetteIntensity);
        _materialInstance.SetColor(_idVignetteColor, VignetteColor);
        if (Camera.current != null)
        {
            _materialInstance.SetFloat(_idScreenWidth, Camera.current.scaledPixelWidth);
            _materialInstance.SetFloat(_idScreenHeight, Camera.current.scaledPixelHeight);            
        }        
    }

    public void LoadFits(string fileName)
    {
        IntPtr fptr;
        int status = 0;
        int cubeDimensions;
        IntPtr dataPtr;
        if (FitsReader.FitsOpenFile(out fptr, fileName, out status) != 0)
        {
            Debug.Log("Fits open failure... code #" + status.ToString());
        }
        if (FitsReader.FitsGetImageDims(fptr, out cubeDimensions, out status) != 0)
        {
            Debug.Log("Fits read image dimensions failed... code #" + status.ToString());
        }
        if (cubeDimensions < 3)
        {
            Debug.Log("Only " + cubeDimensions.ToString() + " found. Please use Fits cube with at least 3 dimensions.");
        }
        if (FitsReader.FitsGetImageSize(fptr, cubeDimensions, out dataPtr, out status) != 0)
        {
            Debug.Log("Fits Read cube size error #" + status.ToString());
            FitsReader.FitsCloseFile(fptr, out status);
        }
        int[] cubeSize = new int[cubeDimensions];
        Marshal.Copy(dataPtr, cubeSize, 0, cubeDimensions);
        FitsReader.FreeMemory(dataPtr);
        long numberDataPoints = cubeSize[0] * cubeSize[1] * cubeSize[2];
        IntPtr fitsDataPtr;
        if (FitsReader.FitsReadImageFloat(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
        {
            Debug.Log("Fits Read cube data error #" + status.ToString());
            FitsReader.FitsCloseFile(fptr, out status);
        }
        Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], TextureFormat.RFloat, false);
        int sliceSize = cubeSize[0] * cubeSize[1];
        Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], TextureFormat.RFloat, false);
        for (int slice = 0; slice < cubeSize[2]; slice++)
        {
            textureSlice.LoadRawTextureData(IntPtr.Add(fitsDataPtr,slice * sliceSize * sizeof(float)), sliceSize * sizeof(float));
            textureSlice.Apply();
            Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize[0], cubeSize[1], dataCube, slice, 0, 0, 0);
        }
        DataCube = dataCube;
        _fitsCubeData = fitsDataPtr;
        _numberDataPts = numberDataPoints;
    }


    public void FindMinAndMax()
    { 
        DataAnalysis.FindMaxMin(_fitsCubeData, _numberDataPts, out ScaleMax, out ScaleMin);
        Debug.Log("max and min vals: " + ScaleMax + " and " + ScaleMin);
    }
}