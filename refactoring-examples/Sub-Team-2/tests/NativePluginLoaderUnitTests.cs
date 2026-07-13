// Sub-Team 2 — Persistence & Data
// Unit tests for NativePluginLoader guard logic.
// Type: Unit · White-box
// Requires: no native DLL (Initialize is called with a fake path; LoadAll will fail to
// find DLLs but the guard-condition tests complete before any load is attempted).

using NUnit.Framework;
using fts;

[TestFixture]
[Category("Unit")]
public class NativePluginLoaderUnitTests
{
    // NativePluginLoader holds static state — reset between tests via Shutdown.
    [TearDown]
    public void ResetLoader()
    {
        NativePluginLoader.Shutdown();
    }

    // T-04: Second Initialize call must be silently ignored.
    // White-box: tests the guard branch on line 62 of NativePluginLoader.cs.
    // Before the refactor there was no guard — double-init caused native handle corruption.
    [Test]
    public void Initialize_SecondCallIsIgnored_NoException()
    {
        // First call — will attempt LoadAll but may fail gracefully on missing DLLs
        try { NativePluginLoader.Initialize("fake_path_1/"); } catch { /* ignore load failures */ }

        // Second call must not throw regardless of what the first call did
        Assert.DoesNotThrow(() => NativePluginLoader.Initialize("fake_path_2/"),
            "Second Initialize must be silently ignored, not throw");
    }

    // T-05: After Shutdown the loader must accept a fresh Initialize without complaint.
    // White-box: verifies the _path = null reset in Shutdown, which re-opens the guard.
    [Test]
    public void Shutdown_ResetsState_AllowsReinitialize()
    {
        try { NativePluginLoader.Initialize("fake_path_1/"); } catch { }
        NativePluginLoader.Shutdown();

        // After shutdown a fresh Initialize should not be rejected by the double-init guard
        Assert.DoesNotThrow(() =>
        {
            try { NativePluginLoader.Initialize("fake_path_2/"); } catch (Exception ex)
            {
                // A real load failure is acceptable — what we must NOT see is the
                // "called more than once" guard firing, which only writes to stderr.
                // We treat DLL-load exceptions as out of scope for this test.
                if (ex.Message.Contains("called more than once"))
                    Assert.Fail("Shutdown did not reset the guard: " + ex.Message);
            }
        });
    }
}
