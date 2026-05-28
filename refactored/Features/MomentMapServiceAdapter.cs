// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// MomentMapServiceAdapter — thin delegation to ST3's IMomentMapRenderer (M-08).
// Realises IMomentMapService (cross-team, provided to ST6 and to ST5's own
// MomentMapMenuController — brief §6.5 "moment maps").
// Internal sealed per DD-5.
//
// Lives in the ST5 Application layer — NOT the Unity ACL. The MonoBehaviour that
// drives MomentMapRenderer.CalculateMomentMaps() and reads the GPU render texture
// lives in ST3 behind IMomentMapRenderer; this adapter never references UnityEngine.
//
// Severs the legacy three-hop coupling:
//   MomentMapMenuController → getFirstActiveDataSet() → GetMomentMapRenderer() → CalculateMomentMaps()
// replacing it with:
//   MomentMapMenuController → IMomentMapService → MomentMapServiceAdapter → IMomentMapRenderer

using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Rendering.Contracts;   // IMomentMapRenderer, MomentMapRequest, MomentMapResult, MomentOrder

namespace iDaVIE.Features
{
    internal sealed class MomentMapServiceAdapter : IMomentMapService
    {
        private readonly IMomentMapRenderer _renderer;

        public MomentMapServiceAdapter(IMomentMapRenderer renderer)
        {
            _renderer = renderer;
        }

        public Task<MomentMapResult> GenerateAsync(int momentOrder, float threshold, bool useMask)
            => _renderer.RenderMomentMap(
                new MomentMapRequest(
                    Order:     (MomentOrder)momentOrder,
                    Threshold: threshold,
                    UseMask:   useMask,
                    UseZScale: false,
                    Inverted:  false),
                CancellationToken.None);
    }
}
