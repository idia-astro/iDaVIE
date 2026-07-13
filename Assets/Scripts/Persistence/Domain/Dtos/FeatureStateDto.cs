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

using System.Collections.Generic;
using iDaVIE.Persistence.Domain.Serialization;

namespace iDaVIE.Persistence.Domain.Dtos
{
    /// <summary>
    /// Persistent state for all feature sets managed by FeatureSetManager.
    /// NOT persisted: Selected, mask bounding boxes, mask statistics, GPU refs.
    /// Recomputed on restore: mask bboxes/stats (DataAnalysis), imported RawData (catalog reader).
    /// Temporary features ARE persisted so crash recovery can restore in-progress work.
    /// </summary>
    public class FeatureStateDto
    {
        [PersistField] public List<FeatureSetDto> FeatureSets { get; set; } = new List<FeatureSetDto>();
    }

    /// <summary>Persistent state for a single feature set (FeatureSetRenderer).</summary>
    public class FeatureSetDto
    {
        /// <summary>FeatureSetType enum serialised as string (e.g. "Mask", "New", "Imported").</summary>
        [PersistField] public string FeatureSetType { get; set; }

        /// <summary>Name from featureSetRenderer.gameObject.name.</summary>
        [PersistField] public string SetName { get; set; }

        /// <summary>FeatureSetRenderer.FeatureColor — Unity Color → SerializableColor.</summary>
        [PersistField] public SerializableColor FeatureColor { get; set; }

        /// <summary>FeatureSetRenderer.featureSetVisible.</summary>
        [PersistField] public bool FeatureVisibility { get; set; } = true;

        // ── Imported sets only ──────────────────────────────────────────────────
        [PersistField(Optional = true)] public string FileName { get; set; }

        /// <summary>Column mapping: SourceMappingOptions.ToString() → catalog column name.</summary>
        [PersistField(Optional = true)] public Dictionary<string, string> ColumnMapping { get; set; }

        [PersistField(Optional = true)] public bool[] ColumnsMask     { get; set; }
        [PersistField(Optional = true)] public bool   ExcludeExternal { get; set; }

        [PersistField] public List<FeatureDto> Features { get; set; } = new List<FeatureDto>();
    }

    /// <summary>Persistent state for a single Feature.</summary>
    public class FeatureDto
    {
        [PersistField] public int    Id      { get; set; }
        [PersistField] public string Name    { get; set; }
        [PersistField] public string Flag    { get; set; }
        [PersistField(Optional = true)] public string Comment { get; set; }
        [PersistField] public bool   Visible   { get; set; } = true;
        [PersistField] public SerializableColor   CubeColor  { get; set; }
        [PersistField(Optional = true)] public SerializableVector3 CornerMin { get; set; }
        [PersistField(Optional = true)] public SerializableVector3 CornerMax { get; set; }
        [PersistField(Optional = true)] public string[]            RawData   { get; set; }
        [PersistField(Optional = true)] public bool                IsTemporary { get; set; } = false;
    }
}
