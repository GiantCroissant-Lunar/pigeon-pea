using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Inventory.Services;
using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Plugins.Inventory.Basic;

/// <summary>
/// Plugin that exposes the basic ECS-backed inventory service implementation.
/// </summary>
public sealed class InventoryBasicPlugin : IPlugin
{
    private ILogger? _logger;
    private BasicInventoryService? _service;

    public string Id => "inventory-basic";
    public string Name => "Basic Inventory Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // TODO: Wire ItemDefinition registry from configuration or a shared database.
        // For now, use an empty dictionary as a placeholder.
        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);

        _service = new BasicInventoryService(definitions);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "BasicInventoryService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Basic inventory service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("InventoryBasic plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("InventoryBasic plugin stopped");
        return Task.CompletedTask;
    }
}
