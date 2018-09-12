using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PointDataSet : MonoBehaviour
{
    public int ParticleCount = 10000;
    public int Seed;
    public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
    public Texture2D ColorMapTexture;
    public Material BillboardMaterial;

    private ComputeBuffer[] _buffers;
    private float[][] _columns;

    private Color[] _colorMapData;
    private const int NumColorMapStops = 256;
    private const int NumColumns = 5;

    void Start()
    {
        Random.InitState(Seed);
        ParticleCount = Math.Max(ParticleCount, 1);

        // Generate random points in a sphere, with random values correlated with radial value
        _columns = new float[NumColumns][];
        for (var i = 0; i < NumColumns; i++)
        {
            _columns[i] = new float[ParticleCount];
        }

        for (var i = 0; i < ParticleCount; ++i)
        {
            Vector3 position = Random.insideUnitSphere * 0.5f * (1.0f + Random.value * 0.5f);
            _columns[0][i] = position.x;
            _columns[1][i] = position.y;
            _columns[2][i] = position.z;
            _columns[3][i] = position.magnitude * 2 + (Random.value - 0.5f) * 0.5f;
            _columns[4][i] = 1.0f - position.magnitude * 2 + (Random.value - 0.5f) * 0.5f;
        }

        _buffers = new ComputeBuffer[NumColumns];
        for (var i = 0; i < NumColumns; i++)
        {
            _buffers[i] = new ComputeBuffer(ParticleCount, 4);
            _buffers[i].SetData(_columns[i]);
        }

        // Create an instance of the material, so that each data set can have different material parameters
        BillboardMaterial = Instantiate(BillboardMaterial);
        BillboardMaterial.SetBuffer("dataX", _buffers[0]);
        BillboardMaterial.SetBuffer("dataY", _buffers[1]);
        BillboardMaterial.SetBuffer("dataZ", _buffers[2]);
        BillboardMaterial.SetBuffer("dataVal", _buffers[3]);
        BillboardMaterial.SetInt("numDataPoints", ParticleCount);

        _colorMapData = new Color[NumColorMapStops];
        SetColorMap(ColorMap);
    }

    // The color map array is calculated from the color map texture and sent to the GPU whenever the color map is changed
    public void SetColorMap(ColorMapEnum newColorMap)
    {
        ColorMap = newColorMap;
        int numColorMaps = ColorMapUtils.NumColorMaps;
        float colorMapPixelDeltaX = (float) (ColorMapTexture.width) / NumColorMapStops;
        float colorMapPixelDeltaY = (float) (ColorMapTexture.height) / numColorMaps;
        int colorMapIndex = newColorMap.GetHashCode();

        for (var i = 0; i < NumColorMapStops; i++)
        {
            _colorMapData[i] = ColorMapTexture.GetPixel((int) (i * colorMapPixelDeltaX), (int) (colorMapIndex * colorMapPixelDeltaY));
        }

        BillboardMaterial.SetColorArray("colorMapData", _colorMapData);
    }

    public void ShiftColorMap(int delta)
    {
        int numColorMaps = ColorMapUtils.NumColorMaps;
        int currentIndex = ColorMap.GetHashCode();
        int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
        SetColorMap(ColorMapUtils.FromHashCode(newIndex));
    }


    void Update()
    {
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5F);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }


    void OnRenderObject()
    {
        // Update the object transform and point scale on the GPU
        BillboardMaterial.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
        BillboardMaterial.SetFloat("pointScale", transform.localScale.x);
        BillboardMaterial.SetInt("scalingTypeX", 0);
        BillboardMaterial.SetInt("scalingTypeY", 0);
        BillboardMaterial.SetInt("scalingTypeZ", 0);
        BillboardMaterial.SetInt("scalingTypeColorMap", 0);
        BillboardMaterial.SetPass(0);
        // Render points on the GPU using vertex pulling
        Graphics.DrawProcedural(MeshTopology.Points, ParticleCount);
    }

    void OnDestroy()
    {
        foreach (var buffer in _buffers)
        {
            if (buffer != null)
            {
                buffer.Release();
            }
        }
    }
}