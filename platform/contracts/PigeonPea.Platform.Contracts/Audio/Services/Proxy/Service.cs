using PigeonPea.Platform.Contracts.Audio.Services;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Contracts.Core.Attributes;

namespace PigeonPea.Platform.Contracts.Audio.Services.Proxy;

[RealizeService(typeof(IService))]
public partial class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
