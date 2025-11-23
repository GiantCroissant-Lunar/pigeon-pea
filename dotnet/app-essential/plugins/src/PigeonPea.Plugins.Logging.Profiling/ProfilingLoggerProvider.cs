using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Profiling.Services;

namespace PigeonPea.Plugins.Logging.Profiling;

/// <summary>
/// Logger provider that routes log entries to the profiling service.
/// </summary>
public class ProfilingLoggerProvider : ILoggerProvider
{
    private readonly IProfilingService _profiling;
    private readonly ProfilingLoggerOptions _options;

    public ProfilingLoggerProvider(IProfilingService profiling, ProfilingLoggerOptions? options = null)
    {
        _profiling = profiling ?? throw new ArgumentNullException(nameof(profiling));
        _options = options ?? new ProfilingLoggerOptions();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ProfilingLogger(_profiling, categoryName, _options);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Configuration options for the profiling logger.
/// </summary>
public class ProfilingLoggerOptions
{
    /// <summary>
    /// Gets or sets whether to log all categories or only specific ones.
    /// </summary>
    public bool LogAllCategories { get; set; } = true;

    /// <summary>
    /// Gets or sets the specific categories to log when LogAllCategories is false.
    /// </summary>
    public HashSet<string> AllowedCategories { get; set; } = new();

    /// <summary>
    /// Gets or sets the minimum log level to profile.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to include the log level as event data.
    /// </summary>
    public bool IncludeLogLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the category as event data.
    /// </summary>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create profiling spans for log entries.
    /// </summary>
    public bool CreateSpans { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix to add to all span names.
    /// </summary>
    public string SpanNamePrefix { get; set; } = "Log_";

    /// <summary>
    /// Gets or sets whether to track performance metrics from log entries.
    /// </summary>
    public bool TrackPerformanceMetrics { get; set; } = true;
}
