// SPDX-License-Identifier: LGPL-3.0-or-later
// VolumeDataSet — refactored from Assets/Scripts/VolumeData/VolumeDataSet.cs
// (1920 LOC god class). Per global_model.md §1 ST1 line 13, the aggregate
// itself moves to ST1 ownership (not ST3 — despite the file living under
// refactored/Rendering/ alongside the renderer decomposition). It holds
// *references* to ST2-realised sub-ports, not raw data.
//
// ⚠ Skeleton authored with ASSUMPTION blocks because refactor_plan.md does
//    NOT contain a per-method hotspot table for VolumeDataSet. Every open
//    design question is recorded inline so the assumption is auditable.
//
// Realises IVolumeDataSet (declared in External/IVolumeDataSet.cs); cross-team
// consumers receive IVolumeDataSet only.

using System.Collections.Generic;
using iDaVIE.Data.Contracts;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Kernel
{
    // ASSUMPTION (file path): the canonical home for this class is
    // refactored/Kernel/VolumeDataSet.cs. The skeleton currently lives at
    // refactored/Rendering/ so it sits alongside its peer renderer. If ST1's
    // Kernel/ directory is later promoted as the boundary, move the file —
    // no namespace change required.

    internal sealed class VolumeDataSet : IVolumeDataSet
    {
        // ASSUMPTION (file I/O ownership): per global_model.md §1 ST1 line 13
        // ("Holds *references* (not data) to raw voxel / mask / WCS / brush-history
        // services realised by ST2"), file open / close / SetSubcube orchestration
        // lives on IVolumeLoader (ST1), not on this aggregate. This class is
        // *populated by* IVolumeLoader and exposes a read-only snapshot.

        // ASSUMPTION (histogram lazy evaluation): GetHistogram / GetStats delegate
        // to IDataAnalysisPlugin on first call; results are cached on this aggregate
        // until DatasetUnloaded fires. The IDataAnalysisPlugin contract in
        // shared_interfaces.md §5.5 only declares ComputeRegionStats — cube-wide
        // stats would need an extension to that interface (raise as a Tier-2
        // contract change against ST5/ST2).

        // ASSUMPTION (WCS frame ownership): the AstFrameSet is held inside
        // WcsTransformPlugin (ST2); this aggregate exposes only the narrow
        // IWcsMapping facade via the sub-ports listed in IVolumeDataSet.

        // ── IVolumeDataSet ──────────────────────────────────────────────────
        public LoadStatus    Status        { get; private set; }
        public string        FilePath      { get; private set; } = "";
        public int           HduIndex      { get; private set; }
        public VolumeExtents Extents       { get; private set; }
        public SubcubeBounds SubcubeBounds { get; private set; }
        public IReadOnlyDictionary<string, string> HeaderDictionary { get; private set; }
            = new Dictionary<string, string>();

        public DataStats     GetStats()      => throw new System.NotImplementedException();
        public HistogramData GetHistogram()  => throw new System.NotImplementedException();
        public AxisUnits     GetAxisUnits()  => throw new System.NotImplementedException();
        public string        FormatCoord(CartesianCoord coord) => throw new System.NotImplementedException();

        public IRawVoxelAccess RawVoxelAccess { get; private set; }
        public IMaskEditState  MaskEditState  { get; private set; }

        // ── Internal sub-port held but not exposed on IVolumeDataSet ────────
        // ASSUMPTION (WCS exposure): consumers reach WCS via the ST2-owned
        // ICoordinateTransformer rather than via this aggregate. Held internally
        // so FormatCoord / GetAxisUnits can delegate to IWcsMapping without
        // exposing the full plug-in interface across the boundary.
        internal IWcsMapping WcsMapping { get; private set; }
    }
}
