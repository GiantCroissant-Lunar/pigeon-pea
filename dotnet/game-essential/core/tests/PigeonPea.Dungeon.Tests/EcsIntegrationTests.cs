using System.Reflection;
using FluentAssertions;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Rendering;
using PigeonPea.Map.Control.WorldManager;
using PigeonPea.Map.Rendering;
using PigeonPea.Shared.ECS.Components;
using Xunit;
using CoreMapData = PigeonPea.Map.Core.MapData;
using FmgMapData = FantasyMapGenerator.Core.Models.MapData;

namespace PigeonPea.Dungeon.Tests;

public class EcsIntegrationTests
{
    [Fact]
    public void DungeonPipeline_RendersPlayerMonstersAndItems()
    {
        var dungeon = CreateOpenDungeon(20, 20);

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(5, 5, "Hero");
        var monster = manager.SpawnMonster(6, 5, "goblin", health: 30);
        var item = manager.SpawnItem(4, 5, "gold");

        var buffer = CreateAsciiBuffer(10, 10, '.');
        EntityRenderer.RenderEntitiesAscii(manager.World, buffer, viewportX: 0, viewportY: 0, fov: null);

        buffer[5, 5].Should().Be('@');
        buffer[5, 6].Should().Be('g');
        buffer[5, 4].Should().Be('$');

        manager.World.IsAlive(player).Should().BeTrue();
        manager.World.IsAlive(monster).Should().BeTrue();
        manager.World.IsAlive(item).Should().BeTrue();
        manager.IsPositionOccupied(5, 5).Should().BeTrue();
        manager.IsPositionOccupied(10, 10).Should().BeFalse();
    }

    [Fact]
    public void DungeonPipeline_PlayerMovementUpdatesRendering()
    {
        var dungeon = CreateOpenDungeon(15, 15);

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(2, 2, "Hero");

        manager.TryMoveEntity(player, 3, 2).Should().BeTrue();
        manager.GetPlayerPosition().Should().Be(new Position(3, 2));

        var buffer = CreateAsciiBuffer(8, 8, '.');
        EntityRenderer.RenderEntitiesAscii(manager.World, buffer, viewportX: 0, viewportY: 0, fov: null);

        buffer[2, 3].Should().Be('@');
        buffer[2, 2].Should().NotBe('@');
    }

    [Fact]
    public void MapPipeline_RendersCitiesAndDiscoveredMarkers()
    {
        var mapData = CreateStubMapData();

        using var manager = new MapWorldManager(mapData);
        manager.CreateCity(50, 60, "Test City", population: 12000, cultureId: "default");
        manager.CreateMarker(80, 90, "quest", "Quest Marker", discovered: true);

        const int viewportSize = 200;
        var rgba = CreateRgbaBuffer(viewportSize, viewportSize);

        MapEntityRenderer.RenderEntities(
            manager.World,
            rgba,
            viewportSize,
            viewportSize,
            viewportX: 0,
            viewportY: 0,
            viewportWidth: viewportSize,
            viewportHeight: viewportSize,
            zoom: 1.0);

        var cityIdx = GetPixelIndex(50, 60, viewportSize);
        rgba[cityIdx].Should().Be(255);
        rgba[cityIdx + 1].Should().Be(200);
        rgba[cityIdx + 2].Should().Be(100);
        rgba[cityIdx + 3].Should().Be(255);

        var markerIdx = GetPixelIndex(80, 90, viewportSize);
        rgba[markerIdx].Should().Be(255);
        rgba[markerIdx + 1].Should().Be(255);
        rgba[markerIdx + 2].Should().Be(0);
        rgba[markerIdx + 3].Should().Be(255);
    }

    [Fact]
    public void MapPipeline_HiddenMarkersAreNotRendered()
    {
        var mapData = CreateStubMapData();

        using var manager = new MapWorldManager(mapData);
        manager.CreateMarker(30, 40, "landmark", "Visible Marker", discovered: true);
        manager.CreateMarker(60, 40, "dungeon", "Hidden Marker", discovered: false);

        const int viewportSize = 200;
        var rgba = CreateRgbaBuffer(viewportSize, viewportSize);

        MapEntityRenderer.RenderEntities(
            manager.World,
            rgba,
            viewportSize,
            viewportSize,
            viewportX: 0,
            viewportY: 0,
            viewportWidth: viewportSize,
            viewportHeight: viewportSize,
            zoom: 1.0);

        var visibleIdx = GetPixelIndex(30, 40, viewportSize);
        rgba[visibleIdx + 3].Should().Be(255); // alpha set when rendered

        var hiddenIdx = GetPixelIndex(60, 40, viewportSize);
        rgba[hiddenIdx].Should().Be(0);
        rgba[hiddenIdx + 1].Should().Be(0);
        rgba[hiddenIdx + 2].Should().Be(0);
        rgba[hiddenIdx + 3].Should().Be(0);
    }

    private static DungeonData CreateOpenDungeon(int width, int height)
    {
        var dungeon = new DungeonData(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                dungeon.SetFloor(x, y);
            }
        }
        return dungeon;
    }

    private static char[,] CreateAsciiBuffer(int height, int width, char fill)
    {
        var buffer = new char[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[y, x] = fill;
            }
        }
        return buffer;
    }

    private static byte[] CreateRgbaBuffer(int width, int height) => new byte[width * height * 4];

    private static int GetPixelIndex(int x, int y, int width) => (y * width + x) * 4;

    private static CoreMapData CreateStubMapData(int width = 256, int height = 256)
    {
        var inner = new FmgMapData(width, height, Math.Max((width * height) / 16, 1));
        var ctor = typeof(CoreMapData).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(FmgMapData) },
            modifiers: null);

        return (CoreMapData)ctor!.Invoke(new object[] { inner });
    }
}
