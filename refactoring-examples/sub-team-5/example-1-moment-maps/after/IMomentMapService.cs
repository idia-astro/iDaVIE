/*
 * Refactoring example 1: moment-map generation
 * Sub-team 5: Feature System and Domain Model
 *
 * IMomentMapService.cs (after state, new file, design-level example)
 *
 * The application-layer use-case interface for moment-map generation, in
 * iDaVIE.Application.Feature (ADR-008). ADR-011 calls moment-map generation an
 * application-layer use case that reaches the Feature aggregate through
 * FeatureSetService with no direct Unity dependency, which is why the interface
 * sits here rather than in iDaVIE.Domain.Feature or the old DataFeatures namespace.
 *
 * The old MomentMapRenderer mixed Unity lifecycle, GPU dispatch, colour-mapping,
 * and UI update in one MonoBehaviour. This interface is the seam that lets the
 * thin adapter (MomentMapRendererAdapter) push the orchestration out to
 * MomentMapService, which in turn hands the raw GPU computation to the injected
 * IMomentMapAdapter. The call chain runs:
 *   MomentMapRendererAdapter : MonoBehaviour     [iDaVIE.Infrastructure.Unity]
 *     IMomentMapService.GenerateMomentMaps()     [iDaVIE.Application.Feature] (here)
 *       MomentMapService                         [iDaVIE.Application.Feature]
 *         IMomentMapAdapter.Compute()            [iDaVIE.Application.Feature]
 *           MomentMapRendererAdapter (GPU)       [iDaVIE.Infrastructure.Unity]
 *         MomentMapCalculator.ComputeBounds()    [iDaVIE.Domain.Feature]
 *       returns MomentMapResult                  [iDaVIE.Application.Feature]
 */

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Application-layer use-case interface for moment-map generation.
    /// <para>
    /// Inject the concrete <see cref="MomentMapService"/> in production.
    /// Inject a stub that returns fixed pixel arrays in unit tests, with no
    /// Unity runtime or GPU compute shader required.
    /// </para>
    /// </summary>
    public interface IMomentMapService
    {
        /// <summary>
        /// Generates moment-0 (integrated intensity) and moment-1
        /// (flux-weighted mean velocity) maps for the given request.
        /// </summary>
        /// <param name="request">
        ///   Describes the data cube, mask, spectrum, and threshold settings.
        ///   Must not be <c>null</c>.
        /// </param>
        /// <returns>
        ///   A <see cref="MomentMapResult"/> containing the computed pixel
        ///   arrays and their min/max bounds, ready for colour-mapping in
        ///   the infrastructure adapter.
        /// </returns>
        MomentMapResult GenerateMomentMaps(MomentMapRequest request);
    }
}
