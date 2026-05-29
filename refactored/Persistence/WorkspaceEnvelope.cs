// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// WorkspaceEnvelope: the top-level serialisation container.
// ST7-internal — does not cross the cross-team boundary.

namespace iDaVIE.Persistence.Internal
{
    using System;
    using iDaVIE.Data;                             // MaskStateDto  (ST2)
    using iDaVIE.Features;                         // FeatureStateDto (ST5)
    using iDaVIE.Interaction;                      // InteractionStateDto (ST4)
    using iDaVIE.Kernel.Contracts.Persistence;     // VolumeStateDto (ST1)
    using iDaVIE.Rendering.Contracts;              // RenderStateDto (ST3)
    using iDaVIE.UI;                               // DesktopStateDto (ST6)

    /// <summary>
    /// Top-level JSON envelope written to disk by <see cref="WorkspaceRepository"/>.
    /// <para>
    /// Fields are nullable so that a partial snapshot (e.g. saved before a
    /// volume is loaded) round-trips without errors. Each team's capture-port
    /// DTO carries its own <c>SchemaVersion</c>; the envelope version gates
    /// the outer shape only.
    /// </para>
    /// </summary>
    internal sealed class WorkspaceEnvelope
    {
        // ── envelope identity ────────────────────────────────────────────────

        /// <summary>Monotonic integer; increment only on breaking schema changes.</summary>
        public int EnvelopeSchemaVersion { get; set; } = 1;

        /// <summary>GUID assigned at save time; used as the primary key in the index.</summary>
        public string StateId { get; set; } = string.Empty;

        /// <summary>Human-readable label chosen by the user (or auto-generated).</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>UTC timestamp recorded when this file was written.</summary>
        public DateTime SavedAtUtc { get; set; }

        // ── per-team capture-port DTOs (one per ST1–ST6) ────────────────────

        /// <summary>ST1 — loaded volumes and active-volume index.</summary>
        public VolumeStateDto? VolumeState { get; set; }

        /// <summary>ST2 — RLE-encoded mask buffer + stroke history flag.</summary>
        public MaskStateDto? MaskState { get; set; }

        /// <summary>ST3 — ray-marching thresholds, colour map, projection, vignette.</summary>
        public RenderStateDto? RenderState { get; set; }

        /// <summary>ST4 — FSM positions, brush config, voice settings, active menu panel.</summary>
        public InteractionStateDto? InteractionState { get; set; }

        /// <summary>ST5 — feature sets (imported, user-defined) and selection-box bounds.</summary>
        public FeatureStateDto? FeatureState { get; set; }

        /// <summary>ST6 — active tab, panel visibility, debug-console scroll position.</summary>
        public DesktopStateDto? DesktopState { get; set; }
    }
}