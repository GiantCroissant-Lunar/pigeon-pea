using System.Collections.Generic;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugin.Inventory.Advanced;

/// <summary>
/// Declares HUD panels contributed by the advanced inventory plugin.
/// </summary>
public sealed class InventoryHudPanelDescriptorProvider : IHudPanelDescriptorProvider
{
    public IEnumerable<HudPanelDescriptor> GetPanels()
    {
        yield return new HudPanelDescriptor
        {
            Id = "inventory",
            DisplayName = "Inventory (Advanced)",
            Order = 100,
            Region = "right"
        };
    }
}
