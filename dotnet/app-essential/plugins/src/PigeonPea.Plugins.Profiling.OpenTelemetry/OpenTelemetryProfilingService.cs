using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using PigeonPea.Profiling.Contracts;
using PigeonPea.Plugins.Profiling.OpenTelemetry.Export;

namespace PigeonPea.Plugins.Profiling.OpenTelemetry;

/// <summary>
/// OpenTelemetry-based profiling service implementation.
/// </summary>
public class OpenTelemetryProfilingService : IService, IDisposable
{
    private static readonly ActivitySource ActivitySource =
        new("PigeonPea.Profiling", "1.0.0");

    private static readonly Meter Meter =
        new("PigeonPea.Profiling", "1.0.0");

    private readonly ConcurrentDictionary<string, ProfileScopeImpl> _activeScopes = new();
    private readonly ConcurrentDictionary<string, FrameStats> _frameStats = new();
    private readonly List<ProfileEvent> _events = new();
    private readonly object _lock = new();
    private readonly ILogger<OpenTelemetryProfilingService>? _logger;
    private readonly OpenTelemetryProfilingOptions _options;

    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;
    private bool _isCapturing;
    private ProfilerMode _mode = ProfilerMode.Instrumentation;
    private readonly HashSet<string> _enabledCategories = new();
    private readonly object _categoriesLock = new();
    private int _sampleRate = 60;
    private long _frameNumber;
    private DateTime _captureStartTime;

    private readonly Histogram<double> _durationHistogram;
    private readonly Counter<long> _eventCounter;

    /// <summary>
    /// Initializes a new instance of OpenTelemetryProfilingService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Configuration options.</param>
    public OpenTelemetryProfilingService(
        ILogger<OpenTelemetryProfilingService>? logger = null,
        OpenTelemetryProfilingOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new OpenTelemetryProfilingOptions();

        _durationHistogram = Meter.CreateHistogram<double>("profiling.duration", "ms", "Duration of profiling operations");
        _eventCounter = Meter.CreateCounter<long>("profiling.events.total", "count", "Total profiling events");

        ConfigureProviders();
    }

    private void ConfigureProviders()
    {
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .AddSource("PigeonPea.Profiling")
            .SetSampler(new TraceIdRatioBasedSampler(1.0)); // Always sample for profiling

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .AddMeter("PigeonPea.Profiling");

        if (_options.UseConsoleExporter)
        {
            tracerBuilder.AddConsoleExporter();
            meterBuilder.AddConsoleExporter();
        }

        if (!string.IsNullOrEmpty(_options.OtlpEndpoint))
        {
            tracerBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(_options.OtlpEndpoint));
            meterBuilder.AddOtlpExporter(o => o.Endpoint = new Uri(_options.OtlpEndpoint));
        }

        if (!string.IsNullOrEmpty(_options.JaegerEndpoint))
        {
            tracerBuilder.AddJaegerExporter(o => o.AgentHost = _options.JaegerEndpoint);
        }

        _tracerProvider = tracerBuilder.Build();
        _meterProvider = meterBuilder.Build();
    }

    /// <inheritdoc />
    public IProfileScope BeginScope(string name)
    {
        return BeginScope(name, "Default");
    }

    /// <inheritdoc />
    public IProfileScope BeginScope(string name, string category)
    {
        if (!IsCategoryEnabled(category))
            return new NoOpProfileScope();

        var scopeId = Guid.NewGuid().ToString("N")[..8];
        var scope = new ProfileScopeImpl(scopeId, name, category);

        _activeScopes[scopeId] = scope;

        var activity = ActivitySource.StartActivity($"Scope: {name}", ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("scope.id", scopeId);
            activity.SetTag("scope.name", name);
            activity.SetTag("scope.category", category);
        }

        scope.Activity = activity;

        _logger?.LogDebug("Started profiling scope: {ScopeId} - {ScopeName} ({Category})", scopeId, name, category);
        _eventCounter.Add(1, new KeyValuePair<string, object?>("event.type", "scope.start"));

        return scope;
    }

    /// <inheritdoc />
    public void RecordMarker(string name)
    {
        using var activity = ActivitySource.StartActivity($"Marker: {name}", ActivityKind.Internal);
        activity?.SetTag("marker.name", name);

        lock (_lock)
        {
            _events.Add(new ProfileEvent
            {
                Name = name,
                Type = ProfileEventType.Marker,
                Timestamp = DateTime.UtcNow,
                FrameNumber = _frameNumber
            });
        }

        _logger?.LogDebug("Recorded profiling marker: {MarkerName}", name);
        _eventCounter.Add(1, new KeyValuePair<string, object?>("event.type", "marker"));
    }

    /// <inheritdoc />
    public void RecordCounter(string name, double value)
    {
        using var activity = ActivitySource.StartActivity($"Counter: {name}", ActivityKind.Internal);
        activity?.SetTag("counter.name", name);
        activity?.SetTag("counter.value", value);

        lock (_lock)
        {
            _events.Add(new ProfileEvent
            {
                Name = name,
                Type = ProfileEventType.Counter,
                Timestamp = DateTime.UtcNow,
                FrameNumber = _frameNumber,
                Value = value
            });
        }

        _logger?.LogDebug("Recorded profiling counter: {CounterName} = {Value}", name, value);
        _eventCounter.Add(1, new KeyValuePair<string, object?>("event.type", "counter"));
    }

    /// <inheritdoc />
    public void StartCapture()
    {
        if (_isCapturing) return;

        _isCapturing = true;
        _captureStartTime = DateTime.UtcNow;
        _frameNumber = 0;

        lock (_lock)
        {
            _events.Clear();
            _frameStats.Clear();
        }

        _logger?.LogInformation("Started profiling capture");
    }

    /// <inheritdoc />
    public ProfileCapture StopCapture()
    {
        if (!_isCapturing)
            return new ProfileCapture { StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow };

        _isCapturing = false;
        var endTime = DateTime.UtcNow;

        lock (_lock)
        {
            var capture = new ProfileCapture
            {
                StartTime = _captureStartTime,
                EndTime = endTime,
                FrameCount = (int)_frameNumber,
                EventCount = _events.Count
            };

            _logger?.LogInformation("Stopped profiling capture: {FrameCount} frames, {EventCount} events",
                capture.FrameCount, capture.EventCount);

            return capture;
        }
    }

    /// <inheritdoc />
    public void ClearCapture()
    {
        lock (_lock)
        {
            _events.Clear();
            _frameStats.Clear();
        }

        _logger?.LogInformation("Cleared profiling capture data");
    }

    /// <inheritdoc />
    public bool IsCapturing => _isCapturing;

    /// <inheritdoc />
    public void SetMode(ProfilerMode mode)
    {
        _mode = mode;
        _logger?.LogInformation("Set profiling mode to {Mode}", mode);
    }

    /// <inheritdoc />
    public ProfilerMode Mode => _mode;

    /// <inheritdoc />
    public void SetCategoryEnabled(string category, bool enabled)
    {
        lock (_categoriesLock)
        {
            if (enabled)
                _enabledCategories.Add(category);
            else
                _enabledCategories.Remove(category);
        }

        _logger?.LogDebug("Set category {Category} enabled: {Enabled}", category, enabled);
    }

    /// <inheritdoc />
    public void SetSampleRate(int samplesPerSecond)
    {
        _sampleRate = samplesPerSecond;
        _logger?.LogInformation("Set sample rate to {SamplesPerSecond} Hz", samplesPerSecond);
    }

    /// <inheritdoc />
    public void ExportToSpeedscope(string filePath)
    {
        Export(filePath, ProfileExportFormat.Speedscope);
    }

    /// <inheritdoc />
    public void ExportToChromeTrace(string filePath)
    {
        Export(filePath, ProfileExportFormat.ChromeTrace);
    }

    /// <inheritdoc />
    public void Export(string filePath, ProfileExportFormat format)
    {
        lock (_lock)
        {
            var profileEvents = ConvertToProfileEvents(_events);

            switch (format)
            {
                case ProfileExportFormat.Speedscope:
                    var speedscopeExporter = new SpeedscopeExporter();
                    speedscopeExporter.Export(profileEvents, filePath);
                    break;

                case ProfileExportFormat.ChromeTrace:
                    var chromeExporter = new ChromeTraceExporter();
                    chromeExporter.Export(profileEvents, filePath);
                    break;

                default:
                    throw new NotSupportedException($"Export format {format} is not supported");
            }
        }

        _logger?.LogInformation("Exported profiling data to {FilePath} in {Format} format", filePath, format);
    }

    /// <inheritdoc />
    public FrameStats GetCurrentFrameStats()
    {
        lock (_lock)
        {
            return _frameStats.Values.LastOrDefault() ?? new FrameStats { FrameNumber = _frameNumber };
        }
    }

    /// <inheritdoc />
    public ScopeStats GetScopeStats(string scopeName, int frameCount = 60)
    {
        lock (_lock)
        {
            var recentFrames = _frameStats.Values.TakeLast(frameCount).ToList();
            var scopeTimes = recentFrames
                .Where(f => f.ScopeTimesMs.ContainsKey(scopeName))
                .Select(f => f.ScopeTimesMs[scopeName])
                .ToList();

            if (!scopeTimes.Any())
                return new ScopeStats { Name = scopeName };

            return new ScopeStats
            {
                Name = scopeName,
                SampleCount = scopeTimes.Count,
                AverageMs = scopeTimes.Average(),
                MinMs = scopeTimes.Min(),
                MaxMs = scopeTimes.Max(),
                TotalMs = scopeTimes.Sum(),
                P95Ms = CalculatePercentile(scopeTimes, 0.95),
                P99Ms = CalculatePercentile(scopeTimes, 0.99)
            };
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ScopeStats> GetAllScopeStats(int frameCount = 60)
    {
        lock (_lock)
        {
            var recentFrames = _frameStats.Values.TakeLast(frameCount).ToList();
            var allScopeNames = recentFrames
                .SelectMany(f => f.ScopeTimesMs.Keys)
                .Distinct()
                .ToList();

            return allScopeNames
                .Select(name => GetScopeStats(name, frameCount))
                .OrderByDescending(s => s.TotalMs)
                .ToList();
        }
    }

    /// <inheritdoc />
    public void InstrumentWorld(object world)
    {
        _logger?.LogInformation("World instrumentation not implemented in OpenTelemetry profiling service");
    }

    /// <inheritdoc />
    public IReadOnlyList<SystemStats> GetSystemReport(object world)
    {
        _logger?.LogInformation("System report not implemented in OpenTelemetry profiling service");
        return new List<SystemStats>();
    }

    /// <inheritdoc />
    public void EnableOverlay(OverlayConfig config)
    {
        _logger?.LogInformation("Debug overlay not implemented in OpenTelemetry profiling service");
    }

    /// <inheritdoc />
    public void DisableOverlay()
    {
        _logger?.LogInformation("Debug overlay not implemented in OpenTelemetry profiling service");
    }

    /// <inheritdoc />
    public bool IsOverlayEnabled => false;

    /// <inheritdoc />
    public void SetTrigger(IProfileTrigger trigger)
    {
        _logger?.LogInformation("Triggers not implemented in OpenTelemetry profiling service");
    }

    /// <inheritdoc />
    public void ClearTriggers()
    {
        _logger?.LogInformation("Triggers not implemented in OpenTelemetry profiling service");
    }

    /// <inheritdoc />
    public void EndFrame()
    {
        _frameNumber++;

        // This is a simplified implementation
        // In a real implementation, you would track actual frame timing
        var frameStats = new FrameStats
        {
            FrameNumber = _frameNumber,
            FrameTimeMs = 16.67, // Assume 60 FPS
            ScopeTimesMs = new Dictionary<string, double>()
        };

        lock (_lock)
        {
            _frameStats[_frameNumber.ToString()] = frameStats;

            // Keep only recent frames
            if (_frameStats.Count > 1000)
            {
                var oldestKeys = _frameStats.Keys.Take(100).ToList();
                foreach (var key in oldestKeys)
                {
                    _frameStats.TryRemove(key, out _);
                }
            }
        }
    }

    private bool IsCategoryEnabled(string category)
    {
        lock (_categoriesLock)
        {
            return _enabledCategories.Count == 0 || _enabledCategories.Contains(category);
        }
    }

    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (!values.Any()) return 0;

        var sorted = values.OrderBy(x => x).ToList();
        var index = (int)(percentile * (sorted.Count - 1));
        return sorted[Math.Min(index, sorted.Count - 1)];
    }

    private IReadOnlyList<PigeonPea.Contracts.Profiling.Services.ProfileEvent> ConvertToProfileEvents(List<ProfileEvent> internalEvents)
    {
        return internalEvents.Select(e => new PigeonPea.Contracts.Profiling.Services.ProfileEvent
        {
            Name = e.Name,
            Type = e.Type switch
            {
                ProfileEventType.Marker => EventType.Marker,
                ProfileEventType.Counter => EventType.Counter,
                ProfileEventType.Scope => EventType.ScopeBegin,
                _ => EventType.Marker
            },
            TimestampTicks = e.Timestamp.Ticks,
            ThreadId = Environment.CurrentManagedThreadId,
            DurationMs = e.Value ?? 0,
            Category = "Default"
        }).ToList();
    }

    /// <summary>
    /// Releases all resources used by the OpenTelemetryProfilingService.
    /// </summary>
    public void Dispose()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
    }

    /// <summary>
    /// Internal profile event representation.
    /// </summary>
    private class ProfileEvent
    {
        public string Name { get; set; } = string.Empty;
        public ProfileEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public long FrameNumber { get; set; }
        public double? Value { get; set; }
    }

    /// <summary>
    /// Profile event types.
    /// </summary>
    private enum ProfileEventType
    {
        Marker,
        Counter,
        Scope
    }
}

/// <summary>
/// Internal profile scope implementation.
/// </summary>
internal class ProfileScopeImpl : IProfileScope
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public DateTime StartTime { get; private set; }
    public Activity? Activity { get; set; }

    private readonly Dictionary<string, string> _metadata = new();

    public ProfileScopeImpl(string id, string name, string category)
    {
        Id = id;
        Name = name;
        Category = category;
        StartTime = DateTime.UtcNow;
    }

    public void AddMetadata(string key, string value)
    {
        _metadata[key] = value;
        Activity?.SetTag($"scope.metadata.{key}", value);
    }

    public void Dispose()
    {
        var duration = DateTime.UtcNow - StartTime;
        Activity?.SetTag("scope.duration_ms", duration.TotalMilliseconds);
        Activity?.Stop();
    }
}

/// <summary>
/// No-operation profile scope for disabled categories.
/// </summary>
internal class NoOpProfileScope : IProfileScope
{
    public void AddMetadata(string key, string value) { }
    public void Dispose() { }
}
