// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// WorkspaceService: application-layer orchestrator.
// Realises IWorkspaceSaveCommand, IWorkspaceLoadCommand, IStateIndexQuery,
// and IPersistenceEvents — the four interfaces ST7 publishes (shared_interfaces.md §7).

namespace iDaVIE.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using iDaVIE.Data;                              // IMaskStateCapture  (ST2)
    using iDaVIE.Features;                          // IFeatureStateCapture (ST5)
    using iDaVIE.Interaction;                       // IInteractionStateCapture (ST4)
    using iDaVIE.Kernel.Contracts;                  // Config, ILogSink
    using iDaVIE.Kernel.Contracts.Persistence;      // IVolumeStateCapture (ST1)
    using iDaVIE.Persistence.Internal;
    using iDaVIE.Rendering.Contracts;               // IRenderStateCapture (ST3)
    using iDaVIE.UI;                                // IDesktopStateCapture (ST6)

    /// <summary>
    /// Orchestrates workspace save and restore by composing the six capture ports
    /// (one per team ST1–ST6) into a single <see cref="WorkspaceEnvelope"/> and
    /// delegating disk I/O to <see cref="WorkspaceRepository"/>.
    ///
    /// <para><b>SRP:</b> This class owns only the save/restore orchestration use
    /// case. Serialisation lives in <see cref="WorkspaceRepository"/>; UI
    /// notification is via <see cref="IPersistenceEvents"/>.</para>
    ///
    /// <para><b>DIP:</b> All six capture ports are injected — no singleton access,
    /// no direct reference to concretes from other sub-teams.</para>
    ///
    /// <para><b>OCP:</b> Adding a new capture port requires adding a constructor
    /// parameter and one Capture/Restore call — no switch statements to modify.</para>
    /// </summary>
    internal sealed class WorkspaceService :
        IWorkspaceSaveCommand,
        IWorkspaceLoadCommand,
        IStateIndexQuery,
        IPersistenceEvents
    {
        // ── Injected capture ports (one per team ST1–ST6) ───────────────────

        private readonly IVolumeStateCapture      _volumeCapture;      // ST1
        private readonly IMaskStateCapture        _maskCapture;        // ST2
        private readonly IRenderStateCapture      _renderCapture;      // ST3
        private readonly IInteractionStateCapture _interactionCapture; // ST4
        private readonly IFeatureStateCapture     _featureCapture;     // ST5
        private readonly IDesktopStateCapture     _desktopCapture;     // ST6

        // ── ST7-internal infrastructure ──────────────────────────────────────

        private readonly WorkspaceRepository _repository;
        private readonly ILogSink            _log;

        // ── IPersistenceEvents implementation ────────────────────────────────
        // Nullable backing fields; interface declares non-nullable.
        // Invoked via ?.Invoke() so null (no subscribers) is safe.

        public event Action?         SaveStarted;
        public event Action<string>? SaveCompleted;
        public event Action<string>? SaveFailed;
        public event Action?         LoadStarted;
        public event Action?         LoadCompleted;
        public event Action<string>? LoadFailed;

        // ── Constructor ──────────────────────────────────────────────────────

        public WorkspaceService(
            IVolumeStateCapture      volumeCapture,
            IMaskStateCapture        maskCapture,
            IRenderStateCapture      renderCapture,
            IInteractionStateCapture interactionCapture,
            IFeatureStateCapture     featureCapture,
            IDesktopStateCapture     desktopCapture,
            WorkspaceRepository      repository,
            ILogSink                 log)
        {
            _volumeCapture      = volumeCapture      ?? throw new ArgumentNullException(nameof(volumeCapture));
            _maskCapture        = maskCapture        ?? throw new ArgumentNullException(nameof(maskCapture));
            _renderCapture      = renderCapture      ?? throw new ArgumentNullException(nameof(renderCapture));
            _interactionCapture = interactionCapture ?? throw new ArgumentNullException(nameof(interactionCapture));
            _featureCapture     = featureCapture     ?? throw new ArgumentNullException(nameof(featureCapture));
            _desktopCapture     = desktopCapture     ?? throw new ArgumentNullException(nameof(desktopCapture));
            _repository         = repository         ?? throw new ArgumentNullException(nameof(repository));
            _log                = log                ?? throw new ArgumentNullException(nameof(log));
        }

        // ── IWorkspaceSaveCommand ────────────────────────────────────────────

        /// <inheritdoc/>
        public void Save()
        {
            SaveStarted?.Invoke();
            _log.LogInfo(nameof(WorkspaceService), "Save pipeline started.");

            try
            {
                var stateId = Guid.NewGuid().ToString("N");
                var name    = $"Workspace {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

                var envelope = new WorkspaceEnvelope
                {
                    StateId               = stateId,
                    DisplayName           = name,
                    SavedAtUtc            = DateTime.UtcNow,
                    EnvelopeSchemaVersion = 1,

                    // --- capture each sub-team's state ---
                    VolumeState      = _volumeCapture.Capture(),
                    MaskState        = _maskCapture.Capture(),
                    RenderState      = _renderCapture.Capture(),
                    InteractionState = _interactionCapture.Capture(),
                    FeatureState     = _featureCapture.Capture(),
                    DesktopState     = _desktopCapture.Capture(),
                };

                _repository.Save(envelope);

                _log.LogInfo(nameof(WorkspaceService),
                    $"Save pipeline completed: stateId={stateId}, displayName={name}");
                SaveCompleted?.Invoke(stateId);
            }
            catch (Exception ex)
            {
                var msg = $"Save failed: {ex.Message}";
                _log.LogError(nameof(WorkspaceService), msg);
                SaveFailed?.Invoke(msg);
            }
        }

        // ── IWorkspaceLoadCommand ────────────────────────────────────────────

        /// <inheritdoc/>
        public void Load(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
            {
                LoadFailed?.Invoke("StateId must not be null or empty.");
                return;
            }

            LoadStarted?.Invoke();
            _log.LogInfo(nameof(WorkspaceService), $"Load pipeline started: stateId={stateId}");

            try
            {
                var envelope = _repository.Load(stateId);

                if (envelope == null)
                {
                    var msg = $"Workspace not found or integrity check failed: stateId={stateId}";
                    _log.LogError(nameof(WorkspaceService), msg);
                    LoadFailed?.Invoke(msg);
                    return;
                }

                // Restore order matters: ST1 first (volumes must exist before
                // mask, render, interaction, features, and desktop state restore).
                if (envelope.VolumeState      != null) _volumeCapture.Restore(envelope.VolumeState);
                if (envelope.MaskState        != null) _maskCapture.Restore(envelope.MaskState);
                if (envelope.RenderState      != null) _renderCapture.Restore(envelope.RenderState);
                if (envelope.InteractionState != null) _interactionCapture.Restore(envelope.InteractionState);
                if (envelope.FeatureState     != null) _featureCapture.Restore(envelope.FeatureState);
                if (envelope.DesktopState     != null) _desktopCapture.Restore(envelope.DesktopState);

                _log.LogInfo(nameof(WorkspaceService),
                    $"Load pipeline completed: stateId={stateId}");
                LoadCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                var msg = $"Load failed: {ex.Message}";
                _log.LogError(nameof(WorkspaceService), msg);
                LoadFailed?.Invoke(msg);
            }
        }

        // ── IStateIndexQuery ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public IReadOnlyList<SavedStateInfo> GetAll()
            => _repository.GetIndex();

        /// <inheritdoc/>
        public IReadOnlyList<SavedStateInfo> Search(string searchTerm)
        {
            var all = _repository.GetIndex();
            if (string.IsNullOrWhiteSpace(searchTerm)) return all;

            return all
                .Where(s => s.DisplayName.Contains(
                    searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Deletes a saved state from disk and the index.
        /// Not part of the cross-team <see cref="IStateIndexQuery"/> surface;
        /// called by <see cref="PersistenceMenuController"/> via the concrete type.
        /// </summary>
        public void Delete(string stateId)
        {
            _repository.Delete(stateId);
            _log.LogInfo(nameof(WorkspaceService), $"Deleted state: {stateId}");
        }
    }
}