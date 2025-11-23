namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Host-provided services and utilities exposed to plugins.
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// Gets the current host profile (e.g., dotnet.console, dotnet.windows).
    /// </summary>
    string Profile { get; }

    /// <summary>
    /// Requests the host to gracefully restart a plugin by id.
    /// Actual implementation is host-defined.
    /// </summary>
    Task<bool> RestartPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Gets the root service provider for the current host.
    /// This can be used by plugins to resolve services and, where appropriate,
    /// create scene-specific scopes.
    /// </summary>
    IServiceProvider Services { get; }
}
