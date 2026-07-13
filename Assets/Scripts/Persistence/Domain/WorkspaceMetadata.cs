/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace iDaVIE.Persistence.Domain
{
    /// <summary>
    /// Envelope metadata written to the root of every snapshot JSON file.
    /// Declared as a record so WorkspaceSnapshot `with` expressions can produce modified copies.
    /// </summary>
    public record WorkspaceMetadata
    {
        public string SchemaVersion { get; set; } = "1.0.0";
        public DateTime SavedAt { get; set; }
        public WorkspaceProfile Profile { get; set; }

        /// <summary>Plugin versions at save time (e.g. "idavie_native" → "3.2.1").</summary>
        public Dictionary<string, string> PluginVersions { get; set; } = new Dictionary<string, string>();

        /// <summary>SHA-256 checksum over the JSON body (excluding this field).</summary>
        public string Checksum { get; set; }

        /// <summary>True when WorkspaceValidator found and auto-corrected issues at save time.</summary>
        public bool HasValidationWarnings { get; set; }

        /// <summary>True when not all sub-states could be recovered (partial restore).</summary>
        public bool PartialRecovery { get; set; }

        public static WorkspaceMetadata Now(WorkspaceProfile profile) => new WorkspaceMetadata
        {
            SchemaVersion = "1.0.0",
            SavedAt       = DateTime.UtcNow,
            Profile       = profile,
        };
    }
}
