using System.Collections.Generic;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Inventory.Basic;

/// <summary>
/// Declares HUD panels contributed by the basic inventory plugin.
/// </summary>
public sealed class InventoryHudPanelDescriptorProvider : IHudPanelDescriptorProvider
{
    public IEnumerable<HudPanelDescriptor> GetPanels()
    {
        yield return new HudPanelDescriptor
        {
            Id = "inventory",
            DisplayName = "Inventory",
            Order = 100,
            Region = "right"
        };
    }
}
