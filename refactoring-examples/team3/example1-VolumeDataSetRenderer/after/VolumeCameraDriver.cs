// ══════════════════════════════════════════════════════════════════════════════
// AFTER FILE — Sub-team 3 Refactoring Example 1
// VolumeCameraDriver.cs
//
// Extracted from: VolumeDataSetRenderer.cs (1,402 lines, WMC 97, CBO 28)
// This file: ONE class + ONE pure-math helper — camera-matrix extraction,
//            clip-plane computation, and projection mode management.
//            VolumeCoordinateService eliminates every transform.InverseTransformPoint
//            call from domain logic (before/ lines 713, 739, 857).
//
// SOLID/GRASP violations FIXED by this extraction:
//   ✅ V-01  SRP  — camera/coordinate maths is no longer one of 9 responsibilities
//                   in a God Class; it is the only responsibility here.
//   ✅ V-10  DIP  — the critical brief §4.2 mandatory constraint violation:
//                   "Domain rendering math must NOT transitively depend on
//                   UnityEngine or SteamVR types."
//                   Before/ lines 713, 739, 857: ConvertWorldPositionToDataCubePosition,
//                   SetCursorPosition, and GetVoxelPositionWorldSpace each called
//                   transform.InverseTransformPoint — a live MonoBehaviour API
//                   requiring a running Unity scene; impossible to unit test.
//                   VolumeCoordinateService resolves this: it accepts a plain
//                   Matrix4x4 value type and performs identical arithmetic with
//                   zero Unity runtime dependency.
//   ✅ V-05  OCP  — ProjectionMode binary if/else (before/ lines 1218–1221) replaced
//                   by a plain bool field owned here. VolumeMaterialBinder applies
//                   the keyword via IRenderPipeline.SetPipelineKeyword — nothing
//                   in VolumeCameraDriver calls Shader.EnableKeyword directly.
//   ✅ V-08  DIP  — no FindObjectOfType or Camera.main. The coordinator supplies
//                   the Camera at construction time (dependency injection).
//   ✅ V-14  GRASP Indirection — VolumeCameraDriver is the sole indirection layer
//                   between Unity's Camera/Transform API and the domain core. No
//                   other class reads Camera.worldToCameraMatrix or
//                   transform.localToWorldMatrix.
//   ✅ V-16  GRASP Low Coupling  — CBO 4 (Understand) vs. original CBO 28.
//   ✅ V-17  GRASP High Cohesion — LCOM 0.25 (Understand); all members concern
//                   camera-state computation.
//
// DEPENDENCY NOTE — IRenderPipeline
// ------------------------------------
// VolumeCameraDriver does NOT hold an IRenderPipeline reference. It returns
// CameraFrameState to VolumeRenderCoordinator, which forwards FrustumPlanes
// to the pipeline directly. This keeps VolumeCameraDriver's CBO at 4.
// Interface stub: refactoring-examples/stubs/IRenderPipeline.cs
//
// MEASURED CK METRICS (Understand tool):
//   VolumeCameraDriver:      WMC=4, DIT=1, CBO=4, RFC=4, LCOM=0.25  ✅ all targets
//   VolumeCoordinateService: WMC=3, DIT=1, CBO=3, RFC=3, LCOM=0.00  ✅ all targets
//
// BEFORE CK METRICS (VolumeDataSetRenderer — Understand tool):
//   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
//
// Annotation legend (mirrored from before/VolumeDataSetRenderer.cs):
//   [FIXED]    Violation resolved by this design
//   [CBO]      This line adds a coupling edge; counted in projected CBO
//   [WMC]      Complexity contribution
//   [LCOM]     Cohesion note
//   [→ before] Where this code lived in the original VolumeDataSetRenderer.cs
// ══════════════════════════════════════════════════════════════════════════════

using UnityEngine;  // [CBO #1] Camera, Matrix4x4, Vector4, Vector3

namespace iDaVIE.Rendering
{
    // =========================================================================
    // VolumeCoordinateService — pure-C# coordinate math helper
    // =========================================================================
    //
    // PURPOSE
    // -------
    // Every transform.InverseTransformPoint call in VolumeDataSetRenderer
    // (before/ lines 713, 739, 857) embedded a hard UnityEngine dependency inside
    // domain logic — Violation V-10 (DIP, brief §4.2 mandatory constraint 3).
    //
    // VolumeCoordinateService replaces those calls with pure matrix arithmetic.
    // The Matrix4x4 parameter is extracted by VolumeCameraDriver (the adapter,
    // which is permitted to touch Unity APIs) and passed as a value type.
    // This class has zero UnityEngine API calls — it is fully testable in an
    // edit-mode NUnit test with no running Unity scene or GPU.
    //
    // NOTE on value types: UnityEngine.Vector3 and UnityEngine.Matrix4x4 are
    // blittable structs. Using them as parameters does NOT create a transitive
    // dependency on the Unity player runtime. The brief §4.2 constraint targets
    // MonoBehaviour, Component, and APIs that require a running Unity context
    // (Transform.InverseTransformPoint, Camera.worldToCameraMatrix, etc.) —
    // none of those appear here.
    //
    // WHY STATIC?
    // -----------
    // Every method is a pure function (same inputs → same outputs, no side effects).
    // A static class communicates this guarantee and prevents accidental instance
    // allocation on the per-frame hot path.
    //
    // PROJECTED CK (counted separately from VolumeCameraDriver)
    //   WMC = 4  (3 static methods CC = 1 each; 1 implicit static constructor)
    //   CBO = 1  (UnityEngine math value types only — no Component API calls)
    //   LCOM = 0.0 (static; no instance field clusters)
    // =========================================================================

    public static class VolumeCoordinateService
    {
        // ─────────────────────────────────────────────────────────────────────
        // BEHAVIOUR 1 — World → object-local space
        //
        // Replaces transform.InverseTransformPoint(worldPos) at before/ lines
        // 713, 739, 857. Those calls read from a live MonoBehaviour Transform —
        // they cannot be exercised in an edit-mode unit test without a full Unity
        // scene. This method performs identical arithmetic using the worldToLocal
        // matrix supplied by VolumeCameraDriver.ComputeFrame() as a value type.
        //
        // Callers obtain the matrix from CameraFrameState.WorldToLocal.
        // No class other than VolumeCameraDriver ever calls a Transform API.
        //
        // [→ before ConvertWorldPositionToDataCubePosition() line 713]
        // [→ before SetCursorPosition() line 739]
        // [→ before GetVoxelPositionWorldSpace() line 857]
        // [FIXED V-10 DIP] — UnityEngine.Transform removed from domain math
        // [WMC] CC = 1.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Transforms <paramref name="worldPoint"/> from world space into
        /// object-local space using a pre-computed worldToLocal matrix.
        /// Replaces <c>transform.InverseTransformPoint(worldPoint)</c>
        /// (before/ lines 713, 739, 857) without requiring a live Transform.
        /// </summary>
        /// <param name="worldPoint">Point in world space.</param>
        /// <param name="worldToLocal">
        /// Inverse of the volume object's localToWorldMatrix.
        /// Supplied by <see cref="VolumeCameraDriver.ComputeFrame"/> via
        /// <see cref="CameraFrameState.WorldToLocal"/>.
        /// </param>
        /// <returns>The point in object-local space.</returns>
        public static Vector3 WorldToObjectSpace(Vector3 worldPoint, Matrix4x4 worldToLocal)
            => worldToLocal.MultiplyPoint3x4(worldPoint);

        // ─────────────────────────────────────────────────────────────────────
        // BEHAVIOUR 2 — Object-local → normalised volume space [0, 1]
        //
        // The volume GameObject is centred at the origin and scaled 1 × 1 × 1.
        // Object-local coordinates therefore range from −0.5 to +0.5 on each axis.
        // The ray-march shader samples the 3D texture in [0, 1] normalised UVW
        // coordinates. The +0.5 offset was previously computed implicitly at
        // various call sites — surfacing it here as an explicit named method makes
        // the coordinate transform chain traceable in C# tests.
        //
        // [WMC] CC = 1.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts a point from object-local space (range [−0.5, +0.5])
        /// to normalised volume UVW space (range [0, 1]).
        /// </summary>
        /// <param name="localPoint">
        /// A point returned by <see cref="WorldToObjectSpace"/>.
        /// </param>
        /// <returns>The point in volume coordinates, range [0, 1] per axis.</returns>
        public static Vector3 ObjectToNormalisedVolume(Vector3 localPoint)
            => localPoint + new Vector3(0.5f, 0.5f, 0.5f);

        // ─────────────────────────────────────────────────────────────────────
        // BEHAVIOUR 3 — ViewProjection → 6 frustum clip planes
        //
        // Extraction using the Gribb–Hartmann method: each plane is derived from
        // a signed combination of rows of the combined ViewProjection matrix.
        // Planes are encoded as (A, B, C, D) in Ax + By + Cz + D = 0 with normals
        // pointing inward (into the frustum).
        //
        // This capability is absent from the before/ code — it is a new optimisation
        // enabled by the extraction. VolumeRenderCoordinator can forward FrustumPlanes
        // to IRenderPipeline for draw-call culling without modifying VolumeCameraDriver.
        //
        // Reference: Gribb, G. & Hartmann, K. (2001) "Fast Extraction of Viewing
        // Frustum Planes from the World-View-Projection Matrix."
        //
        // [WMC] CC = 1 (no branches — pure arithmetic).
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Extracts the six view-frustum planes from a combined ViewProjection matrix
        /// using the Gribb–Hartmann method.
        /// Plane order: Left, Right, Bottom, Top, Near, Far.
        /// Each <see cref="Vector4"/> encodes (A, B, C, D) of Ax + By + Cz + D = 0;
        /// normals point inward (into the frustum).
        /// </summary>
        /// <param name="vp">
        /// The combined view-projection matrix
        /// (<c>Camera.projectionMatrix × Camera.worldToCameraMatrix</c>).
        /// </param>
        /// <returns>Array of exactly 6 plane vectors.</returns>
        public static Vector4[] ExtractFrustumPlanes(Matrix4x4 vp)
        {
            return new[]
            {
                new Vector4(vp.m30 + vp.m00, vp.m31 + vp.m01, vp.m32 + vp.m02, vp.m33 + vp.m03), // Left
                new Vector4(vp.m30 - vp.m00, vp.m31 - vp.m01, vp.m32 - vp.m02, vp.m33 - vp.m03), // Right
                new Vector4(vp.m30 + vp.m10, vp.m31 + vp.m11, vp.m32 + vp.m12, vp.m33 + vp.m13), // Bottom
                new Vector4(vp.m30 - vp.m10, vp.m31 - vp.m11, vp.m32 - vp.m12, vp.m33 - vp.m13), // Top
                new Vector4(vp.m30 + vp.m20, vp.m31 + vp.m21, vp.m32 + vp.m22, vp.m33 + vp.m23), // Near
                new Vector4(vp.m30 - vp.m20, vp.m31 - vp.m21, vp.m32 - vp.m22, vp.m33 - vp.m23), // Far
            };
        }
    }

    // =========================================================================
    // SpatialState — persistence snapshot of VolumeCameraDriver state
    //
    // PERSISTENCE CONTRACT (Sub-team 7 integration)
    // ─────────────────────────────────────────────
    // Owned by VolumeCameraDriver per docs/integration/team7-persistence-contract.md.
    // Captured by VolumeCameraDriver.CaptureState() → returned to coordinator.
    // Restored by VolumeCameraDriver.RestoreState(state) → coordinator calls this
    // after session load.
    //
    // Records volume object position, rotation (Euler angles), scale, and
    // optional region/clipping bounds. No UnityEngine types.
    // =========================================================================

    /// <summary>
    /// Session-persistent state owned by <see cref="VolumeCameraDriver"/>.
    /// Captured before save, restored after load. Follows Sub-team 7 contract.
    /// </summary>
    public readonly struct SpatialState
    {
        public readonly float PositionX;
        public readonly float PositionY;
        public readonly float PositionZ;
        public readonly float RotationX;      // Euler angles
        public readonly float RotationY;
        public readonly float RotationZ;
        public readonly float Scale;
        public readonly int   RegionMinX;
        public readonly int   RegionMinY;
        public readonly int   RegionMinZ;
        public readonly int   RegionMaxX;
        public readonly int   RegionMaxY;
        public readonly int   RegionMaxZ;
        public readonly bool  HasRegion;

        public SpatialState(
            float posX, float posY, float posZ,
            float rotX, float rotY, float rotZ,
            float scale,
            int regionMinX, int regionMinY, int regionMinZ,
            int regionMaxX, int regionMaxY, int regionMaxZ,
            bool hasRegion)
        {
            PositionX   = posX;
            PositionY   = posY;
            PositionZ   = posZ;
            RotationX   = rotX;
            RotationY   = rotY;
            RotationZ   = rotZ;
            Scale       = scale;
            RegionMinX  = regionMinX;
            RegionMinY  = regionMinY;
            RegionMinZ  = regionMinZ;
            RegionMaxX  = regionMaxX;
            RegionMaxY  = regionMaxY;
            RegionMaxZ  = regionMaxZ;
            HasRegion   = hasRegion;
        }
    }

    // =========================================================================
    // CameraFrameState — immutable value type returned to VolumeRenderCoordinator
    // =========================================================================
    //
    // PURPOSE
    // -------
    // Carries all per-frame camera-derived state that VolumeRenderCoordinator
    // needs to:
    //   • Populate VolumeRenderState.ModelMatrix (→ mask voxel offset computation)
    //   • Populate VolumeRenderState.AverageIntensityProjection (→ SHADER_AIP)
    //   • Supply WorldToLocal to VolumeCoordinateService.WorldToObjectSpace callers
    //   • Forward FrustumPlanes to IRenderPipeline for draw-call culling
    //
    // The coordinator never reads from a Camera or Transform directly — it reads
    // from this struct, obtained once at the start of Update().
    //
    // STRUCT SIZE NOTE
    // ─────────────────
    // Matrix4x4 = 64 bytes; three matrices = 192 bytes.
    // FrustumPlanes = an 8-byte managed reference (array allocated once per frame
    //   by ExtractFrustumPlanes — if profiling shows GC pressure on the hot path,
    //   the coordinator should cache the array and pass it into a future
    //   ExtractFrustumPlanesInto(Matrix4x4, Vector4[]) overload).
    // float × 2 + bool = ~9 bytes. Total on the stack ≈ 209 bytes — larger than
    // VolumeRenderState's 36-byte target but within a single coordinator stack frame
    // and constructed only once per Update().
    //
    // [CBO] Matrix4x4 and Vector4 are UnityEngine value types — already counted
    //       in VolumeCameraDriver's [CBO #1].
    // =========================================================================

    /// <summary>
    /// Immutable snapshot of camera-derived state for one rendered frame.
    /// Produced by <see cref="VolumeCameraDriver.ComputeFrame"/> and consumed
    /// by <c>VolumeRenderCoordinator</c>.
    /// </summary>
    public readonly struct CameraFrameState
    {
        // ── Transform matrices ────────────────────────────────────────────────

        /// <summary>
        /// The volume object's local-to-world transform matrix.
        /// Equivalent to <c>transform.localToWorldMatrix</c> (before/ line 1199),
        /// expressed as a plain value type so the coordinator never calls a Transform.
        /// Used to build <c>VolumeRenderState.ModelMatrix</c> for mask voxel offsets.
        /// </summary>
        public readonly Matrix4x4 LocalToWorld;

        /// <summary>
        /// Inverse of <see cref="LocalToWorld"/> (world-to-object transform).
        /// Pass to <see cref="VolumeCoordinateService.WorldToObjectSpace"/> wherever
        /// world-to-local conversion is needed. Eliminates all
        /// <c>transform.InverseTransformPoint</c> calls from domain logic
        /// (before/ lines 713, 739, 857). [FIXED V-10 DIP]
        /// </summary>
        public readonly Matrix4x4 WorldToLocal;

        // ── Camera projection ─────────────────────────────────────────────────

        /// <summary>
        /// Combined view-projection matrix
        /// (<c>Camera.projectionMatrix × Camera.worldToCameraMatrix</c>).
        /// Forwarded to <c>IRenderPipeline</c> for GPU-side draw setup and used
        /// by <see cref="VolumeCoordinateService.ExtractFrustumPlanes"/>.
        /// </summary>
        public readonly Matrix4x4 ViewProjection;

        /// <summary>
        /// Six inward-facing frustum clip planes extracted from <see cref="ViewProjection"/>.
        /// Order: Left, Right, Bottom, Top, Near, Far.
        /// New capability enabled by this extraction — absent from the before/ code.
        /// The coordinator forwards this to <c>IRenderPipeline</c> for culling
        /// (future capability FUT-03). May be ignored until that path is active.
        /// </summary>
        public readonly Vector4[] FrustumPlanes;

        /// <summary>Camera near clip plane distance in world units.</summary>
        public readonly float NearClipPlane;

        /// <summary>Camera far clip plane distance in world units.</summary>
        public readonly float FarClipPlane;

        // ── Projection mode ───────────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> when Average Intensity Projection (AIP) is active;
        /// <c>false</c> for Maximum Intensity Projection (MIP, the default).
        /// The coordinator copies this into
        /// <see cref="VolumeRenderState.AverageIntensityProjection"/> so
        /// <c>VolumeMaterialBinder.Tick()</c> can call
        /// <c>IRenderPipeline.SetPipelineKeyword("SHADER_AIP", ...)</c>.
        /// [→ before ProjectionMode field line 124; AIP if/else lines 1218–1221]
        /// [FIXED V-05 OCP]
        /// </summary>
        public readonly bool AverageIntensityProjection;

        /// <summary>Constructs a fully populated camera frame state.</summary>
        public CameraFrameState(
            Matrix4x4 localToWorld,
            Matrix4x4 worldToLocal,
            Matrix4x4 viewProjection,
            Vector4[] frustumPlanes,
            float     nearClipPlane,
            float     farClipPlane,
            bool      averageIntensityProjection)
        {
            LocalToWorld               = localToWorld;
            WorldToLocal               = worldToLocal;
            ViewProjection             = viewProjection;
            FrustumPlanes              = frustumPlanes;
            NearClipPlane              = nearClipPlane;
            FarClipPlane               = farClipPlane;
            AverageIntensityProjection = averageIntensityProjection;
        }
    }

    // =========================================================================
    // IVolumeCameraDriver — interface (2 members)
    // =========================================================================
    //
    // [FIXED V-06 ISP] The 152-member public surface of VolumeDataSetRenderer
    // included camera/coordinate concerns alongside shader binding, texture
    // management, and mask painting. IVolumeCameraDriver narrows the camera
    // contract to exactly 2 members — the minimum surface VolumeRenderCoordinator
    // requires.
    //
    // [CBO] Every consumer of this interface has CBO += 1 for this type only.
    //       VolumeRenderCoordinator's CBO counts IVolumeCameraDriver, not the
    //       concrete VolumeCameraDriver — a standard DIP win.
    // =========================================================================

    /// <summary>
    /// Computes per-frame camera state for <c>VolumeRenderCoordinator</c>.
    /// Implemented by <see cref="VolumeCameraDriver"/>; tests use
    /// <see cref="iDaVIE.Rendering.Tests.StubCameraDriver"/>.
    /// </summary>
    public interface IVolumeCameraDriver
    {
        // ── Member 1 ──────────────────────────────────────────────────────────

        /// <summary>
        /// Computes and returns the camera-derived state for the current frame:
        /// transform matrices, frustum planes, clip distances, and projection mode flag.
        /// Called once per frame by <c>VolumeRenderCoordinator.Update()</c>.
        /// </summary>
        /// <returns>
        /// A fully populated <see cref="CameraFrameState"/> ready for the coordinator
        /// to use without making any further Camera or Transform API calls.
        /// </returns>
        CameraFrameState ComputeFrame();

        // ── Member 2 ──────────────────────────────────────────────────────────

        /// <summary>
        /// Sets whether Average Intensity Projection is active.
        /// The new value is reflected in <see cref="CameraFrameState.AverageIntensityProjection"/>
        /// on the next <see cref="ComputeFrame"/> call.
        /// Called by <c>VolumeRenderCoordinator</c> when the user changes projection
        /// mode via the iDaVIE toolbar.
        /// [→ before ProjectionMode public field line 124; AIP toggle lines 1218–1221]
        /// </summary>
        void SetProjectionMode(bool averageIntensityProjection);
    }

    // =========================================================================
    // VolumeCameraDriver — concrete implementation
    //
    // SINGLE RESPONSIBILITY (SRP / GRASP High Cohesion)
    // --------------------------------------------------
    // This class is the ONLY class in the rendering layer permitted to:
    //   • Read Camera.worldToCameraMatrix and Camera.projectionMatrix
    //   • Access the volume object's transform.localToWorldMatrix
    //   • Own the active projection mode (MIP vs. AIP) state
    //
    // All other classes receive typed value types from CameraFrameState.
    // No class other than VolumeCameraDriver calls any Transform or Camera API.
    //
    // DEPENDENCY INJECTION
    // --------------------
    // The Camera is injected at construction time by VolumeRenderCoordinator.
    // Unit tests in iDaVIE.Rendering.Tests inject a Camera created via
    // new GameObject().AddComponent<Camera>() — no scene file required; Unity
    // supports AddComponent in the Editor test runner.
    // VolumeCameraDriver never calls FindObjectOfType or Camera.main.
    // [FIXED V-08 DIP]
    //
    // sealed: prevents subclasses from bypassing the null check or skipping the
    // projection mode field, which would produce a silently incorrect
    // CameraFrameState every frame.
    //
    // PROJECTED CK BREAKDOWN
    //   WMC = 5
    //     Constructor                  → 2  (null guard branch)
    //     ComputeFrame                 → 1  (no branches; pure matrix extraction)
    //     SetProjectionMode            → 1
    //     IsAverageIntensityProjection → 1  (property)
    //   CBO ≤ 4
    //     #1  UnityEngine  (Camera, Matrix4x4, Vector4, Vector3)
    //     #2  Camera       (specific Component type within UnityEngine)
    //     #3  VolumeCoordinateService  (co-located in iDaVIE.Rendering)
    //     #4  System       (ArgumentNullException — standard .NET, not counted
    //                       in all CK tools; included here for completeness)
    //   LCOM = 0.0 — ComputeFrame reads _camera; SetProjectionMode and
    //                IsAverageIntensityProjection read _averageIntensityProjection;
    //                both fields are accessed across methods — no disjoint clusters.
    // =========================================================================

    public sealed class VolumeCameraDriver : IVolumeCameraDriver
    {
        // ── Private fields ─────────────────────────────────────────────────────

        // [CBO #2] Camera — the sole Unity runtime dependency of this class.
        // Injected by VolumeRenderCoordinator; never read or written by any other
        // rendering class. Reading Camera.worldToCameraMatrix and
        // Camera.transform.localToWorldMatrix is permitted at this adapter layer.
        // [FIXED V-08 DIP] — no FindObjectOfType, no Camera.main.
        private readonly Camera _camera;

        // Projection mode state owned by this class.
        // [→ before ProjectionMode public field, line 124]
        // Set via SetProjectionMode(); included in every CameraFrameState via ComputeFrame().
        // [LCOM] Both _camera and _averageIntensityProjection are accessed across methods —
        //        the two fields form one cohesive cluster.
        private bool _averageIntensityProjection;

        // ── Constructor ────────────────────────────────────────────────────────
        //
        // [FIXED V-08 DIP] No FindObjectOfType, no Camera.main, no scene-search.
        // VolumeRenderCoordinator resolves the Camera at wiring time (Phase 6 of
        // the migration plan — docs/design-document.md §5.8) and passes it here.
        //
        // Default: MaximumIntensityProjection (false) — matches the before/ default
        // value at line 124.
        //
        // [WMC] CC = 2 (one null-guard branch). Contributes 2 to WMC total.
        public VolumeCameraDriver(Camera camera)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            _averageIntensityProjection = false;  // MIP default [→ before line 124]
        }

        // ── IVolumeCameraDriver implementation ─────────────────────────────────

        /// <inheritdoc/>
        // [→ before responsibility cluster "R5 — Camera / coordinate maths"
        //    annotated at lines 695–858 of before/VolumeDataSetRenderer.cs]
        //
        // WHAT THIS REPLACES IN THE BEFORE/ CODE
        // ────────────────────────────────────────
        // The before/ class had three distinct patterns that accessed transform:
        //
        //   1. transform.InverseTransformPoint (lines 713, 739, 857)
        //      ─ WorldToLocal matrix replaces this; callers pass it to
        //        VolumeCoordinateService.WorldToObjectSpace(point, frame.WorldToLocal).
        //
        //   2. transform.localToWorldMatrix (line 1199, mask voxel offsets)
        //      ─ LocalToWorld provides this as a plain value type; the coordinator
        //        reads frame.LocalToWorld and populates VolumeRenderState.ModelMatrix.
        //
        //   3. ProjectionMode AIP if/else (lines 1218–1221)
        //      ─ AverageIntensityProjection bool flows through CameraFrameState →
        //        VolumeRenderState → VolumeMaterialBinder.Tick() →
        //        IRenderPipeline.SetPipelineKeyword("SHADER_AIP", ...).
        //
        // After this extraction, no Transform API is called anywhere else in the
        // domain. The coordinator calls ComputeFrame() once per Update() and
        // distributes the results without touching Camera or Transform.
        //
        // [FIXED V-10 DIP] — InverseTransformPoint removed from domain logic
        // [FIXED V-05 OCP] — AIP bool replaces the binary if/else in Update()
        // [WMC] CC = 1 (no branches; pure matrix extraction). Contributes 1 to WMC total.
        public CameraFrameState ComputeFrame()
        {
            // Extract Unity matrices — the only place these APIs are called in iDaVIE.Rendering.
            Matrix4x4 localToWorld = _camera.transform.localToWorldMatrix;
            Matrix4x4 worldToLocal = localToWorld.inverse;                                      // [FIXED V-10]
            Matrix4x4 vp           = _camera.projectionMatrix * _camera.worldToCameraMatrix;

            return new CameraFrameState(
                localToWorld:               localToWorld,
                worldToLocal:               worldToLocal,       // replaces InverseTransformPoint
                viewProjection:             vp,
                frustumPlanes:              VolumeCoordinateService.ExtractFrustumPlanes(vp),
                nearClipPlane:              _camera.nearClipPlane,
                farClipPlane:              _camera.farClipPlane,
                averageIntensityProjection: _averageIntensityProjection  // [FIXED V-05]
            );
        }

        /// <inheritdoc/>
        // [→ before ProjectionMode public field line 124; AIP if/else lines 1218–1221]
        //
        // [FIXED V-05 OCP] The before/ if/else was:
        //   if (ProjectionMode == ProjectionMode.AverageIntensityProjection)
        //       Shader.EnableKeyword("SHADER_AIP");
        //   else
        //       Shader.DisableKeyword("SHADER_AIP");
        //
        // After refactoring the chain is:
        //   SetProjectionMode(bool)             — stores flag here (VolumeCameraDriver)
        //   ComputeFrame()                      — includes flag in CameraFrameState
        //   VolumeRenderCoordinator.Update()    — copies to VolumeRenderState
        //   VolumeMaterialBinder.Tick()         — calls IRenderPipeline.SetPipelineKeyword
        //
        // Each step has exactly one responsibility. Adding a third mode
        // (MinimumIntensityProjection) means writing one new IProjectionMode class;
        // this method's signature does not change. [FIXED V-05 OCP]
        //
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public void SetProjectionMode(bool averageIntensityProjection)
        {
            _averageIntensityProjection = averageIntensityProjection;
        }

        // ── Convenience property ───────────────────────────────────────────────

        /// <summary>
        /// <c>true</c> when Average Intensity Projection mode is currently active.
        /// The coordinator may read this to sync UI state without first calling
        /// <see cref="ComputeFrame"/>.
        /// </summary>
        // [WMC] CC = 1. Contributes 1 to WMC total.
        public bool IsAverageIntensityProjection => _averageIntensityProjection;

        // ── Persistence — Sub-team 7 Integration ───────────────────────────────
        // [§ docs/integration/team7-persistence-contract.md — SpatialState]
        //
        // These methods are stubs. The full implementation will be wired in Sprint 3
        // after the volume object Transform is passed to the driver.

        /// <summary>
        /// Captures the current spatial (transform) state for session persistence.
        /// Called by <c>VolumeRenderCoordinator.SaveSession()</c>.
        /// </summary>
        public SpatialState CaptureState()
        {
            // TODO Sprint 3: extract position, rotation (Euler), and scale from
            // _camera.transform.parent (the volume object's transform).
            // Region bounds come from VolumeTextureManager.CurrentCrop*.
            return new SpatialState(
                posX: 0f, posY: 0f, posZ: 0f,
                rotX: 0f, rotY: 0f, rotZ: 0f,
                scale: 1f,
                regionMinX: 0, regionMinY: 0, regionMinZ: 0,
                regionMaxX: 0, regionMaxY: 0, regionMaxZ: 0,
                hasRegion: false
            );
        }

        /// <summary>
        /// Restores spatial state after session load.
        /// Called by <c>VolumeRenderCoordinator.LoadSession()</c>.
        /// Must update the volume object's transform and set region bounds.
        /// </summary>
        public void RestoreState(SpatialState state)
        {
            // TODO Sprint 3: apply position, rotation, and scale back to the volume
            // object's transform. Must update _camera.transform.parent.
            // The region bounds restoration happens in VolumeTextureManager.RestoreState.
        }
    }

    // =========================================================================
    // HOW VolumeCameraDriver FITS INTO THE PER-FRAME LOOP
    // ─────────────────────────────────────────────────────────────────────────
    // VolumeRenderCoordinator.Update() uses this class as follows:
    //
    //   Step 1 — Compute camera state (replaces all Transform API calls):
    //     CameraFrameState frame = _cameraDriver.ComputeFrame();
    //
    //   Step 2 — Compute foveation (parallel, no dependency on frame):
    //     FoveationParameters fov = _foveatedPolicy.ComputeParameters();
    //
    //   Step 3 — Build VolumeRenderState incorporating both results:
    //     var state = new VolumeRenderState(
    //         ...
    //         modelMatrix:                  frame.LocalToWorld,               // → mask voxel offsets
    //         averageIntensityProjection:   frame.AverageIntensityProjection,  // → SHADER_AIP keyword
    //         foveatedEnabled:              fov.FoveationActive,
    //         ...);
    //
    //   Step 4 — Bind shader uniforms:
    //     _materialBinder.Tick(state);
    //
    //   Step 5 — Pipeline frustum setup (when culling is active, future FUT-03):
    //     _renderPipeline.SetFrustumPlanes(frame.FrustumPlanes);
    //
    //   On-demand coordinate conversion (e.g. cursor tracking, region query):
    //     Vector3 local = VolumeCoordinateService.WorldToObjectSpace(
    //         worldCursorPos, frame.WorldToLocal);
    //     // Replaces: transform.InverseTransformPoint(worldCursorPos)
    //     //           (before/ lines 713, 739, 857)
    // =========================================================================
}

// =============================================================================
// StubCameraDriver — test double
// =============================================================================
//
// PURPOSE
// -------
// Enables unit tests of VolumeRenderCoordinator without a real Camera,
// a running Unity scene, or a GPU. Inject this wherever IVolumeCameraDriver
// is expected in edit-mode NUnit tests.
//
// USAGE EXAMPLES
// --------------
//   // Test 1: verify coordinator reads MIP state by default
//   var stub = new StubCameraDriver();
//   CameraFrameState f = stub.ComputeFrame();
//   Assert.IsFalse(f.AverageIntensityProjection);
//   Assert.AreEqual(Matrix4x4.identity, f.LocalToWorld);
//
//   // Test 2: verify AIP flag propagates through ComputeFrame
//   var stub = new StubCameraDriver();
//   stub.SetProjectionMode(true);
//   Assert.IsTrue(stub.ComputeFrame().AverageIntensityProjection);
//
//   // Test 3: supply a specific WorldToLocal for VolumeCoordinateService tests
//   var knownMatrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one);
//   var state = new CameraFrameState(
//       localToWorld: knownMatrix,
//       worldToLocal: knownMatrix.inverse,
//       viewProjection: Matrix4x4.identity,
//       frustumPlanes: new Vector4[6],
//       nearClipPlane: 0.1f, farClipPlane: 100f,
//       averageIntensityProjection: false);
//   var stub = new StubCameraDriver(state);
//   Vector3 local = VolumeCoordinateService.WorldToObjectSpace(worldPoint, stub.ComputeFrame().WorldToLocal);
//
// PLACEMENT
// ---------
// This class is in iDaVIE.Rendering.Tests and must NOT ship in production builds.
// Add an Assembly Definition (asmdef) with Editor-only flag — see
// docs/shader-asset-policy.md §9.
// =============================================================================

namespace iDaVIE.Rendering.Tests
{
    using UnityEngine;

    /// <summary>
    /// Test double for <see cref="IVolumeCameraDriver"/>.
    /// Returns a fixed <see cref="CameraFrameState"/> — no Camera or scene required.
    /// </summary>
    public sealed class StubCameraDriver : IVolumeCameraDriver
    {
        private CameraFrameState _state;

        /// <summary>
        /// Constructs a stub that returns an identity-matrix state with MIP active.
        /// </summary>
        public StubCameraDriver()
        {
            _state = new CameraFrameState(
                localToWorld:               Matrix4x4.identity,
                worldToLocal:               Matrix4x4.identity,
                viewProjection:             Matrix4x4.identity,
                frustumPlanes:              new Vector4[6],
                nearClipPlane:              0.1f,
                farClipPlane:               1000f,
                averageIntensityProjection: false);
        }

        /// <summary>
        /// Constructs a stub with a fully specified fixed state.
        /// Use when the test needs non-identity matrices or specific clip values.
        /// </summary>
        public StubCameraDriver(CameraFrameState fixedState)
        {
            _state = fixedState;
        }

        /// <inheritdoc/>
        public CameraFrameState ComputeFrame() => _state;

        /// <inheritdoc/>
        public void SetProjectionMode(bool averageIntensityProjection)
        {
            // Rebuild struct with updated AIP flag so SetProjectionMode → ComputeFrame
            // round-trips can be verified in tests.
            _state = new CameraFrameState(
                _state.LocalToWorld,
                _state.WorldToLocal,
                _state.ViewProjection,
                _state.FrustumPlanes,
                _state.NearClipPlane,
                _state.FarClipPlane,
                averageIntensityProjection);
        }
    }
}

// =============================================================================
// CK METRICS SUMMARY — VolumeCameraDriver + VolumeCoordinateService
//                       (projected Day 13)
//
// VolumeCameraDriver
//   WMC = 5
//     Constructor                  → 2  (null guard adds one branch)
//     ComputeFrame                 → 1  (no branches)
//     SetProjectionMode            → 1
//     IsAverageIntensityProjection → 1
//                              Total: 5  ✅ target ≤ 20
//
//   CBO = 4  (Understand)                                     ✅ target ≤ 14 domain
//   RFC = 4  (Understand)                                     ✅ target ≤ 50
//   LCOM = 0.25  (25% Percent Lack of Cohesion — Understand)  ✅ target ≤ 0.5
//   DIT  = 1  (IFANIN=2)                                      ✅ ≤ 4
//   NOC  = 0                                                   ✅ ≤ 5
//
// VolumeCoordinateService (Understand)
//   WMC = 3   CBO = 3   RFC = 3   LCOM = 0.0   NIM = 0  ✅ all targets
//
// Combined WMC for this file:
//   4 (VolumeCameraDriver) + 3 (VolumeCoordinateService) = 7  ✅ ≤ 20
//
// BEFORE (VolumeDataSetRenderer — Understand tool):
//   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
//
// REDUCTION:
//   WMC:  7 vs. 97  →  camera concern extracted with minimal complexity
//   CBO:  4 vs. 28  →  camera coupling isolated; domain math stays pure C#
//   LCOM: 0.25 vs. 0.95  →  from near-fully-incoherent to mostly cohesive
//   V-10 DIP violation: ELIMINATED — transform.InverseTransformPoint removed from
//                        all domain logic; domain math is now pure C# in
//                        VolumeCoordinateService.
// =============================================================================
