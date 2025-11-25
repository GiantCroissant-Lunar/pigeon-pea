namespace PigeonPea.Platform.Plugin.Core;

using Microsoft.Extensions.Logging;
using PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Default implementation of IPluginHost.
/// </summary>
public sealed class PluginHost : IPluginHost
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _services;

    public PluginHost(ILoggerFactory loggerFactory, IServiceProvider services)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }

    public IServiceProvider Services => _services;

    [Obsolete("Use IEventBus.PublishAsync instead. Retrieve IEventBus from IRegistry.")]
    public void PublishEvent<T>(T evt)
    {
        // Obsolete: Retained for backward compatibility
        // Use IEventBus.PublishAsync instead
        var logger = _loggerFactory.CreateLogger<PluginHost>();
        logger.LogWarning("PublishEvent is obsolete. Use IEventBus.PublishAsync instead. Event type: {EventType}", typeof(T).Name);
    }
}
