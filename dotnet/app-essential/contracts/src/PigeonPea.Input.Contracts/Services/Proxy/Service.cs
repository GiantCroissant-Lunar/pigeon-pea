using System;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;
using PigeonPea.Input.Contracts.Services;

namespace PigeonPea.Input.Contracts.Services.Proxy;

/// <summary>
/// Proxy implementation of the input service.
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

    public bool IsActionPressed(string actionId)
        => ResolveImplementation().IsActionPressed(actionId);

    public float GetAxis(string axisId)
        => ResolveImplementation().GetAxis(axisId);
}
