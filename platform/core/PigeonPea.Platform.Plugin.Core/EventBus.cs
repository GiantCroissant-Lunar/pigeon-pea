namespace PigeonPea.Platform.Plugin.Core;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Thread-safe event bus implementation with sequential subscriber execution.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ILogger<EventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _subscribers;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _subscribers = new ConcurrentDictionary<Type, List<Func<object, Task>>>();
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        var eventType = typeof(T);

        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            // No subscribers - this is valid, just return
            _logger.LogDebug("No subscribers for event type {EventType}", eventType.Name);
            return;
        }

        _logger.LogDebug("Publishing event {EventType} to {Count} subscribers",
            eventType.Name, handlers.Count);

        // Execute subscribers sequentially
        foreach (var handler in handlers)
        {
            try
            {
                await handler(eventData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Event subscriber failed for {EventType}. Event: {@Event}",
                    eventType.Name,
                    eventData);
                // Continue to next subscriber - error isolation
            }
        }
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(T);

        // Wrap typed handler in object handler
        Func<object, Task> wrappedHandler = obj => handler((T)obj);

        _subscribers.AddOrUpdate(
            eventType,
            _ =>
            {
                _logger.LogDebug("First subscriber registered for event type {EventType}", eventType.Name);
                return new List<Func<object, Task>> { wrappedHandler };
            },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(wrappedHandler);
                    _logger.LogDebug("Subscriber registered for event type {EventType}. Total subscribers: {Count}",
                        eventType.Name, existing.Count);
                }
                return existing;
            }
        );
    }
}
