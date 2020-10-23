using System;
using UnityEngine;

namespace VolumeData
{
    public class MomentMapRenderer : MonoBehaviour
    {
        public RenderTexture Moment0Map { get; private set; }

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

        [Range(-0.1f, 0.1f)] public float MomentMapThreshold = 0.0f;
        public bool UseMask = true;

        private ComputeShader _computeShader;
        private float _cachedMomentMapThreshold = Single.NaN;
        private int _kernelIndex, _kernelIndexMasked;
        private uint _kernelThreadGroupX, _kernelThreadGroupY;
        private Texture3D _dataCube;
        private Texture3D _maskCube;

        private struct MaterialID
        {
            public static readonly int DataCube = Shader.PropertyToID("DataCube");
            public static readonly int MaskCube = Shader.PropertyToID("MaskCube");
            public static readonly int MomentResult = Shader.PropertyToID("MomentResult");
            public static readonly int Threshold = Shader.PropertyToID("Threshold");
            public static readonly int Depth = Shader.PropertyToID("Depth");
        }

        private void Start()
        {
            _computeShader = (ComputeShader) Resources.Load("MomentMapGenerator");
            _kernelIndex = _computeShader.FindKernel("MomentZeroGenerator");
            _kernelIndexMasked = _computeShader.FindKernel("MaskedMomentZeroGenerator");
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
                Debug.Log($"Resizing moment map render texture to {_dataCube.width} x {_dataCube.height}");
                Moment0Map = new RenderTexture(_dataCube.width, _dataCube.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
                Moment0Map.enableRandomWrite = true;
                Moment0Map.useMipMap = false;
                Moment0Map.filterMode = FilterMode.Point;
                Moment0Map.Create();
            }

            // Update shader variables
            _computeShader.SetTexture(activeKernelIndex, MaterialID.DataCube, _dataCube);
            _computeShader.SetTexture(activeKernelIndex, MaterialID.MomentResult, Moment0Map);
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
            return true;
        }

        public void OnGUI()
        {
            if (Moment0Map != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Moment0Map.width * 3, Moment0Map.height * 3), Moment0Map);
            }
        }
    }
}