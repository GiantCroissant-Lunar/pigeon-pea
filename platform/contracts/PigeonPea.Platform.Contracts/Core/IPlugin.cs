namespace PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Plugin lifecycle contract. Plugins implement this interface to integrate with the host.
/// </summary>
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }

    /// <summary>
    /// Initialize plugin with runtime context. Use context.Registry to register services.
    /// </summary>
    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

    /// <summary>
    /// Start async background work (e.g., hosted services, event subscriptions).
    /// </summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>
    /// Stop async work and release resources before unload.
    /// </summary>
    Task StopAsync(CancellationToken ct = default);
}
