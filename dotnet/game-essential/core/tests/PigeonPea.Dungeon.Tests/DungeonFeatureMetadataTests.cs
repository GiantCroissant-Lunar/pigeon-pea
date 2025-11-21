using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Arch.Core;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Plugin.Dungeon.Basic;
using PigeonPea.Plugin.Dungeon.ModernEdgar;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Dungeon;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class DungeonFeatureMetadataTests
{
    [Fact]
    public void BasicDungeonGenerator_Generates_All_Feature_Metadata()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        
        // Act
        var dungeonEntity = generator.Generate(world, new DungeonGenerationOptions
        {
            Width = 128,
            Height = 96,
            Seed = 42
        });

        // Assert
        var dungeonMap = world.Get<DungeonMapComponent>(dungeonEntity);
        
        Assert.NotNull(dungeonMap.FeatureMetadata);
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("doors"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("traps"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("treasure"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("spawn_points"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("stairs"));
        
        // Verify doors
        var doors = JsonSerializer.Deserialize<DoorMetadata[]>(
            dungeonMap.FeatureMetadata["doors"].ToString()!);
        Assert.NotNull(doors);
        Assert.NotEmpty(doors);
        
        // Verify traps
        var traps = JsonSerializer.Deserialize<TrapMetadata[]>(
            dungeonMap.FeatureMetadata["traps"].ToString()!);
        Assert.NotNull(traps);
        Assert.NotEmpty(traps);
        Assert.All(traps, trap =>
        {
            Assert.True(trap.X >= 0 && trap.X < dungeonMap.Width);
            Assert.True(trap.Y >= 0 && trap.Y < dungeonMap.Height);
            Assert.NotEmpty(trap.Type);
        });
        
        // Verify treasure
        var treasure = JsonSerializer.Deserialize<TreasureMetadata[]>(
            dungeonMap.FeatureMetadata["treasure"].ToString()!);
        Assert.NotNull(treasure);
        Assert.NotEmpty(treasure);
        Assert.All(treasure, t =>
        {
            Assert.True(t.X >= 0 && t.X < dungeonMap.Width);
            Assert.True(t.Y >= 0 && t.Y < dungeonMap.Height);
            Assert.NotEmpty(t.ContainerType);
        });
        
        // Verify spawn points
        var spawns = JsonSerializer.Deserialize<SpawnPointMetadata[]>(
            dungeonMap.FeatureMetadata["spawn_points"].ToString()!);
        Assert.NotNull(spawns);
        Assert.NotEmpty(spawns);
        Assert.All(spawns, spawn =>
        {
            Assert.True(spawn.X >= 0 && spawn.X < dungeonMap.Width);
            Assert.True(spawn.Y >= 0 && spawn.Y < dungeonMap.Height);
            Assert.NotNull(spawn.MonsterId);
        });
        
        // Verify stairs
        var stairs = JsonSerializer.Deserialize<StairMetadata[]>(
            dungeonMap.FeatureMetadata["stairs"].ToString()!);
        Assert.NotNull(stairs);
        Assert.NotEmpty(stairs);
        Assert.Contains(stairs, s => s.Direction == "up");
        Assert.Contains(stairs, s => s.Direction == "down");
    }

    [Fact]
    public void DungeonGridOverlaySource_Extracts_All_Features()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var dungeonEntity = generator.Generate(world, new DungeonGenerationOptions
        {
            Width = 128,
            Height = 96,
            Seed = 42
        });
        
        var dungeonMap = world.Get<DungeonMapComponent>(dungeonEntity);
        var overlaySource = new DungeonGridOverlaySource();
        
        // Act
        var overlays = overlaySource.GetOverlays(dungeonMap).ToList();
        
        // Assert
        Assert.NotEmpty(overlays);
        
        var doorOverlays = overlays.Where(o => o.LayerId == "dungeon.doors").ToList();
        var trapOverlays = overlays.Where(o => o.LayerId == "dungeon.traps").ToList();
        var treasureOverlays = overlays.Where(o => o.LayerId == "dungeon.treasure").ToList();
        var spawnOverlays = overlays.Where(o => o.LayerId == "dungeon.spawn_points").ToList();
        var stairOverlays = overlays.Where(o => o.LayerId == "dungeon.stairs").ToList();
        
        Assert.NotEmpty(doorOverlays);
        Assert.NotEmpty(trapOverlays);
        Assert.NotEmpty(treasureOverlays);
        Assert.NotEmpty(spawnOverlays);
        Assert.NotEmpty(stairOverlays);
        
        // Verify trap overlays have proper metadata
        Assert.All(trapOverlays, overlay =>
        {
            Assert.True(overlay.Metadata.ContainsKey("damage"));
            Assert.True(overlay.Metadata.ContainsKey("radius"));
            Assert.True(overlay.Metadata.ContainsKey("discovered"));
        });
        
        // Verify treasure overlays have proper metadata
        Assert.All(treasureOverlays, overlay =>
        {
            Assert.True(overlay.Metadata.ContainsKey("items"));
            Assert.True(overlay.Metadata.ContainsKey("gold"));
            Assert.True(overlay.Metadata.ContainsKey("opened"));
        });
    }

    [Fact]
    public void ModernEdgarGenerator_Generates_All_Feature_Metadata()
    {
        // Arrange
        var world = World.Create();
        var generator = new ModernEdgarDungeonGenerator();
        
        // Act
        var dungeonEntity = generator.Generate(world, new DungeonGenerationOptions
        {
            Width = 120,
            Height = 90,
            Seed = 42
        });

        // Assert
        var dungeonMap = world.Get<DungeonMapComponent>(dungeonEntity);
        
        Assert.NotNull(dungeonMap.FeatureMetadata);
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("doors"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("traps"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("treasure"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("spawn_points"));
        Assert.True(dungeonMap.FeatureMetadata.ContainsKey("stairs"));
    }
}
