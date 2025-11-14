# Domain-Driven Architecture Organization

**Last Updated**: 2025-11-13  
**Status**: Current (Post RFC-007 implementation)

## Overview

PigeonPea is organized around two primary domains—**Map** and **Dungeon**—with a shared infrastructure layer that provides ECS components and rendering utilities. Each domain follows a repeatable **Core / Control / Rendering** pattern so code is discoverable, testable, and replaceable.

```
dotnet/
├── Map/
│   ├── PigeonPea.Map.Core/
│   ├── PigeonPea.Map.Control/
│   └── PigeonPea.Map.Rendering/
│
├── Dungeon/
│   ├── PigeonPea.Dungeon.Core/
│   ├── PigeonPea.Dungeon.Control/
│   └── PigeonPea.Dungeon.Rendering/
│
├── Shared/
│   ├── PigeonPea.Shared.ECS/
│   └── PigeonPea.Shared.Rendering/
│
├── console-app/
└── windows-app/
```

## Core / Control / Rendering Trinity

| Layer | Purpose | Example (Map) | Example (Dungeon) |
| --- | --- | --- | --- |
| **Core** | Pure domain logic, data models, generation | `MapData`, `IMapGenerator`, `FantasyMapGeneratorAdapter` | `DungeonData`, `IDungeonGenerator`, `BasicDungeonGenerator` |
| **Control** | ViewModels, navigation, interaction, orchestration | `MapControlViewModel`, `MapNavigator` | `DungeonControlViewModel`, `DungeonWorldManager` |
| **Rendering** | Visualization targets (Skia, Braille, Sixel, etc.) | `SkiaMapRasterizer`, `BrailleMapRenderer`, map overlays | `BrailleDungeonRenderer`, `EntityRenderer`, lighting/FOV renderers |

### Dependency flow

```
Console/Desktop Apps
        ↓
    Control Layer
        ↓
   Rendering Layer
        ↓
     Core Layer
        ↓
  External Libraries (FantasyMapGenerator, GoRogue, Arch, Mapsui)
        ↓
Shared Infrastructure (ECS, Rendering Utilities)
```

**Rule**: Higher layers can depend on lower layers, but not vice versa. Rendering never depends on Control, Core never references UI, and external libraries are wrapped so they do not leak outside their domain.

## Map Domain

- **Location**: `dotnet/Map/`
- **Core**: MapData modeling, adapters over FantasyMapGenerator, color helpers.
- **Control**: Mapsui navigation, map selection workflows, ReactiveUI ViewModels.
- **Rendering**: Skia-based rasterization, Braille/ASCII renderers, tiles for iTerm2/Sixel outputs.
- **ECS world**: Cities, markers, POIs. Manager: `MapWorldManager`.

## Dungeon Domain

- **Location**: `dotnet/Dungeon/`
- **Core**: GoRogue-driven dungeon layout generation, pathfinding interfaces.
- **Control**: FOV calculators, entity systems, `DungeonWorldManager` for gameplay state.
- **Rendering**: Tile/ASCII/Braille renderers, Skia overlays, lighting.
- **ECS world**: Player, monsters, items, effects.

## Shared Infrastructure

| Project | Purpose |
| --- | --- |
| `PigeonPea.Shared.ECS` | Arch components (Position, Renderable, Health, etc.), tags, helper systems |
| `PigeonPea.Shared.Rendering` | Rendering contracts (`IRenderer`, `IRenderTarget`), primitives, tiles, converters |
| `PigeonPea.Shared.ViewModels` *(optional)* | Common ViewModel helpers (layers, viewport state) |

### Arch ECS integration

Each domain maintains its own Arch `World`. Shared components live in `Shared.ECS/Components`. Typical pattern:

```csharp
var query = new QueryDescription().WithAll<Position, Renderable>();
world.Query(in query, (Entity entity, ref Position pos, ref Renderable rend) =>
{
    if (!viewport.Contains(pos.Point)) return;
    renderer.DrawTile(pos.Point.X, pos.Point.Y, rend.ToTile());
});
```

Reuse `QueryDescription`, limit scope via FOV, and clean up entities regularly (see `DungeonWorldManager.CleanupDeadMonsters`).

## Library Encapsulation

| Library | Wrapped By | Visible To |
| --- | --- | --- |
| FantasyMapGenerator.Core | `Map.Core` adapter | Map domain only |
| GoRogue | Dungeon Core/Control services | Dungeon domain only |
| Mapsui | `Map.Control` | Map Control/UI |
| Arch | `Shared.ECS` | All domains via shared components |
| SkiaSharp | Rendering projects | Rendering layers only |

## Adding a New Domain

1. Scaffold `dotnet/<Domain>/PigeonPea.<Domain>.{Core,Control,Rendering}/`.
2. Define domain models/interfaces in Core.
3. Implement Control layer (world manager, ViewModels, navigation).
4. Implement Rendering layer (Skia/Braille/etc.).
5. Register ECS components/tags in `Shared.ECS` if reusable.
6. Integrate with console/desktop apps.

## Migration References

- [Migration Guide: FMG.Rendering → Map.Rendering](docs/migrations/fmg-rendering-to-map-rendering.md)
- [Migration Guide: SharedApp.Rendering → Domain Structure](docs/migrations/sharedapp-to-domains.md)
- `docs/rfcs/RFC-007-PHASE-6-INSTRUCTIONS.md` (Phase 6 playbook)

## Related Documents

- [Architecture Plan](ARCHITECTURE_PLAN.md)
- [Map Rendering Architecture](ARCHITECTURE_MAP_RENDERING.md)
- [RFC-007: Consolidate Rendering Projects](../rfcs/007-consolidate-rendering-projects.md)
- [ECS Usage Examples](../examples/ecs-usage.md)
