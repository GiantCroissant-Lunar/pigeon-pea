namespace PigeonPea.Plugin.Diagnostic.OpenTelemetry;

/// <summary>
/// Configuration options for OpenTelemetry Diagnostic service.
/// </summary>
public class OpenTelemetryDiagnosticOptions
{
    /// <summary>
    /// OTLP endpoint (e.g., "http://localhost:4317")
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool UseConsoleExporter { get; set; }

    /// <summary>
    /// Maximum number of recent errors to keep in memory
    /// </summary>
    public int MaxRecentErrors { get; set; } = 100;

    /// <summary>
    /// Service name for traces and metrics
    /// </summary>
    public string ServiceName { get; set; } = "PigeonPea";
}
