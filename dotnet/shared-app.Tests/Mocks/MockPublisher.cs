namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Mock publisher for testing event publishing patterns.
/// This is a simplified mock that tracks published events for verification in tests.
/// For MessagePipe-specific testing, use the actual IPublisher from DI container.
/// </summary>
/// <typeparam name="T">The type of event to publish.</typeparam>
public class MockPublisher<T>
{
    /// <summary>
    /// Gets the list of events that were published.
    /// </summary>
    public List<T> PublishedEvents { get; } = new();

    /// <summary>
    /// Gets the number of times Publish was called.
    /// </summary>
    public int PublishCallCount => PublishedEvents.Count;

    /// <summary>
    /// Publishes an event.
    /// </summary>
    /// <param name="message">The event to publish.</param>
    public void Publish(T message)
    {
        PublishedEvents.Add(message);
    }

    /// <summary>
    /// Resets the mock publisher state for a new test.
    /// </summary>
    public void Reset()
    {
        PublishedEvents.Clear();
    }

    /// <summary>
    /// Gets the last published event, or default if no events have been published.
    /// </summary>
    public T? GetLastPublishedEvent()
    {
        return PublishedEvents.Count > 0 ? PublishedEvents[^1] : default;
    }
}
