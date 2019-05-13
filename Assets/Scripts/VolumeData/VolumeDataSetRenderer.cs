using System;
using System.Collections.Generic;
using UnityEngine;
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
        Inverted = 2
    }


    public class VolumeDataSetRenderer : MonoBehaviour
    {
        public ColorMapDelegate OnColorMapChanged;

        [Header("Rendering Settings")]
        // Step control
        [Range(16, 512)] public int MaxSteps = 192;
        public long MaximumCubeSizeInMB = 250;
        public MaskMode MaskMode = MaskMode.Disabled;
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
        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel { get; private set; }

        [Range(0, 1)] public float SelectionSaturateFactor = 0.7f;

        private VectorLine _voxelOutline, _cubeOutline, _regionOutline;

        private MeshRenderer _renderer;
        private Material _materialInstance;
        private VolumeDataSet _dataSet;
        private VolumeDataSet _maskDataSet = null;
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
            _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName, false);
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
            _regionOutline.color = Color.red;
            _regionOutline.active = false;
            _regionOutline.Draw3DAuto();
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
                    CursorValue = _dataSet.GetValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
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
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int)_dataSet.XDim),
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int)_dataSet.YDim),
                    Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int)_dataSet.ZDim)
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

        public void CropToRegion()
        {
            Vector3 regionStartObjectSpace = new Vector3((float)(RegionStartVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionStartVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionStartVoxel.z) / _dataSet.ZDim - 0.5f);
            Vector3 regionEndObjectSpace = new Vector3((float)(RegionEndVoxel.x) / _dataSet.XDim - 0.5f, (float)(RegionEndVoxel.y) / _dataSet.YDim - 0.5f, (float)(RegionEndVoxel.z) / _dataSet.ZDim - 0.5f);
            Vector3 padding = new Vector3(1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
            SliceMin = Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
            SliceMax = Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);
            LoadRegionData();
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.RegionCube);
            if (_maskDataSet != null)
            {
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
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

        public void LoadRegionData()
        {
            Vector3Int deltaRegion = RegionStartVoxel - RegionEndVoxel;
            Vector3Int regionSize = new Vector3Int(Math.Abs(deltaRegion.x) + 1, Math.Abs(deltaRegion.y) + 1, Math.Abs(deltaRegion.z) + 1);
            int xFactor, yFactor, zFactor;
            _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, regionSize.x, regionSize.y, regionSize.z, out xFactor, out yFactor, out zFactor);
            _dataSet.GenerateCroppedVolumeTexture(TextureFilter, RegionStartVoxel, RegionEndVoxel, new Vector3Int(xFactor, yFactor, zFactor));
            if (_maskDataSet != null)
            {
                _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, RegionStartVoxel, RegionEndVoxel, new Vector3Int(xFactor, yFactor, zFactor));
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

            _materialInstance.SetFloat(MaterialID.VignetteFadeStart, VignetteFadeStart);
            _materialInstance.SetFloat(MaterialID.VignetteFadeEnd, VignetteFadeEnd);
            _materialInstance.SetFloat(MaterialID.VignetteIntensity, VignetteIntensity);
            _materialInstance.SetColor(MaterialID.VignetteColor, VignetteColor);
        }

        public void OnDestroy()
        {
            _dataSet.CleanUp();
            if (_maskDataSet != null)
                _maskDataSet.CleanUp();
        }
    }
}