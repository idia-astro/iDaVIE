using DataFeatures;
using iDaVIE.Domain.Feature;
using iDaVIE.Infrastructure.Persistence;
using System.Xml.Linq;

namespace SubTeam5_Tests;

/// <summary>
/// End-to-end scenario: mask, masked features, an edited feature, then an exported
/// VOTable.
///
/// Every assertion targets real domain logic: computed properties (Center, Size),
/// containment behaviour that changes after SetBounds, parent wiring, and the
/// dirty-event chain. There are no trivial round-trips (set a field, read it back).
///
/// The real <see cref="VoTableExportService"/> is used throughout, with no in-test
/// stubs. <see cref="IdentityTransformer"/> replaces the native AstTool DLL with a
/// pass-through so coordinate assertions stay deterministic.
/// </summary>
[TestFixture]
public class MaskEditExportScenarioTests
{
    private static readonly FeatureColor Cyan = new FeatureColor(0f, 1f, 1f);

    [Test]
    public void Scenario_MaskFeaturesEditedAndExported_VoTableReflectsEdits()
    {
        // 1. Mask
        var maskSet = new FeatureSet("HI mask run 1", 0, FeatureSetType.Mask, Cyan);
        maskSet.RawDataKeys  = new[] { "SUM", "PEAK" };
        maskSet.RawDataTypes = new[] { "float", "float" };
        maskSet.ZAxisLabel   = "v_rad";

        Assert.That(maskSet.SetType, Is.EqualTo(FeatureSetType.Mask));

        // 2. Masked features
        var f0 = new Feature(
            new Vec3( 10f,  20f,  5f), new Vec3( 30f,  40f, 15f),
            Cyan, "source_001", "1", 0, 0, new[] { "123.4",  "56.7" }, true);

        var f1 = new Feature(
            new Vec3( 50f,  60f, 10f), new Vec3( 80f,  90f, 30f),
            Cyan, "source_002", "1", 1, 1, new[] { "456.7",  "89.0" }, true);

        var f2 = new Feature(
            new Vec3(100f, 110f, 20f), new Vec3(120f, 130f, 40f),
            Cyan, "source_003", "1", 2, 2, new[] { "789.1", "120.3" }, true);

        maskSet.Add(f0);
        maskSet.Add(f1);
        maskSet.Add(f2);

        Assert.That(maskSet.Features.Count, Is.EqualTo(3));

        // Add must wire the back-reference; that's domain logic, not just a list insert.
        Assert.That(f1.FeatureSetParent, Is.SameAs(maskSet),
            "Add must set FeatureSetParent so dirty events propagate");

        // Center and Size are computed properties; verify them from the initial corners.
        // f1 bounds [50..80, 60..90, 10..30]:
        //   Center = ((50+80)/2, (60+90)/2, (10+30)/2) = (65, 75, 20)
        //   Size   = (80-50+1, 90-60+1, 30-10+1) = (31, 31, 21)
        Assert.That(f1.Center.X, Is.EqualTo(65f), "initial center x");
        Assert.That(f1.Center.Y, Is.EqualTo(75f), "initial center y");
        Assert.That(f1.Center.Z, Is.EqualTo(20f), "initial center z");
        Assert.That(f1.Size.X,   Is.EqualTo(31f), "initial size x (80-50+1)");
        Assert.That(f1.Size.Y,   Is.EqualTo(31f), "initial size y (90-60+1)");
        Assert.That(f1.Size.Z,   Is.EqualTo(21f), "initial size z (30-10+1)");

        // A point just inside f1's x_min=50 must be contained (ContainsPoint uses >=).
        var probePoint = new Vec3(51f, 75f, 20f);
        Assert.That(f1.ContainsPoint(probePoint), Is.True,
            "probe (51,75,20) is inside initial bounds [50..80, 60..90, 10..30]");

        // 3. Edited feature
        var dirtyIndices = new List<int>();
        maskSet.FeatureDirty += idx => dirtyIndices.Add(idx);

        f1.Flag    = "3";
        f1.SetBounds(new Vec3(52f, 62f, 11f), new Vec3(78f, 88f, 29f));
        f1.RawData = new[] { "470.2", "92.1" };

        // The probe point (51,75,20) is now outside because x_min rose from 50 to 52.
        // This proves SetBounds changed the domain behaviour of ContainsPoint, not just
        // stored new values.
        Assert.That(f1.ContainsPoint(probePoint), Is.False,
            "probe (51,75,20) must be outside after x_min is raised to 52");

        // Center re-computed from new corners: ((52+78)/2, (62+88)/2, (11+29)/2) = (65, 75, 20).
        // The shrink is symmetric, so the center is preserved; a wrong formula would differ.
        Assert.That(f1.Center.X, Is.EqualTo(65f), "center x preserved by symmetric shrink");
        Assert.That(f1.Center.Y, Is.EqualTo(75f), "center y preserved");
        Assert.That(f1.Center.Z, Is.EqualTo(20f), "center z preserved");

        // Size re-computed: (78-52+1, 88-62+1, 29-11+1) = (27, 27, 19).
        // Fails if SetBounds didn't update _corners or Size formula dropped the +1 pad.
        Assert.That(f1.Size.X, Is.EqualTo(27f), "size x shrank from 31 to 27");
        Assert.That(f1.Size.Y, Is.EqualTo(27f), "size y shrank from 31 to 27");
        Assert.That(f1.Size.Z, Is.EqualTo(19f), "size z shrank from 21 to 19");

        // SetBounds calls NotifyDirty, which calls FeatureSet.NotifyDirty, which
        // raises the FeatureDirty event.
        Assert.That(dirtyIndices, Has.Member(f1.Index),
            "SetBounds must propagate a dirty notification through to the parent set");

        // Neighbouring features must be unaffected: editing one feature must not
        // corrupt the domain state of others in the same set.
        Assert.That(f0.FeatureSetParent, Is.SameAs(maskSet), "f0 parent still wired");
        Assert.That(f2.Center.X, Is.EqualTo(110f), "f2 center x untouched");
        Assert.That(f2.Center.Y, Is.EqualTo(120f), "f2 center y untouched");
        Assert.That(f2.Center.Z, Is.EqualTo( 30f), "f2 center z untouched");
        Assert.That(f2.Size.X,   Is.EqualTo( 21f), "f2 size x untouched (120-100+1)");

        // 4. Exported VOTable
        // VoTableExportService reads domain properties (Center, CornerMin, CornerMax,
        // Flag, RawData) to build each row. The assertions below verify those reads
        // produce the values that the domain computed above.
        IVoTableExporter exporter = new VoTableExportService();
        string xml = exporter.Export(maskSet, new IdentityTransformer());

        var rows = XDocument.Parse(xml).Descendants("TR").ToList();
        Assert.That(rows.Count, Is.EqualTo(3), "one TR per feature");

        var r0 = rows[0].Elements("TD").ToList();
        var r1 = rows[1].Elements("TD").ToList();
        var r2 = rows[2].Elements("TD").ToList();

        Assert.That(r0.Count, Is.EqualTo(16), "f0: 14 fixed + 2 RawData columns");
        Assert.That(r1.Count, Is.EqualTo(16), "f1: 14 fixed + 2 RawData columns");

        // Column layout: id x y z x_min x_max y_min y_max z_min z_max ra dec v_rad Flag SUM PEAK
        // f0 should still show the original, unedited values.
        Assert.That(r0[Col.Flag].Value, Is.EqualTo("1"),     "f0 flag");
        Assert.That(r0[Col.Sum].Value,  Is.EqualTo("123.4"), "f0 SUM");
        Assert.That(r0[Col.Peak].Value, Is.EqualTo("56.7"),  "f0 PEAK");

        // f1: the exporter must read the domain-computed Center and CornerMin/CornerMax.
        // These columns come from feature.Center.X and feature.CornerMin.X respectively,
        // so a wrong Center or SetBounds implementation would surface here.
        Assert.That(r1[Col.X].Value,    Is.EqualTo("65"),    "f1 computed center x");
        Assert.That(r1[Col.Y].Value,    Is.EqualTo("75"),    "f1 computed center y");
        Assert.That(r1[Col.Z].Value,    Is.EqualTo("20"),    "f1 computed center z");
        Assert.That(r1[Col.XMin].Value, Is.EqualTo("52"),    "f1 CornerMin.X after SetBounds");
        Assert.That(r1[Col.XMax].Value, Is.EqualTo("78"),    "f1 CornerMax.X after SetBounds");
        Assert.That(r1[Col.Flag].Value, Is.EqualTo("3"),     "f1 edited flag");
        Assert.That(r1[Col.Sum].Value,  Is.EqualTo("470.2"), "f1 updated SUM");
        Assert.That(r1[Col.Peak].Value, Is.EqualTo("92.1"),  "f1 updated PEAK");

        // f2 should be unchanged.
        Assert.That(r2[Col.Flag].Value, Is.EqualTo("1"),     "f2 flag");
        Assert.That(r2[Col.Sum].Value,  Is.EqualTo("789.1"), "f2 SUM");
        Assert.That(r2[Col.Peak].Value, Is.EqualTo("120.3"), "f2 PEAK");
    }

    // Column index constants
    private static class Col
    {
        public const int Id    = 0;
        public const int X     = 1;
        public const int Y     = 2;
        public const int Z     = 3;
        public const int XMin  = 4;
        public const int XMax  = 5;
        public const int YMin  = 6;
        public const int YMax  = 7;
        public const int ZMin  = 8;
        public const int ZMax  = 9;
        public const int Ra    = 10;
        public const int Dec   = 11;
        public const int ZPhys = 12;
        public const int Flag  = 13;
        public const int Sum   = 14;
        public const int Peak  = 15;
    }

    // Test double

    /// <summary>
    /// Returns pixel coordinates unchanged as world coordinates, so the
    /// ra/dec/z_phys columns in the XML equal the pixel center values.
    /// No native DLL needed, and no floating-point uncertainty in the assertions.
    /// </summary>
    private sealed class IdentityTransformer : ICoordinateTransformer
    {
        public void Transform(IAstFrame frame,
            double x, double y, double z,
            out double ra, out double dec, out double zPhys)
        {
            ra = x; dec = y; zPhys = z;
        }

        public void Normalise(IAstFrame frame,
            double ra, double dec, double zPhys,
            out double normRa, out double normDec, out double normZ)
        {
            normRa = ra; normDec = dec; normZ = zPhys;
        }
    }
}
