using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DataFeatures;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VolumeData
{
    
    public enum AngleCoordFormat
    {
        Sexagesimal = 0,
        Decimal = 1
    }

    public enum VelocityUnit
    {
        Km = 0,
        M = 1
    }
    
    public struct VoxelEntry
    {
        public readonly int Index;
        public readonly int Value;

        public VoxelEntry(int index, int value)
        {
            Index = index;
            Value = value;
        }

        public static readonly Comparer<VoxelEntry> IndexComparer = Comparer<VoxelEntry>.Create(
            (a, b) => a.Index - b.Index
        );
    }

    public struct BrushStrokeTransaction
    {
        public int NewValue;
        public List<VoxelEntry> Voxels;
        public Dictionary<int, bool> ChangedSources;

        public BrushStrokeTransaction(int newValue)
        {
            NewValue = newValue;
            Voxels = new List<VoxelEntry>();
            ChangedSources = new Dictionary<int, bool>();
            if (NewValue != 0)
            {
                ChangedSources[NewValue] = true;
            }
        }

        public void Add(VoxelEntry entry)
        {
            Voxels.Add(entry);
            if (entry.Value != 0)
            {
                ChangedSources[entry.Value] = true;
            }
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
        public List<BrushStrokeTransaction> BrushStrokeRedoQueue { get; private set; }
        public Dictionary<int, DataAnalysis.SourceStats> SourceStatsDict { get; private set; }

        public string FileName { get; set; }
        public int SelectedHdu { get; set; }
        public long XDim { get; private set; }
        public long YDim { get; private set; }
        public long ZDim { get; private set; }

        public int[] trueSize { get; set; }
        public IDictionary<string, string> HeaderDictionary { get; private set; }

        public int VelDecimal { get; private set; }

        public long NumPoints => XDim * YDim * ZDim;
        public long[] Dims => new[] {XDim, YDim, ZDim};
        public int[] subsetBounds {get; private set; }

        private bool loadSubset => true;//subsetBounds[0] != -1;

        public Vector3Int RegionOffset { get; private set; }

        public bool IsMask { get; private set; }
        private IntPtr ImageDataPtr;
        private FeatureSetRenderer _maskFeatureSet;

        private double _xRef, _yRef, _zRef, _xRefPix, _yRefPix, _zRefPix, _xDelt, _yDelt, _zDelt, _rot;
        private string _xCoord, _yCoord, _zCoord, _wcsProj;

        public double NAxis;
        private List<VoxelEntry> _existingRegionMaskEntries;
        private List<VoxelEntry> _addedRegionMaskEntries;
        private Texture2D _updateTexture;
        private byte[] _cachedBrush;
        private short[] _regionMaskVoxels;
        private Bounds _dirtyMaskBounds;
        private readonly Dictionary<int, int> _addedMaskEntriesDict = new Dictionary<int, int>();
        private static int BrushStrokeLimit = 16777216;
        public short NewSourceId = 1000;

        public IntPtr FitsData = IntPtr.Zero;
        public IntPtr FitsHeader = IntPtr.Zero;
        public int NumberHeaderKeys;
        public IntPtr AstFrameSet { get; private set; }
        public IntPtr AstAltSpecSet { get; private set; }
        public bool AstframeIsFreq = false;
        public bool HasFitsRestFrequency { get; private set; } = false;
        public bool HasRestFrequency { get; set; }
        public double FitsRestFrequency { get; set;}

        public int VelocityDirection
        {
            get
            {
                if (_zCoord != null && _zCoord.Length >= 4 && _zDelt != 0.0)
                {
                    switch (_zCoord)
                    {
                        case "FREQ":
                        case "ENER":
                        case "WAVN":
                            return -1 * Math.Sign(_zDelt);
                        case "VRAD":
                        case "WAVE":
                        case "VOPT":
                        case "ZOPT":
                        case "AWAV":
                        case "VELO":
                        case "BETA":
                            return Math.Sign(_zDelt);
                    }
                }
                return 0;
            }
        }

        public long[] cubeSize;

        public int[] Histogram;
        public float HistogramBinWidth;
        public float MaxValue;
        public float MinValue;
        public float MeanValue;
        public float StanDev;

        public string PixelUnit = "units";

        /// <summary>
        /// Function that creates a random data set of the given dimensions.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="xDim"></param>
        /// <param name="yDim"></param>
        /// <param name="zDim"></param>
        /// <returns></returns>
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
            DataAnalysis.FindStats(dataPtr, numberDataPoints, out volumeDataSet.MaxValue, out volumeDataSet.MinValue, out volumeDataSet.MeanValue,
                out volumeDataSet.StanDev);
            int histogramSize = Mathf.RoundToInt(Mathf.Sqrt(numberDataPoints));
            volumeDataSet.Histogram = new int[histogramSize];
            IntPtr histogramPtr = IntPtr.Zero;
            volumeDataSet.HistogramBinWidth = (volumeDataSet.MaxValue - volumeDataSet.MinValue) / histogramSize;
            DataAnalysis.GetHistogram(dataPtr, numberDataPoints, histogramSize, volumeDataSet.MinValue, volumeDataSet.MaxValue, out histogramPtr);
            Marshal.Copy(histogramPtr, volumeDataSet.Histogram, 0, histogramSize);
            if (histogramPtr != IntPtr.Zero)
                DataAnalysis.FreeDataAnalysisMemory(histogramPtr);
            return volumeDataSet;
        }

        /// <summary>
        /// Function that retrives and loads the data from the actual file on the hard disk.
        /// </summary>
        /// <param name="fileName">The path to the data file.</param>
        /// <param name="subBounds">The bounds from which the data should be loaded (default of [-1,-1,-1,-1,-1,-1] means load the full cube).</param>
        /// <param name="trueBounds">The bounds of the full cube.</param>
        /// <param name="imageDataPtr"></param>
        /// <param name="index2"></param>
        /// <param name="sliceDim"></param>
        /// <returns></returns>
        public static VolumeDataSet LoadDataFromFitsFile(string fileName, int[] subBounds, int[] trueBounds, IntPtr imageDataPtr = default(IntPtr), int index2 = 2, int sliceDim = 1, int selectedHdu = 1)
        {
            VolumeDataSet volumeDataSetRes = new VolumeDataSet();
            volumeDataSetRes.IsMask =  imageDataPtr != IntPtr.Zero;
            volumeDataSetRes.ImageDataPtr = imageDataPtr;
            volumeDataSetRes.FileName = fileName;
            volumeDataSetRes.SelectedHdu = selectedHdu;
            volumeDataSetRes.trueSize = trueBounds;
            IntPtr fptr = IntPtr.Zero;
            int status = 0;
            int cubeDimensions;
            IntPtr dataPtr = IntPtr.Zero;
            IntPtr astFrameSet;

            // Assign to object, since it is used with the cursor positioning.
            volumeDataSetRes.subsetBounds = subBounds;

            if (FitsReader.FitsOpenFile(out fptr, fileName, out status, true) != 0)
            {
                Debug.Log("Fits open failure... code #" + status.ToString());
            }
            if (selectedHdu != 1)
            {
                if (FitsReader.FitsMovabsHdu(fptr, selectedHdu, out int hdutype, out status) != 0)
                {
                    Debug.Log("Fits move to HDU failure... code #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return null;
                }
            }
            if (FitsReader.FitsCreateHdrPtrForAst(fptr, out volumeDataSetRes.FitsHeader, out volumeDataSetRes.NumberHeaderKeys, out status) != 0)
            {
                Debug.Log("Fits create header pointer failure... code #" + status.ToString());
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }
            if (AstTool.InitAstFrameSet(out astFrameSet, volumeDataSetRes.FitsHeader) != 0)
            {
                Debug.Log("Warning... AstFrameSet Error. See Unity Editor logs");
            }
            if (!volumeDataSetRes.IsMask)
            {
                volumeDataSetRes.HeaderDictionary = FitsReader.ExtractHeaders(fptr, out status);
                volumeDataSetRes.ParseHeaderDict();
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

            volumeDataSetRes.cubeSize = new long[cubeDimensions];
            Marshal.Copy(dataPtr, volumeDataSetRes.cubeSize, 0, cubeDimensions);
            
            // Check if the provided subset is wholly within the given filie
            if (subBounds[1] > volumeDataSetRes.cubeSize[0])
            {
                Debug.Log("Fits Read cube error with subset bounds not possible!");
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }
            else
                volumeDataSetRes.cubeSize[0] = subBounds[1] - subBounds[0] + 1;

            if (subBounds[3] > volumeDataSetRes.cubeSize[1])
            {
                Debug.Log("Fits Read cube error with subset bounds not possible!");
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }
            else
                volumeDataSetRes.cubeSize[1] = subBounds[3] - subBounds[2] + 1;

            if (subBounds[5] > volumeDataSetRes.cubeSize[2])
            {
                Debug.Log("Fits Read cube error with subset bounds not possible!");
                FitsReader.FitsCloseFile(fptr, out status);
                return null;
            }
            else
                volumeDataSetRes.cubeSize[2] = subBounds[5] - subBounds[4] + 1;

            if (dataPtr != IntPtr.Zero)
                FitsReader.FreeFitsPtrMemory(dataPtr);
            long numberDataPoints = volumeDataSetRes.cubeSize[0] * volumeDataSetRes.cubeSize[1] * volumeDataSetRes.cubeSize[index2];
            IntPtr fitsDataPtr = IntPtr.Zero;
            
            if (volumeDataSetRes.IsMask)
            {
                int[] startPix = new int[cubeDimensions];
                int[] finalPix = new int[cubeDimensions];
                for (var i = 0; i < cubeDimensions; i++)
                {
                    if (i < 3)
                    {
                        startPix[i] = subBounds[i * 2];
                        finalPix[i] = subBounds[(i * 2) + 1];
                    }
                    else
                    {
                        startPix[i] = 1;
                        finalPix[i] = 1;
                    }
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

                Debug.Log("Loading a subcube mask with start pixel [" + String.Join(", ", startPix) + "] and end pixel [" + String.Join(", ", finalPix) + "].");

                IntPtr startPixPtr = Marshal.AllocHGlobal(sizeof(int) * startPix.Length);
                IntPtr finalPixPtr = Marshal.AllocHGlobal(sizeof(int) * finalPix.Length);
                Marshal.Copy(startPix, 0, startPixPtr, startPix.Length);
                Marshal.Copy(finalPix, 0, finalPixPtr, finalPix.Length);
                if (FitsReader.FitsReadSubImageInt16(fptr, cubeDimensions, index2, startPixPtr, finalPixPtr, numberDataPoints, out fitsDataPtr, out status) != 0)
                {
                    Debug.Log("Fits Read mask cube data error #" + status.ToString());
                    FitsReader.FitsCloseFile(fptr, out status);
                    return null;
                }

                if (startPixPtr == IntPtr.Zero)
                    Marshal.FreeHGlobal(startPixPtr);
                if (finalPixPtr == IntPtr.Zero)
                    Marshal.FreeHGlobal(finalPixPtr);
            }
            else //Is not a mask
            {
                int[] startPix = new int[cubeDimensions];
                int[] finalPix = new int[cubeDimensions];
                for (var i = 0; i < cubeDimensions; i++)
                {
                    if (i < 3)
                    {
                        startPix[i] = subBounds[i * 2];
                        finalPix[i] = subBounds[(i * 2) + 1];
                    }
                    else
                    {
                        startPix[i] = 1;
                        finalPix[i] = 1;
                    }
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
                Debug.Log("Loading a subcube with start pixel [" + String.Join(", ", startPix) + "] and end pixel [" + String.Join(", ", finalPix) + "].");

                IntPtr startPixPtr = Marshal.AllocHGlobal(sizeof(int) * startPix.Length);
                IntPtr finalPixPtr = Marshal.AllocHGlobal(sizeof(int) * finalPix.Length);
                Marshal.Copy(startPix, 0, startPixPtr, startPix.Length);
                Marshal.Copy(finalPix, 0, finalPixPtr, finalPix.Length);
                if (FitsReader.FitsReadSubImageFloat(fptr, cubeDimensions, index2, startPixPtr, finalPixPtr, numberDataPoints, out fitsDataPtr, out status) != 0)
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
            if (!volumeDataSetRes.IsMask)
            {
                DataAnalysis.FindStats(fitsDataPtr, numberDataPoints, out volumeDataSetRes.MaxValue, out volumeDataSetRes.MinValue, out volumeDataSetRes.MeanValue,
                    out volumeDataSetRes.StanDev);
                int histogramSize = Mathf.RoundToInt(Mathf.Sqrt(numberDataPoints));
                volumeDataSetRes.Histogram = new int[histogramSize];
                IntPtr histogramPtr = IntPtr.Zero;
                volumeDataSetRes.HistogramBinWidth = (volumeDataSetRes.MaxValue - volumeDataSetRes.MinValue) / histogramSize;
                DataAnalysis.GetHistogram(fitsDataPtr, numberDataPoints, histogramSize, volumeDataSetRes.MinValue, volumeDataSetRes.MaxValue, out histogramPtr);
                Marshal.Copy(histogramPtr, volumeDataSetRes.Histogram, 0, histogramSize);
                if (histogramPtr != IntPtr.Zero)
                    DataAnalysis.FreeDataAnalysisMemory(histogramPtr);
                volumeDataSetRes.HasFitsRestFrequency =
                    volumeDataSetRes.HeaderDictionary.ContainsKey("RESTFRQ") || volumeDataSetRes.HeaderDictionary.ContainsKey("RESTFREQ");
            }
           
            if (volumeDataSetRes.HasFitsRestFrequency)
            {
                Debug.Log("Found rest frequency in file.");
                StringBuilder restFreqSB = new StringBuilder(70);
                volumeDataSetRes.FitsRestFrequency = AstTool.GetString(astFrameSet, new StringBuilder("RestFreq"), restFreqSB, restFreqSB.Capacity);
                if (double.TryParse(restFreqSB.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                {
                    volumeDataSetRes.FitsRestFrequency = result;
                    volumeDataSetRes.HasRestFrequency = true;
                }
            }
            else
                Debug.Log("Did not find rest frequency in file. Manual setting required.");
            
            // Set wcs angle format from config file. Defaults as sexagesimal
            var config = Config.Instance;
            if (config.angleCoordFormat == AngleCoordFormat.Decimal)
            {
                AstTool.SetString(astFrameSet, new StringBuilder("Format(1)"), new StringBuilder("d.*"));
                AstTool.SetString(astFrameSet, new StringBuilder("Format(2)"), new StringBuilder("d.*"));
            }
            
            volumeDataSetRes.FitsData = fitsDataPtr;
            volumeDataSetRes.XDim = volumeDataSetRes.cubeSize[0];
            volumeDataSetRes.YDim = volumeDataSetRes.cubeSize[1];
            volumeDataSetRes.ZDim = volumeDataSetRes.cubeSize[index2];
            volumeDataSetRes.AstFrameSet = astFrameSet;
            
            //Create alternate AstFrame with frequency or velocity depending on primary's unit
            volumeDataSetRes.CreateAltSpecFrame();
            
            //Check if AstFrameSet or AltSpecSet have velocity
            string primaryFrameZUnit = volumeDataSetRes.GetAstAttribute("System(3)");
            volumeDataSetRes.AstframeIsFreq = primaryFrameZUnit.Equals("FREQ") 
                                            || primaryFrameZUnit.Equals("AWAV")
                                            || primaryFrameZUnit.Equals("WAVE")
                                            || primaryFrameZUnit.Equals("WAVELEN")
                                            || primaryFrameZUnit.Equals("WAVENUM")
                                            || primaryFrameZUnit.Equals("AIRWAVE");
            var velocityUnitToSet = config.velocityUnit == VelocityUnit.Km ? "km/s" : "m/s";
            if (volumeDataSetRes.AstframeIsFreq)
                volumeDataSetRes.SetAltAxisUnit(3, velocityUnitToSet);
            else
                volumeDataSetRes.SetAxisUnit(3, velocityUnitToSet);

            volumeDataSetRes._updateTexture = new Texture2D(1, 1, TextureFormat.R16, false);
            // single pixel brush: 16-bits = 2 bytes
            volumeDataSetRes._cachedBrush = new byte[2];

            if (volumeDataSetRes.IsMask)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var sourceArray = DataAnalysis.GetMaskedSourceArray(volumeDataSetRes.FitsData, volumeDataSetRes.XDim, volumeDataSetRes.YDim, volumeDataSetRes.ZDim);
                volumeDataSetRes.SourceStatsDict = new Dictionary<int, DataAnalysis.SourceStats>();
                foreach (var source in sourceArray)
                {
                    volumeDataSetRes.SourceStatsDict[source.maskVal] = DataAnalysis.SourceStats.FromSourceInfo(source);
                    volumeDataSetRes.UpdateStats(source.maskVal);
                    volumeDataSetRes.NewSourceId = Math.Max(volumeDataSetRes.NewSourceId, (short)(source.maskVal + 1));
                }
                sw.Stop();
                Debug.Log($"Calculated stats for {volumeDataSetRes.SourceStatsDict?.Count} sources in {sw.Elapsed.TotalMilliseconds} ms");
            }

            return volumeDataSetRes;
        }

        /// <summary>
        /// Updates the calculated stats for the given mask value.
        /// </summary>
        /// <param name="maskVal"></param>
        private void UpdateStats(short maskVal)
        {
            if (!SourceStatsDict.ContainsKey(maskVal))
            {
                Debug.Log($"Can't update stats for missing source {maskVal}");
                return;
            }
            var sourceStats = SourceStatsDict[maskVal];
            //Check if AstFrameSet or AltSpecSet have velocity
            var frameWithVelocity = AstframeIsFreq ?  AstAltSpecSet : AstFrameSet;
            DataAnalysis.GetSourceStats(ImageDataPtr, FitsData, XDim, YDim, ZDim, DataAnalysis.SourceInfo.FromSourceStats(sourceStats, maskVal), ref sourceStats, frameWithVelocity);
            if (sourceStats.numVoxels > 0)
            {
                SourceStatsDict[maskVal] = sourceStats;
                // Uncomment for stat calculation debugging:
                // PrintStats(maskVal);
            }
            else if (SourceStatsDict.ContainsKey(maskVal))
            {
                SourceStatsDict.Remove(maskVal);
            }
            
            if (_maskFeatureSet)
            {
                var index = _maskFeatureSet.FeatureList.FindIndex(f => f.Id == maskVal - 1);
                if (index >= 0)
                {
                    if (sourceStats.numVoxels > 0)
                    {
                        // Update existing feature's bounds
                        var feature = _maskFeatureSet.FeatureList[index];
                        var boxMin = new Vector3(sourceStats.minX + 1, sourceStats.minY + 1, sourceStats.minZ + 1);
                        var boxMax = new Vector3(sourceStats.maxX, sourceStats.maxY, sourceStats.maxZ);
                        feature.SetBounds(boxMin, boxMax);
                        feature.RawData = new [] {$"{sourceStats.sum}", $"{sourceStats.peak}", $"{sourceStats.channelVsys}", $"{sourceStats.channelW20}", $"{sourceStats.veloVsys}", $"{sourceStats.veloW20}"};
                        _maskFeatureSet.FeatureManager.NeedToRespawnMenuList = true;
                        
                    }
                    else
                    {
                        // Remove empty feature
                        _maskFeatureSet.FeatureList.RemoveAt(index);
                    }
                }
                else if (sourceStats.numVoxels > 0)
                {
                    // Add new feature for the newly created stats
                    var boxMin = new Vector3(sourceStats.minX, sourceStats.minY, sourceStats.minZ);
                    var boxMax = new Vector3(sourceStats.maxX, sourceStats.maxY, sourceStats.maxZ);
                    var flag = "";
                    var name = $"Masked Source #{maskVal}";
                    var rawStrings = new [] {$"{sourceStats.sum}", $"{sourceStats.peak}", $"{sourceStats.channelVsys}", $"{sourceStats.channelW20}", $"{sourceStats.veloVsys}", $"{sourceStats.veloW20}"};
                    bool vis;
                    if (_maskFeatureSet.FeatureList.Count > 1)
                        vis = _maskFeatureSet.FeatureList[0].Visible;
                    else
                        vis = false;
                    var feature = new Feature(boxMin, boxMax, _maskFeatureSet.FeatureColor, name, flag, _maskFeatureSet.FeatureList.Count, maskVal - 1, rawStrings, vis);
                    _maskFeatureSet.AddFeature(feature);
                    _maskFeatureSet.FeatureMenuScrollerDataSource.InitData();       // Reinitialize the data source to include the new feature
                    _maskFeatureSet.FeatureManager.NeedToRespawnMenuList = true;
                }
            }
        }

        /// <summary>
        /// Debug function to print the calculated stats for the given mask value.
        /// </summary>
        /// <param name="maskVal"></param>
        private void PrintStats(short maskVal)
        {
            if (!SourceStatsDict.ContainsKey(maskVal))
            {
                Debug.Log($"Can't print stats for missing source {maskVal}");
                return;
            }
            var sourceStats = SourceStatsDict[maskVal];
            Debug.Log($"Source {maskVal}: Bounding box [{sourceStats.minX}, {sourceStats.minY}, {sourceStats.minZ}] -> [{sourceStats.maxX}, {sourceStats.maxY}, {sourceStats.maxZ}]; {sourceStats.numVoxels} voxels; {sourceStats.sum} (sum); {sourceStats.peak} (peak); centroid [{sourceStats.cX}, {sourceStats.cY}, {sourceStats.cZ}]; vsys: {sourceStats.channelVsys}; w20: {sourceStats.channelW20}");
        }

        public void FillFeatureSet( FeatureSetRenderer featureSet)
        {
            _maskFeatureSet = featureSet;
            _maskFeatureSet.SpawnFeaturesFromSourceStats(SourceStatsDict);
        }

        public void RecreateFrameSet(double restFreq = 0)
        {
            IntPtr astFrameSet = IntPtr.Zero;
            if (AstFrameSet != IntPtr.Zero)
            {
                AstTool.DeleteObject(AstFrameSet);
                AstFrameSet = IntPtr.Zero;
            }
            if (AstTool.InitAstFrameSet(out astFrameSet, FitsHeader, restFreq) != 0)
                Debug.Log("Failed to recreate AstFrameSet!");
            AstFrameSet = astFrameSet; //Need to delete old one?
            
            //Reset velocity units and skyframe formatting on new astframe
            var config = Config.Instance;
            if (config.angleCoordFormat == AngleCoordFormat.Decimal)
            {
                AstTool.SetString(astFrameSet, new StringBuilder("Format(1)"), new StringBuilder("d.*"));
                AstTool.SetString(astFrameSet, new StringBuilder("Format(2)"), new StringBuilder("d.*"));
            }
            string primaryFrameZUnit = GetAstAttribute("System(3)");
            AstframeIsFreq = primaryFrameZUnit == "FREQ" || primaryFrameZUnit == "AWAV";
            var velocityUnitToSet = config.velocityUnit == VelocityUnit.Km ? "km/s" : "m/s";
            if (AstframeIsFreq)
                SetAltAxisUnit(3, velocityUnitToSet);
            else
                SetAxisUnit(3, velocityUnitToSet);
        }

        public void CreateAltSpecFrame()
        {
            if (AstAltSpecSet != IntPtr.Zero)
                AstTool.DeleteObject(AstAltSpecSet);
            IntPtr astAltSpecFrame = IntPtr.Zero;
            string system, unit;
            GetAltSpecSystemWithUnit(out system, out unit);
            AstTool.GetAltSpecSet(AstFrameSet, out astAltSpecFrame, new StringBuilder(system), new StringBuilder(unit), new StringBuilder(GetStdOfRest()));
            AstAltSpecSet = astAltSpecFrame;
        }

        public static void UpdateHistogram(VolumeDataSet volumeDataSet, float min, float max)
        {
            long numberDataPoints = volumeDataSet.XDim * volumeDataSet.YDim * volumeDataSet.ZDim;
            IntPtr histogramPtr = IntPtr.Zero;
            volumeDataSet.HistogramBinWidth = (max - min) / volumeDataSet.Histogram.Length;
            DataAnalysis.GetHistogram(volumeDataSet.FitsData, numberDataPoints, volumeDataSet.Histogram.Length, min, max, out histogramPtr);
            Marshal.Copy(histogramPtr, volumeDataSet.Histogram, 0, volumeDataSet.Histogram.Length);
            if (histogramPtr != IntPtr.Zero)
                DataAnalysis.FreeDataAnalysisMemory(histogramPtr);
        }
        
        public VolumeDataSet GenerateEmptyMask()
        {
            VolumeDataSet maskDataSet = new VolumeDataSet();
            maskDataSet.IsMask = true;
            int trueX, trueY, trueZ;
            trueX = trueSize[1] - trueSize[0] + 1;
            trueY = trueSize[3] - trueSize[2] + 1;
            trueZ = trueSize[5] - trueSize[4] + 1;
            FitsReader.CreateEmptyImageInt16(trueX, trueY, trueZ, out var dataPtr);
            maskDataSet.FitsData = dataPtr;
            maskDataSet.ImageDataPtr = FitsData;
            maskDataSet.XDim = XDim;
            maskDataSet.YDim = YDim;
            maskDataSet.ZDim = ZDim;
            maskDataSet.subsetBounds = subsetBounds;
            maskDataSet.SourceStatsDict = new Dictionary<int, DataAnalysis.SourceStats>();
            maskDataSet._updateTexture = new Texture2D(1, 1, TextureFormat.R16, false);
            // single pixel brush: 16-bits = 2 bytes
            maskDataSet._cachedBrush = new byte[2];

            // Save new mask because none exists yet
            int status;
            FitsReader.FitsOpenFileReadOnly(out var cubeFitsPtr, this.FileName, out status);
            if (SelectedHdu != 1)
            {
                if (FitsReader.FitsMovabsHdu(cubeFitsPtr, SelectedHdu, out var hdutype, out status) != 0)
                {
                    Debug.Log("Fits move to HDU failure... code #" + status.ToString());
                    FitsReader.FitsCloseFile(cubeFitsPtr, out status);
                    return null;
                }
            }
            string directory = Path.GetDirectoryName(this.FileName);
            string fileName = $"{directory}/{Path.GetFileNameWithoutExtension(this.FileName)}-mask";
            bool exists = File.Exists(fileName + ".fits");
            if (exists)
            {
                var timeStamp = DateTime.Now.ToString("yyyyMMdd_Hmmss");
                fileName = fileName + "_" + timeStamp + ".fits";
            }
            else
                fileName = fileName + ".fits";
            long[] trueDims = {trueX, trueY, trueZ};

            maskDataSet.FileName = null;
            return maskDataSet;
        }
        
        /// <summary>
        /// Use DataAnalysisTool to downsample the entire cube and generate a new Texture3D object for DataCube.
        /// </summary>
        /// <param name="textureFilter"></param>
        /// <param name="xDownsample"></param>
        /// <param name="yDownsample"></param>
        /// <param name="zDownsample"></param>
        public void GenerateVolumeTexture(FilterMode textureFilter, int xDownsample, int yDownsample, int zDownsample)
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
                        zDownsample, Config.Instance.maxModeDownsampling) != 0)
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

            if (!DataCube || DataCube.width != cubeSize[0] || DataCube.height != cubeSize[1] || DataCube.depth != cubeSize[2])
            {
                DataCube = new Texture3D(cubeSize[0], cubeSize[1], cubeSize[2], textureFormat, false);    
            }
            DataCube.filterMode = textureFilter;

            int sliceSize = cubeSize[0] * cubeSize[1];
            Texture2D textureSlice = new Texture2D(cubeSize[0], cubeSize[1], textureFormat, false);
            for (int slice = 0; slice < cubeSize[2]; slice++)
            {
                textureSlice.LoadRawTextureData(IntPtr.Add(reducedData, slice * sliceSize * elementSize),
                    sliceSize * elementSize);
                textureSlice.Apply();
                Graphics.CopyTexture(textureSlice, 0, 0, 0, 0, cubeSize[0], cubeSize[1], DataCube, slice, 0, 0, 0);
            }

            // TODO output cached file
            if (downsampled && reducedData != IntPtr.Zero)
                DataAnalysis.FreeDataAnalysisMemory(reducedData);
        }

        /// <summary>
        /// Similar to GenerateVolumeTexture, but only generates a downsampled cropped region of the cube.
        /// </summary>
        /// <param name="textureFilter"></param>
        /// <param name="cropStart"></param>
        /// <param name="cropEnd"></param>
        /// <param name="downsample"></param>
        public void GenerateCroppedVolumeTexture(FilterMode textureFilter, Vector3Int cropStart, Vector3Int cropEnd, Vector3Int downsample)
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
                    cropEnd.x, cropEnd.y, cropEnd.z, downsample.x, downsample.y, downsample.z, Config.Instance.maxModeDownsampling) != 0)
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

            if (!RegionCube || RegionCube.width != cubeSize[0] || RegionCube.height != cubeSize[1] || RegionCube.depth != cubeSize[2])
            {
                RegionCube = new Texture3D(cubeSize.x, cubeSize.y, cubeSize.z, textureFormat, false);    
            }
            
            RegionCube.filterMode = textureFilter;
            
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

                AddedMaskEntryCount = 0;
                _addedMaskEntriesDict.Clear();

                BrushStrokeHistory = new List<BrushStrokeTransaction>();
            }

            if (regionData != IntPtr.Zero)
                DataAnalysis.FreeDataAnalysisMemory(regionData);
            sw.Stop();
            Debug.Log(
                $"Cropped into {cubeSize.x} x {cubeSize.y} x {cubeSize.z} region ({cubeSize.x * cubeSize.y * cubeSize.z * 4e-6} MB) in {sw.ElapsedMilliseconds} ms");
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
            if (loadSubset)
            {
                x -= (subsetBounds[0] - 1);
                y -= (subsetBounds[2] - 1);
                z -= (subsetBounds[4] - 1);
            }
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                Debug.LogError("Trying to load value from coordinates [" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + "], limited to bounds [1, 1, 1] and [" + XDim.ToString() + ", " + YDim.ToString() + ", " + ZDim.ToString() + "]!");
                return float.NaN;
            }

            float val;
            DataAnalysis.GetVoxelFloatValue(FitsData, out val, (int) XDim, (int) YDim, (int) ZDim, x, y, z);
            return val;
        }

        public Int16 GetMaskValue(int x, int y, int z)
        {
            if (loadSubset)
            {
                x -= (subsetBounds[0] - 1);
                y -= (subsetBounds[2] - 1);
                z -= (subsetBounds[4] - 1);
            }
            if (x < 1 || x > XDim || y < 1 || y > YDim || z < 1 || z > ZDim)
            {
                Debug.LogError("Trying to load value from coordinates [" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + "], limited to bounds [1, 1, 1] and [" + XDim.ToString() + ", " + YDim.ToString() + ", " + ZDim.ToString() + "]!");
                return 0;
            }

            // Use the cached / current mask array if the cursor location is within the cropped region
            if (_regionMaskVoxels != null && RegionCube && x >= RegionOffset.x && x < RegionOffset.x + RegionCube.width &&
                y >= RegionOffset.y && y < RegionOffset.y + RegionCube.height &&
                z >= RegionOffset.z && z < RegionOffset.z + RegionCube.depth)
            {
                var index = IndexFromCoords(x, y, z);
                return _regionMaskVoxels[index];
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

        public void ParseHeaderDict()
        {
            string xProj = "";
            string yProj = "";
            _rot = 0;
            foreach (KeyValuePair<string, string> entry in HeaderDictionary)
            {
                switch (entry.Key)
                {
                    case "NAXIS":
                        NAxis = Convert.ToDouble(entry.Value, CultureInfo.InvariantCulture);
                        break;
                    case "CTYPE1":
                        _xCoord = entry.Value.Substring(1, 4);
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
                        _yCoord = entry.Value.Substring(1, 4);
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
                        _zCoord = entry.Value.Substring(1, 4);
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

        public bool PaintMaskVoxel(Vector3Int coordsRegionSpace, short value, bool addToHistory = true)
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

            _regionMaskVoxels[index] = value;
            Vector3Int location = RegionOffset + coordsRegionSpace;
            if (!FitsReader.UpdateMaskVoxel(FitsData, Dims, location, value))
            {
                Debug.Log("Error updating mask");
                return false;
            }
            _dirtyMaskBounds.Encapsulate(location);

            if (SourceStatsDict.ContainsKey(value))
            {
                var s = SourceStatsDict[value];
                s.AddPointToBoundingBox(location.x, location.y, location.z);
                SourceStatsDict[value] = s;
            }
            else
            {
                SourceStatsDict[value] = DataAnalysis.SourceStats.FromPoint(location.x, location.y, location.z);
            }
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
                    _addedMaskEntriesDict[newEntry.Index] = lastIndex;
                    
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
                        if (_addedMaskEntriesDict.TryGetValue(neighbourIndex, out int addedNeighbourMaskEntryIndex) && addedNeighbourMaskEntryIndex >= 0)
                        {
                            // Update entry in list
                            _addedRegionMaskEntries[addedNeighbourMaskEntryIndex] = neighbourEntry;
                            // Update compute buffer
                            AddedMaskBuffer.SetData(_addedRegionMaskEntries, addedNeighbourMaskEntryIndex, addedNeighbourMaskEntryIndex, 1);
                        }
                    }
                }
            }

            if (addToHistory)
            {
                // Create transaction if it doesn't exist
                if (CurrentBrushStroke.Voxels == null || CurrentBrushStroke.NewValue != value)
                {
                    CurrentBrushStroke = new BrushStrokeTransaction(value);
                }

                CurrentBrushStroke.Add(new VoxelEntry(newEntry.Index, currentValue));
                // New brush strokes clear the redo queue
                BrushStrokeRedoQueue?.Clear();
            }

            return true;
        }

        public void FlushBrushStroke()
        {
            ConsolidateMaskEntries();
            foreach (var maskVal in CurrentBrushStroke.ChangedSources?.Keys)
            {
                UpdateStats((short)maskVal);
            }
            BrushStrokeHistory.Add(CurrentBrushStroke);
            CurrentBrushStroke = new BrushStrokeTransaction(CurrentBrushStroke.NewValue);
        }

        public void ConsolidateDownsampledMask()
        {
            if (_dirtyMaskBounds.size.sqrMagnitude == 0)
            {
                return;
            }

            var currentSizeMb = Mathf.CeilToInt(DataCube.width * DataCube.height * DataCube.depth * 4.0e-6f);
            FindDownsampleFactors(currentSizeMb, out int xFactor, out int yFactor, out int zFactor);
            // TODO: only update the dirty section of the cube, as this is far more efficient than downsampling the entire cube again
            GenerateVolumeTexture(FilterMode.Point, xFactor, yFactor, zFactor);
            _dirtyMaskBounds = new Bounds();
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
            _addedMaskEntriesDict.Clear();
            AddedMaskEntryCount = 0;
        }

        public bool UndoBrushStroke()
        {
            if (BrushStrokeHistory?.Count > 0)
            {
                var lastStroke = BrushStrokeHistory.Last();
                Dictionary<short, bool> changedSources = new Dictionary<short, bool>();
                if (lastStroke.NewValue != 0)
                {
                    changedSources[(short)lastStroke.NewValue] = true;
                }
                foreach (var voxel in lastStroke.Voxels)
                {
                    if (voxel.Value != 0)
                    {
                        changedSources[(short)voxel.Value] = true;
                    }
                    PaintMaskVoxel(CoordsFromIndex(voxel.Index), (short)voxel.Value, false);
                }

                foreach (var maskVal in changedSources.Keys)
                {
                    UpdateStats(maskVal);
                }
                
                if (BrushStrokeRedoQueue == null)
                {
                    BrushStrokeRedoQueue = new List<BrushStrokeTransaction>();
                }
                BrushStrokeRedoQueue.Add(lastStroke);
                BrushStrokeHistory.RemoveAt(BrushStrokeHistory.Count - 1);
                return true;
            }

            return false;
        }
        
        public bool RedoBrushStroke()
        {
            if (BrushStrokeRedoQueue?.Count > 0)
            {
                var nextStroke = BrushStrokeRedoQueue.Last();
                Dictionary<short, bool> changedSources = new Dictionary<short, bool>();
                if (nextStroke.NewValue != 0)
                {
                    changedSources[(short)nextStroke.NewValue] = true;
                }
                foreach (var voxel in nextStroke.Voxels)
                {
                    if (voxel.Value != 0)
                    {
                        changedSources[(short)voxel.Value] = true;
                    }
                    PaintMaskVoxel(CoordsFromIndex(voxel.Index), (short)nextStroke.NewValue, false);
                }
                
                foreach (var maskVal in changedSources.Keys)
                {
                    UpdateStats(maskVal);
                }
                
                if (BrushStrokeHistory == null)
                {
                    BrushStrokeHistory = new List<BrushStrokeTransaction>();
                }
                BrushStrokeHistory.Add(nextStroke);
                BrushStrokeRedoQueue.RemoveAt(BrushStrokeRedoQueue.Count - 1);
                
                return true;
            }

            return false;
        }

        public int IndexFromCoords(int x, int y, int z)
        {
            return (x - RegionOffset.x) + (y - RegionOffset.y) * RegionCube.width + (z - RegionOffset.z) * (RegionCube.width * RegionCube.height);
        }

        public Vector3Int CoordsFromIndex(int index)
        {
            var x = index % RegionCube.width;
            index -= x;
            index /= RegionCube.width;
            var y = index % RegionCube.height;
            index -= y;
            var z = index / RegionCube.height;
            return new Vector3Int(x, y, z);
        }
        
        public int SaveSubCubeFromOriginal(Vector3Int cornerMin, Vector3Int cornerMax, VolumeDataSet maskDataSet)
        {
            IntPtr oldFitsPtr = IntPtr.Zero;
            IntPtr newFitsPtr = IntPtr.Zero;
            int status = 0;
            var directoryPath = Path.GetDirectoryName(FileName);
            var timeStamp = DateTime.Now.ToString("yyyyMMdd_Hmmss");
            var newFilename = $"{Path.GetFileNameWithoutExtension(FileName)}_subCube_{timeStamp}.fits";
            var filePath = Path.Combine(directoryPath, newFilename);
            var maskFilePath = Path.Combine(directoryPath, $"{Path.GetFileNameWithoutExtension(FileName)}_subCube_{timeStamp}_mask.fits");
            Vector3Int offset = new Vector3Int(this.subsetBounds[0], this.subsetBounds[2], this.subsetBounds[4]);
            // Works only with 3D cubes for now... need 4D askap capability
            // Fits_open_file can take a subset specified in the filename, e.g., myimage.fits[101:200,301:400]
            UnityEngine.Debug.Log("Attempting to load file:" + FileName + $"[{cornerMin.x + offset.x}:{cornerMax.x + offset.x}, {cornerMin.y + offset.y}:{cornerMax.y + offset.y}, {cornerMin.z + offset.z}:{cornerMax.z + offset.z}]");
            if (FitsReader.FitsOpenFile(out oldFitsPtr, FileName + $"[{cornerMin.x + offset.x}:{cornerMax.x + offset.x}, {cornerMin.y + offset.y}:{cornerMax.y + offset.y}, {cornerMin.z + offset.z}:{cornerMax.z + offset.z}]", out status, true) == 0)
            {
                UnityEngine.Debug.Log("Old file opened successfully.");
                if (FitsReader.FitsCreateFile(out newFitsPtr, filePath, out status) == 0)
                {
                    UnityEngine.Debug.Log("New file created successfully.");
                    FitsReader.FitsCopyFile(oldFitsPtr, newFitsPtr, out status);
                    UnityEngine.Debug.Log("File copy attempted.");
                    if (maskDataSet != null)
                    {
                        SaveSubMask(maskFilePath, cornerMin, cornerMax, newFitsPtr, maskDataSet);
                    }
                    FitsReader.FitsCloseFile(newFitsPtr, out status);
                }
                FitsReader.FitsCloseFile(oldFitsPtr, out status);
            }
            else
                UnityEngine.Debug.LogWarning("Could not open old file!");

            if (status != 0)
            {
                ToastNotification.ShowError($"Error saving sub-cube (Error #{status.ToString()})");
            }
            else
            {
                ToastNotification.ShowSuccess($"Sub-cube saved to ${newFilename}");
            }

            return status;
        }

        public int SaveSubMask(string filePath, Vector3Int cornerMin, Vector3Int cornerMax, IntPtr subCubeFitsPtr, VolumeDataSet maskDataSet)
        {
            IntPtr subMaskFilePtr = IntPtr.Zero;
            IntPtr subCubeData = IntPtr.Zero;
            int status = 0;
            if (FitsReader.FitsCreateFile(out subMaskFilePtr, filePath, out status) == 0)
            {
                if (FitsReader.FitsCopyHeader(subCubeFitsPtr, subMaskFilePtr, out status) == 0)
                {
                    if (DataAnalysis.MaskCropAndDownsample(maskDataSet.FitsData, out subCubeData, maskDataSet.XDim, maskDataSet.YDim, maskDataSet.ZDim, cornerMin.x, cornerMin.y, cornerMin.z, cornerMax.x, cornerMax.y, cornerMax.z, 1, 1, 1) == 0)
                    {
                        Vector3Int regionVector = cornerMax - cornerMin;
                        int regionVolume = regionVector.x * regionVector.y * regionVector.z;
                        IntPtr keyValue = Marshal.AllocHGlobal(sizeof(int));
                        Marshal.WriteInt32(keyValue, 16);
                        if (FitsReader.FitsUpdateKey(subMaskFilePtr, 21, "BITPIX", keyValue, null, out status) == 0)
                        {
                            if (FitsReader.FitsDeleteKey(subMaskFilePtr, "BUNIT", out status) != 0)
                            {
                                Debug.Log("Could not delete unit key. It probably does not exist!");
                                status = 0;
                            }
                            FitsReader.FitsWriteImageInt16(subMaskFilePtr, 3, regionVolume, subCubeData, out status);
                        }
                        if (keyValue != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(keyValue);
                            keyValue = IntPtr.Zero;
                        }
                    }
                }
                FitsReader.FitsCloseFile(subMaskFilePtr, out status);
            }
            if (status != 0)
                Debug.LogError($"Fits Read mask cube data error #{status.ToString()}");
            if (subCubeData != IntPtr.Zero)
                FitsReader.FreeFitsPtrMemory(subCubeData);
            return status;
        }
        public int SaveMask(IntPtr cubeFitsPtr, string filename)
        {
            int[] firstPix = new int[3];
            int[] lastPix = new int[3];
            for (int i = 0; i < 3; i++)
            {
                firstPix[i] = subsetBounds[i * 2];
                lastPix[i] = subsetBounds[i * 2 + 1];
            }
            int status = FitsReader.SaveSubMask(cubeFitsPtr, FitsData, firstPix, lastPix, filename);
            if (!string.IsNullOrEmpty(filename))
            {
                // Update filename after stripping out exclamation mark indicating overwrite flag
                FileName = filename.Replace("!", "");
            }
            Debug.Log("Wrote submask from first pixel [" + String.Join(", ", firstPix) + "] and end pixel [" + String.Join(", ", lastPix) + "]; VolumeDataSet::SaveMask() complete.");
            return status;
        }

        public string GetAstAttribute(string attributeToGet)
        {
            StringBuilder attributeReceived = new StringBuilder(70);
            StringBuilder attributeToGetSB = new StringBuilder(attributeToGet);
            if (AstTool.GetString(AstFrameSet, attributeToGetSB, attributeReceived, attributeReceived.Capacity) != 0)
            {
                Debug.Log("Cannot find attribute " + attributeToGet + " in Frame!");
                return "";
            }

            return attributeReceived.ToString();
        }
        
        public string GetAltAstAttribute(string attributeToGet)
        {
            StringBuilder attributeReceived = new StringBuilder(70);
            StringBuilder attributeToGetSB = new StringBuilder(attributeToGet);
            if (AstTool.GetString(AstAltSpecSet, attributeToGetSB, attributeReceived, attributeReceived.Capacity) != 0)
            {
                Debug.Log("Cannot find attribute " + attributeToGet + " in Frame!");
                return "";
            }

            return attributeReceived.ToString();
        }

        public string GetAstAltAttribute(string attributeToGet)
        {
            StringBuilder attributeReceived = new StringBuilder(70);
            StringBuilder attributeToGetSB = new StringBuilder(attributeToGet);
            if (AstTool.GetString(AstAltSpecSet, attributeToGetSB, attributeReceived, attributeReceived.Capacity) != 0)
            {
                Debug.Log("Cannot find attribute " + attributeToGet + " in Frame!");
                return "";
            }

            return attributeReceived.ToString();
        }

        public void GetFitsLengthsAst(Vector3 startPoint, Vector3 endPoint, out double xLength, out double yLength, out double zLength, out double angle)
        {
            IntPtr astCmpFrame = IntPtr.Zero;
            AstTool.GetAstFrame(AstFrameSet, out astCmpFrame, 2);
            double xStart, yStart, zStart, xEnd, yEnd, zEnd;
            if (AstTool.Transform3D(AstFrameSet, (double) startPoint.x, (double) startPoint.y, (double) startPoint.z, 1, out xStart, out yStart, out zStart) !=
                0 ||
                AstTool.Transform3D(AstFrameSet, (double) endPoint.x, (double) endPoint.y, (double) endPoint.z, 1, out xEnd, out yEnd, out zEnd) != 0 ||
                AstTool.Distance1D(astCmpFrame, xStart, xEnd, 1, out xLength) != 0 ||
                AstTool.Distance1D(astCmpFrame, yStart, yEnd, 2, out yLength) != 0 ||
                AstTool.Distance1D(astCmpFrame, zStart, zEnd, 3, out zLength) != 0 ||
                AstTool.Distance2D(astCmpFrame, xStart, yStart, xEnd, yEnd, out angle) != 0)
            {
                Debug.Log("Error finding WCS distances!");
                AstTool.DeleteObject(astCmpFrame);
                xLength = yLength = zLength = angle = 0;
            }
        }

        public string GetFormattedCoord(double val, int axis)
        {
            int stringLength = 70;
            StringBuilder coord = new StringBuilder(stringLength);
            if (AstTool.Format(AstFrameSet, axis, val, coord, stringLength) != 0)
            {
                Debug.Log("Error finding formatted ast coordinate!");
            }

            return coord.ToString();
        }

        public string GetFormattedAltCoord(double val)
        {
            double xDummy, yDummy, zNorm;
            if (AstTool.Norm(AstAltSpecSet, 0, 0, val, out xDummy, out yDummy, out zNorm) != 0)
            {
                Debug.Log("Error finding normalized alt spec coordinate!");
            }

            int stringLength = 70;
            StringBuilder coord = new StringBuilder(stringLength);
            if (AstTool.Format(AstAltSpecSet, 3, zNorm, coord, stringLength) != 0)
            {
                Debug.Log("Error finding formatted alt spec coordinate!");
            }

            return coord.ToString();
        }

        public string GetStdOfRest()
        {
            if (HasAstAttribute("StdOfRest"))
                return GetAstAttribute("StdOfRest");
            else
            {
                Debug.Log("No standard of rest found... defaulting to Heliocentric");
                return "Heliocentric";
            }
        }

        public string GetPixelUnit()
        {
            return PixelUnit;
        }

        public string GetAxisUnit(int axis)
        {
            return GetAstAttribute("Unit(" + axis + ")");
        }
        
        public string GetAltAxisUnit(int axis)
        {
            return GetAltAstAttribute("Unit(" + axis + ")");
        }

        public void SetAxisUnit(int axis, string unit)
        {
            SetAstAttribute("Unit(" + axis + ")", unit);
        }

        public void SetAltAxisUnit(int axis, string unit)
        {
            SetAltAstAttribute("Unit(" + axis + ")", unit);
        }
        
        public void SetAstAttribute(string attribute, string value)
        {
            StringBuilder attributeSB = new StringBuilder(attribute);
            StringBuilder valueSB = new StringBuilder(value);
            if (AstTool.SetString(AstFrameSet, attributeSB, valueSB) != 0)
            {
                Debug.Log("Cannot set attribute " + attribute + " in Frame!");
            }
        }

        public void SetAltAstAttribute(string attribute, string value)
        {
            StringBuilder attributeSB = new StringBuilder(attribute);
            StringBuilder valueSB = new StringBuilder(value);
            if (AstTool.SetString(AstAltSpecSet, attributeSB, valueSB) != 0)
            {
                Debug.Log("Cannot set attribute " + attribute + " in Frame!");
            }
        }

        public bool HasAstAttribute(string attributeToCheck)
        {
            StringBuilder attributeToCheckSB = new StringBuilder(attributeToCheck);
            return AstTool.HasAttribute(AstFrameSet, attributeToCheckSB);
        }
        
        public string GetConvertedDepth(double zIn)
        {
            if (!HasRestFrequency)
            {
                Debug.Log("Cannot convert depth without rest frequencies!");
                return "";
            }

            string system = GetAstAltAttribute("System(3)");
            string unit = GetAstAltAttribute("Unit(3)");
            double zOut, dummyX, dummyY;
            AstTool.Transform3D(AstAltSpecSet, 1, 1, zIn, 1, out dummyX, out dummyY, out zOut);
            return $"{system}: {GetFormattedAltCoord(zOut),12} {unit}";
        }

        public string GetAltSpecSystem()
        {
            string system, unit;
            GetAltSpecSystemWithUnit(out system, out unit);
            return system;
        }

        private void GetAltSpecSystemWithUnit(out string system, out string unit)
        {
            system = "";
            unit = "";
            switch (GetAstAttribute("System(3)"))
            {
                case "FREQ":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "VRAD":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "VRADIO":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "VOPT":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "VOPTICAL":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "VELO":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "VREL":
                    system = "FREQ";
                    unit = "Hz";
                    break;
                case "ENER":
                    Debug.Log("Unsupported spectral unit for depth!");
                    break;
                case "ENERGY":
                    Debug.Log("Unsupported spectral unit for depth!");
                    break;
                case "WAVN":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "WAVENUM":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "WAVE":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "WAVELEN":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "AWAV":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "AIRWAVE":
                    system = "VRAD";
                    unit = "km/s";
                    break;
                case "ZOPT":
                    Debug.Log("Unsupported spectral unit for depth!");
                    break;
                case "REDSHIFT":
                    Debug.Log("Unsupported spectral unit for depth!");
                    break;
                case "BETA":
                    Debug.Log("Unsupported spectral unit for depth!");
                    break;
            }
        }

        public void GetFitsCoordsAst(double X, double Y, double Z, out double fitsX, out double fitsY, out double fitsZ)
        {
            if (AstTool.Transform3D(AstFrameSet, X, Y, Z, 1, out fitsX, out fitsY, out fitsZ) != 0)
            {
                Debug.Log("Error transforming sky pixel to physical coordinates!");
            }
        }

        public void GetNormCoords(double X, double Y, double Z, out double normX, out double normY, out double normZ)
        {
            if (AstTool.Norm(AstFrameSet, X, Y, Z, out normX, out normY, out normZ) != 0)
            {
                Debug.Log("Error normalizing physical coordinates!");
            }
        }

        public bool isSubset()
        {
            return this.loadSubset;
        }
        
        public void CleanUp(bool randomCube)
        {
            int status = 0;
            if (FitsData != IntPtr.Zero)
            {
                if (randomCube)
                    Marshal.FreeHGlobal(FitsData);
                else
                    FitsReader.FreeFitsPtrMemory(FitsData);
                FitsData = IntPtr.Zero;
            }
            if (FitsHeader != IntPtr.Zero)
            {
                FitsReader.FreeFitsMemory(FitsHeader, out status);
                FitsHeader = IntPtr.Zero;
            }

            if (AstFrameSet != IntPtr.Zero)
            {
                AstTool.DeleteObject(AstFrameSet);
                AstFrameSet = IntPtr.Zero;
            }

            ExistingMaskBuffer?.Release();
            AddedMaskBuffer?.Release();
            
            Object.DestroyImmediate(DataCube);
            Object.DestroyImmediate(RegionCube);
        }
    }
}
