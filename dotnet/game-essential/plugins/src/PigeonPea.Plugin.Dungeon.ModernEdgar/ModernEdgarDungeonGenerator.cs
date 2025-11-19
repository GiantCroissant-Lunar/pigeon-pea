using Microsoft.Extensions.Logging;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Contracts.Plugin;
using Edgar.Core.Generation;
using Edgar.Core;
using System.Numerics;

namespace PigeonPea.Plugin.Dungeon.ModernEdgar;

public class ModernEdgarDungeonGenerator : IPlugin, IDungeonGenerator
{
    private ILogger _logger = null!;

    public string Id => "dungeon-generator-modern-edgar";
    public string Name => "Modern Edgar Dungeon Generator";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
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

    public DungeonView Generate(DungeonGenerationOptions options)
    {
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

            // 4. Convert to DungeonView
            return ConvertToDungeonView(layout, options.Width, options.Height);
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
                catch
                {
                    // Ignore duplicate connection errors
                }
            }
        }

        return mapDescription;
    }

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
            CarveRect(view, room.X, room.Y, room.Width, room.Height);
        }

        // Carve Corridors
        foreach (var corridor in layout.Corridors)
        {
            if (corridor.Points.Count < 2) continue;

            for (int i = 0; i < corridor.Points.Count - 1; i++)
            {
                var p1 = corridor.Points[i];
                var p2 = corridor.Points[i + 1];
                CarveLine(view, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
            }
        }

        return view;
    }

    private void CarveRect(DungeonView view, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int cx = x + dx;
                int cy = y + dy;

                if (IsInBounds(view, cx, cy))
                {
                    view.Walkable[cy, cx] = true;
                    view.Opaque[cy, cx] = false;
                }
            }
        }
    }

    private void CarveLine(DungeonView view, int x1, int y1, int x2, int y2)
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
            CarvePoint(view, x1, y1);
            CarvePoint(view, x1 + 1, y1); 
            CarvePoint(view, x1, y1 + 1);

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

    private void CarvePoint(DungeonView view, int x, int y)
    {
        if (IsInBounds(view, x, y))
        {
            view.Walkable[y, x] = true;
            view.Opaque[y, x] = false;
        }
    }

    private bool IsInBounds(DungeonView view, int x, int y)
    {
        return x >= 0 && x < view.Width && y >= 0 && y < view.Height;
    }
}
