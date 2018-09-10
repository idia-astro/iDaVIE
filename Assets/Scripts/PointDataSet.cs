using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PointDataSet : MonoBehaviour
{
    // @formatter:off
    public enum ColorMapEnum
    {
        accent, afmhot, autumn, binary, Blues, bone, BrBG, brg, BuGn, BuPu, bwr, CMRmap, cool, coolwarm,
        copper, cubehelix, dark2, flag, gist_earth, gist_gray, gist_heat, gist_ncar, gist_rainbow, gist_stern, gist_yarg,
        GnBu, gnuplot, gnuplot2, gray, greens, greys, hot, hsv, inferno, jet, magma, nipy_spectral, ocean, oranges,
        OrRd, paired, pastel1, pastel2, pink, PiYG, plasma, PRGn, prism, PuBu, PuBuGn, PuOr, PuRd, purples, rainbow,
        RdBu, RdGy, RdPu, RdYlBu, RdYlGn, reds, seismic, set1, set2, set3, spectral, spring, summer, tab10, tab20,
        tab20b, tab20c, terrain, viridis, winter, Wistia, YlGn, YlGnBu, YlOrBr, YlOrRd
    }
    // @formatter:on

    public int particleCount = 10000;
    public int seed;
    public ColorMapEnum colorMap = ColorMapEnum.inferno;
    public bool debugPlaceholder;
    public Texture2D colorMapTexture;
    public Material material;

    private ComputeBuffer[] buffers;
    private float[][] columns;
    
    private Color[] colorMapData;
    private const int NumColorMapStops = 256;
    private const int NumColumns = 5;
  
    void Start()
    {
        Random.InitState(seed);
        particleCount = Math.Max(particleCount, 1);

        // Generate random points in a sphere, with random values correlated with radial value
        columns = new float[NumColumns][];
        for (var i = 0; i < NumColumns; i++)
        {
            columns[i] = new float[particleCount];
        }
        
        for (var i = 0; i < particleCount; ++i)
        {
            Vector3 position = Random.insideUnitSphere * 0.5f * (1.0f + Random.value * 0.5f);
            columns[0][i] = position.x;
            columns[1][i] = position.y;
            columns[2][i] = position.z;
            columns[3][i] = position.magnitude * 2 + (Random.value - 0.5f) * 0.5f;
            columns[4][i] = 1.0f - position.magnitude * 2 + (Random.value - 0.5f) * 0.5f;
        }
        
        buffers = new ComputeBuffer[NumColumns];
        for (var i = 0; i < NumColumns; i++)
        {
            buffers[i] = new ComputeBuffer(particleCount, 4);
            buffers[i].SetData(columns[i]);
        }

        // Create an instance of the material, so that each data set can have different material parameters
        material = Instantiate(material);
        material.SetBuffer("dataX", buffers[0]);
        material.SetBuffer("dataY", buffers[1]);
        material.SetBuffer("dataZ", buffers[2]);
        material.SetBuffer("dataVal", buffers[3]);
        material.SetInt("numDataPoints", particleCount);

        colorMapData = new Color[NumColorMapStops];
        SetColorMap(colorMap);
    }

    // The color map array is calculated from the color map texture and sent to the GPU whenever the color map is changed
    public void SetColorMap(ColorMapEnum newColorMap)
    {
        colorMap = newColorMap;
        int numColorMaps = Enum.GetNames(typeof(ColorMapEnum)).Length;
        float colorMapPixelDeltaX = (float) (colorMapTexture.width) / NumColorMapStops;
        float colorMapPixelDeltaY = (float) (colorMapTexture.height) / numColorMaps;
        int colorMapIndex = newColorMap.GetHashCode();

        for (var i = 0; i < NumColorMapStops; i++)
        {
            colorMapData[i] = colorMapTexture.GetPixel((int) (i * colorMapPixelDeltaX), (int) (colorMapIndex * colorMapPixelDeltaY));
        }

        material.SetColorArray("colorMapData", colorMapData);
    }

    public void ShiftColorMap(int delta)
    {
        int numColorMaps = Enum.GetNames(typeof(ColorMapEnum)).Length;
        int currentIndex = colorMap.GetHashCode();
        int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
        SetColorMap(FromHashCode(newIndex));
    }

    private static ColorMapEnum FromHashCode(int hashCode)
    {
        foreach (var colorMap in Enum.GetValues(typeof(ColorMapEnum)))
        {
            if (colorMap.GetHashCode() == hashCode)
            {
                return (ColorMapEnum) colorMap;
            }
        }

        // If we can't find a color map, return the default one
        return ColorMapEnum.accent;
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
        material.SetMatrix("datasetMatrix", transform.localToWorldMatrix);
        material.SetFloat("pointScale", transform.localScale.x);
        material.SetInt("scalingTypeX", 0);
        material.SetInt("scalingTypeY", 0);
        material.SetInt("scalingTypeZ", 0);
        material.SetInt("scalingTypeColorMap", 0);
        material.SetPass(0);
        // Render points on the GPU using vertex pulling
        Graphics.DrawProcedural(MeshTopology.Points, particleCount);
    }

    void OnDestroy()
    {
        foreach (var buffer in buffers)
        {
            if (buffer != null)
            {
                buffer.Release();                
            }
        }
    }
}