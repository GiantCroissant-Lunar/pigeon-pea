namespace PigeonPea.Game.Contracts.Events;

/// <summary>
/// Event published when the player gains a level.
/// </summary>
public class PlayerLevelUpEvent
{
    public int NewLevel { get; set; }
    public int HealthIncrease { get; set; }
}
