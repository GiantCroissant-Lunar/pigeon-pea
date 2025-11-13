using System.Threading;
using System.Threading.Tasks;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Default host services exposed to plugins.
/// </summary>
public class PluginHost : IPluginHost
{
    public string Profile { get; }

    private readonly System.IServiceProvider _serviceProvider;

    public PluginHost(string profile, System.IServiceProvider serviceProvider)
    {
        Profile = profile;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> RestartPluginAsync(string pluginId, CancellationToken ct = default)
    {
        // Resolve loader on demand to avoid construction-time cycles.
        var loader = (PluginLoader?)_serviceProvider.GetService(typeof(PluginLoader));
        if (loader is null) return false;

        return await loader.ReloadPluginAsync(pluginId, Profile, ct).ConfigureAwait(false);
    }
}
