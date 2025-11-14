# ECS Usage Examples

A practical reference for working with the Arch ECS setup in PigeonPea. The examples below cover dungeon + map worlds, rendering, queries, events, tests, and performance tips. There are more than **15 focused snippets** so you can copy/paste the patterns you need.

---

## 1. Dungeon world lifecycle

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
```

- `CreatePlayer`, `SpawnMonster`, `SpawnItem` compose the shared components (`Position`, `Sprite`, `Health`, `Name`).
- `TryMoveEntity` enforces walkability/collision.

## 2. Map world lifecycle

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
```

- `PopulateCitiesFromMapData` materializes `CityData` entities with `MapEntityTag`.
- Use `GetCitiesInViewport` + `GetMarkersInViewport` for UI layers.

## 3. Rendering dungeon entities (ASCII HUD)

```csharp
using PigeonPea.Dungeon.Rendering;

var asciiBuffer = new char[30, 80];
Array.Fill(asciiBuffer, '.');

bool[,]? fovMask = null; // provide FOV array when available
EntityRenderer.RenderEntitiesAscii(
    worldManager.World,
    asciiBuffer,
    viewportX: 0,
    viewportY: 0,
    fov: fovMask);
```

## 4. Rendering dungeon entities (RGBA for Skia/Braille)

```csharp
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

## 5. Rendering map overlays

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

## 6. Updating field of view

```csharp
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.ECS.Components;

var fovQuery = new QueryDescription().WithAll<Position, FieldOfView>();
worldManager.World.Query(in fovQuery, (Entity entity, ref Position pos, ref FieldOfView fov) =>
{
    fov.VisibleTiles.Clear();
    foreach (var visible in ShadowCaster.Raycast(pos.Point, radius: fov.Radius, worldManager.TransparencyMap))
    {
        fov.VisibleTiles.Add(visible);
    }
    if (entity.Has<PlayerComponent>())
    {
        worldManager.MarkTilesAsExplored(fov.VisibleTiles);
    }
});
```

## 7. Publishing combat events

```csharp
using PigeonPea.Shared.Events;

void ResolveMeleeAttack(Entity attacker, Entity defender)
{
    var attackerStats = worldManager.World.Get<CombatStats>(attacker);
    var defenderHealth = worldManager.World.Get<Health>(defender);

    int damage = Math.Max(1, attackerStats.Attack - defenderHealth.Defense);
    defenderHealth.Current -= damage;
    worldManager.World.Set(defender, defenderHealth);

    if (worldManager.PlayerDamagedPublisher is not null && defender.Has<PlayerComponent>())
    {
        worldManager.PlayerDamagedPublisher.Publish(new PlayerDamagedEvent
        {
            Damage = damage,
            RemainingHealth = defenderHealth.Current,
            Source = attackerStats.Attack.ToString()
        });
    }
}
```

## 8. Inventory interactions

```csharp
using PigeonPea.Shared.ECS.Components;

public bool TryUseItem(World world, Entity player, int index)
{
    ref var inventory = ref world.Get<Inventory>(player);
    if (index < 0 || index >= inventory.Items.Count)
        return false;

    var itemEntity = inventory.Items[index];
    if (!world.IsAlive(itemEntity))
        return false;

    if (itemEntity.Has<Consumable>())
    {
        ref var health = ref world.Get<Health>(player);
        var consumable = world.Get<Consumable>(itemEntity);
        health.Current = Math.Min(health.Maximum, health.Current + consumable.HealthRestore);
        world.Set(player, health);
    }

    inventory.Items.RemoveAt(index);
    world.Destroy(itemEntity);
    return true;
}
```

## 9. Querying for gameplay logic

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

## 10. Caching query descriptions

```csharp
static readonly QueryDescription BlocksViewQuery =
    new QueryDescription().WithAll<Position, BlocksMovement>();

public bool IsPositionOccupied(World world, int x, int y)
{
    bool occupied = false;
    world.Query(in BlocksViewQuery, (Entity entity, ref Position pos) =>
    {
        if (pos.X == x && pos.Y == y)
        {
            occupied = true;
        }
    });
    return occupied;
}
```

## 11. Map overlays from ECS

```csharp
var markerQuery = new QueryDescription().WithAll<Position, MarkerData>();
mapManager.World.Query(in markerQuery, (Entity entity, ref Position pos, ref MarkerData marker) =>
{
    overlayContext.DrawIcon(
        x: pos.X,
        y: pos.Y,
        glyph: marker.MarkerType == "quest" ? '?' : '•',
        label: marker.Title);
});
```

## 12. Shared tile assembler for Braille/Sixel

```csharp
using PigeonPea.Shared.Rendering.Tiles;

var frame = TileAssembler.Assemble(
    tileSource: new SkiaTileSource(mapData),
    mapData,
    viewport: new Viewport(0, 0, 120, 80),
    widthPx: 960,
    heightPx: 640,
    pixelsPerCell: 8,
    zoom: 1.0,
    biomeColors: true,
    rivers: true);
```

## 13. MapViewModel camera binding

```csharp
using PigeonPea.Shared.ViewModels;
using PigeonPea.Shared.Rendering;

var mapVm = new MapViewModel(camera: new Camera(80, 24));
mapVm.Update(mapManager.GameWorld);

Console.WriteLine($"Camera @ {mapVm.CameraPosition}");
foreach (var tile in mapVm.VisibleTiles)
{
    if (tile.IsVisible)
    {
        hudBuffer[tile.Y, tile.X] = tile.Glyph;
    }
}
```

## 14. Writing ECS integration tests

```csharp
[Fact]
public void CleanupDeadMonsters_RemovesEntities()
{
    using var manager = new DungeonWorldManager(CreateOpenDungeon());
    var monster = manager.SpawnMonster(5, 5, "goblin", health: 1);

    ref var health = ref manager.World.Get<Health>(monster);
    health.Current = 0;
    manager.World.Set(monster, health);

    manager.CleanupDeadMonsters();
    manager.World.IsAlive(monster).Should().BeFalse();
}
```

## 15. Performance instrumentation

```csharp
var stopwatch = Stopwatch.StartNew();
worldManager.World.Query(in monsterQuery, (Entity entity, ref Position pos) =>
{
    // heavy logic
});
stopwatch.Stop();
Console.WriteLine($"Monster system took {stopwatch.ElapsedMilliseconds} ms");
```

## 16. Batched pathfinding updates

```csharp
var aiQuery = new QueryDescription().WithAll<Position, AIComponent, Health>();
worldManager.World.Query(in aiQuery, (Entity entity, ref Position pos, ref AIComponent ai, ref Health hp) =>
{
    if (!hp.IsAlive) return;
    if (ai.CurrentPath.Count == 0 || NeedsRepath(ai))
    {
        ai.CurrentPath.Clear();
        ai.CurrentPath.AddRange(worldManager.Pathfinder.ShortestPath(pos.Point, playerPos));
    }
});
```

---

## Best practices

1. **Isolate domains** – Control and Rendering layers should only reference their own Core + `Shared`. Avoid Map ↔ Dungeon dependencies.
2. **Cache queries** – `QueryDescription` allocations are cheap but caching eliminates per-frame GC spikes.
3. **Minimize component churn** – Prefer updating structs in-place (`ref var health = ref world.Get<Health>(entity)`), then `world.Set` once.
4. **Reuse buffers** – Braille/Sixel pipelines allocate large arrays; pool them per viewport to avoid LOH pressure.
5. **Publish events** – Use MessagePipe publishers in `GameWorld` to notify UI instead of direct coupling.
6. **Testing** – Use world managers inside `using` statements so Arch `World` disposes cleanly; combine with test helpers (see ECS integration suite).

## Performance tips

- **Batch FOV + cleanup**: run FOV, AI, cleanup in fixed order to minimize repeated queries.
- **Leverage tags**: `PlayerTag`, `MonsterTag`, `MapEntityTag` keep queries narrow.
- **Clamp pixels-per-cell**: Braille looks best between 4–8 ppc; anything higher hurts performance with no gain.
- **Use arch filters**: `.WithAll<T1, T2>().WithNone<Dead>()` avoids branching per entity.
- **Record diagnostics**: `Stopwatch` + `ILogger` around heavy systems makes regressions obvious.

Refer back to [docs/architecture/domain-organization.md](../architecture/domain-organization.md) for how these patterns fit into the Core/Control/Rendering trinity.
