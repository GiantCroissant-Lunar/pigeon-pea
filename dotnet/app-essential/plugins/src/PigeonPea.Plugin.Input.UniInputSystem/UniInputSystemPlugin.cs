using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Input.Contracts;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.Plugin.Input.UniInputSystem;

public sealed class UniInputSystemPlugin : IPlugin
{
    private ILogger? _logger;
    private UniInputSystemService? _service;

    public string Id => "input-uni";
    public string Name => "Uni Input System";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new UniInputSystemService(context.Registry, _logger);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "UniInputSystemService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Uni Input System service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("UniInputSystem plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("UniInputSystem plugin stopped");
        _service?.Dispose();
        return Task.CompletedTask;
    }
}
