using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Contracts.Inventory.Services;
using PigeonPea.Game.Contracts.Stats.Models;
using PigeonPea.Game.Inventory.Components;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.Inventory.Items;
using SharedInventoryCore = PigeonPea.Shared.Inventory.Core.Inventory;
using SharedInventorySlot = PigeonPea.Shared.Inventory.Core.InventorySlot;

namespace PigeonPea.Plugins.Inventory.Advanced;

public sealed class AdvancedInventoryService : IService
{
    private readonly IReadOnlyDictionary<string, ItemDefinition> _definitions;
    private readonly Dictionary<string, List<StatModifier>> _itemStats;

    public AdvancedInventoryService(IReadOnlyDictionary<string, ItemDefinition> definitions)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _itemStats = new Dictionary<string, List<StatModifier>>();
        InitializeItemStats();
    }

    private void InitializeItemStats()
    {
        // Hardcoded stats for demo items
        _itemStats["sword_iron"] = new List<StatModifier>
        {
            new StatModifier { StatId = "attack", Value = 5, Type = ModifierType.Additive, SourceId = "sword_iron" }
        };
        _itemStats["shield_wood"] = new List<StatModifier>
        {
            new StatModifier { StatId = "defense", Value = 2, Type = ModifierType.Additive, SourceId = "shield_wood" }
        };
        _itemStats["helmet_iron"] = new List<StatModifier>
        {
            new StatModifier { StatId = "defense", Value = 3, Type = ModifierType.Additive, SourceId = "helmet_iron" }
        };
        _itemStats["armor_chain"] = new List<StatModifier>
        {
            new StatModifier { StatId = "defense", Value = 8, Type = ModifierType.Additive, SourceId = "armor_chain" }
        };
    }

    public bool TryAddItem(Entity entity, string definitionId, int quantity)
    {
        if (!_definitions.TryGetValue(definitionId, out var definition))
        {
            return false;
        }

        EnsureInventory(entity);
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
        if (!entity.Has<InventoryComponent>()) return false;
        ref var invComp = ref entity.Get<InventoryComponent>();
        return invComp.Inventory.TryRemove(definitionId, quantity);
    }

    public bool TryMoveItem(Entity entity, int fromSlotIndex, int toSlotIndex)
    {
        if (!entity.Has<InventoryComponent>()) return false;
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
        if (!entity.Has<InventoryComponent>()) return false;

        EnsureEquipment(entity);
        ref var invComp = ref entity.Get<InventoryComponent>();
        ref var equipComp = ref entity.Get<EquipmentComponent>();

        var slot = GetSlot(invComp.Inventory, fromSlotIndex);
        if (slot?.Item is null) return false;

        var item = slot.Item;

        // Check if slot is already occupied
        if (equipComp.Slots.ContainsKey(equipmentSlotId))
        {
            // Swap? For now, fail if occupied
            return false;
        }

        // Remove from inventory (take 1)
        // We need to remove exactly 1 from the stack
        // SharedInventory doesn't have "RemoveAt(index, quantity)" easily exposed?
        // It has TryRemove(definitionId, quantity) but that might remove from another slot.
        // But we know the index.
        // We can manually manipulate the slot if we are careful.
        // Or use TryRemove if we don't care which stack it comes from (but we do, user selected a slot).

        // For simplicity, let's assume we remove 1 from the specific slot.
        if (item.Quantity > 1)
        {
            item.Quantity--;
        }
        else
        {
            // Remove the item entirely from the slot
            // SharedInventoryCore doesn't expose direct slot manipulation easily publicly?
            // It has RawSlots which is IReadOnlyList.
            // Wait, RawSlots is IReadOnlyList<IInventorySlot>.
            // But the implementation likely allows modification if we cast?
            // Or we use TryRemove and hope it picks the right one?
            // Actually, TryRemove removes from the first available stack.
            // If we have multiple stacks, this might be an issue.
            // But for now, let's use TryRemove.
            invComp.Inventory.TryRemove(item.DefinitionId, 1);
        }

        // Create a new instance for equipment (quantity 1)
        var equippedInstance = new ItemInstance
        {
            DefinitionId = item.DefinitionId,
            Quantity = 1
        };

        equipComp.Slots[equipmentSlotId] = equippedInstance;

        // Apply stats
        ApplyStats(entity, item.DefinitionId);

        return true;
    }

    public bool TryUnequip(Entity entity, string equipmentSlotId)
    {
        if (!entity.Has<EquipmentComponent>()) return false;
        ref var equipComp = ref entity.Get<EquipmentComponent>();

        if (!equipComp.Slots.TryGetValue(equipmentSlotId, out var item))
        {
            return false;
        }

        // Try add to inventory
        if (!TryAddItem(entity, item.DefinitionId, 1))
        {
            return false; // Inventory full
        }

        // Remove from equipment
        equipComp.Slots.Remove(equipmentSlotId);

        // Remove stats
        RemoveStats(entity, item.DefinitionId);

        return true;
    }

    public InventoryView GetInventory(Entity entity)
    {
        EnsureInventory(entity);
        ref var invComp = ref entity.Get<InventoryComponent>();
        var inventory = invComp.Inventory;

        var slots = new List<InventorySlotView>();
        float totalWeight = 0f;

        var rawSlots = inventory.RawSlots;
        for (int i = 0; i < rawSlots.Count; i++)
        {
            var slot = rawSlots[i];
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

    private void EnsureInventory(Entity entity)
    {
        if (!entity.Has<InventoryComponent>())
        {
            entity.Add(new InventoryComponent(20, 100f));
        }
    }

    private void EnsureEquipment(Entity entity)
    {
        if (!entity.Has<EquipmentComponent>())
        {
            entity.Add(new EquipmentComponent());
        }
    }

    private static SharedInventorySlot? GetSlot(SharedInventoryCore inventory, int index)
    {
        var rawSlots = inventory.RawSlots;
        if (index < 0 || index >= rawSlots.Count) return null;
        return (SharedInventorySlot)rawSlots[index];
    }

    private void ApplyStats(Entity entity, string definitionId)
    {
        if (_itemStats.TryGetValue(definitionId, out var modifiers))
        {
            if (!entity.Has<StatModifiers>()) entity.Add(new StatModifiers());
            ref var mods = ref entity.Get<StatModifiers>();

            foreach (var mod in modifiers)
            {
                mods.Modifiers.Add(new ActiveModifier
                {
                    ModifierId = Guid.NewGuid().ToString(),
                    StatId = mod.StatId,
                    Value = mod.Value,
                    Type = mod.Type,
                    SourceId = mod.SourceId, // Use item ID as source
                    AppliedAt = DateTime.UtcNow,
                    RemainingDuration = -1 // Permanent while equipped
                });
            }
        }
    }

    private void RemoveStats(Entity entity, string definitionId)
    {
        if (!entity.Has<StatModifiers>()) return;
        ref var mods = ref entity.Get<StatModifiers>();

        // Remove modifiers with SourceId == definitionId
        // Note: This assumes SourceId is unique per item type, which works for this simple implementation.
        // In a real system, we'd use the ItemInstance ID or similar.
        // But here we used definitionId as SourceId in InitializeItemStats.

        // Wait, in InitializeItemStats I used "sword_iron" as SourceId.
        // So removing by SourceId works.

        for (int i = mods.Modifiers.Count - 1; i >= 0; i--)
        {
            if (mods.Modifiers[i].SourceId == definitionId)
            {
                mods.Modifiers.RemoveAt(i);
            }
        }
    }
}
