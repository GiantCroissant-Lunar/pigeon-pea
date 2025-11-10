namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Mock subscriber for testing event subscription patterns.
/// This is a simplified mock that allows tracking subscriptions and manually triggering events.
/// For MessagePipe-specific testing, use the actual ISubscriber from DI container.
/// </summary>
/// <typeparam name="T">The type of event to subscribe to.</typeparam>
public class MockSubscriber<T>
{
    private readonly List<Action<T>> _handlers = new();
    private readonly List<IDisposable> _subscriptions = new();

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public int SubscriptionCount => _handlers.Count;

    /// <summary>
    /// Subscribes to events with the specified handler.
    /// </summary>
    /// <param name="handler">The handler to call when an event is received.</param>
    /// <returns>A disposable subscription that can be disposed to unsubscribe.</returns>
    public IDisposable Subscribe(Action<T> handler)
    {
        _handlers.Add(handler);
        var subscription = new MockSubscription<T>(this, handler);
        _subscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>
    /// Manually triggers all subscribed handlers with the specified event.
    /// </summary>
    /// <param name="message">The event to send to all handlers.</param>
    public void TriggerEvent(T message)
    {
        foreach (var handler in _handlers.ToList())
        {
            handler(message);
        }
    }

    /// <summary>
    /// Removes a handler from the subscription list.
    /// </summary>
    internal void Unsubscribe(Action<T> handler)
    {
        _handlers.Remove(handler);
    }

    /// <summary>
    /// Resets the mock subscriber state for a new test.
    /// </summary>
    public void Reset()
    {
        _handlers.Clear();
        _subscriptions.Clear();
    }
}

/// <summary>
/// Represents a subscription that can be disposed to unsubscribe.
/// </summary>
/// <typeparam name="T">The type of event being subscribed to.</typeparam>
internal class MockSubscription<T> : IDisposable
{
    private readonly MockSubscriber<T> _subscriber;
    private readonly Action<T> _handler;
    private bool _disposed;

    public MockSubscription(MockSubscriber<T> subscriber, Action<T> handler)
    {
        _subscriber = subscriber;
        _handler = handler;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _subscriber.Unsubscribe(_handler);
            _disposed = true;
        }
    }
}
