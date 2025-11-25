using Arch.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.PluginSystem;
using PigeonPea.Console;
using PigeonPea.Scene.Contracts;
using PigeonPea.Shared.Components;
using Xunit;

namespace PigeonPea.Console.Tests;

/// <summary>
/// Integration tests for RFC-035 Scene Management in the console application.
/// Tests the full stack: plugin loading → scene creation → dungeon generation → entity creation.
/// </summary>
public class SceneIntegrationTests : IDisposable
{
    private readonly IHost _host;
    private readonly IRegistry _registry;
    private readonly ILogger<SceneIntegrationTests> _logger;

    public SceneIntegrationTests()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Game:DungeonGen"] = "modern-edgar",
            ["DungeonSystem:UseNewPluginArchitecture"] = "true"
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddPluginSystem(builder.Configuration);
        builder.Services.AddHostedService<PluginLoaderHostedService>();

        _host = builder.Build();
        _host.Start();

        _registry = _host.Services.GetRequiredService<IRegistry>();
        _logger = _host.Services.GetRequiredService<ILogger<SceneIntegrationTests>>();
    }

    public void Dispose()
    {
        _host.StopAsync().Wait();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void PluginSystem_LoadsSceneManagerPlugin()
    {
        // Act
        var isRegistered = _registry.IsRegistered<ISceneManager>();

        // Assert
        isRegistered.Should().BeTrue("SceneManager plugin should be loaded");

        if (isRegistered)
        {
            var sceneManager = _registry.Get<ISceneManager>();
            sceneManager.Should().NotBeNull("SceneManager should be available");
            _logger.LogInformation("SceneManager loaded: {Type}", sceneManager.GetType().FullName);
        }
    }

    [Fact]
    public void PluginSystem_LoadsDungeonGeneratorPlugin()
    {
        // Act
        var isRegistered = _registry.IsRegistered<IDungeonGenerator>();

        // Assert
        isRegistered.Should().BeTrue("DungeonGenerator plugin should be loaded");

        if (isRegistered)
        {
            var generator = _registry.Get<IDungeonGenerator>();
            generator.Should().NotBeNull("DungeonGenerator should be available");
            _logger.LogInformation("DungeonGenerator loaded: {Type}", generator.GetType().FullName);
        }
    }

    [Fact]
    public async Task SceneManager_CanLoadScene()
    {
        // Arrange
        if (!_registry.IsRegistered<ISceneManager>())
        {
            // Skip test if SceneManager plugin not loaded (CI environment)
            return;
        }

        var sceneManager = _registry.Get<ISceneManager>();

        // Act
        var scene = await sceneManager.LoadSceneAsync("TestScene", SceneLoadMode.Single);

        // Assert
        scene.Should().NotBeNull("scene should be created");
        scene.Name.Should().Be("TestScene", "scene name should match");
        scene.State.Should().Be(SceneState.Active, "scene should be in active state");
        scene.World.Should().NotBeNull("scene should have a world");
    }

    [Fact]
    public async Task SceneManager_GetActiveScene_ReturnsActiveScene()
    {
        // Arrange
        if (!_registry.IsRegistered<ISceneManager>())
        {
            return;
        }

        var sceneManager = _registry.Get<ISceneManager>();
        var loadedScene = await sceneManager.LoadSceneAsync("ActiveSceneTest", SceneLoadMode.Single);

        // Act
        var activeScene = sceneManager.GetActiveScene();

        // Assert
        activeScene.Should().NotBeNull("should have an active scene");
        activeScene!.Id.Should().Be(loadedScene.Id, "active scene should be the loaded scene");
        activeScene.Name.Should().Be("ActiveSceneTest", "active scene name should match");
    }

    [Fact]
    public async Task FullStack_SceneWithDungeonAndPlayer()
    {
        // Arrange
        if (!_registry.IsRegistered<ISceneManager>() || !_registry.IsRegistered<IDungeonGenerator>())
        {
            return;
        }

        var sceneManager = _registry.Get<ISceneManager>();
        var generator = _registry.Get<IDungeonGenerator>();

        // Act - Create scene
        var scene = await sceneManager.LoadSceneAsync("DungeonGameplay", SceneLoadMode.Single);
        var world = scene.World!;

        // Act - Generate dungeon
        var dungeonEntity = generator.Generate(world, new DungeonGenerationOptions
        {
            Width = 80,
            Height = 50,
            Seed = 12345
        });

        // Act - Create player
        var playerEntity = world.Create(
            new PositionComponent(40, 25),
            new RenderableComponent
            {
                Glyph = '@',
                Foreground = SadRogue.Primitives.Color.Yellow,
                Background = SadRogue.Primitives.Color.Black,
                Layer = RenderLayer.Actor
            },
            new PlayerComponent("TestPlayer"),
            new PlayerInputComponent(System.Numerics.Vector2.Zero, false)
        );

        // Assert - Scene exists
        scene.Should().NotBeNull("scene should exist");
        scene.State.Should().Be(SceneState.Active, "scene should be active");

        // Assert - Dungeon entity exists and has correct component
        world.IsAlive(dungeonEntity).Should().BeTrue("dungeon entity should exist");
        world.Has<DungeonMapComponent>(dungeonEntity).Should().BeTrue("dungeon should have DungeonMapComponent");

        var dungeonMap = world.Get<DungeonMapComponent>(dungeonEntity);
        dungeonMap.Width.Should().Be(80, "dungeon width should match");
        dungeonMap.Height.Should().Be(50, "dungeon height should match");

        // Assert - Player entity exists and has correct components
        world.IsAlive(playerEntity).Should().BeTrue("player entity should exist");
        world.Has<PositionComponent>(playerEntity).Should().BeTrue("player should have PositionComponent");
        world.Has<RenderableComponent>(playerEntity).Should().BeTrue("player should have RenderableComponent");
        world.Has<PlayerComponent>(playerEntity).Should().BeTrue("player should have PlayerComponent");

        var playerPos = world.Get<PositionComponent>(playerEntity);
        playerPos.X.Should().Be(40, "player X position should match");
        playerPos.Y.Should().Be(25, "player Y position should match");

        var playerRenderable = world.Get<RenderableComponent>(playerEntity);
        playerRenderable.Glyph.Should().Be('@', "player glyph should be @");
        playerRenderable.Foreground.Should().Be(SadRogue.Primitives.Color.Yellow, "player should be yellow");

        // Assert - Can query entities from world
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        var dungeonCount = 0;
        world.Query(in dungeonQuery, (Entity _, ref DungeonMapComponent _) => dungeonCount++);
        dungeonCount.Should().Be(1, "should have exactly one dungeon");

        var playerQuery = new QueryDescription().WithAll<PlayerComponent>();
        var playerCount = 0;
        world.Query(in playerQuery, (Entity _, ref PlayerComponent _) => playerCount++);
        playerCount.Should().Be(1, "should have exactly one player");

        _logger.LogInformation(
            "Full stack test passed: Scene={SceneId}, Dungeon={DungeonId}, Player={PlayerId}",
            scene.Id, dungeonEntity.Id, playerEntity.Id);
    }

    [Fact]
    public async Task DungeonEntity_CanBeRenderedViaEcsQuery()
    {
        // Arrange
        if (!_registry.IsRegistered<ISceneManager>() || !_registry.IsRegistered<IDungeonGenerator>())
        {
            return;
        }

        var sceneManager = _registry.Get<ISceneManager>();
        var generator = _registry.Get<IDungeonGenerator>();

        var scene = await sceneManager.LoadSceneAsync("RenderTestScene", SceneLoadMode.Single);
        var world = scene.World!;

        var dungeonEntity = generator.Generate(world, new DungeonGenerationOptions
        {
            Width = 40,
            Height = 30,
            Seed = 99999
        });

        // Act - Query dungeon for rendering
        var query = new QueryDescription().WithAll<DungeonMapComponent>();
        bool foundDungeon = false;
        int foundWidth = 0;
        int foundHeight = 0;

        world.Query(in query, (ref DungeonMapComponent dungeon) =>
        {
            foundDungeon = true;
            foundWidth = dungeon.Width;
            foundHeight = dungeon.Height;
        });

        // Assert
        foundDungeon.Should().BeTrue("should find dungeon via ECS query");
        foundWidth.Should().Be(40, "queried dungeon should have correct width");
        foundHeight.Should().Be(30, "queried dungeon should have correct height");
    }

    [Fact]
    public async Task MultipleScenes_CanHaveIndependentWorlds()
    {
        // Arrange
        if (!_registry.IsRegistered<ISceneManager>())
        {
            return;
        }

        var sceneManager = _registry.Get<ISceneManager>();

        // Act
        var scene1 = await sceneManager.LoadSceneAsync("World1", SceneLoadMode.Additive);
        var scene2 = await sceneManager.LoadSceneAsync("World2", SceneLoadMode.Additive);

        var entity1 = scene1.World!.Create(new PositionComponent(10, 20));
        var entity2 = scene2.World!.Create(new PositionComponent(30, 40));

        // Assert
        scene1.World.Should().NotBe(scene2.World, "scenes should have different worlds");
        scene1.World!.IsAlive(entity1).Should().BeTrue("entity1 should exist in world1");
        scene2.World!.IsAlive(entity2).Should().BeTrue("entity2 should exist in world2");

        // Verify isolation
        scene1.World.IsAlive(entity2).Should().BeFalse("entity2 should not exist in world1");
        scene2.World.IsAlive(entity1).Should().BeFalse("entity1 should not exist in world2");
    }
}
