# RFC-007: Domain-Driven Architecture Reorganization (Map & Dungeon)

## Status

**Status**: Proposed (Revised 2025-11-13)
**Created**: 2025-11-13
**Author**: Claude Code (based on architecture review)
**Last Updated**: 2025-11-13 (revised for domain-first organization)

## Summary

Reorganize the rendering and control architecture from a flat structure into a **domain-driven design** with separate **Map** and **Dungeon** domains, each following a **Core/Control/Rendering** trinity pattern. This eliminates duplication, clarifies architectural boundaries, enables Arch ECS integration per domain, and supports future extensibility (Battle, Overworld, etc.).

## Motivation

### Current Problems

**1. Rendering Duplication**

- **FantasyMapGenerator.Rendering** (in port) vs **SharedApp.Rendering** (production)
- Unclear which to modify
- Duplicate tile sources, color schemes
- Maintenance burden

**2. Flat Structure Limits Scalability**

- `SharedApp.Rendering/` mixes map-specific and generic rendering code
- Hard to add new domains (dungeon, battle, overworld)
- No clear separation between map and dungeon rendering concerns

**3. Multiple Worlds with Arch ECS**

- Need separate ECS worlds for map entities (cities, markers) vs dungeon entities (monsters, items)
- Current flat structure doesn't support multiple domain-specific worlds cleanly

**4. Library Integration Challenges**

- Mapsui for map navigation
- GoRogue for dungeon generation/FOV
- No clear pattern for integrating domain-specific libraries

### Goals

1. **Domain isolation**: All map code in `Map.*`, all dungeon code in `Dungeon.*`
2. **Clear layering**: Core (data) → Control (interaction) → Rendering (visualization) per domain
3. **Shared infrastructure**: Generic utilities (Braille, primitives, tiles) in `Shared.*`
4. **Arch ECS support**: Each domain owns its ECS world
5. **Library encapsulation**: Mapsui in `Map.Control`, GoRogue in `Dungeon.Core/Control`
6. **Future extensibility**: Easy to add `Battle.*`, `Overworld.*`, etc.

## Design

### Domain-First Organization

```
dotnet/
├── _lib/
│   └── fantasy-map-generator-port/
│       └── src/
│           └── FantasyMapGenerator.Core/          ← Pure generation (external lib, KEEP)
│
├── Map/                                            ← MAP DOMAIN
│   ├── PigeonPea.Map.Core/                        # Data models, interfaces
│   │   ├── PigeonPea.Map.Core.csproj
│   │   ├── MapData.cs                             # Re-export or light wrapper
│   │   ├── IMapGenerator.cs                       # Interface
│   │   ├── IMapDataSource.cs                      # Abstract data provider
│   │   └── Adapters/
│   │       └── FantasyMapGeneratorAdapter.cs      # Wraps FantasyMapGenerator.Core
│   │
│   ├── PigeonPea.Map.Control/                     # Navigation, interaction (Mapsui)
│   │   ├── PigeonPea.Map.Control.csproj
│   │   ├── MapNavigator.cs                        # Direct Mapsui.Navigator exposure
│   │   ├── MapInteractionHandler.cs               # Pan/zoom/click handlers
│   │   └── ViewModels/
│   │       └── MapControlViewModel.cs             # ReactiveUI ViewModel
│   │
│   └── PigeonPea.Map.Rendering/                   # Rendering MapData
│       ├── PigeonPea.Map.Rendering.csproj
│       ├── SkiaMapRasterizer.cs                   # MapData → RGBA tiles (from SharedApp)
│       ├── BrailleMapRenderer.cs                  # MapData → Braille (renamed from MapDataRenderer)
│       ├── ColorSchemes.cs                        # Biome/height color palettes (from RFC-010)
│       ├── SmoothMapRenderer.cs                   # Contour tracing for smooth terrain (from FMG.Rendering)
│       ├── Overlays/
│       │   ├── RiverOverlay.cs                    # River rendering
│       │   ├── BorderOverlay.cs                   # State/province borders
│       │   └── CityOverlay.cs                     # City markers
│       └── Tiles/
│           └── MapTileSource.cs                   # Implements ITileSource (merged from FMG + SharedApp)
│
├── Dungeon/                                        ← DUNGEON DOMAIN
│   ├── PigeonPea.Dungeon.Core/                    # Data models (GoRogue integration)
│   │   ├── PigeonPea.Dungeon.Core.csproj
│   │   ├── DungeonData.cs                         # Dungeon grid, walls, floor
│   │   ├── IDungeonGenerator.cs                   # Interface
│   │   └── Adapters/
│   │       └── GoRogueAdapter.cs                  # Wraps GoRogue generation
│   │
│   ├── PigeonPea.Dungeon.Control/                 # FOV, pathfinding, input
│   │   ├── PigeonPea.Dungeon.Control.csproj
│   │   ├── FovCalculator.cs                       # Wraps GoRogue FOV
│   │   ├── PathfindingService.cs                  # Wraps GoRogue pathfinding
│   │   ├── DungeonNavigator.cs                    # Grid-based navigation
│   │   └── ViewModels/
│   │       └── DungeonControlViewModel.cs         # Player position, FOV state
│   │
│   └── PigeonPea.Dungeon.Rendering/               # Rendering dungeons
│       ├── PigeonPea.Dungeon.Rendering.csproj
│       ├── TilesetRenderer.cs                     # Tileset-based rendering
│       ├── BrailleDungeonRenderer.cs              # Console dungeon (ASCII/Braille)
│       ├── FovRenderer.cs                         # Apply lighting/shadows
│       ├── EntityRenderer.cs                      # Render Arch ECS entities (monsters, items)
│       └── Tiles/
│           └── DungeonTileSource.cs               # Implements ITileSource
│
├── Shared/                                         ← SHARED INFRASTRUCTURE
│   ├── PigeonPea.Shared.Rendering/                # Generic rendering utilities
│   │   ├── PigeonPea.Shared.Rendering.csproj
│   │   ├── Primitives/
│   │   │   ├── SkiaPrimitives.cs                  # DrawPolygon, FillCircle, etc.
│   │   │   ├── ColorUtilities.cs                  # Lerp, blending, etc.
│   │   │   └── CurveSmoothing.cs                  # Spline/curve utilities (from FMG.Rendering)
│   │   ├── Text/
│   │   │   ├── BrailleConverter.cs                # RGBA → Braille (domain-agnostic)
│   │   │   ├── AsciiConverter.cs                  # RGBA → ASCII
│   │   │   └── CharacterPalette.cs                # Character sets for rendering
│   │   └── Tiles/
│   │       ├── ITileSource.cs                     # Interface (from SharedApp)
│   │       ├── TileAssembler.cs                   # Assemble tiles into viewport
│   │       └── TileCache.cs                       # Caching layer
│   │
│   ├── PigeonPea.Shared.ECS/                      # Arch ECS shared components
│   │   ├── PigeonPea.Shared.ECS.csproj
│   │   ├── Components/
│   │   │   ├── Position.cs                        # 2D/3D position
│   │   │   ├── Sprite.cs                          # Visual representation
│   │   │   ├── Renderable.cs                      # Render flags (visible, layer, etc.)
│   │   │   └── Tags/
│   │   │       ├── MapEntityTag.cs                # Tag for map entities
│   │   │       └── DungeonEntityTag.cs            # Tag for dungeon entities
│   │   └── Systems/
│   │       └── RenderingSystem.cs                 # Generic entity rendering helpers
│   │
│   └── PigeonPea.Shared.ViewModels/               # Shared base ViewModels (optional)
│       ├── PigeonPea.Shared.ViewModels.csproj
│       ├── ViewportViewModel.cs                   # Generic viewport state (if reused)
│       └── LayersViewModel.cs                     # Layer visibility/toggling (if reused)
│
├── Console/                                        ← CONSOLE APP (Terminal.Gui)
│   └── PigeonPea.Console/
│       ├── Views/
│       │   ├── MapPanelView.cs                    # Uses Map.Rendering (Braille)
│       │   ├── DungeonPanelView.cs                # Uses Dungeon.Rendering (Braille)
│       │   └── HudView.cs
│       └── TerminalApplication.cs                 # Manages both map and dungeon ECS worlds
│
└── Desktop/                                        ← DESKTOP APP (Avalonia)
    └── PigeonPea.Desktop/
        └── Views/
            ├── MapView.axaml                       # Uses Map.Control (Mapsui MapControl)
            ├── DungeonView.axaml                   # Uses Dungeon.Rendering (Skia)
            └── MainWindow.axaml
```

### Core/Control/Rendering Trinity Pattern

Each domain follows the same layering:

| Layer         | Responsibility                           | Dependencies            | External Libs                     | Example (Map)                                            |
| ------------- | ---------------------------------------- | ----------------------- | --------------------------------- | -------------------------------------------------------- |
| **Core**      | Data models, generation, domain logic    | None (or minimal)       | FantasyMapGenerator.Core, GoRogue | `MapData`, `IMapGenerator`, `FantasyMapGeneratorAdapter` |
| **Control**   | Navigation, interaction, input handling  | Core + UI framework     | Mapsui (Map), GoRogue (Dungeon)   | `MapNavigator`, `MapControlViewModel`                    |
| **Rendering** | Visualization, drawing to screen/console | Core + Shared.Rendering | SkiaSharp                         | `SkiaMapRasterizer`, `BrailleMapRenderer`                |

### Dependency Graph

```
                    ┌─────────────────────┐
                    │   Console/Desktop   │
                    │   Applications      │
                    └──────────┬──────────┘
                               │
                ┌──────────────┼──────────────┐
                │              │              │
                ↓              ↓              ↓
        ┌───────────┐   ┌───────────┐   ┌──────────┐
        │ Map.      │   │ Dungeon.  │   │ Shared.  │
        │ Control   │   │ Control   │   │ ECS      │
        └─────┬─────┘   └─────┬─────┘   └────┬─────┘
              │               │               │
              ↓               ↓               ↓
        ┌───────────┐   ┌───────────┐   ┌──────────┐
        │ Map.      │   │ Dungeon.  │   │ Shared.  │
        │ Rendering │   │ Rendering │   │ Rendering│
        └─────┬─────┘   └─────┬─────┘   └────┬─────┘
              │               │               │
              ↓               ↓               │
        ┌───────────┐   ┌───────────┐        │
        │ Map.Core  │   │ Dungeon.  │        │
        │           │   │ Core      │        │
        └─────┬─────┘   └─────┬─────┘        │
              │               │               │
              ↓               ↓               │
    ┌──────────────┐   ┌──────────────┐      │
    │ FMG.Core     │   │ GoRogue      │      │
    │ (external)   │   │ (external)   │      │
    └──────────────┘   └──────────────┘      │
              │               │               │
              └───────────────┴───────────────┘
                              ↓
                    ┌──────────────────┐
                    │ Shared.Rendering │
                    │ (primitives,     │
                    │  text, tiles)    │
                    └──────────────────┘
```

### Migration from Current Structure

#### Source Projects (Current State)

```
dotnet/_lib/fantasy-map-generator-port/src/
├── FantasyMapGenerator.Core/              → KEEP (external lib)
└── FantasyMapGenerator.Rendering/         → DEPRECATE (merge into Map.Rendering)

dotnet/shared-app/
└── Rendering/                             → SPLIT (Map + Shared)
    ├── SkiaMapRasterizer.cs              → Map.Rendering/
    ├── MapDataRenderer.cs                → Map.Rendering/BrailleMapRenderer.cs
    ├── NavigatorAdapter.cs               → Map.Control/ (or deprecate for Mapsui)
    ├── IRenderer.cs, Camera.cs, etc.     → Shared.Rendering/
    └── Tiles/                            → Shared.Rendering/Tiles/
```

#### Destination Projects (After Reorganization)

```
Map/
├── PigeonPea.Map.Core/
│   └── Adapters/FantasyMapGeneratorAdapter.cs    ← NEW (wraps FMG.Core)
├── PigeonPea.Map.Control/
│   ├── MapNavigator.cs                           ← NEW (Mapsui integration)
│   └── ViewModels/MapControlViewModel.cs         ← NEW
└── PigeonPea.Map.Rendering/
    ├── SkiaMapRasterizer.cs                      ← FROM SharedApp.Rendering
    ├── BrailleMapRenderer.cs                     ← FROM SharedApp.Rendering (renamed)
    ├── ColorSchemes.cs                           ← FROM FMG.Rendering (merged)
    ├── SmoothMapRenderer.cs                      ← FROM FMG.Rendering
    └── Tiles/MapTileSource.cs                    ← MERGED (FMG + SharedApp)

Dungeon/
├── PigeonPea.Dungeon.Core/
│   └── Adapters/GoRogueAdapter.cs                ← NEW (wraps GoRogue)
├── PigeonPea.Dungeon.Control/
│   ├── FovCalculator.cs                          ← NEW (GoRogue FOV)
│   ├── PathfindingService.cs                     ← NEW (GoRogue pathfinding)
│   └── ViewModels/DungeonControlViewModel.cs     ← NEW
└── PigeonPea.Dungeon.Rendering/
    ├── TilesetRenderer.cs                        ← NEW
    ├── BrailleDungeonRenderer.cs                 ← NEW
    └── Tiles/DungeonTileSource.cs                ← NEW

Shared/
├── PigeonPea.Shared.Rendering/
│   ├── Primitives/
│   │   ├── SkiaPrimitives.cs                     ← NEW
│   │   ├── ColorUtilities.cs                     ← NEW
│   │   └── CurveSmoothing.cs                     ← FROM FMG.Rendering
│   ├── Text/
│   │   ├── BrailleConverter.cs                   ← NEW (extract from renderers)
│   │   └── AsciiConverter.cs                     ← NEW
│   └── Tiles/
│       ├── ITileSource.cs                        ← FROM SharedApp.Rendering
│       ├── TileAssembler.cs                      ← FROM SharedApp.Rendering
│       └── TileCache.cs                          ← FROM SharedApp.Rendering
└── PigeonPea.Shared.ECS/
    ├── Components/                               ← NEW
    └── Systems/                                  ← NEW
```

## Implementation Plan

### Phase 1: Create Project Structure (Week 1, Priority: P0)

**Tasks**:

1. Create all new `.csproj` files (fine-grained projects):
   - `Map.Core`, `Map.Control`, `Map.Rendering`
   - `Dungeon.Core`, `Dungeon.Control`, `Dungeon.Rendering`
   - `Shared.Rendering`, `Shared.ECS`
2. Set up project references (dependency graph)
3. Add NuGet packages:
   - `Map.Control` → Mapsui
   - `Dungeon.Core`, `Dungeon.Control` → GoRogue
   - `*.Rendering` → SkiaSharp
   - `Shared.ECS` → Arch ECS
4. Verify all projects build (empty/stub code)

**Files Created**:

- 8 new `.csproj` files
- Updated solution file
- `docs/rfcs/007-project-structure.md` (detailed project references)

**Success Criteria**:

- All projects build successfully
- No circular dependencies
- Solution compiles

### Phase 2: Migrate Shared Infrastructure (Week 1-2, Priority: P0)

**Tasks**:

1. Create `Shared.Rendering/Tiles/` (move from `SharedApp.Rendering/Tiles/`)
   - `ITileSource.cs`
   - `TileAssembler.cs`
   - `TileCache.cs`
2. Create `Shared.Rendering/Primitives/`
   - Extract Skia helper methods from renderers
   - Move `CurveSmoothing.cs` from `FMG.Rendering`
3. Create `Shared.Rendering/Text/`
   - Extract `BrailleConverter` logic from `MapDataRenderer.cs`
   - Create `AsciiConverter.cs` (if needed)
4. Create `Shared.ECS/Components/`
   - `Position.cs`, `Sprite.cs`, `Renderable.cs`
   - `MapEntityTag.cs`, `DungeonEntityTag.cs`
5. Update references in existing code to use `Shared.Rendering`

**Files Created/Modified**:

- `Shared.Rendering/Tiles/` (3 files moved)
- `Shared.Rendering/Primitives/` (2-3 new files)
- `Shared.Rendering/Text/` (2 new files)
- `Shared.ECS/Components/` (5 new files)

**Success Criteria**:

- Shared infrastructure builds independently
- No domain-specific code in `Shared.*`
- All utilities are truly reusable

### Phase 3: Create Map Domain (Week 2-3, Priority: P0)

**Tasks**:

1. **Map.Core**:
   - Create `IMapGenerator.cs` interface
   - Create `FantasyMapGeneratorAdapter.cs` wrapping `FMG.Core`
   - Re-export `MapData` or create light wrapper
2. **Map.Rendering**:
   - Move `SkiaMapRasterizer.cs` from `SharedApp.Rendering`
   - Move `MapDataRenderer.cs` → rename to `BrailleMapRenderer.cs`
   - Merge `TerrainColorSchemes.cs` (FMG) → `ColorSchemes.cs`
   - Move `SmoothTerrainRenderer.cs` (FMG) → `SmoothMapRenderer.cs`
   - Create `Overlays/` directory (RiverOverlay, BorderOverlay, CityOverlay)
   - Create `Tiles/MapTileSource.cs` (merge FMG + SharedApp tile sources)
3. **Map.Control**:
   - Create `MapNavigator.cs` (wraps `Mapsui.Navigator` directly)
   - Create `MapInteractionHandler.cs` (pan/zoom/click)
   - Create `ViewModels/MapControlViewModel.cs` (ReactiveUI)
4. Update console app to use `Map.Rendering.BrailleMapRenderer`
5. Update desktop app to use `Map.Control` (if applicable)

**Files Created/Modified**:

- `Map.Core/` (3 files)
- `Map.Rendering/` (7+ files)
- `Map.Control/` (3 files)
- Console app references updated
- Desktop app references updated

**Success Criteria**:

- Map domain builds independently
- Console app renders maps using `Map.Rendering`
- Desktop app uses `Map.Control` (Mapsui) or `Map.Rendering` (Skia)
- No references to old `SharedApp.Rendering` for map code

### Phase 4: Create Dungeon Domain (Week 3-4, Priority: P1)

**Tasks**:

1. **Dungeon.Core**:
   - Create `DungeonData.cs` (grid, walls, floor)
   - Create `IDungeonGenerator.cs` interface
   - Create `GoRogueAdapter.cs` wrapping GoRogue generation
2. **Dungeon.Control**:
   - Create `FovCalculator.cs` (wraps GoRogue FOV)
   - Create `PathfindingService.cs` (wraps GoRogue pathfinding)
   - Create `DungeonNavigator.cs` (grid-based navigation)
   - Create `ViewModels/DungeonControlViewModel.cs` (player pos, FOV)
3. **Dungeon.Rendering**:
   - Create `TilesetRenderer.cs` (tileset → sprites)
   - Create `BrailleDungeonRenderer.cs` (console dungeon)
   - Create `FovRenderer.cs` (apply lighting/shadows)
   - Create `EntityRenderer.cs` (render ECS entities)
   - Create `Tiles/DungeonTileSource.cs`
4. Create simple dungeon demo in console app (optional)

**Files Created**:

- `Dungeon.Core/` (3 files)
- `Dungeon.Control/` (4 files)
- `Dungeon.Rendering/` (5+ files)
- Optional: Demo view in console app

**Success Criteria**:

- Dungeon domain builds independently
- Can generate dungeon using GoRogue
- Can render dungeon to console (Braille/ASCII)
- FOV and pathfinding work

### Phase 5: Arch ECS Integration (Week 4-5, Priority: P1)

**Tasks**:

1. Create separate ECS worlds for map and dungeon in console app:
   ```csharp
   var mapWorld = World.Create();
   var dungeonWorld = World.Create();
   ```
2. Implement entity rendering in `Map.Rendering`:
   - Query `MapEntityTag` components from `mapWorld`
   - Render cities, markers, etc. as sprites
3. Implement entity rendering in `Dungeon.Rendering`:
   - Query `DungeonEntityTag` components from `dungeonWorld`
   - Render monsters, items, etc. with FOV filtering
4. Add example entities:
   - Map: Create city entities with Position + Sprite + MapEntityTag
   - Dungeon: Create monster entities with Position + Sprite + DungeonEntityTag
5. Test rendering with ECS entities

**Files Modified**:

- Console app (create worlds)
- `Map.Rendering/` (add ECS query in rasterizer)
- `Dungeon.Rendering/EntityRenderer.cs`

**Success Criteria**:

- Separate ECS worlds for map and dungeon
- Entities render correctly in both domains
- No interference between worlds

### Phase 6: Deprecate Old Projects (Week 5-6, Priority: P2)

**Tasks**:

1. Mark `FantasyMapGenerator.Rendering/` as deprecated:
   - Add `README-DEPRECATED.md` explaining migration
   - Rename project to `FantasyMapGenerator.Rendering.Deprecated`
   - Remove from active build configuration
2. Remove old `SharedApp.Rendering/` (now split into Map/Shared):
   - Verify no references remain
   - Archive to `dotnet/archive/SharedApp.Rendering.old/`
3. Update all documentation:
   - `ARCHITECTURE_MAP_RENDERING.md`
   - `ARCHITECTURE_PLAN.md`
   - `README.md` in each project
4. Update `CLAUDE.md` with new structure

**Files Created/Modified**:

- `FantasyMapGenerator.Rendering/README-DEPRECATED.md`
- `dotnet/archive/SharedApp.Rendering.old/` (archived)
- Updated architecture docs

**Success Criteria**:

- Old rendering projects not in active build
- Clear migration path documented
- No broken references

### Phase 7: Documentation and Examples (Week 6, Priority: P2)

**Tasks**:

1. Create `docs/architecture/domain-organization.md`
2. Document Arch ECS integration patterns
3. Create example code for:
   - Adding new map overlay
   - Adding new dungeon feature
   - Creating new domain (Battle, Overworld)
4. Update onboarding guide for new developers

**Files Created**:

- `docs/architecture/domain-organization.md`
- `docs/examples/add-map-overlay.md`
- `docs/examples/add-dungeon-feature.md`
- `docs/examples/create-new-domain.md`

**Success Criteria**:

- Developers understand domain organization
- Clear examples for extension
- Onboarding guide updated

## Arch ECS Integration Pattern

### Map Domain ECS

```csharp
// In Console/TerminalApplication.cs
public class TerminalApplication
{
    private World _mapWorld;
    private MapEcsRenderer _mapRenderer;

    public void InitializeMap()
    {
        // Create ECS world for map entities
        _mapWorld = World.Create();

        // Populate with cities, markers, etc.
        foreach (var city in _mapData.Burgs)
        {
            var entity = _mapWorld.Create<Position, Sprite, MapEntityTag>();
            _mapWorld.Set(entity, new Position(city.X, city.Y));
            _mapWorld.Set(entity, new Sprite("city-icon.png"));
            _mapWorld.Set(entity, new MapEntityTag());
        }

        // Create renderer
        _mapRenderer = new MapEcsRenderer(_mapWorld);
    }

    private void RenderMap()
    {
        var raster = _mapRenderer.Render(_mapData, _viewport, _renderOptions);
        DisplayRasterInTerminal(raster);
    }
}

// In Map.Rendering/MapEcsRenderer.cs
public class MapEcsRenderer
{
    private readonly World _world;

    public Raster Render(MapData baseMap, Viewport viewport, RenderOptions options)
    {
        // 1. Render base terrain
        var baseRaster = SkiaMapRasterizer.Render(baseMap, viewport, options);

        // 2. Query ECS for map entities
        var query = new QueryDescription().WithAll<Position, Sprite, MapEntityTag>();

        _world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            if (viewport.Contains(pos.X, pos.Y))
            {
                DrawSprite(baseRaster, sprite, pos, viewport);
            }
        });

        return baseRaster;
    }
}
```

### Dungeon Domain ECS

```csharp
// In Console/TerminalApplication.cs (dungeon mode)
public void InitializeDungeon()
{
    _dungeonWorld = World.Create();

    // Populate with monsters, items
    foreach (var monster in _dungeonData.Monsters)
    {
        var entity = _dungeonWorld.Create<Position, Sprite, Health, DungeonEntityTag>();
        _dungeonWorld.Set(entity, new Position(monster.X, monster.Y));
        _dungeonWorld.Set(entity, new Sprite("monster-goblin.png"));
        _dungeonWorld.Set(entity, new Health(monster.MaxHp));
        _dungeonWorld.Set(entity, new DungeonEntityTag());
    }

    _dungeonRenderer = new DungeonEcsRenderer(_dungeonWorld);
}

// In Dungeon.Rendering/DungeonEcsRenderer.cs
public class DungeonEcsRenderer
{
    private readonly World _world;

    public Raster Render(DungeonData dungeon, Viewport viewport, FovData fov)
    {
        // 1. Render tiles (walls, floor)
        var baseRaster = TilesetRenderer.Render(dungeon, viewport);

        // 2. Apply FOV (darken out-of-sight areas)
        FovRenderer.ApplyFov(baseRaster, fov);

        // 3. Render entities (only if in FOV)
        var query = new QueryDescription().WithAll<Position, Sprite, DungeonEntityTag>();

        _world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            if (fov.IsVisible(pos.X, pos.Y))
            {
                DrawSprite(baseRaster, sprite, pos, viewport);
            }
        });

        return baseRaster;
    }
}
```

## Testing Strategy

### Unit Tests (Per Domain)

```
tests/
├── Map.Core.Tests/
│   └── FantasyMapGeneratorAdapterTests.cs
├── Map.Rendering.Tests/
│   ├── SkiaMapRasterizerTests.cs
│   ├── ColorSchemesTests.cs
│   └── SmoothMapRendererTests.cs
├── Dungeon.Core.Tests/
│   └── GoRogueAdapterTests.cs
├── Dungeon.Rendering.Tests/
│   └── TilesetRendererTests.cs
└── Shared.Rendering.Tests/
    ├── BrailleConverterTests.cs
    └── TileAssemblerTests.cs
```

### Integration Tests

```csharp
[Fact]
public void MapDomain_FullPipeline_WorksEndToEnd()
{
    // Generate map
    var adapter = new FantasyMapGeneratorAdapter();
    var map = adapter.Generate(new MapGenerationSettings { Seed = 12345 });

    // Render to Skia
    var raster = SkiaMapRasterizer.Render(map, viewport, options);
    Assert.NotNull(raster);

    // Convert to Braille
    var braille = BrailleConverter.Convert(raster.Rgba, raster.WidthPx, raster.HeightPx);
    Assert.NotEmpty(braille);
}

[Fact]
public void DungeonDomain_WithECS_RendersEntities()
{
    var world = World.Create();
    var dungeon = new DungeonData(width: 50, height: 50);

    // Add monster entity
    var entity = world.Create<Position, Sprite, DungeonEntityTag>();
    world.Set(entity, new Position(10, 10));
    world.Set(entity, new Sprite("goblin"));
    world.Set(entity, new DungeonEntityTag());

    // Render with ECS
    var renderer = new DungeonEcsRenderer(world);
    var raster = renderer.Render(dungeon, viewport, fov);

    // Verify monster rendered (pixel check or snapshot)
    Assert.True(RasterContainsSprite(raster, "goblin", new Position(10, 10)));
}
```

## Alternatives Considered

### Alternative 1: Flat Structure (Original RFC-007)

Merge everything into `SharedApp.Rendering/`.

**Pros**: Fewer projects
**Cons**: Doesn't scale to multiple domains, mixes map/dungeon concerns
**Decision**: Rejected in favor of domain-first approach.

### Alternative 2: Coarse-Grained Projects

One project per domain (e.g., `PigeonPea.Map.csproj` contains Core, Control, Rendering namespaces).

**Pros**: Fewer projects
**Cons**: Less granular dependency management, harder to test layers independently
**Decision**: Rejected. Fine-grained projects provide clearer boundaries.

### Alternative 3: Move FantasyMapGenerator.Core to Map.Generation

Move FMG.Core into `Map/PigeonPea.Map.Generation/`.

**Pros**: All map code co-located
**Cons**: Couples FMG to PigeonPea, prevents standalone NuGet publishing
**Decision**: Rejected. Keep FMG.Core in `_lib/` as external library.

### Alternative 4: Mapsui Navigator Wrapper

Create thin wrapper around Mapsui.Navigator instead of direct exposure.

**Pros**: Could swap Mapsui later
**Cons**: Extra indirection, no immediate benefit
**Decision**: Rejected. Use Mapsui directly (can add wrapper later if needed).

## Risks and Mitigations

| Risk                                 | Probability | Impact | Mitigation                                                                        |
| ------------------------------------ | ----------- | ------ | --------------------------------------------------------------------------------- |
| **Too many projects (complexity)**   | Medium      | Medium | Use clear naming conventions; document dependency graph; create project templates |
| **Breaking existing code**           | High        | High   | Phased migration; keep old code until new structure working; comprehensive tests  |
| **Mapsui integration challenges**    | Medium      | Medium | Start with simple Mapsui usage; iterate based on needs                            |
| **GoRogue integration challenges**   | Medium      | Medium | Prototype dungeon generation early; test FOV/pathfinding                          |
| **Developers confused by structure** | Medium      | Low    | Strong documentation; clear examples; onboarding guide                            |
| **Arch ECS performance**             | Low         | Medium | Benchmark ECS rendering; profile query performance                                |

## Success Criteria

1. ✅ **8+ new projects created**: Map._, Dungeon._, Shared.\*
2. ✅ **No rendering duplication**: Old projects deprecated/archived
3. ✅ **Arch ECS integration**: Separate worlds for map and dungeon entities
4. ✅ **Library encapsulation**: Mapsui in Map.Control, GoRogue in Dungeon.\*
5. ✅ **Console rendering works**: Braille output for both map and dungeon
6. ✅ **Desktop rendering works**: Mapsui MapControl for map, Skia for dungeon
7. ✅ **Tests pass**: Unit and integration tests for all domains
8. ✅ **Documentation complete**: Architecture guide, examples, onboarding

## Timeline

- **Week 1**: Phase 1-2 (project structure + shared infrastructure) - **P0**
- **Week 2-3**: Phase 3 (Map domain) - **P0**
- **Week 3-4**: Phase 4 (Dungeon domain) - **P1**
- **Week 4-5**: Phase 5 (Arch ECS integration) - **P1**
- **Week 5-6**: Phase 6-7 (deprecation + documentation) - **P2**

**Total effort**: ~6 weeks (part-time)

## Open Questions

1. ~~Should projects be fine-grained or coarse-grained?~~ ✅ **Resolved: Fine-grained**
2. ~~Should FMG.Core stay in `_lib/` or move to `Map.Generation`?~~ ✅ **Resolved: Stay in `_lib/`**
3. ~~Should we wrap Mapsui.Navigator or expose directly?~~ ✅ **Resolved: Direct exposure**
4. ~~Where should ViewModels live?~~ ✅ **Resolved: Domain-specific (Control layer)**
5. **Performance budget**: What's acceptable rendering time for map with 1000 ECS entities?
   - Recommendation: <16ms (60 FPS) for desktop, <50ms for console
6. **Shared ViewModels**: Should `LayersViewModel` be in `Shared.ViewModels` or duplicated per domain?
   - Recommendation: Start duplicated, merge to Shared if truly identical

## References

- [ARCHITECTURE_MAP_RENDERING.md](../../ARCHITECTURE_MAP_RENDERING.md)
- [FantasyMapGenerator.Core](../../dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Core/)
- Mapsui: https://github.com/Mapsui/Mapsui
- GoRogue: https://github.com/Chris3606/GoRogue
- Arch ECS: https://github.com/genaray/Arch

## Approval

- [ ] Architecture review (domain organization)
- [ ] Performance review (ECS integration)
- [ ] Library integration review (Mapsui, GoRogue)
- [ ] Timeline approval
- [ ] Ready for implementation

---

**Next Steps**:

1. Create project structure (Phase 1)
2. Begin shared infrastructure migration (Phase 2)
3. Implement Map domain (Phase 3)
4. Update other RFCs (008-011) to reference new domain structure
