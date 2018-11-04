using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

//[ExecuteInEditMode]
public class VolumeDataSet : MonoBehaviour
{
    // Step control
    [Range(16, 512)] public int MaxSteps = 192;

    // Jitter factor
    [Range(0, 1)] public float Jitter = 1.0f;

    // Spatial thresholding
    public Vector3 SliceMin = Vector3.zero;
    public Vector3 SliceMax = Vector3.one;

    // Value thresholding
    [Range(0, 1)] public float ThresholdMin = 0;
    [Range(0, 1)] public float ThresholdMax = 1;
    
    public Material RayMarchingMaterial;
    public string FileName;
    public Texture3D DataCube;
    public ColorMapEnum ColorMap = ColorMapEnum.Inferno;

    //private Texture3D _dataCube;
    private MeshRenderer _renderer;
    private Material _materialInstance;

    // Material property IDs
    private int _idSliceMin, _idSliceMax, _idThresholdMin, _idThresholdMax, _idJitter, _idMaxSteps, _idColorMapIndex;
    
    void Start()
    {
        DataCube = LoadFitsAsTexture3D(FileName);
        _renderer = GetComponent<MeshRenderer>();
        _materialInstance = Instantiate(RayMarchingMaterial);
        _materialInstance.SetTexture("_DataCube", DataCube);
        _materialInstance.SetInt("_NumColorMaps", ColorMapUtils.NumColorMaps);
        _renderer.material = _materialInstance;
        _idSliceMin = Shader.PropertyToID("_SliceMin");
        _idSliceMax = Shader.PropertyToID("_SliceMax");
        _idThresholdMin = Shader.PropertyToID("_ThresholdMin");
        _idThresholdMax = Shader.PropertyToID("_ThresholdMax");
        _idJitter = Shader.PropertyToID("_Jitter");
        _idMaxSteps = Shader.PropertyToID("_MaxSteps");
        _idColorMapIndex = Shader.PropertyToID("_ColorMapIndex");
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
    }
    
    public static Texture3D LoadFitsAsTexture3D(string fileName)
    {
        IntPtr fptr;
        int status = 0;
        int cubeDimensions;
        IntPtr dataPtr;
        IntPtr[] ptrCubeSize = new IntPtr[3];
        //need to use int, not long for some reason??
        int[] cubeSize = new int[3];


        if (FitsReader.FitsOpenFile(out fptr, fileName, out status) != 0)
        {
            Debug.Log("Fits open failure... code #" + status.ToString());
            return null;
        }

        //check if 3 dims..
        if (FitsReader.FitsGetImageDims(fptr, out cubeDimensions, out status) != 0)
        {
            Debug.Log("Fits read image dimensions failed... code #" + status.ToString());
            return null;
        }

        if (cubeDimensions != 3)
        {
            Debug.Log("Only " + cubeDimensions.ToString() + " found. Please use Fits cube with 3 dimensions.");
            return null;
        }
        
        if (FitsReader.FitsGetImageSize(fptr, out dataPtr, out status) != 0)
        {
            Debug.Log("Fits Read cube size error #" + status.ToString());
            FitsReader.FitsCloseFile(fptr, out status);
            return null;
        }
        Marshal.Copy(dataPtr, cubeSize, 0, 3);
        //for (int i = 0; i < 1; i++)
        //    cubeSize[i] = Marshal.ReadInt64(ptrCubeSize[i]);
       
        Debug.Log("Number of data points #" + cubeSize[2]);
        FitsReader.FreeMemory(dataPtr);
        long numberDataPoints = cubeSize[0] * cubeSize[1] * cubeSize[2];

        Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], TextureFormat.RFloat, false);
        Color[] colorArray = new Color[numberDataPoints];
        IntPtr fitsDataPtr;
        if (FitsReader.FitsRead3DFloat(fptr, cubeSize[0], cubeSize[1], cubeSize[2], out fitsDataPtr, out status) != 0)
        {
            Debug.Log("Fits Read cube data error #" + status.ToString());
            FitsReader.FitsCloseFile(fptr, out status);
            return null;
        }
        float[] fitsCubeData = new float[numberDataPoints];
        
        Marshal.Copy(fitsDataPtr, fitsCubeData, 0, (int)numberDataPoints);
        FitsReader.FreeMemory(fitsDataPtr);
        float maxPixValue = fitsCubeData.Max();
    
        float minPixValue = fitsCubeData.Min();
        Debug.Log("max and min vals: " + maxPixValue + " and " + minPixValue);
        for (int i = 0; i < numberDataPoints; i++)
        {
            if (float.IsNaN(fitsCubeData[i]))
                colorArray[i].r = 0;
            else
                colorArray[i].r = (fitsCubeData[i])/maxPixValue;
            if (i> 4990 && i < 5000)
                Debug.Log("Fits color info: " + colorArray[i].r.ToString() + " and " + fitsCubeData[i].ToString());
        }
        dataCube.SetPixels(colorArray);
        dataCube.Apply();

        return dataCube;

    }
    
}