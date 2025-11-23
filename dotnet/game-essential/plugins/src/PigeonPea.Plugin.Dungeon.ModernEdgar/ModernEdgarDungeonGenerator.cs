using System.Numerics;
using System.Text.Json;
using Arch.Core;
using Edgar.Core;
using Edgar.Core.Generation;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugin.Dungeon.ModernEdgar;

public class ModernEdgarDungeonGenerator : IPlugin, IDungeonGenerator
{
    private ILogger _logger = null!;

    public string Id => "dungeon-generator-modern-edgar";
    public string Name => "Modern Edgar Dungeon Generator";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger = context.Logger;
        _logger.LogInformation("ModernEdgar dungeon generator initialized");

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
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logger.LogInformation("Generating dungeon with Edgar: {Width}x{Height}", options.Width, options.Height);

        try
        {
            // 1. Create Map Description (Graph)
            var mapDescription = CreateMapDescription(options);

            // 2. Configure Edgar
            var edgarOptions = new DungeonGeneratorOptions
            {
                GridCellSize = 10,
                PrimaryRoomSize = 8,
                CorridorRoomSize = 2,
                RoomsPerRow = Math.Max(1, options.Width / 10),
                Seed = options.Seed
            };

            // 3. Generate Layout
            var generator = new DungeonGenerator<int>(mapDescription, edgarOptions);
            var layout = generator.GenerateLayout();

            // 4. Convert to Entity
            return CreateDungeonEntity(world, layout, options.Width, options.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dungeon using Edgar");
            throw;
        }
    }

    private MapDescription<int> CreateMapDescription(DungeonGenerationOptions options)
    {
        var mapDescription = new MapDescription<int>();
        var random = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();

        // Determine number of rooms based on size
        // GridCellSize is 10, so we fit roughly Width/10 * Height/10 rooms
        int cols = Math.Max(1, options.Width / 10);
        int rows = Math.Max(1, options.Height / 10);
        int roomCount = (cols * rows) / 2; // Fill 50% density
        roomCount = Math.Max(roomCount, 5); // Minimum 5 rooms

        // Add rooms
        for (int i = 0; i < roomCount; i++)
        {
            mapDescription.AddRoom(i);
        }

        // Add connections (Simple Minimum Spanning Tree + random loops)
        // Connect i to i+1 to ensure connectivity
        for (int i = 0; i < roomCount - 1; i++)
        {
            mapDescription.AddConnection(i, i + 1);
        }

        // Add some random connections for loops
        int extraConnections = roomCount / 4;
        for (int i = 0; i < extraConnections; i++)
        {
            var from = random.Next(roomCount);
            var to = random.Next(roomCount);
            if (from != to && mapDescription.GetConnectionMetadata(from, to) == null)
            {
                try
                {
                    mapDescription.AddConnection(from, to);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding extra connection between rooms {From} and {To}", from, to);
                    throw;
                }
            }
        }

        return mapDescription;
    }

    private Entity CreateDungeonEntity(World world, DungeonLayout<int> layout, int width, int height)
    {
        // Create temporary arrays for processing
        var walkable = new bool[height, width];
        var opaque = new bool[height, width];
        var doors = new byte[height, width];

        // Initialize with walls
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                walkable[y, x] = false;
                opaque[y, x] = true;
            }
        }

        // Carve Rooms
        foreach (var room in layout.Rooms)
        {
            CarveRect(walkable, opaque, width, height, room.X, room.Y, room.Width, room.Height);
        }

        // Carve Corridors
        foreach (var corridor in layout.Corridors)
        {
            if (corridor.Points.Count < 2) continue;

            for (int i = 0; i < corridor.Points.Count - 1; i++)
            {
                var p1 = corridor.Points[i];
                var p2 = corridor.Points[i + 1];
                CarveLine(walkable, opaque, width, height, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
            }
        }

        // Convert to ECS component format
        var tileData = new byte[width * height];
        var doorStates = new byte[width * height];
        var walkableArray = new System.Collections.BitArray(width * height);
        var opaqueArray = new System.Collections.BitArray(width * height);
        var doorMetadataList = new List<DoorMetadata>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                tileData[index] = walkable[y, x] ? (byte)1 : (byte)0;
                doorStates[index] = doors[y, x];
                walkableArray[index] = walkable[y, x];
                opaqueArray[index] = opaque[y, x];

                // Extract door metadata
                if (doors[y, x] != 0)
                {
                    doorMetadataList.Add(new DoorMetadata(
                        x, y,
                        (PigeonPea.Dungeon.Contracts.Models.DoorState)doors[y, x],
                        Locked: false,
                        Orientation: DetectDoorOrientation(walkable, width, height, x, y)));
                }
            }
        }

        var trapMetadataList = GenerateTraps(walkableArray, doorStates, width, height);
        var treasureMetadataList = GenerateTreasure(walkableArray, doorStates, width, height);
        var spawnMetadataList = GenerateSpawnPoints(walkableArray, doorStates, width, height);
        var stairMetadataList = GenerateStairs(walkableArray, doorStates, width, height);

        var featureMetadata = new Dictionary<string, object>
        {
            ["doors"] = JsonSerializer.Serialize(doorMetadataList),
            ["traps"] = JsonSerializer.Serialize(trapMetadataList),
            ["treasure"] = JsonSerializer.Serialize(treasureMetadataList),
            ["spawn_points"] = JsonSerializer.Serialize(spawnMetadataList),
            ["stairs"] = JsonSerializer.Serialize(stairMetadataList)
        };

        // Create dungeon entity in the world
        return world.Create(
            new DungeonMapComponent
            {
                Width = width,
                Height = height,
                TileData = tileData,
                DoorStates = doorStates,
                Walkable = walkableArray,
                Opaque = opaqueArray,
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

    private static List<TrapMetadata> GenerateTraps(System.Collections.BitArray walkable, byte[] doors, int width, int height)
    {
        var traps = new List<TrapMetadata>();
        var rng = new Random();
        var trapTypes = new[] { "spike", "arrow", "poison_gas", "pit", "fire" };

        int trapCount = Math.Max(2, (width * height) / 300);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < trapCount * 10 && traps.Count < trapCount; attempts++)
        {
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);
            int index = y * width + x;

            if (!walkable[index] || doors[index] != 0 || placed.Contains((x, y)))
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

    private static List<TreasureMetadata> GenerateTreasure(System.Collections.BitArray walkable, byte[] doors, int width, int height)
    {
        var treasures = new List<TreasureMetadata>();
        var rng = new Random();
        var containerTypes = new[] { "Chest", "Barrel", "Crate", "Sarcophagus" };

        int treasureCount = Math.Max(1, (width * height) / 400);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < treasureCount * 10 && treasures.Count < treasureCount; attempts++)
        {
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);
            int index = y * width + x;

            if (!walkable[index] || doors[index] != 0 || placed.Contains((x, y)))
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

    private static List<SpawnPointMetadata> GenerateSpawnPoints(System.Collections.BitArray walkable, byte[] doors, int width, int height)
    {
        var spawns = new List<SpawnPointMetadata>();
        var rng = new Random();
        var monsterTypes = new[] { "goblin", "orc", "skeleton", "spider", "rat" };

        int spawnCount = Math.Max(3, (width * height) / 250);
        var placed = new HashSet<(int, int)>();

        for (int attempts = 0; attempts < spawnCount * 10 && spawns.Count < spawnCount; attempts++)
        {
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);
            int index = y * width + x;

            if (!walkable[index] || doors[index] != 0 || placed.Contains((x, y)))
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

    private static List<StairMetadata> GenerateStairs(System.Collections.BitArray walkable, byte[] doors, int width, int height)
    {
        var stairs = new List<StairMetadata>();
        var rng = new Random();

        var walkablePositions = new List<(int x, int y)>();
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int index = y * width + x;
                if (walkable[index] && doors[index] == 0)
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

    private string DetectDoorOrientation(bool[,] walkable, int width, int height, int x, int y)
    {
        bool hasEastWest = (x > 0 && walkable[y, x - 1]) || (x < width - 1 && walkable[y, x + 1]);
        bool hasNorthSouth = (y > 0 && walkable[y - 1, x]) || (y < height - 1 && walkable[y + 1, x]);
        return hasEastWest && !hasNorthSouth ? "horizontal" : "vertical";
    }

    private void CarveRect(bool[,] walkable, bool[,] opaque, int width, int height, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int cx = x + dx;
                int cy = y + dy;

                if (IsInBounds(width, height, cx, cy))
                {
                    walkable[cy, cx] = true;
                    opaque[cy, cx] = false;
                }
            }
        }
    }

    private void CarveLine(bool[,] walkable, bool[,] opaque, int width, int height, int x1, int y1, int x2, int y2)
    {
        // Simple horizontal/vertical carving since points are likely aligned
        // But use Bresenham-like logic to be safe

        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Carve a 2-wide path for better playability
            CarvePoint(walkable, opaque, width, height, x1, y1);
            CarvePoint(walkable, opaque, width, height, x1 + 1, y1);
            CarvePoint(walkable, opaque, width, height, x1, y1 + 1);

            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    private void CarvePoint(bool[,] walkable, bool[,] opaque, int width, int height, int x, int y)
    {
        if (IsInBounds(width, height, x, y))
        {
            walkable[y, x] = true;
            opaque[y, x] = false;
        }
    }

    private bool IsInBounds(int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Optional: Keep ConvertToDungeonView for backwards compatibility if needed
    private DungeonView ConvertToDungeonView(DungeonLayout<int> layout, int width, int height)
    {
        var view = new DungeonView
        {
            Width = width,
            Height = height,
            Walkable = new bool[height, width],
            Opaque = new bool[height, width],
            Doors = new byte[height, width]
        };

        // Initialize with walls
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                view.Walkable[y, x] = false;
                view.Opaque[y, x] = true;
            }
        }

        // Carve Rooms
        foreach (var room in layout.Rooms)
        {
            CarveRectView(view, room.X, room.Y, room.Width, room.Height);
        }

        // Carve Corridors
        foreach (var corridor in layout.Corridors)
        {
            if (corridor.Points.Count < 2) continue;

            for (int i = 0; i < corridor.Points.Count - 1; i++)
            {
                var p1 = corridor.Points[i];
                var p2 = corridor.Points[i + 1];
                CarveLineView(view, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
            }
        }

        return view;
    }

    private void CarveRectView(DungeonView view, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int cx = x + dx;
                int cy = y + dy;

                if (IsInViewBounds(view, cx, cy))
                {
                    view.Walkable[cy, cx] = true;
                    view.Opaque[cy, cx] = false;
                }
            }
        }
    }

    private void CarveLineView(DungeonView view, int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            CarvePointView(view, x1, y1);
            CarvePointView(view, x1 + 1, y1);
            CarvePointView(view, x1, y1 + 1);

            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }

    private void CarvePointView(DungeonView view, int x, int y)
    {
        if (IsInViewBounds(view, x, y))
        {
            view.Walkable[y, x] = true;
            view.Opaque[y, x] = false;
        }
    }

    private bool IsInViewBounds(DungeonView view, int x, int y)
    {
        return x >= 0 && x < view.Width && y >= 0 && y < view.Height;
    }
}
