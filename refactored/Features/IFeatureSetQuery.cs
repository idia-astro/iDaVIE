// SPDX-License-Identifier: LGPL-3.0-or-later
// IFeatureSetQuery — application-layer mutation surface.
// Merges query + display mutation + set-membership mutation + per-feature
// mutation + FeatureSetChanged event. ISP trade-off accepted (DD-9 in ST5 proposal):
// every member is reached from the same use cases — the desktop panel and the
// VR menu both query, toggle, and listen as one composite operation.

using System;
using iDaVIE.Kernel.Contracts.Types;
using System.Collections.Generic;

namespace iDaVIE.Features
{
    public interface IFeatureSetQuery
    {
        IReadOnlyList<IFeatureSet> GetAllFeatureSets();
        IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type);

        void SetVisible      (IFeatureSet featureSet, bool visible);
        void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour);

        /// <summary>Updates a non-Mask feature's bounding box. Mask bounds are owned by ST2.</summary>
        void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax);

        void SetFeatureFlag(IFeature feature, string flag);

        /// <summary>Append source to a UserDefined set, creating one lazily if none exists.</summary>
        void CopyFeatureToUserDefined(IFeature source);

        /// <summary>Bounds of the singleton SelectionBox feature; lazily created on first call.</summary>
        void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax);

        event Action FeatureSetChanged;
    }

    public interface IFeatureSelectionService
    {
        IFeature?    SelectedFeature    { get; }
        IFeatureSet? SelectedFeatureSet { get; }

        bool SelectAtCursor(CartesianCoord cursorVoxelSpace);
        void SelectFeature(IFeature feature, IFeatureSet owningSet);
        void DeselectFeature();

        event Action<IFeature?> SelectionChanged;
    }
}
