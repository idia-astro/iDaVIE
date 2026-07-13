/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// =============================================================================
// AFTER — Thin MonoBehaviour orchestrator (design-level worked example)
//
// VolumeDataSetRenderer is now a pure coordinator:
//   1. Holds Inspector-serialised configuration fields.
//   2. Creates and wires service instances in _startFunc.
//   3. In Update, reads Inspector state into value objects and forwards them
//      to services — no business logic lives here.
//   4. Passes through queries to services for external callers.
//
// CK AFTER (estimated):
//   WMC  ≈ 18  (8 public + 3 private, avg complexity 1.8)
//   CBO  ≈ 9   (IVolumeRenderer, IMaskController, ICoordinateMapper,
//               IRegionController, IRestFrequencyController, IVolumeDataExporter,
//               VolumeDataSet, MomentMapRenderer, Config)
//   RFC  ≈ 22
//   LCOM ≈ 0.20  (all methods route through the 6 injected services)
//   DIT  = 1  (MonoBehaviour)
// =============================================================================

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Unity MonoBehaviour entry point for a loaded volume data set.
    /// Owns no business logic; delegates to six injected services.
    /// </summary>
    public class VolumeDataSetRenderer : MonoBehaviour
    {
        // ── Inspector-serialised configuration ────────────────────────────────

        [Header("Rendering Settings")]
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

        [Header("Foveated Rendering Controls")]
        public bool FoveatedRendering = false;
        [Range(0, 0.5f)] public float FoveationStart = 0.15f;
        [Range(0, 0.5f)] public float FoveationEnd = 0.40f;
        [Range(0, 0.5f)] public float FoveationJitter = 0.0f;
        [Range(16, 512)] public int FoveatedStepsLow = 64;
        [Range(16, 512)] public int FoveatedStepsHigh = 384;

        [Header("Vignette Rendering Controls")]
        [Range(0, 0.5f)] public float VignetteFadeStart = 0.15f;
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
        public int[] subsetBounds = { -1, -1, -1, -1, -1, -1 };
        public int SelectedHdu = 1;
        public int[] trueBounds;

        [Header("Debug Settings")]
        public bool FactorOverride = false;
        public int XFactor = 1, YFactor = 1, ZFactor = 1;
        public int CubeDepthAxis = 2;
        public int CubeSlice = 1;

        [Header("Benchmarking")]
        public bool RandomVolume = false;
        public int RandomCubeSize = 512;

        [Header("Miscellaneous")]
        public Material RayMarchingMaterial;
        public Material MaskMaterial;
        public TextMeshProUGUI loadText;
        public Slider progressBar;

        // ── Injected services — all private; consumers call the passthrough API ─

        private IVolumeRenderer          _volumeRenderer;
        private IMaskController          _maskController;
        private ICoordinateMapper        _coordinateMapper;
        private IRegionController        _regionController;
        private IRestFrequencyController _restFrequency;
        private IVolumeDataExporter      _exporter;

        // ── Private state retained by the orchestrator ─────────────────────────

        private VolumeDataSet _dataSet;
        private MomentMapRenderer _momentMapRenderer;
        private MeshRenderer _meshRenderer;
        public bool started = false;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        public void Start()
        {
            started = false;
        }

        public IEnumerator _startFunc()
        {
            var config = Config.Instance;
            StartCoroutine(updateStatus("Loading new cube...", 3));
            yield return null;

            ApplyConfigToInspectorFields(config);

            _dataSet = RandomVolume
                ? VolumeDataSet.LoadRandomFitsCube(0, RandomCubeSize, RandomCubeSize, RandomCubeSize, RandomCubeSize)
                : VolumeDataSet.LoadDataFromFitsFile(FileName, subsetBounds, trueBounds, IntPtr.Zero, CubeDepthAxis, CubeSlice, SelectedHdu);

            StartCoroutine(updateStatus("Loading mask...", 4));
            yield return null;

            VolumeDataSet maskDataSet = null;
            if (!string.IsNullOrEmpty(MaskFileName))
            {
                maskDataSet = VolumeDataSet.LoadDataFromFitsFile(MaskFileName, subsetBounds, trueBounds, _dataSet.FitsData);
                maskDataSet.GenerateVolumeTexture(FilterMode.Point, XFactor, YFactor, ZFactor);
            }

            StartCoroutine(updateStatus("Preparing UI...", 5));
            yield return null;

            // ── Wire services ──────────────────────────────────────────────────

            var renderingService = new VolumeRenderingService(RayMarchingMaterial, MaskMaterial);
            renderingService.BindDataSets(_dataSet, maskDataSet, MaximumCubeSizeInMB);
            _volumeRenderer = renderingService;

            _coordinateMapper = new VolumeCoordinateMapper(transform, _dataSet, CubeDepthAxis);

            var regionService = new RegionControllerService(_coordinateMapper, _dataSet, transform, MaximumCubeSizeInMB);
            regionService.BindInputController(FindObjectOfType<VolumeInputController>());
            _regionController = regionService;

            _maskController = new MaskControllerService();
            if (maskDataSet != null)
                _maskController.Initialise(_dataSet);

            _restFrequency = new RestFrequencyService(_dataSet);

            _exporter = new VolumeDataExportService(_dataSet, GetComponentInChildren<FeatureSetManager>());
            if (maskDataSet != null)
                ((VolumeDataExportService)_exporter).BindMaskDataSet(maskDataSet, false);

            // ── Unity-specific wiring that cannot move into a plain service ────

            _meshRenderer          = GetComponent<MeshRenderer>();
            _meshRenderer.material = renderingService.GetMaterial();

            _momentMapRenderer = gameObject.AddComponent<MomentMapRenderer>();
            if (_momentMapRenderer != null)
            {
                _momentMapRenderer.DataCube = _dataSet.DataCube;
                _momentMapRenderer.Inverted = _dataSet.VelocityDirection == 1;
                _momentMapRenderer.momentMapMenuController = FindObjectOfType<VolumeCommandController>().momentMapMenuController;
                if (maskDataSet != null)
                    _momentMapRenderer.MaskCube = maskDataSet.DataCube;
            }

            ((MaskControllerService)_maskController).BindMomentMapRenderer(_momentMapRenderer);

            Shader.WarmupAllShaders();
            started = true;
            yield return 0;
        }

        public void Update()
        {
            if (!started) return;

            _volumeRenderer.ApplyRenderingParameters(BuildRenderingParameters());
            _volumeRenderer.ApplyMaskParameters(BuildMaskParameters());
            _volumeRenderer.ApplyFoveationParameters(BuildFoveationParameters());
            _volumeRenderer.ApplyVignetteParameters(BuildVignetteParameters());
        }

        public void OnDestroy()
        {
            _dataSet.CleanUp(RandomVolume);
        }

        // ── Public passthrough API (consumed by menus, input controllers, etc.) ─

        public VolumeDataSet     GetDataSet()            => _dataSet;
        public MomentMapRenderer GetMomentMapRenderer()  => _momentMapRenderer;
        public Vector3Int        GetCubeDimensions()     => new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);

        public void RegenerateCubes()       => _volumeRenderer.RegenerateCubes();
        public void ShiftColorMap(int d)    => _volumeRenderer.ShiftColorMap(d);

        public bool PaintCursor(short v)    => _maskController.PaintCursor(v);
        public void FinishBrushStroke()     => _maskController.FinishBrushStroke();

        public void SetCursor(Vector3 p, int b)              => _regionController.SetCursor(p, b);
        public void SetRegionStart(Vector3 p)                => _regionController.SetRegionStart(p);
        public void SetRegionEnd(Vector3 p)                  => _regionController.SetRegionEnd(p);
        public void ClearRegion()                            => _regionController.ClearRegion();
        public void CropToRegion(Vector3 min, Vector3 max)  => _regionController.CropToRegion(min, max);
        public void ResetCrop()                              => _regionController.ResetCrop();
        public void TeleportToRegion()                       => _regionController.TeleportToRegion();

        public void SaveSubCube()               => _exporter.SaveSubCube();
        public void SaveMask(bool overwrite)    => _exporter.SaveMask(overwrite);
        public string GetMaskSavedFilePath()    => _exporter.GetMaskSavedFilePath();

        public IReadOnlyDictionary<string, double> GetAvailableFrequencies()
            => _restFrequency.GetAvailableFrequencies();
        public void SetFrequencyByIndex(int i)        => _restFrequency.SetFrequencyByIndex(i);
        public void SetFrequencyOverride(double f)    => _restFrequency.SetFrequencyOverride(f);

        public IEnumerator updateStatus(string label, int progress)
        {
            loadText.text      = label;
            progressBar.value  = progress;
            yield return null;
        }

        // ── Value-object builders — read Inspector fields, no logic ────────────

        private RenderingParameters BuildRenderingParameters() =>
            new RenderingParameters(
                MaxSteps, ProjectionMode, TextureFilter, Jitter,
                ThresholdMin, ThresholdMax,
                ColorMap, ScalingType,
                ScalingBias, ScalingContrast, ScalingAlpha, ScalingGamma);

        private MaskParameters BuildMaskParameters() =>
            new MaskParameters(MaskMode, MaskVoxelSize, MaskVoxelColor, DisplayMask);

        private FoveationParameters BuildFoveationParameters() =>
            new FoveationParameters(FoveatedRendering, FoveationStart, FoveationEnd,
                                    FoveationJitter, FoveatedStepsLow, FoveatedStepsHigh);

        private VignetteParameters BuildVignetteParameters() =>
            new VignetteParameters(VignetteFadeStart, VignetteFadeEnd, VignetteIntensity, VignetteColor);

        private void ApplyConfigToInspectorFields(Config config)
        {
            TextureFilter     = config.bilinearFiltering ? FilterMode.Bilinear : FilterMode.Point;
            FoveatedRendering = config.foveatedRendering;
            MaxSteps          = config.maxRaymarchingSteps;
            FoveatedStepsHigh = config.maxRaymarchingSteps;
            MaximumCubeSizeInMB = config.gpuMemoryLimitMb;
            ColorMap          = config.defaultColorMap;
            ScalingType       = config.defaultScalingType;
            VignetteFadeEnd   = config.tunnellingVignetteEnd;
        }
    }
}
