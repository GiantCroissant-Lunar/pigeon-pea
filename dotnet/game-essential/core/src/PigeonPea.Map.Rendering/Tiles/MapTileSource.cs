using PigeonPea.Map.Core;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.Rendering.Tiles;

namespace PigeonPea.Map.Rendering.Tiles;

public sealed class MapTileSource : ITileSource
{
    private ColorScheme _colorScheme = ColorScheme.Original;
    private bool _showCapitals = true;
    private bool _showDungeons = true;

    /// <summary>
    /// Gets or sets the color scheme used for rendering tiles.
    /// </summary>
    public ColorScheme ColorScheme
    {
        get => _colorScheme;
        set => _colorScheme = value;
    }

    /// <summary>
    /// Gets or sets whether capital city markers are rendered.
    /// </summary>
    public bool ShowCapitals
    {
        get => _showCapitals;
        set => _showCapitals = value;
    }

    /// <summary>
    /// Gets or sets whether dungeon entrance markers are rendered.
    /// </summary>
    public bool ShowDungeons
    {
        get => _showDungeons;
        set => _showDungeons = value;
    }

    public TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers, double timeSeconds)
    {
        int cols = req.TileCols;
        int rows = req.TileRows;
        int ppc = req.PixelsPerCell;

        // Adjust per-tile viewport: each tile covers TileCols/TileRows cells in screen space,
        // so advance world coordinates by that many cells * zoom from the top-left of the
        // overall world viewport. This prevents repeating the same patch of the map.
        int offsetCellsX = req.TileX * cols;
        int offsetCellsY = req.TileY * rows;
        var local = new Viewport(
            worldViewport.X + (int)(offsetCellsX * zoom),
            worldViewport.Y + (int)(offsetCellsY * zoom),
            cols,
            rows);

        var raster = SkiaMapRasterizer.Render(
            map,
            local,
            zoom,
            ppc,
            biomeColors,
            rivers,
            timeSeconds,
            colorScheme: _colorScheme,
            showCapitals: _showCapitals,
            showDungeons: _showDungeons);
        return new TileImage(raster.Rgba, raster.WidthPx, raster.HeightPx);
    }
}
