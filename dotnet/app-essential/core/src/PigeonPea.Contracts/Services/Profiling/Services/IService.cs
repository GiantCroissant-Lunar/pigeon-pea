using System.Collections.Generic;

namespace PigeonPea.Contracts.Profiling.Services;

/// <summary>
/// Runtime profiling service for performance instrumentation and analysis.
/// Supports export to speedscope, Chrome Trace, and other visualization tools.
/// </summary>
public interface IService
{
    // ===== Core Profiling =====

    /// <summary>
    /// Begins a named profiling scope. Use with 'using' statement.
    /// </summary>
    IProfileScope BeginScope(string name);

    /// <summary>
    /// Begins a profiling scope with category for filtering.
    /// </summary>
    IProfileScope BeginScope(string name, string category);

    /// <summary>
    /// Records a single instant event (marker).
    /// </summary>
    void RecordMarker(string name);

    /// <summary>
    /// Records a counter value over time.
    /// </summary>
    void RecordCounter(string name, double value);

    // ===== Capture Control =====

    /// <summary>
    /// Starts capturing profiling data.
    /// </summary>
    void StartCapture();

    /// <summary>
    /// Stops capturing and returns the captured data.
    /// </summary>
    ProfileCapture StopCapture();

    /// <summary>
    /// Clears all captured profiling data.
    /// </summary>
    void ClearCapture();

    /// <summary>
    /// Gets whether profiling is currently capturing.
    /// </summary>
    bool IsCapturing { get; }

    // ===== Configuration =====

    /// <summary>
    /// Sets the profiling mode (Instrumentation, Sampling, Disabled).
    /// </summary>
    void SetMode(ProfilerMode mode);

    /// <summary>
    /// Gets the current profiling mode.
    /// </summary>
    ProfilerMode Mode { get; }

    /// <summary>
    /// Enables or disables a profiling category.
    /// </summary>
    void SetCategoryEnabled(string category, bool enabled);

    /// <summary>
    /// Sets the sample rate for sampling mode (Hz).
    /// </summary>
    void SetSampleRate(int samplesPerSecond);

    // ===== Export =====

    /// <summary>
    /// Exports captured data to speedscope JSON format.
    /// </summary>
    void ExportToSpeedscope(string filePath);

    /// <summary>
    /// Exports captured data to Chrome Trace JSON format.
    /// </summary>
    void ExportToChromeTrace(string filePath);

    /// <summary>
    /// Exports captured data to the specified format.
    /// </summary>
    void Export(string filePath, ProfileExportFormat format);

    // ===== Real-time Stats =====

    /// <summary>
    /// Gets the current frame statistics.
    /// </summary>
    FrameStats GetCurrentFrameStats();

    /// <summary>
    /// Gets statistics for a specific scope over recent frames.
    /// </summary>
    ScopeStats GetScopeStats(string scopeName, int frameCount = 60);

    /// <summary>
    /// Gets all scope statistics sorted by total time.
    /// </summary>
    IReadOnlyList<ScopeStats> GetAllScopeStats(int frameCount = 60);

    // ===== ECS Integration =====

    /// <summary>
    /// Instruments an ECS world to auto-profile all systems.
    /// </summary>
    void InstrumentWorld(object world);

    /// <summary>
    /// Gets system-level profiling report for an instrumented world.
    /// </summary>
    IReadOnlyList<SystemStats> GetSystemReport(object world);

    // ===== Debug Overlay =====

    /// <summary>
    /// Enables the in-game debug overlay.
    /// </summary>
    void EnableOverlay(OverlayConfig config);

    /// <summary>
    /// Disables the debug overlay.
    /// </summary>
    void DisableOverlay();

    /// <summary>
    /// Gets whether the overlay is currently visible.
    /// </summary>
    bool IsOverlayEnabled { get; }

    // ===== Triggers =====

    /// <summary>
    /// Sets a trigger for conditional profiling.
    /// </summary>
    void SetTrigger(IProfileTrigger trigger);

    /// <summary>
    /// Clears all triggers.
    /// </summary>
    void ClearTriggers();

    // ===== Frame Boundary =====

    /// <summary>
    /// Marks the end of a frame for frame-based statistics.
    /// </summary>
    void EndFrame();
}

/// <summary>
/// A profiling scope that measures time between creation and disposal.
/// </summary>
public interface IProfileScope : IDisposable
{
    /// <summary>
    /// Adds metadata to this scope.
    /// </summary>
    void AddMetadata(string key, string value);
}

/// <summary>
/// Profiling mode.
/// </summary>
public enum ProfilerMode
{
    /// <summary>
    /// Profiling disabled, minimal overhead.
    /// </summary>
    Disabled,

    /// <summary>
    /// Full instrumentation mode, higher overhead but detailed timing.
    /// </summary>
    Instrumentation,

    /// <summary>
    /// Sampling mode, lower overhead, less detail.
    /// </summary>
    Sampling
}

/// <summary>
/// Export format for profile data.
/// </summary>
public enum ProfileExportFormat
{
    /// <summary>
    /// Speedscope JSON format (https://speedscope.app)
    /// </summary>
    Speedscope,

    /// <summary>
    /// Chrome Trace Event format (chrome://tracing)
    /// </summary>
    ChromeTrace,

    /// <summary>
    /// Simple JSON for custom analysis.
    /// </summary>
    Json,

    /// <summary>
    /// ETW (Windows Event Tracing) - Windows only.
    /// </summary>
    Etw
}

/// <summary>
/// Captured profiling data.
/// </summary>
public sealed class ProfileCapture
{
    /// <summary>
    /// Start time of capture.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// End time of capture.
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Total duration of capture.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Number of frames captured.
    /// </summary>
    public int FrameCount { get; init; }

    /// <summary>
    /// Number of scope events captured.
    /// </summary>
    public int EventCount { get; init; }
}

/// <summary>
/// Statistics for a single frame.
/// </summary>
public sealed class FrameStats
{
    /// <summary>
    /// Frame number.
    /// </summary>
    public long FrameNumber { get; init; }

    /// <summary>
    /// Total frame time in milliseconds.
    /// </summary>
    public double FrameTimeMs { get; init; }

    /// <summary>
    /// Frames per second (instantaneous).
    /// </summary>
    public double Fps => FrameTimeMs > 0 ? 1000.0 / FrameTimeMs : 0;

    /// <summary>
    /// Time breakdown by top-level scope.
    /// </summary>
    public IReadOnlyDictionary<string, double> ScopeTimesMs { get; init; }
        = new Dictionary<string, double>();
}

/// <summary>
/// Statistics for a profiling scope over multiple frames.
/// </summary>
public sealed class ScopeStats
{
    /// <summary>
    /// Scope name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Scope category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Number of samples.
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// Average time in milliseconds.
    /// </summary>
    public double AverageMs { get; init; }

    /// <summary>
    /// Minimum time in milliseconds.
    /// </summary>
    public double MinMs { get; init; }

    /// <summary>
    /// Maximum time in milliseconds.
    /// </summary>
    public double MaxMs { get; init; }

    /// <summary>
    /// Total time across all samples.
    /// </summary>
    public double TotalMs { get; init; }

    /// <summary>
    /// 95th percentile time.
    /// </summary>
    public double P95Ms { get; init; }

    /// <summary>
    /// 99th percentile time.
    /// </summary>
    public double P99Ms { get; init; }
}

/// <summary>
/// Statistics for an ECS system.
/// </summary>
public sealed class SystemStats
{
    /// <summary>
    /// System type name.
    /// </summary>
    public string SystemName { get; init; } = string.Empty;

    /// <summary>
    /// Average update time in milliseconds.
    /// </summary>
    public double AverageMs { get; init; }

    /// <summary>
    /// Maximum update time in milliseconds.
    /// </summary>
    public double MaxMs { get; init; }

    /// <summary>
    /// Number of entities processed (if available).
    /// </summary>
    public int? EntityCount { get; init; }

    /// <summary>
    /// Time per entity in microseconds (if available).
    /// </summary>
    public double? PerEntityUs { get; init; }
}

/// <summary>
/// Configuration for the debug overlay.
/// </summary>
public sealed class OverlayConfig
{
    /// <summary>
    /// Show frame time and FPS.
    /// </summary>
    public bool ShowFrameTime { get; init; } = true;

    /// <summary>
    /// Show frame time graph.
    /// </summary>
    public bool ShowFrameGraph { get; init; } = true;

    /// <summary>
    /// Number of top systems to show.
    /// </summary>
    public int ShowTopSystems { get; init; } = 5;

    /// <summary>
    /// Show memory statistics.
    /// </summary>
    public bool ShowMemoryStats { get; init; } = true;

    /// <summary>
    /// Show GC statistics.
    /// </summary>
    public bool ShowGcStats { get; init; } = false;

    /// <summary>
    /// Overlay position on screen.
    /// </summary>
    public OverlayPosition Position { get; init; } = OverlayPosition.TopRight;

    /// <summary>
    /// Frame graph width in samples.
    /// </summary>
    public int GraphWidth { get; init; } = 60;
}

/// <summary>
/// Overlay position on screen.
/// </summary>
public enum OverlayPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Interface for conditional profiling triggers.
/// </summary>
public interface IProfileTrigger
{
    /// <summary>
    /// Evaluates whether to trigger capture.
    /// </summary>
    bool ShouldTrigger(FrameStats currentFrame);

    /// <summary>
    /// Number of frames to capture before trigger.
    /// </summary>
    int CaptureFramesBefore { get; }

    /// <summary>
    /// Number of frames to capture after trigger.
    /// </summary>
    int CaptureFramesAfter { get; }
}

/// <summary>
/// Triggers when frame time exceeds threshold.
/// </summary>
public sealed class FrameTimeThresholdTrigger : IProfileTrigger
{
    public double ThresholdMs { get; init; } = 33.3; // Default: 30 FPS
    public int CaptureFramesBefore { get; init; } = 5;
    public int CaptureFramesAfter { get; init; } = 10;

    public bool ShouldTrigger(FrameStats currentFrame)
        => currentFrame.FrameTimeMs > ThresholdMs;
}
