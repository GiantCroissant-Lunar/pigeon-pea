using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using MessagePipe;
using NexusGas.Abilities;
using NexusGas.Attributes;
using NexusGas.Tags;
using PigeonPea.Game.Abilities.Components;
using PigeonPea.Game.Abilities.Events;
using PigeonPea.Game.Abilities.Systems;

namespace PigeonPea.Game.Abilities.Integration;

public static class AbilityWorldExtensions
{
    public static bool TryCastAbility(
        this World world,
        Entity caster,
        string abilityId,
        Entity? target,
        float currentTime,
        IPublisher<AbilityCastEvent> abilityCastPublisher,
        IPublisher<AbilityCastFailedEvent>? castFailedPublisher = null,
        IPublisher<EffectAppliedEvent>? effectAppliedPublisher = null,
        IEnumerable<IAbilityValidator>? validators = null)
    {
        if (!caster.TryGet<AbilitySystemComponent>(out var asc))
            return false;

        var ability = asc.KnownAbilities.FirstOrDefault(a => a.Id == abilityId);
        if (ability == null)
            return false;

        if (!AbilityValidationSystem.CanActivateAbility(caster, ability, target, out var reason, validators))
        {
            castFailedPublisher?.Publish(new AbilityCastFailedEvent(caster, abilityId, reason, currentTime));
            return false;
        }

        AbilityExecutionSystem.ExecuteAbility(world, caster, ability, target, currentTime,
            abilityCastPublisher, effectAppliedPublisher);
        return true;
    }

    public static void GiveAbility(this World world, Entity entity, AbilityDefinition ability)
    {
        if (!entity.Has<AbilitySystemComponent>())
        {
            entity.Add(new AbilitySystemComponent());
        }

        ref var asc = ref entity.Get<AbilitySystemComponent>();
        if (!asc.KnownAbilities.Any(a => a.Id == ability.Id))
        {
            asc.KnownAbilities.Add(ability);
        }

        if (!asc.CooldownTimers.ContainsKey(ability.Id))
        {
            asc.CooldownTimers[ability.Id] = 0f;
        }
    }
}
