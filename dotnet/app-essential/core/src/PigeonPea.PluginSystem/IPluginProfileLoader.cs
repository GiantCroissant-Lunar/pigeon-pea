using System.Threading;
using System.Threading.Tasks;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Abstraction over host-specific plugin loading strategies (e.g., AssemblyLoadContext, HybridCLR).
/// </summary>
public interface IPluginProfileLoader
{
    /// <summary>
    /// Profile identifier (e.g., "dotnet.console", "unity").
    /// </summary>
    string Profile { get; }

    /// <summary>
    /// Discover and load plugins for this profile.
    /// Returns the number of successfully loaded plugins.
    /// </summary>
    Task<int> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Attempt to unload a plugin by its identifier.
    /// </summary>
    Task<bool> UnloadAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Attempt to reload a plugin by its identifier.
    /// </summary>
    Task<bool> ReloadAsync(string pluginId, CancellationToken ct = default);
}
