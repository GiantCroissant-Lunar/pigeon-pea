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
        // Optimization: Only render visible area (viewport)
        // For now, render everything (simple)
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                var tile = GetTileForCell(dungeon, x, y, playerX, playerY);
                _platformRenderer.DrawTile(x, y, tile);
            }
        }

        _platformRenderer.EndFrame();
    }

    private Tile GetTileForCell(DungeonView dungeon, int x, int y, int playerX, int playerY)
    {
        // Player
        if (x == playerX && y == playerY)
            return new Tile('@', Color.Yellow, Color.Black);

        // Doors
        // Note: DungeonView.Doors is byte[,]. 1=Closed, 2=Open.
        if (dungeon.Doors[y, x] != 0)
        {
            char glyph = dungeon.Doors[y, x] == 1 ? '+' : '/';
            return new Tile(glyph, Color.Brown, Color.Black);
        }

        // Walls
        if (!dungeon.Walkable[y, x])
            return new Tile('#', Color.Gray, Color.Black);

        // Floor
        return new Tile('.', Color.DarkGray, Color.Black);
    }
}
