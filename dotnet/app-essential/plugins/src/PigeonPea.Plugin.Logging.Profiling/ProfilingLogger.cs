using Microsoft.Extensions.Logging;
using PigeonPea.Profiling.Contracts;

namespace PigeonPea.Plugin.Logging.Profiling;

/// <summary>
/// Logger implementation that routes log entries to the profiling service.
/// </summary>
public class ProfilingLogger : ILogger
{
    private readonly IProfilingService _profiling;
    private readonly string _category;
    private readonly ProfilingLoggerOptions _options;

    public ProfilingLogger(IProfilingService profiling, string category, ProfilingLoggerOptions options)
    {
        _profiling = profiling ?? throw new ArgumentNullException(nameof(profiling));
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null; // Profiling doesn't support scopes

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if profiling is enabled and level meets minimum threshold
        if (_profiling.Mode == ProfilerMode.Disabled || logLevel < _options.MinimumLevel)
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
        }

        // Create profiling span if enabled
        if (_options.CreateSpans)
        {
            var spanName = $"{_options.SpanNamePrefix}{eventId.Name ?? $"Event{eventId.Id}"}";
            using var scope = _profiling.BeginScope(spanName);

            // Add log level as scope metadata
            scope.AddMetadata("log.level", logLevel.ToString());
            scope.AddMetadata("log.category", _category);
            scope.AddMetadata("log.event_id", eventId.Id.ToString());

            if (eventId.Name != null)
                scope.AddMetadata("log.event_name", eventId.Name);

            // Add exception metadata if present
            if (exception != null)
            {
                scope.AddMetadata("exception.type", exception.GetType().Name);
                scope.AddMetadata("exception.message", exception.Message);
            }
        }

        // Track performance metrics if enabled
        if (_options.TrackPerformanceMetrics)
        {
            TrackPerformanceMetrics(logLevel);
        }
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
    /// Tracks performance metrics based on log levels.
    /// </summary>
    private void TrackPerformanceMetrics(LogLevel logLevel)
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

        _profiling.RecordCounter(metricName, 1);

        // Track category-specific metrics
        if (!string.IsNullOrEmpty(_category))
        {
            var categoryMetricName = $"{metricName}_{SanitizeCategoryName(_category)}";
            _profiling.RecordCounter(categoryMetricName, 1);
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
