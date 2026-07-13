namespace iDaVIE.Persistence.Application;

/// <summary>
/// Outcome of a crash recovery attempt.
/// </summary>
public class RecoveryResult
{
    public bool Recovered { get; set; }
    public LoadResult? LoadResult { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Runs the full crash recovery flow:
///   try newest → oldest snapshot → hand off to RestoreOrchestrator on first valid slot.
/// Delegates to LoadWorkspaceUseCase for per-slot validation.
/// </summary>
public class CrashRecoveryOrchestrator
{
    private readonly LoadWorkspaceUseCase _loadUseCase;

    public CrashRecoveryOrchestrator(LoadWorkspaceUseCase loadUseCase)
    {
        _loadUseCase = loadUseCase;
    }

    public RecoveryResult Recover()
    {
        var result = _loadUseCase.Execute();
        if (result.Success)
        {
            return new RecoveryResult
            {
                Recovered  = true,
                LoadResult = result,
                Message    = BuildRecoveryMessage(result),
            };
        }

        return new RecoveryResult
        {
            Recovered = false,
            Message   = result.ErrorMessage ?? "No valid snapshot found. Starting blank session.",
        };
    }

    private static string BuildRecoveryMessage(LoadResult result)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Session recovered from last valid snapshot.");
        if (result.Validation?.HasWarnings == true)
        {
            sb.AppendLine("Warnings applied during restore:");
            foreach (var w in result.Validation.Warnings)
                sb.AppendLine($"  • {w}");
        }
        if (result.Validation?.ExcludedFeatureIds.Count > 0)
        {
            sb.AppendLine($"Excluded invalid features (IDs): {string.Join(", ", result.Validation.ExcludedFeatureIds)}");
        }
        return sb.ToString();
    }
}
