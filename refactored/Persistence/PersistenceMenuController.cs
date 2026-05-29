// SPDX-License-Identifier: LGPL-3.0-or-later
// PersistenceMenuController — Unity anti-corruption layer for the persistence UI.
// The ONLY MonoBehaviour in ST7; all domain logic lives in WorkspaceService.
//
// Legacy: no persistence UI exists. The only user-facing save action is
// CanvassDesktop.SaveMaskButton_Click (line ~890 in CanvassDesktop.cs) which
// calls VolumeDataSetRenderer.SaveMask() directly — a GUI → renderer coupling
// with no event feedback and no state-index display.
//
// Refactor delta:
//   - SRP: translates Unity lifecycle events and button callbacks into calls on
//     the four ST7 domain interfaces. Owns no domain logic, no file paths, no
//     serialisation knowledge.
//   - DIP: all ST7 interfaces are resolved from IPluginRegistry in Start().
//     No direct reference to WorkspaceService or WorkspaceRepository.
//   - This is the only file in Persistence/ that may import UnityEngine types
//     (brief §4.2 constraint 3 — domain code must not depend on UnityEngine).
//   - IDesktopShell is imported from ST1 (not ST6) to mount the persistence
//     panels, dissolving the ST6 ↔ ST7 cycle (M-26, global_model.md §2).
//   - OCP: new persistence events (e.g. a future AutoSaveTriggered) add a
//     subscriber method here and a new event on IPersistenceEvents — no
//     existing branches in this file change.

using System.Collections.Generic;
using iDaVIE.Kernel.Contracts;    // IDesktopShell, ILogSink
using UnityEngine;

namespace iDaVIE.Persistence
{
    public sealed class PersistenceMenuController : MonoBehaviour
    {
        [SerializeField] private string _saveDialogPanelId  = "persistence-save";
        [SerializeField] private string _loadDialogPanelId  = "persistence-load";
        [SerializeField] private string _statusToastPanelId = "persistence-toast";

        // Resolved in Start() via IPluginRegistry — no FindObjectOfType, no singletons.
        private IWorkspaceSaveCommand? _saveCommand;
        private IWorkspaceLoadCommand? _loadCommand;
        private IStateIndexQuery?      _indexQuery;
        private IPersistenceEvents?    _events;
        private IDesktopShell?         _shell;
        private ILogSink?              _log;

        private void Start()
        {
            // TODO: resolve from IPluginRegistry once KernelCompositionRoot is wired.
            // Replaces the VolumeDataSetRenderer.Start() pattern of FindObjectOfType
            // and Config.Instance reads (lines 353–537).
            // _saveCommand = pluginRegistry.GetPlugin<IWorkspaceSaveCommand>();
            // _loadCommand = pluginRegistry.GetPlugin<IWorkspaceLoadCommand>();
            // _indexQuery  = pluginRegistry.GetPlugin<IStateIndexQuery>();
            // _events      = pluginRegistry.GetPlugin<IPersistenceEvents>();
            // _shell       = pluginRegistry.GetPlugin<IDesktopShell>();
            // _log         = pluginRegistry.GetPlugin<ILogSink>();

            SubscribeToEvents();
            MountPanels();
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ── UI callbacks (wired to Unity button OnClick events) ───────────────

        // Replaces CanvassDesktop.SaveMaskButton_Click → VolumeDataSetRenderer.SaveMask().
        // Now fire-and-forget; result arrives via IPersistenceEvents.
        public void OnSaveButtonClicked() => _saveCommand?.Save();

        // Replaces the absent "load workspace" button — new feature.
        public void OnLoadButtonClicked(string stateId) => _loadCommand?.Load(stateId);

        // Delete is not on the cross-team IStateIndexQuery surface.
        // TODO: expose via a narrow IWorkspaceDeleteCommand (ST7-03 open item).
        public void OnDeleteButtonClicked(string stateId)
        {
            _log?.LogWarning(nameof(PersistenceMenuController),
                $"Delete requested for stateId={stateId}; wire IWorkspaceDeleteCommand (ST7-03).");
            RefreshStateList();
        }

        public void OnDialogOpened() => RefreshStateList();

        // ── Private helpers ───────────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            if (_events == null) return;
            _events.SaveStarted   += OnSaveStarted;
            _events.SaveCompleted += OnSaveCompleted;
            _events.SaveFailed    += OnSaveFailed;
            _events.LoadStarted   += OnLoadStarted;
            _events.LoadCompleted += OnLoadCompleted;
            _events.LoadFailed    += OnLoadFailed;
        }

        private void UnsubscribeFromEvents()
        {
            if (_events == null) return;
            _events.SaveStarted   -= OnSaveStarted;
            _events.SaveCompleted -= OnSaveCompleted;
            _events.SaveFailed    -= OnSaveFailed;
            _events.LoadStarted   -= OnLoadStarted;
            _events.LoadCompleted -= OnLoadCompleted;
            _events.LoadFailed    -= OnLoadFailed;
        }

        private void MountPanels()
        {
            // TODO: mount via IDesktopShell once cast-token type is agreed (IR-01).
            // _shell?.MountPanel(_saveDialogPanelId, saveDialogGameObject);
            // _shell?.MountPanel(_loadDialogPanelId, loadDialogGameObject);
        }

        private void RefreshStateList()
        {
            if (_indexQuery == null) return;
            IReadOnlyList<SavedStateInfo> states = _indexQuery.GetAll();
            // TODO: populate UI list widget with `states`.
            _log?.LogInfo(nameof(PersistenceMenuController),
                $"State list refreshed: {states.Count} entries.");
        }

        // ── IPersistenceEvents handlers ───────────────────────────────────────

        private void OnSaveStarted()
            => _log?.LogInfo(nameof(PersistenceMenuController), "Save started...");

        private void OnSaveCompleted(string stateId)
        {
            _log?.LogInfo(nameof(PersistenceMenuController), $"Save completed: {stateId}");
            RefreshStateList();
            // TODO: show success toast via IDesktopShell.
        }

        private void OnSaveFailed(string reason)
            => _log?.LogError(nameof(PersistenceMenuController), $"Save failed: {reason}");

        private void OnLoadStarted()
            => _log?.LogInfo(nameof(PersistenceMenuController), "Load started...");

        private void OnLoadCompleted()
        {
            _log?.LogInfo(nameof(PersistenceMenuController), "Load completed.");
            // TODO: close dialog panel via IDesktopShell.
        }

        private void OnLoadFailed(string reason)
            => _log?.LogError(nameof(PersistenceMenuController), $"Load failed: {reason}");
    }
}