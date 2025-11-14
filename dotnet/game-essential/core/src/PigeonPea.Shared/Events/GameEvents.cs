namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when the game state changes between major states.
/// </summary>
/// <remarks>
/// This event is triggered when the game transitions between states such as Menu, Playing, Paused, or GameOver,
/// allowing systems to respond to state changes and update accordingly.
/// </remarks>
public readonly struct GameStateChangedEvent
{
    /// <summary>
    /// Gets the new game state after the transition.
    /// </summary>
    public required string NewState { get; init; }

    /// <summary>
    /// Gets the previous game state before the transition.
    /// </summary>
    public required string PreviousState { get; init; }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(GameStateChangedEvent left, GameStateChangedEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GameStateChangedEvent left, GameStateChangedEvent right)
    {
        return !(left == right);
    }
}
