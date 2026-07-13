/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * IFeature lets Masked, Imported, and User-defined features sit behind one
 * interface so callers don't have to care which kind they're handling.
 *
 * Implemented by:
 *   Feature             concrete base (all three kinds share this class for now)
 *   MaskedFeature       feature derived from volume mask analysis
 *   ImportedFeature     feature loaded from a VOTable or FITS catalogue
 *   UserDefinedFeature  feature drawn interactively in VR
 *
 * Consumers (FeatureSetService, VoTableExportService, FeatureCatalog) depend only
 * on IFeature, so they stay unaware of which kind they are processing.
 */

using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Domain contract for a single marked region in the data volume.
    /// All three feature kinds (Masked, Imported, User-defined) satisfy it; callers
    /// that only query or export features should depend on this interface rather than
    /// the concrete <see cref="Feature"/> class.
    /// </summary>
    public interface IFeature
    {
        // Identity

        /// <summary>Position of this feature within its parent <see cref="FeatureSet"/>.</summary>
        int Index { get; set; }

        /// <summary>Stable numeric identifier assigned at creation time.</summary>
        int Id { get; }

        /// <summary>Human-readable label (e.g. source catalogue name).</summary>
        string Name { get; }

        // Mutable domain state

        /// <summary>Quality flag string (e.g. "1" = good, "3" = uncertain).</summary>
        string Flag { get; set; }

        /// <summary>Per-feature statistics columns (parallel to FeatureSet.RawDataKeys).</summary>
        string[] RawData { get; set; }

        /// <summary>Display colour.</summary>
        FeatureColor CubeColor { get; set; }

        /// <summary>Whether this feature is rendered.</summary>
        bool Visible { get; set; }

        /// <summary>Whether this feature is currently selected in the UI.</summary>
        bool Selected { get; set; }

        // Geometry

        /// <summary>Minimum corner of the axis-aligned bounding box (voxel space).</summary>
        Vec3 CornerMin { get; }

        /// <summary>Maximum corner of the axis-aligned bounding box (voxel space).</summary>
        Vec3 CornerMax { get; }

        /// <summary>
        /// Geometric centre of the bounding box.
        /// Settable: translates both corners by the displacement from the old centre.
        /// </summary>
        Vec3 Center { get; set; }

        /// <summary>
        /// Axis-aligned extent, padded by one voxel on each axis so that a single-voxel
        /// feature reports Size = (1, 1, 1).
        /// </summary>
        Vec3 Size { get; set; }

        /// <summary>Product of <see cref="Size"/> components (voxel count of the bounding box).</summary>
        float Volume { get; }

        // Behaviour

        /// <summary>
        /// Replaces the bounding-box corners and notifies the parent set that this
        /// feature is dirty.
        /// </summary>
        void SetBounds(Vec3 cornerMin, Vec3 cornerMax);

        /// <summary>Returns true when <paramref name="point"/> lies within [CornerMin, CornerMax] (inclusive).</summary>
        bool ContainsPoint(Vec3 point);

        /// <summary>
        /// Returns true when this feature's geometric centre lies outside the supplied
        /// volume bounds; used to discard out-of-range features during import.
        /// </summary>
        bool IsOutsideVolume(Vec3 volumeMin, Vec3 volumeMax);
    }
}
