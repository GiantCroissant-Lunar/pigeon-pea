using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Default implementation of <see cref="IRegistry"/> supporting priority-based selection.
/// </summary>
public class ServiceRegistry : IRegistry
{
    private readonly object _lock = new();

    // Map service abstraction type -> list of (implementation, metadata)
    private readonly Dictionary<Type, List<(object impl, ServiceMetadata meta)>> _services = new();

    public void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class
    {
        if (implementation is null) throw new ArgumentNullException(nameof(implementation));
        if (metadata is null) throw new ArgumentNullException(nameof(metadata));

        var t = typeof(TService);
        lock (_lock)
        {
            if (!_services.TryGetValue(t, out var list))
            {
                list = new List<(object impl, ServiceMetadata meta)>();
                _services[t] = list;
            }

            list.Add((implementation, metadata));
            // Keep highest-priority first
            list.Sort((a, b) => b.meta.Priority.CompareTo(a.meta.Priority));
        }
    }

    public void Register<TService>(TService implementation, int priority = 100) where TService : class
        => Register(implementation, new ServiceMetadata { Priority = priority });

    public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class
    {
        var items = GetAll<TService>().ToList();
        if (items.Count == 0)
            throw new InvalidOperationException($"No implementation registered for {typeof(TService).FullName}");

        return mode switch
        {
            SelectionMode.One => items.Count == 1
                ? items[0]
                : throw new InvalidOperationException($"Expected exactly one implementation of {typeof(TService).FullName} but found {items.Count}"),
            SelectionMode.HighestPriority => items[0],
            SelectionMode.All => throw new InvalidOperationException("SelectionMode.All is invalid for single resolution. Use GetAll<TService>() instead."),
            _ => items[0]
        };
    }

    public IEnumerable<TService> GetAll<TService>() where TService : class
    {
        var t = typeof(TService);
        lock (_lock)
        {
            if (_services.TryGetValue(t, out var list))
            {
                // list already sorted by priority desc
                foreach (var (impl, _) in list)
                {
                    if (impl is TService svc)
                        yield return svc;
                }
            }
        }
    }

    public bool IsRegistered<TService>() where TService : class
    {
        var t = typeof(TService);
        lock (_lock)
        {
            return _services.TryGetValue(t, out var list) && list.Count > 0;
        }
    }

    public bool Unregister<TService>(TService implementation) where TService : class
    {
        if (implementation is null) return false;
        var t = typeof(TService);
        lock (_lock)
        {
            if (!_services.TryGetValue(t, out var list)) return false;
            var removed = list.RemoveAll(x => ReferenceEquals(x.impl, implementation)) > 0;
            if (list.Count == 0)
                _services.Remove(t);
            return removed;
        }
    }
}
