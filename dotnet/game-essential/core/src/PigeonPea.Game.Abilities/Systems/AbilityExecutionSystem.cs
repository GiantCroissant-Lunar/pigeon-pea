using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using MessagePipe;
using PigeonPea.Shared.Gas.Abilities;
using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Shared.Gas.Effects;
using PigeonPea.Shared.Gas.Tags;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Abilities.Events;

namespace PigeonPea.Game.Abilities.Systems;

public static class AbilityExecutionSystem
{
    public static void ExecuteAbility(
        World world,
        Entity caster,
        AbilityDefinition ability,
        Entity? target,
        float currentTime,
        IPublisher<AbilityCastEvent> abilityCastPublisher,
        IPublisher<EffectAppliedEvent>? effectAppliedPublisher = null)
    {
        // Ensure caster has ability system component
        ref var casterAsc = ref caster.Get<AbilitySystemComponent>();

        // 1. Apply cost to caster
        if (ability.Cost is { } cost)
        {
            ApplyCost(cost, ref casterAsc);
        }

        // 2. Start cooldown
        if (ability.CooldownSeconds > 0)
        {
            casterAsc.CooldownTimers[ability.Id] = ability.CooldownSeconds;
        }

        // 3. Determine effect target entity
        var effectTarget = DetermineTargetEntity(ability, caster, target);

        if (!effectTarget.Has<AbilitySystemComponent>())
        {
            effectTarget.Add(new AbilitySystemComponent());
        }

        if (!effectTarget.Has<ActiveEffectsComponent>())
        {
            effectTarget.Add(new ActiveEffectsComponent());
        }

        ref var targetAsc = ref effectTarget.Get<AbilitySystemComponent>();
        ref var activeEffects = ref effectTarget.Get<ActiveEffectsComponent>();

        // 3. Apply or register each effect
        foreach (var effect in ability.Effects)
        {
            switch (effect.DurationPolicy)
            {
                case EffectDurationPolicy.Instant:
                    ApplyInstantEffect(effect, ref targetAsc);
                    ApplyEffectTags(effect, ref targetAsc);
                    effectAppliedPublisher?.Publish(new EffectAppliedEvent(
                        effectTarget,
                        effect.Id,
                        effect.Name,
                        0f,
                        currentTime));
                    break;

                case EffectDurationPolicy.Duration:
                case EffectDurationPolicy.Infinite:
                case EffectDurationPolicy.Periodic:
                    var active = new ActiveEffect(effect, ability.Id);
                    activeEffects.Effects.Add(active);

                    // For duration / infinite effects, apply persistent modifiers immediately
                    if (effect.DurationPolicy is EffectDurationPolicy.Duration or EffectDurationPolicy.Infinite)
                    {
                        ApplyPersistentModifiers(active, ref targetAsc);
                        ApplyEffectTags(effect, ref targetAsc);
                    }

                    effectAppliedPublisher?.Publish(new EffectAppliedEvent(
                        effectTarget,
                        effect.Id,
                        effect.Name,
                        effect.DurationSeconds,
                        currentTime));
                    break;
            }
        }

        // 4. Publish ability cast event
        abilityCastPublisher.Publish(new AbilityCastEvent(
            caster,
            ability.Id,
            ability.Name,
            target,
            true,
            currentTime));
    }

    private static Entity DetermineTargetEntity(AbilityDefinition ability, Entity caster, Entity? target)
    {
        return ability.Targeting.Type switch
        {
            TargetingType.Self or TargetingType.None => caster,
            _ => target ?? caster
        };
    }

    private static void ApplyCost(AbilityCost cost, ref AbilitySystemComponent asc)
    {
        foreach (var modifier in cost.Modifiers)
        {
            var attributeId = modifier.AttributeId;
            var baseValue = asc.Attributes.GetBaseValue(attributeId);

            switch (modifier.Operation)
            {
                case ModifierOperation.Add:
                    asc.Attributes.SetBaseValue(attributeId, baseValue + modifier.Magnitude);
                    break;
                case ModifierOperation.Multiply:
                    asc.Attributes.SetBaseValue(attributeId, baseValue * modifier.Magnitude);
                    break;
                case ModifierOperation.Override:
                    asc.Attributes.SetBaseValue(attributeId, modifier.Magnitude);
                    break;
            }
        }
    }

    private static void ApplyInstantEffect(GameplayEffect effect, ref AbilitySystemComponent asc)
    {
        foreach (var effectModifier in effect.Modifiers)
        {
            var modifier = effectModifier.Modifier;
            var attributeId = modifier.AttributeId;
            var baseValue = asc.Attributes.GetBaseValue(attributeId);

            switch (modifier.Operation)
            {
                case ModifierOperation.Add:
                    asc.Attributes.SetBaseValue(attributeId, baseValue + modifier.Magnitude);
                    break;
                case ModifierOperation.Multiply:
                    asc.Attributes.SetBaseValue(attributeId, baseValue * modifier.Magnitude);
                    break;
                case ModifierOperation.Override:
                    asc.Attributes.SetBaseValue(attributeId, modifier.Magnitude);
                    break;
            }
        }
    }

    private static void ApplyPersistentModifiers(ActiveEffect activeEffect, ref AbilitySystemComponent asc)
    {
        var effect = activeEffect.Definition;
        foreach (var effectModifier in effect.Modifiers.Where(m => !m.ApplyOnTick))
        {
            var modifier = effectModifier.Modifier;

            // Wrap in a new modifier whose SourceTag is this effect's Id so we can remove it later.
            var persistentModifier = new AttributeModifier(
                modifier.AttributeId,
                modifier.Operation,
                modifier.Magnitude,
                effect.Id);

            asc.Attributes.AddModifier(persistentModifier);
        }
    }

    private static void ApplyEffectTags(GameplayEffect effect, ref AbilitySystemComponent asc)
    {
        foreach (var granted in effect.GrantedTags)
        {
            asc.ActiveTags.AddTag(granted);
        }

        foreach (var removed in effect.RemovedTags)
        {
            asc.ActiveTags.RemoveTag(removed);
        }
    }
}
