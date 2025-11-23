using System;
using System.Linq;
using Arch.Core;
using PigeonPea.Gas.Abilities;
using PigeonPea.Game.Abilities.Components;

namespace PigeonPea.Game.Abilities.Systems;

public static class CooldownSystem
{
    public static void Update(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<AbilitySystemComponent>();

        world.Query(in query, (ref AbilitySystemComponent asc) =>
        {
            var cooldowns = asc.CooldownTimers;
            if (cooldowns.Count == 0)
                return;

            var keys = cooldowns.Keys.ToArray();
            foreach (var abilityId in keys)
            {
                var remaining = cooldowns[abilityId] - deltaTime;
                cooldowns[abilityId] = Math.Max(0f, remaining);
            }
        });
    }
}
