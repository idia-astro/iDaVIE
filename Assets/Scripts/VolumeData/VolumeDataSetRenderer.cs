using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DataFeatures;
using LineRenderer;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Numerics;

namespace VolumeData
{
    public enum ScalingType
    {
        Linear = 0,
        Log = 1,
        Sqrt = 2,
        Square = 3,
        Power = 4,
        Gamma = 5
    }

    public enum MaskMode
    {
        Disabled = 0,
        Enabled = 1,
        Inverted = 2,
        Isolated = 3
    }

    public enum ProjectionMode
    {
        MaximumIntensityProjection = 0,
        AverageIntensityProjection = 1
    }

    public class VolumeDataSetRenderer : MonoBehaviour
    {
        public ColorMapDelegate OnColorMapChanged;

        [Header("Rendering Settings")]
        // Step control
        [Range(16, 512)] public int MaxSteps = 192;
        public long MaximumCubeSizeInMB = 250;
        public ProjectionMode ProjectionMode = ProjectionMode.MaximumIntensityProjection;
        public FilterMode TextureFilter = FilterMode.Point;
        [Range(0, 1)] public float Jitter = 1.0f;

        [Header("Mask Rendering and Editing Settings")]
        public bool DisplayMask = false;
        public MaskMode MaskMode = MaskMode.Disabled;
        [Range(0, 1)] public float MaskVoxelSize = 1.0f;
        public Color MaskVoxelColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        // Foveated rendering controls
        [Header("Foveated Rendering Controls")]
        public bool FoveatedRendering = false;

        [Range(0, 0.5f)] public float FoveationStart = 0.15f;
        [Range(0, 0.5f)] public float FoveationEnd = 0.40f;
        [Range(0, 0.5f)] public float FoveationJitter = 0.0f;
        [Range(16, 512)] public int FoveatedStepsLow = 64;
        [Range(16, 512)] public int FoveatedStepsHigh = 384;

        // Vignette Rendering
        [Header("Vignette Rendering Controls")]
        [Range(0, 0.5f)]
        public float VignetteFadeStart = 0.15f;

        [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
        [Range(0, 1)] public float VignetteIntensity = 0.0f;
        public Color VignetteColor = Color.black;

        [Header("Thresholds")] [Range(0, 1)] public float ThresholdMin = 0;
        [Range(0, 1)] public float ThresholdMax = 1;
        
        [Header("Color Mapping")] public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public ScalingType ScalingType = ScalingType.Linear;
        [Range(-1, 1)] public float ScalingBias = 0.0f;
        [Range(0, 5)] public float ScalingContrast = 1.0f;
        public float ScalingAlpha = 1000.0f;
        [Range(0, 5)] public float ScalingGamma = 1.0f;

        [Header("File Input")] public string FileName;
        public string MaskFileName;
        public int[] subsetBounds = {0, 1, 0, 1, 0, 1};//All cubes loaded as sub cube

        public int[] trueBounds;
        public Material RayMarchingMaterial;
        public Material MaskMaterial;

        [Header("Debug Settings")] public bool FactorOverride = false;
        public int XFactor = 1;
        public int YFactor = 1;
        public int ZFactor = 1;
        public float ScaleMax;
        public float ScaleMin;
        public float ZAxisMaxFactor = 10.0f;
        public float ZAxisMinFactor = 0.001f;
        public UnityEngine.Vector3 SliceMin = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 SliceMax = UnityEngine.Vector3.one;
        public int CubeDepthAxis = 2;
        public int CubeSlice = 1;
        public bool ShowMeasuringLine = false;
        public bool OverrideRestFrequency {get; set;} = false;
        private double _restFrequency;
        public double RestFrequency
        {
            get => _restFrequency;
            set
            {
                _restFrequencyChanged = true;
                _restFrequency = value;
            }
        }
        private bool _restFrequencyChanged = false;
        public UnityEngine.Vector3 InitialPosition { get; private set; }
        public UnityEngine.Quaternion InitialRotation { get; private set; }
        public UnityEngine.Vector3 InitialScale { get; private set; }

        public float InitialThresholdMin { get; private set; }
        public float InitialThresholdMax { get; private set; }

        public Vector3Int CursorVoxel { get; private set; }
        public float CursorValue { get; private set; }
        public Int16 CursorSource { get; private set; }
        public Int16 HighlightedSource;
        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel { get; private set; }

        public TextMeshProUGUI loadText;
        public Slider progressBar;

        [Range(0, 1)] public float SelectionSaturateFactor = 0.7f;

        public FeatureSetManager FeatureSetManagerPrefab;

        private PolyLine _measuringLine;
        private CuboidLine _cubeOutline, _voxelOutline, _regionOutline;

        private FeatureSetManager _featureManager = null;

        private MeshRenderer _renderer;
        private Material _materialInstance;
        private Material _maskMaterialInstance;
        private MomentMapRenderer _momentMapRenderer;

        public VolumeInputController _volumeInputController;

        private Vector3Int _previousPaintLocation;
        private short _previousPaintValue;
        private int _previousBrushSize = 1;

        private VolumeDataSet _dataSet = null;
        private VolumeDataSet _maskDataSet = null;

        private string lastSavedMaskPath = "";
        public VolumeDataSet Mask => _maskDataSet;
        public VolumeDataSet Data => _dataSet;
        
        private bool _dirtyMask = false;

        public bool HasWCS { get; private set; }
        public IntPtr AstFrame { get =>_dataSet.AstFrameSet; } 
        public string StdOfRest => _dataSet.GetStdOfRest();

        private int _currentXFactor, _currentYFactor, _currentZFactor;
        public bool IsFullResolution => _currentXFactor * _currentYFactor * _currentZFactor == 1;

        private int _baseXFactor, _baseYFactor, _baseZFactor;
        public bool IsCropped { get; private set; }

        public Vector3Int CurrentCropMin { get; private set; }
        public Vector3Int CurrentCropMax { get; private set; }

        public Dictionary<int, DataAnalysis.SourceStats> SourceStatsDict
        { 
            get 
            {
                return _dataSet.SourceStatsDict;
            }
        }

        [Header("Benchmarking")]
        public bool RandomVolume = false;
        public int RandomCubeSize = 512;
        
        #region Material Property IDs
        private struct MaterialID
        {
            public static readonly int DataCube = Shader.PropertyToID("_DataCube");
            public static readonly int MaskCube = Shader.PropertyToID("MaskCube");
            public static readonly int MaskMode = Shader.PropertyToID("MaskMode");
            public static readonly int NumColorMaps = Shader.PropertyToID("_NumColorMaps");
            public static readonly int SliceMin = Shader.PropertyToID("_SliceMin");
            public static readonly int SliceMax = Shader.PropertyToID("_SliceMax");
            public static readonly int ThresholdMin = Shader.PropertyToID("_ThresholdMin");
            public static readonly int ThresholdMax = Shader.PropertyToID("_ThresholdMax");
            public static readonly int Jitter = Shader.PropertyToID("_Jitter");
            public static readonly int MaxSteps = Shader.PropertyToID("_MaxSteps");
            public static readonly int ColorMapIndex = Shader.PropertyToID("_ColorMapIndex");
            public static readonly int ScaleMin = Shader.PropertyToID("_ScaleMin");
            public static readonly int ScaleMax = Shader.PropertyToID("_ScaleMax");

            public static readonly int ScaleType = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleBias = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast = Shader.PropertyToID("ScaleContrast");
            public static readonly int ScaleAlpha = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma = Shader.PropertyToID("ScaleGamma");

            public static readonly int FoveationStart = Shader.PropertyToID("FoveationStart");
            public static readonly int FoveationEnd = Shader.PropertyToID("FoveationEnd");
            public static readonly int FoveationJitter = Shader.PropertyToID("FoveationJitter");
            public static readonly int FoveatedStepsLow = Shader.PropertyToID("FoveatedStepsLow");
            public static readonly int FoveatedStepsHigh = Shader.PropertyToID("FoveatedStepsHigh");

            public static readonly int VignetteFadeStart = Shader.PropertyToID("VignetteFadeStart");
            public static readonly int VignetteFadeEnd = Shader.PropertyToID("VignetteFadeEnd");
            public static readonly int VignetteIntensity = Shader.PropertyToID("VignetteIntensity");
            public static readonly int VignetteColor = Shader.PropertyToID("VignetteColor");

            public static readonly int HighlightMin = Shader.PropertyToID("HighlightMin");
            public static readonly int HighlightMax = Shader.PropertyToID("HighlightMax");
            public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");

            public static readonly int CubeDimensions = Shader.PropertyToID("CubeDimensions");
            public static readonly int RegionDimensions = Shader.PropertyToID("RegionDimensions");
            public static readonly int RegionOffset = Shader.PropertyToID("RegionOffset");
            public static readonly int MaskEntries = Shader.PropertyToID("MaskEntries");
            public static readonly int MaskVoxelSize = Shader.PropertyToID("MaskVoxelSize");
            public static readonly int MaskVoxelOffsets = Shader.PropertyToID("MaskVoxelOffsets");
            public static readonly int MaskVoxelColor = Shader.PropertyToID("MaskVoxelColor");
            public static readonly int ModelMatrix = Shader.PropertyToID("ModelMatrix");
            public static readonly int HighlightedSource = Shader.PropertyToID("HighlightedSource");
        }

        #endregion

        [Header("Miscellaneous")]
        public bool started = false;

        public bool FileChanged = true;
		
        public float XScale
        {
            get
            {
                return gameObject.transform.localScale.x;
            }
            set
            {
                UnityEngine.Vector3 oldScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new UnityEngine.Vector3(value, oldScale.y, oldScale.z);
            }
        }
        
        public float YScale
        {
            get
            {
                return gameObject.transform.localScale.y;
            }
            set
            {
                UnityEngine.Vector3 oldScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new UnityEngine.Vector3(oldScale.x, value, oldScale.z);
            }
        }
        
        public float ZScale
        {
            get
            {
                return gameObject.transform.localScale.z;
            }
            set
            {
                UnityEngine.Vector3 oldScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new UnityEngine.Vector3(oldScale.x, oldScale.y, value);
            }
        }

        public void Start()
        {
            started = false;
        }

        public IEnumerator _startFunc()
        {
            // Apply settings from config
            var config = Config.Instance;
            Debug.Log("Loading data for the new cube.");
            StartCoroutine(updateStatus("Loading new cube...", 3));
            yield return new WaitForSeconds(0.001f);
            TextureFilter = config.bilinearFiltering ? FilterMode.Bilinear : FilterMode.Point;
            FoveatedRendering = config.foveatedRendering;
            MaxSteps = config.maxRaymarchingSteps;
            FoveatedStepsHigh = config.maxRaymarchingSteps;
            MaximumCubeSizeInMB = config.gpuMemoryLimitMb;
            ColorMap = config.defaultColorMap;
            ScalingType = config.defaultScalingType;
            VignetteFadeEnd = config.tunnellingVignetteEnd;
            
            if (RandomVolume)
                _dataSet = VolumeDataSet.LoadRandomFitsCube(0, RandomCubeSize, RandomCubeSize, RandomCubeSize, RandomCubeSize);
            else
                //subsetBounds guaranteed to be full cube if not selected by user
                _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName, subsetBounds, trueBounds, IntPtr.Zero, CubeDepthAxis, CubeSlice);

            _volumeInputController = FindObjectOfType<VolumeInputController>();
            _featureManager = GetComponentInChildren<FeatureSetManager>();
            if (_featureManager == null)
                Debug.Log($"No FeatureManager attached to VolumeDataSetRenderer. Attach prefab for use of Features.");
            GenerateDownsampledCube();
            _baseXFactor = _currentXFactor;
            _baseYFactor = _currentYFactor;
            _baseZFactor = _currentZFactor;
            ScaleMax = _dataSet.MaxValue;
            ScaleMin = _dataSet.MinValue;
            
            Debug.Log("Loading image data complete, loading data for the mask.");
            StartCoroutine(updateStatus("Loading mask...", 4));
            yield return new WaitForSeconds(0.001f);

            if (!String.IsNullOrEmpty(MaskFileName))
            {
                //subsetBounds guaranteed to be full cube if not selected by user
                _maskDataSet = VolumeDataSet.LoadDataFromFitsFile(MaskFileName, subsetBounds, trueBounds, _dataSet.FitsData);
             
                // Mask data is always point-filtered
                _maskDataSet.GenerateVolumeTexture(FilterMode.Point, XFactor, YFactor, ZFactor);
            }

            Debug.Log("Loading mask data complete.");
            StartCoroutine(updateStatus("Preparing UI...", 5));
            yield return new WaitForSeconds(0.001f);

            _renderer = GetComponent<MeshRenderer>();
            _materialInstance = Instantiate(RayMarchingMaterial);
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.DataCube);
            _materialInstance.SetInt(MaterialID.NumColorMaps, ColorMapUtils.NumColorMaps);
            _materialInstance.SetFloat(MaterialID.FoveationStart, FoveationStart);
            _materialInstance.SetFloat(MaterialID.FoveationEnd, FoveationEnd);
            _maskMaterialInstance = Instantiate(MaskMaterial);

            if (_maskDataSet != null)
            {
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.DataCube);
            }
            _renderer.material = _materialInstance;

            // Set initial values (for resetting later)
            InitialPosition = transform.position;
            InitialScale = transform.localScale;
            InitialRotation = transform.rotation;
            InitialThresholdMax = ThresholdMax;
            InitialThresholdMin = ThresholdMin;

            _cubeOutline = new CuboidLine
            {
                Parent = transform,
                Center = UnityEngine.Vector3.zero,
                Bounds = UnityEngine.Vector3.one
            };
            
            _cubeOutline.Activate();
            Feature.SetCubeColors(_cubeOutline, Color.white, true);

            // Voxel axes
            CursorVoxel = new Vector3Int(-1, -1, -1);
            _voxelOutline = new CuboidLine
            {
                Parent = transform,
                Center = UnityEngine.Vector3.zero,
                Color = Color.green,
                Bounds = UnityEngine.Vector3.one
            };
            
            // Region axes
            _regionOutline = new CuboidLine
            {
                Parent = transform,
                Center = UnityEngine.Vector3.zero,
                Color = Color.green,
                Bounds = UnityEngine.Vector3.one
            };
            
            _measuringLine = new PolyLine
            {
                Parent = transform,
                Color = Color.white,
            };

            if (_featureManager != null)
            {
                _featureManager.CreateSelectionFeatureSet();
                if (_maskDataSet != null)
                {
                    var featureSet = _featureManager.CreateNewFeatureSet();
                    _maskDataSet?.FillFeatureSet(featureSet);
                }
            }

            //No wcs info if AstFrameSet has only 1 frame
            if (_dataSet.AstFrameSet != IntPtr.Zero)
                if (_dataSet.HasAstAttribute("Nframe"))
                    HasWCS = int.Parse(_dataSet.GetAstAttribute("Nframe")) > 1;
                else
                    HasWCS = false;
            else
                HasWCS = false;

            if (HasWCS)
            {
                Debug.Log("WCS loaded successfully!");
                Debug.Log($"x-axis unit is {_dataSet.GetAxisUnit(1)}");
                Debug.Log($"y-axis unit is {_dataSet.GetAxisUnit(2)}");
                Debug.Log($"z-axis unit is {_dataSet.GetAxisUnit(3)}");
                Debug.Log($"alternative z-axis unit is {_dataSet.GetAltAxisUnit(3)}");
            }
            else
            {
                Debug.Log("Problem loading WCS.");
            }
            
            if (_dataSet.HasFitsRestFrequency)
            {
                RestFrequency = _dataSet.FitsRestFrequency;
            }
            
            _momentMapRenderer = gameObject.AddComponent(typeof(MomentMapRenderer)) as MomentMapRenderer;
            if (_momentMapRenderer)
            {
                _momentMapRenderer.DataCube = _dataSet.DataCube;
                _momentMapRenderer.Inverted = _dataSet.VelocityDirection == 1;
                _momentMapRenderer.momentMapMenuController = FindObjectOfType<VolumeCommandController>().momentMapMenuController;

                if (_maskDataSet != null)
                {
                    _momentMapRenderer.MaskCube = _maskDataSet.DataCube;
                }    
            }
            
            if (IsFullResolution)
            {
                CropToRegion(UnityEngine.Vector3.one, new UnityEngine.Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
            }
            
            Shader.WarmupAllShaders();

            CurrentCropMin = new Vector3Int(0, 0, 0);
            CurrentCropMax = new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);

            started = true;
            yield return 0;
        }

        public IEnumerator updateStatus(string label, int progress)
        {
            loadText.text = label;
            progressBar.value = progress;
            yield return new WaitForSeconds(0.001f);
        }

        private void GenerateDownsampledCube()
        {
            if (!FactorOverride)
            {
                _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, out XFactor, out YFactor, out ZFactor);
            }

            _dataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
            _currentXFactor = XFactor;
            _currentYFactor = YFactor;
            _currentZFactor = ZFactor;
        }

        public void RegenerateCubes()
        {
            // Regenerate image and region cubes after mode changed
            GenerateDownsampledCube();
            if (IsCropped)
            {
                CropToFeature();
            } else if (IsFullResolution)
            {
                CropToRegion(UnityEngine.Vector3.one, new UnityEngine.Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
            }
        }
        
        public VolumeDataSet GetDataSet()
        {
            return _dataSet;
        }


        public MomentMapRenderer GetMomentMapRenderer()
        {
            return _momentMapRenderer;
        }


        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            ColorMap = ColorMapUtils.FromHashCode(newIndex);
        }

        /// <summary>
        /// A function that calculates the cursor position and sends it to both the cursor, and the information sent on hover. Note the information on hover
        /// is in the data space, so needs an offset if a subset was loaded, while the cursor is in object/VR space, so real location (no offset).
        /// </summary>
        /// <param name="cursor"></param>
        /// <param name="brushSize"></param>
        public void SetCursorPosition(UnityEngine.Vector3 cursor, int brushSize)
        {
            UnityEngine.Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor);
            Bounds objectBounds = new Bounds(UnityEngine.Vector3.zero, UnityEngine.Vector3.one);
            if (objectBounds.Contains(objectSpacePosition) && _dataSet != null)
            {
                // Always a subset, so apply a suitable offset to the position in data space
                int xOffset, yOffset, zOffset;
                zOffset = yOffset = xOffset = 0 ;

                xOffset = _dataSet.subsetBounds[0] - 1;
                yOffset = _dataSet.subsetBounds[2] - 1;
                zOffset = _dataSet.subsetBounds[4] - 1;
                
                UnityEngine.Vector3 positionCubeSpace = new UnityEngine.Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim + xOffset,
                                                                                (objectSpacePosition.y + 0.5f) * _dataSet.YDim + yOffset,
                                                                                (objectSpacePosition.z + 0.5f) * _dataSet.ZDim + zOffset);
                UnityEngine.Vector3 voxelCornerCubeSpace = new UnityEngine.Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
                UnityEngine.Vector3 voxelCenterCubeSpace = voxelCornerCubeSpace + 0.5f * UnityEngine.Vector3.one;
                Vector3Int newVoxelCursor = new Vector3Int(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1);

                if (!newVoxelCursor.Equals(CursorVoxel) || brushSize != _previousBrushSize)
                {
                    _previousBrushSize = brushSize;
                    CursorVoxel = newVoxelCursor;
                    CursorValue = _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    if (/*GlobalDebugIsOn &&*/Double.IsNaN(CursorValue))
                    {
                        // Debug.Log("NAN value at CursorVoxel [" + CursorVoxel.x.ToString() + ", " + CursorVoxel.y.ToString() + ", " + CursorVoxel.z.ToString() + "]!");
                    }
                    if (_maskDataSet != null)
                    {
                        // Debug.Log("Trying to access mask value at [" + CursorVoxel.x.ToString() + ", " + CursorVoxel.y.ToString() + ", " + CursorVoxel.z.ToString() + "].");
                        CursorSource = _maskDataSet.GetMaskValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    }
                    else
                    {
                        CursorSource = 0;
                    }

                    // Determine the actual voxelCursor's location in the VR space (not data space)
                    UnityEngine.Vector3 voxelCenterObjectSpace = new UnityEngine.Vector3((voxelCenterCubeSpace.x - xOffset) / _dataSet.XDim - 0.5f,
                                                                                         (voxelCenterCubeSpace.y - yOffset) / _dataSet.YDim - 0.5f,
                                                                                         (voxelCenterCubeSpace.z - zOffset) / _dataSet.ZDim - 0.5f);
                    _voxelOutline.Center = voxelCenterObjectSpace;
                    _voxelOutline.Bounds = brushSize * new UnityEngine.Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
                }

                _voxelOutline.Activate();
            }
            else
            {
                _voxelOutline?.Deactivate();
                CursorValue = float.NaN;
                CursorVoxel = new Vector3Int(-1, -1, -1);
            }
        }

        /// <summary>
        /// This function is used to get the cursor location in data space (i.e., including subcube offsets).
        /// </summary>
        /// <param name="cursorPosWorldSpace">Where in the world space is the user pointing?</param>
        /// <returns>An indexed location in data space.</returns>
        public Vector3Int GetVoxelPosition(UnityEngine.Vector3 cursorPosWorldSpace)
        {
            UnityEngine.Vector3 objectSpacePosition = transform.InverseTransformPoint(cursorPosWorldSpace);
            objectSpacePosition = new UnityEngine.Vector3(
                Mathf.Clamp(objectSpacePosition.x, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.y, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.z, -0.5f, 0.5f)
            );

            if (_dataSet == null)
            {
                return Vector3Int.zero;
            }
            
            //Apply offset from subset bounds
            int xOffset, yOffset, zOffset;
            zOffset = yOffset = xOffset = 0 ;
            xOffset = _dataSet.subsetBounds[0] - 1;
            yOffset = _dataSet.subsetBounds[2] - 1;
            zOffset = _dataSet.subsetBounds[4] - 1;
            
            UnityEngine.Vector3 positionCubeSpace = new UnityEngine.Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim + xOffset,
                                                                            (objectSpacePosition.y + 0.5f) * _dataSet.YDim + yOffset,
                                                                            (objectSpacePosition.z + 0.5f) * _dataSet.ZDim + zOffset);
            UnityEngine.Vector3 voxelCornerCubeSpace = new UnityEngine.Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
            Vector3Int newVoxelCursor = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int)_dataSet.XDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int)_dataSet.YDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int)_dataSet.ZDim)
            );

            return newVoxelCursor;
        }
        
        /// <summary>
        /// This function is to be used to get the cursor location in world space (as compared to data space above)
        /// </summary>
        /// <param name="cursorPosWorldSpace">Where in the world space is the user pointing?</param>
        /// <returns>An indexed location in world space.</returns>
        public Vector3Int GetVoxelPositionWorldSpace(UnityEngine.Vector3 cursorPosWorldSpace)
        {
            UnityEngine.Vector3 objectSpacePosition = transform.InverseTransformPoint(cursorPosWorldSpace);
            objectSpacePosition = new UnityEngine.Vector3(
                Mathf.Clamp(objectSpacePosition.x, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.y, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.z, -0.5f, 0.5f)
            );

            if (_dataSet == null)
            {
                return Vector3Int.zero;
            }
            
            UnityEngine.Vector3 positionCubeSpace = new UnityEngine.Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim,
                                                                            (objectSpacePosition.y + 0.5f) * _dataSet.YDim,
                                                                            (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
            UnityEngine.Vector3 voxelCornerCubeSpace = new UnityEngine.Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
            Vector3Int newVoxelCursor = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int)_dataSet.XDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int)_dataSet.YDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int)_dataSet.ZDim)
            );

            return newVoxelCursor;
        }

        /// <summary>
        /// This function sets the bounds of the selected region in world space.
        /// </summary>
        /// <param name="cursor">Where in the world space is the user pointing?</param>
        /// <param name="start">Is this the creation of a selection, or updating its values?</param>
        public void SetRegionPosition(UnityEngine.Vector3 cursor, bool start)
        {
            if (_dataSet != null)
            {
                var newVoxelCursor = GetVoxelPositionWorldSpace(cursor);
                var existingVoxel = start ? RegionStartVoxel : RegionEndVoxel;
                if (!newVoxelCursor.Equals(existingVoxel))
                {
                    if (start)
                    {
                        RegionStartVoxel = newVoxelCursor;
                    }
                    else
                    {
                        RegionEndVoxel = newVoxelCursor;
                    }

                    UpdateRegionBounds();

                }

                _regionOutline.Activate();
                if (ShowMeasuringLine)
                {
                    _measuringLine?.Activate();
                }
                    
            }
        }

        public void SetRegionBounds(Vector3Int min, Vector3Int max, bool drawRegion)
        {
            RegionStartVoxel = min;
            RegionEndVoxel = max;
            if (drawRegion)
                UpdateRegionBounds();
        }

        private void UpdateRegionBounds()
        {
            // Calculate full region bounds
            var regionMin = UnityEngine.Vector3.Min(RegionStartVoxel, RegionEndVoxel);
            var regionMax = UnityEngine.Vector3.Max(RegionStartVoxel, RegionEndVoxel);
            var measureStart = RegionStartVoxel;
            var measureEnd = RegionEndVoxel;
            if (measureStart.x < measureEnd.x)
                measureStart.x--;
            else
                measureEnd.x--;
            if (measureStart.y < measureEnd.y)
                measureStart.y--;
            else
                measureEnd.y--;
            if (measureStart.z < measureEnd.z)
                measureStart.z--;
            else
                measureEnd.z--;
            var regionSize = regionMax - regionMin + UnityEngine.Vector3.one;
            UnityEngine.Vector3 regionCenter = (regionMax + regionMin) / 2.0f - 0.5f * UnityEngine.Vector3.one;

            UnityEngine.Vector3 regionCenterObjectSpace = new UnityEngine.Vector3(regionCenter.x / _dataSet.XDim - 0.5f, regionCenter.y / _dataSet.YDim - 0.5f, regionCenter.z / _dataSet.ZDim - 0.5f);
            _regionOutline.Center = regionCenterObjectSpace;
            _regionOutline.Bounds = new UnityEngine.Vector3(regionSize.x / _dataSet.XDim, regionSize.y / _dataSet.YDim, regionSize.z / _dataSet.ZDim);

            var regionSizeBytes = regionSize.x * regionSize.y * regionSize.z * sizeof(float);
            bool regionIsFullResolution = (regionSizeBytes <= MaximumCubeSizeInMB * 1e6);
            Feature.SetCubeColors(_regionOutline, regionIsFullResolution ? Color.white : Color.yellow, regionIsFullResolution);

            var startPoint = new UnityEngine.Vector3((float)measureStart.x / _dataSet.XDim - 0.5f, (float)measureStart.y / _dataSet.YDim - 0.5f, (float)measureStart.z / _dataSet.ZDim - 0.5f);
            var endPoint = new UnityEngine.Vector3((float)measureEnd.x / _dataSet.XDim - 0.5f, (float)measureEnd.y / _dataSet.YDim - 0.5f, (float)measureEnd.z / _dataSet.ZDim - 0.5f);
            _measuringLine.Vertices = new List<UnityEngine.Vector3> { startPoint, endPoint };
        }

        public void ClearRegion()
        {
            _regionOutline?.Deactivate();
        }

        public void ClearMeasure()
        {
            _measuringLine?.Deactivate();
        }

        public void SelectFeature(UnityEngine.Vector3 cursor)
        {
            if (_featureManager && _featureManager.SelectFeature(cursor))
            {
                Debug.Log($"Selected feature '{_featureManager.SelectedFeature.Name}'");
                SetRegionBounds(Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMinBounds()), Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMaxBounds()), false);
            }
        }

        public void SelectFeature(Feature feature)
        {
            if (_featureManager)
            {
                _featureManager.SelectedFeature = feature;
                Debug.Log($"Selected feature '{_featureManager.SelectedFeature.Name}'");
                SetRegionBounds(Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMinBounds()), Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMaxBounds()), false);
            }
        }

        public bool CropToFeature()
        {
            if (_featureManager != null && _featureManager.SelectedFeature != null)
            {
                var cornerMin = _featureManager.SelectedFeature.CornerMin;
                var cornerMax = _featureManager.SelectedFeature.CornerMax;
                CropToRegion(cornerMin, cornerMax);
                return true;
            }

            return false;
        }

        public void CropToRegion(UnityEngine.Vector3 cornerMin, UnityEngine.Vector3 cornerMax)
        {
            Vector3Int startVoxel = new Vector3Int(Convert.ToInt32(cornerMin.x), Convert.ToInt32(cornerMin.y), Convert.ToInt32(cornerMin.z));
            Vector3Int endVoxel = new Vector3Int(Convert.ToInt32(cornerMax.x), Convert.ToInt32(cornerMax.y), Convert.ToInt32(cornerMax.z));

            UnityEngine.Vector3 regionStartObjectSpace = new UnityEngine.Vector3(cornerMin.x / _dataSet.XDim - 0.5f, cornerMin.y / _dataSet.YDim - 0.5f, cornerMin.z / _dataSet.ZDim - 0.5f);
            UnityEngine.Vector3 regionEndObjectSpace = new UnityEngine.Vector3(cornerMax.x / _dataSet.XDim - 0.5f, cornerMax.y / _dataSet.YDim - 0.5f, cornerMax.z / _dataSet.ZDim - 0.5f);
            UnityEngine.Vector3 padding = new UnityEngine.Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
            SliceMin = UnityEngine.Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
            SliceMax = UnityEngine.Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);
            LoadRegionData(startVoxel, endVoxel);
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.RegionCube);

            if (_maskDataSet != null)
            {
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
                var regionMin = UnityEngine.Vector3.Min(startVoxel, endVoxel);
                _maskMaterialInstance.SetVector(MaterialID.RegionOffset, new UnityEngine.Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
                var regionDimensions = new UnityEngine.Vector4(_maskDataSet.RegionCube.width, _maskDataSet.RegionCube.height, _maskDataSet.RegionCube.depth, 0);
                _maskMaterialInstance.SetVector(MaterialID.RegionDimensions, regionDimensions);
                var cubeDimensions = new UnityEngine.Vector4(_maskDataSet.XDim, _maskDataSet.YDim, _maskDataSet.ZDim, 1);
                _maskMaterialInstance.SetVector(MaterialID.CubeDimensions, cubeDimensions);
                _momentMapRenderer.MaskCube = _maskDataSet.RegionCube;
            }
            
            CurrentCropMin = new Vector3Int(startVoxel.x, startVoxel.y, startVoxel.z);
            CurrentCropMax = new Vector3Int(endVoxel.x, endVoxel.y, endVoxel.z);
            
            _momentMapRenderer.DataCube = _dataSet.RegionCube;

            IsCropped = true;
        }

        public void ResetCrop()
        {
            _currentXFactor = _baseXFactor;
            _currentYFactor = _baseYFactor;
            _currentZFactor = _baseZFactor;
            SliceMin = -0.5f * UnityEngine.Vector3.one;
            SliceMax = +0.5f * UnityEngine.Vector3.one;
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.DataCube);
            if (_maskDataSet != null)
            {
                if (IsFullResolution)
                {
                    _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, Vector3Int.one, 
                        new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim), Vector3Int.one);
                    _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
                    var regionMin = UnityEngine.Vector3.Min(Vector3Int.one, new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim));
                    _maskMaterialInstance.SetVector(MaterialID.RegionOffset, new UnityEngine.Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
                    var regionDimensions = new UnityEngine.Vector4(_maskDataSet.RegionCube.width, _maskDataSet.RegionCube.height, _maskDataSet.RegionCube.depth, 0);
                    _maskMaterialInstance.SetVector(MaterialID.RegionDimensions, regionDimensions);
                    var cubeDimensions = new UnityEngine.Vector4(_maskDataSet.XDim, _maskDataSet.YDim, _maskDataSet.ZDim, 1);
                    _maskMaterialInstance.SetVector(MaterialID.CubeDimensions, cubeDimensions);
                    _momentMapRenderer.MaskCube = _maskDataSet.RegionCube;
                }
                _maskDataSet.ConsolidateDownsampledMask();
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.DataCube);
                ComputeBuffer nullBuffer = null;
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, nullBuffer);
                _momentMapRenderer.MaskCube = _maskDataSet.DataCube;
            }
            _momentMapRenderer.DataCube = _dataSet.DataCube;
            IsCropped = false;
        }

        public void LoadRegionData(Vector3Int startVoxel, Vector3Int endVoxel)
        {
            Vector3Int deltaRegion = startVoxel - endVoxel;
            Vector3Int regionSize = new Vector3Int(Math.Abs(deltaRegion.x) + 1, Math.Abs(deltaRegion.y) + 1, Math.Abs(deltaRegion.z) + 1);
            _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, regionSize.x, regionSize.y, regionSize.z, out _currentXFactor, out _currentYFactor,
                out _currentZFactor);
            _dataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel, new Vector3Int(_currentXFactor, _currentYFactor, _currentZFactor));
            if (_maskDataSet != null)
            {
                _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel,
                    new Vector3Int(_currentXFactor, _currentYFactor, _currentZFactor));
            }

            string resolutionString;
            if (_currentXFactor * _currentYFactor * _currentZFactor == 1)
            {
                resolutionString = "Data is full resolution";
            }
            else
            {
                resolutionString = $"Downsampled by ({_currentXFactor} \u00D7 {_currentYFactor} \u00D7 {_currentZFactor})";
            }

            string cropString;
            if (regionSize.x == _dataSet.XDim && regionSize.y == _dataSet.YDim && regionSize.z == _dataSet.ZDim)
            {
                cropString = "Showing entire cube";
            }
            else
            {
                cropString = $"Cropped to ({regionSize.x} \u00D7 {regionSize.y} \u00D7 {regionSize.z}) region";
            }

            ToastNotification.ShowInfo($"{cropString}\n{resolutionString}");
        }

        public void TeleportToRegion()
        {
            if (_volumeInputController && _featureManager && _featureManager.SelectedFeature != null)
            {
                var boundsMin = _featureManager.SelectedFeature.CornerMin;
                var boundsMax = _featureManager.SelectedFeature.CornerMax;
                _volumeInputController.Teleport(boundsMin - (0.5f * UnityEngine.Vector3.one), boundsMax + (0.5f * UnityEngine.Vector3.one));
            }
        }

        // Update is called once per frame
        public void Update()
        {
            _materialInstance.SetVector(MaterialID.SliceMin, SliceMin);
            _materialInstance.SetVector(MaterialID.SliceMax, SliceMax);
            _materialInstance.SetFloat(MaterialID.ThresholdMin, ThresholdMin);
            _materialInstance.SetFloat(MaterialID.ThresholdMax, ThresholdMax);
            _materialInstance.SetFloat(MaterialID.Jitter, Jitter);
            _materialInstance.SetFloat(MaterialID.MaxSteps, MaxSteps);
            _materialInstance.SetFloat(MaterialID.ColorMapIndex, ColorMap.GetHashCode());
            _materialInstance.SetFloat(MaterialID.ScaleMax, ScaleMax);
            _materialInstance.SetFloat(MaterialID.ScaleMin, ScaleMin);

            _materialInstance.SetInt(MaterialID.ScaleType, ScalingType.GetHashCode());
            _materialInstance.SetFloat(MaterialID.ScaleBias, ScalingBias);
            _materialInstance.SetFloat(MaterialID.ScaleContrast, ScalingContrast);
            _materialInstance.SetFloat(MaterialID.ScaleAlpha, ScalingAlpha);
            _materialInstance.SetFloat(MaterialID.ScaleGamma, ScalingGamma);

            _materialInstance.SetFloat(MaterialID.FoveationStart, FoveationStart);
            _materialInstance.SetFloat(MaterialID.FoveationEnd, FoveationEnd);
            if (FoveatedRendering)
            {
                _materialInstance.SetFloat(MaterialID.FoveationJitter, FoveationJitter);
                _materialInstance.SetInt(MaterialID.FoveatedStepsLow, FoveatedStepsLow);
                _materialInstance.SetInt(MaterialID.FoveatedStepsHigh, FoveatedStepsHigh);
            }
            else
            {
                _materialInstance.SetInt(MaterialID.FoveatedStepsLow, MaxSteps);
                _materialInstance.SetInt(MaterialID.FoveatedStepsHigh, MaxSteps);
            }

            if (_regionOutline.Active)
            {
                UnityEngine.Vector3 regionStartObjectSpace = new UnityEngine.Vector3((float)(RegionStartVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionStartVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionStartVoxel.z) / _dataSet.ZDim - 0.5f);
                UnityEngine.Vector3 regionEndObjectSpace = new UnityEngine.Vector3((float)(RegionEndVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionEndVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionEndVoxel.z) / _dataSet.ZDim - 0.5f);
                UnityEngine.Vector3 padding = new UnityEngine.Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
                var highlightMin = UnityEngine.Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
                var highlightMax = UnityEngine.Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);

                _materialInstance.SetVector(MaterialID.HighlightMin, highlightMin);
                _materialInstance.SetVector(MaterialID.HighlightMax, highlightMax);
                _materialInstance.SetFloat(MaterialID.HighlightSaturateFactor, SelectionSaturateFactor);
            }
            else
            {
                _materialInstance.SetFloat(MaterialID.HighlightSaturateFactor, 1f);
            }

            if (_maskDataSet != null)
            {
                _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode());
                _maskMaterialInstance.SetFloat(MaterialID.MaskVoxelSize, MaskVoxelSize);
                _maskMaterialInstance.SetColor(MaterialID.MaskVoxelColor, MaskVoxelColor);
                _maskMaterialInstance.SetInt(MaterialID.HighlightedSource, HighlightedSource);

                // Calculate and update voxel corner offsets
                var offsets = new UnityEngine.Vector4[4];
                var modelMatrix = transform.localToWorldMatrix;
                offsets[0] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new UnityEngine.Vector3(-1.0f / _maskDataSet.XDim, -1.0f / _maskDataSet.YDim, -1.0f / _maskDataSet.ZDim));
                offsets[1] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new UnityEngine.Vector3(-1.0f / _maskDataSet.XDim, -1.0f / _maskDataSet.YDim, +1.0f / _maskDataSet.ZDim));
                offsets[2] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new UnityEngine.Vector3(-1.0f / _maskDataSet.XDim, +1.0f / _maskDataSet.YDim, -1.0f / _maskDataSet.ZDim));
                offsets[3] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new UnityEngine.Vector3(-1.0f / _maskDataSet.XDim, +1.0f / _maskDataSet.YDim, +1.0f / _maskDataSet.ZDim));
                _maskMaterialInstance.SetVectorArray(MaterialID.MaskVoxelOffsets, offsets);
                _maskMaterialInstance.SetMatrix(MaterialID.ModelMatrix, modelMatrix);

            }
            else
            {
                _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.Disabled.GetHashCode());
            }

            if (ProjectionMode == ProjectionMode.AverageIntensityProjection)
            {
                Shader.EnableKeyword("SHADER_AIP");
            }
            else
            {
                Shader.DisableKeyword("SHADER_AIP");
            }

            _materialInstance.SetFloat(MaterialID.VignetteFadeStart, VignetteFadeStart);
            _materialInstance.SetFloat(MaterialID.VignetteFadeEnd, VignetteFadeEnd);
            _materialInstance.SetFloat(MaterialID.VignetteIntensity, VignetteIntensity);
            _materialInstance.SetColor(MaterialID.VignetteColor, VignetteColor);

            if (_restFrequencyChanged && HasWCS)
            {
                _dataSet.RecreateFrameSet(RestFrequency);
                _dataSet.CreateAltSpecFrame();
                _dataSet.HasRestFrequency = true;
                _restFrequencyChanged = false;
            }
        }

        public void ResetRestFrequency()
        {
            if (_dataSet.HasFitsRestFrequency)
            {
                RestFrequency = _dataSet.FitsRestFrequency;

            }
            else
                _dataSet.HasRestFrequency = false;
        }

        void OnRenderObject()
        {
            if (IsFullResolution && DisplayMask && _maskDataSet?.ExistingMaskBuffer != null)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.ExistingMaskBuffer);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.ExistingMaskBuffer.count);
            }
            if (IsFullResolution && DisplayMask && _maskDataSet?.AddedMaskBuffer != null && _maskDataSet?.AddedMaskEntryCount > 0)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.AddedMaskBuffer);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.AddedMaskEntryCount);
            }
        }

        public void InitialiseMask()
        {
            if (_dataSet != null && _maskDataSet == null)
            {
                _maskDataSet = _dataSet.GenerateEmptyMask();
                if (!FactorOverride)
                {
                    _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, out XFactor, out YFactor, out ZFactor);
                }

                _maskDataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.DataCube);
                
                // Re-crop both datasets to ensure that they match correctly
                if (!CropToFeature() && IsFullResolution)
                {
                    CropToRegion(UnityEngine.Vector3.one, new UnityEngine.Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
                }
            }
        }

        private bool PaintMask(Vector3Int position, short value)
        {
            if (_maskDataSet == null || _maskDataSet.RegionCube == null)
            {
                return false;
            }

            var regionSizeObjectSpace = SliceMax - SliceMin;
            var regionSizeDataSpace = new UnityEngine.Vector3(_maskDataSet.XDim * regionSizeObjectSpace.x, _maskDataSet.YDim * regionSizeObjectSpace.y, _maskDataSet.ZDim * regionSizeObjectSpace.z);
            if (Math.Floor(regionSizeDataSpace.x) > _maskDataSet.RegionCube.width || Math.Floor(regionSizeDataSpace.y) > _maskDataSet.RegionCube.height || Math.Floor(regionSizeDataSpace.z) > _maskDataSet.RegionCube.depth)
            {
                return false;
            }

            Vector3Int offsetRegionSpace = Vector3Int.FloorToInt(new UnityEngine.Vector3((0.5f + SliceMin.x) * _maskDataSet.XDim, (0.5f + SliceMin.y) * _maskDataSet.YDim, (0.5f + SliceMin.z) * _maskDataSet.ZDim));
            Vector3Int coordsRegionSpace = position - Vector3Int.one - offsetRegionSpace;
            if (coordsRegionSpace != _previousPaintLocation || value != _previousPaintValue)
            {
                _previousPaintLocation = coordsRegionSpace;
                _previousPaintValue = value;
                _dirtyMask = true;
                return _maskDataSet.PaintMaskVoxel(coordsRegionSpace, value);
            }
            return true;
        }

        /// <summary>
        /// Paint a mask value in the current cursor location
        /// </summary>
        /// <param name="value">The value to paint on the mask</param>
        /// <returns>True if successful, false if not.</returns>
        public bool PaintCursor(short value)
        {
            Vector3Int cursorLoc = new Vector3Int(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
            if (_dataSet.subsetBounds[0] != -1)
            {
                Vector3Int cursorOffset = new Vector3Int(_dataSet.subsetBounds[0] - 1,
                                                         _dataSet.subsetBounds[2] - 1,
                                                         _dataSet.subsetBounds[4] - 1);
                cursorLoc = cursorLoc - cursorOffset;
            }
            var maskCursorLimit = (_previousBrushSize - 1) / 2;
            Debug.Log("Painting at cursor value [" + cursorLoc.x + ", " + cursorLoc.y + ", " + cursorLoc.z + "].");
            for (int i = -maskCursorLimit; i <= maskCursorLimit; i++)
            {
                for (int j = -maskCursorLimit; j <= maskCursorLimit; j++)
                {
                    for (int k = -maskCursorLimit; k <= maskCursorLimit; k++)
                    {
                        PaintMask(new Vector3Int(cursorLoc.x + i, cursorLoc.y + j, cursorLoc.z + k), value);
                    }
                }
            }
            return true;
        }

        public void FinishBrushStroke()
        {
            if (_maskDataSet != null)
            {
                _maskDataSet?.FlushBrushStroke();
            }
            
            _momentMapRenderer.CalculateMomentMaps();
        }

        public Vector3Int GetCubeDimensions()
        {
            return new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
        }

        // Converts volume space to local space
        public UnityEngine.Vector3 VolumePositionToLocalPosition(UnityEngine.Vector3 volumePosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            UnityEngine.Vector3 localPosition = new UnityEngine.Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
            return localPosition;
        }

        // Converts local space to volume space
        public UnityEngine.Vector3 LocalPositionToVolumePosition(UnityEngine.Vector3 localPosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            UnityEngine.Vector3 volumePosition = new UnityEngine.Vector3((localPosition.x + 0.5f) * cubeDimensions.x, (localPosition.y + 0.5f) * cubeDimensions.y, (localPosition.z + 0.5f) * cubeDimensions.z);
            return volumePosition;
        }
        
        public void SaveSubCube()
        {
            IntPtr subCubeHeader = IntPtr.Zero;
            var cornerMin = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMin);
            var cornerMax = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMax);
             _dataSet.SaveSubCubeFromOriginal(cornerMin, cornerMax, _maskDataSet);
        }
        public void SaveMask(bool overwrite)
        {
            if (_maskDataSet == null)
            {
                ToastNotification.ShowError("Could not find mask data!");
                return;
            }
            IntPtr cubeFitsPtr = IntPtr.Zero;
            int status = 0;
            if (string.IsNullOrEmpty(_maskDataSet.FileName))
            {
                // Save new mask because none exists yet
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, _dataSet.FileName, out status);
                string directory = Path.GetDirectoryName(_dataSet.FileName);
                _maskDataSet.FileName = $"{directory}/{Path.GetFileNameWithoutExtension(_dataSet.FileName)}-mask.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, $"!{_maskDataSet.FileName}") != 0)
                {
                    ToastNotification.ShowError("Error saving new mask!");
                }
                
                this.lastSavedMaskPath = _maskDataSet.FileName;
            }
            else if (!overwrite)
            {
                // Save a copy
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, _maskDataSet.FileName, out status);
                Regex regex = new Regex(@"_copy_\d{8}_\d{5}");
                string fileName = Path.GetFileNameWithoutExtension(_maskDataSet.FileName);
                Match match = regex.Match(fileName);
                var timeStamp = DateTime.Now.ToString("yyyyMMdd_Hmmss");
                if (match.Success)
                {
                    fileName = fileName.Substring(0, fileName.Length - timeStamp.Length - 6) + "_copy_" + timeStamp;
                }
                else
                {
                    fileName = fileName + "_copy_" + timeStamp;
                }
                string directory = Path.GetDirectoryName(_maskDataSet.FileName);
                string fullPath = $"!{directory}/{fileName}.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, fullPath) != 0)
                {
                    ToastNotification.ShowError("Error saving mask copy!");
                }
                else
                {
                    
                    this.lastSavedMaskPath = fullPath;
                    ToastNotification.ShowSuccess($"Mask saved to {fileName}");
                }
            }
            else
            {
                // Overwrite existing mask
                FitsReader.FitsOpenFileReadWrite(out cubeFitsPtr, _maskDataSet.FileName, out status);
                if (_maskDataSet.SaveMask(cubeFitsPtr, null) != 0)
                {
                    ToastNotification.ShowError("Error overwriting existing mask!");
                }
                else
                {
                    this.lastSavedMaskPath = _maskDataSet.FileName;
                    ToastNotification.ShowSuccess($"Mask saved to disk");
                }
                UnityEngine.Debug.Log("Overwriting mask complete, VolumeDataSetRenderer::SaveMask().");
            }
            if (cubeFitsPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.Log("cubeFitsPtr != IntPtr.Zero, closing file.");
                FitsReader.FitsCloseFile(cubeFitsPtr, out status);
            }
        }

        public string GetMaskSavedFilePath()
        {
            var result = this.lastSavedMaskPath;
            return result;
        }

        public void OnDestroy()
        {
            _dataSet.CleanUp(RandomVolume);
            _maskDataSet?.CleanUp(false);
            _measuringLine?.Destroy();
            _cubeOutline?.Destroy();
            _regionOutline?.Destroy();
            _voxelOutline?.Destroy();
        }
    }

}