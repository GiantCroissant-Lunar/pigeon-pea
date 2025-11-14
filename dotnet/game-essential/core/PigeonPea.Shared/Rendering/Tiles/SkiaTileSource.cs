using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.ViewModels;
using FantasyMapGenerator.Core.Models;

namespace PigeonPea.SharedApp.Rendering.Tiles;

public sealed class SkiaTileSource : ITileSource
{
    public LayersViewModel? Layers { get; set; }
    public TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers, double timeSeconds = 0)
    {
        // Shift the world viewport by the tile offset in screen cells
        int offsetCellsX = req.TileX * req.TileCols;
        int offsetCellsY = req.TileY * req.TileRows;
        var tileViewport = new Viewport(
            worldViewport.X + (int)System.Math.Round(offsetCellsX * zoom),
            worldViewport.Y + (int)System.Math.Round(offsetCellsY * zoom),
            req.TileCols,
            req.TileRows);

        int ppc = System.Math.Max(1, req.PixelsPerCell);
        var raster = SkiaMapRasterizer.Render(map, tileViewport, zoom, ppc, biomeColors, rivers, timeSeconds, Layers);
        return new TileImage(raster.Rgba, raster.WidthPx, raster.HeightPx);
    }
}
