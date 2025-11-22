using System.Collections.Concurrent;
using Sentry;
using PigeonPea.Contracts.Profiling.Services;
using PigeonPea.Plugins.Profiling.Basic.Export;
using PigeonPea.Plugins.Profiling.Basic.Internal;

namespace PigeonPea.Plugins.Profiling.Sentry;

/// <summary>
/// Sentry-based profiling service implementation with performance monitoring and transaction tracking.
/// </summary>
public class SentryProfilingService : PigeonPea.Contracts.Profiling.Services.IService
{
    private ProfilerMode _mode = ProfilerMode.Instrumentation;
    private bool _isCapturing;
    private readonly ConcurrentQueue<ProfileEvent> _capturedEvents = new();
    private long _frameNumber;
    private readonly System.Diagnostics.Stopwatch _frameStopwatch = new();
    private readonly ConcurrentDictionary<string, ScopeStatsAccumulator> _scopeStats = new();
    private ITransaction? _currentFrameTransaction;
    private readonly SentryProfilingOptions _options;

    public SentryProfilingService(SentryProfilingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
            span.Operation = _options.DefaultOperation;
        }
        else if (_options.CreateTransactionsForOrphanScopes)
        {
            // Create a transaction for top-level scopes
            var transaction = SentrySdk.StartTransaction(name, _options.DefaultOperation);
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
        if (_options.IncludeMarkersAsSpanEvents && span != null)
        {
            span.SetExtra($"marker.{name}", DateTime.UtcNow);
        }
    }

    public void RecordCounter(string name, double value)
    {
        if (_mode == ProfilerMode.Disabled) return;

        // Add to current span as extra data
        var span = SentrySdk.GetSpan();
        if (_options.IncludeCountersAsSpanData && span != null)
        {
            span.SetExtra($"counter.{name}", value);
        }

        // Also as breadcrumb for visibility
        SentrySdk.AddBreadcrumb(
            message: $"{name}: {value}",
            category: "counter",
            level: BreadcrumbLevel.Debug
        );
    }

    public void StartCapture()
    {
        _isCapturing = true;
    }

    public ProfileCapture StopCapture()
    {
        _isCapturing = false;
        var events = _capturedEvents.ToArray();
        var startTime = events.Length > 0 ? events[0].Timestamp : DateTime.UtcNow;

        return new ProfileCapture
        {
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            FrameCount = (int)_frameNumber,
            EventCount = events.Length
        };
    }

    public void ClearCapture()
    {
        while (_capturedEvents.TryDequeue(out _)) { }
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
        // This is a no-op for Sentry implementation
    }

    public void SetSampleRate(int samplesPerSecond)
    {
        // Configure via SentryOptions.TracesSampleRate
        // This is a no-op for Sentry implementation
    }

    public void ExportToSpeedscope(string filePath)
    {
        var exporter = new SpeedscopeExporter();
        exporter.Export(_capturedEvents.ToList(), filePath);
    }

    public void ExportToChromeTrace(string filePath)
    {
        var exporter = new ChromeTraceExporter();
        exporter.Export(_capturedEvents.ToList(), filePath);
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
            default:
                throw new NotSupportedException($"Export format {format} not supported");
        }
    }

    private void ExportToJson(string filePath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_capturedEvents.ToList(), new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
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

    public void InstrumentWorld(object world)
    {
        // ECS world instrumentation would be implemented here
        // This is a placeholder for future implementation
    }

    public IReadOnlyList<SystemStats> GetSystemReport(object world)
    {
        // System report would be implemented here
        // This is a placeholder for future implementation
        return Array.Empty<SystemStats>();
    }

    public void EnableOverlay(OverlayConfig config)
    {
        // Overlay would be implemented here
        // This is a placeholder for future implementation
    }

    public void DisableOverlay()
    {
        // Overlay would be implemented here
        // This is a placeholder for future implementation
    }

    public bool IsOverlayEnabled => false;

    public void SetTrigger(IProfileTrigger trigger)
    {
        // Trigger implementation would be here
        // This is a placeholder for future implementation
    }

    public void ClearTriggers()
    {
        // Trigger implementation would be here
        // This is a placeholder for future implementation
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
        if (!_scopeStats.TryGetValue(name, out var accumulator))
        {
            accumulator = new ScopeStatsAccumulator(name, category);
            _scopeStats[name] = accumulator;
        }
        accumulator.Record(durationMs);

        if (_isCapturing)
        {
            _capturedEvents.Enqueue(new ProfileEvent
            {
                Name = name,
                Category = category,
                DurationMs = durationMs,
                Timestamp = DateTime.UtcNow
            });

            // Trim captured events if too many
            while (_capturedEvents.Count > 10000)
            {
                _capturedEvents.TryDequeue(out _);
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
    private NullProfileScope() { }
    public void AddMetadata(string key, string value) { }
    public void Dispose() { }
}
