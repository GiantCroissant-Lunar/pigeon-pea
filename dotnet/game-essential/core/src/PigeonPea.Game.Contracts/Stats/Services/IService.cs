using System.Collections.Generic;
using Arch.Core;
using PigeonPea.Game.Contracts.Stats.Models;

namespace PigeonPea.Game.Contracts.Stats.Services;

public interface IService
{
    StatsView GetStats(World world, Entity entity);

    bool SetStat(World world, Entity entity, string statId, float value);

    float GetStatValue(World world, Entity entity, string statId);

    float GetBaseStatValue(World world, Entity entity, string statId);

    string AddModifier(World world, Entity entity, StatModifier modifier);

    bool RemoveModifier(World world, Entity entity, string modifierId);

    int RemoveModifiersBySource(World world, Entity entity, string sourceId);

    IReadOnlyList<StatModifierView> GetModifiers(World world, Entity entity);

    float CalculateDerivedStat(World world, Entity entity, string derivedStatId);

    void RecalculateDerivedStats(World world, Entity entity);

    StatDefinition? GetStatDefinition(string statId);

    IReadOnlyList<StatDefinition> GetAllStatDefinitions();

    IReadOnlyList<StatDefinition> GetStatDefinitionsByCategory(string category);

    bool SetStats(World world, Entity entity, Dictionary<string, float> stats);
}

