using Microsoft.Extensions.Logging;
using PigeonPea.Diagnostic.Contracts;

namespace PigeonPea.Plugins.Logging.Diagnostic;

/// <summary>
/// Logger implementation that routes log entries to the diagnostic service.
/// </summary>
public class DiagnosticLogger : ILogger
{
    private readonly IDiagnosticService _diagnostic;
    private readonly string _category;
    private readonly DiagnosticLoggerOptions _options;

    public DiagnosticLogger(IDiagnosticService diagnostic, string category, DiagnosticLoggerOptions options)
    {
        _diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // Create diagnostic session if enabled
        if (_options.CreateSessions)
        {
            var context = ExtractData(state);
            var sessionName = $"{_options.EventNamePrefix}Scope_{_category}";
            return _diagnostic.CreateSession(sessionName, context);
        }

        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if diagnostics is enabled and level meets minimum threshold
        if (!_diagnostic.IsEnabled || logLevel < _options.MinimumLevel)
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

        data["EventId"] = eventId.Id;
        if (eventId.Name != null)
            data["EventName"] = eventId.Name;

        // Handle different log levels
        switch (logLevel)
        {
            case LogLevel.Warning:
                if (_options.TrackWarnings)
                {
                    var message = formatter(state, exception);
                    _diagnostic.ReportWarning(message, data);
                }
                break;

            case LogLevel.Error:
            case LogLevel.Critical:
                if (_options.TrackErrors && exception != null)
                {
                    var severity = logLevel == LogLevel.Critical ? DiagnosticSeverity.Critical : DiagnosticSeverity.Error;
                    _diagnostic.ReportError(exception, data, severity);
                }
                break;

            default:
                // Track informational events
                var infoMessage = formatter(state, exception);
                _diagnostic.ReportInfo(infoMessage, data);
                break;
        }

        // Track health metrics based on log level
        TrackHealthMetrics(logLevel);
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
    /// Tracks health metrics based on log levels.
    /// </summary>
    private void TrackHealthMetrics(LogLevel logLevel)
    {
        var metricName = logLevel switch
        {
            LogLevel.Trace => $"{_options.MetricPrefix}TraceCount",
            LogLevel.Debug => $"{_options.MetricPrefix}DebugCount",
            LogLevel.Information => $"{_options.MetricPrefix}InformationCount",
            LogLevel.Warning => $"{_options.MetricPrefix}WarningCount",
            LogLevel.Error => $"{_options.MetricPrefix}ErrorCount",
            LogLevel.Critical => $"{_options.MetricPrefix}CriticalCount",
            LogLevel.None => $"{_options.MetricPrefix}NoneCount",
            _ => $"{_options.MetricPrefix}UnknownCount"
        };

        // Increment counter for this log level
        _diagnostic.ReportHealthMetric(metricName, 1.0, "count");

        // Track category-specific metrics
        if (!string.IsNullOrEmpty(_category))
        {
            var categoryMetricName = $"{metricName}_{SanitizeCategoryName(_category)}";
            _diagnostic.ReportHealthMetric(categoryMetricName, 1.0, "count");
        }

        // Track overall health score (simplified - higher error counts reduce health)
        var healthScore = logLevel >= LogLevel.Error ? -1.0 : 0.1;
        _diagnostic.ReportHealthMetric($"{_options.MetricPrefix}HealthScore", healthScore, "score");
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
