// SPDX-License-Identifier: LGPL-3.0-or-later
// CartesianCoordOps — ST5-internal arithmetic extensions on CartesianCoord and
// IFeature geometry. Named per ST5_domain_design.md §4.2 — kept off the
// cross-team contract because CartesianCoord is a shared kernel value struct
// owned by ST1 (shared_interfaces.md §1.1).
//
// Centralises the bounds-arithmetic that previously lived inline in
// SelectionService (AABB containment), VoTableSaver (per-row min/max),
// FeatureSetService (Capture / SnapshotFeature / OnSourceStatsUpdated), and
// FeatureFactory.PopulateFromSourceStats. Information Expert (GRASP) — the
// Feature is the expert for its own geometry; this file provides the
// corresponding helpers without expanding the IFeature surface area.

using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord

namespace iDaVIE.Features
{
    internal static class CartesianCoordOps
    {
        public static CartesianCoord Midpoint(this CartesianCoord min, CartesianCoord max)
            => new((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);

        public static CartesianCoord Extent(this CartesianCoord min, CartesianCoord max)
            => new(max.X - min.X, max.Y - min.Y, max.Z - min.Z);

        public static CartesianCoord BoundsMin(this IFeature feature)
            => new(feature.Center.X - feature.Size.X / 2,
                   feature.Center.Y - feature.Size.Y / 2,
                   feature.Center.Z - feature.Size.Z / 2);

        public static CartesianCoord BoundsMax(this IFeature feature)
            => new(feature.Center.X + feature.Size.X / 2,
                   feature.Center.Y + feature.Size.Y / 2,
                   feature.Center.Z + feature.Size.Z / 2);

        /// <summary>Inclusive AABB containment test in voxel space.
        /// Replaces the inline six-comparison block in SelectionService.SelectAtCursor.</summary>
        public static bool Contains(this IFeature feature, CartesianCoord point)
        {
            var min = feature.BoundsMin();
            var max = feature.BoundsMax();
            return point.X >= min.X && point.X <= max.X
                && point.Y >= min.Y && point.Y <= max.Y
                && point.Z >= min.Z && point.Z <= max.Z;
        }
    }
}
