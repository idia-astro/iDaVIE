using iDaVIE.Persistence.Application;
using iDaVIE.Persistence.Domain;
using iDaVIE.Persistence.Domain.Dtos;
using iDaVIE.Persistence.Domain.Serialization;
using NUnit.Framework;

namespace PersistenceTests;

[TestFixture]
public class WorkspaceValidatorTests
{
    private WorkspaceValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new WorkspaceValidator();

    private static WorkspaceSnapshot ValidSnapshot(Action<WorkspaceSnapshot>? mutate = null)
    {
        var s = new WorkspaceSnapshot
        {
            Metadata = WorkspaceMetadata.Now(WorkspaceProfile.DataOnly),
            DataIo = new DataIoStateDto
            {
                FileName             = "/data/cube.fits",
                SubsetBounds         = new[] { 1, 100, 1, 100, 1, 50 },
                SelectedHdu          = 1,
                PrimarySpectralSystem = "VRAD",
                AlternativeSpectralTarget = "FREQ",
                AlternativeSpectralUnit   = "Hz",
                StandardOfRest       = "Heliocentric",
            },
            Rendering   = new RenderingStateDto { ColorMap = "Inferno", ScalingType = "Linear" },
            Interaction = new InteractionStateDto(),
        };
        mutate?.Invoke(s);
        return s;
    }

    // ── DataIo ───────────────────────────────────────────────────────────────

    [Test]
    public void Validate_MissingFileName_SetsDatasetUnavailable()
    {
        var snap = ValidSnapshot(s => s.DataIo.FileName = null);
        var result = _validator.Validate(snap);
        Assert.That(result.DatasetUnavailable, Is.True);
    }

    [Test]
    public void Validate_EmptyFileName_SetsDatasetUnavailable()
    {
        var snap = ValidSnapshot(s => s.DataIo.FileName = "");
        var result = _validator.Validate(snap);
        Assert.That(result.DatasetUnavailable, Is.True);
    }

    [Test]
    public void Validate_InvalidHdu_ResetsToOne()
    {
        var snap = ValidSnapshot(s => s.DataIo.SelectedHdu = 0);
        _validator.Validate(snap);
        Assert.That(snap.DataIo.SelectedHdu, Is.EqualTo(1));
    }

    [Test]
    public void Validate_NegativeHdu_ResetsToOne()
    {
        var snap = ValidSnapshot(s => s.DataIo.SelectedHdu = -5);
        _validator.Validate(snap);
        Assert.That(snap.DataIo.SelectedHdu, Is.EqualTo(1));
    }

    [Test]
    public void Validate_MissingStandardOfRest_DefaultsToHeliocentric()
    {
        var snap = ValidSnapshot(s => s.DataIo.StandardOfRest = null);
        _validator.Validate(snap);
        Assert.That(snap.DataIo.StandardOfRest, Is.EqualTo("Heliocentric"));
    }

    [Test]
    public void Validate_InvalidIndex2_ClearsIt()
    {
        var snap = ValidSnapshot(s => s.DataIo.Index2 = 5);
        _validator.Validate(snap);
        Assert.That(snap.DataIo.Index2, Is.Null);
    }

    [Test]
    public void Validate_ValidIndex2_Preserved()
    {
        var snap = ValidSnapshot(s => s.DataIo.Index2 = 2);
        _validator.Validate(snap);
        Assert.That(snap.DataIo.Index2, Is.EqualTo(2));
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    [Test]
    public void Validate_InvalidColorMap_DefaultsToInferno()
    {
        var snap = ValidSnapshot(s => s.Rendering.ColorMap = "NotAColor");
        _validator.Validate(snap);
        Assert.That(snap.Rendering.ColorMap, Is.EqualTo("Inferno"));
    }

    [Test]
    public void Validate_NullColorMap_DefaultsToInferno()
    {
        var snap = ValidSnapshot(s => s.Rendering.ColorMap = null);
        _validator.Validate(snap);
        Assert.That(snap.Rendering.ColorMap, Is.EqualTo("Inferno"));
    }

    [Test]
    public void Validate_InvalidScalingType_DefaultsToLinear()
    {
        var snap = ValidSnapshot(s => s.Rendering.ScalingType = "Bogus");
        _validator.Validate(snap);
        Assert.That(snap.Rendering.ScalingType, Is.EqualTo("Linear"));
    }

    [Test]
    public void Validate_ThresholdMinGreaterThanMax_ResetsToZeroOne()
    {
        var snap = ValidSnapshot(s => { s.Rendering.ThresholdMin = 0.8f; s.Rendering.ThresholdMax = 0.2f; });
        _validator.Validate(snap);
        Assert.That(snap.Rendering.ThresholdMin, Is.EqualTo(0f));
        Assert.That(snap.Rendering.ThresholdMax, Is.EqualTo(1f));
    }

    [Test]
    public void Validate_MaxStepsTooLow_ClampsToSixteen()
    {
        var snap = ValidSnapshot(s => s.Rendering.MaxSteps = 8);
        _validator.Validate(snap);
        Assert.That(snap.Rendering.MaxSteps, Is.EqualTo(16));
    }

    [Test]
    public void Validate_MaxStepsTooHigh_ClampsToFiveHundredTwelve()
    {
        var snap = ValidSnapshot(s => s.Rendering.MaxSteps = 1024);
        _validator.Validate(snap);
        Assert.That(snap.Rendering.MaxSteps, Is.EqualTo(512));
    }

    [Test]
    public void Validate_ScaleWithNonPositiveComponent_ResetsToOne()
    {
        var snap = ValidSnapshot(s => s.Rendering.Scale = new SerializableVector3(1f, 0f, 1f));
        _validator.Validate(snap);
        Assert.That(snap.Rendering.Scale!.X, Is.EqualTo(1f));
        Assert.That(snap.Rendering.Scale!.Y, Is.EqualTo(1f));
        Assert.That(snap.Rendering.Scale!.Z, Is.EqualTo(1f));
    }

    [Test]
    public void Validate_UnnormalisedRotation_GetsNormalised()
    {
        var snap = ValidSnapshot(s => s.Rendering.Rotation = new SerializableQuaternion(2f, 0f, 0f, 0f));
        _validator.Validate(snap);
        var rot = snap.Rendering.Rotation!;
        float mag = MathF.Sqrt(rot.X * rot.X + rot.Y * rot.Y + rot.Z * rot.Z + rot.W * rot.W);
        Assert.That(mag, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Validate_MissingFoveation_InsertsDisabledDefault()
    {
        var snap = ValidSnapshot(s => s.Rendering.Foveation = null);
        _validator.Validate(snap);
        Assert.That(snap.Rendering.Foveation, Is.Not.Null);
        Assert.That(snap.Rendering.Foveation!.FoveatedRendering, Is.False);
    }

    [Test]
    public void Validate_MissingMask_InsertsDisabledDefault()
    {
        var snap = ValidSnapshot(s => s.Rendering.Mask = null);
        _validator.Validate(snap);
        Assert.That(snap.Rendering.Mask, Is.Not.Null);
        Assert.That(snap.Rendering.Mask!.MaskMode, Is.EqualTo("Disabled"));
    }

    // ── Interaction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Core policy: VideoCamPosRecording must ALWAYS be reset to IdleSelecting.
    /// Recording mode is a transient VR session state and must never survive across sessions.
    /// </summary>
    [Test]
    public void Validate_VideoCamPosRecording_ResetsToIdleSelecting()
    {
        var snap = ValidSnapshot(s => s.Interaction.ActiveInteractionMode = "VideoCamPosRecording");
        _validator.Validate(snap);
        Assert.That(snap.Interaction.ActiveInteractionMode, Is.EqualTo("IdleSelecting"));
    }

    [Test]
    public void Validate_VideoCamPosRecording_ProducesWarning()
    {
        var snap = ValidSnapshot(s => s.Interaction.ActiveInteractionMode = "VideoCamPosRecording");
        var result = _validator.Validate(snap);
        Assert.That(result.HasWarnings, Is.True);
        Assert.That(result.Warnings, Has.Some.Contains("VideoCamPosRecording"));
    }

    [Test]
    public void Validate_InvalidInteractionMode_DefaultsToIdleSelecting()
    {
        var snap = ValidSnapshot(s => s.Interaction.ActiveInteractionMode = "GibberishMode");
        _validator.Validate(snap);
        Assert.That(snap.Interaction.ActiveInteractionMode, Is.EqualTo("IdleSelecting"));
    }

    [Test]
    public void Validate_ValidInteractionModes_Preserved(
        [Values("IdleSelecting", "IdlePainting", "Editing", "Painting")] string mode)
    {
        var snap = ValidSnapshot(s => s.Interaction.ActiveInteractionMode = mode);
        _validator.Validate(snap);
        Assert.That(snap.Interaction.ActiveInteractionMode, Is.EqualTo(mode));
    }

    [Test]
    public void Validate_InvalidLocomotionState_DefaultsToIdle()
    {
        var snap = ValidSnapshot(s => s.Interaction.LocomotionState = "Flying");
        _validator.Validate(snap);
        Assert.That(snap.Interaction.LocomotionState, Is.EqualTo("Idle"));
    }

    [Test]
    public void Validate_BrushSizeBelowOne_ClampsToOne()
    {
        var snap = ValidSnapshot(s => s.Interaction.BrushSize = 0);
        _validator.Validate(snap);
        Assert.That(snap.Interaction.BrushSize, Is.EqualTo(1));
    }

    [Test]
    public void Validate_SourceIdBelowMinusOne_ResetsToMinusOne()
    {
        var snap = ValidSnapshot(s => s.Interaction.SourceId = -5);
        _validator.Validate(snap);
        Assert.That(snap.Interaction.SourceId, Is.EqualTo(-1));
    }

    [Test]
    public void Validate_VignetteFadeSpeedOutOfRange_Clamped()
    {
        var snap = ValidSnapshot(s => s.Interaction.VignetteFadeSpeed = 10f);
        _validator.Validate(snap);
        Assert.That(snap.Interaction.VignetteFadeSpeed, Is.EqualTo(5.0f));
    }

    [Test]
    public void Validate_InvalidPrimaryHand_DefaultsToRightHand()
    {
        var snap = ValidSnapshot(s => s.Interaction.PrimaryHand = "BothHands");
        _validator.Validate(snap);
        Assert.That(snap.Interaction.PrimaryHand, Is.EqualTo("RightHand"));
    }

    [Test]
    public void Validate_NegativeRotationAxisCutoff_DefaultsToFive()
    {
        var snap = ValidSnapshot(s => s.Interaction.RotationAxisCutoff = -1f);
        _validator.Validate(snap);
        Assert.That(snap.Interaction.RotationAxisCutoff, Is.EqualTo(5f));
    }

    // ── Feature validation ────────────────────────────────────────────────────

    [Test]
    public void Validate_FeatureWithOnlyCornerMin_ExcludesFeature()
    {
        var snap = ValidSnapshot();
        snap.Features = new FeatureStateDto();
        var set = new FeatureSetDto
        {
            FeatureSetType = "New",
            SetName = "TestSet",
            Features = new List<FeatureDto>
            {
                new() { Id = 1, Name = "F1", CornerMin = new SerializableVector3(0,0,0), CornerMax = null },
                new() { Id = 2, Name = "F2", CornerMin = new SerializableVector3(0,0,0), CornerMax = new SerializableVector3(1,1,1) },
            }
        };
        snap.Features.FeatureSets.Add(set);

        var result = _validator.Validate(snap);
        Assert.That(result.ExcludedFeatureIds, Contains.Item(1));
        Assert.That(set.Features.Select(f => f.Id), Does.Not.Contain(1));
        Assert.That(set.Features.Select(f => f.Id), Contains.Item(2));
    }

    [Test]
    public void Validate_TemporaryFeature_WithoutCorners_IsNotExcluded()
    {
        var snapshot = ValidSnapshot();
        snapshot.Features = new FeatureStateDto
        {
            FeatureSets = new List<FeatureSetDto>
            {
                new FeatureSetDto
                {
                    FeatureSetType = "New",
                    Features = new List<FeatureDto>
                    {
                        new FeatureDto { Id = 1, IsTemporary = true, CornerMin = null, CornerMax = null }
                    }
                }
            }
        };

        var result = _validator.Validate(snapshot);

        Assert.That(result.ExcludedFeatureIds, Does.Not.Contain(1));
    }

    // ── GUI ──────────────────────────────────────────────────────────────────

    [Test]
    public void Validate_CubeDepthAxisOutOfRange_DefaultsToTwo()
    {
        var snap = ValidSnapshot();
        snap.Gui = new GuiStateDto { CubeDepthAxis = 5 };
        _validator.Validate(snap);
        Assert.That(snap.Gui.CubeDepthAxis, Is.EqualTo(2));
    }

    [Test]
    public void Validate_ValidSnapshot_HasNoWarnings()
    {
        var snap = ValidSnapshot();
        snap.Rendering.Scale    = new SerializableVector3(1f, 1f, 1f);
        snap.Rendering.Rotation = new SerializableQuaternion(0f, 0f, 0f, 1f);
        var result = _validator.Validate(snap);
        Assert.That(result.DatasetUnavailable, Is.False);
        Assert.That(result.Warnings, Is.Empty);
    }
}
