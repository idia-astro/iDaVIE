// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureSet — concrete mutable collection implementing IFeatureSet.
// Replaces the legacy FeatureSetRenderer's List<Feature>, FeatureColor, and
// featureSetVisible fields. Internal sealed per DD-5; cross-team consumers
// see IFeatureSet only.

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Types;   // FeatureColour

namespace iDaVIE.Features
{
    internal sealed class FeatureSet : IFeatureSet
    {
        private readonly List<IFeature> _features = new();
        // OriginId → positional index in _features. Maintained alongside the list so
        // FeatureVisualiser can resolve the dirty-feature OriginIds it receives via
        // IFeatureDirtyListener back to a vertex-buffer offset in O(1). Mirrors the
        // legacy positional-index lookup that lived inside FeatureSetRenderer.Update.
        private readonly Dictionary<int, int> _indexByOriginId = new();
        // Callback injected by FeatureSetService at construction so any display-state
        // mutation routes through one observable seam (Protected Variations, GRASP).
        private readonly Action _onChanged;

        public FeatureSet(int index, string fileName, FeatureSetType type,
                          IReadOnlyList<string> rawDataKeys,
                          FeatureColour displayColour,
                          IFeatureDirtyListener listener,
                          Action onChanged)
        {
            Index          = index;
            FileName       = fileName;
            Type           = type;
            RawDataKeys    = rawDataKeys;
            _displayColour = displayColour;
            Listener       = listener;
            _onChanged     = onChanged;

            // Bypass the setter — _onChanged must not fire before the service
            // can hold a reference to this set; FeatureSetService.CreateSet
            // raises the empty-set event after registration.
            _isVisible     = true;
        }

        public int                     Index       { get; }
        public string                  FileName    { get; }
        public FeatureSetType          Type        { get; }
        public IReadOnlyList<IFeature> Features    => _features.AsReadOnly();
        public IReadOnlyList<string>   RawDataKeys { get; }

        /// <summary>Shared per-set listener — every Feature attached to this set
        /// notifies through it. Held here so FeatureSetService can construct
        /// new features without a separate listener registry.</summary>
        internal IFeatureDirtyListener Listener { get; }

        private FeatureColour _displayColour;
        public FeatureColour DisplayColour
        {
            get => _displayColour;
            internal set
            {
                if (_displayColour.Equals(value)) return;
                _displayColour = value;
                // Whole-set repaint — OriginId -1 is the visualiser's all-dirty sentinel.
                Listener.OnFeatureDirty(-1);
                _onChanged();
            }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            internal set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                // Visibility flips the per-vertex Visibility flag in the GPU buffer,
                // so the visualiser must re-emit every cube.
                Listener.OnFeatureDirty(-1);
                _onChanged();
            }
        }

        /// <summary>Single-feature append. Silent — the calling service raises
        /// FeatureSetChanged once per batch (bulk-population rule, ST5_interface.md §6).
        /// The vertex buffer is repainted via the per-feature dirty notification only;
        /// callers performing bulk loads should prefer AddFeatures.</summary>
        internal void AddFeature(IFeature feature)
        {
            ((Feature)feature).SetListener(Listener);
            _indexByOriginId[feature.OriginId] = _features.Count;
            _features.Add(feature);
            Listener.OnFeatureDirty(feature.OriginId);
        }

        /// <summary>Bulk attach — raises a single all-dirty notification at the end
        /// instead of one per feature, which would otherwise resize the visualiser
        /// compute buffer N times during import (legacy SpawnFeaturesFromTable hot path).</summary>
        internal void AddFeatures(IEnumerable<IFeature> features)
        {
            foreach (var feature in features)
            {
                ((Feature)feature).SetListener(Listener);
                _indexByOriginId[feature.OriginId] = _features.Count;
                _features.Add(feature);
            }
            Listener.OnFeatureDirty(-1);
        }

        internal void RemoveFeature(IFeature feature)
        {
            var positional = _features.IndexOf(feature);
            if (positional < 0) return;
            _features.RemoveAt(positional);
            _indexByOriginId.Remove(feature.OriginId);
            // Trailing indices shifted by one — rebuild the slice rather than fix up
            // in place because GPU geometry needs a full re-emit anyway.
            for (var i = positional; i < _features.Count; i++)
                _indexByOriginId[_features[i].OriginId] = i;
            Listener.OnFeatureDirty(-1);
        }

        /// <summary>Drops every feature; replaces the legacy FeatureList.Clear path.</summary>
        internal void ClearFeatures()
        {
            if (_features.Count == 0) return;
            _features.Clear();
            _indexByOriginId.Clear();
            Listener.OnFeatureDirty(-1);
        }

        /// <summary>OriginId → buffer index lookup for the visualiser; -1 if not found.
        /// Stable across the set's lifetime as long as the feature is not removed.</summary>
        internal int IndexOf(int originId)
            => _indexByOriginId.TryGetValue(originId, out var i) ? i : -1;
    }
}
