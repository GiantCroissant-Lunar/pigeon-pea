using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Contracts.Recording.Models;

namespace PigeonPea.Plugins.Logging.Recording;

/// <summary>
/// Logger implementation that routes log entries to the recording service.
/// </summary>
public class RecordingLogger : ILogger
{
    private readonly IEventRecorder _recorder;
    private readonly string _category;
    private readonly RecordingLoggerOptions _options;

    public RecordingLogger(IEventRecorder recorder, string category, RecordingLoggerOptions options)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null; // Recording doesn't support scopes

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if recording is active and level meets minimum threshold
        if (!_recorder.IsRecording || logLevel < _options.MinimumLevel)
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

        // Create the GameEvent
        var gameEvent = new GameEvent
        {
            Timestamp = 0, // Will be set by EventRecorder
            Type = eventId.Name ?? $"Event{eventId.Id}",
            Category = DetermineCategoryFromEventId(eventId.Id),
            Data = data
        };

        // Record the event
        _recorder.RecordEvent(gameEvent);
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
    /// Determines the event category based on the event ID range.
    /// </summary>
    private static string DetermineCategoryFromEventId(int eventId)
    {
        return eventId switch
        {
            >= 1000 and < 2000 => "Player",
            >= 2000 and < 3000 => "Entity",
            >= 3000 and < 4000 => "Map",
            >= 4000 and < 5000 => "AI",
            >= 5000 and < 6000 => "Performance",
            >= 6000 and < 7000 => "Input",
            >= 7000 and < 8000 => "Audio",
            >= 8000 and < 9000 => "Network",
            >= 9000 and < 10000 => "Error",
            >= 10000 => "System",
            _ => "General"
        };
    }
}
