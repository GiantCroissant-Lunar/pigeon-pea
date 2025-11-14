using System;
using PigeonPea.Map.Core;

namespace PigeonPea.Map.Rendering;

public static class SkiaMapRasterizer
{
    public sealed record Raster(byte[] Rgba, int WidthPx, int HeightPx);

    public static Raster Render(MapData map, PigeonPea.Shared.Rendering.Viewport viewport, double zoom, int ppc, bool biomeColors, bool rivers, double timeSeconds = 0, ColorScheme colorScheme = ColorScheme.Original)
    {
        int cols = viewport.Width;
        int rows = viewport.Height;
        int widthPx = Math.Max(1, cols * ppc);
        int heightPx = Math.Max(1, rows * ppc);

        var rgba = new byte[widthPx * heightPx * 4];
        for (int cy = 0; cy < rows; cy++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                double wx = viewport.X + (cx + 0.5) * zoom;
                double wy = viewport.Y + (cy + 0.5) * zoom;
                var cell = map.GetCellAt(wx, wy);
                byte r, g, b, a = 255;
                if (cell == null) { r = 0; g = 0; b = 60; }
                else
                {
                    (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors, colorScheme);
                }

                int startPxX = cx * ppc;
                int startPxY = cy * ppc;
                for (int oy = 0; oy < ppc; oy++)
                {
                    int py = startPxY + oy;
                    if ((uint)py >= (uint)heightPx) break;
                    int rowIdx = py * widthPx * 4 + startPxX * 4;
                    for (int ox = 0; ox < ppc; ox++)
                    {
                        int idx = rowIdx + ox * 4;
                        if (idx + 3 >= rgba.Length) break;
                        rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = a;
                    }
                }
            }
        }

        if (rivers && map.Rivers != null)
        {
            void Plot(int x, int y)
            {
                if ((uint)x >= (uint)widthPx || (uint)y >= (uint)heightPx) return;
                int idx = (y * widthPx + x) * 4;
                rgba[idx] = 0; rgba[idx + 1] = 240; rgba[idx + 2] = 255; rgba[idx + 3] = 255;
            }
            void Line(int x0, int y0, int x1, int y1)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                int err = dx + dy;
                while (true)
                {
                    Plot(x0, y0);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }
            int Px(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
            int Py(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

            foreach (var river in map.Rivers)
            {
                if (river?.Cells == null || river.Cells.Count < 2) continue;
                int prevx = -1, prevy = -1;
                foreach (var cid in river.Cells)
                {
                    if ((uint)cid >= (uint)map.Cells.Count) continue;
                    var c = map.Cells[cid];
                    int x = Px(c.Center.X);
                    int y = Py(c.Center.Y);
                    if (prevx >= 0) Line(prevx, prevy, x, y);
                    prevx = x; prevy = y;
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }

}
