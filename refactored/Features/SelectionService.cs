// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// SelectionService — replaces FeatureSetManager.SelectFeature and the
// legacy GameObject.Find("SourcesMenu") inside SelectFeature(Vector3).
//
// Realises IFeatureSelectionService (cross-team, provided to ST4/ST6).
// Internal sealed per DD-5 — cross-team code holds IFeatureSelectionService only.
//
// Design decisions applied:
//   • SelectAtCursor uses linear AABB scan; iDaVIE catalogue sizes (≤ low thousands
//     of features) make a spatial index unnecessary — four comparisons per box runs
//     well under a frame (ST5_refactoring_proposal.md §SelectionService).
//   • Downcast Feature → concrete Feature to set IsSelected is safe: Feature is the
//     sole IFeature implementor in this assembly (DD-5).
//   • IActiveFeatureSetTypeProvider replaces the runtime scene query — the active
//     type is set by FeatureMenuController (ST5-owned) as the user navigates tabs.

using System;
using System.Collections.Generic;
using System.Linq;
using iDaVIE.Kernel.Contracts.Types;   // CartesianCoord

namespace iDaVIE.Features
{
    internal sealed class SelectionService : IFeatureSelectionService
    {
        private readonly ISelectionVisualiser          _anchors;
        private readonly IFeatureSetQuery              _setService;
        private readonly IActiveFeatureSetTypeProvider _activeType;

        public SelectionService(ISelectionVisualiser anchors,
                                IFeatureSetQuery setService,
                                IActiveFeatureSetTypeProvider activeType)
        {
            _anchors     = anchors;
            _setService  = setService;
            _activeType  = activeType;
        }

        public IFeature?    SelectedFeature    { get; private set; }
        public IFeatureSet? SelectedFeatureSet { get; private set; }

        public event Action<IFeature?> SelectionChanged;

        public bool SelectAtCursor(CartesianCoord cursorVoxelSpace)
        {
            var active = _activeType.ActiveType;

            // Prioritise the active type's sets (the tab the user has open in the
            // source-list panel), then scan all remaining sets. This replaces the
            // legacy FeatureSetManager.SelectFeature(Vector3) → GameObject.Find path.
            IEnumerable<IFeatureSet> prioritised = active is { } t
                ? _setService.GetFeatureSetsByType(t)
                    .Concat(_setService.GetAllFeatureSets().Where(s => s.Type != t))
                : _setService.GetAllFeatureSets();

            foreach (var set in prioritised)
            {
                if (!set.IsVisible) continue;
                foreach (var feature in set.Features)
                {
                    // Integer half-size — consistent with the integer CartesianCoord
                    // precision documented in ST5_domain_design.md §4.2.
                    int hx = feature.Size.X / 2;
                    int hy = feature.Size.Y / 2;
                    int hz = feature.Size.Z / 2;

                    if (cursorVoxelSpace.X >= feature.Center.X - hx &&
                        cursorVoxelSpace.X <= feature.Center.X + hx &&
                        cursorVoxelSpace.Y >= feature.Center.Y - hy &&
                        cursorVoxelSpace.Y <= feature.Center.Y + hy &&
                        cursorVoxelSpace.Z >= feature.Center.Z - hz &&
                        cursorVoxelSpace.Z <= feature.Center.Z + hz)
                    {
                        SelectFeature(feature, set);
                        return true;
                    }
                }
            }
            return false;
        }

        public void SelectFeature(IFeature feature, IFeatureSet owningSet)
        {
            if (feature   == null) throw new ArgumentNullException(nameof(feature));
            if (owningSet == null) throw new ArgumentNullException(nameof(owningSet));
            if (!owningSet.Features.Contains(feature))
                throw new ArgumentException(
                    "feature is not a member of owningSet.", nameof(feature));

            DeselectFeature();

            SelectedFeature    = feature;
            SelectedFeatureSet = owningSet;
            // Downcast safe — Feature is the sole IFeature implementor in this assembly (DD-5).
            ((Feature)feature).IsSelected = true;
            _anchors.ShowAt(feature, owningSet);
            SelectionChanged?.Invoke(feature);
        }

        public void DeselectFeature()
        {
            if (SelectedFeature == null) return;
            ((Feature)SelectedFeature).IsSelected = false;
            SelectedFeature    = null;
            SelectedFeatureSet = null;
            _anchors.Hide();
            SelectionChanged?.Invoke(null);
        }
    }
}
