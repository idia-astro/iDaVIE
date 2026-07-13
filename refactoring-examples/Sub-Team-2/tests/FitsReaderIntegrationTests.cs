// Sub-Team 2 — Persistence & Data
// Integration tests — call the real idavie_fits.dll against the sample FITS file.
// Type: Integration · Black-box
//
// Prerequisites:
//   - idavie_fits.dll on PATH (or set env var IDAVIE_FITS_DLL_PATH)
//   - Data/SampleData/test_volume.fits relative to repo root
//
// Run with: dotnet test  (full suite)
//           dotnet test --filter "Category!=Integration"  (skip these)

using NUnit.Framework;
using System;
using System.IO;

[TestFixture]
[Category("Integration")]
public class FitsReaderIntegrationTests
{
    private string _fitsPath;

    [OneTimeSetUp]
    public void FindTestFile()
    {
        // Walk up from the test binary to find the repo root, then Data/SampleData/
        string dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Data", "SampleData", "test_volume.fits")))
            dir = Directory.GetParent(dir)?.FullName;

        if (dir == null)
            Assert.Ignore("test_volume.fits not found — skipping integration tests");

        _fitsPath = Path.Combine(dir, "Data", "SampleData", "test_volume.fits");
    }

    // T-06: Opening an existing FITS file in read-only mode must return status 0.
    // Black-box: we only care that a valid path → status 0 and a non-null pointer.
    [Test]
    public void FitsFile_OpenReadOnly_StatusIsZero()
    {
        int status;
        IntPtr fptr;

        FitsFile.FitsOpenFile(out fptr, _fitsPath, out status, isReadOnly: true);

        Assert.That(status, Is.EqualTo(0), $"Expected status 0, got {FitsHeader.ErrorCodes.GetValueOrDefault(status, "unknown")}");
        Assert.That(fptr, Is.Not.EqualTo(IntPtr.Zero));

        FitsFile.FitsCloseFile(fptr, out status);
    }

    // T-07: The FITS header for the sample volume must contain BITPIX, NAXIS, NAXIS1.
    // Black-box: we only know that any valid FITS image has these mandatory keywords.
    [Test]
    public void FitsHeader_ExtractHeaders_ContainsMandatoryKeys()
    {
        int status;
        IntPtr fptr;
        FitsFile.FitsOpenFile(out fptr, _fitsPath, out status, isReadOnly: true);
        Assume.That(status, Is.EqualTo(0), "Could not open FITS file — skipping");

        var headers = FitsHeader.ExtractHeaders(fptr, out status);

        FitsFile.FitsCloseFile(fptr, out _);

        Assert.That(headers, Contains.Key("BITPIX"), "BITPIX must be present in any FITS image HDU");
        Assert.That(headers, Contains.Key("NAXIS"),  "NAXIS must be present");
        Assert.That(headers, Contains.Key("NAXIS1"), "NAXIS1 must be present for a 3-D volume");
    }

    // T-08: The sample volume is 3-D — FitsGetImageDims must return 3.
    // Black-box: the dimension count drives slice geometry in the render pipeline.
    [Test]
    public void FitsImage_GetDimensions_Returns3ForVolume()
    {
        int status;
        IntPtr fptr;
        FitsFile.FitsOpenFile(out fptr, _fitsPath, out status, isReadOnly: true);
        Assume.That(status, Is.EqualTo(0), "Could not open FITS file — skipping");

        FitsFile.FitsGetImageDims(fptr, out int dims, out status);

        FitsFile.FitsCloseFile(fptr, out _);

        Assert.That(status, Is.EqualTo(0));
        Assert.That(dims, Is.EqualTo(3), "test_volume.fits is a 3-D data cube");
    }

    // T-09: Open and close the file 50 times — no handle leaks allowed.
    // Black-box: if handles leak, CFITSIO returns error 103 ("too many open files").
    // This pins the fix for the conditional-close bug in the original FitsReader.
    [Test]
    public void FitsFile_RepeatedOpenClose_NoHandleLeak()
    {
        for (int i = 0; i < 50; i++)
        {
            int status;
            IntPtr fptr;
            FitsFile.FitsOpenFile(out fptr, _fitsPath, out status, isReadOnly: true);
            Assert.That(status, Is.EqualTo(0), $"Iteration {i}: open failed with status {status}");
            FitsFile.FitsCloseFile(fptr, out status);
            Assert.That(status, Is.EqualTo(0), $"Iteration {i}: close failed with status {status}");
        }
    }
}
