using System.Collections.Generic;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Simplified cooldown tracking component.
/// </summary>
public struct CooldownComponent
{
    public Dictionary<string, CooldownState> Cooldowns { get; set; }

    public CooldownComponent()
    {
        Cooldowns = new Dictionary<string, CooldownState>();
    }
}

public sealed class CooldownState
{
    public float RemainingSeconds { get; set; }
    public float TotalSeconds { get; set; }
    public float Progress => TotalSeconds > 0 ? 1f - (RemainingSeconds / TotalSeconds) : 1f;

    public override string ToString() => $"{RemainingSeconds:F1}s / {TotalSeconds:F1}s";
}
