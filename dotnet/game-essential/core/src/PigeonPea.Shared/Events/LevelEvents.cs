namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when the player gains a level.
/// </summary>
/// <remarks>
/// This event is triggered when the player accumulates enough experience points to advance
/// to the next level, providing information about the new level and stat improvements.
/// </remarks>
public readonly struct PlayerLevelUpEvent
{
    /// <summary>
    /// Gets the player's new level after leveling up.
    /// </summary>
    public int NewLevel { get; init; }

    /// <summary>
    /// Gets the amount of maximum health increased from leveling up.
    /// </summary>
    public int HealthIncrease { get; init; }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(PlayerLevelUpEvent left, PlayerLevelUpEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlayerLevelUpEvent left, PlayerLevelUpEvent right)
    {
        return !(left == right);
    }
}
