namespace PigeonPea.Platform.Contracts.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// Plugin initialization context provided by host at runtime.
/// Isolates ALC boundary by not exposing IServiceCollection directly.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Service registry for cross-ALC service registration (priority-based, runtime type matching).
    /// </summary>
    IRegistry Registry { get; }

    /// <summary>
    /// Host configuration (read-only).
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Logger instance for this plugin (category = plugin ID).
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Host services surface (events, logging, service provider for host-provided services).
    /// </summary>
    IPluginHost Host { get; }
}
