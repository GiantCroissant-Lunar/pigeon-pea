using System;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;

namespace PigeonPea.Contracts.Input.Services.Proxy;

[RealizeService(typeof(IService))]
public partial class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
