/*
 * Refactoring example 2: VOTable export
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureCatalog_after_excerpt.cs (after state, annotated excerpt)
 *
 * This is not the full FeatureCatalog class, just an excerpt showing the
 * ExportToVoTable method and the constructor injection of
 * IFeaturePersistenceService, to illustrate the delegation chain for this
 * example. The full production class is at
 * Assets/Scripts/FeatureData/FeatureCatalog.cs.
 *
 * FeatureCatalog is a domain aggregate (ADR-011) and lives in
 * iDaVIE.Domain.Feature (ADR-008), not in the old DataFeatures namespace that
 * predated the ADR-008 decision.
 *
 * The export call walks down the layers, one boundary at a time:
 *   FeatureMenuController                        [Unity/client]
 *     FeatureSetService.ExportToVoTable(set)     [iDaVIE.Application.Feature]
 *       FeatureCatalog.ExportToVoTable(set)      [iDaVIE.Domain.Feature] (this file)
 *         IFeaturePersistenceService.ExportToVoTable(set)  [interface, no concrete ref]
 *           FeaturePersistenceService            [iDaVIE.Infrastructure.Persistence]
 *             IVoTableExporter.Export(set, transformer)    [iDaVIE.Domain.Feature]
 *               VoTableExportService             [iDaVIE.Infrastructure.Persistence]
 *
 * Dependencies point inward only: FeatureCatalog references neither
 * IVoTableExporter nor VoTableExportService, only IFeaturePersistenceService,
 * which keeps it within the ADR-001 layering and the ADR-008 namespace rules.
 *
 * The old FeatureSetManager called VoTableSaver.SaveFeatureSetAsVoTable(renderer,
 * path) directly, a static call taking a Unity MonoBehaviour, in the DataFeatures
 * namespace. Now FeatureCatalog.ExportToVoTable(set) takes a plain FeatureSet and
 * delegates to IFeaturePersistenceService, with no knowledge of XML, AstTool, file
 * paths, or IntPtr.
 */

using System;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Documentation excerpt, not compiled as part of the Unity project.
    /// <para>
    /// This file is an annotated copy of selected members from
    /// <c>Assets/Scripts/FeatureData/FeatureCatalog.cs</c>. It is not a real
    /// <c>partial</c> extension of that class; it sits outside <c>Assets/</c> so
    /// Unity's compiler never sees it.
    /// </para>
    /// <para>
    /// Don't add this file to the Assets folder or any .asmdef. That would cause
    /// duplicate-member compile errors, because <c>_persistence</c>, the
    /// constructor, and <c>ExportToVoTable</c> are already declared in the
    /// production class, which is <b>not</b> <c>partial</c>.
    /// </para>
    /// <para>
    /// It exists to show the ExportToVoTable delegation chain for example 2
    /// without reproducing the full 224-line class. See the full class in
    /// Assets/Scripts/FeatureData/FeatureCatalog.cs.
    /// </para>
    /// </summary>
    public sealed partial class FeatureCatalog
    {
        // Persistence boundary.
        //
        // IFeaturePersistenceService is injected at construction (ADR-003);
        // FeatureCatalog never creates the concrete persistence object itself,
        // which would break the Dependency Inversion Principle.
        //
        // In production, FeaturePersistenceService (iDaVIE.Infrastructure.Persistence)
        // is passed in, and it in turn holds an injected IVoTableExporter.
        //
        // In unit tests, a mock or NullFeaturePersistenceService is passed in,
        // keeping FeatureCatalog tests free of file I/O and DLL calls.
        private readonly IFeaturePersistenceService _persistence;

        /// <param name="persistence">
        ///   Persistence service implementation.
        ///   Pass a <c>NullFeaturePersistenceService</c> or mock in unit tests.
        /// </param>
        public FeatureCatalog(IFeaturePersistenceService persistence)
        {
            _persistence = persistence
                ?? throw new ArgumentNullException(nameof(persistence));
        }

        // Use-case: export a FeatureSet to VOTable

        /// <summary>
        /// Exports <paramref name="set"/> to a VOTable XML file.
        /// <para>
        /// FeatureCatalog decides when to export;
        /// <see cref="IFeaturePersistenceService"/> decides how to serialise it
        /// and where to write the file.
        /// </para>
        /// </summary>
        /// <returns>
        ///   The absolute path of the file that was written,
        ///   forwarded from the persistence service for display in the GUI.
        /// </returns>
        public string ExportToVoTable(FeatureSet set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));

            // A single delegation call. FeatureCatalog knows nothing about XML,
            // AstTool, IAstFrame, IVoTableExporter, or file paths; those are
            // infrastructure concerns behind IFeaturePersistenceService.
            return _persistence.ExportToVoTable(set);
        }

        // Note on IFeaturePersistenceService.ExportToVoTable.
        //
        // The concrete persistence service (iDaVIE.Infrastructure.Persistence)
        // receives IVoTableExporter and ICoordinateTransformer at construction:
        //
        //   public sealed class FeaturePersistenceService : IFeaturePersistenceService
        //   {
        //       private readonly IVoTableExporter       _exporter;
        //       private readonly ICoordinateTransformer  _transformer;
        //
        //       public FeaturePersistenceService(
        //           IVoTableExporter exporter,
        //           ICoordinateTransformer transformer)
        //       {
        //           _exporter    = exporter    ?? throw new ArgumentNullException(...);
        //           _transformer = transformer ?? throw new ArgumentNullException(...);
        //       }
        //
        //       public string ExportToVoTable(FeatureSet featureSet)
        //       {
        //           string xml      = _exporter.Export(featureSet, _transformer);
        //           string filePath = ResolveOutputPath(featureSet);
        //           File.WriteAllText(filePath, xml);
        //           return filePath;
        //       }
        //   }
        //
        // File I/O stays out of VoTableExportService and out of the domain layer.
        // IAstFrame is passed through FeatureSet.AstFrame; the concrete
        // AstFrameHandle (iDaVIE.Infrastructure.NativePlugins) holds the real IntPtr.
    }
}
