using SharedInventoryCore = PigeonPea.Shared.Inventory.Core.Inventory;

namespace PigeonPea.Game.Inventory.Components;

/// <summary>
/// ECS component that attaches an inventory to an entity.
/// Wraps the shared Inventory model from PigeonPea.Shared.Inventory.
/// </summary>
public struct InventoryComponent
{
    public SharedInventoryCore Inventory;

    public InventoryComponent(int maxSlots, float maxWeight = 0f)
    {
        Inventory = new SharedInventoryCore(maxSlots, maxWeight);
    }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(InventoryComponent left, InventoryComponent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InventoryComponent left, InventoryComponent right)
    {
        return !(left == right);
    }
}
