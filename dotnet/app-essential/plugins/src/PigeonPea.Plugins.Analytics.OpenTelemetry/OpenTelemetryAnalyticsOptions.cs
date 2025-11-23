namespace PigeonPea.Plugins.Analytics.OpenTelemetry;

/// <summary>
/// Configuration options for OpenTelemetry Analytics service.
/// </summary>
public class OpenTelemetryAnalyticsOptions
{
    /// <summary>
    /// OTLP endpoint (e.g., "http://localhost:4317")
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Prometheus scrape endpoint (e.g., "http://localhost:9464/metrics/")
    /// </summary>
    public string? PrometheusEndpoint { get; set; }

    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool UseConsoleExporter { get; set; }

    /// <summary>
    /// Trace sampling rate (0.0 to 1.0)
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// Service name for traces
    /// </summary>
    public string ServiceName { get; set; } = "PigeonPea";
}
