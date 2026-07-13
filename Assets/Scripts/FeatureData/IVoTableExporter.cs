using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Plug-in seam for VOTable serialisation.
    /// Inject a concrete implementation (e.g. <see cref="VoTableExportService"/>)
    /// at the composition root. Inject a stub in unit tests for deterministic output.
    /// </summary>
    public interface IVoTableExporter
    {
        /// <summary>
        /// Serialises <paramref name="featureSet"/> to a VOTable 1.3 XML string.
        /// The caller writes the returned string to disk.
        /// </summary>
        string Export(FeatureSet featureSet, ICoordinateTransformer transformer);
    }
}
