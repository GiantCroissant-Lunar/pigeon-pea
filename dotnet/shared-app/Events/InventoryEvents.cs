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
}
