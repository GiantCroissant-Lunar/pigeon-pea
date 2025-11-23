using System;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Contracts.Combat.Services;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugins.Combat.Basic;

public sealed class BasicCombatService : IService
{
    public int CalculateMeleeDamage(World world, Entity attacker, Entity defender)
    {
        if (!world.Has<CombatStats>(attacker) || !world.Has<CombatStats>(defender))
        {
            return 0;
        }

        ref var attackerStats = ref world.Get<CombatStats>(attacker);
        ref var defenderStats = ref world.Get<CombatStats>(defender);

        var attackerAttack = attackerStats.Attack;
        var defenderDefense = defenderStats.Defense;

        var damage = attackerAttack - defenderDefense;
        return Math.Max(1, damage);
    }
}
