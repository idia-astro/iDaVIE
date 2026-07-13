/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureSet is the domain container for a named collection of Feature objects.
 * The old FeatureSetRenderer (a MonoBehaviour) mixed this list management with
 * GPU ComputeBuffer handling, VOTable export, and Unity lifecycle hooks. Here
 * the set holds only domain state (the list, metadata, colour) and has no Unity
 * dependency, so it can be unit tested without the runtime. The GPU side lives
 * in WP3 (FeatureVisualiser / IFeatureRenderer): the set raises FeatureDirty
 * when it changes and the renderer subscribes and redraws.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using iDaVIE.Domain.Feature;

namespace DataFeatures
{
    /// <summary>Classifies what a <see cref="FeatureSet"/> represents.</summary>
    public enum FeatureSetType
    {
        Unassigned,
        Mask,
        New,
        Imported,
        Selection
    }

    /// <summary>
    /// An ordered, named collection of <see cref="Feature"/> objects with a type tag
    /// and a display colour. No Unity types, no I/O.
    /// </summary>
    public sealed class FeatureSet
    {
        // Identity
        public string Name    { get; }
        public int    Index   { get; }
        public FeatureSetType SetType { get; }

        // Display colour. Kept as a plain struct so this file has no Unity
        // dependency; the visualiser layer converts FeatureColor to UnityEngine.Color.
        public FeatureColor Color { get; set; }

        // Catalogue columns, needed for the VOTable round-trip
        public string[] RawDataKeys  { get; set; } = Array.Empty<string>();
        public string[] RawDataTypes { get; set; } = Array.Empty<string>();
        public string   FileName     { get; set; } = string.Empty;

        // Coordinate frame, passed to ICoordinateTransformer during export
        public IAstFrame AstFrame   { get; set; } = new NullAstFrame();
        public string    ZAxisLabel { get; set; } = "z_phys";

        // Feature list
        private readonly List<Feature> _features = new List<Feature>();

        /// <summary>Read-only view of the feature list.</summary>
        public ReadOnlyCollection<Feature> Features => _features.AsReadOnly();

        /// <summary>
        /// Raised whenever the contents or visual state of the set changes. The
        /// visualiser subscribes to redraw. The int argument is the affected
        /// feature index, or -1 to mean everything is dirty.
        /// </summary>
        public event Action<int> FeatureDirty;

        public FeatureSet(string name, int index, FeatureSetType setType, FeatureColor color)
        {
            Name    = name  ?? throw new ArgumentNullException(nameof(name));
            Index   = index;
            SetType = setType;
            Color   = color;
        }

        // Mutation methods

        /// <summary>Adds a feature and wires its dirty-notification back to this set.</summary>
        public void Add(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            feature.SetParent(this);
            _features.Add(feature);
            FeatureDirty?.Invoke(feature.Index);
        }

        /// <summary>Removes a feature and unwires its parent reference.</summary>
        public bool Remove(Feature feature)
        {
            if (feature == null) return false;
            bool removed = _features.Remove(feature);
            if (removed)
            {
                feature.ClearParent();
                FeatureDirty?.Invoke(-1);   // full redraw needed after removal
            }
            return removed;
        }

        /// <summary>Clears all features and triggers a full redraw.</summary>
        public void Clear()
        {
            foreach (var f in _features) f.ClearParent();
            _features.Clear();
            FeatureDirty?.Invoke(-1);
        }

        // Visibility helpers

        public void ShowAll()  => SetAllVisibility(true);
        public void HideAll()  => SetAllVisibility(false);

        public void ToggleVisibility()
        {
            foreach (var f in _features)
                f.Visible = !f.Visible;
        }

        private void SetAllVisibility(bool visible)
        {
            foreach (var f in _features)
                f.Visible = visible;
        }

        // Internal dirty propagation, called by Feature
        internal void NotifyDirty(int featureIndex)
        {
            FeatureDirty?.Invoke(featureIndex);
        }
    }

}