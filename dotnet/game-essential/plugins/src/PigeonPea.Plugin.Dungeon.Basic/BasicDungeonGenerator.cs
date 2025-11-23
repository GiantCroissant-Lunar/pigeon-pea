using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Arch.Core;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugin.Dungeon.Basic;

public sealed class BasicDungeonGenerator : IPlugin, IDungeonGenerator
{
    private ILogger _logger = null!;

    public string Id => "dungeon-generator-basic";
    public string Name => "Basic Dungeon Generator";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _logger.LogInformation("Basic dungeon generator initialized");

        // Register service
        context.Registry.Register<IDungeonGenerator>(this, new ServiceMetadata
        {
            Name = Name,
            Version = Version,
            PluginId = Id
        });

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public Entity Generate(World world, DungeonGenerationOptions options)
    {
        int width = options.Width;
        int height = options.Height;

        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();
        var d = new DungeonData(width, height);

        // Fill solid walls (non-walkable, opaque)
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                d.SetWall(x, y);

        // Carve N random rectangular rooms with overlap avoidance, store centers
        int roomCount = Math.Max(1, (width * height) / 200);
        var rooms = new List<(int x, int y, int w, int h)>();
        var centers = new List<(int x, int y)>();

        for (int attempts = 0; attempts < roomCount * 10 && rooms.Count < roomCount; attempts++)
        {
            int rw = Math.Max(3, rng.Next(3, Math.Max(4, width / 5)));
            int rh = Math.Max(3, rng.Next(3, Math.Max(4, height / 5)));
            int rx = rng.Next(1, Math.Max(1, width - rw - 1));
            int ry = rng.Next(1, Math.Max(1, height - rh - 1));

            // Check overlap with small padding (1 tile)
            bool overlaps = false;
            foreach (var (ox, oy, ow, oh) in rooms)
            {
                if (rx - 1 < ox + ow + 1 && rx + rw + 1 > ox - 1 &&
                    ry - 1 < oy + oh + 1 && ry + rh + 1 > oy - 1)
                { overlaps = true; break; }
            }
            if (overlaps) continue;

            // Carve room (floors inside, keep 1-tile wall border)
            for (int y = ry; y < ry + rh && y < height; y++)
                for (int x = rx; x < rx + rw && x < width; x++)
                    d.SetFloor(x, y);

            rooms.Add((rx, ry, rw, rh));
            centers.Add((rx + rw / 2, ry + rh / 2));
        }

        // Build a complete graph of room centers with Euclidean weights
        var edges = new List<(int a, int b, double w)>();
        for (int i = 0; i < centers.Count; i++)
            for (int j = i + 1; j < centers.Count; j++)
            {
                var (ax, ay) = centers[i];
                var (bx, by) = centers[j];
                double w = Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
                edges.Add((i, j, w));
            }
        edges.Sort((e1, e2) => e1.w.CompareTo(e2.w));

        // Kruskal MST
        var parent = Enumerable.Range(0, centers.Count).ToArray();
        int Find(int x) => parent[x] == x ? x : (parent[x] = Find(parent[x]));
        void Union(int x, int y) { x = Find(x); y = Find(y); if (x != y) parent[y] = x; }

        var mst = new List<(int a, int b)>();
        foreach (var (a, b, _) in edges)
        {
            if (Find(a) != Find(b)) { Union(a, b); mst.Add((a, b)); }
            if (mst.Count + 1 >= centers.Count) break;
        }

        // Optional extra loops: add a few short non-MST edges
        var rngEdges = new Random(options.Seed ?? Environment.TickCount);
        foreach (var e in edges.Where(e => !mst.Any(m => (m.a == e.a && m.b == e.b) || (m.a == e.b && m.b == e.a))).Take(Math.Max(0, centers.Count / 6)))
        {
            if (rngEdges.NextDouble() < 0.35) mst.Add((e.a, e.b));
        }

        // Carve corridors (min width 2) for each edge in MST/loops
        foreach (var (ia, ib) in mst)
        {
            var (x0, y0) = centers[ia];
            var (x1, y1) = centers[ib];

            // Random corridor widths 1..3
            int hWidth = Math.Clamp(rngEdges.Next(1, 4), 1, 3);
            int vWidth = Math.Clamp(rngEdges.Next(1, 4), 1, 3);

            int sx = Math.Sign(x1 - x0);
            for (int x = x0; x != x1; x += sx)
                for (int wy = 0; wy < hWidth; wy++)
                {
                    int cy = y0 + wy;
                    if (d.InBounds(x, cy)) d.SetFloor(x, cy);
                }

            int sy = Math.Sign(y1 - y0);
            for (int y = y0; y != y1; y += sy)
                for (int wx = 0; wx < vWidth; wx++)
                {
                    int cx = x1 + wx;
                    if (d.InBounds(cx, y)) d.SetFloor(cx, y);
                }
            if (d.InBounds(x1, y1)) d.SetFloor(x1, y1);

            // Place doors only at room boundaries: avoid duplicates and ensure boundary conditions
            TryPlaceDoorAtRoomBoundary(d, rooms, (x0, y0), (x0 + sx, y0));
            TryPlaceDoorAtRoomBoundary(d, rooms, (x1, y1), (x1, y1 - sy));
        }

        return CreateDungeonEntity(world, d);
    }

    private static void TryPlaceDoorAtRoomBoundary(DungeonData d, List<(int x, int y, int w, int h)> rooms, (int x, int y) roomCenter, (int x, int y) entry)
    {
        if (!d.InBounds(roomCenter.x, roomCenter.y) || !d.InBounds(entry.x, entry.y)) return;
        // Room membership
        bool inRoom = rooms.Any(r => roomCenter.x >= r.x && roomCenter.x < r.x + r.w && roomCenter.y >= r.y && roomCenter.y < r.y + r.h);
        if (!inRoom) return;

        // Boundary: entry must be corridor (walkable) and not in any room
        bool entryInRoom = rooms.Any(r => entry.x >= r.x && entry.x < r.x + r.w && entry.y >= r.y && entry.y < r.y + r.h);
        if (!d.IsWalkable(entry.x, entry.y) || entryInRoom) return;

        // Avoid double doors
        if (d.IsDoor(entry.x, entry.y)) return;

        // Ensure adjacent across boundary is room floor and the other side is wall (rough heuristic)
        int dx = Math.Sign(entry.x - roomCenter.x);
        int dy = Math.Sign(entry.y - roomCenter.y);
        int ax = roomCenter.x + dx, ay = roomCenter.y + dy;
        if (!d.InBounds(ax, ay)) return;
        if (!d.IsWalkable(ax, ay)) return;

        d.SetDoorClosed(entry.x, entry.y);
    }

    private static Entity CreateDungeonEntity(World world, DungeonData d)
    {
        var tileData = new byte[d.Width * d.Height];
        var doorStates = new byte[d.Width * d.Height];
        var walkable = new System.Collections.BitArray(d.Width * d.Height);
        var opaque = new System.Collections.BitArray(d.Width * d.Height);
        var doorMetadataList = new List<DoorMetadata>();

        for (int y = 0; y < d.Height; y++)
        {
            for (int x = 0; x < d.Width; x++)
            {
                int index = y * d.Width + x;
                tileData[index] = (byte)(d.Walkable[y, x] ? 1 : 0);
                doorStates[index] = (byte)d.Doors[y, x];
                walkable[index] = d.Walkable[y, x];
                opaque[index] = d.Opaque[y, x];

                // Extract door metadata
                if (d.Doors[y, x] != 0)
                {
                    doorMetadataList.Add(new DoorMetadata(
                        x, y,
                        (PigeonPea.Dungeon.Contracts.Models.DoorState)d.Doors[y, x],
                        Locked: false,
                        Orientation: DetectDoorOrientation(d, x, y)));
                }
            }
        }

        var trapMetadataList = GenerateTraps(d);
        var treasureMetadataList = GenerateTreasure(d);
        var spawnMetadataList = GenerateSpawnPoints(d);
        var stairMetadataList = GenerateStairs(d);

        var featureMetadata = new Dictionary<string, object>
        {
            ["doors"] = JsonSerializer.Serialize(doorMetadataList),
            ["traps"] = JsonSerializer.Serialize(trapMetadataList),
            ["treasure"] = JsonSerializer.Serialize(treasureMetadataList),
            ["spawn_points"] = JsonSerializer.Serialize(spawnMetadataList),
            ["stairs"] = JsonSerializer.Serialize(stairMetadataList)
        };

        return world.Create(
            new DungeonMapComponent
            {
                Width = d.Width,
                Height = d.Height,
                TileData = tileData,
                DoorStates = doorStates,
                Walkable = walkable,
                Opaque = opaque,
                FeatureMetadata = featureMetadata
            },
            new PositionComponent { X = 0, Y = 0, Z = 0 },
            new RenderableComponent
            {
                Glyph = '.',
                Foreground = SadRogue.Primitives.Color.DarkGray,
                Background = SadRogue.Primitives.Color.Black,
                Layer = RenderLayer.Floor
            }
        );
    }

    private static List<TrapMetadata> GenerateTraps(DungeonData d)
    {
        var traps = new List<TrapMetadata>();
        var rng = new Random();
        var trapTypes = new[] { "spike", "arrow", "poison_gas", "pit", "fire" };

        int trapCount = Math.Max(2, (d.Width * d.Height) / 300);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < trapCount * 10 && traps.Count < trapCount; attempts++)
        {
            int x = rng.Next(1, d.Width - 1);
            int y = rng.Next(1, d.Height - 1);

            if (!d.IsWalkable(x, y) || d.IsDoor(x, y) || placed.Contains((x, y)))
                continue;

            traps.Add(new TrapMetadata(
                X: x,
                Y: y,
                Type: trapTypes[rng.Next(trapTypes.Length)],
                Damage: rng.Next(5, 20),
                Radius: rng.Next(1, 4),
                Discovered: false,
                Triggered: false));

            placed.Add((x, y));
        }

        return traps;
    }

    private static List<TreasureMetadata> GenerateTreasure(DungeonData d)
    {
        var treasures = new List<TreasureMetadata>();
        var rng = new Random();
        var containerTypes = new[] { "Chest", "Barrel", "Crate", "Sarcophagus" };

        int treasureCount = Math.Max(1, (d.Width * d.Height) / 400);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < treasureCount * 10 && treasures.Count < treasureCount; attempts++)
        {
            int x = rng.Next(1, d.Width - 1);
            int y = rng.Next(1, d.Height - 1);

            if (!d.IsWalkable(x, y) || d.IsDoor(x, y) || placed.Contains((x, y)))
                continue;

            treasures.Add(new TreasureMetadata(
                X: x,
                Y: y,
                ContainerType: containerTypes[rng.Next(containerTypes.Length)],
                Items: new[] { "potion", "scroll", "gem" },
                Gold: rng.Next(50, 500),
                Opened: false,
                Locked: rng.NextDouble() < 0.3,
                TrapType: rng.NextDouble() < 0.2 ? "poison_needle" : null));

            placed.Add((x, y));
        }

        return treasures;
    }

    private static List<SpawnPointMetadata> GenerateSpawnPoints(DungeonData d)
    {
        var spawns = new List<SpawnPointMetadata>();
        var rng = new Random();
        var monsterTypes = new[] { "goblin", "orc", "skeleton", "spider", "rat" };

        int spawnCount = Math.Max(3, (d.Width * d.Height) / 250);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < spawnCount * 10 && spawns.Count < spawnCount; attempts++)
        {
            int x = rng.Next(1, d.Width - 1);
            int y = rng.Next(1, d.Height - 1);

            if (!d.IsWalkable(x, y) || d.IsDoor(x, y) || placed.Contains((x, y)))
                continue;

            bool isBoss = spawns.Count == 0 && rng.NextDouble() < 0.15;

            spawns.Add(new SpawnPointMetadata(
                X: x,
                Y: y,
                SpawnType: isBoss ? "boss" : "normal",
                MonsterId: monsterTypes[rng.Next(monsterTypes.Length)],
                Level: rng.Next(1, 10),
                IsBoss: isBoss));

            placed.Add((x, y));
        }

        return spawns;
    }

    private static List<StairMetadata> GenerateStairs(DungeonData d)
    {
        var stairs = new List<StairMetadata>();
        var rng = new Random();

        var walkablePositions = new List<(int x, int y)>();
        for (int y = 1; y < d.Height - 1; y++)
        {
            for (int x = 1; x < d.Width - 1; x++)
            {
                if (d.IsWalkable(x, y) && !d.IsDoor(x, y))
                    walkablePositions.Add((x, y));
            }
        }

        if (walkablePositions.Count > 0)
        {
            var upStairPos = walkablePositions[rng.Next(walkablePositions.Count)];
            stairs.Add(new StairMetadata(
                X: upStairPos.x,
                Y: upStairPos.y,
                Direction: "up",
                DestinationLevel: -1,
                DestinationX: 0,
                DestinationY: 0));

            walkablePositions.Remove(upStairPos);
        }

        if (walkablePositions.Count > 0)
        {
            var downStairPos = walkablePositions[rng.Next(walkablePositions.Count)];
            stairs.Add(new StairMetadata(
                X: downStairPos.x,
                Y: downStairPos.y,
                Direction: "down",
                DestinationLevel: 1,
                DestinationX: 0,
                DestinationY: 0));
        }

        return stairs;
    }

    private static string DetectDoorOrientation(DungeonData d, int x, int y)
    {
        bool hasEastWest = (d.InBounds(x - 1, y) && d.IsWalkable(x - 1, y)) ||
                          (d.InBounds(x + 1, y) && d.IsWalkable(x + 1, y));
        bool hasNorthSouth = (d.InBounds(x, y - 1) && d.IsWalkable(x, y - 1)) ||
                            (d.InBounds(x, y + 1) && d.IsWalkable(x, y + 1));
        return hasEastWest && !hasNorthSouth ? "horizontal" : "vertical";
    }
}
