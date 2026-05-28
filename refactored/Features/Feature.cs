// SPDX-License-Identifier: LGPL-3.0-or-later
// Feature — domain aggregate. Plain C#: no UnityEngine, no SteamVR (§4.2.3).
//
// Refactor delta vs Assets/Scripts/FeatureData/Feature.cs:
//   - CornerMin/CornerMax (Vector3) replaced by Center/Size (CartesianCoord).
//   - CubeColor / Visible per-feature removed — display state lives on FeatureSet (DD-14).
//   - FeatureSetParent back-pointer removed; notifications go through IFeatureDirtyListener
//     wired by the owning FeatureSet, breaking the circular-ownership cycle the T2
//     baseline picked up at Feature ↔ FeatureSetRenderer 56% coupling over 30 revisions.
//   - Statistics held here (Information Expert, DD-2); updates routed via UpdateStatistics.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord

namespace iDaVIE.Features
{
    internal sealed class Feature : IFeature
    {
        // Wired by FeatureSet.AddFeature so every Feature in the same set shares
        // one listener instance (the FeatureVisualiser for that set).
        private IFeatureDirtyListener _listener;
        internal void SetListener(IFeatureDirtyListener listener) => _listener = listener;

        public Feature(int originId, string name, string flag,
                       CartesianCoord center, CartesianCoord size,
                       IReadOnlyList<string> rawDataValues)
        {
            OriginId      = originId;
            Name          = name;
            Flag          = flag;
            Center        = center;
            Size          = size;
            RawDataValues = rawDataValues;
        }

        public int                   OriginId      { get; }
        public string                Name          { get; }
        public string                Flag          { get; private set; }
        public CartesianCoord        Center        { get; private set; }
        public CartesianCoord        Size          { get; private set; }
        public IReadOnlyList<string> RawDataValues { get; }
        public IFeatureStatistics?   Statistics    { get; private set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            // Internal: only SelectionService inside iDaVIE.Features may mutate
            // selection; cross-team holders see the read-only IFeature surface.
            internal set { _isSelected = value; _listener.OnFeatureDirty(OriginId); }
        }

        internal void SetFlag(string flag)
        {
            Flag = flag;
            _listener.OnFeatureDirty(OriginId);
        }

        /// <summary>Called for two flows: SourceStatsUpdated (mask edit changed
        /// voxel extent) and SetFeatureBounds (anchor-drag editing). Affects
        /// vertex data — listener is notified.</summary>
        internal void UpdateGeometry(CartesianCoord center, CartesianCoord size)
        {
            Center = center;
            Size   = size;
            _listener.OnFeatureDirty(OriginId);
        }

        /// <summary>Statistics changes don't affect GPU vertex data — listener is NOT notified.</summary>
        internal void UpdateStatistics(IFeatureStatistics? stats) => Statistics = stats;
    }
}
