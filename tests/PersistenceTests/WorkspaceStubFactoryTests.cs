using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Infrastructure;
using NUnit.Framework;

namespace PersistenceTests;

[TestFixture]
public class WorkspaceStubFactoryTests
{
    private WorkspaceStubFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new WorkspaceStubFactory();

    [Test]
    public void DataOnly_HasNoFeatures()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataOnly);
        Assert.That(stub.Features, Is.Null);
    }

    [Test]
    public void DataOnly_HasNoGui()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataOnly);
        Assert.That(stub.Gui, Is.Null);
    }

    [Test]
    public void DataOnly_HasRendering()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataOnly);
        Assert.That(stub.Rendering, Is.Not.Null);
    }

    [Test]
    public void DataWithMask_HasMaskSection()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataWithMask);
        Assert.That(stub.Rendering.Mask, Is.Not.Null);
        Assert.That(stub.Rendering.Mask!.MaskMode, Is.EqualTo("Enabled"));
    }

    [Test]
    public void DataWithMask_HasNoFeatures()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataWithMask);
        Assert.That(stub.Features, Is.Null);
    }

    [Test]
    public void DataWithFeatures_HasFeaturesSection()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataWithFeatures);
        Assert.That(stub.Features, Is.Not.Null);
    }

    [Test]
    public void DataWithFeatures_HasGuiSection()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.DataWithFeatures);
        Assert.That(stub.Gui, Is.Not.Null);
    }

    [Test]
    public void FullWorkspace_HasAllSections()
    {
        var stub = _factory.CreateStub(WorkspaceProfile.FullWorkspace);
        Assert.Multiple(() =>
        {
            Assert.That(stub.Features,            Is.Not.Null, "Features");
            Assert.That(stub.Gui,                 Is.Not.Null, "Gui");
            Assert.That(stub.Rendering.Mask,      Is.Not.Null, "Mask");
            Assert.That(stub.Rendering.Foveation, Is.Not.Null, "Foveation");
            Assert.That(stub.Rendering.MomentMaps,Is.Not.Null, "MomentMaps");
        });
    }

    [Test]
    public void AllProfiles_HaveMetadataWithCorrectProfile(
        [Values(
            WorkspaceProfile.DataOnly,
            WorkspaceProfile.DataWithMask,
            WorkspaceProfile.DataWithFeatures,
            WorkspaceProfile.FullWorkspace)]
        WorkspaceProfile profile)
    {
        var stub = _factory.CreateStub(profile);
        Assert.That(stub.Metadata.Profile, Is.EqualTo(profile));
    }

    [Test]
    public void AllProfiles_HaveDataIoAndInteraction(
        [Values(
            WorkspaceProfile.DataOnly,
            WorkspaceProfile.DataWithMask,
            WorkspaceProfile.DataWithFeatures,
            WorkspaceProfile.FullWorkspace)]
        WorkspaceProfile profile)
    {
        var stub = _factory.CreateStub(profile);
        Assert.Multiple(() =>
        {
            Assert.That(stub.DataIo,      Is.Not.Null, "DataIo");
            Assert.That(stub.Interaction, Is.Not.Null, "Interaction");
        });
    }

    [Test]
    public void UnknownProfile_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _factory.CreateStub((WorkspaceProfile)99));
    }
}
