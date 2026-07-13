/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * IFeatureSystemPort.cs
 * Boundary interface between Team 4 (Koffiewinkel / interaction layer) and
 * Team 5 (Feature domain). All coordinates are in voxel space.
 * Team 5 owns the implementation; Team 4 depends only on this interface.
 */

using DataFeatures;

namespace iDaVIE.Application.Feature
{
    // Request value objects

    /// <summary>Parameters for creating a selection feature from a VR bounding box.</summary>
    public sealed class SelectionRequest
    {
        public Vec3 BoundsMin { get; }
        public Vec3 BoundsMax { get; }

        public SelectionRequest(Vec3 boundsMin, Vec3 boundsMax)
        {
            BoundsMin = boundsMin;
            BoundsMax = boundsMax;
        }
    }

    /// <summary>Parameters for updating the bounding region of an existing feature.</summary>
    public sealed class RegionEditRequest
    {
        public string FeatureId    { get; }
        public Vec3   NewBoundsMin { get; }
        public Vec3   NewBoundsMax { get; }

        public RegionEditRequest(string featureId, Vec3 newBoundsMin, Vec3 newBoundsMax)
        {
            FeatureId    = featureId;
            NewBoundsMin = newBoundsMin;
            NewBoundsMax = newBoundsMax;
        }
    }

    /// <summary>
    /// Initiates a paint stroke. The caller assigns <see cref="StrokeId"/> and uses
    /// it in subsequent <see cref="IFeatureSystemPort.ApplyPaintPoint"/> and
    /// <see cref="IFeatureSystemPort.EndPaintStroke"/> calls.
    /// </summary>
    public sealed class PaintStrokeRequest
    {
        public string StrokeId { get; }

        public PaintStrokeRequest(string strokeId)
        {
            StrokeId = strokeId;
        }
    }

    /// <summary>A single painted voxel point belonging to an in-progress stroke.</summary>
    public sealed class PaintPointRequest
    {
        public string StrokeId { get; }
        /// <summary>Voxel-space coordinate of this paint sample.</summary>
        public Vec3 Point { get; }

        public PaintPointRequest(string strokeId, Vec3 point)
        {
            StrokeId = strokeId;
            Point    = point;
        }
    }

    // Port interface

    /// <summary>
    /// Boundary port that Team 4 (Koffiewinkel) calls to create, edit, and select
    /// features in the Team 5 domain model.
    ///
    /// Coordinate convention: all Vec3 values are in voxel space.
    /// Feature IDs returned are the string form of <see cref="DataFeatures.Feature.Id"/>.
    /// Team 5 owns source-ID validation.
    /// </summary>
    public interface IFeatureSystemPort
    {
        /// <summary>
        /// Creates (or replaces) the transient selection feature from a VR bounding box.
        /// Returns the feature ID, or null on failure.
        /// </summary>
        string CreateSelectionFeature(SelectionRequest request);

        /// <summary>Updates the bounding box of an existing feature in-place.</summary>
        void UpdateFeatureRegion(RegionEditRequest request);

        /// <summary>
        /// Begins a new paint stroke. Use <see cref="PaintStrokeRequest.StrokeId"/> to
        /// correlate with subsequent <see cref="ApplyPaintPoint"/> and
        /// <see cref="EndPaintStroke"/> calls.
        /// </summary>
        void BeginPaintStroke(PaintStrokeRequest request);

        /// <summary>
        /// Records a voxel-space paint point. The stroke's bounding box expands to include it.
        /// Silently ignored if <paramref name="request"/>'s StrokeId is unknown.
        /// </summary>
        void ApplyPaintPoint(PaintPointRequest request);

        /// <summary>
        /// Finalises the stroke, committing its accumulated bounding box as a new selection
        /// feature. Does nothing if the stroke had no points or the ID is unknown.
        /// </summary>
        void EndPaintStroke(string strokeId);

        /// <summary>
        /// Selects the feature with the given ID. Does nothing if the ID is not found.
        /// </summary>
        void SelectFeature(string featureId);
    }
}
