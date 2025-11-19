using System;
using FantasyMapGenerator.Core.Models;
using PigeonPea.Shared.Rendering;

namespace PigeonPea.SharedApp.Rendering.Tiles;

public static class TileAssembler
{
    /// <summary>
    /// Assemble a full-frame RGBA buffer by requesting Skia tiles and stitching them.
    /// </summary>
    public static (byte[] Rgba, int WidthPx, int HeightPx) Assemble(
        ITileSource source,
        MapData map,
        Viewport worldViewport,
        int frameCols,
        int frameRows,
        int pixelsPerCell,
        double zoom,
        bool biomeColors = true,
        bool rivers = true,
        int tileCols = 32,
        int tileRows = 16,
        double timeSeconds = 0)
    {
        int ppc = Math.Max(1, pixelsPerCell);
        int widthPx = frameCols * ppc;
        int heightPx = frameRows * ppc;
        var rgba = new byte[widthPx * heightPx * 4];

        int tilesX = (frameCols + tileCols - 1) / tileCols;
        int tilesY = (frameRows + tileRows - 1) / tileRows;

        for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int gx = tx * tileCols;
                int gy = ty * tileRows;
                int wCells = Math.Min(tileCols, frameCols - gx);
                int hCells = Math.Min(tileRows, frameRows - gy);
                var req = new TileRequest(tx, ty, wCells, hCells, ppc);
                var tile = source.GetTile(map, worldViewport, req, zoom, biomeColors, rivers, timeSeconds);

                // Blit tile into final buffer
                int destStride = widthPx * 4;
                int srcStride = tile.WidthPx * 4;
                for (int y = 0; y < tile.HeightPx; y++)
                {
                    int destY = gy * ppc + y;
                    if ((uint)destY >= (uint)heightPx) break;
                    Buffer.BlockCopy(tile.Rgba, y * srcStride, rgba, destY * destStride + gx * ppc * 4, Math.Min(srcStride, destStride - gx * ppc * 4));
                }
            }

        return (rgba, widthPx, heightPx);
    }
}
