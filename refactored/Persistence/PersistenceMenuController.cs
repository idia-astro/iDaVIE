// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// PersistenceMenuController: Unity anti-corruption layer.
// The ONLY MonoBehaviour in ST7; all domain logic lives in WorkspaceService.
// Mounts the save/load/list UI panels into ST6's desktop shell via IDesktopShell.

namespace iDaVIE.Persistence
{
    using System.Collections.Generic;
    using iDaVIE.Kernel.Contracts;    // IDesktopShell, ILogSink
    using UnityEngine;

    /// <summary>
    /// Unity anti-corruption layer for the persistence UI.
    ///
    /// <para><b>SRP:</b> Translates Unity lifecycle events and UI button callbacks
    /// into calls on the ST7 domain interfaces. Owns no domain logic.</para>
    ///
    /// <para><b>DIP:</b> All ST7 interfaces are resolved from <c>IPluginRegistry</c>
    /// in <c>Start()</c>. No direct reference to <see cref="WorkspaceService"/>
    /// or <see cref="Internal.WorkspaceRepository"/>.</para>
    ///
    /// <para>This is the only file in <c>Persistence/</c> that may reference
    /// <c>UnityEngine</c> types — brief §4.2 constraint 3.</para>
    /// </summary>
    public sealed class PersistenceMenuController : MonoBehaviour
    {
        // ── Injected by KernelCompositionRoot or Unity Inspector ─────────────

        [SerializeField] private string _saveDialogPanelId  = "persistence-save";
        [SerializeField] private string _loadDialogPanelId  = "persistence-load";
        [SerializeField] private string _statusToastPanelId = "persistence-toast";

        // Resolved in Start() via IPluginRegistry.
        private IWorkspaceSaveCommand? _saveCommand;
        private IWorkspaceLoadCommand? _loadCommand;
        private IStateIndexQuery?      _indexQuery;
        private IPersistenceEvents?    _events;
        private IDesktopShell?         _shell;
        private ILogSink?              _log;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            // TODO: resolve interfaces from IPluginRegistry once KernelCompositionRoot is wired.
            // _saveCommand = pluginRegistry.GetPlugin<IWorkspaceSaveCommand>();
            // _loadCommand = pluginRegistry.GetPlugin<IWorkspaceLoadCommand>();
            // _indexQuery  = pluginRegistry.GetPlugin<IStateIndexQuery>();
            // _events      = pluginRegistry.GetPlugin<IPersistenceEvents>();
            // _shell       = pluginRegistry.GetPlugin<IDesktopShell>();
            // _log         = pluginRegistry.GetPlugin<ILogSink>();

            SubscribeToEvents();
            MountPanels();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ── UI callbacks (called by Unity button OnClick events) ──────────────

        /// <summary>Called when the user clicks the Save button.</summary>
        public void OnSaveButtonClicked()
        {
            _saveCommand?.Save();
        }

        /// <summary>Called when the user selects a state from the list and clicks Load.</summary>
        public void OnLoadButtonClicked(string stateId)
        {
            _loadCommand?.Load(stateId);
        }

        /// <summary>Called when the user clicks Delete on a state-list entry.</summary>
        public void OnDeleteButtonClicked(string stateId)
        {
            // Delete is not on IStateIndexQuery — wire via a dedicated internal port.
            // TODO: expose via IWorkspaceDeleteCommand or resolve WorkspaceService directly.
            _log?.LogWarning(nameof(PersistenceMenuController),
                $"Delete requested for stateId={stateId}; wire IWorkspaceDeleteCommand.");
            RefreshStateList();
        }

        /// <summary>Called when the save/load dialog is opened.</summary>
        public void OnDialogOpened()
        {
            RefreshStateList();
        }

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
            // TODO: mount via IDesktopShell once the concrete token type is agreed (IR-01).
            // _shell?.MountPanel(_saveDialogPanelId, saveDialogGameObject);
            // _shell?.MountPanel(_loadDialogPanelId, loadDialogGameObject);
        }

        private void RefreshStateList()
        {
            if (_indexQuery == null) return;
            IReadOnlyList<SavedStateInfo> states = _indexQuery.GetAll();
            // TODO: populate the UI list widget with `states`.
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
        {
            _log?.LogError(nameof(PersistenceMenuController), $"Save failed: {reason}");
            // TODO: show error toast via IDesktopShell.
        }

        private void OnLoadStarted()
            => _log?.LogInfo(nameof(PersistenceMenuController), "Load started...");

        private void OnLoadCompleted()
        {
            _log?.LogInfo(nameof(PersistenceMenuController), "Load completed.");
            // TODO: close dialog panel via IDesktopShell.
        }

        private void OnLoadFailed(string reason)
        {
            _log?.LogError(nameof(PersistenceMenuController), $"Load failed: {reason}");
            // TODO: show error toast via IDesktopShell.
        }
    }
}