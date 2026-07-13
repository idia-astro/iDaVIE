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
using System.Collections.Generic;
using System.IO;
using iDaVIE.Persistence.Domain;
using Valve.Newtonsoft.Json;

namespace iDaVIE.Persistence.Infrastructure
{
    /// <summary>
    /// A handle pointing at one ring slot on disk.
    /// </summary>
    public class SnapshotHandle
    {
        public int      SlotIndex { get; }
        public string   FilePath  { get; }
        public DateTime SavedAt   { get; }

        public SnapshotHandle(int slotIndex, string filePath, DateTime savedAt)
        {
            SlotIndex = slotIndex;
            FilePath  = filePath;
            SavedAt   = savedAt;
        }
    }

    internal class RingIndex
    {
        public int Head     { get; set; }
        public int Count    { get; set; }
        public int Capacity { get; set; } = 10;
    }

    /// <summary>
    /// Manages a fixed-capacity FIFO ring buffer of snapshot files on disk.
    ///
    /// Layout:
    ///   dir/ring.json          — index: { head, count, capacity }
    ///   dir/snapshot_00.json   — slot 0
    ///   ...
    ///
    /// Atomic write protocol (crash-safe):
    ///   1. Write JSON to snapshot_XX.json.tmp
    ///   2. Rename .tmp → .json  (atomic on NTFS/ext4)
    ///   3. Update ring.json
    /// </summary>
    public class SnapshotRing
    {
        private readonly string _directory;
        private readonly int    _capacity;
        private readonly object _lock = new object();

        public SnapshotRing(string directory, int capacity = 10)
        {
            _directory = directory;
            _capacity  = capacity;
            Directory.CreateDirectory(directory);
        }

        /// <summary>Serialises and atomically pushes a snapshot into the ring.</summary>
        public void Push(WorkspaceSnapshot snapshot, SnapshotSerializer serializer)
        {
            lock (_lock)
            {
                var index = ReadIndex();

                int    slot  = index.Head % _capacity;
                string tmp   = SlotTmpPath(slot);
                string final = SlotPath(slot);

                string json = serializer.Serialize(snapshot);
                File.WriteAllText(tmp, json, System.Text.Encoding.UTF8);

                if (File.Exists(final)) File.Delete(final);
                File.Move(tmp, final);

                index.Head  = (index.Head + 1) % _capacity;
                index.Count = Math.Min(index.Count + 1, _capacity);
                WriteIndex(index);
            }
        }

        /// <summary>Returns all occupied slot handles, newest-first.</summary>
        public IReadOnlyList<SnapshotHandle> GetAllNewestFirst()
        {
            lock (_lock)
            {
                var index   = ReadIndex();
                var handles = new List<SnapshotHandle>();

                for (int i = 1; i <= index.Count; i++)
                {
                    int    slot = ((index.Head - i) % _capacity + _capacity) % _capacity;
                    string path = SlotPath(slot);
                    if (!File.Exists(path)) continue;

                    var savedAt = File.GetLastWriteTimeUtc(path);
                    handles.Add(new SnapshotHandle(slot, path, savedAt));
                }
                return handles;
            }
        }

        /// <summary>Returns the most recent snapshot handle, or null if the ring is empty.</summary>
        public SnapshotHandle Peek()
        {
            var all = GetAllNewestFirst();
            return all.Count > 0 ? all[0] : null;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private string SlotPath(int slot)    => Path.Combine(_directory, $"snapshot_{slot:D2}.json");
        private string SlotTmpPath(int slot) => Path.Combine(_directory, $"snapshot_{slot:D2}.json.tmp");
        private string IndexPath             => Path.Combine(_directory, "ring.json");

        private RingIndex ReadIndex()
        {
            if (!File.Exists(IndexPath))
                return new RingIndex { Capacity = _capacity };
            string json = File.ReadAllText(IndexPath);
            return JsonConvert.DeserializeObject<RingIndex>(json) ?? new RingIndex { Capacity = _capacity };
        }

        private void WriteIndex(RingIndex index)
        {
            string json = JsonConvert.SerializeObject(index, Formatting.Indented);
            string tmp  = IndexPath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(IndexPath)) File.Delete(IndexPath);
            File.Move(tmp, IndexPath);
        }
    }
}
