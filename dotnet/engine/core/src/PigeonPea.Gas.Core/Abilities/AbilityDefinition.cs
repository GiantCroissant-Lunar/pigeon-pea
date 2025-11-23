using System.Collections.Generic;
using PigeonPea.Gas.Effects;
using PigeonPea.Gas.Tags;

namespace PigeonPea.Gas.Abilities;

/// <summary>
/// Definition of a gameplay ability.
/// </summary>
public sealed class AbilityDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;

    public AbilityCost Cost { get; set; } = new();
    public float CooldownSeconds { get; set; } = 0f;
    public AbilityActivationPolicy ActivationPolicy { get; set; } = AbilityActivationPolicy.Always;

    public List<GameplayTag> ActivationRequiredTags { get; set; } = new();
    public List<GameplayTag> ActivationBlockedTags { get; set; } = new();

    public AbilityTargeting Targeting { get; set; } = new();
    public List<GameplayEffect> Effects { get; set; } = new();

    /// <summary>
    /// Tags granted to the caster while the ability is active/channeling.
    /// </summary>
    public List<GameplayTag> CasterTagsWhileActive { get; set; } = new();

    /// <summary>
    /// Maximum number of times this ability can be activated (0 = unlimited).
    /// </summary>
    public int MaxActivations { get; set; } = 0;

    public override string ToString() =>
        $"{Name} (Cooldown: {CooldownSeconds}s, Cost: {Cost})";
}
