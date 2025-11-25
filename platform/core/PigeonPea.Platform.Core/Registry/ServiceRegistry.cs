using PigeonPea.Platform.Contracts.Core;

namespace PigeonPea.Platform.Core.Registry;

public class ServiceRegistry : IRegistry
{
    private readonly Dictionary<Type, List<(object Instance, int Priority)>> _services = new();

    public void Register<TService>(TService implementation, int priority = 0) where TService : class
    {
        var type = typeof(TService);
        if (!_services.ContainsKey(type))
            _services[type] = new();
        
        _services[type].Add((implementation, priority));
        _services[type].Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public TService Get<TService>() where TService : class
    {
        return TryGet<TService>() 
            ?? throw new InvalidOperationException($"No service registered for {typeof(TService).Name}");
    }

    public TService? TryGet<TService>() where TService : class
    {
        var type = typeof(TService);
        if (_services.TryGetValue(type, out var list) && list.Count > 0)
            return (TService)list[0].Instance;
        return null;
    }

    public IEnumerable<TService> GetAll<TService>() where TService : class
    {
        var type = typeof(TService);
        if (_services.TryGetValue(type, out var list))
            return list.Select(x => (TService)x.Instance);
        return Enumerable.Empty<TService>();
    }
}
