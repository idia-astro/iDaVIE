// SPDX-License-Identifier: LGPL-3.0-or-later
// StatsTabViewModel — ST6 ViewModel for the Stats tab of CanvassDesktop.
// Replaces the stats panel state in CanvassDesktop.cs + the View half of
// Assets/Scripts/Menu/HistogramHelper.cs.
//
// Same pattern as RenderTabViewModel / SourcesTabViewModel: pure C# state +
// commands + events; the View (MonoBehaviour) subscribes.

using System;
using iDaVIE.Rendering;                  // IHistogramService
using iDaVIE.Rendering.Contracts;        // IRenderSettings, IRenderSettingsMutator
using iDaVIE.Kernel.Contracts.Types;     // HistogramData

namespace iDaVIE.UI
{
    public sealed class StatsTabViewModel : IDisposable
    {
        public StatsTabViewModel(IHistogramService histograms,
                                 IRenderSettings settings,
                                 IRenderSettingsMutator settingsMutator)
            => throw new NotImplementedException();

        public HistogramData? CurrentHistogram { get; private set; }
        public float          ThresholdMin     { get; private set; }
        public float          ThresholdMax     { get; private set; }
        public bool           ZScaleEnabled    { get; private set; }

        public event Action HistogramChanged;
        public event Action ThresholdRangeChanged;

        public void Refresh()                                  => throw new NotImplementedException();
        public void SetThreshold(float min, float max)         => throw new NotImplementedException();
        public void ResetThreshold()                           => throw new NotImplementedException();
        public void ToggleZScale()                             => throw new NotImplementedException();

        public void Dispose() => throw new NotImplementedException();
    }
}
