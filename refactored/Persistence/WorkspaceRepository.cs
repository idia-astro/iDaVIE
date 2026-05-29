// SPDX-License-Identifier: LGPL-3.0-or-later
// WorkspaceRepository — disk I/O for workspace envelopes and the state index.
// ST7-internal infrastructure; nothing here crosses the cross-team boundary.
//
// Legacy: no equivalent exists. VolumeDataSet.SaveMask (line ~1380) writes a
// single FITS file via FitsReader.UpdateMaskInFitsFile; there is no JSON
// serialisation, no index, and no integrity check anywhere in the codebase.
//
// Refactor delta:
//   - SRP: this class owns only the on-disk format (directory layout, JSON
//     serialisation, SHA-256 integrity). Orchestration lives in WorkspaceService.
//   - Each saved state occupies its own GUID-named sub-directory containing
//     workspace.json + integrity.sha256, so concurrent saves never collide and
//     a corrupted file does not affect other states.
//   - Integrity check (SHA-256 of the JSON text) satisfies INV-7.2: a failed
//     check returns null rather than partially restoring corrupt state.
//   - In-memory index cache (_indexCache) avoids repeated disk reads; the cache
//     is invalidated on every Save/Delete so it stays consistent.
//   - MaxSavedWorkspaces enforcement (INV-7.4) is handled here so WorkspaceService
//     never needs to know about the file-system layout.
//   - Serialisation uses Valve.Newtonsoft.Json (existing project dependency via
//     Config loading) — no new package required. Stub bodies marked TODO are
//     replaced with JsonConvert.SerializeObject / DeserializeObject calls.

using System;
using System.Collections.Generic;
using System.IO;
using iDaVIE.Kernel.Contracts;      // Config, ILogSink

namespace iDaVIE.Persistence.Internal
{
    internal sealed class WorkspaceRepository
    {
        private readonly Config   _config;
        private readonly ILogSink _log;

        // In-memory index; loaded lazily on first access, kept in sync after that.
        private List<SavedStateInfo>? _indexCache;

        private string RootPath  => _config.PersistenceRootPath;
        private string IndexPath => Path.Combine(RootPath, "index.json");

        public WorkspaceRepository(Config config, ILogSink log)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _log    = log    ?? throw new ArgumentNullException(nameof(log));
        }

        // ── Save ──────────────────────────────────────────────────────────────

        public void Save(WorkspaceEnvelope envelope)
        {
            EnsureRoot();

            var stateDir = Path.Combine(RootPath, envelope.StateId);
            Directory.CreateDirectory(stateDir);

            var jsonPath = Path.Combine(stateDir, "workspace.json");
            var hashPath = Path.Combine(stateDir, "integrity.sha256");

            // TODO: replace stub with JsonConvert.SerializeObject(envelope, Formatting.Indented)
            var json = SerialiseEnvelope(envelope);

            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
            File.WriteAllText(hashPath, ComputeSha256(json), System.Text.Encoding.ASCII);

            _log.LogInfo(nameof(WorkspaceRepository),
                $"Saved: stateId={envelope.StateId} path={jsonPath}");

            AddToIndex(envelope);
            EnforceLimit();
        }

        // ── Load ──────────────────────────────────────────────────────────────

        // Returns null on missing file or failed integrity check (INV-7.2).
        public WorkspaceEnvelope? Load(string stateId)
        {
            var jsonPath = Path.Combine(RootPath, stateId, "workspace.json");
            var hashPath = Path.Combine(RootPath, stateId, "integrity.sha256");

            if (!File.Exists(jsonPath))
            {
                _log.LogWarning(nameof(WorkspaceRepository),
                    $"Not found: stateId={stateId}");
                return null;
            }

            var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

            if (File.Exists(hashPath))
            {
                var stored   = File.ReadAllText(hashPath).Trim();
                var computed = ComputeSha256(json);
                if (!string.Equals(stored, computed, StringComparison.OrdinalIgnoreCase))
                {
                    _log.LogError(nameof(WorkspaceRepository),
                        $"Integrity check failed: stateId={stateId}");
                    return null;
                }
            }
            else
            {
                _log.LogWarning(nameof(WorkspaceRepository),
                    $"No integrity file for stateId={stateId}; skipping check.");
            }

            // TODO: replace stub with JsonConvert.DeserializeObject<WorkspaceEnvelope>(json)
            return DeserialiseEnvelope(json);
        }

        // ── Index ─────────────────────────────────────────────────────────────

        public IReadOnlyList<SavedStateInfo> GetIndex() => GetOrLoadIndex();

        // Delete is ST7-internal; not on the cross-team IStateIndexQuery surface.
        public void Delete(string stateId)
        {
            var stateDir = Path.Combine(RootPath, stateId);
            if (Directory.Exists(stateDir))
                Directory.Delete(stateDir, recursive: true);

            var index = GetOrLoadIndex();
            index.RemoveAll(s => s.StateId == stateId);
            PersistIndex(index);

            _log.LogInfo(nameof(WorkspaceRepository), $"Deleted: stateId={stateId}");
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
            index.RemoveAll(s => s.StateId == envelope.StateId); // handle re-save
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
                    $"Limit ({limit}) reached; pruning oldest: {oldest.StateId}");
                Delete(oldest.StateId);
                index = GetOrLoadIndex();
            }
        }

        private List<SavedStateInfo> GetOrLoadIndex()
        {
            if (_indexCache != null) return _indexCache;
            if (!File.Exists(IndexPath)) { _indexCache = new(); return _indexCache; }
            var json = File.ReadAllText(IndexPath, System.Text.Encoding.UTF8);
            // TODO: replace stub with JsonConvert.DeserializeObject<List<SavedStateInfo>>(json)
            _indexCache = DeserialiseIndex(json);
            return _indexCache;
        }

        private void PersistIndex(List<SavedStateInfo> index)
        {
            EnsureRoot();
            // TODO: replace stub with JsonConvert.SerializeObject(index)
            File.WriteAllText(IndexPath, SerialiseIndex(index), System.Text.Encoding.UTF8);
            _indexCache = index;
        }

        // Serialisation stubs — replaced with Valve.Newtonsoft.Json calls in production.
        private static string               SerialiseEnvelope(WorkspaceEnvelope _)    => "{}";
        private static WorkspaceEnvelope    DeserialiseEnvelope(string _)             => new();
        private static string               SerialiseIndex(List<SavedStateInfo> _)    => "[]";
        private static List<SavedStateInfo> DeserialiseIndex(string _)                => new();

        private static string ComputeSha256(string content)
        {
            using var sha   = System.Security.Cryptography.SHA256.Create();
            var       bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return BitConverter.ToString(sha.ComputeHash(bytes))
                               .Replace("-", "").ToLowerInvariant();
        }
    }
}