using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DataAnalysis
{
    [DllImport("data_analysis_tool")]
    public static extern int FindMaxMin(IntPtr dataPtr, long numberElements, out float maxResult, out float minResult);

    [DllImport("data_analysis_tool")]
    public static extern int DataDownsampleByFactor(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, int factorX, int factorY, int factorZ);

    [DllImport("data_analysis_tool")]
    public static extern int DataCropAndDownsample(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, long cropX1, long cropY1, long cropZ1, 
       long cropX2, long cropY2, long cropZ2, int factorX, int factorY, int factorZ);

    [DllImport("data_analysis_tool")]
    public static extern int GetVoxelValue(IntPtr dataPtr, out float voxelValue, long dimX, long dimY, long dimZ, long x, long y, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetXProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long y, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetYProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetZProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long y);

    [DllImport("data_analysis_tool")]
    public static extern int FreeMemory(IntPtr pointerToDelete);


    public static float[] GetXProfileAsArray(IntPtr dataPtr, long dimX, long dimY, long dimZ, long y, long z)
    {
        float[] profile = new float[dimX];
        IntPtr profilePtr;
        if (GetXProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, y, z) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimX);
        return profile;
    }

    public static float[] GetYProfileAsArray(IntPtr dataPtr, long dimX, long dimY, long dimZ, long x, long z)
    {
        float[] profile = new float[dimY];
        IntPtr profilePtr;
        if (GetYProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, x, z) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimY);
        return profile;
    }

    public static float[] GetZProfileAsArray(IntPtr dataPtr, int dimX, int dimY, int dimZ, int x, int y)
    {
        float[] profile = new float[dimZ];
        IntPtr profilePtr;
        if (GetZProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, x, y) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimZ);
        return profile;
    }
}
