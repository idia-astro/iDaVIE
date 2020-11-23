using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DataFeatures;
using JetBrains.Annotations;
using UnityEngine.XR;
using Vectrosity;
using Random = System.Random;
using System.Text;

namespace VolumeData
{
    public enum TextureFilterEnum
    {
        Point,
        Bilinear,
        Trilinear
    }

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
        public TextureFilterEnum TextureFilter = TextureFilterEnum.Point;
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
        public Vector3 SliceMin = Vector3.zero;
        public Vector3 SliceMax = Vector3.one;
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
        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }
        public Vector3 InitialScale { get; private set; }

        public float InitialThresholdMin { get; private set; }
        public float InitialThresholdMax { get; private set; }

        public Vector3Int CursorVoxel { get; private set; }
        public float CursorValue { get; private set; }
        public Int16 CursorSource { get; private set; }
        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel { get; private set; }

        [Range(0, 1)] public float SelectionSaturateFactor = 0.7f;

        public FeatureSetManager FeatureSetManagerPrefab;

        public VectorLine _voxelOutline, _cubeOutline, _regionOutline, _regionMeasure;

        private FeatureSetManager _featureManager = null;
        private MeshRenderer _renderer;
        private Material _materialInstance;
        private Material _maskMaterialInstance;

        public VolumeInputController _volumeInputController;

        private Vector3Int _previousPaintLocation;
        private short _previousPaintValue;
        private int _previousBrushSize = 1;

        private VolumeDataSet _dataSet = null;
        private VolumeDataSet _maskDataSet = null;
        private bool _dirtyMask = false;
        public bool HasWCS { get; private set; }


        private int _currentXFactor, _currentYFactor, _currentZFactor;
        public bool IsFullResolution => _currentXFactor * _currentYFactor * _currentZFactor == 1;

        public bool IsCropped { get; private set; }

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
            public static readonly int VignetteColor = Shader.PropertyToID("VignetteIntensity");

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
            public static readonly int CursorSource = Shader.PropertyToID("CursorSource");
        }

        #endregion

        [Header("Miscellaneous")]
        public bool started = false;

        public bool FileChanged = true;
		
        public float ZScale
        {
            get
            {
                return gameObject.transform.localScale.z;
            }
            set
            {
                Vector3 oldScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(oldScale.x, oldScale.y, value);
            }
        }

        
        public void Start()
        {
            if (RandomVolume)
                _dataSet = VolumeDataSet.LoadRandomFitsCube(0, RandomCubeSize, RandomCubeSize, RandomCubeSize, RandomCubeSize);
            else
                _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName, false, CubeDepthAxis, CubeSlice);
            _volumeInputController = FindObjectOfType<VolumeInputController>();
            _featureManager = GetComponentInChildren<FeatureSetManager>();
            if (_featureManager == null)
                Debug.Log($"No FeatureManager attached to VolumeDataSetRenderer. Attach prefab for use of Features.");
            if (!FactorOverride)
            {
                _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, out XFactor, out YFactor, out ZFactor);
            }
            _dataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
            _currentXFactor = XFactor;
            _currentYFactor = YFactor;
            _currentZFactor = ZFactor;
            ScaleMax = _dataSet.MaxValue;
            ScaleMin = _dataSet.MinValue;
            if (!String.IsNullOrEmpty(MaskFileName))
            {
                _maskDataSet = VolumeDataSet.LoadDataFromFitsFile(MaskFileName, true);
                _maskDataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
            }
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

            _cubeOutline = new VectorLine("CubeAxes", new List<Vector3>(24), 2.0f);
            _cubeOutline.MakeCube(Vector3.zero, 1, 1, 1);
            SetCubeColors(_cubeOutline, Color.white, true);
            _cubeOutline.drawTransform = transform;
            _cubeOutline.Draw3DAuto();

            // Voxel axes
            CursorVoxel = new Vector3Int(-1, -1, -1);
            _voxelOutline = new VectorLine("VoxelOutline", new List<Vector3>(24), 1.0f);
            _voxelOutline.MakeCube(Vector3.zero, 1, 1, 1);
            _voxelOutline.drawTransform = transform;
            _voxelOutline.color = Color.green;
            _voxelOutline.active = false;
            _voxelOutline.Draw3DAuto();

            // Voxel axes
            _regionOutline = new VectorLine("VoxelOutline", new List<Vector3>(24), 1.0f);
            _regionOutline.MakeCube(Vector3.zero, 1, 1, 1);
            _regionOutline.drawTransform = transform;
            _regionOutline.color = Color.green;
            _regionOutline.active = false;
            _regionOutline.Draw3DAuto();

            //Region measuring line
            _regionMeasure = new VectorLine("RegionMeasure", new List<Vector3>(), 1.0f);
            _regionMeasure.drawTransform = transform;
            _regionMeasure.color = Color.white;
            _regionMeasure.active = false;
            _regionMeasure.Draw3DAuto();

            if (_featureManager)
            {
                _featureManager.CreateNewFeatureSet();
            }

            //No wcs info if AstFrameSet has only 1 frame
            if (_dataSet.AstFrameSet != IntPtr.Zero)
                if (_dataSet.HasAstAttribute("Nframe"))
                    HasWCS = int.Parse(_dataSet.GetAstAttribute("Nframe")) > 1;
                else
                    HasWCS = false;
            else
                HasWCS = false;

            if (_dataSet.HasFitsRestFrequency)
            {
                RestFrequency = _dataSet.FitsRestFrequency;
            }
            Shader.WarmupAllShaders();

            started = true;

        }

        public VolumeDataSet GetDataSet()
        {
            return _dataSet;
        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            ColorMap = ColorMapUtils.FromHashCode(newIndex);
        }

        public void SetCursorPosition(Vector3 cursor, int brushSize)
        {
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor);
            Bounds objectBounds = new Bounds(Vector3.zero, Vector3.one);
            if (objectBounds.Contains(objectSpacePosition) && _dataSet != null)
            {
                Vector3 positionCubeSpace = new Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim, (objectSpacePosition.y + 0.5f) * _dataSet.YDim, (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
                Vector3 voxelCornerCubeSpace = new Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
                Vector3 voxelCenterCubeSpace = voxelCornerCubeSpace + 0.5f * Vector3.one;
                Vector3Int newVoxelCursor = new Vector3Int(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1);

                if (!newVoxelCursor.Equals(CursorVoxel) || brushSize != _previousBrushSize)
                {
                    _previousBrushSize = brushSize;
                    CursorVoxel = newVoxelCursor;
                    CursorValue = _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    if (_maskDataSet != null)
                    {
                        CursorSource = _maskDataSet.GetMaskValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    }
                    else
                    {
                        CursorSource = 0;
                    }

                    Vector3 voxelCenterObjectSpace = new Vector3(voxelCenterCubeSpace.x / _dataSet.XDim - 0.5f, voxelCenterCubeSpace.y / _dataSet.YDim - 0.5f,
                        voxelCenterCubeSpace.z / _dataSet.ZDim - 0.5f);
                    _voxelOutline.MakeCube(voxelCenterObjectSpace, (float)brushSize / _dataSet.XDim, (float)brushSize / _dataSet.YDim, (float)brushSize / _dataSet.ZDim);
                }

                _voxelOutline.active = true;
            }
            else
            {
                if (_voxelOutline != null && _voxelOutline.active)
                {
                    _voxelOutline.active = false;
                }

                CursorValue = float.NaN;
                CursorVoxel = new Vector3Int(-1, -1, -1);
            }
        }

        public Vector3Int GetVoxelPosition(Vector3 cursorPosWorldSpace)
        {
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursorPosWorldSpace);
            objectSpacePosition = new Vector3(
                Mathf.Clamp(objectSpacePosition.x, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.y, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.z, -0.5f, 0.5f)
            );

            if (_dataSet == null)
            {
                return Vector3Int.zero;
            }
            
            Vector3 positionCubeSpace = new Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim, (objectSpacePosition.y + 0.5f) * _dataSet.YDim, (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
            Vector3 voxelCornerCubeSpace = new Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
            Vector3Int newVoxelCursor = new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int)_dataSet.XDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int)_dataSet.YDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int)_dataSet.ZDim)
            );

            return newVoxelCursor;
        }

        public void SetRegionPosition(Vector3 cursor, bool start)
        {
            if (_dataSet != null)
            {
                var newVoxelCursor = GetVoxelPosition(cursor);
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

                _regionOutline.active = true;
                if (ShowMeasuringLine == true)
                    _regionMeasure.active = true;
            }
        }

        public void SetRegionBounds(Vector3Int min, Vector3Int max)
        {
            RegionStartVoxel = min;
            RegionEndVoxel = max;
            UpdateRegionBounds();
        }

        private void UpdateRegionBounds()
        {
            // Calculate full region bounds
            var regionMin = Vector3.Min(RegionStartVoxel, RegionEndVoxel);
            var regionMax = Vector3.Max(RegionStartVoxel, RegionEndVoxel);
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
            var regionSize = regionMax - regionMin + Vector3.one;
            Vector3 regionCenter = (regionMax + regionMin) / 2.0f - 0.5f * Vector3.one;

            Vector3 regionCenterObjectSpace = new Vector3(regionCenter.x / _dataSet.XDim - 0.5f, regionCenter.y / _dataSet.YDim - 0.5f, regionCenter.z / _dataSet.ZDim - 0.5f);
            _regionOutline.MakeCube(regionCenterObjectSpace, regionSize.x / _dataSet.XDim, regionSize.y / _dataSet.YDim, regionSize.z / _dataSet.ZDim);
            _regionMeasure.points3.Clear();
            _regionMeasure.points3.Add(new Vector3((float)measureStart.x/_dataSet.XDim- 0.5f, (float)measureStart.y/_dataSet.YDim- 0.5f, (float)measureStart.z/_dataSet.ZDim- 0.5f));
            _regionMeasure.points3.Add(new Vector3((float)measureEnd.x/_dataSet.XDim- 0.5f, (float)measureEnd.y/_dataSet.YDim- 0.5f, (float)measureEnd.z/_dataSet.ZDim- 0.5f));

            var regionSizeBytes = regionSize.x * regionSize.y * regionSize.z * sizeof(float);
            bool regionIsFullResolution = (regionSizeBytes <= MaximumCubeSizeInMB * 1e6);
            SetCubeColors(_regionOutline, regionIsFullResolution ? Color.white : Color.yellow, regionIsFullResolution);
        }

        public void ClearRegion()
        {
            if (_regionOutline != null)
            {
                _regionOutline.active = false;
            }
        }

        public void ClearMeasure()
        {
            if (_regionMeasure != null)
            {
                _regionMeasure.active = false;
            }
        }

        public void SelectFeature(Vector3 cursor)
        {
            if (_featureManager && _featureManager.SelectFeature(cursor))
            {
                Debug.Log($"Selected feature '{_featureManager.SelectedFeature.Name}'");
            }
        }

        public void CropToRegion()
        {
            if (_featureManager != null && _featureManager.SelectedFeature != null)
            {
                var cornerMin = _featureManager.SelectedFeature.CornerMin;
                var cornerMax = _featureManager.SelectedFeature.CornerMax;
                Vector3Int startVoxel = new Vector3Int(Convert.ToInt32(cornerMin.x), Convert.ToInt32(cornerMin.y), Convert.ToInt32(cornerMin.z));
                Vector3Int endVoxel = new Vector3Int(Convert.ToInt32(cornerMax.x), Convert.ToInt32(cornerMax.y), Convert.ToInt32(cornerMax.z));

                Vector3 regionStartObjectSpace = new Vector3(cornerMin.x / _dataSet.XDim - 0.5f, cornerMin.y / _dataSet.YDim - 0.5f, cornerMin.z / _dataSet.ZDim - 0.5f);
                Vector3 regionEndObjectSpace = new Vector3(cornerMax.x / _dataSet.XDim - 0.5f, cornerMax.y / _dataSet.YDim - 0.5f, cornerMax.z / _dataSet.ZDim - 0.5f);
                Vector3 padding = new Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
                SliceMin = Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
                SliceMax = Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);
                LoadRegionData(startVoxel, endVoxel);
                _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.RegionCube);
                if (_maskDataSet != null)
                {
                    _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
                    var regionMin = Vector3.Min(RegionStartVoxel, RegionEndVoxel);
                    _maskMaterialInstance.SetVector(MaterialID.RegionOffset, new Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
                    var regionDimensions = new Vector4(_maskDataSet.RegionCube.width, _maskDataSet.RegionCube.height, _maskDataSet.RegionCube.depth, 0);
                    _maskMaterialInstance.SetVector(MaterialID.RegionDimensions, regionDimensions);
                    var cubeDimensions = new Vector4(_maskDataSet.XDim, _maskDataSet.YDim, _maskDataSet.ZDim, 1);
                    _maskMaterialInstance.SetVector(MaterialID.CubeDimensions, cubeDimensions);
                }
                IsCropped = true;
            }
        }

        public void ResetCrop()
        {
            SliceMin = -0.5f * Vector3.one;
            SliceMax = +0.5f * Vector3.one;
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.DataCube);
            if (_maskDataSet != null)
            {
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.DataCube);
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, null);
            }

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
        }

        public void TeleportToRegion()
        {
            if (_volumeInputController && _featureManager && _featureManager.SelectedFeature != null)
            {
                var boundsMin = _featureManager.SelectedFeature.CornerMin;
                var boundsMax = _featureManager.SelectedFeature.CornerMax;
                _volumeInputController.Teleport(boundsMin - (0.5f * Vector3.one), boundsMax + (0.5f * Vector3.one));
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

            if (_regionOutline.active)
            {
                Vector3 regionStartObjectSpace = new Vector3((float)(RegionStartVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionStartVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionStartVoxel.z) / _dataSet.ZDim - 0.5f);
                Vector3 regionEndObjectSpace = new Vector3((float)(RegionEndVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionEndVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionEndVoxel.z) / _dataSet.ZDim - 0.5f);
                Vector3 padding = new Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
                var highlightMin = Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
                var highlightMax = Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);

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
                _maskMaterialInstance.SetInt(MaterialID.CursorSource, CursorSource);

                // Calculate and update voxel corner offsets
                var offsets = new Vector4[4];
                var modelMatrix = transform.localToWorldMatrix;
                offsets[0] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new Vector3(-1.0f / _maskDataSet.XDim, -1.0f / _maskDataSet.YDim, -1.0f / _maskDataSet.ZDim));
                offsets[1] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new Vector3(-1.0f / _maskDataSet.XDim, -1.0f / _maskDataSet.YDim, +1.0f / _maskDataSet.ZDim));
                offsets[2] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new Vector3(-1.0f / _maskDataSet.XDim, +1.0f / _maskDataSet.YDim, -1.0f / _maskDataSet.ZDim));
                offsets[3] = modelMatrix.MultiplyVector(0.5f * MaskVoxelSize * new Vector3(-1.0f / _maskDataSet.XDim, +1.0f / _maskDataSet.YDim, +1.0f / _maskDataSet.ZDim));
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
            if (DisplayMask && _maskDataSet?.ExistingMaskBuffer != null)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.ExistingMaskBuffer);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.ExistingMaskBuffer.count);
            }
            if (DisplayMask && _maskDataSet?.AddedMaskBuffer != null && _maskDataSet?.AddedMaskEntryCount > 0)
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
                _maskDataSet = VolumeDataSet.GenerateEmptyMask(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim);
                // Re-crop both datasets to ensure that they match correctly
                CropToRegion();
            }
        }

        private bool PaintMask(Vector3Int position, short value)
        {
            if (_maskDataSet == null || _maskDataSet.RegionCube == null)
            {
                return false;
            }

            var regionSizeObjectSpace = SliceMax - SliceMin;
            var regionSizeDataSpace = new Vector3(_maskDataSet.XDim * regionSizeObjectSpace.x, _maskDataSet.YDim * regionSizeObjectSpace.y, _maskDataSet.ZDim * regionSizeObjectSpace.z);
            if (Math.Floor(regionSizeDataSpace.x) > _maskDataSet.RegionCube.width || Math.Floor(regionSizeDataSpace.y) > _maskDataSet.RegionCube.height || Math.Floor(regionSizeDataSpace.z) > _maskDataSet.RegionCube.depth)
            {
                return false;
            }

            Vector3Int offsetRegionSpace = Vector3Int.FloorToInt(new Vector3((0.5f + SliceMin.x) * _maskDataSet.XDim, (0.5f + SliceMin.y) * _maskDataSet.YDim, (0.5f + SliceMin.z) * _maskDataSet.ZDim));
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

        public bool PaintCursor(short value)
        {
            var maskCursorLimit = (_previousBrushSize - 1) / 2;
            for (int i = -maskCursorLimit; i <= maskCursorLimit; i++)
            {
                for (int j = -maskCursorLimit; j <= maskCursorLimit; j++)
                {
                    for (int k = -maskCursorLimit; k <= maskCursorLimit; k++)
                    {
                        PaintMask(new Vector3Int(CursorVoxel.x + i, CursorVoxel.y + j, CursorVoxel.z + k), value);
                    }
                }
            }
            return true;
        }

        public void FinishBrushStroke()
        {
            _maskDataSet?.FlushBrushStroke();
        }

        public Vector3Int GetCubeDimensions()
        {
            return new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
        }

        // Converts volume space to local space
        public Vector3 VolumePositionToLocalPosition(Vector3 volumePosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            Vector3 localPosition = new Vector3(volumePosition.x / cubeDimensions.x - 0.5f, volumePosition.y / cubeDimensions.y - 0.5f, volumePosition.z / cubeDimensions.z - 0.5f);
            return localPosition;
        }

        // Converts local space to volume space
        public Vector3 LocalPositionToVolumePosition(Vector3 localPosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            Vector3 volumePosition = new Vector3((localPosition.x + 0.5f) * cubeDimensions.x, (localPosition.y + 0.5f) * cubeDimensions.y, (localPosition.z + 0.5f) * cubeDimensions.z);
            return volumePosition;
        }

        public void CommitMask()
        {
            // Update cropped region and recalculate downsampled cube if it has been updated
            if (_dirtyMask)
            {
                _maskDataSet?.CommitMask();
                _maskDataSet?.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
                _dirtyMask = false;
            }
        }
        
        public void SaveMask(bool overwrite)
        {
            if (_maskDataSet == null)
            {
                Debug.LogError("Could not find mask data!");
                return;
            }

            _maskDataSet.CommitMask();

            IntPtr cubeFitsPtr;
            int status = 0;
            
            if (string.IsNullOrEmpty(_maskDataSet.FileName))
            {
                // Save new mask
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, _dataSet.FileName, out status);
                string directory = Path.GetDirectoryName(_dataSet.FileName);
                string filename = $"!{directory}/{Path.GetFileNameWithoutExtension(_dataSet.FileName)}-mask.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, filename) != 0)
                {
                    Debug.LogError("Error saving new mask!");
                    FitsReader.FitsCloseFile(cubeFitsPtr, out status);
                }

            }
            else if (!overwrite)
            {
                // Save a copy (overwrites existing copy)
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, _maskDataSet.FileName, out status);
                string directory = Path.GetDirectoryName(_maskDataSet.FileName);
                string filename = $"!{directory}/{Path.GetFileNameWithoutExtension(_maskDataSet.FileName)}-copy.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, filename) != 0)
                {
                    Debug.LogError("Error saving copy!");
                    FitsReader.FitsCloseFile(cubeFitsPtr, out status);
                }
            }
            else
            {
                // Overwrite existing mask
                FitsReader.FitsOpenFileReadWrite(out cubeFitsPtr, _maskDataSet.FileName, out status);
                if (_maskDataSet.SaveMask(cubeFitsPtr, null) != 0)
                {
                    Debug.LogError("Error overwriting existing mask!");
                    FitsReader.FitsCloseFile(cubeFitsPtr, out status);
                }
            }
            FitsReader.FitsCloseFile(cubeFitsPtr, out status);
        }

        public void SetScalingType(ScalingType scalingType)
        {
            ScalingType = scalingType;
        }

        public void OnDestroy()
        {
            _dataSet.CleanUp(RandomVolume);
            _maskDataSet?.CleanUp(false);

        }

        private void SetCubeColors(VectorLine cube, Color32 baseColor, bool colorAxes)
        {
            cube.SetColor(baseColor);

            if (colorAxes)
            {
                var colorAxisX = new Color(1.0f, 0.3f, 0.3f);
                var colorAxisY = new Color(0.3f, 1.0f, 0.3f);
                var colorAxisZ = new Color(0.3f, 0.3f, 1.0f);
                cube.SetColor(colorAxisX, 8);
                cube.SetColor(colorAxisY, 4);
                cube.SetColor(colorAxisZ, 11);
            }
        }
    }
}