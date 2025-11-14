using System.Threading;
using System.Threading.Tasks;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Core plugin lifecycle contract.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-friendly plugin name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Semantic version string (e.g., 1.0.0).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Initialize the plugin and register services.
    /// </summary>
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

    /// <summary>
    /// Start plugin runtime behavior.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stop and cleanup plugin resources.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);
}
