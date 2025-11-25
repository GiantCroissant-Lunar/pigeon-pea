using Microsoft.Extensions.Logging;
using PigeonPea.Analytics.Contracts;

namespace PigeonPea.Plugin.Logging.Analytics;

/// <summary>
/// Logger implementation that routes log entries to the analytics service.
/// </summary>
public class AnalyticsLogger : ILogger
{
    private readonly IAnalyticsService _analytics;
    private readonly string _category;
    private readonly AnalyticsLoggerOptions _options;

    public AnalyticsLogger(IAnalyticsService analytics, string category, AnalyticsLoggerOptions options)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null; // Analytics doesn't support scopes

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if analytics is enabled and level meets minimum threshold
        if (!_analytics.IsEnabled || logLevel < _options.MinimumLevel)
            return false;

        // Check category filtering
        if (!_options.LogAllCategories && !_options.AllowedCategories.Contains(_category))
            return false;

        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // Extract structured data from the log state
        var data = ExtractData(state);

        // Add metadata about the log entry itself
        if (_options.IncludeLogLevel)
            data["LogLevel"] = logLevel.ToString();

        if (_options.IncludeCategory)
            data["LoggerCategory"] = _category;

        // Add exception information if present
        if (exception != null)
        {
            data["ExceptionType"] = exception.GetType().Name;
            data["ExceptionMessage"] = exception.Message;
            if (!string.IsNullOrEmpty(exception.StackTrace))
                data["StackTrace"] = exception.StackTrace;

            // Track exception as separate analytics event if enabled
            if (_options.TrackExceptions)
            {
                TrackException(exception, data);
            }
        }

        // Track custom event if enabled
        if (_options.TrackCustomEvents)
        {
            var eventName = $"{_options.EventNamePrefix}{eventId.Name ?? $"Event{eventId.Id}"}";
            _analytics.TrackEvent(eventName, data);
        }

        // Track metrics based on log level
        TrackLogLevelMetrics(logLevel);
    }

    /// <summary>
    /// Extracts structured data from the log state.
    /// </summary>
    private static Dictionary<string, object> ExtractData<TState>(TState state)
    {
        var data = new Dictionary<string, object>();

        // Handle the standard logger state format
        if (state is IReadOnlyList<KeyValuePair<string, object>> kvps)
        {
            foreach (var kvp in kvps)
            {
                if (kvp.Key != "{OriginalFormat}" && kvp.Value != null)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }
        }
        // Handle simple key-value pairs
        else if (state is IEnumerable<KeyValuePair<string, object>> enumerable)
        {
            foreach (var kvp in enumerable)
            {
                if (kvp.Key != "{OriginalFormat}" && kvp.Value != null)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }
        }
        // Handle anonymous objects or other types
        else
        {
            var properties = state?.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(state);
                    if (value != null)
                    {
                        data[prop.Name] = value;
                    }
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Tracks an exception as a separate analytics event.
    /// </summary>
    private void TrackException(Exception exception, Dictionary<string, object> context)
    {
        var exceptionData = new Dictionary<string, object>(context)
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["ExceptionMessage"] = exception.Message,
            ["ExceptionSource"] = exception.Source ?? "Unknown"
        };

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            exceptionData["StackTrace"] = exception.StackTrace;
        }

        _analytics.TrackEvent("Exception", exceptionData);
    }

    /// <summary>
    /// Tracks metrics based on log levels.
    /// </summary>
    private void TrackLogLevelMetrics(LogLevel logLevel)
    {
        var metricName = logLevel switch
        {
            LogLevel.Trace => "LogTraceCount",
            LogLevel.Debug => "LogDebugCount",
            LogLevel.Information => "LogInformationCount",
            LogLevel.Warning => "LogWarningCount",
            LogLevel.Error => "LogErrorCount",
            LogLevel.Critical => "LogCriticalCount",
            LogLevel.None => "LogNoneCount",
            _ => "LogUnknownCount"
        };

        _analytics.TrackMetric(metricName, 1);

        // Track category-specific metrics
        if (!string.IsNullOrEmpty(_category))
        {
            var categoryMetricName = $"{metricName}_{SanitizeCategoryName(_category)}";
            _analytics.TrackMetric(categoryMetricName, 1);
        }
    }

    /// <summary>
    /// Sanitizes category name for use in metric names.
    /// </summary>
    private static string SanitizeCategoryName(string category)
    {
        return category
            .Replace(".", "_")
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "");
    }
}
