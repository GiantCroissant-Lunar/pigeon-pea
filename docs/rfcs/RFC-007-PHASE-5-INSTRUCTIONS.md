# RFC-007 Phase 5: Arch ECS Integration - Detailed Instructions

## Status: Ready for Implementation

**Created**: 2025-11-13
**For**: Agent implementing Phase 5 of RFC-007
**Prerequisites**: Phase 4 complete (Dungeon domain implemented)

## Overview

This document provides step-by-step instructions for implementing Phase 5 of RFC-007: Arch ECS Integration. This phase creates separate ECS worlds for Map and Dungeon domains, enabling entity-based gameplay (cities, monsters, items, etc.).

## Current State Assessment

### ✅ Already Implemented

**Shared.ECS Components** (5 files, ~15 lines):
- `Position.cs` - Simple position (X, Y)
- `Sprite.cs` - Simple sprite (Id)
- `Renderable.cs` - Visibility and layer
- `Tags/MapEntityTag.cs` - Tag for map entities
- `Tags/DungeonEntityTag.cs` - Tag for dungeon entities

**Dungeon.Rendering EntityRenderer** (~85 lines):
- `EntityRenderer.RenderEntities()` - RGBA rendering
- `EntityRenderer.RenderEntitiesAscii()` - ASCII rendering
- Both methods support FOV filtering

**Arch Package**:
- ✅ Referenced in `Shared.ECS.csproj` (v2.0.0)

### ❌ To Be Implemented

**ECS Components** (gameplay):
- `Health.cs`, `Name.cs`, `Description.cs`
- `Inventory.cs`, `Item.cs`
- `Monster.cs`, `Player.cs` (tag components)
- `City.cs`, `Marker.cs` (map entity data)

**Systems**:
- `Systems/RenderingSystem.cs` - Helper for rendering queries
- `Systems/MovementSystem.cs` - Entity movement logic (optional)

**World Management**:
- `Map.Control/WorldManager/MapWorldManager.cs` - Manage map ECS world
- `Dungeon.Control/WorldManager/DungeonWorldManager.cs` - Manage dungeon ECS world

**ECS-Aware Renderers**:
- `Map.Rendering/MapEcsRenderer.cs` - Render map + entities
- `Dungeon.Rendering/DungeonEcsRenderer.cs` - Render dungeon + entities

**Application Integration**:
- Console app: Dual world management (map + dungeon)
- Windows app: ECS integration (optional)

**Tests**:
- ECS component tests
- World manager tests
- Integration tests (full pipeline with entities)

---

## Implementation Tasks

---

## Task 1: Add Gameplay ECS Components

**Priority**: P0
**Estimated Lines**: ~150-200
**Dependencies**: None

### 1.1 Create Core Gameplay Components

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Health.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Health component for entities (monsters, player).
/// </summary>
public struct Health
{
    public int Current { get; set; }
    public int Maximum { get; set; }

    public Health(int maximum)
    {
        Maximum = maximum;
        Current = maximum;
    }

    public Health(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly float HealthPercent => Maximum > 0 ? (float)Current / Maximum : 0f;
}
```

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Name.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Name component for entities.
/// </summary>
public readonly record struct Name(string Value)
{
    public static implicit operator string(Name name) => name.Value;
    public static implicit operator Name(string value) => new(value);
}
```

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Description.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Description component for entities (examine text).
/// </summary>
public readonly record struct Description(string Text);
```

### 1.2 Create Dungeon-Specific Components

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Tags/PlayerTag.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components.Tags;

/// <summary>
/// Tag component identifying the player entity.
/// </summary>
public struct PlayerTag { }
```

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Tags/MonsterTag.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components.Tags;

/// <summary>
/// Tag component identifying monster entities.
/// </summary>
public struct MonsterTag { }
```

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Tags/ItemTag.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components.Tags;

/// <summary>
/// Tag component identifying item entities.
/// </summary>
public struct ItemTag { }
```

### 1.3 Create Map-Specific Components

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/CityData.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Data component for city entities on the world map.
/// </summary>
public struct CityData
{
    public string CityName { get; set; }
    public int Population { get; set; }
    public string CultureId { get; set; }

    public CityData(string cityName, int population, string cultureId = "")
    {
        CityName = cityName;
        Population = population;
        CultureId = cultureId;
    }
}
```

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/MarkerData.cs`

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Data component for map markers (points of interest).
/// </summary>
public struct MarkerData
{
    public string MarkerType { get; set; } // "quest", "dungeon", "landmark", etc.
    public string Title { get; set; }
    public bool Discovered { get; set; }

    public MarkerData(string markerType, string title, bool discovered = false)
    {
        MarkerType = markerType;
        Title = title;
        Discovered = discovered;
    }
}
```

### 1.4 Enhance Sprite Component

The current `Sprite` component is minimal. Let's enhance it while keeping backward compatibility:

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Components/Sprite.cs` (replace)

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Sprite component for visual representation.
/// </summary>
public struct Sprite
{
    public string Id { get; set; }          // Texture/sprite ID
    public char AsciiChar { get; set; }     // For terminal rendering
    public byte R { get; set; }             // Color (red)
    public byte G { get; set; }             // Color (green)
    public byte B { get; set; }             // Color (blue)

    public Sprite(string id, char asciiChar = '?', byte r = 255, byte g = 255, byte b = 255)
    {
        Id = id;
        AsciiChar = asciiChar;
        R = r;
        G = g;
        B = b;
    }

    public Sprite(string id) : this(id, '?', 255, 255, 255) { }
}
```

**Note**: This is a breaking change from the current `record struct Sprite(string Id)`. Update existing usages.

---

## Task 2: Create ECS Systems (Helpers)

**Priority**: P1
**Estimated Lines**: ~100-150
**Dependencies**: Task 1

### 2.1 Create Systems Directory

Create directory: `dotnet/Shared/PigeonPea.Shared.ECS/Systems/`

### 2.2 Create Rendering System Helpers

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Systems/RenderingSystem.cs`

```csharp
using Arch.Core;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Shared.ECS.Systems;

/// <summary>
/// Helper utilities for rendering ECS entities.
/// </summary>
public static class RenderingSystem
{
    /// <summary>
    /// Get all entities within a viewport rectangle.
    /// </summary>
    public static QueryDescription GetVisibleEntitiesQuery<TTag>()
        where TTag : struct
    {
        return new QueryDescription()
            .WithAll<Position, Sprite, TTag>()
            .WithAll<Renderable>(); // Optional: only if entity is renderable
    }

    /// <summary>
    /// Check if entity is within viewport bounds.
    /// </summary>
    public static bool IsInViewport(in Position pos, int viewportX, int viewportY, int viewportWidth, int viewportHeight)
    {
        int localX = pos.X - viewportX;
        int localY = pos.Y - viewportY;
        return localX >= 0 && localX < viewportWidth && localY >= 0 && localY < viewportHeight;
    }

    /// <summary>
    /// Check if entity is visible with FOV.
    /// </summary>
    public static bool IsVisibleWithFov(in Position pos, bool[,]? fov)
    {
        if (fov == null) return true;

        int x = pos.X;
        int y = pos.Y;

        if (y < 0 || y >= fov.GetLength(0) || x < 0 || x >= fov.GetLength(1))
            return false;

        return fov[y, x];
    }
}
```

---

## Task 3: Create World Managers

**Priority**: P0
**Estimated Lines**: ~300-400
**Dependencies**: Task 1, Task 2

### 3.1 Create Dungeon World Manager

**Directory**: `dotnet/Dungeon/PigeonPea.Dungeon.Control/WorldManager/`

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Control/WorldManager/DungeonWorldManager.cs`

```csharp
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Control.WorldManager;

/// <summary>
/// Manages the ECS world for dungeon entities (player, monsters, items).
/// </summary>
public class DungeonWorldManager : IDisposable
{
    private readonly World _world;
    private readonly DungeonData _dungeon;
    private Entity? _playerEntity;

    public World World => _world;
    public Entity? PlayerEntity => _playerEntity;

    public DungeonWorldManager(DungeonData dungeon)
    {
        _world = World.Create();
        _dungeon = dungeon;
    }

    /// <summary>
    /// Initialize player entity at a walkable position.
    /// </summary>
    public Entity CreatePlayer(int x, int y, string name = "Player")
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        _playerEntity = _world.Create<Position, Sprite, Health, Name, PlayerTag, DungeonEntityTag, Renderable>();

        _world.Set(_playerEntity.Value, new Position(x, y));
        _world.Set(_playerEntity.Value, new Sprite("player", '@', 255, 255, 255));
        _world.Set(_playerEntity.Value, new Health(100));
        _world.Set(_playerEntity.Value, new Name(name));
        _world.Set(_playerEntity.Value, new PlayerTag());
        _world.Set(_playerEntity.Value, new DungeonEntityTag());
        _world.Set(_playerEntity.Value, new Renderable(true, Layer: 10)); // High layer priority

        return _playerEntity.Value;
    }

    /// <summary>
    /// Spawn a monster at the given position.
    /// </summary>
    public Entity SpawnMonster(int x, int y, string monsterType, int health = 20)
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        // Choose sprite based on monster type
        (char ch, byte r, byte g, byte b) = monsterType.ToLowerInvariant() switch
        {
            "goblin" => ('g', (byte)100, (byte)200, (byte)100),
            "orc" => ('o', (byte)150, (byte)100, (byte)100),
            "troll" => ('T', (byte)100, (byte)150, (byte)100),
            "dragon" => ('D', (byte)255, (byte)100, (byte)100),
            _ => ('m', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, Health, Name, MonsterTag, DungeonEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(monsterType, ch, r, g, b));
        _world.Set(entity, new Health(health));
        _world.Set(entity, new Name(monsterType));
        _world.Set(entity, new MonsterTag());
        _world.Set(entity, new DungeonEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 5));

        return entity;
    }

    /// <summary>
    /// Spawn multiple random monsters in the dungeon.
    /// </summary>
    public void SpawnRandomMonsters(int count, Random? rng = null)
    {
        rng ??= new Random();
        var monsterTypes = new[] { "goblin", "orc", "troll" };

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 20; // Prevent infinite loop

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;
            int x = rng.Next(0, _dungeon.Width);
            int y = rng.Next(0, _dungeon.Height);

            if (!_dungeon.IsWalkable(x, y))
                continue;

            // Check if position is already occupied
            if (IsPositionOccupied(x, y))
                continue;

            string monsterType = monsterTypes[rng.Next(monsterTypes.Length)];
            int health = rng.Next(15, 30);

            SpawnMonster(x, y, monsterType, health);
            spawned++;
        }
    }

    /// <summary>
    /// Spawn an item at the given position.
    /// </summary>
    public Entity SpawnItem(int x, int y, string itemType)
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        (char ch, byte r, byte g, byte b) = itemType.ToLowerInvariant() switch
        {
            "potion" => ('!', (byte)255, (byte)100, (byte)255),
            "sword" => ('/', (byte)192, (byte)192, (byte)192),
            "gold" => ('$', (byte)255, (byte)215, (byte)0),
            _ => ('?', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, Name, ItemTag, DungeonEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(itemType, ch, r, g, b));
        _world.Set(entity, new Name(itemType));
        _world.Set(entity, new ItemTag());
        _world.Set(entity, new DungeonEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 1)); // Low priority (under monsters)

        return entity;
    }

    /// <summary>
    /// Check if any entity occupies the given position.
    /// </summary>
    public bool IsPositionOccupied(int x, int y)
    {
        var query = new QueryDescription().WithAll<Position, DungeonEntityTag>();
        bool occupied = false;

        _world.Query(in query, (ref Position pos) =>
        {
            if (pos.X == x && pos.Y == y)
                occupied = true;
        });

        return occupied;
    }

    /// <summary>
    /// Move an entity to a new position if valid.
    /// </summary>
    public bool TryMoveEntity(Entity entity, int newX, int newY)
    {
        if (!_dungeon.IsWalkable(newX, newY))
            return false;

        if (IsPositionOccupied(newX, newY))
            return false;

        if (_world.Has<Position>(entity))
        {
            var pos = _world.Get<Position>(entity);
            _world.Set(entity, new Position(newX, newY));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get player position.
    /// </summary>
    public Position? GetPlayerPosition()
    {
        if (_playerEntity.HasValue && _world.IsAlive(_playerEntity.Value))
        {
            return _world.Get<Position>(_playerEntity.Value);
        }
        return null;
    }

    /// <summary>
    /// Remove dead monsters from the world.
    /// </summary>
    public void CleanupDeadMonsters()
    {
        var query = new QueryDescription().WithAll<Health, MonsterTag>();
        var deadEntities = new List<Entity>();

        _world.Query(in query, (Entity entity, ref Health health) =>
        {
            if (health.IsDead)
                deadEntities.Add(entity);
        });

        foreach (var entity in deadEntities)
        {
            _world.Destroy(entity);
        }
    }

    public void Dispose()
    {
        World.Dispose(_world);
    }
}
```

### 3.2 Create Map World Manager

**Directory**: `dotnet/Map/PigeonPea.Map.Control/WorldManager/`

**File**: `dotnet/Map/PigeonPea.Map.Control/WorldManager/MapWorldManager.cs`

```csharp
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Map.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Map.Control.WorldManager;

/// <summary>
/// Manages the ECS world for map entities (cities, markers, etc.).
/// </summary>
public class MapWorldManager : IDisposable
{
    private readonly World _world;
    private readonly MapData _mapData;

    public World World => _world;

    public MapWorldManager(MapData mapData)
    {
        _world = World.Create();
        _mapData = mapData;
    }

    /// <summary>
    /// Populate cities from MapData.
    /// Assumes MapData has city/burg information (from FantasyMapGenerator).
    /// </summary>
    public void PopulateCitiesFromMapData()
    {
        // NOTE: This depends on FantasyMapGenerator's burg data structure
        // For now, this is a placeholder showing the pattern
        // You'll need to access actual burg data from MapData

        // Example: If MapData exposes Burgs collection
        // foreach (var burg in _mapData.Burgs)
        // {
        //     CreateCity(burg.X, burg.Y, burg.Name, burg.Population);
        // }

        // Placeholder: Create a few example cities
        CreateCity(100, 100, "Capital City", 50000, "default");
        CreateCity(200, 150, "Port Town", 20000, "coastal");
        CreateCity(150, 200, "Mountain Fortress", 10000, "highland");
    }

    /// <summary>
    /// Create a city entity.
    /// </summary>
    public Entity CreateCity(int x, int y, string cityName, int population, string cultureId = "")
    {
        var entity = _world.Create<Position, Sprite, CityData, Name, MapEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite("city", '◉', 255, 200, 100)); // Gold circle
        _world.Set(entity, new CityData(cityName, population, cultureId));
        _world.Set(entity, new Name(cityName));
        _world.Set(entity, new MapEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 10));

        return entity;
    }

    /// <summary>
    /// Create a map marker (point of interest).
    /// </summary>
    public Entity CreateMarker(int x, int y, string markerType, string title, bool discovered = false)
    {
        (char ch, byte r, byte g, byte b) = markerType.ToLowerInvariant() switch
        {
            "quest" => ('!', (byte)255, (byte)255, (byte)0),   // Yellow
            "dungeon" => ('▼', (byte)200, (byte)100, (byte)100), // Red
            "landmark" => ('△', (byte)100, (byte)200, (byte)255), // Blue
            _ => ('?', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, MarkerData, Name, MapEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(markerType, ch, r, g, b));
        _world.Set(entity, new MarkerData(markerType, title, discovered));
        _world.Set(entity, new Name(title));
        _world.Set(entity, new MapEntityTag());
        _world.Set(entity, new Renderable(discovered, Layer: 5)); // Only visible if discovered

        return entity;
    }

    /// <summary>
    /// Create multiple random dungeon markers.
    /// </summary>
    public void CreateRandomDungeonMarkers(int count, Random? rng = null)
    {
        rng ??= new Random();

        for (int i = 0; i < count; i++)
        {
            // Generate random positions within map bounds
            // NOTE: Adjust based on actual MapData dimensions
            int x = rng.Next(0, 500);
            int y = rng.Next(0, 500);

            bool discovered = rng.NextDouble() < 0.3; // 30% discovered
            CreateMarker(x, y, "dungeon", $"Dungeon {i + 1}", discovered);
        }
    }

    /// <summary>
    /// Get all cities within a viewport.
    /// </summary>
    public List<(Entity entity, Position pos, CityData data)> GetCitiesInViewport(int viewportX, int viewportY, int viewportWidth, int viewportHeight)
    {
        var result = new List<(Entity, Position, CityData)>();
        var query = new QueryDescription().WithAll<Position, CityData, MapEntityTag>();

        _world.Query(in query, (Entity entity, ref Position pos, ref CityData data) =>
        {
            int localX = pos.X - viewportX;
            int localY = pos.Y - viewportY;

            if (localX >= 0 && localX < viewportWidth && localY >= 0 && localY < viewportHeight)
            {
                result.Add((entity, pos, data));
            }
        });

        return result;
    }

    public void Dispose()
    {
        World.Dispose(_world);
    }
}
```

---

## Task 4: Create ECS-Aware Renderers

**Priority**: P0
**Estimated Lines**: ~200-300
**Dependencies**: Task 3

### 4.1 Update Dungeon EntityRenderer

The existing `EntityRenderer` already works, but let's enhance it to use the new Sprite component with colors:

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/EntityRenderer.cs` (update)

Replace the hardcoded RGB values (lines 48-50) with sprite colors:

```csharp
// OLD (lines 48-50):
rgba[idx] = 255;
rgba[idx + 1] = 100;
rgba[idx + 2] = 100;

// NEW:
rgba[idx] = sprite.R;
rgba[idx + 1] = sprite.G;
rgba[idx + 2] = sprite.B;
```

And for ASCII rendering (line 81), use the sprite's AsciiChar:

```csharp
// OLD (line 81):
asciiBuffer[localY, localX] = 'g';

// NEW:
asciiBuffer[localY, localX] = sprite.AsciiChar;
```

### 4.2 Create Map Entity Renderer

**File**: `dotnet/Map/PigeonPea.Map.Rendering/MapEntityRenderer.cs`

```csharp
using Arch.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Map.Rendering;

/// <summary>
/// Renders ECS entities (cities, markers) onto map rasters.
/// </summary>
public static class MapEntityRenderer
{
    /// <summary>
    /// Render map entities (cities, markers) onto RGBA buffer.
    /// </summary>
    /// <param name="world">Arch ECS world containing map entities</param>
    /// <param name="rgba">RGBA buffer to draw on</param>
    /// <param name="widthPx">Width in pixels</param>
    /// <param name="heightPx">Height in pixels</param>
    /// <param name="viewportX">Viewport top-left X (world coordinates)</param>
    /// <param name="viewportY">Viewport top-left Y (world coordinates)</param>
    /// <param name="viewportWidth">Viewport width (world coordinates)</param>
    /// <param name="viewportHeight">Viewport height (world coordinates)</param>
    /// <param name="zoom">Zoom level (pixels per world unit)</param>
    public static void RenderEntities(
        World world,
        byte[] rgba,
        int widthPx,
        int heightPx,
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        double zoom)
    {
        var query = new QueryDescription()
            .WithAll<Position, Sprite, MapEntityTag, Renderable>();

        world.Query(in query, (ref Position pos, ref Sprite sprite, ref Renderable renderable) =>
        {
            if (!renderable.Visible)
                return;

            // Convert world position to viewport local
            double localX = (pos.X - viewportX) / viewportWidth * widthPx;
            double localY = (pos.Y - viewportY) / viewportHeight * heightPx;

            // Check if in viewport
            if (localX < 0 || localX >= widthPx || localY < 0 || localY >= heightPx)
                return;

            // Draw entity as a small circle/marker (5x5 pixels centered on position)
            int centerPx = (int)localX;
            int centerPy = (int)localY;
            int radius = 3;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius)
                        continue; // Circle shape

                    int px = centerPx + dx;
                    int py = centerPy + dy;

                    if (px < 0 || px >= widthPx || py < 0 || py >= heightPx)
                        continue;

                    int idx = (py * widthPx + px) * 4;
                    if (idx + 3 < rgba.Length)
                    {
                        rgba[idx] = sprite.R;
                        rgba[idx + 1] = sprite.G;
                        rgba[idx + 2] = sprite.B;
                        rgba[idx + 3] = 255;
                    }
                }
            }
        });
    }
}
```

---

## Task 5: Console Application Integration

**Priority**: P0
**Estimated Lines**: ~150-200
**Dependencies**: Task 3, Task 4

### 5.1 Update DungeonPanelView to Use ECS

**File**: `dotnet/console-app/Views/DungeonPanelView.cs` (update)

Replace the current implementation with ECS-aware version:

```csharp
using Terminal.Gui;
using Arch.Core;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Dungeon.Rendering;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Console.Views;

public class DungeonPanelView : View
{
    private readonly DungeonData _dungeon;
    private readonly DungeonWorldManager _worldManager;
    private readonly FovCalculator _fov;
    private int _fovRange = 8;

    public DungeonPanelView(DungeonData dungeon, DungeonWorldManager worldManager)
    {
        _dungeon = dungeon;
        _worldManager = worldManager;
        _fov = new FovCalculator(dungeon);

        CanFocus = true;
        KeyDown += OnKeyDown;
    }

    protected override bool OnDrawingContent()
    {
        var playerPos = _worldManager.GetPlayerPosition();
        if (!playerPos.HasValue)
            return false;

        var fovMask = _fov.ComputeVisible(playerPos.Value.X, playerPos.Value.Y, _fovRange);

        int viewWidth = Viewport.Width;
        int viewHeight = Viewport.Height;
        int viewX = playerPos.Value.X - viewWidth / 2;
        int viewY = playerPos.Value.Y - viewHeight / 2;

        // Render dungeon base
        string asciiDungeon = BrailleDungeonRenderer.RenderAscii(
            _dungeon,
            viewX,
            viewY,
            viewWidth,
            viewHeight,
            fovMask);

        // Render entities on top (convert to char array for modification)
        var lines = asciiDungeon.Split('\n');
        var charBuffer = new char[viewHeight, viewWidth];

        for (int y = 0; y < lines.Length && y < viewHeight; y++)
        {
            string line = lines[y];
            for (int x = 0; x < line.Length && x < viewWidth; x++)
            {
                charBuffer[y, x] = line[x];
            }
        }

        // Render ECS entities
        EntityRenderer.RenderEntitiesAscii(_worldManager.World, charBuffer, viewX, viewY, fovMask);

        // Convert back to string
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < viewHeight; y++)
        {
            for (int x = 0; x < viewWidth; x++)
            {
                sb.Append(charBuffer[y, x]);
            }
            if (y < viewHeight - 1)
                sb.AppendLine();
        }

        Driver.Move(0, 0);
        Driver.SetAttribute(new Terminal.Gui.Attribute(Color.White, Color.Black));
        Driver.AddStr(sb.ToString());

        return true;
    }

    private void OnKeyDown(object? sender, Key e)
    {
        var playerPos = _worldManager.GetPlayerPosition();
        if (!playerPos.HasValue || !_worldManager.PlayerEntity.HasValue)
            return;

        int dx = 0, dy = 0;
        switch (e.KeyCode)
        {
            case KeyCode.CursorUp:
            case KeyCode.W: dy = -1; break;
            case KeyCode.CursorDown:
            case KeyCode.S: dy = 1; break;
            case KeyCode.CursorLeft:
            case KeyCode.A: dx = -1; break;
            case KeyCode.CursorRight:
            case KeyCode.D: dx = 1; break;
            default: return;
        }

        int newX = playerPos.Value.X + dx;
        int newY = playerPos.Value.Y + dy;

        if (_worldManager.TryMoveEntity(_worldManager.PlayerEntity.Value, newX, newY))
        {
            SetNeedsDraw();
        }
    }
}
```

### 5.2 Update Program.cs Dungeon Demo

**File**: `dotnet/console-app/Program.cs` (update dungeon demo section)

Find the dungeon demo implementation (around line 220-360) and update to use `DungeonWorldManager`:

```csharp
// In the dungeon demo section, after generating dungeon:
var generator = new BasicDungeonGenerator();
var dungeon = generator.Generate(w: 80, h: 40, seed: seed);

// NEW: Create world manager and populate entities
using var worldManager = new DungeonWorldManager(dungeon);

// Find player spawn position
(int px, int py) = (0, 0);
for (int y = 0; y < dungeon.Height; y++)
{
    for (int x = 0; x < dungeon.Width; x++)
    {
        if (dungeon.IsWalkable(x, y))
        {
            px = x;
            py = y;
            goto foundSpawn;
        }
    }
}
foundSpawn:

// Create player
worldManager.CreatePlayer(px, py, "Hero");

// Spawn monsters
worldManager.SpawnRandomMonsters(count: 10, rng: new Random(seed ?? 42));

// Spawn items
for (int i = 0; i < 5; i++)
{
    int ix, iy;
    do
    {
        ix = rng.Next(0, dungeon.Width);
        iy = rng.Next(0, dungeon.Height);
    } while (!dungeon.IsWalkable(ix, iy) || worldManager.IsPositionOccupied(ix, iy));

    worldManager.SpawnItem(ix, iy, i % 2 == 0 ? "potion" : "gold");
}

// ... rest of demo using worldManager.GetPlayerPosition(), etc.
```

---

## Task 6: Create Integration Tests

**Priority**: P1
**Estimated Lines**: ~200-300
**Dependencies**: Task 3, Task 4

### 6.1 Dungeon World Manager Tests

**File**: `dotnet/Dungeon.Tests/DungeonWorldManagerTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Tests;

public class DungeonWorldManagerTests
{
    [Fact]
    public void CreatePlayer_CreatesEntityWithCorrectComponents()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        var player = manager.CreatePlayer(5, 5, "TestPlayer");

        // Assert
        manager.PlayerEntity.Should().NotBeNull();
        manager.World.IsAlive(player).Should().BeTrue();
        manager.World.Has<Position>(player).Should().BeTrue();
        manager.World.Has<Sprite>(player).Should().BeTrue();
        manager.World.Has<Health>(player).Should().BeTrue();
        manager.World.Has<Name>(player).Should().BeTrue();
        manager.World.Has<PlayerTag>(player).Should().BeTrue();

        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(5);
        pos.Y.Should().Be(5);

        var health = manager.World.Get<Health>(player);
        health.Current.Should().Be(100);
        health.Maximum.Should().Be(100);
    }

    [Fact]
    public void SpawnMonster_CreatesMonsterEntity()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(10, 10);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        var monster = manager.SpawnMonster(10, 10, "goblin", 20);

        // Assert
        manager.World.IsAlive(monster).Should().BeTrue();
        manager.World.Has<MonsterTag>(monster).Should().BeTrue();
        manager.World.Has<DungeonEntityTag>(monster).Should().BeTrue();

        var health = manager.World.Get<Health>(monster);
        health.Maximum.Should().Be(20);
    }

    [Fact]
    public void SpawnRandomMonsters_CreatesMultipleMonsters()
    {
        // Arrange
        var dungeon = new DungeonData(50, 50);
        for (int y = 0; y < 50; y++)
            for (int x = 0; x < 50; x++)
                dungeon.SetFloor(x, y);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        manager.SpawnRandomMonsters(count: 10, rng: new Random(123));

        // Assert
        var query = new Arch.Core.QueryDescription().WithAll<MonsterTag>();
        int monsterCount = 0;
        manager.World.Query(in query, (Arch.Core.Entity entity) => monsterCount++);

        monsterCount.Should().Be(10);
    }

    [Fact]
    public void TryMoveEntity_MovesPlayerToValidPosition()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetFloor(6, 5);

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(5, 5);

        // Act
        bool moved = manager.TryMoveEntity(player, 6, 5);

        // Assert
        moved.Should().BeTrue();
        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(6);
        pos.Y.Should().Be(5);
    }

    [Fact]
    public void TryMoveEntity_FailsForWall()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetWall(6, 5);

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(5, 5);

        // Act
        bool moved = manager.TryMoveEntity(player, 6, 5);

        // Assert
        moved.Should().BeFalse();
        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(5); // Still at original position
    }

    [Fact]
    public void IsPositionOccupied_DetectsOccupiedTile()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetFloor(6, 6);

        using var manager = new DungeonWorldManager(dungeon);
        manager.CreatePlayer(5, 5);

        // Act & Assert
        manager.IsPositionOccupied(5, 5).Should().BeTrue();
        manager.IsPositionOccupied(6, 6).Should().BeFalse();
    }
}
```

### 6.2 ECS Integration Test

**File**: `dotnet/Dungeon.Tests/EcsIntegrationTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Dungeon.Rendering;

namespace PigeonPea.Dungeon.Tests;

public class EcsIntegrationTests
{
    [Fact]
    public void FullPipeline_GenerateDungeonPopulateEntitiesRender_Works()
    {
        // 1. Generate dungeon
        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(width: 40, height: 40, seed: 42);

        // 2. Create ECS world and populate
        using var worldManager = new DungeonWorldManager(dungeon);

        // Find walkable position for player
        (int px, int py) = FindWalkablePosition(dungeon);
        var player = worldManager.CreatePlayer(px, py, "TestHero");

        // Spawn monsters
        worldManager.SpawnRandomMonsters(count: 5, rng: new Random(42));

        // Spawn items
        for (int i = 0; i < 3; i++)
        {
            var (ix, iy) = FindWalkablePosition(dungeon, new Random(100 + i));
            worldManager.SpawnItem(ix, iy, "potion");
        }

        // 3. Calculate FOV
        var fov = new FovCalculator(dungeon);
        var visible = fov.ComputeVisible(px, py, range: 10);

        // 4. Render to raster with entities
        var raster = SkiaDungeonRasterizer.Render(dungeon, 0, 0, 30, 30, pixelsPerCell: 16, fov: visible);
        raster.Should().NotBeNull();

        // Render entities onto raster
        EntityRenderer.RenderEntities(
            worldManager.World,
            raster.Rgba,
            raster.WidthPx,
            raster.HeightPx,
            0, 0,
            pixelsPerCell: 16,
            fov: visible);

        // 5. Verify rendering succeeded
        raster.Rgba.Should().NotBeEmpty();

        // 6. Render to ASCII
        var ascii = BrailleDungeonRenderer.RenderAscii(dungeon, 0, 0, 30, 30, fov: visible);
        ascii.Should().NotBeNullOrEmpty();

        // Verify entities exist in world
        var query = new Arch.Core.QueryDescription().WithAll<PigeonPea.Shared.ECS.Components.Tags.DungeonEntityTag>();
        int entityCount = 0;
        worldManager.World.Query(in query, (Arch.Core.Entity e) => entityCount++);

        entityCount.Should().BeGreaterThan(0); // Player + monsters + items
    }

    private static (int x, int y) FindWalkablePosition(DungeonData dungeon, Random? rng = null)
    {
        rng ??= new Random();
        for (int attempts = 0; attempts < 1000; attempts++)
        {
            int x = rng.Next(0, dungeon.Width);
            int y = rng.Next(0, dungeon.Height);

            if (dungeon.IsWalkable(x, y))
                return (x, y);
        }

        // Fallback: scan entire dungeon
        for (int y = 0; y < dungeon.Height; y++)
            for (int x = 0; x < dungeon.Width; x++)
                if (dungeon.IsWalkable(x, y))
                    return (x, y);

        throw new InvalidOperationException("No walkable position found");
    }
}
```

---

## Task 7: Windows App Integration (Optional)

**Priority**: P2
**Estimated Lines**: ~100-200
**Dependencies**: Task 3, Task 4

### 7.1 Update Windows App ViewModel

If the Windows app uses MVVM with ReactiveUI, create a ViewModel:

**File**: `dotnet/windows-app/ViewModels/DungeonViewModel.cs` (new)

```csharp
using ReactiveUI;
using Arch.Core;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Windows.ViewModels;

public class DungeonViewModel : ReactiveObject, IDisposable
{
    private readonly DungeonData _dungeon;
    private readonly DungeonWorldManager _worldManager;

    public DungeonData Dungeon => _dungeon;
    public World EcsWorld => _worldManager.World;

    public DungeonViewModel()
    {
        var generator = new BasicDungeonGenerator();
        _dungeon = generator.Generate(width: 80, height: 60, seed: 42);

        _worldManager = new DungeonWorldManager(_dungeon);

        // Initialize player
        var (px, py) = FindWalkablePosition();
        _worldManager.CreatePlayer(px, py, "Player");

        // Spawn entities
        _worldManager.SpawnRandomMonsters(count: 20);
    }

    private (int x, int y) FindWalkablePosition()
    {
        for (int y = 0; y < _dungeon.Height; y++)
            for (int x = 0; x < _dungeon.Width; x++)
                if (_dungeon.IsWalkable(x, y))
                    return (x, y);
        return (0, 0);
    }

    public void Dispose()
    {
        _worldManager?.Dispose();
    }
}
```

---

## Task 8: Performance Optimization

**Priority**: P2
**Estimated Lines**: ~50-100
**Dependencies**: Task 3, Task 4

### 8.1 Add Entity Pooling (Optional)

For better performance, consider entity pooling:

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Systems/EntityPool.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Shared.ECS.Systems;

/// <summary>
/// Simple entity pool for reusing destroyed entities.
/// </summary>
public class EntityPool
{
    private readonly World _world;
    private readonly Queue<Entity> _pool = new();

    public EntityPool(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Get an entity from the pool or create a new one.
    /// </summary>
    public Entity GetOrCreate<T1, T2, T3, T4, T5>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        if (_pool.Count > 0)
        {
            var entity = _pool.Dequeue();
            if (_world.IsAlive(entity))
                return entity;
        }

        return _world.Create<T1, T2, T3, T4, T5>();
    }

    /// <summary>
    /// Return an entity to the pool.
    /// </summary>
    public void Return(Entity entity)
    {
        if (_world.IsAlive(entity))
        {
            _pool.Enqueue(entity);
        }
    }
}
```

### 8.2 Add Spatial Partitioning (Optional)

For large maps with many entities, add spatial indexing:

**File**: `dotnet/Shared/PigeonPea.Shared.ECS/Systems/SpatialIndex.cs`

```csharp
using Arch.Core;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Shared.ECS.Systems;

/// <summary>
/// Simple spatial hash for fast entity lookup by position.
/// </summary>
public class SpatialIndex
{
    private readonly Dictionary<(int x, int y), List<Entity>> _grid = new();

    public void Add(Entity entity, int x, int y)
    {
        var key = (x, y);
        if (!_grid.ContainsKey(key))
            _grid[key] = new List<Entity>();

        _grid[key].Add(entity);
    }

    public void Remove(Entity entity, int x, int y)
    {
        var key = (x, y);
        if (_grid.TryGetValue(key, out var list))
        {
            list.Remove(entity);
            if (list.Count == 0)
                _grid.Remove(key);
        }
    }

    public List<Entity> GetEntitiesAt(int x, int y)
    {
        return _grid.TryGetValue((x, y), out var list) ? list : new List<Entity>();
    }

    public void Clear()
    {
        _grid.Clear();
    }
}
```

---

## Task 9: Documentation and Examples

**Priority**: P2
**Estimated Lines**: N/A
**Dependencies**: All tasks complete

### 9.1 Create ECS Usage Examples

**File**: `docs/examples/ecs-usage.md`

```markdown
# ECS Usage Examples

## Creating a Dungeon with Entities

```csharp
// 1. Generate dungeon
var generator = new BasicDungeonGenerator();
var dungeon = generator.Generate(width: 80, height: 60, seed: 12345);

// 2. Create ECS world manager
using var worldManager = new DungeonWorldManager(dungeon);

// 3. Create player
var player = worldManager.CreatePlayer(x: 10, y: 10, name: "Hero");

// 4. Spawn monsters
worldManager.SpawnRandomMonsters(count: 20, rng: new Random());

// 5. Spawn items
worldManager.SpawnItem(x: 15, y: 15, itemType: "potion");

// 6. Move player
worldManager.TryMoveEntity(player, newX: 11, newY: 10);

// 7. Get player position
var pos = worldManager.GetPlayerPosition();
Console.WriteLine($"Player at ({pos?.X}, {pos?.Y})");
```

## Rendering with ECS

```csharp
// Render dungeon with entities
var fov = new FovCalculator(dungeon);
var visible = fov.ComputeVisible(playerX, playerY, range: 10);

var raster = SkiaDungeonRasterizer.Render(
    dungeon,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: 40,
    viewportHeight: 30,
    pixelsPerCell: 16,
    fov: visible);

// Render entities on top
EntityRenderer.RenderEntities(
    worldManager.World,
    raster.Rgba,
    raster.WidthPx,
    raster.HeightPx,
    viewportX: 0,
    viewportY: 0,
    pixelsPerCell: 16,
    fov: visible);
```

## Querying Entities

```csharp
// Find all monsters within 5 tiles of player
var playerPos = worldManager.GetPlayerPosition().Value;
var query = new QueryDescription().WithAll<Position, MonsterTag>();

worldManager.World.Query(in query, (Entity entity, ref Position pos) =>
{
    int distance = Math.Abs(pos.X - playerPos.X) + Math.Abs(pos.Y - playerPos.Y);
    if (distance <= 5)
    {
        Console.WriteLine($"Monster at ({pos.X}, {pos.Y})");
    }
});
```
```

---

## Task 10: Cleanup

**Priority**: P2
**Dependencies**: None

### 10.1 Remove Placeholder Files

Delete remaining `Class1.cs` files:

```bash
rm dotnet/Dungeon/PigeonPea.Dungeon.Rendering/Class1.cs
rm dotnet/Map/PigeonPea.Map.Core/Class1.cs
rm dotnet/Map/PigeonPea.Map.Control/Class1.cs
```

### 10.2 Fix Failing Tests

Fix the 2 failing tests from Phase 4:
- `FovBlockingTests.ComputeVisible_WallsBlockLos`
- `PathfindingWeightedCornerTests.WeightedCost_PrefersCheaperTiles`

---

## Success Criteria

### Build & Test
- [ ] `dotnet build dotnet/PigeonPea.sln` succeeds (0 errors, 0 warnings)
- [ ] `dotnet test dotnet/Dungeon.Tests/` passes all tests (15+ tests)
- [ ] All placeholder `Class1.cs` files removed

### Functionality
- [ ] Can create dungeon ECS world with `DungeonWorldManager`
- [ ] Can create map ECS world with `MapWorldManager`
- [ ] Can spawn player, monsters, items in dungeon
- [ ] Can spawn cities, markers on map
- [ ] Can move entities with collision detection
- [ ] Can render dungeon + entities with FOV filtering
- [ ] Can render map + entities (cities, markers)
- [ ] Console app runs with ECS entities: `dotnet run --project dotnet/console-app -- --dungeon-demo`

### Architecture
- [ ] Separate ECS worlds for Map and Dungeon (no interference)
- [ ] Entity rendering works in both domains
- [ ] World managers properly encapsulate ECS logic
- [ ] Renderers remain stateless (worlds passed as parameters)

### Performance
- [ ] Rendering 100+ entities maintains >30 FPS (console)
- [ ] No memory leaks (worlds properly disposed)
- [ ] Query performance acceptable (<1ms for typical dungeon)

---

## Verification Commands

After implementation, run these commands to verify success:

```bash
# Build solution
dotnet build dotnet/PigeonPea.sln

# Run all tests
dotnet test dotnet/Dungeon.Tests/
dotnet test dotnet/

# Run ECS dungeon demo
dotnet run --project dotnet/console-app -- --dungeon-demo

# Run map demo (if updated with ECS)
dotnet run --project dotnet/console-app -- --map-demo

# Check for placeholder files (should find none)
find dotnet/ -name "Class1.cs" -not -path "*/obj/*"
```

Expected: Clean build, all tests pass, demos run with entities visible.

---

## Performance Benchmarks

**Target performance** (for validation):

| Scenario | Target | Measurement |
|----------|--------|-------------|
| Create 100 entities | <1ms | Time to spawn 100 monsters |
| Query 100 entities | <0.5ms | Time to query all monsters in viewport |
| Render 100 entities (RGBA) | <5ms | Time to render entities to 640x480 raster |
| Render 100 entities (ASCII) | <2ms | Time to render entities to 80x40 console |
| Move entity | <0.1ms | Time to move single entity with collision check |

**Benchmark code** (optional, in `dotnet/benchmarks/`):

```csharp
using BenchmarkDotNet.Attributes;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;

[MemoryDiagnoser]
public class EcsBenchmarks
{
    private DungeonData _dungeon = null!;
    private DungeonWorldManager _manager = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dungeon = new BasicDungeonGenerator().Generate(100, 100, seed: 42);
        _manager = new DungeonWorldManager(_dungeon);
    }

    [Benchmark]
    public void SpawnMonster()
    {
        _manager.SpawnMonster(10, 10, "goblin", 20);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _manager?.Dispose();
    }
}
```

---

## Common Issues and Solutions

### Issue 1: Sprite Component Breaking Change

**Problem**: Changing `Sprite` from `record struct Sprite(string Id)` to full struct breaks existing code.

**Solution**: Update all usages:

```csharp
// OLD:
new Sprite("goblin")

// NEW:
new Sprite("goblin", 'g', 100, 200, 100)
```

Or keep backward compatibility with overload:
```csharp
public Sprite(string id) : this(id, '?', 255, 255, 255) { }
```

### Issue 2: World Not Disposed

**Problem**: Memory leak from not disposing ECS worlds.

**Solution**: Always use `using` statements:

```csharp
using var worldManager = new DungeonWorldManager(dungeon);
// ... use world ...
// Automatically disposed when out of scope
```

### Issue 3: Entity Query Performance

**Problem**: Querying all entities every frame is slow.

**Solution**: Cache query results or use spatial indexing (Task 8.2).

### Issue 4: FOV and Entity Rendering Mismatch

**Problem**: Entities render outside FOV.

**Solution**: Always pass FOV to `EntityRenderer.RenderEntities()` and check `IsVisibleWithFov()`.

---

## Next Steps After Phase 5

**Phase 6**: Deprecate Old Projects (RFC-007 lines 448-476)
- Mark `FantasyMapGenerator.Rendering` as deprecated
- Remove old `SharedApp.Rendering` code
- Update documentation

**Phase 7**: Documentation and Polish (RFC-007 lines 477-500)
- Complete architecture documentation
- Add usage examples
- Update onboarding guide

---

## Implementation Timeline

**Estimated effort**: 15-20 hours

- **Task 1-2**: Components & Systems (3-4 hours)
- **Task 3**: World Managers (4-5 hours)
- **Task 4**: ECS Renderers (2-3 hours)
- **Task 5**: Console Integration (2-3 hours)
- **Task 6**: Tests (3-4 hours)
- **Task 7-8**: Optional enhancements (2-3 hours)
- **Task 9-10**: Documentation & cleanup (1-2 hours)

**Critical path**: Tasks 1 → 3 → 4 → 5 → 6

---

## Final Checklist

Before marking Phase 5 complete:

- [ ] All new components created (10+ files)
- [ ] World managers implemented for both domains
- [ ] Entity rendering works with FOV filtering
- [ ] Console app demo runs with ECS entities
- [ ] Build succeeds (0 errors)
- [ ] All tests pass (18+ tests)
- [ ] Performance targets met
- [ ] Placeholder files removed
- [ ] Code follows existing patterns
- [ ] Documentation updated

---

**Implementation Status**: Ready to begin
**Prerequisites**: Phase 4 complete ✅
**Next Phase**: Phase 6 (Deprecation and cleanup)
