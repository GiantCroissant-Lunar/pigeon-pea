namespace PigeonPea.Platform.Contracts.Analytics;

/// <summary>
/// Analytics service for tracking events, metrics, and user behavior.
/// </summary>
public interface IService
{
    /// <summary>
    /// Tracks a named event with optional properties.
    /// </summary>
    void TrackEvent(string eventName, IDictionary<string, object>? properties = null);

    /// <summary>
    /// Tracks a metric value.
    /// </summary>
    void TrackMetric(string metricName, double value, IDictionary<string, string>? dimensions = null);

    /// <summary>
    /// Tracks a counter increment.
    /// </summary>
    void IncrementCounter(string counterName, long value = 1);

    /// <summary>
    /// Tracks a timing/duration metric.
    /// </summary>
    void TrackTiming(string operationName, TimeSpan duration, IDictionary<string, string>? dimensions = null);

    /// <summary>
    /// Begins a timed operation. Dispose to complete tracking.
    /// </summary>
    ITimedOperation BeginTimedOperation(string operationName);

    /// <summary>
    /// Sets a user property for all subsequent events.
    /// </summary>
    void SetUserProperty(string propertyName, object value);

    /// <summary>
    /// Sets the user identifier for session tracking.
    /// </summary>
    void SetUserId(string? userId);

    /// <summary>
    /// Flushes any buffered analytics data.
    /// </summary>
    void Flush();

    /// <summary>
    /// Gets whether analytics collection is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables analytics collection.
    /// </summary>
    void SetEnabled(bool enabled);
}

/// <summary>
/// A timed operation that tracks duration on disposal.
/// </summary>
public interface ITimedOperation : IDisposable
{
    /// <summary>
    /// Adds a property to this operation.
    /// </summary>
    void AddProperty(string key, object value);

    /// <summary>
    /// Marks the operation as failed.
    /// </summary>
    void SetFailed(Exception? exception = null);
}
