using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PigeonPea.Diagnostic.Contracts;

namespace PigeonPea.Plugin.Diagnostic.OpenTelemetry;

/// <summary>
/// OpenTelemetry-based diagnostic service implementation.
/// </summary>
public class OpenTelemetryDiagnosticService : IService, IDisposable
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Diagnostic", "1.0.0");

    private static readonly Meter Meter =
        new("PigeonPea.Diagnostic", "1.0.0");

    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
    private readonly List<ErrorReport> _recentErrors = new();
    private readonly int _maxRecentErrors;
    private readonly object _lock = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;

    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _warningCounter;
    private readonly ObservableGauge<long> _uptimeGauge;
    private readonly ObservableGauge<long> _memoryGauge;

    /// <summary>
    /// Initializes a new instance of the OpenTelemetryDiagnosticService.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public OpenTelemetryDiagnosticService(OpenTelemetryDiagnosticOptions? options = null)
    {
        _maxRecentErrors = options?.MaxRecentErrors ?? 100;

        _errorCounter = Meter.CreateCounter<long>("diagnostic.errors.total", "count", "Total errors reported");
        _warningCounter = Meter.CreateCounter<long>("diagnostic.warnings.total", "count", "Total warnings reported");
        _uptimeGauge = Meter.CreateObservableGauge("diagnostic.uptime.seconds", () => (long)(DateTime.UtcNow - _startTime).TotalSeconds);
        _memoryGauge = Meter.CreateObservableGauge("diagnostic.memory.bytes", () => GC.GetTotalMemory(false));

        ConfigureProviders(options);
    }

    private void ConfigureProviders(OpenTelemetryDiagnosticOptions? options)
    {
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .AddSource("PigeonPea.Diagnostic")
            .SetSampler(new TraceIdRatioBasedSampler(1.0)); // Always sample for diagnostics

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .AddMeter("PigeonPea.Diagnostic");

        if (options?.UseConsoleExporter == true)
        {
            tracerBuilder.AddConsoleExporter();
            meterBuilder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(options?.OtlpEndpoint))
        {
            tracerBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            meterBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
        }

        _tracerProvider = tracerBuilder.Build();
        _meterProvider = meterBuilder.Build();
    }

    /// <inheritdoc />
    public HealthCheckResult CheckHealth()
    {
        var stopwatch = Stopwatch.StartNew();
        var entries = new Dictionary<string, HealthCheckEntry>();
        var overallStatus = HealthStatus.Healthy;

        foreach (var (name, check) in _healthChecks)
        {
            try
            {
                var entry = check.Check();
                entries[name] = entry;

                if (entry.Status == HealthStatus.Unhealthy)
                    overallStatus = HealthStatus.Unhealthy;
                else if (entry.Status == HealthStatus.Degraded && overallStatus == HealthStatus.Healthy)
                    overallStatus = HealthStatus.Degraded;
            }
            catch (Exception ex)
            {
                entries[name] = new HealthCheckEntry
                {
                    Status = HealthStatus.Unhealthy,
                    Description = $"Health check threw exception: {ex.Message}",
                    Exception = ex,
                    Duration = TimeSpan.Zero
                };
                overallStatus = HealthStatus.Unhealthy;
            }
        }

        stopwatch.Stop();

        return new HealthCheckResult
        {
            Status = overallStatus,
            Description = overallStatus == HealthStatus.Healthy ? "All checks passed" : "Some checks failed",
            Entries = entries,
            Duration = stopwatch.Elapsed
        };
    }

    /// <inheritdoc />
    public HealthCheckResult CheckHealth(string checkName)
    {
        if (!_healthChecks.TryGetValue(checkName, out var check))
        {
            return HealthCheckResult.Unhealthy($"Health check '{checkName}' not found");
        }

        var stopwatch = Stopwatch.StartNew();
        var entry = check.Check();
        stopwatch.Stop();

        return new HealthCheckResult
        {
            Status = entry.Status,
            Description = entry.Description,
            Entries = new Dictionary<string, HealthCheckEntry> { [checkName] = entry },
            Duration = stopwatch.Elapsed
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetHealthCheckNames()
    {
        return _healthChecks.Keys.ToList();
    }

    /// <inheritdoc />
    public void ReportError(Exception exception, IDictionary<string, object>? context = null)
    {
        _errorCounter.Add(1);

        using var activity = ActivitySource.StartActivity("Error", ActivityKind.Internal);
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.RecordException(exception); // Added missing RecordException call

        if (context != null)
        {
            foreach (var (key, value) in context)
            {
                activity?.SetTag(key, value?.ToString());
            }
        }

        lock (_lock)
        {
            var report = new ErrorReport
            {
                Timestamp = DateTime.UtcNow,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Context = context?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            _recentErrors.Add(report);

            // Trim old errors
            while (_recentErrors.Count > _maxRecentErrors)
            {
                _recentErrors.RemoveAt(0);
            }
        }
    }

    /// <inheritdoc />
    public void ReportWarning(string message, IDictionary<string, object>? context = null)
    {
        _warningCounter.Add(1);

        using var activity = ActivitySource.StartActivity("Warning", ActivityKind.Internal);
        activity?.SetTag("warning.message", message);

        if (context != null)
        {
            foreach (var (key, value) in context)
            {
                activity?.SetTag(key, value?.ToString());
            }
        }
    }

    /// <inheritdoc />
    public SystemStatus GetSystemStatus()
    {
        var healthResult = CheckHealth();

        return new SystemStatus
        {
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Uptime = DateTime.UtcNow - _startTime,
            MemoryUsedBytes = GC.GetTotalMemory(false),
            ThreadCount = Process.GetCurrentProcess().Threads.Count,
            PluginCount = _healthChecks.Count, // Approximate
            HealthStatus = healthResult.Status,
            StartTime = _startTime
        };
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetDiagnosticInfo()
    {
        var process = Process.GetCurrentProcess();

        return new Dictionary<string, object>
        {
            ["runtime"] = Environment.Version.ToString(),
            ["os"] = Environment.OSVersion.ToString(),
            ["processors"] = Environment.ProcessorCount,
            ["memory.working_set"] = process.WorkingSet64,
            ["memory.private"] = process.PrivateMemorySize64,
            ["memory.gc_total"] = GC.GetTotalMemory(false),
            ["gc.gen0_collections"] = GC.CollectionCount(0),
            ["gc.gen1_collections"] = GC.CollectionCount(1),
            ["gc.gen2_collections"] = GC.CollectionCount(2),
            ["threads"] = process.Threads.Count,
            ["uptime_seconds"] = (DateTime.UtcNow - _startTime).TotalSeconds
        };
    }

    /// <inheritdoc />
    public DiagnosticSnapshot CreateSnapshot()
    {
        return new DiagnosticSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SystemStatus = GetSystemStatus(),
            HealthCheck = CheckHealth(),
            RecentErrors = GetRecentErrors(),
            Data = GetDiagnosticInfo(),
            JsonSerializer = (snapshot) => System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            })
        };
    }

    /// <inheritdoc />
    public void RegisterHealthCheck(string name, IHealthCheck healthCheck)
    {
        _healthChecks[name] = healthCheck;
    }

    /// <inheritdoc />
    public IReadOnlyList<ErrorReport> GetRecentErrors(int maxCount = 10)
    {
        lock (_lock)
        {
            return _recentErrors
                .TakeLast(maxCount)
                .ToList();
        }
    }

    /// <summary>
    /// Releases all resources used by the OpenTelemetryDiagnosticService.
    /// </summary>
    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}
