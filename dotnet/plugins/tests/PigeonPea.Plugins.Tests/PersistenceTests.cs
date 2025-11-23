using System;
using System.IO;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using FluentAssertions;
using Moq;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Plugins.Persistence.Json;
using SadRogue.Primitives;
using Xunit;

using CharacterComponent = PigeonPea.Shared.ECS.Components.Character;
using StatsComponent = PigeonPea.Shared.ECS.Components.Stats;
using PositionComponent = PigeonPea.Shared.Components.Position;
using HealthComponent = PigeonPea.Shared.Components.Health;

namespace PigeonPea.Plugins.Tests;

public class PersistenceTests : IDisposable
{
    private readonly JsonPersistenceService _service;
    private readonly World _world;
    private readonly Mock<IPluginContext> _contextMock;
    private readonly Mock<IRegistry> _registryMock;
    private readonly string _testSaveDir = "saves";
    private bool _disposed;

    public PersistenceTests()
    {
        _registryMock = new Mock<IRegistry>();
        _contextMock = new Mock<IPluginContext>();
        _contextMock.Setup(c => c.Registry).Returns(_registryMock.Object);

        _service = new JsonPersistenceService();
        _service.InitializeAsync(_contextMock.Object).Wait();
        _world = World.Create();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        if (Directory.Exists(_testSaveDir))
        {
            try
            {
                Directory.Delete(_testSaveDir, true);
            }
            catch (IOException)
            {
                // Ignore IO errors during test cleanup
            }
        }

        _world.Dispose();
        _disposed = true;
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistEntityComponents()
    {
        // Arrange
        var entity = _world.Create();
        var charComp = new CharacterComponent { Name = "Hero", ClassId = "Warrior", Level = 5 };
        entity.Add(charComp);

        // Verify immediate storage
        var storedChar = entity.Get<CharacterComponent>();
        storedChar.Name.Should().Be("Hero");
        storedChar.Level.Should().Be(5);

        entity.Add(new PositionComponent(new Point(10, 20)));
        entity.Add(new HealthComponent(100, 100));
        entity.Add(new StatsComponent()); // Empty stats for now

        var saveName = "test_save";

        // Act
        var saveResult = _service.SaveWorld(_world, saveName);
        saveResult.Success.Should().BeTrue();

        // Clear world (simulate new session)
        // Arch doesn't have Clear(), so we create a new world
        using var newWorld = World.Create();

        var loadResult = _service.LoadWorld(newWorld, saveName);
        loadResult.Success.Should().BeTrue();
        loadResult.EntitiesLoaded.Should().Be(1);

        // Assert
        var query = new QueryDescription().WithAll<CharacterComponent, PositionComponent, HealthComponent>();
        var count = 0;
        newWorld.Query(in query, (Entity e, ref CharacterComponent c, ref PositionComponent p, ref HealthComponent h) =>
        {
            count++;
            c.Name.Should().Be("Hero");
            c.Level.Should().Be(5);
            p.Point.X.Should().Be(10);
            p.Point.Y.Should().Be(20);
            h.Current.Should().Be(100);
        });

        count.Should().Be(1);
    }

    [Fact]
    public void Load_ShouldFail_WhenFileDoesNotExist()
    {
        var result = _service.LoadWorld(_world, "non_existent_save");
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}
