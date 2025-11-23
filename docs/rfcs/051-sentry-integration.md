---
doc_id: RFC-00051
title: 'Sentry Integration: Error Tracking and Performance Monitoring'
doc_type: rfc
status: draft
canonical: true
created: '2025-11-22'
tags:
  - error-tracking
  - sentry
  - diagnostics
  - profiling
  - monitoring
  - plugins
summary: Sentry-based plugin implementations for Diagnostic and Profiling services with error tracking, breadcrumbs, and performance monitoring
related:
  - RFC-00049
  - RFC-00050
---

# RFC: Sentry Integration

- **Status:** Draft
- **Date:** 2025-11-22
- **Related:** RFC-00049 (Profiling Service), RFC-00050 (OpenTelemetry Integration)

## Summary

This RFC defines Sentry-based plugin implementations for the Diagnostic and Profiling service contracts. Sentry provides:

- **Error tracking** with stack traces, breadcrumbs, and context
- **Performance monitoring** with transactions and spans
- **Release tracking** and deployment visibility
- **User feedback** collection
- **Native .NET SDK** with excellent integration

This complements RFC-00050 (OpenTelemetry) by providing specialized error tracking and user-facing monitoring capabilities.

## Motivation

### Why Sentry?

| Benefit           | Description                                         |
| ----------------- | --------------------------------------------------- |
| **Error-focused** | Purpose-built for error tracking with rich context  |
| **Breadcrumbs**   | Automatic capture of events leading to errors       |
| **User context**  | Associate errors with users and sessions            |
| **Performance**   | Transaction-based performance monitoring            |
| **Releases**      | Track errors by version and deployment              |
| **Alerts**        | Built-in alerting and notification system           |
| **Dashboard**     | Production-ready UI without self-hosting complexity |

### Sentry vs OpenTelemetry

| Aspect            | Sentry                | OpenTelemetry            |
| ----------------- | --------------------- | ------------------------ |
| **Primary focus** | Error tracking        | Distributed tracing      |
| **Best for**      | Production monitoring | Debugging & optimization |
| **User context**  | First-class support   | Manual tagging           |
| **Breadcrumbs**   | Automatic             | Manual events            |
| **Hosting**       | SaaS (or self-hosted) | Self-hosted backends     |
| **Cost**          | Free tier + paid      | Free (backend costs)     |

**Recommendation:** Use both - Sentry for production error monitoring, OpenTelemetry for development profiling and traces.

### Goals

1. **Implement Diagnostic service** with Sentry error tracking
2. **Implement Profiling service** with Sentry performance monitoring
3. **Automatic breadcrumbs** from game events
4. **Release tracking** integration
5. **User identification** for error context

## Architecture

### Plugin Structure

```
dotnet/app-essential/plugins/
└── src/
    ├── PigeonPea.Plugin.Diagnostic.Sentry/
    │   ├── SentryDiagnosticService.cs
    │   ├── DiagnosticPlugin.cs
    │   ├── SentryHealthCheck.cs
    │   └── plugin.json
    └── PigeonPea.Plugin.Profiling.Sentry/
        ├── SentryProfilingService.cs
        ├── ProfilingPlugin.cs
        └── plugin.json
```

### Sentry Mapping

| Service Contract                      | Sentry Feature                 |
| ------------------------------------- | ------------------------------ |
| `IDiagnosticService.ReportError()`    | `SentrySdk.CaptureException()` |
| `IDiagnosticService.ReportWarning()`  | Breadcrumb with level=warning  |
| `IDiagnosticService.CreateSnapshot()` | Custom event with diagnostics  |
| `IProfilingService.BeginScope()`      | `ISpan` / `ITransaction`       |
| `IProfilingService.RecordMarker()`    | Breadcrumb                     |

## Service Implementations

### Diagnostic Service (Sentry)

```csharp
using Sentry;
using PigeonPea.Contracts.Diagnostic.Services;

namespace PigeonPea.Plugin.Diagnostic.Sentry;

public class SentryDiagnosticService : IService, IDisposable
{
    private readonly Dictionary<string, IHealthCheck> _healthChecks = new();
    private readonly List<ErrorReport> _recentErrors = new();
    private readonly int _maxRecentErrors;
    private readonly object _lock = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly IDisposable _sentryDisposable;
    private readonly SentryDiagnosticOptions _options;

    public SentryDiagnosticService(SentryDiagnosticOptions options)
    {
        _options = options;
        _maxRecentErrors = options.MaxRecentErrors;

        _sentryDisposable = SentrySdk.Init(o =>
        {
            o.Dsn = options.Dsn;
            o.Environment = options.Environment ?? "development";
            o.Release = options.Release;
            o.Debug = options.Debug;
            o.TracesSampleRate = options.TracesSampleRate;
            o.AutoSessionTracking = options.AutoSessionTracking;

            // Attach diagnostic info to all events
            o.SetBeforeSend((sentryEvent, hint) =>
            {
                sentryEvent.SetExtra("uptime_seconds", (DateTime.UtcNow - _startTime).TotalSeconds);
                sentryEvent.SetExtra("memory_bytes", GC.GetTotalMemory(false));
                return sentryEvent;
            });

            // Configure breadcrumbs
            o.MaxBreadcrumbs = options.MaxBreadcrumbs;
        });

        // Register built-in health checks
        RegisterHealthCheck("sentry", new SentryHealthCheck());
        RegisterHealthCheck("memory", new MemoryHealthCheck(options.MemoryWarningThreshold));
    }

    public HealthCheckResult CheckHealth()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
                    Description = $"Check failed: {ex.Message}",
                    Exception = ex,
                    Duration = TimeSpan.Zero
                };
                overallStatus = HealthStatus.Unhealthy;

                // Report to Sentry as non-fatal
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("health_check", name);
                    scope.Level = SentryLevel.Warning;
                });
            }
        }

        stopwatch.Stop();

        // Record health check as breadcrumb
        SentrySdk.AddBreadcrumb(
            message: $"Health check: {overallStatus}",
            category: "health",
            level: overallStatus == HealthStatus.Healthy ? BreadcrumbLevel.Info : BreadcrumbLevel.Warning
        );

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

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
        // Capture to Sentry
        SentrySdk.CaptureException(exception, scope =>
        {
            if (context != null)
            {
                foreach (var (key, value) in context)
                {
                    scope.SetExtra(key, value);
                }
            }
        });

        // Store locally
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

            while (_recentErrors.Count > _maxRecentErrors)
            {
                _recentErrors.RemoveAt(0);
            }
        }
    }

    public void ReportWarning(string message, IDictionary<string, object>? context = null)
    {
        // Add as breadcrumb
        SentrySdk.AddBreadcrumb(
            message: message,
            category: "warning",
            level: BreadcrumbLevel.Warning,
            data: context?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
        );

        // Also capture as message if configured
        if (_options.CaptureWarningsAsEvents)
        {
            SentrySdk.CaptureMessage(message, SentryLevel.Warning);
        }
    }

    public SystemStatus GetSystemStatus()
    {
        var healthResult = CheckHealth();
        var process = System.Diagnostics.Process.GetCurrentProcess();

        return new SystemStatus
        {
            Version = _options.Release ?? "unknown",
            Uptime = DateTime.UtcNow - _startTime,
            MemoryUsedBytes = GC.GetTotalMemory(false),
            ThreadCount = process.Threads.Count,
            PluginCount = _healthChecks.Count,
            HealthStatus = healthResult.Status,
            StartTime = _startTime
        };
    }

    public IReadOnlyDictionary<string, object> GetDiagnosticInfo()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();

        return new Dictionary<string, object>
        {
            ["sentry.dsn_configured"] = !string.IsNullOrEmpty(_options.Dsn),
            ["sentry.environment"] = _options.Environment ?? "development",
            ["sentry.release"] = _options.Release ?? "unknown",
            ["runtime"] = Environment.Version.ToString(),
            ["os"] = Environment.OSVersion.ToString(),
            ["processors"] = Environment.ProcessorCount,
            ["memory.working_set"] = process.WorkingSet64,
            ["memory.gc_total"] = GC.GetTotalMemory(false),
            ["gc.gen0"] = GC.CollectionCount(0),
            ["gc.gen1"] = GC.CollectionCount(1),
            ["gc.gen2"] = GC.CollectionCount(2),
            ["uptime_seconds"] = (DateTime.UtcNow - _startTime).TotalSeconds
        };
    }

    public DiagnosticSnapshot CreateSnapshot()
    {
        var snapshot = new DiagnosticSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SystemStatus = GetSystemStatus(),
            HealthCheck = CheckHealth(),
            RecentErrors = GetRecentErrors(),
            Data = GetDiagnosticInfo()
        };

        // Send snapshot to Sentry as custom event
        SentrySdk.CaptureMessage("Diagnostic Snapshot", scope =>
        {
            scope.Level = SentryLevel.Info;
            scope.SetExtra("snapshot", snapshot);
        });

        return snapshot;
    }

    public void RegisterHealthCheck(string name, IHealthCheck healthCheck)
    {
        _healthChecks[name] = healthCheck;
    }

    public IReadOnlyList<ErrorReport> GetRecentErrors(int maxCount = 10)
    {
        lock (_lock)
        {
            return _recentErrors.TakeLast(maxCount).ToList();
        }
    }

    public void Dispose()
    {
        SentrySdk.Flush(TimeSpan.FromSeconds(2));
        _sentryDisposable.Dispose();
    }
}

/// <summary>
/// Health check that verifies Sentry connectivity.
/// </summary>
public class SentryHealthCheck : IHealthCheck
{
    public HealthCheckEntry Check()
    {
        var isEnabled = SentrySdk.IsEnabled;

        return new HealthCheckEntry
        {
            Status = isEnabled ? HealthStatus.Healthy : HealthStatus.Degraded,
            Description = isEnabled ? "Sentry SDK is enabled" : "Sentry SDK is not enabled",
            Duration = TimeSpan.Zero
        };
    }
}

/// <summary>
/// Health check for memory usage.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _warningThresholdBytes;

    public MemoryHealthCheck(long warningThresholdBytes = 500_000_000) // 500 MB default
    {
        _warningThresholdBytes = warningThresholdBytes;
    }

    public HealthCheckEntry Check()
    {
        var memory = GC.GetTotalMemory(false);
        var status = memory < _warningThresholdBytes ? HealthStatus.Healthy : HealthStatus.Degraded;

        return new HealthCheckEntry
        {
            Status = status,
            Description = $"Memory: {memory / 1_000_000} MB",
            Duration = TimeSpan.Zero,
            Data = new Dictionary<string, object>
            {
                ["bytes"] = memory,
                ["threshold"] = _warningThresholdBytes
            }
        };
    }
}
```

### Profiling Service (Sentry)

```csharp
using Sentry;
using PigeonPea.Contracts.Profiling.Services;

namespace PigeonPea.Plugin.Profiling.Sentry;

public class SentryProfilingService : IService
{
    private ProfilerMode _mode = ProfilerMode.Instrumentation;
    private bool _isCapturing;
    private readonly List<ProfileEvent> _capturedEvents = new();
    private readonly object _lock = new();
    private long _frameNumber;
    private readonly System.Diagnostics.Stopwatch _frameStopwatch = new();
    private readonly Dictionary<string, ScopeStatsAccumulator> _scopeStats = new();
    private ITransaction? _currentFrameTransaction;
    private readonly SentryProfilingOptions _options;

    public SentryProfilingService(SentryProfilingOptions options)
    {
        _options = options;
    }

    public IProfileScope BeginScope(string name)
    {
        return BeginScope(name, "default");
    }

    public IProfileScope BeginScope(string name, string category)
    {
        if (_mode == ProfilerMode.Disabled)
            return NullProfileScope.Instance;

        // Create as child span if there's an active transaction
        var parentSpan = SentrySdk.GetSpan();
        ISpan? span = null;

        if (parentSpan != null)
        {
            span = parentSpan.StartChild(name);
            span.SetTag("category", category);
        }
        else if (_options.CreateTransactionsForOrphanScopes)
        {
            // Create a transaction for top-level scopes
            var transaction = SentrySdk.StartTransaction(name, category);
            span = transaction;
        }

        // Always add breadcrumb
        SentrySdk.AddBreadcrumb(
            message: $"Begin: {name}",
            category: category,
            level: BreadcrumbLevel.Debug
        );

        return new SentryProfileScope(span, name, category, this);
    }

    public void RecordMarker(string name)
    {
        if (_mode == ProfilerMode.Disabled) return;

        SentrySdk.AddBreadcrumb(
            message: name,
            category: "marker",
            level: BreadcrumbLevel.Info
        );

        // Also add as span event if there's an active span
        var span = SentrySdk.GetSpan();
        span?.SetExtra($"marker.{name}", DateTime.UtcNow);
    }

    public void RecordCounter(string name, double value)
    {
        if (_mode == ProfilerMode.Disabled) return;

        // Add to current span as extra data
        var span = SentrySdk.GetSpan();
        span?.SetExtra($"counter.{name}", value);

        // Also as breadcrumb for visibility
        SentrySdk.AddBreadcrumb(
            message: $"{name}: {value}",
            category: "counter",
            level: BreadcrumbLevel.Debug
        );
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
        // Sentry doesn't have category filtering, use sampling instead
    }

    public void SetSampleRate(int samplesPerSecond)
    {
        // Configure via SentryOptions.TracesSampleRate
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
        // Finish frame transaction if exists
        _currentFrameTransaction?.Finish();

        _frameNumber++;
        _frameStopwatch.Restart();

        // Start new frame transaction if configured
        if (_options.TrackFramesAsTransactions && _mode != ProfilerMode.Disabled)
        {
            _currentFrameTransaction = SentrySdk.StartTransaction(
                $"Frame {_frameNumber}",
                "game.frame"
            );
        }
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
}

internal class SentryProfileScope : IProfileScope
{
    private readonly ISpan? _span;
    private readonly string _name;
    private readonly string _category;
    private readonly SentryProfilingService _service;
    private readonly System.Diagnostics.Stopwatch _stopwatch;

    public SentryProfileScope(
        ISpan? span,
        string name,
        string category,
        SentryProfilingService service)
    {
        _span = span;
        _name = name;
        _category = category;
        _service = service;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    }

    public void AddMetadata(string key, string value)
    {
        _span?.SetTag(key, value);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var durationMs = _stopwatch.Elapsed.TotalMilliseconds;

        _service.RecordScopeTime(_name, _category, durationMs);

        _span?.SetExtra("duration_ms", durationMs);
        _span?.Finish();

        SentrySdk.AddBreadcrumb(
            message: $"End: {_name} ({durationMs:F2}ms)",
            category: _category,
            level: BreadcrumbLevel.Debug
        );
    }
}

internal class NullProfileScope : IProfileScope
{
    public static readonly NullProfileScope Instance = new();
    public void AddMetadata(string key, string value) { }
    public void Dispose() { }
}
```

## Configuration

### Options Classes

```csharp
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

public class SentryProfilingOptions
{
    /// <summary>
    /// Create transactions for scopes without a parent.
    /// </summary>
    public bool CreateTransactionsForOrphanScopes { get; set; } = true;

    /// <summary>
    /// Track each frame as a Sentry transaction.
    /// Warning: High volume in production.
    /// </summary>
    public bool TrackFramesAsTransactions { get; set; }
}
```

### Configuration Examples

#### Development

```json
{
  "Sentry": {
    "Diagnostic": {
      "Dsn": "https://your-dsn@o123.ingest.sentry.io/456",
      "Environment": "development",
      "Debug": true,
      "TracesSampleRate": 1.0,
      "CaptureWarningsAsEvents": true
    },
    "Profiling": {
      "CreateTransactionsForOrphanScopes": true,
      "TrackFramesAsTransactions": false
    }
  }
}
```

#### Production

```json
{
  "Sentry": {
    "Diagnostic": {
      "Dsn": "https://your-dsn@o123.ingest.sentry.io/456",
      "Environment": "production",
      "Release": "1.0.0",
      "Debug": false,
      "TracesSampleRate": 0.1,
      "AutoSessionTracking": true,
      "CaptureWarningsAsEvents": false
    },
    "Profiling": {
      "CreateTransactionsForOrphanScopes": false,
      "TrackFramesAsTransactions": false
    }
  }
}
```

## NuGet Dependencies

```xml
<ItemGroup>
  <PackageReference Include="Sentry" Version="4.*" />
</ItemGroup>
```

## User Context Integration

```csharp
// Set user context when player logs in
public void OnPlayerLogin(string playerId, string playerName)
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.User = new SentryUser
        {
            Id = playerId,
            Username = playerName
        };
    });
}

// Add game context to all events
public void OnGameStateChange(string state)
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.SetTag("game.state", state);
    });
}

// Add character context
public void OnCharacterSelected(string characterId, string characterClass)
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.SetExtra("character.id", characterId);
        scope.SetExtra("character.class", characterClass);
    });
}
```

## Game-Specific Breadcrumbs

```csharp
// Automatically track important game events as breadcrumbs
public class GameEventBreadcrumbHandler
{
    public void OnLevelLoaded(string levelName)
    {
        SentrySdk.AddBreadcrumb(
            message: $"Level loaded: {levelName}",
            category: "navigation",
            level: BreadcrumbLevel.Info
        );
    }

    public void OnCombatStart(int enemyCount)
    {
        SentrySdk.AddBreadcrumb(
            message: $"Combat started with {enemyCount} enemies",
            category: "gameplay",
            level: BreadcrumbLevel.Info
        );
    }

    public void OnItemPickup(string itemId, string itemName)
    {
        SentrySdk.AddBreadcrumb(
            message: $"Picked up: {itemName}",
            category: "inventory",
            level: BreadcrumbLevel.Debug,
            data: new Dictionary<string, string> { ["item_id"] = itemId }
        );
    }

    public void OnPlayerDeath(string cause)
    {
        SentrySdk.AddBreadcrumb(
            message: $"Player died: {cause}",
            category: "gameplay",
            level: BreadcrumbLevel.Warning
        );
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void ReportError_CapturesException()
{
    var options = new SentryDiagnosticOptions { Dsn = TestDsn };
    var service = new SentryDiagnosticService(options);

    var exception = new InvalidOperationException("Test error");
    service.ReportError(exception, new Dictionary<string, object>
    {
        ["custom_key"] = "custom_value"
    });

    // Verify via Sentry test project or mock
}

[Fact]
public void BeginScope_CreatesSpan()
{
    var options = new SentryProfilingOptions();
    var service = new SentryProfilingService(options);

    using (service.BeginScope("TestScope", "test"))
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
public async Task Diagnostic_SendsToSentry()
{
    var options = new SentryDiagnosticOptions
    {
        Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN"),
        Environment = "test"
    };

    using var service = new SentryDiagnosticService(options);

    service.ReportError(new Exception("Integration test error"));

    // Wait for flush
    await Task.Delay(3000);

    // Verify in Sentry dashboard
}
```

## Comparison with OpenTelemetry

| Feature        | Sentry Plugin | OpenTelemetry Plugin |
| -------------- | ------------- | -------------------- |
| Error tracking | Excellent     | Basic (logs)         |
| Breadcrumbs    | Automatic     | Manual               |
| User context   | First-class   | Manual tags          |
| Performance    | Transactions  | Traces               |
| Dashboards     | Built-in      | Need Grafana/Jaeger  |
| Alerting       | Built-in      | Need external        |
| Self-hosted    | Optional      | Required             |

**Recommendation**: Use Sentry for production monitoring and OpenTelemetry for development profiling.

## Implementation Plan

### Phase 1: Diagnostic Service

1. Create `PigeonPea.Plugin.Diagnostic.Sentry` project
2. Implement `SentryDiagnosticService`
3. Implement built-in health checks
4. Unit tests

### Phase 2: Profiling Service

1. Create `PigeonPea.Plugin.Profiling.Sentry` project
2. Implement `SentryProfilingService`
3. Implement span-based scopes
4. Unit tests

### Phase 3: Game Integration

1. Add breadcrumb handlers for game events
2. User context integration
3. Release tracking
4. Performance testing

## Success Criteria

- [ ] Errors appear in Sentry dashboard with full context
- [ ] Breadcrumbs show event trail leading to errors
- [ ] User context correctly associated with events
- [ ] Performance transactions visible in Sentry Performance
- [ ] Health checks report to Sentry
- [ ] < 1ms overhead per error capture
- [ ] < 100μs overhead per breadcrumb

## References

- [Sentry .NET SDK](https://docs.sentry.io/platforms/dotnet/)
- [Sentry Performance Monitoring](https://docs.sentry.io/product/performance/)
- [Sentry Breadcrumbs](https://docs.sentry.io/product/issues/issue-details/breadcrumbs/)
- [Sentry Releases](https://docs.sentry.io/product/releases/)
