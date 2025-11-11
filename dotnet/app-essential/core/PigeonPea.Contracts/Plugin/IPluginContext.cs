using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Initialization context provided to plugins during <see cref="IPlugin.InitializeAsync"/>.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Cross-ALC service registry for publishing and consuming services.
    /// </summary>
    IRegistry Registry { get; }

    /// <summary>
    /// Host configuration.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Logger scoped for this plugin.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Host-provided services and utilities.
    /// </summary>
    IPluginHost Host { get; }
}
