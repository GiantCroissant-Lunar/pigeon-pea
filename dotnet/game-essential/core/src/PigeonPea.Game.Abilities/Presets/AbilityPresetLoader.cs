using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusGas.Abilities;
using NexusGas.Attributes;
using NexusGas.Effects;
using NexusGas.Tags;

namespace PigeonPea.Game.Abilities.Presets;

/// <summary>
/// Loads ability presets from JSON into NexusGas.Core domain types.
/// </summary>
public static class AbilityPresetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static AbilityPreset? DeserializePreset(string json)
    {
        return JsonSerializer.Deserialize<AbilityPreset>(json, JsonOptions);
    }

    public static AbilityDefinition LoadAbilityFromJson(string json)
    {
        var preset = DeserializePreset(json)
                     ?? throw new InvalidOperationException("Failed to deserialize AbilityPreset from JSON.");

        return preset.ToDomain();
    }

    public static AbilityDefinition LoadAbilityFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return LoadAbilityFromJson(json);
    }
}

public static class AbilityPresetMapper
{
    public static AbilityDefinition ToDomain(this AbilityPreset preset)
    {
        var ability = new AbilityDefinition
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            IconPath = preset.IconPath ?? string.Empty,
            CooldownSeconds = preset.CooldownSeconds,
            ActivationPolicy = preset.ActivationPolicy,
            Cost = preset.Cost != null
                ? new AbilityCost
                {
                    Modifiers = preset.Cost.Modifiers
                        .Select(m => new AttributeModifier(m.AttributeId, m.Operation, m.Magnitude))
                        .ToList()
                }
                : new AbilityCost(),
            Targeting = preset.Targeting.ToDomain(),
            Effects = preset.Effects.Select(e => e.ToDomain()).ToList(),
            ActivationRequiredTags = preset.ActivationRequiredTags
                .Select(t => new GameplayTag(t)).ToList(),
            ActivationBlockedTags = preset.ActivationBlockedTags
                .Select(t => new GameplayTag(t)).ToList(),
            CasterTagsWhileActive = preset.CasterTagsWhileActive
                .Select(t => new GameplayTag(t)).ToList(),
            MaxActivations = preset.MaxActivations ?? 0
        };

        return ability;
    }

    public static AbilityTargeting ToDomain(this AbilityTargetingPreset preset)
    {
        return new AbilityTargeting
        {
            Type = preset.Type,
            Range = preset.Range,
            AoeRadius = preset.AoeRadius,
            RequiresLineOfSight = preset.RequiresLineOfSight,
            CanTargetSelf = preset.CanTargetSelf,
            CanTargetAllies = preset.CanTargetAllies,
            CanTargetEnemies = preset.CanTargetEnemies
        };
    }

    public static GameplayEffect ToDomain(this EffectPreset preset)
    {
        var effect = new GameplayEffect
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            DurationPolicy = preset.DurationPolicy,
            DurationSeconds = preset.DurationSeconds,
            PeriodSeconds = preset.PeriodSeconds,
            Modifiers = preset.Modifiers
                .Select(m => new EffectModifier(
                    new AttributeModifier(m.AttributeId, m.Operation, m.Magnitude),
                    m.ApplyOnTick))
                .ToList(),
            GrantedTags = preset.GrantedTags.Select(t => new GameplayTag(t)).ToList(),
            RemovedTags = preset.RemovedTags.Select(t => new GameplayTag(t)).ToList(),
            ApplicationRequiredTags = preset.ApplicationRequiredTags.Select(t => new GameplayTag(t)).ToList(),
            ApplicationBlockedTags = preset.ApplicationBlockedTags.Select(t => new GameplayTag(t)).ToList()
        };

        return effect;
    }
}
