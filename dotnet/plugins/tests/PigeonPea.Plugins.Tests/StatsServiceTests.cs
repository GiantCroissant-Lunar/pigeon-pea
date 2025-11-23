using System.Collections.Generic;
using System.Threading.Tasks;
using Arch.Core;
using FluentAssertions;
using Moq;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Stats.Models;
using PigeonPea.Plugins.Stats.Basic;
using PigeonPea.Shared.ECS.Components;
using Xunit;

namespace PigeonPea.Plugins.Tests;

public class StatsServiceTests
{
    private readonly BasicStatsService _service;
    private readonly World _world;
    private readonly Mock<IPluginContext> _contextMock;
    private readonly Mock<IRegistry> _registryMock;

    public StatsServiceTests()
    {
        _registryMock = new Mock<IRegistry>();
        _contextMock = new Mock<IPluginContext>();
        _contextMock.Setup(c => c.Registry).Returns(_registryMock.Object);

        _service = new BasicStatsService();
        _service.InitializeAsync(_contextMock.Object).Wait();
        _world = World.Create();
    }

    [Fact]
    public void SetStat_ShouldUpdateBaseAndCurrentValues()
    {
        var entity = _world.Create();

        _service.SetStat(_world, entity, "Health", 100);

        var stats = _service.GetStats(_world, entity);
        stats.BaseStats["Health"].Should().Be(100);
        stats.CurrentStats["Health"].Should().Be(100);
    }

    [Fact]
    public void AddModifier_ShouldAffectCurrentValue()
    {
        var entity = _world.Create();
        _service.SetStat(_world, entity, "Attack", 10);

        var modifier = new StatModifier
        {
            StatId = "Attack",
            Value = 5,
            Type = ModifierType.Additive,
            Duration = 10,
            SourceId = "TestBuff"
        };

        _service.AddModifier(_world, entity, modifier);

        var stats = _service.GetStats(_world, entity);
        stats.BaseStats["Attack"].Should().Be(10);
        stats.CurrentStats["Attack"].Should().Be(15);
    }

    [Fact]
    public void AddModifier_Multiplicative_ShouldAffectCurrentValue()
    {
        var entity = _world.Create();
        _service.SetStat(_world, entity, "Speed", 100);

        var modifier = new StatModifier
        {
            StatId = "Speed",
            Value = 0.5f, // +50%
            Type = ModifierType.Multiplicative,
            Duration = 10,
            SourceId = "Haste"
        };

        _service.AddModifier(_world, entity, modifier);

        var stats = _service.GetStats(_world, entity);
        stats.BaseStats["Speed"].Should().Be(100);
        stats.CurrentStats["Speed"].Should().Be(150);
    }
}
