using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Simple in-memory pub/sub event bus.
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        var key = typeof(TEvent);
        var list = _handlers.GetOrAdd(key, _ => new List<Func<object, Task>>());
        lock (list)
        {
            list.Add(async o =>
            {
                if (o is TEvent evt)
                {
                    await handler(evt).ConfigureAwait(false);
                }
            });
        }
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default)
    {
        if (evt is null)
            throw new ArgumentNullException(nameof(evt));

        var key = typeof(TEvent);
        if (!_handlers.TryGetValue(key, out var list)) return;

        Func<object, Task>[] copy;
        lock (list)
        {
            copy = list.ToArray();
        }

        var payload = evt!;
        var exceptions = new List<Exception>();

        // Execute all handlers, collecting any exceptions
        foreach (var handler in copy)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await handler(payload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Collect exception but continue executing other handlers
                exceptions.Add(ex);
            }
        }

        // If any handlers failed, throw AggregateException with all failures
        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"One or more event handlers failed for {typeof(TEvent).FullName}",
                exceptions);
        }
    }
}
