namespace PigeonPea.Plugins.Diagnostic.Sentry;

/// <summary>
/// Configuration options for Sentry diagnostic service.
/// </summary>
public class SentryDiagnosticOptions
{
    /// <summary>
    /// Sentry DSN (Data Source Name).
    /// </summary>
    public string? Dsn { get; set; }

    /// <summary>
    /// Environment name (e.g., "production", "staging", "development").
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Release version (e.g., "1.0.0", "1.0.0+abc123").
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Enable debug mode for troubleshooting.
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// Traces sample rate (0.0 to 1.0).
    /// </summary>
    public double TracesSampleRate { get; set; } = 1.0;

    /// <summary>
    /// Enable automatic session tracking.
    /// </summary>
    public bool AutoSessionTracking { get; set; } = true;

    /// <summary>
    /// Maximum breadcrumbs to keep.
    /// </summary>
    public int MaxBreadcrumbs { get; set; } = 100;

    /// <summary>
    /// Maximum recent errors to store locally.
    /// </summary>
    public int MaxRecentErrors { get; set; } = 100;

    /// <summary>
    /// Memory warning threshold in bytes.
    /// </summary>
    public long MemoryWarningThreshold { get; set; } = 500_000_000;

    /// <summary>
    /// Capture warnings as Sentry events (not just breadcrumbs).
    /// </summary>
    public bool CaptureWarningsAsEvents { get; set; }
}
