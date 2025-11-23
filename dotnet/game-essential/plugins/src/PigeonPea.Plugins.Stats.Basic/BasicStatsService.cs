using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Contracts.Stats.Services;
using PigeonPea.Shared.Components;
using StatsComponent = PigeonPea.Shared.Components.Stats;

namespace PigeonPea.Plugins.Stats.Basic;

public sealed class BasicStatsService : IService
{
    private readonly IReadOnlyDictionary<string, StatDefinition> _definitions;
    private readonly IFormulaEvaluator _formulaEvaluator;

    public BasicStatsService(IReadOnlyDictionary<string, StatDefinition> definitions, IFormulaEvaluator formulaEvaluator)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _formulaEvaluator = formulaEvaluator ?? throw new ArgumentNullException(nameof(formulaEvaluator));
    }

    public StatsView GetStats(World world, Entity entity)
    {
        if (!world.Has<StatsComponent>(entity))
        {
            return StatsView.Empty;
        }

        ref var stats = ref world.Get<StatsComponent>(entity);

        List<StatModifierView> modifierViews;
        if (world.Has<StatModifiers>(entity))
        {
            ref var modifiers = ref world.Get<StatModifiers>(entity);
            modifierViews = new List<StatModifierView>(modifiers.Modifiers.Count);
            foreach (var m in modifiers.Modifiers)
            {
                modifierViews.Add(new StatModifierView
                {
                    ModifierId = m.ModifierId,
                    StatId = m.StatId,
                    Value = m.Value,
                    Type = m.Type,
                    RemainingDuration = m.RemainingDuration,
                    SourceId = m.SourceId,
                    AppliedAt = m.AppliedAt
                });
            }
        }
        else
        {
            modifierViews = new List<StatModifierView>();
        }

        return new StatsView
        {
            BaseStats = stats.BaseStats,
            CurrentStats = stats.CurrentStats,
            ActiveModifiers = modifierViews
        };
    }

    public bool SetStat(World world, Entity entity, string statId, float value)
    {
        var definition = GetStatDefinition(statId);
        if (definition == null)
        {
            return false;
        }

        value = Clamp(value, definition);

        if (!world.Has<StatsComponent>(entity))
        {
            world.Add(entity, new StatsComponent());
        }

        ref var stats = ref world.Get<StatsComponent>(entity);
        stats.BaseStats[statId] = value;

        RecalculateCurrentValue(world, entity, statId, definition, ref stats);
        return true;
    }

    public float GetStatValue(World world, Entity entity, string statId)
    {
        var definition = GetStatDefinition(statId);
        if (definition == null)
        {
            return 0f;
        }

        if (!world.Has<StatsComponent>(entity))
        {
            return definition.DefaultValue;
        }

        ref var stats = ref world.Get<StatsComponent>(entity);

        if (!stats.CurrentStats.TryGetValue(statId, out var value))
        {
            RecalculateCurrentValue(world, entity, statId, definition, ref stats);
            if (!stats.CurrentStats.TryGetValue(statId, out value))
            {
                value = definition.DefaultValue;
            }
        }

        return value;
    }

    public float GetBaseStatValue(World world, Entity entity, string statId)
    {
        var definition = GetStatDefinition(statId);
        if (definition == null)
        {
            return 0f;
        }

        if (!world.Has<StatsComponent>(entity))
        {
            return definition.DefaultValue;
        }

        ref var stats = ref world.Get<StatsComponent>(entity);
        return stats.BaseStats.TryGetValue(statId, out var value) ? value : definition.DefaultValue;
    }

    public string AddModifier(World world, Entity entity, StatModifier modifier)
    {
        if (string.IsNullOrWhiteSpace(modifier.StatId))
        {
            return string.Empty;
        }

        var definition = GetStatDefinition(modifier.StatId);
        if (definition == null)
        {
            return string.Empty;
        }

        if (!world.Has<StatModifiers>(entity))
        {
            world.Add(entity, new StatModifiers());
        }

        ref var modifiers = ref world.Get<StatModifiers>(entity);

        var id = Guid.NewGuid().ToString();

        var active = new ActiveModifier
        {
            ModifierId = id,
            StatId = modifier.StatId,
            Value = modifier.Value,
            Type = modifier.Type,
            RemainingDuration = modifier.Duration,
            SourceId = modifier.SourceId,
            AppliedAt = DateTime.UtcNow
        };

        modifiers.Modifiers.Add(active);

        if (!world.Has<StatsComponent>(entity))
        {
            world.Add(entity, new StatsComponent());
        }

        ref var stats = ref world.Get<StatsComponent>(entity);
        RecalculateCurrentValue(world, entity, modifier.StatId, definition, ref stats);

        return id;
    }

    public bool RemoveModifier(World world, Entity entity, string modifierId)
    {
        if (!world.Has<StatModifiers>(entity))
        {
            return false;
        }

        ref var modifiers = ref world.Get<StatModifiers>(entity);
        string? affectedStatId = null;

        for (int i = 0; i < modifiers.Modifiers.Count; i++)
        {
            if (string.Equals(modifiers.Modifiers[i].ModifierId, modifierId, StringComparison.Ordinal))
            {
                affectedStatId = modifiers.Modifiers[i].StatId;
                modifiers.Modifiers.RemoveAt(i);
                break;
            }
        }

        if (affectedStatId == null)
        {
            return false;
        }

        var definition = GetStatDefinition(affectedStatId);
        if (definition == null)
        {
            return true;
        }

        if (world.Has<StatsComponent>(entity))
        {
            ref var stats = ref world.Get<StatsComponent>(entity);
            RecalculateCurrentValue(world, entity, affectedStatId, definition, ref stats);
        }

        return true;
    }

    public int RemoveModifiersBySource(World world, Entity entity, string sourceId)
    {
        if (!world.Has<StatModifiers>(entity))
        {
            return 0;
        }

        ref var modifiers = ref world.Get<StatModifiers>(entity);
        if (modifiers.Modifiers.Count == 0)
        {
            return 0;
        }

        var affectedStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int removed = 0;

        for (int i = modifiers.Modifiers.Count - 1; i >= 0; i--)
        {
            if (string.Equals(modifiers.Modifiers[i].SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
            {
                affectedStats.Add(modifiers.Modifiers[i].StatId);
                modifiers.Modifiers.RemoveAt(i);
                removed++;
            }
        }

        if (removed == 0)
        {
            return 0;
        }

        if (world.Has<StatsComponent>(entity))
        {
            ref var stats = ref world.Get<StatsComponent>(entity);
            foreach (var statId in affectedStats)
            {
                var definition = GetStatDefinition(statId);
                if (definition != null)
                {
                    RecalculateCurrentValue(world, entity, statId, definition, ref stats);
                }
            }
        }

        return removed;
    }

    public IReadOnlyList<StatModifierView> GetModifiers(World world, Entity entity)
    {
        if (!world.Has<StatModifiers>(entity))
        {
            return Array.Empty<StatModifierView>();
        }

        ref var modifiers = ref world.Get<StatModifiers>(entity);
        if (modifiers.Modifiers.Count == 0)
        {
            return Array.Empty<StatModifierView>();
        }

        var result = new List<StatModifierView>(modifiers.Modifiers.Count);
        foreach (var m in modifiers.Modifiers)
        {
            result.Add(new StatModifierView
            {
                ModifierId = m.ModifierId,
                StatId = m.StatId,
                Value = m.Value,
                Type = m.Type,
                RemainingDuration = m.RemainingDuration,
                SourceId = m.SourceId,
                AppliedAt = m.AppliedAt
            });
        }

        return result;
    }

    public float CalculateDerivedStat(World world, Entity entity, string derivedStatId)
    {
        var definition = GetStatDefinition(derivedStatId);
        if (definition == null || string.IsNullOrWhiteSpace(definition.Formula))
        {
            return 0f;
        }

        var context = BuildFormulaContext(world, entity);
        return _formulaEvaluator.Evaluate(definition.Formula, context);
    }

    public void RecalculateDerivedStats(World world, Entity entity)
    {
        if (!world.Has<StatsComponent>(entity))
        {
            world.Add(entity, new StatsComponent());
        }

        ref var stats = ref world.Get<StatsComponent>(entity);

        foreach (var definition in _definitions.Values)
        {
            if (string.IsNullOrWhiteSpace(definition.Formula))
            {
                continue;
            }

            var value = CalculateDerivedStat(world, entity, definition.Id);
            stats.CurrentStats[definition.Id] = Clamp(value, definition);
        }
    }

    public StatDefinition? GetStatDefinition(string statId)
    {
        if (string.IsNullOrWhiteSpace(statId))
        {
            return null;
        }

        if (_definitions.TryGetValue(statId, out var definition))
        {
            return definition;
        }

        return null;
    }

    public IReadOnlyList<StatDefinition> GetAllStatDefinitions()
    {
        return new List<StatDefinition>(_definitions.Values);
    }

    public IReadOnlyList<StatDefinition> GetStatDefinitionsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Array.Empty<StatDefinition>();
        }

        var result = new List<StatDefinition>();
        foreach (var definition in _definitions.Values)
        {
            if (string.Equals(definition.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(definition);
            }
        }

        return result;
    }

    public bool SetStats(World world, Entity entity, Dictionary<string, float> stats)
    {
        if (stats == null || stats.Count == 0)
        {
            return false;
        }

        var allOk = true;
        foreach (var kvp in stats)
        {
            var ok = SetStat(world, entity, kvp.Key, kvp.Value);
            if (!ok)
            {
                allOk = false;
            }
        }

        return allOk;
    }

    private static float Clamp(float value, StatDefinition definition)
    {
        if (definition.MinValue != 0f || definition.MaxValue != 0f)
        {
            if (value < definition.MinValue)
            {
                value = definition.MinValue;
            }
            if (value > definition.MaxValue)
            {
                value = definition.MaxValue;
            }
        }

        return value;
    }

    private static void EnsureComponents(World world, Entity entity)
    {
        if (!world.Has<StatsComponent>(entity))
        {
            world.Add(entity, new StatsComponent());
        }

        if (!world.Has<StatModifiers>(entity))
        {
            world.Add(entity, new StatModifiers());
        }
    }

    private void RecalculateCurrentValue(World world, Entity entity, string statId, StatDefinition definition, ref StatsComponent stats)
    {
        EnsureComponents(world, entity);

        float baseValue;
        if (!stats.BaseStats.TryGetValue(statId, out baseValue))
        {
            baseValue = definition.DefaultValue;
        }

        float additive = 0f;
        float multiplicative = 1f;

        if (world.Has<StatModifiers>(entity))
        {
            ref var modifiers = ref world.Get<StatModifiers>(entity);
            foreach (var m in modifiers.Modifiers)
            {
                if (!string.Equals(m.StatId, statId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (m.Type == ModifierType.Additive)
                {
                    additive += m.Value;
                }
                else if (m.Type == ModifierType.Multiplicative)
                {
                    multiplicative *= m.Value;
                }
            }
        }

        var current = (baseValue + additive) * multiplicative;
        stats.CurrentStats[statId] = Clamp(current, definition);
    }

    private Dictionary<string, float> BuildFormulaContext(World world, Entity entity)
    {
        var context = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        if (world.Has<StatsComponent>(entity))
        {
            ref var stats = ref world.Get<StatsComponent>(entity);
            foreach (var kvp in stats.CurrentStats)
            {
                context[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in stats.BaseStats)
            {
                if (!context.ContainsKey(kvp.Key))
                {
                    context[kvp.Key] = kvp.Value;
                }
            }
        }

        if (entity.Has<Experience>())
        {
            var experience = entity.Get<Experience>();
            context["level"] = experience.Level;
        }

        foreach (var definition in _definitions.Values)
        {
            if (!context.ContainsKey(definition.Id))
            {
                var value = GetStatValue(world, entity, definition.Id);
                context[definition.Id] = value;
            }
        }

        return context;
    }
}
