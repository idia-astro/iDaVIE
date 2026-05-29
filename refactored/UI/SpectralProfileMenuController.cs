// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// SpectralProfileMenuController — ST5-owned Unity ACL MonoBehaviour. The sprite /
// plot rendering half of the spectral-profile menu (brief §6.5).
//
// Replaces:
//   Assets/Scripts/Menu/SpectralProfileMenuController.cs (107 LOC)
//
// Refactor delta:
//   - Previously forward-declared as an inline stub inside UI/SpectralProfileHelper.cs;
//     now split into its own file so the helper orchestrates (data via
//     ISpectralProfileService) and this controller renders (OxyPlot → Sprite → Image).
//   - Render(SpectralProfileResult) absorbs the OxyPlot PlotModel construction that the
//     legacy helper did inline (CreateSpectralProfileImg), driven by the
//     SpectralProfileResult shape rather than an unmanaged spectralProfilePtr.
//   - UpdateUI(Sprite) is retained as the legacy entry point for callers that already
//     hold a rendered sprite.

using System;
using UnityEngine;
using UnityEngine.UI;          // Image (display target)
using iDaVIE.Features;         // SpectralProfileResult

namespace iDaVIE.UI
{
    internal sealed class SpectralProfileMenuController : MonoBehaviour
    {
        [SerializeField] private Image _plotImage;

        /// <summary>Builds the OxyPlot sprite from the profile data and displays it.
        /// Verbatim plot logic from legacy CreateSpectralProfileImg (l. 116-150), but
        /// driven by SpectralProfileResult.Profile / ZStartChannel.</summary>
        public void Render(SpectralProfileResult result) => throw new NotImplementedException();

        /// <summary>Legacy entry point — display an already-rendered sprite.</summary>
        public void UpdateUI(Sprite sprite) => throw new NotImplementedException();
    }
}
