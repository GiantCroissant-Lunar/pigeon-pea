using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Overlays;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Scale;
using SadRogue.Primitives;
using System;
using RenderTile = PigeonPea.Rendering.Contracts.Tile;

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
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

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
    private IScaleManager? _scaleManager;
    private bool _debugMode;

    public void Initialize(IRenderer renderer)
    {
        _platformRenderer = renderer;
    }

    public void SetScaleManager(IScaleManager scaleManager)
    {
        _scaleManager = scaleManager;
    }

    public void RenderWithOverlays(int width, int height, System.Collections.BitArray walkable,
        IEnumerable<IOverlayFeature<GridPosition>> overlays, int playerX, int playerY, int scale = 1)
    {
        if (walkable is null)
        {
            throw new ArgumentNullException(nameof(walkable));
        }

        if (overlays is null)
        {
            throw new ArgumentNullException(nameof(overlays));
        }

        if (_platformRenderer == null)
        {
            throw new InvalidOperationException("DungeonRenderer not initialized with platform renderer.");
        }

        _platformRenderer.BeginFrame();
        _platformRenderer.Clear(Color.Black);

        // Render base dungeon tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = GetBaseTile(walkable, width, x, y);
                _platformRenderer.DrawTile(x, y, tile);
            }
        }

        // Get current zoom from scale manager if available
        var currentZoom = _scaleManager?.CurrentZoom ?? scale;
        var activeScale = _scaleManager?.ActiveScale;

        // Render overlays (doors, traps, treasure, etc.)
        foreach (var overlay in overlays)
        {
            if (ShouldRenderOverlay(overlay, currentZoom, activeScale))
            {
                var tile = GetOverlayTile(overlay, (int)currentZoom);
                if (tile != null)
                {
                    _platformRenderer.DrawTile(overlay.Position.X, overlay.Position.Y, tile.Value);
                }
            }
        }

        // Render player on top if within bounds
        if (playerX >= 0 && playerX < width && playerY >= 0 && playerY < height)
        {
            var playerTile = new RenderTile('@', Color.Yellow, Color.Black);
            _platformRenderer.DrawTile(playerX, playerY, playerTile);
        }

        _platformRenderer.EndFrame();
    }

    [Obsolete("Use the overlay-based Render method instead")]
    public void Render(DungeonView dungeon, int playerX, int playerY)
    {
        if (dungeon is null)
        {
            throw new ArgumentNullException(nameof(dungeon));
        }

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
            var playerTile = new RenderTile('@', Color.Yellow, Color.Black);
            _platformRenderer.DrawTile(playerX, playerY, playerTile);
        }

        _platformRenderer.EndFrame();
    }

    private static RenderTile GetBaseTile(System.Collections.BitArray walkable, int width, int x, int y)
    {
        int index = y * width + x;

        // Walls
        if (!walkable[index])
        {
            return new RenderTile('#', Color.Gray, Color.Black);
        }

        // Floor
        return new RenderTile('.', Color.DarkGray, Color.Black);
    }

    private bool ShouldRenderOverlay(IOverlayFeature<GridPosition> overlay, double currentZoom,
        ScaleConfig? activeScale)
    {
        // Check scale-based overlay rules if ScaleManager is available
        if (activeScale != null && activeScale.OverlayRules != null)
        {
            // Map overlay kind to layer ID (e.g., "trap" -> "dungeon.traps")
            var layerId = overlay.LayerId;

            if (activeScale.OverlayRules.TryGetValue(layerId, out var rule))
            {
                // Check if current zoom is within the rule's zoom range
                if (currentZoom < rule.MinZoom || currentZoom > rule.MaxZoom)
                {
                    return false; // Outside visible zoom range
                }

                // Apply filter rules if specified
                if (!string.IsNullOrEmpty(rule.Filter))
                {
                    if (rule.Filter == "discovered" && overlay.Kind == "trap")
                    {
                        if (overlay.Metadata.TryGetValue("discovered", out var discovered))
                        {
                            if (discovered is bool d && !d)
                                return false; // Hide undiscovered traps
                        }
                    }
                }
            }
            // If no specific rule, check if layer is in allowed layers list
            else if (activeScale.OverlayLayers != null && !activeScale.OverlayLayers.Contains(layerId))
            {
                return false; // Layer not enabled for this scale
            }
        }
        else
        {
            // Fallback to legacy scale-based LOD if no ScaleManager
            if (currentZoom < 2)
            {
                // At small scales, only show essential features
                if (overlay.Kind == "trap" && !_debugMode)
                    return false;
            }
        }

        // State-based visibility rules (always applied)
        if (overlay.Kind == "trap" && !_debugMode)
        {
            if (overlay.Metadata.TryGetValue("discovered", out var discovered))
            {
                if (discovered is bool d && !d)
                    return false; // Hide undiscovered traps
            }
        }

        if (overlay.Kind == "spawn_point" && !_debugMode)
        {
            return false; // Spawn points only visible in debug mode
        }

        return true;
    }

    private static RenderTile? GetOverlayTile(IOverlayFeature<GridPosition> overlay, int scale)
    {
        return overlay.Kind switch
        {
            "door" => GetDoorTile(overlay),
            "trap" => GetTrapTile(overlay),
            "treasure" => GetTreasureTile(overlay),
            "spawn_point" => GetSpawnPointTile(overlay),
            "stairs" => GetStairsTile(overlay),
            _ => null
        };
    }

    private static RenderTile GetDoorTile(IOverlayFeature<GridPosition> overlay)
    {
        // Extract door state from metadata
        var state = overlay.Metadata.TryGetValue("state", out var s) && s is int stateInt
            ? stateInt
            : 1; // Default to closed

        char glyph = state switch
        {
            1 => '+', // Closed
            2 => '/', // Open
            3 => '+', // Locked (same as closed but different color)
            4 => '%', // Broken
            _ => '+'
        };

        Color color = state == 3 ? Color.DarkRed : Color.Brown;
        return new RenderTile(glyph, color, Color.Black);
    }

    private static RenderTile GetTrapTile(IOverlayFeature<GridPosition> overlay)
    {
        var triggered = overlay.Metadata.TryGetValue("triggered", out var t) && t is bool tb && tb;
        char glyph = triggered ? '^' : '^';
        Color color = triggered ? Color.DarkGray : Color.Red;
        return new RenderTile(glyph, color, Color.Black);
    }

    private static RenderTile GetTreasureTile(IOverlayFeature<GridPosition> overlay)
    {
        var opened = overlay.Metadata.TryGetValue("opened", out var o) && o is bool ob && ob;
        char glyph = opened ? '∩' : '∩';
        Color color = opened ? Color.DarkGray : Color.Gold;
        return new RenderTile(glyph, color, Color.Black);
    }

    private static RenderTile GetSpawnPointTile(IOverlayFeature<GridPosition> overlay)
    {
        var isBoss = overlay.Metadata.TryGetValue("is_boss", out var b) && b is bool bb && bb;
        char glyph = isBoss ? '★' : '○';
        Color color = isBoss ? Color.Purple : Color.Cyan;
        return new RenderTile(glyph, color, Color.Black);
    }

    private static RenderTile GetStairsTile(IOverlayFeature<GridPosition> overlay)
    {
        var direction = overlay.Metadata.TryGetValue("direction", out var d) && d is string ds
            ? ds
            : "down";

        char glyph = direction == "up" ? '<' : '>';
        return new RenderTile(glyph, Color.White, Color.Black);
    }

    private static RenderTile GetTileForDungeonCell(DungeonView dungeon, int x, int y)
    {
        // Doors
        var doorState = dungeon.Doors[x, y];
        if (doorState != 0)
        {
            char glyph = doorState == 1 ? '+' : '/';
            return new RenderTile(glyph, Color.Brown, Color.Black);
        }

        // Walls
        if (!dungeon.Walkable[x, y])
        {
            return new RenderTile('#', Color.Gray, Color.Black);
        }

        // Floor
        return new RenderTile('.', Color.DarkGray, Color.Black);
    }
}
