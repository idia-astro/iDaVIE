﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace VolumeData
{
    public struct VoxelEntry
    {
        public readonly int Index;
        public readonly int Value;

        public VoxelEntry(int index, int value)
        {
            this.Index = index;
            this.Value = value;
        }

        public static readonly Comparer<VoxelEntry> IndexComparer = Comparer<VoxelEntry>.Create(
            (a, b) => a.Index - b.Index
        );
    }

    public struct BrushStrokeTransaction
    {
        public int NewValue;
        public List<VoxelEntry> Voxels;

        public BrushStrokeTransaction(int newValue)
        {
            NewValue = newValue;
            Voxels = new List<VoxelEntry>();
        }
    }

    public class VolumeDataSet
    {
        public Texture3D DataCube { get; private set; }
        public Texture3D RegionCube { get; private set; }
        public ComputeBuffer ExistingMaskBuffer { get; private set; }
        public ComputeBuffer AddedMaskBuffer { get; private set; }
        public int AddedMaskEntryCount { get; private set; }
        public BrushStrokeTransaction CurrentBrushStroke { get; private set; }
        public List<BrushStrokeTransaction> BrushStrokeHistory { get; private set; }

        public string FileName { get; private set; }
        public long XDim { get; private set; }
        public long YDim { get; private set; }
        public long ZDim { get; private set; }
        public int XDimDecimal { get; private set; }
        public int YDimDecimal { get; private set; }
        public int ZDimDecimal { get; private set; }
        private IDictionary<string, string> HeaderDictionary;

        public int VelDecimal { get; private set; }

        public long NumPoints => XDim * YDim * ZDim;
        public long[] Dims => new[] {XDim, YDim, ZDim};

        public Vector3Int RegionOffset { get; private set; }


        public bool IsMask { get; private set; }

        private IDictionary<string, string> _headerDictionary;

        private double _xRef, _yRef, _zRef, _xRefPix, _yRefPix, _zRefPix, _xDelt, _yDelt, _zDelt, _rot;
        private string _xCoord, _yCoord, _zCoord, _wcsProj;

        public double NAxis;
        private List<VoxelEntry> _existingRegionMaskEntries;
        private List<VoxelEntry> _addedRegionMaskEntries;
        private Texture2D _updateTexture;
        private byte[] _cachedBrush;
        private short[] _regionMaskVoxels;
        private static int BrushStrokeLimit = 25000;

        public IntPtr FitsData = IntPtr.Zero;
        public IntPtr FitsHeader = IntPtr.Zero;
        public int NumberHeaderKeys;
        public IntPtr AstFrameSet = IntPtr.Zero;
        
        public long[] cubeSize;

        public int[] Histogram;
        public float HistogramBinWidth;
        public float MaxValue;
        public float MinValue;
        public float MeanValue;
        public float StanDev;

        public string PixelUnit = "units";

        public static VolumeDataSet LoadRandomFitsCube(float min, float max, int xDim, int yDim, int zDim)
        {
            VolumeDataSet volumeDataSet = new VolumeDataSet();
            long numberDataPoints = xDim * yDim * zDim;
            IntPtr dataPtr = IntPtr.Zero;
            dataPtr = Marshal.AllocHGlobal(sizeof(float) * (int) numberDataPoints);
            float[] generatedData = new float[numberDataPoints];
            for (int i = 0; i < numberDataPoints; i++)
            {
                generatedData[i] = UnityEngine.Random.Range(min, max);
            }

            Marshal.Copy(generatedData, 0, dataPtr, (int) numberDataPoints);
            volumeDataSet.FitsData = dataPtr;
            volumeDataSet.XDim = xDim;
            volumeDataSet.YDim = yDim;
            volumeDataSet.ZDim = zDim;
            volumeDataSet.XDimDecimal = xDim.ToString().Length;
            volumeDataSet.YDimDecimal = yDim.ToString().Length;
            volumeDataSet.ZDimDecimal = zDim.ToString().Length;
            DataAnalysis.FindStats(dataPtr, numberDataPoints, out volumeDataSet.MaxValue, out volumeDataSet.MinValue, out volumeDataSet.MeanValue,
                out volumeDataSet.StanDev);
            int histogramSize = Mathf.RoundToInt(Mathf.Sqrt(numberDataPoints));
            volumeDataSet.Histogram = new int[histogramSize];
            IntPtr histogramPtr = IntPtr.Zero;
            volumeDataSet.HistogramBinWidth = (volumeDataSet.MaxValue - volumeDataSet.MinValue) / histogramSize;
            DataAnalysis.GetHistogram(dataPtr, numberDataPoints, histogramSize, volumeDataSet.MinValue, volumeDataSet.MaxValue, out histogramPtr);
            Marshal.Copy(histogramPtr, volumeDataSet.Histogram, 0, histogramSize);
            if (histogramPtr != IntPtr.Zero)
                DataAnalysis.FreeMemory(histogramPtr);
            return volumeDataSet;
        }

        public static VolumeDataSet LoadDataFromFitsFile(string fileName, bool isMask, int index2 = 2, int sliceDim = 1)
        {
            VolumeDataSet volumeDataSet = new VolumeDataSet();
            volumeDataSet.IsMask = isMask;
            volumeDataSet.FileName = fileName;
            IntPtr fptr = IntPtr.Zero;
            int status = 0;
            int cubeDimensions;
            IntPtr dataPtr = IntPtr.Zero;
            if (FitsReader.FitsOpenFile(out fptr, fileName, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }
            if (FitsReader.FitsCreateHdrPtr(fptr, out volumeDataSet.FitsHeader, out volumeDataSet.NumberHeaderKeys, out status) != 0)
            {
                Debug.Log("Fits create header pointer failure... code #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }
            IntPtr astFrameSet = IntPtr.Zero;
            if (AstTool.InitAstFrameSet(out volumeDataSet.AstFrameSet, volumeDataSet.FitsHeader) != 0)
            {
                Debug.Log("Warning... AstFrameSet Error. See Unity Editor logs");
            }
            if (!isMask)
            {
                volumeDataSet._headerDictionary = FitsReader.ExtractHeaders(fptr, out status);
                volumeDataSet.ParseHeaderDict();
            }

            if (FitsReader.FitsGetImageDims(fptr, out cubeDimensions, out status) != 0)
            {
                Debug.Log("Fits read image dimensions failed... code #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }

            if (cubeDimensions < 3)
            {
                Debug.Log("Only " + cubeDimensions.ToString() +
                          " found. Please use Fits cube with at least 3 dimensions.");
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }

            if (index2 != 2 && index2 != 3)
            {
                Debug.Log("Depth index must be either 2 or 3." + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }

            if (FitsReader.FitsGetImageSize(fptr, cubeDimensions, out dataPtr, out status) != 0)
            {
                Debug.Log("Fits Read cube size error #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }

            volumeDataSet.cubeSize = new long[cubeDimensions];
            Marshal.Copy(dataPtr, volumeDataSet.cubeSize, 0, cubeDimensions);
            if (dataPtr != IntPtr.Zero)
                FitsReader.FreeMemory(dataPtr);
            long numberDataPoints = volumeDataSet.cubeSize[0] * volumeDataSet.cubeSize[1] * volumeDataSet.cubeSize[index2];
            IntPtr fitsDataPtr = IntPtr.Zero;
            if (isMask)
            {
                if (FitsReader.FitsReadImageInt16(fptr, cubeDimensions, numberDataPoints, out fitsDataPtr, out status) != 0)
                {
                    Debug.Log("Fits Read mask cube data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return null;
                }
            }
            else
            {
                int[] startPix = new int[cubeDimensions];
                int[] finalPix = new int[cubeDimensions];
                for (var i = 0; i < cubeDimensions; i++)
                {
                    startPix[i] = 1;
                    if (i < 4)
                        finalPix[i] = (int) volumeDataSet.cubeSize[i];
                    else
                        finalPix[i] = 1;
                }

                if (index2 == 3)
                {
                    startPix[2] = sliceDim;
                    finalPix[2] = sliceDim;
                }
                else if (cubeDimensions > 3)
                {
                    startPix[3] = sliceDim;
                    finalPix[3] = sliceDim;
                }

                IntPtr startPixPtr = Marshal.AllocHGlobal(sizeof(int) * startPix.Length);
                IntPtr finalPixPtr = Marshal.AllocHGlobal(sizeof(int) * finalPix.Length);
                Marshal.Copy(startPix, 0, startPixPtr, startPix.Length);
                Marshal.Copy(finalPix, 0, finalPixPtr, finalPix.Length);
                if (FitsReader.FitsReadSubImageFloat(fptr, cubeDimensions, startPixPtr, finalPixPtr, numberDataPoints, out fitsDataPtr, out status) != 0)
                {
                    Debug.Log("Fits Read cube data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return null;
                }

                if (startPixPtr == IntPtr.Zero)
                    Marshal.FreeHGlobal(startPixPtr);
                if (finalPixPtr == IntPtr.Zero)
                    Marshal.FreeHGlobal(finalPixPtr);
            }

            FitsReader.FitsCloseFile(fptr, out status);
            if (!isMask)
            {
                DataAnalysis.FindStats(fitsDataPtr, numberDataPoints, out volumeDataSet.MaxValue, out volumeDataSet.MinValue, out volumeDataSet.MeanValue,
                    out volumeDataSet.StanDev);
                int histogramSize = Mathf.RoundToInt(Mathf.Sqrt(numberDataPoints));
                volumeDataSet.Histogram = new int[histogramSize];
                IntPtr histogramPtr = IntPtr.Zero;
                volumeDataSet.HistogramBinWidth = (volumeDataSet.MaxValue - volumeDataSet.MinValue) / histogramSize;
                DataAnalysis.GetHistogram(fitsDataPtr, numberDataPoints, histogramSize, volumeDataSet.MinValue, volumeDataSet.MaxValue, out histogramPtr);
                Marshal.Copy(histogramPtr, volumeDataSet.Histogram, 0, histogramSize);
                if (histogramPtr != IntPtr.Zero)
                    DataAnalysis.FreeMemory(histogramPtr);
            }

            volumeDataSet.FitsData = fitsDataPtr;
            volumeDataSet.XDim = volumeDataSet.cubeSize[0];
            volumeDataSet.YDim = volumeDataSet.cubeSize[1];
            volumeDataSet.ZDim = volumeDataSet.cubeSize[index2];
            volumeDataSet.XDimDecimal = volumeDataSet.cubeSize[0].ToString().Length;
            volumeDataSet.YDimDecimal = volumeDataSet.cubeSize[1].ToString().Length;
            volumeDataSet.ZDimDecimal = volumeDataSet.cubeSize[index2].ToString().Length;
            volumeDataSet.HeaderDictionary = volumeDataSet._headerDictionary;

            volumeDataSet._updateTexture = new Texture2D(1, 1, TextureFormat.R16, false);
            // single pixel brush: 16-bits = 2 bytes
            volumeDataSet._cachedBrush = new byte[2];

            return volumeDataSet;
        }

        public static void UpdateHistogram(VolumeDataSet volumeDataSet, float min, float max)
        {
            long numberDataPoints = volumeDataSet.XDim * volumeDataSet.YDim * volumeDataSet.ZDim;
            IntPtr histogramPtr = IntPtr.Zero;
            volumeDataSet.HistogramBinWidth = (max - min) / volumeDataSet.Histogram.Length;
            DataAnalysis.GetHistogram(volumeDataSet.FitsData, numberDataPoints, volumeDataSet.Histogram.Length, min, max, out histogramPtr);
            Marshal.Copy(histogramPtr, volumeDataSet.Histogram, 0, volumeDataSet.Histogram.Length);
            if (histogramPtr != IntPtr.Zero)
                DataAnalysis.FreeMemory(histogramPtr);
        }

        public static VolumeDataSet GenerateEmptyMask(long cubeSizeX, long cubeSizeY, long cubeSizeZ)
        {
            VolumeDataSet volumeDataSet = new VolumeDataSet();
            volumeDataSet.IsMask = true;
            IntPtr dataPtr;
            FitsReader.CreateEmptyImageInt16(cubeSizeX, cubeSizeY, cubeSizeZ, out dataPtr);
            volumeDataSet.FitsData = dataPtr;
            volumeDataSet.XDim = cubeSizeX;
            volumeDataSet.YDim = cubeSizeY;
            volumeDataSet.ZDim = cubeSizeZ;

            volumeDataSet.XDimDecimal = volumeDataSet.XDim.ToString().Length;
            volumeDataSet.YDimDecimal = volumeDataSet.YDim.ToString().Length;
            volumeDataSet.ZDimDecimal = volumeDataSet.ZDim.ToString().Length;

            volumeDataSet._updateTexture = new Texture2D(1, 1, TextureFormat.R16, false);
            // single pixel brush: 16-bits = 2 bytes
            volumeDataSet._cachedBrush = new byte[2];

            return volumeDataSet;
        }

        public void GenerateVolumeTexture(TextureFilterEnum textureFilter, int xDownsample, int yDownsample, int zDownsample)
        {
            TextureFormat textureFormat;
            int elementSize;
            if (IsMask)
            {
                textureFormat = TextureFormat.R16;
                elementSize = sizeof(Int16);
            }
            else
            {
                textureFormat = TextureFormat.RFloat;
                elementSize = sizeof(float);
            }

            IntPtr reducedData = IntPtr.Zero;
            bool downsampled = false;
            if (xDownsample != 1 || yDownsample != 1 || zDownsample != 1)
            {
                if (IsMask)
                {
                    if (DataAnalysis.MaskCropAndDownsample(FitsData, out reducedData, XDim, YDim, ZDim, 1, 1, 1, XDim, YDim, ZDim, xDownsample, yDownsample,
                        zDownsample) != 0)
                    {
                        Debug.Log("Data cube downsample error!");
                    }
                }
                else
                {
                    if (DataAnalysis.DataCropAndDownsample(FitsData, out reducedData, XDim, YDim, ZDim, 1, 1, 1, XDim, YDim, ZDim, xDownsample, yDownsample,
                        zDownsample) != 0)
                    {
                        Debug.Log("Data cube downsample error!");
                    }
                }

                downsampled = true;
            }
            else
                reducedData = FitsData;

            int[] cubeSize = new int[3]; //assume 3D cube
            cubeSize[0] = (int) XDim / xDownsample;
            cubeSize[1] = (int) YDim / yDownsample;
            cubeSize[2] = (int) ZDim / zDownsample;
            if (XDim % xDownsample != 0)
                cubeSize[0]++;
            if (YDim % yDownsample != 0)
                cubeSize[1]++;
            if (ZDim % zDownsample != 0)
                cubeSize[2]++;
            Texture3D dataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], textureFormat, false);
            switch (textureFilter)
            {
                case TextureFilterEnum.Point:
                    dataCube.filterMode = FilterMode.Point;
                    break;
                case TextureFilterEnum.Bilinear:
                    dataCube.filterMode = FilterMode.Bilinear;
                    break;
                case TextureFilterEnum.Trilinear:
                    dataCube.filterMode = FilterMode.Trilinear;
                    break;
            }

            int sliceSize = cubeSize[0] * cubeSize[1];
            Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], textureFormat, false);
            for (int slice = 0; slice < cubeSize[2]; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(reducedData, slice * sliceSize * elementSize),
                    sliceSize * elementSize);
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize[0], cubeSize[1], dataCube, slice, 0, 0, 0);
            }

            DataCube = dataCube;
            //TODO output cached file
            if (downsampled && reducedData != IntPtr.Zero)
                DataAnalysis.FreeMemory(reducedData);
        }

        public void GenerateCroppedVolumeTexture(TextureFilterEnum textureFilter, Vector3Int cropStart, Vector3Int cropEnd, Vector3Int downsample)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            TextureFormat textureFormat;
            int elementSize;
            IntPtr regionData = IntPtr.Zero;
            if (IsMask)
            {
                textureFormat = TextureFormat.R16;
                elementSize = sizeof(Int16);
                if (DataAnalysis.MaskCropAndDownsample(FitsData, out regionData, XDim, YDim, ZDim, cropStart.x, cropStart.y, cropStart.z,
                    cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z) != 0)
                {
                    Debug.Log("Mask cube downsample error!");
                }
            }
            else
            {
                textureFormat = TextureFormat.RFloat;
                elementSize = sizeof(float);
                if (DataAnalysis.DataCropAndDownsample(FitsData, out regionData, XDim, YDim, ZDim, cropStart.x, cropStart.y, cropStart.z,
                    cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z) != 0)
                {
                    Debug.Log("Data cube downsample error!");
                }
            }

            RegionOffset = Vector3Int.Min(cropStart, cropEnd);
            Vector3Int cubeSize = new Vector3Int();
            cubeSize.x = (Math.Abs(cropStart.x - cropEnd.x) + 1) / downsample.x;
            cubeSize.y = (Math.Abs(cropStart.y - cropEnd.y) + 1) / downsample.y;
            cubeSize.z = (Math.Abs(cropStart.z - cropEnd.z) + 1) / downsample.z;
            if ((Math.Abs(cropStart.x - cropEnd.x) + 1) % downsample.x != 0)
                cubeSize.x++;
            if ((Math.Abs(cropStart.y - cropEnd.y) + 1) % downsample.y != 0)
                cubeSize.y++;
            if ((Math.Abs(cropStart.z - cropEnd.z) + 1) % downsample.z != 0)
                cubeSize.z++;

            RegionCube = new Texture3D(cubeSize.x, cubeSize.y, cubeSize.z, textureFormat, false);
            switch (textureFilter)
            {
                case TextureFilterEnum.Point:
                    RegionCube.filterMode = FilterMode.Point;
                    break;
                case TextureFilterEnum.Bilinear:
                    RegionCube.filterMode = FilterMode.Bilinear;
                    break;
                case TextureFilterEnum.Trilinear:
                    RegionCube.filterMode = FilterMode.Trilinear;
                    break;
            }

            int sliceSize = cubeSize.x * cubeSize.y;
            Texture2D textureSlice = new Texture2D(cubeSize.x, cubeSize.y, textureFormat, false);

            for (int slice = 0; slice < cubeSize.z; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(regionData, slice * sliceSize * elementSize), sliceSize * elementSize);
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize.x, cubeSize.y, RegionCube, slice, 0, 0, 0);
            }

            if (IsMask)
            {
                var numVoxels = cubeSize.x * cubeSize.y * cubeSize.z;
                _regionMaskVoxels = new short[numVoxels];
                Marshal.Copy(regionData, _regionMaskVoxels, 0, numVoxels);

                _existingRegionMaskEntries = new List<VoxelEntry>();
                for (int i = 0; i < numVoxels; i++)
                {
                    var voxelVal = _regionMaskVoxels[i];
                    if (voxelVal != 0)
                    {
                        // check if voxel is surrounded by other masked voxels and encode the active edges into the value
                        int compoundValue = VoxelActiveFaces(i, cubeSize, _regionMaskVoxels) * 32768 + (int) voxelVal;
                        _existingRegionMaskEntries.Add(new VoxelEntry(i, compoundValue));
                    }
                }

                Debug.Log($"Found {_existingRegionMaskEntries.Count} non-empty mask voxels in region");

                ExistingMaskBuffer?.Release();
                if (_existingRegionMaskEntries.Count > 0)
                {
                    ExistingMaskBuffer = new ComputeBuffer(_existingRegionMaskEntries.Count, Marshal.SizeOf(typeof(VoxelEntry)));
                    ExistingMaskBuffer.SetData(_existingRegionMaskEntries);
                }
                else
                {
                    ExistingMaskBuffer = null;
                }

                if (AddedMaskBuffer == null)
                {
                    AddedMaskBuffer = new ComputeBuffer(BrushStrokeLimit, Marshal.SizeOf(typeof(VoxelEntry)));
                }

                if (_addedRegionMaskEntries == null)
                {
                    _addedRegionMaskEntries = new List<VoxelEntry>();
                }

                _addedRegionMaskEntries.Clear();
                AddedMaskEntryCount = 0;
                BrushStrokeHistory = new List<BrushStrokeTransaction>();
            }

            if (regionData != IntPtr.Zero)
                DataAnalysis.FreeMemory(regionData);
            sw.Stop();
            Debug.Log(
                $"Cropped into {cubeSize.x} x {cubeSize.y} x {cubeSize.z} region ({cubeSize.x * cubeSize.y * cubeSize.z * 4e-6} MB) in {sw.ElapsedMilliseconds} ms");
        }

        public IDictionary<string, string> GetHeaderDictionary()
        {
            Debug.Log(_headerDictionary);
            return _headerDictionary;
        }

        private static int VoxelActiveFaces(int i, Vector3Int cubeSize, short[] voxels)
        {
            short voxelValue = voxels[i];
            Vector3Int voxelIndices = Vector3Int.zero;
            voxelIndices.x = i % cubeSize.x;
            int j = (i - voxelIndices.x) / cubeSize.x;
            voxelIndices.y = j % cubeSize.y;
            voxelIndices.z = (j - voxelIndices.y) / cubeSize.y;

            int activeFaces = 0;

            // -x face
            if (voxelIndices.x <= 0)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 1;
                }
            }
            else if (voxels[i - 1] != voxelValue)
            {
                activeFaces += 1;
            }

            // +x face
            if (voxelIndices.x >= cubeSize.x - 1)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 2;
                }
            }
            else if (voxels[i + 1] != voxelValue)
            {
                activeFaces += 2;
            }
            
            // -y face
            if (voxelIndices.y <= 0)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 4;
                }
            }
            else if (voxels[i - cubeSize.x] != voxelValue)
            {
                activeFaces += 4;
            }
            
            // +y face
            if (voxelIndices.y >= cubeSize.y - 1)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 8;
                }
            }
            else if (voxels[i + cubeSize.x] == 0)
            {
                activeFaces += 8;
            }

            // -z face
            if (voxelIndices.z <= 0)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 16;
                }
            }
            else if (voxels[i - cubeSize.y * cubeSize.x] != voxelValue)
            {
                activeFaces += 16;
            }

            // +z face
            if (voxelIndices.z >= cubeSize.z - 1)
            {
                if (voxelValue != 0)
                {
                    activeFaces += 32;
                }
            }
            else if (voxels[i + cubeSize.y * cubeSize.x] != voxelValue)
            {
                activeFaces += 32;
            }
            
            return activeFaces;
        }

        public float GetDataValue(int x, int y, int z)
        {
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                return float.NaN;
            }

            float val;
            DataAnalysis.GetVoxelFloatValue(FitsData, out val, (int) XDim, (int) YDim, (int) ZDim, x, y, z);
            return val;
        }

        public Int16 GetMaskValue(int x, int y, int z)
        {
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                return 0;
            }

            Int16 val;
            DataAnalysis.GetVoxelInt16Value(FitsData, out val, (int) XDim, (int) YDim, (int) ZDim, x, y, z);
            return val;
        }

        public void FindDownsampleFactors(long maxCubeSizeInMb, out int xFactor, out int yFactor, out int zFactor)
        {
            FindDownsampleFactors(maxCubeSizeInMb, XDim, YDim, ZDim, out xFactor, out yFactor, out zFactor);
        }

        public void FindDownsampleFactors(long maxCubeSizeInMB, long regionXDim, long regionYDim, long regionZDim, out int xFactor, out int yFactor,
            out int zFactor)
        {
            xFactor = 1;
            yFactor = 1;
            zFactor = 1;
            while (regionXDim / xFactor > 2048 || regionYDim / yFactor > 2048)
            {
                xFactor++;
                yFactor++;
            }

            while (regionZDim / zFactor > 2048)
                zFactor++;
            long maximumElements = maxCubeSizeInMB * 1000000 / 4;
            while (regionXDim * regionYDim * regionZDim / (xFactor * yFactor * zFactor) > maximumElements)
            {
                if (regionZDim / zFactor > regionXDim / xFactor || regionZDim / zFactor > regionYDim / yFactor)
                    zFactor++;
                else
                {
                    xFactor++;
                    yFactor++;
                }
            }
        }

        public Vector3 GetWCSDeltas()
        {
            return new Vector3((float) _xDelt, (float) _yDelt, (float) _zDelt);
        }

        public void ParseHeaderDict()
        {
            string xProj = "";
            string yProj = "";
            string zProj = "";
            _rot = 0;
            foreach (KeyValuePair<string, string> entry in _headerDictionary)
            {
                switch (entry.Key)
                {
                    case "NAXIS":
                        NAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE1":
                        _xCoord = entry.Value.Substring(0, 4);
                        xProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX1":
                        _xRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT1":
                        _xDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL1":
                        _xRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE2":
                        _yCoord = entry.Value.Substring(0, 4);
                        yProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX2":
                        _yRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT2":
                        _yDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL2":
                        _yRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE3":
                        _zCoord = entry.Value.Substring(0, 4); //Crashing with some data sets. Need to fix
                        zProj = entry.Value.Substring(5, 4);
                        break;
                    case "CRPIX3":
                        _zRefPix = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CDELT3":
                        _zDelt = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CRVAL3":
                        _zRef = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CROTA2":
                        _rot = Convert.ToDouble(entry.Value.Replace("'", ""), CultureInfo.InvariantCulture);
                        break;
                    case "BUNIT":
                        PixelUnit = entry.Value.Substring(1, entry.Value.Length - 2);
                        break;
                    default:
                        break;
                }
            }

            if (xProj != yProj)
                Debug.Log("Warning: WCS projection types do not agree for dimensions! x: " + xProj + ", y: " + yProj);
            _wcsProj = xProj;
        }

        public bool PaintMaskVoxel(Vector3Int coordsRegionSpace, short value)
        {
            if (coordsRegionSpace.x < 0 || coordsRegionSpace.x >= RegionCube.width || coordsRegionSpace.y < 0 || coordsRegionSpace.y >= RegionCube.height ||
                coordsRegionSpace.z < 0 ||
                coordsRegionSpace.z >= RegionCube.depth)
            {
                return false;
            }

            // encode the active edges into the value. For now, edges are all on or all off
            int index = coordsRegionSpace.x + coordsRegionSpace.y * RegionCube.width + coordsRegionSpace.z * (RegionCube.width * RegionCube.height);
            var currentValue = _regionMaskVoxels[index];

            // If the voxel already has the correct value, exit
            if (currentValue == value)
            {
                return true;
            }

            // Create transaction if it doesn't exist
            if (CurrentBrushStroke.Voxels == null)
            {
                CurrentBrushStroke = new BrushStrokeTransaction(value);
            }

            _regionMaskVoxels[index] = value;
            // convert from int to byte array
            _cachedBrush = BitConverter.GetBytes(value);
            _updateTexture.LoadRawTextureData(_cachedBrush);
            _updateTexture.Apply();
            Graphics.CopyTexture(_updateTexture, 0, 0, 0, 0, 1, 1, RegionCube, coordsRegionSpace.z, 0, coordsRegionSpace.x, coordsRegionSpace.y);

            Vector3Int cubeSize = new Vector3Int(RegionCube.width, RegionCube.height, RegionCube.depth);
            int compoundValue = VoxelActiveFaces(index, cubeSize, _regionMaskVoxels) * 32768 + (int) value;
            VoxelEntry newEntry = new VoxelEntry(index, compoundValue);

            if (_existingRegionMaskEntries != null)
            {
                int maskEntryIndex = _existingRegionMaskEntries.BinarySearch(newEntry, VoxelEntry.IndexComparer);
                if (maskEntryIndex >= 0)
                {
                    // Update entry in list
                    _existingRegionMaskEntries[maskEntryIndex] = newEntry;
                    // Update compute buffer
                    ExistingMaskBuffer.SetData(_existingRegionMaskEntries, maskEntryIndex, maskEntryIndex, 1);
                }
                else
                {
                    _addedRegionMaskEntries.Add(newEntry);
                    var lastIndex = _addedRegionMaskEntries.Count - 1;
                    if (lastIndex <= AddedMaskBuffer.count)
                    {
                        AddedMaskBuffer.SetData(_addedRegionMaskEntries, lastIndex, lastIndex, 1);
                        AddedMaskEntryCount = _addedRegionMaskEntries.Count;
                    }
                }

                // Update neighbouring entries' active faces
                int[] neighbourIndices =
                {
                    index - 1,
                    index + 1,
                    index + cubeSize.x,
                    index - cubeSize.x,
                    index + (cubeSize.x * cubeSize.y),
                    index - (cubeSize.x * cubeSize.y)
                };

                foreach (var neighbourIndex in neighbourIndices)
                {
                    // Skip out-of range neighbours
                    if (neighbourIndex < 0 || neighbourIndex >= _regionMaskVoxels.Length)
                    {
                        continue;
                    }

                    // Skip neighbours that are empty
                    short neighbourValue = _regionMaskVoxels[neighbourIndex];
                    if (neighbourValue == 0)
                    {
                        continue;
                    }

                    // Re-calculate active edges
                    int compoundNeighbourValue = VoxelActiveFaces(neighbourIndex, cubeSize, _regionMaskVoxels) * 32768 + (int) neighbourValue;
                    var neighbourEntry = new VoxelEntry(neighbourIndex, compoundNeighbourValue);
                    // To update the entry, we first do a binary search on the existing entries. If this gets a hit, we update this. Otherwise, we update the added entries list
                    int existingNeighbourMaskEntryIndex = _existingRegionMaskEntries.BinarySearch(neighbourEntry, VoxelEntry.IndexComparer);
                    if (existingNeighbourMaskEntryIndex >= 0)
                    {
                        // Update entry in list
                        _existingRegionMaskEntries[existingNeighbourMaskEntryIndex] = neighbourEntry;
                        // Update compute buffer
                        ExistingMaskBuffer.SetData(_existingRegionMaskEntries, existingNeighbourMaskEntryIndex, existingNeighbourMaskEntryIndex, 1);
                    }
                    else
                    {
                        int addedNeighbourMaskEntryIndex = _addedRegionMaskEntries.FindIndex(entry => entry.Index == neighbourIndex);
                        if (addedNeighbourMaskEntryIndex >= 0)
                        {
                            // Update entry in list
                            _addedRegionMaskEntries[addedNeighbourMaskEntryIndex] = neighbourEntry;
                            // Update compute buffer
                            AddedMaskBuffer.SetData(_addedRegionMaskEntries, addedNeighbourMaskEntryIndex, addedNeighbourMaskEntryIndex, 1);
                        }
                    }
                }
            }

            CurrentBrushStroke.Voxels.Add(new VoxelEntry(newEntry.Index, currentValue));

            return true;
        }

        public void FlushBrushStroke()
        {
            ConsolidateMaskEntries();
            BrushStrokeHistory.Add(CurrentBrushStroke);
            CurrentBrushStroke = new BrushStrokeTransaction(CurrentBrushStroke.NewValue);
        }

        private void ConsolidateMaskEntries()
        {
            Vector3Int cubeSize = new Vector3Int(RegionCube.width, RegionCube.height, RegionCube.depth);
            if (_existingRegionMaskEntries == null || _existingRegionMaskEntries.Count == 0)
            {
                _existingRegionMaskEntries = new List<VoxelEntry>();
            }
            else
            {
                _existingRegionMaskEntries = _existingRegionMaskEntries.Where(entry => entry.Value != 0).ToList();
            }

            if (_addedRegionMaskEntries.Count > 0)
            {
                _existingRegionMaskEntries.AddRange(_addedRegionMaskEntries);
                _existingRegionMaskEntries.Sort(VoxelEntry.IndexComparer);
            }

            ExistingMaskBuffer?.Release();
            if (_existingRegionMaskEntries.Count > 0)
            {
                ExistingMaskBuffer = new ComputeBuffer(_existingRegionMaskEntries.Count, Marshal.SizeOf(typeof(VoxelEntry)));
                ExistingMaskBuffer.SetData(_existingRegionMaskEntries);
            }
            else
            {
                ExistingMaskBuffer = null;
            }

            _addedRegionMaskEntries = new List<VoxelEntry>();
            AddedMaskEntryCount = 0;
        }

        public int CommitMask()
        {
            int status = 0;
            if (_regionMaskVoxels == null || _regionMaskVoxels.Length == 0)
            {
                Debug.Log("Can't save empty region to mask");
                return -1;
            }

            int unmangedMemorySize = Marshal.SizeOf(_regionMaskVoxels[0]) * _regionMaskVoxels.Length;
            IntPtr unmanagedCopy = Marshal.AllocHGlobal(unmangedMemorySize);
            long[] regionDims = {RegionCube.width, RegionCube.height, RegionCube.depth};
            long[] regionOffset = {RegionOffset.x, RegionOffset.y, RegionOffset.z};
            Marshal.Copy(_regionMaskVoxels, 0, unmanagedCopy, _regionMaskVoxels.Length);
            if (!FitsReader.UpdateMask(FitsData, Dims, unmanagedCopy, regionDims, regionOffset))
            {
                Debug.Log("Error updating mask");
                return - 1;
            }

            if (unmanagedCopy != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(unmanagedCopy);
            }

            return status;
        }

        public int SaveMask(IntPtr cubeFitsPtr, string filename)
        {
            int status = FitsReader.SaveMask(cubeFitsPtr, FitsData, Dims, filename);
            if (!string.IsNullOrEmpty(filename))
            {
                // Update filename after stripping out exclamation mark indicating overwrite flag
                FileName = filename.Replace("!", "");
            }
            return status;
        }

        public void CleanUp(bool randomCube)
        {
            int status;
            if (FitsData != IntPtr.Zero)
            {
                if (randomCube)
                    Marshal.FreeHGlobal(FitsData);
                else
                    FitsReader.FreeMemory(FitsData);
            }
            if (FitsHeader != IntPtr.Zero)
            {
                FitsReader.FreeFitsMemory(FitsHeader, out status);
            }
            if (AstFrameSet != IntPtr.Zero)
            {
                AstTool.DeleteObject(AstFrameSet);
                AstTool.AstEnd();
            }
            ExistingMaskBuffer?.Release();
            AddedMaskBuffer?.Release();
        }
    }
}