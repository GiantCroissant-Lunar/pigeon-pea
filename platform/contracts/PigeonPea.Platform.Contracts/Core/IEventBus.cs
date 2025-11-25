namespace PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Event bus for publish-subscribe communication between plugins.
/// Enables loose coupling via asynchronous event delivery.
/// </summary>
/// <remarks>
/// <para>
/// The event bus provides a central mechanism for plugins to communicate without
/// direct dependencies. Publishers emit events, and subscribers receive them
/// asynchronously.
/// </para>
/// <para>
/// Key characteristics:
/// - Sequential execution: Subscribers execute in order, not in parallel
/// - Error isolation: Exceptions in one subscriber don't affect others
/// - No persistence: Events are in-memory only, not stored
/// - Thread-safe: Supports concurrent publishing from multiple plugins
/// </para>
/// </remarks>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to all registered subscribers.
    /// </summary>
    /// <typeparam name="T">Event type (must be a class)</typeparam>
    /// <param name="eventData">Event data to publish</param>
    /// <returns>Task that completes when all subscribers have been notified</returns>
    /// <remarks>
    /// <para>
    /// Subscribers are executed sequentially (not in parallel) to ensure predictable
    /// ordering. If a subscriber throws an exception, it is logged and execution
    /// continues to the next subscriber.
    /// </para>
    /// <para>
    /// If no subscribers exist for the event type, this method completes successfully
    /// without error.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if eventData is null</exception>
    Task PublishAsync<T>(T eventData) where T : class;

    /// <summary>
    /// Subscribe to events of a specific type.
    /// </summary>
    /// <typeparam name="T">Event type to subscribe to</typeparam>
    /// <param name="handler">Async handler function invoked when events are published</param>
    /// <remarks>
    /// <para>
    /// Handlers are typically registered during plugin initialization (InitializeAsync).
    /// The handler will be invoked asynchronously whenever an event of type T is published.
    /// </para>
    /// <para>
    /// Handlers should complete quickly and avoid blocking operations. For long-running
    /// work, consider offloading to a background task.
    /// </para>
    /// <para>
    /// Note: There is no unsubscribe mechanism in the initial version. Subscribers
    /// are expected to live for the lifetime of the application.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if handler is null</exception>
    void Subscribe<T>(Func<T, Task> handler) where T : class;
}
