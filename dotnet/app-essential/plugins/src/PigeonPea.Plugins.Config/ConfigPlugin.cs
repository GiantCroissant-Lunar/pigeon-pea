using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Config.Contracts;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.Plugins.Config;

/// <summary>
/// Plugin that exposes a configuration-backed implementation of the Config IService.
/// </summary>
public class ConfigPlugin : IPlugin
{
    private ILogger? _logger;
    private ConfigurationConfigService? _service;

    public string Id => "config-service";
    public string Name => "Configuration-backed Config Service";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new ConfigurationConfigService(context.Configuration);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "ConfigurationConfigService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Configuration-backed Config service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Config plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Config plugin stopped");
        return Task.CompletedTask;
    }
}
