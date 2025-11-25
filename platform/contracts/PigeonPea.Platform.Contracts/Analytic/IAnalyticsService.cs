namespace PigeonPea.Platform.Contracts.Analytic;

/// <summary>
/// Analytics service interface for tracking events, metrics, and exceptions.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets or sets whether analytics tracking is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Tracks a custom analytics event with associated data.
    /// </summary>
    /// <param name="eventName">The name of the event to track.</param>
    /// <param name="data">Optional structured data associated with the event.</param>
    void TrackEvent(string eventName, Dictionary<string, object>? data = null);

    /// <summary>
    /// Tracks an exception with optional context data.
    /// </summary>
    /// <param name="exception">The exception to track.</param>
    /// <param name="context">Optional context data about the exception.</param>
    void TrackException(Exception exception, Dictionary<string, object>? context = null);

    /// <summary>
    /// Tracks a numeric metric value.
    /// </summary>
    /// <param name="metricName">The name of the metric.</param>
    /// <param name="value">The numeric value to track.</param>
    void TrackMetric(string metricName, double value);

    /// <summary>
    /// Tracks a user property or attribute.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    void SetUserProperty(string propertyName, object value);

    /// <summary>
    /// Tracks a screen view or page view event.
    /// </summary>
    /// <param name="screenName">The name of the screen or page.</param>
    /// <param name="data">Optional additional data about the view.</param>
    void TrackScreenView(string screenName, Dictionary<string, object>? data = null);

    /// <summary>
    /// Begins a timed operation for performance tracking.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>A disposable that when disposed ends the timing.</returns>
    IDisposable BeginTimedOperation(string operationName);

    /// <summary>
    /// Flushes any pending analytics data.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the flush operation.</returns>
    Task FlushAsync(System.Threading.CancellationToken cancellationToken = default);
}
