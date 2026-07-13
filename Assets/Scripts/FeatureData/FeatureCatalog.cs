/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureCatalog owns the identity and persistence of the FeatureSets in a
 * session. It takes over the list management and file writing that used to live
 * in FeatureSetManager, but keeps no Unity types so it can be unit tested on its
 * own. File I/O goes through IFeaturePersistenceService, and changes to the
 * registry are announced with the SetRegistered / SetRemoved events.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Registry and persistence gateway for the <see cref="FeatureSet"/> instances
    /// in a session. Has no Unity dependency.
    /// </summary>
    public sealed class FeatureCatalog
    {
        // Colour palette, mirrors the original FeatureSetManager.FeatureColors
        public static readonly FeatureColor[] FeatureColors =
        {
            new FeatureColor(0f,  1f,  1f),        // cyan
            new FeatureColor(1f,  0f,  0.5f),      // hot pink
            new FeatureColor(1f,  1f,  0f),        // yellow
            new FeatureColor(0f,  0f,  1f),        // blue
            new FeatureColor(1f,  0f,  1f),        // magenta
            new FeatureColor(0f,  1f,  0f),        // green
            new FeatureColor(1f,  0f,  0f),        // red
            new FeatureColor(0.8f,0.8f,0.8f),      // light grey
            new FeatureColor(1f,  0.5f,0f),        // orange
        };

        // Keys whose values carry a physical unit suffix in the info panel
        public static readonly string[] UnitisedKeys = { "SUM", "PEAK" };

        // Typed set registries
        private readonly List<FeatureSet> _maskSets     = new List<FeatureSet>();
        private readonly List<FeatureSet> _importedSets = new List<FeatureSet>();
        private readonly List<FeatureSet> _userSets     = new List<FeatureSet>();

        public ReadOnlyCollection<FeatureSet> MaskSets     => _maskSets.AsReadOnly();
        public ReadOnlyCollection<FeatureSet> ImportedSets => _importedSets.AsReadOnly();
        public ReadOnlyCollection<FeatureSet> UserSets     => _userSets.AsReadOnly();

        /// <summary>
        /// The single transient set used for the active VR selection box.
        /// Null until <see cref="EnsureSelectionSet"/> is called.
        /// </summary>
        public FeatureSet SelectionSet { get; private set; }

        // Session output file name, timestamped when the catalogue is constructed
        public string SessionOutputFileName { get; }

        // Persistence boundary, injected (WP7 provides the implementation)
        private readonly IFeaturePersistenceService _persistence;

        /// <summary>Raised when a new FeatureSet is registered in any list.</summary>
        public event Action<FeatureSet> SetRegistered;

        /// <summary>Raised when a FeatureSet is removed from any list.</summary>
        public event Action<FeatureSet> SetRemoved;

        /// <param name="persistence">
        ///   Persistence service implementation (provided by WP7).
        ///   Pass a <c>NullFeaturePersistenceService</c> in unit tests.
        /// </param>
        public FeatureCatalog(IFeaturePersistenceService persistence)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            SessionOutputFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".ascii";
        }

        // FeatureSet factory methods

        /// <summary>
        /// Creates and registers a new mask-derived feature set.
        /// Called by FeatureSetService when mask analysis completes.
        /// </summary>
        public FeatureSet CreateMaskSet()
        {
            var set = new FeatureSet(
                name:    "Mask Source Set",
                index:   _maskSets.Count,
                setType: FeatureSetType.Mask,
                color:   FeatureColor.Magenta);
            _maskSets.Add(set);
            SetRegistered?.Invoke(set);
            return set;
        }

        /// <summary>
        /// Creates and registers a new imported feature set.
        /// Called by FeatureSetService after loading a VOTable or FITS catalogue.
        /// </summary>
        /// <param name="name">Catalogue name shown in the UI.</param>
        public FeatureSet CreateImportedSet(string name)
        {
            int idx   = _importedSets.Count;
            var color = idx < FeatureColors.Length ? FeatureColors[idx] : FeatureColor.Cyan;
            var set   = new FeatureSet(name, idx, FeatureSetType.Imported, color);
            _importedSets.Add(set);
            SetRegistered?.Invoke(set);
            return set;
        }

        /// <summary>
        /// Creates and registers a new user-drawn feature set (VR creation flow).
        /// Initialises RawData columns required for VOTable round-trip.
        /// </summary>
        public FeatureSet CreateUserSet()
        {
            var set = new FeatureSet(
                name:    "New Feature Set",
                index:   _userSets.Count,
                setType: FeatureSetType.New,
                color:   FeatureColor.Green)
            {
                RawDataKeys  = new[] { "RawData" },
                RawDataTypes = new[] { "char" }
            };
            _userSets.Add(set);
            SetRegistered?.Invoke(set);
            return set;
        }

        /// <summary>
        /// Ensures the singleton selection set exists and returns it.
        /// Safe to call repeatedly; returns the existing set once created.
        /// </summary>
        public FeatureSet EnsureSelectionSet()
        {
            if (SelectionSet != null) return SelectionSet;

            SelectionSet = new FeatureSet(
                name:    "Selection Set",
                index:   0,
                setType: FeatureSetType.Selection,
                color:   FeatureColor.White)
            {
                RawDataKeys  = new[] { "RawData" },
                RawDataTypes = new[] { "string" }
            };
            return SelectionSet;
        }

        /// <summary>
        /// Removes a set from whichever typed list owns it.
        /// </summary>
        public bool Remove(FeatureSet set)
        {
            if (set == null) return false;
            bool removed = _maskSets.Remove(set)
                        || _importedSets.Remove(set)
                        || _userSets.Remove(set);
            if (removed) SetRemoved?.Invoke(set);
            return removed;
        }

        // Persistence (delegates to the WP7 service)

        /// <summary>
        /// Persists a user-created feature to the session ASCII file.
        /// The catalogue decides when to write; the persistence service decides how.
        /// </summary>
        public void AppendFeatureToSessionFile(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            _persistence.AppendFeatureToAscii(feature, SessionOutputFileName);
        }

        /// <summary>
        /// Exports a complete FeatureSet to a VOTable XML file.
        /// Returns the path of the file written (for UI confirmation messages).
        /// </summary>
        public string ExportToVoTable(FeatureSet set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            return _persistence.ExportToVoTable(set);
        }

        // Utility

        /// <summary>
        /// Iterates all registered sets regardless of type, for hit-testing
        /// selection across the full catalogue.
        /// </summary>
        public IEnumerable<FeatureSet> AllSets()
        {
            foreach (var s in _maskSets)     yield return s;
            foreach (var s in _importedSets) yield return s;
            foreach (var s in _userSets)     yield return s;
            if (SelectionSet != null)        yield return SelectionSet;
        }
    }
}