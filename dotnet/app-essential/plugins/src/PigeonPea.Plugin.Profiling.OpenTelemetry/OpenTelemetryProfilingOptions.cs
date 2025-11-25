namespace PigeonPea.Plugin.Profiling.OpenTelemetry;

/// <summary>
/// Configuration options for OpenTelemetry Profiling service.
/// </summary>
public class OpenTelemetryProfilingOptions
{
    /// <summary>
    /// OTLP endpoint (e.g., "http://localhost:4317")
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Jaeger agent endpoint (e.g., "localhost")
    /// </summary>
    public string? JaegerEndpoint { get; set; }

    /// <summary>
    /// Enable console exporter for development
    /// </summary>
    public bool UseConsoleExporter { get; set; }

    /// <summary>
    /// Service name for traces
    /// </summary>
    public string ServiceName { get; set; } = "PigeonPea";
}
