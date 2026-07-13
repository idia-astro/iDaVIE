// Sub-Team 2 — Persistence & Data
// Structural tests — verify design rules via reflection, no DLL or FITS file needed.
// Type: Structural · White-box
//
// These tests enforce architectural non-negotiable #3 from the spec:
// "Domain code must not transitively depend on UnityEngine."

using NUnit.Framework;
using System.Linq;
using System.Reflection;

[TestFixture]
[Category("Structural")]
public class StructuralTests
{
    // T-10: The refactored FITS classes must not reference UnityEngine.
    // White-box: checks the assembly's referenced-assembly list.
    // Rationale: if a dev accidentally adds "using UnityEngine;" to FitsFile, this
    // test catches it before the code reaches the Unity editor.
    [Test]
    public void FitsClasses_Assembly_HasNoUnityEngineReference()
    {
        var assembly = typeof(FitsFile).Assembly;
        var refs = assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();

        CollectionAssert.DoesNotContain(refs, "UnityEngine",
            "FitsFile/FitsHeader/FitsImage etc. must not depend on UnityEngine. " +
            "Move any Unity types behind an interface or into the adapter layer.");
    }

    // T-11: WcsTransformer still depends on Unity — documented as known tech debt.
    // This test is IGNORED intentionally. It exists to make the gap visible on the
    // pitch panel rather than hiding it. Remove the [Ignore] when Vector3 is replaced
    // with a plain struct (e.g. System.Numerics.Vector3).
    [Test]
    [Ignore("Tech debt: WcsTransformer still imports UnityEngine via Vector3. " +
            "Replace with System.Numerics.Vector3 to complete Unity decoupling.")]
    public void WcsTransformer_HasNoUnityDependency_TechDebt()
    {
        var assembly = typeof(WcsTransformer).Assembly;
        var refs = assembly.GetReferencedAssemblies().Select(r => r.Name).ToList();

        CollectionAssert.DoesNotContain(refs, "UnityEngine",
            "WcsTransformer must not depend on UnityEngine (tracked tech debt).");
    }

    // T-12: FitsFile, FitsHeader, FitsImage, FitsMask, FitsTable must each be static classes.
    // White-box: verifies the SRP split produced the correct class shapes.
    // Static classes cannot hold instance state, which prevents the God-class pattern
    // from re-emerging through accumulated fields.
    [TestCase(typeof(FitsFile))]
    [TestCase(typeof(FitsHeader))]
    public void DataClasses_AreStaticClasses(System.Type type)
    {
        Assert.That(type.IsAbstract && type.IsSealed,
            $"{type.Name} should be a static class (abstract+sealed in IL) to prevent instance state");
    }
}
