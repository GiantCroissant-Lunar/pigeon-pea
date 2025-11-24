namespace PigeonPea.Game.Contracts.Events;

/// <summary>
/// Event published when an item is picked up by the player.
/// </summary>
public class ItemPickedUpEvent
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an item is used by the player.
/// </summary>
public class ItemUsedEvent
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an item is dropped by the player.
/// </summary>
public class ItemDroppedEvent
{
    public string ItemName { get; set; } = string.Empty;
}
