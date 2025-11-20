using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Plugin.Dungeon.Rendering;

/// <summary>
/// Domain plugin that handles the logic of HOW to represent a dungeon visually,
/// but delegates the actual drawing to the platform-specific IRenderer.
/// </summary>
public class DungeonRendererPlugin : IPlugin
{
    private ILogger _logger = null!;

    public string Id => "dungeon-renderer-domain";
    public string Name => "Dungeon Domain Renderer";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _logger.LogInformation("DungeonRenderer domain plugin initialized");

        // Register the DungeonRenderer as a service
        context.Registry.Register<IDungeonRenderer>(
            new DungeonRenderer(),
            new ServiceMetadata
            {
                Priority = 100,
                Name = "DomainDungeonRenderer",
                Version = Version,
                PluginId = Id
            }
        );

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

public class DungeonRenderer : IDungeonRenderer
{
    private IRenderer? _platformRenderer;

    public void Initialize(IRenderer renderer)
    {
        _platformRenderer = renderer;
    }

    public void Render(DungeonView dungeon, int playerX, int playerY)
    {
        if (_platformRenderer == null)
        {
            throw new InvalidOperationException("DungeonRenderer not initialized with platform renderer.");
        }

        _platformRenderer.BeginFrame();
        _platformRenderer.Clear(Color.Black);

        // Render dungeon tiles
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                var tile = GetTileForDungeonCell(dungeon, x, y);
                _platformRenderer.DrawTile(x, y, tile);
            }
        }

        // Render player on top if within bounds
        if (playerX >= 0 && playerX < dungeon.Width && playerY >= 0 && playerY < dungeon.Height)
        {
            var playerTile = new Tile('@', Color.Yellow, Color.Black);
            _platformRenderer.DrawTile(playerX, playerY, playerTile);
        }

        _platformRenderer.EndFrame();
    }

    private static Tile GetTileForDungeonCell(DungeonView dungeon, int x, int y)
    {
        // Doors
        var doorState = dungeon.Doors[x, y];
        if (doorState != 0)
        {
            char glyph = doorState == 1 ? '+' : '/';
            return new Tile(glyph, Color.Brown, Color.Black);
        }

        // Walls
        if (!dungeon.Walkable[x, y])
        {
            return new Tile('#', Color.Gray, Color.Black);
        }

        // Floor
        return new Tile('.', Color.DarkGray, Color.Black);
    }
}
