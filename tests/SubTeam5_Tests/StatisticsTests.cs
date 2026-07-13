using DataFeatures;
using FsCheck;
using FsCheck.NUnit;
using iDaVIE.Application.Feature;

namespace SubTeam5_Tests;

[TestFixture]
public class FeatureStatisticsTests
{
    private static readonly FeatureColor DefaultColor = new FeatureColor(1f, 1f, 1f);

    private static Feature MakeFeature(Vec3 min, Vec3 max) =>
        new Feature(min, max, DefaultColor, "test", "1", 0, 0, Array.Empty<string>(), true);

    // Property 1: centroid inside the bounding box.
    //
    // For any feature with valid bounds, placing the centroid at the exact geometric
    // centre of that feature always satisfies CentroidInsideBounds.
    //
    // Generator: x0 is an arbitrary int constrained to +/-999 via % so all values
    // are exactly representable as float. dx is a PositiveInt constrained to
    // [1, 500], so maxX > minX and the centre is always strictly inside.

    [FsCheck.NUnit.Property]
    public bool CentroidInsideBounds_HoldsForFeatureCenter(
        int x0, PositiveInt dx, int y0, PositiveInt dy, int z0, PositiveInt dz)
    {
        float minX = (float)(x0 % 1000);
        float maxX = minX + (float)(dx.Get % 500 + 1);
        float minY = (float)(y0 % 1000);
        float maxY = minY + (float)(dy.Get % 500 + 1);
        float minZ = (float)(z0 % 1000);
        float maxZ = minZ + (float)(dz.Get % 500 + 1);

        var feature = MakeFeature(new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ));
        var stats = new FeatureStatistics
        {
            CentroidX = (minX + maxX) / 2.0,
            CentroidY = (minY + maxY) / 2.0,
            CentroidZ = (minZ + maxZ) / 2.0
        };
        return stats.CentroidInsideBounds(feature);
    }

    // Boundary and exterior cases, mirroring Feature_ContainsPoint_ReturnsCorrectResult.
    // Two boundary rows verify >= / <= (not strict); six exterior rows verify each axis
    // independently so a missing axis check is caught immediately.
    [TestCase( 0.0,  0.0,  0.0, true,  "at CornerMin, included (tests >=)")]
    [TestCase(10.0, 10.0, 10.0, true,  "at CornerMax, included (tests <=)")]
    [TestCase( 5.0,  5.0,  5.0, true,  "interior")]
    [TestCase(-0.1,  5.0,  5.0, false, "below x_min")]
    [TestCase(10.1,  5.0,  5.0, false, "above x_max")]
    [TestCase( 5.0, -0.1,  5.0, false, "below y_min")]
    [TestCase( 5.0, 10.1,  5.0, false, "above y_max")]
    [TestCase( 5.0,  5.0, -0.1, false, "below z_min")]
    [TestCase( 5.0,  5.0, 10.1, false, "above z_max")]
    public void CentroidInsideBounds_BoundaryAndExteriorCases(
        double cx, double cy, double cz, bool expected, string _)
    {
        var feature = MakeFeature(new Vec3(0f, 0f, 0f), new Vec3(10f, 10f, 10f));
        var stats = new FeatureStatistics { CentroidX = cx, CentroidY = cy, CentroidZ = cz };
        Assert.That(stats.CentroidInsideBounds(feature), Is.EqualTo(expected));
    }

    // Property 2: flux is non-negative.
    //
    // For any non-negative TotalFlux and PeakFlux, FluxIsNonNegative() is true.
    // NonNegativeInt.Get >= 0; dividing by 100 gives sub-integer values while
    // keeping exact double representation.

    [FsCheck.NUnit.Property]
    public bool FluxIsNonNegative_HoldsForNonNegativeInputs(
        NonNegativeInt totalFluxCents, NonNegativeInt peakFluxCents)
    {
        var stats = new FeatureStatistics
        {
            TotalFlux = totalFluxCents.Get / 100.0,
            PeakFlux  = peakFluxCents.Get  / 100.0
        };
        return stats.FluxIsNonNegative();
    }

    [Test]
    public void FluxIsNonNegative_FalseWhenTotalFluxIsNegative()
    {
        var stats = new FeatureStatistics { TotalFlux = -0.01, PeakFlux = 1.0 };
        Assert.That(stats.FluxIsNonNegative(), Is.False);
    }

    [Test]
    public void FluxIsNonNegative_FalseWhenPeakFluxIsNegative()
    {
        var stats = new FeatureStatistics { TotalFlux = 1.0, PeakFlux = -0.01 };
        Assert.That(stats.FluxIsNonNegative(), Is.False);
    }

    // Property 3: W20 is at least W50.
    //
    // The line width at 20% of peak (W20) is always at least as wide as the line
    // width at 50% of peak (W50), because a lower threshold captures more of the
    // spectral line.
    // Generator: w50 is any non-negative value; w20 = w50 + non-negative delta,
    // so w20 >= w50 by construction.

    [FsCheck.NUnit.Property]
    public bool W20GeqW50_HoldsWhenW20AtLeastW50(
        NonNegativeInt w50Units, NonNegativeInt deltaUnits)
    {
        double w50 = w50Units.Get  / 10.0;
        double w20 = w50 + deltaUnits.Get / 10.0;
        var stats = new FeatureStatistics { W50 = w50, W20 = w20 };
        return stats.W20GeqW50();
    }

    [Test]
    public void W20GeqW50_FalseWhenW20SmallerThanW50()
    {
        var stats = new FeatureStatistics { W20 = 10.0, W50 = 20.0 };
        Assert.That(stats.W20GeqW50(), Is.False);
    }
}
