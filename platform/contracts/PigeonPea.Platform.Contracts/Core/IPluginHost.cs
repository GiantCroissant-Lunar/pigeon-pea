namespace PigeonPea.Platform.Contracts.Core;

using Microsoft.Extensions.Logging;

/// <summary>
/// Host surface available to plugins for logging, events, and services.
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// Create a logger with the specified category name.
    /// </summary>
    ILogger CreateLogger(string categoryName);

    /// <summary>
    /// Service provider for host-provided services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Publish an event to the host event bus.
    /// </summary>
    /// <remarks>
    /// OBSOLETE: Use IEventBus.PublishAsync instead. This method is retained for backward compatibility.
    /// </remarks>
    [Obsolete("Use IEventBus.PublishAsync instead. Retrieve IEventBus from IRegistry.")]
    void PublishEvent<T>(T evt);
}
