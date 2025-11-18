using System.Threading;
using System.Threading.Tasks;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Default .NET host implementation of <see cref="IPluginProfileLoader"/> that
/// delegates to <see cref="PluginLoader"/> and uses AssemblyLoadContext under the hood.
/// </summary>
public sealed class DotNetPluginProfileLoader : IPluginProfileLoader
{
    private readonly PluginLoader _loader;

    /// <inheritdoc />
    public string Profile { get; }

    public DotNetPluginProfileLoader(PluginLoader loader, string profile)
    {
        _loader = loader ?? throw new System.ArgumentNullException(nameof(loader));
        Profile = profile ?? throw new System.ArgumentNullException(nameof(profile));
    }

    /// <inheritdoc />
    public Task<int> LoadAsync(CancellationToken ct = default)
    {
        var paths = _loader.GetConfiguredPluginPaths();
        return _loader.DiscoverAndLoadAsync(paths, Profile, ct);
    }

    /// <inheritdoc />
    public Task<bool> UnloadAsync(string pluginId, CancellationToken ct = default)
        => _loader.UnloadPluginAsync(pluginId, ct);

    /// <inheritdoc />
    public Task<bool> ReloadAsync(string pluginId, CancellationToken ct = default)
        => _loader.ReloadPluginAsync(pluginId, Profile, ct);
}
