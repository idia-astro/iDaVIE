using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using fts;
using UnityEngine;

[PluginAttr("libdata_analysis_tool")]
public static class DataAnalysis
{
    [PluginFunctionAttr("FindMaxMin")] 
    public static readonly FindMaxMinDelegate FindMaxMin = null;
    public delegate int FindMaxMinDelegate(IntPtr dataPtr, long numberElements, out float maxResult, out float minResult);

    [PluginFunctionAttr("FindStats")] 
    public static readonly FindStatsDelegate FindStats = null;
    public delegate int FindStatsDelegate(IntPtr dataPtr, long numberElements, out float maxResult, out float minResult, out float meanResult, out float stdDevResult);

    [PluginFunctionAttr("DataCropAndDownsample")]
    public static readonly DataCropAndDownsampleDelegate DataCropAndDownsample = null;
    public delegate int DataCropAndDownsampleDelegate(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, long cropX1, long cropY1, long cropZ1, 
       long cropX2, long cropY2, long cropZ2, int factorX, int factorY, int factorZ);

    [PluginFunctionAttr("MaskCropAndDownsample")]
    public static readonly MaskCropAndDownsampleDelegate MaskCropAndDownsample = null;
    public delegate int MaskCropAndDownsampleDelegate(IntPtr dataPtr, out IntPtr newDataPtr, long dimX, long dimY, long dimZ, long cropX1, long cropY1, long cropZ1,
   long cropX2, long cropY2, long cropZ2, int factorX, int factorY, int factorZ);

    [PluginFunctionAttr("GetVoxelFloatValue")] 
    public static readonly GetVoxelFloatValueDelegate GetVoxelFloatValue = null;
    public delegate int GetVoxelFloatValueDelegate(IntPtr dataPtr, out float voxelValue, long dimX, long dimY, long dimZ, long x, long y, long z);

    [PluginFunctionAttr("GetVoxelInt16Value")]
    public static readonly GetVoxelInt16ValueDelegate GetVoxelInt16Value = null;
    public delegate int GetVoxelInt16ValueDelegate(IntPtr dataPtr, out Int16 voxelValue, long dimX, long dimY, long dimZ, long x, long y, long z);

    [PluginFunctionAttr("GetXProfile")]
    public static readonly GetXProfileDelegate GetXProfile = null;
    public delegate int GetXProfileDelegate(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long y, long z);

    [PluginFunctionAttr("GetYProfile")] 
    public static readonly GetYProfileDelegate GetYProfile = null;
    public delegate int GetYProfileDelegate(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long z);

    [PluginFunctionAttr("GetZProfile")]
    public static readonly GetZProfileDelegate GetZProfile = null;
    public delegate int GetZProfileDelegate(IntPtr dataPtr, out IntPtr profile, long dimX, long dimY, long dimZ, long x, long y);

    [PluginFunctionAttr("GetHistogram")] 
    public static readonly GetHistogramDelegate GetHistogram = null;
    public delegate int GetHistogramDelegate(IntPtr dataPtr, long numElements, int numBins, float minVal, float maxVal, out IntPtr histogram);
    
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct SourceInfo
    {
        public long minX, maxX;
        public long minY, maxY;
        public long minZ, maxZ;
        public short maskVal;
        //char _padding[14];
        
        public static SourceInfo FromSourceStats(SourceStats sourceStats, short maskVal)
        {
            var sourceInfo = new SourceInfo()
            {
                minX = sourceStats.minX, maxX = sourceStats.maxX, 
                minY = sourceStats.minY, maxY = sourceStats.maxY,
                minZ = sourceStats.minZ, maxZ = sourceStats.maxZ,
                maskVal = maskVal
            };
            
            return sourceInfo;
        }
    };
    
    [StructLayout(LayoutKind.Sequential, Pack=8)]
    public struct SourceStats
    {
        // Bounding box
        public long minX, maxX;
        public long minY, maxY;
        public long minZ, maxZ;
        // Number of finite voxels
        public long numVoxels;
        // Centroid
        public double cX, cY, cZ;
        // Flux
        public double sum;
        public double peak;
        // Vsys (in channel units)
        public double channelVsys;
        public double channelW20;

        public bool IsEmpty => numVoxels == 0;
        public void AddPointToBoundingBox(long x, long y, long z)
        {
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
            minZ = Math.Min(minZ, z);
            maxZ = Math.Max(maxZ, z);
        }

        public static SourceStats FromPoint(long x, long y, long z)
        {
            var sourceStats = new SourceStats()
            {
                minX = x, maxX = x,
                minY = y, maxY = y,
                minZ = z, maxZ = z 
            };
            
            return sourceStats;
        }

        public static SourceStats FromSourceInfo(SourceInfo sourceInfo)
        {
            var sourceStats = new SourceStats()
            {
                minX = sourceInfo.minX, maxX = sourceInfo.maxX, 
                minY = sourceInfo.minY, maxY = sourceInfo.maxY,
                minZ = sourceInfo.minZ, maxZ = sourceInfo.maxZ 
            };
            
            return sourceStats;
        }
    };

    [PluginFunctionAttr("GetMaskedSources")]
    public static readonly GetMaskedSourcesDelegate GetMaskedSources = null;
    public delegate int GetMaskedSourcesDelegate(IntPtr maskDataPtr, long dimX, long dimY, long dimZ, out int maskCount, out IntPtr sources);
    
    [PluginFunctionAttr("GetSourceStats")] 
    public static readonly GetSourceStatsDelegate GetSourceStats = null;
    public delegate int GetSourceStatsDelegate(IntPtr dataPtr, IntPtr maskDataPtr, long dimX, long dimY, long dimZ, SourceInfo source, ref SourceStats stats);

    [PluginFunctionAttr("GetZScale")] 
    public static readonly GetZScaleDelegate GetZScale = null;
    public unsafe delegate int GetZScaleDelegate(void* dataPtr, long width, long height, out float z1, out float z2);
    
    
    [PluginFunctionAttr("FreeMemory")] 
    public static readonly FreeMemoryDelegate FreeMemory = null;
    public delegate int FreeMemoryDelegate(IntPtr pointerToDelete);

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

    public static unsafe List<SourceInfo> GetMaskedSourceArray(IntPtr maskDataPtr, long dimX, long dimY, long dimZ)
    {
        int maskCount;
        IntPtr sourcesPtr;
        List<SourceInfo> sources = new List<SourceInfo>();
        
        if (GetMaskedSources(maskDataPtr, dimX, dimY, dimZ, out maskCount, out sourcesPtr) != 0)
        {
            Debug.Log("Error extracting sources");
            return sources;
        }
        try
        {
            for (var i = 0; i < maskCount; i++)
            {
                SourceInfo s = Marshal.PtrToStructure<SourceInfo>(IntPtr.Add(sourcesPtr, sizeof(SourceInfo)* i));
                sources.Add(s);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        FreeMemory(sourcesPtr);
        sources.Sort((s1, s2) => s1.maskVal - s2.maskVal);
        return sources;
    }
}