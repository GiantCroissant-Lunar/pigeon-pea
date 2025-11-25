using PigeonPea.Platform.Contracts.Core;

namespace PigeonPea.Platform.Core.Plugin;

public class PluginContext : IPluginContext
{
    public IRegistry Registry { get; }
    public IServiceProvider Services { get; }

    public PluginContext(IRegistry registry, IServiceProvider? services = null)
    {
        Registry = registry;
        Services = services ?? new EmptyServiceProvider();
    }

    private class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
