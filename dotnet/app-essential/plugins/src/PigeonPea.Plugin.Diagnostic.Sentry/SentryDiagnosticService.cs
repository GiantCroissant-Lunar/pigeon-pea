using System.Collections.Concurrent;
using Sentry;
using PigeonPea.Diagnostic.Contracts;
using Plate.SCG.General.DisposePattern.Attributes;

namespace PigeonPea.Plugin.Diagnostic.Sentry;

/// <summary>
/// Sentry-based diagnostic service implementation with error tracking and health monitoring.
/// </summary>
[DisposePattern]
public partial class SentryDiagnosticService : PigeonPea.Contracts.Diagnostic.Services.IService
{
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
    private readonly ConcurrentQueue<ErrorReport> _recentErrors = new();
    private readonly int _maxRecentErrors;
    private readonly DateTime _startTime = DateTime.UtcNow;
    [ToBeDisposed]
    private readonly IDisposable? _sentryDisposable;
    private readonly SentryDiagnosticOptions _options;

    public SentryDiagnosticService(SentryDiagnosticOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _maxRecentErrors = options.MaxRecentErrors;

        if (!string.IsNullOrEmpty(options.Dsn))
        {
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
        }

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
        var report = new ErrorReport
        {
            Timestamp = DateTime.UtcNow,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Context = context?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        _recentErrors.Enqueue(report);

        // Trim old errors
        while (_recentErrors.Count > _maxRecentErrors)
        {
            _recentErrors.TryDequeue(out _);
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
        _healthChecks[name] = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
    }

    public IReadOnlyList<ErrorReport> GetRecentErrors(int maxCount = 10)
    {
        return _recentErrors.TakeLast(maxCount).ToList();
    }

    partial void DisposeManagedResources()
    {
        SentrySdk.Flush(TimeSpan.FromSeconds(2));
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
