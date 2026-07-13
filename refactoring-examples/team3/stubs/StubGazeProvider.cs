// StubGazeProvider.cs
// Test double for IGazeProvider.
//
// Used by: Example1_RendererSplitTests.cs
//
// PURPOSE
// -------
// FoveatedSamplingPolicy depends on IGazeProvider (the Sub-team 4 contract) to
// read gaze position and availability. In unit tests we cannot use the real
// SteamVR / OpenXR eye-tracking SDK — it requires physical HMD hardware, a
// running player loop, and a signed SteamVR plugin.
//
// StubGazeProvider is the test double. It takes a fixed focus point and
// availability flag at construction time and returns them unchanged on every
// call. This lets tests drive any gaze scenario — foveal, parafoveal, peripheral,
// HMD absent — without any hardware or SDK dependency.
//
// DESIGN NOTE
// -----------
// Named "Stub" rather than "Mock" intentionally:
//   • A Stub returns pre-programmed values. No interaction verification is
//     performed on it — we care about the output of FoveatedSamplingPolicy,
//     not about whether GazeFocusPoint was called N times.
//   • Stubs live in the iDaVIE.Rendering.Tests namespace so they cannot
//     accidentally be shipped in the production assembly.
//
// PLACEMENT
//   refactoring-examples/team3/stubs/StubGazeProvider.cs
//
// ASMDEF
//   iDaVIE.Rendering.Tests.asmdef  (Editor-only, references iDaVIE.Rendering)

using UnityEngine;

// IGazeProvider is defined in FoveatedSamplingPolicy.cs (iDaVIE.Rendering namespace).
// In a real Unity project both files share the iDaVIE.Rendering assembly; the
// test assembly then references it. Here they are co-located for the proposal.
namespace iDaVIE.Rendering.Tests
{
    /// <summary>
    /// Controllable test double for <see cref="IGazeProvider"/>.
    /// Returns a fixed gaze focus point and availability flag supplied at
    /// construction time. Suitable for all unit-test tiers — no Unity player
    /// loop, no HMD hardware, no eye-tracking SDK required.
    /// </summary>
    public sealed class StubGazeProvider : IGazeProvider
    {
        // ── Stored state ──────────────────────────────────────────────────────

        private readonly Vector2 _fixedFocus;
        private readonly bool    _gazeAvailable;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the stub with the values it will return on every call.
        /// </summary>
        /// <param name="fixedFocus">
        ///   Normalised screen-space position [0,1]×[0,1] returned by
        ///   <see cref="GazeFocusPoint"/>. Use <c>(0.5f, 0.5f)</c> to simulate
        ///   gaze at screen centre (the foveal zone default).
        /// </param>
        /// <param name="gazeAvailable">
        ///   Value returned by <see cref="IsGazeAvailable"/>. Pass
        ///   <c>false</c> to drive the HMD-absent fallback path.
        /// </param>
        public StubGazeProvider(Vector2 fixedFocus, bool gazeAvailable)
        {
            _fixedFocus    = fixedFocus;
            _gazeAvailable = gazeAvailable;
        }

        // ── IGazeProvider implementation ──────────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Returns the <c>fixedFocus</c> value supplied at construction time,
        /// unchanged on every call. Simulates a stable gaze point for
        /// deterministic test assertions.
        /// </remarks>
        public Vector2 GazeFocusPoint => _fixedFocus;

        /// <inheritdoc/>
        /// <remarks>
        /// Returns the <c>gazeAvailable</c> flag supplied at construction time.
        /// Set to <c>false</c> to exercise the
        /// <see cref="FoveationParameters.Uniform"/> fallback path.
        /// </remarks>
        public bool IsGazeAvailable => _gazeAvailable;
    }
}
