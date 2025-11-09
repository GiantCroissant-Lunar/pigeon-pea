namespace PigeonPea.Shared.Events;

/// <summary>
/// Example event published when a player takes damage.
/// </summary>
public readonly struct PlayerDamagedEvent
{
    public int Damage { get; init; }
    public int RemainingHealth { get; init; }
    public string Source { get; init; }
}

/// <summary>
/// Example event published when an item is picked up.
/// </summary>
public readonly struct ItemPickedUpEvent
{
    public string ItemName { get; init; }
    public string ItemType { get; init; }
}

/// <summary>
/// Example event published when the game state changes.
/// </summary>
public readonly struct GameStateChangedEvent
{
    public string NewState { get; init; }
    public string PreviousState { get; init; }
}
