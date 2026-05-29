// iDaVIE — immersive Data Visualisation Interactive Explorer
// Copyright (C) 2024 IDIA, INAF-OACT
// SPDX-License-Identifier: LGPL-3.0-or-later
//
// Sub-Team 7 — Persistence & Workspace State
// WorkspaceRepository: disk I/O for workspace envelopes and the state index.
// ST7-internal infrastructure — nothing here crosses the cross-team boundary.

namespace iDaVIE.Persistence.Internal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using iDaVIE.Kernel.Contracts;      // Config, ILogSink

    /// <summary>
    /// Reads and writes <see cref="WorkspaceEnvelope"/> files under
    /// <c>Config.PersistenceRootPath</c> and maintains an in-memory + on-disk
    /// index of saved states.
    ///
    /// <para>
    /// Each saved state occupies its own sub-directory named after the
    /// <see cref="WorkspaceEnvelope.StateId"/> (a GUID). Inside that directory:
    /// <list type="bullet">
    ///   <item><description><c>workspace.json</c> — the serialised envelope.</description></item>
    ///   <item><description><c>integrity.sha256</c> — SHA-256 hex digest of the JSON file.</description></item>
    /// </list>
    /// The root directory also contains <c>index.json</c> — a flat list of
    /// <see cref="SavedStateInfo"/> records sorted newest-first, rebuilt on every
    /// save and delete to keep the list consistent without directory scanning.
    /// </para>
    ///
    /// <para>
    /// Serialisation uses <c>Valve.Newtonsoft.Json</c> (already a project
    /// dependency via <c>Config</c> loading — no new package required).
    /// </para>
    /// </summary>
    internal sealed class WorkspaceRepository
    {
        private readonly Config   _config;
        private readonly ILogSink _log;

        // Cached index — rebuilt from disk on first access, then kept in sync.
        private List<SavedStateInfo>? _indexCache;

        private string RootPath  => _config.PersistenceRootPath;
        private string IndexPath => Path.Combine(RootPath, "index.json");

        public WorkspaceRepository(Config config, ILogSink log)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _log    = log    ?? throw new ArgumentNullException(nameof(log));
        }

        // ── Save ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="envelope"/> to disk and updates the index.
        /// Creates the state sub-directory if it does not exist.
        /// Throws <see cref="IOException"/> on file-system errors (caller handles).
        /// </summary>
        public void Save(WorkspaceEnvelope envelope)
        {
            EnsureRoot();

            var stateDir = Path.Combine(RootPath, envelope.StateId);
            Directory.CreateDirectory(stateDir);

            var jsonPath = Path.Combine(stateDir, "workspace.json");
            var hashPath = Path.Combine(stateDir, "integrity.sha256");

            // TODO: replace with Valve.Newtonsoft.Json serialisation
            var json = SerialiseEnvelope(envelope);

            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
            File.WriteAllText(hashPath, ComputeSha256(json), System.Text.Encoding.ASCII);

            _log.LogInfo(nameof(WorkspaceRepository),
                $"Workspace saved: stateId={envelope.StateId}, file={jsonPath}");

            AddToIndex(envelope);
            EnforceLimit();
        }

        // ── Load ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads and deserialises the envelope for <paramref name="stateId"/>.
        /// Returns null if the state does not exist or fails integrity check.
        /// </summary>
        public WorkspaceEnvelope? Load(string stateId)
        {
            var stateDir = Path.Combine(RootPath, stateId);
            var jsonPath = Path.Combine(stateDir, "workspace.json");
            var hashPath = Path.Combine(stateDir, "integrity.sha256");

            if (!File.Exists(jsonPath))
            {
                _log.LogWarning(nameof(WorkspaceRepository),
                    $"Workspace not found: stateId={stateId}");
                return null;
            }

            var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

            if (File.Exists(hashPath))
            {
                var storedHash   = File.ReadAllText(hashPath).Trim();
                var computedHash = ComputeSha256(json);
                if (!string.Equals(storedHash, computedHash, StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogError(nameof(WorkspaceRepository),
                        $"Integrity check failed for stateId={stateId}. " +
                        "File may be corrupted or tampered with.");
                    return null;
                }
            }
            else
            {
                _log.LogWarning(nameof(WorkspaceRepository),
                    $"No integrity file found for stateId={stateId}; proceeding without check.");
            }

            // TODO: replace with Valve.Newtonsoft.Json deserialisation
            return DeserialiseEnvelope(json);
        }

        // ── Index query ───────────────────────────────────────────────────────

        /// <summary>Returns the full in-memory index, loading from disk if needed.</summary>
        public IReadOnlyList<SavedStateInfo> GetIndex()
            => GetOrLoadIndex();

        /// <summary>Removes an entry from the index and deletes its directory.</summary>
        public void Delete(string stateId)
        {
            var stateDir = Path.Combine(RootPath, stateId);
            if (Directory.Exists(stateDir))
                Directory.Delete(stateDir, recursive: true);

            var index = GetOrLoadIndex();
            index.RemoveAll(s => s.StateId == stateId);
            PersistIndex(index);

            _log.LogInfo(nameof(WorkspaceRepository),
                $"Workspace deleted: stateId={stateId}");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EnsureRoot()
        {
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
        }

        private void AddToIndex(WorkspaceEnvelope envelope)
        {
            var index = GetOrLoadIndex();
            index.RemoveAll(s => s.StateId == envelope.StateId);
            index.Insert(0, new SavedStateInfo
            {
                StateId     = envelope.StateId,
                DisplayName = envelope.DisplayName,
                SavedAtUtc  = envelope.SavedAtUtc,
            });
            PersistIndex(index);
        }

        private void EnforceLimit()
        {
            var index = GetOrLoadIndex();
            int limit = _config.MaxSavedWorkspaces;
            while (index.Count > limit)
            {
                var oldest = index[index.Count - 1];
                _log.LogInfo(nameof(WorkspaceRepository),
                    $"Workspace limit ({limit}) reached; pruning oldest: {oldest.StateId}");
                Delete(oldest.StateId);
                index = GetOrLoadIndex();
            }
        }

        private List<SavedStateInfo> GetOrLoadIndex()
        {
            if (_indexCache != null) return _indexCache;

            if (!File.Exists(IndexPath))
            {
                _indexCache = new List<SavedStateInfo>();
                return _indexCache;
            }

            var json = File.ReadAllText(IndexPath, System.Text.Encoding.UTF8);
            // TODO: replace with Valve.Newtonsoft.Json deserialisation
            _indexCache = DeserialiseIndex(json);
            return _indexCache;
        }

        private void PersistIndex(List<SavedStateInfo> index)
        {
            EnsureRoot();
            // TODO: replace with Valve.Newtonsoft.Json serialisation
            var json = SerialiseIndex(index);
            File.WriteAllText(IndexPath, json, System.Text.Encoding.UTF8);
            _indexCache = index;
        }

        // Stubs — replaced with Valve.Newtonsoft.Json calls in production.
        private static string SerialiseEnvelope(WorkspaceEnvelope _)
            => "{}"; // TODO

        private static WorkspaceEnvelope DeserialiseEnvelope(string _)
            => new WorkspaceEnvelope(); // TODO

        private static string SerialiseIndex(List<SavedStateInfo> _)
            => "[]"; // TODO

        private static List<SavedStateInfo> DeserialiseIndex(string _)
            => new List<SavedStateInfo>(); // TODO

        private static string ComputeSha256(string content)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return BitConverter.ToString(sha.ComputeHash(bytes))
                               .Replace("-", "")
                               .ToLowerInvariant();
        }
    }
}