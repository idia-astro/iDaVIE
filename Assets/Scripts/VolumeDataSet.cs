using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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
    public Texture3D DataCube;
    public ColorMapEnum ColorMap = ColorMapEnum.Inferno;

    private MeshRenderer _renderer;
    private Material _materialInstance;

    // Material property IDs
    private int _idSliceMin, _idSliceMax, _idThresholdMin, _idThresholdMax, _idJitter, _idMaxSteps, _idColorMapIndex;
    
    void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _materialInstance = Instantiate(RayMarchingMaterial);
        _materialInstance.SetTexture("DataCube", DataCube);
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
        long nrows;
        int ncols;
        

        if (FitsReader.FitsOpenFile(out fptr, fileName, out status) != 0)
        {
            Debug.Log("Fits Failure... cfits code #" + status.ToString());
            return null;
        }

        //check if 3 dims..

        if (FitsReader.FitsGetNumRows(fptr, out nrows, out status) != 0 || FitsReader.FitsGetNumCols(fptr, out ncols, out status) != 0)
        {
            Debug.Log("Fits Read table size error #" + status.ToString());
            FitsReader.FitsCloseFile(fptr, out status);
            return null;
        }
        Texture3D dataCube = new Texture3D(0, 0, 0, TextureFormat.Alpha8, false);

        return dataCube;

    }
    
}