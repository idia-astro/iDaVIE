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
    public static extern int FindStats(IntPtr dataPtr, long numberElements, out float maxResult, out float minResult, out float meanResult, out float stdDevResult);

    [DllImport("data_analysis_tool")]
    public static extern int DataCropAndDownsample(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, long cropX1, long cropY1, long cropZ1, 
       long cropX2, long cropY2, long cropZ2, int factorX, int factorY, int factorZ);

    [DllImport("data_analysis_tool")]
    public static extern int MaskCropAndDownsample(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, long cropX1, long cropY1, long cropZ1,
   long cropX2, long cropY2, long cropZ2, int factorX, int factorY, int factorZ);

    [DllImport("data_analysis_tool")]
    public static extern int GetVoxelFloatValue(IntPtr dataPtr, out float voxelValue, long dimX, long dimY, long dimZ, long x, long y, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetVoxelInt16Value(IntPtr dataPtr, out Int16 voxelValue, long dimX, long dimY, long dimZ, long x, long y, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetXProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long y, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetYProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long z);

    [DllImport("data_analysis_tool")]
    public static extern int GetZProfile(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long y);

    [DllImport("data_analysis_tool")]
    public static extern int GetHistogram(IntPtr dataPtr, long numElements, int numBins, float minVal, float maxVal, out IntPtr histogram);

    [DllImport("data_analysis_tool")]
    public static extern int FreeMemory(IntPtr pointerToDelete);


    public static float[] GetXProfileAsArray(IntPtr dataPtr, long dimX, long dimY, long dimZ, long y, long z)
    {
        float[] profile = new float[dimX];
        IntPtr profilePtr = IntPtr.Zero;
        if (GetXProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, y, z) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimX);
        if (profilePtr != IntPtr.Zero)
            FreeMemory(profilePtr);
        return profile;
    }

    public static float[] GetYProfileAsArray(IntPtr dataPtr, long dimX, long dimY, long dimZ, long x, long z)
    {
        float[] profile = new float[dimY];
        IntPtr profilePtr = IntPtr.Zero;
        if (GetYProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, x, z) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimY);
        if (profilePtr != IntPtr.Zero)
            FreeMemory(profilePtr);
        return profile;
    }

    public static float[] GetZProfileAsArray(IntPtr dataPtr, int dimX, int dimY, int dimZ, int x, int y)
    {
        float[] profile = new float[dimZ];
        IntPtr profilePtr = IntPtr.Zero;
        if (GetZProfile(dataPtr, out profilePtr, dimX, dimY, dimZ, x, y) != 0)
        {
            Debug.Log("Error finding profile");
            return profile;
        }
        Marshal.Copy(profilePtr, profile, 0, (int)dimZ);
        if (profilePtr != IntPtr.Zero)
            FreeMemory(profilePtr);
        return profile;
    }
}
