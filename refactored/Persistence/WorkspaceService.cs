// SPDX-License-Identifier: LGPL-3.0-or-later
// WorkspaceService — application-layer orchestrator for workspace save/restore.
// Realises IWorkspaceSaveCommand, IWorkspaceLoadCommand, IStateIndexQuery,
// and IPersistenceEvents (shared_interfaces.md §7).
//
// Legacy: no equivalent exists. The closest prior art is the manual sequence a
// user must perform today to "save" state: File → Save Mask (VolumeDataSet line
// ~1380), then manually note the FITS path and re-open it next session. Feature
// sets, render settings, interaction state, and desktop layout are lost on close.
//
// Refactor delta:
//   - SRP: this class owns only the save/restore orchestration. Serialisation
//     lives in WorkspaceRepository; UI notification is via IPersistenceEvents;
//     state capture/restore is delegated to the six injected capture ports.
//   - DIP: all six capture ports are constructor-injected interfaces — no
//     Config.Instance, no FindObjectOfType, no static AstTool/FitsReader calls.
//     Conforms to brief §4.2 constraint 4 (every boundary is an interface).
//   - OCP: adding a seventh capture port requires one new constructor parameter
//     and one Capture/Restore call. No switch statements or enums to modify.
//   - Restore order is ST1 → ST2 → ST3 → ST4 → ST5 → ST6, matching the
//     acyclic ownership graph (global_model.md §2): volumes must exist before
//     mask/render/interaction/feature/desktop state can be meaningfully restored.
//   - ISP trade-off: WorkspaceService realises all four ST7 interfaces on one
//     class because all four share the WorkspaceRepository instance and the
//     event-raise logic. Splitting into four classes would require a shared
//     coordinator — recreating the same coupling at one level up. Documented
//     trade-off per brief §4.2 constraint 1.

using System;
using System.Collections.Generic;
using System.Linq;
using iDaVIE.Data;                              // IMaskStateCapture (ST2)
using iDaVIE.Features;                          // IFeatureStateCapture (ST5)
using iDaVIE.Interaction;                       // IInteractionStateCapture (ST4)
using iDaVIE.Kernel.Contracts;                  // ILogSink
using iDaVIE.Kernel.Contracts.Persistence;      // IVolumeStateCapture (ST1)
using iDaVIE.Persistence.Internal;
using iDaVIE.Rendering.Contracts;               // IRenderStateCapture (ST3)
using iDaVIE.UI;                                // IDesktopStateCapture (ST6)

namespace iDaVIE.Persistence
{
    internal sealed class WorkspaceService :
        IWorkspaceSaveCommand,
        IWorkspaceLoadCommand,
        IStateIndexQuery,
        IPersistenceEvents
    {
        // ── Injected capture ports (one per team ST1–ST6) ─────────────────────

        private readonly IVolumeStateCapture      _volumeCapture;      // ST1
        private readonly IMaskStateCapture        _maskCapture;        // ST2
        private readonly IRenderStateCapture      _renderCapture;      // ST3
        private readonly IInteractionStateCapture _interactionCapture; // ST4
        private readonly IFeatureStateCapture     _featureCapture;     // ST5
        private readonly IDesktopStateCapture     _desktopCapture;     // ST6

        private readonly WorkspaceRepository _repository;
        private readonly ILogSink            _log;

        // IPersistenceEvents — nullable backing fields; ?.Invoke() is safe with no subscribers.
        public event Action?         SaveStarted;
        public event Action<string>? SaveCompleted;
        public event Action<string>? SaveFailed;
        public event Action?         LoadStarted;
        public event Action?         LoadCompleted;
        public event Action<string>? LoadFailed;

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

        // ── IWorkspaceSaveCommand ─────────────────────────────────────────────

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
                    // Capture each sub-team's state via its injected port.
                    VolumeState      = _volumeCapture.Capture(),
                    MaskState        = _maskCapture.Capture(),
                    RenderState      = _renderCapture.Capture(),
                    InteractionState = _interactionCapture.Capture(),
                    FeatureState     = _featureCapture.Capture(),
                    DesktopState     = _desktopCapture.Capture(),
                };

                _repository.Save(envelope);
                _log.LogInfo(nameof(WorkspaceService),
                    $"Save completed: stateId={stateId}");
                SaveCompleted?.Invoke(stateId);
            }
            catch (Exception ex)
            {
                var msg = $"Save failed: {ex.Message}";
                _log.LogError(nameof(WorkspaceService), msg);
                SaveFailed?.Invoke(msg);
            }
        }

        // ── IWorkspaceLoadCommand ─────────────────────────────────────────────

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

                // Restore order follows global_model.md §2 acyclic graph: ST1 first.
                if (envelope.VolumeState      != null) _volumeCapture.Restore(envelope.VolumeState);
                if (envelope.MaskState        != null) _maskCapture.Restore(envelope.MaskState);
                if (envelope.RenderState      != null) _renderCapture.Restore(envelope.RenderState);
                if (envelope.InteractionState != null) _interactionCapture.Restore(envelope.InteractionState);
                if (envelope.FeatureState     != null) _featureCapture.Restore(envelope.FeatureState);
                if (envelope.DesktopState     != null) _desktopCapture.Restore(envelope.DesktopState);

                _log.LogInfo(nameof(WorkspaceService), $"Load completed: stateId={stateId}");
                LoadCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                var msg = $"Load failed: {ex.Message}";
                _log.LogError(nameof(WorkspaceService), msg);
                LoadFailed?.Invoke(msg);
            }
        }

        // ── IStateIndexQuery ──────────────────────────────────────────────────

        public IReadOnlyList<SavedStateInfo> GetAll() => _repository.GetIndex();

        public IReadOnlyList<SavedStateInfo> Search(string searchTerm)
        {
            var all = _repository.GetIndex();
            if (string.IsNullOrWhiteSpace(searchTerm)) return all;
            return all.Where(s => s.DisplayName.Contains(
                searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Not on the cross-team IStateIndexQuery surface; called by
        // PersistenceMenuController which holds the concrete WorkspaceService type.
        public void Delete(string stateId)
        {
            _repository.Delete(stateId);
            _log.LogInfo(nameof(WorkspaceService), $"Deleted: stateId={stateId}");
        }
    }
}