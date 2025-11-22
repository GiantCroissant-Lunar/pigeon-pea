namespace PigeonPea.Contracts.Diagnostic.Services;

/// <summary>
/// Diagnostic service for health checks, error reporting, and system status.
/// </summary>
public interface IService
{
    /// <summary>
    /// Performs a health check and returns the overall status.
    /// </summary>
    HealthCheckResult CheckHealth();

    /// <summary>
    /// Performs a specific health check by name.
    /// </summary>
    HealthCheckResult CheckHealth(string checkName);

    /// <summary>
    /// Gets all registered health check names.
    /// </summary>
    IReadOnlyList<string> GetHealthCheckNames();

    /// <summary>
    /// Reports an error or exception.
    /// </summary>
    void ReportError(Exception exception, IDictionary<string, object>? context = null);

    /// <summary>
    /// Reports a warning condition.
    /// </summary>
    void ReportWarning(string message, IDictionary<string, object>? context = null);

    /// <summary>
    /// Gets the current system status.
    /// </summary>
    SystemStatus GetSystemStatus();

    /// <summary>
    /// Gets diagnostic information as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object> GetDiagnosticInfo();

    /// <summary>
    /// Creates a diagnostic snapshot for debugging.
    /// </summary>
    DiagnosticSnapshot CreateSnapshot();

    /// <summary>
    /// Registers a health check provider.
    /// </summary>
    void RegisterHealthCheck(string name, IHealthCheck healthCheck);

    /// <summary>
    /// Gets recent errors (up to a configured limit).
    /// </summary>
    IReadOnlyList<ErrorReport> GetRecentErrors(int maxCount = 10);
}

/// <summary>
/// Result of a health check.
/// </summary>
public sealed class HealthCheckResult
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Individual check results.
    /// </summary>
    public IReadOnlyDictionary<string, HealthCheckEntry> Entries { get; init; }
        = new Dictionary<string, HealthCheckEntry>();

    /// <summary>
    /// Total duration of health checks.
    /// </summary>
    public TimeSpan Duration { get; init; }

    public static HealthCheckResult Healthy(string description = "Healthy")
        => new() { Status = HealthStatus.Healthy, Description = description };

    public static HealthCheckResult Unhealthy(string description)
        => new() { Status = HealthStatus.Unhealthy, Description = description };

    public static HealthCheckResult Degraded(string description)
        => new() { Status = HealthStatus.Degraded, Description = description };
}

/// <summary>
/// Individual health check entry.
/// </summary>
public sealed class HealthCheckEntry
{
    public HealthStatus Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public Exception? Exception { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// The component is degraded but functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// The component is unhealthy.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Overall system status.
/// </summary>
public sealed class SystemStatus
{
    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Uptime since application start.
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Current memory usage in bytes.
    /// </summary>
    public long MemoryUsedBytes { get; init; }

    /// <summary>
    /// Number of active threads.
    /// </summary>
    public int ThreadCount { get; init; }

    /// <summary>
    /// Number of loaded plugins.
    /// </summary>
    public int PluginCount { get; init; }

    /// <summary>
    /// Overall health status.
    /// </summary>
    public HealthStatus HealthStatus { get; init; }

    /// <summary>
    /// Application start time.
    /// </summary>
    public DateTime StartTime { get; init; }
}

/// <summary>
/// A snapshot of diagnostic information.
/// </summary>
public sealed class DiagnosticSnapshot
{
    /// <summary>
    /// Snapshot timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// System status at snapshot time.
    /// </summary>
    public SystemStatus SystemStatus { get; init; } = new();

    /// <summary>
    /// Health check results.
    /// </summary>
    public HealthCheckResult HealthCheck { get; init; } = HealthCheckResult.Healthy();

    /// <summary>
    /// Recent errors.
    /// </summary>
    public IReadOnlyList<ErrorReport> RecentErrors { get; init; } = Array.Empty<ErrorReport>();

    /// <summary>
    /// Additional diagnostic data.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; init; }
        = new Dictionary<string, object>();

    /// <summary>
    /// Exports the snapshot to JSON (implementation provided by plugin).
    /// </summary>
    public Func<DiagnosticSnapshot, string>? JsonSerializer { get; set; }

    /// <summary>
    /// Exports the snapshot to JSON.
    /// </summary>
    public string ToJson() => JsonSerializer?.Invoke(this) ?? throw new InvalidOperationException("JsonSerializer not configured");
}

/// <summary>
/// An error report entry.
/// </summary>
public sealed class ErrorReport
{
    /// <summary>
    /// Error timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Exception type name.
    /// </summary>
    public string ExceptionType { get; init; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Stack trace (if available).
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Additional context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}

/// <summary>
/// Interface for health check providers.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Performs the health check.
    /// </summary>
    HealthCheckEntry Check();
}
