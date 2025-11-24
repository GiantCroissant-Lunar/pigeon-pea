using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;
using PigeonPea.Game.Contracts.Character.Services;

namespace PigeonPea.Game.Contracts.Character.Services.Proxy;

[RealizeService(typeof(IService))]
public partial class Service : IService
{
    private readonly IRegistry _registry;
    public Service(IRegistry registry) => _registry = registry;
}
