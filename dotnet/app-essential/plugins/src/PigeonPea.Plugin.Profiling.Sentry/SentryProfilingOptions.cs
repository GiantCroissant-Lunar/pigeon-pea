namespace PigeonPea.Plugin.Profiling.Sentry;

/// <summary>
/// Configuration options for Sentry profiling service.
/// </summary>
public class SentryProfilingOptions
{
    /// <summary>
    /// Create transactions for scopes without a parent.
    /// </summary>
    public bool CreateTransactionsForOrphanScopes { get; set; } = true;

    /// <summary>
    /// Track each frame as a Sentry transaction.
    /// Warning: High volume in production.
    /// </summary>
    public bool TrackFramesAsTransactions { get; set; }

    /// <summary>
    /// Default transaction operation name for scopes.
    /// </summary>
    public string DefaultOperation { get; set; } = "profiling.scope";

    /// <summary>
    /// Include performance markers as span events.
    /// </summary>
    public bool IncludeMarkersAsSpanEvents { get; set; } = true;

    /// <summary>
    /// Include counters as span data.
    /// </summary>
    public bool IncludeCountersAsSpanData { get; set; } = true;

    /// <summary>
    /// Maximum number of scope statistics to keep in memory.
    /// </summary>
    public int MaxScopeStats { get; set; } = 1000;

    /// <summary>
    /// Frame history size for statistics calculations.
    /// </summary>
    public int FrameHistorySize { get; set; } = 60;
}
