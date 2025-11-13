# ECS Usage Examples

These examples summarize how the Phase 5 ECS pieces fit together across dungeon and map domains.

## Dungeon world lifecycle

```csharp
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;

var generator = new BasicDungeonGenerator();
var dungeon = generator.Generate(width: 80, height: 60, seed: 12345);

using var worldManager = new DungeonWorldManager(dungeon);

var player = worldManager.CreatePlayer(x: 10, y: 10, name: "Hero");
worldManager.SpawnRandomMonsters(count: 15, rng: new Random(42));
worldManager.SpawnItem(x: 12, y: 10, itemType: "potion");

worldManager.TryMoveEntity(player, newX: 11, newY: 10);
var playerPos = worldManager.GetPlayerPosition();
Console.WriteLine($"Player at ({playerPos?.X}, {playerPos?.Y})");

bool occupied = worldManager.IsPositionOccupied(11, 10);
worldManager.CleanupDeadMonsters();
```

Key APIs:

- `CreatePlayer`, `SpawnMonster`, `SpawnItem` construct entities with the shared components (`Position`, `Sprite`, `Health`, `Name`).
- `TryMoveEntity` enforces dungeon walkability and collision rules.
- `SpawnRandomMonsters` and `CleanupDeadMonsters` keep population balanced without leaking entities.

## Map world lifecycle

```csharp
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Map.Core.Settings;
using PigeonPea.Map.Control.WorldManager;

var adapter = new FantasyMapGeneratorAdapter();
var mapData = adapter.Generate(new MapGenerationSettings
{
    Width = 1024,
    Height = 768,
    Seed = 20251113,
    Points = 12_000
});

using var mapManager = new MapWorldManager(mapData);

mapManager.PopulateCitiesFromMapData();
mapManager.CreateMarker(200, 180, markerType: "quest", title: "Lost Relic", discovered: true);
mapManager.CreateRandomDungeonMarkers(count: 5, rng: new Random(99));

var cities = mapManager.GetCitiesInViewport(0, 0, 512, 512);
foreach (var (_, pos, data) in cities)
{
    Console.WriteLine($"{data.CityName} at ({pos.X}, {pos.Y})");
}
```

- `MapWorldManager` mirrors the dungeon API but focuses on `CityData`, `MarkerData`, and `MapEntityTag`.
- The adapter keeps the Fantasy Map Generator dependency behind the `IMapGenerator` abstraction.

## Rendering entities

### Dungeon (ASCII + RGBA overlays)

```csharp
using PigeonPea.Dungeon.Rendering;

var asciiBuffer = new char[30, 80];
for (int y = 0; y < asciiBuffer.GetLength(0); y++)
    for (int x = 0; x < asciiBuffer.GetLength(1); x++)
        asciiBuffer[y, x] = '.';

bool[,]? fovMask = null; // supply FOV array when available
EntityRenderer.RenderEntitiesAscii(
    worldManager.World,
    asciiBuffer,
    viewportX: 0,
    viewportY: 0,
    fov: fovMask);

var rgba = new byte[80 * 30 * 4];
EntityRenderer.RenderEntities(
    worldManager.World,
    rgba,
    widthPx: 640,
    heightPx: 480,
    viewportX: 0,
    viewportY: 0,
    pixelsPerCell: 16,
    fov: fovMask);
```

### Map (raster overlays)

```csharp
using PigeonPea.Map.Rendering;

const int viewportSize = 512;
var rgba = new byte[viewportSize * viewportSize * 4];

MapEntityRenderer.RenderEntities(
    mapManager.World,
    rgba,
    widthPx: viewportSize,
    heightPx: viewportSize,
    viewportX: 0,
    viewportY: 0,
    viewportWidth: viewportSize,
    viewportHeight: viewportSize,
    zoom: 1.0);
```

## Querying for gameplay logic

```csharp
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

var playerPos = worldManager.GetPlayerPosition() ?? new Position(0, 0);
var monsterQuery = new QueryDescription().WithAll<Position, MonsterTag>();

worldManager.World.Query(in monsterQuery, (Entity entity, ref Position pos) =>
{
    int distance = Math.Abs(pos.X - playerPos.X) + Math.Abs(pos.Y - playerPos.Y);
    if (distance <= 5)
    {
        var name = worldManager.World.Get<Name>(entity);
        Console.WriteLine($"{name.Value} within 5 tiles");
    }
});
```

- Use `QueryDescription` filters plus tags (`PlayerTag`, `MonsterTag`, `MapEntityTag`) to scope searches.
- The shared `Renderable` component exposes a `Visible` flag and optional `Layer` field for advanced ordering.

## Testing checklist

- `DungeonWorldManagerTests` cover component composition, collision checks, random spawning, and cleanup.
- `EcsIntegrationTests` verify rendering output for dungeon ASCII buffers and map RGBA overlays.
- When writing new tests, reference `CreateOpenDungeon`/`CreateRgbaBuffer` helpers and the `MapWorldManager` reflection helper inside the integration suite.
