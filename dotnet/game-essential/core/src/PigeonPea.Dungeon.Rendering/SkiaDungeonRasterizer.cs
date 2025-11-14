using System;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Rendering;

public static class SkiaDungeonRasterizer
{
    public sealed record Raster(byte[] Rgba, int WidthPx, int HeightPx);

    public static Raster Render(
        DungeonData dungeon,
        int viewportX,
        int viewportY,
        int viewportWidth,
        int viewportHeight,
        int pixelsPerCell = 16,
        bool[,]? fov = null)
    {
        int widthPx = Math.Max(1, viewportWidth * pixelsPerCell);
        int heightPx = Math.Max(1, viewportHeight * pixelsPerCell);
        var rgba = new byte[widthPx * heightPx * 4];

        for (int cy = 0; cy < viewportHeight; cy++)
        {
            for (int cx = 0; cx < viewportWidth; cx++)
            {
                int wx = viewportX + cx;
                int wy = viewportY + cy;

                byte r, g, b, a = 255;
                if (!dungeon.InBounds(wx, wy))
                {
                    r = 0; g = 0; b = 0;
                }
                else if (dungeon.IsDoor(wx, wy))
                {
                    if (dungeon.IsDoorClosed(wx, wy)) { r = 139; g = 69; b = 19; }
                    else { r = 210; g = 180; b = 140; }
                }
                else if (dungeon.IsWalkable(wx, wy))
                {
                    r = 200; g = 200; b = 200;
                }
                else
                {
                    r = 80; g = 80; b = 80;
                }

                if (fov != null)
                {
                    bool visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];
                    if (!visible)
                    {
                        r = (byte)(r * 0.3);
                        g = (byte)(g * 0.3);
                        b = (byte)(b * 0.3);
                    }
                }

                int startPxX = cx * pixelsPerCell;
                int startPxY = cy * pixelsPerCell;
                for (int oy = 0; oy < pixelsPerCell; oy++)
                {
                    int py = startPxY + oy;
                    if ((uint)py >= (uint)heightPx) break;
                    int rowIdx = py * widthPx * 4 + startPxX * 4;
                    for (int ox = 0; ox < pixelsPerCell; ox++)
                    {
                        int idx = rowIdx + ox * 4;
                        if (idx + 3 >= rgba.Length) break;
                        rgba[idx] = r;
                        rgba[idx + 1] = g;
                        rgba[idx + 2] = b;
                        rgba[idx + 3] = a;
                    }
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }
}
