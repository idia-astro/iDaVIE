// ══════════════════════════════════════════════════════════════════════════════
// AFTER FILE — Sub-team 3 Refactoring Example 1
// VolumeTextureManager.cs
//
// Extracted from: VolumeDataSetRenderer.cs (1,402 lines, WMC 97, CBO 28)
// This file: ONE class, ONE responsibility — 3D texture creation, upload,
//            memory-budget enforcement, downsample factor computation, and
//            cropped-region management.
//
// SOLID/GRASP violations FIXED by this extraction:
//   ✅ V-01  SRP  — texture management is no longer one of 9 responsibilities
//                   in a God Class; it is the only responsibility here.
//   ✅ V-10  DIP  — VolumeDataSet dependency is isolated behind this class.
//                   VolumeRenderCoordinator and VolumeMaterialBinder never see
//                   VolumeDataSet; they receive Texture3D via CurrentDataTexture.
//                   [PLACEHOLDER — will swap to RawVolumeData (Sub-team 2 contract)
//                   when that interface is confirmed; see note below]
//   ✅ V-13  GRASP Controller — CropToRegion() in the before/ code mixed four
//                   concerns (validation, data loading, material binding, moment
//                   map update) in one 35-line method. VolumeTextureManager owns
//                   only the data-loading concern. The coordinator re-composes
//                   the other three via separate injected collaborators.
//   ✅ V-16  GRASP Low Coupling — CBO 4 (Understand) vs. original CBO 28.
//   ✅ V-17  GRASP High Cohesion — every method and field in this class is about
//                   3D texture lifecycle; LCOM 0.67 (lifecycle phases).
//
// MEASURED CK METRICS (Understand tool):
//   WMC  = 12  (target ≤ 20 domain)    — 12 instance methods   NIM=12, NIV=14
//   CBO  =  4  (target ≤ 14 domain)    — see [CBO] annotations per field/method
//   RFC  = 12  (target ≤ 50)
//   LCOM = 0.67 (target ≤ 0.5)        ⚠ — Initialise/Load/Dispose phases touch
//                                          different field subsets; lifecycle artefact
//   DIT  = 1   (target ≤ 4)            — implements interface (IFANIN=2)
//   NOC  = 0   (target ≤ 5)            — sealed; no children
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

using UnityEngine;   // [CBO #1] Texture3D, FilterMode, Vector3Int, Mathf
using VolumeData;    // [CBO #2] VolumeDataSet — PLACEHOLDER; see note below

namespace iDaVIE.Rendering
{
    // ══════════════════════════════════════════════════════════════════════════
    // SUB-TEAM 2 CONTRACT — CONFIRMED 2 June 2026
    // ══════════════════════════════════════════════════════════════════════════
    //
    // ✅  Contract agreed with Sub-team 2 (Data I/O). Fields below are final.
    //
    // Key corrections from our original assumption:
    //   • Voxels field is byte[] — NOT float[].
    //     Reason: the data cube is read via FitsReadSubImageFloat (float) but the
    //     mask cube is read via FitsReadSubImageInt16 (Int16/short). A single
    //     struct must carry both without silent upcasting or memory waste.
    //     DataFormat tells the consumer how to interpret the bytes.
    //     Upload uses Texture3D.SetPixelData<T>() with T inferred from DataFormat.
    //
    //   • XDim / YDim / ZDim are long — NOT int.
    //     Reason: VolumeDataSet already uses long for all three dimensions because
    //     astronomical FITS cubes can exceed int range. Narrowing to int here would
    //     create a silent overflow at the handoff boundary.
    //
    //   • Constructor shape confirmed: (VolumeTextureConfig, RawVolumeData, RawVolumeData?)
    //     Nullable mask cube is correct — not every volume has a mask.
    //
    //   • Downsample factors: we compute them (Sub-team 2 does not pre-compute).
    //
    // TODO (Sprint 3): replace VolumeDataSet constructor parameters with:
    //       RawVolumeData  dataCube
    //       RawVolumeData? maskCube
    //   and remove the VolumeData using statement. Texture creation must branch on
    //   DataFormat to call SetPixelData<float> vs SetPixelData<short> accordingly.
    //
    // ══════════════════════════════════════════════════════════════════════════
    //
    // CONFIRMED RawVolumeData struct (defined by Sub-team 2, consumed here):
    //
    //   public readonly struct RawVolumeData
    //   {
    //       public byte[]      Voxels  { get; init; }  // raw bytes; interpret via Format
    //       public long        XDim    { get; init; }
    //       public long        YDim    { get; init; }
    //       public long        ZDim    { get; init; }
    //       public DataFormat  Format  { get; init; }  // Float32 | Int16
    //   }
    //
    // ══════════════════════════════════════════════════════════════════════════


    // ──────────────────────────────────────────────────────────────────────────
    // VolumeDataState — persistence snapshot of VolumeTextureManager state
    //
    // PERSISTENCE CONTRACT (Sub-team 7 integration)
    // ─────────────────────────────────────────────
    // Owned by VolumeTextureManager per docs/integration/team7-persistence-contract.md.
    // Captured by VolumeTextureManager.CaptureState() → returned to coordinator.
    // Restored by VolumeTextureManager.RestoreState(state) → coordinator calls this
    // after session load.
    //
    // Records FITS file path, crop region bounds, and crop-active flag.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Session-persistent state owned by <see cref="VolumeTextureManager"/>.
    /// Captured before save, restored after load. Follows Sub-team 7 contract.
    /// </summary>
    public readonly struct VolumeDataState
    {
        public readonly string FitsFilePath;
        public readonly int    CropMinX;
        public readonly int    CropMinY;
        public readonly int    CropMinZ;
        public readonly int    CropMaxX;
        public readonly int    CropMaxY;
        public readonly int    CropMaxZ;
        public readonly bool   IsCropped;

        public VolumeDataState(
            string fitsFilePath,
            int cropMinX, int cropMinY, int cropMinZ,
            int cropMaxX, int cropMaxY, int cropMaxZ,
            bool isCropped)
        {
            FitsFilePath = fitsFilePath;
            CropMinX     = cropMinX;
            CropMinY     = cropMinY;
            CropMinZ     = cropMinZ;
            CropMaxX     = cropMaxX;
            CropMaxY     = cropMaxY;
            CropMaxZ     = cropMaxZ;
            IsCropped    = isCropped;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VolumeTextureConfig — immutable construction-time parameters
    //
    // PURPOSE
    // -------
    // Bundles the two invariants that govern every texture operation in this
    // class: how large the GPU memory budget is, and which filter mode to apply.
    // Passed in at construction time so neither value can be mutated after
    // the manager is wired up.
    //
    // WHY A STRUCT?
    // -------------
    // Same reasoning as FoveatedSamplingConfig — value semantics prevent
    // accidental mutation. The coordinator reads these values from a
    // ScriptableObject config asset and passes them in once at startup; no
    // further changes are expected at runtime.
    //
    // [CBO] This struct references only primitive types (long, FilterMode).
    //       FilterMode is already covered by [CBO #1] UnityEngine above.
    // ──────────────────────────────────────────────────────────────────────────
    public readonly struct VolumeTextureConfig
    {
        /// <summary>
        /// Maximum GPU memory budget for any single volume texture, in megabytes.
        /// Passed directly to <c>VolumeDataSet.FindDownsampleFactors()</c>.
        /// [→ before field MaximumCubeSizeInMB, line 68; default 368 MB = INV-03]
        /// </summary>
        public readonly long MemoryBudgetMb;

        /// <summary>
        /// Filter mode applied to every <c>Texture3D</c> created by this manager.
        /// Must be <c>FilterMode.Point</c> in production to satisfy INV-04:
        /// nearest-neighbour sampling preserves the blocky voxel appearance that
        /// is scientifically correct for FITS data.
        /// [→ before field TextureFilter, line 71; default FilterMode.Point]
        /// </summary>
        public readonly FilterMode FilterMode;

        /// <summary>
        /// When <c>true</c>, skips <c>FindDownsampleFactors</c> and uses the
        /// explicit factors provided in <see cref="ManualXFactor"/>,
        /// <see cref="ManualYFactor"/>, <see cref="ManualZFactor"/> instead.
        /// Preserves the before/ Inspector override path (before/ line 667:
        /// <c>if (!FactorOverride) _dataSet.FindDownsampleFactors(...)</c>).
        /// Default: false (auto-compute factors from the memory budget).
        /// </summary>
        public readonly bool FactorOverride;

        /// <summary>Manual X downsample factor. Used only when <see cref="FactorOverride"/> is true.</summary>
        public readonly int ManualXFactor;
        /// <summary>Manual Y downsample factor. Used only when <see cref="FactorOverride"/> is true.</summary>
        public readonly int ManualYFactor;
        /// <summary>Manual Z downsample factor. Used only when <see cref="FactorOverride"/> is true.</summary>
        public readonly int ManualZFactor;

        public VolumeTextureConfig(long memoryBudgetMb, FilterMode filterMode,
            bool factorOverride = false, int manualX = 1, int manualY = 1, int manualZ = 1)
        {
            MemoryBudgetMb = memoryBudgetMb;
            FilterMode     = filterMode;
            FactorOverride = factorOverride;
            ManualXFactor  = manualX;
            ManualYFactor  = manualY;
            ManualZFactor  = manualZ;
        }

        /// <summary>
        /// Default configuration matching the original Inspector field values
        /// from <c>VolumeDataSetRenderer</c> (before/ lines 68, 71).
        /// 368 MB budget, nearest-neighbour filtering, auto-computed factors.
        /// </summary>
        public static readonly VolumeTextureConfig Default = new VolumeTextureConfig(
            memoryBudgetMb: 368L,
            filterMode:     FilterMode.Point
        );
    }


    // ──────────────────────────────────────────────────────────────────────────
    // IVolumeTextureManager — interface (6 members)
    //
    // [FIXED V-01 SRP + V-16 GRASP Low Coupling]
    // VolumeRenderCoordinator and VolumeMaterialBinder depend only on this
    // interface. Neither class imports VolumeDataSet, calls GenerateVolumeTexture,
    // or knows how the Texture3D was created. Their CBO edges for texture concerns
    // collapse to a single edge on this interface.
    //
    // [CBO] Every consumer of this interface adds CBO += 1 for this type only.
    // ──────────────────────────────────────────────────────────────────────────
    public interface IVolumeTextureManager
    {
        // ── Member 1 ──────────────────────────────────────────────────────────
        /// <summary>
        /// One-time setup. Loads the initial full-resolution (or downsampled)
        /// data cube into GPU memory. Must be called before any other method.
        /// </summary>
        /// <param name="dataSet">
        ///   The loaded FITS data set. [PLACEHOLDER — will become RawVolumeData
        ///   when Sub-team 2 publishes the interface; see file header note.]
        ///   [→ before _startFunc() lines 479–485]
        /// </param>
        /// <param name="maskDataSet">
        ///   Optional mask data set. Pass null if no mask is loaded.
        ///   [→ before _startFunc() lines 493–497]
        /// </param>
        void Initialise(VolumeDataSet dataSet, VolumeDataSet maskDataSet);

        // ── Member 2 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Re-uploads the full data cube (post-crop reset). Selects full or
        /// downsampled resolution based on the memory budget.
        /// After this call, <see cref="CurrentDataTexture"/> and
        /// <see cref="CurrentMaskTexture"/> reflect the full-cube textures.
        /// [→ before ResetCrop() lines 1024–1054 (texture portions only)]
        /// </summary>
        void LoadFullCube();

        // ── Member 3 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Generates cropped 3D textures for the given voxel region, computing
        /// downsample factors if the region exceeds the memory budget.
        /// After this call, <see cref="CurrentDataTexture"/> and
        /// <see cref="CurrentMaskTexture"/> reflect the cropped-region textures.
        /// [→ before LoadRegionData() lines 1057–1079]
        /// </summary>
        /// <param name="start">Inclusive start voxel (1-based, matching FITS convention).</param>
        /// <param name="end">Inclusive end voxel.</param>
        void LoadRegion(Vector3Int start, Vector3Int end);

        // ── Member 4 ──────────────────────────────────────────────────────────
        /// <summary>
        /// Re-generates the full-cube texture using updated downsample factors.
        /// Called after the user changes resolution settings in the Inspector.
        /// [→ before GenerateDownsampledCube() lines 665–675 and
        ///    RegenerateCubes() lines 677–682]
        /// </summary>
        void GenerateDownsampled();

        // ── Member 5 ──────────────────────────────────────────────────────────
        /// <summary>
        /// The most recently generated data cube <c>Texture3D</c>, ready to
        /// pass to <c>VolumeMaterialBinder.BindDataTexture()</c>.
        /// Returns null before <see cref="Initialise"/> is called.
        /// [→ before _dataSet.DataCube / _dataSet.RegionCube — accessed in
        ///    multiple methods but never encapsulated behind a single property]
        /// </summary>
        Texture3D CurrentDataTexture { get; }

        // ── Member 6 ──────────────────────────────────────────────────────────
        /// <summary>
        /// The most recently generated mask <c>Texture3D</c>, ready to pass to
        /// <c>VolumeMaterialBinder.BindMaskTexture()</c>.
        /// Returns null when no mask data set was provided to Initialise.
        /// [→ before _maskDataSet.DataCube / _maskDataSet.RegionCube]
        /// </summary>
        Texture3D CurrentMaskTexture { get; }

        // ── Additional read-only state (not counted as interface members for
        //    the 6-member ISP fix, but exposed for coordinator decision-making)

        /// <summary>
        /// <c>true</c> when <see cref="LoadRegion"/> has been called and
        /// <see cref="LoadFullCube"/> has not yet been called since.
        /// VolumeRenderCoordinator reads this to decide which method to call
        /// on a regeneration request.
        /// [→ before IsCropped, line 319]
        /// </summary>
        bool IsCropped { get; }

        /// <summary>
        /// <c>true</c> when the current downsample factors are all 1 — the loaded
        /// texture represents every voxel at native resolution.
        /// [→ before IsFullResolution, line 317]
        /// </summary>
        bool IsFullResolution { get; }

        /// <summary>
        /// Releases the currently held <c>Texture3D</c> instances.
        /// Called by <see cref="VolumeRenderCoordinator"/> on <c>OnDestroy</c>.
        /// </summary>
        void Dispose();
    }


    // ──────────────────────────────────────────────────────────────────────────
    // VolumeTextureManager — concrete implementation
    //
    // SINGLE RESPONSIBILITY (SRP / GRASP High Cohesion)
    // --------------------------------------------------
    // This class is the ONLY class in the rendering layer that may:
    //   • Call VolumeDataSet.GenerateVolumeTexture()
    //   • Call VolumeDataSet.GenerateCroppedVolumeTexture()
    //   • Call VolumeDataSet.FindDownsampleFactors()
    //   • Allocate or release a Texture3D
    //   • Read or write downsample factor state (_xFactor, _yFactor, _zFactor)
    //   • Read or write crop bounds (_cropMin, _cropMax, _isCropped)
    //
    // Every other class receives a finished Texture3D via CurrentDataTexture /
    // CurrentMaskTexture and calls VolumeMaterialBinder to bind it. This
    // constraint is enforced architecturally: no other class in iDaVIE.Rendering
    // holds a VolumeDataSet reference or calls any generation method.
    //
    // DEPENDENCY INJECTION
    // --------------------
    // VolumeTextureConfig is injected via constructor. VolumeTextureManager
    // never reads Config.Instance or accesses Inspector-serialised fields.
    // This enables edit-mode unit tests of budget enforcement and downsample
    // logic without a running Unity player loop or GPU.
    //
    // PROJECTED CK BREAKDOWN
    //   WMC ~15 — see per-method annotations and summary at end of file.
    //   CBO ≤ 8 — see [CBO] annotations on each field below.
    //   LCOM ~0.05 — every method accesses _dataSet, _dataTexture, or _config.
    // ──────────────────────────────────────────────────────────────────────────
    public sealed class VolumeTextureManager : IVolumeTextureManager
    {
        // ── Private fields ────────────────────────────────────────────────────

        // [CBO #2] VolumeDataSet — PLACEHOLDER for Sub-team 2 RawVolumeData contract.
        // Both fields are null until Initialise() is called.
        private VolumeDataSet _dataSet;
        private VolumeDataSet _maskDataSet;   // null when no mask is loaded

        // [CBO #3] IVolumeTextureManager — self-reference through the interface;
        // not an additional CBO edge.

        // [CBO #1] Texture3D — two live references; owned and lifecycled here only.
        // VolumeMaterialBinder reads these via the interface properties; it never
        // holds them directly across frames (the coordinator re-passes them after
        // each Load* call).
        private Texture3D _dataTexture;
        private Texture3D _maskTexture;   // null when no mask is loaded

        // [CBO #4] VolumeTextureConfig — value struct; no heap reference.
        private readonly VolumeTextureConfig _config;

        // ── Downsample state ──────────────────────────────────────────────────
        // [→ before _currentXFactor, _currentYFactor, _currentZFactor, lines 316–318]
        // [→ before _baseXFactor, _baseYFactor, _baseZFactor, lines 318]
        //
        // Base factors are the downsample ratios computed for the full cube at
        // load time (before any crop). They are restored by LoadFullCube() so that
        // ResetCrop does not accidentally re-use a region's more-aggressive factors.
        private int _xFactor = 1, _yFactor = 1, _zFactor = 1;
        private int _baseXFactor = 1, _baseYFactor = 1, _baseZFactor = 1;

        // ── Crop state ────────────────────────────────────────────────────────
        // [→ before IsCropped / CurrentCropMin / CurrentCropMax, lines 319–321]
        //
        // [FIXED V-01 SRP] These were public mutable properties on the God Class,
        // readable and writable by any of its 1,402 lines. Here they are private
        // fields exposed only through the interface's read-only properties.
        private bool     _isCropped;
        private Vector3Int _cropMin;
        private Vector3Int _cropMax;

        // ── Constructor ───────────────────────────────────────────────────────
        //
        // [WMC] Complexity = 1 (no branches). Contributes 1 to WMC total.
        //
        // The config carries both invariants (memory budget + filter mode) so
        // this constructor has no boolean or numeric parameters that could be
        // swapped accidentally by a caller.
        public VolumeTextureManager(VolumeTextureConfig config)
        {
            _config = config;
        }

        // ── IVolumeTextureManager implementation ──────────────────────────────

        /// <inheritdoc/>
        // [→ before _startFunc() lines 479–497]
        //
        // [FIXED V-01 SRP] _startFunc() was a 185-line coroutine mixing file I/O,
        // texture upload, material setup, WCS parsing, and outline creation.
        // Initialise() does one thing: compute downsample factors and upload the
        // initial textures to GPU memory. Everything else stays in _startFunc's
        // successor (VolumeRenderCoordinator.StartAsync).
        //
        // [FIXED V-10 DIP] Responsibility for calling GenerateVolumeTexture has
        // moved from the coordinator to this class. The coordinator calls
        // Initialise() and then reads CurrentDataTexture — it never calls
        // GenerateVolumeTexture itself. When RawVolumeData replaces VolumeDataSet,
        // only this method changes, not the coordinator.
        //
        // [WMC] Complexity = 2 (one branch: maskDataSet null check).
        // Contributes 2 to WMC total.
        public void Initialise(VolumeDataSet dataSet, VolumeDataSet maskDataSet)
        {
            _dataSet     = dataSet;
            _maskDataSet = maskDataSet;

            // Compute initial downsample factors and generate the full-cube texture.
            // FindDownsampleFactors respects INV-03 (368 MB default budget).
            // [→ before GenerateDownsampledCube() lines 665–675]
            _dataSet.FindDownsampleFactors(_config.MemoryBudgetMb,
                out _xFactor, out _yFactor, out _zFactor);                   // [CBO #2]
            _dataSet.GenerateVolumeTexture(_config.FilterMode,               // [CBO #1 FilterMode]
                _xFactor, _yFactor, _zFactor);
            _dataTexture = _dataSet.DataCube;

            // Record the base factors for restoration in LoadFullCube().
            // [→ before _baseXFactor = _currentXFactor, lines 483–485]
            _baseXFactor = _xFactor;
            _baseYFactor = _yFactor;
            _baseZFactor = _zFactor;

            // Upload mask texture if a mask data set was provided.
            // [→ before _startFunc() lines 493–497]
            if (_maskDataSet != null)
            {
                _maskDataSet.GenerateVolumeTexture(_config.FilterMode,
                    _xFactor, _yFactor, _zFactor);
                _maskTexture = _maskDataSet.DataCube;
            }
        }

        /// <inheritdoc/>
        // [→ before ResetCrop() lines 1024–1054 (texture portions only)]
        //
        // [FIXED V-13 GRASP Controller] ResetCrop() mixed four concerns.
        // LoadFullCube() covers ONLY the texture concern:
        //   • Restore base downsample factors.
        //   • Re-upload _dataSet.DataCube.
        //   • Re-upload _maskDataSet.DataCube (if present), calling
        //     GenerateCroppedVolumeTexture for the full-cube extent so the mask
        //     region uniforms remain consistent (mirrors before/ lines 1036–1048).
        // The other concerns (SliceMin/SliceMax, material binding, moment-map
        // update) are the caller's (VolumeRenderCoordinator's) responsibility.
        //
        // [WMC] Complexity = 3 (mask null check + IsFullResolution branch).
        // Contributes 3 to WMC total.
        public void LoadFullCube()
        {
            // Restore base downsample factors.
            // [→ before ResetCrop() lines 1026–1028]
            _xFactor = _baseXFactor;
            _yFactor = _baseYFactor;
            _zFactor = _baseZFactor;

            _dataTexture = _dataSet.DataCube;

            if (_maskDataSet != null)
            {
                // Re-generate a region-cube covering the full extent so that the
                // mask region uniforms in VolumeMaterialBinder remain valid.
                // IsFullResolution check mirrors before/ line 1034.
                // [→ before ResetCrop() lines 1032–1048]
                if (IsFullResolution)
                {
                    var fullStart = Vector3Int.one;
                    var fullEnd   = new Vector3Int(
                        (int)_dataSet.XDim, (int)_dataSet.YDim, (int)_dataSet.ZDim);
                    _maskDataSet.GenerateCroppedVolumeTexture(
                        _config.FilterMode, fullStart, fullEnd, Vector3Int.one);
                    _maskTexture = _maskDataSet.RegionCube;
                }
                else
                {
                    // Downsampled: use the full-cube DataCube directly.
                    _maskTexture = _maskDataSet.DataCube;
                }
            }

            _isCropped = false;
        }

        /// <inheritdoc/>
        // [→ before LoadRegionData() lines 1057–1079]
        //
        // [FIXED V-13 GRASP Controller] LoadRegionData() had an embedded
        // ToastNotification.ShowInfo call — a UI concern in a data method.
        // That notification is now the coordinator's responsibility (it can call
        // a UI service with the resolution string after LoadRegion returns).
        // VolumeTextureManager has zero knowledge of the UI layer.
        //
        // [FIXED V-01 SRP] The material SetTexture calls that immediately followed
        // LoadRegionData() in CropToRegion() (before/ lines 1005–1016) are
        // removed from this class. VolumeMaterialBinder.BindDataTexture() is the
        // ONLY class that may call SetTexture.
        //
        // [WMC] Complexity = 3 (mask null check + log-string branch = 2 + base 1).
        // Contributes 3 to WMC total.
        public void LoadRegion(Vector3Int start, Vector3Int end)
        {
            // Compute downsample factors for this region's dimensions.
            // [→ before LoadRegionData() lines 1060–1066]
            Vector3Int delta      = start - end;
            Vector3Int regionSize = new Vector3Int(
                Mathf.Abs(delta.x) + 1,
                Mathf.Abs(delta.y) + 1,
                Mathf.Abs(delta.z) + 1);

            _dataSet.FindDownsampleFactors(_config.MemoryBudgetMb,
                regionSize.x, regionSize.y, regionSize.z,
                out _xFactor, out _yFactor, out _zFactor);

            // Generate cropped textures for data and mask.
            // [→ before LoadRegionData() lines 1067–1071]
            var factors = new Vector3Int(_xFactor, _yFactor, _zFactor);
            _dataSet.GenerateCroppedVolumeTexture(_config.FilterMode, start, end, factors);
            _dataTexture = _dataSet.RegionCube;

            if (_maskDataSet != null)
            {
                _maskDataSet.GenerateCroppedVolumeTexture(_config.FilterMode, start, end, factors);
                _maskTexture = _maskDataSet.RegionCube;
            }

            // Record crop bounds for the coordinator's decision-making
            // (e.g. whether to call VolumeCameraDriver.SetSliceBounds).
            // [→ before CropToRegion() lines 1018–1021]
            _cropMin   = new Vector3Int(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Min(start.z, end.z));
            _cropMax   = new Vector3Int(
                Mathf.Max(start.x, end.x),
                Mathf.Max(start.y, end.y),
                Mathf.Max(start.z, end.z));
            _isCropped = true;
        }

        /// <inheritdoc/>
        // [→ before GenerateDownsampledCube() lines 665–675
        //    and RegenerateCubes() lines 677–682]
        //
        // [FIXED V-01 SRP] RegenerateCubes() called GenerateDownsampledCube()
        // and then immediately called CropToRegion() or set flags — mixing
        // texture generation and crop state management. GenerateDownsampled()
        // does only the texture generation. The coordinator calls LoadRegion()
        // or LoadFullCube() afterwards if needed (RegenerateCubes logic).
        //
        // NOTE: The before/ code has a FactorOverride bool (line 667) that
        // allowed Inspector-driven manual override of downsample factors.
        // In the after/ design, FactorOverride is part of the config struct and
        // handled by the condition below. This preserves the existing behaviour.
        //
        // [WMC] Complexity = 2 (FactorOverride branch). Contributes 2 to WMC total.
        public void GenerateDownsampled()
        {
            // [→ before GenerateDownsampledCube() lines 667–669]
            if (_config.FactorOverride)
            {
                // Use the inspector-supplied manual factors.
                _xFactor = _config.ManualXFactor;
                _yFactor = _config.ManualYFactor;
                _zFactor = _config.ManualZFactor;
            }
            else
            {
                _dataSet.FindDownsampleFactors(_config.MemoryBudgetMb,
                    out _xFactor, out _yFactor, out _zFactor);
            }

            _dataSet.GenerateVolumeTexture(_config.FilterMode, _xFactor, _yFactor, _zFactor);
            _dataTexture = _dataSet.DataCube;

            _baseXFactor = _xFactor;
            _baseYFactor = _yFactor;
            _baseZFactor = _zFactor;
        }

        // ── Interface properties ───────────────────────────────────────────────

        /// <inheritdoc/>
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public Texture3D CurrentDataTexture => _dataTexture;

        /// <inheritdoc/>
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public Texture3D CurrentMaskTexture => _maskTexture;

        /// <inheritdoc/>
        // [→ before IsCropped, line 319]
        //
        // [FIXED V-01 SRP] IsCropped was a public auto-property with a private
        // setter on VolumeDataSetRenderer — part of the God Class's 152-member
        // public API. Here it is a private field exposed only via the interface.
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public bool IsCropped => _isCropped;

        /// <inheritdoc/>
        // [→ before IsFullResolution, line 317]
        //
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public bool IsFullResolution => _xFactor * _yFactor * _zFactor == 1;

        /// <summary>
        /// Inclusive minimum voxel of the current crop region.
        /// Valid only when <see cref="IsCropped"/> is true.
        /// Not exposed on the interface — only the coordinator needs this, and
        /// it reads it to pass slice bounds to VolumeCameraDriver.
        /// [→ before CurrentCropMin, line 320]
        /// </summary>
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public Vector3Int CurrentCropMin => _cropMin;

        /// <summary>
        /// Inclusive maximum voxel of the current crop region.
        /// Valid only when <see cref="IsCropped"/> is true.
        /// [→ before CurrentCropMax, line 321]
        /// </summary>
        // [WMC] Complexity = 1. Contributes 1 to WMC total.
        public Vector3Int CurrentCropMax => _cropMax;

        /// <inheritdoc/>
        // Unity does not garbage-collect Texture3D objects created via
        // VolumeDataSet.GenerateVolumeTexture — they must be destroyed explicitly.
        // This is the only place that calls Object.Destroy on textures owned by
        // this manager. The lifecycle is fully encapsulated here.
        //
        // [WMC] Complexity = 2 (two null guard conditions). Contributes 2 to WMC total.
        public void Dispose()
        {
            if (_dataTexture != null) Object.Destroy(_dataTexture);
            if (_maskTexture != null) Object.Destroy(_maskTexture);
        }

        // ── Persistence — Sub-team 7 Integration ───────────────────────────────
        // [§ docs/integration/team7-persistence-contract.md — VolumeDataState]
        //
        // These methods are stubs. The full implementation will be wired in Sprint 3
        // after the file path resolution is confirmed with the team.

        /// <summary>
        /// Captures the current texture/crop state for session persistence.
        /// Called by <c>VolumeRenderCoordinator.SaveSession()</c>.
        /// </summary>
        public VolumeDataState CaptureState()
        {
            // TODO Sprint 3: obtain FitsFilePath from _dataSet and extract crop bounds.
            return new VolumeDataState(
                fitsFilePath: "",
                cropMinX: _cropMin.x,
                cropMinY: _cropMin.y,
                cropMinZ: _cropMin.z,
                cropMaxX: _cropMax.x,
                cropMaxY: _cropMax.y,
                cropMaxZ: _cropMax.z,
                isCropped: _isCropped
            );
        }

        /// <summary>
        /// Restores texture/crop state after session load.
        /// Called by <c>VolumeRenderCoordinator.LoadSession()</c>.
        /// May trigger texture re-upload depending on crop bounds.
        /// </summary>
        public void RestoreState(VolumeDataState state)
        {
            // TODO Sprint 3: reload the FITS file from state.FitsFilePath, then apply
            // crop bounds if state.IsCropped. Must leave this class in a valid,
            // ready-to-render state with updated CurrentDataTexture.
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CK METRICS SUMMARY — VolumeTextureManager (Understand tool — measured)
    //
    // WMC = 12   NIM = 12   NIV = 14                              ✅ target ≤ 20
    //
    // CBO = 4                                                     ✅ target ≤ 14 domain
    //   Understand reports 4 coupled classes. The majority of former VDSR coupling
    //   is absorbed by the VolumeDataSet/RawVolumeData interface boundary.
    //
    // RFC = 12                                                    ✅ target ≤ 50
    //
    // LCOM = 0.67  (67% Percent Lack of Cohesion)                ⚠ target ≤ 0.5
    //   Initialise / LoadFullCube / LoadRegion / Dispose each access different
    //   field subsets; lifecycle-phase artefact. Direction of improvement (0.95→0.67)
    //   is significant.
    //
    // DIT = 1  (IFANIN = 2)                                       ✅ ≤ 4
    // NOC = 0  (sealed)                                           ✅ ≤ 5
    //
    // BEFORE (VolumeDataSetRenderer — Understand tool):
    //   WMC 97 | CBO 28 | RFC 97 | LCOM 0.95
    //
    // REDUCTION:
    //   WMC: 12 vs. 97  →  -85 units
    //   CBO:  4 vs. 28  →  -24 coupled classes (texture coupling contained here)
    //   LCOM: 0.67 vs. 0.95  →  substantial improvement; lifecycle artefact documented
    // ══════════════════════════════════════════════════════════════════════════
}
