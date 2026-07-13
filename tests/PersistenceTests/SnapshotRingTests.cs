using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Infrastructure;
using NUnit.Framework;

namespace PersistenceTests;

[TestFixture]
public class SnapshotRingTests
{
    private string _dir = null!;
    private SnapshotRing _ring = null!;
    private SnapshotSerializer _serializer = null!;

    [SetUp]
    public void SetUp()
    {
        _dir        = Path.Combine(Path.GetTempPath(), $"idavie-ring-test-{Guid.NewGuid()}");
        _ring       = new SnapshotRing(_dir, capacity: 3);  // small capacity for easy testing
        _serializer = new SnapshotSerializer();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static WorkspaceSnapshot MakeSnapshot(string fileName = "/data/cube.fits")
    {
        return new WorkspaceSnapshot
        {
            Metadata    = WorkspaceMetadata.Now(WorkspaceProfile.DataOnly),
            DataIo      = new DataIoStateDto { FileName = fileName, SelectedHdu = 1,
                SubsetBounds = new[] { 1, 10, 1, 10, 1, 10 },
                PrimarySpectralSystem = "VRAD", AlternativeSpectralTarget = "FREQ",
                AlternativeSpectralUnit = "Hz", StandardOfRest = "Heliocentric" },
            Rendering   = new RenderingStateDto { ColorMap = "Inferno", ScalingType = "Linear" },
            Interaction = new InteractionStateDto(),
        };
    }

    [Test]
    public void Push_FirstSnapshot_CreatesSlotZero()
    {
        _ring.Push(MakeSnapshot(), _serializer);
        Assert.That(File.Exists(Path.Combine(_dir, "snapshot_00.json")), Is.True);
    }

    [Test]
    public void Push_MultipleSnapshots_CreatesSequentialSlots()
    {
        for (int i = 0; i < 3; i++) _ring.Push(MakeSnapshot(), _serializer);
        for (int i = 0; i < 3; i++)
            Assert.That(File.Exists(Path.Combine(_dir, $"snapshot_{i:D2}.json")), Is.True);
    }

    [Test]
    public void Push_BeyondCapacity_WrapsAround()
    {
        // capacity = 3, push 4 — slot 0 gets overwritten
        for (int i = 0; i < 4; i++) _ring.Push(MakeSnapshot($"/data/cube{i}.fits"), _serializer);
        var handles = _ring.GetAllNewestFirst();
        Assert.That(handles.Count, Is.EqualTo(3));  // still 3 (capacity)
    }

    [Test]
    public void GetAllNewestFirst_ReturnsNewestFirst()
    {
        _ring.Push(MakeSnapshot("a.fits"), _serializer);
        Thread.Sleep(10);
        _ring.Push(MakeSnapshot("b.fits"), _serializer);
        Thread.Sleep(10);
        _ring.Push(MakeSnapshot("c.fits"), _serializer);

        var handles = _ring.GetAllNewestFirst();
        Assert.That(handles.Count, Is.EqualTo(3));
        // Verify ordering by file write time
        Assert.That(handles[0].SavedAt, Is.GreaterThanOrEqualTo(handles[1].SavedAt));
        Assert.That(handles[1].SavedAt, Is.GreaterThanOrEqualTo(handles[2].SavedAt));
    }

    [Test]
    public void Peek_EmptyRing_ReturnsNull()
    {
        Assert.That(_ring.Peek(), Is.Null);
    }

    [Test]
    public void Peek_AfterPush_ReturnsMostRecent()
    {
        _ring.Push(MakeSnapshot(), _serializer);
        var handle = _ring.Peek();
        Assert.That(handle, Is.Not.Null);
        Assert.That(handle!.SlotIndex, Is.EqualTo(0));
    }

    [Test]
    public void Push_AtomicWrite_NoTmpFilesLeftBehind()
    {
        _ring.Push(MakeSnapshot(), _serializer);
        var tmpFiles = Directory.GetFiles(_dir, "*.tmp");
        Assert.That(tmpFiles, Is.Empty);
    }

    [Test]
    public void GetAllNewestFirst_CorruptSlotFileDeleted_SkipsIt()
    {
        _ring.Push(MakeSnapshot(), _serializer);
        _ring.Push(MakeSnapshot(), _serializer);

        // Corrupt slot 0 by deleting its file
        File.Delete(Path.Combine(_dir, "snapshot_00.json"));

        var handles = _ring.GetAllNewestFirst();
        Assert.That(handles, Has.None.Matches<SnapshotHandle>(h => h.SlotIndex == 0));
    }

    [Test]
    public void RingIndex_WrittenAndReadCorrectly()
    {
        _ring.Push(MakeSnapshot(), _serializer);
        _ring.Push(MakeSnapshot(), _serializer);

        // Ring index file must exist
        Assert.That(File.Exists(Path.Combine(_dir, "ring.json")), Is.True);
        var handles = _ring.GetAllNewestFirst();
        Assert.That(handles.Count, Is.EqualTo(2));
    }
}
