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
using iDaVIE.Kernel.Contracts;           // IVolumeDataSet
using iDaVIE.Kernel.Contracts.Types;     // CartesianCoord, FeatureColour
// ISourceStatsProvider, SourceStats live in iDaVIE.Features (this namespace) per shared_interfaces.md §5.5.

namespace iDaVIE.Features
{
    internal sealed class FeatureSetService : IFeatureSetQuery, IFeatureSetCatalog
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
            => _sets.Values.SelectMany(l => l).Cast<IFeatureSet>().ToList();

        public IReadOnlyList<IFeatureSet> GetFeatureSetsByType(FeatureSetType type)
            => _sets[type].Cast<IFeatureSet>().ToList();

        public void SetVisible(IFeatureSet featureSet, bool visible)
            => ((FeatureSet)featureSet).IsVisible = visible;

        public void SetDisplayColour(IFeatureSet featureSet, FeatureColour colour)
            => ((FeatureSet)featureSet).DisplayColour = colour;

        public void SetFeatureBounds(IFeature feature, CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            var owningSet = FindOwningSet(feature);
            if (owningSet.Type == FeatureSetType.Mask)
                throw new InvalidOperationException("Mask feature bounds are owned by ISourceStatsProvider.");

            // TODO: replace with CartesianCoord helpers Add/Sub/Scale once they land in shared types
            var center = new CartesianCoord((boundsMin.X + boundsMax.X) / 2,
                                            (boundsMin.Y + boundsMax.Y) / 2,
                                            (boundsMin.Z + boundsMax.Z) / 2);
            var size = new CartesianCoord(boundsMax.X - boundsMin.X,
                                          boundsMax.Y - boundsMin.Y,
                                          boundsMax.Z - boundsMin.Z);
            ((Feature)feature).UpdateGeometry(center, size);
            FeatureSetChanged?.Invoke();
        }

        public void SetFeatureFlag(IFeature feature, string flag)
        {
            ((Feature)feature).SetFlag(flag);
            FeatureSetChanged?.Invoke();
        }

        public void CopyFeatureToUserDefined(IFeature source)
        {
            // TODO: locate UserDefined set; create one lazily via CreateSet if none exists.
            // Construct a Feature copy with the source's bounds + RawDataValues, OriginId verbatim
            // (multiple copies share OriginId per IFeature spec). FeatureSetChanged raised by
            // CreateSet (lazy creation) and a second time after the copy is attached.
            throw new NotImplementedException();
        }

        public void SetSelectionBoxBounds(CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            // TODO: lazily create the singleton SelectionBox set on first call; on subsequent
            // calls update the single feature's bounds in-place (never replace the set).
            throw new NotImplementedException();
        }

        public event Action FeatureSetChanged;

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

        // ── Mask creation flow ──────────────────────────────────────────────

        private void OnSourceStatsUpdated(int originId)
        {
            // Four branches per ST5_domain_design.md §6.2:
            //   1. Bootstrap — no Mask set yet → run §6.3 sequence (binder → CreateSet
            //      → PopulateFromSourceStats → FeatureSetChanged raised twice via CreateSet
            //      + the post-populate invoke below).
            //   2. New source — add a single Feature; raise FeatureSetChanged.
            //   3. Removed source — remove the Feature; raise FeatureSetChanged.
            //   4. Updated source — refresh the Feature.Statistics; UpdateStatistics does
            //      NOT call OnFeatureDirty (DD-2). Raise FeatureSetChanged.
            // Branches 2–4 mirror legacy VolumeDataSet.UpdateStats; branch 1 replaces the
            // legacy early-return when _maskFeatureSet was null.
            throw new NotImplementedException();
        }

        private FeatureSet FindOwningSet(IFeature feature)
            => _sets.Values.SelectMany(l => l).First(s => s.Features.Contains(feature));
    }
}
