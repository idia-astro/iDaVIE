using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DataAnalysis
{
    [DllImport("data_analysis_tool")]
    public static extern int FindMaxMin(IntPtr dataPtr, long numberElements, out float maxResult, out float minResult);

    //[DllImport("data_analysis_tool")]
    //public static extern int NearNeighborScale(IntPtr dataPtr, out IntPtr newDataPtr, int dimX, int dimY, int dimZ, int windowX, int windowY, int windowZ);

    [DllImport("data_analysis_tool")]
    public static extern int GetVoxelValue(IntPtr dataPtr, out float voxelValue, int dimX, int dimY, int dimZ, int x, int y, int z);

    [DllImport("data_analysis_tool")]
    public static extern int GetXProfile(IntPtr dataPtr, out IntPtr profile, int dimX, int dimY, int dimZ, int y, int z);

    [DllImport("data_analysis_tool")]
    public static extern int GetYProfile(IntPtr dataPtr, out IntPtr profile, int dimX, int dimY, int dimZ, int x, int z);

    [DllImport("data_analysis_tool")]
    public static extern int GetZProfile(IntPtr dataPtr, out IntPtr profile, int dimX, int dimY, int dimZ, int x, int y);
}
