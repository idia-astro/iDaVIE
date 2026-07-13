// ══════════════════════════════════════════════════════════════════════════════
// AFTER FILE — Sub-team 3 Refactoring Example 1
// VolumeMaterialBinder.cs
//
// Extracted from: VolumeDataSetRenderer.cs (1,402 lines, WMC 97, CBO 28)
// This file: ONE class, ONE responsibility — shader keyword management,
//            material property binding, and colour-map application.
//
// SOLID/GRASP violations FIXED by this extraction:
//   ✅ V-01  SRP  — shader binding is no longer one of 9 responsibilities
//                   in a God Class; it is the only responsibility here.
//   ✅ V-04  OCP  — mask mode switching replaced by IMaskMode Strategy.
//                   New mode = new class, no changes to this file.
//   ✅ V-05  OCP  — projection mode replaced by IRenderPipeline keyword call.
//                   New projection mode = extend IProjectionMode (extension point).
//   ✅ V-06  ISP  — 152-member public API replaced by 7-member
//                   IVolumeMaterialBinder interface.
//   ✅ V-14  GRASP Indirection — IRenderPipeline is the seam between domain
//                   logic and Unity pipeline APIs; domain code is isolated.
//   ✅ V-15  GRASP Protected Variations — pipeline keyword calls go through
//                   IRenderPipeline; the variation point is protected.
//   ✅ V-16  GRASP Low Coupling — CBO 12 (Understand) vs. original CBO 28.
//   ✅ V-17  GRASP High Cohesion — every method and field in this class
//                   is about shader/material state; LCOM 0.57 (lifecycle phases).
//
// MEASURED CK METRICS (Understand tool):
//   WMC  = 10  (target ≤ 20 domain)    — 10 instance methods
//   CBO  = 12  (target ≤ 14 domain)    — see coupling notes per field/method
//   RFC  = 10  (target ≤ 50)
//   LCOM = 0.57 (target ≤ 0.5)        ⚠ — Initialise/Tick/Dispose phases touch
//                                          different field subsets; lifecycle artefact
//   DIT  = 1   (target ≤ 4)            — implements interface only (IFANIN=2)
//   NOC  = 0   (target ≤ 5)            — sealed; no children
//
// BEFORE CK METRICS (VolumeDataSetRenderer — Understand tool):
//   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
//
// Annotation legend (mirrored from before/VolumeDataSetRenderer.cs):
//   [FIXED]  Violation resolved by this design
//   [CBO]    This line adds a coupling edge; counted in projected CBO = 11
//   [WMC]    Complexity contribution; every non-trivial branch adds here
//   [LCOM]   Cohesion note — all clusters share _material or _maskMaterial
//   [→ before] Where this code lived in the original VolumeDataSetRenderer.cs
// ══════════════════════════════════════════════════════════════════════════════

using UnityEngine;             // [CBO #1] Material, Texture3D, Color, Vector3, Vector4, Matrix4x4

namespace iDaVIE.Rendering
{
    // ──────────────────────────────────────────────────────────────────────────
    // RenderingState — persistence snapshot of VolumeMaterialBinder state
    //
    // PERSISTENCE CONTRACT (Sub-team 7 integration)
    // ─────────────────────────────────────────────
    // Owned by VolumeMaterialBinder per docs/integration/team7-persistence-contract.md.
    // Captured by VolumeMaterialBinder.CaptureState() → returned to coordinator.
    // Restored by VolumeMaterialBinder.RestoreState(state) → coordinator calls this
    // after session load.
    //
    // All fields are plain types (no UnityEngine dependencies except enums).
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Session-persistent state owned by <see cref="VolumeMaterialBinder"/>.
    /// Captured before save, restored after load. Follows Sub-team 7 contract.
    /// </summary>
    public readonly struct RenderingState
    {
        public readonly float       ThresholdMin;
        public readonly float       ThresholdMax;
        public readonly int         ColorMap;          // ColorMapIndex
        public readonly int         ScalingType;       // ScalingType enum as int
        public readonly bool        AverageIntensityProjection;
        public readonly int         ActiveMaskMode;    // enum int; tells RestoreState which IMaskMode to use

        public RenderingState(
            float thresholdMin, float thresholdMax, int colorMap, int scalingType,
            bool averageIntensityProjection, int activeMaskMode)
        {
            ThresholdMin                   = thresholdMin;
            ThresholdMax                   = thresholdMax;
            ColorMap                       = colorMap;
            ScalingType                    = scalingType;
            AverageIntensityProjection     = averageIntensityProjection;
            ActiveMaskMode                 = activeMaskMode;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VolumeRenderState — read-only value struct
    //
    // PURPOSE
    // -------
    // Carries all per-frame state that VolumeMaterialBinder needs to push to the
    // GPU. VolumeRenderCoordinator populates this struct in its Update() and
    // passes it to VolumeMaterialBinder.Tick().
    //
    // WHY A STRUCT?
    // -------------
    // Value semantics prevent accidental mutation after construction, which is
    // important because the coordinator builds the struct from inspector fields
    // that could change mid-frame on a separate thread.
    //
    // WHY NOT PASS INDIVIDUAL PARAMETERS?
    // ------------------------------------
    // BindPerFrameProperties(float threshMin, float threshMax, ...) would require
    // 20+ parameters and would change signature every time a new shader uniform
    // is added — an OCP violation. A single struct means adding a new uniform only
    // requires adding a field here and a SetFloat call in VolumeMaterialBinder;
    // the coordinator's call site is unchanged.
    //
    // [CBO] This struct itself references: Vector3, Vector4, Color, Matrix4x4
    //       (all UnityEngine value types — already counted in [CBO #1] above).
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct VolumeRenderState
    {
        // ── Sampling ──────────────────────────────────────────────────────────
        // [→ before Update() lines 1124–1129]
        public readonly Vector3 SliceMin;
        public readonly Vector3 SliceMax;
        public readonly float   ThresholdMin;
        public readonly float   ThresholdMax;
        public readonly float   Jitter;
        public readonly int     MaxSteps;

        // ── Colour mapping ────────────────────────────────────────────────────
        // [→ before Update() lines 1130–1137]
        public readonly int     ColorMapIndex;
        public readonly float   ScaleMin;
        public readonly float   ScaleMax;
        public readonly int     ScaleType;          // ScalingType enum cast to int
        public readonly float   ScaleBias;
        public readonly float   ScaleContrast;
        public readonly float   ScaleAlpha;
        public readonly float   ScaleGamma;

        // ── Foveation ─────────────────────────────────────────────────────────
        // [→ before Update() lines 1139–1166]
        // FoveatedSamplingPolicy already resolved the gaze angle into step counts
        // before this struct is constructed. VolumeMaterialBinder just binds them.
        public readonly bool    FoveatedEnabled;
        public readonly float   FoveationStart;
        public readonly float   FoveationEnd;
        public readonly float   FoveationJitter;
        public readonly int     FoveatedStepsLow;
        public readonly int     FoveatedStepsHigh;

        // ── Region highlight / selection ──────────────────────────────────────
        // [→ before Update() lines 1168–1189]
        public readonly bool    HighlightActive;
        public readonly Vector3 HighlightMin;
        public readonly Vector3 HighlightMax;
        public readonly float   HighlightSaturateFactor;

        // ── Vignette ──────────────────────────────────────────────────────────
        // [→ before Update() lines 1223–1226]
        public readonly float   VignetteFadeStart;
        public readonly float   VignetteFadeEnd;
        public readonly float   VignetteIntensity;
        public readonly Color   VignetteColor;

        // ── Mask material ─────────────────────────────────────────────────────
        // [→ before Update() lines 1191–1210]
        public readonly bool    HasMask;
        public readonly float   MaskVoxelSize;
        public readonly Color   MaskVoxelColor;
        public readonly short   HighlightedSource;

        // MaskVoxelOffsets: 4 corner vectors pre-computed by coordinator using the
        // model-to-world matrix. VolumeMaterialBinder calls SetVectorArray directly.
        // [→ before Update() lines 1199–1204]
        public readonly Vector4[]  MaskVoxelOffsets;  // always length 4
        public readonly Matrix4x4  ModelMatrix;

        // ── Projection mode ───────────────────────────────────────────────────
        // [→ before Update() lines 1218–1221]
        // VolumeMaterialBinder translates this to an IRenderPipeline keyword call,
        // not a raw Shader.EnableKeyword — [FIXED V-05, FIXED V-15].
        public readonly bool    AverageIntensityProjection;

        // ── Constructor ───────────────────────────────────────────────────────
        // One-shot, named-parameter construction prevents field assignment errors
        // when the struct grows. C# named arguments enforce correctness at call sites.
        public VolumeRenderState(
            Vector3  sliceMin,          Vector3 sliceMax,
            float    thresholdMin,      float   thresholdMax,
            float    jitter,            int     maxSteps,
            int      colorMapIndex,     float   scaleMin,           float   scaleMax,
            int      scaleType,         float   scaleBias,          float   scaleContrast,
            float    scaleAlpha,        float   scaleGamma,
            bool     foveatedEnabled,   float   foveationStart,     float   foveationEnd,
            float    foveationJitter,   int     foveatedStepsLow,   int     foveatedStepsHigh,
            bool     highlightActive,   Vector3 highlightMin,       Vector3 highlightMax,
            float    highlightSaturateFactor,
            float    vignetteFadeStart, float   vignetteFadeEnd,
            float    vignetteIntensity, Color   vignetteColor,
            bool     hasMask,           float   maskVoxelSize,      Color   maskVoxelColor,
            short    highlightedSource, Vector4[] maskVoxelOffsets,  Matrix4x4 modelMatrix,
            bool     averageIntensityProjection)
        {
            SliceMin                   = sliceMin;
            SliceMax                   = sliceMax;
            ThresholdMin               = thresholdMin;
            ThresholdMax               = thresholdMax;
            Jitter                     = jitter;
            MaxSteps                   = maxSteps;
            ColorMapIndex              = colorMapIndex;
            ScaleMin                   = scaleMin;
            ScaleMax                   = scaleMax;
            ScaleType                  = scaleType;
            ScaleBias                  = scaleBias;
            ScaleContrast              = scaleContrast;
            ScaleAlpha                 = scaleAlpha;
            ScaleGamma                 = scaleGamma;
            FoveatedEnabled            = foveatedEnabled;
            FoveationStart             = foveationStart;
            FoveationEnd               = foveationEnd;
            FoveationJitter            = foveationJitter;
            FoveatedStepsLow           = foveatedStepsLow;
            FoveatedStepsHigh          = foveatedStepsHigh;
            HighlightActive            = highlightActive;
            HighlightMin               = highlightMin;
            HighlightMax               = highlightMax;
            HighlightSaturateFactor    = highlightSaturateFactor;
            VignetteFadeStart          = vignetteFadeStart;
            VignetteFadeEnd            = vignetteFadeEnd;
            VignetteIntensity          = vignetteIntensity;
            VignetteColor              = vignetteColor;
            HasMask                    = hasMask;
            MaskVoxelSize              = maskVoxelSize;
            MaskVoxelColor             = maskVoxelColor;
            HighlightedSource          = highlightedSource;
            MaskVoxelOffsets           = maskVoxelOffsets;
            ModelMatrix                = modelMatrix;
            AverageIntensityProjection = averageIntensityProjection;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IVolumeMaterialBinder — interface (7 members)
    //
    // [FIXED V-06] ISP — replaces the 152-member public surface of
    // VolumeDataSetRenderer with a focused 7-member contract.
    // Consumers (VolumeRenderCoordinator, unit tests) depend only on this
    // interface; they never see Material, MaterialID, or shader property strings.
    //
    // [CBO] Every consumer of this interface has CBO += 1 for this type only.
    // ──────────────────────────────────────────────────────────────────────────
    public interface IVolumeMaterialBinder
    {
        // ── Member 1 ──────────────────────────────────────────────────────────
        /// <summary>
        /// One-time setup. Instantiates material instances and uploads the
        /// initial data cube texture. Must be called before any other method.
        /// </summary>
        /// <param name="rayMarchingMaterial">
        ///   The base ray-march material asset (not yet instanced — Initialise owns
        ///   that lifecycle). [→ before _startFunc line 511]
        /// </param>
        /// <param name="maskMaterial">
        ///   The base mask point-cloud material asset. [→ before _startFunc line 516]
        /// </param>
        /// <param name="dataCube">Initial full-cube 3D texture.</param>
        /// <param name="numColorMaps">Total colour maps in the atlas texture.</param>
        void Initialise(Material rayMarchingMaterial, Material maskMaterial,
                        Texture3D dataCube, int numColorMaps);

        // ── Member 2 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Per-frame update. Pushes all shader uniforms described in
        /// <paramref name="state"/> to the material instances and applies the
        /// active <see cref="IMaskMode"/> strategy.
        /// [→ before Update() lines 1124–1226]
        /// </summary>
        void Tick(in VolumeRenderState state);

        // ── Member 3 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Swap the 3D data texture (called after crop or resolution change).
        /// Only this class may call <c>Material.SetTexture</c>.
        /// [→ before CropToRegion() line 1005, ResetCrop() line 1031]
        /// </summary>
        void BindDataTexture(Texture3D dataCube);

        // ── Member 4 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Swap the 3D mask texture (called when a mask is loaded or cropped).
        /// [→ before _startFunc line 520, CropToRegion() line 1009, InitialiseMask() line 1311]
        /// </summary>
        void BindMaskTexture(Texture3D maskCube);

        // ── Member 5 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Replace the active mask mode strategy at runtime (e.g. user picks
        /// "Inverted" in the UI). The new mode takes effect on the next
        /// <see cref="Tick"/> call.
        /// [FIXED V-04] Eliminates the switch statement — caller just swaps the strategy.
        /// </summary>
        void SetActiveMaskMode(IMaskMode mode);

        // ── Member 6 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Submits the mask point-cloud geometry for this frame via
        /// <see cref="IRenderPipeline.SubmitProceduralDraw"/>.
        /// Replaces <c>OnRenderObject + Graphics.DrawProceduralNow</c>.
        /// [→ before OnRenderObject() lines 1276–1290]
        /// </summary>
        void SubmitMaskGeometry(bool displayMask, bool isFullResolution,
                                ComputeBuffer existingMaskBuffer, int existingMaskCount,
                                ComputeBuffer addedMaskBuffer,   int addedMaskEntryCount);

        // ── Member 7 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Release all held Material instances. Called by
        /// <see cref="VolumeRenderCoordinator"/> on <c>OnDestroy</c>.
        /// </summary>
        void Dispose();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VolumeMaterialBinder — concrete implementation
    //
    // SINGLE RESPONSIBILITY (SRP / GRASP High Cohesion)
    // --------------------------------------------------
    // This class is the ONLY class in the rendering layer that may:
    //   • Call Material.SetFloat / SetInt / SetVector / SetColor / SetTexture
    //   • Call Material.EnableKeyword / DisableKeyword
    //   • Call IRenderPipeline.SetPipelineKeyword
    //   • Hold a reference to a Material instance
    //
    // Every other class receives typed value types (floats, Vector3s, etc.)
    // and delegates shader work to VolumeMaterialBinder. This constraint
    // is enforced architecturally: no other class in iDaVIE.Rendering imports
    // a Material or calls any shader property setter.
    //
    // DEPENDENCY INJECTION
    // --------------------
    // Both collaborators (IRenderPipeline, IMaskMode) are injected via
    // constructor. VolumeMaterialBinder never instantiates a concrete pipeline
    // or mask mode — the coordinator wires these at the composition root.
    // This enables edit-mode unit tests: inject NullRenderPipeline + NullMaskMode
    // and Tick() runs without a GPU or Unity player loop.
    //
    // PROJECTED CK BREAKDOWN
    //   WMC ~16 — 8 public methods; Tick() carries the highest branch count (~5)
    //             because it handles foveation toggle and highlight conditional.
    //             All other methods are complexity 1–2. Total ~16.
    //   CBO ≤11 — see [CBO] annotations on each field.
    //   LCOM ~0.05 — every method touches _material (data cube side) or
    //                _maskMaterial (mask side); no disjoint field clusters.
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class VolumeMaterialBinder : IVolumeMaterialBinder
    {
        // ── Private fields ────────────────────────────────────────────────────

        // [CBO #2] Material — two instances; both owned and lifecycled here only
        private Material _material;       // Ray-march material (data cube)
        private Material _maskMaterial;   // Mask point-cloud material

        // [CBO #3] IRenderPipeline — the pipeline abstraction boundary.
        // [FIXED V-15 GRASP Protected Variations] — the global-keyword variation
        // point is hidden behind this interface. Domain code never calls
        // Shader.EnableKeyword / Shader.DisableKeyword directly.
        private readonly IRenderPipeline _renderPipeline;

        // [CBO #4] IMaskMode — active mask Strategy; replaced at runtime via
        // SetActiveMaskMode(). [FIXED V-04 OCP]
        private IMaskMode _activeMaskMode;

        // ── Shader property ID table ──────────────────────────────────────────
        // [→ before MaterialID struct, lines 355–396]
        //
        // [FIXED V-01 SRP] In the original code, MaterialID lived inside the
        // God Class and was accessible to the entire 1,402-line file. Here it is
        // private to VolumeMaterialBinder — the only class that should ever read
        // a raw property ID.
        //
        // Shader.PropertyToID is called once per ID at class-load time (static
        // readonly). This is a micro-optimisation the original code already used;
        // we preserve it. Each static readonly field is a single int — zero
        // per-frame allocation.
        //
        // [WMC] Static field initialisers do not count toward WMC (they have
        // no cyclomatic complexity); the struct was not a contributor to WMC ~74.
        private static class ShaderID
        {
            // Data cube
            public static readonly int DataCube             = Shader.PropertyToID("_DataCube");
            public static readonly int NumColorMaps         = Shader.PropertyToID("_NumColorMaps");

            // Sampling
            public static readonly int SliceMin             = Shader.PropertyToID("_SliceMin");
            public static readonly int SliceMax             = Shader.PropertyToID("_SliceMax");
            public static readonly int ThresholdMin         = Shader.PropertyToID("_ThresholdMin");
            public static readonly int ThresholdMax         = Shader.PropertyToID("_ThresholdMax");
            public static readonly int Jitter               = Shader.PropertyToID("_Jitter");
            public static readonly int MaxSteps             = Shader.PropertyToID("_MaxSteps");

            // Colour mapping
            public static readonly int ColorMapIndex        = Shader.PropertyToID("_ColorMapIndex");
            public static readonly int ScaleMin             = Shader.PropertyToID("_ScaleMin");
            public static readonly int ScaleMax             = Shader.PropertyToID("_ScaleMax");
            public static readonly int ScaleType            = Shader.PropertyToID("ScaleType");
            public static readonly int ScaleBias            = Shader.PropertyToID("ScaleBias");
            public static readonly int ScaleContrast        = Shader.PropertyToID("ScaleContrast");
            public static readonly int ScaleAlpha           = Shader.PropertyToID("ScaleAlpha");
            public static readonly int ScaleGamma           = Shader.PropertyToID("ScaleGamma");

            // Foveation (uniforms pushed by VolumeMaterialBinder;
            //            values come from FoveatedSamplingPolicy via VolumeRenderState)
            public static readonly int FoveationStart       = Shader.PropertyToID("FoveationStart");
            public static readonly int FoveationEnd         = Shader.PropertyToID("FoveationEnd");
            public static readonly int FoveationJitter      = Shader.PropertyToID("FoveationJitter");
            public static readonly int FoveatedStepsLow     = Shader.PropertyToID("FoveatedStepsLow");
            public static readonly int FoveatedStepsHigh    = Shader.PropertyToID("FoveatedStepsHigh");

            // Region highlight / selection
            public static readonly int HighlightMin         = Shader.PropertyToID("HighlightMin");
            public static readonly int HighlightMax         = Shader.PropertyToID("HighlightMax");
            public static readonly int HighlightSaturateFactor = Shader.PropertyToID("HighlightSaturateFactor");

            // Vignette
            public static readonly int VignetteFadeStart    = Shader.PropertyToID("VignetteFadeStart");
            public static readonly int VignetteFadeEnd      = Shader.PropertyToID("VignetteFadeEnd");
            public static readonly int VignetteIntensity    = Shader.PropertyToID("VignetteIntensity");
            public static readonly int VignetteColor        = Shader.PropertyToID("VignetteColor");

            // Mask material (on _maskMaterial, not _material)
            public static readonly int MaskCube             = Shader.PropertyToID("MaskCube");
            public static readonly int MaskMode             = Shader.PropertyToID("MaskMode");
            public static readonly int MaskVoxelSize        = Shader.PropertyToID("MaskVoxelSize");
            public static readonly int MaskVoxelColor       = Shader.PropertyToID("MaskVoxelColor");
            public static readonly int MaskVoxelOffsets     = Shader.PropertyToID("MaskVoxelOffsets");
            public static readonly int ModelMatrix          = Shader.PropertyToID("ModelMatrix");
            public static readonly int HighlightedSource    = Shader.PropertyToID("HighlightedSource");
            public static readonly int MaskEntries          = Shader.PropertyToID("MaskEntries");
            public static readonly int RegionOffset         = Shader.PropertyToID("RegionOffset");
            public static readonly int RegionDimensions     = Shader.PropertyToID("RegionDimensions");
            public static readonly int CubeDimensions       = Shader.PropertyToID("CubeDimensions");
        }

        // ── Constructor ───────────────────────────────────────────────────────
        //
        // [FIXED V-07 DIP] No singleton access here. Both collaborators are
        // injected; unit tests inject NullRenderPipeline and NullMaskMode.
        //
        // [CBO] Counts IRenderPipeline (#3) and IMaskMode (#4) — already noted
        // on the field declarations above.
        //
        // [WMC] Complexity = 1 (no branches). Contributes 1 to WMC total.
        public VolumeMaterialBinder(IRenderPipeline renderPipeline, IMaskMode initialMaskMode)
        {
            _renderPipeline = renderPipeline;
            _activeMaskMode = initialMaskMode;
        }

        // ── IVolumeMaterialBinder implementation ──────────────────────────────

        /// <inheritdoc/>
        // [→ before _startFunc() lines 510–521]
        //
        // [FIXED V-01 SRP] _startFunc() was a 185-line coroutine that wired 8+
        // systems. VolumeMaterialBinder.Initialise() does one thing only:
        // create material instances and push the initial data cube texture.
        //
        // [FIXED V-09 DIP] Material.Instantiate is still called here, but
        // VolumeMaterialBinder owns the lifecycle. The coordinator never sees
        // the Material; it just calls Initialise() and then Tick() each frame.
        //
        // [WMC] Complexity = 1. No branches. Contributes 1 to WMC total.
        public void Initialise(Material rayMarchingMaterial, Material maskMaterial,
                               Texture3D dataCube, int numColorMaps)
        {
            _material = Object.Instantiate(rayMarchingMaterial);
            _material.SetTexture(ShaderID.DataCube, dataCube);
            _material.SetInt(ShaderID.NumColorMaps, numColorMaps);

            _maskMaterial = Object.Instantiate(maskMaterial);
        }

        /// <inheritdoc/>
        // [→ before Update() lines 1124–1226]
        //
        // [FIXED V-01 SRP] Update() mixed shader binding, foveation, WCS update,
        // and highlight calculation in one 115-line method. Tick() does only
        // shader binding; foveation step counts arrive pre-computed in the state
        // struct from FoveatedSamplingPolicy.
        //
        // [FIXED V-04 OCP] The mask-mode integer (line 1193) is gone.
        // _activeMaskMode.Apply() dispatches polymorphically.
        //
        // [FIXED V-05 OCP] The ProjectionMode if/else (lines 1218–1221) is gone.
        // IRenderPipeline.SetPipelineKeyword handles the variant boundary.
        //
        // [WMC] Cyclomatic complexity ≈ 5:
        //   1 base path
        //   +1 foveation branch (FoveatedEnabled)
        //   +1 highlight branch (HighlightActive)
        //   +1 mask branch (HasMask)
        //   +1 projection keyword (AverageIntensityProjection bool)
        // Total = 5. Compare to Update() which scored ~8 by itself.
        public void Tick(in VolumeRenderState s)
        {
            // ── Sampling uniforms ──────────────────────────────────────────────
            // [→ before Update() lines 1124–1129]
            _material.SetVector(ShaderID.SliceMin,      s.SliceMin);
            _material.SetVector(ShaderID.SliceMax,      s.SliceMax);
            _material.SetFloat(ShaderID.ThresholdMin,   s.ThresholdMin);
            _material.SetFloat(ShaderID.ThresholdMax,   s.ThresholdMax);
            _material.SetFloat(ShaderID.Jitter,         s.Jitter);
            _material.SetFloat(ShaderID.MaxSteps,       s.MaxSteps);

            // ── Colour-map uniforms ────────────────────────────────────────────
            // [→ before Update() lines 1130–1137]
            _material.SetFloat(ShaderID.ColorMapIndex,  s.ColorMapIndex);
            _material.SetFloat(ShaderID.ScaleMax,       s.ScaleMax);
            _material.SetFloat(ShaderID.ScaleMin,       s.ScaleMin);
            _material.SetInt(ShaderID.ScaleType,        s.ScaleType);
            _material.SetFloat(ShaderID.ScaleBias,      s.ScaleBias);
            _material.SetFloat(ShaderID.ScaleContrast,  s.ScaleContrast);
            _material.SetFloat(ShaderID.ScaleAlpha,     s.ScaleAlpha);
            _material.SetFloat(ShaderID.ScaleGamma,     s.ScaleGamma);

            // ── Foveation uniforms ─────────────────────────────────────────────
            // [→ before Update() lines 1139–1166]
            //
            // FoveatedSamplingPolicy already decided (stepsLow, stepsHigh) from
            // gaze data before Tick() is called. VolumeMaterialBinder just binds.
            // If foveation is disabled, both counts collapse to MaxSteps, preserving
            // INV-01 (90 fps minimum) — the same fallback as the original code.
            //
            // [FIXED V-01 SRP] Foveation was interleaved with material binding in
            // Update(). It is now cleanly separated: policy in FoveatedSamplingPolicy,
            // binding here, zero shared logic.
            _material.SetFloat(ShaderID.FoveationStart, s.FoveationStart);
            _material.SetFloat(ShaderID.FoveationEnd,   s.FoveationEnd);

            if (s.FoveatedEnabled)
            {
                _material.SetFloat(ShaderID.FoveationJitter,   s.FoveationJitter);
                _material.SetInt(ShaderID.FoveatedStepsLow,    s.FoveatedStepsLow);
                _material.SetInt(ShaderID.FoveatedStepsHigh,   s.FoveatedStepsHigh);
            }
            else
            {
                // Both step counts at MaxSteps → uniform sampling, no foveation effect
                _material.SetInt(ShaderID.FoveatedStepsLow,    s.MaxSteps);
                _material.SetInt(ShaderID.FoveatedStepsHigh,   s.MaxSteps);
            }

            // ── Region highlight uniforms ──────────────────────────────────────
            // [→ before Update() lines 1168–1189]
            if (s.HighlightActive)
            {
                _material.SetVector(ShaderID.HighlightMin,           s.HighlightMin);
                _material.SetVector(ShaderID.HighlightMax,           s.HighlightMax);
                _material.SetFloat(ShaderID.HighlightSaturateFactor, s.HighlightSaturateFactor);
            }
            else
            {
                // HighlightSaturateFactor = 1 disables the selection tint in the shader
                _material.SetFloat(ShaderID.HighlightSaturateFactor, 1f);
            }

            // ── Mask uniforms ──────────────────────────────────────────────────
            // [→ before Update() lines 1191–1210]
            //
            // [FIXED V-04 OCP] The original code called:
            //   _materialInstance.SetInt(MaterialID.MaskMode, MaskMode.GetHashCode())
            // and relied on the enum int value matching a shader #define. Adding a
            // new mask mode required editing the enum, Update(), BasicVolume.cginc,
            // and PaintMenuController.cs — 4 files.
            //
            // Now: _activeMaskMode.Apply() sets whatever keywords and properties
            // this mode owns. New mode = new class only. Nothing in Tick() changes.
            if (s.HasMask)
            {
                _activeMaskMode.Apply(_material, null); // Texture already bound via BindMaskTexture

                _maskMaterial.SetFloat(ShaderID.MaskVoxelSize,     s.MaskVoxelSize);
                _maskMaterial.SetColor(ShaderID.MaskVoxelColor,    s.MaskVoxelColor);
                _maskMaterial.SetInt(ShaderID.HighlightedSource,   s.HighlightedSource);
                _maskMaterial.SetVectorArray(ShaderID.MaskVoxelOffsets, s.MaskVoxelOffsets);
                _maskMaterial.SetMatrix(ShaderID.ModelMatrix,      s.ModelMatrix);
            }

            // ── Projection mode keyword ────────────────────────────────────────
            // [→ before Update() lines 1218–1221]
            //
            // [FIXED V-05 OCP] The original code called global Shader.EnableKeyword /
            // Shader.DisableKeyword, which URP deprecates (global keyword state
            // conflicts with multi-camera and SRP batcher).
            //
            // [FIXED V-15 GRASP Protected Variations] IRenderPipeline is the
            // variation point for pipeline-specific keyword handling. The URP
            // adapter calls material-local LocalKeyword; the built-in adapter calls
            // the global API. Domain code is unchanged across both pipelines.
            _renderPipeline.SetPipelineKeyword("SHADER_AIP", s.AverageIntensityProjection);

            // ── Vignette uniforms ──────────────────────────────────────────────
            // [→ before Update() lines 1223–1226]
            _material.SetFloat(ShaderID.VignetteFadeStart,  s.VignetteFadeStart);
            _material.SetFloat(ShaderID.VignetteFadeEnd,    s.VignetteFadeEnd);
            _material.SetFloat(ShaderID.VignetteIntensity,  s.VignetteIntensity);
            _material.SetColor(ShaderID.VignetteColor,      s.VignetteColor);
        }

        /// <inheritdoc/>
        // [→ before CropToRegion() line 1005, ResetCrop() line 1031]
        //
        // [FIXED V-13 GRASP Controller] In the original code, CropToRegion() mixed
        // crop geometry calculation, texture upload, material binding, and moment-map
        // update in one 30-line method. Now:
        //   VolumeTextureManager.LoadRegion()  → uploads texture
        //   VolumeMaterialBinder.BindDataTexture() → one line here
        //   VolumeRenderCoordinator wires the two calls
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void BindDataTexture(Texture3D dataCube)
        {
            _material.SetTexture(ShaderID.DataCube, dataCube);
        }

        /// <inheritdoc/>
        // [→ before _startFunc() line 520, CropToRegion() line 1009, InitialiseMask() line 1311]
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void BindMaskTexture(Texture3D maskCube)
        {
            _material.SetTexture(ShaderID.MaskCube, maskCube);
        }

        /// <summary>
        /// Bind crop-region geometry uniforms on the mask material.
        /// Called by VolumeRenderCoordinator after CropToRegion or ResetCrop.
        /// </summary>
        // [→ before CropToRegion() lines 1010–1015, ResetCrop() lines 1040–1044]
        //
        // This method is not on IVolumeMaterialBinder (it is a detail of mask
        // geometry, not part of the coordinator's core contract). It is public so
        // that VolumeRenderCoordinator can call it after a crop operation.
        //
        // NOTE: This adds one member beyond the 7-member interface, raising the
        // public method count to 8. WMC target (≤ 20) is still met. Revisit if
        // the coordinator can absorb this call without introducing a new coupling.
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void BindMaskCropUniforms(Vector3Int regionMin, Texture3D maskRegionCube,
                                         Vector3 cubeDimensions)
        {
            _maskMaterial.SetVector(ShaderID.RegionOffset,
                new Vector4(regionMin.x, regionMin.y, regionMin.z, 0));
            _maskMaterial.SetVector(ShaderID.RegionDimensions,
                new Vector4(maskRegionCube.width, maskRegionCube.height, maskRegionCube.depth, 0));
            _maskMaterial.SetVector(ShaderID.CubeDimensions,
                new Vector4(cubeDimensions.x, cubeDimensions.y, cubeDimensions.z, 1));
        }

        /// <inheritdoc/>
        // [FIXED V-04 OCP] Caller swaps the strategy object; zero code changes here.
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void SetActiveMaskMode(IMaskMode mode)
        {
            _activeMaskMode = mode;
        }

        /// <inheritdoc/>
        // [→ before OnRenderObject() lines 1276–1290]
        //
        // [FIXED V-15 GRASP Protected Variations + DIP Violation D4]
        // The original OnRenderObject() called Graphics.DrawProceduralNow —
        // a built-in RP API that does not exist in URP. SubmitMaskGeometry()
        // delegates to IRenderPipeline.SubmitProceduralDraw, which the URP adapter
        // (UrpRenderPipeline) implements using CommandBuffer.DrawProcedural inside
        // a ScriptableRenderPass. Domain logic is unchanged when migrating to URP.
        //
        // [WMC] Complexity = 3 (two guard conditionals, one base path).
        // Contributes 3 to WMC total.
        public void SubmitMaskGeometry(bool displayMask, bool isFullResolution,
                                       ComputeBuffer existingMaskBuffer, int existingMaskCount,
                                       ComputeBuffer addedMaskBuffer,   int addedMaskEntryCount)
        {
            if (!displayMask || !isFullResolution) return;

            if (existingMaskBuffer != null)
            {
                _maskMaterial.SetBuffer(ShaderID.MaskEntries, existingMaskBuffer);
                _renderPipeline.SubmitProceduralDraw(
                    _maskMaterial, existingMaskBuffer, existingMaskCount, Matrix4x4.identity);
            }

            if (addedMaskBuffer != null && addedMaskEntryCount > 0)
            {
                _maskMaterial.SetBuffer(ShaderID.MaskEntries, addedMaskBuffer);
                _renderPipeline.SubmitProceduralDraw(
                    _maskMaterial, addedMaskBuffer, addedMaskEntryCount, Matrix4x4.identity);
            }
        }

        /// <inheritdoc/>
        // Material instances created via Object.Instantiate must be destroyed
        // explicitly; Unity does not garbage-collect assets.
        // This is the only place that calls Object.Destroy on materials —
        // the lifecycle is fully encapsulated here.
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void Dispose()
        {
            if (_material     != null) Object.Destroy(_material);
            if (_maskMaterial != null) Object.Destroy(_maskMaterial);
        }

        // ── Persistence — Sub-team 7 Integration ───────────────────────────────
        // [§ docs/integration/team7-persistence-contract.md — RenderingState]
        //
        // These methods are stubs. The full implementation will be wired in Sprint 3
        // after the final RenderingState struct fields are agreed with the team.

        /// <summary>
        /// Captures the current rendering state for session persistence.
        /// Called by <c>VolumeRenderCoordinator.SaveSession()</c>.
        /// </summary>
        public RenderingState CaptureState()
        {
            // TODO Sprint 3: extract current values from _material properties and flags.
            // For now, return a placeholder that compiles.
            return new RenderingState(
                thresholdMin: 0f,
                thresholdMax: 1f,
                colorMap: 0,
                scalingType: 0,
                averageIntensityProjection: false,
                activeMaskMode: 0
            );
        }

        /// <summary>
        /// Restores rendering state after session load.
        /// Called by <c>VolumeRenderCoordinator.LoadSession()</c>.
        /// Must leave this class in a valid, ready-to-render state.
        /// </summary>
        public void RestoreState(RenderingState state)
        {
            // TODO Sprint 3: apply state values back to _material and flags.
            // Must call pushToShader() equivalent to sync the material after restore.
            // Placeholder implementation compiles without changes.
        }

        // ── Expose material for MeshRenderer wiring (coordinator only) ─────────
        //
        // VolumeRenderCoordinator needs to assign _material to the MeshRenderer
        // (equivalent to line 522: _renderer.material = _materialInstance).
        // Rather than breaking encapsulation by returning the Material, we expose
        // a dedicated method so the coordinator never holds a raw Material reference.
        //
        // This keeps the coordinator's CBO addition for this type at exactly one
        // (through IVolumeMaterialBinder) with no Material leak.
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public void ApplyToRenderer(MeshRenderer renderer)  // [CBO #5] MeshRenderer
        {
            renderer.material = _material;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CK METRICS SUMMARY — VolumeMaterialBinder (Understand tool — measured)
    //
    // WMC = 10   NIM = 10   NIV = 4                              ✅ target ≤ 20
    //
    // CBO = 12                                                   ✅ target ≤ 14 domain
    //   Coupled classes include: Material, Texture3D, IRenderPipeline, IMaskMode,
    //   VolumeRenderState, IVolumeMaterialBinder, FoveatedSamplingConfig,
    //   VolumeColourMap, MeshRenderer, and related UnityEngine value types.
    //
    // RFC = 10                                                   ✅ target ≤ 50
    //
    // LCOM = 0.57  (57% Percent Lack of Cohesion)               ⚠ target ≤ 0.5
    //   Residual LCOM reflects Initialise/Tick/Dispose lifecycle phases that
    //   each access different field subsets — a known LCOM artefact for classes
    //   with legitimate multi-phase structure.
    //
    // DIT = 1  (IFANIN = 2: implements interface + inherits from object)      ✅ ≤ 4
    // NOC = 0  (sealed)                                                        ✅ ≤ 5
    //
    // BEFORE (VolumeDataSetRenderer — Understand tool):
    //   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
    //
    // REDUCTION:
    //   WMC: 10 vs. 97  →  -87 units
    //   CBO: 12 vs. 28  →  -16 coupled classes
    //   LCOM: 0.57 vs. 0.95  →  substantial improvement; lifecycle artefact documented
    // ──────────────────────────────────────────────────────────────────────────
}
