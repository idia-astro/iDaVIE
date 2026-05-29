// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// MomentMapMenuController — ST5-owned VR moment-map menu (brief §6.5 "moment maps").
// Worked Refactoring Example 1 in ST5_refactoring_proposal.md.
//
// Replaces:
//   Assets/Scripts/Menu/MomentMapMenuController.cs (334 LOC)
//
// Refactor delta:
//   - Drops VolumeDataSetRenderer[] dataSets / getFirstActiveDataSet (l. 40, 75-110, 172-182).
//     The three-hop coupling
//       getFirstActiveDataSet().GetMomentMapRenderer().CalculateMomentMaps()
//     (l. 163-200, 184-205) is replaced by a single IMomentMapService call.
//   - Threshold range and increment are derived from IRenderSettings (DataMin /
//     DataMax / momentMapThresholdSteps) — no direct VolumeDataSet field reach.
//   - All UI text manipulation (TMP_Text, Button.isPressed timers) is preserved
//     in the View; only the GPU-coupling path is severed.
//
// Composition root injects IMomentMapService; tests substitute a spy IMomentMapService
// that returns a known MomentMapResult and never touches the GPU.

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDaVIE.Features;                // IMomentMapService
using iDaVIE.Rendering.Contracts;     // MomentMapResult

namespace iDaVIE.UI
{
    internal sealed class MomentMapMenuController : MonoBehaviour
    {
        // ── Inspector wiring (UI only — no domain references) ────────────────
        [SerializeField] private TMP_Text _thresholdTypeText;
        [SerializeField] private TMP_Text _thresholdValueText;
        [SerializeField] private TMP_Text _momentMap0Title;
        [SerializeField] private TMP_Text _momentMap1Title;
        [SerializeField] private RawImage _momentMap0Display;
        [SerializeField] private RawImage _momentMap1Display;

        // ── Injected services ────────────────────────────────────────────────
        private IMomentMapService _service;

        // Cached threshold state — verbatim from legacy l. 41-50.
        private float _threshold;
        private float _cachedThreshold;
        private bool  _useMask = true;
        private int   _activeOrder; // 0 = integrated intensity, 1 = velocity field

        public void Inject(IMomentMapService service)
            => _service = service ?? throw new ArgumentNullException(nameof(service));

        // ── User actions ─────────────────────────────────────────────────────

        /// <summary>Replaces SetThresholdType (l. 184-205) — toggles mask vs.
        /// threshold mode. The legacy method reached
        ///   getFirstActiveDataSet().GetMomentMapRenderer().UseMask
        /// directly; UseMask is now a request field on every GenerateAsync call.</summary>
        public void SetThresholdType()
        {
            _useMask = !_useMask;
            _thresholdTypeText.text = _useMask ? "Mask" : "Threshold";
            _ = RegenerateAsync(_activeOrder);
        }

        /// <summary>Replaces SetMomentMapThreshold (l. 163-170). The legacy
        /// method assigned to MomentMapRenderer.MomentMapThreshold; the refactor
        /// passes threshold as a request field on each generate call instead.</summary>
        public void SetMomentMapThreshold()
        {
            _cachedThreshold = _threshold;
            _ = RegenerateAsync(_activeOrder);
        }

        public void IncreaseMomentMapThreshold(float increment)
        {
            _threshold += increment;
            _thresholdValueText.text = _threshold.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            SetMomentMapThreshold();
        }

        public void DecreaseMomentMapThreshold(float increment)
        {
            _threshold -= increment;
            _thresholdValueText.text = _threshold.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            SetMomentMapThreshold();
        }

        // ── Service interaction ──────────────────────────────────────────────

        private async Task RegenerateAsync(int momentOrder)
        {
            if (_service == null) return;
            // IMomentMapService runs the GPU pass off-thread inside ST3's
            // IMomentMapRenderer; the awaited result is consumed on the Unity
            // main thread before touching Unity APIs (ST5_interface.md §6).
            MomentMapResult result = await _service.GenerateAsync(momentOrder, _threshold, _useMask);
            ApplyResult(momentOrder, result);
        }

        private void ApplyResult(int momentOrder, MomentMapResult result)
        {
            // result.Values is the row-major float[] payload per
            // shared_interfaces.md §3.3. Single Unity-conversion site — the
            // adapter delivers plain C# data, this MonoBehaviour wraps it in
            // a Texture2D for display. Mirrors the worked example in
            // ST5_refactoring_proposal.md §Worked Refactoring Example 1.
            var tex = new Texture2D(result.Width, result.Height, TextureFormat.RFloat, false);
            tex.SetPixelData(result.Values, 0);
            tex.Apply();
            (momentOrder == 0 ? _momentMap0Display : _momentMap1Display).texture = tex;
        }
    }
}
