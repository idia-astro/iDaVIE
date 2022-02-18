using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace VolumeData
{
    public class MomentMapRenderer : MonoBehaviour
    {
        public RenderTexture Moment0Map { get; private set; }
        public RenderTexture Moment1Map { get; private set; }
        public RenderTexture ImageOutput { get; private set; }

        public bool Inverted = false;
        
        //mom map threshold step
        public float momstep = 0.00025f;


        public MomentMapMenuController momentMapMenuController;
        public Texture3D DataCube
        {
            get => _dataCube;
            set
            {
                if (value != _dataCube)
                {
                    _dataCube = value;
                    CalculateMomentMaps();
                }
            }
        }

        public Texture3D MaskCube
        {
            get => _maskCube;
            set
            {
                if (value != _maskCube)
                {
                    _maskCube = value;
                    if (UseMask)
                    {
                        CalculateMomentMaps();
                    }
                }
            }
        }

        public bool UseZScale
        {
            get => _useZScale;
            set
            {
                if (_useZScale != value)
                {
                    _useZScale = value;
                    _moment0Bounds = GetBounds(Moment0Map);
                    _moment1Bounds = GetBounds(Moment1Map);
                    UpdatePlotWindow();
                }
            }
        }
        private bool _useZScale = true;


        [Range(-0.1f, 0.1f)] public float MomentMapThreshold = 0.0f;
        public bool UseMask = true;


        [Header("Color Mapping")] 
        public ColorMapEnum ColorMapM0 = ColorMapEnum.Plasma;
        public ColorMapEnum ColorMapM1 = ColorMapEnum.Turbo;
        public ScalingType ScalingTypeM0 = ScalingType.Sqrt;
        public ScalingType ScalingTypeM1 = ScalingType.Linear;
        [Range(-1, 1)] public float ScalingBias = 0.0f;
        [Range(0, 5)] public float ScalingContrast = 1.0f;
        public float ScalingAlpha = 1000.0f;
        [Range(0, 5)] public float ScalingGamma = 1.0f;
        
        private ComputeShader _computeShader;
        private float _cachedMomentMapThreshold = Single.NaN;
        private int _kernelIndex, _kernelIndexMasked, _colormapKernelIndex;
        private uint _kernelThreadGroupX, _kernelThreadGroupY;
        private Texture3D _dataCube;
        private Texture3D _maskCube;
        private Texture2D _colormapTexture;
        private Vector2 _moment0Bounds, _moment1Bounds;
        
        private struct MaterialID
        {
            public static readonly int DataCube = Shader.PropertyToID("DataCube");
            public static readonly int MaskCube = Shader.PropertyToID("MaskCube");
            public static readonly int Moment0Result = Shader.PropertyToID("Moment0Result");
            public static readonly int Moment1Result = Shader.PropertyToID("Moment1Result");
            public static readonly int Threshold = Shader.PropertyToID("Threshold");
            public static readonly int Depth = Shader.PropertyToID("Depth");

            public static readonly int ClampMin = Shader.PropertyToID("ClampMin");
            public static readonly int ClampMax = Shader.PropertyToID("ClampMax");
            public static readonly int InputTexture = Shader.PropertyToID("InputTexture");
            public static readonly int OutputTexture = Shader.PropertyToID("OutputTexture");
            public static readonly int ColormapTexture = Shader.PropertyToID("ColormapTexture");
            public static readonly int ColormapOffset = Shader.PropertyToID("ColormapOffset");
            
            public static readonly int ScaleType = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleAlpha = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma = Shader.PropertyToID("ScaleGamma");
            public static readonly int ScaleBias = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast = Shader.PropertyToID("ScaleContrast");
        }

        private void Start()
        {
            // Apply settings from config
            var config = Config.Instance;
            ColorMapM0 = config.momentMaps.m0.colorMap;
            ScalingTypeM0 = config.momentMaps.m0.scalingType;

            ColorMapM1 = config.momentMaps.m1.colorMap;
            ScalingTypeM1 = config.momentMaps.m1.scalingType;

            UseMask = config.momentMaps.defaultThresholdType == MomentMapMenuController.ThresholdType.Mask;
            MomentMapThreshold = config.momentMaps.defaultThreshold;
            
            UseZScale = config.momentMaps.defaultLimitType == MomentMapMenuController.LimitType.ZScale;
            
            _colormapTexture = (Texture2D) Resources.Load("allmaps");
            _colormapTexture.filterMode = FilterMode.Point;
            _colormapTexture.wrapMode = TextureWrapMode.Clamp;
            
            _computeShader = (ComputeShader) Resources.Load("MomentMapGenerator");
            _kernelIndex = _computeShader.FindKernel("MomentsGenerator");
            _kernelIndexMasked = _computeShader.FindKernel("MaskedMomentsGenerator");
            _colormapKernelIndex = _computeShader.FindKernel("LinearColormap");
            uint temp;
            // This assumes the masked and unmasked kernels use the same group size. They really should, though
            _computeShader.GetKernelThreadGroupSizes(_kernelIndex, out _kernelThreadGroupX, out _kernelThreadGroupY, out temp);
        }

        private void Update()
        {
            if (_cachedMomentMapThreshold != MomentMapThreshold)
            {
                CalculateMomentMaps(MomentMapThreshold);
            }
        }

        public bool CalculateMomentMaps()
        {
            return CalculateMomentMaps(_cachedMomentMapThreshold);
        }

        public bool CalculateMomentMaps(float threshold)
        {
            if (!_dataCube || !_computeShader)
            {
                return false;
            }

            bool maskActive = _maskCube && UseMask;
            int activeKernelIndex = maskActive ? _kernelIndexMasked : _kernelIndex;

            _cachedMomentMapThreshold = threshold;

            if (!Moment0Map || (Moment0Map.width != _dataCube.width || Moment0Map.height != _dataCube.height))
            {
                Moment0Map?.Release();
                Moment1Map?.Release();
                ImageOutput?.Release();
                
                Debug.Log($"Resizing moment map render texture to {_dataCube.width} x {_dataCube.height}");
                Moment0Map = InitRenderTexture(RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                Moment1Map = InitRenderTexture(RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                ImageOutput = InitRenderTexture();
            }

            // Update shader variables
            _computeShader.SetTexture(activeKernelIndex, MaterialID.DataCube, _dataCube);
            _computeShader.SetTexture(activeKernelIndex, MaterialID.Moment0Result, Moment0Map);
            _computeShader.SetTexture(activeKernelIndex, MaterialID.Moment1Result, Moment1Map);
            if (maskActive)
            {
                _computeShader.SetTexture(activeKernelIndex, MaterialID.MaskCube, _maskCube);
            }
            else
            {
                _computeShader.SetFloat(MaterialID.Threshold, _cachedMomentMapThreshold);
            }

            _computeShader.SetInt(MaterialID.Depth, _dataCube.depth);

            // Run compute shader in tiles, based on kernel's group size
            int threadGroupsX = Mathf.CeilToInt(_dataCube.width / ((float) (_kernelThreadGroupX)));
            int threadGroupsY = Mathf.CeilToInt(_dataCube.height / ((float) (_kernelThreadGroupY)));
            _computeShader.Dispatch(activeKernelIndex, threadGroupsX, threadGroupsY, 1);
            _moment0Bounds = GetBounds(Moment0Map);
            _moment1Bounds = GetBounds(Moment1Map);
            UpdatePlotWindow();
            return true;
        }

        private RenderTexture InitRenderTexture(RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
        {
            var texture  = new RenderTexture(_dataCube.width, _dataCube.height, 0, format, readWrite);
            texture.enableRandomWrite = true;
            texture.useMipMap = false;
            texture.filterMode = FilterMode.Point;
            texture.Create();
            return texture;
        }

        private Vector2 GetBounds(RenderTexture momentMap)
        {
            RenderTexture currentActiveRT = RenderTexture.active;
            Texture2D tex = new Texture2D(_dataCube.width, _dataCube.height, TextureFormat.RFloat, false);
            RenderTexture.active = momentMap;
            tex.ReadPixels(new Rect(0, 0, ImageOutput.width, ImageOutput.height), 0, 0);
            tex.Apply();
            var data = tex.GetRawTextureData<float>();

            float minValue = Single.MaxValue;
            float maxValue = -Single.MaxValue;
            if (_useZScale)
            {
                unsafe
                {
                    DataAnalysis.GetZScale(data.GetUnsafeReadOnlyPtr(), ImageOutput.width, ImageOutput.height, out minValue, out maxValue);
                }
            }
            else
            {
                foreach (var val in data)
                {
                    if (!float.IsNaN(val))
                    {
                        minValue = Math.Min(minValue, val);
                        maxValue = Math.Max(maxValue, val);
                    }
            
                }                
            }

            RenderTexture.active = currentActiveRT;
            return new Vector2(minValue, maxValue);
        }

        public void UpdatePlotWindow()
        {
            if (Moment0Map != null && Moment1Map != null )
            {
                // Run colormapping compute shader
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.InputTexture, Moment0Map);
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.OutputTexture, ImageOutput);
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.ColormapTexture, _colormapTexture);
                
                _computeShader.SetInt(MaterialID.ScaleType, ScalingTypeM0.GetHashCode());
                _computeShader.SetFloat(MaterialID.ScaleBias, ScalingBias);
                _computeShader.SetFloat(MaterialID.ScaleContrast, ScalingContrast);
                _computeShader.SetFloat(MaterialID.ScaleAlpha, ScalingAlpha);
                _computeShader.SetFloat(MaterialID.ScaleGamma, ScalingGamma);

                // Default MomentZero bounds: min to max
                _computeShader.SetFloat(MaterialID.ClampMin, _moment0Bounds.x);
                _computeShader.SetFloat(MaterialID.ClampMax, _moment0Bounds.y);
                float offset = (ColorMapM0.GetHashCode() + 0.5f) / ColorMapUtils.NumColorMaps;
                _computeShader.SetFloat(MaterialID.ColormapOffset, offset);
                int threadGroupsX = Mathf.CeilToInt(_dataCube.width / ((float)(_kernelThreadGroupX)));
                int threadGroupsY = Mathf.CeilToInt(_dataCube.height / ((float)(_kernelThreadGroupY)));
                _computeShader.Dispatch(_colormapKernelIndex, threadGroupsX, threadGroupsY, 1);
                
                var colorBarM0 =  momentMapMenuController.gameObject.transform.Find("Map_container").gameObject.transform.Find("ColorbarM0").GetComponent<Colorbar>();
                colorBarM0.ScalingType = ScalingTypeM0;
                colorBarM0.ColorMap = ColorMapM0;
                colorBarM0.ScaleMin = _moment0Bounds.x;
                colorBarM0.ScaleMax = _moment0Bounds.y;

                Texture2D tex = new Texture2D(ImageOutput.width, ImageOutput.height);
                RenderTexture currentActiveRT = RenderTexture.active;
                RenderTexture.active = ImageOutput;
                tex.ReadPixels(new Rect(0, 0, ImageOutput.width, ImageOutput.height), 0, 0);
                tex.Apply();
                Sprite spriteM0 = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width, tex.height));
                spriteM0.texture.filterMode = FilterMode.Point;
                var imageM0 = momentMapMenuController.gameObject.transform.Find("Map_container").gameObject.transform.Find("MomentMap0").GetComponent<Image>();
                imageM0.sprite = spriteM0;
                imageM0.preserveAspect = true;

                // Run colormapping compute shader
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.InputTexture, Moment1Map);
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.OutputTexture, ImageOutput);
                _computeShader.SetTexture(_colormapKernelIndex, MaterialID.ColormapTexture, _colormapTexture);

                // Default MomentOne bounds: ZScale min to max (linear scaling)
                _computeShader.SetInt(MaterialID.ScaleType, ScalingTypeM1.GetHashCode());
                // Switch bounds if the map needs to be inverted
                _computeShader.SetFloat(MaterialID.ClampMin, Inverted ? _moment1Bounds.y: _moment1Bounds.x);
                _computeShader.SetFloat(MaterialID.ClampMax, Inverted ? _moment1Bounds.x: _moment1Bounds.y);
                
                var colorBarM1 =  momentMapMenuController.gameObject.transform.Find("Map_container").gameObject.transform.Find("ColorbarM1").GetComponent<Colorbar>();
                colorBarM1.ScalingType = ScalingTypeM1;
                colorBarM1.ColorMap = ColorMapM1;
                colorBarM1.ScaleMin = Inverted ? _moment1Bounds.y : _moment1Bounds.x;
                colorBarM1.ScaleMax = Inverted ? _moment1Bounds.x : _moment1Bounds.y;

                offset = (ColorMapM1.GetHashCode() + 0.5f) / ColorMapUtils.NumColorMaps;
                _computeShader.SetFloat(MaterialID.ColormapOffset, offset);
                threadGroupsX = Mathf.CeilToInt(_dataCube.width / ((float)(_kernelThreadGroupX)));
                threadGroupsY = Mathf.CeilToInt(_dataCube.height / ((float)(_kernelThreadGroupY)));
                _computeShader.Dispatch(_colormapKernelIndex, threadGroupsX, threadGroupsY, 1);


                Texture2D tex1 = new Texture2D(ImageOutput.width, ImageOutput.height);
                RenderTexture.active = ImageOutput;
                tex1.ReadPixels(new Rect(0, 0, ImageOutput.width, ImageOutput.height), 0, 0);
                tex1.Apply();
                Sprite spriteM1 = Sprite.Create(tex1, new Rect(0, 0, tex1.width, tex1.height), new Vector2(tex1.width, tex1.height));
                spriteM1.texture.filterMode = FilterMode.Point;

                var imageM1 = momentMapMenuController.gameObject.transform.Find("Map_container").gameObject.transform.Find("MomentMap1").GetComponent<Image>();
                imageM1.sprite = spriteM1;
                imageM1.preserveAspect = true;
                momentMapMenuController.gameObject.transform.Find("Main_container").gameObject.transform.Find("Line_Threshold").gameObject.transform.Find("ThresholdValue").GetComponent<Text>().text = MomentMapThreshold.ToString();
                RenderTexture.active = currentActiveRT;
            }
        }

    }
}