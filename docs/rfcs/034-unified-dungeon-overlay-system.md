---
canonical: true
created: '2025-11-21'
dependencies:
  external: []
  rfcs:
  - RFC-00014
doc_id: RFC-00034
doc_type: rfc
implementation:
  completion: 0
  issues: []
  status: not-started
  tasks: []
related:
- RFC-00032
- RFC-00033
- RFC-00014
status: draft
summary: Extend the overlay abstraction (IOverlaySource) to dungeons, enabling doors,
  traps, spawn points, and treasure to be represented as overlay features instead
  of flat arrays
supersedes: []
tags:
- overlay
- dungeon
- architecture
- rendering
- ecs
title: Unified Dungeon Overlay System
updated: '2025-11-21'
---


# RFC-034: Unified Dungeon Overlay System

- **Status:** Draft
- **Author:** Claude Agent (Architecture Design)
- **Date:** 2025-11-21
- **Dependencies:** RFC-014 (Scene Management)
- **Related:** RFC-032 (Multi-Backend Rendering), RFC-033 (Scale Config System)

## Summary

Extend the existing `IOverlaySource<TContext, TPosition>` abstraction to dungeons, enabling dungeon features (doors, traps, spawn points, treasure, stairs, etc.) to be represented as overlay features instead of flat arrays in `DungeonMapComponent`. This unifies the overlay pattern across world map and dungeon domains, improves extensibility, and enables feature-rich dungeon rendering.

## Motivation

### Current Problems

1. **Inconsistent Overlay Architecture**
   - **World map:** Uses `IOverlaySource<MapData, WorldPosition>` with clean overlay abstraction
   - **Dungeon:** Uses flat arrays (`DoorStates`, `Walkable`, `Opaque`) - no overlay extraction
   - No unified pattern

2. **Limited Dungeon Features**
   - Current `DungeonMapComponent` only tracks:
     - `DoorStates` (byte array)
     - `Walkable` (BitArray)
     - `Opaque` (BitArray)
   - No support for:
     - Spawn points (player, monsters, bosses)
     - Treasure chests
     - Traps (pressure plates, spike traps, etc.)
     - Stairs (up/down)
     - Special tiles (altars, fountains, etc.)

3. **Extensibility Issues**
   - Adding new dungeon features requires:
     - Modifying `DungeonMapComponent` structure
     - Updating generator code
     - Updating renderer code
   - No plugin-based extensibility

4. **Rendering Coupling**
   - `DungeonRenderer` directly accesses `DoorStates` array
   - Cannot render feature metadata (e.g., door locked state, trap damage type)
   - Cannot filter/hide features based on player knowledge

### Goals

1. **Unified Overlay Pattern**
   - Extend `IOverlaySource` to dungeons
   - Same abstraction for world and dungeon overlays
   - Consistent architecture across all domains

2. **Rich Dungeon Features**
   - Support doors, traps, spawn points, treasure, stairs
   - Extensible metadata per feature type
   - Plugin-based feature additions

3. **Decoupled Rendering**
   - Renderers query overlays, not raw arrays
   - Overlay visibility rules (e.g., hide traps until discovered)
   - LOD for dungeon features (hide minor details at dungeon-coarse scale)

4. **ECS Integration**
   - Optional: Doors/traps as ECS entities (fully dynamic)
   - Or: Doors/traps as overlay features (lightweight, static)
   - Hybrid approach: Static features via overlays, dynamic features via entities

## Architecture Overview

### Dungeon Overlay System

```
┌─────────────────────────────────────────────────────────┐
│ DungeonMapComponent (ECS Entity)                        │
├─────────────────────────────────────────────────────────┤
│ - Width, Height                                         │
│ - TileData (byte array) - tile types                    │
│ - Walkable (BitArray) - walkable flags                  │
│ - Opaque (BitArray) - blocks vision flags               │
│ - Metadata (optional dictionaries)                      │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↓ Passed to
┌─────────────────────────────────────────────────────────┐
│ DungeonGridOverlaySource                                │
│ : IOverlaySource<DungeonMapComponent, GridPosition>    │
├─────────────────────────────────────────────────────────┤
│ GetOverlays(DungeonMapComponent dungeon)                │
│ → IEnumerable<IOverlayFeature<GridPosition>>           │
│                                                          │
│ Extracts:                                               │
│ - dungeon.doors (from DoorStates or metadata)           │
│ - dungeon.traps (from metadata)                         │
│ - dungeon.spawn_points (from metadata)                  │
│ - dungeon.treasure (from metadata)                      │
│ - dungeon.stairs (from metadata)                        │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↓ Consumed by
┌─────────────────────────────────────────────────────────┐
│ DungeonDomainRenderer                                   │
│ - Renders tiles from DungeonMapComponent                │
│ - Renders overlay features on top                       │
│ - Respects visibility rules (discovered vs hidden)      │
└─────────────────────────────────────────────────────────┘
```

### Overlay Feature Types

```
Dungeon Overlay Layers:
├─ dungeon.doors          → Door features (open, closed, locked)
├─ dungeon.traps          → Trap features (spike, poison, fire, etc.)
├─ dungeon.spawn_points   → Spawn features (player, monster, boss)
├─ dungeon.treasure       → Treasure features (chest, pile, item)
├─ dungeon.stairs         → Stair features (up, down)
├─ dungeon.special_tiles  → Special features (altar, fountain, portal)
└─ dungeon.annotations    → Debug/editor annotations (room labels, etc.)
```

## Core Contracts

### DungeonGridOverlaySource

```csharp
// dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/DungeonGridOverlaySource.cs

namespace PigeonPea.Dungeon.Core;

/// <summary>
/// Extracts overlay features from a dungeon map component.
/// Implements the same IOverlaySource pattern used for world maps.
/// </summary>
public class DungeonGridOverlaySource : IOverlaySource<DungeonMapComponent, GridPosition>
{
    public IEnumerable<IOverlayFeature<GridPosition>> GetOverlays(DungeonMapComponent dungeon)
    {
        // Extract doors
        foreach (var doorFeature in ExtractDoors(dungeon))
        {
            yield return doorFeature;
        }

        // Extract traps
        foreach (var trapFeature in ExtractTraps(dungeon))
        {
            yield return trapFeature;
        }

        // Extract spawn points
        foreach (var spawnFeature in ExtractSpawnPoints(dungeon))
        {
            yield return spawnFeature;
        }

        // Extract treasure
        foreach (var treasureFeature in ExtractTreasure(dungeon))
        {
            yield return treasureFeature;
        }

        // Extract stairs
        foreach (var stairFeature in ExtractStairs(dungeon))
        {
            yield return stairFeature;
        }

        // Extract special tiles
        foreach (var specialFeature in ExtractSpecialTiles(dungeon))
        {
            yield return specialFeature;
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractDoors(DungeonMapComponent dungeon)
    {
        // Option 1: Extract from existing DoorStates array (backward compatibility)
        if (dungeon.DoorStates != null)
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

        // Option 2: Extract from new metadata (future)
        if (dungeon.FeatureMetadata != null && dungeon.FeatureMetadata.TryGetValue("doors", out var doorsData))
        {
            // Parse doors from metadata
            // ...
        }
    }

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractTraps(DungeonMapComponent dungeon)
    {
        if (dungeon.FeatureMetadata == null || !dungeon.FeatureMetadata.TryGetValue("traps", out var trapsData))
        {
            yield break;
        }

        // Parse traps from metadata
        // Example metadata format:
        // "traps": [
        //   {"x": 10, "y": 15, "type": "spike", "damage": 5, "discovered": false},
        //   {"x": 20, "y": 25, "type": "poison_gas", "damage": 3, "radius": 2}
        // ]

        var traps = JsonSerializer.Deserialize<List<TrapMetadata>>(trapsData.ToString()!);
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
        if (dungeon.FeatureMetadata == null || !dungeon.FeatureMetadata.TryGetValue("spawn_points", out var spawnsData))
        {
            yield break;
        }

        var spawns = JsonSerializer.Deserialize<List<SpawnPointMetadata>>(spawnsData.ToString()!);
        if (spawns == null) yield break;

        foreach (var spawn in spawns)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.spawn_points",
                Position: new GridPosition(spawn.X, spawn.Y),
                Kind: spawn.SpawnType, // "player", "monster", "boss"
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
        if (dungeon.FeatureMetadata == null || !dungeon.FeatureMetadata.TryGetValue("treasure", out var treasureData))
        {
            yield break;
        }

        var treasures = JsonSerializer.Deserialize<List<TreasureMetadata>>(treasureData.ToString()!);
        if (treasures == null) yield break;

        foreach (var treasure in treasures)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.treasure",
                Position: new GridPosition(treasure.X, treasure.Y),
                Kind: treasure.ContainerType, // "chest", "pile", "item"
                Name: $"{treasure.ContainerType}",
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
        if (dungeon.FeatureMetadata == null || !dungeon.FeatureMetadata.TryGetValue("stairs", out var stairsData))
        {
            yield break;
        }

        var stairs = JsonSerializer.Deserialize<List<StairMetadata>>(stairsData.ToString()!);
        if (stairs == null) yield break;

        foreach (var stair in stairs)
        {
            yield return new DungeonOverlayFeature(
                LayerId: "dungeon.stairs",
                Position: new GridPosition(stair.X, stair.Y),
                Kind: stair.Direction, // "up", "down"
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

    private IEnumerable<IOverlayFeature<GridPosition>> ExtractSpecialTiles(DungeonMapComponent dungeon)
    {
        // Implement extraction of altars, fountains, portals, etc.
        yield break;
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
        // Return "horizontal" or "vertical"
        return "horizontal"; // Simplified for example
    }
}

public enum DoorState : byte
{
    Closed = 1,
    Open = 2,
    Locked = 3,
    Broken = 4
}
```

### Updated DungeonMapComponent

```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared/Components.cs

public struct DungeonMapComponent
{
    public int Width;
    public int Height;

    // Core tile data
    public byte[] TileData;           // Tile types (floor, wall, etc.)
    public BitArray Walkable;         // Walkable flags
    public BitArray Opaque;           // Vision blocking flags

    // Legacy door data (backward compatibility)
    public byte[]? DoorStates;        // Optional, deprecated in favor of FeatureMetadata

    // NEW: Feature metadata (for overlays)
    public Dictionary<string, object>? FeatureMetadata;
    // Example:
    // {
    //   "doors": [...],
    //   "traps": [...],
    //   "spawn_points": [...],
    //   "treasure": [...],
    //   "stairs": [...]
    // }
}
```

### Metadata Models

```csharp
// dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/FeatureMetadata.cs

namespace PigeonPea.Dungeon.Core;

public record TrapMetadata(
    int X,
    int Y,
    string Type,           // "spike", "poison_gas", "fire", "arrow", etc.
    int Damage,
    int Radius,
    bool Discovered,
    bool Triggered
);

public record SpawnPointMetadata(
    int X,
    int Y,
    string SpawnType,      // "player", "monster", "boss"
    string? MonsterId,     // Optional monster ID
    int Level,
    bool IsBoss
);

public record TreasureMetadata(
    int X,
    int Y,
    string ContainerType,  // "chest", "pile", "item"
    List<string> Items,
    int Gold,
    bool Opened,
    bool Locked,
    string? TrapType       // Optional trap on chest
);

public record StairMetadata(
    int X,
    int Y,
    string Direction,      // "up", "down"
    int DestinationLevel,
    int DestinationX,
    int DestinationY
);
```

## Integration with Dungeon Generators

### ModernEdgar Generator Update

```csharp
// dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs

public Entity Generate(World world, DungeonGenerationOptions options)
{
    // Generate dungeon layout with Edgar
    var layout = _edgarGenerator.Generate(options);

    // Extract features from Edgar layout
    var doors = ExtractDoors(layout);
    var spawns = ExtractSpawnPoints(layout);
    var treasure = ExtractTreasure(layout);
    var stairs = ExtractStairs(layout);

    // Create feature metadata
    var featureMetadata = new Dictionary<string, object>
    {
        ["doors"] = JsonSerializer.Serialize(doors),
        ["spawn_points"] = JsonSerializer.Serialize(spawns),
        ["treasure"] = JsonSerializer.Serialize(treasure),
        ["stairs"] = JsonSerializer.Serialize(stairs)
    };

    // Create dungeon entity
    var dungeonEntity = world.Create(
        new DungeonMapComponent
        {
            Width = layout.Width,
            Height = layout.Height,
            TileData = layout.TileData,
            Walkable = layout.Walkable,
            Opaque = layout.Opaque,
            DoorStates = null, // Deprecated, use FeatureMetadata instead
            FeatureMetadata = featureMetadata
        },
        new PositionComponent { X = 0, Y = 0 },
        new RenderableComponent { Glyph = ' ', Foreground = Color.White, Background = Color.Black }
    );

    return dungeonEntity;
}

private List<TrapMetadata> ExtractTraps(EdgarLayout layout)
{
    // Edgar may not natively support traps, add them procedurally
    var traps = new List<TrapMetadata>();
    var rng = new Random(options.Seed);

    for (int i = 0; i < layout.Rooms.Count * 2; i++)
    {
        var room = layout.Rooms[rng.Next(layout.Rooms.Count)];
        var x = rng.Next(room.Bounds.Left, room.Bounds.Right);
        var y = rng.Next(room.Bounds.Top, room.Bounds.Bottom);

        if (layout.Walkable[y * layout.Width + x])
        {
            traps.Add(new TrapMetadata(
                X: x,
                Y: y,
                Type: PickRandomTrapType(rng),
                Damage: rng.Next(3, 10),
                Radius: rng.Next(1, 3),
                Discovered: false,
                Triggered: false
            ));
        }
    }

    return traps;
}
```

## Rendering Integration

### DungeonDomainRenderer Update

```csharp
// dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Rendering/DungeonDomainRenderer.cs

public class DungeonDomainRenderer : IDomainRenderer
{
    private readonly DungeonGridOverlaySource _overlaySource = new();

    public void Render(World world, IRenderCommandList commands, RenderOptions options)
    {
        commands.BeginFrame();
        commands.Clear(Color.Black);

        var query = new QueryDescription().WithAll<DungeonMapComponent>();
        world.Query(in query, (ref DungeonMapComponent dungeon) =>
        {
            // Render base tiles
            RenderBaseTiles(dungeon, commands);

            // Render overlays
            if (options.ShowOverlays)
            {
                RenderOverlays(dungeon, commands, options);
            }
        });

        // Render player
        RenderPlayer(world, commands);

        commands.EndFrame();
    }

    private void RenderOverlays(DungeonMapComponent dungeon, IRenderCommandList commands, RenderOptions options)
    {
        var overlays = _overlaySource.GetOverlays(dungeon);

        foreach (var overlay in overlays)
        {
            // Check visibility rules
            if (!IsOverlayVisible(overlay, options))
            {
                continue;
            }

            // Render overlay based on layer and kind
            RenderOverlayFeature(overlay, commands);
        }
    }

    private bool IsOverlayVisible(IOverlayFeature<GridPosition> overlay, RenderOptions options)
    {
        // Example: Hide undiscovered traps
        if (overlay.LayerId == "dungeon.traps" && overlay.Metadata.TryGetValue("discovered", out var discovered))
        {
            if (discovered is bool discoveredFlag && !discoveredFlag)
            {
                return false; // Don't render hidden traps
            }
        }

        // Example: Hide spawn points in production (show in debug mode)
        if (overlay.LayerId == "dungeon.spawn_points" && !options.ShowDebugInfo)
        {
            return false;
        }

        // Example: Scale-based LOD (hide minor features at dungeon-coarse scale)
        if (options.ActiveScale?.Id == "dungeon-coarse")
        {
            // Hide treasure piles (show only chests)
            if (overlay.LayerId == "dungeon.treasure" && overlay.Kind == "pile")
            {
                return false;
            }
        }

        return true;
    }

    private void RenderOverlayFeature(IOverlayFeature<GridPosition> overlay, IRenderCommandList commands)
    {
        var tile = overlay.LayerId switch
        {
            "dungeon.doors" => GetDoorTile(overlay),
            "dungeon.traps" => GetTrapTile(overlay),
            "dungeon.spawn_points" => GetSpawnPointTile(overlay),
            "dungeon.treasure" => GetTreasureTile(overlay),
            "dungeon.stairs" => GetStairTile(overlay),
            _ => new Tile('?', Color.Magenta, Color.Black)
        };

        commands.DrawTile(overlay.Position.X, overlay.Position.Y, tile);
    }

    private static Tile GetDoorTile(IOverlayFeature<GridPosition> overlay)
    {
        return overlay.Kind switch
        {
            "door_open" => new Tile('/', Color.Brown, Color.Black),
            "door_closed" => new Tile('+', Color.Brown, Color.Black),
            "door_locked" => new Tile('+', Color.Yellow, Color.Black),
            "door_broken" => new Tile('/', Color.Gray, Color.Black),
            _ => new Tile('+', Color.Brown, Color.Black)
        };
    }

    private static Tile GetTrapTile(IOverlayFeature<GridPosition> overlay)
    {
        // Only show if discovered
        return new Tile('^', Color.Red, Color.Black);
    }

    private static Tile GetTreasureTile(IOverlayFeature<GridPosition> overlay)
    {
        return overlay.Kind switch
        {
            "chest" => new Tile('$', Color.Gold, Color.Black),
            "pile" => new Tile('*', Color.Yellow, Color.Black),
            "item" => new Tile('?', Color.Cyan, Color.Black),
            _ => new Tile('$', Color.Gold, Color.Black)
        };
    }

    private static Tile GetStairTile(IOverlayFeature<GridPosition> overlay)
    {
        return overlay.Kind switch
        {
            "up" => new Tile('<', Color.White, Color.Black),
            "down" => new Tile('>', Color.White, Color.Black),
            _ => new Tile('%', Color.White, Color.Black)
        };
    }

    private static Tile GetSpawnPointTile(IOverlayFeature<GridPosition> overlay)
    {
        // Debug rendering only
        return overlay.Kind switch
        {
            "player" => new Tile('P', Color.Green, Color.Black),
            "monster" => new Tile('M', Color.Red, Color.Black),
            "boss" => new Tile('B', Color.Purple, Color.Black),
            _ => new Tile('S', Color.Gray, Color.Black)
        };
    }
}
```

## Implementation Plan

### Phase 1: Core Overlay Contracts (Week 1)

**Files to Create:**
- `dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/DungeonGridOverlaySource.cs`
- `dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/FeatureMetadata.cs`

**Files to Update:**
- `dotnet/game-essential/core/src/PigeonPea.Shared/Components.cs` (add FeatureMetadata to DungeonMapComponent)

**Tasks:**
1. Implement `DungeonGridOverlaySource`
2. Define metadata models (TrapMetadata, SpawnPointMetadata, etc.)
3. Write unit tests for overlay extraction

### Phase 2: Generator Integration (Week 1-2)

**Files to Update:**
- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs`
- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGenerator.cs`

**Tasks:**
1. Update generators to populate FeatureMetadata
2. Maintain backward compatibility with DoorStates
3. Add procedural trap/treasure placement
4. Test dungeon generation with metadata

### Phase 3: Renderer Integration (Week 2)

**Files to Update:**
- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Rendering/DungeonDomainRenderer.cs`

**Tasks:**
1. Add overlay rendering to dungeon renderer
2. Implement visibility rules (discovered traps, debug spawn points)
3. Add scale-based LOD (hide minor features at coarse scale)
4. Test rendering with all overlay layers

### Phase 4: Testing & Polish (Week 2-3)

**Tasks:**
1. Unit tests for all feature types
2. Integration tests (generation → overlay extraction → rendering)
3. Visual tests (ensure overlays render correctly)
4. Performance tests (overlay extraction overhead)
5. Documentation updates

## Migration Strategy

### Backward Compatibility

Maintain backward compatibility with existing `DoorStates`:

```csharp
public IEnumerable<IOverlayFeature<GridPosition>> GetOverlays(DungeonMapComponent dungeon)
{
    // Option 1: Extract from legacy DoorStates (if present)
    if (dungeon.DoorStates != null)
    {
        foreach (var door in ExtractDoorsFromArray(dungeon.DoorStates, dungeon.Width))
        {
            yield return door;
        }
    }

    // Option 2: Extract from new FeatureMetadata (preferred)
    if (dungeon.FeatureMetadata != null)
    {
        foreach (var door in ExtractDoorsFromMetadata(dungeon.FeatureMetadata))
        {
            yield return door;
        }
    }

    // Extract other features (only from metadata)
    if (dungeon.FeatureMetadata != null)
    {
        foreach (var trap in ExtractTraps(dungeon.FeatureMetadata))
        {
            yield return trap;
        }
        // ...
    }
}
```

Deprecate `DoorStates` after migration complete.

## Benefits

1. **Unified Architecture**
   - Same overlay pattern for world and dungeon
   - Consistent extensibility across domains

2. **Rich Dungeon Features**
   - Support traps, treasure, spawn points, stairs
   - Extensible metadata per feature
   - Plugin-based feature additions

3. **Improved Rendering**
   - Visibility rules (hide undiscovered traps)
   - LOD for dungeon features (scale-aware)
   - Debug overlays (spawn points, room labels)

4. **Better Separation of Concerns**
   - Generators create features
   - Overlay source extracts features
   - Renderers render features
   - No direct coupling

5. **Future-Proof**
   - Easy to add new feature types
   - No structural changes to DungeonMapComponent
   - Metadata-driven extensibility

## Success Criteria

1. ✅ `DungeonGridOverlaySource` implemented and tested
2. ✅ All feature types supported (doors, traps, spawn points, treasure, stairs)
3. ✅ Generators populate FeatureMetadata
4. ✅ Renderer uses overlays for all features
5. ✅ Backward compatibility with DoorStates maintained
6. ✅ Visibility rules work (hidden traps, debug spawn points)
7. ✅ All unit and integration tests passing
8. ✅ Documentation updated

## References

- **RFC-032**: Multi-Backend Rendering Architecture
- **RFC-033**: Scale Config System Implementation
- **RFC-014**: Scene Management with ECS
- **FmgWorldOverlaySource**: Reference implementation for world overlays

## Appendix: Example Feature Metadata

### Dungeon with All Feature Types

```json
{
  "doors": [
    {"x": 10, "y": 5, "state": "closed", "locked": false, "orientation": "horizontal"},
    {"x": 20, "y": 15, "state": "locked", "locked": true, "orientation": "vertical"}
  ],
  "traps": [
    {"x": 12, "y": 8, "type": "spike", "damage": 5, "radius": 1, "discovered": false, "triggered": false},
    {"x": 25, "y": 20, "type": "poison_gas", "damage": 3, "radius": 3, "discovered": true, "triggered": false}
  ],
  "spawn_points": [
    {"x": 5, "y": 5, "spawn_type": "player", "monster_id": null, "level": 1, "is_boss": false},
    {"x": 40, "y": 40, "spawn_type": "boss", "monster_id": "dragon", "level": 10, "is_boss": true},
    {"x": 15, "y": 20, "spawn_type": "monster", "monster_id": "goblin", "level": 2, "is_boss": false}
  ],
  "treasure": [
    {"x": 30, "y": 25, "container_type": "chest", "items": ["sword", "potion"], "gold": 100, "opened": false, "locked": true, "trap_type": "poison_needle"},
    {"x": 18, "y": 12, "container_type": "pile", "items": [], "gold": 20, "opened": true, "locked": false, "trap_type": null}
  ],
  "stairs": [
    {"x": 2, "y": 2, "direction": "up", "destination_level": 0, "destination_x": 50, "destination_y": 50},
    {"x": 48, "y": 48, "direction": "down", "destination_level": 2, "destination_x": 5, "destination_y": 5}
  ]
}
```
