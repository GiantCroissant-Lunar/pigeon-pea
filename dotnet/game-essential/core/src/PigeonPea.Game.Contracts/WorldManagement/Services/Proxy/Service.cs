using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;
using PigeonPea.Game.Contracts.WorldManagement.Services;

namespace PigeonPea.Game.Contracts.WorldManagement.Services.Proxy;

[RealizeService(typeof(IService))]
public partial class Service : IService
{
    private readonly IRegistry _registry;
    public Service(IRegistry registry) => _registry = registry;
}



