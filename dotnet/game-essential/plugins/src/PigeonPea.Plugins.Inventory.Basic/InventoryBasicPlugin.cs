using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Inventory.Services;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Plugin.Inventory.Basic;

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
        // For now, seed a small in-memory set of item definitions for testing.
        // Map both the internal ID and the in-game item name to the same definition
        // so game logic can pass item.Name as the definitionId.
        var smallHealthPotion = new ItemDefinition
        {
            Id = "health_potion_small",
            Name = "Small Health Potion",
            Type = ItemType.Consumable,
            Rarity = ItemRarity.Common,
            Weight = 0.1f,
            MaxStack = 10,
        };

        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["health_potion_small"] = smallHealthPotion,
            ["Health Potion"] = smallHealthPotion,
        };

        _service = new BasicInventoryService(definitions);

        // Register inventory HUD panel descriptors so HUD hosts can discover them
        var hudPanels = new InventoryHudPanelDescriptorProvider();
        context.Registry.Register<IHudPanelDescriptorProvider>(
            hudPanels,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "InventoryHudPanels",
                Version = Version,
                PluginId = Id
            });

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
