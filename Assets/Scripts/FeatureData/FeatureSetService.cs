/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureSetService is the application-layer service that runs the use-cases
 * spanning FeatureSet and Feature: importing catalogues, creating and selecting
 * features, adding a selection to a user set, and exporting. These all used to
 * be tangled into FeatureSetManager alongside rendering and GUI calls.
 *
 * It works only on domain objects and raises events for FeatureVisualiser and
 * the GUI to react to, so it has no Unity types and can be tested with mocks.
 * Dependencies are constructor-injected:
 *   FeatureCatalog              registry and persistence gateway
 *   IFeatureTableLoader         data I/O boundary (WP2)
 *   IFeatureStatisticsProvider  statistics boundary (PluginInterface/DataAnalysis)
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using DataFeatures;
using iDaVIE.Domain.Feature;

namespace iDaVIE.Application.Feature
{
    // Boundary interfaces (defined here, implemented by other WPs)

    /// <summary>
    /// Loads a FeatureTable from a file path.
    /// Implemented by WP2 (Data I/O). Injected into FeatureSetService.
    /// </summary>
    public interface IFeatureTableLoader
    {
        /// <summary>Returns null and populates <paramref name="error"/> on failure.</summary>
        FeatureTable Load(string filePath, out string error);
    }

    // IFeatureStatisticsProvider and FeatureStatistics live in FeatureStatistics.cs.

    /// <summary>
    /// Orchestrates feature domain use-cases. Has no Unity dependency.
    /// </summary>
    public sealed class FeatureSetService
    {
        private readonly FeatureCatalog              _catalog;
        private readonly IFeatureTableLoader         _loader;
        private readonly IFeatureStatisticsProvider  _statistics;

        // Selection state
        private Feature _selectedFeature;

        /// <summary>The currently selected feature, or null.</summary>
        public Feature SelectedFeature
        {
            get => _selectedFeature;
            private set
            {
                var previous = _selectedFeature;
                _selectedFeature = value;
                FeatureSelectionChanged?.Invoke(previous, value);
            }
        }

        // Domain events

        /// <summary>
        /// Raised when the selection changes.
        /// Args are (previousFeature, newFeature); either may be null.
        /// FeatureVisualiser subscribes to spawn/hide anchor handles.
        /// </summary>
        public event Action<Feature, Feature> FeatureSelectionChanged;

        /// <summary>Raised when a mask-derived feature is selected (triggers stats panel update).</summary>
        public event Action MaskFeatureSelected;

        /// <summary>Raised after a FeatureSet is fully imported and populated.</summary>
        public event Action<FeatureSet> FeatureSetImported;

        public FeatureSetService(
            FeatureCatalog             catalog,
            IFeatureTableLoader        loader,
            IFeatureStatisticsProvider statistics)
        {
            _catalog    = catalog    ?? throw new ArgumentNullException(nameof(catalog));
            _loader     = loader     ?? throw new ArgumentNullException(nameof(loader));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        }

        // Use-case: import from file

        /// <summary>
        /// Loads a VOTable or FITS catalogue from <paramref name="filePath"/>,
        /// applies the column mapping, and registers the result as an imported set.
        /// </summary>
        /// <param name="filePath">Absolute path to the source file.</param>
        /// <param name="name">Display name for the new FeatureSet.</param>
        /// <param name="mapping">Column-to-axis mapping (from FeatureMapper).</param>
        /// <param name="columnsMask">Which data columns to include.</param>
        /// <param name="excludeExternal">If true, features outside the volume bounds are dropped.</param>
        /// <param name="volumeMin">Lower-left-back corner of the volume in voxel space. Required when <paramref name="excludeExternal"/> is true.</param>
        /// <param name="volumeMax">Upper-right-front corner of the volume in voxel space. Required when <paramref name="excludeExternal"/> is true.</param>
        /// <returns>The new FeatureSet, or null on load failure.</returns>
        public FeatureSet ImportFromFile(
            string filePath,
            string name,
            Dictionary<SourceMappingOptions, string> mapping,
            bool[] columnsMask,
            bool excludeExternal,
            Vec3 volumeMin = default,
            Vec3 volumeMax = default)
        {
            var table = _loader.Load(filePath, out string error);
            if (table == null)
            {
                // No Unity Debug.Log down here. The caller (FeatureVisualiser
                // or GUI) surfaces the error message to the user.
                LastImportError = error;
                return null;
            }

            var set = _catalog.CreateImportedSet(name);
            set.FileName = filePath;

            PopulateFromTable(set, mapping, table, columnsMask, excludeExternal, volumeMin, volumeMax);

            FeatureSetImported?.Invoke(set);
            return set;
        }

        /// <summary>Last error message from a failed import (for GUI display).</summary>
        public string LastImportError { get; private set; }

        // Use-case: create selection feature

        /// <summary>
        /// Creates (or replaces) the transient selection feature. This feature is
        /// always temporary and disappears on deselect.
        /// </summary>
        /// <param name="boundsMin">Lower-left-back corner in voxel space.</param>
        /// <param name="boundsMax">Upper-right-front corner in voxel space.</param>
        public Feature CreateSelectionFeature(Vec3 boundsMin, Vec3 boundsMax)
        {
            var selectionSet = _catalog.EnsureSelectionSet();
            selectionSet.Clear();

            Deselect();

            var selectionFeature = new Feature(
                cornerMin:  boundsMin,
                cornerMax:  boundsMax,
                color:      FeatureColor.White,
                name:       "selection",
                flag:       string.Empty,
                index:      -1,
                id:         -1,
                rawData:    new[] { string.Empty },
                visible:    true)
            {
                Temporary = true
            };

            selectionSet.Add(selectionFeature);
            Select(selectionFeature);
            return selectionFeature;
        }

        // Use-case: selection

        /// <summary>
        /// Selects the smallest feature whose bounding box contains
        /// <paramref name="cursorPosition"/> (in volume/voxel space).
        /// Returns true if a feature was found.
        /// </summary>
        public bool SelectAtPosition(Vec3 cursorPosition)
        {
            Deselect();

            Feature best = null;
            float   bestVolume = float.MaxValue;

            foreach (var set in _catalog.AllSets())
            {
                foreach (var feature in set.Features)
                {
                    if (!feature.Visible) continue;
                    if (!feature.BoundsContains(cursorPosition)) continue;

                    float vol = feature.BoundsVolume();
                    if (vol < bestVolume ||
                        (Math.Abs(vol - bestVolume) < 1e-6f && set.SetType != FeatureSetType.Selection))
                    {
                        best        = feature;
                        bestVolume  = vol;
                    }
                }
            }

            if (best == null) return false;
            Select(best);
            return true;
        }

        /// <summary>Selects a specific feature directly (e.g. via UI list tap).</summary>
        public void Select(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            Deselect();
            feature.Selected = true;
            SelectedFeature  = feature;

            if (feature.FeatureSetParent?.SetType == FeatureSetType.Mask)
                MaskFeatureSelected?.Invoke();
        }

        /// <summary>Deselects the current feature. If it was temporary, hides it.</summary>
        public void Deselect()
        {
            if (SelectedFeature == null) return;
            SelectedFeature.Selected = false;
            if (SelectedFeature.Temporary)
                SelectedFeature.Visible = false;
            SelectedFeature = null;
        }

        // Use-case: add selected feature to user set

        /// <summary>
        /// Duplicates the currently selected feature into the first user-created set
        /// (creating that set if none exists yet) and persists it to the session file.
        /// Returns false if nothing is selected.
        /// </summary>
        public bool AddSelectedFeatureToUserSet()
        {
            if (SelectedFeature == null) return false;

            var userSets = _catalog.UserSets;
            if (userSets.Count == 0) _catalog.CreateUserSet();

            var targetSet = _catalog.UserSets[0];

            // Copy it across; features behave like values in the domain model.
            var duplicate = new Feature(
                cornerMin: SelectedFeature.CornerMin,
                cornerMax: SelectedFeature.CornerMax,
                color:     targetSet.Color,
                name:      SelectedFeature.Name,
                flag:      SelectedFeature.Flag,
                index:     targetSet.Features.Count,
                id:        SelectedFeature.Id,
                rawData:   new[] { string.Empty },
                visible:   SelectedFeature.Visible)
            {
                Temporary = false,
                Comment   = SelectedFeature.Comment,
                Metric    = SelectedFeature.Metric
            };

            targetSet.Add(duplicate);
            _catalog.AppendFeatureToSessionFile(duplicate);
            return true;
        }

        // Use-case: export

        /// <summary>
        /// Exports a FeatureSet to VOTable XML.
        /// Returns the written file path for display in the GUI.
        /// </summary>
        public string ExportToVoTable(FeatureSet set)
        {
            return _catalog.ExportToVoTable(set);
        }

        // Use-case: real-time statistics

        /// <summary>
        /// Returns up-to-date statistics for the given feature, delegating to
        /// IFeatureStatisticsProvider (which wraps DataAnalysis). It's a pass-through
        /// for now; this is the place to add caching or throttling later if
        /// statistics get expensive.
        /// </summary>
        public FeatureStatistics GetStatistics(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            return _statistics.GetStatistics(feature);
        }

        // Internal helpers

        private static void PopulateFromTable(
            FeatureSet                               set,
            Dictionary<SourceMappingOptions, string> mapping,
            FeatureTable                             table,
            bool[]                                   columnsMask,
            bool                                     excludeExternal,
            Vec3                                     volumeMin = default,
            Vec3                                     volumeMax = default)
        {
            if (table.Rows.Count == 0 || table.Column.Count == 0)
                return;

            // Build column name index and raw-data key list from the mask.
            var colNames         = new string[table.Column.Count];
            var rawDataKeysList  = new List<string>();
            for (int i = 0; i < table.Column.Count; i++)
            {
                colNames[i] = table.Column[i].Name;
                if (columnsMask[i])
                    rawDataKeysList.Add(colNames[i]);
            }
            set.RawDataKeys  = rawDataKeysList.ToArray();
            set.RawDataTypes = new string[rawDataKeysList.Count];
            for (int i = 0; i < set.RawDataTypes.Length; i++)
                set.RawDataTypes[i] = "string";

            var keys           = mapping.Keys;
            bool containsBoxes = keys.Contains(SourceMappingOptions.Xmin);
            bool containsXYZ   = keys.Contains(SourceMappingOptions.X);

            // Sky-coordinate transforms (Ra/Dec + Velo/Freq/Redshift) need AstTool,
            // a native dependency that sits outside the domain layer. WP2's
            // IFeatureTableLoader converts sky coords to pixel coords before handing
            // back the FeatureTable, so by this point the mapping should already
            // contain X/Y/Z or Xmin/Xmax columns.
            if (!containsBoxes && !containsXYZ)
                return;

            int nameIndex = keys.Contains(SourceMappingOptions.ID)
                ? Array.IndexOf(colNames, mapping[SourceMappingOptions.ID])
                : -1;
            int flagIndex = keys.Contains(SourceMappingOptions.Flag)
                ? Array.IndexOf(colNames, mapping[SourceMappingOptions.Flag])
                : -1;

            int[] posIndices = { -1, -1, -1 };
            if (containsXYZ)
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.X]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]);
                posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]);
            }

            int[] boxIndices = { -1, -1, -1, -1, -1, -1 };
            if (containsBoxes)
            {
                boxIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmin]);
                boxIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmax]);
                boxIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymin]);
                boxIndices[3] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymax]);
                boxIndices[4] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmin]);
                boxIndices[5] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmax]);
            }

            for (int row = 0; row < table.Rows.Count; row++)
            {
                var rawData = new List<string>();
                for (int i = 0; i < table.Column.Count; i++)
                {
                    if (columnsMask[i])
                        rawData.Add(table.Rows[row].ColumnData[i]?.ToString() ?? string.Empty);
                }

                string name = nameIndex >= 0
                    ? (string)table.Rows[row].ColumnData[nameIndex]
                    : $"Source #{row + 1}";

                string flag = flagIndex >= 0
                    ? (string)table.Rows[row].ColumnData[flagIndex]
                    : string.Empty;

                Vec3 cornerMin, cornerMax;
                if (containsBoxes)
                {
                    float xMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[0]]);
                    float xMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[1]]);
                    float yMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[2]]);
                    float yMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[3]]);
                    float zMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[4]]);
                    float zMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[5]]);
                    cornerMin = new Vec3(xMin, yMin, zMin);
                    cornerMax = new Vec3(xMax, yMax, zMax);
                }
                else
                {
                    // Point coordinate: expand to a 1-voxel box, matching SpawnFeaturesFromTable.
                    float x = ParseColFloat(table.Rows[row].ColumnData[posIndices[0]]);
                    float y = ParseColFloat(table.Rows[row].ColumnData[posIndices[1]]);
                    float z = ParseColFloat(table.Rows[row].ColumnData[posIndices[2]]);
                    cornerMin = new Vec3(x - 1f, y - 1f, z - 1f);
                    cornerMax = new Vec3(x + 1f, y + 1f, z + 1f);
                }

                var feature = new Feature(
                    cornerMin: cornerMin,
                    cornerMax: cornerMax,
                    color:     set.Color,
                    name:      name,
                    flag:      flag,
                    index:     set.Features.Count,
                    id:        row,
                    rawData:   rawData.ToArray(),
                    visible:   false);

                if (excludeExternal && feature.IsOutsideVolume(volumeMin, volumeMax))
                    continue;

                set.Add(feature);
            }
        }

        private static float ParseColFloat(object value)
        {
            if (value == null) return 0f;
            return float.TryParse(value.ToString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out float result) ? result : 0f;
        }
    }

}