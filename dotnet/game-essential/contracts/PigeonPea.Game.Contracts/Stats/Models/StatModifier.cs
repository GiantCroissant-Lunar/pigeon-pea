namespace PigeonPea.Game.Contracts.Stats.Models;

public sealed class StatModifier
{
    public string StatId { get; init; } = string.Empty;

    public float Value { get; init; }

    public ModifierType Type { get; init; }

    public float Duration { get; init; }

    public string SourceId { get; init; } = string.Empty;
}
