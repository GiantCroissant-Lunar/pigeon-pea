using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Shared.Inventory.Core;

/// <summary>
/// Core inventory model: fixed number of slots with optional stacking.
/// This type is engine-agnostic and does not know about ECS or plugins.
/// </summary>
public sealed class Inventory
{
    private readonly List<InventorySlot> _slots;

    public int MaxSlots { get; }

    /// <summary>
    /// Maximum total weight of all items. 0 or negative means "no limit".
    /// Weight enforcement is left to higher layers for now.
    /// </summary>
    public float MaxWeight { get; }

    /// <summary>
    /// Read-only view of all slots.
    /// </summary>
    public IReadOnlyList<InventorySlot> Slots => _slots;

    /// <summary>
    /// Concrete slot list for internal consumers that need index-based access
    /// without relying on interface-based enumeration.
    /// </summary>
    public List<InventorySlot> RawSlots => _slots;

    public Inventory(int maxSlots, float maxWeight = 0f)
    {
        if (maxSlots <= 0) throw new ArgumentOutOfRangeException(nameof(maxSlots));

        MaxSlots = maxSlots;
        MaxWeight = maxWeight;
        _slots = Enumerable.Range(0, maxSlots)
                            .Select(i => new InventorySlot(i))
                            .ToList();
    }

    /// <summary>
    /// Gets the total quantity of a given item definition across all slots.
    /// </summary>
    public int GetTotalQuantity(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("DefinitionId must be provided", nameof(definitionId));

        var total = 0;
        foreach (var slot in _slots)
        {
            var item = slot.Item;
            if (item != null && item.DefinitionId == definitionId)
            {
                total += item.Quantity;
            }
        }
        return total;
    }

    /// <summary>
    /// Attempts to add the entire item stack to the inventory.
    /// Returns true only if the full quantity can be placed (no partial adds).
    /// </summary>
    public bool TryAdd(ItemInstance instance, ItemDefinition definition)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (instance.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(instance.Quantity));
        if (!string.Equals(instance.DefinitionId, definition.Id, StringComparison.Ordinal))
            throw new ArgumentException("Instance.DefinitionId must match definition.Id", nameof(instance));

        // First, compute how many items we CAN fit given current stacks and empty slots.
        var capacity = CalculateAvailableCapacity(definition);
        if (capacity < instance.Quantity)
        {
            // Not enough room for the whole stack; do not modify inventory.
            return false;
        }

        var remaining = instance.Quantity;

        // 1) Fill existing stacks of the same item up to MaxStack.
        foreach (var slot in _slots)
        {
            if (remaining <= 0)
                break;

            var existing = slot.Item;
            if (existing == null || existing.DefinitionId != definition.Id)
                continue;

            var freeSpace = definition.MaxStack - existing.Quantity;
            if (freeSpace <= 0)
                continue;

            var toAdd = Math.Min(freeSpace, remaining);
            existing.Quantity += toAdd;
            remaining -= toAdd;
        }

        // 2) Use empty slots for any remaining quantity.
        if (remaining > 0)
        {
            foreach (var slot in _slots)
            {
                if (remaining <= 0)
                    break;

                if (!slot.IsEmpty)
                    continue;

                var toPlace = Math.Min(definition.MaxStack, remaining);
                slot.SetItem(new ItemInstance
                {
                    DefinitionId = definition.Id,
                    Quantity = toPlace,
                });

                remaining -= toPlace;
            }
        }

        // By capacity check above, remaining must be 0 here.
        return remaining == 0;
    }

    /// <summary>
    /// Attempts to remove the requested quantity of an item across all slots.
    /// Returns true only if the full quantity is removed.
    /// </summary>
    public bool TryRemove(string definitionId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("DefinitionId must be provided", nameof(definitionId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        var total = GetTotalQuantity(definitionId);
        if (total < quantity)
        {
            return false;
        }

        var remaining = quantity;

        foreach (var slot in _slots)
        {
            if (remaining <= 0)
                break;

            var item = slot.Item;
            if (item == null || item.DefinitionId != definitionId)
                continue;

            var toRemove = Math.Min(item.Quantity, remaining);
            item.Quantity -= toRemove;
            remaining -= toRemove;

            if (item.Quantity <= 0)
            {
                slot.Clear();
            }
        }

        return remaining == 0;
    }

    /// <summary>
    /// Attempts to move an item stack between slots. If the destination is empty, the stack is moved.
    /// If the destination holds the same item, stacks are merged up to MaxStack and any overflow
    /// remains in the source slot.
    /// </summary>
    public bool TryMove(ItemDefinition definition, int fromSlotIndex, int toSlotIndex)
    {
        if (fromSlotIndex < 0 || fromSlotIndex >= _slots.Count) return false;
        if (toSlotIndex < 0 || toSlotIndex >= _slots.Count) return false;
        if (fromSlotIndex == toSlotIndex) return true;

        var from = _slots[fromSlotIndex];
        var to = _slots[toSlotIndex];

        var sourceItem = from.Item;
        if (sourceItem == null)
            return false;

        if (!to.IsEmpty)
        {
            var destItem = to.Item!;
            if (!string.Equals(destItem.DefinitionId, sourceItem.DefinitionId, StringComparison.Ordinal))
            {
                // Different items: do a simple swap.
                from.SetItem(destItem);
                to.SetItem(sourceItem);
                return true;
            }

            // Same item: merge stacks.
            var freeSpace = definition.MaxStack - destItem.Quantity;
            if (freeSpace <= 0)
                return true; // No-op, but considered successful.

            var toMove = Math.Min(freeSpace, sourceItem.Quantity);
            destItem.Quantity += toMove;
            sourceItem.Quantity -= toMove;

            if (sourceItem.Quantity <= 0)
            {
                from.Clear();
            }

            return true;
        }
        else
        {
            // Destination empty: move the whole stack.
            to.SetItem(sourceItem);
            from.Clear();
            return true;
        }
    }

    private int CalculateAvailableCapacity(ItemDefinition definition)
    {
        var totalCapacity = 0;

        // Space in existing stacks.
        foreach (var slot in _slots)
        {
            var item = slot.Item;
            if (item != null && item.DefinitionId == definition.Id)
            {
                var freeSpace = definition.MaxStack - item.Quantity;
                if (freeSpace > 0)
                {
                    totalCapacity += freeSpace;
                }
            }
        }

        // Space in empty slots.
        var emptySlots = 0;
        foreach (var slot in _slots)
        {
            if (slot.IsEmpty)
            {
                emptySlots++;
            }
        }

        totalCapacity += emptySlots * definition.MaxStack;

        return totalCapacity;
    }
}
