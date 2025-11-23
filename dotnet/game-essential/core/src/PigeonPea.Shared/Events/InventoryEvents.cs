namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when an item is picked up by the player.
/// </summary>
/// <remarks>
/// This event is triggered when a player successfully picks up an item from the game world,
/// notifying inventory systems and UI components to update displays and message logs.
/// </remarks>
public readonly struct ItemPickedUpEvent
{
    /// <summary>
    /// Gets the name of the item that was picked up.
    /// </summary>
    public required string ItemName { get; init; }

    /// <summary>
    /// Gets the type of the item that was picked up (e.g., "Consumable", "Equipment", "QuestItem").
    /// </summary>
    public required string ItemType { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ItemPickedUpEvent other
               && ItemName == other.ItemName
               && ItemType == other.ItemType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (ItemName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ItemType?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public static bool operator ==(ItemPickedUpEvent left, ItemPickedUpEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemPickedUpEvent left, ItemPickedUpEvent right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Event published when an item is used by the player.
/// </summary>
/// <remarks>
/// This event is triggered when the player consumes or activates an item from their inventory,
/// allowing game systems to apply item effects and update the UI accordingly.
/// </remarks>
public readonly struct ItemUsedEvent
{
    /// <summary>
    /// Gets the name of the item that was used.
    /// </summary>
    public required string ItemName { get; init; }

    /// <summary>
    /// Gets the type of the item that was used (e.g., "Consumable", "Equipment", "QuestItem").
    /// </summary>
    public required string ItemType { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ItemUsedEvent other
               && ItemName == other.ItemName
               && ItemType == other.ItemType;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (ItemName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ItemType?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public static bool operator ==(ItemUsedEvent left, ItemUsedEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemUsedEvent left, ItemUsedEvent right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Event published when an item is dropped by the player.
/// </summary>
/// <remarks>
/// This event is triggered when the player removes an item from their inventory and drops it
/// on the ground, allowing the game to update the world state and UI displays.
/// </remarks>
public readonly struct ItemDroppedEvent
{
    /// <summary>
    /// Gets the name of the item that was dropped.
    /// </summary>
    public required string ItemName { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ItemDroppedEvent other
               && ItemName == other.ItemName;
    }

    public override int GetHashCode()
    {
        return ItemName?.GetHashCode() ?? 0;
    }

    public static bool operator ==(ItemDroppedEvent left, ItemDroppedEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemDroppedEvent left, ItemDroppedEvent right)
    {
        return !(left == right);
    }
}
