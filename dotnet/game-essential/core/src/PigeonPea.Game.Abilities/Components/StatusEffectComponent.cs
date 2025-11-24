using System.Collections.Generic;
using PigeonPea.Shared.Gas.Tags;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Visual/gameplay status effects (for UI display).
/// </summary>
public struct StatusEffectComponent
{
    public List<StatusEffect> ActiveStatuses { get; set; }

    public StatusEffectComponent()
    {
        ActiveStatuses = new List<StatusEffect>();
    }
}

public sealed class StatusEffect
{
    public string Type { get; set; } = string.Empty; // "Stunned", "Poisoned", "Burning"
    public int DurationTurns { get; set; }
    public int Magnitude { get; set; }
    public GameplayTag Tag { get; set; }

    public override string ToString() => $"{Type} ({DurationTurns} turns)";
}
