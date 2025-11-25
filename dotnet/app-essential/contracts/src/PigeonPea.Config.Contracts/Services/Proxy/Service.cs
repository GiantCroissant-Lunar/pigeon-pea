using System;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;
using PigeonPea.Config.Contracts.Services;

namespace PigeonPea.Config.Contracts.Services.Proxy;

/// <summary>
/// Proxy implementation of the configuration service.
/// Delegates to the highest-priority IService implementation registered in IRegistry.
/// </summary>
[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    private IService ResolveImplementation()
    {
        return _registry.Get<IService>();
    }

    public string? GetValue(string key)
        => ResolveImplementation().GetValue(key);

    public T? GetValue<T>(string key)
        => ResolveImplementation().GetValue<T>(key);

    public bool TryGetValue(string key, out string value)
        => ResolveImplementation().TryGetValue(key, out value);
}
