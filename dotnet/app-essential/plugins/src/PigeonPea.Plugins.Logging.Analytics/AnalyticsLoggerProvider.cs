using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Analytics.Services;

namespace PigeonPea.Plugins.Logging.Analytics;

/// <summary>
/// Logger provider that routes log entries to the analytics service.
/// </summary>
public class AnalyticsLoggerProvider : ILoggerProvider
{
    private readonly IAnalyticsService _analytics;
    private readonly AnalyticsLoggerOptions _options;

    public AnalyticsLoggerProvider(IAnalyticsService analytics, AnalyticsLoggerOptions? options = null)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _options = options ?? new AnalyticsLoggerOptions();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new AnalyticsLogger(_analytics, categoryName, _options);
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}

/// <summary>
/// Configuration options for the analytics logger.
/// </summary>
public class AnalyticsLoggerOptions
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
    /// Gets or sets the minimum log level to record.
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
    /// Gets or sets whether to track custom events from log entries.
    /// </summary>
    public bool TrackCustomEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix to add to all event names.
    /// </summary>
    public string EventNamePrefix { get; set; } = "Log_";

    /// <summary>
    /// Gets or sets whether to track exceptions as separate analytics events.
    /// </summary>
    public bool TrackExceptions { get; set; } = true;
}
