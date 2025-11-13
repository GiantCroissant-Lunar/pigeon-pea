using PigeonPea.Shared.Rendering;
using FantasyMapGenerator.Core.Models;

namespace PigeonPea.SharedApp.Rendering.Tiles;

public readonly record struct TileRequest(int TileX, int TileY, int TileCols, int TileRows, int PixelsPerCell);
public readonly record struct TileImage(byte[] Rgba, int WidthPx, int HeightPx);

public interface ITileSource
{
    // Backward-compatible default: calls the time-aware overload with timeSeconds=0
    TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers)
        => GetTile(map, worldViewport, req, zoom, biomeColors, rivers, 0);

    // Time-aware overload for animation
    TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers, double timeSeconds);
}

