// SPDX-License-Identifier: LGPL-3.0-or-later
// FeatureSetService — application-layer orchestrator. Replaces the catalog-
// management role of the legacy FeatureSetManager and absorbs the stats-refresh
// logic that previously sat in VolumeDataSet.UpdateStats (lines 528–587).
//
// Realises IFeatureSetQuery (cross-team) and IFeatureSetCatalog (ST5-internal).
// Holds: IVisualiserBinder, IFeatureFactory, ISourceStatsProvider, IVolumeDataSet.

using System;
using System.Collections.Generic;
using System.Linq;
using iDaVIE.Kernel.Contracts;             // IVolumeDataSet, LoadStatus, EnumString
using iDaVIE.Kernel.Contracts.Persistence; // SubcubeBoundsDto
using iDaVIE.Kernel.Contracts.Types;       // CartesianCoord, FeatureColour
// ISourceStatsProvider, SourceStats live in iDaVIE.Features (this namespace) per shared_interfaces.md §5.5.

namespace iDaVIE.Features
{
    internal sealed class FeatureSetService : IFeatureSetQuery, IFeatureSetCatalog, IFeatureStateCapture
    {
        private readonly IVisualiserBinder    _binder;
        private readonly IFeatureFactory      _factory;
        private readonly ISourceStatsProvider _stats;
        private readonly IVolumeDataSet       _volume;
        private int _nextIndex;

        // Mask / Imported / UserDefined hold 0..* sets each; SelectionBox is 0..1.
        private readonly Dictionary<FeatureSetType, List<FeatureSet>> _sets = new()
        {
            [FeatureSetType.Mask]         = new(),
            [FeatureSetType.Imported]     = new(),
            [FeatureSetType.UserDefined]  = new(),
            [FeatureSetType.SelectionBox] = new(),
        };

        // Explicit FeatureSetType-declaration order for GetAllFeatureSets, so the
        // SelectionService spatial-search fallback order (ST5_domain_design.md §8.3)
        // is guaranteed rather than relying on Dictionary enumeration order.
        private static readonly FeatureSetType[] TypeOrder =
        {
            FeatureSetType.Mask, FeatureSetType.Imported,
            FeatureSetType.UserDefined, FeatureSetType.SelectionBox,
        };

        // Provenance of Imported sets so Capture() can write file-path + mapping
        // instead of inlining the (re-derivable) features. Populated by
        // FeatureImportService via RecordImportProvenance after CreateSet.
        private readonly Dictionary<int, ImportProvenance> _importedMetadata = new();

        // Setter-injected by the composition root so Restore() can hand Imported
        // entries back to FeatureImportService.ImportFromFile. Avoids the
        // FeatureSetService ↔ FeatureImportService construction cycle.
        private IImportRestorer _importRestorer;
        internal void SetImportRestorer(IImportRestorer restorer) => _importRestorer = restorer;

        public FeatureSetService(IVisualiserBinder binder, IFeatureFactory factory,
                                 ISourceStatsProvider stats, IVolumeDataSet volume)
        {
            _binder  = binder;
            _factory = factory;
            _stats   = stats;
            _volume  = volume;

            _stats.SourceStatsUpdated += OnSourceStatsUpdated;
        }

        // ── IFeatureSetQuery ────────────────────────────────────────────────

        public IReadOnlyList<IFeatureSet> GetAllFeatureSets()
            => TypeOrder.SelectMany(t => _sets[t]).Cast<IFeatureSet>().ToList();

        public IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type)
            => _sets[type].Cast<IFeatureSet>().ToList();

        public void SetVisible(IFeatureSet featureSet, bool visible)
            => ((FeatureSet)featureSet).IsVisible = visible;

        public void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour)
            => ((FeatureSet)featureSet).DisplayColour = colour;

        public void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            if (boundsMin.X > boundsMax.X || boundsMin.Y > boundsMax.Y || boundsMin.Z > boundsMax.Z)
                throw new ArgumentException("boundsMin must be ≤ boundsMax on each axis.", nameof(boundsMin));

            var owningSet = FindOwningSet(feature);
            if (owningSet.Type == FeatureSetType.Mask)
                throw new InvalidOperationException("Mask feature bounds are owned by ISourceStatsProvider.");

            ((Feature)feature).UpdateGeometry(boundsMin.Midpoint(boundsMax),
                                              boundsMin.Extent(boundsMax));
            FeatureSetChanged?.Invoke();
        }

        public void SetFeatureFlag(IFeature feature, string flag)
        {
            ((Feature)feature).SetFlag(flag);
            FeatureSetChanged?.Invoke();
        }

        public void CopyFeatureToUserDefined(IFeature source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Multiple UserDefined copies of the same source share OriginId per IFeature spec.
            var copy = new Feature(
                originId: source.OriginId,
                name: source.Name,
                flag: source.Flag,
                center: source.Center,
                size: source.Size,
                rawDataValues: source.RawDataValues);

            var userDefined = _sets[FeatureSetType.UserDefined].FirstOrDefault();
            if (userDefined == null)
            {
                // §6.3 lazy bootstrap: listener → set → attach → feature → second event.
                var listener = _binder.CreateListener();
                userDefined = CreateSet(
                    fileName: "User Defined",
                    type: FeatureSetType.UserDefined,
                    rawDataKeys: Array.Empty<string>(),
                    colour: default,
                    listener: listener);
                _binder.AttachToSet(listener, userDefined);
                userDefined.AddFeature(copy);
                NotifySetPopulated();
            }
            else
            {
                userDefined.AddFeature(copy);
                FeatureSetChanged?.Invoke();
            }
        }

        public void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            if (boundsMin.X > boundsMax.X || boundsMin.Y > boundsMax.Y || boundsMin.Z > boundsMax.Z)
                throw new ArgumentException("boundsMin must be ≤ boundsMax on each axis.", nameof(boundsMin));

            var center = boundsMin.Midpoint(boundsMax);
            var size   = boundsMin.Extent(boundsMax);

            var selectionBox = _sets[FeatureSetType.SelectionBox].FirstOrDefault();
            if (selectionBox == null)
            {
                // §6.3 lazy bootstrap — first call only.
                var listener = _binder.CreateListener();
                selectionBox = CreateSet(
                    fileName: "Selection Box",
                    type: FeatureSetType.SelectionBox,
                    rawDataKeys: Array.Empty<string>(),
                    colour: default,
                    listener: listener);
                _binder.AttachToSet(listener, selectionBox);

                var feature = new Feature(
                    originId: -1, name: "Selection Box", flag: string.Empty,
                    center: center, size: size,
                    rawDataValues: Array.Empty<string>());
                selectionBox.AddFeature(feature);
                NotifySetPopulated();
            }
            else
            {
                // Singleton cardinality — never replace the set, update the single feature in-place.
                var existing = (Feature)selectionBox.Features[0];
                existing.UpdateGeometry(center, size);
                FeatureSetChanged?.Invoke();
            }
        }

        public event Action FeatureSetChanged;

        // ── IFeatureStateCapture (ST7 persistence — M-16) ───────────────────

        public FeatureStateDto Capture()
        {
            var dto = new FeatureStateDto { SchemaVersion = 1 };

            foreach (var set in _sets[FeatureSetType.UserDefined])
            {
                dto.FeatureSets.Add(new FeatureSetEntryDto
                {
                    SetName       = set.FileName,
                    Type          = nameof(FeatureSetType.UserDefined),
                    DisplayColour = set.DisplayColour,
                    IsVisible     = set.IsVisible,
                    Features      = set.Features.Select(SnapshotFeature).ToList(),
                });
            }

            // Imported sets re-derive features from the source file + mapping on Restore.
            // ImportFilePath and ImportMapping are populated by FeatureImportService at
            // creation time via RecordImportProvenance (called alongside CreateSet).
            foreach (var set in _sets[FeatureSetType.Imported])
            {
                _importedMetadata.TryGetValue(set.Index, out var meta);
                dto.FeatureSets.Add(new FeatureSetEntryDto
                {
                    SetName        = set.FileName,
                    Type           = nameof(FeatureSetType.Imported),
                    DisplayColour  = set.DisplayColour,
                    IsVisible      = set.IsVisible,
                    ImportFilePath = meta?.FilePath,
                    ImportMapping  = meta?.Mapping,
                });
            }

            // Mask sets re-derive from ST2 via IMaskStateCapture + SourceStatsUpdated bootstrap.
            // Not snapshotted here per shared_interfaces.md §5.6.

            var selectionBox = _sets[FeatureSetType.SelectionBox].FirstOrDefault();
            if (selectionBox != null && selectionBox.Features.Count > 0)
            {
                var only = selectionBox.Features[0];
                var min  = only.BoundsMin();
                var max  = only.BoundsMax();
                dto.SelectionBoxBounds = new SubcubeBoundsDto
                {
                    XMin = min.X, XMax = max.X,
                    YMin = min.Y, YMax = max.Y,
                    ZMin = min.Z, ZMax = max.Z,
                };
            }

            return dto;
        }

        public void Restore(FeatureStateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // Drop existing UserDefined / Imported / SelectionBox state — Mask sets are
            // owned by the SourceStatsUpdated bootstrap path and are not touched here.
            foreach (var type in new[] { FeatureSetType.UserDefined,
                                          FeatureSetType.Imported,
                                          FeatureSetType.SelectionBox })
            {
                foreach (var set in _sets[type]) set.ClearFeatures();
                _sets[type].Clear();
            }
            _importedMetadata.Clear();

            foreach (var entry in dto.FeatureSets)
            {
                // Unknown future enum values fall back to UserDefined via the shared ST1
                // Kernel helper (EnumString.TryParseOrDefault; ST5_interface.md §3 /
                // shared_interfaces.md §1.9) — one parse policy across all capture ports.
                var type = EnumString.TryParseOrDefault(entry.Type, FeatureSetType.UserDefined);

                if (type == FeatureSetType.Mask) continue;   // not snapshotted by ST5

                if (type == FeatureSetType.Imported)
                {
                    // Import path: file + mapping → features re-derived. Delegated to
                    // FeatureImportService via the IImportRestorer seam to avoid a
                    // circular FeatureSetService ↔ FeatureImportService construction.
                    if (entry.ImportFilePath != null && entry.ImportMapping != null)
                        _importRestorer?.RestoreImported(entry.ImportFilePath, entry.ImportMapping,
                                                         entry.DisplayColour, entry.IsVisible);
                    continue;
                }

                // UserDefined or SelectionBox: inline features.
                var listener = _binder.CreateListener();
                var set = CreateSet(entry.SetName, type, Array.Empty<string>(),
                                    entry.DisplayColour, listener);
                _binder.AttachToSet(listener, set);
                foreach (var f in entry.Features)
                {
                    var center = new CartesianCoord(f.CenterX, f.CenterY, f.CenterZ);
                    var size   = new CartesianCoord(f.BoundsMaxX - f.BoundsMinX,
                                                    f.BoundsMaxY - f.BoundsMinY,
                                                    f.BoundsMaxZ - f.BoundsMinZ);
                    set.AddFeature(new Feature(f.OriginId, f.Name, f.Flag, center, size,
                                               Array.Empty<string>()));
                }
                ((FeatureSet)set).IsVisible = entry.IsVisible;
                NotifySetPopulated();
            }

            if (dto.SelectionBoxBounds is { } bounds)
                SetSelectionBoxBounds(
                    new CartesianCoord(bounds.XMin, bounds.YMin, bounds.ZMin),
                    new CartesianCoord(bounds.XMax, bounds.YMax, bounds.ZMax));
        }

        private static FeatureEntryDto SnapshotFeature(IFeature f)
        {
            var min = f.BoundsMin();
            var max = f.BoundsMax();
            return new FeatureEntryDto
            {
                OriginId   = f.OriginId,
                Name       = f.Name,
                Flag       = f.Flag,
                CenterX    = f.Center.X, CenterY = f.Center.Y, CenterZ = f.Center.Z,
                BoundsMinX = min.X, BoundsMinY = min.Y, BoundsMinZ = min.Z,
                BoundsMaxX = max.X, BoundsMaxY = max.Y, BoundsMaxZ = max.Z,
            };
        }

        // ── IFeatureSetCatalog (ST5-internal) ───────────────────────────────

        public FeatureSet CreateSet(string fileName, FeatureSetType type,
                                    IReadOnlyList<string> rawDataKeys,
                                    FeatureColour colour,
                                    IFeatureDirtyListener listener)
        {
            var set = new FeatureSet(_nextIndex++, fileName, type, rawDataKeys, colour,
                                     listener, onChanged: () => FeatureSetChanged?.Invoke());
            _sets[type].Add(set);
            FeatureSetChanged?.Invoke();
            return set;
        }

        public void NotifySetPopulated() => FeatureSetChanged?.Invoke();

        public void RecordImportProvenance(IFeatureSet set, string filePath, FeatureImportMapping mapping)
        {
            if (set      == null) throw new ArgumentNullException(nameof(set));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (mapping  == null) throw new ArgumentNullException(nameof(mapping));
            _importedMetadata[set.Index] = new ImportProvenance(filePath, mapping);
        }

        private readonly record struct ImportProvenance(string FilePath, FeatureImportMapping Mapping);

        // ── Dataset lifecycle ───────────────────────────────────────────────

        /// <summary>Clears every feature set when the active volume is unloaded.
        /// Wired by the composition root to ST1's <see cref="DatasetUnloadedHandler"/>
        /// (the service holds IVolumeDataSet but cannot subscribe to a lifecycle event
        /// through the read-only aggregate). Mask sets re-derive on the next load via
        /// the SourceStatsUpdated bootstrap below.</summary>
        internal void ResetCatalogue()
        {
            foreach (var list in _sets.Values)
            {
                foreach (var set in list) set.ClearFeatures();
                list.Clear();
            }
            _importedMetadata.Clear();
            FeatureSetChanged?.Invoke();
        }

        // ── Mask creation flow ──────────────────────────────────────────────

        private void OnSourceStatsUpdated(int originId)
        {
            // Validate cube state: ignore stats churn while no volume is loaded
            // (e.g. during teardown) so the Mask set is not bootstrapped against a
            // stale snapshot — the catalogue is cleared via ResetCatalogue on unload.
            if (_volume.Status != LoadStatus.Loaded) return;

            var maskSet = _sets[FeatureSetType.Mask].FirstOrDefault();

            // Branch 1 — Bootstrap: no Mask set yet. Either first-stats-load (originId
            // could be anything ≥ 0, or -1 on bulk reload) triggers a full populate.
            if (maskSet == null)
            {
                var listener = _binder.CreateListener();
                maskSet = CreateSet(
                    fileName: "Mask",
                    type: FeatureSetType.Mask,
                    rawDataKeys: Array.Empty<string>(),
                    colour: default,
                    listener: listener);
                _binder.AttachToSet(listener, maskSet);
                _factory.PopulateFromSourceStats(maskSet);
                NotifySetPopulated();
                return;
            }

            // Bulk reload sentinel (originId = -1): re-derive every feature.
            if (originId < 0)
            {
                maskSet.ClearFeatures();
                _factory.PopulateFromSourceStats(maskSet);
                FeatureSetChanged?.Invoke();
                return;
            }

            var existing = maskSet.Features.FirstOrDefault(f => f.OriginId == originId);
            var stats    = _stats.GetStatsForSource(originId);

            if (existing == null && stats != null)
            {
                // Branch 2 — New source.
                var feature = BuildMaskFeature(originId, stats);
                maskSet.AddFeature(feature);
                FeatureSetChanged?.Invoke();
            }
            else if (existing != null && stats == null)
            {
                // Branch 3 — Removed source (voxel count fell to zero).
                maskSet.RemoveFeature(existing);
                FeatureSetChanged?.Invoke();
            }
            else if (existing != null && stats != null)
            {
                // Branch 4 — Updated source: refresh geometry + statistics.
                var center = stats.BoundsMin.Midpoint(stats.BoundsMax);
                var size   = stats.BoundsMin.Extent(stats.BoundsMax);
                ((Feature)existing).UpdateGeometry(center, size);
                ((Feature)existing).UpdateStatistics(new FeatureStatistics(
                    stats.VoxelCount, stats.TotalFlux, stats.PeakFlux, stats.FluxWeightedCentroid,
                    stats.ChannelW20, stats.VeloW20, stats.ChannelVsys, stats.VeloVsys));
                FeatureSetChanged?.Invoke();
            }
            // (existing == null && stats == null) — race; nothing to do.
        }

        private static Feature BuildMaskFeature(int originId, SourceStats stats)
        {
            var center = stats.BoundsMin.Midpoint(stats.BoundsMax);
            var size   = stats.BoundsMin.Extent(stats.BoundsMax);
            var feature = new Feature(originId, $"Masked Source #{originId}", flag: string.Empty,
                                      center, size, rawDataValues: Array.Empty<string>());
            feature.UpdateStatistics(new FeatureStatistics(
                stats.VoxelCount, stats.TotalFlux, stats.PeakFlux, stats.FluxWeightedCentroid,
                stats.ChannelW20, stats.VeloW20, stats.ChannelVsys, stats.VeloVsys));
            return feature;
        }

        private FeatureSet FindOwningSet(IFeature feature)
            => _sets.Values.SelectMany(l => l).First(s => s.Features.Contains(feature));
    }
}
