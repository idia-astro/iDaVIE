using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Infrastructure;
using NUnit.Framework;

namespace PersistenceTests;

[TestFixture]
public class MigrationCoordinatorTests
{
    private MigrationCoordinator _coordinator = null!;

    [SetUp]
    public void SetUp() => _coordinator = new MigrationCoordinator();

    private static WorkspaceSnapshot MakeSnapshot(string version)
    {
        return new WorkspaceSnapshot
        {
            Metadata    = new WorkspaceMetadata { SchemaVersion = version, Profile = WorkspaceProfile.DataOnly },
            DataIo      = new DataIoStateDto { FileName = "/data/cube.fits", SelectedHdu = 1,
                SubsetBounds = new[] { 1, 10, 1, 10, 1, 10 },
                PrimarySpectralSystem = "VRAD", AlternativeSpectralTarget = "FREQ",
                AlternativeSpectralUnit = "Hz", StandardOfRest = "Heliocentric" },
            Rendering   = new RenderingStateDto { ColorMap = "Inferno", ScalingType = "Linear" },
            Interaction = new InteractionStateDto(),
        };
    }

    [Test]
    public void MigrateIfNeeded_CurrentVersion_ReturnsUnchanged()
    {
        var snap = MakeSnapshot(MigrationCoordinator.CurrentVersion);
        snap.DataIo.TransformedSpectralValue = 42.0;

        var result = _coordinator.MigrateIfNeeded(snap);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.DataIo.TransformedSpectralValue, Is.EqualTo(42.0));
    }

    [Test]
    public void MigrateIfNeeded_V1_0_MigratesTo_V1_1()
    {
        var snap = MakeSnapshot("1.0.0");

        var result = _coordinator.MigrateIfNeeded(snap);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Metadata.SchemaVersion, Is.EqualTo("1.1.0"));
    }

    [Test]
    public void Migration_1_0_to_1_1_SetsTransformedSpectralValueToZero()
    {
        var snap = MakeSnapshot("1.0.0");

        var result = _coordinator.MigrateIfNeeded(snap)!;

        Assert.That(result.DataIo.TransformedSpectralValue, Is.EqualTo(0.0));
    }

    [Test]
    public void Migration_1_0_to_1_1_PreservesAllOtherFields()
    {
        var snap = MakeSnapshot("1.0.0");
        snap.DataIo.FileName  = "/test/original.fits";
        snap.DataIo.SelectedHdu = 3;
        snap.DataIo.StandardOfRest = "Barycentric";

        var result = _coordinator.MigrateIfNeeded(snap)!;

        Assert.That(result.DataIo.FileName,       Is.EqualTo("/test/original.fits"));
        Assert.That(result.DataIo.SelectedHdu,    Is.EqualTo(3));
        Assert.That(result.DataIo.StandardOfRest, Is.EqualTo("Barycentric"));
    }

    [Test]
    public void MigrateIfNeeded_FutureMajorVersion_ReturnsNull()
    {
        var snap = MakeSnapshot("99.0.0");
        var result = _coordinator.MigrateIfNeeded(snap);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void MigrateIfNeeded_UnparsableVersion_ReturnsNull()
    {
        var snap = MakeSnapshot("not-a-version");
        var result = _coordinator.MigrateIfNeeded(snap);
        Assert.That(result, Is.Null);
    }
}
