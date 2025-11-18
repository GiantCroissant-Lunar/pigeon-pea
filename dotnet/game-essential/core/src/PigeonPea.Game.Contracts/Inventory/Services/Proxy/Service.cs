using System;
using Arch.Core;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Plugin.Attributes;
using PigeonPea.Game.Contracts.Inventory.Services;

namespace PigeonPea.Game.Contracts.Inventory.Services.Proxy;

/// <summary>
/// Proxy implementation of the game-level inventory service.
/// Delegates to the highest-priority IService implementation registered in IRegistry.
/// </summary>
[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    private IService ResolveImplementation()
    {
        // Use default selection mode (HighestPriority) to get the active inventory implementation.
        return _registry.Get<IService>();
    }

    public bool TryAddItem(Entity entity, string definitionId, int quantity)
        => ResolveImplementation().TryAddItem(entity, definitionId, quantity);

    public bool TryRemoveItem(Entity entity, string definitionId, int quantity)
        => ResolveImplementation().TryRemoveItem(entity, definitionId, quantity);

    public bool TryMoveItem(Entity entity, int fromSlotIndex, int toSlotIndex)
        => ResolveImplementation().TryMoveItem(entity, fromSlotIndex, toSlotIndex);

    public bool TryEquip(Entity entity, int fromSlotIndex, string equipmentSlotId)
        => ResolveImplementation().TryEquip(entity, fromSlotIndex, equipmentSlotId);

    public bool TryUnequip(Entity entity, string equipmentSlotId)
        => ResolveImplementation().TryUnequip(entity, equipmentSlotId);

    public InventoryView GetInventory(Entity entity)
        => ResolveImplementation().GetInventory(entity);
}
