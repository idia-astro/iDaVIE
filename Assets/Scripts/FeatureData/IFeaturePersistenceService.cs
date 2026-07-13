/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Sub-team 5: Feature System and Domain Model
 *
 * IFeaturePersistenceService is the boundary between the feature domain
 * (FeatureCatalog) and the persistence layer (WP7). The domain calls this
 * interface and WP7 owns the implementation that touches StreamWriter, XDocument,
 * and Application.dataPath, so the domain depends on an abstraction it defines
 * rather than on a concrete I/O class.
 */

using System.Collections.Generic;
using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Persistence boundary for the feature domain.
    /// Implemented by WP7 (Persistence and Workspace State).
    /// Only FeatureCatalog calls it; domain objects never do.
    /// </summary>
    public interface IFeaturePersistenceService
    {
        /// <summary>
        /// Appends a single user-created feature to the session ASCII output file.
        /// </summary>
        void AppendFeatureToAscii(Feature feature, string outputFileName);

        /// <summary>
        /// Exports a complete feature set to a VOTable XML file.
        /// The implementation, not the caller, resolves the path
        /// (output directory and timestamp naming).
        /// </summary>
        /// <returns>The absolute path of the file that was written.</returns>
        string ExportToVoTable(FeatureSet featureSet);

        /// <summary>
        /// Returns true if the session ASCII output file already exists on disk.
        /// Used by FeatureCatalog to decide whether to write a header row.
        /// </summary>
        bool AsciiOutputExists(string outputFileName);
    }
}