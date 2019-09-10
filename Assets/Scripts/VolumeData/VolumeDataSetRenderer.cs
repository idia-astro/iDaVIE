using System;
using System.Collections.Generic;
using UnityEngine;
using DataFeatures;
using Vectrosity;


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
        public MaskMode MaskMode = MaskMode.Disabled;
        public ProjectionMode ProjectionMode = ProjectionMode.MaximumIntensityProjection;
        public TextureFilterEnum TextureFilter = TextureFilterEnum.Point;

        // Jitter factor
        [Range(0, 1)] public float Jitter = 1.0f;

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

        [Header("Thresholds")]        
        [Range(0, 1)] public float ThresholdMin = 0;
        [Range(0, 1)] public float ThresholdMax = 1;

        [Header("Color Mapping")]
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public ScalingType ScalingType = ScalingType.Linear;
        [Range(-1, 1)] public float ScalingBias = 0.0f;
        [Range(0, 5)] public float ScalingContrast = 1.0f;
        public float ScalingAlpha = 1000.0f;
        [Range(0, 5)] public float ScalingGamma = 1.0f;

        [Header("File Input")]
        public string FileName;
        public string MaskFileName;        
        public Material RayMarchingMaterial;

        [Header("Debug Settings")]
        public bool FactorOverride = false;
        public int XFactor = 1;
        public int YFactor = 1;
        public int ZFactor = 1;
        public float ScaleMax;
        public float ScaleMin;
        public Vector3 SliceMin = Vector3.zero;
        public Vector3 SliceMax = Vector3.one;

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

        private VectorLine _voxelOutline, _cubeOutline, _regionOutline;
        

        private FeatureSetManager _featureManager = null;
        private MeshRenderer _renderer;
        private Material _materialInstance;
        private VolumeDataSet _dataSet;
        private VolumeDataSet _maskDataSet = null;
        private VolumeInputController _volumeInputController;

        public bool RandomVolume = false;
        
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
        }
        #endregion

        public void Start()
        {
            if (RandomVolume)
                _dataSet = VolumeDataSet.LoadRandomFitsCube(0, 512, 512, 512, 512);
            else
                _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName, false);

            _volumeInputController = FindObjectOfType<VolumeInputController>();
            _featureManager = GetComponentInChildren<FeatureSetManager>();
            if (_featureManager == null)
                Debug.Log($"No FeatureManager attached to VolumeDataSetRenderer. Attach prefab for use of Features.");

            if (!FactorOverride)
            {
                _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, out XFactor, out YFactor, out ZFactor);
            }

            _dataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
            DataAnalysis.FindMaxMin(_dataSet.FitsData, _dataSet.NumPoints, out ScaleMax, out ScaleMin);
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

            // Bounding box outline and axes
            Vector3 axesIndicatorOrigin = -0.5f * Vector3.one;
            Vector3 axesIndicatorOpposite = 0.5f * Vector3.one;
            var cubeOutlinePoints = new List<Vector3>
            {
                // axis indicators
                axesIndicatorOrigin, axesIndicatorOrigin + Vector3.right,
                axesIndicatorOrigin, axesIndicatorOrigin + Vector3.up,
                axesIndicatorOrigin, axesIndicatorOrigin + Vector3.forward,
                // opposite axes
                axesIndicatorOpposite, axesIndicatorOpposite - Vector3.right,
                axesIndicatorOpposite, axesIndicatorOpposite - Vector3.up,
                axesIndicatorOpposite, axesIndicatorOpposite - Vector3.forward,
                // remaining vertices
                axesIndicatorOrigin + Vector3.right, axesIndicatorOrigin + Vector3.right + Vector3.up,
                axesIndicatorOrigin + Vector3.right, axesIndicatorOrigin + Vector3.right + Vector3.forward,
                axesIndicatorOrigin + Vector3.up, axesIndicatorOrigin + Vector3.up + Vector3.right,
                axesIndicatorOrigin + Vector3.up, axesIndicatorOrigin + Vector3.up + Vector3.forward,
                axesIndicatorOrigin + Vector3.forward, axesIndicatorOrigin + Vector3.forward + Vector3.right,
                axesIndicatorOrigin + Vector3.forward, axesIndicatorOrigin + Vector3.forward + Vector3.up
            };

            var defaultColor = Color.white;
            var axesLineColors = new List<Color32>
            {
                new Color(1.0f, 0.4f, 0.4f),
                new Color(0.4f, 1.0f, 0.4f),
                new Color(0.4f, 0.4f, 1.0f),
                defaultColor, defaultColor, defaultColor,
                defaultColor, defaultColor,
                defaultColor, defaultColor,
                defaultColor, defaultColor
            };

            _cubeOutline = new VectorLine("CubeAxes", cubeOutlinePoints, 2.0f, LineType.Discrete);
            _cubeOutline.SetColors(axesLineColors);
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

            if (_featureManager)
            {
                _featureManager.CreateNewFeatureSet();
            }
            
            Shader.WarmupAllShaders();

        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            ColorMap = ColorMapUtils.FromHashCode(newIndex);
        }

        public void SetCursorPosition(Vector3 cursor)
        {
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor);
            Bounds objectBounds = new Bounds(Vector3.zero, Vector3.one);
            if (objectBounds.Contains(objectSpacePosition) && _dataSet != null)
            {
                Vector3 positionCubeSpace = new Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim, (objectSpacePosition.y + 0.5f) * _dataSet.YDim, (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
                Vector3 voxelCornerCubeSpace = new Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
                Vector3 voxelCenterCubeSpace = voxelCornerCubeSpace + 0.5f * Vector3.one;
                Vector3Int newVoxelCursor = new Vector3Int(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1);

                if (!newVoxelCursor.Equals(CursorVoxel))
                {
                    CursorVoxel = newVoxelCursor;
                    CursorValue = _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    if (_maskDataSet != null)
                        CursorSource = _maskDataSet.GetMaskValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    else
                        CursorSource = 0;
                    Vector3 voxelCenterObjectSpace = new Vector3(voxelCenterCubeSpace.x / _dataSet.XDim - 0.5f, voxelCenterCubeSpace.y / _dataSet.YDim - 0.5f, voxelCenterCubeSpace.z / _dataSet.ZDim - 0.5f);
                    _voxelOutline.MakeCube(voxelCenterObjectSpace, 1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
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

        public void SetRegionPosition(Vector3 cursor, bool start)
        {
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor);
            objectSpacePosition = new Vector3(
                Mathf.Clamp(objectSpacePosition.x, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.y, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.z, -0.5f, 0.5f)
            );
            if (_dataSet != null)
            {
                Vector3 positionCubeSpace = new Vector3((objectSpacePosition.x + 0.5f) * _dataSet.XDim, (objectSpacePosition.y + 0.5f) * _dataSet.YDim, (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
                Vector3 voxelCornerCubeSpace = new Vector3(Mathf.Floor(positionCubeSpace.x), Mathf.Floor(positionCubeSpace.y), Mathf.Floor(positionCubeSpace.z));
                Vector3Int newVoxelCursor = new Vector3Int(
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int) _dataSet.XDim),
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int) _dataSet.YDim),
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int) _dataSet.ZDim)
                );

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

                    // Calculate full region bounds
                    var regionMin = Vector3.Min(RegionStartVoxel, RegionEndVoxel);
                    var regionMax = Vector3.Max(RegionStartVoxel, RegionEndVoxel);
                    var regionSize = regionMax - regionMin + Vector3.one;
                    Vector3 regionCenter = (regionMax + regionMin) / 2.0f - 0.5f * Vector3.one;

                    Vector3 regionCenterObjectSpace = new Vector3(regionCenter.x / _dataSet.XDim - 0.5f, regionCenter.y / _dataSet.YDim - 0.5f, regionCenter.z / _dataSet.ZDim - 0.5f);
                    _regionOutline.MakeCube(regionCenterObjectSpace, regionSize.x / _dataSet.XDim, regionSize.y / _dataSet.YDim, regionSize.z / _dataSet.ZDim);
                }

                _regionOutline.active = true;
            }
        }

        public void ClearRegion()
        {
            if (_regionOutline != null)
            {
                _regionOutline.active = false;
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
                }
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
            }
        }

        public void LoadRegionData(Vector3Int startVoxel, Vector3Int endVoxel)
        {
            Vector3Int deltaRegion = startVoxel - endVoxel;
            Vector3Int regionSize = new Vector3Int(Math.Abs(deltaRegion.x) + 1, Math.Abs(deltaRegion.y) + 1, Math.Abs(deltaRegion.z) + 1);
            int xFactor, yFactor, zFactor;
            _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, regionSize.x, regionSize.y, regionSize.z, out xFactor, out yFactor, out zFactor);
            _dataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel, new Vector3Int(xFactor, yFactor, zFactor));
            if (_maskDataSet != null)
            {
                _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel, new Vector3Int(xFactor, yFactor, zFactor));
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
        }

        public Vector3 GetFitsCoords(double X, double Y, double Z)
        {
            Vector2 raDec = _dataSet.GetRADecFromXY(X, Y);
            float z = (float)_dataSet.GetVelocityFromZ(Z);
            return new Vector3(raDec.x, raDec.y, z);
        }

        public Vector3 GetFitsLengths(double X, double Y, double Z)
        {
            Vector3 wcsConversion = _dataSet.GetWCSDeltas();
            float xLength = Math.Abs((float)X * wcsConversion.x);
            float yLength = Math.Abs((float)Y * wcsConversion.y);
            float zLength = Math.Abs((float)Z * wcsConversion.z);
            return new Vector3(xLength, yLength, zLength);
        }

        public string GetFitsCoordsString(double X, double Y, double Z)
        {
            Vector2 raDec = _dataSet.GetRADecFromXY(X, Y);
            string vel = string.Format("{0,8:F2} km/s", (float)_dataSet.GetVelocityFromZ(Z)/1000);
            double raHours = (raDec.x * 12.0 / 180.0);
            double raMin = (raHours - Math.Truncate(raHours)) * 60;
            double raSec = Math.Truncate((raMin - Math.Truncate(raMin)) * 60 * 100)/100;

            double decMin = Math.Abs((raDec.y - Math.Truncate(raDec.y)) * 60);
            double decSec = Math.Truncate((decMin - Math.Truncate(decMin)) * 60 * 100) / 100;

            string ra = Math.Truncate(raHours).ToString("00").PadLeft(3) + ":" + Math.Truncate(raMin).ToString("00") + ":" + raSec.ToString("00.00");
            string dec = Math.Truncate(raDec.y).ToString("00").PadLeft(3) + ":" + Math.Truncate(decMin).ToString("00") + ":" + decSec.ToString("00.00");
            return ra + " " + dec + " " + vel;
        }

        public Vector3Int GetDimDecimals()
        {
            return new Vector3Int(_dataSet.XDimDecimal, _dataSet.YDimDecimal, _dataSet.ZDimDecimal);
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

        public void OnDestroy()
        {
            _dataSet.CleanUp();
            if (_maskDataSet != null)
                _maskDataSet.CleanUp();
        }
    }
}