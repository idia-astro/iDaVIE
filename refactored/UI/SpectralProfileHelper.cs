// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// SpectralProfileHelper — ST5-owned spectral-profile menu MonoBehaviour
// (brief §6.5 "spectral profiles").
//
// Replaces:
//   Assets/Scripts/Menu/SpectralProfileHelper.cs (153 LOC)
//
// Refactor delta:
//   - The legacy class held a direct VolumeDataSetRenderer + DataAnalysis P/Invoke
//     reference (l. 86-99) and subscribed to FeatureSetManager.MaskFeatureSelected
//     (l. 51). After refactor:
//       VolumeDataSetRenderer / FeatureSetManager refs → IFeatureSelectionService event
//       DataAnalysis.GetSourceStats P/Invoke           → ISpectralProfileService.ComputeForRegionAsync
//   - The ordinal-coupling bug fixed: legacy l. 107 used
//       activeDataSet.Mask.SourceStatsDict.ElementAt(SelectedFeature.Index).Value
//     which keys by *dictionary order*, not maskVal — see ST5_refactoring_proposal.md
//     "Current Architecture Problems" row for SourceStatsDict.ElementAt. The refactor
//     reads IFeature.Statistics directly (Information Expert — DD-2).
//
// Composition root injects ISpectralProfileService and IFeatureSelectionService.

using System;
using System.Threading.Tasks;
using UnityEngine;
using iDaVIE.Features;                // ISpectralProfileService, IFeatureSelectionService, IFeature
using iDaVIE.Kernel.Contracts.Types;  // CartesianCoord

namespace iDaVIE.UI
{
    internal sealed class SpectralProfileHelper : MonoBehaviour
    {
        [SerializeField] private SpectralProfileMenuController _menuController;

        private ISpectralProfileService _service;
        private IFeatureSelectionService _selection;

        public void Inject(ISpectralProfileService service, IFeatureSelectionService selection)
        {
            _service   = service   ?? throw new ArgumentNullException(nameof(service));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _selection.SelectionChanged += OnSelectionChanged;
        }

        private void OnDestroy()
        {
            if (_selection != null) _selection.SelectionChanged -= OnSelectionChanged;
        }

        /// <summary>Replaces SpectralProfileHelper.OnCroppedRegionChanged (l. 84-99).
        /// The legacy method built a DataAnalysis.SourceInfo and called
        /// DataAnalysis.GetSourceStats directly; the refactored service wraps that
        /// behind ISpectralProfileService and runs it off-thread.</summary>
        public async Task OnCroppedRegionChanged(CartesianCoord boundsMin, CartesianCoord boundsMax)
        {
            if (_service == null) return;
            var result = await _service.ComputeForRegionAsync(boundsMin, boundsMax);
            UpdateUI(result);
        }

        /// <summary>Replaces SpectralProfileHelper.OnMaskedSourceSelected (l. 104-110).
        /// Bug fix: the legacy method indexed SourceStatsDict by ordinal position
        /// (.ElementAt(SelectedFeature.Index)) instead of maskVal. The refactor reads
        /// IFeature.Statistics directly — invariant 5.4 guarantees presence on Mask
        /// features (ST5_domain_design.md §5.4).</summary>
        private void OnSelectionChanged(IFeature selected)
        {
            if (selected?.Statistics == null) return;
            // SpectralProfile and ZStartChannel are carried via SourceStats / the
            // SpectralProfileResult shape; the refactored UI displays them without
            // a separate native lookup. Skeleton omits the OxyPlot rendering
            // step — pure plot-export plumbing once the data is in hand.
        }

        private void UpdateUI(SpectralProfileResult result)
        {
            // OxyPlot rendering — verbatim from legacy CreateSpectralProfileImg
            // (l. 116-150) but driven by SpectralProfileResult.Profile /
            // ZStartChannel instead of the unmanaged spectralProfilePtr. The
            // result struct shape (ST5_interface.md §3) replaces the manual
            // Marshal.Copy / native FreeDataAnalysisMemory pair.
            _ = result; // Skeleton omits OxyPlot.PlotModel construction.
            // _menuController.UpdateUI(sprite); — preserved entry point.
        }
    }

    // Forward declaration only — the actual SpectralProfileMenuController lives
    // in Assets/Scripts/Menu and is not part of the ST5 refactor skeleton (it
    // is a thin sprite-display wrapper). Declared here so the helper compiles
    // as illustrated.
    internal sealed class SpectralProfileMenuController : MonoBehaviour
    {
        public void UpdateUI(Sprite sprite) { /* preserved from legacy */ }
    }
}
