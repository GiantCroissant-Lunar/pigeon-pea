using System;
using System.Collections.Generic;
using System.Text.Json;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Overlays;
using PigeonPea.Shared.Components;

namespace PigeonPea.Shared.Dungeon;

/// <summary>
/// Extracts overlay features from a dungeon map component.
/// Implements the same IOverlaySource pattern used for world maps.
/// </summary>
public sealed class DungeonGridOverlaySource : IOverlaySource<DungeonMapComponent, GridPosition>
{
    public IEnumerable<IOverlayFeature<GridPosition>> GetOverlays(DungeonMapComponent dungeon)
    {
        foreach (var doorFeature in ExtractDoors(dungeon))
        {
            yield return doorFeature;
        }

        foreach (var trapFeature in ExtractTraps(dungeon))
        {
            yield return trapFeature;
        }

        foreach (var spawnFeature in ExtractSpawnPoints(dungeon))
        {
            yield return spawnFeature;
        }

        foreach (var treasureFeature in ExtractTreasure(dungeon))
        {
            yield return treasureFeature;
        }

        foreach (var stairFeature in ExtractStairs(dungeon))
        {
            yield return stairFeature;
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractDoors(DungeonMapComponent dungeon)
    {
        // Option 1: Extract from legacy DoorStates array (backward compatibility)
        if (dungeon.DoorStates != null && dungeon.DoorStates.Length > 0)
        {
            for (int i = 0; i < dungeon.DoorStates.Length; i++)
            {
                if (dungeon.DoorStates[i] > 0)
                {
                    var (x, y) = IndexToPosition(i, dungeon.Width);
                    var doorState = (DoorState)dungeon.DoorStates[i];

                    yield return new DungeonOverlayFeature(
                        LayerId: "dungeon.doors",
                        Position: new GridPosition(x, y),
                        Kind: GetDoorKind(doorState),
                        Name: $"Door at ({x},{y})",
                        Metadata: new Dictionary<string, object?>
                        {
                            ["state"] = doorState,
                            ["locked"] = doorState == DoorState.Locked,
                            ["orientation"] = DetectDoorOrientation(dungeon, x, y)
                        }
                    );
                }
            }
        }

        // Option 2: Extract from new metadata (preferred)
        if (dungeon.FeatureMetadata != null && 
            dungeon.FeatureMetadata.TryGetValue("doors", out var doorsData))
        {
            var doors = TryDeserialize<DoorMetadata[]>(doorsData);
            if (doors != null)
            {
                foreach (var door in doors)
                {
                    yield return new DungeonOverlayFeature(
                        LayerId: "dungeon.doors",
                        Position: new GridPosition(door.X, door.Y),
                        Kind: GetDoorKind(door.State),
                        Name: $"Door at ({door.X},{door.Y})",
                        Metadata: new Dictionary<string, object?>
                        {
                            ["state"] = door.State,
                            ["locked"] = door.Locked,
                            ["orientation"] = door.Orientation
                        }
                    );
                }
            }
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractTraps(DungeonMapComponent dungeon)
    {
        if (dungeon.FeatureMetadata == null || 
            !dungeon.FeatureMetadata.TryGetValue("traps", out var trapsData))
        {
            yield break;
        }

        var traps = TryDeserialize<TrapMetadata[]>(trapsData);
        if (traps == null) yield break;

        foreach (var trap in traps)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.traps",
                Position: new GridPosition(trap.X, trap.Y),
                Kind: trap.Type,
                Name: $"{trap.Type} trap",
                Metadata: new Dictionary<string, object?>
                {
                    ["damage"] = trap.Damage,
                    ["radius"] = trap.Radius,
                    ["discovered"] = trap.Discovered,
                    ["triggered"] = trap.Triggered
                }
            );
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractSpawnPoints(DungeonMapComponent dungeon)
    {
        if (dungeon.FeatureMetadata == null || 
            !dungeon.FeatureMetadata.TryGetValue("spawn_points", out var spawnsData))
        {
            yield break;
        }

        var spawns = TryDeserialize<SpawnPointMetadata[]>(spawnsData);
        if (spawns == null) yield break;

        foreach (var spawn in spawns)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.spawn_points",
                Position: new GridPosition(spawn.X, spawn.Y),
                Kind: spawn.SpawnType,
                Name: $"{spawn.SpawnType} spawn",
                Metadata: new Dictionary<string, object?>
                {
                    ["monster_id"] = spawn.MonsterId,
                    ["level"] = spawn.Level,
                    ["is_boss"] = spawn.IsBoss
                }
            );
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractTreasure(DungeonMapComponent dungeon)
    {
        if (dungeon.FeatureMetadata == null || 
            !dungeon.FeatureMetadata.TryGetValue("treasure", out var treasureData))
        {
            yield break;
        }

        var treasures = TryDeserialize<TreasureMetadata[]>(treasureData);
        if (treasures == null) yield break;

        foreach (var treasure in treasures)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.treasure",
                Position: new GridPosition(treasure.X, treasure.Y),
                Kind: treasure.ContainerType,
                Name: treasure.ContainerType,
                Metadata: new Dictionary<string, object?>
                {
                    ["items"] = treasure.Items,
                    ["gold"] = treasure.Gold,
                    ["opened"] = treasure.Opened,
                    ["locked"] = treasure.Locked,
                    ["trap_type"] = treasure.TrapType
                }
            );
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractStairs(DungeonMapComponent dungeon)
    {
        if (dungeon.FeatureMetadata == null || 
            !dungeon.FeatureMetadata.TryGetValue("stairs", out var stairsData))
        {
            yield break;
        }

        var stairs = TryDeserialize<StairMetadata[]>(stairsData);
        if (stairs == null) yield break;

        foreach (var stair in stairs)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.stairs",
                Position: new GridPosition(stair.X, stair.Y),
                Kind: stair.Direction,
                Name: $"Stairs {stair.Direction}",
                Metadata: new Dictionary<string, object?>
                {
                    ["destination_level"] = stair.DestinationLevel,
                    ["destination_x"] = stair.DestinationX,
                    ["destination_y"] = stair.DestinationY
                }
            );
        }
    }

    private static T? TryDeserialize<T>(object data)
    {
        try
        {
            if (data is string json)
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            else if (data is JsonElement jsonElement)
            {
                return jsonElement.Deserialize<T>();
            }
        }
        catch
        {
            // Swallow deserialization errors
        }

        return default;
    }

    private static (int x, int y) IndexToPosition(int index, int width)
    {
        return (index % width, index / width);
    }

    private static string GetDoorKind(DoorState state)
    {
        return state switch
        {
            DoorState.Open => "door_open",
            DoorState.Closed => "door_closed",
            DoorState.Locked => "door_locked",
            DoorState.Broken => "door_broken",
            _ => "door"
        };
    }

    private static string DetectDoorOrientation(DungeonMapComponent dungeon, int x, int y)
    {
        // Check adjacent tiles to determine if door is horizontal or vertical
        // Simplified: check if walls are on left/right or top/bottom
        
        bool hasLeftWall = x > 0 && !dungeon.Walkable[y * dungeon.Width + (x - 1)];
        bool hasRightWall = x < dungeon.Width - 1 && !dungeon.Walkable[y * dungeon.Width + (x + 1)];
        bool hasTopWall = y > 0 && !dungeon.Walkable[(y - 1) * dungeon.Width + x];
        bool hasBottomWall = y < dungeon.Height - 1 && !dungeon.Walkable[(y + 1) * dungeon.Width + x];

        if (hasLeftWall && hasRightWall)
        {
            return "vertical";
        }
        else if (hasTopWall && hasBottomWall)
        {
            return "horizontal";
        }

        return "horizontal"; // Default
    }
}
