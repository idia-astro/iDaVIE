/*
 * Refactoring example 1: moment-map generation
 * Sub-team 5: Feature System and Domain Model
 *
 * IMomentMapAdapter.cs (after state, new file, design-level example)
 *
 * The anti-corruption-layer interface for GPU compute-shader dispatch, in
 * iDaVIE.Application.Feature (ADR-008).
 *
 * The old MomentMapRenderer dispatched ComputeShader kernels inside the same
 * class that held domain state (threshold, useMask, colour maps), so the
 * computation path couldn't be tested without a Unity GPU context. This interface
 * is the seam: application and domain code depend on it, the production
 * implementation (MomentMapRendererAdapter in iDaVIE.Infrastructure.Unity) is the
 * only class that imports UnityEngine to do GPU work, and a unit test can inject a
 * StubMomentMapAdapter that returns pre-computed float arrays with no
 * ComputeShader, RenderTexture, or Unity runtime.
 *
 * The work splits cleanly: Compute() extracts raw pixels (GPU in production,
 * float[] in tests), MomentMapCalculator computes bounds (CPU maths), and
 * MomentMapService orchestrates both and returns a MomentMapResult. The adapter
 * knows how to dispatch GPU work but not what to do with the results; that is the
 * service's job.
 */

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Anti-Corruption Layer interface for the moment-map GPU computation back-end.
    /// <para>
    /// Hides Unity <c>ComputeShader</c> dispatch behind a testable boundary.
    /// The production implementation (<c>MomentMapRendererAdapter</c>) lives in
    /// <c>iDaVIE.Infrastructure.Unity</c> and dispatches GPU kernels.
    /// A stub can be injected in unit tests without a Unity runtime.
    /// </para>
    /// </summary>
    public interface IMomentMapAdapter
    {
        /// <summary>
        /// Computes the raw moment-0 and moment-1 pixel arrays from the data
        /// described by <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        ///   Input description: voxel data, optional mask, spectrum, dimensions,
        ///   threshold, and mask flag. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        ///   A tuple of (<c>moment0</c>, <c>moment1</c>), each a flat float array
        ///   of length <c>request.Width * request.Height</c>, ordered [y, x].
        ///   Pixel values are raw sums/means before colour-mapping or display.
        ///   <c>moment1</c> pixels with zero integrated flux are
        ///   <see cref="float.NaN"/>.
        /// </returns>
        (float[] moment0, float[] moment1) Compute(MomentMapRequest request);
    }
}
