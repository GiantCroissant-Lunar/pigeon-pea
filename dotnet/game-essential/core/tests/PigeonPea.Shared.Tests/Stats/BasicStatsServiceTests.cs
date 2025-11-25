using System;
using System.Collections.Generic;
using Arch.Core;
using PigeonPea.Game.Contracts.Stats.Services;
using PigeonPea.Plugin.Stats.Basic;
using Xunit;

namespace PigeonPea.Shared.Tests.Stats;

public class BasicStatsServiceTests
{
    private static BasicStatsService CreateService()
    {
        var definitions = new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["strength"] = new StatDefinition
            {
                Id = "strength",
                DisplayName = "Strength",
                Category = "attribute",
                MinValue = 0,
                MaxValue = 100,
                DefaultValue = 0
            },
            ["constitution"] = new StatDefinition
            {
                Id = "constitution",
                DisplayName = "Constitution",
                Category = "attribute",
                MinValue = 0,
                MaxValue = 100,
                DefaultValue = 0
            },
            ["level"] = new StatDefinition
            {
                Id = "level",
                DisplayName = "Level",
                Category = "attribute",
                MinValue = 1,
                MaxValue = 100,
                DefaultValue = 1
            },
            ["max_health"] = new StatDefinition
            {
                Id = "max_health",
                DisplayName = "Max Health",
                Category = "derived",
                MinValue = 0,
                MaxValue = 10000,
                DefaultValue = 0,
                Formula = "constitution * 10 + level * 5"
            }
        };

        var evaluator = new FormulaEvaluator();
        return new BasicStatsService(definitions, evaluator);
    }

    [Fact]
    public void SetStat_SetsBaseValue()
    {
        var world = World.Create();
        var entity = world.Create();
        var service = CreateService();

        var ok = service.SetStat(world, entity, "strength", 15);

        Assert.True(ok);
        var view = service.GetStats(world, entity);
        Assert.True(view.BaseStats.TryGetValue("strength", out var value));
        Assert.Equal(15, value);
    }

    [Fact]
    public void AddModifier_AppliesAdditiveModifier()
    {
        var world = World.Create();
        var entity = world.Create();
        var service = CreateService();

        service.SetStat(world, entity, "strength", 10);
        var id = service.AddModifier(world, entity, new StatModifier
        {
            StatId = "strength",
            Value = 5,
            Type = ModifierType.Additive,
            Duration = -1,
            SourceId = "test"
        });

        Assert.False(string.IsNullOrWhiteSpace(id));
        var value = service.GetStatValue(world, entity, "strength");
        Assert.Equal(15, value);
    }

    [Fact]
    public void CalculateDerivedStat_EvaluatesFormula()
    {
        var world = World.Create();
        var entity = world.Create();
        var service = CreateService();

        service.SetStat(world, entity, "constitution", 14);
        service.SetStat(world, entity, "level", 1);

        var value = service.CalculateDerivedStat(world, entity, "max_health");
        Assert.Equal(145, value);
    }
}
