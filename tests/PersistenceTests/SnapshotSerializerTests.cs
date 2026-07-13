using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Domain.Serialization;
using iDaVIE.Persistence.Infrastructure;
using NUnit.Framework;

namespace PersistenceTests;

[TestFixture]
public class SnapshotSerializerTests
{
    private SnapshotSerializer _serializer = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _serializer = new SnapshotSerializer();
        _tempDir    = Path.Combine(Path.GetTempPath(), $"idavie-ser-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static WorkspaceSnapshot MakeFullSnapshot()
    {
        return new WorkspaceSnapshot
        {
            Metadata = new WorkspaceMetadata
            {
                SchemaVersion = "1.1.0",
                Profile       = WorkspaceProfile.FullWorkspace,
                SavedAt       = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc),
            },
            DataIo = new DataIoStateDto
            {
                FileName                  = "/data/cube.fits",
                MaskFileName              = "/data/cube-mask.fits",
                SelectedHdu               = 1,
                SubsetBounds              = new[] { 1, 100, 1, 100, 1, 50 },
                PrimarySpectralSystem     = "VRAD",
                AlternativeSpectralTarget = "FREQ",
                AlternativeSpectralUnit   = "Hz",
                StandardOfRest            = "Heliocentric",
                TransformedSpectralValue  = 1420.4,
            },
            Rendering = new RenderingStateDto
            {
                MaxSteps             = 256,
                ThresholdMin         = 0.1f,
                ThresholdMax         = 0.9f,
                ColorMap             = "Plasma",
                ScalingType          = "Sqrt",
                SelectionSaturateFactor = 0.7f,
                Scale                = new SerializableVector3(1f, 1f, 1f),
                Rotation             = new SerializableQuaternion(0f, 0f, 0f, 1f),
                Position             = new SerializableVector3(0f, 0f, 0f),
                Foveation            = new FoveationStateDto { FoveatedRendering = true },
                Mask                 = new MaskStateDto { MaskMode = "Enabled", DisplayMask = true },
                MomentMaps           = new MomentMapStateDto { ColorMapM0 = "Plasma", UseMask = true },
            },
            Interaction = new InteractionStateDto
            {
                ActiveInteractionMode = "IdlePainting",
                LocomotionState       = "Idle",
                BrushSize             = 3,
                PrimaryHand           = "LeftHand",
            },
            Features = new FeatureStateDto
            {
                FeatureSets = new List<FeatureSetDto>
                {
                    new()
                    {
                        FeatureSetType = "New",
                        SetName        = "MySet",
                        FeatureColor   = new SerializableColor(0f, 1f, 1f),
                        FeatureVisibility = true,
                        Features       = new List<FeatureDto>
                        {
                            new()
                            {
                                Id = 1, Name = "Source1", Flag = "0",
                                CornerMin = new SerializableVector3(10f, 10f, 5f),
                                CornerMax = new SerializableVector3(20f, 20f, 15f),
                                CubeColor = new SerializableColor(0f, 1f, 0f),
                            }
                        }
                    }
                }
            },
            Gui = new GuiStateDto { CubeDepthAxis = 2 },
        };
    }

    private string WriteAndReturn(WorkspaceSnapshot snap)
    {
        string json = _serializer.Serialize(snap);
        string path = Path.Combine(_tempDir, "snap.json");
        File.WriteAllText(path, json);
        return path;
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Test]
    public void RoundTrip_PreservesFileName()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        Assert.That(loaded.DataIo.FileName, Is.EqualTo(snap.DataIo.FileName));
    }

    [Test]
    public void RoundTrip_PreservesTransformedSpectralValue()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        Assert.That(loaded.DataIo.TransformedSpectralValue, Is.EqualTo(1420.4).Within(0.001));
    }

    [Test]
    public void RoundTrip_PreservesRenderingFields()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Rendering.MaxSteps,      Is.EqualTo(256));
            Assert.That(loaded.Rendering.ThresholdMin,  Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(loaded.Rendering.ThresholdMax,  Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(loaded.Rendering.ColorMap,      Is.EqualTo("Plasma"));
            Assert.That(loaded.Rendering.ScalingType,   Is.EqualTo("Sqrt"));
        });
    }

    [Test]
    public void RoundTrip_PreservesFeatureSets()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        Assert.That(loaded.Features, Is.Not.Null);
        Assert.That(loaded.Features!.FeatureSets, Has.Count.EqualTo(1));
        Assert.That(loaded.Features.FeatureSets[0].SetName, Is.EqualTo("MySet"));
    }

    [Test]
    public void RoundTrip_PreservesFeatureCorners()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        var feature = loaded.Features!.FeatureSets[0].Features[0];
        Assert.That(feature.CornerMin!.X, Is.EqualTo(10f).Within(0.001f));
        Assert.That(feature.CornerMax!.Z, Is.EqualTo(15f).Within(0.001f));
    }

    [Test]
    public void RoundTrip_PreservesInteraction()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        var loaded = _serializer.Deserialize(path);
        Assert.That(loaded.Interaction.ActiveInteractionMode, Is.EqualTo("IdlePainting"));
        Assert.That(loaded.Interaction.BrushSize,             Is.EqualTo(3));
        Assert.That(loaded.Interaction.PrimaryHand,           Is.EqualTo("LeftHand"));
    }

    // ── Checksum ──────────────────────────────────────────────────────────────

    [Test]
    public void Deserialize_ValidChecksum_Succeeds()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);
        Assert.DoesNotThrow(() => _serializer.Deserialize(path));
    }

    [Test]
    public void Deserialize_TamperedFile_ThrowsInvalidDataException()
    {
        var snap = MakeFullSnapshot();
        var path = WriteAndReturn(snap);

        // Tamper: append a character to corrupt the body without touching the checksum field
        string content = File.ReadAllText(path);
        // Inject a change somewhere in the body
        content = content.Replace("\"Plasma\"", "\"Turbo\"");
        File.WriteAllText(path, content);

        Assert.Throws<InvalidDataException>(() => _serializer.Deserialize(path));
    }

    [Test]
    public void Deserialize_TruncatedFile_ThrowsException()
    {
        var snap = MakeFullSnapshot();
        string json = _serializer.Serialize(snap);
        string path = Path.Combine(_tempDir, "truncated.json");
        // Write only first 200 characters
        File.WriteAllText(path, json[..Math.Min(200, json.Length)]);

        Assert.Catch<Exception>(() => _serializer.Deserialize(path));
    }

    [Test]
    public void Serialize_WritesSchemaVersionToOutput()
    {
        var snap = MakeFullSnapshot();
        string json = _serializer.Serialize(snap);
        // CamelCase serialiser emits "schemaVersion"; value is always present
        Assert.That(json, Does.Contain("schemaVersion").And.Contain("1.1.0"));
    }

    [Test]
    public void Serialize_WritesChecksumField()
    {
        var snap = MakeFullSnapshot();
        string json = _serializer.Serialize(snap);
        Assert.That(json, Does.Contain("sha256:"));
    }

    [Test]
    public void Serialize_NullSectionsAreOmitted()
    {
        var snap = new WorkspaceSnapshot
        {
            Metadata    = WorkspaceMetadata.Now(WorkspaceProfile.DataOnly),
            DataIo      = new DataIoStateDto { FileName = "/data/cube.fits", SelectedHdu = 1,
                SubsetBounds = new[] { 1,10,1,10,1,10 },
                PrimarySpectralSystem = "VRAD", AlternativeSpectralTarget = "FREQ",
                AlternativeSpectralUnit = "Hz", StandardOfRest = "Heliocentric" },
            Rendering   = new RenderingStateDto { ColorMap = "Inferno", ScalingType = "Linear" },
            Interaction = new InteractionStateDto(),
            Features    = null,
            Gui         = null,
        };
        string json = _serializer.Serialize(snap);
        // NullValueHandling.Ignore + CamelCase means absent null sections must not appear
        Assert.That(json, Does.Not.Contain("\"features\""));
        Assert.That(json, Does.Not.Contain("\"gui\""));
    }
}
