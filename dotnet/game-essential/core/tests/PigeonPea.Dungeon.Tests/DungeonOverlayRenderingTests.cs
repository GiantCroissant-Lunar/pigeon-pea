using Arch.Core;
using FluentAssertions;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Overlays;
using PigeonPea.Plugin.Dungeon.Basic;
using PigeonPea.Plugin.Dungeon.Rendering;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Dungeon;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class DungeonOverlayRenderingTests
{
    [Fact]
    public void Generator_produces_door_metadata()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions(50, 50, Seed: 42);

        // Act
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        dungeon.FeatureMetadata.Should().NotBeNull();
        dungeon.FeatureMetadata.Should().ContainKey("doors");
        dungeon.DoorStates.Should().NotBeNull(); // Backward compatibility
    }

    [Fact]
    public void Overlay_source_extracts_doors_from_metadata()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions(50, 50, Seed: 42);
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        var overlaySource = new DungeonGridOverlaySource();

        // Act
        var overlays = overlaySource.GetOverlays(dungeon).ToList();

        // Assert
        overlays.Should().NotBeEmpty();
        var doorOverlays = overlays.Where(o => o.Kind == "door").ToList();
        doorOverlays.Should().NotBeEmpty("dungeon should have doors");

        foreach (var door in doorOverlays)
        {
            door.Should().NotBeNull();
            door.Position.Should().NotBeNull();
            door.Metadata.Should().ContainKey("state");
            door.Metadata.Should().ContainKey("orientation");
        }
    }

    [Fact]
    public void Renderer_can_render_with_overlays()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions(50, 50, Seed: 42);
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        var overlaySource = new DungeonGridOverlaySource();
        var overlays = overlaySource.GetOverlays(dungeon).ToList();

        var renderer = new DungeonRenderer();
        var mockPlatformRenderer = new MockPlatformRenderer(50, 50);
        renderer.Initialize(mockPlatformRenderer);

        // Act
        renderer.RenderWithOverlays(
            dungeon.Width,
            dungeon.Height,
            dungeon.Walkable,
            overlays,
            playerX: 25,
            playerY: 25,
            scale: 1
        );

        // Assert
        mockPlatformRenderer.FrameCount.Should().BeGreaterThan(0);
        mockPlatformRenderer.DrawCalls.Should().NotBeEmpty();
        mockPlatformRenderer.TilesDrawn.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Door_overlays_have_correct_properties()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions(50, 50, Seed: 42);
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        var overlaySource = new DungeonGridOverlaySource();
        var overlays = overlaySource.GetOverlays(dungeon).ToList();

        // Act
        var doorOverlays = overlays.Where(o => o.Kind == "door").ToList();

        // Assert
        foreach (var door in doorOverlays)
        {
            door.Position.X.Should().BeInRange(0, dungeon.Width - 1);
            door.Position.Y.Should().BeInRange(0, dungeon.Height - 1);

            door.Metadata["state"].Should().BeOfType<int>()
                .Which.Should().BeInRange(1, 4); // Closed, Open, Locked, Broken

            var orientation = door.Metadata["orientation"];
            orientation.Should().BeOfType<string>();
            orientation.ToString().Should().BeOneOf("horizontal", "vertical");
        }
    }

    [Fact]
    public void Full_integration_generator_to_renderer()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions(80, 40, Seed: 123);

        // Act - Generate dungeon
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        // Act - Extract overlays
        var overlaySource = new DungeonGridOverlaySource();
        var overlays = overlaySource.GetOverlays(dungeon).ToList();

        // Act - Render
        var renderer = new DungeonRenderer();
        var mockPlatformRenderer = new MockPlatformRenderer(80, 40);
        renderer.Initialize(mockPlatformRenderer);

        renderer.RenderWithOverlays(
            dungeon.Width,
            dungeon.Height,
            dungeon.Walkable,
            overlays,
            playerX: 40,
            playerY: 20,
            scale: 2 // Higher scale to test LOD
        );

        // Assert
        mockPlatformRenderer.FrameCount.Should().Be(1);
        mockPlatformRenderer.DrawCalls.Should().NotBeEmpty();

        // Verify door tiles were rendered
        var doorDrawCalls = mockPlatformRenderer.DrawCalls
            .Where(c => c.Glyph == '+' || c.Glyph == '/')
            .ToList();
        doorDrawCalls.Should().NotBeEmpty("doors should be rendered");

        // Verify player was rendered
        var playerDrawCalls = mockPlatformRenderer.DrawCalls
            .Where(c => c.Glyph == '@' && c.X == 40 && c.Y == 20)
            .ToList();
        playerDrawCalls.Should().NotBeEmpty("player should be rendered");
    }

    [Fact]
    public void Legacy_render_method_still_works()
    {
        // Arrange
        var dungeonView = new DungeonView
        {
            Width = 10,
            Height = 10,
            Walkable = new bool[10, 10],
            Opaque = new bool[10, 10],
            Doors = new byte[10, 10]
        };

        // Make a simple room
        for (int y = 2; y < 8; y++)
        {
            for (int x = 2; x < 8; x++)
            {
                dungeonView.Walkable[y, x] = true;
                dungeonView.Opaque[y, x] = false;
            }
        }

        // Add a door
        dungeonView.Doors[5, 5] = 1; // Closed door

        var renderer = new DungeonRenderer();
        var mockPlatformRenderer = new MockPlatformRenderer(10, 10);
        renderer.Initialize(mockPlatformRenderer);

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        renderer.Render(dungeonView, playerX: 5, playerY: 5);
#pragma warning restore CS0618

        // Assert
        mockPlatformRenderer.FrameCount.Should().Be(1);
        mockPlatformRenderer.DrawCalls.Should().NotBeEmpty();
    }
}

/// <summary>
/// Mock platform renderer for testing
/// </summary>
public class MockPlatformRenderer : IRenderer
{
    public int Width { get; }
    public int Height { get; }
    public int FrameCount { get; private set; }
    public int TilesDrawn => DrawCalls.Count;
    public List<DrawCall> DrawCalls { get; } = new();

    public MockPlatformRenderer(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Initialize(IRenderTarget target) { }

    public void BeginFrame()
    {
        DrawCalls.Clear();
    }

    public void EndFrame()
    {
        FrameCount++;
    }

    public void Clear(Color color)
    {
        // No-op for testing
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        DrawCalls.Add(new DrawCall(x, y, tile.Glyph, tile.Foreground, tile.Background));
    }

    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        for (int i = 0; i < text.Length; i++)
        {
            DrawCalls.Add(new DrawCall(x + i, y, text[i], foreground, background));
        }
    }

    public void Shutdown() { }
}

public record DrawCall(int X, int Y, char Glyph, Color Foreground, Color Background);
