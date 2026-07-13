/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * ICoordinateTransformer.cs (after state, new file, design-level example)
 *
 * The boundary over the AstTool native DLL calls, in iDaVIE.Domain.Feature
 * (ADR-008).
 *
 * The old VoTableSaver called AstTool.Transform3D and AstTool.Norm as static
 * methods straight inside the export loop. AstTool is a P/Invoke wrapper around
 * DataAnalysis.dll, so the exporter couldn't run in a test without the DLL, its
 * coordinate values came from real WCS maths and weren't deterministic, and it
 * was coupled to AstTool and AstFrame. This interface removes both couplings: in
 * production the concrete AstToolCoordinateTransformer (owned by sub-team 2's WCS
 * plug-in) wraps the real DLL calls, and in tests a StubCoordinateTransformer
 * returns fixed values so assertions stay predictable.
 *
 * It also drops the IntPtr. The previous design passed IntPtr astFrame to both
 * Transform() and Normalise(); a native-boundary type like that belongs in
 * iDaVIE.Infrastructure.NativePlugins, not in a domain interface. Both methods now
 * take IAstFrame (see IAstFrame.cs), and the infrastructure AstFrameHandle holds
 * the real IntPtr out of sight, so domain code never touches unmanaged memory.
 *
 * Sub-team 2 owns the concrete AstToolCoordinateTransformer, so the two teams need
 * to agree the interface name before sprint 2.
 */

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Abstracts the <c>AstTool.Transform3D</c> and <c>AstTool.Norm</c> DLL entry points.
    /// <para>
    /// Implement this interface to wrap the concrete WCS coordinate library.
    /// Inject a stub in unit tests to produce deterministic RA / Dec / Z values
    /// without requiring the <c>DataAnalysis</c> native DLL.
    /// </para>
    /// <para>
    /// The production implementation (<c>AstToolCoordinateTransformer</c>) is
    /// owned by Sub-team 2 (WCS transform plug-in). This interface is the
    /// agreed boundary between Sub-team 5 (feature domain) and Sub-team 2.
    /// </para>
    /// </summary>
    public interface ICoordinateTransformer
    {
        /// <summary>
        /// Converts pixel-space coordinates (x, y, z) to world coordinates
        /// (RA, Dec, spectral value) using the supplied AST frame.
        /// </summary>
        /// <param name="frame">
        ///   Opaque domain handle to the AST World Coordinate System frame
        ///   (see <see cref="IAstFrame"/>). Pass a <c>NullAstFrame</c> stub
        ///   in unit tests, with no unsafe or unmanaged code required.
        /// </param>
        /// <param name="x">Pixel X coordinate.</param>
        /// <param name="y">Pixel Y coordinate.</param>
        /// <param name="z">Pixel Z coordinate (channel or spectral axis).</param>
        /// <param name="ra">Output: Right Ascension in radians.</param>
        /// <param name="dec">Output: Declination in radians.</param>
        /// <param name="zPhys">Output: Physical spectral value (velocity, frequency, or redshift).</param>
        void Transform(
            IAstFrame frame,
            double x, double y, double z,
            out double ra, out double dec, out double zPhys);

        /// <summary>
        /// Normalises world coordinates within the AST frame to a canonical range.
        /// Wraps <c>AstTool.Norm</c> in the production implementation.
        /// </summary>
        /// <param name="frame">Opaque AST frame handle (see <see cref="IAstFrame"/>).</param>
        /// <param name="ra">Right Ascension in radians (from <see cref="Transform"/>).</param>
        /// <param name="dec">Declination in radians (from <see cref="Transform"/>).</param>
        /// <param name="zPhys">Physical spectral value (from <see cref="Transform"/>).</param>
        /// <param name="normRa">Output: normalised RA in radians.</param>
        /// <param name="normDec">Output: normalised Dec in radians.</param>
        /// <param name="normZ">Output: normalised spectral value.</param>
        void Normalise(
            IAstFrame frame,
            double ra, double dec, double zPhys,
            out double normRa, out double normDec, out double normZ);
    }
}
