/*
 * Refactoring example 1: moment-map generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapService.cs (after state, new file, design-level example)
 *
 * The application-layer service in iDaVIE.Application.Feature (ADR-008). It
 * orchestrates moment-map generation but does none of the GPU or UI work itself.
 *
 * The old MomentMapRenderer.CalculateMomentMaps() did everything inside one
 * MonoBehaviour method: GPU dispatch, bounds extraction via RenderTexture
 * readback, and the colour-mapped sprite update. That was untestable and broke
 * SRP. Here the service just:
 *   1. validates the request;
 *   2. hands raw pixel computation to IMomentMapAdapter (GPU in production, a
 *      stub in tests, per ADR-002);
 *   3. computes min/max bounds with MomentMapCalculator (plain maths in
 *      iDaVIE.Domain.Feature, no Unity dependency);
 *   4. returns an immutable MomentMapResult.
 *
 * The infrastructure adapter (MomentMapRendererAdapter) takes that result and
 * does the Unity GPU and UI work: colour-mapping, RenderTexture upload, colourbar
 * update, sprite creation. IMomentMapAdapter is injected at construction, so this
 * class never news up a concrete type and has no UnityEngine import, which lets a
 * StubMomentMapAdapter returning fixed float arrays drive it under test.
 */

using System;
using iDaVIE.Domain.Feature;

// No "using UnityEngine" here on purpose: the class stays Unity-free (ADR-002).

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Application-layer orchestrator for moment-map generation.
    /// <para>
    /// Delegates raw pixel computation to the injected <see cref="IMomentMapAdapter"/>
    /// (GPU in production, a stub in unit tests), then uses
    /// <see cref="MomentMapCalculator"/> to compute display bounds before
    /// packaging results into an immutable <see cref="MomentMapResult"/>.
    /// </para>
    /// <para>
    /// It holds no Unity types, so it runs in headless CI.
    /// </para>
    /// </summary>
    public sealed class MomentMapService : IMomentMapService
    {
        private readonly IMomentMapAdapter _adapter;

        /// <param name="adapter">
        ///   Computation back-end.
        ///   In production: <c>MomentMapRendererAdapter</c> (dispatches GPU kernels).
        ///   In tests: a <c>StubMomentMapAdapter</c> returning fixed float arrays.
        /// </param>
        public MomentMapService(IMomentMapAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <inheritdoc/>
        public MomentMapResult GenerateMomentMaps(MomentMapRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Step 1: hand raw pixel computation to the adapter. In production
            // this dispatches a ComputeShader kernel; in tests it returns pre-set
            // float arrays with no Unity involved.
            var (moment0, moment1) = _adapter.Compute(request);

            // Step 2: compute bounds with the domain maths. MomentMapCalculator
            // lives in iDaVIE.Domain.Feature with no Unity dependency, so the
            // service depends inward on the domain layer (ADR-001).
            var (m0Min, m0Max) = MomentMapCalculator.ComputeMinMaxBounds(moment0);
            var (m1Min, m1Max) = MomentMapCalculator.ComputeMinMaxBounds(moment1);

            // Step 3: return the immutable result.
            return new MomentMapResult(
                moment0Pixels: moment0,
                moment1Pixels: moment1,
                width:         request.Width,
                height:        request.Height,
                moment0Min:    m0Min,
                moment0Max:    m0Max,
                moment1Min:    m1Min,
                moment1Max:    m1Max);
        }
    }
}
