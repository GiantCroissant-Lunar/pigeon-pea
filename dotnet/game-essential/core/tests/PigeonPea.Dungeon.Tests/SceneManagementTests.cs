using Arch.Core;
using FluentAssertions;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Plugin.Dungeon.Basic;
using PigeonPea.Plugin.Dungeon.ModernEdgar;
using PigeonPea.Scene.Contracts;
using PigeonPea.Shared.Components;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

/// <summary>
/// Tests for RFC-035 Scene Management integration with dungeon generation.
/// Verifies that dungeons are properly created as entities in ECS worlds.
/// </summary>
public class SceneManagementTests : IDisposable
{
    private readonly World _world;

    public SceneManagementTests()
    {
        _world = World.Create();
    }

    public void Dispose()
    {
        World.Destroy(_world);
        GC.SuppressFinalize(this);
    }

    #region BasicDungeonGenerator Tests

    [Fact]
    public void BasicGenerator_CreatesEntityInWorld()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 40,
            Height = 30,
            Seed = 12345
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);

        // Assert
        _world.IsAlive(dungeonEntity).Should().BeTrue("dungeon entity should exist in world");
        _world.Has<DungeonMapComponent>(dungeonEntity).Should().BeTrue("dungeon should have DungeonMapComponent");
    }

    [Fact]
    public void BasicGenerator_DungeonHasCorrectDimensions()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 50,
            Height = 40,
            Seed = 67890
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);
        var dungeonMap = _world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        dungeonMap.Width.Should().Be(50, "dungeon width should match options");
        dungeonMap.Height.Should().Be(40, "dungeon height should match options");
        dungeonMap.TileData.Should().NotBeNull("tile data should be initialized");
        dungeonMap.TileData.Length.Should().Be(50 * 40, "tile data should have correct size");
    }

    [Fact]
    public void BasicGenerator_CreatesDeterministicDungeon()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 30,
            Height = 25,
            Seed = 99999
        };

        // Act
        var dungeon1Entity = generator.Generate(_world, options);
        var dungeon1 = _world.Get<DungeonMapComponent>(dungeon1Entity);

        var world2 = World.Create();
        var dungeon2Entity = generator.Generate(world2, options);
        var dungeon2 = world2.Get<DungeonMapComponent>(dungeon2Entity);

        // Assert
        dungeon1.TileData.Should().BeEquivalentTo(dungeon2.TileData,
            "dungeons with same seed should be identical");

        World.Destroy(world2);
    }

    [Fact]
    public void BasicGenerator_CreatesWalkableFloors()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 60,
            Height = 40,
            Seed = 11111
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);
        var dungeon = _world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        var walkableTileCount = 0;
        for (int i = 0; i < dungeon.TileData.Length; i++)
        {
            if (dungeon.Walkable![i])
            {
                walkableTileCount++;
            }
        }

        walkableTileCount.Should().BeGreaterThan(0, "dungeon should have walkable floors");
        walkableTileCount.Should().BeLessThan(dungeon.TileData.Length,
            "dungeon should have walls (not all walkable)");
    }

    #endregion

    #region ModernEdgarDungeonGenerator Tests

    [Fact]
    public void EdgarGenerator_CreatesEntityInWorld()
    {
        // Arrange
        var generator = new ModernEdgarDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 80,
            Height = 50,
            Seed = 54321
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);

        // Assert
        _world.IsAlive(dungeonEntity).Should().BeTrue("dungeon entity should exist in world");
        _world.Has<DungeonMapComponent>(dungeonEntity).Should().BeTrue("dungeon should have DungeonMapComponent");
    }

    [Fact]
    public void EdgarGenerator_DungeonHasCorrectDimensions()
    {
        // Arrange
        var generator = new ModernEdgarDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 100,
            Height = 60,
            Seed = 22222
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);
        var dungeonMap = _world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        dungeonMap.Width.Should().Be(100, "dungeon width should match options");
        dungeonMap.Height.Should().Be(60, "dungeon height should match options");
        dungeonMap.TileData.Should().NotBeNull("tile data should be initialized");
        dungeonMap.TileData.Length.Should().Be(100 * 60, "tile data should have correct size");
    }

    [Fact]
    public void EdgarGenerator_CreatesValidLayout()
    {
        // Arrange
        var generator = new ModernEdgarDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions
        {
            Width = 80,
            Height = 50,
            Seed = 33333
        };

        // Act
        var dungeonEntity = generator.Generate(_world, options);
        var dungeon = _world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        var walkableTileCount = 0;
        for (int i = 0; i < dungeon.TileData.Length; i++)
        {
            if (dungeon.Walkable![i])
            {
                walkableTileCount++;
            }
        }

        walkableTileCount.Should().BeGreaterThan(0, "Edgar dungeon should have walkable rooms");
    }

    #endregion

    #region ECS Integration Tests

    [Fact]
    public void DungeonEntity_CanBeQueriedFromWorld()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions { Width = 40, Height = 30, Seed = 44444 };

        // Act
        var dungeonEntity = generator.Generate(_world, options);

        // Query for dungeon
        var query = new QueryDescription().WithAll<DungeonMapComponent>();
        var foundEntity = Entity.Null;
        _world.Query(in query, (Entity entity, ref DungeonMapComponent _) =>
        {
            foundEntity = entity;
        });

        // Assert
        foundEntity.Should().Be(dungeonEntity, "should find dungeon entity via query");
    }

    [Fact]
    public void MultipleEntities_CanCoexistInSameWorld()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var dungeonOptions = new DungeonGenerationOptions { Width = 40, Height = 30, Seed = 55555 };

        // Act
        var dungeonEntity = generator.Generate(_world, dungeonOptions);
        var playerEntity = _world.Create(
            new PositionComponent(20, 15),
            new RenderableComponent
            {
                Glyph = '@',
                Foreground = SadRogue.Primitives.Color.Yellow,
                Background = SadRogue.Primitives.Color.Black
            }
        );

        // Assert
        _world.IsAlive(dungeonEntity).Should().BeTrue("dungeon entity should exist");
        _world.IsAlive(playerEntity).Should().BeTrue("player entity should exist");

        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        var dungeonCount = 0;
        _world.Query(in dungeonQuery, (Entity _, ref DungeonMapComponent _) => dungeonCount++);
        dungeonCount.Should().Be(1, "should have exactly one dungeon");

        var playerQuery = new QueryDescription().WithAll<PositionComponent, RenderableComponent>();
        var playerCount = 0;
        _world.Query(in playerQuery, (Entity _, ref PositionComponent _, ref RenderableComponent _) => playerCount++);
        playerCount.Should().Be(1, "should have exactly one player");
    }

    [Fact]
    public void DungeonComponent_ContainsRequiredData()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        InitializeGenerator(generator);
        var options = new DungeonGenerationOptions { Width = 50, Height = 40, Seed = 66666 };

        // Act
        var dungeonEntity = generator.Generate(_world, options);
        var dungeon = _world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        dungeon.Width.Should().BeGreaterThan(0, "width should be set");
        dungeon.Height.Should().BeGreaterThan(0, "height should be set");
        dungeon.TileData.Should().NotBeNull("tile data should exist");
        dungeon.Walkable.Should().NotBeNull("walkable array should exist");
        dungeon.Opaque.Should().NotBeNull("opaque array should exist");
        dungeon.DoorStates.Should().NotBeNull("door states should exist");
    }

    #endregion

    #region Helper Methods

    private static void InitializeGenerator(IDungeonGenerator generator)
    {
        // Initialize generator if it has an InitializeAsync method (plugin interface)
        if (generator is BasicDungeonGenerator basic)
        {
            // BasicDungeonGenerator implements IPlugin but doesn't need initialization for standalone use
            // The logger will be null, which is acceptable for testing
        }
        else if (generator is ModernEdgarDungeonGenerator edgar)
        {
            // Same for ModernEdgarDungeonGenerator
        }
    }

    #endregion
}
