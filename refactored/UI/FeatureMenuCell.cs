// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// FeatureMenuCell — ST5-owned source-list row MonoBehaviour. Renders one
// IFeature inside the recyclable scroll view owned by FeatureMenuController.
//
// Replaces:
//   Assets/Scripts/FeatureData/FeatureMenuCell.cs (297 LOC)
//   Assets/Scripts/FeatureData/FeatureMenuCell.cs:62-77
//     Start() — GameObject.Find("VolumeDataSetManager") + GetComponentsInChildren<VolumeDataSetRenderer>
//       → composition-root injection of IFeatureSetQuery + IFeatureSelectionService;
//         no scene traversal at runtime
//   Assets/Scripts/FeatureData/FeatureMenuCell.cs:190-200
//     ToggleFlagIndex — Config.Instance.flags singleton read
//       → IFlagVocabulary injected (small port owned by ST1 / global config;
//         keeps Config singleton out of the UI layer per DD-5)
//   Assets/Scripts/FeatureData/FeatureMenuCell.cs:225-242
//     Select — _featureSetManager.SelectFeature(Feature) + sibling-iteration recolouring
//       → IFeatureSelectionService.SelectFeature + cell highlight from
//         OnSelectionChanged event (no sibling iteration; one event, many cells observe)
//   Assets/Scripts/FeatureData/FeatureMenuCell.cs:269-284
//     AddToNewList / RemoveFromList — direct _featureSetManager.AddFeatureToNewSet
//       → IFeatureSetQuery.CopyFeatureToUserDefined (Add); set-membership
//         mutators on the same surface (Remove is internal to ST5's
//         UserDefined-set flow — not exposed on the cross-team contract).
//
// The cell holds an IFeature reference. Per DD-14 there is no per-feature
// visibility flag on IFeature — visibility is per-set (IFeatureSet.IsVisible).
// The legacy per-feature visibility icon is dropped along with Feature.Visible.

using System;
using UnityEngine;
using iDaVIE.Features;                // IFeature, IFeatureSetQuery, ...

namespace iDaVIE.UI
{
    /// <summary>Small flag-vocabulary port — pulls the closed list of flag
    /// strings out of Config.Instance (DD-5: no singleton reach from UI).
    /// Realised in the ST1 / composition-root layer; not part of the ST5
    /// cross-team contract.</summary>
    internal interface IFlagVocabulary
    {
        System.Collections.Generic.IReadOnlyList<string> Flags { get; }
    }

    internal sealed class FeatureMenuCell : MonoBehaviour
    {
        // ── Injected ST5 services ─────────────────────────────────────────────
        private IFeatureSetQuery         _query;
        private IFeatureSelectionService _selection;
        private IFlagVocabulary          _flags;

        // ── Bound by the scroll-view adapter ──────────────────────────────────
        private IFeature    _feature;
        private IFeatureSet _owningSet;
        private int         _cellIndex;

        public IFeature Feature   => _feature;
        public int      CellIndex => _cellIndex;

        public void Inject(IFeatureSetQuery query,
                           IFeatureSelectionService selection,
                           IFlagVocabulary flags)
        {
            _query     = query     ?? throw new ArgumentNullException(nameof(query));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _flags     = flags     ?? throw new ArgumentNullException(nameof(flags));
        }

        /// <summary>Called by the scroll-view adapter (PolyAndCode RecyclableScrollRect)
        /// when this cell is recycled to display a new feature. Replaces the legacy
        /// ConfigureCell on FeatureMenuCell.cs:89-121.</summary>
        public void Configure(IFeature feature, IFeatureSet owningSet, int cellIndex)
        {
            _feature   = feature   ?? throw new ArgumentNullException(nameof(feature));
            _owningSet = owningSet ?? throw new ArgumentNullException(nameof(owningSet));
            _cellIndex = cellIndex;
            // Skeleton intentionally omits the IDTextField / SourceName / FlagButton
            // assignment — purely TMP_Text plumbing once the IFeature is resolved.
        }

        // ── User actions ──────────────────────────────────────────────────────

        /// <summary>Replaces FeatureMenuCell.ToggleFlagIndex (l. 190-200) — cycles
        /// through the closed flag list. Mutation goes via IFeatureSetQuery so
        /// FeatureSetChanged fires (DD-3).</summary>
        public void ToggleFlagIndex()
        {
            if (_feature == null) return;
            var flags = _flags.Flags;
            if (flags.Count == 0) return;

            var current = _feature.Flag ?? string.Empty;
            var idx = IndexOf(flags, current);
            var next = idx + 1 >= flags.Count ? " " : flags[idx + 1];
            _query.SetFeatureFlag(_feature, next);
        }

        /// <summary>Replaces FeatureMenuCell.Select (l. 225-242). The legacy
        /// method iterated siblings to recolour rows; here cells observe
        /// SelectionChanged on the service and recolour themselves.</summary>
        public void Select()
        {
            if (_feature == null || _owningSet == null) return;
            _selection.SelectFeature(_feature, _owningSet);
        }

        /// <summary>Replaces FeatureMenuCell.AddToNewList (l. 269-272).</summary>
        public void AddToUserDefinedSet()
        {
            if (_feature != null) _query.CopyFeatureToUserDefined(_feature);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int IndexOf(System.Collections.Generic.IReadOnlyList<string> list, string value)
        {
            for (var i = 0; i < list.Count; i++)
                if (string.Equals(list[i], value, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
