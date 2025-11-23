using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Inventory.Services;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Plugins.Inventory.Advanced;

public class InventoryAdvancedPlugin : IPlugin
{
    public string Id => "inventory-advanced";
    public string Name => "Advanced Inventory Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        // Register item definitions (could be loaded from JSON in a real scenario)
        var definitions = new Dictionary<string, ItemDefinition>
        {
            ["health_potion_small"] = new ItemDefinition { Id = "health_potion_small", Name = "Small Health Potion", Weight = 0.5f, MaxStack = 10 },
            ["sword_iron"] = new ItemDefinition { Id = "sword_iron", Name = "Iron Sword", Weight = 3.0f, MaxStack = 1 },
            ["shield_wood"] = new ItemDefinition { Id = "shield_wood", Name = "Wooden Shield", Weight = 2.0f, MaxStack = 1 },
            ["helmet_iron"] = new ItemDefinition { Id = "helmet_iron", Name = "Iron Helmet", Weight = 1.5f, MaxStack = 1 },
            ["armor_chain"] = new ItemDefinition { Id = "armor_chain", Name = "Chainmail Armor", Weight = 8.0f, MaxStack = 1 }
        };

        // Register the service
        var service = new AdvancedInventoryService(definitions);
        context.Registry.Register<IService>(service);

        // Register HUD panel provider
        context.Registry.Register<IHudPanelDescriptorProvider>(new InventoryHudPanelDescriptorProvider());

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
}
