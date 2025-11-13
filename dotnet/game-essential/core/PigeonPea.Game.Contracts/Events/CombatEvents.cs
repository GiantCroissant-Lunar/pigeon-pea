namespace PigeonPea.Game.Contracts.Events;

/// <summary>
/// Event published when a player takes damage from a source.
/// </summary>
public class PlayerDamagedEvent
{
    public int Damage { get; set; }
    public int RemainingHealth { get; set; }
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Event published when an enemy is defeated by the player.
/// </summary>
public class EnemyDefeatedEvent
{
    public string EnemyName { get; set; } = string.Empty;
    public int ExperienceGained { get; set; }
}
