using PigeonPea.Shared.Inventory.Items;

namespace PigeonPea.Shared.Inventory.Core;

/// <summary>
/// Single slot in an Inventory. May be empty or hold a stack of items.
/// </summary>
public sealed class InventorySlot
{
    public int Index { get; }

    /// <summary>
    /// Current item stack in this slot, or null if empty.
    /// </summary>
    public ItemInstance? Item { get; private set; }

    public bool IsEmpty => Item == null || Item.Quantity <= 0;

    public InventorySlot(int index)
    {
        Index = index;
    }

    /// <summary>
    /// Clears the slot.
    /// </summary>
    public void Clear()
    {
        Item = null;
    }

    /// <summary>
    /// Sets the item stack for this slot.
    /// </summary>
    public void SetItem(ItemInstance? instance)
    {
        Item = instance;
    }
}
