/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// =============================================================================
// AFTER — Service: RestFrequencyService (design-level worked example)
//
// Single responsibility: manages the rest-frequency catalogue and the active
// selection, then fires an event so downstream UI (spectra panel, WCS display)
// can react without coupling to the MonoBehaviour.
//
// CK AFTER (estimated):
//   WMC  ≈ 9   (4 public + 2 private, avg complexity 1.8)
//   CBO  ≈ 3   (VolumeDataSet, Config, System.Linq)
//   RFC  ≈ 11
//   LCOM ≈ 0.08
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace VolumeData.Refactored
{
    /// <summary>
    /// Builds and manages the spectral rest-frequency catalogue for a loaded dataset.
    /// Raises <see cref="FrequencyChanged"/> whenever the active frequency is updated
    /// so that WCS-dependent UI panels can refresh without direct renderer coupling.
    /// </summary>
    public sealed class RestFrequencyService : IRestFrequencyController
    {
        private readonly VolumeDataSet _dataSet;
        private readonly Dictionary<string, double> _frequencies = new Dictionary<string, double>();

        private double _activeFrequencyGHz;
        private bool _overrideActive;

        public event Action<double> FrequencyChanged;

        public RestFrequencyService(VolumeDataSet dataSet)
        {
            _dataSet = dataSet;
            LoadFrequencyList();
        }

        // ── IRestFrequencyController ───────────────────────────────────────────

        public IReadOnlyDictionary<string, double> GetAvailableFrequencies()
            => _frequencies;

        public void SetFrequencyByIndex(int index)
        {
            if (index == 0)
            {
                ClearOverride();
                return;
            }

            if (index == _frequencies.Count - 1)
            {
                _overrideActive = true;
                return;
            }

            _overrideActive       = true;
            _activeFrequencyGHz   = _frequencies.Values.ElementAt(index);
            ApplyFrequency();
        }

        public void SetFrequencyOverride(double freqGHz)
        {
            _overrideActive     = true;
            _activeFrequencyGHz = freqGHz;
            ApplyFrequency();
        }

        public void ClearOverride()
        {
            _overrideActive = false;
            _activeFrequencyGHz = _dataSet.HasFitsRestFrequency
                ? _dataSet.FitsRestFrequency
                : 0.0;
            ApplyFrequency();
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void LoadFrequencyList()
        {
            _frequencies.Clear();
            _frequencies.Add("Default", _dataSet.HasFitsRestFrequency ? _dataSet.FitsRestFrequency : 0.0);
            foreach (var line in Config.Instance.restFrequenciesGHz)
                _frequencies.Add(line.Key, line.Value);
            _frequencies.Add("Custom", 0.0);

            _activeFrequencyGHz = _frequencies["Default"];
        }

        private void ApplyFrequency()
        {
            if (_dataSet.HasWCS)
            {
                _dataSet.RecreateFrameSet(_activeFrequencyGHz);
                _dataSet.CreateAltSpecFrame();
                _dataSet.HasRestFrequency = true;
            }
            FrequencyChanged?.Invoke(_activeFrequencyGHz);
        }
    }
}
