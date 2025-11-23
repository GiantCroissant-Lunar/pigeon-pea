using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Gas.Abilities;
using PigeonPea.Game.Abilities.Components;

namespace PigeonPea.Game.Abilities.Systems;

public static class AbilityValidationSystem
{
    public static bool CanActivateAbility(
        Entity caster,
        AbilityDefinition ability,
        Entity? target,
        out string reason,
        IEnumerable<IAbilityValidator>? validators = null)
    {
        reason = string.Empty;

        if (!caster.Has<AbilitySystemComponent>())
        {
            reason = "Caster has no ability system component.";
            return false;
        }

        ref var asc = ref caster.Get<AbilitySystemComponent>();

        if (!asc.KnownAbilities.Any(a => a.Id == ability.Id))
        {
            reason = $"Ability {ability.Id} is not known by caster.";
            return false;
        }

        if (asc.CooldownTimers.TryGetValue(ability.Id, out var cooldown) && cooldown > 0f)
        {
            reason = "Ability is on cooldown.";
            return false;
        }

        if (ability.ActivationRequiredTags.Count > 0 &&
            !asc.ActiveTags.HasAllTags(ability.ActivationRequiredTags))
        {
            reason = "Required tags are missing.";
            return false;
        }

        if (ability.ActivationBlockedTags.Count > 0 &&
            asc.ActiveTags.HasAnyTag(ability.ActivationBlockedTags))
        {
            reason = "Ability is blocked by tags.";
            return false;
        }

        if (ability.Cost is { } cost && !cost.CanAfford(asc.Attributes))
        {
            reason = "Insufficient attributes to pay cost.";
            return false;
        }

        var targeting = ability.Targeting;
        if (targeting.Type != TargetingType.Self && targeting.Type != TargetingType.None)
        {
            if (target is null)
            {
                reason = "Ability requires a target.";
                return false;
            }

            var targetEntity = target.Value;

            if (!targeting.CanTargetSelf && targetEntity.Equals(caster))
            {
                reason = "Ability cannot target self.";
                return false;
            }
        }

        if (validators != null)
        {
            foreach (var validator in validators)
            {
                if (!validator.CanActivate(ability, asc.Attributes, asc.ActiveTags, out var validatorReason))
                {
                    reason = validatorReason;
                    return false;
                }
            }
        }

        return true;
    }
}
