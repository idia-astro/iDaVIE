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
// BEFORE — God-class skeleton (design-level worked example)
//
// This file is a skeleton of the ORIGINAL VolumeDataSetRenderer.cs, included
// here to document the god-class violations.  It is NOT a replacement for the
// upstream source; the upstream source is unchanged.
//
// Violations annotated inline:
//   [GOD]  – responsibility belongs to a dedicated service
//   [CBO]  – coupling that raises CBO above threshold (≤14 domain)
//   [WMC]  – method that inflates Weighted Methods per Class
//   [LCOM] – method with no shared state with other responsibilities
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using DataFeatures;
using LineRenderer;
using UnityEngine;

namespace VolumeData
{
    // CK BEFORE snapshot (estimated from source):
    //   WMC  ≈ 138  (45 methods, avg cyclomatic complexity ≈ 3.1)  — FAIL (limit 20 domain)
    //   CBO  ≈ 17   (Material×2, FeatureSetManager, VolumeInputController,
    //                MomentMapRenderer, PolyLine, CuboidLine×4, Config,
    //                VolumeDataSet×2, FitsReader, ToastNotification, Regex, …)
    //                — FAIL (limit 14 domain)
    //   RFC  ≈ 72                                                   — FAIL (limit 50)
    //   LCOM ≈ 0.82 (Henderson-Sellers, many disjoint method clusters) — FAIL (limit 0.5)
    //   DIT  = 1  (MonoBehaviour)                                    — PASS

    public class VolumeDataSetRenderer : MonoBehaviour
    {
        // ── Rendering parameters (should be RenderingParameters value object) ──
        // [WMC][GOD] 20+ serialised fields covering 5 distinct concerns
        [Range(16, 512)] public int MaxSteps = 192;
        public ProjectionMode ProjectionMode = ProjectionMode.MaximumIntensityProjection;
        public FilterMode TextureFilter = FilterMode.Point;
        [Range(0, 1)] public float Jitter = 1.0f;
        [Range(0, 1)] public float ThresholdMin = 0;
        [Range(0, 1)] public float ThresholdMax = 1;
        public ColorMapEnum ColorMap = ColorMapEnum.Inferno;
        public ScalingType ScalingType = ScalingType.Linear;
        [Range(-1, 1)] public float ScalingBias = 0.0f;
        [Range(0, 5)] public float ScalingContrast = 1.0f;
        public float ScalingAlpha = 1000.0f;
        [Range(0, 5)] public float ScalingGamma = 1.0f;

        // ── Mask parameters (should be MaskParameters value object) ──
        // [WMC][GOD]
        public bool DisplayMask = false;
        public MaskMode MaskMode = MaskMode.Disabled;
        [Range(0, 1)] public float MaskVoxelSize = 1.0f;
        public Color MaskVoxelColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        // ── Foveation parameters (should be FoveationParameters value object) ──
        // [WMC][GOD]
        public bool FoveatedRendering = false;
        [Range(0, 0.5f)] public float FoveationStart = 0.15f;
        [Range(0, 0.5f)] public float FoveationEnd = 0.40f;
        [Range(0, 0.5f)] public float FoveationJitter = 0.0f;
        [Range(16, 512)] public int FoveatedStepsLow = 64;
        [Range(16, 512)] public int FoveatedStepsHigh = 384;

        // ── Vignette parameters (should be VignetteParameters value object) ──
        // [WMC][GOD]
        [Range(0, 0.5f)] public float VignetteFadeStart = 0.15f;
        [Range(0, 0.5f)] public float VignetteFadeEnd = 0.40f;
        [Range(0, 1)] public float VignetteIntensity = 0.0f;
        public Color VignetteColor = Color.black;

        // ── File / data state (belongs closer to data loading / orchestration) ──
        public string FileName;
        public string MaskFileName;
        public int[] subsetBounds = { -1, -1, -1, -1, -1, -1 };
        public int SelectedHdu = 1;

        // ── Materials (rendering concern only) ──
        // [CBO] direct coupling to Unity Material
        public Material RayMarchingMaterial;
        public Material MaskMaterial;
        private Material _materialInstance;
        private Material _maskMaterialInstance;

        // ── Rest-frequency state (should be RestFrequencyService) ──
        // [GOD][LCOM] completely separate concern: frequency lookup + event firing
        private double _restFrequencyGHz;
        private int _restFrequencyGHzListListIndex;
        public event Action RestFrequencyGHzChanged;
        public event Action RestFrequencyGHzListIndexChanged;
        public Dictionary<string, double> RestFrequencyGHzList { get; private set; }
        public bool OverrideRestFrequency { get; set; } = false;

        // ── Coordinate mapping state (should be VolumeCoordinateMapper) ──
        // [GOD][LCOM] coordinate maths has nothing to do with rendering
        public int CubeDepthAxis = 2;

        // ── Region / crop state (should be RegionControllerService) ──
        // [GOD][LCOM]
        public Vector3Int RegionStartVoxel { get; private set; }
        public Vector3Int RegionEndVoxel { get; private set; }
        public bool IsCropped { get; private set; }
        public Vector3Int CurrentCropMin { get; private set; }
        public Vector3Int CurrentCropMax { get; private set; }
        private CuboidLine _regionOutline;
        private PolyLine _measuringLine;

        // ── Cursor / paint state (should be MaskControllerService) ──
        // [GOD][LCOM]
        public Vector3Int CursorVoxel { get; private set; }
        public float CursorValue { get; private set; }
        public Int16 CursorSource { get; private set; }
        private Vector3Int _previousPaintLocation;
        private short _previousPaintValue;
        private int _previousBrushSize = 1;

        // ── Scene references (Unity coupling) ──
        // [CBO] direct coupling to 6+ other MonoBehaviours
        private MeshRenderer _renderer;
        private MomentMapRenderer _momentMapRenderer;
        private FeatureSetManager _featureManager;
        public VolumeInputController volumeInputController;
        private CuboidLine _cubeOutline, _voxelOutline, _videoCursorPositionOutline;

        // ── Data ──
        private VolumeDataSet _dataSet;
        private VolumeDataSet _maskDataSet;
        private string lastSavedMaskPath = "";

        // ==========================================================================
        // Unity lifecycle — responsible for wiring everything (violates SRP)
        // ==========================================================================

        public void Start() { started = false; }

        // [WMC=12] _startFunc alone has cyclomatic complexity ≈ 12
        // [GOD] does: config read, FITS load, texture gen, material setup,
        //             outline init, WCS check, frequency init — 7 distinct concerns
        public IEnumerator _startFunc() { /* ~120 lines in original */ yield return 0; }

        // [WMC=6][GOD] pushes material uniforms for every concern every frame
        public void Update() { /* ~90 lines in original */ }

        public void OnDestroy() { /* cleanup */ }

        // ==========================================================================
        // Rendering concern
        // ==========================================================================

        // [GOD] should live in VolumeRenderingService
        private void GenerateDownsampledCube() { }
        public void RegenerateCubes() { }
        public void ShiftColorMap(int delta) { }

        // ==========================================================================
        // Coordinate mapping concern
        // ==========================================================================

        // [GOD][LCOM] these four are a cohesive group with no shared state with mask painting
        public Vector3 ConvertWorldPositionToDataCubePosition(Vector3 worldLoc) => default;
        public Quaternion ConvertWorldRotationToDatacubeRotation(Quaternion worldRot) => default;
        public Vector3Int GetVoxelPositionWorldSpace(Vector3 cursorPosWorldSpace) => default;
        public Vector3Int GetVoxelPositionDataSpace() => default;
        public Vector3 VolumePositionToLocalPosition(Vector3 volumePosition) => default;
        public Vector3 LocalPositionToVolumePosition(Vector3 localPosition) => default;

        // ==========================================================================
        // Region / crop concern
        // ==========================================================================

        // [GOD][LCOM] region concerns are unrelated to mask painting
        public void SetCursorPosition(Vector3 cursor, int brushSize) { }
        public void SetVideoCursorLocPosition(Vector3 vidCursorLocPos) { }
        public void DeactivateVideoCursorLocPosition() { }
        public void SetRegionPosition(Vector3 cursor, bool start) { }
        public void SetRegionBounds(Vector3Int min, Vector3Int max, bool drawRegion) { }
        private void UpdateRegionBounds() { }
        public void ClearRegion() { }
        public void ClearMeasure() { }
        public void TeleportToRegion() { }
        public bool CropToFeature() => false;
        public bool CropToFeature(Feature f) => false;
        public void CropToRegion(Vector3 cornerMin, Vector3 cornerMax) { }
        public void ResetCrop() { }
        public void LoadRegionData(Vector3Int startVoxel, Vector3Int endVoxel) { }

        // ==========================================================================
        // Mask painting concern
        // ==========================================================================

        // [GOD][LCOM] mask painting is entirely unrelated to rendering parameter push
        public void InitialiseMask() { }
        private bool PaintMask(Vector3Int position, short value) => false;
        public bool PaintCursor(short value) => false;
        public void FinishBrushStroke() { }

        // ==========================================================================
        // Rest-frequency concern
        // ==========================================================================

        // [GOD][LCOM] frequency look-up has nothing to do with shader material binding
        private void PopulateRestFrequenyList() { }
        public void ResetRestFrequency() { }
        public double RestFrequencyGHz { get; set; }
        public int RestFrequencyGHzListIndex { get; set; }

        // ==========================================================================
        // Export concern
        // ==========================================================================

        // [GOD][LCOM] file I/O is entirely separate from rendering
        public void SaveSubCube() { }
        public void SaveMask(bool overwrite) { }
        public string GetMaskSavedFilePath() => lastSavedMaskPath;
        public void AddSelectionToList() { }

        // ==========================================================================
        // Feature selection concern
        // ==========================================================================

        // [GOD][LCOM]
        public void SelectFeature(Vector3 cursor) { }
        public void SelectFeature(Feature feature) { }

        // ==========================================================================
        // Misc / passthrough
        // ==========================================================================
        public bool started = false;
        public VolumeDataSet GetDataSet() => _dataSet;
        public MomentMapRenderer GetMomentMapRenderer() => _momentMapRenderer;
        public Vector3Int GetCubeDimensions() => default;
        public IEnumerator updateStatus(string label, int progress) { yield break; }
        public void ResetThresholds() { }
        void OnRenderObject() { }

        // Material property ID cache (only needed by rendering concern)
        private struct MaterialID
        {
            public static readonly int DataCube = Shader.PropertyToID("_DataCube");
            public static readonly int MaskCube = Shader.PropertyToID("MaskCube");
            // ... 28 more IDs, all owned by the rendering concern
        }
    }
}
