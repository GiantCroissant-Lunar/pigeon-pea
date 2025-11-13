using PigeonPea.Map.Core;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.Rendering.Tiles;

namespace PigeonPea.Map.Rendering.Tiles;

public sealed class MapTileSource : ITileSource
{
    public TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers, double timeSeconds)
    {
        int cols = req.TileCols;
        int rows = req.TileRows;
        int ppc = req.PixelsPerCell;
        // Adjust per-tile viewport
        var local = new Viewport(worldViewport.X + req.TileX * (int)zoom, worldViewport.Y + req.TileY * (int)zoom, cols, rows);
        var raster = SkiaMapRasterizer.Render(map, local, zoom, ppc, biomeColors, rivers, timeSeconds);
        return new TileImage(raster.Rgba, raster.WidthPx, raster.HeightPx);
    }
}
