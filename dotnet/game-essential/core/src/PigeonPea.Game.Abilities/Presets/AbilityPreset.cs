using System.Collections.Generic;
using System.Text.Json.Serialization;
using PigeonPea.Gas.Abilities;
using PigeonPea.Gas.Effects;
using PigeonPea.Gas.Attributes;

namespace PigeonPea.Game.Abilities.Presets;

/// <summary>
/// JSON-friendly DTO for an ability preset. Maps to AbilityDefinition.
/// </summary>
public sealed class AbilityPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public AbilityCostPreset? Cost { get; set; }
    public float CooldownSeconds { get; set; } = 0f;
    public AbilityActivationPolicy ActivationPolicy { get; set; } = AbilityActivationPolicy.Always;

    public List<string> ActivationRequiredTags { get; set; } = new();
    public List<string> ActivationBlockedTags { get; set; } = new();

    public AbilityTargetingPreset Targeting { get; set; } = new();
    public List<EffectPreset> Effects { get; set; } = new();

    public List<string> CasterTagsWhileActive { get; set; } = new();
    public int? MaxActivations { get; set; }
}

public sealed class AbilityCostPreset
{
    public List<AttributeModifierPreset> Modifiers { get; set; } = new();
}

public sealed class AbilityTargetingPreset
{
    public TargetingType Type { get; set; } = TargetingType.Self;
    public float Range { get; set; } = 0f;
    public float AoeRadius { get; set; } = 0f;
    public bool RequiresLineOfSight { get; set; } = false;
    public bool CanTargetSelf { get; set; } = true;
    public bool CanTargetAllies { get; set; } = true;
    public bool CanTargetEnemies { get; set; } = true;
}

public sealed class EffectPreset
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public EffectDurationPolicy DurationPolicy { get; set; }
    public float DurationSeconds { get; set; }
    public float PeriodSeconds { get; set; }

    public List<EffectModifierPreset> Modifiers { get; set; } = new();
    public List<string> GrantedTags { get; set; } = new();
    public List<string> RemovedTags { get; set; } = new();

    public List<string> ApplicationRequiredTags { get; set; } = new();
    public List<string> ApplicationBlockedTags { get; set; } = new();
}

public sealed class EffectModifierPreset
{
    public string AttributeId { get; set; } = string.Empty;
    public ModifierOperation Operation { get; set; } = ModifierOperation.Add;
    public float Magnitude { get; set; }
    public bool ApplyOnTick { get; set; } = false;
}

public sealed class AttributeModifierPreset
{
    public string AttributeId { get; set; } = string.Empty;
    public ModifierOperation Operation { get; set; } = ModifierOperation.Add;
    public float Magnitude { get; set; }
}
