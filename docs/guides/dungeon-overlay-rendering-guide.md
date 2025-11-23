# Dungeon Overlay Rendering Guide

**Date:** 2025-11-21
**Status:** Complete

## Overview

This guide explains how to use the new overlay-based dungeon rendering system introduced in RFC-034.

## Quick Start

### 1. Generate a Dungeon

```csharp
using Arch.Core;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Plugin.Dungeon.Basic;
using PigeonPea.Shared.Components;

// Create ECS world
var world = World.Create();

// Generate dungeon
var generator = new BasicDungeonGenerator();
var options = new DungeonGenerationOptions(width: 80, height: 40, Seed: 42);
var dungeonEntity = generator.Generate(world, options);

// Get dungeon component
var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);
```

### 2. Extract Overlay Features

```csharp
using PigeonPea.Overlays;
using PigeonPea.Shared.Dungeon;

// Create overlay source
var overlaySource = new DungeonGridOverlaySource();

// Extract all overlay features (doors, traps, treasure, etc.)
var overlays = overlaySource.GetOverlays(dungeon);
```

### 3. Render with Overlays

```csharp
using PigeonPea.Plugin.Dungeon.Rendering;
using PigeonPea.Rendering.Contracts;

// Create renderer
var renderer = new DungeonRenderer();
renderer.Initialize(platformRenderer); // Your platform-specific renderer

// Render dungeon with overlays
renderer.RenderWithOverlays(
    width: dungeon.Width,
    height: dungeon.Height,
    walkable: dungeon.Walkable,
    overlays: overlays,
    playerX: 40,
    playerY: 20,
    scale: 1  // 1 = normal, 2+ = zoomed in
);
```

## Feature Types

The overlay system supports the following feature types:

### Doors

**Kind:** `"door"`
**Metadata:**

- `state` (int): 1=Closed, 2=Open, 3=Locked, 4=Broken
- `orientation` (string): "horizontal" or "vertical"
- `locked` (bool): Whether door is locked

**Rendering:**

- `+` brown - Closed door
- `/` brown - Open door
- `+` red - Locked door
- `%` - Broken door

### Traps

**Kind:** `"trap"`
**Metadata:**

- `type` (string): Trap type (e.g., "spike", "poison")
- `damage` (int): Damage dealt
- `radius` (int): Effect radius
- `discovered` (bool): Whether player has discovered it
- `triggered` (bool): Whether trap has been triggered

**Rendering:**

- `^` red - Active trap (discovered or debug mode)
- `^` gray - Triggered trap
- Hidden - Undiscovered traps (unless debug mode)

**Visibility Rules:**

- Only shown if `discovered == true` OR debug mode enabled
- Hidden at scale < 2 (small zoom levels)

### Treasure

**Kind:** `"treasure"`
**Metadata:**

- `container_type` (string): "chest", "pile", "item"
- `items` (string[]): List of items
- `gold` (int): Gold amount
- `opened` (bool): Whether container is opened
- `locked` (bool): Whether container is locked
- `trap_type` (string?): Optional trap on container

**Rendering:**

- `∩` gold - Unopened treasure
- `∩` gray - Opened treasure

### Spawn Points

**Kind:** `"spawn_point"`
**Metadata:**

- `spawn_type` (string): "player", "monster", "boss"
- `monster_id` (string?): Monster type ID
- `level` (int): Monster level
- `is_boss` (bool): Whether this is a boss spawn

**Rendering:**

- `○` cyan - Regular spawn point
- `★` purple - Boss spawn point
- **Only visible in debug mode**

### Stairs

**Kind:** `"stairs"`
**Metadata:**

- `direction` (string): "up" or "down"
- `destination_level` (int): Target dungeon level
- `destination_x` (int): Target X coordinate
- `destination_y` (int): Target Y coordinate

**Rendering:**

- `<` white - Stairs up
- `>` white - Stairs down

## Scale-Aware LOD

The renderer implements level-of-detail based on scale:

```csharp
// Scale 1 (normal zoom)
renderer.RenderWithOverlays(..., scale: 1);
// Shows: doors, treasure, stairs
// Hides: undiscovered traps, spawn points

// Scale 2+ (zoomed in)
renderer.RenderWithOverlays(..., scale: 2);
// Shows: all discovered features
// Hides: only undiscovered traps, spawn points
```

## Debug Mode

Enable debug mode to see all features including:

- Undiscovered traps
- Spawn points
- Internal metadata

```csharp
// Note: Debug mode is currently internal to renderer
// Future: Add public SetDebugMode(bool) method
```

## Backward Compatibility

The legacy rendering method is still supported for existing code:

```csharp
// Legacy method (deprecated)
var dungeonView = new DungeonView { ... };
renderer.Render(dungeonView, playerX, playerY);
```

This method is marked as `[Obsolete]` and will emit compiler warnings. Migrate to `RenderWithOverlays` for full feature support.

## Generator Integration

Both `BasicDungeonGenerator` and `ModernEdgarDungeonGenerator` now populate the `FeatureMetadata` dictionary:

```csharp
var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

// Check for feature metadata
if (dungeon.FeatureMetadata != null)
{
    // Metadata is available
    var doorJson = dungeon.FeatureMetadata["doors"];
    // ... deserialize and use
}

// Legacy door states still available
if (dungeon.DoorStates != null)
{
    // Backward compatible access
}
```

## Architecture

The overlay system follows a clean separation of concerns:

```
┌─────────────────┐
│   Generators    │ ─── Produce ───> FeatureMetadata
└─────────────────┘                         │
                                           │
┌─────────────────┐                         ▼
│ OverlaySource   │ ─── Extract ───> IOverlayFeature<GridPosition>
└─────────────────┘                         │
                                           │
┌─────────────────┐                         ▼
│   Renderers     │ ─── Consume ───> Render to screen
└─────────────────┘
```

### Key Classes

- **`DungeonMapComponent`** - ECS component storing dungeon data
- **`DungeonGridOverlaySource`** - Extracts overlay features from dungeon
- **`DungeonOverlayFeature`** - Implementation of `IOverlayFeature<GridPosition>`
- **`DungeonRenderer`** - Renders dungeon using overlay system
- **`IDungeonRenderer`** - Interface with `RenderWithOverlays` method

## Extension Points

### Adding New Feature Types

1. **Define Metadata Model:**

```csharp
// In PigeonPea.Dungeon.Contracts/Models/FeatureMetadata.cs
public sealed record SecretDoorMetadata(
    int X,
    int Y,
    bool Discovered,
    string OpenMechanism);
```

2. **Populate in Generator:**

```csharp
var secretDoors = new List<SecretDoorMetadata>();
// ... add secret doors ...
featureMetadata["secret_doors"] = JsonSerializer.Serialize(secretDoors);
```

3. **Extract in Overlay Source:**

```csharp
// In DungeonGridOverlaySource.ExtractSecretDoors
if (TryDeserialize<SecretDoorMetadata[]>(dungeon, "secret_doors", out var secretDoors))
{
    foreach (var secret in secretDoors)
    {
        yield return new DungeonOverlayFeature(
            new GridPosition(secret.X, secret.Y),
            kind: "secret_door",
            name: "Secret Door",
            metadata: new Dictionary<string, object?>
            {
                ["discovered"] = secret.Discovered,
                ["mechanism"] = secret.OpenMechanism
            });
    }
}
```

4. **Render in Renderer:**

```csharp
// In DungeonRenderer.GetOverlayTile
"secret_door" => GetSecretDoorTile(overlay),

private static RenderTile GetSecretDoorTile(IOverlayFeature<GridPosition> overlay)
{
    var discovered = overlay.Metadata.TryGetValue("discovered", out var d) && d is bool db && db;
    return discovered
        ? new RenderTile('§', Color.DarkGray, Color.Black)
        : null; // Hidden until discovered
}
```

## Performance Considerations

- Overlay extraction is cached per dungeon component
- Feature rendering uses switch expressions for optimal performance
- Visibility checks are performed per-frame but are lightweight
- Scale-based LOD reduces rendering overhead at small zooms

## Testing

Example test demonstrating full integration:

```csharp
[Fact]
public void Full_integration_test()
{
    // Arrange
    var world = World.Create();
    var generator = new BasicDungeonGenerator();
    var options = new DungeonGenerationOptions(80, 40, Seed: 123);

    // Generate
    var dungeonEntity = generator.Generate(world, options);
    var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

    // Extract overlays
    var overlaySource = new DungeonGridOverlaySource();
    var overlays = overlaySource.GetOverlays(dungeon);

    // Render
    var renderer = new DungeonRenderer();
    var platformRenderer = new MockPlatformRenderer(80, 40);
    renderer.Initialize(platformRenderer);

    renderer.RenderWithOverlays(
        dungeon.Width,
        dungeon.Height,
        dungeon.Walkable,
        overlays,
        playerX: 40,
        playerY: 20,
        scale: 1
    );

    // Assert
    Assert.NotEmpty(platformRenderer.DrawCalls);
}
```

## Migration Checklist

Migrating from legacy to overlay-based rendering:

- [ ] Update generator to populate `FeatureMetadata`
- [ ] Replace `Render(DungeonView)` with `RenderWithOverlays`
- [ ] Extract overlays using `DungeonGridOverlaySource`
- [ ] Pass overlays to renderer
- [ ] Remove legacy `DoorStates` array usage
- [ ] Test rendering with different scales
- [ ] Verify feature visibility rules work correctly

## Future Enhancements

- Public debug mode API
- Custom feature renderers via plugins
- Dynamic overlay filtering
- Performance profiling tools
- Overlay animation support
- Multi-layer overlay rendering

## See Also

- [RFC-034: Unified Dungeon Overlay System](../rfcs/034-unified-dungeon-overlay-system.md)
- [RFC-034 Implementation Status](../implementation/rfc-034-dungeon-overlay-status.md)
- [Overlay System Documentation](../../dotnet/game-essential/core/src/PigeonPea.Overlays/README.md)
