using iDaVIE.Persistence.Domain;
using Newtonsoft.Json;

namespace iDaVIE.Persistence.Infrastructure;

/// <summary>
/// A handle pointing at one ring slot on disk.
/// </summary>
public record SnapshotHandle(int SlotIndex, string FilePath, DateTime SavedAt);

/// <summary>
/// On-disk representation of the ring index.
/// </summary>
internal class RingIndex
{
    public int Head     { get; set; }  // next slot to write
    public int Count    { get; set; }
    public int Capacity { get; set; } = 10;
}

/// <summary>
/// Manages a fixed-capacity FIFO ring buffer of snapshot files on disk.
///
/// Layout:
///   &lt;dir&gt;/ring.json          — index: { head, count, capacity }
///   &lt;dir&gt;/snapshot_00.json   — slot 0
///   ...
///   &lt;dir&gt;/snapshot_09.json   — slot 9
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
    private readonly object _lock = new();

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

            int slot     = index.Head % _capacity;
            string tmp   = SlotTmpPath(slot);
            string final = SlotPath(slot);

            // Write tmp
            string json = serializer.Serialize(snapshot);
            File.WriteAllText(tmp, json, System.Text.Encoding.UTF8);

            // Atomic rename
            File.Move(tmp, final, overwrite: true);

            // Advance ring
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
                int slot = ((index.Head - i) % _capacity + _capacity) % _capacity;
                string path = SlotPath(slot);
                if (!File.Exists(path)) continue;

                var savedAt = File.GetLastWriteTimeUtc(path);
                handles.Add(new SnapshotHandle(slot, path, savedAt));
            }
            return handles;
        }
    }

    /// <summary>Returns the most recent snapshot handle, or null if the ring is empty.</summary>
    public SnapshotHandle? Peek()
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
        File.Move(tmp, IndexPath, overwrite: true);
    }
}
