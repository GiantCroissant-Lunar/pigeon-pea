using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Resource.Services;

namespace PigeonPea.Plugins.ResourceLoader;

/// <summary>
/// Minimal plugin that provides an in-memory implementation of the Resource IService.
/// This is a Tier-3 service that registers itself into the shared plugin registry.
/// </summary>
public class ResourceLoaderPlugin : IPlugin
{
    private ILogger? _logger;
    private InMemoryResourceService? _service;

    public string Id => "resource-loader";
    public string Name => "In-Memory Resource Loader";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new InMemoryResourceService(_logger);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "InMemoryResourceService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("In-memory resource service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Resource loader plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Resource loader plugin stopped");
        return Task.CompletedTask;
    }
}
