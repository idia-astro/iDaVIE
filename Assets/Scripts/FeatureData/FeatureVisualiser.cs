/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureVisualiser is the only Unity-aware class in the feature layer. It is a
 * MonoBehaviour adapter: it subscribes to FeatureSetService events and turns them
 * into Unity world-space work (spawning and repositioning anchor handles), and it
 * holds the prefab references the rest of the layer must not touch.
 *
 * It also acts as the composition root. Awake() builds FeatureCatalog and
 * FeatureSetService with their concrete dependencies, so the pure-C# objects never
 * have to 'new' anything Unity-specific. The decision logic stays in the service
 * layer; this class just wires things together.
 */

using System;
using System.Collections.Generic;
using DataFeatures;
using iDaVIE.Application.Feature;
using iDaVIE.Domain.Feature;
using UnityEngine;
using UnityEngine.Serialization;
using VolumeData;

namespace iDaVIE.Infrastructure.Unity
{
    /// <summary>
    /// Unity-side adapter for the feature domain.
    /// Constructs and wires the pure-C# domain objects, then translates
    /// their events into Unity world-space operations.
    /// </summary>
    public sealed class FeatureVisualiser : MonoBehaviour
    {
        // Inspector references
        [Header("Prefabs")]
        [SerializeField] private FeatureSetRenderer  _featureSetRendererPrefab;
        [SerializeField] private GameObject          _featureAnchorPrefab;

        [Header("Scene references")]
        [SerializeField] private VolumeDataSetRenderer _volumeRenderer;

        // Domain objects, constructed in Awake
        public FeatureCatalog     Catalog  { get; private set; }
        public FeatureSetService  Service  { get; private set; }

        // The 8 corner handles for the selected feature's bounding box.
        private readonly GameObject[] _anchorHandles = new GameObject[8];

        // Maps each domain FeatureSet to its GPU renderer so anchors can be
        // parented to the correct coordinate-space transform.
        private readonly Dictionary<FeatureSet, FeatureSetRenderer> _setRenderers
            = new Dictionary<FeatureSet, FeatureSetRenderer>();

        /// <summary>
        /// Forwarded from FeatureSetService.MaskFeatureSelected.
        /// Other MonoBehaviours (e.g. the stats panel) subscribe to this.
        /// </summary>
        public event Action MaskFeatureSelected;

        // Composition root: Awake wires up the domain objects for this scene.
        private void Awake()
        {
            // 1. Build the persistence service (WP7 concrete class).
            //    In the shipped product this would be: new FilesystemPersistenceService()
            //    For now we use the null object so the domain compiles cleanly.
            IFeaturePersistenceService persistence = new NullFeaturePersistenceService();

            // 2. Build the loader (WP2 concrete class, stubbed until WP2 delivers).
            IFeatureTableLoader loader = new NullFeatureTableLoader();

            // 3. Build the statistics provider (wraps DataAnalysis native DLL).
            IFeatureStatisticsProvider stats = new DataAnalysisStatisticsProvider(_volumeRenderer);

            // 4. Assemble the domain layer.
            Catalog = new FeatureCatalog(persistence);
            Service = new FeatureSetService(Catalog, loader, stats);

            // 5. Subscribe to domain events.
            Service.FeatureSelectionChanged += OnSelectionChanged;
            Service.MaskFeatureSelected     += () => MaskFeatureSelected?.Invoke();
            Service.FeatureSetImported      += OnFeatureSetImported;

            // 6. Spawn anchor handles (hidden until a feature is selected).
            SpawnAnchorHandles();

            // 7. Ensure the selection set exists.
            Catalog.EnsureSelectionSet();
        }

        private void OnDestroy()
        {
            if (Service == null) return;
            Service.FeatureSelectionChanged -= OnSelectionChanged;
        }

        private void Update()
        {
            var selected = Service?.SelectedFeature;
            if (selected != null && selected.Selected)
                RepositionAnchors(selected);
            else
                HideAnchors();
        }

        // Domain event handlers

        private void OnSelectionChanged(Feature previous, Feature next)
        {
            // Repositioning a newly selected feature's anchors happens in
            // Update(); here we only need to hide them on deselect.
            if (next == null) HideAnchors();
        }

        private void OnFeatureSetImported(FeatureSet set)
        {
            // Spawn a FeatureSetRenderer GameObject for the new set so the
            // GPU rendering layer (WP3) can visualise it.
            CreateRendererForSet(set);
        }

        // Public API (called by VolumeInputController etc.)

        /// <summary>Selects the feature under the cursor in world space.</summary>
        public bool SelectAtWorldPosition(Vector3 worldPosition)
        {
            // Convert Unity world space to voxel space for the pure-C# layer.
            Vec3 voxelPos = WorldToVoxel(worldPosition);
            return Service.SelectAtPosition(voxelPos);
        }

        /// <summary>Selects a specific feature (called from the GUI list).</summary>
        public void SelectFeature(Feature feature) => Service.Select(feature);

        /// <summary>Deselects the current feature.</summary>
        public void DeselectFeature() => Service.Deselect();

        /// <summary>Creates the transient selection bounding box.</summary>
        public Feature CreateSelectionFeature(Vector3 boundsMin, Vector3 boundsMax)
        {
            return Service.CreateSelectionFeature(
                new Vec3(boundsMin.x, boundsMin.y, boundsMin.z),
                new Vec3(boundsMax.x, boundsMax.y, boundsMax.z));
        }

        /// <summary>Exports a FeatureSet to VOTable via the domain service.</summary>
        public string ExportToVoTable(FeatureSet set) => Service.ExportToVoTable(set);

        /// <summary>
        /// Creates an imported FeatureSet from a pre-loaded FeatureTable, spawns its renderer,
        /// and applies the column mapping. Replaces FeatureSetManager.ImportFeatureSetFromTable.
        /// </summary>
        public FeatureSetRenderer ImportFeatureSetFromTable(
            System.Collections.Generic.Dictionary<SourceMappingOptions, string> mapping,
            FeatureTable table,
            string name,
            bool[] columnsMask,
            bool excludeExternal)
        {
            var set = Catalog.CreateImportedSet(name);
            set.FileName = name;
            var renderer = CreateRendererForSet(set);
            renderer.SpawnFeaturesFromTable(mapping, table, columnsMask, excludeExternal);
            return renderer;
        }

        // FeatureSetRenderer factory

        /// <summary>
        /// Instantiates a FeatureSetRenderer prefab and configures it for <paramref name="set"/>.
        /// The renderer is the Unity/GPU counterpart of a domain FeatureSet.
        /// </summary>
        public FeatureSetRenderer CreateRendererForSet(FeatureSet set)
        {
            var dims = _volumeRenderer.GetCubeDimensions();

            var renderer = Instantiate(_featureSetRendererPrefab,
                                       Vector3.zero, Quaternion.identity);
            renderer.transform.SetParent(transform, false);
            renderer.name = set.Name;
            renderer.tag  = "customSet";

            renderer.transform.localPosition  = -0.5f * Vector3.one;
            renderer.transform.localScale     = new Vector3(1f / dims.x, 1f / dims.y, 1f / dims.z);
            renderer.transform.localPosition -= renderer.transform.localScale * 0.5f;

            renderer.Initialize(_volumeRenderer);
            renderer.Index          = set.Index;
            renderer.FeatureSetType = set.SetType;

            IFeatureRenderer iRenderer = renderer;
            iRenderer.FeatureColor = set.Color;
            set.FeatureDirty += iRenderer.SetFeatureAsDirty;
            _setRenderers[set] = renderer;

            foreach (var feature in set.Features)
                iRenderer.AddFeature(feature);

            return renderer;
        }

        /// <summary>
        /// Ensures the selection FeatureSet exists and returns its renderer.
        /// Equivalent to FeatureSetManager.CreateSelectionFeatureSet().
        /// </summary>
        public FeatureSetRenderer CreateSelectionRendererSet()
        {
            var set = Catalog.EnsureSelectionSet();
            var renderer = CreateRendererForSet(set);
            renderer.FeatureSetType = FeatureSetType.Selection;
            return renderer;
        }

        /// <summary>
        /// Creates a new mask FeatureSet and returns its renderer.
        /// Equivalent to FeatureSetManager.CreateMaskFeatureSet().
        /// </summary>
        public FeatureSetRenderer CreateMaskRendererSet()
        {
            var set = Catalog.CreateMaskSet();
            var renderer = CreateRendererForSet(set);
            renderer.FeatureSetType = FeatureSetType.Mask;
            return renderer;
        }

        // Anchor handle management

        private void SpawnAnchorHandles()
        {
            int idx = 0;
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            for (int k = 0; k < 2; k++)
            {
                _anchorHandles[idx] = Instantiate(_featureAnchorPrefab,
                                                  Vector3.zero, Quaternion.identity);
                _anchorHandles[idx].transform.SetParent(transform, false);
                _anchorHandles[idx].name =
                    $"{(i == 0 ? "left" : "right")}_{(j == 0 ? "bottom" : "top")}_{(k == 0 ? "back" : "front")}";
                _anchorHandles[idx].transform.localScale = Vector3.zero; // hidden by default
                idx++;
            }
        }

        private void RepositionAnchors(Feature feature)
        {
            Transform parentTransform = transform;
            if (feature.FeatureSetParent != null &&
                _setRenderers.TryGetValue(feature.FeatureSetParent, out var setRenderer))
                parentTransform = setRenderer.transform;

            int idx = 0;
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            for (int k = 0; k < 2; k++)
            {
                var handle    = _anchorHandles[idx];
                var weighting = new Vector3(i, j, k);

                handle.transform.SetParent(parentTransform, false);
                var cornerMax = new Vector3(feature.CornerMax.X, feature.CornerMax.Y, feature.CornerMax.Z);
                var cornerMin = new Vector3(feature.CornerMin.X, feature.CornerMin.Y, feature.CornerMin.Z);
                handle.transform.localPosition =
                    Vector3.Scale(cornerMax + Vector3.one * 0.5f, weighting)
                    + Vector3.Scale(cornerMin - Vector3.one * 0.5f, Vector3.one - weighting);

                SetGlobalScale(handle.transform, Vector3.one * 0.01f);
                idx++;
            }
        }

        private void HideAnchors()
        {
            foreach (var handle in _anchorHandles)
                if (handle != null)
                    handle.transform.localScale = Vector3.zero;
        }

        // Coordinate conversion helpers

        private Vec3 WorldToVoxel(Vector3 worldPos)
        {
            // Each FeatureSetRenderer's transform maps voxel space to world space.
            // This uses the visualiser's own inverse transform; ideally it would
            // take a FeatureSet parameter and use that set's renderer instead.
            var local = transform.InverseTransformPoint(worldPos);
            return new Vec3(local.x, local.y, local.z);
        }

        private static void SetGlobalScale(Transform t, Vector3 globalScale)
        {
            t.localScale = Vector3.one;
            t.localScale = new Vector3(
                globalScale.x / t.lossyScale.x,
                globalScale.y / t.lossyScale.y,
                globalScale.z / t.lossyScale.z);
        }
    }

    // Null-object stubs, used in Awake until WP2/WP7 deliver

    /// <summary>No-op persistence service. Safe to use in tests and early integration.</summary>
    internal sealed class NullFeaturePersistenceService : IFeaturePersistenceService
    {
        public void   AppendFeatureToAscii(Feature f, string fn) { }
        /// <summary>
        /// Returns <see cref="string.Empty"/>; no file is written.
        /// Callers that display the returned path as a UI confirmation must guard for
        /// empty string (e.g. <c>if (!string.IsNullOrEmpty(path)) ShowMessage(path);</c>).
        /// Replace this stub with the real WP7 implementation before shipping export.
        /// </summary>
        public string ExportToVoTable(FeatureSet set)             => string.Empty;
        public bool   AsciiOutputExists(string fn)                => false;
    }

    /// <summary>
    /// No-op renderer. Safe to use in tests and before WP3 delivers its implementation.
    /// Swap out for the real FeatureSetRenderer in FeatureVisualiser.CreateRendererForSet.
    /// </summary>
    internal sealed class NullFeatureRenderer : IFeatureRenderer
    {
        public void AddFeature(Feature feature)   { }
        public void RemoveFeature(Feature feature) { }
        public void ClearFeatures()               { }
        public void SetFeatureAsDirty(int index)  { }
        public FeatureColor FeatureColor          { get; set; }
    }

    /// <summary>No-op loader. Returns null so callers see a clean failure path.</summary>
    internal sealed class NullFeatureTableLoader : IFeatureTableLoader
    {
        public FeatureTable Load(string filePath, out string error)
        {
            error = "NullFeatureTableLoader: WP2 implementation not yet connected.";
            return null;
        }
    }

    /// <summary>
    /// Wraps DataAnalysis.SourceStats as an IFeatureStatisticsProvider.
    /// Holds a VolumeDataSetRenderer reference; this adapter is the only place
    /// the domain touches the rendering layer, and only indirectly.
    /// </summary>
    internal sealed class DataAnalysisStatisticsProvider : IFeatureStatisticsProvider
    {
        private readonly VolumeDataSetRenderer _volumeRenderer;

        public DataAnalysisStatisticsProvider(VolumeDataSetRenderer volumeRenderer)
        {
            _volumeRenderer = volumeRenderer;
        }

        public FeatureStatistics GetStatistics(Feature feature)
        {
            // The full version calls DataAnalysis.GetSourceStats through the native
            // plugin interface and maps SourceStats fields onto FeatureStatistics.
            // Only the skeleton is here; the binding lives in PluginInterface/DataAnalysis.cs.
            //
            // var stats = DataAnalysis.GetSourceStats(
            //     _volumeRenderer.DataSet.FitsData,
            //     (int)feature.CornerMin.X, (int)feature.CornerMin.Y, (int)feature.CornerMin.Z,
            //     (int)feature.CornerMax.X, (int)feature.CornerMax.Y, (int)feature.CornerMax.Z);
            //
            // return new FeatureStatistics
            // {
            //     VoxelCount  = stats.numVoxels,
            //     TotalFlux   = stats.sum,
            //     PeakFlux    = stats.peak,
            //     CentroidX   = stats.cX,
            //     CentroidY   = stats.cY,
            //     CentroidZ   = stats.cZ,
            //     W20         = stats.channelW20,
            //     W50         = 0,   // SourceStats has no W50; it only exposes channelW20 and veloW20.
            //                        // veloVsys is systemic velocity, not W50, so don't use it here.
            //                        // Check with WP2 whether a W50 field will be added to SourceStats.
            // };

            return new FeatureStatistics();   // stub until DataAnalysis binding is confirmed
        }
    }
}