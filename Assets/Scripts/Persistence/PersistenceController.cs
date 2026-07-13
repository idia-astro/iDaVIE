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

using System;
using System.IO;
using iDaVIE.Persistence.Application;
using iDaVIE.Persistence.Application.Interfaces;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;
using iDaVIE.Persistence.Adapters;
using UnityEngine;
using VolumeData;
using DataFeatures;

namespace iDaVIE.Persistence
{
    /// <summary>
    /// Bootstraps the persistence pipeline inside Unity.
    ///
    /// Wiring order:
    ///   Awake  → resolve scene refs, build all services
    ///   Start  → CrashDetector.CheckAndAcquire() → optional restore → InvokeRepeating(RunAutosave)
    ///   OnApplicationQuit / OnDestroy → CancelInvoke, release crash lock
    /// </summary>
    public class PersistenceController : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("The VolumeInputController in the scene.")]
        public VolumeInputController volumeInputController;

        // ── Services (built in Awake) ─────────────────────────────────────────
        private CrashDetector        _crashDetector;
        private SaveWorkspaceUseCase _saveUseCase;
        private SnapshotRing         _ring;
        private SnapshotSerializer   _serializer;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            string snapshotDir = ResolveSnapshotDirectory();
            Debug.Log($"[Persistence] Snapshot directory: {snapshotDir}");

            _ring          = new SnapshotRing(snapshotDir, Config.Instance.snapshotCapacity);
            _serializer    = new SnapshotSerializer();
            _crashDetector = new CrashDetector(snapshotDir);

            _saveUseCase = new SaveWorkspaceUseCase(
                BuildCollector,
                new WorkspaceStubFactory(),
                new WorkspaceValidator(),
                _serializer,
                _ring);
        }

        private void Start()
        {
            bool crashed = _crashDetector.CheckAndAcquire();

            if (crashed)
            {
                Debug.LogWarning("[Persistence] Crash detected — attempting recovery from last snapshot.");
                TryRestore();
            }
            else
            {
                Debug.Log("[Persistence] Clean start — no crash detected.");
            }

            float interval = Config.Instance.autosaveIntervalSeconds;
            InvokeRepeating(nameof(RunAutosave), interval, interval);
            Debug.Log($"[Persistence] Autosave started (interval = {Config.Instance.autosaveIntervalSeconds}s).");
        }

        private void OnApplicationQuit()
        {
            CleanShutdown();
        }

        private void OnDestroy()
        {
            CleanShutdown();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers an immediate save and resets the autosave timer.
        /// Call from a UI button handler on the main thread.
        /// </summary>
        public void SaveNow()
        {
            RunAutosave();
            CancelInvoke(nameof(RunAutosave));
            float interval = Config.Instance.autosaveIntervalSeconds;
            InvokeRepeating(nameof(RunAutosave), interval, interval);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void RunAutosave()
        {
            bool saved = _saveUseCase.Execute();
            Debug.Log(saved
                ? "[Persistence] Autosave snapshot written."
                : "[Persistence] Save skipped — no dataset loaded.");
        }

        private IWorkspaceStateCollector BuildCollector()
        {
            var renderer = volumeInputController != null ? volumeInputController.ActiveDataSet : null;
            if (renderer == null) return null;

            var featureManager = renderer.GetComponentInChildren<FeatureSetManager>();
            return new UnityWorkspaceStateCollector(
                new DataIoStateAdapter(renderer),
                new RenderingStateAdapter(renderer),
                new FeatureStateAdapter(featureManager),
                new InteractionStateAdapter(volumeInputController),
                new GuiStateAdapter(renderer));
        }

        private void TryRestore()
        {
            // RestoreOrchestrator is not yet wired to a UI button; restore is deferred.
            Debug.LogWarning("[Persistence] Crash recovery restore is not yet implemented.");
        }

        private void CleanShutdown()
        {
            CancelInvoke(nameof(RunAutosave));
            if (_crashDetector != null)
                _crashDetector.Release();
            Debug.Log("[Persistence] Clean shutdown — crash lock released.");
        }

        private static string ResolveSnapshotDirectory()
        {
            string dir = Config.Instance.workspaceSaveDirectory;
            if (!string.IsNullOrEmpty(dir))
                return dir;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "iDaVIE", "workspaces", "default", "snapshots");
        }
    }
}
