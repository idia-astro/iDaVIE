// SPDX-License-Identifier: LGPL-3.0-or-later
// IRestFrequencyCatalogue — replaces direct Config.Instance.restFrequenciesGHz
// reach from VolumeDataSetRenderer (PopulateRestFrequenyList:549,
// RestFrequencyGHzListIndex setter:139-167, ResetRestFrequency:1121).
//
// Refactor delta:
//   - Renderer no longer holds the catalogue. It holds a port; the concrete
//     is realised at the ST3 composition root from Config.restFrequenciesGHz.
//     ST1 cannot realise an iDaVIE.Rendering port — that would back-edge from
//     the kernel to a higher layer (see global_model.md ownership graph).
//   - DIP — depends on an interface, not on the Config singleton.
//
// The catalogue carries three classes of entries per the legacy code:
//   "Default" (entry 0)            → use the FITS rest frequency from the file
//   <named emission lines>         → from Config.restFrequenciesGHz
//   "Custom" (entry n)             → user-typed value in the desktop GUI

using System;
using System.Collections.Generic;

namespace iDaVIE.Rendering
{
    public readonly record struct RestFrequencyEntry(string Label, double FrequencyGHz);

    public interface IRestFrequencyCatalogue
    {
        IReadOnlyList<RestFrequencyEntry> Entries { get; }

        /// <summary>Currently selected entry. Setting it fires SelectionChanged
        /// and (for Default / named entries) updates the active rest frequency
        /// on the loaded dataset; "Custom" defers to the desktop input field.</summary>
        int SelectedIndex { get; set; }

        double CurrentFrequencyGHz { get; }

        event Action SelectionChanged;
    }
}
