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

    public override bool Equals(object? obj)
    {
        return obj is GameStateChangedEvent other
               && NewState == other.NewState
               && PreviousState == other.PreviousState;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (NewState?.GetHashCode() ?? 0);
            hash = (hash * 31) + (PreviousState?.GetHashCode() ?? 0);
            return hash;
        }
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
