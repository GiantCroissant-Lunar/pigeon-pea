using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Contracts.Stats.Services;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Plugins.Stats.Basic;

public sealed class ModifierDurationSystem
{
    private readonly IService _statsService;

    public ModifierDurationSystem(IService statsService)
    {
        _statsService = statsService;
    }

    public void Update(World world, float deltaTime)
    {
        var query = new QueryDescription().WithAll<StatModifiers>();

        world.Query(in query, (Entity entity, ref StatModifiers modifiers) =>
        {
            if (modifiers.Modifiers.Count == 0)
            {
                return;
            }

            var expired = new List<string>();

            for (int i = 0; i < modifiers.Modifiers.Count; i++)
            {
                var modifier = modifiers.Modifiers[i];

                if (modifier.RemainingDuration < 0f)
                {
                    continue;
                }

                modifier.RemainingDuration -= deltaTime;
                modifiers.Modifiers[i] = modifier;

                if (modifier.RemainingDuration <= 0f)
                {
                    expired.Add(modifier.ModifierId);
                }
            }

            if (expired.Count == 0)
            {
                return;
            }

            foreach (var id in expired)
            {
                _statsService.RemoveModifier(world, entity, id);
            }
        });
    }
}
