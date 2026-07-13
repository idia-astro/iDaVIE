/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * IVoTableExporter.cs (after state, new file, design-level example)
 *
 * The export plug-in seam, in iDaVIE.Domain.Feature (ADR-008).
 *
 * The old VoTableSaver was a static class in the VoTableReader namespace with no
 * interface, so callers were hard-wired to one output format and adding FITS or
 * JSON-LD would have meant editing VoTableSaver, breaking Open/Closed. This
 * interface is the only method FeatureCatalog needs to know about, so any concrete
 * exporter (VOTable, FITS, JSON-LD) can be injected without touching
 * FeatureCatalog.
 *
 * It lives in the domain namespace because FeatureCatalog (domain) references it,
 * which keeps the dependency pointing inward; the concrete VoTableExportService
 * lives in iDaVIE.Infrastructure.Persistence and depends on this interface. The
 * export call runs:
 *   FeatureSetService.ExportToVoTable            [iDaVIE.Application.Feature]
 *     FeatureCatalog.ExportToVoTable             [iDaVIE.Domain.Feature]
 *       IFeaturePersistenceService               [iDaVIE.Domain.Feature]
 *         IVoTableExporter.Export                [iDaVIE.Domain.Feature] (here)
 *           VoTableExportService                 [iDaVIE.Infrastructure.Persistence]
 */

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Plug-in seam for VOTable serialisation.
    /// <para>
    /// Implement this interface to provide a concrete exporter.
    /// Inject the implementation into <see cref="IFeaturePersistenceService"/>
    /// at the composition root; <see cref="FeatureCatalog"/> is never aware
    /// of the concrete type.
    /// </para>
    /// <para>
    /// A future FITS or JSON-LD exporter can replace this implementation
    /// without touching any domain class (Open/Closed Principle).
    /// </para>
    /// </summary>
    public interface IVoTableExporter
    {
        /// <summary>
        /// Serialises <paramref name="featureSet"/> to a VOTable 1.3 XML string.
        /// </summary>
        /// <param name="featureSet">
        ///   The domain object containing features to export.
        ///   Must not be <c>null</c>.
        /// </param>
        /// <param name="transformer">
        ///   Coordinate transformer used to convert pixel (x,y,z) to (RA, Dec, Z).
        ///   Must not be <c>null</c>.
        ///   Inject a <c>StubCoordinateTransformer</c> in unit tests for
        ///   deterministic output.
        /// </param>
        /// <returns>
        ///   A well-formed VOTable 1.3 XML string ready for writing to disk
        ///   or transmission. The caller (<see cref="IFeaturePersistenceService"/>)
        ///   decides where to write it.
        /// </returns>
        string Export(FeatureSet featureSet, ICoordinateTransformer transformer);
    }
}
