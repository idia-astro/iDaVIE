// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// FeatureMenuController — ST5-owned VR source-list menu (brief §6.5 "source-list
// statistics" + "VOTable export"). MonoBehaviour, but consumes ST5 services
// only — no GameObject.Find, no static native-DLL reach, no direct
// VolumeDataSetRenderer reference.
//
// Replaces:
//   Assets/Scripts/FeatureData/FeatureMenuController.cs (425 LOC)
//   Assets/Scripts/FeatureData/FeatureMenuController.cs:35-50
//     VolumeDatasetRendererObj / _activeDataSet / _featureSetManager fields
//       → IFeatureSetQuery / IFeatureSelectionService / IActiveFeatureSetTypeProvider
//   Assets/Scripts/FeatureData/FeatureMenuController.cs:176-199
//     DisplayNextSet / DisplayPreviousSet on the controller class
//       → realised on this class via IFeatureListNavigation (M-11)
//   Assets/Scripts/FeatureData/FeatureMenuController.cs:280-289
//     ToggleListVisibility — GameObject.Find("RenderMenu")...transform.Find(...) chain
//       → SetVisible delegated via IFeatureSetQuery; visibility icon state
//         tracked in the View, not via runtime scene traversal
//   Assets/Scripts/FeatureData/FeatureMenuController.cs:310-391
//     UpdateInfo — AstTool.Transform3D / SourceStatsDict.ElementAt(Index) direct calls
//       → reads IFeature.Statistics / IFeature.Center; sky coordinates from
//         ICoordinateTransformer (held by the saver, not the UI). DD-12 keeps WCS
//         work out of the UI layer.
//   Assets/Scripts/FeatureData/FeatureMenuController.cs:399-418
//     SaveListAsVoTable — directly called FeatureSetRenderer.SaveAsVoTable
//       → injected IFeatureCatalogueWriter (DD-4 — the writer interface).
//
// Held interface roles:
//   - Realises IFeatureListNavigation (M-11) consumed by ST4 voice commands
//     "next source list" / "previous source list".
//   - Pushes ActiveType through IActiveFeatureSetTypeProvider — SelectionService
//     reads the same property to prioritise spatial search.

using System;
using System.IO;
using UnityEngine;
using iDaVIE.Features;                // IFeature*, IFeatureSetQuery, etc.

namespace iDaVIE.UI
{
    internal sealed class FeatureMenuController : MonoBehaviour, IFeatureListNavigation
    {
        // ── Inspector wiring (Unity scene serialisation only) ─────────────────
        [SerializeField] private FeatureSetType _menuFeatureSetType; // tab identity (Mask / Imported / UserDefined)

        // ── Injected ST5 services (composition root) ──────────────────────────
        private IFeatureSetQuery              _query;
        private IFeatureSelectionService      _selection;
        private IFeatureCatalogueWriter       _writer;
        private IActiveFeatureSetTypeProvider _activeType;

        // The currently-displayed set within this menu's tab type. Tracks the
        // tab the user has selected — replaces the legacy CurrentFeatureSetIndex
        // field (FeatureMenuController.cs:44). Resolved against the snapshot list
        // from _query.GetFeatureSetsByType(_menuFeatureSetType) on every change.
        private int _currentSetIndex;

        public int CurrentFeatureSetIndex => _currentSetIndex;

        // Composition root calls this; constructor-injection isn't possible on a
        // MonoBehaviour — see ST5_domain_design.md §7 ("MonoBehaviours but they
        // consume ST5 service interfaces directly").
        public void Inject(IFeatureSetQuery query,
                           IFeatureSelectionService selection,
                           IFeatureCatalogueWriter writer,
                           IActiveFeatureSetTypeProvider activeType)
        {
            _query      = query      ?? throw new ArgumentNullException(nameof(query));
            _selection  = selection  ?? throw new ArgumentNullException(nameof(selection));
            _writer     = writer     ?? throw new ArgumentNullException(nameof(writer));
            _activeType = activeType ?? throw new ArgumentNullException(nameof(activeType));

            _query.FeatureSetChanged     += OnFeatureSetChanged;
            _selection.SelectionChanged  += OnSelectionChanged;
        }

        private void OnEnable()
        {
            // The user opened this tab → publish it as the active type so
            // SelectionService prioritises this type during spatial search
            // (ST5_refactoring_proposal.md "IActiveFeatureSetTypeProvider").
            if (_activeType != null) _activeType.ActiveType = _menuFeatureSetType;
        }

        private void OnDisable()
        {
            // Closing the panel means "no source list open" — SelectionService
            // falls back to scanning all sets in FeatureSetType-declaration order.
            if (_activeType != null && _activeType.ActiveType == _menuFeatureSetType)
                _activeType.ActiveType = null;
        }

        private void OnDestroy()
        {
            if (_query     != null) _query.FeatureSetChanged    -= OnFeatureSetChanged;
            if (_selection != null) _selection.SelectionChanged -= OnSelectionChanged;
        }

        // ── IFeatureListNavigation (M-11 — ST4 voice commands) ────────────────
        // Verbatim semantics of the legacy DisplayNext/PreviousSet — wrap-around
        // navigation within the current tab type's set list. The list is the
        // live snapshot from IFeatureSetQuery rather than the cached
        // _featureSetRendererList field the legacy class held.

        public void DisplayNextSet()
        {
            var sets = _query.GetFeatureSetsByType(_menuFeatureSetType);
            if (sets.Count <= 1) return;
            _currentSetIndex = (_currentSetIndex + 1) % sets.Count;
            // View refresh — re-render the cell list. The skeleton intentionally
            // omits the RecyclableScrollRect plumbing (UI-framework detail).
        }

        public void DisplayPreviousSet()
        {
            var sets = _query.GetFeatureSetsByType(_menuFeatureSetType);
            if (sets.Count <= 1) return;
            _currentSetIndex = (_currentSetIndex - 1 + sets.Count) % sets.Count;
        }

        // ── Source-list actions ───────────────────────────────────────────────

        /// <summary>Replaces FeatureMenuController.SaveListAsVoTable (l. 399-418).
        /// The legacy method called FeatureSetRenderer.SaveAsVoTable, which delegated
        /// to the static VoTableSaver; the refactored writer is injected so unit
        /// tests can substitute a fake IFeatureCatalogueWriter.</summary>
        public void SaveListAsVoTable(string outputDirectory)
        {
            var sets = _query.GetFeatureSetsByType(_menuFeatureSetType);
            if (sets.Count == 0) return;
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            var fileName = $"iDaVIE_cat_{DateTime.UtcNow:yyyyMMdd_Hmmss}.xml";
            _writer.Write(sets[_currentSetIndex], Path.Combine(outputDirectory, fileName));
        }

        /// <summary>Replaces FeatureMenuController.AddSelectedFeatureToNewSet (l. 420-423).</summary>
        public void AddSelectedFeatureToNewSet()
        {
            var selected = _selection.SelectedFeature;
            if (selected != null) _query.CopyFeatureToUserDefined(selected);
        }

        /// <summary>Replaces FeatureMenuController.ToggleListVisibility (l. 276-290) —
        /// no GameObject.Find chain; the View observes IFeatureSet.IsVisible directly
        /// and re-renders its icon on FeatureSetChanged.</summary>
        public void ToggleListVisibility()
        {
            var sets = _query.GetFeatureSetsByType(_menuFeatureSetType);
            if (sets.Count == 0) return;
            var set = sets[_currentSetIndex];
            _query.SetVisible(set, !set.IsVisible);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnFeatureSetChanged()
        {
            // Re-resolve the displayed set. The legacy class relied on
            // _featureSetManager.NeedToRespawnMenuList polling flags
            // (FeatureMenuController.cs:130-149); we re-render on the event.
            var sets = _query.GetFeatureSetsByType(_menuFeatureSetType);
            if (_currentSetIndex >= sets.Count) _currentSetIndex = Math.Max(0, sets.Count - 1);
            // View refresh — omitted in skeleton.
        }

        private void OnSelectionChanged(IFeature feature)
        {
            // Replaces FeatureMenuController.UpdateInfo (l. 310-391) — the info
            // panel re-renders using IFeature.Statistics, IFeature.Center, and
            // IFeatureSet.RawDataKeys. WCS sky coordinates come from the same
            // ICoordinateTransformer held by the writer; the controller never
            // calls AstTool directly (DD-12 — no native plug-in reach from UI).
            // Skeleton intentionally omits the TMP_Text formatting plumbing.
        }
    }
}
