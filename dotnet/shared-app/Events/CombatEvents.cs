namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when a player takes damage from a source.
/// </summary>
/// <remarks>
/// This event is used to notify UI components and game systems that the player has been damaged,
/// allowing for updates to health displays, message logs, and other relevant UI elements.
/// </remarks>
public readonly struct PlayerDamagedEvent
{
    /// <summary>
    /// Gets the amount of damage dealt to the player.
    /// </summary>
    public int Damage { get; init; }

    /// <summary>
    /// Gets the player's remaining health after taking damage.
    /// </summary>
    public int RemainingHealth { get; init; }

    /// <summary>
    /// Gets the source of the damage (e.g., enemy name, trap type).
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Event published when an enemy is defeated by the player.
/// </summary>
/// <remarks>
/// This event is triggered when an enemy's health reaches zero, providing information
/// about the defeated enemy and experience gained for progression systems.
/// </remarks>
public readonly struct EnemyDefeatedEvent
{
    /// <summary>
    /// Gets the name of the defeated enemy.
    /// </summary>
    public required string EnemyName { get; init; }

    /// <summary>
    /// Gets the amount of experience points awarded for defeating the enemy.
    /// </summary>
    public int ExperienceGained { get; init; }
}
