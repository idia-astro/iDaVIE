// SPDX-License-Identifier: LGPL-3.0-or-later
// WorkspaceEnvelope — top-level JSON container written to disk by WorkspaceRepository.
// ST7-internal; does not cross the cross-team boundary.
//
// Legacy: no equivalent exists. VolumeDataSet.SaveMask (line ~1380) writes only
// the mask FITS file. There is no format that captures all six sub-systems.
//
// Refactor delta:
//   - All six per-team capture-port DTOs are nullable fields so a partial
//     snapshot (e.g. no volume loaded yet) round-trips without errors.
//   - EnvelopeSchemaVersion gates the outer container shape only; each team's
//     DTO carries its own SchemaVersion for independent forward-compatibility
//     (INV-7.3 — unknown future SchemaVersion must not throw).
//   - No UnityEngine types (brief §4.2 constraint 3). All DTOs are plain C#
//     as specified by their owners in shared_interfaces.md §1–§6.

using System;
using iDaVIE.Data;                             // MaskStateDto  (ST2)
using iDaVIE.Features;                         // FeatureStateDto (ST5)
using iDaVIE.Interaction;                      // InteractionStateDto (ST4)
using iDaVIE.Kernel.Contracts.Persistence;     // VolumeStateDto (ST1)
using iDaVIE.Rendering.Contracts;              // RenderStateDto (ST3)
using iDaVIE.UI;                               // DesktopStateDto (ST6)

namespace iDaVIE.Persistence.Internal
{
    internal sealed class WorkspaceEnvelope
    {
        public int      EnvelopeSchemaVersion { get; set; } = 1;
        public string   StateId               { get; set; } = string.Empty;
        public string   DisplayName           { get; set; } = string.Empty;
        public DateTime SavedAtUtc            { get; set; }

        // One DTO per capture port (ST1–ST6). Null means that sub-system had
        // no state at save time; Restore skips null fields rather than throwing.
        public VolumeStateDto?      VolumeState      { get; set; }  // ST1
        public MaskStateDto?        MaskState        { get; set; }  // ST2
        public RenderStateDto?      RenderState      { get; set; }  // ST3
        public InteractionStateDto? InteractionState { get; set; }  // ST4
        public FeatureStateDto?     FeatureState     { get; set; }  // ST5
        public DesktopStateDto?     DesktopState     { get; set; }  // ST6
    }
}