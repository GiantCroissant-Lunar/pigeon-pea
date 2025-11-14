# RFC-007 Phase 6: Deprecation and Documentation - Detailed Instructions

## Status: Ready for Implementation

**Created**: 2025-11-13
**For**: Agent implementing Phase 6 of RFC-007
**Prerequisites**: Phase 5 complete (ECS integration working)

## Overview

This document provides step-by-step instructions for implementing Phase 6 of RFC-007: Deprecating old projects, cleaning up duplicate code, and updating documentation. This is primarily a cleanup and polish phase to finalize the domain-driven architecture reorganization.

## Current State Assessment

### ✅ Already Implemented (Phases 1-5)

**New Architecture** (operational):

- Map domain: Core, Control, Rendering (3 projects)
- Dungeon domain: Core, Control, Rendering (3 projects)
- Shared: ECS, Rendering (2 projects)
- ECS integration with dual worlds
- Console/Windows apps using new architecture

**Build Status**: ✅ 0 errors, 0 warnings
**Test Status**: ✅ 22/24 passing (92%)

### ❌ To Be Cleaned Up

**Old Projects** (deprecated/duplicate):

- `_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/` (~12 files)
- `dotnet/shared-app/Rendering/` (~16 files)

**Placeholder Files** (6 files):

- `Dungeon/PigeonPea.Dungeon.Control/Class1.cs`
- `Dungeon/PigeonPea.Dungeon.Core/Class1.cs`
- `Dungeon/PigeonPea.Dungeon.Rendering/Class1.cs`
- `Map/PigeonPea.Map.Control/Class1.cs`
- `Map/PigeonPea.Map.Core/Class1.cs`
- `Map/PigeonPea.Map.Rendering/Class1.cs`

**Documentation** (needs updates):

- Architecture docs outdated
- No domain organization guide
- No ECS usage examples
- CLAUDE.md needs architecture section update

**Failing Tests** (2 edge cases from Phase 4):

- `FovBlockingTests.ComputeVisible_WallsBlockLos`
- `PathfindingWeightedCornerTests.WeightedCost_PrefersCheaperTiles`

---

## Implementation Tasks

---

## Task 1: Deprecate FantasyMapGenerator.Rendering

**Priority**: P0
**Estimated Time**: 1-2 hours
**Dependencies**: None

### 1.1 Mark as Deprecated

**File**: `dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/README-DEPRECATED.md` (new)

````markdown
# ⚠️ DEPRECATED - FantasyMapGenerator.Rendering

**Status**: Deprecated as of 2025-11-13 (RFC-007 Phase 6)

## Why Deprecated?

This project was part of the original FantasyMapGenerator port but has been superseded by the new domain-driven architecture:

- **Replacement**: `PigeonPea.Map.Rendering`
- **Location**: `dotnet/Map/PigeonPea.Map.Rendering/`

## Migration Path

If you were using `FantasyMapGenerator.Rendering`, migrate to:

### Old Code (Deprecated)

```csharp
using FantasyMapGenerator.Rendering;

var renderer = new TerrainRenderer();
var image = renderer.RenderMap(mapData);
```
````

### New Code (Current)

```csharp
using PigeonPea.Map.Rendering;

var raster = SkiaMapRasterizer.Render(
    mapData,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: 100,
    viewportHeight: 100,
    pixelsPerCell: 16
);
```

## What Replaced What?

| Old (Deprecated)        | New (Current)                        |
| ----------------------- | ------------------------------------ |
| `TerrainRenderer`       | `SkiaMapRasterizer`                  |
| `ColorSchemes`          | `MapColor` (in Map.Core/Domain/)     |
| `SmoothTerrainRenderer` | Integrated into `SkiaMapRasterizer`  |
| `TileSource`            | `Shared.Rendering/Tiles/ITileSource` |

## Files in This Directory

This directory contains ~12 files that are **no longer used** in the PigeonPea project:

- Rendering/TerrainRenderer.cs
- Rendering/ColorSchemes.cs
- Rendering/SmoothTerrainRenderer.cs
- etc.

## Should I Delete This?

**No, keep it for reference**. This is part of the FantasyMapGenerator library port and may be useful for:

- Understanding original rendering algorithms
- Reference implementation
- Standalone FantasyMapGenerator usage (if someone uses it outside PigeonPea)

**PigeonPea project does not use this code anymore** - all rendering is in `Map.Rendering`.

## See Also

- [RFC-007: Domain-Driven Architecture](../../../../../docs/rfcs/007-consolidate-rendering-projects.md)
- [Map.Rendering Documentation](../../../../../docs/architecture/map-rendering.md)
- [Migration Guide](../../../../../docs/migration/fmg-rendering-to-map-rendering.md)

````

### 1.2 Update .csproj (Mark as Non-Buildable)

**File**: `dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/FantasyMapGenerator.Rendering.csproj`

Add this property to mark it as deprecated (optional, for clarity):

```xml
<PropertyGroup>
  <!-- ... existing properties ... -->

  <!-- Mark as deprecated -->
  <IsPackable>false</IsPackable>
  <Description>⚠️ DEPRECATED - Use PigeonPea.Map.Rendering instead. See README-DEPRECATED.md</Description>
</PropertyGroup>
````

### 1.3 Exclude from Active Build (Optional)

If you want to exclude it from the solution build (optional step):

**Option A**: Remove from solution file (not recommended - keep for reference)

**Option B**: Comment out in any project references (verify none exist):

```bash
# Check for references
grep -r "FantasyMapGenerator.Rendering" dotnet/ --include="*.csproj"
```

Expected: No results (already verified - no active references).

---

## Task 2: Archive SharedApp.Rendering

**Priority**: P0
**Estimated Time**: 2-3 hours
**Dependencies**: Verify no active references

### 2.1 Verify No Active References

**Command**:

```bash
cd dotnet
grep -r "SharedApp.Rendering\|shared-app/Rendering" --include="*.cs" --include="*.csproj" \
  --exclude-dir=obj \
  --exclude-dir=bin \
  --exclude-dir=archive
```

**Expected**: No results (or only in comments/old files).

If you find references, update them to use the new architecture:

- `SharedApp.Rendering.SkiaMapRasterizer` → `PigeonPea.Map.Rendering.SkiaMapRasterizer`
- `SharedApp.Rendering.MapDataRenderer` → `PigeonPea.Map.Rendering.BrailleMapRenderer`
- `SharedApp.Rendering.Tiles.*` → `PigeonPea.Shared.Rendering.Tiles.*`

### 2.2 Create Archive Directory

```bash
mkdir -p dotnet/archive/
```

### 2.3 Move to Archive

**Command**:

```bash
cd dotnet
mv shared-app/Rendering archive/SharedApp.Rendering.archived-2025-11-13/
```

### 2.4 Create Archive README

**File**: `dotnet/archive/SharedApp.Rendering.archived-2025-11-13/README.md` (new)

```markdown
# Archived: SharedApp.Rendering

**Archived**: 2025-11-13 (RFC-007 Phase 6)
**Original Location**: `dotnet/shared-app/Rendering/`

## Why Archived?

This code was split into the new domain-driven architecture:

### Map-Specific Code → Map.Rendering

- `SkiaMapRasterizer.cs` → `Map.Rendering/SkiaMapRasterizer.cs`
- `MapDataRenderer.cs` → `Map.Rendering/BrailleMapRenderer.cs`
- Map-specific rendering logic

### Shared Code → Shared.Rendering

- `Tiles/ITileSource.cs` → `Shared.Rendering/Tiles/ITileSource.cs`
- `Tiles/TileAssembler.cs` → `Shared.Rendering/Tiles/TileAssembler.cs`
- `Tiles/TileCache.cs` → `Shared.Rendering/Tiles/TileCache.cs`
- Generic rendering utilities

### Dungeon Code → Dungeon.Rendering

- Dungeon-specific rendering is now in `Dungeon.Rendering/`

## Files Archived

~16 files from the old flat rendering structure:

- SkiaMapRasterizer.cs
- MapDataRenderer.cs
- NavigatorAdapter.cs
- RenderLayout.cs
- Viewport.cs
- Camera.cs
- IRenderer.cs
- Tiles/MapTileSource.cs
- etc.

## Migration Completed

All functionality has been migrated to the new architecture. This archive is kept for:

- Historical reference
- Understanding evolution of rendering architecture
- Recovering any accidentally deleted utility code

## See Also

- [RFC-007](../../docs/rfcs/007-consolidate-rendering-projects.md)
- [New Map.Rendering](../Map/PigeonPea.Map.Rendering/)
- [New Shared.Rendering](../Shared/PigeonPea.Shared.Rendering/)
```

### 2.5 Update .gitignore (Optional)

Add to `.gitignore` if you want to keep the archive out of version control:

```gitignore
# Archived old rendering code
dotnet/archive/SharedApp.Rendering.archived-*/
```

**Recommendation**: Keep it in version control initially, remove later if confirmed not needed.

---

## Task 3: Remove Placeholder Files

**Priority**: P1
**Estimated Time**: 15 minutes
**Dependencies**: None

### 3.1 Delete Class1.cs Files

**Command**:

```bash
cd dotnet

# Dungeon placeholders
rm Dungeon/PigeonPea.Dungeon.Control/Class1.cs
rm Dungeon/PigeonPea.Dungeon.Core/Class1.cs
rm Dungeon/PigeonPea.Dungeon.Rendering/Class1.cs

# Map placeholders
rm Map/PigeonPea.Map.Control/Class1.cs
rm Map/PigeonPea.Map.Core/Class1.cs
rm Map/PigeonPea.Map.Rendering/Class1.cs
```

### 3.2 Verify Build Still Succeeds

```bash
dotnet build dotnet/PigeonPea.sln
```

Expected: 0 errors, 0 warnings.

---

## Task 4: Update Architecture Documentation

**Priority**: P0
**Estimated Time**: 3-4 hours
**Dependencies**: None

### 4.1 Create Domain Organization Guide

**File**: `docs/architecture/domain-organization.md` (new)

```markdown
# Domain-Driven Architecture Organization

**Last Updated**: 2025-11-13
**Status**: Current (Post RFC-007 implementation)

## Overview

PigeonPea uses a domain-driven architecture with two primary domains: **Map** and **Dungeon**. Each domain follows a **Core/Control/Rendering** trinity pattern.

## Domain Structure
```

dotnet/
├── Map/ ← World Map Domain
│ ├── PigeonPea.Map.Core/ # Data models, generation, domain logic
│ ├── PigeonPea.Map.Control/ # Navigation, interaction (Mapsui)
│ └── PigeonPea.Map.Rendering/ # Visualization (Skia, entities)
│
├── Dungeon/ ← Dungeon Exploration Domain
│ ├── PigeonPea.Dungeon.Core/ # Data models, generation (GoRogue)
│ ├── PigeonPea.Dungeon.Control/# FOV, pathfinding, navigation
│ └── PigeonPea.Dungeon.Rendering/ # Visualization (tiles, entities)
│
├── Shared/ ← Shared Infrastructure
│ ├── PigeonPea.Shared.ECS/ # Arch ECS components, systems
│ └── PigeonPea.Shared.Rendering/ # Generic rendering utilities
│
├── console-app/ ← Terminal.Gui Console App
└── windows-app/ ← Avalonia Desktop App

```

## Core/Control/Rendering Trinity

Each domain follows the same layering pattern:

### Core Layer
- **Purpose**: Data models, generation algorithms, domain logic
- **Dependencies**: Minimal (only external generation libraries)
- **Examples**:
  - `Map.Core`: MapData, IMapGenerator, FantasyMapGeneratorAdapter, MapColor
  - `Dungeon.Core`: DungeonData, IDungeonGenerator, BasicDungeonGenerator

### Control Layer
- **Purpose**: Navigation, interaction, input handling, ViewModels
- **Dependencies**: Core + UI frameworks
- **Examples**:
  - `Map.Control`: MapNavigator (Mapsui), MapWorldManager
  - `Dungeon.Control`: FovCalculator, PathfindingService, DungeonWorldManager

### Rendering Layer
- **Purpose**: Visualization, drawing to screen/console
- **Dependencies**: Core + Shared.Rendering
- **Examples**:
  - `Map.Rendering`: SkiaMapRasterizer, BrailleMapRenderer, MapEntityRenderer
  - `Dungeon.Rendering`: SkiaDungeonRasterizer, BrailleDungeonRenderer, EntityRenderer

## Dependency Flow

```

Console/Desktop Apps
↓
Control Layer (ViewModels, Navigation)
↓
Rendering Layer (Visualization)
↓
Core Layer (Data, Logic)
↓
External Libraries (FantasyMapGenerator, GoRogue, Arch)
↓
Shared Infrastructure (ECS, Rendering Utilities)

```

**Key Principle**: Higher layers can depend on lower layers, but not vice versa.

## Arch ECS Integration

Each domain manages its own ECS world for entities:

### Map Domain ECS
- **World Manager**: `MapWorldManager`
- **Entities**: Cities, markers, points of interest
- **Components**: Position, Sprite, CityData, MarkerData, MapEntityTag
- **Renderer**: `MapEntityRenderer`

### Dungeon Domain ECS
- **World Manager**: `DungeonWorldManager`
- **Entities**: Player, monsters, items
- **Components**: Position, Sprite, Health, Name, PlayerTag, MonsterTag, ItemTag
- **Renderer**: `EntityRenderer`

### Shared ECS
- **Location**: `Shared.ECS/Components/`
- **Reusable Components**: Position, Sprite, Health, Name, Renderable, Description
- **Systems**: RenderingSystem (query helpers)

## Library Encapsulation

External libraries are wrapped behind clean interfaces:

| Library | Used By | Wrapper/Adapter | Exposed To |
|---------|---------|----------------|------------|
| FantasyMapGenerator.Core | Map.Core | FantasyMapGeneratorAdapter | Map domain only |
| GoRogue | Dungeon.Core/Control | (Direct usage, could add adapter) | Dungeon domain only |
| Mapsui | Map.Control | Direct usage | Map.Control only |
| Arch | Shared.ECS | Direct usage | All domains (via Shared.ECS) |
| SkiaSharp | Shared.Rendering, *.Rendering | Direct usage | Rendering layers only |

**Principle**: External library types **never** leak into application code. All cross-domain data uses internal types (MapData, DungeonData, etc.).

## Adding a New Domain

To add a new domain (e.g., Battle, Overworld):

1. **Create Projects**:
```

dotnet/Battle/
├── PigeonPea.Battle.Core/
├── PigeonPea.Battle.Control/
└── PigeonPea.Battle.Rendering/

```

2. **Define Core Types**:
- Data models (BattleData)
- Interfaces (IBattleGenerator)
- Domain logic

3. **Implement Control**:
- Navigation/interaction logic
- ECS world manager (BattleWorldManager)
- ViewModels

4. **Implement Rendering**:
- Visualization (SkiaBattleRasterizer)
- Entity rendering (BattleEntityRenderer)

5. **Add ECS Components**:
- Shared components (reuse Position, Sprite, Health)
- Domain-specific (BattleEntityTag, BattleData)

6. **Integrate in Apps**:
- Console: Add BattlePanelView
- Desktop: Add BattleView.axaml

## Examples

See:
- [Map Rendering Guide](map-rendering.md)
- [Dungeon System Guide](dungeon-system.md)
- [ECS Usage Examples](../examples/ecs-usage.md)
- [RFC-007](../rfcs/007-consolidate-rendering-projects.md)

## Migration from Old Architecture

See:
- [Migration Guide: FMG.Rendering → Map.Rendering](../migration/fmg-rendering-to-map-rendering.md)
- [Migration Guide: SharedApp.Rendering → Domain Structure](../migration/sharedapp-to-domains.md)
```

### 4.2 Update Existing Architecture Docs

**File**: `docs/architecture/ARCHITECTURE_PLAN.md` (update)

Add a section at the top:

```markdown
# Architecture Plan

> ⚠️ **Note**: This document is historical. Current architecture is documented in:
>
> - [Domain Organization](domain-organization.md) - **START HERE**
> - [Map Rendering](ARCHITECTURE_MAP_RENDERING.md)
> - [RFC-007: Domain-Driven Architecture](../rfcs/007-consolidate-rendering-projects.md)

---

[... existing content ...]
```

**File**: `docs/architecture/ARCHITECTURE_MAP_RENDERING.md` (update)

Add note about current status:

```markdown
# Map Rendering Architecture

> ✅ **Status**: Implemented as of RFC-007 Phase 3 (2025-11-13)
>
> - **Location**: `dotnet/Map/PigeonPea.Map.Rendering/`
> - **Replaces**: Old `SharedApp.Rendering` (archived)

---

[... existing content, update paths to new locations ...]
```

### 4.3 Create Migration Guides

**File**: `docs/migration/fmg-rendering-to-map-rendering.md` (new)

````markdown
# Migration Guide: FantasyMapGenerator.Rendering → Map.Rendering

**Audience**: Developers using old FMG.Rendering APIs
**Date**: 2025-11-13 (RFC-007 Phase 6)

## Overview

This guide helps you migrate from deprecated `FantasyMapGenerator.Rendering` to the new `PigeonPea.Map.Rendering`.

## Quick Reference

| Old (Deprecated)               | New (Current)                        | Notes                            |
| ------------------------------ | ------------------------------------ | -------------------------------- |
| `TerrainRenderer.Render()`     | `SkiaMapRasterizer.Render()`         | New API uses viewport parameters |
| `ColorSchemes.GetBiomeColor()` | `MapColor.ColorForCell()`            | Moved to Map.Core/Domain/        |
| `SmoothTerrainRenderer`        | Built into `SkiaMapRasterizer`       | No separate class                |
| `TileSource`                   | `Shared.Rendering.Tiles.ITileSource` | Now generic interface            |

## Example Migrations

### Example 1: Basic Map Rendering

**Before (Deprecated)**:

```csharp
using FantasyMapGenerator.Rendering;

var renderer = new TerrainRenderer();
var bitmap = renderer.RenderMap(mapData, width: 800, height: 600);
```
````

**After (Current)**:

```csharp
using PigeonPea.Map.Rendering;
using PigeonPea.Map.Core;

var raster = SkiaMapRasterizer.Render(
    mapData,              // MapData from Map.Core (wrapper around FMG's MapData)
    viewportX: 0,
    viewportY: 0,
    viewportWidth: mapData.Width,
    viewportHeight: mapData.Height,
    pixelsPerCell: 4,     // 800px / 200 cells = 4px per cell
    biomeColors: true,
    rivers: true
);

// raster.Rgba contains RGBA byte array
// raster.WidthPx = 800
// raster.HeightPx = 600
```

### Example 2: Getting Cell Colors

**Before**:

```csharp
using FantasyMapGenerator.Rendering;

var color = ColorSchemes.GetBiomeColor(cell, biomes);
```

**After**:

```csharp
using PigeonPea.Map.Core.Domain;

var (r, g, b) = MapColor.ColorForCell(mapData, cell, biomeColors: true);
```

### Example 3: Tile-Based Rendering

**Before**:

```csharp
using FantasyMapGenerator.Rendering.Tiles;

var tileSource = new MapTileSource(mapData);
var tile = tileSource.GetTile(tileX, tileY, zoom);
```

**After**:

```csharp
using PigeonPea.Shared.Rendering.Tiles;
using PigeonPea.Map.Rendering;

// Tiles are now handled at application level
// See Shared.Rendering/Tiles/ITileSource for interface
```

## See Also

- [Map.Rendering API Reference](../api/map-rendering.md)
- [Domain Organization](../architecture/domain-organization.md)

````

### 4.4 Update CLAUDE.md

**File**: `CLAUDE.md` (update)

Add a new section after the "Project-Specific Guidelines" section:

```markdown
## Architecture Overview

### Domain-Driven Organization

This project uses domain-driven design with two primary domains:

- **Map Domain** (`dotnet/Map/`): World map generation, navigation, rendering
- **Dungeon Domain** (`dotnet/Dungeon/`): Dungeon generation, FOV, pathfinding, rendering

Each domain follows a **Core/Control/Rendering** trinity:
- **Core**: Data models, generation algorithms (e.g., `Map.Core`, `Dungeon.Core`)
- **Control**: Navigation, interaction, ViewModels (e.g., `Map.Control`, `Dungeon.Control`)
- **Rendering**: Visualization (e.g., `Map.Rendering`, `Dungeon.Rendering`)

**Shared Infrastructure**:
- `Shared.ECS`: Arch ECS components and systems
- `Shared.Rendering`: Generic rendering utilities (tiles, primitives)

### Arch ECS

Each domain manages its own ECS world:
- **Map World**: Cities, markers, points of interest
- **Dungeon World**: Player, monsters, items

ECS components are in `Shared.ECS/Components/`.

### External Libraries

External libraries are encapsulated within domains:
- **FantasyMapGenerator.Core**: Used by `Map.Core` (via adapter)
- **GoRogue**: Used by `Dungeon.Core` and `Dungeon.Control`
- **Mapsui**: Used by `Map.Control` for navigation
- **Arch**: Used via `Shared.ECS`

**Important**: External library types should **not** leak outside their domain. Use internal wrappers (e.g., `MapData` wraps FMG's MapData).

### Deprecated/Archived Code

The following directories are deprecated/archived:
- `_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/` - See README-DEPRECATED.md
- `dotnet/archive/SharedApp.Rendering.archived-*/` - Archived old rendering code

Do not reference these in new code.

For detailed architecture documentation, see:
- [Domain Organization](docs/architecture/domain-organization.md)
- [RFC-007](docs/rfcs/007-consolidate-rendering-projects.md)

[... rest of existing content ...]
````

---

## Task 5: Create ECS Usage Examples

**Priority**: P2
**Estimated Time**: 2-3 hours
**Dependencies**: None

### 5.1 Create Examples Directory

```bash
mkdir -p docs/examples/
```

### 5.2 Create ECS Usage Guide

**File**: `docs/examples/ecs-usage.md` (new)

````markdown
# ECS Usage Examples

**Last Updated**: 2025-11-13
**Framework**: Arch ECS v2.0.0

## Overview

PigeonPea uses Arch ECS for entity management in both Map and Dungeon domains. Each domain has its own ECS world.

## Creating Entities

### Dungeon Domain: Player, Monsters, Items

```csharp
using Arch.Core;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

// 1. Generate dungeon
var generator = new BasicDungeonGenerator();
var dungeon = generator.Generate(width: 80, height: 60, seed: 12345);

// 2. Create world manager
using var worldManager = new DungeonWorldManager(dungeon);

// 3. Create player
var player = worldManager.CreatePlayer(x: 10, y: 10, name: "Hero");

// 4. Spawn monsters
worldManager.SpawnMonster(x: 15, y: 15, monsterType: "goblin", health: 20);
worldManager.SpawnMonster(x: 20, y: 20, monsterType: "orc", health: 30);

// 5. Spawn random monsters
worldManager.SpawnRandomMonsters(count: 10, rng: new Random());

// 6. Spawn items
worldManager.SpawnItem(x: 12, y: 12, itemType: "potion");
worldManager.SpawnItem(x: 18, y: 18, itemType: "gold");
```
````

### Map Domain: Cities, Markers

```csharp
using PigeonPea.Map.Core;
using PigeonPea.Map.Control.WorldManager;

// 1. Generate map
var mapGenerator = new FantasyMapGeneratorAdapter();
var mapData = mapGenerator.Generate(new MapGenerationSettings { Seed = 42 });

// 2. Create world manager
using var worldManager = new MapWorldManager(mapData);

// 3. Create cities
worldManager.CreateCity(x: 100, y: 100, cityName: "Capital", population: 50000, cultureId: "empire");
worldManager.CreateCity(x: 200, y: 150, cityName: "Port City", population: 20000, cultureId: "coastal");

// 4. Create markers
worldManager.CreateMarker(x: 150, y: 120, markerType: "dungeon", title: "Dark Caves", discovered: false);
worldManager.CreateMarker(x: 180, y: 130, markerType: "quest", title: "Dragon's Lair", discovered: true);

// 5. Create random dungeon markers
worldManager.CreateRandomDungeonMarkers(count: 5, rng: new Random());
```

## Querying Entities

### Basic Queries

```csharp
using Arch.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

// Query all monsters
var monsterQuery = new QueryDescription().WithAll<Position, MonsterTag>();

worldManager.World.Query(in monsterQuery, (Entity entity, ref Position pos) =>
{
    Console.WriteLine($"Monster at ({pos.X}, {pos.Y})");
});

// Query all entities with health
var healthQuery = new QueryDescription().WithAll<Health>();

worldManager.World.Query(in healthQuery, (Entity entity, ref Health health) =>
{
    Console.WriteLine($"Entity health: {health.Current}/{health.Maximum}");
});
```

### Advanced Queries: Spatial Filtering

```csharp
// Find all monsters within 10 tiles of player
var playerPos = worldManager.GetPlayerPosition();
if (playerPos.HasValue)
{
    var query = new QueryDescription().WithAll<Position, MonsterTag>();
    var nearbyMonsters = new List<(Entity entity, Position pos)>();

    worldManager.World.Query(in query, (Entity entity, ref Position pos) =>
    {
        int distance = Math.Abs(pos.X - playerPos.Value.X) + Math.Abs(pos.Y - playerPos.Value.Y);
        if (distance <= 10)
        {
            nearbyMonsters.Add((entity, pos));
        }
    });

    Console.WriteLine($"Found {nearbyMonsters.Count} nearby monsters");
}
```

### Query with Multiple Components

```csharp
// Find all low-health monsters
var query = new QueryDescription().WithAll<Position, Health, MonsterTag>();

worldManager.World.Query(in query, (Entity entity, ref Position pos, ref Health health) =>
{
    if (health.HealthPercent < 0.3f)
    {
        Console.WriteLine($"Low health monster at ({pos.X}, {pos.Y}): {health.Current}/{health.Maximum}");
    }
});
```

## Modifying Entities

### Moving Entities

```csharp
// Move player
if (worldManager.PlayerEntity.HasValue)
{
    bool moved = worldManager.TryMoveEntity(
        worldManager.PlayerEntity.Value,
        newX: 11,
        newY: 10
    );

    if (moved)
        Console.WriteLine("Player moved successfully");
    else
        Console.WriteLine("Move blocked (wall or occupied)");
}
```

### Damaging Entities

```csharp
// Damage a monster
var query = new QueryDescription().WithAll<Health, MonsterTag>();

worldManager.World.Query(in query, (Entity entity, ref Health health) =>
{
    if (health.Current > 0)
    {
        health.Current -= 10;  // Deal 10 damage
        Console.WriteLine($"Monster damaged! Health: {health.Current}/{health.Maximum}");
    }
});

// Clean up dead monsters
worldManager.CleanupDeadMonsters();
```

### Modifying Components Directly

```csharp
if (worldManager.PlayerEntity.HasValue)
{
    var player = worldManager.PlayerEntity.Value;

    // Get current health
    var health = worldManager.World.Get<Health>(player);

    // Heal player
    health.Current = Math.Min(health.Current + 20, health.Maximum);

    // Update component
    worldManager.World.Set(player, health);

    Console.WriteLine($"Player healed! Health: {health.Current}/{health.Maximum}");
}
```

## Rendering with ECS

### Rendering Dungeon Entities

```csharp
using PigeonPea.Dungeon.Rendering;
using PigeonPea.Dungeon.Control;

// 1. Calculate FOV
var fov = new FovCalculator(dungeon);
var visible = fov.ComputeVisible(playerX, playerY, range: 10);

// 2. Render dungeon base
var raster = SkiaDungeonRasterizer.Render(
    dungeon,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: 40,
    viewportHeight: 30,
    pixelsPerCell: 16,
    fov: visible
);

// 3. Render entities on top
EntityRenderer.RenderEntities(
    worldManager.World,
    raster.Rgba,
    raster.WidthPx,
    raster.HeightPx,
    viewportX: 0,
    viewportY: 0,
    pixelsPerCell: 16,
    fov: visible  // Only render visible entities
);

// Now raster.Rgba contains dungeon + entities
```

### Rendering Map Entities

```csharp
using PigeonPea.Map.Rendering;

// 1. Render map base
var mapRaster = SkiaMapRasterizer.Render(
    mapData,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: mapData.Width,
    viewportHeight: mapData.Height,
    pixelsPerCell: 4,
    biomeColors: true,
    rivers: true
);

// 2. Render cities and markers
MapEntityRenderer.RenderEntities(
    worldManager.World,
    mapRaster.Rgba,
    mapRaster.WidthPx,
    mapRaster.HeightPx,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: mapData.Width,
    viewportHeight: mapData.Height,
    zoom: 1.0
);
```

## Best Practices

### 1. Always Dispose World Managers

```csharp
// Good: Using statement
using var worldManager = new DungeonWorldManager(dungeon);

// Bad: Manual disposal (easy to forget)
var worldManager = new DungeonWorldManager(dungeon);
// ... use world manager ...
worldManager.Dispose();  // Easy to forget!
```

### 2. Use World Managers, Not Raw World.Create()

```csharp
// Good: Use world manager
using var worldManager = new DungeonWorldManager(dungeon);
var player = worldManager.CreatePlayer(x, y, name);

// Bad: Direct ECS world manipulation
var world = World.Create();
var player = world.Create<Position, Sprite, Health, Name, PlayerTag, DungeonEntityTag, Renderable>();
// ... manual component setup (error-prone)
```

### 3. Check Entity Existence Before Access

```csharp
// Good: Check if entity is alive
if (worldManager.PlayerEntity.HasValue && worldManager.World.IsAlive(worldManager.PlayerEntity.Value))
{
    var pos = worldManager.World.Get<Position>(worldManager.PlayerEntity.Value);
    // Use pos...
}

// Bad: Assume entity exists
var pos = worldManager.World.Get<Position>(worldManager.PlayerEntity.Value);  // May throw!
```

### 4. Prefer Helper Methods Over Direct Queries

```csharp
// Good: Use helper method
var playerPos = worldManager.GetPlayerPosition();

// Less good: Direct query
var query = new QueryDescription().WithAll<Position, PlayerTag>();
Position? playerPos = null;
worldManager.World.Query(in query, (ref Position pos) => playerPos = pos);
```

### 5. Batch Entity Creation

```csharp
// Good: Use batch methods
worldManager.SpawnRandomMonsters(count: 50, rng: new Random());

// Less efficient: Individual spawns in loop
for (int i = 0; i < 50; i++)
{
    worldManager.SpawnMonster(x, y, "goblin", 20);  // Many collision checks
}
```

## Performance Tips

### 1. Cache Queries

```csharp
// Cache query description if used multiple times per frame
private static readonly QueryDescription MonsterQuery = new QueryDescription().WithAll<Position, MonsterTag>();

// Reuse in render loop
worldManager.World.Query(in MonsterQuery, (ref Position pos) => { /* ... */ });
```

### 2. Limit Query Scope with FOV

```csharp
// Good: Only process visible entities
var query = new QueryDescription().WithAll<Position, Sprite, DungeonEntityTag>();
worldManager.World.Query(in query, (ref Position pos, ref Sprite sprite) =>
{
    if (fov[pos.Y, pos.X])  // Only process if visible
    {
        // Render entity
    }
});
```

### 3. Cleanup Dead Entities

```csharp
// Regularly cleanup dead entities to avoid querying dead objects
worldManager.CleanupDeadMonsters();
```

## See Also

- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [Domain Organization](../architecture/domain-organization.md)
- [DungeonWorldManager API](../api/dungeon-world-manager.md)
- [MapWorldManager API](../api/map-world-manager.md)

````

---

## Task 6: Fix Failing Tests

**Priority**: P1
**Estimated Time**: 2-3 hours
**Dependencies**: None

### 6.1 Investigate Test Failures

Run tests with detailed output:

```bash
cd dotnet
dotnet test Dungeon.Tests/ --logger "console;verbosity=detailed" --filter "FovBlockingTests.ComputeVisible_WallsBlockLos"
````

### 6.2 Fix FOV Blocking Test

**File**: `dotnet/Dungeon.Tests/FovBlockingTests.cs`

The test likely expects walls to block LOS, but the current implementation may have an edge case.

**Common issue**: Off-by-one error in Bresenham LOS or FOV calculation.

**Investigation**:

1. Check if the test setup creates walls correctly
2. Verify FOV calculator respects opaque tiles
3. Check Bresenham implementation for edge cases

**Potential fix**: Update `FovCalculator.cs` to handle wall blocking correctly.

### 6.3 Fix Weighted Pathfinding Test

**File**: `dotnet/Dungeon.Tests/PathfindingWeightedCornerTests.cs`

The test likely expects pathfinding to prefer cheaper tiles when using weighted costs.

**Common issue**: Weighted cost not properly integrated into A\* algorithm.

**Investigation**:

1. Verify `PathfindingService` accepts and uses tileCost function
2. Check if A\* priority calculation includes tileCost
3. Ensure weighted cost affects path selection

**Potential fix**: Update `PathfindingService.FindPath()` to properly weight costs.

### 6.4 Verify All Tests Pass

After fixes:

```bash
dotnet test dotnet/Dungeon.Tests/
```

Expected: 24/24 passing (100%).

---

## Task 7: Final Polish

**Priority**: P2
**Estimated Time**: 1-2 hours
**Dependencies**: All previous tasks

### 7.1 Update README.md

Add section about architecture:

**File**: `README.md` (update)

```markdown
## Architecture

PigeonPea uses a **domain-driven architecture** with two primary domains:

- **Map Domain**: World map generation and navigation
- **Dungeon Domain**: Dungeon exploration with FOV and pathfinding

Each domain follows a **Core/Control/Rendering** pattern. See [Architecture Guide](docs/architecture/domain-organization.md) for details.

### Technology Stack

- **.NET 9.0**: Core framework
- **Arch ECS**: Entity Component System
- **FantasyMapGenerator**: Procedural world map generation
- **GoRogue**: Dungeon generation, FOV, pathfinding
- **Mapsui**: Map navigation UI
- **SkiaSharp**: 2D graphics rendering
- **Terminal.Gui**: Console UI
- **Avalonia**: Desktop UI

[... rest of README ...]
```

### 7.2 Add CHANGELOG Entry

**File**: `CHANGELOG.md` (create if doesn't exist)

```markdown
# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added (RFC-007 Implementation)

- Domain-driven architecture with Map and Dungeon domains
- Arch ECS integration for entity management
- Separate ECS worlds for Map (cities, markers) and Dungeon (player, monsters, items)
- World managers for clean ECS API (DungeonWorldManager, MapWorldManager)
- Complete dungeon rendering with FOV-based entity visibility
- Map entity rendering (cities, markers)
- Comprehensive test coverage (24 tests)

### Changed

- Reorganized rendering from flat structure to domain-specific projects
- Map rendering moved from `SharedApp.Rendering` to `Map.Rendering`
- Dungeon rendering split from shared code into `Dungeon.Rendering`
- Generic utilities moved to `Shared.Rendering`
- Updated architecture documentation

### Deprecated

- `FantasyMapGenerator.Rendering` (use `Map.Rendering` instead)

### Removed

- Old `SharedApp.Rendering` (archived to `dotnet/archive/`)
- Placeholder `Class1.cs` files

## [Previous Versions]

...
```

### 7.3 Verify Solution Quality

Run complete verification:

```bash
# Build
dotnet build dotnet/PigeonPea.sln

# All tests
dotnet test

# Check for warnings
dotnet build dotnet/PigeonPea.sln -warnaserror

# Format check (if using dotnet format)
dotnet format dotnet/PigeonPea.sln --verify-no-changes
```

Expected: All pass.

---

## Success Criteria

### Build & Test

- [ ] `dotnet build dotnet/PigeonPea.sln` succeeds (0 errors, 0 warnings)
- [ ] All tests pass (24/24 = 100%)
- [ ] No placeholder `Class1.cs` files exist
- [ ] No active references to deprecated projects

### Deprecation & Cleanup

- [ ] `FantasyMapGenerator.Rendering` marked as deprecated (README-DEPRECATED.md exists)
- [ ] `SharedApp.Rendering` archived to `dotnet/archive/`
- [ ] Archive README explains what was moved where
- [ ] No active code references old projects

### Documentation

- [ ] `docs/architecture/domain-organization.md` created
- [ ] `docs/examples/ecs-usage.md` created
- [ ] Migration guides created
- [ ] Existing architecture docs updated with notes
- [ ] `CLAUDE.md` updated with architecture overview
- [ ] `README.md` updated with architecture section
- [ ] `CHANGELOG.md` created/updated

### Code Quality

- [ ] All failing tests fixed
- [ ] Build produces 0 warnings
- [ ] No deprecated code referenced in active projects
- [ ] Documentation is accurate and up-to-date

---

## Verification Commands

After implementation, run these commands:

```bash
# Build
dotnet build dotnet/PigeonPea.sln

# All tests
dotnet test

# Check for placeholder files (should find 0 in Map/Dungeon)
find dotnet/Map dotnet/Dungeon -name "Class1.cs" -not -path "*/obj/*"

# Check for references to deprecated projects
grep -r "FantasyMapGenerator.Rendering\|SharedApp.Rendering" dotnet/ \
  --include="*.cs" \
  --include="*.csproj" \
  --exclude-dir=obj \
  --exclude-dir=bin \
  --exclude-dir=archive \
  --exclude-dir=_lib

# Verify documentation exists
ls docs/architecture/domain-organization.md
ls docs/examples/ecs-usage.md
ls dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/README-DEPRECATED.md
ls dotnet/archive/SharedApp.Rendering.archived-*/README.md
```

Expected: All checks pass, no errors, documentation files exist.

---

## Timeline Estimate

**Total**: 10-15 hours

- **Task 1**: Deprecate FMG.Rendering (1-2 hours)
- **Task 2**: Archive SharedApp.Rendering (2-3 hours)
- **Task 3**: Remove placeholders (15 minutes)
- **Task 4**: Update documentation (3-4 hours)
- **Task 5**: Create ECS examples (2-3 hours)
- **Task 6**: Fix failing tests (2-3 hours)
- **Task 7**: Final polish (1-2 hours)

**Critical path**: Tasks 1-4 (documentation is most time-consuming).

---

## Notes for Implementation

### What NOT to Delete

**Keep these**:

- `_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Core/` - Still used by Map.Core
- `_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Rendering/` - Deprecated but kept for reference
- `dotnet/archive/` - Historical reference

**Only delete**:

- Placeholder `Class1.cs` files
- Active references to deprecated code (if any)

### Documentation Priority

Focus documentation efforts on:

1. **Domain organization guide** (high value for new developers)
2. **ECS usage examples** (commonly needed)
3. **Migration guides** (helps with transition)
4. **README updates** (first impression)

### Test Fixes

If test fixes are complex:

- Document the issue in test comments
- Consider skipping tests temporarily with `[Fact(Skip = "Known issue - investigating")]`
- File issues for follow-up
- Don't block Phase 6 completion on edge-case test fixes

---

## Final Checklist

Before marking Phase 6 complete:

- [ ] Old projects deprecated/archived
- [ ] Placeholder files removed
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] All documentation created/updated
- [ ] Tests pass (or known issues documented)
- [ ] CHANGELOG updated
- [ ] README updated
- [ ] No broken links in documentation
- [ ] Code follows project patterns
- [ ] Ready for Phase 7 (if planned) or production

---

**Implementation Status**: Ready to begin
**Prerequisites**: Phase 5 complete ✅
**Next Phase**: Phase 7 (Polish and examples) or Production Release
