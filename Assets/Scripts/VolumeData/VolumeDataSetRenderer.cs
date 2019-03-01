using UnityEngine;

namespace VolumeData
{
    public class VolumeDataSetRenderer : MonoBehaviour
    {
        public ColorMapDelegate OnColorMapChanged;

        [Header("Rendering Settings")]
        // Step control
        [Range(16, 512)]
        public int MaxSteps = 192;

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

        // RenderDownsampling
        [Header("Render Downsampling")]
        public int XFactor = 1;
        public int YFactor = 1;
        public int ZFactor = 1;

        // Vignette Rendering
        [Header("Vignette Rendering Controls")] [Range(0, 0.5f)]
        public float VignetteFadeStart = 0.15f;

        [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
        [Range(0, 1)] public float VignetteIntensity = 0.0f;
        public Color VignetteColor = Color.black;

        [Header("Thresholds")]
        // Spatial thresholding
        public Vector3 SliceMin = Vector3.zero;

        public Vector3 SliceMax = Vector3.one;

        // Scale max n' min
        public float ScaleMax;
        public float ScaleMin;

        // Value thresholding
        [Range(0, 1)] public float ThresholdMin = 0;
        [Range(0, 1)] public float ThresholdMax = 1;

        [Space(10)] public Material RayMarchingMaterial;
        public string FileName;
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }
        public Vector3 InitialScale { get; private set; }
        
        public float InitialThresholdMin { get; private set; }
        public float InitialThresholdMax { get; private set; }

        private MeshRenderer _renderer;
        private Material _materialInstance;
        private VolumeDataSet _dataSet;

        #region Material Property IDs

        private int _idSliceMin, _idSliceMax, _idThresholdMin, _idThresholdMax, _idJitter, _idMaxSteps;
        private int _idColorMapIndex, _idScaleMin, _idScaleMax;
        private int _idFoveationStart, _idFoveationEnd, _idFoveationJitter, _idFoveatedStepsLow, _idFoveatedStepsHigh;
        private int _idVignetteFadeStart, _idVignetteFadeEnd, _idVignetteIntensity, _idVignetteColor;

        #endregion

        private void GetPropertyIds()
        {
            _idSliceMin = Shader.PropertyToID("_SliceMin");
            _idSliceMax = Shader.PropertyToID("_SliceMax");
            _idThresholdMin = Shader.PropertyToID("_ThresholdMin");
            _idThresholdMax = Shader.PropertyToID("_ThresholdMax");
            _idJitter = Shader.PropertyToID("_Jitter");
            _idMaxSteps = Shader.PropertyToID("_MaxSteps");
            _idColorMapIndex = Shader.PropertyToID("_ColorMapIndex");
            _idScaleMin = Shader.PropertyToID("_ScaleMin");
            _idScaleMax = Shader.PropertyToID("_ScaleMax");

            _idFoveationStart = Shader.PropertyToID("FoveationStart");
            _idFoveationEnd = Shader.PropertyToID("FoveationEnd");
            _idFoveationJitter = Shader.PropertyToID("FoveationJitter");
            _idFoveatedStepsLow = Shader.PropertyToID("FoveatedStepsLow");
            _idFoveatedStepsHigh = Shader.PropertyToID("FoveatedStepsHigh");

            _idVignetteFadeStart = Shader.PropertyToID("VignetteFadeStart");
            _idVignetteFadeEnd = Shader.PropertyToID("VignetteFadeEnd");
            _idVignetteIntensity = Shader.PropertyToID("VignetteIntensity");
            _idVignetteColor = Shader.PropertyToID("VignetteIntensity");
        }

        public void Start()
        {
            _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName);
            _dataSet.RenderVolume(XFactor, YFactor, ZFactor);
            ScaleMin = _dataSet.CubeMin;
            ScaleMax = _dataSet.CubeMax;

            GetPropertyIds();
            _renderer = GetComponent<MeshRenderer>();
            _materialInstance = Instantiate(RayMarchingMaterial);
            _materialInstance.SetTexture("_DataCube", _dataSet.DataCube);
            _materialInstance.SetInt("_NumColorMaps", ColorMapUtils.NumColorMaps);
            _materialInstance.SetFloat(_idFoveationStart, FoveationStart);
            _materialInstance.SetFloat(_idFoveationEnd, FoveationEnd);
            _renderer.material = _materialInstance;
            
            // Set initial values (for resetting later)
            InitialPosition = transform.position;
            InitialScale = transform.localScale;
            InitialRotation = transform.rotation;
            InitialThresholdMax = ThresholdMax;
            InitialThresholdMin = ThresholdMin;
        }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            ColorMap = ColorMapUtils.FromHashCode(newIndex);
        }

        // Update is called once per frame
        public void Update()
        {
            _materialInstance.SetVector(_idSliceMin, SliceMin);
            _materialInstance.SetVector(_idSliceMax, SliceMax);
            _materialInstance.SetFloat(_idThresholdMin, ThresholdMin);
            _materialInstance.SetFloat(_idThresholdMax, ThresholdMax);
            _materialInstance.SetFloat(_idJitter, Jitter);
            _materialInstance.SetFloat(_idMaxSteps, MaxSteps);
            _materialInstance.SetFloat(_idColorMapIndex, ColorMap.GetHashCode());
            _materialInstance.SetFloat(_idScaleMax, ScaleMax);
            _materialInstance.SetFloat(_idScaleMin, ScaleMin);

            _materialInstance.SetFloat(_idFoveationStart, FoveationStart);
            _materialInstance.SetFloat(_idFoveationEnd, FoveationEnd);
            if (FoveatedRendering)
            {
                _materialInstance.SetFloat(_idFoveationJitter, FoveationJitter);
                _materialInstance.SetInt(_idFoveatedStepsLow, FoveatedStepsLow);
                _materialInstance.SetInt(_idFoveatedStepsHigh, FoveatedStepsHigh);
            }
            else
            {
                _materialInstance.SetInt(_idFoveatedStepsLow, MaxSteps);
                _materialInstance.SetInt(_idFoveatedStepsHigh, MaxSteps);
            }

            _materialInstance.SetFloat(_idVignetteFadeStart, VignetteFadeStart);
            _materialInstance.SetFloat(_idVignetteFadeEnd, VignetteFadeEnd);
            _materialInstance.SetFloat(_idVignetteIntensity, VignetteIntensity);
            _materialInstance.SetColor(_idVignetteColor, VignetteColor);
        }

        public void OnDestroy()
        {
            _dataSet.CleanUp();
        }


    }
}