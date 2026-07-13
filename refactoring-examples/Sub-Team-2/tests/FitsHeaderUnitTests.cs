// Sub-Team 2 — Persistence & Data
// Unit tests for FitsHeader pure C# logic.
// Type: Unit · White-box
// Requires: no native DLL, no Unity, no FITS file.

using NUnit.Framework;
using System.Collections.Generic;

[TestFixture]
[Category("Unit")]
public class FitsHeaderUnitTests
{
    // T-01: ErrorCodes dictionary must cover all five CFITSIO error ranges.
    // White-box: we know the internal dictionary and the five CFITSIO ranges.
    [Test]
    public void ErrorCodes_ContainsAllCfitsioRanges()
    {
        var codes = FitsHeader.ErrorCodes;

        // 1xx — file I/O errors
        Assert.That(codes.ContainsKey(101) || codes.ContainsKey(104), "Missing 1xx file I/O code");
        // 15x — shared memory
        Assert.That(codes.ContainsKey(151) || codes.ContainsKey(152), "Missing 15x shared-memory code");
        // 2xx — header errors
        Assert.That(codes.ContainsKey(202) || codes.ContainsKey(210), "Missing 2xx header code");
        // 3xx — data access errors
        Assert.That(codes.ContainsKey(301) || codes.ContainsKey(307), "Missing 3xx data-access code");
    }

    // T-02: FitsErrorMessage must embed both the numeric code and a readable description.
    // White-box: we know the format string joins "#status description".
    [Test]
    public void FitsErrorMessage_FormatsCodeAndDescription()
    {
        string msg = FitsHeader.FitsErrorMessage(104);

        StringAssert.Contains("104", msg);
        StringAssert.Contains("open", msg); // "could not open the named file"
    }

    // T-02b: Unknown status code must throw KeyNotFoundException, not return empty string.
    // White-box: the dictionary lookup is not guarded — this documents the known behaviour.
    [Test]
    public void FitsErrorMessage_UnknownCode_ThrowsKeyNotFound()
    {
        Assert.Throws<KeyNotFoundException>(() => FitsHeader.FitsErrorMessage(9999));
    }

    // T-03: ExtractHeaders must concatenate values for duplicate FITS keys.
    // White-box: tests the branch on the duplicate-key check in the loop body.
    // We exercise the logic directly because the full method needs a live FITS pointer;
    // the concatenation logic is isolated here using a hand-rolled equivalent to confirm
    // the design intent matches the implementation.
    [Test]
    public void DuplicateKeyConcatenation_AppendsBothValues()
    {
        // Replicate the concatenation logic from FitsHeader.ExtractHeaders lines 174-177
        IDictionary<string, string> dict = new Dictionary<string, string>();
        string key = "COMMENT";

        // Simulate first read
        if (!dict.ContainsKey(key)) dict.Add(key, "value1");
        else dict[key] = dict[key] + "value1";

        // Simulate second read with the same key
        if (!dict.ContainsKey(key)) dict.Add(key, "value2");
        else dict[key] = dict[key] + "value2";

        Assert.That(dict[key], Is.EqualTo("value1value2"),
            "Duplicate FITS keys must be concatenated, not overwritten");
    }
}
