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
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// Unity-side adapter — only layer permitted to reference UnityEngine.
// Reads from FeatureSetManager. Must be called on the Unity main thread.

using System.Collections.Generic;
using System.Linq;
using DataFeatures;
using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Domain.Serialization;
using UnityEngine;

namespace iDaVIE.Persistence.Adapters
{
    /// <summary>
    /// Captures and restores feature set state.
    ///
    /// NOT captured: Selected, Temporary, mask bounding boxes, mask statistics, GPU refs.
    /// ColumnMapping is serialised as Dictionary&lt;string, string&gt; (SourceMappingOptions.ToString() → column name).
    ///
    /// Restore ordering:
    ///   1. Identity fields (Type, Name, Color, Visibility)
    ///   2. Imported: FileName, ColumnMapping, ColumnsMask, ExcludeExternal
    ///   3. Per-feature: Id, Name, Flag, Comment, Visible, CubeColor, CornerMin/Max
    ///   4. New features: RawData
    ///   5. Trigger async recomputation (mask bboxes/stats via DataAnalysis; imported RawData)
    /// </summary>
    public class FeatureStateAdapter : IFeatureStateAdapter
    {
        private readonly FeatureSetManager _manager;

        public FeatureStateAdapter(FeatureSetManager manager)
        {
            _manager = manager;
        }

        public FeatureStateDto? Capture()
        {
            var allSets = _manager.MaskFeatureSetList
                .Concat(_manager.NewFeatureSetList)
                .Concat(_manager.ImportedFeatureSetList)
                .ToList();

            if (allSets.Count == 0) return null;

            var dto = new FeatureStateDto();
            foreach (var fsr in allSets)
            {
                var setDto = new FeatureSetDto
                {
                    FeatureSetType  = fsr.FeatureSetType.ToString(),
                    SetName         = fsr.gameObject.name,
                    FeatureColor    = new SerializableColor(fsr.FeatureColor.r, fsr.FeatureColor.g, fsr.FeatureColor.b, fsr.FeatureColor.a),
                    FeatureVisibility = fsr.featureSetVisible,
                    Features        = new List<FeatureDto>(),
                };

                // Imported-only fields
                if (fsr.FeatureSetType == FeatureSetType.Imported && fsr.FileName != null)
                {
                    setDto.FileName = fsr.FileName;
                    // ColumnMapping is passed to SpawnFeaturesFromTable — adapter must store it
                    // TODO: expose ColumnMapping and ColumnsMask on FeatureSetRenderer or pass here
                }

                foreach (var feature in fsr.FeatureList)
                {
                    var fDto = new FeatureDto
                    {
                        Id      = feature.Id,
                        Name    = feature.Name,
                        Flag    = feature.Flag,
                        Comment = feature.Comment,
                        Visible = feature.Visible,
                        CubeColor = new SerializableColor(feature.CubeColor.r, feature.CubeColor.g, feature.CubeColor.b, feature.CubeColor.a),
                    };

                    // CornerMin/Max — Imported and New features only
                    if (fsr.FeatureSetType == FeatureSetType.Imported || fsr.FeatureSetType == FeatureSetType.New)
                    {
                        fDto.CornerMin = new SerializableVector3(feature.CornerMin.x, feature.CornerMin.y, feature.CornerMin.z);
                        fDto.CornerMax = new SerializableVector3(feature.CornerMax.x, feature.CornerMax.y, feature.CornerMax.z);
                    }

                    // RawData — New features only
                    if (fsr.FeatureSetType == FeatureSetType.New)
                        fDto.RawData = feature.RawData;
                    fDto.IsTemporary = feature.Temporary;
                    setDto.Features.Add(fDto);
                }
                dto.FeatureSets.Add(setDto);
            }
            return dto;
        }

        public void Restore(FeatureStateDto dto)
        {
            // Feature restore runs after Data I/O is complete.
            // Mask bounding boxes and statistics are recomputed asynchronously
            // once the FITS data is available.
            foreach (var setDto in dto.FeatureSets)
            {
                if (!System.Enum.TryParse<FeatureSetType>(setDto.FeatureSetType, out var featureSetType))
                {
                    Debug.LogWarning($"[Persistence] Unknown FeatureSetType '{setDto.FeatureSetType}'; skipping set '{setDto.SetName}'.");
                    continue;
                }

                var color = setDto.FeatureColor != null
                    ? new Color(setDto.FeatureColor.R, setDto.FeatureColor.G, setDto.FeatureColor.B, setDto.FeatureColor.A)
                    : Color.white;

                var fsr = _manager.CreateEmptyFeatureSet(
                    setDto.SetName ?? "Restored Set",
                    "RestoredTag",
                    _manager.NewFeatureSetList.Count,
                    color,
                    featureSetType);

                if (setDto.FeatureVisibility)
                    fsr.SetVisibilityOn();
                else
                    fsr.SetVisibilityOff();

                foreach (var fDto in setDto.Features)
                {
                    if (fDto.CornerMin == null || fDto.CornerMax == null) continue;

                    var cubeColor = fDto.CubeColor != null
                        ? new Color(fDto.CubeColor.R, fDto.CubeColor.G, fDto.CubeColor.B, fDto.CubeColor.A)
                        : color;

                    var min = new Vector3(fDto.CornerMin.X, fDto.CornerMin.Y, fDto.CornerMin.Z);
                    var max = new Vector3(fDto.CornerMax.X, fDto.CornerMax.Y, fDto.CornerMax.Z);

                    var restoredFeature = new Feature(
                            cubeMin:      min,
                            cubeMax:      max,
                            cubeColor:    cubeColor,
                            name:         fDto.Name ?? $"Feature_{fDto.Id}",
                            flag:         fDto.Flag ?? "0",
                            index:        fsr.FeatureList.Count,
                            id:           fDto.Id,
                            rawData:      fDto.RawData,
                            startVisible: fDto.Visible
                        );
                        restoredFeature.Temporary = fDto.IsTemporary;
                        fsr.AddFeature(restoredFeature);
                }
            }

            Debug.Log("[Persistence] FeatureState restored. Async recomputation pending.");
        }
    }
}
