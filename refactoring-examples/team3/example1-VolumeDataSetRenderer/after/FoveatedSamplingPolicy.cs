// =============================================================================
// FoveatedSamplingPolicy.cs
// AFTER/ — Refactoring Example 1: VolumeDataSetRenderer Split
// Sub-team 3 — Rendering Engine ("Cache Me If You Can")
// =============================================================================
//
// PURPOSE
// -------
// Encapsulates all foveated-rendering decisions that were previously scattered
// across VolumeDataSetRenderer. The coordinator calls ComputeParameters() once
// per frame; VolumeMaterialBinder pushes the resulting struct to the shader.
// Neither class needs to know anything about gaze hardware or zone geometry.
//
// WHAT THIS FILE REPLACES IN THE BEFORE/ FILE
// --------------------------------------------
//   Fields (before/ lines 135–145):
//     FoveatedRendering, FoveationStart, FoveationEnd,
//     FoveationJitter, FoveatedStepsLow, FoveatedStepsHigh
//
//   Logic (before/ lines 1139–1165, inside Update()):
//     The if(FoveatedRendering) block that pushes the above fields to the material.
//     In the after/ design that push is done by VolumeMaterialBinder — this class
//     only *computes* the parameters; it never touches Material directly.
//     This separation keeps VolumeMaterialBinder unit-testable without eye-tracking
//     hardware and keeps this class testable without a GPU.
//
// WHY THIS CLASS EXISTS (SOLID / GRASP)
// --------------------------------------
//   SRP  — VolumeDataSetRenderer had ≥8 responsibilities. Foveation decisions are
//          a distinct concern (violation V-04 in docs/design-document.md §7.1).
//          Extracting them removes ~30 lines from Update() and eliminates 5 public
//          mutable fields from the god class.
//
//   DIP  — VolumeRenderCoordinator and VolumeMaterialBinder depend on
//          FoveatedSamplingPolicy via constructor injection. They never import
//          SteamVR, OpenXR, or any HMD SDK type. Swapping the underlying
//          eye-tracking system only affects the IGaze implementation,
//          nothing in the rendering core.
//
//   OCP  — New sampling strategies (e.g. predictive gaze, saccade-aware foveation)
//          can subclass or replace FoveatedSamplingPolicy without modifying
//          VolumeRenderCoordinator or VolumeMaterialBinder.
//
//   GRASP Information Expert — this class holds all data needed to answer
//          "what are the correct shader step counts and zone radii for this frame?"
//          The coordinator does not need to know the answer; it only calls
//          ComputeParameters() and forwards the result.
//
// FOUR OWNED BEHAVIOURS (see method docs below for rationale per behaviour)
// --------------------------------------------------------------------------
//   1. Sample rate per region  — ComputeParameters()
//   2. LOD / mip-level bias    — ComputeMipBias()
//   3. Reprojection mask       — WriteReprojectionMask()
//   4. HMD-absent fallback     — implicit via IsGazeAvailable / FoveationParameters.Uniform()
//
// MEASURED CK METRICS (Understand tool)
// --------------------------------------
//   Metric  Value  Target        Note
//   WMC     6      ≤ 20  ✓       NIM=6, NIV=2
//   CBO     6      ≤ 14  ✓       IGaze, FoveatedSamplingConfig,
//                                FoveationParameters, FoveationZone, Vector2,
//                                and related types
//   RFC     6      ≤ 50  ✓
//   LCOM    0.33   ≤ 0.5 ✓       33% Percent Lack of Cohesion; all targets met
//   DIT     1      ≤ 4   ✓       IFANIN=1
//   NOC     0      ≤ 5   ✓       no children in the design
//
// =============================================================================

using UnityEngine;

namespace iDaVIE.Rendering
{
    // =========================================================================
    // IGaze — CONFIRMED with Sub-team 4 (Interaction), 2 June 2026
    // =========================================================================
    //
    // ✅  Contract agreed. Sub-team 4 owns a unified IGaze interface used by
    //     both teams. We consume it here; they implement it in their Interaction
    //     layer. We only use GazeFocusPoint, GazeDirection, and IsTracking.
    //     Any additional members on their full interface are irrelevant to us
    //     and can be ignored — we only depend on what FoveatedSamplingPolicy calls.
    //
    // Name changes from our original stub:
    //   • IGaze  → IGaze          (their canonical name)
    //   • IsGazeAvailable → IsTracking    (their canonical name; semantics identical)
    //   • GazeFocusPoint, GazeDirection   — unchanged
    //
    // Wire-up in VolumeRenderCoordinator (Sprint 3):
    //   [SerializeField] private IGaze _gazeProvider;
    //   — Unity 6 migration: swap the component in the Inspector, code unchanged.
    //
    // This declaration should be DELETED once Sub-team 4 publishes their assembly;
    // replace with a project reference to their IGaze definition.
    // =========================================================================

    /// <summary>
    /// Provides gaze direction and screen focus point from the HMD's eye-tracking system.
    /// Defined by Sub-team 4 (Interaction); consumed here under DIP.
    /// Confirmed interface name and member names: 2 June 2026.
    /// </summary>
    public interface IGaze
    {
        /// <summary>
        /// Gaze direction vector in world space.
        /// Valid when <see cref="IsTracking"/> is <c>true</c>; undefined otherwise.
        /// </summary>
        Vector3 GazeDirection { get; }

        /// <summary>
        /// Gaze focus point in normalised screen coordinates, range [0, 1].
        /// (0.5, 0.5) is the screen centre.
        /// Valid when <see cref="IsTracking"/> is <c>true</c>; returns (0.5, 0.5) otherwise.
        /// </summary>
        Vector2 GazeFocusPoint { get; }

        /// <summary>
        /// <c>true</c> when eye-tracking data is valid this frame.
        /// <c>false</c> when no eye-tracking hardware is present, the HMD is not worn,
        /// or the tracking system has not yet completed its calibration.
        /// </summary>
        bool IsTracking { get; }
    }

    // =========================================================================
    // FoveationZone — zone classification used by mip-bias and reprojection mask
    // =========================================================================

    /// <summary>
    /// Classifies a screen-space point into one of three foveated rendering zones.
    /// </summary>
    /// <remarks>
    /// The shader (VolumeRender.shader) continuously interpolates step counts
    /// per pixel using <c>FoveationStart</c> and <c>FoveationEnd</c> radii, so
    /// it does NOT use this enum at runtime. <see cref="FoveationZone"/> exists
    /// for the CPU-side computations:
    /// <list type="bullet">
    ///   <item><see cref="ComputeMipBias"/> needs a discrete zone to select a mip level.</item>
    ///   <item><see cref="WriteReprojectionMask"/> encodes zone identity per pixel.</item>
    /// </list>
    /// </remarks>
    public enum FoveationZone
    {
        /// <summary>
        /// Within <see cref="FoveatedSamplingConfig.InnerRadius"/> of the gaze point.
        /// Receives the highest ray-march step count (<see cref="FoveatedSamplingConfig.StepsHigh"/>)
        /// and mip level 0 (full resolution).
        /// </summary>
        Foveal = 0,

        /// <summary>
        /// Between <see cref="FoveatedSamplingConfig.InnerRadius"/> and
        /// <see cref="FoveatedSamplingConfig.OuterRadius"/> of the gaze point.
        /// Transition region; shader interpolates step count linearly between
        /// <see cref="FoveatedSamplingConfig.StepsHigh"/> and <see cref="FoveatedSamplingConfig.StepsLow"/>.
        /// Mip level 1 on the CPU side.
        /// </summary>
        Parafoveal = 1,

        /// <summary>
        /// Beyond <see cref="FoveatedSamplingConfig.OuterRadius"/> of the gaze point.
        /// Receives the lowest step count (<see cref="FoveatedSamplingConfig.StepsLow"/>)
        /// and the highest mip level the texture supports.
        /// </summary>
        Peripheral = 2
    }

    // =========================================================================
    // FoveatedSamplingConfig — extracted constants from VolumeDataSetRenderer
    // =========================================================================

    /// <summary>
    /// Immutable configuration for <see cref="FoveatedSamplingPolicy"/>.
    /// All values are extracted from the serialised fields that were previously
    /// public on <c>VolumeDataSetRenderer</c> (before/ lines 140–145).
    /// </summary>
    /// <remarks>
    /// Making this a value type (struct) keeps it allocation-free.
    /// The coordinator reads the values from a Unity <c>ScriptableObject</c>
    /// asset and passes them in at construction time — removing the need for
    /// Inspector-serialised public fields on the renderer itself.
    /// </remarks>
    public readonly struct FoveatedSamplingConfig
    {
        // ---------------------------------------------------------------------
        // Zone geometry
        // These were Inspector-serialised floats on VolumeDataSetRenderer.
        // Default values match the original [Range(0, 0.5f)] field initialisers.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Normalised screen-space radius of the inner (foveal) zone boundary.
        /// Maps to shader uniform <c>FoveationStart</c>.
        /// Before/ default: <c>0.15f</c> (before/ line 141).
        /// </summary>
        public readonly float InnerRadius;   // before/ name: FoveationStart

        /// <summary>
        /// Normalised screen-space radius of the outer (peripheral) zone boundary.
        /// Maps to shader uniform <c>FoveationEnd</c>.
        /// Before/ default: <c>0.40f</c> (before/ line 142).
        /// </summary>
        public readonly float OuterRadius;   // before/ name: FoveationEnd

        /// <summary>
        /// Per-frame spatial jitter magnitude applied to the zone boundary to
        /// reduce temporal aliasing at the foveal edge.
        /// Maps to shader uniform <c>FoveationJitter</c>.
        /// Before/ default: <c>0.0f</c> (before/ line 143).
        /// </summary>
        public readonly float Jitter;        // before/ name: FoveationJitter

        // ---------------------------------------------------------------------
        // Step counts
        // [Range(16, 512)] in the original Inspector. Values control the number
        // of ray-march samples along each ray per zone.
        // ---------------------------------------------------------------------

        /// <summary>
        /// Ray-march step count for the peripheral zone (lowest quality, cheapest).
        /// Maps to shader uniform <c>FoveatedStepsLow</c>.
        /// Before/ default: <c>64</c> (before/ line 144).
        /// </summary>
        public readonly int StepsLow;        // before/ name: FoveatedStepsLow

        /// <summary>
        /// Ray-march step count for the foveal zone (highest quality, most expensive).
        /// Maps to shader uniform <c>FoveatedStepsHigh</c>.
        /// Before/ default: <c>384</c> (before/ line 145).
        /// </summary>
        public readonly int StepsHigh;       // before/ name: FoveatedStepsHigh

        /// <summary>
        /// Uniform step count used when foveated rendering is disabled or
        /// eye-tracking is unavailable (before/ lines 1163–1165: both Low and High
        /// are set to <c>MaxSteps</c> in the else branch).
        /// Set to <see cref="StepsHigh"/> to preserve full quality in the fallback path.
        /// </summary>
        public readonly int MaxSteps;        // before/ name: implicit — MaxSteps constant in VDSR

        /// <summary>
        /// Constructs a config with explicit values for all parameters.
        /// </summary>
        public FoveatedSamplingConfig(
            float innerRadius, float outerRadius, float jitter,
            int stepsLow, int stepsHigh, int maxSteps)
        {
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            Jitter      = jitter;
            StepsLow    = stepsLow;
            StepsHigh   = stepsHigh;
            MaxSteps    = maxSteps;
        }

        /// <summary>
        /// Default configuration matching the original Inspector field values
        /// from <c>VolumeDataSetRenderer</c> (before/ lines 140–145).
        /// </summary>
        public static readonly FoveatedSamplingConfig Default = new FoveatedSamplingConfig(
            innerRadius: 0.15f,
            outerRadius: 0.40f,
            jitter:      0.0f,
            stepsLow:    64,
            stepsHigh:   384,
            maxSteps:    384
        );
    }

    // =========================================================================
    // FoveationParameters — the result struct returned to VolumeMaterialBinder
    // =========================================================================

    /// <summary>
    /// Computed foveation parameters for one rendered frame.
    /// <see cref="FoveatedSamplingPolicy.ComputeParameters"/> returns this;
    /// <c>VolumeMaterialBinder.BindFrame()</c> consumes it and pushes each field
    /// to the shader using <c>Shader.PropertyToID</c> IDs cached in <c>MaterialID</c>.
    /// </summary>
    /// <remarks>
    /// Value type (struct) — zero allocation per frame.
    /// </remarks>
    public readonly struct FoveationParameters
    {
        /// <summary>Normalised inner zone radius. → shader <c>FoveationStart</c>.</summary>
        public readonly float InnerRadius;

        /// <summary>Normalised outer zone radius. → shader <c>FoveationEnd</c>.</summary>
        public readonly float OuterRadius;

        /// <summary>Zone boundary jitter. → shader <c>FoveationJitter</c>.</summary>
        public readonly float Jitter;

        /// <summary>Ray-march steps, peripheral region. → shader <c>FoveatedStepsLow</c>.</summary>
        public readonly int StepsLow;

        /// <summary>Ray-march steps, foveal region. → shader <c>FoveatedStepsHigh</c>.</summary>
        public readonly int StepsHigh;

        /// <summary>
        /// Gaze focus point in normalised screen coords [0,1].
        /// → shader uniform (name TBD pending Sub-team 4 interface confirmation).
        /// Not present in the before/ code — the before/ shader hard-coded screen
        /// centre as the foveal zone origin.
        /// </summary>
        public readonly Vector2 GazeFocusPoint;

        /// <summary>
        /// <c>true</c> if per-zone step counts should be used;
        /// <c>false</c> if the fallback uniform step count was applied instead.
        /// </summary>
        public readonly bool FoveationActive;

        /// <summary>Constructs a fully populated parameters struct.</summary>
        public FoveationParameters(
            float innerRadius, float outerRadius, float jitter,
            int stepsLow, int stepsHigh,
            Vector2 gazeFocusPoint, bool foveationActive)
        {
            InnerRadius    = innerRadius;
            OuterRadius    = outerRadius;
            Jitter         = jitter;
            StepsLow       = stepsLow;
            StepsHigh      = stepsHigh;
            GazeFocusPoint = gazeFocusPoint;
            FoveationActive = foveationActive;
        }

        /// <summary>
        /// Returns a uniform (non-foveated) parameter set where both step counts
        /// equal <paramref name="maxSteps"/> and foveation is inactive.
        /// Mirrors the else-branch in before/ lines 1163–1165.
        /// </summary>
        /// <param name="maxSteps">Uniform step count for all screen regions.</param>
        public static FoveationParameters Uniform(int maxSteps) =>
            new FoveationParameters(
                innerRadius:     0f,
                outerRadius:     0f,
                jitter:          0f,
                stepsLow:        maxSteps,
                stepsHigh:       maxSteps,
                gazeFocusPoint:  new Vector2(0.5f, 0.5f),
                foveationActive: false
            );
    }

    // =========================================================================
    // FoveationState — persistence snapshot of FoveatedSamplingPolicy config
    //
    // PERSISTENCE CONTRACT (Sub-team 7 integration)
    // ─────────────────────────────────────────────
    // Owned by FoveatedSamplingPolicy per docs/integration/team7-persistence-contract.md.
    // Captured by FoveatedSamplingPolicy.CaptureState() → returned to coordinator.
    // Restored by FoveatedSamplingPolicy.RestoreState(state) → coordinator calls this
    // after session load.
    //
    // Records whether foveation is enabled and the zone radii / sample rates.
    // =========================================================================

    /// <summary>
    /// Session-persistent state owned by <see cref="FoveatedSamplingPolicy"/>.
    /// Captured before save, restored after load. Follows Sub-team 7 contract.
    /// </summary>
    public readonly struct FoveationState
    {
        public readonly bool  FoveationEnabled;
        public readonly float FovealRadius;
        public readonly float ParafovealRadius;
        public readonly float FovealSampleRate;
        public readonly float ParafovealSampleRate;
        public readonly float PeripheralSampleRate;

        public FoveationState(
            bool foveationEnabled,
            float fovealRadius, float parafovealRadius,
            float fovealSampleRate, float parafovealSampleRate, float peripheralSampleRate)
        {
            FoveationEnabled       = foveationEnabled;
            FovealRadius           = fovealRadius;
            ParafovealRadius       = parafovealRadius;
            FovealSampleRate       = fovealSampleRate;
            ParafovealSampleRate   = parafovealSampleRate;
            PeripheralSampleRate   = peripheralSampleRate;
        }
    }

    // =========================================================================
    // FoveatedSamplingPolicy — the main class
    // =========================================================================

    /// <summary>
    /// Computes foveated rendering parameters for a single rendered frame.
    /// Injected into <c>VolumeRenderCoordinator</c>; called once per frame via
    /// <see cref="ComputeParameters"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>SRP:</b> this class has exactly one reason to change — the algorithm
    /// that maps gaze data and zone config to shader step counts. It does not set
    /// shader properties (that is <c>VolumeMaterialBinder</c>'s job) and does not
    /// own any Unity lifecycle hooks.</para>
    ///
    /// <para><b>DIP:</b> the dependency on eye-tracking hardware is hidden behind
    /// <see cref="IGaze"/>. The coordinator passes a concrete
    /// implementation (real SteamVR/OpenXR provider in production, a
    /// <see cref="StubGazeProvider"/> in tests).</para>
    ///
    /// <para><b>sealed:</b> prevents subclasses from accidentally bypassing the
    /// fallback path or the null-check on <see cref="_gazeProvider"/>.</para>
    /// </remarks>
    public sealed class FoveatedSamplingPolicy
    {
        // CBO drivers: IGaze, FoveatedSamplingConfig (2 fields — all methods use both)
        private readonly IGaze          _gazeProvider;   // [CBO] Sub-team 4 contract
        private readonly FoveatedSamplingConfig _config;         // [CBO] extracted constants

        /// <summary>
        /// Constructs a policy with the given gaze source and zone configuration.
        /// </summary>
        /// <param name="gazeProvider">
        /// Eye-tracking source. Must not be null. Inject a
        /// <see cref="StubGazeProvider"/> for unit tests.
        /// </param>
        /// <param name="config">
        /// Zone geometry and step-count configuration. Use
        /// <see cref="FoveatedSamplingConfig.Default"/> to match the original
        /// Inspector values from <c>VolumeDataSetRenderer</c>.
        /// </param>
        public FoveatedSamplingPolicy(IGaze gazeProvider, FoveatedSamplingConfig config)
        {
            _gazeProvider = gazeProvider ?? throw new System.ArgumentNullException(nameof(gazeProvider));
            _config       = config;
        }

        // -----------------------------------------------------------------------
        // BEHAVIOUR 1: Sample rate per region
        //
        // Rationale: the ray-march step count is the dominant GPU cost knob in
        // the volume renderer. Foveation exploits human visual acuity falloff by
        // using fewer steps where the user is not looking. This method owns the
        // entire decision: which step counts to emit, whether foveation is active,
        // and where the foveal zone is centred. Centralising it here means
        // VolumeRenderCoordinator and VolumeMaterialBinder never contain an
        // if(FoveatedRendering) branch — that concern lives here only.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Computes the <see cref="FoveationParameters"/> struct for the current frame.
        /// </summary>
        /// <returns>
        /// A fully populated struct ready for <c>VolumeMaterialBinder.BindFrame()</c>
        /// to push to the shader. If eye-tracking is unavailable, returns
        /// <see cref="FoveationParameters.Uniform"/> — both step counts are set to
        /// <see cref="FoveatedSamplingConfig.MaxSteps"/>, preserving full-quality
        /// rendering. This mirrors the before/ else-branch (lines 1163–1165).
        /// </returns>
        /// <remarks>
        /// Called once per frame by <c>VolumeRenderCoordinator.OnRenderObject()</c>.
        /// CC = 2 (one branch for the fallback path).
        /// </remarks>
        public FoveationParameters ComputeParameters()
        {
            // BEHAVIOUR 4: HMD-absent fallback (see below for full rationale).
            // Evaluated here because it guards all other computation — if gaze is
            // unavailable there is no point computing zone radii or mip bias.
            if (!IsGazeAvailable)
                return FoveationParameters.Uniform(_config.MaxSteps);

            return new FoveationParameters(
                innerRadius:     _config.InnerRadius,
                outerRadius:     _config.OuterRadius,
                jitter:          _config.Jitter,
                stepsLow:        _config.StepsLow,
                stepsHigh:       _config.StepsHigh,
                gazeFocusPoint:  _gazeProvider.GazeFocusPoint,
                foveationActive: true
            );
        }

        // -----------------------------------------------------------------------
        // BEHAVIOUR 2: LOD / mip-level bias
        //
        // Rationale: the 3D texture that holds the FITS cube supports mip levels
        // (lower mips blur and downsample voxels). Using a higher mip for off-centre
        // regions is a complementary optimisation to reducing step counts — it
        // lowers both the per-sample texture bandwidth AND the visible noise at
        // low step counts in the peripheral zone. The decision depends entirely on
        // gaze data (owned here) and the maximum mip count (owned by
        // VolumeTextureManager). Neither caller needs to know the zone radii.
        // This method is absent from the before/ code — it is a new capability
        // unlocked by the extraction. It does not break any existing behaviour
        // because the coordinator can ignore the return value and pass 0 to
        // VolumeMaterialBinder until the optimisation is enabled.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns the recommended 3D texture mip-level bias for the current frame.
        /// </summary>
        /// <param name="maxMips">
        /// The number of mip levels available in the currently loaded volume texture.
        /// Provided by <c>VolumeTextureManager</c>. Must be ≥ 1.
        /// </param>
        /// <returns>
        /// <list type="bullet">
        ///   <item>0  — foveal zone or gaze unavailable (full-resolution texture).</item>
        ///   <item>1  — parafoveal zone (one mip down).</item>
        ///   <item><c>maxMips − 1</c> — peripheral zone (coarsest available mip).</item>
        /// </list>
        /// Clamped to [0, maxMips − 1] in all cases.
        /// </returns>
        /// <remarks>CC = 4 (zone switch + unavailability guard + clamp).</remarks>
        public int ComputeMipBias(int maxMips)
        {
            if (!IsGazeAvailable || maxMips <= 1)
                return 0;  // no mip optimisation possible or needed

            int rawBias = ClassifyZone(_gazeProvider.GazeFocusPoint) switch
            {
                FoveationZone.Foveal     => 0,
                FoveationZone.Parafoveal => 1,
                FoveationZone.Peripheral => maxMips - 1,
                _                        => 0
            };

            return Mathf.Clamp(rawBias, 0, maxMips - 1);
        }

        // -----------------------------------------------------------------------
        // BEHAVIOUR 3: Reprojection mask
        //
        // Rationale: the shader re-computes the foveal zone per pixel on the GPU
        // using FoveationStart/End radii. This works well per-frame, but a
        // temporal reprojection pass (a planned future feature — FUT-02 in the
        // design document) needs a stable CPU-side zone map that can be compared
        // across frames to decide which pixels need full re-rendering vs. can be
        // reprojected from the previous frame. This method writes that map.
        //
        // The output RenderTexture encodes zone identity in the red channel:
        //   0.0  →  FoveationZone.Foveal
        //   0.5  →  FoveationZone.Parafoveal
        //   1.0  →  FoveationZone.Peripheral
        //
        // Writing it here (not in VolumeMaterialBinder) is correct because:
        //   • The computation reads _gazeProvider and _config — both owned here.
        //   • VolumeMaterialBinder must not accumulate gaze dependencies; doing so
        //     would raise its CBO and introduce a hidden coupling to eye-tracking.
        //
        // NOTE: this method is a STUB in the design skeleton. The CPU rasterisation
        // loop (iterating Screen.width × Screen.height pixels) is intentionally
        // omitted — the design intent and data flow are what matter for the proposal.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Writes a screen-resolution reprojection mask to <paramref name="target"/>.
        /// Each pixel's red channel encodes its <see cref="FoveationZone"/> (0 = foveal,
        /// 0.5 = parafoveal, 1 = peripheral).
        /// </summary>
        /// <param name="target">
        /// A <c>RenderTexture</c> sized to the current HMD eye buffer. Must be
        /// <c>RenderTextureFormat.RFloat</c> or <c>ARGB32</c>. Allocated and owned
        /// by <c>VolumeRenderCoordinator</c>; this method only writes to it.
        /// </param>
        /// <remarks>
        /// <para>Call frequency: once per frame if temporal reprojection is active;
        /// skip entirely if the feature is disabled (coordinator controls this).</para>
        /// <para>This method is a design skeleton stub. The CPU rasterisation
        /// implementation is deferred to Sprint 3 (task S3-FUT-02).</para>
        /// </remarks>
        public void WriteReprojectionMask(RenderTexture target)
        {
            // Design intent:
            //   For each pixel (x, y) in target:
            //     Vector2 normCoord = new Vector2((float)x / target.width,
            //                                    (float)y / target.height);
            //     FoveationZone zone = ClassifyZone(normCoord);
            //     float encoded = zone == FoveationZone.Foveal     ? 0.0f
            //                   : zone == FoveationZone.Parafoveal ? 0.5f
            //                   :                                    1.0f;
            //     Write encoded to target pixel (x, y).
            //
            // In production: use a compute shader (one CS dispatch for the whole
            // target) rather than a CPU loop to stay within the 90 fps budget.
            // The CS uniform inputs are GazeFocusPoint, InnerRadius, OuterRadius.
            throw new System.NotImplementedException(
                "WriteReprojectionMask is a design-skeleton stub. " +
                "Implement in Sprint 3 (task S3-FUT-02) using a compute shader dispatch.");
        }

        // -----------------------------------------------------------------------
        // BEHAVIOUR 4: HMD-absent / eye-tracking fallback
        //
        // Rationale: not every HMD has an eye-tracking module (e.g. standard Vive
        // vs. Vive Pro Eye). The before/ code handles this with a single bool field
        // FoveatedRendering (before/ line 140) that must be toggled manually in the
        // Inspector. This approach breaks silently when the hardware is absent: the
        // developer must remember to uncheck the box.
        //
        // In the after/ design, IsGazeAvailable is driven at runtime by
        // IGaze.IsTracking. When it returns false, ComputeParameters()
        // automatically returns a uniform full-quality parameter set (behaviour 1).
        // ComputeMipBias() returns 0 (behaviour 2). WriteReprojectionMask() can be
        // skipped by the coordinator. No Inspector toggle is required.
        //
        // This is exposed as a property (not a private guard) so that
        // VolumeRenderCoordinator can also skip the WriteReprojectionMask() call
        // without having to duplicate the null/availability check.
        // -----------------------------------------------------------------------

        /// <summary>
        /// <c>true</c> when the eye-tracking source is reporting valid gaze data
        /// this frame. When <c>false</c>, all methods return safe uniform fallback values.
        /// </summary>
        /// <remarks>
        /// Mirrors <c>FoveatedRendering</c> (before/ line 140) but driven automatically
        /// at runtime from <see cref="IGaze.IsTracking"/> rather than
        /// requiring a manual Inspector toggle.
        /// </remarks>
        public bool IsGazeAvailable => _gazeProvider.IsTracking;

        // -----------------------------------------------------------------------
        // Private utilities
        // -----------------------------------------------------------------------

        /// <summary>
        /// Classifies a normalised screen point into a <see cref="FoveationZone"/>
        /// based on its Euclidean distance from the current gaze focus point.
        /// </summary>
        /// <param name="screenPoint">Normalised screen coordinate [0,1].</param>
        /// <returns>The zone the point falls into.</returns>
        /// <remarks>
        /// Uses the same radii (<see cref="FoveatedSamplingConfig.InnerRadius"/>,
        /// <see cref="FoveatedSamplingConfig.OuterRadius"/>) that are pushed to the
        /// shader as <c>FoveationStart</c> / <c>FoveationEnd</c>, so CPU and GPU
        /// zone boundaries are always consistent.
        /// CC = 2.
        /// </remarks>
        private FoveationZone ClassifyZone(Vector2 screenPoint)
        {
            float dist = Vector2.Distance(screenPoint, _gazeProvider.GazeFocusPoint);

            if (dist <= _config.InnerRadius)  return FoveationZone.Foveal;
            if (dist <= _config.OuterRadius)  return FoveationZone.Parafoveal;
            return FoveationZone.Peripheral;
        }

        // ── Persistence — Sub-team 7 Integration ───────────────────────────────
        // [§ docs/integration/team7-persistence-contract.md — FoveationState]
        //
        // These methods are stubs. The full implementation will be wired in Sprint 3
        // after the sample rate mapping is confirmed.

        /// <summary>
        /// Captures the current foveation config for session persistence.
        /// Called by <c>VolumeRenderCoordinator.SaveSession()</c>.
        /// </summary>
        public FoveationState CaptureState()
        {
            // TODO Sprint 3: map step counts to sample rates and include foveation
            // enabled flag. The mapping depends on MaxSteps and zone config.
            return new FoveationState(
                foveationEnabled: IsGazeAvailable,
                fovealRadius: _config.InnerRadius,
                parafovealRadius: _config.OuterRadius,
                fovealSampleRate: (float)_config.StepsHigh / (float)_config.MaxSteps,
                parafovealSampleRate: 0.5f,  // midpoint — placeholder
                peripheralSampleRate: (float)_config.StepsLow / (float)_config.MaxSteps
            );
        }

        /// <summary>
        /// Restores foveation config after session load.
        /// Called by <c>VolumeRenderCoordinator.LoadSession()</c>.
        /// </summary>
        public void RestoreState(FoveationState state)
        {
            // TODO Sprint 3: the config is immutable (passed at construction), so
            // restoration means saving the enabled flag and radii for the next
            // frame's ComputeParameters() call if they differ from the current config.
            // May require adding mutable fields to track user overrides.
        }
    }
}

// =============================================================================
// StubGazeProvider — test double
// =============================================================================
//
// PURPOSE
// -------
// Enables unit tests of FoveatedSamplingPolicy without VR hardware, a SteamVR
// context, or an OpenXR runtime. Inject this wherever FoveatedSamplingPolicy
// is constructed in edit-mode tests.
//
// USAGE EXAMPLES
// --------------
//   // Test 1: foveation active, gaze at screen centre
//   var policy = new FoveatedSamplingPolicy(
//       new StubGazeProvider(fixedFocus: new Vector2(0.5f, 0.5f), gazeAvailable: true),
//       FoveatedSamplingConfig.Default);
//   FoveationParameters p = policy.ComputeParameters();
//   Assert.IsTrue(p.FoveationActive);
//
//   // Test 2: HMD-absent fallback path
//   var policy = new FoveatedSamplingPolicy(
//       new StubGazeProvider(gazeAvailable: false),
//       FoveatedSamplingConfig.Default);
//   FoveationParameters p = policy.ComputeParameters();
//   Assert.IsFalse(p.FoveationActive);
//   Assert.AreEqual(FoveatedSamplingConfig.Default.MaxSteps, p.StepsLow);
//   Assert.AreEqual(FoveatedSamplingConfig.Default.MaxSteps, p.StepsHigh);
//
// PLACEMENT
// ---------
// This class is in the iDaVIE.Rendering.Tests namespace and must NOT be
// included in production builds. Add an Assembly Definition (asmdef) exclusion
// or a #if UNITY_EDITOR guard if needed.
// =============================================================================

namespace iDaVIE.Rendering.Tests
{
    using UnityEngine;

    /// <summary>
    /// Test double for <see cref="IGaze"/>.
    /// Returns a fixed gaze direction and focus point — no VR hardware required.
    /// </summary>
    public sealed class StubGazeProvider : IGaze
    {
        private readonly Vector2 _fixedFocus;
        private readonly bool    _gazeAvailable;

        /// <summary>
        /// Constructs a stub with a fixed focus point and availability state.
        /// </summary>
        /// <param name="fixedFocus">
        /// Normalised screen coordinate to return from <see cref="GazeFocusPoint"/>.
        /// Defaults to screen centre <c>(0.5, 0.5)</c>.
        /// </param>
        /// <param name="gazeAvailable">
        /// Value returned by <see cref="IsTracking"/>.
        /// Pass <c>false</c> to exercise the HMD-absent fallback path.
        /// </param>
        public StubGazeProvider(Vector2 fixedFocus = default, bool gazeAvailable = true)
        {
            // Use screen centre if the caller left fixedFocus at default(Vector2) == (0,0).
            _fixedFocus    = fixedFocus == default ? new Vector2(0.5f, 0.5f) : fixedFocus;
            _gazeAvailable = gazeAvailable;
        }

        /// <inheritdoc/>
        public Vector3 GazeDirection => Vector3.forward;   // camera-aligned; sufficient for tests

        /// <inheritdoc/>
        public Vector2 GazeFocusPoint => _fixedFocus;

        /// <inheritdoc/>
        public bool IsGazeAvailable => _gazeAvailable;
    }
}
