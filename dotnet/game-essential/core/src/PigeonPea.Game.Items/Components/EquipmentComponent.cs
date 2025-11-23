using System.Collections.Generic;
using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Game.Inventory.Components;

/// <summary>
/// ECS component that stores equipped items.
/// Maps slot IDs (e.g. "Head", "Body") to ItemInstances.
/// </summary>
public struct EquipmentComponent
{
    public Dictionary<string, ItemInstance> Slots;

    public EquipmentComponent()
    {
        Slots = new Dictionary<string, ItemInstance>();
    }
}
