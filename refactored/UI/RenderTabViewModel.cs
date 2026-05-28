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

// RenderTabViewModel — pure C# ViewModel for the Render tab of CanvassDesktop.
//
// Extracts these GUI concerns from VolumeDataSetRenderer:
//
//   VolumeDataSetRenderer.cs:196  public TextMeshProUGUI loadText
//   VolumeDataSetRenderer.cs:197  public Slider progressBar
//   VolumeDataSetRenderer.cs:560  public IEnumerator updateStatus(string label, int progress)
//       → LoadingStepChanged event. View subscribes and updates the UI elements;
//         no Unity type (TextMeshProUGUI, Slider) enters this class.
//
//   VolumeDataSetRenderer.cs:1008 ToastNotification.ShowInfo(...)
//   VolumeDataSetRenderer.cs:1273 ToastNotification.ShowWarning(...)
//   VolumeDataSetRenderer.cs:1283/1315/1330/1361 ToastNotification.ShowError(...)
//   VolumeDataSetRenderer.cs:1319/1349/1365 ToastNotification.ShowSuccess(...)
//       → StatusRaised event. A single ToastAdapter MonoBehaviour subscribes;
//         the renderer never calls ToastNotification directly.
//
//   VolumeDataSetRenderer.cs:63   public ColorMapDelegate OnColorMapChanged
//       → ColorMapChanged event, fired on every IRenderSettings.SettingsChanged.
//         The field-level delegate with no interface is replaced by a typed event.
//
//   VolumeDataSetRenderer.cs:988-1008  cropString + resolutionString building
//       → structured CropStatus value raised via CropStatusChanged; the View
//         formats and displays it without touching the renderer.
//
//   VolumeDataSetRenderer.cs:139-167  RestFrequencyGHzListIndex (setter touches CanvassDesktop)
//   VolumeDataSetRenderer.cs:549-558  PopulateRestFrequenyList() reading Config.Instance
//       → delegates to the injected IRestFrequencyCatalogue; IsCustomRestFrequencySelected
//         replaces the inline comment "use the input field in the CanvassDesktop".
//
//   VolumeDataSetRenderer.cs:603  ShiftColorMap(int delta)
//   VolumeDataSetRenderer.cs:1136 ResetThresholds()
//       → commands that delegate to IRenderSettingsMutator; the renderer never
//         exposes these directly to the GUI after the refactor.
//
// SOLID/GRASP:
//   SRP  — Render-tab presentation state only. No UnityEngine import.
//   OCP  — new notifications add a subscriber, not a branch in this class.
//   DIP  — depends on IRenderSettings, IRenderSettingsMutator, IRestFrequencyCatalogue.
//   GRASP Controller — single entry point for all Render-tab user actions.

using System;
using iDaVIE.Rendering;            // IRestFrequencyCatalogue, RestFrequencyEntry
using iDaVIE.Rendering.Contracts;  // IRenderSettings, IRenderSettingsMutator

namespace iDaVIE.UI
{
    public enum StatusLevel { Info, Warning, Error, Success }

    public readonly struct StatusMessage
    {
        public readonly string      Text;
        public readonly StatusLevel Level;
        public StatusMessage(string text, StatusLevel level) { Text = text; Level = level; }
    }

    public readonly struct CropStatus
    {
        public readonly string CropDescription;
        public readonly string ResolutionDescription;
        public CropStatus(string cropDescription, string resolutionDescription)
        {
            CropDescription       = cropDescription;
            ResolutionDescription = resolutionDescription;
        }
    }

    public sealed class RenderTabViewModel : IDisposable
    {
        private readonly IRenderSettings         _settings;
        private readonly IRenderSettingsMutator  _mutator;
        private readonly IRestFrequencyCatalogue _restFreqs;

        // Replaces TextMeshProUGUI loadText + Slider progressBar + updateStatus() coroutine.
        // View subscribes and updates the UI elements; no Unity type crosses this boundary.
        public event Action<string, int>? LoadingStepChanged;

        // Replaces ToastNotification.ShowInfo/Warning/Error/Success scattered across the renderer.
        // A single ToastAdapter MonoBehaviour subscribes and shows the notification.
        public event Action<StatusMessage>? StatusRaised;

        // Replaces ColorMapDelegate OnColorMapChanged (field-level event with no interface).
        // Fired after every IRenderSettings.SettingsChanged so the colour-map label refreshes.
        public event Action? ColorMapChanged;

        // Replaces inline crop/resolution string building in VolumeDataSetRenderer.LoadRegionData.
        // Consumer is the Render-tab crop-status label.
        public event Action<CropStatus>? CropStatusChanged;

        // Replaces RestFrequencyGHzList + RestFrequencyGHzListIndex + the PopulateRestFrequenyList()
        // singleton pull. View binds to Entries for dropdown items and SelectedIndex for selection.
        public IRestFrequencyCatalogue RestFrequencyCatalogue => _restFreqs;

        // Replaces the inline comment in RestFrequencyGHzListIndex setter:
        // "if 'Custom' is selected, use the input field in the CanvassDesktop".
        // View binds to show/hide the custom-frequency text field.
        public bool IsCustomRestFrequencySelected =>
            _restFreqs.SelectedIndex == _restFreqs.Entries.Count - 1;

        public RenderTabViewModel(
            IRenderSettings         settings,
            IRenderSettingsMutator  mutator,
            IRestFrequencyCatalogue restFreqs)
        {
            _settings  = settings  ?? throw new ArgumentNullException(nameof(settings));
            _mutator   = mutator   ?? throw new ArgumentNullException(nameof(mutator));
            _restFreqs = restFreqs ?? throw new ArgumentNullException(nameof(restFreqs));

            _settings.SettingsChanged   += OnSettingsChanged;
            _restFreqs.SelectionChanged += OnRestFrequencySelectionChanged;
        }

        // Replaces VolumeDataSetRenderer.updateStatus() coroutine body.
        // Called by the composition root during startup, not by the renderer.
        public void ReportLoadingStep(string label, int stepIndex) =>
            LoadingStepChanged?.Invoke(label, stepIndex);

        // Replaces the scattered ToastNotification.Show*() calls inside the renderer.
        public void RaiseStatus(string text, StatusLevel level) =>
            StatusRaised?.Invoke(new StatusMessage(text, level));

        // Replaces the cropString + resolutionString string building in LoadRegionData.
        public void ReportCropStatus(string cropDescription, string resolutionDescription) =>
            CropStatusChanged?.Invoke(new CropStatus(cropDescription, resolutionDescription));

        // Command — replaces VolumeDataSetRenderer.ShiftColorMap(int delta) as GUI entry point.
        public void ShiftColorMap(int delta)
        {
            _mutator.ShiftColorMap(delta);
            ColorMapChanged?.Invoke();
        }

        // Command — replaces VolumeDataSetRenderer.ResetThresholds() as GUI entry point.
        public void ResetThresholds() => _mutator.ResetThreshold();

        // Command — replaces the transform-reset call that CanvassDesktop drove directly.
        public void ResetTransform() => _mutator.ResetTransform();

        private void OnSettingsChanged()             => ColorMapChanged?.Invoke();
        private void OnRestFrequencySelectionChanged() { /* propagation handled by IRestFrequencyCatalogue */ }

        public void Dispose()
        {
            _settings.SettingsChanged   -= OnSettingsChanged;
            _restFreqs.SelectionChanged -= OnRestFrequencySelectionChanged;
        }
    }
}
