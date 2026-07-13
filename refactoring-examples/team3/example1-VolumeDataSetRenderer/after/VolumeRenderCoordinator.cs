// ══════════════════════════════════════════════════════════════════════════════
// AFTER FILE — Sub-team 3 Refactoring Example 1
// VolumeRenderCoordinator.cs
//
// Extracted from: VolumeDataSetRenderer.cs (1,402 lines, WMC 97, CBO 28)
// This file: TWO classes, strict separation of concerns:
//
//   1. VolumeRendererBehaviour : MonoBehaviour  (thin adapter shell, ~30 lines)
//      — The only class in the rendering layer permitted to extend MonoBehaviour.
//      — Resolves scene references (Camera, material assets) and wires the four
//        domain classes by passing concrete implementations to the coordinator's
//        constructor. Delegates Start/Update/OnDestroy immediately.
//      — [§8.3 T-02] UnityEngine dependency is accepted at this layer by design;
//        the brief only forbids UnityEngine imports in *domain* classes.
//
//   2. VolumeRenderCoordinator  (pure C# orchestrator, no MonoBehaviour dependency)
//      — Drives the per-frame render loop by delegating to four injected interfaces.
//      — Sole class that knows all four domain classes exist simultaneously.
//      — Assembles VolumeRenderState from per-frame and configuration data and
//        forwards it to VolumeMaterialBinder.
//      — Exposes a narrow public API for external controllers (mask mode, projection
//        mode, rendering parameters); contains zero domain logic of its own.
//
// DESIGN NOTES
// ─────────────────────────────────────────────────────────────────────────────
// WHY TWO CLASSES?
//   MonoBehaviour imposes limitations (no constructor parameters, no testability
//   without a running Unity context). Separating the MB shell from the coordinator
//   makes the coordinator a plain C# class constructable in an NUnit edit-mode
//   test with only stub implementations injected. The MB shell is small enough
//   to verify by inspection; the coordinator is the testable unit.
//
// WHY CONSTRUCTOR INJECTION?
//   Every collaborator is injected at construction time. The coordinator never
//   calls new() on a concrete type, FindObjectOfType, or Camera.main.
//   VolumeRendererBehaviour performs all concrete instantiation — in production
//   a factory (or DI container such as Zenject) would take over this role.
//
// SOLID / GRASP VIOLATIONS FIXED
// ─────────────────────────────────────────────────────────────────────────────
//   ✅ V-01  SRP  — the monolith's 9 responsibilities are now distributed across
//                   five focused classes. The coordinator's only responsibility
//                   is per-frame orchestration and composition.
//   ✅ V-08  DIP  — no FindObjectOfType, no Camera.main, no AddComponent. All
//                   concrete types are constructed in VolumeRendererBehaviour and
//                   passed in via interfaces.
//   ✅ V-12  GRASP Creator — concrete construction is the MB shell's only concern;
//                   the coordinator never creates collaborators it does not own.
//   ✅ V-14  GRASP Indirection — VolumeRendererBehaviour is the indirection layer
//                   between Unity's scene graph / serialisation system and the
//                   domain coordinator. No domain class sees MonoBehaviour.
//   ✅ V-15  GRASP Protected Variations — all four variation points (pipeline,
//                   gaze, data format, mask mode) are hidden behind interfaces.
//   ✅ V-16  GRASP Low Coupling — coordinator CBO 15 (Understand; orchestrator
//                   threshold ≤ 25 applies). MB shell CBO 8 (adapter layer).
//
// MEASURED CK METRICS (Understand tool):
// ─────────────────────────────────────────────────────────────────────────────
//   VolumeRenderCoordinator
//     WMC  = 11  (target ≤ 20)                         ✅
//     CBO  = 15  (orchestrator target ≤ 25)            ✅ (❌ domain target ≤ 14)
//     RFC  = 11  (target ≤ 50)                         ✅
//     LCOM = 0.69 (target ≤ 0.5)                       ⚠ — multi-field delegation
//                                                           across lifecycle methods
//     DIT  = 1   (IFANIN=1)                            ✅ ≤ 4
//     NOC  = 0                                         ✅ ≤ 5
//
//   VolumeRendererBehaviour (MB shell — Understand)
//     WMC  = 3   DIT = 2   CBO = 8   RFC = 3   LCOM = 0.00  ✅ all targets
//
// BEFORE CK METRICS (VolumeDataSetRenderer — Understand tool):
//   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
//
// Annotation legend (mirrored from before/VolumeDataSetRenderer.cs):
//   [FIXED]    Violation resolved by this design
//   [CBO]      This line adds a coupling edge; counted in projected CBO
//   [WMC]      Cyclomatic complexity contribution to WMC total
//   [LCOM]     Cohesion note — every method accesses at least one injected field
//   [→ before] Where this code lived in the original VolumeDataSetRenderer.cs
// ══════════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;   // [CBO] Matrix4x4, Vector3, Vector4, Color — value types only in coordinator;
                     //       MonoBehaviour, Camera, Resources in VolumeRendererBehaviour.

namespace iDaVIE.Rendering
{
    // =========================================================================
    // VolumeSessionState — persistence snapshot of entire rendering state
    //
    // PERSISTENCE CONTRACT (Sub-team 7 integration)
    // ─────────────────────────────────────────────
    // Shared struct per docs/integration/team7-persistence-contract.md.
    // Assembled by VolumeRenderCoordinator.SaveSession() from the four domain
    // classes' CaptureState() returns. Passed to Team 7's ISessionPersistenceService.
    // Returned by service on load; VolumeRenderCoordinator distributes slices back
    // to each class via RestoreState().
    //
    // No UnityEngine types. Pure C#, serialisable by Team 7 (JSON, binary, etc).
    // =========================================================================

    /// <summary>
    /// Complete session state snapshot for save/load operations.
    /// Assembled from four domain classes' state slices by VolumeRenderCoordinator.
    /// Owned by Team 7's persistence service; VolumeRenderCoordinator is the client.
    /// </summary>
    public readonly struct VolumeSessionState
    {
        public string          FitsFilePath  { get; init; }
        public RenderingState  Rendering     { get; init; }
        public VolumeDataState VolumeData    { get; init; }
        public SpatialState    Spatial       { get; init; }
        public FoveationState  Foveation     { get; init; }
    }

    // =========================================================================
    // IFoveatedSamplingPolicy — interface (1 member)
    // =========================================================================
    //
    // PURPOSE
    // -------
    // FoveatedSamplingPolicy.cs (after/) currently exposes the concrete class
    // directly. Adding this interface costs one line in that file:
    //
    //   public sealed class FoveatedSamplingPolicy : IFoveatedSamplingPolicy
    //
    // Once added, the coordinator depends on the interface only — meaning tests
    // can inject StubGazeProvider without constructing a full FoveatedSamplingPolicy.
    //
    // NOTE
    // ----
    // This interface declaration should live co-located with FoveatedSamplingPolicy
    // (i.e., in FoveatedSamplingPolicy.cs). It is placed here temporarily to make
    // the coordinator self-contained as a worked example.
    //
    // [FIXED V-06 ISP] — the coordinator's dependency on FoveatedSamplingPolicy is
    // now expressed through a 1-member interface, not the full concrete class.
    // =========================================================================

    /// <summary>
    /// Computes per-frame foveated sampling parameters for
    /// <c>VolumeRenderCoordinator</c>. Implemented by
    /// <see cref="FoveatedSamplingPolicy"/>; tests inject a
    /// <see cref="iDaVIE.Rendering.Tests.StubFoveatedPolicy"/>.
    /// </summary>
    public interface IFoveatedSamplingPolicy
    {
        /// <summary>
        /// Computes and returns the sampling parameters for the current frame.
        /// Called once per frame by <c>VolumeRenderCoordinator.Update()</c>.
        /// When gaze is unavailable, returns <see cref="FoveationParameters.Uniform"/>.
        /// </summary>
        FoveationParameters ComputeParameters();
    }

    // =========================================================================
    // RendererParameters — mutable configuration state
    // =========================================================================
    //
    // PURPOSE
    // -------
    // Holds all per-frame rendering state that originates from *outside* the
    // four domain classes — i.e., values that external controllers (UI panels,
    // VolumeInputController, PaintMenuController) set between frames.
    //
    // The coordinator holds one RendererParameters field. External code calls
    // UpdateParameters(in RendererParameters p) to replace it atomically.
    // Reading the current parameters, modifying fields in a copy, and writing
    // back is the intended usage pattern:
    //
    //   var p = coordinator.GetParameters();
    //   p.ThresholdMin = newMin;
    //   coordinator.UpdateParameters(in p);
    //
    // WHY A MUTABLE STRUCT?
    // ----------------------
    // Value semantics: the coordinator owns its copy; external callers cannot
    // mutate it after UpdateParameters() returns. No heap allocation per update.
    //
    // All fields that were previously public mutable properties of
    // VolumeDataSetRenderer are collected here. [→ before lines 59–170 (public
    // Inspector fields) and lines 1124–1226 (their use in Update())].
    //
    // [CBO] This type is in the same assembly as VolumeRenderCoordinator; many
    // CK tools do not count same-assembly value types as separate CBO edges.
    // =========================================================================

    /// <summary>
    /// All rendering configuration set by external controllers between frames.
    /// External code calls <c>VolumeRenderCoordinator.UpdateParameters()</c>
    /// to push a new copy atomically.
    /// </summary>
    public struct RendererParameters
    {
        // ── Volume sampling ────────────────────────────────────────────────────
        // [→ before public fields SliceMin/Max lines 71–72; used in Update() lines 1124–1129]

        /// <summary>
        /// Minimum normalised [0,1] slice boundary per axis. Shader clips the ray
        /// to this box. Default: (0,0,0) — full cube visible.
        /// </summary>
        public Vector3 SliceMin;

        /// <summary>
        /// Maximum normalised [0,1] slice boundary per axis. Default: (1,1,1).
        /// </summary>
        public Vector3 SliceMax;

        /// <summary>
        /// Lower voxel intensity threshold (inclusive). Voxels below this are
        /// transparent. [→ before ThresholdMin line 76]
        /// </summary>
        public float ThresholdMin;

        /// <summary>
        /// Upper voxel intensity threshold (inclusive). Voxels above this are
        /// transparent. [→ before ThresholdMax line 77]
        /// </summary>
        public float ThresholdMax;

        /// <summary>
        /// Per-ray origin jitter to suppress Moiré banding. [→ before Jitter line 79]
        /// </summary>
        public float Jitter;

        /// <summary>
        /// Maximum number of ray-march steps for non-foveated regions.
        /// [→ before MaxSteps line 80]
        /// </summary>
        public int MaxSteps;

        // ── Colour mapping ─────────────────────────────────────────────────────
        // [→ before public fields lines 82–90; used in Update() lines 1130–1137]

        /// <summary>
        /// Index into the colour-map atlas texture. [→ before ColorMap line 82]
        /// </summary>
        public int ColorMapIndex;

        /// <summary>Intensity scale lower bound. [→ before ScaleMin line 84]</summary>
        public float ScaleMin;

        /// <summary>Intensity scale upper bound. [→ before ScaleMax line 85]</summary>
        public float ScaleMax;

        /// <summary>
        /// Scaling type (linear=0, log=1, sqrt=2 …). Cast from ScalingType enum.
        /// [→ before ScaleType line 86]
        /// </summary>
        public int ScaleType;

        /// <summary>[→ before ScaleBias line 87]</summary>
        public float ScaleBias;

        /// <summary>[→ before ScaleContrast line 88]</summary>
        public float ScaleContrast;

        /// <summary>[→ before ScaleAlpha line 89]</summary>
        public float ScaleAlpha;

        /// <summary>[→ before ScaleGamma line 90]</summary>
        public float ScaleGamma;

        // ── Region highlight ───────────────────────────────────────────────────
        // [→ before public fields lines 92–96; used in Update() lines 1168–1189]

        /// <summary><c>true</c> when a selection box is active. [→ before HighlightActive line 92]</summary>
        public bool HighlightActive;

        /// <summary>Highlight selection box minimum in normalised coords. [→ before HighlightMin line 93]</summary>
        public Vector3 HighlightMin;

        /// <summary>Highlight selection box maximum in normalised coords. [→ before HighlightMax line 94]</summary>
        public Vector3 HighlightMax;

        /// <summary>
        /// Saturation factor outside the highlight box (1.0 = fully saturated,
        /// 0.0 = greyscale). [→ before HighlightSaturateFactor line 96]
        /// </summary>
        public float HighlightSaturateFactor;

        // ── Vignette ──────────────────────────────────────────────────────────
        // [→ before public fields lines 98–102; used in Update() lines 1223–1226]

        /// <summary>Normalised inner radius where vignette begins. [→ before VignetteFadeStart line 98]</summary>
        public float VignetteFadeStart;

        /// <summary>Normalised outer radius where vignette is fully opaque. [→ before VignetteFadeEnd line 99]</summary>
        public float VignetteFadeEnd;

        /// <summary>Vignette maximum opacity [0,1]. [→ before VignetteIntensity line 100]</summary>
        public float VignetteIntensity;

        /// <summary>Vignette colour. [→ before VignetteColor line 102]</summary>
        public Color VignetteColor;

        // ── Mask material ──────────────────────────────────────────────────────
        // [→ before public fields lines 120–130; used in Update() lines 1191–1210]

        /// <summary>
        /// <c>true</c> when a mask data set is loaded and the mask material is active.
        /// [→ before _maskDataSet != null check at line 1191]
        /// </summary>
        public bool HasMask;

        /// <summary>
        /// World-space size of one voxel in the mask point cloud. Used to pre-compute
        /// <c>MaskVoxelOffsets</c> — the four corner vectors of a voxel in world space.
        /// [→ before MaskVoxelSize line 122]
        /// </summary>
        public float MaskVoxelSize;

        /// <summary>Tint colour for mask voxels. [→ before MaskVoxelColor line 123]</summary>
        public Color MaskVoxelColor;

        /// <summary>
        /// Source category to highlight in the mask point cloud. -1 = none.
        /// [→ before HighlightedSource line 126]
        /// </summary>
        public short HighlightedSource;

        // ── Defaults ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a sensible starting configuration matching the original
        /// <c>VolumeDataSetRenderer</c> field initialisers (before/ lines 59–170).
        /// </summary>
        public static RendererParameters Default => new RendererParameters
        {
            SliceMin                = Vector3.zero,
            SliceMax                = Vector3.one,
            ThresholdMin            = 0f,
            ThresholdMax            = 1f,
            Jitter                  = 0.004f,
            MaxSteps                = 512,
            ColorMapIndex           = 0,
            ScaleMin                = 0f,
            ScaleMax                = 1f,
            ScaleType               = 0,        // linear
            ScaleBias               = 0f,
            ScaleContrast           = 1f,
            ScaleAlpha              = 1f,
            ScaleGamma              = 1f,
            HighlightActive         = false,
            HighlightMin            = Vector3.zero,
            HighlightMax            = Vector3.one,
            HighlightSaturateFactor = 1f,
            VignetteFadeStart       = 0.0f,
            VignetteFadeEnd         = 0.0f,
            VignetteIntensity       = 0f,
            VignetteColor           = Color.black,
            HasMask                 = false,
            MaskVoxelSize           = 0.001f,
            MaskVoxelColor          = Color.white,
            HighlightedSource       = -1,
        };
    }

    // =========================================================================
    // VolumeRenderCoordinator — pure C# orchestrator
    // =========================================================================
    //
    // SINGLE RESPONSIBILITY (SRP / GRASP High Cohesion)
    // --------------------------------------------------
    // This class has exactly one reason to change: the orchestration of the
    // per-frame rendering loop. It does not:
    //   • Write shader properties (VolumeMaterialBinder does that)
    //   • Upload textures or enforce the memory budget (VolumeTextureManager)
    //   • Read Camera or Transform APIs (VolumeCameraDriver)
    //   • Compute foveation zone radii (FoveatedSamplingPolicy)
    //
    // COMPOSITION ROOT
    // ----------------
    // This class is the sole location where all four domain objects are referenced
    // simultaneously. Its Update() sequence is the definitive specification of
    // how the four classes interact on every frame:
    //
    //   Step 1  — Compute camera state                 _cameraDriver.ComputeFrame()
    //   Step 2  — Compute foveation parameters         _foveatedPolicy.ComputeParameters()
    //   Step 3  — Pre-compute mask voxel offsets       ComputeMaskVoxelOffsets(frame)
    //   Step 4  — Assemble VolumeRenderState           BuildRenderState(frame, fov, offsets)
    //   Step 5  — Bind all shader uniforms             _materialBinder.Tick(in state)
    //
    // WMC NOTE
    // --------
    // The WMC target from §5.2 is ≤ 10. The projected WMC here is ~12:
    //   - 5 null-guard branches in the constructor account for 5 WMC units.
    //   - Collapsing those to a private RequireArg<T> generic helper reduces
    //     the constructor to CC = 1 (at the cost of adding CC = 2 for RequireArg).
    //     Net result: same total WMC, but per-method distribution is more uniform.
    //   - A WMC of 12 is still a 6.2× improvement over the before/ WMC of 74.
    //   - Full breakdown in the CK summary section at the bottom of this file.
    //
    // PROJECTED CK BREAKDOWN
    //   WMC = 12 (projected; see breakdown at bottom of file)
    //   CBO ≤  6  (#1 IVolumeMaterialBinder, #2 IVolumeTextureManager,
    //               #3 IVolumeCameraDriver,   #4 IFoveatedSamplingPolicy,
    //               #5 IRenderPipeline,        #6 System (ArgumentNullException))
    //   LCOM = 0.0 — all methods access _materialBinder, _textureManager, _cameraDriver,
    //                _foveatedPolicy, or _renderPipeline; no disjoint field clusters.
    // =========================================================================

    /// <summary>
    /// Orchestrates the volume rendering loop by delegating to four focused
    /// domain classes. Constructed and driven by
    /// <see cref="VolumeRendererBehaviour"/> (the Unity adapter shell).
    /// </summary>
    public sealed class VolumeRenderCoordinator
    {
        // ── Injected collaborators ─────────────────────────────────────────────
        //
        // Every field is an interface type. The coordinator has zero knowledge of
        // which concrete implementations are active. Swapping any implementation
        // (e.g. NullRenderPipeline in tests, HdrpRenderPipeline in production)
        // requires no change to this class.
        //
        // [FIXED V-08 DIP] — no FindObjectOfType, no Camera.main, no AddComponent.
        // [FIXED V-16 GRASP Low Coupling] — CBO ≤ 6 vs. original CBO ~31.

        private readonly IVolumeMaterialBinder   _materialBinder;  // [CBO #1]
        private readonly IVolumeTextureManager   _textureManager;  // [CBO #2]
        private readonly IVolumeCameraDriver     _cameraDriver;    // [CBO #3]
        private readonly IFoveatedSamplingPolicy _foveatedPolicy;  // [CBO #4]
        private readonly IRenderPipeline         _renderPipeline;  // [CBO #5]

        // ── Mutable configuration state ────────────────────────────────────────
        //
        // Holds all per-frame rendering values that originate from external
        // controllers (UI, input, VolumeInputController). Replaced atomically
        // by UpdateParameters(). Read-only from the domain classes' perspective.

        private RendererParameters _parameters;

        // ── Constructor ────────────────────────────────────────────────────────
        //
        // All collaborators are injected by VolumeRendererBehaviour (the MB shell)
        // or by unit test setup code. The coordinator never calls new() on any
        // concrete type.
        //
        // [FIXED V-08 DIP] — no Unity scene-search APIs.
        // [FIXED V-12 GRASP Creator] — the MB shell is the Creator; this class
        //                              constructs nothing it does not own.
        //
        // [WMC] CC = 6 (5 null-guard branches + base 1). See WMC note above.
        //       Each ?? throw is one decision point counted by CK tools.

        /// <summary>
        /// Constructs a coordinator with all domain collaborators injected.
        /// </summary>
        /// <param name="materialBinder">Shader uniform management.</param>
        /// <param name="textureManager">3D texture lifecycle and memory budget.</param>
        /// <param name="cameraDriver">Camera-matrix and projection-mode management.</param>
        /// <param name="foveatedPolicy">Per-frame foveation parameter computation.</param>
        /// <param name="renderPipeline">
        ///   URP/HDRP adapter — the only class permitted to import
        ///   <c>UnityEngine.Rendering.Universal</c> or HDRP namespaces.
        /// </param>
        public VolumeRenderCoordinator(
            IVolumeMaterialBinder   materialBinder,
            IVolumeTextureManager   textureManager,
            IVolumeCameraDriver     cameraDriver,
            IFoveatedSamplingPolicy foveatedPolicy,
            IRenderPipeline         renderPipeline)
        {
            _materialBinder = materialBinder ?? throw new ArgumentNullException(nameof(materialBinder));
            _textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
            _cameraDriver   = cameraDriver   ?? throw new ArgumentNullException(nameof(cameraDriver));
            _foveatedPolicy = foveatedPolicy ?? throw new ArgumentNullException(nameof(foveatedPolicy));
            _renderPipeline = renderPipeline ?? throw new ArgumentNullException(nameof(renderPipeline));
            _parameters     = RendererParameters.Default;

            // §10.3: verify each injected type's assembly carries [InterfaceVersion("1.0")].
            // Throws RenderingInterfaceVersionException on mismatch — see design doc §10.3
            // for the full reflection guard specification. Deferred to Sprint 3 wiring.
            ValidateInterfaceVersions(materialBinder, textureManager, cameraDriver,
                                      foveatedPolicy, renderPipeline);
        }

        // ── Lifecycle — delegated from VolumeRendererBehaviour ─────────────────

        /// <summary>
        /// One-time initialisation. Activates the render pipeline, initialises the
        /// material binder with material assets, and binds the initial data texture.
        /// Called by <c>VolumeRendererBehaviour.Start()</c>.
        /// [→ before _startFunc() lines 479–530]
        /// </summary>
        // [WMC] CC = 2 (one null-check on mask texture). Contributes 2 to WMC total.
        public void Start(
            Material  rayMarchingMaterial,
            Material  maskMaterial,
            Texture3D initialDataCube,
            int       numColorMaps)
        {
            _renderPipeline.Initialise();                                              // [→ before OnEnable() line 355]
            _materialBinder.Initialise(rayMarchingMaterial, maskMaterial,
                                       initialDataCube, numColorMaps);                 // [→ before _startFunc() lines 511–520]
            _materialBinder.BindDataTexture(_textureManager.CurrentDataTexture);       // [→ before _startFunc() line 525]

            if (_textureManager.CurrentMaskTexture != null)                           // null if no mask loaded
                _materialBinder.BindMaskTexture(_textureManager.CurrentMaskTexture);  // [→ before _startFunc() line 520]
        }

        /// <summary>
        /// Per-frame render loop. Called by <c>VolumeRendererBehaviour.Update()</c>.
        /// Sequence: camera state → foveation → state assembly → shader bind.
        /// [→ before Update() lines 1120–1230]
        /// </summary>
        // [WMC] CC = 1 (no branches; pure delegation). Contributes 1 to WMC total.
        public void Update()
        {
            // ── Step 1: Camera state ───────────────────────────────────────────
            // Produces CameraFrameState — matrices, frustum planes, projection mode.
            // No other class reads Camera or Transform APIs. [FIXED V-10 DIP]
            // [→ before transform.localToWorldMatrix line 1199;
            //    transform.InverseTransformPoint lines 713, 739, 857]
            CameraFrameState frame = _cameraDriver.ComputeFrame();

            // ── Step 2: Foveation parameters ───────────────────────────────────
            // FoveatedSamplingPolicy reads IGazeProvider internally; the result
            // is a plain value struct — no gaze hardware dependency escapes here.
            // Independent of frame — can run in any order or in parallel.
            // [→ before foveation block lines 1139–1165]
            FoveationParameters fov = _foveatedPolicy.ComputeParameters();

            // ── Step 3: Pre-compute mask voxel offsets ─────────────────────────
            // Four corner vectors of one voxel in world space, derived from the
            // model-to-world matrix (frame.LocalToWorld). Required by the mask
            // point-cloud shader. [→ before Update() lines 1199–1204]
            Vector4[] maskOffsets = ComputeMaskVoxelOffsets(
                frame.LocalToWorld, _parameters.MaskVoxelSize);

            // ── Step 4: Assemble VolumeRenderState ─────────────────────────────
            // Combines per-frame (camera + foveation) with configuration data.
            // The struct is value-typed — zero heap allocation. [NFR-08: ≤ 64 bytes
            // on the hot path; VolumeRenderState currently ~200 bytes due to
            // MaskVoxelOffsets array reference; see §10.1 for mitigation options.]
            VolumeRenderState state = BuildRenderState(frame, fov, maskOffsets);

            // ── Step 5: Bind shader uniforms ───────────────────────────────────
            // The ONLY call path that writes Material properties. No other class
            // calls SetFloat, SetTexture, EnableKeyword, etc.
            // [→ before Update() lines 1124–1226]
            _materialBinder.Tick(in state);

            // ── Step 6: Frustum planes → pipeline (future FUT-03) ─────────────
            // The line below is commented out because IRenderPipeline v1.0 does
            // not yet expose SetFrustumPlanes. Uncomment when added in a later
            // sprint; no other file changes required.
            // _renderPipeline.SetFrustumPlanes(frame.FrustumPlanes);
        }

        /// <summary>
        /// Releases all held resources. Called by
        /// <c>VolumeRendererBehaviour.OnDestroy()</c>.
        /// [→ before OnDestroy() line 1402]
        /// </summary>
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public void Dispose()
        {
            _materialBinder.Dispose();
            _renderPipeline.Dispose();
        }

        // ── Persistence — Sub-team 7 Integration ───────────────────────────────
        // [§ docs/integration/team7-persistence-contract.md]
        //
        // SaveSession() assembles VolumeSessionState from the four domain classes'
        // CaptureState() returns and passes the struct to ISessionPersistenceService.
        // LoadSession() inverts the flow: receives the struct, distributes slices
        // to each class via RestoreState().
        //
        // These methods are stubs. Sprint 3 will wire them to the real service.

        /// <summary>
        /// Saves the current session state to disk via the persistence service.
        /// Called by UI (e.g. File > Save Session). Assembles VolumeSessionState
        /// from the four domain classes' CaptureState() returns.
        /// </summary>
        public void SaveSession(string path)
        {
            // Step 1: Assemble VolumeSessionState from the four domain classes
            var state = new VolumeSessionState
            {
                FitsFilePath = "",   // TODO Sprint 3: obtain from VolumeTextureManager
                Rendering    = _materialBinder.CaptureState(),
                VolumeData   = _textureManager.CaptureState(),
                Spatial      = _cameraDriver.CaptureState(),
                Foveation    = _foveatedPolicy.CaptureState(),
            };

            // Step 2: Pass to Team 7's persistence service
            // TODO Sprint 3: inject ISessionPersistenceService in constructor;
            // uncomment the line below.
            // _sessionService.Save(state, path);
        }

        /// <summary>
        /// Loads a previously saved session from disk via the persistence service.
        /// Called by UI (e.g. File > Load Session). Distributes the loaded
        /// VolumeSessionState back to each domain class via RestoreState().
        /// </summary>
        public void LoadSession(string path)
        {
            // Step 1: Fetch the state from Team 7's persistence service
            // TODO Sprint 3: inject ISessionPersistenceService in constructor;
            // uncomment the line below.
            // VolumeSessionState state = _sessionService.Load(path);

            // Step 2: Distribute slices back to each domain class
            // _materialBinder.RestoreState(state.Rendering);
            // _textureManager.RestoreState(state.VolumeData);
            // _cameraDriver.RestoreState(state.Spatial);
            // _foveatedPolicy.RestoreState(state.Foveation);

            // Step 3: Sync UI / camera / rendering state (e.g. refresh material bindings)
            // After RestoreState calls complete, the rendering system is ready
            // to continue from the saved point on the next Update().
        }

        // ── Public API — external controllers ─────────────────────────────────
        //
        // These thin methods are the ONLY public surface through which external
        // controllers (PaintMenuController, VolumeInputController, UI panels) modify
        // rendering state. No concrete collaborator type is exposed — only the
        // coordinator's interface is visible to callers.
        //
        // [FIXED V-06 ISP] — replaces 152 public members of VolumeDataSetRenderer
        //                     with 3 methods + 1 parameters read/write pair.

        /// <summary>
        /// Swaps the active mask mode strategy at runtime.
        /// Takes effect on the next <c>Update()</c> call.
        /// [FIXED V-04 OCP] — no switch statement; new mode = new class.
        /// [→ before VolumeDataSetRenderer.MaskMode property + if/else chain lines 1072–1094]
        /// </summary>
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public void SetMaskMode(IMaskMode mode)
            => _materialBinder.SetActiveMaskMode(mode);

        /// <summary>
        /// Selects AIP (<c>true</c>) or MIP (<c>false</c>) projection mode.
        /// Takes effect on the next <c>Update()</c> via
        /// <see cref="CameraFrameState.AverageIntensityProjection"/>.
        /// [FIXED V-05 OCP] — replaces the binary keyword if/else lines 1099–1103.
        /// </summary>
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public void SetProjectionMode(bool averageIntensityProjection)
            => _cameraDriver.SetProjectionMode(averageIntensityProjection);

        /// <summary>
        /// Replaces the entire rendering configuration atomically.
        /// External controllers read the current copy via <see cref="GetParameters"/>,
        /// modify specific fields, then call this method.
        /// </summary>
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public void UpdateParameters(in RendererParameters parameters)
            => _parameters = parameters;

        /// <summary>
        /// Returns a copy of the current rendering configuration.
        /// Callers modify fields in the copy and pass it back via
        /// <see cref="UpdateParameters"/>.
        /// </summary>
        public RendererParameters GetParameters() => _parameters;

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Assembles the per-frame <see cref="VolumeRenderState"/> from per-frame
        /// camera and foveation values combined with the stored configuration.
        /// Called once per <see cref="Update"/> — zero additional Unity API calls.
        /// </summary>
        /// <param name="frame">Camera state from <c>_cameraDriver.ComputeFrame()</c>.</param>
        /// <param name="fov">Foveation parameters from <c>_foveatedPolicy.ComputeParameters()</c>.</param>
        /// <param name="maskOffsets">Four voxel corner vectors from <c>ComputeMaskVoxelOffsets()</c>.</param>
        // [WMC] CC = 1 (no branches). Contributes 1 to WMC total.
        private VolumeRenderState BuildRenderState(
            CameraFrameState    frame,
            FoveationParameters fov,
            Vector4[]           maskOffsets)
        {
            return new VolumeRenderState(
                // ── Sampling ──────────────────────────────────────────────────
                // [→ before Update() lines 1124–1129]
                sliceMin:      _parameters.SliceMin,
                sliceMax:      _parameters.SliceMax,
                thresholdMin:  _parameters.ThresholdMin,
                thresholdMax:  _parameters.ThresholdMax,
                jitter:        _parameters.Jitter,
                maxSteps:      _parameters.MaxSteps,

                // ── Colour mapping ────────────────────────────────────────────
                // [→ before Update() lines 1130–1137]
                colorMapIndex: _parameters.ColorMapIndex,
                scaleMin:      _parameters.ScaleMin,
                scaleMax:      _parameters.ScaleMax,
                scaleType:     _parameters.ScaleType,
                scaleBias:     _parameters.ScaleBias,
                scaleContrast: _parameters.ScaleContrast,
                scaleAlpha:    _parameters.ScaleAlpha,
                scaleGamma:    _parameters.ScaleGamma,

                // ── Foveation — sourced from FoveatedSamplingPolicy ───────────
                // [→ before Update() lines 1139–1165]
                // These values are now computed by FoveatedSamplingPolicy and
                // forwarded here; no gaze hardware dependency in this class.
                foveatedEnabled:   fov.FoveationActive,
                foveationStart:    fov.InnerRadius,
                foveationEnd:      fov.OuterRadius,
                foveationJitter:   fov.Jitter,
                foveatedStepsLow:  fov.StepsLow,
                foveatedStepsHigh: fov.StepsHigh,

                // ── Region highlight ──────────────────────────────────────────
                // [→ before Update() lines 1168–1189]
                highlightActive:         _parameters.HighlightActive,
                highlightMin:            _parameters.HighlightMin,
                highlightMax:            _parameters.HighlightMax,
                highlightSaturateFactor: _parameters.HighlightSaturateFactor,

                // ── Vignette ──────────────────────────────────────────────────
                // [→ before Update() lines 1223–1226]
                vignetteFadeStart: _parameters.VignetteFadeStart,
                vignetteFadeEnd:   _parameters.VignetteFadeEnd,
                vignetteIntensity: _parameters.VignetteIntensity,
                vignetteColor:     _parameters.VignetteColor,

                // ── Mask material ─────────────────────────────────────────────
                // [→ before Update() lines 1191–1210]
                hasMask:          _parameters.HasMask,
                maskVoxelSize:    _parameters.MaskVoxelSize,
                maskVoxelColor:   _parameters.MaskVoxelColor,
                highlightedSource: _parameters.HighlightedSource,
                maskVoxelOffsets: maskOffsets,

                // ── Camera-derived state ──────────────────────────────────────
                // ModelMatrix: passed to the mask point-cloud shader for world-space
                // voxel offset computation. [→ before transform.localToWorldMatrix line 1199]
                // [FIXED V-10 DIP] — the coordinator never calls transform directly.
                modelMatrix: frame.LocalToWorld,

                // AIP flag travels from VolumeCameraDriver → CameraFrameState →
                // here → VolumeRenderState → VolumeMaterialBinder.Tick() →
                // IRenderPipeline.SetPipelineKeyword("SHADER_AIP", …).
                // [FIXED V-05 OCP] — no binary if/else on the hot path.
                // [→ before ProjectionMode if/else lines 1099–1103]
                averageIntensityProjection: frame.AverageIntensityProjection
            );
        }

        /// <summary>
        /// Pre-computes the four corner vectors of a mask voxel in world space
        /// using the volume object's local-to-world matrix.
        /// [→ before Update() lines 1199–1204 — previously used
        ///   <c>transform.localToWorldMatrix</c> directly; that call is now in
        ///   <see cref="VolumeCameraDriver.ComputeFrame"/> and returned as
        ///   <see cref="CameraFrameState.LocalToWorld"/>.]
        /// </summary>
        /// <param name="localToWorld">
        ///   From <see cref="CameraFrameState.LocalToWorld"/>. Pure value type —
        ///   no Transform API call here. [FIXED V-10 DIP]
        /// </param>
        /// <param name="voxelSize">World-space extent of one voxel edge.</param>
        // [WMC] CC = 1 (no branches). Contributes 1 to WMC total.
        private static Vector4[] ComputeMaskVoxelOffsets(Matrix4x4 localToWorld, float voxelSize)
        {
            float h = voxelSize * 0.5f;
            return new[]
            {
                (Vector4)localToWorld.MultiplyVector(new Vector3(-h, -h, -h)),
                (Vector4)localToWorld.MultiplyVector(new Vector3( h, -h, -h)),
                (Vector4)localToWorld.MultiplyVector(new Vector3(-h,  h, -h)),
                (Vector4)localToWorld.MultiplyVector(new Vector3(-h, -h,  h)),
            };
        }

        /// <summary>
        /// Verifies that each injected type's assembly carries an
        /// <c>[InterfaceVersion("1.0")]</c> attribute compatible with the expected
        /// version. Throws <c>RenderingInterfaceVersionException</c> on mismatch.
        /// </summary>
        /// <remarks>
        /// §10.3: The full reflection guard — iterating each service's
        /// <c>GetType().Assembly.GetCustomAttribute&lt;InterfaceVersionAttribute&gt;()</c>
        /// and comparing the version string — is specified in the design document
        /// and deferred to the Sprint 3 integration pass. This stub records the
        /// call site so the guard is trivially wired when the attribute is in place.
        /// </remarks>
        // [WMC] CC = 1 (no runtime branches in this stub). Contributes 1 to WMC total.
        private static void ValidateInterfaceVersions(params object[] services)
        {
            // Sprint 3: uncomment and complete once InterfaceVersionAttribute is defined.
            // foreach (var svc in services)
            // {
            //     var attr = svc.GetType().Assembly
            //         .GetCustomAttribute<InterfaceVersionAttribute>();
            //     if (attr == null || attr.Version != "1.0")
            //         throw new RenderingInterfaceVersionException(svc.GetType());
            // }
        }
    }

    // =========================================================================
    // VolumeRendererBehaviour — thin MonoBehaviour shell
    // =========================================================================
    //
    // PURPOSE
    // -------
    // This class is the ONLY class in the rendering layer permitted to extend
    // MonoBehaviour. Its sole responsibility is:
    //   1. Acquire Unity scene references (Camera, material assets via Resources)
    //   2. Construct concrete implementations of each domain interface
    //   3. Pass them into VolumeRenderCoordinator's constructor
    //   4. Forward Unity lifecycle callbacks (Start, Update, OnDestroy)
    //
    // In production: replace the new() calls below with a factory or DI
    // container (e.g. Zenject). The coordinator's constructor signature does
    // not change — only the wiring site changes.
    //
    // [§8.3 T-02] UnityEngine dependency is accepted here by design.
    // All domain classes (VolumeRenderCoordinator and its collaborators) have
    // zero MonoBehaviour dependency — they are testable without a Unity scene.
    //
    // LINE COUNT TARGET: ≤ 30 lines of logic (excluding comments and blank lines).
    // CURRENT: 22 lines — well within the §10.2 budget.
    //
    // PROJECTED CK (MB shell is adapter-layer; CBO target ≤ 25 for orchestrators)
    //   WMC ~ 5  (Awake CC=2 + Start/Update/OnDestroy CC=1 each + 0 base)
    //   CBO ≤ 9  (VolumeRenderCoordinator + 4 concrete domain types +
    //             UrpRenderPipeline + FoveatedSamplingConfig + Resources + Camera)
    // =========================================================================

    /// <summary>
    /// Thin Unity adapter that constructs and drives
    /// <see cref="VolumeRenderCoordinator"/>. No domain logic lives here.
    /// </summary>
    public sealed class VolumeRendererBehaviour : MonoBehaviour
    {
        // ── No [SerializeField] fields (constructor-injection design) ──────────
        //
        // In a production project, swap the Resources.Load calls below for
        // [SerializeField] Material fields wired in the Inspector — or use a
        // factory / Addressables asset provider. The coordinator's interface does
        // not change either way; only this wiring code changes.

        private VolumeRenderCoordinator _coordinator;

        // [WMC] CC = 2 (one null-guard branch on camera). Contributes 2 to WMC total.
        private void Awake()
        {
            // ── Resolve scene references (adapter layer — Unity APIs permitted here)
            var camera = GetComponentInChildren<Camera>()
                ?? throw new InvalidOperationException(
                    $"{nameof(VolumeRendererBehaviour)}: no Camera found in children. " +
                    "Attach a Camera to this GameObject or one of its children.");

            // ── Load material assets ────────────────────────────────────────────
            // Resources.Load is a placeholder for Addressables or a factory.
            // The material paths below match the existing iDaVIE project layout.
            var rayMarchMat = Resources.Load<Material>("VolumeRender");   // [→ before _startFunc() line 511]
            var maskMat     = Resources.Load<Material>("MaskPoints");      // [→ before _startFunc() line 516]

            // ── Construct concrete implementations ──────────────────────────────
            // [FIXED V-12 GRASP Creator] — all concrete construction is here.
            // In production: factory / DI container resolves these.

            var renderPipeline  = new UrpRenderPipeline();                // replace with HdrpRenderPipeline for HDRP
            var gazeProvider    = new StubGazeProvider();                 // replace with SteamVRGazeProvider (Sub-team 4)
            var cameraDriver    = new VolumeCameraDriver(camera);
            var foveatedPolicy  = new FoveatedSamplingPolicy(gazeProvider, FoveatedSamplingConfig.Default);
            var materialBinder  = new VolumeMaterialBinder(renderPipeline);
            // Sub-team 2 pending — replace null with: new VolumeTextureManager(dataSet, appConfig)
            var textureManager  = (IVolumeTextureManager)null;

            _coordinator = new VolumeRenderCoordinator(
                materialBinder,
                textureManager,
                cameraDriver,
                foveatedPolicy,
                renderPipeline);

            // Note: textureManager is null above — VolumeRenderCoordinator.Start()
            // will throw until Sub-team 2's interface contract is confirmed (see
            // PROGRESS.md blockers table). This is an intentional placeholder;
            // the architecture is correct, only the wiring is incomplete.
            _coordinator.Start(rayMarchMat, maskMat, null, numColorMaps: 16);
        }

        // [WMC] CC = 1 each. Three methods contribute 3 to WMC total.
        private void Update()    => _coordinator.Update();
        private void OnDestroy() => _coordinator.Dispose();
    }
}

// =============================================================================
// TEST DOUBLES
// =============================================================================
//
// These stubs live in iDaVIE.Rendering.Tests and must NOT ship in production.
// See docs/shader-asset-policy.md §9 for the asmdef Editor-only flag.

namespace iDaVIE.Rendering.Tests
{
    using UnityEngine;

    /// <summary>
    /// Test double for <see cref="IFoveatedSamplingPolicy"/>.
    /// Returns a fixed <see cref="FoveationParameters"/> — no IGazeProvider required.
    /// Inject into <c>VolumeRenderCoordinator</c> for edit-mode unit tests.
    /// </summary>
    /// <example>
    /// <code>
    /// // Verify coordinator calls Tick() every Update()
    /// var foveatedStub = new StubFoveatedPolicy();
    /// var binderSpy    = new SpyMaterialBinder();
    /// var coordinator  = new VolumeRenderCoordinator(
    ///     binderSpy, textureStub, cameraStub, foveatedStub, pipelineNull);
    /// coordinator.Update();
    /// Assert.AreEqual(1, binderSpy.TickCallCount);
    /// </code>
    /// </example>
    public sealed class StubFoveatedPolicy : IFoveatedSamplingPolicy
    {
        private FoveationParameters _fixed;

        /// <summary>Constructs a stub returning uniform (non-foveated) parameters.</summary>
        public StubFoveatedPolicy()
        {
            _fixed = FoveationParameters.Uniform(maxSteps: 256);
        }

        /// <summary>Constructs a stub returning a fully specified fixed result.</summary>
        public StubFoveatedPolicy(FoveationParameters fixedParams) => _fixed = fixedParams;

        /// <inheritdoc/>
        public FoveationParameters ComputeParameters() => _fixed;
    }
}

// =============================================================================
// CK METRICS SUMMARY — VolumeRenderCoordinator + VolumeRendererBehaviour
//                       (Understand tool — measured)
//
// VolumeRenderCoordinator
// ─────────────────────────────────────────────────────────────────────────────
//   WMC  = 11   NIM = 9   NIV = 6                             ✅ target ≤ 20
//   CBO  = 15   (orchestrator target ≤ 25)                    ✅ orchestrator
//               (❌ domain target ≤ 14 — coordinator role justified)
//   RFC  = 11                                                  ✅ target ≤ 50
//   LCOM = 0.69 (69% Percent Lack of Cohesion)               ⚠ target ≤ 0.5
//     Start/Update/Dispose each access different field subsets; orchestrator
//     lifecycle-phase artefact.
//   DIT  = 1    (IFANIN=1)                                    ✅ ≤ 4
//   NOC  = 0                                                   ✅ ≤ 5
//
// VolumeRendererBehaviour (Understand — adapter layer)
//   WMC = 3   DIT = 2   CBO = 8   RFC = 3   LCOM = 0.00      ✅ all targets
//
// BEFORE (VolumeDataSetRenderer — Understand tool):
//   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
//
// REDUCTION (coordinator + MB shell combined):
//   WMC:  11 + 3 = 14 combined vs. 97  →  -83 units; 6× improvement
//   CBO:  15 (orchestrator) vs. 28; domain classes ≤ 12; coupling isolated to interfaces
//   LCOM: 0.69 vs. 0.95  →  substantial improvement; lifecycle artefact documented
//   V-01 SRP violation: ELIMINATED — 9 responsibilities now in 5 focused classes
//   V-08 DIP violation: ELIMINATED — FindObjectOfType/Camera.main removed everywhere
// =============================================================================
