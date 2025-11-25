namespace PigeonPea.Platform.Contracts.Diagnostic.Services;

/// <summary>
/// Diagnostic service interface for tracking errors, warnings, and system health.
/// </summary>
public interface IDiagnosticService
{
    /// <summary>
    /// Gets or sets whether diagnostic tracking is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Reports an error with optional context data.
    /// </summary>
    /// <param name="error">The error to report.</param>
    /// <param name="context">Optional context data about the error.</param>
    /// <param name="severity">The severity level of the error.</param>
    void ReportError(Exception error, Dictionary<string, object>? context = null, DiagnosticSeverity severity = DiagnosticSeverity.Error);

    /// <summary>
    /// Reports a warning message with optional context data.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="context">Optional context data about the warning.</param>
    void ReportWarning(string message, Dictionary<string, object>? context = null);

    /// <summary>
    /// Reports an informational diagnostic event.
    /// </summary>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="context">Optional context data about the event.</param>
    void ReportInfo(string message, Dictionary<string, object>? context = null);

    /// <summary>
    /// Reports a system health metric.
    /// </summary>
    /// <param name="metricName">The name of the health metric.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    void ReportHealthMetric(string metricName, double value, string? unit = null);

    /// <summary>
    /// Creates a diagnostic session for tracking related events.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    /// <param name="context">Optional initial context data.</param>
    /// <returns>A disposable session that ends when disposed.</returns>
    IDiagnosticSession CreateSession(string sessionName, Dictionary<string, object>? context = null);

    /// <summary>
    /// Gets the current diagnostic status and health information.
    /// </summary>
    /// <returns>The current diagnostic status.</returns>
    DiagnosticStatus GetStatus();

    /// <summary>
    /// Flushes any pending diagnostic data.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the flush operation.</returns>
    Task FlushAsync(System.Threading.CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a diagnostic session for grouping related events.
/// </summary>
public interface IDiagnosticSession : IDisposable
{
    /// <summary>
    /// Gets the unique identifier of the session.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the name of the session.
    /// </summary>
    string SessionName { get; }

    /// <summary>
    /// Gets the start time of the session.
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Adds context data to the session.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    void AddContext(string key, object value);

    /// <summary>
    /// Reports an error within the session context.
    /// </summary>
    /// <param name="error">The error to report.</param>
    /// <param name="additionalContext">Additional context data.</param>
    void ReportError(Exception error, Dictionary<string, object>? additionalContext = null);

    /// <summary>
    /// Reports a warning within the session context.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="additionalContext">Additional context data.</param>
    void ReportWarning(string message, Dictionary<string, object>? additionalContext = null);
}

/// <summary>
/// Diagnostic severity levels.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational level.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error level.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error level.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Current diagnostic status and health information.
/// </summary>
public class DiagnosticStatus
{
    /// <summary>
    /// Gets or sets whether the system is healthy.
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Gets or sets the overall health score (0-100).
    /// </summary>
    public double HealthScore { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the number of errors reported.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the number of warnings reported.
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Gets or sets the number of active sessions.
    /// </summary>
    public int ActiveSessionCount { get; set; }

    /// <summary>
    /// Gets or sets the last error timestamp.
    /// </summary>
    public DateTimeOffset? LastErrorTimestamp { get; set; }

    /// <summary>
    /// Gets or sets health metrics.
    /// </summary>
    public Dictionary<string, double> HealthMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets additional status information.
    /// </summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
