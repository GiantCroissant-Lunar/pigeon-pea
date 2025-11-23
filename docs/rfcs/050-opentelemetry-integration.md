---
doc_id: RFC-00050
title: 'OpenTelemetry Integration: Unified Observability for Analytics, Profiling, and Diagnostics'
doc_type: rfc
status: draft
canonical: true
created: '2025-11-22'
tags:
  - observability
  - opentelemetry
  - analytics
  - profiling
  - diagnostics
  - plugins
  - infrastructure
summary: OpenTelemetry-based plugin implementations for Analytics, Profiling, and Diagnostic services with OTLP export to Jaeger, Prometheus, and Grafana
related:
  - RFC-00049
---

# RFC: OpenTelemetry Integration

- **Status:** Draft
- **Date:** 2025-11-22
- **Related:** RFC-00049 (Profiling Service)

## Summary

This RFC defines OpenTelemetry-based plugin implementations for the Analytics, Profiling, and Diagnostic service contracts. OpenTelemetry provides a vendor-neutral, standardized approach to observability with support for:

- **Traces** (distributed tracing, spans)
- **Metrics** (counters, gauges, histograms)
- **Logs** (structured logging)

These plugins export telemetry data via OTLP (OpenTelemetry Protocol) to backends like Jaeger, Prometheus, Grafana Tempo, and others.

## Motivation

### Why OpenTelemetry?

| Benefit                 | Description                                                               |
| ----------------------- | ------------------------------------------------------------------------- |
| **Vendor-neutral**      | Single API, many backends (Jaeger, Zipkin, Prometheus, Datadog, etc.)     |
| **Standardized**        | CNCF project, industry standard for observability                         |
| **Native .NET support** | First-class support via `OpenTelemetry.Api` and instrumentation libraries |
| **Unified**             | Traces, metrics, and logs in one framework                                |
| **Low overhead**        | Designed for production use with minimal performance impact               |

### Current State

We have three service contracts (RFC-00049 and related):

- `PigeonPea.Contracts.Analytics.Services.IService`
- `PigeonPea.Contracts.Profiling.Services.IService`
- `PigeonPea.Contracts.Diagnostic.Services.IService`

These need concrete implementations that can export to observability backends.

### Goals

1. **Implement all three services** using OpenTelemetry primitives
2. **OTLP export** to standard backends (Jaeger, Prometheus, OTLP collector)
3. **Zero-config development** mode with console exporter
4. **Production-ready** with configurable endpoints and sampling
5. **Game-friendly** with low overhead and batch export

## Architecture

### Plugin Structure

```
dotnet/app-essential/plugins/
└── src/
    ├── PigeonPea.Plugin.Analytics.OpenTelemetry/
    │   ├── OpenTelemetryAnalyticsService.cs
    │   ├── AnalyticsPlugin.cs
    │   └── plugin.json
    ├── PigeonPea.Plugin.Profiling.OpenTelemetry/
    │   ├── OpenTelemetryProfilingService.cs
    │   ├── ProfilingPlugin.cs
    │   └── plugin.json
    └── PigeonPea.Plugin.Diagnostic.OpenTelemetry/
        ├── OpenTelemetryDiagnosticService.cs
        ├── DiagnosticPlugin.cs
        └── plugin.json
```

### OpenTelemetry Mapping

| Service Contract                       | OpenTelemetry Primitive      | Export Target |
| -------------------------------------- | ---------------------------- | ------------- |
| `IAnalyticsService.TrackEvent()`       | `Activity` (trace span)      | Jaeger, Tempo |
| `IAnalyticsService.TrackMetric()`      | `Meter.CreateHistogram()`    | Prometheus    |
| `IAnalyticsService.IncrementCounter()` | `Meter.CreateCounter()`      | Prometheus    |
| `IProfilingService.BeginScope()`       | `Activity.Start()`           | Jaeger, Tempo |
| `IProfilingService.RecordMarker()`     | `Activity.AddEvent()`        | Jaeger, Tempo |
| `IDiagnosticService.CheckHealth()`     | Custom health metrics        | Prometheus    |
| `IDiagnosticService.ReportError()`     | `Activity` with error status | Jaeger + logs |

## Service Implementations

### Analytics Service (OpenTelemetry)

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PigeonPea.Contracts.Analytics.Services;

namespace PigeonPea.Plugin.Analytics.OpenTelemetry;

public class OpenTelemetryAnalyticsService : IService, IDisposable
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Analytics", "1.0.0");

    private static readonly Meter Meter =
        new("PigeonPea.Analytics", "1.0.0");

    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();

    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;
    private bool _isEnabled = true;
    private string? _userId;
    private readonly Dictionary<string, object> _userProperties = new();

    public OpenTelemetryAnalyticsService(OpenTelemetryAnalyticsOptions options)
    {
        ConfigureProviders(options);
    }

    private void ConfigureProviders(OpenTelemetryAnalyticsOptions options)
    {
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .AddSource("PigeonPea.Analytics")
            .SetSampler(new TraceIdRatioBasedSampler(options.SampleRate));

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .AddMeter("PigeonPea.Analytics");

        if (options.UseConsoleExporter)
        {
            tracerBuilder.AddConsoleExporter();
            meterBuilder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            tracerBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
            meterBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
        }

        if (!string.IsNullOrEmpty(options.PrometheusEndpoint))
        {
            meterBuilder.AddPrometheusHttpListener(o =>
                o.UriPrefixes = new[] { options.PrometheusEndpoint });
        }

        _tracerProvider = tracerBuilder.Build();
        _meterProvider = meterBuilder.Build();
    }

    public void TrackEvent(string eventName, IDictionary<string, object>? properties = null)
    {
        if (!_isEnabled) return;

        using var activity = ActivitySource.StartActivity(eventName, ActivityKind.Internal);
        if (activity == null) return;

        // Add user context
        if (_userId != null)
            activity.SetTag("user.id", _userId);

        foreach (var prop in _userProperties)
            activity.SetTag($"user.{prop.Key}", prop.Value?.ToString());

        // Add event properties
        if (properties != null)
        {
            foreach (var prop in properties)
                activity.SetTag(prop.Key, prop.Value?.ToString());
        }
    }

    public void TrackMetric(string metricName, double value, IDictionary<string, string>? dimensions = null)
    {
        if (!_isEnabled) return;

        var histogram = GetOrCreateHistogram(metricName);

        if (dimensions != null)
        {
            var tags = dimensions.Select(d => new KeyValuePair<string, object?>(d.Key, d.Value)).ToArray();
            histogram.Record(value, tags);
        }
        else
        {
            histogram.Record(value);
        }
    }

    public void IncrementCounter(string counterName, long value = 1)
    {
        if (!_isEnabled) return;

        var counter = GetOrCreateCounter(counterName);
        counter.Add(value);
    }

    public void TrackTiming(string operationName, TimeSpan duration, IDictionary<string, string>? dimensions = null)
    {
        TrackMetric($"{operationName}.duration_ms", duration.TotalMilliseconds, dimensions);
    }

    public ITimedOperation BeginTimedOperation(string operationName)
    {
        return new OpenTelemetryTimedOperation(ActivitySource, operationName, _isEnabled);
    }

    public void SetUserProperty(string propertyName, object value)
    {
        _userProperties[propertyName] = value;
    }

    public void SetUserId(string? userId)
    {
        _userId = userId;
    }

    public void Flush()
    {
        _tracerProvider?.ForceFlush();
        _meterProvider?.ForceFlush();
    }

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        if (!_counters.TryGetValue(name, out var counter))
        {
            counter = Meter.CreateCounter<long>(name);
            _counters[name] = counter;
        }
        return counter;
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        if (!_histograms.TryGetValue(name, out var histogram))
        {
            histogram = Meter.CreateHistogram<double>(name);
            _histograms[name] = histogram;
        }
        return histogram;
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }
}

internal class OpenTelemetryTimedOperation : ITimedOperation
{
    private readonly Activity? _activity;
    private bool _failed;
    private Exception? _exception;

    public OpenTelemetryTimedOperation(ActivitySource source, string name, bool enabled)
    {
        if (enabled)
        {
            _activity = source.StartActivity(name, ActivityKind.Internal);
        }
    }

    public void AddProperty(string key, object value)
    {
        _activity?.SetTag(key, value?.ToString());
    }

    public void SetFailed(Exception? exception = null)
    {
        _failed = true;
        _exception = exception;
    }

    public void Dispose()
    {
        if (_activity == null) return;

        if (_failed)
        {
            _activity.SetStatus(ActivityStatusCode.Error, _exception?.Message);
            if (_exception != null)
            {
                _activity.RecordException(_exception);
            }
        }
        else
        {
            _activity.SetStatus(ActivityStatusCode.Ok);
        }

        _activity.Dispose();
    }
}
```

### Profiling Service (OpenTelemetry)

```csharp
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PigeonPea.Contracts.Profiling.Services;

namespace PigeonPea.Plugin.Profiling.OpenTelemetry;

public class OpenTelemetryProfilingService : IService, IDisposable
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Profiling", "1.0.0");

    private TracerProvider? _tracerProvider;
    private ProfilerMode _mode = ProfilerMode.Instrumentation;
    private bool _isCapturing;
    private readonly List<ProfileEvent> _capturedEvents = new();
    private readonly object _lock = new();
    private long _frameNumber;
    private readonly Stopwatch _frameStopwatch = new();
    private readonly Dictionary<string, ScopeStatsAccumulator> _scopeStats = new();

    public OpenTelemetryProfilingService(OpenTelemetryProfilingOptions options)
    {
        ConfigureProvider(options);
    }

    private void ConfigureProvider(OpenTelemetryProfilingOptions options)
    {
        var builder = Sdk.CreateTracerProviderBuilder()
            .AddSource("PigeonPea.Profiling")
            .SetSampler(new AlwaysOnSampler());

        if (options.UseConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            builder.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
        }

        if (!string.IsNullOrEmpty(options.JaegerEndpoint))
        {
            builder.AddJaegerExporter(o => o.AgentHost = options.JaegerEndpoint);
        }

        _tracerProvider = builder.Build();
    }

    public IProfileScope BeginScope(string name)
    {
        return BeginScope(name, "default");
    }

    public IProfileScope BeginScope(string name, string category)
    {
        if (_mode == ProfilerMode.Disabled)
            return NullProfileScope.Instance;

        var activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag("category", category);

        return new OpenTelemetryProfileScope(activity, name, category, this);
    }

    public void RecordMarker(string name)
    {
        if (_mode == ProfilerMode.Disabled) return;

        Activity.Current?.AddEvent(new ActivityEvent(name));
    }

    public void RecordCounter(string name, double value)
    {
        if (_mode == ProfilerMode.Disabled) return;

        Activity.Current?.SetTag($"counter.{name}", value);
    }

    public void StartCapture()
    {
        lock (_lock)
        {
            _capturedEvents.Clear();
            _isCapturing = true;
        }
    }

    public ProfileCapture StopCapture()
    {
        lock (_lock)
        {
            _isCapturing = false;
            return new ProfileCapture
            {
                StartTime = _capturedEvents.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                FrameCount = (int)_frameNumber,
                EventCount = _capturedEvents.Count
            };
        }
    }

    public void ClearCapture()
    {
        lock (_lock)
        {
            _capturedEvents.Clear();
        }
    }

    public bool IsCapturing => _isCapturing;

    public void SetMode(ProfilerMode mode)
    {
        _mode = mode;
    }

    public ProfilerMode Mode => _mode;

    public void SetCategoryEnabled(string category, bool enabled)
    {
        // OpenTelemetry handles this via sampling
    }

    public void SetSampleRate(int samplesPerSecond)
    {
        // Configure via options
    }

    public void ExportToSpeedscope(string filePath)
    {
        var exporter = new SpeedscopeExporter();
        exporter.Export(_capturedEvents, filePath);
    }

    public void ExportToChromeTrace(string filePath)
    {
        var exporter = new ChromeTraceExporter();
        exporter.Export(_capturedEvents, filePath);
    }

    public void Export(string filePath, ProfileExportFormat format)
    {
        switch (format)
        {
            case ProfileExportFormat.Speedscope:
                ExportToSpeedscope(filePath);
                break;
            case ProfileExportFormat.ChromeTrace:
                ExportToChromeTrace(filePath);
                break;
            case ProfileExportFormat.Json:
                ExportToJson(filePath);
                break;
        }
    }

    private void ExportToJson(string filePath)
    {
        // Simple JSON export
    }

    public FrameStats GetCurrentFrameStats()
    {
        return new FrameStats
        {
            FrameNumber = _frameNumber,
            FrameTimeMs = _frameStopwatch.Elapsed.TotalMilliseconds,
            ScopeTimesMs = _scopeStats.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.LastMs)
        };
    }

    public ScopeStats GetScopeStats(string scopeName, int frameCount = 60)
    {
        if (_scopeStats.TryGetValue(scopeName, out var accumulator))
        {
            return accumulator.GetStats(frameCount);
        }
        return new ScopeStats { Name = scopeName };
    }

    public IReadOnlyList<ScopeStats> GetAllScopeStats(int frameCount = 60)
    {
        return _scopeStats.Values
            .Select(a => a.GetStats(frameCount))
            .OrderByDescending(s => s.TotalMs)
            .ToList();
    }

    public void EndFrame()
    {
        _frameNumber++;
        _frameStopwatch.Restart();
    }

    internal void RecordScopeTime(string name, string category, double durationMs)
    {
        lock (_lock)
        {
            if (!_scopeStats.TryGetValue(name, out var accumulator))
            {
                accumulator = new ScopeStatsAccumulator(name, category);
                _scopeStats[name] = accumulator;
            }
            accumulator.Record(durationMs);

            if (_isCapturing)
            {
                _capturedEvents.Add(new ProfileEvent
                {
                    Name = name,
                    Category = category,
                    DurationMs = durationMs,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
    }
}

internal class OpenTelemetryProfileScope : IProfileScope
{
    private readonly Activity? _activity;
    private readonly string _name;
    private readonly string _category;
    private readonly OpenTelemetryProfilingService _service;
    private readonly Stopwatch _stopwatch;

    public OpenTelemetryProfileScope(
        Activity? activity,
        string name,
        string category,
        OpenTelemetryProfilingService service)
    {
        _activity = activity;
        _name = name;
        _category = category;
        _service = service;
        _stopwatch = Stopwatch.StartNew();
    }

    public void AddMetadata(string key, string value)
    {
        _activity?.SetTag(key, value);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _service.RecordScopeTime(_name, _category, _stopwatch.Elapsed.TotalMilliseconds);
        _activity?.Dispose();
    }
}

internal class NullProfileScope : IProfileScope
{
    public static readonly NullProfileScope Instance = new();
    public void AddMetadata(string key, string value) { }
    public void Dispose() { }
}
```

### Diagnostic Service (OpenTelemetry)

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using PigeonPea.Contracts.Diagnostic.Services;

namespace PigeonPea.Plugin.Diagnostic.OpenTelemetry;

public class OpenTelemetryDiagnosticService : IService
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Diagnostic", "1.0.0");

    private static readonly Meter Meter =
        new("PigeonPea.Diagnostic", "1.0.0");

    private readonly Dictionary<string, IHealthCheck> _healthChecks = new();
    private readonly List<ErrorReport> _recentErrors = new();
    private readonly int _maxRecentErrors;
    private readonly object _lock = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _warningCounter;
    private readonly ObservableGauge<long> _uptimeGauge;
    private readonly ObservableGauge<long> _memoryGauge;

    public OpenTelemetryDiagnosticService(OpenTelemetryDiagnosticOptions? options = null)
    {
        _maxRecentErrors = options?.MaxRecentErrors ?? 100;

        _errorCounter = Meter.CreateCounter<long>("diagnostic.errors.total", "count", "Total errors reported");
        _warningCounter = Meter.CreateCounter<long>("diagnostic.warnings.total", "count", "Total warnings reported");
        _uptimeGauge = Meter.CreateObservableGauge("diagnostic.uptime.seconds", () => (long)(DateTime.UtcNow - _startTime).TotalSeconds);
        _memoryGauge = Meter.CreateObservableGauge("diagnostic.memory.bytes", () => GC.GetTotalMemory(false));
    }

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

    public IReadOnlyList<string> GetHealthCheckNames()
    {
        return _healthChecks.Keys.ToList();
    }

    public void ReportError(Exception exception, IDictionary<string, object>? context = null)
    {
        _errorCounter.Add(1);

        using var activity = ActivitySource.StartActivity("Error", ActivityKind.Internal);
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.RecordException(exception);

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

    public DiagnosticSnapshot CreateSnapshot()
    {
        return new DiagnosticSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SystemStatus = GetSystemStatus(),
            HealthCheck = CheckHealth(),
            RecentErrors = GetRecentErrors(),
            Data = GetDiagnosticInfo()
        };
    }

    public void RegisterHealthCheck(string name, IHealthCheck healthCheck)
    {
        _healthChecks[name] = healthCheck;
    }

    public IReadOnlyList<ErrorReport> GetRecentErrors(int maxCount = 10)
    {
        lock (_lock)
        {
            return _recentErrors
                .TakeLast(maxCount)
                .ToList();
        }
    }
}
```

## Configuration

### Options Classes

```csharp
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

public class OpenTelemetryProfilingOptions
{
    public string? OtlpEndpoint { get; set; }
    public string? JaegerEndpoint { get; set; }
    public bool UseConsoleExporter { get; set; }
    public string ServiceName { get; set; } = "PigeonPea";
}

public class OpenTelemetryDiagnosticOptions
{
    public int MaxRecentErrors { get; set; } = 100;
}
```

### Configuration Examples

#### Development (Console Output)

```json
{
  "OpenTelemetry": {
    "Analytics": {
      "UseConsoleExporter": true,
      "SampleRate": 1.0
    },
    "Profiling": {
      "UseConsoleExporter": true
    }
  }
}
```

#### Production (OTLP to Grafana Stack)

```json
{
  "OpenTelemetry": {
    "Analytics": {
      "OtlpEndpoint": "http://otel-collector:4317",
      "PrometheusEndpoint": "http://0.0.0.0:9464/metrics/",
      "SampleRate": 0.1
    },
    "Profiling": {
      "OtlpEndpoint": "http://otel-collector:4317"
    }
  }
}
```

#### Local Development (Jaeger)

```json
{
  "OpenTelemetry": {
    "Profiling": {
      "JaegerEndpoint": "localhost"
    }
  }
}
```

## NuGet Dependencies

```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry" Version="1.7.*" />
  <PackageReference Include="OpenTelemetry.Api" Version="1.7.*" />
  <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.7.*" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.*" />
  <PackageReference Include="OpenTelemetry.Exporter.Prometheus.HttpListener" Version="1.7.*-*" />
  <PackageReference Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.*" />
</ItemGroup>
```

## Backend Setup (Docker Compose)

```yaml
version: '3.8'

services:
  # Jaeger for traces
  jaeger:
    image: jaegertracing/all-in-one:1.52
    ports:
      - '16686:16686' # UI
      - '6831:6831/udp' # Thrift compact
      - '14268:14268' # HTTP collector
    environment:
      - COLLECTOR_OTLP_ENABLED=true

  # Prometheus for metrics
  prometheus:
    image: prom/prometheus:v2.48.0
    ports:
      - '9090:9090'
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  # Grafana for visualization
  grafana:
    image: grafana/grafana:10.2.2
    ports:
      - '3000:3000'
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void TrackEvent_CreatesActivity()
{
    using var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "PigeonPea.Analytics",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        ActivityStarted = activity => Assert.Equal("TestEvent", activity.OperationName)
    };
    ActivitySource.AddActivityListener(listener);

    var service = new OpenTelemetryAnalyticsService(new OpenTelemetryAnalyticsOptions());
    service.TrackEvent("TestEvent", new Dictionary<string, object> { ["key"] = "value" });
}

[Fact]
public void BeginScope_MeasuresDuration()
{
    var service = new OpenTelemetryProfilingService(new OpenTelemetryProfilingOptions());

    using (service.BeginScope("TestScope"))
    {
        Thread.Sleep(10);
    }

    var stats = service.GetScopeStats("TestScope");
    Assert.True(stats.AverageMs >= 10);
}
```

### Integration Tests

```csharp
[Fact]
public async Task Analytics_ExportsToOtlp()
{
    // Requires running OTLP collector
    var options = new OpenTelemetryAnalyticsOptions
    {
        OtlpEndpoint = "http://localhost:4317"
    };

    var service = new OpenTelemetryAnalyticsService(options);
    service.TrackEvent("IntegrationTest");
    service.Flush();

    // Verify via Jaeger API or collector logs
}
```

## Implementation Plan

### Phase 1: Core Implementation

1. Create plugin projects with NuGet references
2. Implement `OpenTelemetryAnalyticsService`
3. Implement `OpenTelemetryProfilingService`
4. Implement `OpenTelemetryDiagnosticService`
5. Unit tests for all services

### Phase 2: Export & Configuration

1. Add OTLP exporter support
2. Add Prometheus exporter support
3. Add Jaeger exporter support
4. Configuration via appsettings.json
5. Integration tests with backends

### Phase 3: Game Integration

1. Wire up to game loop
2. Add ECS system instrumentation
3. Add frame timing
4. Performance testing

## Success Criteria

- [ ] All three services implemented with OpenTelemetry
- [ ] Console exporter works for development
- [ ] OTLP export works with collector
- [ ] Prometheus metrics scraping works
- [ ] Jaeger shows traces correctly
- [ ] < 100μs overhead per operation when enabled
- [ ] < 10ns overhead when disabled

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)
- [OTLP Protocol](https://opentelemetry.io/docs/specs/otlp/)
- [Jaeger](https://www.jaegertracing.io/)
- [Prometheus](https://prometheus.io/)
