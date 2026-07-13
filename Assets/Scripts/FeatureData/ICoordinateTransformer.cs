namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Abstracts AstTool.Transform3D and AstTool.Norm so callers (e.g. VoTableExportService)
    /// have no direct dependency on the native DataAnalysis.dll.
    /// Inject a stub implementation in unit tests for deterministic coordinate output.
    /// </summary>
    public interface ICoordinateTransformer
    {
        /// <summary>Converts pixel (x, y, z) to world (RA, Dec, spectral) coordinates.</summary>
        void Transform(
            IAstFrame frame,
            double x, double y, double z,
            out double ra, out double dec, out double zPhys);

        /// <summary>Normalises world coordinates to a canonical range within the AST frame.</summary>
        void Normalise(
            IAstFrame frame,
            double ra, double dec, double zPhys,
            out double normRa, out double normDec, out double normZ);
    }
}
