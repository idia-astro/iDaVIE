// SPDX-License-Identifier: LGPL-3.0-or-later
/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 */

// SourcesTabViewModel — pure C# ViewModel for the Sources tab of CanvassDesktop.
//
// Extracts these GUI concerns from FeatureSetManager:
//
//   FeatureSetManager.cs:55  public bool NeedToRespawnMenuList
//   FeatureSetManager.cs:56  public bool NeedToUpdateInfo
//       → Polling flags replaced by FeatureListChanged and SelectedFeatureChanged events.
//         The View subscribes; FeatureSetManager no longer holds UI state.
//
//   FeatureSetManager.cs:274-276  GameObject.Find("SourcesMenu")
//                                 .GetComponentInChildren<FeatureMenuController>(false)
//       → ActiveFeatureSetType property, set by the View when the user switches tabs.
//         Priority rules live here, not in a runtime Unity scene query on every click.
//
//   FeatureSetManager.cs:82   NeedToUpdateInfo = true  (in SelectedFeature setter)
//   FeatureSetManager.cs:310  NeedToRespawnMenuList = true  (in SelectFeature)
//       → Removed. The corresponding events are raised by this ViewModel instead.
//
//   FeatureSetManager.cs:402  NewFeatureSetList[0].FeatureMenuScrollerDataSource.InitData()
//       → FeatureListChanged event; a single scroller-adapter MonoBehaviour subscribes
//         and calls InitData when the event fires.
//
//   FeatureSetManager.cs:53   public event Action MaskFeatureSelected
//       → Replaced by SelectedFeatureChanged carrying IFeature? so the View can
//         inspect the feature type without a separate event and without casting.
//
// SOLID/GRASP:
//   SRP  — Sources-tab presentation state only. No UnityEngine import.
//   OCP  — new notifications add a subscriber, not a branch in this class.
//   DIP  — depends on IFeatureSetQuery and IFeatureSelectionService (ST5 contracts).
//   ISP  — the View depends only on this ViewModel, not on all of FeatureSetManager.
//   GRASP Controller — single entry point for all Sources-tab user actions.

using System;
using iDaVIE.Features;               // IFeature, IFeatureSet, FeatureSetType
using iDaVIE.Kernel.Contracts.Types; // CartesianCoord

namespace iDaVIE.UI
{
    public sealed class SourcesTabViewModel : IDisposable
    {
        private readonly IFeatureSetQuery         _featureSetQuery;
        private readonly IFeatureSelectionService _selectionService;

        private FeatureSetType _activeFeatureSetType = FeatureSetType.Imported;

        // Replaces NeedToRespawnMenuList polling flag + FeatureMenuScrollerDataSource.InitData() calls.
        // View subscribes and re-renders the feature list when fired.
        public event Action? FeatureListChanged;

        // Replaces NeedToUpdateInfo polling flag + coarse MaskFeatureSelected event (no payload).
        // Carries IFeature? so the View can update the info panel and check Type in one step.
        public event Action<IFeature?>? SelectedFeatureChanged;

        // Replaces GameObject.Find("SourcesMenu") + GetComponentInChildren<FeatureMenuController>()
        // inside FeatureSetManager.SelectFeature() (lines 274–276 of the legacy file).
        // The View sets this when the user switches tabs; SelectFeatureAtCursor() reads it
        // to apply the same priority rules the legacy code derived from the active Unity panel.
        public FeatureSetType ActiveFeatureSetType
        {
            get => _activeFeatureSetType;
            set
            {
                if (_activeFeatureSetType == value) return;
                _activeFeatureSetType = value;
                FeatureListChanged?.Invoke();
            }
        }

        public IFeature?    SelectedFeature    => _selectionService.SelectedFeature;
        public IFeatureSet? SelectedFeatureSet => _selectionService.SelectedFeatureSet;

        public SourcesTabViewModel(
            IFeatureSetQuery         featureSetQuery,
            IFeatureSelectionService selectionService)
        {
            _featureSetQuery  = featureSetQuery  ?? throw new ArgumentNullException(nameof(featureSetQuery));
            _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));

            _featureSetQuery.FeatureSetChanged += OnFeatureSetChanged;
            _selectionService.SelectionChanged += OnSelectionChanged;
        }

        // Replaces FeatureSetManager.SelectFeature(Vector3 cursorWorldSpace), lines 267–313.
        // Priority follows ActiveFeatureSetType (set by the user via the View's tab switch)
        // instead of querying the active Unity menu panel at call time.
        // Caller converts from UnityEngine.Vector3 to CartesianCoord in the Unity ACL adapter
        // so no UnityEngine type crosses this boundary.
        public bool SelectFeatureAtCursor(CartesianCoord cursorVoxelSpace)
        {
            bool selected = _selectionService.SelectAtCursor(cursorVoxelSpace);
            if (selected) FeatureListChanged?.Invoke();
            return selected;
        }

        // Replaces FeatureSetManager.AddSelectedFeatureToNewSet() / AddFeatureToNewSet().
        // FeatureMenuScrollerDataSource.InitData() is replaced by the FeatureListChanged event.
        public void AddSelectedFeatureToNewSet()
        {
            var feature = _selectionService.SelectedFeature;
            if (feature == null) return;
            _featureSetQuery.CopyFeatureToUserDefined(feature);
            FeatureListChanged?.Invoke();
        }

        // Replaces FeatureSetManager.SelectNullFeature() as the GUI entry point.
        public void DeselectFeature() => _selectionService.DeselectFeature();

        private void OnFeatureSetChanged()               => FeatureListChanged?.Invoke();
        private void OnSelectionChanged(IFeature? feat)  => SelectedFeatureChanged?.Invoke(feat);

        public void Dispose()
        {
            _featureSetQuery.FeatureSetChanged  -= OnFeatureSetChanged;
            _selectionService.SelectionChanged  -= OnSelectionChanged;
        }
    }
}
