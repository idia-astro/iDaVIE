// SPDX-License-Identifier: LGPL-3.0-or-later
// HistogramMenuController — VR menu shell holding IHistogramService. Replaces
// Assets/Scripts/Menu/HistogramMenuController.cs (222 LOC). The histogram menu
// is a volume-data view (ST3 ownership); this controller renders the bins +
// threshold sliders, the math lives in IHistogramService.

using UnityEngine;
using iDaVIE.Rendering;                  // IHistogramService
using iDaVIE.Rendering.Contracts;        // IRenderSettingsMutator

namespace iDaVIE.UI
{
    internal sealed class HistogramMenuController : MonoBehaviour
    {
        private IHistogramService       _service;
        private IRenderSettingsMutator  _settings;

        public void Inject(IHistogramService service, IRenderSettingsMutator settings)
            => throw new System.NotImplementedException();

        public void OnThresholdRangeChanged(float min, float max) => throw new System.NotImplementedException();
        public void ResetThreshold()                              => throw new System.NotImplementedException();
    }
}
