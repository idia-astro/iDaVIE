// SPDX-License-Identifier: LGPL-3.0-or-later
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT — refactor skeleton, design-only.
//
// IFeatureStateCapture and DTO types — M-16 uniform Capture/Restore persistence port.
// Canonical schema: shared_interfaces.md §5.6 (IR-01 minimum viable field list).
// FeatureSetService implements IFeatureStateCapture. ST7 holds this interface.
//
// Persistable state:
//   UserDefined sets — inline feature list (bounds, flags, OriginId).
//   Imported sets   — file path + mapping; features re-derived on Restore.
//   SelectionBox    — bounds only, if active at time of capture.
//   Mask sets       — NOT snapshotted by ST5; they re-derive from ST2 on restore
//                     via IMaskStateCapture + the SourceStatsUpdated bootstrap path.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts.Persistence;   // SubcubeBoundsDto
using iDaVIE.Kernel.Contracts.Types;         // FeatureColour

namespace iDaVIE.Features
{
    /// <summary>Top-level workspace snapshot for the Feature domain.
    /// All fields have defaults so existing callers using new FeatureStateDto()
    /// still compile; teams extend post Day-9 by adding fields with defaults
    /// (non-breaking under the schema-version pattern).</summary>
    public sealed class FeatureStateDto
    {
        public int SchemaVersion { get; set; } = 1;

        /// <summary>One entry per persisted feature set.</summary>
        public List<FeatureSetEntryDto> FeatureSets { get; set; } = new();

        /// <summary>SelectionBox bounds at time of capture; null if none was active.</summary>
        public SubcubeBoundsDto? SelectionBoxBounds { get; set; }
    }

    public sealed class FeatureSetEntryDto
    {
        public string SetName { get; set; } = string.Empty;

        /// <summary>Serialised as enum-name string for forward compatibility.
        /// On Restore, unknown values fall back to UserDefined via
        /// InteractionStateDto.TryParseOrDefault (shared_interfaces.md §4.4).</summary>
        public string Type { get; set; } = nameof(FeatureSetType.UserDefined);

        public FeatureColour DisplayColour { get; set; }
        public bool          IsVisible     { get; set; } = true;

        /// <summary>For Imported sets only: file path + original column mapping.
        /// Features are re-derived from the file on Restore rather than stored inline.
        /// For UserDefined and SelectionBox sets: null.</summary>
        public string?               ImportFilePath { get; set; }
        public FeatureImportMapping? ImportMapping  { get; set; }

        /// <summary>For UserDefined sets: inline feature snapshots.
        /// Empty for Imported (re-derived on Restore) and Mask (not captured).</summary>
        public List<FeatureEntryDto> Features { get; set; } = new();
    }

    public sealed class FeatureEntryDto
    {
        public int    OriginId   { get; set; }
        public string Name       { get; set; } = string.Empty;
        public string Flag       { get; set; } = string.Empty;
        // Bounding-box stored as min/max corners (redundant with Center/Size) so the
        // DTO is self-contained without requiring CartesianCoord arithmetic on restore.
        public int    CenterX    { get; set; }
        public int    CenterY    { get; set; }
        public int    CenterZ    { get; set; }
        public int    BoundsMinX { get; set; }
        public int    BoundsMinY { get; set; }
        public int    BoundsMinZ { get; set; }
        public int    BoundsMaxX { get; set; }
        public int    BoundsMaxY { get; set; }
        public int    BoundsMaxZ { get; set; }
    }

    /// <summary>M-16 uniform Capture/Restore pattern. Implemented by FeatureSetService
    /// (ST5-internal sealed class). ST7 holds this interface.</summary>
    public interface IFeatureStateCapture
    {
        FeatureStateDto Capture();
        void Restore(FeatureStateDto dto);
    }
}
