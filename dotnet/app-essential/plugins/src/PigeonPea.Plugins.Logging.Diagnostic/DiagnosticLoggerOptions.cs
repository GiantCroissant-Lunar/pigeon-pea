using Microsoft.Extensions.Logging;

/// <summary>
/// Configuration options for the diagnostic logger provider.
/// </summary>
public class DiagnosticLoggerOptions
{
    /// <summary>
    /// Gets or sets the minimum log level to capture.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Warning;
    
    /// <summary>
    /// Gets or sets whether to include log level in diagnostic data.
    /// </summary>
    public bool IncludeLogLevel { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to include logger category in diagnostic data.
    /// </summary>
    public bool IncludeCategory { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to track warnings as diagnostic events.
    /// </summary>
    public bool TrackWarnings { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to track errors as diagnostic events.
    /// </summary>
    public bool TrackErrors { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to create diagnostic sessions for related events.
    /// </summary>
    public bool CreateSessions { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to log all categories or filter them.
    /// </summary>
    public bool LogAllCategories { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the list of allowed categories when LogAllCategories is false.
    /// </summary>
    public HashSet<string> AllowedCategories { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the prefix for diagnostic event names.
    /// </summary>
    public string EventNamePrefix { get; set; } = "Log_";
    
    /// <summary>
    /// Gets or sets the prefix for health metric names.
    /// </summary>
    public string MetricPrefix { get; set; } = "Log";
}
