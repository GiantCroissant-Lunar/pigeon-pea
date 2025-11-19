using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Contracts.Inventory.Services;
using PigeonPea.Game.Inventory.Components;
using PigeonPea.Shared.Inventory.Core;
using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Plugins.Inventory.Basic;

/// <summary>
/// Basic implementation of the game-level inventory service.
/// Uses InventoryComponent + PigeonPea.Shared.Inventory mechanics.
/// </summary>
public sealed class BasicInventoryService : IService
{
    private readonly IReadOnlyDictionary<string, ItemDefinition> _definitions;

    public BasicInventoryService(IReadOnlyDictionary<string, ItemDefinition> definitions)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    public bool TryAddItem(Entity entity, string definitionId, int quantity)
    {
        if (!_definitions.TryGetValue(definitionId, out var definition))
        {
            return false;
        }

        ref var invComp = ref entity.Get<InventoryComponent>();
        var instance = new ItemInstance
        {
            DefinitionId = definition.Id,
            Quantity = quantity,
        };

        return invComp.Inventory.TryAdd(instance, definition);
    }

    public bool TryRemoveItem(Entity entity, string definitionId, int quantity)
    {
        ref var invComp = ref entity.Get<InventoryComponent>();
        return invComp.Inventory.TryRemove(definitionId, quantity);
    }

    public bool TryMoveItem(Entity entity, int fromSlotIndex, int toSlotIndex)
    {
        // Movement semantics depend on MaxStack, so we need the definition.
        ref var invComp = ref entity.Get<InventoryComponent>();
        var fromSlot = GetSlot(invComp.Inventory, fromSlotIndex);
        if (fromSlot?.Item is null)
        {
            return false;
        }

        var defId = fromSlot.Item.DefinitionId;
        if (!_definitions.TryGetValue(defId, out var definition))
        {
            return false;
        }

        return invComp.Inventory.TryMove(definition, fromSlotIndex, toSlotIndex);
    }

    public bool TryEquip(Entity entity, int fromSlotIndex, string equipmentSlotId)
    {
        // Equipment model is not implemented yet; return false for now.
        return false;
    }

    public bool TryUnequip(Entity entity, string equipmentSlotId)
    {
        // Equipment model is not implemented yet; return false for now.
        return false;
    }

    public InventoryView GetInventory(Entity entity)
    {
        ref var invComp = ref entity.Get<InventoryComponent>();
        var inventory = invComp.Inventory;

        var slots = new List<InventorySlotView>(inventory.Slots.Count);
        float totalWeight = 0f;

        foreach (var slot in inventory.Slots)
        {
            var item = slot.Item;
            string? definitionId = null;
            var quantity = 0;

            if (item != null)
            {
                definitionId = item.DefinitionId;
                quantity = item.Quantity;

                if (_definitions.TryGetValue(item.DefinitionId, out var def))
                {
                    totalWeight += def.Weight * item.Quantity;
                }
            }

            slots.Add(new InventorySlotView
            {
                SlotIndex = slot.Index,
                DefinitionId = definitionId,
                Quantity = quantity,
            });
        }

        return new InventoryView
        {
            MaxSlots = inventory.MaxSlots,
            MaxWeight = inventory.MaxWeight,
            CurrentWeight = totalWeight,
            Slots = slots,
        };
    }

    private static InventorySlot? GetSlot(Inventory inventory, int index)
    {
        if (index < 0 || index >= inventory.Slots.Count)
        {
            return null;
        }

        return (InventorySlot)inventory.Slots[index];
    }
}
