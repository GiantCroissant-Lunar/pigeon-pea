using System;
using System.Threading;
using System.Threading.Tasks;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Simple pub/sub event bus contract for plugin messaging.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe an asynchronous handler for the specified event type.
    /// </summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler);

    /// <summary>
    /// Publish an event to all subscribers of the event type.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default);
}
