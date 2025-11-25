namespace PigeonPea.Platform.Plugin.Core;

using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Cross-ALC service registry with priority-based selection.
/// Thread-safe implementation for concurrent plugin loading.
/// </summary>
public sealed class ServiceRegistry : IRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<Type, List<ServiceRegistration>> _services = new();

    public void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class
    {
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        lock (_lock)
        {
            var serviceType = typeof(TService);
            if (!_services.ContainsKey(serviceType))
            {
                _services[serviceType] = new List<ServiceRegistration>();
            }

            var registration = new ServiceRegistration
            {
                Implementation = implementation,
                Metadata = metadata
            };

            _services[serviceType].Add(registration);
        }
    }

    public void Register<TService>(TService implementation, int priority = 100) where TService : class
    {
        Register(implementation, new ServiceMetadata { Priority = priority });
    }

    public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            if (!_services.TryGetValue(serviceType, out var registrations) || registrations.Count == 0)
            {
                throw new InvalidOperationException($"No service registered for type {serviceType.Name}");
            }

            return mode switch
            {
                SelectionMode.One => GetOne<TService>(registrations),
                SelectionMode.HighestPriority => GetHighestPriority<TService>(registrations),
                SelectionMode.All => throw new InvalidOperationException($"Use GetAll<{serviceType.Name}>() for multiple services"),
                _ => throw new ArgumentException($"Unknown selection mode: {mode}", nameof(mode))
            };
        }
    }

    public IEnumerable<TService> GetAll<TService>() where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            if (!_services.TryGetValue(serviceType, out var registrations))
            {
                return Enumerable.Empty<TService>();
            }

            return registrations
                .OrderByDescending(r => r.Metadata.Priority)
                .Select(r => (TService)r.Implementation)
                .ToList();
        }
    }

    public bool IsRegistered<TService>() where TService : class
    {
        lock (_lock)
        {
            var serviceType = typeof(TService);
            return _services.TryGetValue(serviceType, out var registrations) && registrations.Count > 0;
        }
    }

    public bool Unregister<TService>(TService implementation) where TService : class
    {
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));

        lock (_lock)
        {
            var serviceType = typeof(TService);
            if (!_services.TryGetValue(serviceType, out var registrations))
            {
                return false;
            }

            var removed = registrations.RemoveAll(r => ReferenceEquals(r.Implementation, implementation));
            return removed > 0;
        }
    }

    private TService GetOne<TService>(List<ServiceRegistration> registrations) where TService : class
    {
        if (registrations.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one service for {typeof(TService).Name}, but found {registrations.Count}");
        }

        return (TService)registrations[0].Implementation;
    }

    private TService GetHighestPriority<TService>(List<ServiceRegistration> registrations) where TService : class
    {
        var highest = registrations.OrderByDescending(r => r.Metadata.Priority).First();
        return (TService)highest.Implementation;
    }

    private sealed class ServiceRegistration
    {
        public object Implementation { get; init; } = null!;
        public ServiceMetadata Metadata { get; init; } = null!;
    }
}
