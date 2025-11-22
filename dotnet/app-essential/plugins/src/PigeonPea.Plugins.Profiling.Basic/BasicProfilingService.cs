using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Profiling.Services;
using PigeonPea.Plugins.Profiling.Basic.Export;
using PigeonPea.Plugins.Profiling.Basic.Internal;

namespace PigeonPea.Plugins.Profiling.Basic;

/// <summary>
/// Basic implementation of profiling service with low-overhead design.
/// </summary>
public class BasicProfilingService : IService
{
    private readonly ILogger _logger;
    private readonly StringTable _stringTable;
    private readonly SpeedscopeExporter _speedscopeExporter;
    private readonly ChromeTraceExporter _chromeTraceExporter;

    // Thread-local storage for low-overhead profiling
    [ThreadStatic]
    private static EventBuffer? _threadLocalBuffer;
    
    [ThreadStatic]
    private static ScopeStack? _threadLocalScopeStack;

    // Global state
    private readonly ConcurrentDictionary<int, EventBuffer> _threadBuffers = new();
    private volatile ProfilerMode _mode = ProfilerMode.Disabled;
    private volatile bool _isCapturing = false;
    private readonly HashSet<string> _enabledCategories = new();
    private readonly object _categoryLock = new();
    private int _sampleRate = 100; // Hz

    // Frame tracking
    private long _currentFrameNumber = 0;
    private readonly ConcurrentDictionary<long, List<ProfileEvent>> _frameEvents = new();
    private readonly ConcurrentDictionary<string, List<double>> _scopeTimings = new();

    // Triggers
    private readonly List<IProfileTrigger> _triggers = new();
    private readonly object _triggerLock = new();

    // Overlay state
    private OverlayConfig? _overlayConfig;
    private volatile bool _overlayEnabled = false;

    // ECS integration
    private readonly ConcurrentDictionary<object, List<SystemStats>> _worldSystemStats = new();

    public BasicProfilingService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stringTable = new StringTable();
        _speedscopeExporter = new SpeedscopeExporter(_stringTable);
        _chromeTraceExporter = new ChromeTraceExporter(_stringTable);

        // Enable all categories by default
        _enabledCategories.Add("default");
        _enabledCategories.Add("frame");
        _enabledCategories.Add("update");
        _enabledCategories.Add("render");
        _enabledCategories.Add("ecs");
    }

    // ===== Core Profiling =====

    public IProfileScope BeginScope(string name)
    {
        return BeginScope(name, "default");
    }

    public IProfileScope BeginScope(string name, string category)
    {
        if (_mode == ProfilerMode.Disabled || !IsCategoryEnabled(category))
        {
            return new NoOpProfileScope();
        }

        var buffer = GetThreadLocalBuffer();
        var stack = GetThreadLocalScopeStack();
        var startTicks = Stopwatch.GetTimestamp();

        // Add begin event
        var nameIndex = _stringTable.GetOrCreateIndex(name);
        var categoryIndex = _stringTable.GetOrCreateIndex(category);
        var threadId = Environment.CurrentManagedThreadId;

        if (_isCapturing)
        {
            var beginEvent = new ProfileEvent
            {
                TimestampTicks = startTicks,
                Type = EventType.ScopeBegin,
                NameIndex = nameIndex,
                CategoryIndex = categoryIndex,
                ThreadId = threadId,
                Depth = stack.Depth
            };

            buffer.TryAddEvent(beginEvent);
        }

        stack.Push(name, category, startTicks);

        return new ProfileScope(this, name, category, startTicks, threadId, stack.Depth);
    }

    public void RecordMarker(string name)
    {
        if (_mode == ProfilerMode.Disabled) return;

        var buffer = GetThreadLocalBuffer();
        var nameIndex = _stringTable.GetOrCreateIndex(name);
        var categoryIndex = _stringTable.GetOrCreateIndex("marker");
        var threadId = Environment.CurrentManagedThreadId;

        if (_isCapturing)
        {
            var markerEvent = new ProfileEvent
            {
                TimestampTicks = Stopwatch.GetTimestamp(),
                Type = EventType.Marker,
                NameIndex = nameIndex,
                CategoryIndex = categoryIndex,
                ThreadId = threadId,
                Depth = 0
            };

            buffer.TryAddEvent(markerEvent);
        }
    }

    public void RecordCounter(string name, double value)
    {
        if (_mode == ProfilerMode.Disabled) return;

        var buffer = GetThreadLocalBuffer();
        var nameIndex = _stringTable.GetOrCreateIndex(name);
        var categoryIndex = _stringTable.GetOrCreateIndex("counter");
        var threadId = Environment.CurrentManagedThreadId;

        if (_isCapturing)
        {
            var counterEvent = new ProfileEvent
            {
                TimestampTicks = Stopwatch.GetTimestamp(),
                Type = EventType.Counter,
                NameIndex = nameIndex,
                CategoryIndex = categoryIndex,
                ThreadId = threadId,
                Depth = 0,
                DurationMs = value
            };

            buffer.TryAddEvent(counterEvent);
        }
    }

    // ===== Capture Control =====

    public void StartCapture()
    {
        _isCapturing = true;
        _logger.LogDebug("Profiling capture started");
    }

    public ProfileCapture StopCapture()
    {
        _isCapturing = false;

        var allEvents = new List<ProfileEvent>();
        foreach (var buffer in _threadBuffers.Values)
        {
            allEvents.AddRange(buffer.ExtractAndClear());
        }

        // Clear thread-local buffer too
        var threadBuffer = GetThreadLocalBuffer();
        allEvents.AddRange(threadBuffer.ExtractAndClear());

        var capture = new ProfileCapture
        {
            StartTime = allEvents.Count > 0 ? new DateTime(allEvents.Min(e => e.TimestampTicks)) : DateTime.UtcNow,
            EndTime = allEvents.Count > 0 ? new DateTime(allEvents.Max(e => e.TimestampTicks)) : DateTime.UtcNow,
            FrameCount = _frameEvents.Count,
            EventCount = allEvents.Count
        };

        // Store events in capture for export (using reflection to access internal Events property)
        var eventsProperty = typeof(ProfileCapture).GetField("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        eventsProperty?.SetValue(capture, allEvents);

        _logger.LogDebug("Profiling capture stopped: {EventCount} events, {FrameCount} frames", capture.EventCount, capture.FrameCount);
        return capture;
    }

    public void ClearCapture()
    {
        foreach (var buffer in _threadBuffers.Values)
        {
            buffer.Clear();
        }

        GetThreadLocalBuffer().Clear();
        _frameEvents.Clear();
        _scopeTimings.Clear();
        _stringTable.Clear();

        _logger.LogDebug("Profiling capture cleared");
    }

    public bool IsCapturing => _isCapturing;

    // ===== Configuration =====

    public void SetMode(ProfilerMode mode)
    {
        _mode = mode;
        _logger.LogDebug("Profiling mode set to {Mode}", mode);
    }

    public ProfilerMode Mode => _mode;

    public void SetCategoryEnabled(string category, bool enabled)
    {
        lock (_categoryLock)
        {
            if (enabled)
            {
                _enabledCategories.Add(category);
            }
            else
            {
                _enabledCategories.Remove(category);
            }
        }
    }

    public void SetSampleRate(int samplesPerSecond)
    {
        _sampleRate = Math.Max(1, samplesPerSecond);
    }

    // ===== Export =====

    public void ExportToSpeedscope(string filePath)
    {
        var capture = StopCapture();
        var events = GetCaptureEvents(capture);
        _speedscopeExporter.Export(events, filePath);
        _logger.LogInformation("Exported {EventCount} events to Speedscope format: {FilePath}", events.Count, filePath);
    }

    public void ExportToChromeTrace(string filePath)
    {
        var capture = StopCapture();
        var events = GetCaptureEvents(capture);
        _chromeTraceExporter.Export(events, filePath);
        _logger.LogInformation("Exported {EventCount} events to Chrome Trace format: {FilePath}", events.Count, filePath);
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
                throw new ArgumentException($"Unsupported export format: {format}");
        }
    }

    // ===== Real-time Stats =====

    public FrameStats GetCurrentFrameStats()
    {
        var frameNumber = _currentFrameNumber;
        var frameEvents = _frameEvents.GetValueOrDefault(frameNumber, new List<ProfileEvent>());
        
        // Calculate frame time from scope events
        var frameScopes = frameEvents.Where(e => e.Type == EventType.ScopeBegin || e.Type == EventType.ScopeEnd).ToList();
        var frameTimeMs = 0.0;
        var scopeTimes = new Dictionary<string, double>();

        // Simple frame time calculation (can be improved)
        if (frameScopes.Count >= 2)
        {
            var startTicks = frameScopes.Min(e => e.TimestampTicks);
            var endTicks = frameScopes.Max(e => e.TimestampTicks);
            frameTimeMs = (endTicks - startTicks) / (double)TimeSpan.TicksPerMillisecond;
        }

        return new FrameStats
        {
            FrameNumber = frameNumber,
            FrameTimeMs = frameTimeMs,
            ScopeTimesMs = scopeTimes
        };
    }

    public ScopeStats GetScopeStats(string scopeName, int frameCount = 60)
    {
        var timings = _scopeTimings.GetValueOrDefault(scopeName, new List<double>());
        var recentTimings = timings.TakeLast(frameCount).ToList();

        if (!recentTimings.Any())
        {
            return new ScopeStats { Name = scopeName };
        }

        var sorted = recentTimings.OrderBy(x => x).ToList();
        var count = sorted.Count;

        return new ScopeStats
        {
            Name = scopeName,
            SampleCount = count,
            AverageMs = sorted.Average(),
            MinMs = sorted[0],
            MaxMs = sorted[^1],
            TotalMs = sorted.Sum(),
            P95Ms = sorted[(int)(count * 0.95)],
            P99Ms = sorted[(int)(count * 0.99)]
        };
    }

    public IReadOnlyList<ScopeStats> GetAllScopeStats(int frameCount = 60)
    {
        return _scopeTimings.Keys
            .Select(name => GetScopeStats(name, frameCount))
            .OrderByDescending(stats => stats.TotalMs)
            .ToList();
    }

    // ===== ECS Integration =====

    public void InstrumentWorld(object world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        
        // Basic implementation - store world reference for system tracking
        _worldSystemStats.TryAdd(world, new List<SystemStats>());
        _logger.LogDebug("World instrumented for profiling: {WorldType}", world.GetType().Name);
    }

    public IReadOnlyList<SystemStats> GetSystemReport(object world)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        
        return _worldSystemStats.GetValueOrDefault(world, new List<SystemStats>());
    }

    // ===== Debug Overlay =====

    public void EnableOverlay(OverlayConfig config)
    {
        _overlayConfig = config;
        _overlayEnabled = true;
        _logger.LogDebug("Debug overlay enabled");
    }

    public void DisableOverlay()
    {
        _overlayEnabled = false;
        _logger.LogDebug("Debug overlay disabled");
    }

    public bool IsOverlayEnabled => _overlayEnabled;

    // ===== Triggers =====

    public void SetTrigger(IProfileTrigger trigger)
    {
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        
        lock (_triggerLock)
        {
            _triggers.Clear();
            _triggers.Add(trigger);
        }

        _logger.LogDebug("Profile trigger set: {TriggerType}", trigger.GetType().Name);
    }

    public void ClearTriggers()
    {
        lock (_triggerLock)
        {
            _triggers.Clear();
        }

        _logger.LogDebug("All profile triggers cleared");
    }

    // ===== Frame Boundary =====

    public void EndFrame()
    {
        Interlocked.Increment(ref _currentFrameNumber);

        // Check triggers
        var currentFrameStats = GetCurrentFrameStats();
        lock (_triggerLock)
        {
            foreach (var trigger in _triggers)
            {
                if (trigger.ShouldTrigger(currentFrameStats))
                {
                    _logger.LogInformation("Profile trigger activated for frame {FrameNumber}", currentFrameStats.FrameNumber);
                    // Trigger-specific logic would go here
                }
            }
        }
    }

    // ===== Internal Methods =====

    internal void EndScope(string name, string category, long startTicks, int threadId, int depth)
    {
        if (_mode == ProfilerMode.Disabled) return;

        var stack = GetThreadLocalScopeStack();
        stack.TryPop(out _);

        var buffer = GetThreadLocalBuffer();
        var endTicks = Stopwatch.GetTimestamp();
        var durationMs = (endTicks - startTicks) / (double)TimeSpan.TicksPerMillisecond;

        if (_isCapturing)
        {
            var nameIndex = _stringTable.GetOrCreateIndex(name);
            var categoryIndex = _stringTable.GetOrCreateIndex(category);

            var endEvent = new ProfileEvent
            {
                TimestampTicks = endTicks,
                Type = EventType.ScopeEnd,
                NameIndex = nameIndex,
                CategoryIndex = categoryIndex,
                ThreadId = threadId,
                Depth = depth,
                DurationMs = durationMs
            };

            buffer.TryAddEvent(endEvent);

            // Store timing for statistics
            _scopeTimings.AddOrUpdate(name, 
                new List<double> { durationMs },
                (key, list) =>
                {
                    list.Add(durationMs);
                    // Keep only recent samples (last 1000)
                    if (list.Count > 1000)
                    {
                        return list.SkipLast(list.Count - 1000).ToList();
                    }
                    return list;
                });
        }
    }

    private bool IsCategoryEnabled(string category)
    {
        lock (_categoryLock)
        {
            return _enabledCategories.Contains(category);
        }
    }

    private EventBuffer GetThreadLocalBuffer()
    {
        return _threadLocalBuffer ??= new EventBuffer();
    }

    private ScopeStack GetThreadLocalScopeStack()
    {
        return _threadLocalScopeStack ??= new ScopeStack();
    }

    private IReadOnlyList<ProfileEvent> GetCaptureEvents(ProfileCapture capture)
    {
        var eventsProperty = typeof(ProfileCapture).GetField("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (IReadOnlyList<ProfileEvent>)(eventsProperty?.GetValue(capture) ?? Array.Empty<ProfileEvent>());
    }

    private void ExportToJson(string filePath)
    {
        var capture = StopCapture();
        var events = GetCaptureEvents(capture);

        var jsonEvents = events.Select(e => new
        {
            timestamp = e.TimestampTicks,
            type = e.Type.ToString(),
            name = _stringTable.GetString(e.NameIndex),
            category = _stringTable.GetString(e.CategoryIndex),
            threadId = e.ThreadId,
            depth = e.Depth,
            durationMs = e.DurationMs
        });

        var json = System.Text.Json.JsonSerializer.Serialize(jsonEvents, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(filePath, json);
    }
}

/// <summary>
/// No-operation profile scope for when profiling is disabled.
/// </summary>
internal sealed class NoOpProfileScope : IProfileScope
{
    public void Dispose() { }
    public void AddMetadata(string key, string value) { }
}

/// <summary>
/// Active profile scope that measures timing.
/// </summary>
internal sealed class ProfileScope : IProfileScope
{
    private readonly BasicProfilingService _service;
    private readonly string _name;
    private readonly string _category;
    private readonly long _startTicks;
    private readonly int _threadId;
    private readonly int _depth;

    public ProfileScope(BasicProfilingService service, string name, string category, long startTicks, int threadId, int depth)
    {
        _service = service;
        _name = name;
        _category = category;
        _startTicks = startTicks;
        _threadId = threadId;
        _depth = depth;
    }

    public void Dispose()
    {
        _service.EndScope(_name, _category, _startTicks, _threadId, _depth);
    }

    public void AddMetadata(string key, string value)
    {
        // Metadata not implemented in this basic version
        // Could be stored in a thread-local dictionary
    }
}
