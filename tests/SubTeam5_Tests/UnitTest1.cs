using DataFeatures;

namespace SubTeam5_Tests;

[TestFixture]
public class FeatureTests
{
    private static readonly FeatureColor DefaultColor = new FeatureColor(1f, 1f, 1f);

    // TEST 1: Center returns the exact computed midpoint of the two corners.
    // Fails if the formula is wrong (e.g. using only one corner, integer division, etc.)
    [TestCase( 0f, 10f,  0f, 10f,  0f, 10f,   5f,   5f,   5f)]
    [TestCase( 2f,  8f,  4f, 12f,  6f, 18f,   5f,   8f,  12f)]
    [TestCase(-5f,  5f, -5f,  5f, -5f,  5f,   0f,   0f,   0f)]
    [TestCase( 1f,  1f,  1f,  1f,  1f,  1f,   1f,   1f,   1f)]
    [TestCase( 0f,  1f,  0f,  2f,  0f,  3f,  0.5f,  1f,  1.5f)]
    public void Feature_Center_IsExactMidpoint(
        float minX, float maxX,
        float minY, float maxY,
        float minZ, float maxZ,
        float expectedX, float expectedY, float expectedZ)
    {
        var feature = new Feature(
            new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ),
            DefaultColor, "test", "", 0, 1, Array.Empty<string>(), true);

        Assert.That(feature.Center.X, Is.EqualTo(expectedX));
        Assert.That(feature.Center.Y, Is.EqualTo(expectedY));
        Assert.That(feature.Center.Z, Is.EqualTo(expectedZ));
    }

    // TEST 2: Size pads each axis by one voxel (bounding box includes both min and max voxels).
    // Fails if the +1 padding is missing or applied to the wrong dimension.
    [TestCase( 0f,  9f,  0f,  9f,  0f,  9f,  10f, 10f, 10f)]
    [TestCase( 5f, 14f,  5f, 14f,  5f, 14f,  10f, 10f, 10f)]
    [TestCase( 0f,  0f,  0f,  0f,  0f,  0f,   1f,  1f,  1f)]
    [TestCase( 0f,  4f,  0f,  9f,  0f, 19f,   5f, 10f, 20f)]
    [TestCase(-5f,  4f, -5f,  4f, -5f,  4f,  10f, 10f, 10f)]
    public void Feature_Size_IsPaddedByOneVoxel(
        float minX, float maxX,
        float minY, float maxY,
        float minZ, float maxZ,
        float expectedX, float expectedY, float expectedZ)
    {
        var feature = new Feature(
            new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ),
            DefaultColor, "test", "", 0, 1, Array.Empty<string>(), true);

        Assert.That(feature.Size.X, Is.EqualTo(expectedX));
        Assert.That(feature.Size.Y, Is.EqualTo(expectedY));
        Assert.That(feature.Size.Z, Is.EqualTo(expectedZ));
    }

    // TEST 3: ContainsPoint correctly classifies interior, boundary, and exterior points.
    // Fails if the comparison uses < instead of <= (excludes boundary) or has an axis wrong.
    [TestCase( 5f,    5f,   5f, true)]   // interior
    [TestCase( 0f,    0f,   0f, true)]   // min corner, included
    [TestCase(10f,   10f,  10f, true)]   // max corner, included
    [TestCase(-0.1f,  5f,   5f, false)]  // just outside x_min
    [TestCase(10.1f,  5f,   5f, false)]  // just outside x_max
    public void Feature_ContainsPoint_ReturnsCorrectResult(
        float px, float py, float pz, bool expected)
    {
        var feature = new Feature(
            new Vec3(0f, 0f, 0f), new Vec3(10f, 10f, 10f),
            DefaultColor, "test", "", 0, 1, Array.Empty<string>(), true);

        Assert.That(feature.ContainsPoint(new Vec3(px, py, pz)), Is.EqualTo(expected));
    }
}
