using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Character.Models;
using PigeonPea.Game.Contracts.Character.Services;
using PigeonPea.Shared.ECS.Components;
using IStatsService = PigeonPea.Game.Contracts.Stats.Services.IService;

namespace PigeonPea.Plugins.Character.Basic;

public class BasicCharacterService : IService, IPlugin
{
    private IRegistry _registry;
    private readonly Dictionary<string, CharacterClass> _classes = new();

    public string Id => "pigeon-pea.plugins.character.basic";
    public string Name => "Basic Character Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _registry = context.Registry;
        context.Registry.Register<IService>(this);
        RegisterDefaultClasses();
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default)
    {
        _classes.Clear();
        return Task.CompletedTask;
    }

    private void RegisterDefaultClasses()
    {
        var warrior = new CharacterClass
        {
            Id = "Warrior",
            Name = "Warrior",
            Description = "A strong melee fighter.",
            StartingStats = new Dictionary<string, float>
            {
                { "Health", 150 },
                { "MaxHealth", 150 },
                { "Attack", 15 },
                { "Defense", 10 },
                { "Speed", 90 }
            }
        };
        _classes[warrior.Id] = warrior;

        var rogue = new CharacterClass
        {
            Id = "Rogue",
            Name = "Rogue",
            Description = "A fast and agile fighter.",
            StartingStats = new Dictionary<string, float>
            {
                { "Health", 100 },
                { "MaxHealth", 100 },
                { "Attack", 20 },
                { "Defense", 5 },
                { "Speed", 120 }
            }
        };
        _classes[rogue.Id] = rogue;
    }

    public CharacterView GetCharacter(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Character>())
        {
            return new CharacterView();
        }

        var character = entity.Get<PigeonPea.Shared.ECS.Components.Character>();

        // Calculate XP to next level: 100 * level^1.5
        int xpToNext = (int)(100 * Math.Pow(character.Level + 1, 1.5));

        return new CharacterView
        {
            Name = character.Name,
            ClassId = character.ClassId,
            Level = character.Level,
            Experience = character.Experience,
            ExperienceToNextLevel = xpToNext
        };
    }

    public void SetClass(World world, Entity entity, string classId)
    {
        if (!_classes.TryGetValue(classId, out var charClass))
        {
            // Fallback or error
            return;
        }

        if (!entity.Has<PigeonPea.Shared.ECS.Components.Character>())
        {
            entity.Add(new PigeonPea.Shared.ECS.Components.Character
            {
                Name = "Unknown",
                Level = 1,
                Experience = 0
            });
        }

        ref var character = ref entity.Get<PigeonPea.Shared.ECS.Components.Character>();
        character.ClassId = classId;

        // Apply starting stats
        var statsService = _registry.Get<IStatsService>();
        if (statsService != null)
        {
            statsService.SetStats(world, entity, charClass.StartingStats);
        }
    }

    public void AddExperience(World world, Entity entity, int amount)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Character>()) return;

        ref var character = ref entity.Get<PigeonPea.Shared.ECS.Components.Character>();
        character.Experience += amount;

        CheckLevelUp(world, entity, ref character);
    }

    private void CheckLevelUp(World world, Entity entity, ref PigeonPea.Shared.ECS.Components.Character character)
    {
        while (true)
        {
            int xpToNext = (int)(100 * Math.Pow(character.Level + 1, 1.5));
            if (character.Experience >= xpToNext)
            {
                LevelUp(world, entity);
            }
            else
            {
                break;
            }
        }
    }

    public void LevelUp(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Character>()) return;

        ref var character = ref entity.Get<PigeonPea.Shared.ECS.Components.Character>();
        character.Level++;

        // Stat growth logic
        var statsService = _registry.Get<IStatsService>();
        if (statsService != null)
        {
            // Simple growth: +10% HP, +2 Attack
            float currentMaxHp = statsService.GetBaseStatValue(world, entity, "MaxHealth");
            statsService.SetStat(world, entity, "MaxHealth", currentMaxHp * 1.1f);

            // Heal on level up
            statsService.SetStat(world, entity, "Health", currentMaxHp * 1.1f);

            float currentAttack = statsService.GetBaseStatValue(world, entity, "Attack");
            statsService.SetStat(world, entity, "Attack", currentAttack + 2);
        }
    }
}
