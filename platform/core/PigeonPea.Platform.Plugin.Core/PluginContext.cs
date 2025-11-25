namespace PigeonPea.Platform.Plugin.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Implementation of IPluginContext for plugin initialization.
/// </summary>
public sealed class PluginContext : IPluginContext
{
    public PluginContext(
        IRegistry registry,
        IConfiguration configuration,
        ILogger logger,
        IPluginHost host)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public IRegistry Registry { get; }
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public IPluginHost Host { get; }
}
