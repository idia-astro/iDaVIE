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
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 */

// ══════════════════════════════════════════════════════════════════════════════
// BEFORE FILE — Sub-team 3 Refactoring Example 1
//
// This is the original VolumeDataSetRenderer.cs, annotated inline to show
// every code smell, SOLID/GRASP violation, and CK metric driver.
//
// Annotation legend:
//   [SRP]   Single Responsibility Principle violation
//   [OCP]   Open/Closed Principle violation
//   [ISP]   Interface Segregation Principle violation
//   [DIP]   Dependency Inversion Principle violation
//   [GRASP] GRASP principle violation
//   [CBO]   Coupling Between Objects — this line adds a coupling edge
//   [WMC]   Weighted Methods per Class — complexity note
//   [LCOM]  Cohesion note — these methods/fields share no common data
//   [→]     Which new class absorbs this responsibility in the after/ design
//
// Baseline CK metrics (Day 2, SonarQube + Understand):
//   WMC  = ~74   (target ≤ 20 for domain classes)
//   CBO  = ~31   (target ≤ 14 for domain / ≤ 25 for orchestrators)
//   RFC  = ~89   (target ≤ 50)
//   LCOM = ~0.81 (target ≤ 0.5)
//   Lines = 1,402; Public members = 152
// ══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
// [CBO] Every using below that introduces a concrete type adds a coupling edge.
using DataFeatures;       // [CBO] DataFeatures namespace — FeatureSetManager, Feature
using LineRenderer;       // [CBO] LineRenderer namespace — CuboidLine, PolyLine
using TMPro;              // [CBO] UI dependency — TextMeshProUGUI
using UnityEngine;        // [CBO] Unity engine — 1 using hides dozens of type dependencies
using UnityEngine.UI;     // [CBO] Unity UI — Slider
using Debug = UnityEngine.Debug;

namespace VolumeData
{
    // ──────────────────────────────────────────────────────────────────────────
    // [SRP] These three enums mix domain concepts (ScalingType, MaskMode) with
    // rendering pipeline concepts (ProjectionMode). In the after/ design,
    // ScalingType and MaskMode live in the domain layer; ProjectionMode is owned
    // by VolumeMaterialBinder.
    // ──────────────────────────────────────────────────────────────────────────
    public enum ScalingType
    {
        Linear = 0, Log = 1, Sqrt = 2, Square = 3, Power = 4, Gamma = 5
    }

    public enum MaskMode
    {
        // [OCP] Violation O1: Adding a fifth mode (e.g. Highlight) forces editing
        // this enum + Update() + BasicVolume.cginc + PaintMenuController.cs.
        // Four files, one new behaviour. The after/ IMaskMode Strategy means
        // a new mode = a new class only, nothing existing changes.
        // → After: IMaskMode + three concrete implementations per mode
        Disabled = 0, Enabled = 1, Inverted = 2, Isolated = 3
    }

    public enum ProjectionMode
    {
        MaximumIntensityProjection = 0, AverageIntensityProjection = 1
    }

    // ══════════════════════════════════════════════════════════════════════════
    // [SRP] GOD CLASS — 9 distinct responsibilities in 1,402 lines.
    //
    // Any change to file I/O, shader parameters, mask painting, coordinate
    // maths, foveated rendering, WCS, or VR comfort systems requires opening
    // this single file. Every developer who touches it risks breaking
    // unrelated functionality.
    //
    // Responsibility clusters identified:
    //   R1 — FITS data loading                    → VolumeDataSetRenderer (thin coordinator)
    //   R2 — GPU texture upload / eviction         → VolumeTextureManager
    //   R3 — Shader uniform binding                → VolumeMaterialBinder
    //   R4 — Foveated rendering decisions          → FoveatedSamplingPolicy
    //   R5 — Camera / coordinate maths             → VolumeCameraDriver
    //   R6 — Mask voxel painting                   → MaskEditor (out of scope for Example 1)
    //   R7 — Mask serialisation to FITS            → MaskPersistenceService
    //   R8 — WCS / rest frequency management       → WCSService
    //   R9 — Unity lifecycle plumbing              → VolumeRenderCoordinator (thin MonoBehaviour)
    //
    // [ISP] Violation I1: 152 public members, zero interfaces.
    // PaintMenuController only needs MaskMode + PaintMask() but gets all 152.
    // RenderingController only needs 5 rendering properties but gets all 152.
    //
    // [CBO] Estimated CBO = ~31 (target: ≤14 domain / ≤25 orchestrators) → FAIL
    // [WMC] Estimated WMC = ~74 (target: ≤20 domain)                     → FAIL
    // [LCOM] Estimated LCOM = ~0.81 (target: ≤0.5)                       → FAIL
    // ══════════════════════════════════════════════════════════════════════════
    public class VolumeDataSetRenderer : MonoBehaviour
    {
        public ColorMapDelegate OnColorMapChanged;

        // ──────────────────────────────────────────────────────────────────────
        // [SRP / ISP] These public fields belong to at least four different
        // classes in the after/ design. Grouping them all on one MonoBehaviour
        // forces the Inspector serialisation concern into every responsibility.
        // ──────────────────────────────────────────────────────────────────────

        [Header("Rendering Settings")]
        // [→ VolumeMaterialBinder] MaxSteps, Jitter, ProjectionMode, TextureFilter
        // [→ VolumeTextureManager] MaximumCubeSizeInMB, TextureFilter
        [Range(16, 512)] public int MaxSteps = 192;
        public long MaximumCubeSizeInMB = 250;
        public ProjectionMode ProjectionMode = ProjectionMode.MaximumIntensityProjection;
        public FilterMode TextureFilter = FilterMode.Point;
        [Range(0, 1)] public float Jitter = 1.0f;

        [Header("Mask Rendering and Editing Settings")]
        // [→ VolumeMaterialBinder] DisplayMask, MaskMode, MaskVoxelSize, MaskVoxelColor
        public bool DisplayMask = false;
        public MaskMode MaskMode = MaskMode.Disabled;
        [Range(0, 1)] public float MaskVoxelSize = 1.0f;
        public Color MaskVoxelColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        [Header("Foveated Rendering Controls")]
        // [→ FoveatedSamplingPolicy] All foveation parameters
        // [SRP] Foveated rendering is a self-contained subsystem. Its five
        // parameters and two step-count outputs have no shared fields with
        // texture management or coordinate maths — they are LCOM evidence.
        public bool FoveatedRendering = false;
        [Range(0, 0.5f)] public float FoveationStart = 0.15f;
        [Range(0, 0.5f)] public float FoveationEnd = 0.40f;
        [Range(0, 0.5f)] public float FoveationJitter = 0.0f;
        [Range(16, 512)] public int FoveatedStepsLow = 64;
        [Range(16, 512)] public int FoveatedStepsHigh = 384;

        [Header("Vignette Rendering Controls")]
        // [→ VolumeMaterialBinder] Vignette parameters
        [Range(0, 0.5f)] public float VignetteFadeStart = 0.15f;
        [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
        [Range(0, 1)] public float VignetteIntensity = 0.0f;
        public Color VignetteColor = Color.black;

        [Header("Thresholds")]
        // [→ VolumeMaterialBinder] ThresholdMin, ThresholdMax
        [Range(0, 1)] public float ThresholdMin = 0;
        [Range(0, 1)] public float ThresholdMax = 1;

        [Header("Color Mapping")]
        // [→ VolumeMaterialBinder] ColorMap, ScalingType and all scaling params
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public ScalingType ScalingType = ScalingType.Linear;
        [Range(-1, 1)] public float ScalingBias = 0.0f;
        [Range(0, 5)] public float ScalingContrast = 1.0f;
        public float ScalingAlpha = 1000.0f;
        [Range(0, 5)] public float ScalingGamma = 1.0f;

        [Header("File Input")]
        // [→ VolumeRenderCoordinator / data loading concern]
        public string FileName;
        public string MaskFileName;
        public int[] subsetBounds = {-1, -1, -1, -1, -1, -1};
        public int SelectedHdu = 1;
        public int[] trueBounds;

        // [CBO] Direct references to two Material instances — tightly coupled to
        // the built-in render pipeline. In the after/ design, VolumeMaterialBinder
        // owns these and VolumeRenderCoordinator injects them via IRenderPipeline.
        public Material RayMarchingMaterial;
        public Material MaskMaterial;

        [Header("Debug Settings")]
        public bool FactorOverride = false;
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
        public bool OverrideRestFrequency { get; set; } = false;

        // [→ WCSService] Rest frequency state — its own coherent cluster of fields
        // and methods that share nothing with texture or shader management.
        private double _restFrequencyGHz;
        private int _restFrequencyGHzListListIndex;
        public event Action RestFrequencyGHzChanged;
        public event Action RestFrequencyGHzListIndexChanged;
        public Dictionary<string, double> RestFrequencyGHzList { get; private set; }

        public int RestFrequencyGHzListIndex
        {
            get => _restFrequencyGHzListListIndex;
            set
            {
                _restFrequencyGHzListListIndex = value;
                if (value == 0)
                {
                    OverrideRestFrequency = false;
                    ResetRestFrequency();
                }
                else if (value == RestFrequencyGHzList.Count)
                {
                    OverrideRestFrequency = true;
                }
                else
                {
                    OverrideRestFrequency = true;
                    var newRestFrequencyGHz = RestFrequencyGHzList.Values.ElementAt(value);
                    RestFrequencyGHz = newRestFrequencyGHz;
                }
                RestFrequencyGHzListIndexChanged?.Invoke();
            }
        }

        public double RestFrequencyGHz
        {
            get => _restFrequencyGHz;
            set
            {
                _restFrequencyGHzChanged = true;
                _restFrequencyGHz = value;
                RestFrequencyGHzChanged?.Invoke();
            }
        }

        private bool _restFrequencyGHzChanged = false;
        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }
        public Vector3 InitialScale { get; private set; }
        public float InitialThresholdMin { get; private set; }
        public float InitialThresholdMax { get; private set; }

        // [→ VolumeCameraDriver] Cursor state — pure coordinate tracking with
        // no shared fields with shader parameters or texture management (LCOM evidence).
        public Vector3Int CursorVoxel { get; private set; }
        public float CursorValue { get; private set; }
        public Int16 CursorSource { get; private set; }
        public Int16 HighlightedSource;
        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel { get; private set; }
        private Vector3Int _vidCursorLocVoxel { get; set; }

        // [CBO] UI coupling — direct dependency on TextMeshPro and UnityEngine.UI.
        // A renderer class should not own references to loading-screen widgets.
        // [→ VolumeRenderCoordinator or an ILoadingView interface]
        public TextMeshProUGUI loadText;
        public Slider progressBar;

        [Range(0, 1)] public float SelectionSaturateFactor = 0.7f;

        // [CBO] + [GRASP Creator] Direct concrete type reference.
        // [→ Injected as IFeatureSetManager in after/ design]
        public FeatureSetManager FeatureSetManagerPrefab;

        // [→ VolumeCameraDriver] Outline objects for visual feedback
        // [GRASP Creator] The renderer creates CuboidLine and PolyLine objects it
        // does not logically own — see _startFunc() lines 430–468.
        private PolyLine _measuringLine;
        private CuboidLine _cubeOutline, _voxelOutline, _regionOutline, _videoCursorPositionOutline;

        // [CBO] Direct field reference to concrete FeatureSetManager
        private FeatureSetManager _featureManager = null;

        // [→ VolumeMaterialBinder] Material instances
        private MeshRenderer _renderer;
        private Material _materialInstance;
        private Material _maskMaterialInstance;

        // [CBO] + [SRP] MomentMapRenderer is a distinct subsystem created and
        // wired here via AddComponent — a Creator violation. [→ Injected IMomentMapRenderer]
        private MomentMapRenderer _momentMapRenderer;

        // [CBO] VolumeInputController dependency obtained via FindObjectOfType<>
        // (removed in Unity 6). [→ Inject IVolumeInputController]
        public VolumeInputController volumeInputController;

        // [→ MaskEditor] Mask painting state — no shared fields with shader or coordinate clusters
        private Vector3Int _previousPaintLocation;
        private short _previousPaintValue;
        private int _previousBrushSize = 1;
        private float _videoCursorLocSize = 0.06f;

        // [→ VolumeTextureManager] Data sets
        private VolumeDataSet _dataSet = null;
        private VolumeDataSet _maskDataSet = null;

        public bool IsMaskNew { get; private set; } = false;
        private string lastSavedMaskPath = "";
        public VolumeDataSet Mask => _maskDataSet;
        public VolumeDataSet Data => _dataSet;
        public bool HasWCS { get; private set; }

        // [CBO] AstFrame is an unmanaged IntPtr from the native WCS library.
        // This leaks the native ABI into the renderer's public surface.
        // [→ WCSService hides the IntPtr behind IWCSProvider]
        public IntPtr AstFrame { get => _dataSet.AstFrameSet; }
        public string StdOfRest => _dataSet.GetStdOfRest();

        // [→ VolumeTextureManager] Downsampling state
        private int _currentXFactor, _currentYFactor, _currentZFactor;
        public bool IsFullResolution => _currentXFactor * _currentYFactor * _currentZFactor == 1;
        private int _baseXFactor, _baseYFactor, _baseZFactor;
        public bool IsCropped { get; private set; }
        public Vector3Int CurrentCropMin { get; private set; }
        public Vector3Int CurrentCropMax { get; private set; }

        // [GRASP Information Expert] SourceStatsDict delegates directly to _maskDataSet.
        // The expert is _maskDataSet — this pass-through property is unnecessary surface area.
        public Dictionary<int, DataAnalysis.SourceStats> SourceStatsDict
        {
            get
            {
                if (_maskDataSet == null) return null;
                return _maskDataSet.SourceStatsDict;
            }
        }

        [Header("Benchmarking")]
        public bool RandomVolume = false;
        public int RandomCubeSize = 512;

        // ──────────────────────────────────────────────────────────────────────
        // [→ VolumeMaterialBinder]
        // This nested struct is the right instinct — caching Shader.PropertyToID
        // is correct. But it lives inside the God Class instead of inside
        // VolumeMaterialBinder, which is the class that should own all
        // shader property management.
        //
        // [WMC] The struct itself contributes ~30 static field initialisers to
        // the class's initialisation complexity.
        //
        // [CBO] Every call to _materialInstance.SetFloat(MaterialID.X, ...)
        // in Update() is a coupling to the shader property table. In the after/
        // design, VolumeMaterialBinder owns this struct and exposes typed
        // methods (e.g. SetThreshold(float min, float max)) so callers never
        // touch raw property IDs.
        // ──────────────────────────────────────────────────────────────────────
        #region Material Property IDs
        private struct MaterialID
        {
            public static readonly int DataCube             = Shader.PropertyToID("_DataCube");
            public static readonly int MaskCube             = Shader.PropertyToID("MaskCube");
            public static readonly int MaskMode             = Shader.PropertyToID("MaskMode");
            public static readonly int NumColorMaps         = Shader.PropertyToID("_NumColorMaps");
            public static readonly int SliceMin             = Shader.PropertyToID("_SliceMin");
            public static readonly int SliceMax             = Shader.PropertyToID("_SliceMax");
            public static readonly int ThresholdMin         = Shader.PropertyToID("_ThresholdMin");
            public static readonly int ThresholdMax         = Shader.PropertyToID("_ThresholdMax");
            public static readonly int Jitter               = Shader.PropertyToID("_Jitter");
            public static readonly int MaxSteps             = Shader.PropertyToID("_MaxSteps");
            public static readonly int ColorMapIndex        = Shader.PropertyToID("_ColorMapIndex");
            public static readonly int ScaleMin             = Shader.PropertyToID("_ScaleMin");
            public static readonly int ScaleMax             = Shader.PropertyToID("_ScaleMax");
            public static readonly int ScaleType            = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleBias            = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast        = Shader.PropertyToID("ScaleContrast");
            public static readonly int ScaleAlpha           = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma           = Shader.PropertyToID("ScaleGamma");
            public static readonly int FoveationStart       = Shader.PropertyToID("FoveationStart");
            public static readonly int FoveationEnd         = Shader.PropertyToID("FoveationEnd");
            public static readonly int FoveationJitter      = Shader.PropertyToID("FoveationJitter");
            public static readonly int FoveatedStepsLow     = Shader.PropertyToID("FoveatedStepsLow");
            public static readonly int FoveatedStepsHigh    = Shader.PropertyToID("FoveatedStepsHigh");
            public static readonly int VignetteFadeStart    = Shader.PropertyToID("VignetteFadeStart");
            public static readonly int VignetteFadeEnd      = Shader.PropertyToID("VignetteFadeEnd");
            public static readonly int VignetteIntensity    = Shader.PropertyToID("VignetteIntensity");
            public static readonly int VignetteColor        = Shader.PropertyToID("VignetteColor");
            public static readonly int HighlightMin         = Shader.PropertyToID("HighlightMin");
            public static readonly int HighlightMax         = Shader.PropertyToID("HighlightMax");
            public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");
            public static readonly int CubeDimensions       = Shader.PropertyToID("CubeDimensions");
            public static readonly int RegionDimensions     = Shader.PropertyToID("RegionDimensions");
            public static readonly int RegionOffset         = Shader.PropertyToID("RegionOffset");
            public static readonly int MaskEntries          = Shader.PropertyToID("MaskEntries");
            public static readonly int MaskVoxelSize        = Shader.PropertyToID("MaskVoxelSize");
            public static readonly int MaskVoxelOffsets     = Shader.PropertyToID("MaskVoxelOffsets");
            public static readonly int MaskVoxelColor       = Shader.PropertyToID("MaskVoxelColor");
            public static readonly int ModelMatrix          = Shader.PropertyToID("ModelMatrix");
            public static readonly int HighlightedSource    = Shader.PropertyToID("HighlightedSource");
        }
        #endregion

        [Header("Miscellaneous")]
        public bool started = false;
        public bool FileChanged = true;

        // ──────────────────────────────────────────────────────────────────────
        // [→ VolumeTextureManager or VolumeRenderCoordinator]
        // Scale properties that wrap transform.localScale. Fine as utilities,
        // but their presence here inflates the public API surface (ISP violation).
        // ──────────────────────────────────────────────────────────────────────
        public float XScale
        {
            get { return gameObject.transform.localScale.x; }
            set { Vector3 s = gameObject.transform.localScale; gameObject.transform.localScale = new Vector3(value, s.y, s.z); }
        }
        public float YScale
        {
            get { return gameObject.transform.localScale.y; }
            set { Vector3 s = gameObject.transform.localScale; gameObject.transform.localScale = new Vector3(s.x, value, s.z); }
        }
        public float ZScale
        {
            get { return gameObject.transform.localScale.z; }
            set { Vector3 s = gameObject.transform.localScale; gameObject.transform.localScale = new Vector3(s.x, s.y, value); }
        }

        // ══════════════════════════════════════════════════════════════════════
        // [SRP] R9 — Unity lifecycle
        // [→ VolumeRenderCoordinator (thin MonoBehaviour)]
        // Start() and _startFunc() are the composition root. They wire up 8+
        // systems. In the after/ design, VolumeRenderCoordinator.Start() calls
        // each extracted class's Initialise() in sequence.
        // ══════════════════════════════════════════════════════════════════════
        public void Start()
        {
            started = false;
        }

        public IEnumerator _startFunc()
        {
            // ──────────────────────────────────────────────────────────────────
            // [DIP] Violation D1 — Config.Instance is a global singleton.
            // The renderer cannot be tested with different config values without
            // modifying global state. [→ Inject IAppConfig]
            // ──────────────────────────────────────────────────────────────────
            var config = Config.Instance;     // [CBO] Config singleton
            Debug.Log("Loading data for the new cube.");
            StartCoroutine(updateStatus("Loading new cube...", 3));
            yield return new WaitForSeconds(0.001f);
            TextureFilter    = config.bilinearFiltering ? FilterMode.Bilinear : FilterMode.Point;
            FoveatedRendering = config.foveatedRendering;
            MaxSteps         = config.maxRaymarchingSteps;
            FoveatedStepsHigh = config.maxRaymarchingSteps;
            MaximumCubeSizeInMB = config.gpuMemoryLimitMb;
            ColorMap         = config.defaultColorMap;
            ScalingType      = config.defaultScalingType;
            VignetteFadeEnd  = config.tunnellingVignetteEnd;
            _videoCursorLocSize = config.videoCursorLocHighlightSize;

            // ──────────────────────────────────────────────────────────────────
            // [SRP] R1 — FITS data loading
            // [→ VolumeRenderCoordinator calls IVolumeDataLoader.LoadAsync()]
            // Loading a random cube for benchmarking vs. loading from disk are
            // two separate data-source strategies; neither belongs in a renderer.
            // ──────────────────────────────────────────────────────────────────
            if (RandomVolume)
                _dataSet = VolumeDataSet.LoadRandomFitsCube(0, RandomCubeSize, RandomCubeSize, RandomCubeSize, RandomCubeSize);
            else
                _dataSet = VolumeDataSet.LoadDataFromFitsFile(FileName, subsetBounds, trueBounds, IntPtr.Zero, CubeDepthAxis, CubeSlice, SelectedHdu);

            // ──────────────────────────────────────────────────────────────────
            // [DIP] Violation D2 — FindObjectOfType is a scene-graph search.
            // Removed in Unity 6. Cannot be mocked in tests.
            // [→ Inject IVolumeInputController and IFeatureSetManager]
            // ──────────────────────────────────────────────────────────────────
            volumeInputController = FindObjectOfType<VolumeInputController>();   // [CBO]
            _featureManager = GetComponentInChildren<FeatureSetManager>();       // [CBO]
            if (_featureManager == null)
                Debug.Log($"No FeatureManager attached to VolumeDataSetRenderer. Attach prefab for use of Features.");

            // ──────────────────────────────────────────────────────────────────
            // [SRP] R2 — GPU texture management
            // [→ VolumeTextureManager.GenerateInitialTexture()]
            // ──────────────────────────────────────────────────────────────────
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
                _maskDataSet = VolumeDataSet.LoadDataFromFitsFile(MaskFileName, subsetBounds, trueBounds, _dataSet.FitsData);
                _maskDataSet.GenerateVolumeTexture(FilterMode.Point, XFactor, YFactor, ZFactor);
            }

            Debug.Log("Loading mask data complete.");
            StartCoroutine(updateStatus("Preparing UI...", 5));
            yield return new WaitForSeconds(0.001f);

            // ──────────────────────────────────────────────────────────────────
            // [SRP] R3 — Shader/material setup
            // [→ VolumeMaterialBinder.Initialise(dataSet, maskDataSet)]
            // Note: Instantiate() here creates concrete Material objects.
            // VolumeMaterialBinder abstracts this behind IRenderPipeline so the
            // coordinator never touches Material directly.
            // ──────────────────────────────────────────────────────────────────
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

            InitialPosition = transform.position;
            InitialScale    = transform.localScale;
            InitialRotation = transform.rotation;
            InitialThresholdMax = ThresholdMax;
            InitialThresholdMin = ThresholdMin;

            // ──────────────────────────────────────────────────────────────────
            // [GRASP Creator] The renderer creates five visual objects it does
            // not logically own. CuboidLine and PolyLine are presentation-layer
            // objects owned by VolumeCameraDriver. A renderer should not be a
            // factory for outline gizmos.
            // [→ VolumeCameraDriver.Initialise() creates these internally]
            // ──────────────────────────────────────────────────────────────────
            _cubeOutline = new CuboidLine { Parent = transform, Center = Vector3.zero, Bounds = Vector3.one };
            _cubeOutline.Activate();
            Feature.SetCubeColors(_cubeOutline, Color.white, true);

            CursorVoxel = new Vector3Int(-1, -1, -1);
            _voxelOutline = new CuboidLine { Parent = transform, Center = Vector3.zero, Color = Color.green, Bounds = Vector3.one };

            _vidCursorLocVoxel = new Vector3Int(-1, -1, -1);
            _videoCursorPositionOutline = new CuboidLine { Parent = transform, Center = Vector3.zero, Color = Color.cyan, Bounds = Vector3.one };

            _regionOutline = new CuboidLine { Parent = transform, Center = Vector3.zero, Color = Color.green, Bounds = Vector3.one };

            _measuringLine = new PolyLine { Parent = transform, Color = Color.white };

            if (_featureManager != null)
            {
                _featureManager.CreateSelectionFeatureSet();
                if (_maskDataSet != null)
                {
                    var maskFeatureSet = _featureManager.CreateMaskFeatureSet();
                    _maskDataSet?.FillFeatureSet(maskFeatureSet);
                }
            }

            // ──────────────────────────────────────────────────────────────────
            // [SRP] R8 — WCS setup
            // [→ WCSService.Initialise(dataSet)]
            // The WCS block reads AstFrameSet (native IntPtr) — domain logic
            // embedded in a renderer method.
            // ──────────────────────────────────────────────────────────────────
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
                RestFrequencyGHz = _dataSet.FitsRestFrequency;
            else
                _restFrequencyGHz = 0.0;

            PopulateRestFrequenyList();

            // ──────────────────────────────────────────────────────────────────
            // [DIP] Violation D3 — AddComponent creates MomentMapRenderer at
            // runtime. Cannot be mocked. Replaced in Unity 6 by component setup
            // at design time or explicit injection.
            // [→ Inject IMomentMapRenderer; wired at scene composition root]
            // ──────────────────────────────────────────────────────────────────
            _momentMapRenderer = gameObject.AddComponent(typeof(MomentMapRenderer)) as MomentMapRenderer; // [CBO]
            if (_momentMapRenderer)
            {
                _momentMapRenderer.DataCube = _dataSet.DataCube;
                _momentMapRenderer.Inverted = _dataSet.VelocityDirection == 1;
                // [DIP] Another FindObjectOfType — scene-graph coupling
                _momentMapRenderer.momentMapMenuController = FindObjectOfType<VolumeCommandController>().momentMapMenuController; // [CBO]

                if (_maskDataSet != null)
                    _momentMapRenderer.MaskCube = _maskDataSet.DataCube;
            }

            if (IsFullResolution)
            {
                CropToRegion(Vector3.one, new Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
                IsCropped = false;
            }

            Shader.WarmupAllShaders();
            CurrentCropMin = new Vector3Int(0, 0, 0);
            CurrentCropMax = new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
            started = true;
            yield return 0;
        }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] R8 — WCS / rest frequency: this method and the related property
        // setter form a coherent cluster that shares zero fields with shader
        // management or texture upload — LCOM evidence.
        // [→ WCSService.PopulateRestFrequencyList()]
        //
        // [DIP] Reads Config.Instance again — second DIP D1 occurrence.
        // ──────────────────────────────────────────────────────────────────────
        private void PopulateRestFrequenyList()
        {
            RestFrequencyGHzList = new Dictionary<string, double>();
            RestFrequencyGHzList.Add("Default", RestFrequencyGHz);
            foreach (var emissionLine in Config.Instance.restFrequenciesGHz) // [CBO] [DIP]
            {
                RestFrequencyGHzList.Add(emissionLine.Key, emissionLine.Value);
            }
            RestFrequencyGHzList.Add("Custom", 0.0);
        }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] UI concern inside a renderer — loading progress callbacks.
        // [→ ILoadingView interface observed by a separate loading controller]
        // ──────────────────────────────────────────────────────────────────────
        public IEnumerator updateStatus(string label, int progress)
        {
            loadText.text = label;          // [CBO] TextMeshProUGUI dependency
            progressBar.value = progress;   // [CBO] Slider dependency
            yield return new WaitForSeconds(0.001f);
        }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] R2 — GPU texture management
        // [→ VolumeTextureManager.GenerateDownsampledTexture()]
        //
        // This is a clean method but its home is wrong. VolumeTextureManager
        // owns all logic for computing downsample factors and uploading to GPU.
        // [WMC] Method complexity: 1 (simple delegation) — acceptable in isolation,
        // but it contributes to the overall WMC of the God Class.
        // ──────────────────────────────────────────────────────────────────────
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
            GenerateDownsampledCube();
            if (IsCropped) CropToFeature();
            else if (IsFullResolution) CropToRegion(Vector3.one, new Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
        }

        public VolumeDataSet GetDataSet() { return _dataSet; }
        public MomentMapRenderer GetMomentMapRenderer() { return _momentMapRenderer; }

        public void ShiftColorMap(int delta)
        {
            int numColorMaps = ColorMapUtils.NumColorMaps;
            int currentIndex = ColorMap.GetHashCode();
            int newIndex = (currentIndex + delta + numColorMaps) % numColorMaps;
            ColorMap = ColorMapUtils.FromHashCode(newIndex);
        }

        // ══════════════════════════════════════════════════════════════════════
        // [SRP] R5 — Camera / coordinate maths cluster
        // [→ VolumeCameraDriver]
        //
        // [DIP] Violation D4 — These methods use transform.InverseTransformPoint
        // and Quaternion.Inverse, which are UnityEngine types. The brief mandates:
        // "Domain code must NOT transitively depend on UnityEngine types."
        // These are domain functions (coordinate maths) but they cannot be tested
        // outside Unity because they read from transform (a MonoBehaviour field).
        //
        // [→ VolumeCameraDriver.WorldToObjectSpace(Vector3, Matrix4x4)] accepts
        // a plain Matrix4x4 — a value type. The coordinator passes the current
        // transform matrix. The maths becomes a pure function, testable in
        // edit mode with no GPU or Unity player loop.
        // ══════════════════════════════════════════════════════════════════════
        public Vector3 ConvertWorldPositionToDataCubePosition(Vector3 worldLoc)
        {
            // [DIP] transform.InverseTransformPoint — UnityEngine dependency in domain logic
            Vector3 dataCubePos = transform.InverseTransformPoint(worldLoc);
            return dataCubePos;
        }

        public Quaternion ConvertWorldRotationToDatacubeRotation(Quaternion worldRot)
        {
            // [DIP] Quaternion.Inverse — UnityEngine dependency in domain logic
            Quaternion dataCubeRot = Quaternion.Inverse(transform.rotation) * worldRot;
            return dataCubeRot;
        }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] Violation S3 — SetCursorPosition does four jobs (see violation
        // catalogue S3):
        //   1. World-to-voxel coordinate conversion  → VolumeCameraDriver
        //   2. Voxel outline position update         → VolumeCameraDriver
        //   3. Cursor value lookup from data         → data layer concern
        //   4. Mask source ID lookup                 → MaskEditor concern
        //
        // [GRASP Information Expert] Line 657: the data value lookup belongs
        // with VolumeDataSet (the expert), not inside the renderer.
        //
        // [WMC] High cyclomatic complexity: 5+ branches → contributes ~6 to WMC
        // ──────────────────────────────────────────────────────────────────────
        public void SetCursorPosition(Vector3 cursor, int brushSize)
        {
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursor); // [DIP]
            Bounds objectBounds = new Bounds(Vector3.zero, Vector3.one);
            bool cursorInDataCube = objectBounds.Contains(objectSpacePosition);

            // [DIP] Config.Instance accessed again — third occurrence
            if ((cursorInDataCube || Config.Instance.displayCursorInfoOutsideCube) && _dataSet != null) // [CBO]
            {
                Vector3 positionCubeSpace = new Vector3(
                    (objectSpacePosition.x + 0.5f) * _dataSet.XDim,
                    (objectSpacePosition.y + 0.5f) * _dataSet.YDim,
                    (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
                Vector3 voxelCornerCubeSpace = new Vector3(
                    Mathf.Floor(positionCubeSpace.x),
                    Mathf.Floor(positionCubeSpace.y),
                    Mathf.Floor(positionCubeSpace.z));
                Vector3 voxelCenterCubeSpace = voxelCornerCubeSpace + 0.5f * Vector3.one;
                Vector3Int newVoxelCursor = new Vector3Int(
                    Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1,
                    Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1,
                    Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1);

                if (!newVoxelCursor.Equals(CursorVoxel) || brushSize != _previousBrushSize)
                {
                    _previousBrushSize = brushSize;
                    CursorVoxel = newVoxelCursor;

                    // [GRASP Information Expert] Data value lookup — belongs with data layer
                    CursorValue = (cursorInDataCube)
                        ? _dataSet.GetDataValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z)
                        : Single.NaN;

                    if (_maskDataSet != null && cursorInDataCube)
                    {
                        CursorSource = _maskDataSet.GetMaskValue(CursorVoxel.x, CursorVoxel.y, CursorVoxel.z);
                    }
                    else
                    {
                        CursorSource = 0;
                    }

                    Vector3 voxelCenterObjectSpace = new Vector3(
                        voxelCenterCubeSpace.x / _dataSet.XDim - 0.5f,
                        voxelCenterCubeSpace.y / _dataSet.YDim - 0.5f,
                        voxelCenterCubeSpace.z / _dataSet.ZDim - 0.5f);
                    if (_voxelOutline != null)
                    {
                        _voxelOutline.Center = voxelCenterObjectSpace;
                        _voxelOutline.Bounds = brushSize * new Vector3(
                            1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
                    }
                }
                _voxelOutline?.Activate();
            }
            else
            {
                _voxelOutline?.Deactivate();
                CursorValue = float.NaN;
                CursorVoxel = new Vector3Int(-1, -1, -1);
            }
        }

        // [→ VolumeCameraDriver]
        public void SetVideoCursorLocPosition(Vector3 vidCursorLocPos)
        {
            Vector3 positionCubeSpace = new Vector3(
                (0.5f + vidCursorLocPos.x) * _dataSet.XDim,
                (0.5f + vidCursorLocPos.y) * _dataSet.YDim,
                (0.5f + vidCursorLocPos.z) * _dataSet.ZDim);
            Vector3 voxelCornerCubeSpace = new Vector3(
                Mathf.Floor(positionCubeSpace.x),
                Mathf.Floor(positionCubeSpace.y),
                Mathf.Floor(positionCubeSpace.z));
            Vector3 voxelCenterCubeSpace = voxelCornerCubeSpace + 0.5f * Vector3.one;
            Vector3Int newVidCursorLocVoxel = new Vector3Int(
                Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1,
                Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1,
                Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1);
            if (!newVidCursorLocVoxel.Equals(_vidCursorLocVoxel))
            {
                _vidCursorLocVoxel = newVidCursorLocVoxel;
                Vector3 voxelCenterObjectSpace = new Vector3(
                    voxelCenterCubeSpace.x / _dataSet.XDim - 0.5f,
                    voxelCenterCubeSpace.y / _dataSet.YDim - 0.5f,
                    voxelCenterCubeSpace.z / _dataSet.ZDim - 0.5f);
                if (_videoCursorPositionOutline != null)
                {
                    _videoCursorPositionOutline.Center = voxelCenterObjectSpace;
                    _videoCursorPositionOutline.Bounds = 1 * new Vector3(_videoCursorLocSize, _videoCursorLocSize, _videoCursorLocSize);
                }
                _videoCursorPositionOutline?.Activate();
            }
            else
            {
                DeactivateVideoCursorLocPosition();
            }
        }

        public void DeactivateVideoCursorLocPosition()
        {
            _videoCursorPositionOutline?.Deactivate();
            _vidCursorLocVoxel = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }

        // [→ VolumeCameraDriver] — pure offset arithmetic
        public Vector3Int GetVoxelPositionDataSpace()
        {
            Vector3Int offset = new Vector3Int(_dataSet.subsetBounds[0], _dataSet.subsetBounds[2], _dataSet.subsetBounds[4]);
            return CursorVoxel + offset - new Vector3Int(1, 1, 1);
        }

        public Vector3Int GetVoxelPositionDataSpace(Vector3 worldSpacePos)
        {
            Vector3Int offset = new Vector3Int(_dataSet.subsetBounds[0], _dataSet.subsetBounds[2], _dataSet.subsetBounds[4]);
            return Vector3Int.FloorToInt(worldSpacePos + offset - new Vector3Int(1, 1, 1));
        }

        public Vector3Int GetVoxelPositionWorldSpace(Vector3 cursorPosWorldSpace)
        {
            // [DIP] transform.InverseTransformPoint — UnityEngine in domain logic
            Vector3 objectSpacePosition = transform.InverseTransformPoint(cursorPosWorldSpace);
            objectSpacePosition = new Vector3(
                Mathf.Clamp(objectSpacePosition.x, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.y, -0.5f, 0.5f),
                Mathf.Clamp(objectSpacePosition.z, -0.5f, 0.5f));
            if (_dataSet == null) return Vector3Int.zero;
            Vector3 positionCubeSpace = new Vector3(
                (objectSpacePosition.x + 0.5f) * _dataSet.XDim,
                (objectSpacePosition.y + 0.5f) * _dataSet.YDim,
                (objectSpacePosition.z + 0.5f) * _dataSet.ZDim);
            Vector3 voxelCornerCubeSpace = new Vector3(
                Mathf.Floor(positionCubeSpace.x),
                Mathf.Floor(positionCubeSpace.y),
                Mathf.Floor(positionCubeSpace.z));
            return new Vector3Int(
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.x) + 1, 1, (int)_dataSet.XDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.y) + 1, 1, (int)_dataSet.YDim),
                Mathf.Clamp(Mathf.RoundToInt(voxelCornerCubeSpace.z) + 1, 1, (int)_dataSet.ZDim));
        }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] Region selection — another responsibility cluster.
        // [→ VolumeCameraDriver for outline management; VolumeTextureManager for crop]
        // [GRASP Controller] CropToRegion (line 909) mixes validation, data loading,
        // material update, and outline update — four concerns in one method.
        // ──────────────────────────────────────────────────────────────────────
        public void SetRegionPosition(Vector3 cursor, bool start)
        {
            if (_dataSet != null)
            {
                var newVoxelCursor = GetVoxelPositionWorldSpace(cursor);
                var existingVoxel = start ? RegionStartVoxel : RegionEndVoxel;
                if (!newVoxelCursor.Equals(existingVoxel))
                {
                    if (start) RegionStartVoxel = newVoxelCursor;
                    else RegionEndVoxel = newVoxelCursor;
                    UpdateRegionBounds();
                }
                _regionOutline.Activate();
                if (ShowMeasuringLine) _measuringLine?.Activate();
            }
        }

        public void SetRegionBounds(Vector3Int min, Vector3Int max, bool drawRegion)
        {
            RegionStartVoxel = min;
            RegionEndVoxel = max;
            if (drawRegion) UpdateRegionBounds();
        }

        private void UpdateRegionBounds()
        {
            var regionMin = Vector3.Min(RegionStartVoxel, RegionEndVoxel);
            var regionMax = Vector3.Max(RegionStartVoxel, RegionEndVoxel);
            var measureStart = RegionStartVoxel;
            var measureEnd = RegionEndVoxel;
            if (measureStart.x < measureEnd.x) measureStart.x--; else measureEnd.x--;
            if (measureStart.y < measureEnd.y) measureStart.y--; else measureEnd.y--;
            if (measureStart.z < measureEnd.z) measureStart.z--; else measureEnd.z--;
            var regionSize = regionMax - regionMin + Vector3.one;
            Vector3 regionCenter = (regionMax + regionMin) / 2.0f - 0.5f * Vector3.one;
            Vector3 regionCenterObjectSpace = new Vector3(
                regionCenter.x / _dataSet.XDim - 0.5f,
                regionCenter.y / _dataSet.YDim - 0.5f,
                regionCenter.z / _dataSet.ZDim - 0.5f);
            _regionOutline.Center = regionCenterObjectSpace;
            _regionOutline.Bounds = new Vector3(
                regionSize.x / _dataSet.XDim,
                regionSize.y / _dataSet.YDim,
                regionSize.z / _dataSet.ZDim);
            var regionSizeBytes = regionSize.x * regionSize.y * regionSize.z * sizeof(float);
            bool regionIsFullResolution = (regionSizeBytes <= MaximumCubeSizeInMB * 1e6);
            Feature.SetCubeColors(_regionOutline, regionIsFullResolution ? Color.white : Color.yellow, regionIsFullResolution);
            var startPoint = new Vector3(
                (float)measureStart.x / _dataSet.XDim - 0.5f,
                (float)measureStart.y / _dataSet.YDim - 0.5f,
                (float)measureStart.z / _dataSet.ZDim - 0.5f);
            var endPoint = new Vector3(
                (float)measureEnd.x / _dataSet.XDim - 0.5f,
                (float)measureEnd.y / _dataSet.YDim - 0.5f,
                (float)measureEnd.z / _dataSet.ZDim - 0.5f);
            _measuringLine.Vertices = new List<Vector3> { startPoint, endPoint };
        }

        public void ClearRegion()  { _regionOutline?.Deactivate(); }
        public void ClearMeasure() { _measuringLine?.Deactivate(); }

        public void SelectFeature(Vector3 cursor)
        {
            if (_featureManager && _featureManager.SelectFeature(cursor))
            {
                Debug.Log($"Selected feature '{_featureManager.SelectedFeature.Name}'");
                SetRegionBounds(
                    Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMinBounds()),
                    Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMaxBounds()), false);
            }
        }

        public void SelectFeature(Feature feature)
        {
            if (_featureManager)
            {
                _featureManager.SelectedFeature = feature;
                Debug.Log($"Selected feature '{_featureManager.SelectedFeature.Name}'");
                SetRegionBounds(
                    Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMinBounds()),
                    Vector3Int.FloorToInt(_featureManager.SelectedFeature.GetMaxBounds()), false);
            }
        }

        public bool CropToFeature()
        {
            if (_featureManager != null && _featureManager.SelectedFeature != null)
            {
                CropToRegion(_featureManager.SelectedFeature.CornerMin, _featureManager.SelectedFeature.CornerMax);
                return true;
            }
            return false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // [GRASP Controller] CropToRegion — four concerns in one method:
        //   1. Input → startVoxel/endVoxel   (validation)
        //   2. LoadRegionData()              (data concern → VolumeTextureManager)
        //   3. _materialInstance.SetTexture  (shader concern → VolumeMaterialBinder)
        //   4. _momentMapRenderer update     (moment map concern → separate subsystem)
        //
        // In the after/ design, VolumeRenderCoordinator.CropToRegion() calls:
        //   textureManager.LoadRegion(start, end)
        //   materialBinder.BindDataTexture(textureManager.CurrentTexture)
        //   momentMap.UpdateSource(textureManager.CurrentTexture)
        // ──────────────────────────────────────────────────────────────────────
        public void CropToRegion(Vector3 cornerMin, Vector3 cornerMax)
        {
            Vector3Int startVoxel = new Vector3Int(
                Convert.ToInt32(cornerMin.x), Convert.ToInt32(cornerMin.y), Convert.ToInt32(cornerMin.z));
            Vector3Int endVoxel = new Vector3Int(
                Convert.ToInt32(cornerMax.x), Convert.ToInt32(cornerMax.y), Convert.ToInt32(cornerMax.z));
            Vector3 regionStartObjectSpace = new Vector3(
                cornerMin.x / _dataSet.XDim - 0.5f, cornerMin.y / _dataSet.YDim - 0.5f, cornerMin.z / _dataSet.ZDim - 0.5f);
            Vector3 regionEndObjectSpace = new Vector3(
                cornerMax.x / _dataSet.XDim - 0.5f, cornerMax.y / _dataSet.YDim - 0.5f, cornerMax.z / _dataSet.ZDim - 0.5f);
            Vector3 padding = new Vector3(
                1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
            SliceMin = Vector3.Min(regionStartObjectSpace, regionEndObjectSpace) - padding;
            SliceMax = Vector3.Max(regionStartObjectSpace, regionEndObjectSpace);
            LoadRegionData(startVoxel, endVoxel);
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.RegionCube);   // [→ VolumeMaterialBinder]

            if (_maskDataSet != null)
            {
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
                var regionMin = Vector3.Min(startVoxel, endVoxel);
                _maskMaterialInstance.SetVector(MaterialID.RegionOffset, new Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
                var regionDimensions = new Vector4(_maskDataSet.RegionCube.width, _maskDataSet.RegionCube.height, _maskDataSet.RegionCube.depth, 0);
                _maskMaterialInstance.SetVector(MaterialID.RegionDimensions, regionDimensions);
                var cubeDimensions = new Vector4(_maskDataSet.XDim, _maskDataSet.YDim, _maskDataSet.ZDim, 1);
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
            SliceMin = -0.5f * Vector3.one;
            SliceMax = +0.5f * Vector3.one;
            _materialInstance.SetTexture(MaterialID.DataCube, _dataSet.DataCube);
            if (_maskDataSet != null)
            {
                if (IsFullResolution)
                {
                    _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, Vector3Int.one,
                        new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim), Vector3Int.one);
                    _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.RegionCube);
                    var regionMin = Vector3.Min(Vector3Int.one, new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim));
                    _maskMaterialInstance.SetVector(MaterialID.RegionOffset, new Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
                    var regionDimensions = new Vector4(_maskDataSet.RegionCube.width, _maskDataSet.RegionCube.height, _maskDataSet.RegionCube.depth, 0);
                    _maskMaterialInstance.SetVector(MaterialID.RegionDimensions, regionDimensions);
                    var cubeDimensions = new Vector4(_maskDataSet.XDim, _maskDataSet.YDim, _maskDataSet.ZDim, 1);
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

        // [→ VolumeTextureManager.LoadRegion()]
        public void LoadRegionData(Vector3Int startVoxel, Vector3Int endVoxel)
        {
            Vector3Int deltaRegion = startVoxel - endVoxel;
            Vector3Int regionSize = new Vector3Int(
                Math.Abs(deltaRegion.x) + 1,
                Math.Abs(deltaRegion.y) + 1,
                Math.Abs(deltaRegion.z) + 1);
            _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, regionSize.x, regionSize.y, regionSize.z,
                out _currentXFactor, out _currentYFactor, out _currentZFactor);
            _dataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel,
                new Vector3Int(_currentXFactor, _currentYFactor, _currentZFactor));
            if (_maskDataSet != null)
                _maskDataSet.GenerateCroppedVolumeTexture(TextureFilter, startVoxel, endVoxel,
                    new Vector3Int(_currentXFactor, _currentYFactor, _currentZFactor));

            string resolutionString = (_currentXFactor * _currentYFactor * _currentZFactor == 1)
                ? "Data is full resolution"
                : $"Downsampled by ({_currentXFactor} × {_currentYFactor} × {_currentZFactor})";
            string cropString = (regionSize.x == _dataSet.XDim && regionSize.y == _dataSet.YDim && regionSize.z == _dataSet.ZDim)
                ? "Showing entire cube"
                : $"Cropped to ({regionSize.x} × {regionSize.y} × {regionSize.z}) region";
            ToastNotification.ShowInfo($"{cropString}\n{resolutionString}"); // [CBO] UI concern in data method
        }

        public void TeleportToRegion()
        {
            if (volumeInputController && _featureManager && _featureManager.SelectedFeature != null)
            {
                var boundsMin = _featureManager.SelectedFeature.CornerMin;
                var boundsMax = _featureManager.SelectedFeature.CornerMax;
                volumeInputController.Teleport(boundsMin - (0.5f * Vector3.one), boundsMax + (0.5f * Vector3.one));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // [WMC] LARGEST METHOD — Update() is called every frame.
        //
        // This single method does the work of three classes:
        //   Lines 1026–1041  → VolumeMaterialBinder.BindSceneProperties()
        //   Lines 1042–1053  → FoveatedSamplingPolicy.ApplyToMaterial()
        //   Lines 1055–1071  → VolumeMaterialBinder.BindHighlightProperties()
        //   Lines 1073–1094  → VolumeMaterialBinder.BindMaskProperties()
        //   Lines 1097–1103  → VolumeMaterialBinder.BindProjectionKeyword()  [OCP violation O2]
        //   Lines 1106–1109  → VolumeMaterialBinder.BindVignetteProperties()
        //   Lines 1111–1117  → WCSService.UpdateIfChanged()
        //
        // [WMC] Cyclomatic complexity of Update(): ≥8 (6 if/else branches + 2
        // null-checks). Contributes ~8 to the class WMC of ~74.
        //
        // [LCOM] The foveation block (lines 1042–1053) uses FoveationStart,
        // FoveationEnd, FoveatedRendering, FoveatedStepsLow, FoveatedStepsHigh.
        // The WCS block (lines 1111–1117) uses _restFrequencyGHzChanged, _dataSet.
        // These two clusters share ZERO instance fields — direct LCOM evidence.
        // ══════════════════════════════════════════════════════════════════════
        public void Update()
        {
            if (started)
            {
                // ──────────────────────────────────────────────────────────────
                // [SRP] R3 — Shader uniform binding (25+ SetFloat/SetInt calls)
                // [→ VolumeMaterialBinder.BindPerFrameProperties()]
                // All 25+ uniform pushes belong in VolumeMaterialBinder. The
                // coordinator calls binder.Tick() each frame; the binder reads
                // from a read-only VolumeRenderState value-type and calls SetFloat.
                // Callers never touch MaterialID or Material directly.
                // ──────────────────────────────────────────────────────────────
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

                // ──────────────────────────────────────────────────────────────
                // [SRP] R4 — Foveated rendering decision
                // [→ FoveatedSamplingPolicy.ComputeStepCounts(gazeAngle)]
                //
                // FoveatedSamplingPolicy is a Strategy: given the current gaze
                // angle (from IGazeProvider), it returns (stepsLow, stepsHigh).
                // The coordinator passes these to VolumeMaterialBinder.
                // This keeps the step-count algorithm testable without GPU.
                //
                // [OCP] Hardcoded binary toggle — no abstraction for future
                // eye-tracking hardware. IGazeProvider interface (provided by
                // Sub-team 4) isolates the hardware dependency.
                // ──────────────────────────────────────────────────────────────
                if (FoveatedRendering)
                {
                    _materialInstance.SetFloat(MaterialID.FoveationJitter, FoveationJitter);
                    _materialInstance.SetInt(MaterialID.FoveatedStepsLow, FoveatedStepsLow);
                    _materialInstance.SetInt(MaterialID.FoveatedStepsHigh, FoveatedStepsHigh);
                }
                else
                {
                    // When disabled, both step counts collapse to MaxSteps
                    _materialInstance.SetInt(MaterialID.FoveatedStepsLow, MaxSteps);
                    _materialInstance.SetInt(MaterialID.FoveatedStepsHigh, MaxSteps);
                }

                if (_regionOutline.Active)
                {
                    Vector3 regionStartObjectSpace = new Vector3(
                        (float)(RegionStartVoxel.x) / _dataSet.XDim - 0.5f,
                        (float)(RegionStartVoxel.y) / _dataSet.YDim - 0.5f,
                        (float)(RegionStartVoxel.z) / _dataSet.ZDim - 0.5f);
                    Vector3 regionEndObjectSpace = new Vector3(
                        (float)(RegionEndVoxel.x) / _dataSet.XDim - 0.5f,
                        (float)(RegionEndVoxel.y) / _dataSet.YDim - 0.5f,
                        (float)(RegionEndVoxel.z) / _dataSet.ZDim - 0.5f);
                    Vector3 padding = new Vector3(
                        1.0f / _dataSet.XDim, 1.0f / _dataSet.YDim, 1.0f / _dataSet.ZDim);
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
                    _maskMaterialInstance.SetInt(MaterialID.HighlightedSource, HighlightedSource);

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

                // ──────────────────────────────────────────────────────────────
                // [OCP] Violation O2 — ProjectionMode binary toggle.
                // Adding a third mode (MinimumIntensityProjection) requires
                // editing this if/else. [→ IProjectionMode Strategy, same pattern
                // as IMaskMode, each implementation applies its own keyword]
                // ──────────────────────────────────────────────────────────────
                if (ProjectionMode == ProjectionMode.AverageIntensityProjection)
                    Shader.EnableKeyword("SHADER_AIP");
                else
                    Shader.DisableKeyword("SHADER_AIP");

                _materialInstance.SetFloat(MaterialID.VignetteFadeStart, VignetteFadeStart);
                _materialInstance.SetFloat(MaterialID.VignetteFadeEnd, VignetteFadeEnd);
                _materialInstance.SetFloat(MaterialID.VignetteIntensity, VignetteIntensity);
                _materialInstance.SetColor(MaterialID.VignetteColor, VignetteColor);

                // ──────────────────────────────────────────────────────────────
                // [SRP] R8 — WCS update inside a per-frame rendering method.
                // [→ WCSService.UpdateIfRestFrequencyChanged()]
                // ──────────────────────────────────────────────────────────────
                if (_restFrequencyGHzChanged && HasWCS)
                {
                    _dataSet.RecreateFrameSet(RestFrequencyGHz);
                    _dataSet.CreateAltSpecFrame();
                    _dataSet.HasRestFrequency = true;
                    _restFrequencyGHzChanged = false;
                }
            }
        }

        // [→ WCSService]
        public void ResetRestFrequency()
        {
            if (_dataSet.HasFitsRestFrequency)
            {
                RestFrequencyGHz = _dataSet.FitsRestFrequency;
            }
            else
            {
                _restFrequencyGHz = 0.0;
                _dataSet.HasRestFrequency = false;
                RestFrequencyGHzChanged?.Invoke();
            }
        }

        public void ResetThresholds()
        {
            ThresholdMin = InitialThresholdMin;
            ThresholdMax = InitialThresholdMax;
        }

        // ──────────────────────────────────────────────────────────────────────
        // [GRASP Protected Variations] OnRenderObject uses Graphics.DrawProceduralNow
        // which is a legacy built-in render pipeline API — non-existent in URP/HDRP.
        // There is zero abstraction protecting against the Unity 6 migration.
        //
        // [→ IRenderPipeline.SubmitMaskGeometry(buffer, count)] hides the
        // DrawProceduralNow / DrawProcedural distinction behind an interface.
        // The coordinator calls the interface; the URP adapter calls the correct
        // URP equivalent; the built-in adapter calls DrawProceduralNow.
        //
        // [DIP] Violation D4 — domain-level decision (should we draw the mask?)
        // mixed with render-pipeline-specific API call.
        // ──────────────────────────────────────────────────────────────────────
        void OnRenderObject()
        {
            if (IsFullResolution && DisplayMask && _maskDataSet?.ExistingMaskBuffer != null)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.ExistingMaskBuffer);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.ExistingMaskBuffer.count); // Legacy API
            }
            if (IsFullResolution && DisplayMask && _maskDataSet?.AddedMaskBuffer != null && _maskDataSet?.AddedMaskEntryCount > 0)
            {
                _maskMaterialInstance.SetBuffer(MaterialID.MaskEntries, _maskDataSet.AddedMaskBuffer);
                _maskMaterialInstance.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Points, _maskDataSet.AddedMaskEntryCount); // Legacy API
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // [SRP] R6 — Mask painting cluster
        // [→ MaskEditor (out of Example 1 scope but still annotated)]
        // Methods: InitialiseMask, PaintMask, PaintCursor, FinishBrushStroke
        // Shared fields: _maskDataSet, _maskMaterialInstance, _previousPaintLocation
        // These four methods share zero fields with the shader cluster or the
        // WCS cluster — LCOM evidence.
        // ══════════════════════════════════════════════════════════════════════
        public void InitialiseMask()
        {
            if (_dataSet != null && _maskDataSet == null)
            {
                _maskDataSet = _dataSet.GenerateEmptyMask();
                IsMaskNew = true;
                var maskFeatureSet = _featureManager.CreateMaskFeatureSet();
                _maskDataSet?.FillFeatureSet(maskFeatureSet);
                if (!FactorOverride)
                    _dataSet.FindDownsampleFactors(MaximumCubeSizeInMB, out XFactor, out YFactor, out ZFactor);
                _maskDataSet.GenerateVolumeTexture(TextureFilter, XFactor, YFactor, ZFactor);
                _materialInstance.SetTexture(MaterialID.MaskCube, _maskDataSet.DataCube);
                if (!CropToFeature() && IsFullResolution)
                    CropToRegion(Vector3.one, new Vector3(_dataSet.XDim, _dataSet.YDim, _dataSet.ZDim));
            }
        }

        private bool PaintMask(Vector3Int position, short value)
        {
            if (_maskDataSet == null || _maskDataSet.RegionCube == null) return false;
            var regionSizeObjectSpace = SliceMax - SliceMin;
            var regionSizeDataSpace = new Vector3(
                _maskDataSet.XDim * regionSizeObjectSpace.x,
                _maskDataSet.YDim * regionSizeObjectSpace.y,
                _maskDataSet.ZDim * regionSizeObjectSpace.z);
            if (Math.Floor(regionSizeDataSpace.x) > _maskDataSet.RegionCube.width ||
                Math.Floor(regionSizeDataSpace.y) > _maskDataSet.RegionCube.height ||
                Math.Floor(regionSizeDataSpace.z) > _maskDataSet.RegionCube.depth)
                return false;
            Vector3Int offsetRegionSpace = Vector3Int.FloorToInt(new Vector3(
                (0.5f + SliceMin.x) * _maskDataSet.XDim,
                (0.5f + SliceMin.y) * _maskDataSet.YDim,
                (0.5f + SliceMin.z) * _maskDataSet.ZDim));
            Vector3Int coordsRegionSpace = position - Vector3Int.one - offsetRegionSpace;
            if (coordsRegionSpace != _previousPaintLocation || value != _previousPaintValue)
            {
                _previousPaintLocation = coordsRegionSpace;
                _previousPaintValue = value;
                return _maskDataSet.PaintMaskVoxel(coordsRegionSpace, value);
            }
            return true;
        }

        public bool PaintCursor(short value)
        {
            var maskCursorLimit = (_previousBrushSize - 1) / 2;
            Debug.Log("Painting at cursor value [" + CursorVoxel.x + ", " + CursorVoxel.y + ", " + CursorVoxel.z + "].");
            for (int i = -maskCursorLimit; i <= maskCursorLimit; i++)
                for (int j = -maskCursorLimit; j <= maskCursorLimit; j++)
                    for (int k = -maskCursorLimit; k <= maskCursorLimit; k++)
                        PaintMask(new Vector3Int(CursorVoxel.x + i, CursorVoxel.y + j, CursorVoxel.z + k), value);
            return true;
        }

        public void FinishBrushStroke()
        {
            _maskDataSet?.FlushBrushStroke();
            _momentMapRenderer.CalculateMomentMaps();
        }

        public Vector3Int GetCubeDimensions()
        {
            return new Vector3Int((int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
        }

        public Vector3 VolumePositionToLocalPosition(Vector3 volumePosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            return new Vector3(
                volumePosition.x / cubeDimensions.x - 0.5f,
                volumePosition.y / cubeDimensions.y - 0.5f,
                volumePosition.z / cubeDimensions.z - 0.5f);
        }

        public Vector3 LocalPositionToVolumePosition(Vector3 localPosition)
        {
            Vector3Int cubeDimensions = GetCubeDimensions();
            return new Vector3(
                (localPosition.x + 0.5f) * cubeDimensions.x,
                (localPosition.y + 0.5f) * cubeDimensions.y,
                (localPosition.z + 0.5f) * cubeDimensions.z);
        }

        public void SaveSubCube()
        {
            Vector3Int cornerMin, cornerMax, cornerMinWorld, cornerMaxWorld, featureSize;
            if (_featureManager.SelectedFeature != null)
            {
                cornerMinWorld = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMin);
                cornerMaxWorld = Vector3Int.FloorToInt(_featureManager.SelectedFeature.CornerMax);
                cornerMin = GetVoxelPositionDataSpace(cornerMinWorld);
                cornerMax = GetVoxelPositionDataSpace(cornerMaxWorld);
            }
            else
            {
                ToastNotification.ShowWarning("No feature selected, saving entire loaded cube as subcube.");
                cornerMin = cornerMinWorld = new Vector3Int(1, 1, 1);
                cornerMax = cornerMaxWorld = GetCubeDimensions();
            }
            featureSize = cornerMax - cornerMin + Vector3Int.one;
            long elements = (long)featureSize.x * (long)featureSize.y * (long)featureSize.z;
            if (elements > int.MaxValue)
            {
                long oversize = Mathf.RoundToInt((elements / int.MaxValue) * 100);
                ToastNotification.ShowError($"Selected subcube ({featureSize.x} x {featureSize.y} x {featureSize.z}) {oversize}% of maximum size supported by CFITSIO.");
                return;
            }
            Debug.Log("Saving subcube from " + cornerMin.ToString() + " to " + cornerMax.ToString() + ".");
            _dataSet.SaveSubCubeFromOriginal(cornerMin, cornerMax, cornerMinWorld, cornerMaxWorld, _maskDataSet);
        }

        // ══════════════════════════════════════════════════════════════════════
        // [SRP] Violation S2 — SaveMask() does THREE jobs in 90 lines:
        //   1. File path string manipulation + overwrite logic (lines 1303–1311)
        //   2. Native plugin IntPtr calls — FitsReader.FitsOpenFileReadOnly (line 1309)
        //   3. UI toast notifications — ToastNotification.ShowError (line 1315)
        //
        // A renderer class should not know about the file system, the native
        // FITS ABI, or the notification system.
        //
        // [CBO] This method alone adds 3 CBO edges: FitsReader, ToastNotification,
        // and Regex — all concrete dependencies, none abstracted.
        //
        // [→ MaskPersistenceService.Save(bool overwrite)]
        //   - Owns native call and path logic
        //   - Fires SaveCompleted/SaveFailed events
        //   - UI notification system subscribes to those events
        //   - Renderer never touches FitsReader directly
        // ══════════════════════════════════════════════════════════════════════
        public void SaveMask(bool overwrite)
        {
            if (_maskDataSet == null)
            {
                ToastNotification.ShowError("Could not find mask data!"); // [CBO] UI in renderer
                return;
            }
            IntPtr cubeFitsPtr = IntPtr.Zero;
            int status = 0;
            if (IsMaskNew)
            {
                Debug.Log("Attempting to save new mask because none exist.");
                string datasetFileName = _dataSet.FileName;
                if (_dataSet.SelectedHdu != 1)
                    datasetFileName += $"[{_dataSet.SelectedHdu}]"; // File path logic in renderer
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, datasetFileName, out status); // [CBO] Native plugin
                string directory = Path.GetDirectoryName(_dataSet.FileName);
                _maskDataSet.FileName = $"!{directory}/{Path.GetFileNameWithoutExtension(_dataSet.FileName)}-mask.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, _maskDataSet.FileName, false) != 0)
                    ToastNotification.ShowError("Error saving new mask!");
                else
                    ToastNotification.ShowSuccess($"New mask saved to {Path.GetFileName(_maskDataSet.FileName)}");
                IsMaskNew = false;
                this.lastSavedMaskPath = _maskDataSet.FileName;
            }
            else if (!overwrite)
            {
                Debug.Log("Attempting to save new mask copy.");
                FitsReader.FitsOpenFileReadOnly(out cubeFitsPtr, _maskDataSet.FileName, out status); // [CBO]
                Regex regex = new Regex(@"_copy_\d{8}_\d{5}"); // [CBO] Regex — file naming logic in renderer
                string fileName = Path.GetFileNameWithoutExtension(_maskDataSet.FileName);
                Match match = regex.Match(fileName);
                var timeStamp = DateTime.Now.ToString("yyyyMMdd_Hmmss");
                if (match.Success)
                    fileName = fileName.Substring(0, fileName.Length - timeStamp.Length - 6) + "_copy_" + timeStamp;
                else
                    fileName = fileName + "_copy_" + timeStamp;
                string directory = Path.GetDirectoryName(_maskDataSet.FileName);
                string fullPath = $"!{directory}/{fileName}.fits";
                if (_maskDataSet.SaveMask(cubeFitsPtr, fullPath, true) != 0)
                    ToastNotification.ShowError("Error saving mask copy!");
                else
                {
                    this.lastSavedMaskPath = fullPath;
                    ToastNotification.ShowSuccess($"New mask saved to {fileName}");
                }
            }
            else
            {
                Debug.Log("Attempting to overwrite existing mask.");
                FitsReader.FitsOpenFileReadWrite(out cubeFitsPtr, _maskDataSet.FileName, out status); // [CBO]
                if (_maskDataSet.SaveMask(cubeFitsPtr, null, false) != 0)
                    ToastNotification.ShowError("Error overwriting existing mask!");
                else
                {
                    this.lastSavedMaskPath = _maskDataSet.FileName;
                    ToastNotification.ShowSuccess($"Mask saved to disk");
                }
                Debug.Log("Overwriting mask complete, VolumeDataSetRenderer::SaveMask().");
            }
            if (cubeFitsPtr != IntPtr.Zero)
            {
                Debug.Log("cubeFitsPtr != IntPtr.Zero, closing file.");
                FitsReader.FitsCloseFile(cubeFitsPtr, out status); // [CBO]
            }
        }

        public string GetMaskSavedFilePath() { return this.lastSavedMaskPath; }

        public void AddSelectionToList() { _featureManager.AddSelectedFeatureToNewSet(); }

        // ──────────────────────────────────────────────────────────────────────
        // [SRP] R9 — Unity lifecycle teardown
        // [→ VolumeRenderCoordinator.OnDestroy() calls Dispose() on each service]
        // ──────────────────────────────────────────────────────────────────────
        public void OnDestroy()
        {
            _dataSet.CleanUp(RandomVolume);
            _maskDataSet?.CleanUp(false);
            _measuringLine?.Destroy();
            _cubeOutline?.Destroy();
            _regionOutline?.Destroy();
            _voxelOutline?.Destroy();
            _videoCursorPositionOutline?.Destroy();
        }
    }

} // namespace VolumeData

// ══════════════════════════════════════════════════════════════════════════════
// ANNOTATION SUMMARY
//
// Violation count by principle:
//   SRP  : 9 responsibility clusters (S1, S2, S3)
//   OCP  : 2 violations (O1 MaskMode enum, O2 ProjectionMode toggle)
//   ISP  : 1 violation  (152 public members, zero interfaces)
//   DIP  : 4 violations (D1 Config singleton, D2 FindObjectOfType x2,
//                        D3 AddComponent, D4 UnityEngine types in domain)
//   GRASP: 7 violations (Information Expert, Creator, Controller,
//                        Indirection, Protected Variations, Low Coupling,
//                        High Cohesion)
//
// CK metrics requiring correction:
//   WMC  = ~74   → After: 5 classes × avg 12 = ~60 total (≤20 each)
//   CBO  = ~31   → After: each class ≤8
//   RFC  = ~89   → After: each class ≤30
//   LCOM = ~0.81 → After: each class ≤0.2
//
// See after/ directory for the four extracted classes and the projected
// Day 13 CK metrics with rationale.
// ══════════════════════════════════════════════════════════════════════════════
