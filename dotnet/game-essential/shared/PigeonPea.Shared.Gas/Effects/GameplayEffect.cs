using System.Collections.Generic;
using PigeonPea.Shared.Gas.Tags;

namespace PigeonPea.Shared.Gas.Effects;

/// <summary>
/// Definition of a gameplay effect that modifies attributes and grants tags.
/// </summary>
public sealed class GameplayEffect
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public EffectDurationPolicy DurationPolicy { get; set; }
    public float DurationSeconds { get; set; }
    public float PeriodSeconds { get; set; } // For Periodic effects

    public List<EffectModifier> Modifiers { get; set; } = new();
    public List<GameplayTag> GrantedTags { get; set; } = new();
    public List<GameplayTag> RemovedTags { get; set; } = new();

    /// <summary>
    /// Tags required on target for effect to apply.
    /// </summary>
    public List<GameplayTag> ApplicationRequiredTags { get; set; } = new();

    /// <summary>
    /// Tags that prevent effect from applying.
    /// </summary>
    public List<GameplayTag> ApplicationBlockedTags { get; set; } = new();

    public override string ToString() =>
        $"{Name} ({DurationPolicy}, {DurationSeconds}s)";
}
