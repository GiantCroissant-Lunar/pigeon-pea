using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Contracts.Recording.Models;

namespace PigeonPea.Plugins.Logging.Recording;

/// <summary>
/// Logger provider that routes log entries to the recording service.
/// </summary>
public class RecordingLoggerProvider : ILoggerProvider
{
    private readonly IEventRecorder _recorder;
    private readonly RecordingLoggerOptions _options;

    public RecordingLoggerProvider(IEventRecorder recorder, RecordingLoggerOptions? options = null)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _options = options ?? new RecordingLoggerOptions();
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return new RecordingLogger(_recorder, categoryName, _options);
    }
    
    public void Dispose() 
    {
        // No resources to dispose
    }
}

/// <summary>
/// Configuration options for the recording logger.
/// </summary>
public class RecordingLoggerOptions
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
}
