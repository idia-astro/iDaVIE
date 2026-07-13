namespace iDaVIE.Persistence.Application;

/// <summary>
/// Manages a session lock file to detect unclean shutdowns (crashes).
///
/// Protocol:
///   On startup: check whether lock file exists.
///     Present → previous session crashed → trigger crash recovery.
///     Absent  → clean start.
///   Write lock file immediately after decision.
///   Delete lock file on clean shutdown (Dispose / explicit Release).
/// </summary>
public class CrashDetector : IDisposable
{
    private readonly string _lockFilePath;
    private bool _lockHeld;

    public CrashDetector(string snapshotDirectory)
    {
        _lockFilePath = Path.Combine(snapshotDirectory, "session.lock");
    }

    /// <summary>
    /// Returns true when a previous session lock file is present (crash detected).
    /// Always writes a new lock file for this session after checking.
    /// </summary>
    public bool CheckAndAcquire()
    {
        bool crashed = File.Exists(_lockFilePath);

        // Overwrite / create the lock file for this session
        File.WriteAllText(_lockFilePath,
            $"pid={Environment.ProcessId}\nstarted={DateTime.UtcNow:O}");
        _lockHeld = true;

        return crashed;
    }

    /// <summary>Deletes the lock file — call on clean shutdown.</summary>
    public void Release()
    {
        if (!_lockHeld) return;
        if (File.Exists(_lockFilePath))
            File.Delete(_lockFilePath);
        _lockHeld = false;
    }

    public void Dispose() => Release();
}
