using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Default implementation of <see cref="IPluginContext"/> passed to plugins during initialization.
/// </summary>
public class PluginContext : IPluginContext
{
    public IRegistry Registry { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public IPluginHost Host { get; }

    public PluginContext(IRegistry registry, IConfiguration configuration, ILogger logger, IPluginHost host)
    {
        Registry = registry;
        Configuration = configuration;
        Logger = logger;
        Host = host;
    }
}
