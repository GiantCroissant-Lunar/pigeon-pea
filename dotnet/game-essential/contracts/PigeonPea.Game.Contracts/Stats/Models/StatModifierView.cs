using System;

namespace PigeonPea.Game.Contracts.Stats.Models;

public sealed class StatModifierView
{
    public string ModifierId { get; init; } = string.Empty;

    public string StatId { get; init; } = string.Empty;

    public float Value { get; init; }

    public ModifierType Type { get; init; }

    public float RemainingDuration { get; init; }

    public string SourceId { get; init; } = string.Empty;

    public DateTime AppliedAt { get; init; }
}
