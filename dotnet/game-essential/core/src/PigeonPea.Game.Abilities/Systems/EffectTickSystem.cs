using System;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Gas.Attributes;
using PigeonPea.Gas.Effects;
using PigeonPea.Game.Abilities.Components;

namespace PigeonPea.Game.Abilities.Systems;

public static class EffectTickSystem
{
    public static void Update(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<ActiveEffectsComponent, AbilitySystemComponent>();

        world.Query(in query, (Entity entity,
            ref ActiveEffectsComponent effects,
            ref AbilitySystemComponent asc) =>
        {
            for (int i = effects.Effects.Count - 1; i >= 0; i--)
            {
                var active = effects.Effects[i];
                var def = active.Definition;

                // Decrement remaining time for non-infinite effects
                if (def.DurationPolicy != EffectDurationPolicy.Infinite)
                {
                    active.RemainingTime -= deltaTime;
                }

                // Handle periodic effects
                if (def.DurationPolicy == EffectDurationPolicy.Periodic && def.PeriodSeconds > 0)
                {
                    active.TimeToNextTick -= deltaTime;
                    while (active.TimeToNextTick <= 0f)
                    {
                        ApplyTickModifiers(active, ref asc);
                        active.TimeToNextTick += def.PeriodSeconds;
                    }
                }

                // Remove expired effects (and their persistent modifiers/tags)
                if (active.IsExpired)
                {
                    RemoveEffect(active, ref asc);
                    effects.Effects.RemoveAt(i);
                    continue;
                }

                // Write back modified active effect
                effects.Effects[i] = active;
            }
        });
    }

    private static void ApplyTickModifiers(ActiveEffect active, ref AbilitySystemComponent asc)
    {
        foreach (var effectModifier in active.Definition.Modifiers.Where(m => m.ApplyOnTick))
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

    private static void RemoveEffect(ActiveEffect active, ref AbilitySystemComponent asc)
    {
        var def = active.Definition;

        // Remove attribute modifiers associated with this effect (added as persistent modifiers)
        asc.Attributes.RemoveModifiersBySource(def.Id);

        // Remove granted tags when effect ends
        foreach (var granted in def.GrantedTags)
        {
            asc.ActiveTags.RemoveTag(granted);
        }
    }
}
