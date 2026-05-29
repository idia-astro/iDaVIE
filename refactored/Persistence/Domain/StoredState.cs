// SPDX-License-Identifier: LGPL-3.0-or-later
// StoredState — ST7 Domain envelope. Carries state identity + metadata +
// per-team serialised payloads + integrity record. Immutable once written.

using System;
using iDaVIE.Data.Contracts;            // MaskStateDto
using iDaVIE.Features;                  // FeatureStateDto
using iDaVIE.Interaction;               // InteractionStateDto
using iDaVIE.Kernel.Contracts.Persistence; // VolumeStateDto
using iDaVIE.Rendering.Contracts;       // RenderStateDto
using iDaVIE.UI.Contracts;              // DesktopStateDto

namespace iDaVIE.Persistence.Domain
{
    public sealed class StoredState
    {
        public string   StateId        { get; init; } = "";
        public string   Label          { get; init; } = "";
        public DateTime CreatedAt      { get; init; }
        public int      SchemaVersion  { get; init; } = 1;

        public VolumeStateDto      Volume      { get; init; }
        public MaskStateDto        Mask        { get; init; }
        public RenderStateDto      Render      { get; init; }
        public InteractionStateDto Interaction { get; init; }
        public FeatureStateDto     Features    { get; init; }
        public DesktopStateDto     Desktop     { get; init; }

        public IntegrityRecord     Integrity   { get; init; }
    }
}
