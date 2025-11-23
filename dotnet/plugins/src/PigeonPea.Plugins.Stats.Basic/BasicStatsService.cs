using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Stats.Models;
using PigeonPea.Game.Contracts.Stats.Services;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Plugins.Stats.Basic;

public class BasicStatsService : IService, IPlugin
{
    private readonly Dictionary<string, StatDefinition> _statDefinitions = new();

    public string Id => "pigeon-pea.plugins.stats.basic";
    public string Name => "Basic Stats Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Registry.Register<IService>(this);
        RegisterDefaultStats();
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default)
    {
        _statDefinitions.Clear();
        return Task.CompletedTask;
    }

    private void RegisterDefaultStats()
    {
        // Register some common stats
        RegisterStat(new StatDefinition { Id = "Health", DisplayName = "Health", DefaultValue = 100, MinValue = 0, MaxValue = 9999 });
        RegisterStat(new StatDefinition { Id = "MaxHealth", DisplayName = "Max Health", DefaultValue = 100, MinValue = 1, MaxValue = 9999 });
        RegisterStat(new StatDefinition { Id = "Attack", DisplayName = "Attack", DefaultValue = 10, MinValue = 0, MaxValue = 999 });
        RegisterStat(new StatDefinition { Id = "Defense", DisplayName = "Defense", DefaultValue = 0, MinValue = 0, MaxValue = 999 });
        RegisterStat(new StatDefinition { Id = "Speed", DisplayName = "Speed", DefaultValue = 100, MinValue = 0, MaxValue = 500 });
    }

    private void RegisterStat(StatDefinition definition)
    {
        _statDefinitions[definition.Id] = definition;
    }

    public StatsView GetStats(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>())
        {
            return StatsView.Empty;
        }

        var stats = entity.Get<PigeonPea.Shared.ECS.Components.Stats>();
        var modifiers = entity.Has<StatModifiers>() ? entity.Get<StatModifiers>() : new StatModifiers();

        var view = new StatsView
        {
            BaseStats = new Dictionary<string, float>(stats.BaseStats),
            CurrentStats = new Dictionary<string, float>(stats.CurrentStats),
            ActiveModifiers = modifiers.Modifiers.Select(m => new StatModifierView
            {
                ModifierId = m.ModifierId,
                StatId = m.StatId,
                Value = m.Value,
                Type = m.Type,
                RemainingDuration = m.RemainingDuration,
                SourceId = m.SourceId,
                AppliedAt = m.AppliedAt
            }).ToList()
        };

        return view;
    }

    public bool SetStat(World world, Entity entity, string statId, float value)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>())
        {
            entity.Add(new PigeonPea.Shared.ECS.Components.Stats());
        }

        ref var stats = ref entity.Get<PigeonPea.Shared.ECS.Components.Stats>();

        // Ensure dictionary exists
        if (stats.BaseStats == null) stats.BaseStats = new Dictionary<string, float>();
        if (stats.CurrentStats == null) stats.CurrentStats = new Dictionary<string, float>();

        stats.BaseStats[statId] = value;

        // For now, just set current to base. Recalculate will handle modifiers.
        stats.CurrentStats[statId] = value;

        RecalculateDerivedStats(world, entity);
        return true;
    }

    public float GetStatValue(World world, Entity entity, string statId)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>()) return 0;

        var stats = entity.Get<PigeonPea.Shared.ECS.Components.Stats>();
        return stats.CurrentStats.TryGetValue(statId, out var value) ? value : 0;
    }

    public float GetBaseStatValue(World world, Entity entity, string statId)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>()) return 0;

        var stats = entity.Get<PigeonPea.Shared.ECS.Components.Stats>();
        return stats.BaseStats.TryGetValue(statId, out var value) ? value : 0;
    }

    public string AddModifier(World world, Entity entity, StatModifier modifier)
    {
        if (modifier is null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        if (!entity.Has<StatModifiers>())
        {
            entity.Add(new StatModifiers());
        }

        ref var modifiers = ref entity.Get<StatModifiers>();

        var id = Guid.NewGuid().ToString();
        var activeModifier = new ActiveModifier
        {
            ModifierId = id,
            StatId = modifier.StatId,
            Value = modifier.Value,
            Type = modifier.Type,
            RemainingDuration = modifier.Duration,
            SourceId = modifier.SourceId,
            AppliedAt = DateTime.UtcNow
        };

        modifiers.Modifiers.Add(activeModifier);
        RecalculateDerivedStats(world, entity);

        return id;
    }

    public bool RemoveModifier(World world, Entity entity, string modifierId)
    {
        if (!entity.Has<StatModifiers>()) return false;

        ref var modifiers = ref entity.Get<StatModifiers>();
        var removed = modifiers.Modifiers.RemoveAll(m => m.ModifierId == modifierId);

        if (removed > 0)
        {
            RecalculateDerivedStats(world, entity);
            return true;
        }
        return false;
    }

    public int RemoveModifiersBySource(World world, Entity entity, string sourceId)
    {
        if (!entity.Has<StatModifiers>()) return 0;

        ref var modifiers = ref entity.Get<StatModifiers>();
        var removed = modifiers.Modifiers.RemoveAll(m => m.SourceId == sourceId);

        if (removed > 0)
        {
            RecalculateDerivedStats(world, entity);
        }
        return removed;
    }

    public IReadOnlyList<StatModifierView> GetModifiers(World world, Entity entity)
    {
        if (!entity.Has<StatModifiers>()) return Array.Empty<StatModifierView>();

        var modifiers = entity.Get<StatModifiers>();
        return modifiers.Modifiers.Select(m => new StatModifierView
        {
            ModifierId = m.ModifierId,
            StatId = m.StatId,
            Value = m.Value,
            Type = m.Type,
            RemainingDuration = m.RemainingDuration,
            SourceId = m.SourceId,
            AppliedAt = m.AppliedAt
        }).ToList();
    }

    public float CalculateDerivedStat(World world, Entity entity, string derivedStatId)
    {
        // Simple implementation: just return current value
        // In a real system, this would evaluate formulas
        return GetStatValue(world, entity, derivedStatId);
    }

    public void RecalculateDerivedStats(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>()) return;

        ref var stats = ref entity.Get<PigeonPea.Shared.ECS.Components.Stats>();
        var modifiers = entity.Has<StatModifiers>() ? entity.Get<StatModifiers>() : new StatModifiers();

        // Reset current to base
        foreach (var kvp in stats.BaseStats)
        {
            stats.CurrentStats[kvp.Key] = kvp.Value;
        }

        // Apply Additive
        foreach (var mod in modifiers.Modifiers.Where(m => m.Type == ModifierType.Additive))
        {
            if (stats.CurrentStats.ContainsKey(mod.StatId))
            {
                stats.CurrentStats[mod.StatId] += mod.Value;
            }
        }

        // Apply Multiplicative
        foreach (var mod in modifiers.Modifiers.Where(m => m.Type == ModifierType.Multiplicative))
        {
            if (stats.CurrentStats.ContainsKey(mod.StatId))
            {
                stats.CurrentStats[mod.StatId] *= (1 + mod.Value);
            }
        }

        // Clamp values if definitions exist
        foreach (var kvp in stats.CurrentStats.ToList())
        {
            if (_statDefinitions.TryGetValue(kvp.Key, out var def))
            {
                stats.CurrentStats[kvp.Key] = Math.Clamp(kvp.Value, def.MinValue, def.MaxValue);
            }
        }
    }

    public StatDefinition? GetStatDefinition(string statId)
    {
        return _statDefinitions.TryGetValue(statId, out var def) ? def : null;
    }

    public IReadOnlyList<StatDefinition> GetAllStatDefinitions()
    {
        return _statDefinitions.Values.ToList();
    }

    public IReadOnlyList<StatDefinition> GetStatDefinitionsByCategory(string category)
    {
        return _statDefinitions.Values.Where(d => d.Category == category).ToList();
    }

    public bool SetStats(World world, Entity entity, Dictionary<string, float> stats)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Stats>())
        {
            entity.Add(new PigeonPea.Shared.ECS.Components.Stats());
        }

        ref var comp = ref entity.Get<PigeonPea.Shared.ECS.Components.Stats>();
        comp.BaseStats = new Dictionary<string, float>(stats);
        comp.CurrentStats = new Dictionary<string, float>(stats);

        RecalculateDerivedStats(world, entity);
        return true;
    }
}

