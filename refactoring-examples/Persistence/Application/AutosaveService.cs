namespace iDaVIE.Persistence.Application;

/// <summary>
/// Fires a save cycle on a System.Threading.Timer every <paramref name="intervalSeconds"/> seconds.
/// Fully Unity-independent. The Unity-side integration must ensure <see cref="SaveWorkspaceUseCase.Execute"/>
/// runs on the Unity main thread (e.g. via a thread-safe ConcurrentQueue polled in Update()).
///
/// Timer fires on a background thread; state collection is synchronised by the use-case itself.
/// If a save is already running when the timer fires, that cycle is silently skipped.
/// A user-triggered save (Save button) should call TriggerNow() which also resets the timer.
/// </summary>
public sealed class AutosaveService : IDisposable
{
    private readonly SaveWorkspaceUseCase _saveUseCase;
    private readonly int                 _intervalMs;
    private Timer?                       _timer;
    private bool                         _disposed;

    public event Action<bool>? SaveCompleted; // arg: whether save was skipped

    public AutosaveService(SaveWorkspaceUseCase saveUseCase, int intervalSeconds = 20)
    {
        _saveUseCase = saveUseCase;
        _intervalMs  = intervalSeconds * 1000;
    }

    public void Start()
    {
        _timer?.Dispose();
        _timer = new Timer(OnTimerFired, null, _intervalMs, _intervalMs);
    }

    public void Stop() => _timer?.Change(Timeout.Infinite, Timeout.Infinite);

    /// <summary>
    /// Triggers an immediate save and resets the periodic timer.
    /// Call from Unity main thread (Save button handler).
    /// </summary>
    public void TriggerNow()
    {
        bool saved = _saveUseCase.Execute();
        SaveCompleted?.Invoke(saved);
        // Reset timer so next autosave is a full interval away
        _timer?.Change(_intervalMs, _intervalMs);
    }

    private void OnTimerFired(object? _)
    {
        bool saved = _saveUseCase.Execute();
        SaveCompleted?.Invoke(saved);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
